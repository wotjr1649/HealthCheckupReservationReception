#!/usr/bin/env bash
# 계층·호출 경계 (07 §10 P05·P06·P07).
#
# LAY-001  C# 에 Inline TVF 직접 호출이 0건이다               05 §1.4 "C# 직접 호출 금지"
# LAY-002  C# 이 부르는 SP 이름 전건이 05 §1.3 목록 안이다     05 §1.5 미생성 객체도 이것이 막는다
# LAY-003  inline DML 0건 · AddWithValue 0건                  킷 §3
# LAY-004  SqlClient 는 Repositories/ 안에만 · DevExpress 는 Views/·Program.cs 안에만 · IXxxView 는 DevExpress-free
#                                                            킷 §2
set -uo pipefail
cd "$(dirname "$0")/.."

: "${DOC:=../docs/baseline/05_DB_Rule_SP_Contract.md}"
: "${SRC:=src/HealthCheckupReservationReception.WinForms}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

srcfiles() { find "$SRC" -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' 2>/dev/null | sort; }

# [X] 낱말만 찾으면 **그 규칙을 설명하는 주석이 첫 HIT** 가 된다. verify-no-secret.sh 초판이
#     자기 자신을 세었던 것과 같은 함정이다. 줄 주석을 걷고 코드만 본다.
nocomment() { sed 's|//.*||' "$1"; }
codegrep() {  # codegrep <패턴> <파일...>  → 파일:줄:내용
  local pat="$1"; shift
  local f
  for f in "$@"; do nocomment "$f" | grep -nE "$pat" | sed "s|^|$f:|"; done
}

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  mkdir -p "$D/src/Repositories" "$D/src/Views"
  printf '## 1.3 외부 호출\n| SP-COM-01 | `[dbo].[USP_HC_좋은것_조회]` | SELECT | x |\n## 1.4 다음\n' > "$D/doc.md"
  fire() { # fire <라벨> <기대 exit>
    DOC="$D/doc.md" SRC="$D/src" "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS LAY-SELFTEST $1"
    else echo "FAIL LAY-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  reset() {
    printf 'using System.Data.SqlClient;\nvar c = new SqlCommand("dbo.USP_HC_좋은것_조회");\n' > "$D/src/Repositories/R.cs"
    printf 'using DevExpress.XtraEditors;\nclass V { }\n' > "$D/src/Views/V.cs"
    printf 'interface IXView { }\n' > "$D/src/Views/IXView.cs"
    rm -f "$D/src/Bad.cs"
  }
  reset; fire '기준 상태는 통과한다' 0
  reset; printf 'var s = "dbo.UFN_HC_일정확인";\n' > "$D/src/Bad.cs";                       fire 'TVF 직접 호출을 잡는다' 1
  reset; printf 'var s = "dbo.USP_HC_없는것_조회";\n' > "$D/src/Bad.cs";                    fire '05 §1.3 밖의 SP 를 잡는다' 1
  reset; printf 'var s = "SELECT 1 FROM T";\n' > "$D/src/Bad.cs";                           fire 'inline DML 을 잡는다' 1
  reset; printf 'cmd.Parameters.AddWithValue("@a", 1);\n' > "$D/src/Bad.cs";                fire 'AddWithValue 를 잡는다' 1
  reset; printf 'using System.Data.SqlClient;\nvar c = new SqlCommand();\n' > "$D/src/Bad.cs"; fire 'Repositories 밖의 SqlClient 를 잡는다' 1
  reset; printf 'using DevExpress.XtraGrid;\n' > "$D/src/Bad.cs";                           fire 'Views 밖의 DevExpress 를 잡는다' 1
  reset; printf 'using DevExpress.XtraBars;\ninterface IXView { }\n' > "$D/src/Views/IXView.cs"; fire 'IXxxView 의 DevExpress 를 잡는다' 1
  # 규칙을 설명하는 주석은 세지 않는다 — 초판이 정확히 여기서 걸렸다(IMainView.cs 의 주석).
  reset; printf '// DevExpress 타입과 UFN_HC_ 직접호출과 SELECT 를 쓰지 않는다\nclass Ok { }\n' > "$D/src/Bad.cs"
  fire '규칙을 설명하는 주석은 세지 않는다' 0
  reset
  rm -f "$D/src/Repositories/R.cs" "$D/src/Views/V.cs" "$D/src/Views/IXView.cs" "$D/doc.md" "$D/out.txt"
  rmdir "$D/src/Repositories" "$D/src/Views" "$D/src" "$D"
  exit $RC
fi

[ -f "$DOC" ] || { say FAIL "LAY-000 파일이 없다: $DOC"; echo "== FAIL =="; exit 1; }
FILES=$(srcfiles)
[ -n "$FILES" ] || { say FAIL "LAY-000 .cs 를 하나도 못 찾았다 ($SRC)"; echo "== FAIL =="; exit 1; }

# ── LAY-001
HIT=$(codegrep 'UFN_HC_' $FILES || true)
if [ -z "$HIT" ]; then
  say PASS "LAY-001 C# 에 Inline TVF 직접 호출 0건 (05 §1.4)"
else
  say FAIL "LAY-001 Inline TVF 를 C# 이 직접 부른다 (05 §1.4)"; echo "$HIT" | sed 's/^/    /'
fi

# ── LAY-002
T=$(mktemp -d)
trap 'rm -f "$T"/allowed "$T"/used; rmdir "$T"' EXIT
tr -d '\r' < "$DOC" | awk '/^## 1[.]3 /{f=1;next} f && /^## /{exit} f' \
  | grep -oE 'USP_HC_[^]`]+' | sort -u > "$T/allowed"
for f in $FILES; do nocomment "$f"; done | grep -ohE 'USP_HC_[A-Za-z0-9_가-힣]+' | sort -u > "$T/used"
if [ ! -s "$T/allowed" ]; then
  say FAIL "LAY-002 05 §1.3 에서 SP 이름을 못 읽었다 — 표 형식이 바뀌었다"
else
  UNKNOWN=$(comm -13 "$T/allowed" "$T/used")
  if [ -z "$UNKNOWN" ]; then
    say PASS "LAY-002 C# 이 부르는 SP $(wc -l < "$T/used") 건 전부 05 §1.3 안이다"
  else
    say FAIL "LAY-002 05 §1.3 에 없는 SP 를 부른다"; echo "$UNKNOWN" | sed 's/^/    /'
  fi
fi

# ── LAY-003  문자열 리터럴 안의 DML 과 AddWithValue
HIT=$(codegrep '"[^"]*\b(SELECT|INSERT|UPDATE|DELETE)\b[^"]*"|AddWithValue' $FILES || true)
if [ -z "$HIT" ]; then
  say PASS "LAY-003 inline DML 0건 · AddWithValue 0건 (킷 §3)"
else
  say FAIL "LAY-003 inline DML 또는 AddWithValue"; echo "$HIT" | sed 's/^/    /'
fi

# ── LAY-004
HIT=
for f in $FILES; do
  case "$f" in
    */Repositories/*) ;;
    *) nocomment "$f" | grep -qE '\b(SqlConnection|SqlCommand|SqlDataReader|SqlTransaction)\b' \
         && HIT="$HIT\n    $f — SqlClient 타입이 Repositories/ 밖에 있다" ;;
  esac
  case "$f" in
    */Views/I*.cs) nocomment "$f" | grep -q 'DevExpress' \
         && HIT="$HIT\n    $f — View 계약이 DevExpress 를 노출한다" ;;
    */Views/*|*/Program.cs) ;;
    *) nocomment "$f" | grep -q 'DevExpress' \
         && HIT="$HIT\n    $f — DevExpress 타입이 Views/·Program.cs 밖에 있다" ;;
  esac
done
if [ -z "$HIT" ]; then
  say PASS "LAY-004 SqlClient·DevExpress 가 제 계층 안에 있다 (킷 §2)"
else
  say FAIL "LAY-004 계층 격리 위반"; printf '%b\n' "$HIT"
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS 계층·호출 경계 =="; else echo "== FAIL =="; fi
exit $FAIL
