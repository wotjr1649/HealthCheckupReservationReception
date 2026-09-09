#!/usr/bin/env bash
# 실물 ↔ 05 §1.1 「프로그램·DB 명칭」 대조.
# 05 §1.1 이 확정한 이름을 App.config·csproj·소스가 그대로 쓰는지 본다.
# 이 저장소는 "값을 베껴 두었다가 계약이 바뀌어 거짓이 된" 함정에 두 번 물렸다
# (루트 AGENTS.md §6). App.config·csproj 는 그 값을 안 적을 수 없는 자리이므로 검사를 붙인다.
#
# database/scripts/verify-schema-doc.sh 와 같은 층의 검사다 — 계약 문서를 파싱해 실물과 맞춘다.
set -uo pipefail
cd "$(dirname "$0")/.."

# selftest 가 대체 경로를 넣는다. 평소에는 실물을 본다.
: "${DOC:=../docs/baseline/05_DB_Rule_SP_Contract.md}"
: "${CFG:=src/HealthCheckupReservationReception.WinForms/App.config}"
: "${PROJ:=src/HealthCheckupReservationReception.WinForms/HealthCheckupReservationReception.WinForms.csproj}"
: "${SRC:=src/HealthCheckupReservationReception.WinForms}"
FAIL=0

# selftest — "불일치를 실제로 잡는가" 를 재현 가능하게 판정한다.
# [X] 조용히 통과하는 게이트가 이 검사에서 가장 위험하다: 표 형식이 바뀌어 파싱이 빈 값을
#     내면 비교가 전부 참으로 보인다. 그 경로까지 시험한다.
#
# [I] 뒷정리는 만든 파일만 지우고 빈 디렉터리를 닫는다. 재귀 삭제를 쓰지 않는다 —
#     변수가 빈 값이 되는 순간 지우는 범위가 통째로 달라지는 부류의 명령이다.
if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  mkdir -p "$D/src"

  # run <라벨> <기대 exit> <doc 본문> <config 본문> <csproj 본문> <cs 본문>
  run() {
    printf '%s\n' "$3" > "$D/doc.md"
    printf '%s\n' "$4" > "$D/app.config"
    printf '%s\n' "$5" > "$D/p.csproj"
    printf '%s\n' "$6" > "$D/src/A.cs"
    DOC="$D/doc.md" CFG="$D/app.config" PROJ="$D/p.csproj" SRC="$D/src" \
      "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS CFG-SELFTEST $1"
    else echo "FAIL CFG-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }

  DOCOK='| Database | `GoodDb` |
| Connection String Name | `GoodKey` |
| Root Namespace | `Good` |
| WinForms Project | `Good.WinForms` |'
  CFGOK='<add name="GoodKey" connectionString="Initial Catalog=GoodDb" providerName="System.Data.SqlClient" />'
  PROJOK='<RootNamespace>Good</RootNamespace><AssemblyName>Good.WinForms</AssemblyName>'
  CSOK='namespace Good.Views'

  run '전부 맞으면 통과한다'         0 "$DOCOK" "$CFGOK" "$PROJOK" "$CSOK"
  run 'DB 이름 불일치를 잡는다'      1 "$DOCOK" '<add name="GoodKey" connectionString="Initial Catalog=StaleDb" providerName="System.Data.SqlClient" />' "$PROJOK" "$CSOK"
  run '키 이름 불일치를 잡는다'      1 "$DOCOK" '<add name="AppDb" connectionString="Initial Catalog=GoodDb" providerName="System.Data.SqlClient" />' "$PROJOK" "$CSOK"
  run 'provider 불일치를 잡는다'     1 "$DOCOK" '<add name="GoodKey" connectionString="Initial Catalog=GoodDb" providerName="Microsoft.Data.SqlClient" />' "$PROJOK" "$CSOK"
  run 'RootNamespace 불일치를 잡는다' 1 "$DOCOK" "$CFGOK" '<RootNamespace>Good.WinForms</RootNamespace><AssemblyName>Good.WinForms</AssemblyName>' "$CSOK"
  run 'AssemblyName 불일치를 잡는다'  1 "$DOCOK" "$CFGOK" '<RootNamespace>Good</RootNamespace><AssemblyName>Wrong</AssemblyName>' "$CSOK"
  run '루트 밖 namespace 를 잡는다'   1 "$DOCOK" "$CFGOK" "$PROJOK" 'namespace Other.Views'
  # Good 으로 시작하기만 하는 이름(GoodExtra)은 루트 안이 아니다 — 경계에서 새지 않는지 본다.
  run '접두사만 같은 namespace 를 잡는다' 1 "$DOCOK" "$CFGOK" "$PROJOK" 'namespace GoodExtra.Views'
  # 표 형식이 바뀌어 파싱이 빈 값을 내는 경우 — 통과가 아니라 FAIL 이어야 한다.
  run '05 표를 못 읽으면 통과가 아니라 FAIL 이다' 1 '| Database : GoodDb |' "$CFGOK" "$PROJOK" "$CSOK"

  rm -f "$D/doc.md" "$D/app.config" "$D/p.csproj" "$D/out.txt" "$D/src/A.cs"
  rmdir "$D/src" "$D"
  exit $RC
fi

say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# 05 §1.1 표에서 한 행을 뽑는다. | 구분 | `값` | 형식이다.
row() { sed -n "s/^| *$1 *| *\`\([^\`]*\)\`.*/\1/p" "$DOC" | head -1; }

for f in "$DOC" "$CFG" "$PROJ"; do
  [ -f "$f" ] || { say FAIL "CFG-000 파일이 없다: $f"; echo "== FAIL =="; exit 1; }
done

DB=$(row 'Database')
KEY=$(row 'Connection String Name')
ROOTNS=$(row 'Root Namespace')
PROJNAME=$(row 'WinForms Project')

# [X] 파싱이 빈 값을 내면 이후 비교가 전부 "일치" 로 보인다 — 조용히 통과하는 게이트가 된다.
#     표 형식이 바뀌면 여기서 먼저 멈춘다.
if [ -z "$DB" ] || [ -z "$KEY" ] || [ -z "$ROOTNS" ] || [ -z "$PROJNAME" ]; then
  say FAIL "CFG-000 05 §1.1 에서 값을 못 읽었다 (Database='$DB' Key='$KEY' Root='$ROOTNS' Project='$PROJNAME') — 표 형식이 바뀌었다"
  echo "== FAIL =="; exit 1
fi

# ── App.config
# 속성이 여러 줄에 걸쳐 있으므로 개행을 지우고 add 요소 하나를 통째로 뽑는다.
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

# ── csproj · 소스 이름 (07 §14 X-04)
GOT=$(sed -n 's|.*<RootNamespace>\([^<]*\)</RootNamespace>.*|\1|p' "$PROJ" | head -1)
if [ "$GOT" = "$ROOTNS" ]; then
  say PASS "CFG-004 csproj RootNamespace 가 05 §1.1 과 같다 ($ROOTNS)"
else
  say FAIL "CFG-004 RootNamespace 불일치 — csproj='$GOT' vs 05 §1.1='$ROOTNS'"
fi

GOT=$(sed -n 's|.*<AssemblyName>\([^<]*\)</AssemblyName>.*|\1|p' "$PROJ" | head -1)
if [ "$GOT" = "$PROJNAME" ]; then
  say PASS "CFG-005 csproj AssemblyName 이 05 §1.1 의 WinForms Project 와 같다 ($PROJNAME)"
else
  say FAIL "CFG-005 AssemblyName 불일치 — csproj='$GOT' vs 05 §1.1='$PROJNAME'"
fi

# [X] 루트만 고치고 소스의 namespace 선언이 따라오지 않으면 아무것도 달라지지 않는다.
#     선언 전건이 루트이거나 루트 + '.' 로 시작하는지 본다 (킷 §1 "Folders equal namespaces").
#     BOM 이 첫 선언 앞에 붙으므로 grep 전에 걷어낸다.
BAD=$(grep -rhoE '^(\xef\xbb\xbf)?namespace +[A-Za-z0-9_.]+' "$SRC" --include=*.cs 2>/dev/null \
      | sed 's/^\xef\xbb\xbf//; s/^namespace  *//' | sort -u \
      | grep -vxF -e "$ROOTNS" | grep -v "^${ROOTNS}\." || true)
if [ -z "$BAD" ]; then
  say PASS "CFG-006 소스의 namespace 선언 전건이 $ROOTNS 아래에 있다"
else
  say FAIL "CFG-006 루트 밖 namespace 선언"
  echo "$BAD" | sed 's/^/    /'
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS 실물 ↔ 05 §1.1 =="; else echo "== FAIL =="; fi
exit $FAIL
