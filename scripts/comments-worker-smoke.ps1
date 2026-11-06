# Start the API with a short interval first:
#   $env:Ai__Provider='Fake'; $env:Comments__CheckEverySeconds='3'; dotnet run --project SmartMarketingPlatform --launch-profile http
param([string]$Base = "http://localhost:5233")
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

$outbox = Join-Path $PSScriptRoot '..\SmartMarketingPlatform\App_Data\outbox'
function AlertsFor($name) {
  if (-not (Test-Path $outbox)) { return @() }
  @(Get-ChildItem $outbox -Filter *.html | Where-Object { (Get-Content $_.FullName -Raw -Encoding UTF8) -match [regex]::Escape("Subject: ${name}:") })
}

$script:token = $null
$id = [guid]::NewGuid().ToString('N').Substring(0, 8)
$name = "Worker Cafe $id"
$script:token = (Call POST /api/auth/register @{ email = "cworker-$id@test.local"; password = 'Passw0rd!234'; fullName = 'Owner'; tenantName = "CWorker $id" }).accessToken
$sub = Call POST /api/tenant/subscriptions @{ duration = 'OneMonth' }
Call POST "/api/tenant/subscriptions/$($sub.id)/activate" @{ paymentReference = "CWK-$id" } | Out-Null
$biz = Call POST /api/businesses @{ name = $name; category = 'Cafe'; city = 'Nablus'; timeZoneId = 'Asia/Hebron'; defaultCurrency = 'ILS' }
$B = "/api/businesses/$($biz.id)"
$account = Call POST "$B/social-accounts" @{ platformCode = 'instagram'; externalAccountId = "ig_$id"; handle = "worker.$id"; accessToken = 'fake-token' }
$camp = Call POST "$B/campaigns" @{ name = 'Launch'; objective = 'Orders'; goalStatement = 'Sell more lattes' }
$item = Call POST "$B/content-items" @{ campaignId = $camp.id; platformCode = 'instagram'; contentType = 'Post'; firstRevision = @{ text = 'New pistachio latte, 14 ILS!'; format = 'Text' } }
Call PATCH "$B/content-items/$($item.id)/status" @{ status = 'InReview' } | Out-Null
Call POST "$B/content-items/$($item.id)/reviews" @{ decision = 'Approved' } | Out-Null
Call POST "$B/content-items/$($item.id)/schedule" @{ socialAccountId = $account.id } | Out-Null

$published = $false
foreach ($i in 1..15) { Start-Sleep 2; $c = Call GET "$B/content-items/$($item.id)"; if ($c.status.code -eq 'Published') { $published = $true; break } }
Check "post published by the worker" $published

$sync = Call POST "$B/comments/sync"
Check "sync added 5 new comments ($($sync.added))" ($sync.added -eq 5)

$analyzed = $null
foreach ($i in 1..12) {
  Start-Sleep 2
  $page = Call GET "$B/comments?status=New"
  if ($page.totalCount -eq 0) { $analyzed = (Call GET "$B/comments?status=Analyzed").totalCount; break }
}
Check "worker analyzed all comments without calling analyze ($analyzed)" ($analyzed -eq 5)

$complaints = Call GET "$B/comments?category=Complaint"
Check "complaint found ($($complaints.totalCount))" ($complaints.totalCount -ge 1)

$alerts = @()
foreach ($i in 1..5) { $alerts = AlertsFor $name; if ($alerts.Count -gt 0) { break }; Start-Sleep 1 }
Check "one complaint email in the outbox ($($alerts.Count))" ($alerts.Count -eq 1)
if ($alerts.Count -gt 0) {
  $html = Get-Content $alerts[0].FullName -Raw -Encoding UTF8
  Check "email goes to the owner" ($html -match [regex]::Escape("cworker-$id@test.local"))
  Check "email shows the complaint text" ($html -match [regex]::Escape($complaints.items[0].body.Substring(0, [Math]::Min(10, $complaints.items[0].body.Length))))
}

Start-Sleep 7
Check "no second email for the same comments" ((AlertsFor $name).Count -eq 1)
$again = Call POST "$B/comments/sync"
Check "nothing new to sync ($($again.added))" ($again.added -eq 0)
