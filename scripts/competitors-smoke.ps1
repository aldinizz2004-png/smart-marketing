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
$script:token = (Call POST /api/auth/register @{ email = "rivals-$id@test.local"; password = 'Passw0rd!234'; fullName = 'Owner'; tenantName = "Rivals $id" }).accessToken
$sub = Call POST /api/tenant/subscriptions @{ duration = 'OneMonth' }
Call POST "/api/tenant/subscriptions/$($sub.id)/activate" @{ paymentReference = "RIV-$id" } | Out-Null
$biz = Call POST /api/businesses @{ name = 'Qahwet Al-Hara'; category = 'Cafe'; city = 'Nablus'; timeZoneId = 'Asia/Hebron'; defaultCurrency = 'ILS' }
$B = "/api/businesses/$($biz.id)"
Call PUT "$B/brand-profile" @{ dialect = 'Palestinian Arabic'; tone = 'friendly'; emojiLevel = 1 } | Out-Null
Call POST "$B/products" @{ name = 'لاتيه الفستق'; price = 14; currency = 'ILS' } | Out-Null

# 1. Add accounts by pasting links
$rival = Call POST "$B/tracked-sources" @{ url = 'https://www.instagram.com/Rival.Cafe/?igsh=abc123'; kind = 'Competitor'; notes = 'Biggest cafe in town' }
Check "competitor added and scanned at once ($($rival.status.code), $($rival.posts) posts)" ($rival.status.code -eq 'Active' -and $rival.posts -eq 12)
Check "link cleaned to one address per account ($($rival.handle))" ($rival.handle -eq 'rival.cafe')
Expect POST "$B/tracked-sources" @{ url = 'instagram.com/rival.cafe/'; kind = 'Competitor' } 409
Expect POST "$B/tracked-sources" @{ url = 'https://instagram.com/p/C0abc123/'; kind = 'Competitor' } 400
Expect POST "$B/tracked-sources" @{ url = 'https://some-shop.example.com'; kind = 'Inspiration' } 400
$inspiration = Call POST "$B/tracked-sources" @{ url = 'https://instagram.com/bakery_queen'; kind = 'Inspiration' }
Check "inspiration account added ($($inspiration.posts) posts)" ($inspiration.posts -eq 12)
$broken = Call POST "$B/tracked-sources" @{ url = 'https://instagram.com/notfound_shop'; kind = 'Competitor' }
Check "unreadable account is Failed ($($broken.status.code))" ($broken.status.code -eq 'Failed')

# 2. Scanning again updates the same posts
$scan = Call POST "$B/tracked-sources/$($rival.id)/scan"
Check "re-scan adds nothing and refreshes 12 ($($scan.added)/$($scan.updated))" ($scan.added -eq 0 -and $scan.updated -eq 12)
$posts = (Call GET "$B/tracked-sources/$($rival.id)/posts?pageSize=50").items
$reel = $posts | ? { $_.format -eq 'Reel' -and $_.hookType -eq 'Question' } | Select-Object -First 1
Check "posts have format, hook, hashtags and engagement" ($null -ne $reel -and $reel.hashtags.Count -gt 0 -and $reel.engagementRate -gt 0)
$list = Call GET "$B/tracked-sources?kind=Competitor"
Check "filter by kind ($($list.totalCount))" ($list.totalCount -eq 2)

# 3. Learn the patterns
$analysis = Call POST "$B/competitor-insights/analyze"
Check "analysis of $($analysis.postsAnalyzed) posts from $($analysis.sources) sources -> $($analysis.insights.Count) insights" ($analysis.insights.Count -ge 3 -and $analysis.postsAnalyzed -eq 24)
"--- insights ---"
$analysis.insights | % { "[{0}] {1}`n      -> {2}" -f $_.type, $_.summary, $_.applyToUs }
$latest = Call GET "$B/competitor-insights"
Check "latest insights listed ($($latest.Count))" ($latest.Count -eq $analysis.insights.Count)

# 4. Recommendations follow what was learned
$run = Call POST "$B/strategy-runs" @{ windowDays = 7 }
$cited = @($run.recommendations | ? { $_.evidence.Count -gt 0 })
Check "recommendations cite competitor insights ($($cited.Count) of $($run.recommendations.Count))" ($cited.Count -gt 0)
if ($cited.Count -gt 0) { "      evidence: $($cited[0].evidence[0].interpretation)" }

# 5. Learning is allowed, copying is not
$camp = Call POST "$B/campaigns" @{ name = 'Launch'; objective = 'Orders'; goalStatement = 'Sell more pistachio lattes' }
Expect POST "$B/content-items" @{ campaignId = $camp.id; platformCode = 'instagram'; contentType = 'Post'; firstRevision = @{ text = 'شو أطيب إشي بتطلبوه الصبح؟ شوفوا كيف بنحضر قهوتنا'; format = 'Text' } } 400
$own = Call POST "$B/content-items" @{ campaignId = $camp.id; platformCode = 'instagram'; contentType = 'Post'; firstRevision = @{ text = 'لاتيه الفستق الجديد بـ 14 شيكل، جربته؟ مستنيينك'; format = 'Text' } }
Check "our own text is accepted" ($null -ne $own.id)
$ai = Call POST "$B/content-items" @{ campaignId = $camp.id; platformCode = 'instagram'; contentType = 'Post'; firstRevision = @{ generateWithAi = $true; format = 'Text' } }
Check "AI post written with the insights passes the copy guard" ($null -ne $ai.id)

# 6. Pause, resume, delete
Call PATCH "$B/tracked-sources/$($inspiration.id)/status" @{ status = 'Paused' } | Out-Null
Expect POST "$B/tracked-sources/$($inspiration.id)/scan" $null 409
$resumed = Call PATCH "$B/tracked-sources/$($inspiration.id)/status" @{ status = 'Active' }
Check "resumed ($($resumed.status.code))" ($resumed.status.code -eq 'Active')
Call DELETE "$B/tracked-sources/$($broken.id)" | Out-Null
Expect GET "$B/tracked-sources/$($broken.id)" $null 404
