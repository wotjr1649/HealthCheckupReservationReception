#!/usr/bin/env bash
# 잠금 대상 버튼의 `Enabled` 는 누가 쓰는가.
#
# `clsBusyScope` 는 잠글 때 `Enabled` 를 떠 두었다가 `Dispose` 에서 되돌린다. 그런데 잠그는
# 버튼이 Presenter 가 구동하는 바로 그 버튼이라, 스코프 **안에서** 화면이 `Enabled` 를 직접
# 쓰면 되돌리기가 그것을 옛 값으로 덮는다.
#
# [X] **이것이 실제로 났다** (2026-09-14). DLG-HOL-01 `[추가]` 뒤 `[수정]`·`[삭제]` 가 회색으로
#     남고, `[삭제]` 뒤에는 눌러도 아무 일도 없는 버튼이 살아났다. 화면은 잠금 여부를 모르고
#     Presenter 는 fake view 를 쓰는 시험에서 green 이라 아무도 못 봤다.
#
# [!] **`EnabledChanged` 로는 못 잡는다** — 이미 `false` 인 것에 `false` 를 다시 쓰면 그
#     이벤트가 울리지 않는다. 그래서 관찰이 아니라 `clsBusyScope.SetEnabled` 로 **기록**한다.
#     그 길을 안 쓰면 조용히 되살아나므로 여기서 판정한다.
#
# ENB-001  잠금 대상 버튼 목록을 읽었다
# ENB-002  그 버튼에 `.Enabled =` 직접 쓰기 0건 (Designer 는 제외 — 초기값은 거기 것이다)
set -uo pipefail
cd "$(dirname "$0")/.."

: "${SRC:=src/HealthCheckupReservationReception.WinForms/Views}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# ConfigureUI 의 `new clsActionRunner(this, btnA, btnB)` 가 잠금 대상의 단일 출처다.
locked() {
  cat "$1"/*.UI.cs 2>/dev/null | tr -d '\r' \
    | grep -oE 'new clsActionRunner\(this[^)]*\)' \
    | grep -oE 'btn[A-Za-z]+' \
    | sort -u
}
# 화면 코드에서 그 버튼에 직접 쓰는 곳. Designer 는 초기값을 쓰는 자리이므로 뺀다.
direct() {
  local dir="$1"; shift
  for f in "$dir"/*.cs; do
    case "$f" in *.Designer.cs) continue;; esac
    for b in "$@"; do
      grep -nH "$b[.]Enabled[[:space:]]*=" "$f" 2>/dev/null || true
    done
  done
}

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  mkdir -p "$D/v"
  run() { # run <라벨> <기대 exit> <Frm 본문>
    printf 'partial void ConfigureUI() { _a = new clsActionRunner(this, btnSave, btnClose); }\n' > "$D/v/Frm.UI.cs"
    printf '%s\n' "$3" > "$D/v/Frm.cs"
    printf 'this.btnSave.Enabled = false;\n' > "$D/v/Frm.Designer.cs"
    SRC="$D/v" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS ENB-SELFTEST $1"
    else echo "FAIL ENB-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  run '직접 쓰기가 없으면 통과한다'        0 'set { clsBusyScope.SetEnabled(btnSave, value); }'
  run '직접 쓰기를 잡는다'                1 'set { btnSave.Enabled = value; }'
  run '공백이 끼어도 잡는다'              1 'set { btnSave.Enabled  =  value; }'
  run '잠금 대상이 아니면 안 잡는다'       0 'set { gcAex.Enabled = value; }'
  # [X] Designer 는 초기값을 쓰는 자리다. 그것까지 잡으면 아무도 이 게이트를 못 지킨다.
  run 'Designer 는 세지 않는다'           0 'set { clsBusyScope.SetEnabled(btnSave, value); }'
  # [X] 목록을 못 읽으면 "대상이 없으니 통과" 가 된다 — 조용히 통과하는 게이트다.
  rm -f "$D/v/Frm.UI.cs"; printf 'nothing\n' > "$D/v/Frm.UI.cs"
  run '잠금 목록을 못 읽으면 FAIL 이다'    1 'set { btnSave.Enabled = value; }'
  rm -f "$D/v/Frm.cs" "$D/v/Frm.UI.cs" "$D/v/Frm.Designer.cs" "$D/out.txt"
  rmdir "$D/v" "$D"
  exit $RC
fi

[ -d "$SRC" ] || { say FAIL "ENB-000 디렉터리가 없다: $SRC"; echo "== FAIL =="; exit 1; }

BTNS=$(locked "$SRC")
if [ -z "$BTNS" ]; then
  say FAIL "ENB-001 잠금 대상 버튼을 하나도 못 읽었다 — clsActionRunner 호출 형식이 바뀌었다"
  echo "== FAIL =="; exit 1
fi
say PASS "ENB-001 잠금 대상 버튼 $(echo "$BTNS" | wc -l) 종을 읽었다 ($(echo $BTNS | tr '\n' ' '))"

HITS=$(direct "$SRC" $BTNS)
if [ -z "$HITS" ]; then
  say PASS "ENB-002 잠금 대상에 Enabled 직접 쓰기 0건 — 전부 clsBusyScope.SetEnabled 를 지난다"
else
  say FAIL "ENB-002 잠금 대상에 Enabled 직접 쓰기"
  echo "$HITS" | sed 's/^/    /'
  echo "    → clsBusyScope.SetEnabled(버튼, 값) 으로 쓴다. 직접 쓰면 잠금이 풀릴 때 덮인다"
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS Enabled 소유 =="; else echo "== FAIL =="; fi
exit $FAIL
