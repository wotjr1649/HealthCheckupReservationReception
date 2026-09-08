#!/usr/bin/env bash
# Clean Rebuild 계약 중 **회차 사이 비교**가 필요한 것 (스펙 §40).
#   RBD-002  대상 DB 컨텍스트에서 Rebuild.sql -> THROW 50021 로 중단
#   RBD-003  빈 DB 에서 Deploy.sql 전체 실행 -> exit 0
#   RBD-005  연속 2회 Rebuild 후 정렬 덤프 diff -> 차이 0줄
#   RBD-007  Deploy.sql 단독 재실행(DB 유지) -> exit 0, 덤프 동일
#   RBD-008  Procedure 파일만 단독 재실행 -> exit 0, 덤프 동일
# 한 회차 안에서 판정 가능한 RBD-004·006·010 은 tests/14_Clean_Rebuild_Verify.sql 이 한다.
# RBD-001(잘못된 서버명)은 인스턴스가 1개뿐이라 음성 시험이 불가능하다 — NOT RUN 으로 남긴다.
#
# set -e 를 쓰지 않는다 — 실패한 줄에서 셸이 끝나 RC 수집도 로그 출력도 안 된다 (CLAUDE.md §6).
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'; DB='HealthCheckupReservationReceptionDb'
mkdir -p artifacts/logs artifacts/reports
OUT=artifacts/reports/clean-rebuild.txt
: > "$OUT"
FAILED=0

dump() {                      # dump <출력파일>
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -h-1 -W -s"|" \
         -i tests/14_Clean_Rebuild_Verify.sql -o "artifacts/logs/_dump.log"
  local rc=$?
  iconv -f UTF-16 -t UTF-8 "artifacts/logs/_dump.log" > "$1"
  return $rc
}

say() { echo "$1" | tee -a "$OUT"; }

# 타 DB 메타데이터 스냅샷. RBD-009 가 rebuild **전후**로 한 번씩 찍어 비교한다.
#   [X] name 을 문자열 연결하면 sysname 의 catalog collation 과 리터럴이 충돌해 Msg 451 이다(실측).
#       컬럼을 그대로 나열하고 -s"|" 가 구분자를 붙이게 한다.
otherdb() {                   # otherdb <출력파일>
  sqlcmd -S "$SRV" -E -d master -b -I -h-1 -W -s"|" \
    -Q "SET NOCOUNT ON; SELECT name, state_desc, user_access_desc, CONVERT(VARCHAR(1), CONVERT(INT, is_read_only)), CONVERT(VARCHAR(30), create_date, 126) FROM sys.databases WHERE name NOT IN (N'$DB', N'tempdb') ORDER BY database_id;" \
    -o "$1"
}

# [X] 이 스냅샷을 **회차 시작 시점에** 찍는다. 예전에는 2026-09-04 에 T04 가 남긴 파일 하나를
#     4일째 기준으로 삼았고, 그 사이 인스턴스에 다른 DB 가 생기자 우리 rebuild 와 무관하게
#     영구히 FAIL 했다(실측 2026-09-08 — 다른 계열이 만든 DB 2개). RBD-009 가 묻는 것은
#     "이 rebuild 가 타 DB 를 건드렸는가" 이므로 비교 구간도 이 회차여야 한다.
otherdb artifacts/reports/otherdb_before.txt

# ── RBD-002  Rebuild.sql 은 master 컨텍스트를 강제한다. 대상 DB 에서 돌리면 50021 이다.
sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -i Rebuild.sql -o artifacts/logs/rbd002.log
RC=$?
if iconv -f UTF-16 -t UTF-8 artifacts/logs/rbd002.log 2>/dev/null | grep -q '50021'; then
  say "PASS RBD-002 대상 DB 컨텍스트의 Rebuild 가 Msg 50021 로 중단됐다 (exit=$RC)"
else
  say "FAIL RBD-002 50021 이 관측되지 않았다 (exit=$RC)"; FAILED=1
fi
say 'NOT RUN RBD-001 잘못된 서버명(50020) - 인스턴스가 1개뿐이라 음성 시험 불가'

# ── RBD-003  빈 DB 에서 Deploy 전체
./scripts/rebuild.sh > artifacts/logs/rbd003.log 2>&1
RC=$?
if [ "$RC" -eq 0 ]; then say 'PASS RBD-003 빈 DB 에서 Deploy 전체 실행 exit 0'
else say "FAIL RBD-003 exit=$RC"; FAILED=1; fi
dump artifacts/reports/inventory_run1.txt || { say 'FAIL RBD-003 1회차 덤프 실패'; FAILED=1; }

# ── RBD-005  연속 2회 Rebuild 후 덤프 동일
./scripts/rebuild.sh > artifacts/logs/rbd005.log 2>&1
RC=$?
[ "$RC" -ne 0 ] && { say "FAIL RBD-005 2회차 rebuild exit=$RC"; FAILED=1; }
dump artifacts/reports/inventory_run2.txt || { say 'FAIL RBD-005 2회차 덤프 실패'; FAILED=1; }
if diff -q artifacts/reports/inventory_run1.txt artifacts/reports/inventory_run2.txt > /dev/null; then
  say 'PASS RBD-005 연속 2회 Rebuild 의 정렬 덤프가 완전히 동일'
else
  say 'FAIL RBD-005 덤프 불일치'; diff -u artifacts/reports/inventory_run1.txt artifacts/reports/inventory_run2.txt >> "$OUT"; FAILED=1
fi

