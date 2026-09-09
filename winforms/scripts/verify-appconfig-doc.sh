#!/usr/bin/env bash
# App.config ↔ 05 §1.1 대조.
# 05 §1.1 이 확정한 DB 이름과 연결문자열 키 이름을 App.config 가 그대로 쓰는지 본다.
# 이 저장소는 "값을 베껴 두었다가 계약이 바뀌어 거짓이 된" 함정에 두 번 물렸다
# (루트 AGENTS.md §6). App.config 는 그 값을 안 적을 수 없는 자리이므로 검사를 붙인다.
#
# database/scripts/verify-schema-doc.sh 와 같은 층의 검사다 — 계약 문서를 파싱해 실물과 맞춘다.
set -uo pipefail
cd "$(dirname "$0")/.."

# selftest 가 대체 경로를 넣는다. 평소에는 실물을 본다.
: "${DOC:=../docs/baseline/05_DB_Rule_SP_Contract.md}"
: "${CFG:=src/HealthCheckupReservationReception.WinForms/App.config}"
FAIL=0

# selftest — "불일치를 실제로 잡는가" 를 재현 가능하게 판정한다.
# [X] 조용히 통과하는 게이트가 이 검사에서 가장 위험하다: 표 형식이 바뀌어 파싱이 빈 값을
#     내면 비교가 전부 참으로 보인다. 그 경로까지 시험한다.
if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  printf '| Database | `GoodDb` |\n| Connection String Name | `GoodKey` |\n' > "$D/doc.md"
  ok() { # ok <라벨> <기대 exit> <config 본문>
    printf '%s\n' "$3" > "$D/app.config"
    DOC="$D/doc.md" CFG="$D/app.config" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS CFG-SELFTEST $1"
    else echo "FAIL CFG-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  ok '일치하면 통과한다'      0 '<add name="GoodKey" connectionString="Initial Catalog=GoodDb" providerName="System.Data.SqlClient" />'
  ok 'DB 이름 불일치를 잡는다' 1 '<add name="GoodKey" connectionString="Initial Catalog=StaleDb" providerName="System.Data.SqlClient" />'
  ok '키 이름 불일치를 잡는다' 1 '<add name="AppDb" connectionString="Initial Catalog=GoodDb" providerName="System.Data.SqlClient" />'
  ok 'provider 불일치를 잡는다' 1 '<add name="GoodKey" connectionString="Initial Catalog=GoodDb" providerName="Microsoft.Data.SqlClient" />'
  # 표 형식이 바뀌어 파싱이 빈 값을 내는 경우 — 통과가 아니라 FAIL 이어야 한다.
  printf '| Database : GoodDb |\n' > "$D/doc.md"
  ok '05 표를 못 읽으면 통과가 아니라 FAIL 이다' 1 '<add name="GoodKey" connectionString="Initial Catalog=GoodDb" providerName="System.Data.SqlClient" />'
  rm -rf "$D"
  exit $RC
fi

say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# 05 §1.1 표에서 한 행을 뽑는다. | 구분 | `값` | 형식이다.
row() { sed -n "s/^| *$1 *| *\`\([^\`]*\)\`.*/\1/p" "$DOC" | head -1; }

for f in "$DOC" "$CFG"; do
  [ -f "$f" ] || { say FAIL "CFG-000 파일이 없다: $f"; echo "== FAIL =="; exit 1; }
done

DB=$(row 'Database')
KEY=$(row 'Connection String Name')

# [X] 파싱이 빈 값을 내면 이후 비교가 전부 "일치" 로 보인다 — 조용히 통과하는 게이트가 된다.
#     표 형식이 바뀌면 여기서 먼저 멈춘다.
if [ -z "$DB" ] || [ -z "$KEY" ]; then
  say FAIL "CFG-000 05 §1.1 에서 값을 못 읽었다 (Database='$DB' Key='$KEY') — 표 형식이 바뀌었다"
  echo "== FAIL =="; exit 1
fi

# App.config 에서 그 이름의 add 요소 한 줄 묶음을 뽑는다 (속성이 여러 줄에 걸쳐 있다).
ENTRY=$(tr '\n' ' ' < "$CFG" | grep -oE "<add[^>]*name=\"$KEY\"[^>]*>" || true)

if [ -z "$ENTRY" ]; then
  say FAIL "CFG-001 App.config 에 connectionStrings 항목 '$KEY' 가 없다 (05 §1.1)"
else
  say PASS "CFG-001 연결문자열 키 이름이 05 §1.1 과 같다 ($KEY)"

  CAT=$(echo "$ENTRY" | sed -n 's/.*[Ii]nitial [Cc]atalog *= *\([^;"]*\).*/\1/p')
  if [ "$CAT" = "$DB" ]; then
    say PASS "CFG-002 Initial Catalog 가 05 §1.1 의 Database 와 같다 ($DB)"
  else
    say FAIL "CFG-002 Initial Catalog 불일치 — App.config='$CAT' vs 05 §1.1='$DB'"
  fi

  # providerName 은 킷 §3 이 System.Data.SqlClient 로 고정한다 (Microsoft.Data.SqlClient 는 out).
  case "$ENTRY" in
    *'providerName="System.Data.SqlClient"'*)
      say PASS "CFG-003 providerName 이 System.Data.SqlClient 다 (킷 §3)" ;;
    *)
      say FAIL "CFG-003 providerName 이 System.Data.SqlClient 가 아니다 (킷 §3)" ;;
  esac
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS App.config ↔ 05 §1.1 =="; else echo "== FAIL =="; fi
exit $FAIL
