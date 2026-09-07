#!/usr/bin/env bash
# RED 음성시험 — "시험이 실제로 실패를 잡는가" 를 재현 가능하게 판정한다.
#
# [X] 계획서의 RED Step 5건(plans/01 T02·T06·T07 · plans/02 T09·T11)은 일회성 관측이라
#     산출물이 남지 않았고, 그래서 체크박스를 닫을 수 없었다. 재현하려면 대상 DB 를 파괴하거나
#     커밋된 파일을 변조해야 해서 미이행으로 뒀다.
#     **폐기용 별도 DB** 를 쓰면 둘 다 필요 없다. 대상 DB 와 커밋된 파일은 손대지 않는다.
#
# [!] 이 스크립트는 자기가 만든 DB 만 지운다. 대상 DB 이름과 한 글자도 겹치지 않는 이름을 쓰고,
#     시작과 끝에서 각각 지운다 — 중간에 죽어도 잔여가 다음 회차의 RBD-009(타 DB 무사)를 깨지 않는다.
#
# set -e 를 쓰지 않는다 — 실패한 줄에서 셸이 끝나 판정도 로그 출력도 안 된다 (CLAUDE.md §6).
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'
DB='HealthCheckupReservationReceptionDb'
RED='HC_RedProbe'                       # 대상 DB 와 접두사도 겹치지 않는다
OUT=artifacts/reports/red-probe.txt
mkdir -p artifacts/reports artifacts/logs
: > "$OUT"
FAILED=0

say() { echo "$1" | tee -a "$OUT"; }
m()  { sqlcmd -S "$SRV" -E -d master -b -I -Q "$1" > /dev/null 2>&1; }
run() {                                  # run <파일> <로그이름> -> exit code
  sqlcmd -S "$SRV" -E -d "$RED" -b -I -u -i "$1" -o "artifacts/logs/red_$2.log" 2>/dev/null
  echo $?
}
has() { iconv -f UTF-16 -t UTF-8 "artifacts/logs/red_$1.log" 2>/dev/null | grep -c "$2" || true; }

m "IF DB_ID('$RED') IS NOT NULL BEGIN ALTER DATABASE [$RED] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$RED]; END"
m "CREATE DATABASE [$RED] COLLATE Korean_Wansung_CI_AS"
if ! sqlcmd -S "$SRV" -E -d "$RED" -b -I -Q "SELECT 1" > /dev/null 2>&1; then
  say "FAIL RED-SETUP 폐기용 DB [$RED] 를 만들지 못했다"; exit 1
fi

# ── RED-001  객체가 하나도 없는 DB 에서 스키마 시험이 실패하는가 (plans/01 T06 Step 1)
# [X] 기대를 Msg 208 로 잡았다가 틀렸다. sys.tables·sys.procedures 는 **어느 DB 에나 있어**
#     개체오류가 아니라 단언 실패로 잡힌다. 실측이 옳고 기대가 틀렸다 — 실측에 맞춘다.
RC=$(run tests/01_Schema_Tests.sql 001)
N=$(has 001 '^FAIL SCH-')
if [ "$RC" -ne 0 ] && [ "$N" -ge 10 ]; then
  say "PASS RED-001 객체 없는 DB 에서 01_Schema_Tests 가 SCH 단언 $N 건을 실패로 잡았다 (exit=$RC)"
else say "FAIL RED-001 객체가 없는데 통과했다 (exit=$RC · FAIL 줄 $N 건)"; FAILED=1; fi

# ── RED-002  TVF 가 없는 DB 에서 Rule 시험이 실패하는가 (plans/02 T11 Step 2)
RC=$(run tests/03_Rule_Tests.sql 002)
N=$(( $(has 002 'Msg 208') + $(has 002 'Msg 4121') ))
if [ "$RC" -ne 0 ] && [ "$N" -ne 0 ]; then
  say "PASS RED-002 TVF 없는 DB 에서 03_Rule_Tests 가 Msg 208/4121 로 실패 (exit=$RC)"
else say "FAIL RED-002 TVF 가 없는데 통과했다 (exit=$RC · 개체오류 $N 건)"; FAILED=1; fi

