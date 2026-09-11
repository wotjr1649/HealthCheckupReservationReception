#!/usr/bin/env bash
# 주민등록번호 7번째 자리 표 ↔ Presenter 의 파생 분기 (03 §6.2).
#
# [X] 이 표는 03 §6.2 와 05 §10.1 이 갖는다. 그런데 03 §6.2 가 화면더러 생년월일·성별을
#     산출하라고 하므로 C# 이 같은 표를 한 벌 더 갖는 수밖에 없다. 값을 두 곳에 두면
#     그것을 재는 검사를 함께 만든다 — 만들 수 없으면 적지 않는다 (ROOT AGENTS.md §6).
#
# SOC-001  03 §6.2 의 표에서 여덟 갈래를 읽는다
# SOC-002  03 §6.2 ↔ PatientEditorPresenter.CenturyOf 양방향 차집합 0
set -uo pipefail
cd "$(dirname "$0")/.."

: "${DOC:=../docs/baseline/03_Wireframe_Definition.md}"
: "${SRC:=src/HealthCheckupReservationReception.WinForms/Presenters/PatientEditorPresenter.cs}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# 03 §6.2 의 표 한 행(`| 1, 5 | 1900년대 | 남 |`) → `<자리> <세기> <M|F>` 두 줄.
doc_rows() {
  tr -d '\r' < "$DOC" \
    | awk '/^## 6[.]2 /{f=1;next} f && /^## /{exit} f' \
    | sed -n 's/^| *\([0-9]\), *\([0-9]\) *| *\([0-9]\{4\}\)년대 *| *\(남\|여\) *|.*/\1 \3 \4\n\2 \3 \4/p' \
    | sed -e 's/ 남$/ M/' -e 's/ 여$/ F/' \
    | LC_ALL=C sort -u
}

# C# 의 분기(`case "1": case "5": return new Century(1900, "M");`) → 같은 꼴.
src_rows() {
  tr -d '\r' < "$SRC" \
    | sed -n 's/^ *case "\([0-9]\)": case "\([0-9]\)": return new Century(\([0-9]\{4\}\), "\([MF]\)");.*/\1 \3 \4\n\2 \3 \4/p' \
    | LC_ALL=C sort -u
}

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  DOCOK='## 6.2 주민등록번호
| 7번째 자리 | 세기 | 성별 |
|---|---:|---|
| 1, 5 | 1900년대 | 남 |
| 2, 6 | 1900년대 | 여 |
| 3, 7 | 2000년대 | 남 |
| 4, 8 | 2000년대 | 여 |
## 6.3 다음'
  SRCOK='            switch (code)
            {
                case "1": case "5": return new Century(1900, "M");
                case "2": case "6": return new Century(1900, "F");
                case "3": case "7": return new Century(2000, "M");
                case "4": case "8": return new Century(2000, "F");
                default: return null;
            }'
  run() { # run <라벨> <기대 exit> <doc> <src>
    printf '%s\n' "$3" > "$D/doc.md"
    printf '%s\n' "$4" > "$D/p.cs"
    DOC="$D/doc.md" SRC="$D/p.cs" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS SOC-SELFTEST $1"
    else echo "FAIL SOC-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }

  run '표와 코드가 같으면 통과한다'     0 "$DOCOK" "$SRCOK"
  run '성별이 뒤집히면 잡는다'          1 "$DOCOK" "${SRCOK/Century(1900, \"M\")/Century(1900, \"F\")}"
  run '세기가 어긋나면 잡는다'          1 "$DOCOK" "${SRCOK/Century(2000, \"M\")/Century(1900, \"M\")}"
  run '자리 하나가 빠지면 잡는다'       1 "$DOCOK" "${SRCOK/case \"4\": case \"8\":/case \"4\":}"
  # 1800년대생(9·0)을 넣으면 03 에 없는 갈래이므로 잡혀야 한다.
  run '표에 없는 자리를 넣으면 잡는다'  1 "$DOCOK" "${SRCOK/default: return null;/case \"9\": case \"0\": return new Century(1800, \"M\");}"
  # [X] 표를 못 읽으면 통과가 아니라 FAIL 이다 — 조용히 통과하는 게이트가 가장 위험하다.
  run '03 표를 못 읽으면 FAIL 이다'     1 '## 6.2 주민등록번호
표가 없다
## 6.3 다음' "$SRCOK"

  rm -f "$D/doc.md" "$D/p.cs" "$D/out.txt"
  rmdir "$D"
  exit $RC
fi

for f in "$DOC" "$SRC"; do
  [ -f "$f" ] || { say FAIL "SOC-000 파일이 없다: $f"; echo "== FAIL =="; exit 1; }
done

T=$(mktemp -d)
trap 'rm -f "$T"/doc "$T"/src; rmdir "$T"' EXIT
doc_rows > "$T/doc"
src_rows > "$T/src"

N=$(wc -l < "$T/doc")
if [ "$N" -ne 8 ]; then
  say FAIL "SOC-001 03 §6.2 에서 여덟 갈래를 못 읽었다 (읽은 것 $N 건) — 표 형식이 바뀌었다"
  echo "== FAIL =="; exit 1
fi
say PASS "SOC-001 03 §6.2 의 7번째 자리 여덟 갈래를 읽었다"

ONLYDOC=$(LC_ALL=C comm -23 "$T/doc" "$T/src")
ONLYSRC=$(LC_ALL=C comm -13 "$T/doc" "$T/src")
if [ -z "$ONLYDOC" ] && [ -z "$ONLYSRC" ]; then
  say PASS "SOC-002 03 §6.2 ↔ CenturyOf 양방향 차집합 0 (8 건)"
else
  say FAIL "SOC-002 7번째 자리 해석 불일치"
  [ -n "$ONLYDOC" ] && echo "$ONLYDOC" | sed 's/^/    03 §6.2 에만: /'
  [ -n "$ONLYSRC" ] && echo "$ONLYSRC" | sed 's/^/    C# 에만: /'
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS 주민번호 7번째 자리 =="; else echo "== FAIL =="; fi
exit $FAIL
