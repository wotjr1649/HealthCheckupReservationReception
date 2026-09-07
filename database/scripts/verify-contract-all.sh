#!/usr/bin/env bash
# tests/contract/* 를 전건 실행하고 tools/verify-contract.js 로 계약을 판정한다 (G09).
# set -e 를 쓰지 않는다 — 실패한 줄에서 셸이 끝나 RC 수집도 로그 출력도 안 된다.
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'; DB='HealthCheckupReservationReceptionDb'
OUT=artifacts/reports/contract-verify.txt
mkdir -p artifacts/logs artifacts/reports
: > "$OUT"
FAILED=0

# Write SP 의 성공 시나리오는 업무시간(월~토 09:00~18:00, 비휴무일) 밖에서 RS0(308/309) 하나만
# 반환한다. expected-contracts.json 이 RS 2개를 기대하므로 야간 회귀는 반드시 FAIL 한다.
# Write SP 계약은 업무시간에만 판정하고, 밖이면 SKIP 을 남긴다. SKIP 은 PASS 가 아니다.
BIZ=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON;
  SELECT CASE WHEN DATEPART(WEEKDAY, SYSDATETIME()) BETWEEN 2 AND 7
              AND CONVERT(TIME(0), SYSDATETIME()) >= '09:00:00'
              AND CONVERT(TIME(0), SYSDATETIME()) <  '18:00:00'
              AND NOT EXISTS (SELECT 1 FROM dbo.휴무일
                               WHERE [휴무일자] = CONVERT(DATE, SYSDATETIME()) AND [사용여부] = 1)
         THEN 1 ELSE 0 END;" | tr -d ' \r')

# 접수마감(AM 11:00 / PM 16:00) 판정. 이 값은 게이트 시작 시 한 번만 재므로 경계 10분 전부터는
# 어느 쪽도 판정하지 않는다 — 실측: 15:59 에 시작한 회차가 16:00 을 넘겨 CWR-006 이 304 를 받았다.
#   1 = 마감까지 여유 있음(CWR-006)   2 = 마감 경과(CWR-009)   0 = 경계 근처(둘 다 SKIP)
CUT=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON;
  DECLARE @T TIME(0) = CONVERT(TIME(0), SYSDATETIME());
  SELECT CASE WHEN @T >= CONVERT(TIME(0), '16:00:00') THEN 2
              WHEN @T <  CONVERT(TIME(0), '10:50:00') THEN 1
              WHEN @T >= CONVERT(TIME(0), '11:00:00')
               AND @T <  CONVERT(TIME(0), '15:50:00') THEN 1
              ELSE 0 END;" | tr -d ' \r')

for f in tests/contract/*.sql; do
  k=$(basename "$f" .sql)
  # SELECT SP 계약은 시간대와 무관하다. Write SP 계약만 가드한다 (T36 이 추가한다).
  case "$k" in
    PWR-*|RWR-*|CWR-*)
      if [ "${BIZ:-0}" -ne 1 ]; then
        echo "SKIP $k 업무시간 밖 — Write SP 는 308/309 를 업무 Rule 보다 먼저 판정한다" >> "$OUT"
        continue
      fi ;;
  esac
  # 접수완료는 업무시간 안에서도 접수마감(AM 11:00 / PM 16:00) 전이어야 성공한다 (05 §2.4).
  # CWR-006(성공)과 CWR-009(마감경과)는 배타적이라 시각으로 갈라 하나만 판정한다.
  case "$k" in
    CWR-006_*) if [ "${CUT:-0}" -ne 1 ]; then
        echo "SKIP $k 접수마감 경과 또는 경계 10분 이내 — 성공 경로는 마감 전에만 성립한다" >> "$OUT"; continue; fi ;;
    CWR-009_*) if [ "${CUT:-0}" -ne 2 ]; then
        echo "SKIP $k 접수마감 전 — 마감경과 경로는 마감 후에만 성립한다" >> "$OUT"; continue; fi ;;
  esac
  # -W -w 65535 를 빼지 않는다. 기본 폭 80 에서 줄이 접히면 파서가 무너진다(실측 확인).
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -W -w 65535 -s"|" \
         -i "$f" -o "artifacts/logs/rs_${k}.txt" || FAILED=1
  node tools/verify-contract.js "artifacts/logs/rs_${k}.txt" "$k" >> "$OUT" 2>&1 || FAILED=1
done

cat "$OUT"
echo "contract-verify FAILED=$FAILED"
exit $FAILED
