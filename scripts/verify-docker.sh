#!/usr/bin/env bash
# Verifies the Docker setup end to end (the 6 points of the Week 5 checklist).
# Run from the repository root:   bash scripts/verify-docker.sh
# Needs: Docker + Compose, curl, python3, and a .env file (copy .env.example).
set -uo pipefail
cd "$(dirname "$0")/.."

[ -f .env ] || { echo "Missing .env -> run: cp .env.example .env  (and set your own values)"; exit 2; }
set -a; . ./.env; set +a
API="http://localhost:${API_HOST_PORT:-8080}"
FAILS=0
RUN=$(date +%H%M%S)

title() { printf '\n==================== %s ====================\n' "$1"; }
ok()    { echo "  PASS  $1"; }
bad()   { echo "  FAIL  $1"; FAILS=$((FAILS+1)); }
check() { if eval "$2"; then ok "$1"; else bad "$1"; fi; }
json()  { python3 -c 'import sys,json; d=json.load(open("/tmp/sm_last.json")); print(eval(sys.argv[1]))' "$1"; }
sql()   { docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C \
            -d SmartMarketingDb -h -1 -W -s '|' -Q "SET NOCOUNT ON; $1"; }

# call METHOD PATH [JSON_BODY] [EXPECTED_STATUS]  -> response saved in /tmp/sm_last.json
call() {
  local method=$1 path=$2 body=${3:-} expected=${4:-200} code
  local args=(-s -o /tmp/sm_last.json -w '%{http_code}' -X "$method" "$API$path" -H 'Content-Type: application/json')
  [ -n "${TOKEN:-}" ] && args+=(-H "Authorization: Bearer $TOKEN")
  [ -n "$body" ] && args+=(--data "$body")
  code=$(curl "${args[@]}")
  if [ "$code" = "$expected" ]; then ok "$method $path -> $code"; else bad "$method $path -> $code (expected $expected): $(head -c 300 /tmp/sm_last.json)"; fi
}

wait_api() {
  for _ in $(seq 1 60); do
    [ "$(curl -s -o /dev/null -w '%{http_code}' "$API/health")" = "200" ] && return 0
    sleep 2
  done
  return 1
}

title "1. App image builds and starts"
docker compose up -d --build 2>&1 | tail -5
check "API answers /health" "wait_api"
docker image ls smart-marketing-api --format '  image: {{.Repository}}:{{.Tag}}  size {{.Size}}'
check "runs as a non-root user" '[ "$(docker compose exec -T api id -u)" != "0" ]'
check "no secret baked into the image" '! docker image inspect smart-marketing-api:latest --format "{{json .Config.Env}}" | grep -qi -e password -e jwt'

title "2. Database container running"
docker compose ps --format '  {{.Service}}: {{.Status}}'
check "db container is healthy" '[ "$(docker inspect -f "{{.State.Health.Status}}" "$(docker compose ps -q db)")" = "healthy" ]'
echo "  engine: $(sql 'SELECT CAST(SERVERPROPERTY('"'"'ProductVersion'"'"') AS nvarchar(30)) + '"'"' '"'"' + CAST(SERVERPROPERTY('"'"'Edition'"'"') AS nvarchar(60))')"

title "3. Migrations applied automatically"
docker compose logs api 2>&1 | grep -E "migration|Migrations|up to date" | sed 's/^/  /' | tail -4
echo "  __EFMigrationsHistory:"; sql "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId" | sed 's/^/    /'
check "2 migrations recorded" '[ "$(sql "SELECT COUNT(*) FROM __EFMigrationsHistory")" = "2" ]'

title "4. Schema, primary keys and foreign keys"
TABLES=$(sql "SELECT COUNT(*) FROM sys.tables WHERE name <> '__EFMigrationsHistory'")
PKS=$(sql "SELECT COUNT(*) FROM sys.key_constraints WHERE type = 'PK' AND OBJECT_NAME(parent_object_id) <> '__EFMigrationsHistory'")
FKS=$(sql "SELECT COUNT(*) FROM sys.foreign_keys")
CASCADE=$(sql "SELECT COUNT(*) FROM sys.foreign_keys WHERE delete_referential_action <> 0")
echo "  tables=$TABLES  primary keys=$PKS  foreign keys=$FKS  cascading FKs=$CASCADE"
check "39 tables, each with a primary key" '[ "$TABLES" = "39" ] && [ "$PKS" = "39" ]'
check "foreign keys exist and none cascade (DeleteBehavior.Restrict)" '[ "$FKS" -gt 0 ] && [ "$CASCADE" = "0" ]'
echo "  sample relationships:"
sql "SELECT TOP 6 OBJECT_NAME(parent_object_id) + ' -> ' + OBJECT_NAME(referenced_object_id) + '  (' + name + ')' FROM sys.foreign_keys WHERE OBJECT_NAME(parent_object_id) IN ('Subscriptions','Businesses','ContentItems','PublicationJobs','TenantMemberships') ORDER BY 1" | sed 's/^/    /'
echo "  schema fingerprint (compare with the local database): $(sql "SELECT CHECKSUM_AGG(CHECKSUM(TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH)) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME <> '__EFMigrationsHistory'")"
check "seed data present (63 statuses, 9 platforms)" '[ "$(sql "SELECT COUNT(*) FROM Statuses")" = "63" ] && [ "$(sql "SELECT COUNT(*) FROM Platforms")" = "9" ]'

