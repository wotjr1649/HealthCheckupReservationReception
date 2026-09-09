#!/usr/bin/env bash
# winforms 계열 회귀. DB 서버도 MSBuild 도 필요 없는 것만 모은다 — 어디서나 같은 판정이 나온다.
#
# database/scripts/test.sh 와 층이 다르다. 저쪽은 SQLEXPRESS 에 붙어 SP·Rule 을 돌린다.
# ../database/scripts/verify-winforms-unchanged.sh 는 여기서 부르지 않는다 — 그것은
# "database 계열이 winforms 를 안 건드렸는가" 를 보는 database 쪽 게이트라, winforms 를
# 정당하게 고치는 커밋에서는 red 가 되는 것이 정상이다 (winforms/AGENTS.md).
set -uo pipefail
cd "$(dirname "$0")/.."

FAIL=0
run() {
  echo
  echo "── $*"
  "$@" || FAIL=1
}

run ./scripts/verify-no-secret.sh selftest
run ./scripts/verify-no-secret.sh
run ./scripts/verify-appconfig-doc.sh selftest
run ./scripts/verify-appconfig-doc.sh
run ./scripts/verify-ui-db-matrix.sh selftest
run ./scripts/verify-ui-db-matrix.sh

echo
if [ "$FAIL" -eq 0 ]; then
  echo "=== winforms 회귀 PASS ==="
else
  echo "=== winforms 회귀 FAIL ==="
fi
exit $FAIL
