#!/usr/bin/env bash
# winforms 계열 secret 게이트.
# database/scripts/verify-no-secret.sh 는 Deploy.sql·deploy/·scripts/·tests/·tools/·artifacts/ 만
# 훑는다(실측). winforms/** 는 그 범위 밖이라 연결문자열을 보는 게이트가 하나도 없었다.
# 판정 방식은 그 스크립트와 같다 — 근거 주석은 거기 있고 여기 베끼지 않는다.
#
# set -e 를 쓰지 않는다: 일치한 줄에서 셸이 끝나면 판정도 출력도 안 나온다.
set -uo pipefail
cd "$(dirname "$0")/.."

# [X] 낱말만 찾으면 이 스크립트 자신이 첫 HIT 가 된다. 값을 동반한 할당만 찾는다.
#     패턴 안의 [[:space:]] 자체가 자기일치를 깨뜨린다 — "user" 뒤에 오는 것은 공백이 아니라 '[' 다.
PAT='(password|passwd|pwd|secret|api[_-]?key|apikey|access[_-]?token|bearer|credential)[[:space:]]*[:=][[:space:]]*[^[:space:];"'"'"'<>)]{3,}'
PAT2='integrated[[:space:]]+security[[:space:]]*=[[:space:]]*(false|no)|user[[:space:]]+id[[:space:]]*=|\buid[[:space:]]*='

# selftest — "금지 연결문자열을 실제로 잡는가" 를 재현 가능하게 판정한다.
# [X] 일회성으로 임시파일을 만들어 FAIL 을 보고 지우면 증거가 남지 않는다
#     (verify-winforms-unchanged.sh 의 같은 [X] 주석). 하위명령으로 고정한다.
if [ "${1:-check}" = "selftest" ]; then
  T=$(mktemp); RC=0
  # 실제 금지형: 통합인증을 끄고 계정을 싣는다.
  # [X] 이 문자열을 그대로 적으면 **이 스크립트가 자기 게이트의 첫 HIT** 가 된다(실측).
  #     키에 값이 붙는 자리를 %s 로 갈라 두고 실행 시점에만 붙인다.
  printf 'Data Source=.;Integrated Security%sFalse;User Id%ssa;Password%shunter2\n' = = = > "$T"
  n=$(grep -IicE "$PAT" "$T" || true)
  m=$(grep -IicE "$PAT2" "$T" || true)
  if [ "$n" -gt 0 ] && [ "$m" -gt 0 ]; then
    echo "PASS WF-SEC-SELFTEST 금지 연결문자열을 두 패턴 모두 잡았다 (PAT $n · PAT2 $m)"
  else
    echo "FAIL WF-SEC-SELFTEST 금지 연결문자열을 놓쳤다 (PAT $n · PAT2 $m)"; RC=1
  fi
  # 무해한 산문은 잡지 않아야 한다 — 초판이 자기 자신을 세었던 함정.
  printf 'this rule forbids user id and uid in a connection string\n' > "$T"
  z=$(( $(grep -IicE "$PAT" "$T") + $(grep -IicE "$PAT2" "$T") ))
  if [ "$z" -eq 0 ]; then
    echo "PASS WF-SEC-SELFTEST 값 없는 산문은 세지 않는다"
  else
    echo "FAIL WF-SEC-SELFTEST 값 없는 산문을 $z 줄 세었다"; RC=1
  fi
  rm -f "$T"
  exit $RC
fi

# git 이 나르는 것만 센다 — bin/obj/.vs 가 자동으로 빠지고 새 클론에서도 같은 판정이 나온다.
# (verify-winforms-unchanged.sh 가 같은 이유로 같은 목록을 쓴다.)
mapfile -t FILES < <(git -C .. ls-files --cached --others --exclude-standard -- winforms | sed 's|^winforms/||')

HITS=0
for f in "${FILES[@]}"; do
  [ -f "$f" ] || continue
  n=$(grep -IicE "$PAT" "$f" 2>/dev/null || true)
  n=$((n + $(grep -IicE "$PAT2" "$f" 2>/dev/null || true)))
  if [ "${n:-0}" -ne 0 ]; then
    echo "HIT  winforms/$f  ($n 줄)"
    HITS=$((HITS + n))
  fi
done

echo "검사 ${#FILES[@]} 개 · HIT $HITS"
if [ "$HITS" -eq 0 ]; then
  echo 'PASS WF-SEC-010 winforms 에 자격증명 0건'
  exit 0
fi
echo "FAIL WF-SEC-010 secret 후보 $HITS 줄이 검출됐다"
exit 1
