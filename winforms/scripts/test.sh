#!/usr/bin/env bash
# winforms 계열 회귀. DB 서버도 MSBuild 도 필요 없는 것만 모은다 — 어디서나 같은 판정이 나온다.
#
# database/scripts/test.sh 와 층이 다르다. 저쪽은 SQLEXPRESS 에 붙어 SP·Rule 을 돌린다.
# ../database/scripts/verify-winforms-unchanged.sh 는 여기서 부르지 않는다 — 그것은
# "database 계열이 winforms 를 안 건드렸는가" 를 보는 database 쪽 게이트라, winforms 를
# 정당하게 고치는 커밋에서는 red 가 되는 것이 정상이다 (winforms/AGENTS.md).
# [X] **게이트가 간헐적으로 멈춘다. 원인은 아직 모른다** (2026-09-09, 07 §12.5).
#     세 번 관측했고 전부 verify-screen-design.js 였다 — 두 번은 `selftest`, 한 번은 그냥
#     `check` 다. 배경 실행에서도 전경 실행에서도 났다. 실측: CPU 시간이 멈춘 채
#     state=S(sleeping) 이므로 도는 것이 아니라 **I/O 에서 잠들어 있다.** 같은 명령이
#     대개는 1초에 끝난다.
#
#     원인을 못 짚었으므로 **증상에 경계를 둔다.** 경계가 없으면 멈춘 게이트 하나가 회귀
#     전체를 영원히 잡고, 그 사이 아무 판정도 나오지 않는다.
#
# [X] 배경으로 돌린 "통과" 는 아무것도 증명하지 않는다. `./scripts/test.sh | tail -60` 로
#     감싸면 파이프라인의 종료코드는 tail 것이고 tail 은 stdin 이 닫히면 0 을 낸다 —
#     **출력 0바이트에 exit 0** 이 나온다. 실제로 두 번 그렇게 보고됐다. 판정은 출력을 본다.
set -uo pipefail
cd "$(dirname "$0")/.."

# 게이트 하나가 쓸 수 있는 시간. 가장 느린 것이 selftest 이고 대개 20초 안이다.
GATE_TIMEOUT=${GATE_TIMEOUT:-300}

FAIL=0
run() {
  echo
  echo "── $*"
  # -k: TERM 을 무시하면 10초 뒤 KILL 한다. I/O 에 잠든 프로세스는 TERM 을 못 받기도 한다.
  timeout -k 10 "$GATE_TIMEOUT" "$@"
  rc=$?
  if [ "$rc" -eq 124 ] || [ "$rc" -eq 137 ]; then
    echo "FAIL 시간 초과 — ${GATE_TIMEOUT}초 안에 끝나지 않았다: $*"
    FAIL=1
  elif [ "$rc" -ne 0 ]; then
    FAIL=1
  fi
}

run ./scripts/verify-no-secret.sh selftest
run ./scripts/verify-no-secret.sh
run ./scripts/verify-contract-names.sh selftest
run ./scripts/verify-contract-names.sh
run ./scripts/verify-ui-baseline.sh selftest
run ./scripts/verify-ui-baseline.sh
run ./scripts/verify-dbcode.sh selftest
run ./scripts/verify-dbcode.sh
run ./scripts/verify-layering.sh selftest
run ./scripts/verify-layering.sh
run ./scripts/verify-rs-columns.sh selftest
run ./scripts/verify-rs-columns.sh
run node tools/verify-screen-design.js selftest
run node tools/verify-screen-design.js
run ./scripts/verify-ui-db-matrix.sh selftest
run ./scripts/verify-ui-db-matrix.sh

echo
if [ "$FAIL" -eq 0 ]; then
  echo "=== winforms 회귀 PASS ==="
else
  echo "=== winforms 회귀 FAIL ==="
fi
exit $FAIL
