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
#
# [X] 초안은 하나의 3상태 변수로 006 과 009 를 갈랐다. 그래서 둘이 "배타적" 이 됐는데,
#     배타성은 Rule 이 아니라 **둘 다 PM Work 를 쓴 선택**의 결과였다. 마감은 Slot 마다 다르다.
#     TVF 에 시각을 주입해 실측: 13:00 에 AM Work 는 CutoffPassed=1, PM Work 는 0 이다.
#     CWR-009 를 AM 으로 옮기면 11:10~15:50 에 둘 다 판정된다.
#     이전 구성에서 CWR-009 는 16:00~18:00 에만 돌아 일반 회귀가 한 번도 닿지 못했다.
#   CUTPM  PM Work 성공 경로(CWR-006)     15:50 전이면 1
#   CUTAM  AM Work 마감경과 경로(CWR-009)  11:10 이후면 1
CUTPM=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON;
  SELECT CASE WHEN CONVERT(TIME(0), SYSDATETIME()) <  CONVERT(TIME(0), '15:50:00') THEN 1 ELSE 0 END;" | tr -d ' \r')
CUTAM=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON;
  SELECT CASE WHEN CONVERT(TIME(0), SYSDATETIME()) >= CONVERT(TIME(0), '11:10:00') THEN 1 ELSE 0 END;" | tr -d ' \r')

# 업무일 여부. BIZ 와 나눠 재야 창 밖에서 308 과 309 를 갈라 판정할 수 있다.
DAYOK=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON;
  SELECT CASE WHEN DATEPART(WEEKDAY, SYSDATETIME()) BETWEEN 2 AND 7
              AND NOT EXISTS (SELECT 1 FROM dbo.휴무일
                               WHERE [휴무일자] = CONVERT(DATE, SYSDATETIME()) AND [사용여부] = 1)
         THEN 1 ELSE 0 END;" | tr -d ' \r')

# SEL-025 정원 일치. 조회 SP 의 Capacity 와 저장 SP 의 상한이 갈리면 화면은 "자리 있음" 을
# 보여주고 저장은 305 를 낸다 (06 §43-21). 정원 20 은 조회 SP 8곳·저장 SP 2곳에 따로 박혀 있고
# 어떤 시험도 둘의 일치를 보지 않았다.
#   조회 쪽은 여기서 매 회귀 고정한다 (SELECT SP 라 시각과 무관하다).
#   저장 쪽은 CON-002(19/20 → 20)·CON-008(20/20 → 305)이 경계에서 고정한다.
#   한쪽만 바뀌면 둘 중 하나가 반드시 깨진다.
CAPQ="SET NOCOUNT ON; DECLARE @P BIGINT = (SELECT TOP (1) [수검자ID] FROM [dbo].[수검자] ORDER BY [수검자ID]); EXEC [dbo].[USP_HC_예약가능정보_조회] @P, NULL, NULL, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0;"
CAPP=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -s"|" -Q "$CAPQ" 2>/dev/null \
       | grep -E '^(AM|PM)\|' | cut -d'|' -f3 | sort -u | tr -d ' \r' | tr '\n' ' ' | sed 's/ *$//')
if [ "$CAPP" = "20" ]; then
  echo "PASS SEL-025 조회 SP 의 Capacity = 20 (AM·PM 동일) — 저장 쪽은 CON-002·008 이 고정" >> "$OUT"
else
  echo "FAIL SEL-025 조회 SP 의 Capacity 가 [$CAPP] 다 (기대 20)" >> "$OUT"; FAILED=1
fi

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
  # OFF-* 는 업무시간 **밖**에서만 성립한다. PWR/RWR/CWR 과 정확히 배타적이라
  # 어느 시각에 돌려도 Write SP 의 업무시간 계약이 한쪽에서 반드시 판정된다.
  case "$k" in
    OFF-309-*) if [ "${BIZ:-0}" -eq 1 ] || [ "${DAYOK:-0}" -ne 1 ]; then
        echo "SKIP $k 업무시간 안이거나 업무일이 아님 — 309 는 업무일의 시간 밖에서만 나온다" >> "$OUT"; continue; fi ;;
    # OFF-308 은 가드하지 않는다. 시험이 오늘을 활성 휴무일로 **직접 심고** 지운다 —
    # 일요일을 기다리던 구성에서는 평일 회차마다 SKIP 이라 한 번도 판정된 적이 없었다.
  esac
  # 접수완료는 업무시간 안에서도 접수마감(AM 11:00 / PM 16:00) 전이어야 성공한다 (05 §2.4).
  # CWR-006(성공)과 CWR-009(마감경과)는 배타적이라 시각으로 갈라 하나만 판정한다.
  case "$k" in
    CWR-006_*) if [ "${CUTPM:-0}" -ne 1 ]; then
        echo "SKIP $k PM 마감(16:00) 10분 전을 지났다 — 성공 경로는 마감 전에만 성립한다" >> "$OUT"; continue; fi ;;
    CWR-009_*) if [ "${CUTAM:-0}" -ne 1 ]; then
        echo "SKIP $k 11:10 이전 — AM 마감(11:00) 경과 경로는 그 뒤에만 성립한다" >> "$OUT"; continue; fi ;;
  esac
  # -W -w 65535 를 빼지 않는다. 기본 폭 80 에서 줄이 접히면 파서가 무너진다(실측 확인).
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -W -w 65535 -s"|" \
         -i "$f" -o "artifacts/logs/rs_${k}.txt" || FAILED=1
  node tools/verify-contract.js "artifacts/logs/rs_${k}.txt" "$k" >> "$OUT" 2>&1 || FAILED=1
done

# OFF-308 이 심은 임시 휴무일이 남아 있으면 뒤따르는 모든 회차가 308 이 된다.
# 시험 파일이 지우지만 한 번 더 확인한다 — 남은 채로 green 을 내지 않는다.
LEFT=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON;
  DELETE FROM dbo.휴무일 WHERE [휴무일명] = N'OFF-308 시험용 임시 휴무일';
  SELECT CONVERT(VARCHAR(5), @@ROWCOUNT) + '/' + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM dbo.휴무일));" | tr -d ' \r')
# [X] 여기서 총계까지 세면 Seed 건수의 **세 번째 사본**이 된다(ROOT AGENTS.md §6).
#     총계 판정은 SED-008 · VER-007 · RBD-004 가 이미 한다. 이 게이트가 지키는 것은
#     "OFF-308 이 심은 임시 행이 남지 않았다" 하나뿐이므로 잔여만 본다.
#     R8 에서 Seed 가 2 -> 41 이 되며 이 줄이 거짓이 되었고, 아무도 그것을 보지 않았다.
case "$LEFT" in
  0/*) echo "PASS OFF-308-CLEAN 임시 휴무일 잔여 0건 (총계 $LEFT — 총계는 SED-008·VER-007 이 판정한다)" >> "$OUT" ;;
  *)   echo "FAIL OFF-308-CLEAN 임시 휴무일이 남았다 — 잔여/총계 = $LEFT" >> "$OUT"; FAILED=1 ;;
esac

cat "$OUT"
echo "contract-verify FAILED=$FAILED"
exit $FAILED