# ── RED-003  테이블이 하나 모자라면 SCH-001 이 잡는가 (plans/01 T07 Step 1 의 취지)
#    계획서는 '기대값을 8로 틀리게 적어 실패를 본다' 였다. 시험 파일을 변조하는 대신
#    **실측 쪽을 하나 줄여** 같은 것을 증명한다 — 기대 6 vs 실측 5.
sqlcmd -S "$SRV" -E -d "$RED" -b -I -i deploy/01_Schema.sql -o artifacts/logs/red_schema.log 2>/dev/null
if [ $? -ne 0 ]; then say "FAIL RED-003 폐기용 DB 에 스키마를 배포하지 못했다"; FAILED=1; fi
m2() { sqlcmd -S "$SRV" -E -d "$RED" -b -I -Q "$1" > /dev/null 2>&1; }
m2 "DROP TABLE [dbo].[완료이력]"
RC=$(run tests/01_Schema_Tests.sql 003)
N=$(has 003 'FAIL SCH-001')
if [ "$RC" -ne 0 ] && [ "$N" -ne 0 ]; then
  say "PASS RED-003 테이블 6->5 에서 SCH-001 이 실패를 잡았다 (exit=$RC)"
else say "FAIL RED-003 테이블이 모자란데 SCH-001 이 통과했다 (exit=$RC · FAIL줄 $N 건)"; FAILED=1; fi

# ── RED-004  Seed 에 없는 행을 하나 넣으면 SED 가 잡는가 (plans/02 T09 Step 1)
m "IF DB_ID('$RED') IS NOT NULL BEGIN ALTER DATABASE [$RED] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$RED]; END"
m "CREATE DATABASE [$RED] COLLATE Korean_Wansung_CI_AS"
sqlcmd -S "$SRV" -E -d "$RED" -b -I -i deploy/01_Schema.sql -o artifacts/logs/red_schema2.log 2>/dev/null
sqlcmd -S "$SRV" -E -d "$RED" -b -I -i deploy/02_Seed.sql   -o artifacts/logs/red_seed.log   2>/dev/null
m2 "INSERT INTO [dbo].[검사코드] ([검사항목코드],[검사항목명],[국가검사규칙코드]) VALUES ('EX999', N'없는검사', 'NEX-01')"
RC=$(run tests/02_Seed_Tests.sql 004)
N=$(has 004 '^FAIL')
if [ "$RC" -ne 0 ] && [ "$N" -ne 0 ]; then
  say "PASS RED-004 Seed 에 없는 행 1건을 넣자 02_Seed_Tests 가 실패를 잡았다 (exit=$RC)"
else say "FAIL RED-004 Seed 가 오염됐는데 통과했다 (exit=$RC · FAIL줄 $N 건)"; FAILED=1; fi

# ── 정리. 대상 DB 는 한 번도 건드리지 않았다.
m "IF DB_ID('$RED') IS NOT NULL BEGIN ALTER DATABASE [$RED] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$RED]; END"
LEFT=$(sqlcmd -S "$SRV" -E -d master -b -I -h-1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = N'$RED';" 2>/dev/null | tr -d ' \r' | head -1)
if [ "${LEFT:-1}" = "0" ]; then say "PASS RED-CLEAN 폐기용 DB 잔여 0건"
else say "FAIL RED-CLEAN 폐기용 DB [$RED] 가 남았다 — 다음 회차의 RBD-009 를 깨뜨린다"; FAILED=1; fi

# 대상 DB 가 살아 있는지 확인한다. 이 스크립트가 그것을 건드렸다면 여기서 드러난다.
ALIVE=$(sqlcmd -S "$SRV" -E -d master -b -I -h-1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = N'$DB';" 2>/dev/null | tr -d ' \r' | head -1)
if [ "${ALIVE:-0}" = "1" ]; then say "PASS RED-TARGET 대상 DB 무사"
else say "FAIL RED-TARGET 대상 DB 가 사라졌다"; FAILED=1; fi

say "red-probe FAILED=$FAILED"
exit $FAILED
