#!/usr/bin/env bash
# 라이브 DB ↔ deploy/ 원본 동기화 (G17).
#
# [!] 이 축은 회귀 전체에서 비어 있었다. G05(`verify-schema-doc`)는 `04` ↔ DB 를 보고,
#     `RBD-005` 는 rebuild 두 번의 덤프를 견준다 — **둘 다 배포 원본을 다시 돌린 뒤**다.
#     "지금 붙어 있는 DB 가 deploy/ 와 같은가" 를 묻는 것은 하나도 없었다.
#
# [X] Phase 5 에서 이것이 실제 위험이다. winforms 의 `TestCategory=Db` 통합시험은
#     rebuild 없이 **살아 있는 DB 에** 붙는다. 누가 SSMS 에서 SP 를 ALTER 하면 그 시험은
#     조용히 다른 제품을 재고 초록을 낸다. 다음 rebuild 가 흔적을 지워 사후 확인도 안 된다.
#
#   SYNC-001  deploy/ 가 만드는 모듈 이름 == 라이브 DB 의 모듈 이름 (양방향 차집합 0)
#   SYNC-002  모듈마다 본문이 같다 (NVARCHAR 해시 대조 · 맞춰본 건수까지 판정)
#   SYNC-003  Deploy.sql 이 부르는 파일 == deploy/ 의 .sql (양방향 차집합 0)
#   SYNC-004  App.config 가 가리키는 DB == 위에서 판정한 그 DB
#             (`05` §1.1 은 Database 만 확정하고 **서버/인스턴스는 선언하지 않는다**.
#              그래서 화면이 게이트가 재지 않은 DB 를 볼 수 있는 자리가 남아 있었다.)
#
# [I] **정규화는 한 곳뿐이다.** SQL Server 는 `CREATE OR ALTER X` 를 `CREATE   X`(공백 3칸)로
#     바꿔 저장한다 — 실측으로 확인했고 24개 모듈 전부 그 형태다. 그 한 자리만 양쪽에서 같은
#     꼴로 맞추고 **나머지는 바이트 그대로** 견준다. 공백 한 칸 차이도 차이로 잡으려는 것이다.
#     (전체 공백 정규화를 하면 실제 드리프트를 삼킬 수 있다.)
set -uo pipefail
cd "$(dirname "$0")/.."

: "${SRV:=.\\SQLEXPRESS}"
: "${DB:=HealthCheckupReservationReceptionDb}"
: "${DEPLOYDIR:=deploy}"
: "${DEPLOYFILE:=Deploy.sql}"
: "${LIVE:=1}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

if [ "${1:-check}" = "selftest" ]; then
  SELF=$(cd "$(dirname "$0")" && pwd)/$(basename "$0")
  D=$(mktemp -d); RC=0
  mkdir -p "$D/deploy"
  : > "$D/deploy/01_Schema.sql"; : > "$D/deploy/02_Seed.sql"
  run() {   # run <라벨> <기대exit> <Deploy.sql 내용>
    printf '%s\n' "$3" > "$D/Deploy.sql"
    # [X] 하위 셸에서 cd 해도 소용없다 — 스크립트가 자기 위치로 다시 cd 한다.
    #     경로를 **인자로** 넘겨야 임시 트리를 본다 (실측: 안 그러면 진짜 저장소를 재고 통과한다).
    DEPLOYDIR="$D/deploy" DEPLOYFILE="$D/Deploy.sql" LIVE=0 \
      "$SELF" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS SYNC-SELFTEST $1"
    else echo "FAIL SYNC-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  run '목록과 파일이 같으면 통과한다'      0 ':r $(DeployDir)\01_Schema.sql
:r $(DeployDir)\02_Seed.sql'
  run 'Deploy.sql 이 빠뜨린 파일을 잡는다'  1 ':r $(DeployDir)\01_Schema.sql'
  run 'Deploy.sql 의 유령 참조를 잡는다'    1 ':r $(DeployDir)\01_Schema.sql
:r $(DeployDir)\02_Seed.sql
:r $(DeployDir)\99_Ghost.sql'
  rm -f "$D/Deploy.sql" "$D/out.txt" "$D/deploy/01_Schema.sql" "$D/deploy/02_Seed.sql"
  rmdir "$D/deploy" "$D"
  exit $RC
fi

# ── SYNC-003 파일 집합. DB 없이 돈다 ────────────────────────────────────────
DISK=$(ls "$DEPLOYDIR" 2>/dev/null | grep -E '\.sql$' | sort)
REF=$(grep -oE '[0-9]+[a-z]?_[A-Za-z_]+\.sql' "$DEPLOYFILE" 2>/dev/null | sort -u)
if [ -z "$DISK" ] || [ -z "$REF" ]; then
  say FAIL "SYNC-003 $DEPLOYDIR/ 또는 $DEPLOYFILE 을 읽지 못했다 — 미실행은 PASS 가 아니다"
elif [ "$DISK" = "$REF" ]; then
  say PASS "SYNC-003 $DEPLOYFILE 이 부르는 파일 == $DEPLOYDIR/ 의 .sql $(echo "$DISK" | wc -l) 개"
else
  say FAIL "SYNC-003 배포 목록과 파일이 어긋난다"
  diff <(echo "$DISK") <(echo "$REF") | sed 's/^/    /'
fi

if [ "$LIVE" != "1" ]; then
  [ "$FAIL" -eq 0 ] && echo "== PASS (파일 집합만) =="
  exit $FAIL
fi

mkdir -p artifacts/logs
Q=artifacts/logs/_sync_q.sql
O=artifacts/logs/_sync_o.txt
LIVEF=artifacts/logs/_sync_live.txt
SRCF=artifacts/logs/_sync_src.txt
PYF=artifacts/logs/_sync_py.txt

# `CREATE   X` 를 `CREATE X` 로 되돌린 뒤 해시한다. 아래 파이썬이 원본에 같은 꼴을 만든다.
cat > "$Q" <<'SQL'
SET NOCOUNT ON;
SELECT o.name COLLATE Korean_Wansung_CI_AS
     + N'|'
     + CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', CONVERT(NVARCHAR(MAX),
         REPLACE(REPLACE(OBJECT_DEFINITION(o.object_id),
                 N'CREATE   PROCEDURE', N'CREATE PROCEDURE'),
                 N'CREATE   FUNCTION',  N'CREATE FUNCTION'))), 2)
  FROM sys.objects o
 WHERE o.is_ms_shipped = 0 AND o.type IN ('P','IF','FN','TF');
