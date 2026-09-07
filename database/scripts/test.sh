#!/usr/bin/env bash
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'
DB='HealthCheckupReservationReceptionDb'
mkdir -p artifacts/logs artifacts/reports
FAILED=0
NOTRUN=0

./scripts/rebuild.sh || { echo "rebuild 실패"; exit 1; }

run() {                       # run <파일> <로그번호>
  local f="$1" log="artifacts/logs/test_${2}.log" rc=0
  echo "--- $f"
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -i "$f" -o "$log" || rc=$?
  iconv -f UTF-16 -t UTF-8 "$log" | grep -E '^(PASS|FAIL|SKIP|INFO|Msg )' || true
  [ "$rc" -ne 0 ] && { echo "!! $f exit=$rc"; FAILED=1; }
  return 0
}

run tests/00_Test_Harness.sql             00
run tests/01_Schema_Tests.sql             01
run tests/02_Seed_Tests.sql               02
run tests/00b_Test_Harness_RCP.sql        00b     # T14b — TVF 배포 이후이고 tests/03 보다 앞이어야 한다
                                                  #   RUL-A09 가 T011 의 RCP Work(저장 NEX)를 읽는다
run tests/03_Rule_Tests.sql               03
run tests/04_Select_SP_Tests.sql          04
run tests/05_Patient_Write_Tests.sql      05
run tests/06_Reservation_Write_Tests.sql  06
run tests/07_Reception_Write_Tests.sql    07
run tests/08_Rollback_Tests.sql           08
# Security(T31·T32)는 범위 밖이다 (06 §32, 2026-09-07 사용자 결정).
# 파일이 생기면 자동으로 실행된다. NOT RUN 은 PASS 가 아니다 (CLAUDE.md §10).
if [ -f tests/13_Security_Tests.sql ]; then run tests/13_Security_Tests.sql 13
else echo "NOT RUN tests/13_Security_Tests.sql — Security 는 범위 밖 (06 §32)"; NOTRUN=$((NOTRUN+1)); fi
run tests/14_Clean_Rebuild_Verify.sql     14

# 계약 검증 (G09) — 스펙 §8.4 가 test.sh 범위로 지정했다
./scripts/verify-contract-all.sh || FAILED=1

# 동시성 (G11) — 8개 시나리오
if [ ! -x scripts/concurrency-test.sh ]; then
  echo "NOT RUN scripts/concurrency-test.sh — T34 미착수. CON-001~008 이 판정되지 않는다"
  NOTRUN=$((NOTRUN+1))
else
for s in 1 2 3 4 5 6 7 8; do
  # [X] 시나리오마다 rebuild + fixture 재배치. T34 Step 4 가 "시나리오 간 오염이 없다"의 근거로 삼은 절차다.
  #     이것이 없으면 tests/01~14 가 이미 변형한 DB 위에서 CON-002(19/20) 가 첫 실행부터 FAIL 한다.
  # exit 3 = 업무시간·접수마감 밖이라 실행하지 않았다. SKIP 은 PASS 가 아니다 (CLAUDE.md §10).
  # 게이트를 **먼저** 묻는다. 창 밖에서 rebuild 를 8회 돌리면 몇 분을 버리고 결과는 전부 NOT RUN 이다.
  crc=0; ./scripts/concurrency-test.sh --check "$s" || crc=$?
  if [ "$crc" -eq 3 ]; then NOTRUN=$((NOTRUN+1)); continue; fi
  ./scripts/rebuild.sh > /dev/null 2>&1 || { FAILED=1; continue; }
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -i tests/00_Test_Harness.sql > /dev/null 2>&1 || FAILED=1
  crc=0; ./scripts/concurrency-test.sh "$s" || crc=$?
  if [ "$crc" -eq 3 ]; then NOTRUN=$((NOTRUN+1)); elif [ "$crc" -ne 0 ]; then FAILED=1; fi
done
fi

# Clean Rebuild 계약 중 회차 사이 비교가 필요한 것 (RBD-002·003·005·007·008·009).
# tests/14 는 한 회차의 지문만 보므로 이 스크립트가 없으면 G14 의 절반이 빈다.
./scripts/clean-rebuild-verify.sh || FAILED=1

# SEC-010 (secret 스캔) 은 SQL 이 아니라 셸이다. 전체 회귀에 반드시 포함한다.
./scripts/verify-no-secret.sh || FAILED=1

# 문서 정합성 게이트 (스펙 §45.3)
node tools/verify-docs.js || FAILED=1

[ "$NOTRUN" -ne 0 ] && echo "!! NOT RUN $NOTRUN 건 — 아래 요약을 완료로 읽지 않는다 (CLAUDE.md §10)"

if [ "$FAILED" -eq 0 ]; then
  echo "=== 실패 0건 (NOT RUN $NOTRUN 건) ==="; exit 0
else echo "=== 실패한 단계가 있습니다 (NOT RUN $NOTRUN 건) ==="; exit 1; fi