title "5. Data persists across a full restart"
EMAIL="docker-check-$RUN@test.local"
TOKEN=""
call POST /api/auth/register "{\"email\":\"$EMAIL\",\"password\":\"Secret123\",\"fullName\":\"Docker Check\",\"tenantName\":\"Docker Check $RUN\"}" 201
TOKEN=$(json "d['accessToken']")
call POST /api/tenant/subscriptions '{"duration":"OneMonth"}' 201;  SUB=$(json "d['id']")
call POST "/api/tenant/subscriptions/$SUB/activate" "{\"paymentReference\":\"DOCKER-$RUN\"}"
call POST /api/businesses '{"name":"Rival Cafe","category":"Cafe","city":"Nablus","timeZoneId":"Asia/Hebron","defaultCurrency":"ILS"}' 201
BIZ=$(json "d['id']")
printf '\x89PNG\r\n\x1a\n docker volume test %s' "$RUN" > /tmp/sm_img.png
curl -s -o /tmp/sm_last.json -H "Authorization: Bearer $TOKEN" -F "file=@/tmp/sm_img.png" -F "kind=ProductPhoto" "$API/api/businesses/$BIZ/media"
MEDIA=$(json "d['id']"); SHA=$(json "d['sha256']"); ok "image uploaded to the appdata volume (sha256 ${SHA:0:12}...)"
call POST "/api/businesses/$BIZ/social-accounts" "{\"platformCode\":\"instagram\",\"externalAccountId\":\"ig-$RUN\",\"accessToken\":\"secret-token-$RUN\"}" 201
call POST "/api/businesses/$BIZ/campaigns" '{"name":"Docker","objective":"Awareness","goalStatement":"Check persistence"}' 201
CAMP=$(json "d['id']")
call POST "/api/businesses/$BIZ/content-items" "{\"campaignId\":\"$CAMP\",\"platformCode\":\"instagram\",\"contentType\":\"Post\",\"firstRevision\":{\"text\":\"Hello from Docker\",\"format\":\"Text\"}}" 201
ITEM=$(json "d['id']")
call PATCH "/api/businesses/$BIZ/content-items/$ITEM/status" '{"status":"InReview"}'
call POST "/api/businesses/$BIZ/content-items/$ITEM/reviews" '{"decision":"Approved"}' 201
USERS_BEFORE=$(sql "SELECT COUNT(*) FROM AppUsers")

echo "  -> docker compose down   (containers and network removed, named volumes kept)"
docker compose down 2>&1 | tail -3 | sed 's/^/     /'
echo "  -> docker compose up -d"
docker compose up -d 2>&1 | tail -3 | sed 's/^/     /'
check "API back after the restart" "wait_api"
docker compose logs api 2>&1 | grep -E "up to date|Applying" | tail -1 | sed 's/^/  /'

check "same number of users in the database ($USERS_BEFORE)" '[ "$(sql "SELECT COUNT(*) FROM AppUsers")" = "$USERS_BEFORE" ]'
TOKEN=""
call POST /api/auth/login "{\"email\":\"$EMAIL\",\"password\":\"Secret123\"}"
TOKEN=$(json "d['accessToken']")
call GET "/api/businesses/$BIZ"
curl -s -o /tmp/sm_img_back.png -H "Authorization: Bearer $TOKEN" "$API/api/businesses/$BIZ/media/$MEDIA/content"
check "uploaded file survived (same SHA-256)" '[ "$(sha256sum /tmp/sm_img_back.png | cut -d" " -f1)" = "$SHA" ]'
# The social token is encrypted with Data Protection keys kept in the appdata volume.
# Publishing needs to decrypt it: if the keys had been lost, this job would fail.
call POST "/api/businesses/$BIZ/content-items/$ITEM/schedule" '{}' 201
JOB=$(json "d['id']")
STATUS=""
for _ in $(seq 1 30); do
  curl -s -o /tmp/sm_last.json -H "Authorization: Bearer $TOKEN" "$API/api/businesses/$BIZ/publication-jobs/$JOB"
  STATUS=$(json "d['status']['code']"); [ "$STATUS" = "Succeeded" ] || [ "$STATUS" = "Failed" ] && break; sleep 2
done
check "encrypted token still readable after restart: post published by the worker ($STATUS)" '[ "$STATUS" = "Succeeded" ]'

title "6. Network connectivity between containers"
docker network ls --filter name=smart-marketing --format '  network: {{.Name}} ({{.Driver}})'
docker network inspect smart-marketing_backend --format '{{range .Containers}}  member: {{.Name}} {{.IPv4Address}}{{"\n"}}{{end}}'
check "the API container resolves the service name 'db'" 'docker compose exec -T api getent hosts db | sed "s/^/  /"'
curl -s -o /tmp/sm_last.json "$API/health"
check "API -> database health check is Healthy" '[ "$(json "d[\"checks\"][0][\"status\"]")" = "Healthy" ]'
curl -s -H 'Accept-Language: ar' "$API/api/statuses/Campaign" -o /tmp/sm_last.json
check "translations shipped in the image (Arabic statuses)" '[ "$(json "d[0][\"code\"]")" = "Draft" ]'

title "RESULT"
if [ "$FAILS" = "0" ]; then echo "  ALL CHECKS PASSED"; else echo "  $FAILS CHECK(S) FAILED"; fi
exit "$FAILS"