SQL
sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -h-1 -W -i "$Q" -o "$O" || {
  say FAIL "SYNC-000 DB 에 붙지 못했다 — 미실행은 PASS 가 아니다"; echo "== FAIL =="; exit 1; }
iconv -f UTF-16 -t UTF-8 "$O" | tr -d '\r' | sed 's/ *$//' \
  | grep -E '^[^|]+\|[0-9A-F]{64}$' | sort > "$LIVEF"

LIVEN=$(wc -l < "$LIVEF")
if [ "$LIVEN" -eq 0 ]; then
  say FAIL "SYNC-000 라이브 모듈을 하나도 읽지 못했다 — 미실행은 PASS 가 아니다"
  echo "== FAIL =="; exit 1
fi

# [X] PYTHONIOENCODING 을 빼면 콘솔 코드페이지(cp949)로 나가 한글 객체명이 깨지고
#     이름 대조가 전건 불일치가 된다 (실측).
DEPLOYDIR="$DEPLOYDIR" PYTHONIOENCODING=utf-8 python - <<'PY' > "$PYF" 2>&1
# 배포 원본에서 모듈 **배치**를 잘라 같은 방식으로 해시한다.
# 저장되는 것은 CREATE 부터가 아니라 배치 전체다 — 앞의 주석까지 들어간다(실측).
# 배치 경계는 줄 하나가 GO 뿐인 곳이고, 마지막 줄바꿈도 배치에 들어간다.
import io, os, re, hashlib

D = os.environ.get('DEPLOYDIR', 'deploy')
HEAD = re.compile(r'CREATE\s+(?:OR\s+ALTER\s+)?(PROCEDURE|FUNCTION)\s+\[dbo\]\.\[([^\]]+)\]', re.I)
out, dup, nofire = {}, [], []
for fn in sorted(os.listdir(D)):
    if not fn.endswith('.sql'):
        continue
    raw = io.open(os.path.join(D, fn), 'r', encoding='utf-8-sig', newline='').read()
    batches, cur = [], []
    for line in raw.split('\n'):
        if re.match(r'^\s*GO\s*$', line, re.I):
            batches.append(cur); cur = []
        else:
            cur.append(line)
    if cur:
        batches.append(cur)
    for b in batches:
        body = '\n'.join(b) + '\n'
        m = HEAD.search(body)
        if not m:
            continue
        name = m.group(2)
        # SQL Server 가 저장하는 꼴에 맞춘다: CREATE OR ALTER X -> CREATE X (공백 1칸).
        norm, n = HEAD.subn(lambda mm: 'CREATE ' + mm.group(1) + ' [dbo].[' + mm.group(2) + ']', body, count=1)
        if n != 1:
            nofire.append(name)
        if name in out:
            dup.append(name)
        out[name] = hashlib.sha256(norm.encode('utf-16-le')).hexdigest().upper()
for n in sorted(dup):
    print('DUP|' + n)
for n in sorted(nofire):
    print('NOFIRE|' + n)
for n in sorted(out):
    print('SRC|%s|%s' % (n, out[n]))
PY

grep '^SRC|' "$PYF" | sed 's/^SRC|//' | sort > "$SRCF"
SRCN=$(wc -l < "$SRCF")
DUPS=$(grep -c '^DUP|' "$PYF" || true)
NOFIRE=$(grep -c '^NOFIRE|' "$PYF" || true)

