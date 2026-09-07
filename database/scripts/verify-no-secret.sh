#!/usr/bin/env bash
# SEC-010 · RBD-010 — 배포 원본·로그·보고서에 비밀번호·연결 secret 이 0건인지 스캔한다 (스펙 §39.3).
# set -e 를 쓰지 않는다 — 실패한 줄에서 셸이 끝나 판정도 로그 출력도 안 된다 (CLAUDE.md §6).
set -uo pipefail
cd "$(dirname "$0")/.."

# [X] grep -q 를 쓰지 않는다. 첫 일치에서 조기 종료하므로 앞단 iconv 가 SIGPIPE(141) 로 죽고,
#     pipefail 아래에서 그 파이프라인이 "실패" 로 보여 조건이 거짓이 된다 — secret 이 파일
#     앞쪽에 있을수록 놓친다. 위치 의존적인 판정이었다 (06 §45.1 결함 #1 실측).
#     개수를 변수로 받아 0 인지 본다.
#
# [X] 낱말만 찾으면 **자기 자신이 걸린다.** 초판은 secret·password 를 낱말로 찾아
#     이 스크립트(7줄)·test.sh(2줄)·tests/14(4줄)·자기 보고서(1줄)를 전부 HIT 로 세었다.
#     전부 "secret 을 검사한다" 고 말하는 산문이지 secret 이 아니다.
#     값을 동반한 할당만 찾는다 — 키워드 뒤에 = 또는 : 가 오고 그 뒤에 실제 값이 있어야 한다.
#     이 저장소는 어디서나 sqlcmd -E(통합인증)만 쓰므로 연결문자열에 자격증명이 0건이어야 한다.
PAT='(password|passwd|pwd|secret|api[_-]?key|apikey|access[_-]?token|bearer|credential)[[:space:]]*[:=][[:space:]]*[^[:space:];"'"'"'<>)]{3,}'
PAT2='integrated[[:space:]]+security[[:space:]]*=[[:space:]]*(false|no)|user[[:space:]]+id[[:space:]]*=|\buid[[:space:]]*='

OUT=artifacts/reports/no-secret.txt
mkdir -p artifacts/reports
: > "$OUT"
FAILED=0
TOTAL=0

count_in() {                  # count_in <파일> -> stdout 에 일치 줄 수
  local f="$1" n
  # UTF-16 판정은 BOM 으로 결정한다. iconv 결과가 0 이라고 원본을 다시 훑으면
  # 같은 파일을 두 인코딩으로 세어 거짓 HIT 이 생긴다.
  if [ "$(head -c 2 "$f" | od -An -tx1 | tr -d ' \n')" = "fffe" ]; then
    n=$(iconv -f UTF-16 -t UTF-8 "$f" 2>/dev/null | grep -icE "$PAT" || true)
    n=$((n + $(iconv -f UTF-16 -t UTF-8 "$f" 2>/dev/null | grep -icE "$PAT2" || true)))
  else
    n=$(grep -icE "$PAT" "$f" 2>/dev/null || true)
    n=$((n + $(grep -icE "$PAT2" "$f" 2>/dev/null || true)))
  fi
  echo "${n:-0}"
}

scan() {                      # scan <라벨> <파일...>
  local label="$1"; shift
  local hits=0 files=0 f n
  for f in "$@"; do
    [ -f "$f" ] || continue
    files=$((files + 1))
    n=$(count_in "$f")
    if [ "$n" -ne 0 ]; then
      echo "HIT  $f  ($n 줄)" >> "$OUT"
      hits=$((hits + n))
    fi
  done
  echo "$label 검사 $files 개 · HIT $hits" >> "$OUT"
  TOTAL=$((TOTAL + hits))
  [ "$hits" -ne 0 ] && FAILED=1
  return 0
}

# 배포 원본까지 본다. 로그만 보면 원본에 심긴 secret 을 영영 놓친다.
# 이 스크립트 자신과 자기 보고서도 제외하지 않는다 — 값을 동반한 패턴이라 자기참조로 걸리지 않는다.
scan '배포 원본'   Deploy.sql Rebuild.sql deploy/*.sql
scan '실행 스크립트' scripts/*.sh scripts/*.sql
scan '시험 원본'   tests/*.sql tests/contract/*.sql
scan '도구'        tools/*.js tools/*.json
scan '로그'        artifacts/logs/*.log artifacts/logs/*.txt
scan '보고서'      artifacts/reports/*.txt

if [ "$FAILED" -eq 0 ]; then
  echo 'PASS SEC-010 배포 원본·로그·보고서에 secret 0건' >> "$OUT"
else
  echo "FAIL SEC-010 secret 후보 $TOTAL 줄이 검출됐다" >> "$OUT"
fi

cat "$OUT"
exit $FAILED
