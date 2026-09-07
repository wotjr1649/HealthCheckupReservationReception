#!/usr/bin/env bash
# G13-b — 허용 T-SQL 목록(스펙 §9.2) 밖 기능이 배포·시험 원본에 0건인지 스캔한다.
#
# [X] plans/08 T37 Step 4 의 일회성 grep 은 **항상 FAIL 한다.** 실측 27건이 전부 오탐이었다.
#     1) 금지를 설명하는 주석이 걸린다.
#        deploy/06:12  "문자열 조립은 WHILE 루프로 한다 — STRING_AGG·FOR XML PATH 는 … 밖이고"
#        tests/12:49   "STRING_SPLIT 은 허용목록 밖이다 (06 §9.2)"
#        tools/verify-docs.js:126  [/FOR\s+XML\s+PATH/i, 'FOR XML PATH — … 허용목록 밖']
#     2) \bTRIM\( 가 JavaScript 의 .trim( 를 전부 잡는다. \b 는 '.' 과 't' 사이에 성립한다.
#        tools/ 는 T-SQL 이 아니다 — §9.2 는 T-SQL 허용목록이므로 검사 대상이 아니다.
#     -> 대상을 .sql 로 한정하고, 주석(-- 이후)을 지운 뒤 검사한다.
#        LTRIM(·RTRIM( 은 \b 가 성립하지 않아 원래 오탐이 아니다.
#
# set -e 를 쓰지 않는다 — 실패한 줄에서 셸이 끝나 판정도 로그 출력도 안 된다 (CLAUDE.md §6).
set -uo pipefail
cd "$(dirname "$0")/.."

BAN='STRING_SPLIT|STRING_AGG|OPENJSON|FOR JSON|FOR XML|JSON_VALUE|JSON_QUERY|JSON_MODIFY'
BAN="$BAN"'|SESSION_CONTEXT|CONTEXT_INFO|AT TIME ZONE|CONCAT_WS|TRANSLATE\(|DATEDIFF_BIG'
BAN="$BAN"'|COMPRESS\(|DECOMPRESS\(|GREATEST\(|LEAST\(|GENERATE_SERIES|APPROX_COUNT_DISTINCT'
BAN="$BAN"'|REGEXP_|\bTRIM\(|DECLARE +[A-Za-z_@]+ +CURSOR|\bCOLLATE\b|\bEXEC *\(|sp_executesql'

OUT=artifacts/reports/tsql-allowlist.txt
mkdir -p artifacts/reports
: > "$OUT"
FAILED=0
TOTAL=0
FILES=0

for f in Deploy.sql Rebuild.sql deploy/*.sql tests/*.sql tests/contract/*.sql; do
  [ -f "$f" ] || continue
  FILES=$((FILES + 1))
  # 주석을 지운 뒤 검사한다. 금지 기능을 **설명하는** 문장은 위반이 아니다.
  # CREATE DATABASE 의 COLLATE 는 DB 정렬 지정이라 설계가 요구한다 (Korean_Wansung_CI_AS).
  # §9.2 가 막는 것은 **식 수준** COLLATE 다 - 질의에서 정렬을 갈아끼워 비교 의미를 바꾸는 쪽.
  hits=$(sed 's/--.*$//' "$f" | grep -niE "$BAN" | grep -viE 'CREATE DATABASE .*COLLATE' || true)
  if [ -n "$hits" ]; then
    echo "HIT  $f" >> "$OUT"
    echo "$hits" | sed 's/^/     /' >> "$OUT"
    TOTAL=$((TOTAL + $(echo "$hits" | grep -c . )))
    FAILED=1
  fi
done

# CREATE OR ALTER 는 배포 배관 전용 허용이다 (스펙 §9.2 예외 · D4-004b).
# deploy/ · Deploy.sql · Rebuild.sql 밖에 있으면 결함이다.
COA=$(grep -rl 'CREATE OR ALTER' tests/ tools/ 2>/dev/null || true)
if [ -n "$COA" ]; then
  echo "HIT  CREATE OR ALTER 가 배포 밖에 있다:" >> "$OUT"
  echo "$COA" | sed 's/^/     /' >> "$OUT"
  FAILED=1
else
  echo 'PASS G13-b2 CREATE OR ALTER 는 배포 원본 안에만 있다' >> "$OUT"
fi

if [ "$FAILED" -eq 0 ]; then
  echo "PASS G13-b 허용목록 밖 T-SQL 0건 (.sql $FILES 개 · 주석 제외)" >> "$OUT"
else
  echo "FAIL G13-b 허용목록 밖 후보 $TOTAL 줄" >> "$OUT"
fi
# (a) 배포·테스트 성공 · (c) §9.2 준수는 이 스크립트가 판정할 수 없다. 보고서에 따로 적는다.
echo 'INFO G13 은 세 갈래다 — (a) 배포·시험 성공 (b) 이 스캔 (c) §9.2 준수는 REVIEWED (자동 판정 불가)' >> "$OUT"

cat "$OUT"
exit $FAILED
