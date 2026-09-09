#!/usr/bin/env bash
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'
DB='HealthCheckupReservationReceptionDb'
mkdir -p artifacts/logs artifacts/reports
FAILED=0
NOTRUN=0

./scripts/rebuild.sh || { echo "rebuild 실패"; exit 1; }

# 운영기준(04 §8.7) 게이트. R13 이 운영시간·마감시각을 SP 안 리터럴에서 테이블로 옮겼고,
# 그래서 시험이 창을 옮겨 성공 경로를 24시간 판정할 수 있게 됐다. 그 대가로 새 실패 방식이
# 하나 생긴다 — **넓힌 창을 되돌리지 않은 회차**. 그러면 다음 회차가 다른 제품을 시험한다.
#   OPR-G1·G2  값의 소유자는 00 이다 (CP-04 · §3 마감표)
#   OPR-G3     02_Seed.sql 이 그 값과 같다        → "설정" 이 아니라 정책이다 (06 §43-21)
#   OPR-G4     실물 [운영기준] 이 그 값과 같다    → 넓힌 창이 되돌아왔다
# 자체시험을 먼저 돌린다. 게이트가 변조를 못 잡으면 아래 두 판정은 아무 뜻이 없다.
if ./scripts/verify-operating-baseline.sh selftest > /dev/null; then
  echo "PASS OPR-SELFTEST 운영기준 게이트가 Seed·00 변조를 실제로 잡는다"
else ./scripts/verify-operating-baseline.sh selftest; FAILED=1; fi
opr() {                       # opr <어디까지 돌았는지>
  if ./scripts/verify-operating-baseline.sh > /dev/null; then
    echo "PASS OPR-G1~G4 운영기준 = 00 ($1 뒤) — 넓힌 창이 되돌아왔다"
  else ./scripts/verify-operating-baseline.sh; FAILED=1; fi
}

# 이 회차가 쓴 로그만 목록에 남긴다. artifacts/logs/ 는 누적되고 옛 RED 회차의
# test_01_red.log 같은 파일이 섞이면 요약에 이 회차와 무관한 FAIL 이 들어온다 (실측).
: > artifacts/logs/_manifest.txt

