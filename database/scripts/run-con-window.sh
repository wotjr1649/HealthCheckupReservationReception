#!/usr/bin/env bash
# 동시성 8종을 **한 창 안에서** 몰아 돌린다 (G11 증거를 한 회차로 모은다).
#   CON-005·CON-008 은 접수완료 성공이 필요해 PM Slot 창(11:00~15:50)에서만 성립한다 (스펙 §38.7).
#   그 창 밖이면 concurrency-test.sh 가 exit 3(NOT RUN)을 내고 여기서도 그대로 센다.
#
# 시각을 옮겨서 돌리는 경우: 이 스크립트는 시각을 **바꾸지 않는다.** 바꾸는 것도 되돌리는 것도
# 사람이 한다 — 되돌리지 못한 채 죽는 자동화를 남기지 않는다.
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'; DB='HealthCheckupReservationReceptionDb'

NOW=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q \
      "SET NOCOUNT ON; SELECT CONVERT(VARCHAR(19), SYSDATETIME(), 120);" 2>/dev/null | tr -d ' \r' | head -1)
echo "INFO SQL Server 현재 시각: $NOW"
echo "INFO 셸 현재 시각:        $(date '+%Y-%m-%dT%H:%M:%S')"

# [X] 옛 회차의 conc_*.log 를 지우고 시작한다. 시각을 뒤로 옮겨 돌리면 새 로그의 파일명
#     conc_<RUN>_<SCEN>_*.log 가 옛 것보다 **사전순으로 앞서서**, make-summary.sh 의
#     'sort | tail -1'(시나리오별 최신) 이 옛 회차를 최신으로 집는다 (실측 대비).
rm -f artifacts/logs/conc_*.log
echo "INFO 이전 conc_*.log 제거 — 시각을 옮겨 돌려도 최신 회차 판별이 뒤집히지 않는다"

PASSN=0; FAILN=0; NOTRUN=0
for s in 1 2 3 4 5 6 7 8; do
  crc=0; ./scripts/concurrency-test.sh --check "$s" || crc=$?
  if [ "$crc" -eq 3 ]; then NOTRUN=$((NOTRUN+1)); continue; fi
  ./scripts/rebuild.sh > /dev/null 2>&1 || { echo "FAIL 시나리오 $s rebuild"; FAILN=$((FAILN+1)); continue; }
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -i tests/00_Test_Harness.sql > /dev/null 2>&1 \
    || { echo "FAIL 시나리오 $s harness"; FAILN=$((FAILN+1)); continue; }
  crc=0; ./scripts/concurrency-test.sh "$s" || crc=$?
  if   [ "$crc" -eq 0 ]; then PASSN=$((PASSN+1))
  elif [ "$crc" -eq 3 ]; then NOTRUN=$((NOTRUN+1))
  else FAILN=$((FAILN+1)); fi
done

echo
echo "=== CON 요약  통과 $PASSN · 실패 $FAILN · NOT RUN $NOTRUN ==="
[ "$NOTRUN" -ne 0 ] && echo "!! NOT RUN 은 PASS 가 아니다 (CLAUDE.md §10)"
[ "$FAILN" -eq 0 ] && [ "$NOTRUN" -eq 0 ] && echo "CON-001~008 전건 통과"
exit $FAILN
