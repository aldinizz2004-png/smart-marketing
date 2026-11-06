param([string]$Base = "http://localhost:5233")
# Start the API with small limits so the test is quick:
#   RateLimiting__AuthPerMinute=3  RateLimiting__ExpensivePerMinute=2  RateLimiting__GlobalPerMinute=40
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8

function Send($method, $path, $body = $null, $token = $null) {
  $req = @{ Method = $method; Uri = "$Base$path"; ContentType = 'application/json; charset=utf-8'; UseBasicParsing = $true }
  if ($token) { $req.Headers = @{ Authorization = "Bearer $token" } }
  if ($null -ne $body) { $req.Body = [Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 10)) }
  try { $r = Invoke-WebRequest @req; [pscustomobject]@{ Status = [int]$r.StatusCode; RetryAfter = $r.Headers['Retry-After']; Body = $r.Content } }
  catch {
    $resp = $_.Exception.Response
    if (-not $resp) { throw }
    $text = (New-Object IO.StreamReader($resp.GetResponseStream())).ReadToEnd()
    [pscustomobject]@{ Status = [int]$resp.StatusCode; RetryAfter = $resp.Headers['Retry-After']; Body = $text }
  }
}
function Check($name, $ok) { if ($ok) { "PASS $name" } else { "FAIL $name" } }

function NewTenant() {
  $id = [guid]::NewGuid().ToString('N').Substring(0, 8)
  $auth = (Send POST /api/auth/register @{ email = "limits-$id@test.local"; password = 'Passw0rd!234'; fullName = 'Owner'; tenantName = "Limits $id" }).Body | ConvertFrom-Json
  $sub = (Send POST /api/tenant/subscriptions @{ duration = 'OneMonth' } $auth.accessToken).Body | ConvertFrom-Json
  Send POST "/api/tenant/subscriptions/$($sub.id)/activate" @{ paymentReference = "LIM-$id" } $auth.accessToken | Out-Null
  $biz = (Send POST /api/businesses @{ name = 'Qahwet Al-Hara'; category = 'Cafe'; city = 'Nablus'; timeZoneId = 'Asia/Hebron'; defaultCurrency = 'ILS' } $auth.accessToken).Body | ConvertFrom-Json
  [pscustomobject]@{ Token = $auth.accessToken; Email = "limits-$id@test.local"; B = "/api/businesses/$($biz.id)" }
}

$a = NewTenant    # auth call 1
$b = NewTenant    # auth call 2

# 1. AI calls: a limit per tenant, other tenants are not affected
$codes = 1..3 | % { (Send POST "$($a.B)/comments/analyze" $null $a.Token).Status }
Check "tenant A: 2 AI calls allowed, the 3rd is 429 ($($codes -join ','))" ($codes[0] -eq 200 -and $codes[1] -eq 200 -and $codes[2] -eq 429)
Check "tenant B still has its own AI calls" ((Send POST "$($b.B)/comments/analyze" $null $b.Token).Status -eq 200)

# 2. The 429 says how long to wait, as ProblemDetails
$blocked = Send POST "$($a.B)/comments/analyze" $null $a.Token
$problem = $blocked.Body | ConvertFrom-Json
Check "429 has Retry-After ($($blocked.RetryAfter)s) and a ProblemDetails body" ($blocked.Status -eq 429 -and [int]$blocked.RetryAfter -ge 1 -and $problem.title -eq 'Too many requests')

# 3. Webhooks and health checks are never limited
$hooks = 1..45 | % { (Send POST /api/integrations/whatsapp/webhook @{ entry = @() }).Status }
Check "45 webhook calls, none limited (all 401: bad signature)" (@($hooks | ? { $_ -eq 429 }).Count -eq 0)
$health = 1..45 | % { (Send GET /health).Status }
Check "45 health checks, none limited" (@($health | ? { $_ -eq 429 }).Count -eq 0)

# 4. Everything else: a limit per user
$me = 1..45 | % { (Send GET /api/me $null $b.Token).Status }
$first429 = [array]::IndexOf($me, 429) + 1
Check "user B is limited after about 40 requests (first 429 at #$first429)" ($first429 -gt 30 -and $first429 -le 41)
Check "user A is not affected by B's limit" ((Send GET /api/me $null $a.Token).Status -eq 200)

# 5. Login: a limit per IP against password guessing (register x2 above + 1 = 3 allowed)
$guess = 1..2 | % { (Send POST /api/auth/login @{ email = $a.Email; password = 'Wrong-pass-1' }).Status }
Check "after 3 register/login calls the next one is 429 ($($guess -join ','))" ($guess[0] -eq 401 -and $guess[1] -eq 429)
Check "even the right password waits until the window passes" ((Send POST /api/auth/login @{ email = $a.Email; password = 'Passw0rd!234' }).Status -eq 429)
