#!/usr/bin/env bash
# Common/DbWorkAction.cs ↔ 05 §8.2 고정 업무동작코드 대조.
#
# 05 §8.2 RS4 는 `업무동작코드` 다섯을 고정으로 돌려주고, 화면은 그 문자열로 Ribbon 버튼을
# 찾는다. 코드에 옮겨 적은 이상 사본이 둘이다 (ROOT AGENTS.md §6) — DbCode 와 같은 처지다.
#
# [X] **오타는 조용하다.** 컴파일도 되고, 단위시험은 같은 상수를 쓰므로 함께 틀린다.
#     실행하면 그 버튼 하나가 영영 닫힌 채로 있고 아무도 이유를 모른다.
#
# WKA-001  코드 문자열 집합이 양방향 차집합 0
set -uo pipefail
cd "$(dirname "$0")/.."

: "${DOC:=../docs/baseline/05_DB_Rule_SP_Contract.md}"
: "${CODE:=src/HealthCheckupReservationReception.WinForms/Common/DbWorkAction.cs}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# §8.2 구획만 본다 — 다른 절의 대문자 토큰이 섞이지 않게.
# 그 안에서 **한 줄을 통째로 차지한** 대문자_밑줄 토큰만 센다: `USP_HC_...` 는 산문 안에 있고
# 뒤에 한글이 붙으므로 걸리지 않는다.
doccodes() {
  tr -d '\r' < "$1" \
    | awk '/^## 8[.]2 /{f=1;next} f && /^## /{exit} f' \
    | sed -n 's|^[[:space:]]*\([A-Z][A-Z0-9]*_[A-Z0-9_]*\)[[:space:]]*$|\1|p' \
    | sort -u
}
# public const string X = "Y"; 의 Y.
codecodes() {
  tr -d '\r' < "$1" \
    | sed -n 's|^[[:space:]]*public const string [A-Za-z][A-Za-z0-9]* = "\([A-Z0-9_]*\)";[[:space:]]*$|\1|p' \
    | sort -u
}

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  run() { # run <라벨> <기대 exit> <doc 구획> <code 본문>
    printf '## 8.2 `[dbo].[USP_HC_예약접수상세_조회]`\n%s\n## 8.3 다음\n' "$3" > "$D/doc.md"
    printf '%s\n' "$4" > "$D/code.cs"
    DOC="$D/doc.md" CODE="$D/code.cs" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS WKA-SELFTEST $1"
    else echo "FAIL WKA-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  DOK='EDIT_RESERVATION
START_RECEPTION'
  COK='        public const string EditReservation = "EDIT_RESERVATION";
        public const string StartReception = "START_RECEPTION";'
  run '같으면 통과한다'              0 "$DOK" "$COK"
  run '코드에 없는 동작을 잡는다'     1 "$DOK" '        public const string EditReservation = "EDIT_RESERVATION";'
  run '코드에만 있는 동작을 잡는다'   1 "$DOK" "$COK
        public const string Extra = \"EDIT_EXTRA\";"
  run '오타를 잡는다'                1 "$DOK" '        public const string EditReservation = "EDIT_RESERVATON";
        public const string StartReception = "START_RECEPTION";'
  # [X] 한쪽이 비면 차집합이 0 이 되어 "일치" 로 보인다 — 조용히 통과하는 게이트다.
  run '구획을 못 읽으면 FAIL 이다'    1 '' "$COK"
  run '코드를 못 읽으면 FAIL 이다'    1 "$DOK" '        // 아무것도 없다'
  # 산문의 USP_HC_... 를 동작코드로 세지 않는다.
  run '절 제목의 SP 이름을 세지 않는다' 0 "$DOK" "$COK"
  rm -f "$D/doc.md" "$D/code.cs" "$D/out.txt"
  rmdir "$D"
  exit $RC
fi

for f in "$DOC" "$CODE"; do
  [ -f "$f" ] || { say FAIL "WKA-000 파일이 없다: $f"; echo "== FAIL =="; exit 1; }
done

T=$(mktemp -d)
trap 'rm -f "$T"/doc.txt "$T"/code.txt; rmdir "$T"' EXIT

doccodes "$DOC"  > "$T/doc.txt"
codecodes "$CODE" > "$T/code.txt"

if [ ! -s "$T/doc.txt" ] || [ ! -s "$T/code.txt" ]; then
  say FAIL "WKA-000 업무동작코드를 못 읽었다 (05 §8.2 $(wc -l < "$T/doc.txt") · 코드 $(wc -l < "$T/code.txt")) — 형식이 바뀌었다"
  echo "== FAIL =="; exit 1
fi

ONLY_DOC=$(comm -23 "$T/doc.txt" "$T/code.txt")
ONLY_CODE=$(comm -13 "$T/doc.txt" "$T/code.txt")
if [ -z "$ONLY_DOC" ] && [ -z "$ONLY_CODE" ]; then
  say PASS "WKA-001 업무동작코드 양방향 차집합 0 ($(wc -l < "$T/doc.txt") 건)"
else
  say FAIL "WKA-001 업무동작코드 불일치"
  [ -n "$ONLY_DOC" ]  && echo "$ONLY_DOC"  | sed 's/^/    05 §8.2 에만: /'
  [ -n "$ONLY_CODE" ] && echo "$ONLY_CODE" | sed 's/^/    코드에만: /'
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS 업무동작코드 ↔ 05 §8.2 =="; else echo "== FAIL =="; fi
exit $FAIL
