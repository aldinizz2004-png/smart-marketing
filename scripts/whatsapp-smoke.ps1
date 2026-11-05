param(
  [string]$Base = "http://localhost:5233",
  [string]$AppSecret = "dev-only-whatsapp-app-secret",
  [string]$VerifyToken = "dev-only-verify-token",
  [string]$Outbox = "$PSScriptRoot\..\SmartMarketingPlatform\App_Data\outbox")
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

# What Meta would POST, signed with the app secret like the real webhook.
function Webhook($value, [string]$signature = $null) {
  $json = @{ object = 'whatsapp_business_account'; entry = @(@{ id = 'WABA'; changes = @(@{ field = 'messages'; value = $value }) }) } | ConvertTo-Json -Depth 12 -Compress
  $bytes = [Text.Encoding]::UTF8.GetBytes($json)
  if (-not $signature) {
    $hmac = New-Object Security.Cryptography.HMACSHA256 (,[Text.Encoding]::UTF8.GetBytes($AppSecret))
    $signature = 'sha256=' + (($hmac.ComputeHash($bytes) | % { $_.ToString('x2') }) -join '')
  }
  try { Invoke-RestMethod -Method POST -Uri "$Base/api/integrations/whatsapp/webhook" -Body $bytes -ContentType 'application/json; charset=utf-8' -Headers @{ 'X-Hub-Signature-256' = $signature } }
  catch { throw "webhook -> $($_.Exception.Response.StatusCode.value__)" }
}
function CustomerSays($phone, $name, $text) {
  $script:n++
  Webhook @{ messaging_product = 'whatsapp'; metadata = @{ display_phone_number = '970599000000'; phone_number_id = $script:pnid }
             contacts = @(@{ profile = @{ name = $name }; wa_id = $phone })
             messages = @(@{ from = $phone; id = "wamid.in.$script:id.$script:n"; timestamp = [string][DateTimeOffset]::UtcNow.ToUnixTimeSeconds(); type = 'text'; text = @{ body = $text } }) }
}
function Conversation($phone) { (Call GET "$B/conversations?pageSize=50").items | ? { $_.customerPhone -eq $phone } | Select-Object -First 1 }
function WaitFor($phone, [scriptblock]$done) {
  foreach ($i in 1..20) { Start-Sleep 1; $c = Conversation $phone; if ($c -and (& $done $c)) { return Call GET "$B/conversations/$($c.id)" } }
  return Call GET "$B/conversations/$((Conversation $phone).id)"
}

$script:token = $null; $script:n = 0
$script:id = [guid]::NewGuid().ToString('N').Substring(0, 8)
$script:pnid = "PN$($script:id)"
$script:token = (Call POST /api/auth/register @{ email = "wa-$id@test.local"; password = 'Passw0rd!234'; fullName = 'Owner'; tenantName = "WhatsApp $id" }).accessToken
$sub = Call POST /api/tenant/subscriptions @{ duration = 'OneMonth' }
Call POST "/api/tenant/subscriptions/$($sub.id)/activate" @{ paymentReference = "WA-$id" } | Out-Null
$biz = Call POST /api/businesses @{ name = 'Qahwet Al-Hara'; category = 'Cafe'; city = 'Nablus'; timeZoneId = 'Asia/Hebron'; defaultCurrency = 'ILS' }
$B = "/api/businesses/$($biz.id)"
Call PUT "$B/brand-profile" @{ dialect = 'Palestinian Arabic'; tone = 'friendly'; emojiLevel = 1 } | Out-Null
Call POST "$B/brand-terms" @{ term = 'رخيص'; rule = 'Prohibited' } | Out-Null
Call POST "$B/products" @{ name = 'لاتيه الفستق'; price = 14; currency = 'ILS' } | Out-Null
Call POST "$B/social-accounts" @{ platformCode = 'whatsapp'; externalAccountId = $pnid; handle = '970599000000'; accessToken = 'fake-token' } | Out-Null

# 1. Webhook security
$challenge = Invoke-RestMethod "$Base/api/integrations/whatsapp/webhook?hub.mode=subscribe&hub.verify_token=$VerifyToken&hub.challenge=4242"
Check "Meta verification answers the challenge" ("$challenge" -eq '4242')
try { Invoke-RestMethod "$Base/api/integrations/whatsapp/webhook?hub.mode=subscribe&hub.verify_token=wrong&hub.challenge=1" | Out-Null; "FAIL wrong verify token accepted" } catch { Check "wrong verify token is 403" ($_.Exception.Response.StatusCode.value__ -eq 403) }
try { Webhook @{ metadata = @{ phone_number_id = $pnid } } 'sha256=00' | Out-Null; "FAIL unsigned webhook accepted" } catch { Check "badly signed webhook is 401" ("$_" -match '401') }

