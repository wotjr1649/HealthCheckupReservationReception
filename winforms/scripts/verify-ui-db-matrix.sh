#!/usr/bin/env bash
# 07 §3·§4 ↔ 03 §2 · 05 §1.3 대조.
#
# 07 의 대조표는 베낀 목록이다. 베낀 목록은 원본이 바뀌면 뒤처진다 — 이 저장소가 두 번 물린
# 함정이 정확히 그것이다(ROOT AGENTS.md §6). 원본과 양방향 차집합 0 인지 매 회귀에서 본다.
#
# UIDB-001  03 §2 의 화면 ID 전건 ↔ 07 §3
# UIDB-002  05 §1.3 의 SP ID 전건 ↔ 07 §4
# UIDB-003  07 §4 의 SP ID 가 07 §3 에도 나타난다 (두 표가 서로 어긋나지 않는다)
set -uo pipefail
cd "$(dirname "$0")/.."

: "${WF:=../docs/baseline/03_Wireframe_Definition.md}"
: "${SP:=../docs/baseline/05_DB_Rule_SP_Contract.md}"
: "${M7:=../docs/phase5/07_UI_DB_Matrix_Final_Validation.md}"

FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# 문서의 한 구획을 잘라낸다. sec <파일> <시작 정규식> <끝 정규식>
sec() { awk -v s="$2" -v e="$3" 'f && $0 ~ e {exit} $0 ~ s {f=1} f' "$1"; }

ids() { grep -oE "$1" | sort -u; }

# 두 목록의 양방향 차집합을 본다. cmp2 <검사ID> <설명> <왼쪽라벨> <파일> <오른쪽라벨> <파일>
cmp2() {
  local id="$1" desc="$2" ln="$3" lf="$4" rn="$5" rf="$6"
  local nl nr only_l only_r
  nl=$(wc -l < "$lf"); nr=$(wc -l < "$rf")
  # [X] 파싱이 빈 목록을 내면 차집합이 0 이 되어 "일치" 로 보인다 — 조용히 통과하는 게이트다.
  #     양쪽 다 1건 이상인지 먼저 본다.
  if [ "$nl" -eq 0 ] || [ "$nr" -eq 0 ]; then
    say FAIL "$id $desc — 목록을 못 읽었다 ($ln $nl · $rn $nr). 문서 형식이 바뀌었다"
    return 0
  fi
  only_l=$(comm -23 "$lf" "$rf"); only_r=$(comm -13 "$lf" "$rf")
  if [ -z "$only_l" ] && [ -z "$only_r" ]; then
    say PASS "$id $desc 양방향 차집합 0 ($nl 건)"
  else
    say FAIL "$id $desc 불일치"
    [ -n "$only_l" ] && echo "$only_l" | sed "s/^/    $ln 에만: /"
    [ -n "$only_r" ] && echo "$only_r" | sed "s/^/    $rn 에만: /"
  fi
}

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  printf '# 2. 화면 목록\n| WF-00 | a |\n| DLG-X-01 | b |\n# 3. 다음\n' > "$D/wf.md"
  printf '## 1.3 외부 호출\n| SP-COM-01 | a |\n| SP-PAT-01 | b |\n## 1.4 다음\n' > "$D/sp.md"
  run7() { # run7 <라벨> <기대 exit> <07 본문>
    printf '%s' "$3" > "$D/07.md"
    WF="$D/wf.md" SP="$D/sp.md" M7="$D/07.md" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS UIDB-SELFTEST $1"
    else echo "FAIL UIDB-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  GOOD='# 3. M1
## 3.1 WF-00 x
| a | `SP-COM-01` |
## 3.2 DLG-X-01 y
| a | `SP-PAT-01` |
# 4. M2
| `SP-COM-01` | WF-00 |
| `SP-PAT-01` | DLG-X-01 |
# 5. 끝
'
  run7 '전건 일치하면 통과한다' 0 "$GOOD"
  run7 '07 §3 에 화면이 빠지면 잡는다' 1 "${GOOD/\#\# 3.2 DLG-X-01 y/## 3.2 없음 y}"
  run7 '07 §4 에 SP 가 빠지면 잡는다' 1 "${GOOD/| \`SP-PAT-01\` | DLG-X-01 |/}"
  run7 '07 §4 에만 있는 SP 를 잡는다' 1 "${GOOD/| \`SP-PAT-01\` | DLG-X-01 |/| \`SP-ZZZ-99\` | DLG-X-01 |}"
  run7 '구획을 못 읽으면 통과가 아니라 FAIL 이다' 1 '내용 없음'
  # [I] 만든 파일만 지우고 빈 디렉터리를 닫는다. 재귀 삭제를 쓰지 않는다 —
  #     변수가 빈 값이 되는 순간 지우는 범위가 통째로 달라지는 부류의 명령이다.
  rm -f "$D/wf.md" "$D/sp.md" "$D/07.md" "$D/out.txt"
  rmdir "$D"
  exit $RC
fi

for f in "$WF" "$SP" "$M7"; do
  [ -f "$f" ] || { say FAIL "UIDB-000 파일이 없다: $f"; echo "== FAIL =="; exit 1; }
done

T=$(mktemp -d)
trap 'rm -f "$T"/03.screens "$T"/07.screens "$T"/05.sps "$T"/07.sps "$T"/07.m1sps; rmdir "$T"' EXIT
SCREEN='\b(WF|DLG|CNF)-[A-Z0-9]+(-[0-9]+)?\b'
SPID='\bSP-[A-Z]{3}-[0-9]{2}\b'

sec "$WF" '^# 2[.] '           '^# '    | ids "$SCREEN" > "$T/03.screens"
sec "$M7" '^# 3[.] M1'         '^# 4[.]' | ids "$SCREEN" > "$T/07.screens"
sec "$SP" '^## 1[.]3 '         '^## '   | ids "$SPID"   > "$T/05.sps"
sec "$M7" '^# 4[.] M2'         '^# 5[.]' | ids "$SPID"   > "$T/07.sps"
sec "$M7" '^# 3[.] M1'         '^# 4[.]' | ids "$SPID"   > "$T/07.m1sps"

cmp2 UIDB-001 '화면 ID' '03 §2'   "$T/03.screens" '07 §3' "$T/07.screens"
cmp2 UIDB-002 'SP ID'   '05 §1.3' "$T/05.sps"     '07 §4' "$T/07.sps"
cmp2 UIDB-003 'SP ID'   '07 §4'   "$T/07.sps"     '07 §3' "$T/07.m1sps"

if [ "$FAIL" -eq 0 ]; then echo "== PASS 07 ↔ 03 · 05 대조 =="; else echo "== FAIL =="; fi
exit $FAIL
