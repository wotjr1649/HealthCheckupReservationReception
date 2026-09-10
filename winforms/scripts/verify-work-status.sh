#!/usr/bin/env bash
# Common/DbWorkStatus.cs ↔ 05 §2.2 허용 코드의 `상태코드` 대조.
#
# 05 §2.2 가 `상태코드 : RSV / RCP / CNR / CNC` 한 줄로 넷을 못박는다. 코드에 옮겨 적은 이상
# 사본이 둘이다 (ROOT AGENTS.md §6) — DbWorkAction · DbCode 와 같은 처지다.
#
# [X] **오타는 조용하다.** 컴파일도 되고 단위시험은 같은 상수를 쓰므로 함께 틀린다. 수검자
#     목록의 `예약 가능/불가` 가 이 값으로 유효업무를 가르므로, 틀리면 **잘못된 판정**이
#     화면에 뜨고 아무 소리도 나지 않는다.
#
# WKS-001  코드 문자열 집합이 양방향 차집합 0
set -uo pipefail
cd "$(dirname "$0")/.."

: "${DOC:=../docs/baseline/05_DB_Rule_SP_Contract.md}"
: "${CODE:=src/HealthCheckupReservationReception.WinForms/Common/DbWorkStatus.cs}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# `상태코드 ... : A / B / C` 한 줄만 본다. 다른 절에도 같은 낱말이 있으므로 **콜론 뒤가
# 대문자 코드의 슬래시 나열인 줄**로 좁힌다.
doccodes() {
  tr -d '\r' < "$1" \
    | sed -n 's|^상태코드[[:space:]]*:[[:space:]]*\([A-Z /]*\)$|\1|p' \
    | head -1 \
    | tr '/' '\n' \
    | sed 's|[[:space:]]||g' \
    | sed '/^$/d' \
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
  run() { # run <라벨> <기대 exit> <doc 본문> <code 본문>
    printf '%s\n' "$3" > "$D/doc.md"
    printf '%s\n' "$4" > "$D/code.cs"
    DOC="$D/doc.md" CODE="$D/code.cs" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS WKS-SELFTEST $1"
    else echo "FAIL WKS-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  DOK='예약구분 : NORMAL / WALKIN
상태코드          : RSV / RCP'
  COK='        public const string Reserved = "RSV";
        public const string Received = "RCP";'
  run '같으면 통과한다'                0 "$DOK" "$COK"
  run '코드에 없는 상태를 잡는다'       1 "$DOK" '        public const string Reserved = "RSV";'
  run '코드에만 있는 상태를 잡는다'     1 "$DOK" "$COK
        public const string Extra = \"CNR\";"
  run '오타를 잡는다'                  1 "$DOK" '        public const string Reserved = "RVS";
        public const string Received = "RCP";'
  # [X] 한쪽이 비면 차집합이 0 이 되어 "일치" 로 보인다 — 조용히 통과하는 게이트다.
  run '줄을 못 읽으면 FAIL 이다'        1 '예약구분 : NORMAL / WALKIN' "$COK"
  run '코드를 못 읽으면 FAIL 이다'      1 "$DOK" '        // 아무것도 없다'
  # 예약구분 줄을 상태코드로 세지 않는다 — 위 DOK 이 이미 그 줄을 함께 담고 있다.
  rm -f "$D/doc.md" "$D/code.cs" "$D/out.txt"
  rmdir "$D"
  exit $RC
fi

for f in "$DOC" "$CODE"; do
  [ -f "$f" ] || { say FAIL "WKS-000 파일이 없다: $f"; echo "== FAIL =="; exit 1; }
done

T=$(mktemp -d)
trap 'rm -f "$T"/doc.txt "$T"/code.txt; rmdir "$T"' EXIT

doccodes "$DOC"   > "$T/doc.txt"
codecodes "$CODE" > "$T/code.txt"

if [ ! -s "$T/doc.txt" ] || [ ! -s "$T/code.txt" ]; then
  say FAIL "WKS-000 상태코드를 못 읽었다 (05 §2.2 $(wc -l < "$T/doc.txt") · 코드 $(wc -l < "$T/code.txt")) — 형식이 바뀌었다"
  echo "== FAIL =="; exit 1
fi

ONLY_DOC=$(comm -23 "$T/doc.txt" "$T/code.txt")
ONLY_CODE=$(comm -13 "$T/doc.txt" "$T/code.txt")
if [ -z "$ONLY_DOC" ] && [ -z "$ONLY_CODE" ]; then
  say PASS "WKS-001 상태코드 양방향 차집합 0 ($(wc -l < "$T/doc.txt") 건)"
else
  say FAIL "WKS-001 상태코드 불일치"
  [ -n "$ONLY_DOC" ]  && echo "$ONLY_DOC"  | sed 's/^/    05 §2.2 에만: /'
  [ -n "$ONLY_CODE" ] && echo "$ONLY_CODE" | sed 's/^/    코드에만: /'
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS 상태코드 ↔ 05 §2.2 =="; else echo "== FAIL =="; fi
exit $FAIL
