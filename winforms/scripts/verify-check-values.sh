#!/usr/bin/env bash
# `Common/Db*.cs` 의 상수 ↔ 배포된 CHECK 제약의 허용값 대조.
#
# 어떤 제약을 어느 파일과 대조할지는 **환경변수로 받는다** — 같은 검사가 둘 이상이기
# 때문이다 (2026-09-11). 스크립트를 복사하면 한쪽만 고쳐지는 날이 온다.
#
#   CONSTRAINT  제약 이름          기본 CK_휴무일_TYPE
#   CODE        대조할 C# 파일      기본 Common/DbHolidayType.cs
#   LABEL       메시지에 적을 이름   기본 휴무구분
#
# `04` 가 그 컬럼의 허용값을 못박고 배포 스크립트의 CHECK 제약이 그것을 실제로 강제한다.
# 코드에 옮겨 적은 이상 사본이 둘이다 (ROOT AGENTS.md §6).
#
# [X] **오타는 조용하다.** 컴파일도 되고 fake 를 쓰는 단위시험도 같은 상수를 쓰므로 함께
#     틀린다. 실행하면 03 §24.5 의 편집 잠금이 뒤집힌다 — 모든 행이 편집 불가가 되거나
#     법정공휴일이 편집 가능으로 열린다.
#
# 05 가 아니라 배포 SQL 을 보는 이유: 05 는 허용값을 한자리에 나열하지 않는다. 실제로
# 강제하는 곳이 그 CHECK 제약 한 줄이고, `verify-db-frozen.sh` 가 그 파일을 얼려 둔다.
#
# CHK-001  허용값 집합이 양방향 차집합 0
set -uo pipefail
cd "$(dirname "$0")/.."

: "${SCHEMA:=../database/deploy/01_Schema.sql}"
: "${CODE:=src/HealthCheckupReservationReception.WinForms/Common/DbHolidayType.cs}"
: "${CONSTRAINT:=CK_휴무일_TYPE}"
: "${LABEL:=휴무구분}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# CONSTRAINT [<이름>] ... CHECK ([컬럼] IN (N'a', N'b', N'c')) 의 a·b·c.
schemavalues() {
  tr -d '\r' < "$1" \
    | grep -F "$CONSTRAINT" \
    | head -1 \
    | grep -oE "N'[^']+'" \
    | sed "s|^N'||; s|'$||" \
    | sort -u
}
# public const string X = "Y"; 의 Y.
codevalues() {
  tr -d '\r' < "$1" \
    | sed -n 's|^[[:space:]]*public const string [A-Za-z][A-Za-z0-9]* = "\([^"]*\)";[[:space:]]*$|\1|p' \
    | sort -u
}

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  run() { # run <라벨> <기대 exit> <schema 본문> <code 본문>
    printf '%s\n' "$3" > "$D/schema.sql"
    printf '%s\n' "$4" > "$D/code.cs"
    SCHEMA="$D/schema.sql" CODE="$D/code.cs" CONSTRAINT=CK_휴무일_TYPE LABEL=휴무구분 "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS CHK-SELFTEST $1"
    else echo "FAIL CHK-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  SOK="    CONSTRAINT [CK_휴무일_TYPE] CHECK ([휴무구분] IN (N'법정공휴일', N'자체휴무일')),"
  COK='        public const string Statutory = "법정공휴일";
        public const string Own = "자체휴무일";'
  run '같으면 통과한다'                0 "$SOK" "$COK"
  run '코드에 없는 값을 잡는다'         1 "$SOK" '        public const string Own = "자체휴무일";'
  run '코드에만 있는 값을 잡는다'       1 "$SOK" "$COK
        public const string Extra = \"대체공휴일\";"
  run '오타를 잡는다'                  1 "$SOK" '        public const string Statutory = "법정공유일";
        public const string Own = "자체휴무일";'
  # [X] 한쪽이 비면 차집합이 0 이 되어 "일치" 로 보인다 — 조용히 통과하는 게이트다.
  run '제약을 못 읽으면 FAIL 이다'      1 '    CONSTRAINT [CK_다른것] CHECK (1=1),' "$COK"
  run '코드를 못 읽으면 FAIL 이다'      1 "$SOK" '        // 아무것도 없다'
  # 다른 테이블의 CHECK 를 섞어 세지 않는다.
  run '다른 제약을 섞지 않는다'         0 "    CONSTRAINT [CK_남_TYPE] CHECK ([X] IN (N'남의값')),
$SOK" "$COK"
  rm -f "$D/schema.sql" "$D/code.cs" "$D/out.txt"
  rmdir "$D"
  exit $RC
fi

for f in "$SCHEMA" "$CODE"; do
  [ -f "$f" ] || { say FAIL "CHK-000 파일이 없다: $f"; echo "== FAIL =="; exit 1; }
done

T=$(mktemp -d)
trap 'rm -f "$T"/schema.txt "$T"/code.txt; rmdir "$T"' EXIT

schemavalues "$SCHEMA" > "$T/schema.txt"
codevalues "$CODE"     > "$T/code.txt"

if [ ! -s "$T/schema.txt" ] || [ ! -s "$T/code.txt" ]; then
  say FAIL "CHK-000 $LABEL 을(를) 못 읽었다 (제약 $(wc -l < "$T/schema.txt") · 코드 $(wc -l < "$T/code.txt")) — 형식이 바뀌었다"
  echo "== FAIL =="; exit 1
fi

ONLY_DB=$(comm -23 "$T/schema.txt" "$T/code.txt")
ONLY_CODE=$(comm -13 "$T/schema.txt" "$T/code.txt")
if [ -z "$ONLY_DB" ] && [ -z "$ONLY_CODE" ]; then
  say PASS "CHK-001 $LABEL 양방향 차집합 0 ($(wc -l < "$T/schema.txt") 건)"
else
  say FAIL "CHK-001 $LABEL 불일치"
  [ -n "$ONLY_DB" ]   && echo "$ONLY_DB"   | sed "s|^|    $CONSTRAINT 에만: |"
  [ -n "$ONLY_CODE" ] && echo "$ONLY_CODE" | sed 's/^/    코드에만: /'
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS $LABEL ↔ $CONSTRAINT =="; else echo "== FAIL =="; fi
exit $FAIL
