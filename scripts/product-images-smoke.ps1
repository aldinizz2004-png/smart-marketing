param([string]$Base = "http://localhost:5233")
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
Add-Type -AssemblyName System.Net.Http
Add-Type -AssemblyName System.Drawing

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

# A drawn product photo: a cup of pistachio latte on a wooden table.
function Photo([int]$size, [string]$path) {
  $bmp = New-Object Drawing.Bitmap $size, $size
  $g = [Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = 'AntiAlias'
  $g.Clear([Drawing.Color]::FromArgb(245, 240, 230))
  $g.FillRectangle((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(150, 100, 60))), 0, [int]($size * 0.7), $size, [int]($size * 0.3))
  $g.FillEllipse([Drawing.Brushes]::White, [int]($size * 0.25), [int]($size * 0.3), [int]($size * 0.5), [int]($size * 0.45))
  $g.FillEllipse((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(170, 200, 120))), [int]($size * 0.3), [int]($size * 0.33), [int]($size * 0.4), [int]($size * 0.2))
  $g.DrawArc((New-Object Drawing.Pen ([Drawing.Color]::White), ([int]($size * 0.04))), [int]($size * 0.68), [int]($size * 0.42), [int]($size * 0.15), [int]($size * 0.15), -90, 180)
  $g.Dispose(); $bmp.Save($path, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
}
function Upload($path, $kind, $productId, $mime = 'image/png') {
  $client = New-Object Net.Http.HttpClient
  $client.DefaultRequestHeaders.Authorization = New-Object Net.Http.Headers.AuthenticationHeaderValue('Bearer', $script:token)
  $form = New-Object Net.Http.MultipartFormDataContent
  $file = New-Object Net.Http.ByteArrayContent (,[IO.File]::ReadAllBytes($path))
  $file.Headers.ContentType = [Net.Http.Headers.MediaTypeHeaderValue]::Parse($mime)
  $form.Add($file, 'file', [IO.Path]::GetFileName($path))
  $form.Add((New-Object Net.Http.StringContent $kind), 'kind')
  if ($productId) { $form.Add((New-Object Net.Http.StringContent $productId), 'productId') }
  $response = $client.PostAsync("$Base$B/media", $form).Result
  $text = $response.Content.ReadAsStringAsync().Result
  if (-not $response.IsSuccessStatusCode) { throw "upload -> $([int]$response.StatusCode): $text" }
  $text | ConvertFrom-Json
}

$script:token = $null
$id = [guid]::NewGuid().ToString('N').Substring(0, 8)
$script:token = (Call POST /api/auth/register @{ email = "photos-$id@test.local"; password = 'Passw0rd!234'; fullName = 'Owner'; tenantName = "Photos $id" }).accessToken
$sub = Call POST /api/tenant/subscriptions @{ duration = 'OneMonth' }
Call POST "/api/tenant/subscriptions/$($sub.id)/activate" @{ paymentReference = "IMG-$id" } | Out-Null
$biz = Call POST /api/businesses @{ name = 'Qahwet Al-Hara'; category = 'Cafe'; city = 'Nablus'; timeZoneId = 'Asia/Hebron'; defaultCurrency = 'ILS' }
$script:B = "/api/businesses/$($biz.id)"
Call PUT "$B/brand-profile" @{ dialect = 'Palestinian Arabic'; tone = 'friendly'; emojiLevel = 1 } | Out-Null
$latte = Call POST "$B/products" @{ name = 'لاتيه الفستق'; price = 14; currency = 'ILS' }

$dir = Join-Path $env:TEMP "smp-photos-$id"; New-Item -ItemType Directory $dir | Out-Null
Photo 1080 "$dir\latte-big.png"; Photo 400 "$dir\latte-small.png"
"just a text file" | Set-Content -Encoding utf8 "$dir\notes.txt"

# 1. Upload: the size is read from the file
$big = Upload "$dir\latte-big.png" 'ProductPhoto' $latte.id
$small = Upload "$dir\latte-small.png" 'ProductPhoto' $latte.id
Check "photo size read on upload ($($big.width)x$($big.height))" ($big.width -eq 1080 -and $big.height -eq 1080)

# 2. The AI looks at the photos once
$seen = Call POST "$B/media/$($big.id)/describe"
"      description: $($seen.visualDescription)"
Check "photo described" ($seen.visualDescription -and $seen.describedAt)
$weak = Call POST "$B/media/$($small.id)/describe"
"      issues: $($weak.qualityIssues -join ', ') | advice: $($weak.qualityAdvice)"
Check "small photo flagged LowResolution with advice" (($weak.qualityIssues -contains 'LowResolution') -and $weak.qualityAdvice)
$cached = Call POST "$B/media/$($big.id)/describe"
Check "second describe reuses the first answer" ($cached.describedAt -eq $seen.describedAt)
$again = Call POST "$B/media/$($big.id)/describe?force=true"
Check "force=true looks again" ($again.describedAt -ne $seen.describedAt)
$doc = Upload "$dir\notes.txt" 'Document' $null 'text/plain'
Expect POST "$B/media/$($doc.id)/describe" $null 400

# 3. A post about the product uses its best photo, and the text knows what the photo shows
$camp = Call POST "$B/campaigns" @{ name = 'Launch'; objective = 'Orders'; goalStatement = 'Sell more pistachio lattes' }
$post = Call POST "$B/content-items" @{ campaignId = $camp.id; platformCode = 'instagram'; contentType = 'Post'; firstRevision = @{ generateWithAi = $true; productId = $latte.id } }
$rev = $post.latestRevision
Check "AI post got the sharp product photo ($($rev.format))" ($rev.format -eq 'SingleImage' -and $rev.assets.Count -eq 1 -and $rev.assets[0].assetId -eq $big.id)
"      post: $($rev.body -replace "`n", ' / ')"
"      visual: $($rev.creativeDetails.visualIdea)"
$listed = (Call GET "$B/media?productId=$($latte.id)").items
Check "media list shows the AI fields ($($listed.Count))" (@($listed | ? { $_.visualDescription }).Count -eq 2)

Remove-Item -Recurse -Force $dir
