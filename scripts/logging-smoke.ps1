param([string]$Base = "http://localhost:5233", [string]$Logs = "$PSScriptRoot\..\SmartMarketingPlatform\App_Data\logs")
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8

function Send($method, $path, $body = $null, $token = $null, $correlation = $null) {
  $headers = @{}
  if ($token) { $headers.Authorization = "Bearer $token" }
  if ($correlation) { $headers['X-Correlation-Id'] = $correlation }
  $req = @{ Method = $method; Uri = "$Base$path"; ContentType = 'application/json; charset=utf-8'; UseBasicParsing = $true; Headers = $headers }
  if ($null -ne $body) { $req.Body = [Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 10)) }
  try { $r = Invoke-WebRequest @req; [pscustomobject]@{ Status = [int]$r.StatusCode; Id = $r.Headers['X-Correlation-Id']; Body = $r.Content } }
  catch {
    $resp = $_.Exception.Response
    if (-not $resp) { throw }
    $text = (New-Object IO.StreamReader($resp.GetResponseStream())).ReadToEnd()
    [pscustomobject]@{ Status = [int]$resp.StatusCode; Id = $resp.Headers['X-Correlation-Id']; Body = $text }
  }
}
function Check($name, $ok) { if ($ok) { "PASS $name" } else { "FAIL $name" } }

# Log lines of one request, read while the API keeps the file open.
function LogLines($correlation) {
  $file = Get-ChildItem $Logs -Filter 'api-*.json' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
  $stream = [IO.File]::Open($file.FullName, 'Open', 'Read', 'ReadWrite')
  $text = (New-Object IO.StreamReader($stream, [Text.Encoding]::UTF8)).ReadToEnd(); $stream.Dispose()
  $text -split "`n" | ? { $_ -like "*$correlation*" } | % { $_ | ConvertFrom-Json }
}

$id = [guid]::NewGuid().ToString('N').Substring(0, 8)
$auth = (Send POST /api/auth/register @{ email = "logs-$id@test.local"; password = 'Passw0rd!234'; fullName = 'Owner'; tenantName = "Logs $id" }).Body | ConvertFrom-Json
$sub = (Send POST /api/tenant/subscriptions @{ duration = 'OneMonth' } $auth.accessToken).Body | ConvertFrom-Json
Send POST "/api/tenant/subscriptions/$($sub.id)/activate" @{ paymentReference = "LOG-$id" } $auth.accessToken | Out-Null

# 1. The correlation id travels with the request
$mine = "smoke-$id"
$me = Send GET /api/me $null $auth.accessToken $mine
Check "the caller's id comes back in X-Correlation-Id" ($me.Id -eq $mine)
$generated = Send GET /api/me $null $auth.accessToken
Check "without one, the API makes one ($($generated.Id))" ($generated.Id -match '^[0-9a-f]{32}$')
$unsafe = Send GET /api/me $null $auth.accessToken 'bad id <script>'
Check "an unsafe id is replaced" ($unsafe.Id -match '^[0-9a-f]{32}$')

# 2. Error responses carry it, so an owner can quote it
$missing = Send GET "/api/businesses/$([guid]::NewGuid())/products" $null $auth.accessToken "$mine-404"
$problem = $missing.Body | ConvertFrom-Json
Check "404 ProblemDetails has the correlationId" ($missing.Status -eq 404 -and $problem.correlationId -eq "$mine-404")

# 3. The log file has one structured line per request, with who and how long
Start-Sleep 2
$line = LogLines $mine | ? { $_.RequestPath -eq '/api/me' } | Select-Object -First 1
Check "request logged as JSON with status and time ($($line.StatusCode), $([int]$line.Elapsed) ms)" ($line -and $line.StatusCode -eq 200 -and $line.Elapsed -ge 0)
Check "the line knows the user and the tenant" ($line.UserId -eq "$($auth.user.id)" -and $line.TenantId -eq "$($auth.tenant.id)")
Check "and the correlation id" ($line.CorrelationId -eq $mine)
$notFound = LogLines "$mine-404" | ? { $_.StatusCode -eq 404 } | Select-Object -First 1
Check "the 404 is logged with its business id from the route" ($notFound -and $notFound.BusinessId)
Check "no token or password in the log" (-not (Select-String -Path (Join-Path $Logs 'api-*.json') -SimpleMatch $auth.accessToken -Quiet))