# ── RBD-007  Deploy 단독 재실행 (DB 유지). 변경이력 보존 경로이기도 하다 (CLAUDE.md §8).
./scripts/deploy.sh > artifacts/logs/rbd007.log 2>&1
RC=$?
dump artifacts/reports/inventory_run3.txt || { say 'FAIL RBD-007 덤프 실패'; FAILED=1; }
if [ "$RC" -eq 0 ] && diff -q artifacts/reports/inventory_run2.txt artifacts/reports/inventory_run3.txt > /dev/null; then
  say 'PASS RBD-007 Deploy 단독 재실행 exit 0 + 덤프 동일'
else
  say "FAIL RBD-007 exit=$RC 또는 덤프 불일치"; FAILED=1
fi

# ── RBD-008  Procedure 계열 파일(03~07)만 단독 재실행.
#    CREATE OR ALTER 는 기존 권한을 유지하고 DROP+CREATE 는 지운다. 배포가 어느 쪽인지
#    문서가 아니라 덤프의 GRANT 구획으로 확인한다. Security 가 범위 밖이라 현재 기대는 0건이고,
#    재실행 뒤에도 0건이면 "권한이 사라지지 않았다" 와 "생기지 않았다" 를 함께 본다.
RC=0
for f in deploy/03_Functions.sql deploy/04_Procedures_Select.sql \
         deploy/05_Procedures_Patient_Write.sql deploy/06_Procedures_Reservation_Write.sql \
         deploy/07_Procedures_Reception_Write.sql; do
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -i "$f" -o "artifacts/logs/rbd008_$(basename "$f" .sql).log" || RC=1
done
dump artifacts/reports/inventory_run4.txt || { say 'FAIL RBD-008 덤프 실패'; FAILED=1; }
if [ "$RC" -eq 0 ] && diff -q artifacts/reports/inventory_run3.txt artifacts/reports/inventory_run4.txt > /dev/null; then
  say 'PASS RBD-008 Procedure 파일 단독 재실행 exit 0 + 덤프 동일 (GRANT 구획 포함)'
else
  say "FAIL RBD-008 exit=$RC 또는 덤프 불일치"; FAILED=1
fi

# ── RBD-009  타 DB 무사. 이 회차 시작 시점에 찍은 스냅샷과 같은 명령·같은 컬럼으로 다시 찍어 diff 한다.
#    이름 존재 확인만으로는 증거가 되지 않는다. state_desc·user_access_desc·is_read_only·create_date 를 본다.
otherdb artifacts/reports/otherdb_after.txt
if diff -q artifacts/reports/otherdb_before.txt artifacts/reports/otherdb_after.txt > /dev/null; then
  say 'PASS RBD-009 타 DB 메타데이터가 rebuild 전후로 동일'
else
  say 'FAIL RBD-009 타 DB 변경 감지'; diff -u artifacts/reports/otherdb_before.txt artifacts/reports/otherdb_after.txt >> "$OUT"; FAILED=1
fi


# ── RBD-011  재배포가 IDENTITY 시드를 감사기록 뒤로 이어받는가 (06 §9.2 · §43-16)
#    [X] 공허하지 않게 만든다. 변경이력이 비어 있으면 "MAX <= IDENT_CURRENT" 는 언제나 참이라
#        재시드를 통째로 지워도 통과한다. **탐침 감사행을 일부러 심고** 시드가 그 뒤로 가는지 본다.
PK=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON;
  DELETE FROM dbo.변경이력 WHERE [조작자명] = N'RBD-011-PROBE';
  DECLARE @K BIGINT = ISNULL((SELECT MAX([대상키]) FROM dbo.변경이력 WHERE [대상테이블]=N'수검자'),0) + 1000;
  INSERT INTO dbo.변경이력 ([기록일시],[조작자명],[대상테이블],[대상키],[컬럼명],[변경전],[변경후])
  VALUES (SYSDATETIME(), N'RBD-011-PROBE', N'수검자', @K, N'성명', N'전세대', N'전세대');
  SELECT CONVERT(VARCHAR(20), @K);" 2>/dev/null | tr -d ' \r' | head -1)
./scripts/deploy.sh > artifacts/logs/rbd011.log 2>&1
RC=$?
SEED=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q \
  "SET NOCOUNT ON; SELECT CONVERT(VARCHAR(20), CONVERT(BIGINT, IDENT_CURRENT('dbo.수검자')));" 2>/dev/null | tr -d ' \r' | head -1)
sqlcmd -S "$SRV" -E -d "$DB" -b -I -Q "DELETE FROM dbo.변경이력 WHERE [조작자명] = N'RBD-011-PROBE';" > /dev/null 2>&1
LEFT=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q \
  "SET NOCOUNT ON; SELECT CONVERT(VARCHAR(5), COUNT(*)) FROM dbo.변경이력 WHERE [조작자명] = N'RBD-011-PROBE';" 2>/dev/null | tr -d ' \r' | head -1)
if [ "$RC" -eq 0 ] && [ -n "$PK" ] && [ "${SEED:-0}" -ge "${PK:-1}" ] && [ "${LEFT:-1}" = "0" ]; then
  say "PASS RBD-011 재배포가 IDENTITY 시드를 감사기록 뒤로 이어받았다 (탐침 대상키 $PK → 시드 $SEED · 탐침 정리 0건)"
else
  say "FAIL RBD-011 시드가 안 밀렸다 (deploy=$RC · 탐침 대상키 $PK · 시드 $SEED · 잔여 $LEFT)"; FAILED=1
fi
cp artifacts/reports/inventory_run4.txt artifacts/reports/object-inventory.txt
rm -f artifacts/reports/inventory_run1.txt artifacts/reports/inventory_run2.txt \
      artifacts/reports/inventory_run3.txt artifacts/reports/inventory_run4.txt

say "clean-rebuild FAILED=$FAILED"
exit $FAILED
