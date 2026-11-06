param([string]$Base = "http://localhost:5233", [int]$GraceSeconds = 2)
# Start the API with Jwt__RefreshReuseGraceSeconds=2 so the reuse test does not wait 30 seconds.
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8

function Call($method, $path, $body = $null, $token = $null) {
  $req = @{ Method = $method; Uri = "$Base$path"; ContentType = 'application/json; charset=utf-8' }
  if ($token) { $req.Headers = @{ Authorization = "Bearer $token" } }
  if ($null -ne $body) { $req.Body = [Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 10)) }
  try { Invoke-RestMethod @req } catch {
    $detail = $_.ErrorDetails.Message
    if (-not $detail -and $_.Exception.Response) { $detail = (New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() }
    throw "$method $path -> $($_.Exception.Response.StatusCode.value__): $detail"
  }
}
function Status($method, $path, $body = $null, $token = $null) {
  try { Call $method $path $body $token | Out-Null; 200 } catch { if ("$_" -match '-> (\d{3})') { [int]$Matches[1] } else { throw } }
}
function Check($name, $ok) { if ($ok) { "PASS $name" } else { "FAIL $name" } }
function Refresh($token) { Call POST /api/auth/refresh @{ refreshToken = $token } }

$id = [guid]::NewGuid().ToString('N').Substring(0, 8)
$email = "tokens-$id@test.local"; $password = 'Passw0rd!234'

# 1. Login gives a short access token and a refresh token
$first = Call POST /api/auth/register @{ email = $email; password = $password; fullName = 'Owner'; tenantName = "Tokens $id" }
$minutes = ([DateTimeOffset]$first.expiresAt - [DateTimeOffset]::UtcNow).TotalMinutes
Check "access token lives about 15 minutes ($([int]$minutes))" ($minutes -gt 13 -and $minutes -le 15)
Check "refresh token returned, valid about 30 days" ($first.refreshToken.Length -ge 80 -and ([DateTimeOffset]$first.refreshTokenExpiresAt - [DateTimeOffset]::UtcNow).TotalDays -gt 29)

# 2. Refresh rotates: a new pair, and the new access token works
$second = Refresh $first.refreshToken
Check "refresh gives a new refresh token" ($second.refreshToken -ne $first.refreshToken)
Check "new access token works (GET /api/me)" ((Status GET /api/me $null $second.accessToken) -eq 200)

# 3. Two tabs refreshing at once: the late one is refused, the session survives
Check "same token again right away is refused (401)" ((Status POST /api/auth/refresh @{ refreshToken = $first.refreshToken }) -eq 401)
$third = Refresh $second.refreshToken
Check "the session still works after a parallel refresh" ($null -ne $third.accessToken)

# 4. An old token used later means it was stolen: the whole session ends
Start-Sleep ($GraceSeconds + 1)
Check "stolen old token is refused (401)" ((Status POST /api/auth/refresh @{ refreshToken = $first.refreshToken }) -eq 401)
Check "and the newest token of that session is revoked too" ((Status POST /api/auth/refresh @{ refreshToken = $third.refreshToken }) -eq 401)

# 5. Logout ends one session
$laptop = Call POST /api/auth/login @{ email = $email; password = $password }
Check "logout answers 204" ((Status POST /api/auth/logout @{ refreshToken = $laptop.refreshToken }) -eq 200)
Check "logged-out refresh token is refused" ((Status POST /api/auth/refresh @{ refreshToken = $laptop.refreshToken }) -eq 401)
Check "logout with an unknown token gives the same answer" ((Status POST /api/auth/logout @{ refreshToken = 'not-a-real-token' }) -eq 200)

# 6. Logout everywhere (lost phone)
$phone = Call POST /api/auth/login @{ email = $email; password = $password }
$tablet = Call POST /api/auth/login @{ email = $email; password = $password }
$ended = Call POST /api/auth/logout-all $null $tablet.accessToken
Check "logout-all ended the open sessions ($($ended.sessionsEnded))" ($ended.sessionsEnded -ge 2)
Check "phone can no longer refresh" ((Status POST /api/auth/refresh @{ refreshToken = $phone.refreshToken }) -eq 401)
Check "logout-all needs the access token" ((Status POST /api/auth/logout-all) -eq 401)

# 7. Bad input
Check "random refresh token is 401" ((Status POST /api/auth/refresh @{ refreshToken = 'abc' }) -eq 401)
Check "missing refresh token is 400" ((Status POST /api/auth/refresh @{}) -eq 400)
