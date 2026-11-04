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
function Expect($method, $path, $body, [int]$status) {
  try { Call $method $path $body | Out-Null; "FAIL expected $status from $method $path" } catch { if ("$_" -match "-> $status") { "PASS $status $method $path" } else { "FAIL $_" } }
}
function Check($name, $ok) { if ($ok) { "PASS $name" } else { "FAIL $name" } }

$script:token = $null
$id = [guid]::NewGuid().ToString('N').Substring(0, 8)
$script:token = (Call POST /api/auth/register @{ email = "comments-$id@test.local"; password = 'Passw0rd!234'; fullName = 'Owner'; tenantName = "Comments $id" }).accessToken
$sub = Call POST /api/tenant/subscriptions @{ duration = 'OneMonth' }
Call POST "/api/tenant/subscriptions/$($sub.id)/activate" @{ paymentReference = "CMT-$id" } | Out-Null
$biz = Call POST /api/businesses @{ name = 'Qahwet Al-Hara'; category = 'Cafe'; city = 'Nablus'; timeZoneId = 'Asia/Hebron'; defaultCurrency = 'ILS' }
$B = "/api/businesses/$($biz.id)"
Call PUT "$B/brand-profile" @{ dialect = 'Palestinian Arabic'; tone = 'friendly'; emojiLevel = 1 } | Out-Null
Call POST "$B/brand-terms" @{ term = 'رخيص'; rule = 'Prohibited' } | Out-Null
$account = Call POST "$B/social-accounts" @{ platformCode = 'instagram'; externalAccountId = "ig_$id"; handle = 'qahwet.alhara'; accessToken = 'fake-token' }
$camp = Call POST "$B/campaigns" @{ name = 'Launch'; objective = 'Orders'; goalStatement = 'Sell more pistachio lattes' }
$item = Call POST "$B/content-items" @{ campaignId = $camp.id; platformCode = 'instagram'; contentType = 'Post'; firstRevision = @{ text = 'New pistachio latte, 14 ILS!'; format = 'Text' } }
Call PATCH "$B/content-items/$($item.id)/status" @{ status = 'InReview' } | Out-Null
Call POST "$B/content-items/$($item.id)/reviews" @{ decision = 'Approved' } | Out-Null
try { Call POST "$B/content-items/$($item.id)/schedule" @{ socialAccountId = $account.id } | Out-Null } catch { "schedule: $_" }

$published = $false
foreach ($i in 1..15) { Start-Sleep 2; $c = Call GET "$B/content-items/$($item.id)"; if ($c.status.code -eq 'Published') { $published = $true; break } }
Check "post published by the worker" $published

$sync = Call POST "$B/comments/sync"
Check "sync added 5 comments ($($sync.added))" ($sync.added -eq 5)
$again = Call POST "$B/comments/sync"
Check "second sync adds nothing ($($again.added))" ($again.added -eq 0)

$analysis = Call POST "$B/comments/analyze"
Check "analyzed 5, 4 need a reply ($($analysis.analyzed)/$($analysis.needReply))" ($analysis.analyzed -eq 5 -and $analysis.needReply -eq 4)

$all = (Call GET "$B/comments?pageSize=50").items
"--- comments ---"
$all | Sort-Object postedAt | % { "[{0}] {1}: {2}`n      -> {3}" -f $_.category, $_.authorName, $_.body, $_.suggestedReply }

$complaint = $all | ? { $_.category -eq 'Complaint' } | Select-Object -First 1
$praise = $all | ? { $_.category -eq 'Praise' } | Select-Object -First 1
$spam = $all | ? { $_.category -eq 'Spam' } | Select-Object -First 1

$filtered = Call GET "$B/comments?category=Complaint&needsReply=true"
Check "filter by category + needsReply ($($filtered.totalCount))" ($filtered.totalCount -eq 1)

$replied = Call POST "$B/comments/$($praise.id)/reply" @{}
Check "reply with the AI suggestion" ($replied.status.code -eq 'Replied' -and $replied.replyBody -eq $praise.suggestedReply)
$custom = Call POST "$B/comments/$($complaint.id)/reply" @{ text = 'آسفين جداً، رح نتواصل معك بالخاص هلأ' }
Check "reply with the owner's own text" ($custom.replyBody -like 'آسفين*')
Expect POST "$B/comments/$($complaint.id)/reply" @{ text = 'again' } 409
Expect POST "$B/comments/$($praise.id)/ignore" $null 409
$question = $all | ? { $_.category -eq 'Question' } | Select-Object -First 1
Expect POST "$B/comments/$($question.id)/reply" @{ text = 'عنا أرخص لاتيه، رخيص كتير' } 400
$ignored = Call POST "$B/comments/$($spam.id)/ignore"
Check "ignore spam" ($ignored.status.code -eq 'Ignored')
Expect POST "$B/comments/$($spam.id)/reply" @{} 400
