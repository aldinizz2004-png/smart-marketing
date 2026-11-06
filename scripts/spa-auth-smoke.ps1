# Checks what the web app needs from the API: CORS for http://localhost:5173 and the refresh token in an httpOnly cookie.
#   $env:Ai__Provider='Fake'; dotnet run --project SmartMarketingPlatform --launch-profile http
param([string]$Base = "http://localhost:5233", [string]$Origin = "http://localhost:5173")
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
Add-Type -AssemblyName System.Net.Http

$cookies = New-Object System.Net.CookieContainer
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.CookieContainer = $cookies
$client = New-Object System.Net.Http.HttpClient($handler)

function Send($method, $path, $body = $null, [hashtable]$headers = @{}) {
  $req = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::new($method), "$Base$path")
  foreach ($k in $headers.Keys) { $req.Headers.TryAddWithoutValidation($k, [string]$headers[$k]) | Out-Null }
  if ($null -ne $body) { $req.Content = New-Object System.Net.Http.StringContent(($body | ConvertTo-Json -Depth 5), [Text.Encoding]::UTF8, 'application/json') }
  $res = $client.SendAsync($req).Result
  $text = $res.Content.ReadAsStringAsync().Result
  [pscustomobject]@{ Status = [int]$res.StatusCode; Res = $res; Json = $(if ($text -and $text.TrimStart().StartsWith('{')) { $text | ConvertFrom-Json } else { $null }) }
}
function Header($r, $name) {
  $values = $null
  if ($r.Res.Headers.TryGetValues($name, [ref]$values)) { return ($values -join ', ') }
  if ($r.Res.Content.Headers.TryGetValues($name, [ref]$values)) { return ($values -join ', ') }
  return $null
}
function SetCookie($r) { $v = $null; if ($r.Res.Headers.TryGetValues('Set-Cookie', [ref]$v)) { ($v -join ' | ') } else { '' } }
function RefreshCookieValue { ($cookies.GetCookies([Uri]"$Base/api/auth") | Where-Object Name -eq 'smp_refresh').Value }
function Check($name, $ok) { if ($ok) { "PASS $name" } else { "FAIL $name" } }

$cookieMode = @{ 'X-Auth-Transport' = 'cookie'; 'Origin' = $Origin }

# CORS
$pre = Send OPTIONS /api/auth/login $null @{ Origin = $Origin; 'Access-Control-Request-Method' = 'POST'; 'Access-Control-Request-Headers' = 'content-type,x-auth-transport' }
Check "preflight from the web app allowed ($($pre.Status))" ($pre.Status -eq 204 -and (Header $pre 'Access-Control-Allow-Origin') -eq $Origin)
Check "preflight allows credentials" ((Header $pre 'Access-Control-Allow-Credentials') -eq 'true')
Check "preflight allows X-Auth-Transport" ((Header $pre 'Access-Control-Allow-Headers') -match 'x-auth-transport')
$evil = Send OPTIONS /api/auth/login $null @{ Origin = 'https://evil.example'; 'Access-Control-Request-Method' = 'POST' }
Check "preflight from another origin gets no CORS headers" (-not (Header $evil 'Access-Control-Allow-Origin'))

# Cookie mode
$id = [guid]::NewGuid().ToString('N').Substring(0, 8)
$email = "spa-$id@test.local"
$reg = Send POST /api/auth/register @{ email = $email; password = 'Passw0rd!234'; fullName = 'Owner'; tenantName = "SPA $id" } $cookieMode
$set = SetCookie $reg
Check "register in cookie mode ($($reg.Status))" ($reg.Status -eq 201 -and $reg.Json.accessToken)
Check "refresh token not in the body" ($null -eq $reg.Json.refreshToken)
Check "cookie is httpOnly, SameSite=Strict, Path=/api/auth" ($set -match 'smp_refresh=' -and $set -match 'httponly' -and $set -match 'samesite=strict' -and $set -match 'path=/api/auth')
Check "CORS exposes X-Correlation-Id" ((Header $reg 'Access-Control-Expose-Headers') -match 'X-Correlation-Id')
$first = RefreshCookieValue

$ref = Send POST /api/auth/refresh $null $cookieMode
Check "refresh with the cookie and no body ($($ref.Status))" ($ref.Status -eq 200 -and $ref.Json.accessToken -and $null -eq $ref.Json.refreshToken)
$second = RefreshCookieValue
Check "cookie rotated" ($second -and $second -ne $first)

$noHeader = Send POST /api/auth/refresh $null @{ Origin = $Origin }
Check "cookie without X-Auth-Transport is ignored ($($noHeader.Status))" ($noHeader.Status -eq 400)

$out = Send POST /api/auth/logout $null $cookieMode
Check "logout in cookie mode ($($out.Status))" ($out.Status -eq 204)
Check "logout removes the cookie" (-not (RefreshCookieValue))
$after = Send POST /api/auth/refresh @{ refreshToken = $second }
Check "token from the ended session is refused ($($after.Status))" ($after.Status -eq 401)
$empty = Send POST /api/auth/logout $null $cookieMode
Check "logout without a cookie is still 204 ($($empty.Status))" ($empty.Status -eq 204)

# Body mode (mobile, scripts) is unchanged
$login = Send POST /api/auth/login @{ email = $email; password = 'Passw0rd!234' }
Check "login in body mode returns the refresh token" ($login.Status -eq 200 -and $login.Json.refreshToken)
Check "body mode sets no cookie" (-not ((SetCookie $login) -match 'smp_refresh'))
$bodyRefresh = Send POST /api/auth/refresh @{ refreshToken = $login.Json.refreshToken }
Check "refresh in body mode ($($bodyRefresh.Status))" ($bodyRefresh.Status -eq 200 -and $bodyRefresh.Json.refreshToken)
$missing = Send POST /api/auth/refresh @{}
Check "refresh without any token is 400 ($($missing.Status))" ($missing.Status -eq 400)
