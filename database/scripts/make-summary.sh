#!/usr/bin/env bash
# T37 Step 2 — artifacts/reports/test-summary.txt 생성.
#   회귀 로그를 세어 한 장으로 접는다. **판정하지 않는다** — 판정은 test.sh 가 이미 했다.
#
# [X] 계획서 초안은 tests/*.log 만 셌다. 그러면 계약(SEL·PWR·RWR·CWR·OFF 110종)·
#     동시성(CON 8종)·Clean Rebuild 가 통째로 빠져 "총 테스트 건수" 가 실제의 절반이 된다.
# [X] NOT RUN 을 요약 **머리**에 세운다. 업무시간 밖 회차는 성공 경로를 돌지 못해
#     PASS 가 176 까지 떨어지는데, 숫자만 보면 통과한 회차처럼 읽힌다 (CLAUDE.md §10).
set -uo pipefail
cd "$(dirname "$0")/.."
OUT=artifacts/reports/test-summary.txt
mkdir -p artifacts/reports
RUNLOG="${1:-artifacts/logs/full_test_run.log}"

u() { iconv -f UTF-16 -t UTF-8 "$1" 2>/dev/null || cat "$1" 2>/dev/null; }
c() { grep -ac "$1" "$2" 2>/dev/null || true; }   # grep -c 는 0건일 때 exit 1 이다

{
  echo "=== Phase 4 테스트 요약 ==="
  echo "생성시각: $(date -Iseconds)"
  echo "회귀 로그: $RUNLOG"
  # 회차의 **자기 시각**을 함께 적는다. 시각을 옮겨 돌린 회차는 생성시각과 크게 어긋난다.
  [ -f "$RUNLOG" ] && grep -a -m1 '=== Deploy 시작' "$RUNLOG" | sed 's/^/회차 시각: /'
  if [ -f "$RUNLOG" ]; then
    echo
    echo "--- 이 회차의 판정 (test.sh 출력 그대로) ---"
    grep -a -E '^(=== 실패|!! NOT RUN|NOT RUN )' "$RUNLOG" || echo "(판정 줄을 찾지 못했습니다)"
    echo
    printf 'PASS=%s  FAIL=%s  SKIP=%s\n' "$(c '^PASS' "$RUNLOG")" "$(c '^FAIL' "$RUNLOG")" "$(c '^SKIP' "$RUNLOG")"
  else
    echo "(회귀 로그가 없습니다 — ./scripts/test.sh > $RUNLOG 2>&1 을 먼저 돌리십시오)"
  fi

  echo
  echo "--- SQL 시험 파일별 (이 회차가 쓴 것만) ---"
  # [X] artifacts/logs/ 는 누적된다. 옛 RED 회차의 test_01_red.log 가 남아 있어 와일드카드로
  #     세면 이 회차와 무관한 FAIL=1 이 섞인다 (실측).
  # [X] mtime 비교도 안 된다 — RUNLOG 는 회귀가 끝날 때까지 계속 쓰여 **가장 새 파일**이 되고,
  #     그러면 모든 시험 로그가 '오래된 것' 으로 걸러져 요약이 통째로 빈다.
  #     test.sh 가 자기가 쓴 목록을 _manifest.txt 에 남긴다. 그것만 읽는다.
  MAN=artifacts/logs/_manifest.txt
  [ -f "$MAN" ] || echo "(manifest 없음 — test.sh 를 다시 돌리십시오)"
  while IFS= read -r l; do
    [ -f "$l" ] || continue
    t=$(u "$l")
    printf '%-34s PASS=%-4s FAIL=%-4s SKIP=%s\n' "$(basename "$l")" \
      "$(echo "$t" | grep -c '^PASS' || true)" \
      "$(echo "$t" | grep -c '^FAIL' || true)" \
      "$(echo "$t" | grep -c '^SKIP' || true)"
  done < "${MAN:-/dev/null}"

  echo
  echo "--- 동시성 (CON) 시나리오별 최신 회차 ---"
  # [X] conc_*_verify.log 를 통째로 읽으면 고치는 동안 남은 옛 회차까지 나온다.
  #     실측에서 CON-004 가 세 번 찍혔다. 파일명이 conc_<RUN>_<SCEN>_verify.log 이고
  #     RUN 이 YYYYmmdd_HHMMSS 라 사전순 정렬이 곧 시간순이다 — 시나리오마다 마지막 하나만 본다.
  for sc in 1 2 3 4 5 6 7 8; do
    last=$(ls artifacts/logs/conc_*_"${sc}"_verify.log 2>/dev/null | sort | tail -1)
    if [ -n "$last" ]; then
      u "$last" | grep -E "^(PASS|FAIL) CON" || echo "CON-00$sc  (판정 줄 없음)"
    else
      echo "NOT RUN CON-00$sc  회차 로그 없음 — CON 은 업무시간 창에서만 돈다 (스펙 §38.7)"
    fi
  done

  for f in artifacts/reports/contract-verify.txt artifacts/reports/no-secret.txt \
           artifacts/reports/tsql-allowlist.txt artifacts/reports/clean-rebuild.txt \
           artifacts/reports/object-inventory.txt; do
    [ -f "$f" ] || continue
    echo; echo "--- $f ---"; cat "$f"
  done
} > "$OUT"

head -14 "$OUT"
echo "..."
echo "생성: $OUT ($(wc -l < "$OUT") 줄)"
