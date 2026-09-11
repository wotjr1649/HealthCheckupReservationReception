#!/usr/bin/env bash
# 운영기준(04 §8.7) ↔ 00 ↔ 실물 DB.
#
# [!] R13 이 운영시간·마감시각을 SQL 리터럴에서 테이블로 옮겼다. 그것이 **정책을 설정으로
#     바꾸는 것이 아니라는 것**을 지키는 게이트다. 06 §43-21 이 정원 20 에 대해 세운 기준
#     ("00 이 정책 수치로 확정 · 관리자 CRUD 는 §8.2 밖")을 이 값이 어떻게 지나는지를 여기서 잰다.
#
#   OPR-G1  00 CP-04 에서 운영시작·종료를 읽는다 — 못 읽으면 통과가 아니라 FAIL
#   OPR-G2  00 §3 마감표에서 넷을 읽는다 — 못 읽으면 FAIL
#   OPR-G3  02_Seed.sql 의 값이 00 과 같다
#   OPR-G4  배포된 [운영기준] 1행이 00 과 같다
#
# [X] G4 는 **복원 판정을 겸한다.** 시험이 창을 넓혔다 되돌리지 않으면 이후 회차가
#     "다른 제품을 시험한 PASS" 가 된다 (06 §43-14). test.sh 끝에서 돌린다.
# [I] 관리 CRUD SP 가 없다는 것은 LAY/허용목록이 아니라 05 §1.3 의 SP 20 개 목록이 지킨다 —
#     운영기준을 바꾸는 SP 가 생기면 그 표에 나타나야 하고 UIDB-002 가 그것을 본다.
set -uo pipefail
cd "$(dirname "$0")/.."

: "${DOC:=../docs/baseline/00_Project_Policy.md}"
: "${SEED:=deploy/02_Seed.sql}"
: "${SRV:=.\\SQLEXPRESS}"
: "${DB:=HealthCheckupReservationReceptionDb}"
: "${LIVE:=1}"
FAIL=0
say() { echo "$1 $2"; [ "$1" = FAIL ] && FAIL=1; return 0; }

# 00 CP-04 — `09:00 <= 현재시각 < 18:00`
read_hours() {
  tr -d '\r' < "$DOC" | sed -n 's/.*CP-04.*운영시간.*`\([0-9][0-9]:[0-9][0-9]\) *<= *현재시각 *< *\([0-9][0-9]:[0-9][0-9]\)`.*/\1 \2/p' | head -1
}

# 00 §3 마감표 — | **평일 오전** | 10:00 | 11:00 |
read_cutoff() {
  tr -d '\r' < "$DOC" \
    | sed -n 's/^| *\*\*평일 '"$1"'\*\* *| *\([0-9][0-9]:[0-9][0-9]\) *| *\([0-9][0-9]:[0-9][0-9]\) *|.*/\1 \2/p' | head -1
}

if [ "${1:-check}" = "selftest" ]; then
  D=$(mktemp -d); RC=0
  GOOD='| **CP-04** | 운영시간 | 업무 운영시간은 `09:00 <= 현재시각 < 18:00`으로 한다. |
| 구분 | 당일예약 마감 | 접수 마감 |
| **평일 오전** | 10:00 | 11:00 |
| **평일 오후** | 15:00 | 16:00 |'
  SEEDOK="VALUES (1, '09:00:00', '18:00:00', '10:00:00', '15:00:00', '11:00:00', '16:00:00');"
  run() { # run <라벨> <기대exit> <doc> <seed>
    printf '%s\n' "$3" > "$D/doc.md"
    printf '%s\n' "$4" > "$D/seed.sql"
    DOC="$D/doc.md" SEED="$D/seed.sql" LIVE=0 "$0" check > "$D/out.txt" 2>&1
    local got=$?
    if [ "$got" -eq "$2" ]; then echo "PASS OPR-SELFTEST $1"
    else echo "FAIL OPR-SELFTEST $1 (기대 exit $2, 실제 $got)"; sed 's/^/    /' "$D/out.txt"; RC=1; fi
  }
  run '00 과 Seed 가 같으면 통과한다'   0 "$GOOD" "$SEEDOK"
  run 'Seed 의 운영시간 변조를 잡는다'  1 "$GOOD" "${SEEDOK/'09:00:00'/'08:00:00'}"
  run 'Seed 의 마감시각 변조를 잡는다'  1 "$GOOD" "${SEEDOK/'16:00:00'/'17:00:00'}"
  run '00 을 못 읽으면 FAIL 이다'      1 '운영시간 표가 없다' "$SEEDOK"
  run 'Seed 를 못 읽으면 FAIL 이다'    1 "$GOOD" '-- INSERT 가 없다'
  rm -f "$D/doc.md" "$D/seed.sql" "$D/out.txt"; rmdir "$D"
  exit $RC