if [ "$SRCN" -eq 0 ]; then
  say FAIL "SYNC-000 $DEPLOYDIR/ 에서 모듈을 하나도 잘라내지 못했다 — 미실행은 PASS 가 아니다"
  sed 's/^/    /' "$PYF" | head -20; echo "== FAIL =="; exit 1
fi
[ "${DUPS:-0}" -gt 0 ]   && say FAIL "SYNC-001 $DEPLOYDIR/ 안에 같은 모듈이 두 번 정의돼 있다 — 뒤의 것이 이긴다"
[ "${NOFIRE:-0}" -gt 0 ] && say FAIL "SYNC-002 CREATE 정규화가 발동하지 않은 모듈이 있다 — 조용한 통과를 막는다"

# ── SYNC-001 이름 양방향 ────────────────────────────────────────────────────
ONLY_SRC=$(comm -23 <(cut -d'|' -f1 "$SRCF") <(cut -d'|' -f1 "$LIVEF"))
ONLY_DB=$(comm -13 <(cut -d'|' -f1 "$SRCF") <(cut -d'|' -f1 "$LIVEF"))
if [ -z "$ONLY_SRC" ] && [ -z "$ONLY_DB" ]; then
  say PASS "SYNC-001 모듈 이름 양방향 차집합 0 ($SRCN 건)"
else
  say FAIL "SYNC-001 모듈 이름이 어긋난다"
  [ -n "$ONLY_SRC" ] && echo "$ONLY_SRC" | sed 's/^/    deploy 에만: /'
  [ -n "$ONLY_DB" ]  && echo "$ONLY_DB"  | sed 's/^/    DB 에만:     /'
fi

# ── SYNC-002 본문 ──────────────────────────────────────────────────────────
# [X] 초안은 join 결과가 비어도 "차이 0건" 으로 읽어 PASS 를 냈다. 이름이 전건 어긋난 회차에서
#     그 fail-open 이 실제로 발동했다(실측). **맞춰본 건수**를 함께 판정한다.
JOINED=$(join -t'|' "$SRCF" "$LIVEF" | wc -l)
DIFF=$(join -t'|' "$SRCF" "$LIVEF" | awk -F'|' '$2 != $3 {print "    " $1}')
if [ "$JOINED" -ne "$SRCN" ]; then
  say FAIL "SYNC-002 본문을 맞춰본 모듈이 $JOINED 건뿐이다 (deploy $SRCN 건) — 미실행은 PASS 가 아니다"
elif [ -z "$DIFF" ]; then
  say PASS "SYNC-002 라이브 모듈 $SRCN 건의 본문이 $DEPLOYDIR/ 와 같다 (CREATE 한 자리만 정규화)"
else
  say FAIL "SYNC-002 라이브 DB 가 배포 원본과 다른 모듈을 갖고 있다 — 누가 DB 를 직접 고쳤다"
  echo "$DIFF"
fi

# ── SYNC-004 화면이 붙는 DB 가 방금 잰 그 DB 인가 ──────────────────────────
# [X] `05` §1.1 은 Database 이름만 확정하고 서버/인스턴스는 선언하지 않는다. `CFG-002` 도
#     Initial Catalog 만 본다. App.config 의 Data Source 를 바꾸면 화면은 다른 DB 를 보는데
#     이 게이트도 winforms 의 Db 통합시험도 그것을 몰랐다.
APPCFG=$(ls ../winforms/src/*/App.config 2>/dev/null | head -1)
if [ -z "$APPCFG" ]; then
  say FAIL "SYNC-004 App.config 를 찾지 못했다 — 미실행은 PASS 가 아니다"
else
  CS=$(tr -d '\r' < "$APPCFG" | grep -o 'connectionString="[^"]*"' | head -1)
  ASRV=$(echo "$CS" | sed -n 's/.*[Dd]ata [Ss]ource=\([^;"]*\).*/\1/p')
  ADB=$(echo "$CS" | sed -n 's/.*[Ii]nitial [Cc]atalog=\([^;"]*\).*/\1/p')
  if [ -z "$ASRV" ] || [ -z "$ADB" ]; then
    say FAIL "SYNC-004 App.config 에서 Data Source/Initial Catalog 를 못 읽었다 — 미실행은 PASS 가 아니다"
  elif [ "$ASRV" = "$SRV" ] && [ "$ADB" = "$DB" ]; then
    say PASS "SYNC-004 App.config 가 가리키는 DB 가 위에서 잰 그 DB 다 ($ASRV / $ADB)"
  else
    say FAIL "SYNC-004 화면은 다른 DB 를 본다 — App.config[$ASRV / $ADB] vs 게이트[$SRV / $DB]"
  fi
fi

rm -f "$Q" "$O" "$PYF" "$SRCF" "$LIVEF"
if [ "$FAIL" -eq 0 ]; then echo "== PASS 라이브 DB == $DEPLOYDIR/ =="; else echo "== FAIL =="; fi
exit $FAIL
