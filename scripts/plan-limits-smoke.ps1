param([string]$Base = "http://localhost:5233")
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8

function Call($method, $path, $body = $null) {
  $req = @{ Method = $method; Uri = "$Base$path"; ContentType = 'application/json; charset=utf-8' }
  if ($script:token) { $req.Headers = @{ Authorization = "Bearer $script:token" } }
  if ($null -ne $body) { $req.Body = [Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 10)) }
  try { Invoke-RestMethod @req } catch {
    $detail = $_.ErrorDetails.Message
    if (-not $detail -and $_.Exception.Response) { $detail = (New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() }
    throw "$method $path -> $($_.Exception.Response.StatusCode.value__): $detail"
  }
}
function Expect($method, $path, $body, [int]$status, [string]$contains = '') {
  try { Call $method $path $body | Out-Null; "FAIL expected $status from $method $path" }
  catch { if ("$_" -match "-> $status" -and "$_" -like "*$contains*") { "PASS $status $method $path $contains" } else { "FAIL $_" } }
}
function Check($name, $ok) { if ($ok) { "PASS $name" } else { "FAIL $name" } }

# A tenant with an active subscription of the given plan, one business and a connected Instagram account.
function NewTenant($plan) {
  $script:token = $null
  $id = [guid]::NewGuid().ToString('N').Substring(0, 8)
  $script:token = (Call POST /api/auth/register @{ email = "plan-$id@test.local"; password = 'Passw0rd!234'; fullName = 'Owner'; tenantName = "Plan $id" }).accessToken
  $sub = Call POST /api/tenant/subscriptions @{ plan = $plan; duration = 'OneMonth' }
  $active = Call POST "/api/tenant/subscriptions/$($sub.id)/activate" @{ paymentReference = "PLAN-$id" }
  $biz = Call POST /api/businesses @{ name = 'Qahwet Al-Hara'; category = 'Cafe'; city = 'Nablus'; timeZoneId = 'Asia/Hebron'; defaultCurrency = 'ILS' }
  $script:B = "/api/businesses/$($biz.id)"
  $script:account = Call POST "$B/social-accounts" @{ platformCode = 'instagram'; externalAccountId = "ig_$id"; handle = 'qahwet'; accessToken = 'fake-token' }
  $script:camp = Call POST "$B/campaigns" @{ name = 'Launch'; objective = 'Orders'; goalStatement = 'Sell more' }
  $active
}
function ApprovedPost($n) {
  $item = Call POST "$B/content-items" @{ campaignId = $camp.id; platformCode = 'instagram'; contentType = 'Post'; firstRevision = @{ text = "Post number $n of the day"; format = 'Text' } }
  Call PATCH "$B/content-items/$($item.id)/status" @{ status = 'InReview' } | Out-Null
  Call POST "$B/content-items/$($item.id)/reviews" @{ decision = 'Approved' } | Out-Null
  $item.id
}
# Daytime in Palestine (UTC+2/+3): 08:00-11:00 UTC is the same local day.
function At($daysAhead, $utcHour) { [DateTime]::UtcNow.Date.AddDays($daysAhead).AddHours($utcHour).ToString('yyyy-MM-ddTHH:mm:ssZ') }

# 1. Plans are public
$plans = Call GET /api/plans
"      plans: $(($plans | % { "$($_.plan)=$($_.maxPostsPerDay)/day `$$($_.prices.OneMonth)" }) -join ', ')"
Check "plans listed without login ($($plans.Count))" ($plans.Count -eq 3 -and ($plans | ? plan -eq 'Basic').maxPostsPerDay -eq 3)

# 2. Basic: 3 posts a day, whatever the owner asks
$sub = NewTenant 'Basic'
Check "subscription shows its plan and limit ($($sub.plan), $($sub.maxPostsPerDay))" ($sub.plan -eq 'Basic' -and $sub.maxPostsPerDay -eq 3)
$policy = Call GET "$B/automation-policy"
Check "unconfigured policy uses the plan limit ($($policy.maxPostsPerDay)/$($policy.planMaxPostsPerDay))" ($policy.maxPostsPerDay -eq 3 -and $policy.planMaxPostsPerDay -eq 3)
Expect PUT "$B/automation-policy" @{ automationEnabled = $true; maxPostsPerDay = 5 } 400 'Basic plan allows at most 3'

$posts = 1..4 | % { ApprovedPost $_ }
foreach ($i in 0..2) { Call POST "$B/content-items/$($posts[$i])/schedule" @{ socialAccountId = $account.id; publishAt = (At 1 (8 + $i)) } | Out-Null }
"PASS 3 posts scheduled for tomorrow"
Expect POST "$B/content-items/$($posts[3])/schedule" @{ socialAccountId = $account.id; publishAt = (At 1 11) } 409 'Basic plan allows 3 posts per day'
$auto = Call POST "$B/content-items/$($posts[3])/schedule" @{ socialAccountId = $account.id }
Check "without a time it goes to the first day with room ($($auto.scheduledAt))" ($null -ne $auto.scheduledAt)

# 3. The owner can choose fewer than the plan
Call PUT "$B/automation-policy" @{ automationEnabled = $true; maxPostsPerDay = 2 } | Out-Null
$more = 1..3 | % { ApprovedPost (10 + $_) }
foreach ($i in 0..1) { Call POST "$B/content-items/$($more[$i])/schedule" @{ socialAccountId = $account.id; publishAt = (At 3 (8 + $i)) } | Out-Null }
Expect POST "$B/content-items/$($more[2])/schedule" @{ socialAccountId = $account.id; publishAt = (At 3 10) } 409 "owner's limit of 2"

# 4. Pro allows more
$pro = NewTenant 'Pro'
Check "Pro subscription allows 10 a day" ($pro.plan -eq 'Pro' -and $pro.maxPostsPerDay -eq 10)
$saved = Call PUT "$B/automation-policy" @{ automationEnabled = $true; maxPostsPerDay = 8 }
Check "Pro owner can set 8 ($($saved.maxPostsPerDay)/$($saved.planMaxPostsPerDay))" ($saved.maxPostsPerDay -eq 8 -and $saved.planMaxPostsPerDay -eq 10)
Expect PUT "$B/automation-policy" @{ automationEnabled = $true; maxPostsPerDay = 11 } 400 'Pro plan allows at most 10'
