#!/usr/bin/env bash
# Common/DbCodes.cs 의 `DbWorkAction` ↔ 05 §8.2 고정 업무동작코드 대조.
#
# 05 §8.2 RS4 는 `업무동작코드` 다섯을 고정으로 돌려주고, 화면은 그 문자열로 Ribbon 버튼을
# 찾는다. 코드에 옮겨 적은 이상 사본이 둘이다 (ROOT AGENTS.md §6) — DbCode 와 같은 처지다.
#
# [X] **오타는 조용하다.** 컴파일도 되고, 단위시험은 같은 상수를 쓰므로 함께 틀린다.
#     실행하면 그 버튼 하나가 영영 닫힌 채로 있고 아무도 이유를 모른다.
#
# WKA-001  업무동작코드   DbCodes.cs `DbWorkAction`    <-> 05 §8.2
# WKA-002  휴무동작코드   DbCodes.cs `DbHolidayAction` <-> 05 §12.6   [R22]
set -uo pipefail
cd "$(dirname "$0")/.."

: "${DOC:=../docs/baseline/05_DB_Rule_SP_Contract.md}"
: "${CODE:=src/HealthCheckupReservationReception.WinForms/Common/DbCodes.cs}"
# 한 파일에 상수 class 가 여럿이다 — 어느 것을 볼지 말한다 (2026-09-14).
: "${CLASS:=DbWorkAction}"
: "${DOC2:=../docs/baseline/05_DB_Rule_SP_Contract.md}"
: "${CODE2:=src/HealthCheckupReservationReception.WinForms/Common/DbCodes.cs}"
: "${CLASS2:=DbHolidayAction}"
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
# [R22] §12.6 입력표의 `@휴무동작코드` 행. 그 줄의 백틱 안 대문자 토큰만 센다 —
#       `VARCHAR(30)` 은 괄호가 있어 걸리지 않고, 한글 칸은 애초에 대문자가 아니다.
holcodes() {
  tr -d '\r' < "$1" \
    | awk '/^## 12[.]6 /{f=1;next} f && /^## /{exit} f' \
    | grep '@휴무동작코드' \
    | grep -oE '`[A-Z][A-Z0-9_]*`' \
    | tr -d '`' \
    | sort -u
}