fi

[ -f "$DOC" ]  || { say FAIL "OPR-G0 파일이 없다: $DOC";  echo "== FAIL =="; exit 1; }
[ -f "$SEED" ] || { say FAIL "OPR-G0 파일이 없다: $SEED"; echo "== FAIL =="; exit 1; }

HOURS=$(read_hours)
if [ -z "$HOURS" ]; then
  say FAIL "OPR-G1 00 CP-04 에서 운영시간을 못 읽었다 — 표 형식이 바뀌었다"
  echo "== FAIL =="; exit 1
fi
say PASS "OPR-G1 00 CP-04 운영시간 $HOURS"

AM=$(read_cutoff 오전); PM=$(read_cutoff 오후)
if [ -z "$AM" ] || [ -z "$PM" ]; then
  say FAIL "OPR-G2 00 §3 마감표를 못 읽었다 (오전='$AM' 오후='$PM') — 표 형식이 바뀌었다"
  echo "== FAIL =="; exit 1
fi
say PASS "OPR-G2 00 §3 마감 오전[$AM] 오후[$PM]"

# 00 이 정한 여섯 값을 `HH:MM:SS` 로 편다
WANT=$(printf '%s %s %s %s %s %s' \
  "$(echo "$HOURS" | cut -d' ' -f1):00" "$(echo "$HOURS" | cut -d' ' -f2):00" \
  "$(echo "$AM" | cut -d' ' -f1):00"    "$(echo "$PM" | cut -d' ' -f1):00" \
  "$(echo "$AM" | cut -d' ' -f2):00"    "$(echo "$PM" | cut -d' ' -f2):00")

GOT=$(tr -d '\r' < "$SEED" \
      | sed -n "s/^VALUES (1, '\([0-9:]*\)', '\([0-9:]*\)', '\([0-9:]*\)', '\([0-9:]*\)', '\([0-9:]*\)', '\([0-9:]*\)');.*/\1 \2 \3 \4 \5 \6/p" | head -1)

if [ -z "$GOT" ]; then
  say FAIL "OPR-G3 02_Seed.sql 에서 운영기준 INSERT 를 못 읽었다"
elif [ "$GOT" = "$WANT" ]; then
  say PASS "OPR-G3 Seed = 00 ($WANT)"
else
  say FAIL "OPR-G3 Seed 가 00 과 다르다 — Seed[$GOT] vs 00[$WANT]"
fi

if [ "${LIVE:-1}" = "1" ]; then
  Q="SET NOCOUNT ON; SELECT CONVERT(VARCHAR(8), [운영시작시각]) + ' ' + CONVERT(VARCHAR(8), [운영종료시각])
     + ' ' + CONVERT(VARCHAR(8), [일반예약AM마감]) + ' ' + CONVERT(VARCHAR(8), [일반예약PM마감])
     + ' ' + CONVERT(VARCHAR(8), [접수AM마감])     + ' ' + CONVERT(VARCHAR(8), [접수PM마감])
     FROM [dbo].[운영기준] WHERE [기준ID] = 1;"
  DBV=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "$Q" 2>/dev/null | tr -d '\r' | grep -E '^[0-9]{2}:' | head -1)
  if [ -z "$DBV" ]; then
    say FAIL "OPR-G4 배포된 [운영기준] 1행을 못 읽었다 — 행이 없거나 DB 에 붙지 못했다"
  elif [ "$DBV" = "$WANT" ]; then
    say PASS "OPR-G4 실물 [운영기준] = 00 ($DBV) — 회차가 창을 넓혔다면 되돌아왔다"
  else
    say FAIL "OPR-G4 실물 [운영기준] 이 00 과 다르다 — DB[$DBV] vs 00[$WANT]. 넓힌 창을 되돌리지 않았다"
  fi
fi

if [ "$FAIL" -eq 0 ]; then echo "== PASS 운영기준 ↔ 00 =="; else echo "== FAIL =="; fi
exit $FAIL
