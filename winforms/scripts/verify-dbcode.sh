#!/usr/bin/env bash
# Common/DbCode.cs ↔ 05 §16.1 대조.
#
# 05 §16.1 은 C# Enum 정의를 문서 안에 직접 싣는다. 그것을 코드로 옮긴 이상 사본이 둘이고,
# 사본은 원본이 바뀌면 뒤처진다 (ROOT AGENTS.md §6). 이름과 값을 함께 본다.
#
# DBC-001  이름 집합이 양방향 차집합 0
# DBC-002  같은 이름의 값이 전건 일치
# DBC-003  600 을 쓰지 않는다 (05 §4.4 — 재사용 금지)
set -uo pipefail
cd "$(dirname "$0")/.."

: "${DOC:=../docs/baseline/05_DB_Rule_SP_Contract.md}"
: "${CODE:=src/HealthCheckupReservationReception.WinForms/Common/DbCode.cs}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# `이름 = 숫자` 한 줄씩 뽑는다. 주석과 여닫는 줄은 걸리지 않는다.
pairs() { tr -d '\r' < "$1" | sed -n 's|^[[:space:]]*\([A-Za-z][A-Za-z0-9_]*\)[[:space:]]*=[[:space:]]*\([0-9]\+\),\?[[:space:]]*$|\1 \2|p' | sort -u; }

# 문서에서는 §16.1 구획만 본다 — 다른 절의 C# 조각이 섞이지 않게.
docsec() { tr -d '\r' < "$1" | awk '/^## 16[.]1 /{f=1;next} f && /^## /{exit} f'; }

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  run() { # run <라벨> <기대 exit> <doc 구획> <code 본문>
    printf '## 16.1 ResultCode Enum\n%s\n## 16.2 다음\n' "$3" > "$D/doc.md"
    printf '%s\n' "$4" > "$D/code.cs"
    DOC="$D/doc.md" CODE="$D/code.cs" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS DBC-SELFTEST $1"
    else echo "FAIL DBC-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  DOK='    Ok = 0,
    BadValue = 101,
    RowChanged = 601'
  COK='    Ok = 0,
    BadValue = 101,
    RowChanged = 601'
  run '같으면 통과한다'            0 "$DOK" "$COK"
  run '코드에 없는 멤버를 잡는다'   1 "$DOK" '    Ok = 0,
    RowChanged = 601'
  run '코드에만 있는 멤버를 잡는다' 1 "$DOK" "$COK
    Extra = 999,"
  run '값이 다르면 잡는다'         1 "$DOK" '    Ok = 0,
    BadValue = 102,
    RowChanged = 601'
  run '600 재사용을 잡는다'        1 "$DOK
    PatientChanged = 600," "$COK
    PatientChanged = 600,"
  run '구획을 못 읽으면 FAIL 이다'  1 '' "$COK"
  rm -f "$D/doc.md" "$D/code.cs" "$D/out.txt"
  rmdir "$D"
  exit $RC
fi

for f in "$DOC" "$CODE"; do
  [ -f "$f" ] || { say FAIL "DBC-000 파일이 없다: $f"; echo "== FAIL =="; exit 1; }
done

T=$(mktemp -d)
trap 'rm -f "$T"/doc.txt "$T"/doc.pairs "$T"/code.pairs "$T"/doc.names "$T"/code.names; rmdir "$T"' EXIT

docsec "$DOC" > "$T/doc.txt"
pairs "$T/doc.txt" > "$T/doc.pairs"
pairs "$CODE"      > "$T/code.pairs"

# [X] 한쪽이 비면 차집합이 0 이 되어 "일치" 로 보인다 — 조용히 통과하는 게이트다.
if [ ! -s "$T/doc.pairs" ] || [ ! -s "$T/code.pairs" ]; then
  say FAIL "DBC-000 Enum 을 못 읽었다 (05 §16.1 $(wc -l < "$T/doc.pairs") · 코드 $(wc -l < "$T/code.pairs")) — 형식이 바뀌었다"
  echo "== FAIL =="; exit 1
fi

cut -d' ' -f1 "$T/doc.pairs"  | sort -u > "$T/doc.names"
cut -d' ' -f1 "$T/code.pairs" | sort -u > "$T/code.names"
ONLY_DOC=$(comm -23 "$T/doc.names" "$T/code.names")
ONLY_CODE=$(comm -13 "$T/doc.names" "$T/code.names")
if [ -z "$ONLY_DOC" ] && [ -z "$ONLY_CODE" ]; then
  say PASS "DBC-001 Enum 멤버 이름 양방향 차집합 0 ($(wc -l < "$T/doc.names") 건)"
else
  say FAIL "DBC-001 Enum 멤버 이름 불일치"
  [ -n "$ONLY_DOC" ]  && echo "$ONLY_DOC"  | sed 's/^/    05 §16.1 에만: /'
  [ -n "$ONLY_CODE" ] && echo "$ONLY_CODE" | sed 's/^/    코드에만: /'
fi

if [ -z "$(comm -3 "$T/doc.pairs" "$T/code.pairs")" ]; then
  say PASS "DBC-002 Enum 값 전건 일치"
else
  say FAIL "DBC-002 Enum 값 불일치"
  comm -3 "$T/doc.pairs" "$T/code.pairs" | sed 's/^/    /'
fi

if grep -qE '(^| )600$' "$T/doc.pairs" "$T/code.pairs"; then
  say FAIL "DBC-003 600 이 다시 쓰였다 — 05 §4.4 가 재사용을 금지한다"
else
  say PASS "DBC-003 600 을 쓰지 않는다 (05 §4.4)"
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS DbCode ↔ 05 §16.1 =="; else echo "== FAIL =="; fi
exit $FAIL
