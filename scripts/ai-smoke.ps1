param([string]$Base = "http://localhost:5233", [string]$Out = "$env:TEMP\smp-ai-e2e.json")
$ErrorActionPreference = 'Stop'
$results = [ordered]@{}
function Call($method, $path, $body = $null, $token = $script:token) {
  $h = @{}; if ($token) { $h.Authorization = "Bearer $token" }
  $req = @{ Method = $method; Uri = "$Base$path"; Headers = $h; ContentType = 'application/json; charset=utf-8' }
  if ($null -ne $body) { $req.Body = [Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 10)) }
  $sw = [Diagnostics.Stopwatch]::StartNew()
  try { $r = Invoke-RestMethod @req; $script:lastMs = $sw.ElapsedMilliseconds; return $r }
  catch { $msg = $_.ErrorDetails.Message; throw "$method $path -> $($_.Exception.Response.StatusCode.value__): $msg" }
}
$script:token = $null
$email = "ai-e2e-$([guid]::NewGuid().ToString('N').Substring(0,8))@test.local"
$reg = Call POST /api/auth/register @{ email = $email; password = 'Passw0rd!234'; fullName = 'Izz Test'; tenantName = 'AI E2E'; preferredLanguage = 'ar' }
$script:token = $reg.accessToken; if (-not $script:token) { $script:token = $reg.token }
$sub = Call POST /api/tenant/subscriptions @{ duration = 'OneMonth' }
Call POST "/api/tenant/subscriptions/$($sub.id)/activate" @{ paymentReference = ('TEST-AI-' + [guid]::NewGuid().ToString('N').Substring(0,8)) } | Out-Null

$biz = Call POST /api/businesses @{ name = 'قهوة الحارة'; category = 'Cafe'; countryCode = 'PS'; city = 'Nablus'; timeZoneId = 'Asia/Hebron'; defaultCurrency = 'ILS'; description = 'Neighborhood specialty cafe and Arabic sweets' }
$B = "/api/businesses/$($biz.id)"
Call PUT "$B/brand-profile" @{ dialect = 'Palestinian Arabic'; tone = 'friendly'; emojiLevel = 2; guidelines = 'Warm, local, short sentences.' } | Out-Null
Call POST "$B/brand-terms" @{ term = 'رخيص'; rule = 'Prohibited' } | Out-Null
Call POST "$B/brand-terms" @{ term = 'طازة'; rule = 'Preferred' } | Out-Null
$latte = Call POST "$B/products" @{ name = 'لاتيه الفستق'; price = 14; currency = 'ILS'; description = 'New pistachio latte' }
$knafeh = Call POST "$B/products" @{ name = 'كنافة نابلسية'; price = 12; currency = 'ILS' }
$now = (Get-Date).ToUniversalTime()
$offer = Call POST "$B/offers" @{ title = 'خصم 20% على الكنافة'; discountPercent = 20; startsAt = $now.ToString('o'); endsAt = $now.AddDays(10).ToString('o') }
Call PUT "$B/automation-policy" @{ automationEnabled = $false; maxPostsPerDay = 2; publishWindowStart = '10:00'; publishWindowEnd = '22:00'; allowedPlatforms = @('instagram', 'facebook') } | Out-Null
$camp = Call POST "$B/campaigns" @{ name = 'Pistachio latte launch'; objective = 'Orders'; goalStatement = 'Launch the new pistachio latte and sell 300 cups in two weeks'; budget = 200; budgetCurrency = 'ILS'; startsAt = $now.ToString('o'); endsAt = $now.AddDays(14).ToString('o'); productIds = @($latte.id); offerIds = @($offer.id) }

$plan = Call POST "$B/campaigns/$($camp.id)/plans"; $results.planMs = $script:lastMs; $results.plan = $plan
$run = Call POST "$B/strategy-runs" @{ campaignId = $camp.id; windowDays = 7 }; $results.runMs = $script:lastMs; $results.run = $run
$recs = Call GET "$B/recommendations?campaignId=$($camp.id)"; $results.recommendations = $recs
$item = Call POST "$B/content-items" @{ campaignId = $camp.id; platformCode = 'instagram'; contentType = 'Reel'; firstRevision = @{ generateWithAi = $true; productId = $latte.id; hookType = 'Question' } }
$results.contentMs = $script:lastMs; $results.content = $item
$item2 = Call POST "$B/content-items" @{ campaignId = $camp.id; platformCode = 'facebook'; contentType = 'Post'; firstRevision = @{ generateWithAi = $true; offerId = $offer.id; productId = $knafeh.id } }
$results.content2Ms = $script:lastMs; $results.content2 = $item2

$execIds = @()
foreach ($x in @($plan, $run, $item, $item2)) { ($x | ConvertTo-Json -Depth 20) | Select-String -Pattern '"(?:generatedByExecutionId|aiExecutionId|executionId)":\s*"([0-9a-f-]{36})"' -AllMatches | % { $_.Matches } | % { $execIds += $_.Groups[1].Value } }
$results.executions = @($execIds | Select-Object -Unique | % { Call GET "$B/ai-executions/$_" })
[IO.File]::WriteAllText($Out, ($results | ConvertTo-Json -Depth 30), (New-Object Text.UTF8Encoding($false)))
"OK -> $Out"
"planMs=$($results.planMs) runMs=$($results.runMs) contentMs=$($results.contentMs) content2Ms=$($results.content2Ms) executions=$($results.executions.Count)"