# `public static class <이름>` 줄부터 그 class 의 닫는 `}` 까지만 남긴다. 빈 값이면 파일 전체다.
#
# [X] **class 를 못 찾으면 출력이 빈다.** 그때는 아래 WKA-000·WKA-002 가 FAIL 을 낸다 —
#     한쪽이 비면 차집합이 0 이 되어 조용히 통과하는 그 함정을 그것이 이미 막고 있다.
scope() {  # scope <class 이름>
  if [ -z "${1:-}" ]; then cat; return 0; fi
  awk -v c="$1" '$0 ~ ("class[[:space:]]+" c "[[:space:]]*$") { f=1; next } f && /^[[:space:]]*}[[:space:]]*$/ { exit } f'
}
# public const string X = "Y"; 의 Y. 지정한 class 안에서만 센다.
codecodes() {  # codecodes <파일> <class 이름>
  tr -d '\r' < "$1" \
    | scope "$2" \
    | sed -n 's|^[[:space:]]*public const string [A-Za-z][A-Za-z0-9]* = "\([A-Z0-9_]*\)";[[:space:]]*$|\1|p' \
    | sort -u
}

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  # runfile 은 code.cs 를 통째로 받는다. run 은 본문을 대상 class 로 감싸 준다 —
  # 게이트가 class 단위로 좁혀 읽으므로 감싸지 않으면 아무것도 못 읽는다.
  runfile() { # runfile <라벨> <기대 exit> <doc 구획> <code 파일 전체>
    printf '## 8.2 `[dbo].[USP_HC_예약접수상세_조회]`\n%s\n## 8.3 다음\n' "$3" > "$D/doc.md"
    printf '%s\n' "$4" > "$D/code.cs"
    DOC="$D/doc.md" CODE="$D/code.cs" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS WKA-SELFTEST $1"
    else echo "FAIL WKA-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  run() { # run <라벨> <기대 exit> <doc 구획> <class 본문>
    runfile "$1" "$2" "$3" "    public static class DbWorkAction
    {
$4
    }"
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
  # ── [R22] WKA-002. 실물 DOC/CODE 를 그대로 두고 DOC2/CODE2 만 갈아 끼운다 —
  #         그래야 여기서 나는 red 가 WKA-002 의 것임이 분명하다.
  runfile2() { # runfile2 <라벨> <기대 exit> <doc 구획> <code 파일 전체>
    printf '## 12.6 `SP`
%s
## 12.7 다음
' "$3" > "$D/doc2.md"
    printf "%s\n" "$4" > "$D/code2.cs"
    DOC2="$D/doc2.md" CODE2="$D/code2.cs" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS WKA-SELFTEST $1"
    else echo "FAIL WKA-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  run2() { # run2 <라벨> <기대 exit> <doc 구획> <class 본문>
    runfile2 "$1" "$2" "$3" "    public static class DbHolidayAction
    {
$4
    }"
  }
  HDOK='| `@휴무동작코드` | `VARCHAR(30)` | X | `CREATE_HOLIDAY` / `UPDATE_HOLIDAY` |'
  HCOK='        public const string Create = "CREATE_HOLIDAY";
        public const string Update = "UPDATE_HOLIDAY";'
  run2 '휴무동작코드가 같으면 통과한다'   0 "$HDOK" "$HCOK"
  run2 '휴무동작코드 오타를 잡는다'       1 "$HDOK" '        public const string Create = "CREAT";
        public const string Update = "UPDATE_HOLIDAY";'
  run2 '휴무 구획을 못 읽으면 FAIL 이다'  1 '| 없음 |' "$HCOK"
  # ── 2026-09-14 Common/DbCodes.cs 로 합치면서. 한 파일의 다른 class 를 섞어 세면 안 된다.
  runfile2 '휴무: 같은 파일의 다른 class 를 세지 않는다' 0 "$HDOK" '    public static class DbHolidayAction
    {
        public const string Create = "CREATE_HOLIDAY";
        public const string Update = "UPDATE_HOLIDAY";
    }

    public static class DbWorkAction
    {
        public const string EditReservation = "EDIT_RESERVATION";
    }'
  runfile2 '휴무: class 를 못 찾으면 FAIL 이다' 1 "$HDOK" '    public static class 딴것
    {
        public const string Create = "CREATE_HOLIDAY";
        public const string Update = "UPDATE_HOLIDAY";
    }'
  rm -f "$D/doc2.md" "$D/code2.cs"

  # 산문의 USP_HC_... 를 동작코드로 세지 않는다.
  run '절 제목의 SP 이름을 세지 않는다' 0 "$DOK" "$COK"
  runfile '같은 파일의 다른 class 를 세지 않는다' 0 "$DOK" '    public static class DbWorkAction
    {
        public const string EditReservation = "EDIT_RESERVATION";
        public const string StartReception = "START_RECEPTION";
    }

    public static class DbWorkStatus
    {
        public const string Reserved = "RSV";
    }'
  runfile 'class 를 못 찾으면 FAIL 이다' 1 "$DOK" '    public static class 딴것
    {
        public const string EditReservation = "EDIT_RESERVATION";
        public const string StartReception = "START_RECEPTION";
    }'
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
codecodes "$CODE" "$CLASS" > "$T/code.txt"

if [ ! -s "$T/doc.txt" ] || [ ! -s "$T/code.txt" ]; then
  say FAIL "WKA-000 업무동작코드를 못 읽었다 (05 §8.2 $(wc -l < "$T/doc.txt") · 코드 $(wc -l < "$T/code.txt")) — 형식이 바뀌었거나 class $CLASS 를 못 찾았다"
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

# ── WKA-002 [R22] 휴무동작코드. 같은 이유로 같은 방식이다.
if [ -f "$DOC2" ] && [ -f "$CODE2" ]; then
  H=$(mktemp -d)
  holcodes  "$DOC2"  > "$H/doc.txt"
  codecodes "$CODE2" "$CLASS2" > "$H/code.txt"
  if [ ! -s "$H/doc.txt" ] || [ ! -s "$H/code.txt" ]; then
    say FAIL "WKA-002 휴무동작코드를 못 읽었다 — 05 §12.6 의 형식이 바뀌었거나 class $CLASS2 를 못 찾았다"
  else
    ONLY_D=$(comm -23 "$H/doc.txt" "$H/code.txt")
    ONLY_C=$(comm -13 "$H/doc.txt" "$H/code.txt")
    if [ -z "$ONLY_D" ] && [ -z "$ONLY_C" ]; then
      say PASS "WKA-002 휴무동작코드 양방향 차집합 0 ($(wc -l < "$H/doc.txt") 건)"
    else
      say FAIL "WKA-002 휴무동작코드 불일치"
      [ -n "$ONLY_D" ] && echo "$ONLY_D" | sed 's/^/    05 §12.6 에만: /'
      [ -n "$ONLY_C" ] && echo "$ONLY_C" | sed 's/^/    코드에만: /'
    fi
  fi
  rm -f "$H/doc.txt" "$H/code.txt"; rmdir "$H"
else
  say FAIL "WKA-002 파일이 없다: $DOC2 · $CODE2"
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS 동작코드 ↔ 05 §8.2 · §12.6 =="; else echo "== FAIL =="; fi
exit $FAIL
