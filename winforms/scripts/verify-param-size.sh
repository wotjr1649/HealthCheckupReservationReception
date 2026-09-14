#!/usr/bin/env bash
# Common/DbSize.cs ↔ 05 의 Parameter 크기 대조.
#
# 05 의 Parameter 표가 `@조작자명 NVARCHAR(50)` 을 못박고 C# 이 그것을 길이 검증에 쓴다.
# 코드에 옮겨 적은 이상 사본이 둘이다 (ROOT AGENTS.md §6) — DbCode 와 같은 처지다.
#
# [X] **크기가 틀리면 조용하다.** 컴파일도 되고, fake 를 쓰는 단위시험은 같은 상수를 쓰므로
#     함께 틀린다. 크게 잡으면 화면이 통과시킨 값을 DB 가 자르거나 튕기고, 작게 잡으면
#     계약이 허락한 입력을 화면이 막는다. 어느 쪽이든 사용자는 이유를 못 본다.
#
# PSZ-001  상수 전건이 `// @파라미터` 표시를 갖는다
# PSZ-002  표시한 파라미터 전건이 05 에 있다
# PSZ-003  크기가 전건 일치
# PSZ-004  05 가 한 파라미터에 한 크기만 준다
set -uo pipefail
cd "$(dirname "$0")/.."

: "${DOC:=../docs/baseline/05_DB_Rule_SP_Contract.md}"
: "${CODE:=src/HealthCheckupReservationReception.WinForms/Common/DbSize.cs}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# 05 의 Parameter 표 한 줄: | `@이름` | `NVARCHAR(100)` | ... |
# 백틱으로 칸을 가르면 $2 가 파라미터, $4 가 형식이다.
docsizes() {
  tr -d '\r' < "$1" \
    | awk -F'`' '/^\| `@/ && $4 ~ /^[A-Za-z]+\([0-9]+\)$/ {
        n = $4; sub(/^[A-Za-z]+\(/, "", n); sub(/\)$/, "", n); print $2, n }' \
    | sort -u
}
# public const int X = 100;   // @파라미터
codesizes() {
  tr -d '\r' < "$1" \
    | sed -n 's|^[[:space:]]*public const int [A-Za-z][A-Za-z0-9]* = \([0-9][0-9]*\);[[:space:]]*// \(@[^ ]*\)[[:space:]]*$|\2 \1|p' \
    | sort -u
}
# 표시가 없는 상수. 있으면 그것은 아무도 안 재는 계약값이다.
codeunmarked() {
  tr -d '\r' < "$1" \
    | grep -E '^[[:space:]]*public const int [A-Za-z][A-Za-z0-9]* = [0-9]+;' \
    | grep -vE '// @[^ ]+[[:space:]]*$' || true
}

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  run() { # run <라벨> <기대 exit> <doc 본문> <code 본문>
    printf '%s\n' "$3" > "$D/doc.md"
    printf '    public static class DbSize\n    {\n%s\n    }\n' "$4" > "$D/code.cs"
    DOC="$D/doc.md" CODE="$D/code.cs" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS PSZ-SELFTEST $1"
    else echo "FAIL PSZ-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  DOK='| `@조작자명` | `NVARCHAR(50)` | X |
| `@차트번호` | `NVARCHAR(100)` | O | 포함검색 |'
  COK='        public const int OperatorName = 50;   // @조작자명
        public const int ChartNo = 100;       // @차트번호'
  run '같으면 통과한다'                 0 "$DOK" "$COK"
  run '크기가 다르면 잡는다'            1 "$DOK" '        public const int OperatorName = 30;   // @조작자명
        public const int ChartNo = 100;       // @차트번호'
  run '05 에 없는 파라미터를 잡는다'     1 "$DOK" "$COK
        public const int Ghost = 7;           // @없는것"
  run '표시 없는 상수를 잡는다'          1 "$DOK" "$COK
        public const int Bare = 9;"
  # [X] 한쪽이 비면 비교할 것이 없어 "일치" 로 보인다 — 조용히 통과하는 게이트다.
  run '코드를 못 읽으면 FAIL 이다'       1 "$DOK" '        // 아무것도 없다'
  run '05 를 못 읽으면 FAIL 이다'        1 '| 표가 없다 |' "$COK"
  run '05 가 자기 자신과 어긋나면 잡는다' 1 "$DOK
| \`@조작자명\` | \`NVARCHAR(80)\` | X |" "$COK"
  rm -f "$D/doc.md" "$D/code.cs" "$D/out.txt"
  rmdir "$D"
  exit $RC
fi

for f in "$DOC" "$CODE"; do
  [ -f "$f" ] || { say FAIL "PSZ-000 파일이 없다: $f"; echo "== FAIL =="; exit 1; }
done

T=$(mktemp -d)
trap 'rm -f "$T"/doc.txt "$T"/code.txt; rmdir "$T"' EXIT

docsizes "$DOC"   > "$T/doc.txt"
codesizes "$CODE" > "$T/code.txt"

if [ ! -s "$T/doc.txt" ] || [ ! -s "$T/code.txt" ]; then
  say FAIL "PSZ-000 크기를 못 읽었다 (05 $(wc -l < "$T/doc.txt") · 코드 $(wc -l < "$T/code.txt")) — 형식이 바뀌었다"
  echo "== FAIL =="; exit 1
fi

UNMARKED=$(codeunmarked "$CODE")
if [ -z "$UNMARKED" ]; then
  say PASS "PSZ-001 상수 전건이 파라미터 표시를 갖는다 ($(wc -l < "$T/code.txt") 건)"
else
  say FAIL "PSZ-001 파라미터 표시가 없는 상수 — 아무도 재지 않는 계약값이다"
  echo "$UNMARKED" | sed 's/^/    /'
fi

DUP=$(cut -d' ' -f1 "$T/doc.txt" | uniq -d)
if [ -z "$DUP" ]; then
  say PASS "PSZ-004 05 가 한 파라미터에 한 크기만 준다"
else
  say FAIL "PSZ-004 05 가 자기 자신과 어긋난다 — 같은 파라미터에 크기가 둘이다"
  for p in $DUP; do grep "^$p " "$T/doc.txt" | sed 's/^/    /'; done
fi

MISSING=""; WRONG=""
while read -r param size; do
  doc=$(grep "^$param " "$T/doc.txt" | head -1 | cut -d' ' -f2)
  if [ -z "$doc" ]; then MISSING="$MISSING$param
"
  elif [ "$doc" != "$size" ]; then WRONG="$WRONG$param 코드 $size · 05 $doc
"
  fi
done < "$T/code.txt"

if [ -z "$MISSING" ]; then
  say PASS "PSZ-002 표시한 파라미터 전건이 05 에 있다"
else
  say FAIL "PSZ-002 05 에 없는 파라미터"
  printf '%s' "$MISSING" | sed 's/^/    /'
fi

if [ -z "$WRONG" ]; then
  say PASS "PSZ-003 크기 전건 일치"
else
  say FAIL "PSZ-003 크기 불일치"
  printf '%s' "$WRONG" | sed 's/^/    /'
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS Parameter 크기 ↔ 05 =="; else echo "== FAIL =="; fi
exit $FAIL