# 2. Without auto-reply the AI only suggests
$lina = '970599111222'
$stored = CustomerSays $lina 'Lina Haddad' 'مرحبا، قديش سعر لاتيه الفستق؟'
Check "customer message stored ($($stored.messages))" ($stored.messages -eq 1)
$again = Webhook @{ messaging_product = 'whatsapp'; metadata = @{ phone_number_id = $pnid }; messages = @(@{ from = $lina; id = "wamid.in.$id.1"; timestamp = '1'; type = 'text'; text = @{ body = 'x' } }) }
Check "the same webhook twice stores it once ($($again.messages))" ($again.messages -eq 0)
$c = WaitFor $lina { param($x) $x.hasSuggestion -or $x.status.code -eq 'WaitingForOwner' }
"      suggestion: $($c.suggestedReply)"
Check "AI prepared a reply, nothing sent ($($c.conversation.lastIntent))" ($c.suggestedReply -and -not ($c.messages | ? { $_.direction -eq 'Outbound' }))
Check "customer name and 24h window known" ($c.conversation.customerName -eq 'Lina Haddad' -and $c.conversation.canReplyFreely)

# 3. The owner sends it
$sent = Call POST "$B/conversations/$($c.conversation.id)/messages" @{}
$out = $sent.messages | ? { $_.direction -eq 'Outbound' } | Select-Object -Last 1
Check "owner sent the suggestion ($($out.author))" ($out.author -eq 'User' -and $out.body -eq $c.suggestedReply -and -not $sent.suggestedReply)
Expect POST "$B/conversations/$($c.conversation.id)/messages" @{ text = 'عنا أرخص قهوة، رخيص كتير' } 400
Expect POST "$B/conversations/$($c.conversation.id)/messages" @{ text = 'a'; templateName = 'b' } 400
$tpl = Call POST "$B/conversations/$($c.conversation.id)/messages" @{ templateName = 'hello_world'; languageCode = 'en_US' }
Check "template message sent" (($tpl.messages | Select-Object -Last 1).messageType -eq 'template')

# 4. Owner allows auto-replies: simple questions are answered alone, orders go to the owner
Call PUT "$B/automation-policy" @{ automationEnabled = $true; maxPostsPerDay = 3; autoReplyToMessages = $true } | Out-Null
$sami = '970599333444'
$before = @(Get-ChildItem $Outbox -Filter *.html -ErrorAction SilentlyContinue).Count
CustomerSays $sami 'Sami Odeh' 'مرحبا' | Out-Null
$s = WaitFor $sami { param($x) $x.lastMessage -ne 'مرحبا' -or $x.status.code -eq 'WaitingForOwner' }
$ai = $s.messages | ? { $_.author -eq 'Ai' } | Select-Object -First 1
Check "AI answered a greeting alone" ($null -ne $ai)
if ($ai) { "      AI: $($ai.body)" }
CustomerSays $sami 'Sami Odeh' 'بدي اطلب 2 لاتيه الفستق توصيل لرفيديا' | Out-Null
$s = WaitFor $sami { param($x) $x.status.code -eq 'WaitingForOwner' }
Check "order handed to the owner ($($s.conversation.handoffReason))" ($s.conversation.status.code -eq 'WaitingForOwner' -and $s.conversation.lastIntent -eq 'Order')
Check "no automatic reply to the order" (@($s.messages | ? { $_.author -eq 'Ai' }).Count -eq 1)
"      suggestion: $($s.suggestedReply)"
$after = @(Get-ChildItem $Outbox -Filter *.html -ErrorAction SilentlyContinue).Count
Check "owner notified by email ($before -> $after)" ($after -gt $before)
$waiting = Call GET "$B/conversations?status=WaitingForOwner"
Check "inbox filter WaitingForOwner ($($waiting.totalCount))" ($waiting.totalCount -ge 1)

# 5. Hand back to the AI, close
$open = Call PATCH "$B/conversations/$($s.conversation.id)/status" @{ status = 'Open' }
Check "handed back to the AI" ($open.conversation.status.code -eq 'Open' -and -not $open.conversation.handoffReason)
$closed = Call PATCH "$B/conversations/$($s.conversation.id)/status" @{ status = 'Closed' }
Check "conversation closed" ($closed.conversation.status.code -eq 'Closed')
Expect PATCH "$B/conversations/$($s.conversation.id)/status" @{ status = 'WaitingForOwner' } 400
CustomerSays $sami 'Sami Odeh' 'شكراً' | Out-Null
Start-Sleep 1
Check "a new message reopens it" ((Conversation $sami).status.code -ne 'Closed')
