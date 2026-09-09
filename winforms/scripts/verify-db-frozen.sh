#!/usr/bin/env bash
# Phase 5 동결 — 계약과 DB 배포본을 더 이상 고치지 않는다 (사용자 확정 2026-09-10).
#
# [!] **선언은 썩는다.** 이 저장소가 그것으로 여러 번 물렸다 (ROOT `AGENTS.md` §6).
#     "이제 DB 는 안 고친다" 를 문장으로만 두면 다음 회차가 조용히 어긴다. 게이트로 둔다.
#
#   DBF-001  docs/baseline/ 봉인 7건이 그대로다   ../database/scripts/verify-baseline.sh 에 위임
#   DBF-002  database/deploy/ 와 배포 진입점이 그대로다   아래 매니페스트와 대조
#
# [I] **해시를 여기 두 번 적지 않는다.** `docs/baseline/` 은 `verify-baseline.sh` 가 이미 세는
#     곳이므로 그것을 부르고 결과만 받는다. 여기가 갖는 것은 `deploy/` 쪽 매니페스트 하나다.
#
# [!] **이 게이트가 red 라는 것은 "고치면 안 된다" 가 아니라 "고쳤으면 회차를 붙여라" 다.**
#     정당하게 DB 를 여는 날에는 재봉인 절차(ROOT `AGENTS.md` §2.2)를 밟고 `init` 으로
#     매니페스트를 다시 뜬 다음 **같은 커밋에** 넣는다. winforms 매니페스트와 같은 규칙이다.
#
#   ./scripts/verify-db-frozen.sh init       매니페스트를 다시 뜬다 (동결 해제가 아니라 갱신)
#   ./scripts/verify-db-frozen.sh selftest   변조를 실제로 잡는지 시험한다
set -uo pipefail
cd "$(dirname "$0")/.."

: "${DBDIR:=../database}"
: "${MANIFEST:=artifacts/db-frozen-manifest.txt}"
# 봉인 판정은 위임한다. 자체시험이 임시 트리를 쓰므로 경로를 따로 받는다 —
# 그래도 **진짜 게이트를 부른다**: 자체시험이 부르는 것과 회귀가 부르는 것이 같아야 한다.
: "${BASELINE_SH:=$DBDIR/scripts/verify-baseline.sh}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# 동결 대상 — 배포되는 것과 배포를 부르는 것. 시험·게이트 스크립트는 대상이 아니다
# (그것은 Phase 5 가 정당하게 늘린다).
list_files() {
  ( cd "$DBDIR" 2>/dev/null || return 1
    ls deploy/*.sql 2>/dev/null
    ls Deploy.sql Rebuild.sql 2>/dev/null ) | sort
}

make_manifest() {
  ( cd "$DBDIR" 2>/dev/null || return 1
    list_files_rel=$( ( ls deploy/*.sql; ls Deploy.sql Rebuild.sql ) 2>/dev/null | sort )
    for f in $list_files_rel; do
      printf '%s  %s\n' "$(sha256sum "$f" | cut -d' ' -f1)" "$f"
    done )
}

if [ "${1:-check}" = "init" ]; then
  mkdir -p "$(dirname "$MANIFEST")"
  make_manifest > "$MANIFEST" || { echo "FAIL 매니페스트를 뜨지 못했다"; exit 1; }
  echo "OK 매니페스트 재생성 $(wc -l < "$MANIFEST") 건 → $MANIFEST"
  echo "   [!] 같은 커밋에 넣어라. 회차 이름 없이 이것만 바꾸면 동결이 뜻을 잃는다."
  exit 0
fi

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  SELF=$(cd "$(dirname "$0")" && pwd)/$(basename "$0")
  mkdir -p "$D/db/deploy"
  printf 'CREATE TABLE x;\n' > "$D/db/deploy/01_Schema.sql"
  printf ':r deploy\\01_Schema.sql\n' > "$D/db/Deploy.sql"
  printf 'DROP DATABASE x;\n' > "$D/db/Rebuild.sql"
  REALBASE=$(cd "$(dirname "$0")/.." && cd "$DBDIR/scripts" && pwd)/verify-baseline.sh
  DBDIR="$D/db" MANIFEST="$D/m.txt" BASELINE_SH="$REALBASE" "$SELF" init > /dev/null 2>&1
  run() {   # run <라벨> <기대exit>
    DBDIR="$D/db" MANIFEST="$D/m.txt" BASELINE_SH="$REALBASE" "$SELF" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS DBF-SELFTEST $1"
    else echo "FAIL DBF-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  run '뜬 그대로면 통과한다' 0
  printf 'CREATE TABLE x; -- 한 글자\n' > "$D/db/deploy/01_Schema.sql"
  run '배포 원본 변조를 잡는다' 1
  printf 'CREATE TABLE x;\n' > "$D/db/deploy/01_Schema.sql"
  printf 'SELECT 1;\n' > "$D/db/deploy/02_New.sql"
  run '새로 생긴 배포 파일을 잡는다' 1
  rm -f "$D/db/deploy/02_New.sql"
  rm -f "$D/db/deploy/01_Schema.sql"
  run '사라진 배포 파일을 잡는다' 1
  rm -f "$D/db/Deploy.sql" "$D/db/Rebuild.sql" "$D/m.txt" "$D/out.txt"
  rmdir "$D/db/deploy" "$D/db" "$D"
  exit $RC
fi

# ── DBF-001 봉인 문서 ───────────────────────────────────────────────────────
if [ -x "$BASELINE_SH" ]; then
  BL=$("$BASELINE_SH" 2>&1 | tail -1)
  if "$BASELINE_SH" > /dev/null 2>&1; then
    say PASS "DBF-001 봉인 계약 $(echo "$BL" | tr -d '= ') — 세는 곳은 verify-baseline.sh 하나다"
  else
    say FAIL "DBF-001 봉인 계약이 바뀌었다 — 재봉인 회차 없이 열지 않는다"
    "$BASELINE_SH" 2>&1 | grep -vE '^OK ' | sed 's/^/    /'
  fi
else
  say FAIL "DBF-001 verify-baseline.sh 를 찾지 못했다: $BASELINE_SH — 미실행은 PASS 가 아니다"
fi

# ── DBF-002 배포본 ─────────────────────────────────────────────────────────
if [ ! -f "$MANIFEST" ]; then
  say FAIL "DBF-002 매니페스트가 없다: $MANIFEST — './scripts/verify-db-frozen.sh init' 로 뜬다"
else
  NOW=$(make_manifest)
  if [ -z "$NOW" ]; then
    say FAIL "DBF-002 $DBDIR 의 배포본을 읽지 못했다 — 미실행은 PASS 가 아니다"
  else
    D1=$(diff <(sort "$MANIFEST") <(echo "$NOW" | sort) || true)
    if [ -z "$D1" ]; then
      say PASS "DBF-002 배포본 $(echo "$NOW" | wc -l) 건이 동결 시점 그대로다 (deploy/*.sql · Deploy.sql · Rebuild.sql)"
    else
      say FAIL "DBF-002 배포본이 바뀌었다 — DB 를 여는 것은 회차를 붙이는 일이다 (ROOT AGENTS.md §2.2)"
      echo "$D1" | sed 's/^/    /'
    fi
  fi
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS 계약·DB 동결 =="; else echo "== FAIL =="; fi
exit $FAIL