run() {                       # run <파일> <로그번호>
  local f="$1" log="artifacts/logs/test_${2}.log" rc=0
  echo "$log" >> artifacts/logs/_manifest.txt
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
# Security(계정·권한)는 2026-09-07 사용자 결정으로 **폐기**했다 (06 §32 · §39).
# NOT RUN 으로 미루지 않는다 — 미룬 것이 아니라 산출물이 아니므로 잔여를 남기지 않는다.
# 남은 SEC-010(secret 스캔)은 scripts/verify-no-secret.sh 가 아래에서 판정한다.
run tests/15_Holiday_Tests.sql            15      # R7 — 기준정보 SP 4개. Seed 를 건드리므로 자기가 심은 것을 자기가 지운다.
                                                  #   14 보다 **앞**에 둔다. 15 가 남긴 행은 바로 뒤 14 의 RBD-004 지문
                                                  #   (...|19|41)이 잡는다. RBD-005 의 diff 는 clean-rebuild-verify.sh 가
                                                  #   매번 새 DB 에서 뜨므로 15 의 잔여가 거기까지 가지 못한다.
run tests/14_Clean_Rebuild_Verify.sql     14

# 계약 검증 (G09) — 스펙 §8.4 가 test.sh 범위로 지정했다
./scripts/verify-contract-all.sh || FAILED=1

# tests/05~08 과 계약 게이트가 넓힌 창을 되돌렸는가. **여기서** 한 번 물어야 한다 —
# 뒤의 동시성 루프가 시나리오마다 rebuild 를 돌려 잔여를 지워 버리기 때문이다.
opr "쓰기 시험·계약 게이트"

# R4-3 C# 호출 (06 §46.3). 여기 두는 이유는 DB 에 Fixture 가 살아 있는 마지막 지점이기
# 때문이다 — 뒤의 verify-red·clean-rebuild 가 DB 를 다시 만든다.
# csc.exe 가 없는 환경에서는 exit 3 = NOT RUN 이다. SKIP 을 PASS 로 쓰지 않는다.
crc=0; ./scripts/verify-csharp-call.sh > /dev/null || crc=$?
if   [ "$crc" -eq 0 ]; then echo "PASS CS-001~016 C# 호출 — 한글 SP·Parameter·Result Set 컬럼을 ADO.NET 으로 실제 호출"
elif [ "$crc" -eq 3 ]; then ./scripts/verify-csharp-call.sh; NOTRUN=$((NOTRUN+1))
else ./scripts/verify-csharp-call.sh; FAILED=1; fi

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

# 동시성도 창을 넓힌다. 뒤의 verify-red·clean-rebuild 가 DB 를 다시 만들기 전에 묻는다.
opr "동시성"

# RED 음성시험 (G15) — "시험이 실제로 실패를 잡는가" 를 폐기용 DB 에서 판정한다 (스펙 §40a).
# clean-rebuild 보다 **먼저** 돌린다. 폐기용 DB 가 남아 있으면 RBD-009(타 DB 무사)를 깨뜨리는데,
# 여기서 먼저 돌려 지워지는 것까지 확인해야 그 순서가 증거가 된다.
if ./scripts/verify-red.sh > /dev/null; then
  echo "PASS RED-001~004 음성시험 — 객체 없음·테이블 부족·Seed 오염을 시험이 실제로 잡는다 (폐기용 DB)"
else ./scripts/verify-red.sh; FAILED=1; fi

# Clean Rebuild 계약 중 회차 사이 비교가 필요한 것 (RBD-002·003·005·007·008·009).
# tests/14 는 한 회차의 지문만 보므로 이 스크립트가 없으면 G14 의 절반이 빈다.
./scripts/clean-rebuild-verify.sh || FAILED=1

# SEC-010 (secret 스캔) 은 SQL 이 아니라 셸이다. 전체 회귀에 반드시 포함한다.
./scripts/verify-no-secret.sh || FAILED=1

# G13-b (허용 T-SQL 목록) 도 셸이다. 계획서의 일회성 grep 은 주석과 JS 의 .trim( 을 잡아
# 항상 FAIL 했다 — 반복 가능한 게이트로 옮겼다 (스펙 §9.2 · plans/08 T37 Step 4).
./scripts/verify-tsql-allowlist.sh > /dev/null || { ./scripts/verify-tsql-allowlist.sh; FAILED=1; }

# G13-c (§9.2 허용목록 준수) 의 검토 기록. 위 스캔은 **금지 블랙리스트**만 보므로
# 그 BAN 에 없는 TVP·Trigger·FK Cascade 는 아무도 보지 않았다(실측). 검토 파일을 손으로
# 남기면 다음 회차에 썩으니 매 회차 다시 만들고, 목록 밖이 나오면 회귀를 깨뜨린다.
if node tools/allowlist-review.js > /dev/null; then
  echo "PASS G13-c §9.2 허용목록 검토 재생성 · 목록 밖 0건 (artifacts/reports/allowlist-review.md)"
else
  node tools/allowlist-review.js; FAILED=1
fi

# 기준선·WinForms·스키마 대조 (G00·G01·G05).
# [X] 셋 다 회귀 밖에 있었다. §42 를 채우려고 손으로 돌린 증거는 다음 회차에 썩는다 —
#     G13-b 와 같은 실패 방식이다. 회귀가 매번 판정하게 한다.
# [X] 결과와 무관하게 PASS 를 찍지 않는다. 셋 다 성공했을 때만 한 줄을 남긴다.
# [X] 봉인 건수를 이 줄에 적어 두었더니 06 이 입주해 7건이 된 뒤에도 "6/6" 을 찍었다.
#     매 회차 거짓을 출력하면서 아무도 잡지 않았다 (ROOT AGENTS.md §6). 세는 곳을 하나로 둔다 —
#     게이트가 낸 값을 그대로 옮긴다.
G0=0
BL=$(./scripts/verify-baseline.sh 2>&1 | tail -1) || { echo "$BL"; ./scripts/verify-baseline.sh; G0=1; }
./scripts/verify-winforms-unchanged.sh > /dev/null || { ./scripts/verify-winforms-unchanged.sh; G0=1; }
./scripts/verify-winforms-unchanged.sh selftest > /dev/null || { ./scripts/verify-winforms-unchanged.sh selftest; G0=1; }
./scripts/verify-schema-doc.sh         > /dev/null || { ./scripts/verify-schema-doc.sh;         G0=1; }
if [ "$G0" -eq 0 ]; then
  echo "PASS G00 기준선 $(echo "$BL" | tr -d '= ') · G01 WinForms 변경 0건(+변조 감지 자체시험) · G05 스키마↔04 양방향 대조"
else
  echo "FAIL G00/G01/G05 중 하나 이상 실패 — 위 출력을 보라"; FAILED=1
fi

# 문서 정합성 게이트 (스펙 §45.3)
node tools/verify-docs.js || FAILED=1

# 공휴일 Seed 만료·사본 일치 (00 HOL-06 · 06 §14.7). DB 없이 돈다.
# [!] 이 게이트는 **날짜가 지나면 저절로 red 가 된다.** 그것이 목적이다 —
#     공휴일 Seed 는 만료해도 아무것도 실패하지 않고 조용히 오판정을 낸다.
./scripts/verify-holiday-seed.sh || FAILED=1

# R4-2 기대값 ↔ 기준선 (06 §46.3). verify-contract.js 는 기대값과 실행결과를 맞출 뿐이라
# 둘이 **같이** 틀리면 통과한다 — 06 §44.8 의 결함이 그 축으로 살아남았다.
# 기대값의 Result Set 컬럼을 기준선 05 의 표와 직접 대조하는 것은 이 게이트뿐이다.
node tools/verify-rs-contract.js || FAILED=1

# RBD-001(잘못된 서버명 50020)은 인스턴스가 1개뿐이라 음성 시험이 **구조적으로** 불가능하다.
# clean-rebuild-verify.sh 가 NOT RUN 으로 출력하는데 여기서 세지 않으면 요약이 "NOT RUN 0" 을
# 주장하게 된다 — 실행하지 않은 것을 통과로 읽히게 하지 않는다 (CLAUDE.md §10).
NOTRUN=$((NOTRUN+1))

[ "$NOTRUN" -ne 0 ] && echo "!! NOT RUN $NOTRUN 건 — 아래 요약을 완료로 읽지 않는다 (CLAUDE.md §10)"

if [ "$FAILED" -eq 0 ]; then
  echo "=== 실패 0건 (NOT RUN $NOTRUN 건) ==="; exit 0
else echo "=== 실패한 단계가 있습니다 (NOT RUN $NOTRUN 건) ==="; exit 1; fi
