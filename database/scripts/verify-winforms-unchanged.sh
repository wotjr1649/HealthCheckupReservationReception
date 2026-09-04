#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
MANIFEST="artifacts/reports/winforms-manifest.txt"

gen() {
  find ../winforms -type f \
    -not -path '*/obj/*' -not -path '*/bin/*' \
    -not -path '*/.vs/*'  -not -path '*/TestResults/*' \
    -print0 | sort -z | xargs -0 sha256sum
}

if [ "${1:-check}" = "init" ]; then
  gen > "$MANIFEST"
  echo "manifest 생성: $(wc -l < "$MANIFEST") 파일"
else
  gen > /tmp/winforms-now.txt
  if diff -u "$MANIFEST" /tmp/winforms-now.txt; then
    echo "PASS WinForms 변경 0건"
  else
    echo "FAIL WinForms 변경 감지"; exit 1
  fi
fi
