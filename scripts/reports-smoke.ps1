param([string]$Base = "http://localhost:5233", [string]$Outbox = "$PSScriptRoot\..\SmartMarketingPlatform\App_Data\outbox")
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8

function Call($method, $path, $body = $null) {
  $req = @{ Method = $method; Uri = "$Base$path"; Headers = @{ Authorization = "Bearer $script:token" }; ContentType = 'application/json; charset=utf-8' }
  if ($null -ne $body) { $req.Body = [Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 10)) }
  try { Invoke-RestMethod @req } catch {
    $detail = $_.ErrorDetails.Message
    if (-not $detail -and $_.Exception.Response) { $detail = (New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() }
    throw "$method $path -> $($_.Exception.Response.StatusCode.value__): $detail"
  }
}
function Check($name, $ok) { if ($ok) { "PASS $name" } else { "FAIL $name" } }

$script:token = $null
$id = [guid]::NewGuid().ToString('N').Substring(0, 8)
$script:token = (Call POST /api/auth/register @{ email = "reports-$id@test.local"; password = 'Passw0rd!234'; fullName = 'Owner'; tenantName = "Reports $id" }).accessToken
$sub = Call POST /api/tenant/subscriptions @{ duration = 'OneMonth' }
Call POST "/api/tenant/subscriptions/$($sub.id)/activate" @{ paymentReference = "RPT-$id" } | Out-Null
$biz = Call POST /api/businesses @{ name = 'Qahwet Al-Hara'; category = 'Cafe'; city = 'Nablus'; timeZoneId = 'Asia/Hebron'; defaultCurrency = 'ILS' }
$B = "/api/businesses/$($biz.id)"
Call PUT "$B/brand-profile" @{ dialect = 'Palestinian Arabic'; tone = 'friendly'; emojiLevel = 1 } | Out-Null
Call POST "$B/products" @{ name = 'Pistachio latte'; price = 14; currency = 'ILS' } | Out-Null
$now = (Get-Date).ToUniversalTime()
$offer = Call POST "$B/offers" @{ title = '20% off knafeh'; discountPercent = 20; startsAt = $now.ToString('o'); endsAt = $now.AddDays(3).ToString('o') }
Call PATCH "$B/offers/$($offer.id)/status" @{ status = 'Active' } | Out-Null
$account = Call POST "$B/social-accounts" @{ platformCode = 'instagram'; externalAccountId = "ig_$id"; handle = 'qahwet.alhara'; accessToken = 'fake-token' }
$camp = Call POST "$B/campaigns" @{ name = 'Launch'; objective = 'Orders'; goalStatement = 'Sell more pistachio lattes' }
foreach ($n in 1..2) {
  $item = Call POST "$B/content-items" @{ campaignId = $camp.id; platformCode = 'instagram'; contentType = 'Post'; firstRevision = @{ text = "Pistachio latte post $n, 14 ILS"; format = 'Text' } }
  Call PATCH "$B/content-items/$($item.id)/status" @{ status = 'InReview' } | Out-Null
  Call POST "$B/content-items/$($item.id)/reviews" @{ decision = 'Approved' } | Out-Null
  Call POST "$B/content-items/$($item.id)/schedule" @{ socialAccountId = $account.id } | Out-Null
}
$waiting = Call POST "$B/content-items" @{ campaignId = $camp.id; platformCode = 'instagram'; contentType = 'Post'; firstRevision = @{ text = 'Waiting for approval'; format = 'Text' } }
Call PATCH "$B/content-items/$($waiting.id)/status" @{ status = 'InReview' } | Out-Null

foreach ($i in 1..20) { Start-Sleep 2; $pubs = Call GET "$B/publications"; if ($pubs.totalCount -ge 2) { break } }
Check "two posts published ($($pubs.totalCount))" ($pubs.totalCount -ge 2)
foreach ($p in $pubs.items) { Call POST "$B/publications/$($p.id)/metrics/refresh" | Out-Null }
Call POST "$B/comments/sync" | Out-Null
Call POST "$B/comments/analyze" | Out-Null

$before = @(Get-ChildItem $Outbox -Filter *.html -ErrorAction SilentlyContinue).Count
$report = Call POST "$B/reports" @{ days = 7; send = $true }
Check "report generated and sent ($($report.sentTo))" ($null -ne $report.sentAt -and $report.sentTo -like "reports-$id*")
Check "facts: 2 posts, 10 comments, 2 open complaints, 1 awaiting review, 1 offer ending" ($report.facts.posts -eq 2 -and $report.facts.newComments -eq 10 -and $report.facts.unansweredComplaints -eq 2 -and $report.facts.awaitingReview -eq 1 -and $report.facts.offersEndingSoon.Count -eq 1)
$after = @(Get-ChildItem $Outbox -Filter *.html).Count
Check "email written to the outbox" ($after -eq $before + 1)

"--- report ---"
"Headline: $($report.headline)"
"Summary:  $($report.summary)"
"Wins:     " + ($report.wins -join ' | ')
"Concerns: " + ($report.concerns -join ' | ')
"Actions:  " + ($report.nextActions -join ' | ')

$list = Call GET "$B/reports"
Check "report listed ($($list.totalCount))" ($list.totalCount -eq 1)
$html = Invoke-WebRequest -Uri "$Base$B/reports/$($report.id)/html" -Headers @{ Authorization = "Bearer $script:token" } -UseBasicParsing
Check "html preview is right-to-left" ($html.Content -match 'dir="rtl"')
$latest = Get-ChildItem $Outbox -Filter *.html | Sort-Object LastWriteTime | Select-Object -Last 1
"Email file: $($latest.FullName)"
