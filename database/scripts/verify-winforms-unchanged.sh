#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
MANIFEST="artifacts/reports/winforms-manifest.txt"

gen() {
  # [X] find 로 훑으면 .gitignore 대상까지 센다. 제외 목록을 손으로 적고 있었는데
  #     winforms/artifacts/logs/ 가 빠져 있어 Phase 5 가 빌드하는 순간 manifest 가
  #     churn 한다. git 이 나르는 것만 센다 — 무시 대상이 자동으로 빠지고, manifest 가
  #     새 클론에서도 그대로 재현된다.
  git -C .. ls-files --cached --others --exclude-standard -z -- winforms \
    | sed -z 's|^|../|' | sort -z | xargs -0 sha256sum
}

# selftest — "변조를 실제로 감지하는가" 를 재현 가능하게 판정한다 (plans/01 T02 Step 4).
# [X] 그 Step 은 manifest 를 변조해 FAIL 을 보는 일회성 관측이었고 증거가 남지 않았다.
#     커밋된 manifest 를 건드리지 않고 **사본**을 변조해 같은 비교를 돌려 판정한다.
if [ "${1:-check}" = "selftest" ]; then
  TMP=$(mktemp); TAMPER=$(mktemp); RC=0
  gen > "$TMP"
  # 해시 한 글자만 바꾼다. 파일 목록은 그대로이고 내용만 다른 상태다.
  awk 'NR==1{ h=substr($1,1,63); c=substr($1,64,1); nc=(c=="0"?"1":"0"); $1=h nc } {print}' "$TMP" > "$TAMPER"
  if diff -q "$TMP" "$TAMPER" > /dev/null; then
    echo "FAIL WF-SELFTEST 변조본이 원본과 같다 — 시험이 성립하지 않는다"; RC=1
  elif diff -q "$TAMPER" "$TMP" > /dev/null; then
    echo "FAIL WF-SELFTEST 해시가 달라졌는데 diff 가 같다고 했다"; RC=1
  else
    echo "PASS WF-SELFTEST manifest 한 글자 변조를 diff 가 감지했다"
  fi
  # 파일이 사라진 경우도 본다
  tail -n +2 "$TMP" > "$TAMPER"
  if diff -q "$TAMPER" "$TMP" > /dev/null; then
    echo "FAIL WF-SELFTEST 파일 1개 누락을 감지하지 못했다"; RC=1
  else
    echo "PASS WF-SELFTEST 파일 1개 누락을 diff 가 감지했다"
  fi
  rm -f "$TMP" "$TAMPER"
  exit $RC
fi

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
