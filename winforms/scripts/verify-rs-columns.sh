#!/usr/bin/env bash
# Repositories 의 컬럼 이름 문자열 ↔ 05 의 Result Set 계약.
#
# 이것이 단위시험이 못 보는 유일한 고리다. reader.GetOrdinal("오늘날짜") 의 오타는
# 컴파일도 통과하고 fake 리포지토리를 쓰는 시험도 통과한다 — 실행할 때만 터진다.
#
# DB 에 붙지 않는다. 05 를 본다 — 05 와 실물 DB 가 맞는지는 database 계열의 G09 가
# 판정하므로(06 §42), 여기서 05 를 지키면 사슬이 닫힌다.
#
# RSC-001  Repositories 가 부르는 SP 마다 05 에 그 계약 절이 있다
# RSC-002  GetOrdinal 문자열 전건이 그 절(+ 05 §3.1 공통 RS0)의 컬럼 이름이다
set -uo pipefail
cd "$(dirname "$0")/.."

: "${DOC:=../docs/baseline/05_DB_Rule_SP_Contract.md}"
: "${REPOS:=src/HealthCheckupReservationReception.WinForms/Repositories}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# 헤더가 <이름> 을 포함하는 절의 본문. 없으면 빈 출력.
#
# [X] 절 깊이는 SP 마다 다르다. `USP_HC_예약접수목록_조회` 는 `## 8.1` 인데
#     `USP_HC_예약가능정보_조회` 는 문서 한 장을 통째로 차지해 `# 9.` 다. `## ` 만 찾으면
#     뒤엣것이 "계약 절이 없는 SP" 로 걸려 red 가 된다 — 계약은 멀쩡한데 파서가 못 읽은
#     것이다. 그래서 제목 깊이를 재서 **같거나 더 얕은** 다음 제목에서 멈춘다.
section_for() {
  tr -d '\r' < "$DOC" | awk -v n="$1" '
    !f && index($0, n) && /^#+ / { match($0, /^#+/); lvl = RLENGTH; f = 1; next }
    f && /^#+ / { match($0, /^#+/); if (RLENGTH <= lvl) exit }
    f'
}
# 05 §3.1 공통 RS0 절.
rs0_section() { tr -d '\r' < "$DOC" | awk '/^## 3[.]1 /{f=1;next} f && /^## /{exit} f'; }
# 백틱으로 감싼 토큰 = 계약이 이름으로 부르는 것.
ticks() { grep -oE '`[^`]+`' | tr -d '`' | sort -u; }

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  mkdir -p "$D/repos"
  # 두 깊이를 다 담는다 — `## ` 한 절짜리 SP 와 `# ` 한 장짜리 SP.
  {
    printf '## 3.1 RS0\n| 1 | `성공여부` | BIT |\n'
    printf '## 9.9 `[dbo].[USP_HC_좋은것_조회]`\n| `좋은컬럼` | INT |\n## 9.10 다음\n'
    printf '# 12. `[dbo].[USP_HC_한장짜리_조회]` 계약\n## 12.1 RS1\n| `한장컬럼` | INT |\n'
    printf '# 13. 다른 장\n## 13.1 RS1\n| `남의컬럼` | INT |\n'
  } > "$D/doc.md"
  run() { # run <라벨> <기대 exit> <repo 본문>
    printf '%s\n' "$3" > "$D/repos/R.cs"
    DOC="$D/doc.md" REPOS="$D/repos" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS RSC-SELFTEST $1"
    else echo "FAIL RSC-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  run '계약에 있는 컬럼이면 통과한다' 0 'new SqlCommand("dbo.USP_HC_좋은것_조회");
reader.GetOrdinal("좋은컬럼");
reader.GetOrdinal("성공여부");'
  run '오타난 컬럼을 잡는다'          1 'new SqlCommand("dbo.USP_HC_좋은것_조회");
reader.GetOrdinal("조은컬럼");'
  run '05 에 절이 없는 SP 를 잡는다'   1 'new SqlCommand("dbo.USP_HC_없는것_조회");
reader.GetOrdinal("좋은컬럼");'
  # 주석 안의 컬럼 이름은 세지 않는다 — 다른 게이트가 여기서 걸렸다.
  run '주석 속 이름은 세지 않는다'    0 'new SqlCommand("dbo.USP_HC_좋은것_조회");
// reader.GetOrdinal("이건주석이다");
reader.GetOrdinal("좋은컬럼");'
  # [X] `# ` 한 장을 통째로 쓰는 SP 가 있다(USP_HC_예약가능정보_조회). `## ` 만 찾던 판은
  #     그것을 "계약 절이 없는 SP" 로 잡았다 — 계약은 멀쩡한데 파서가 못 읽은 red 였다.
  run '한 장(#)짜리 절도 읽는다'      0 'new SqlCommand("dbo.USP_HC_한장짜리_조회");
reader.GetOrdinal("한장컬럼");'
  run '다음 장의 컬럼을 제 것으로 세지 않는다' 1 'new SqlCommand("dbo.USP_HC_한장짜리_조회");
reader.GetOrdinal("남의컬럼");'
  rm -f "$D/repos/R.cs" "$D/doc.md" "$D/out.txt"
  rmdir "$D/repos" "$D"
  exit $RC
fi

[ -f "$DOC" ] || { say FAIL "RSC-000 파일이 없다: $DOC"; echo "== FAIL =="; exit 1; }
FILES=$(find "$REPOS" -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' 2>/dev/null | sort)
if [ -z "$FILES" ]; then
  say PASS "RSC-000 Repositories 에 .cs 가 없다 — 검사할 것이 아직 없다"
  echo "== PASS Result Set 컬럼 이름 =="; exit 0
fi

T=$(mktemp -d)
trap 'rm -f "$T"/allowed "$T"/used; rmdir "$T"' EXIT
rs0_section | ticks > "$T/allowed"

SPS=$(for f in $FILES; do sed 's|//.*||' "$f"; done | grep -ohE 'USP_HC_[A-Za-z0-9_가-힣]+' | sort -u)
MISSING=
for sp in $SPS; do
  BODY=$(section_for "$sp")
  if [ -z "$BODY" ]; then
    MISSING="$MISSING $sp"
  else
    printf '%s\n' "$BODY" | ticks >> "$T/allowed"
  fi
done
sort -u -o "$T/allowed" "$T/allowed"

if [ -z "$MISSING" ]; then
  say PASS "RSC-001 Repositories 가 부르는 SP $(echo $SPS | wc -w) 건의 계약 절이 05 에 있다"
else
  say FAIL "RSC-001 05 에 계약 절이 없는 SP —$MISSING"
fi

for f in $FILES; do sed 's|//.*||' "$f"; done \
  | grep -ohE 'GetOrdinal\("[^"]+"\)' | sed 's|GetOrdinal("||; s|")||' | sort -u > "$T/used"

if [ ! -s "$T/used" ]; then
  say PASS "RSC-002 GetOrdinal 호출이 아직 없다"
else
  UNKNOWN=$(comm -13 "$T/allowed" "$T/used")
  if [ -z "$UNKNOWN" ]; then
    say PASS "RSC-002 컬럼 이름 $(wc -l < "$T/used") 건 전부 05 의 계약 이름이다"
  else
    say FAIL "RSC-002 05 에 없는 컬럼 이름"; echo "$UNKNOWN" | sed 's/^/    /'
  fi
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS Result Set 컬럼 이름 =="; else echo "== FAIL =="; fi
exit $FAIL
