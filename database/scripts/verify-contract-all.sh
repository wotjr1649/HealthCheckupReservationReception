#!/usr/bin/env bash
# tests/contract/* 를 전건 실행하고 tools/verify-contract.js 로 계약을 판정한다 (G09).
# set -e 를 쓰지 않는다 — 실패한 줄에서 셸이 끝나 RC 수집도 로그 출력도 안 된다.
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'; DB='HealthCheckupReservationReceptionDb'
OUT=artifacts/reports/contract-verify.txt
mkdir -p artifacts/logs artifacts/reports
: > "$OUT"
FAILED=0

# Write SP 의 성공 시나리오는 창 밖에서 RS0(308/309) 하나만 반환한다.
# expected-contracts.json 이 RS 2개를 기대하므로 창 밖 회차는 반드시 FAIL 한다.
#
# [X] R12 까지는 그래서 "업무시간 밖이면 SKIP" 이었다. SKIP 은 PASS 가 아니므로(CLAUDE.md §10)
#     야간·주말 회차는 RWR/CWR 계약을 **한 건도** 판정하지 못한 채 green 을 냈다.
# [R13] 운영시간이 04 §8.7 [운영기준] 으로 나왔다. 루프 동안만 창을 넓힌다.
#     넓힌 것은 창뿐이고, 창 자체의 계약은 OFF-308-01(휴무일을 심는다)·OFF-309-02(창을 좁힌다)가
#     루프 안에서 **자기 조건을 자기가 만들어** 판정한다 — 이제 셋 다 시계를 기다리지 않는다.
# [!] 루프가 어떻게 끝나든 아래에서 되돌린다. 되돌아왔는지는 회차 끝에서
#     scripts/verify-operating-baseline.sh OPR-G4 가 따로 판정한다.
OPRSAVE=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON;
  SELECT CONVERT(VARCHAR(8), [운영시작시각]) + '|' + CONVERT(VARCHAR(8), [운영종료시각])
    FROM [dbo].[운영기준] WHERE [기준ID] = 1;" | tr -d ' \r')
if [ -z "$OPRSAVE" ]; then
  echo "FAIL 운영기준 1행을 읽지 못했다 — 창을 넓힐 수도 되돌릴 수도 없다" >> "$OUT"; FAILED=1
else
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -Q "UPDATE [dbo].[운영기준]
     SET [운영시작시각] = '00:00:00', [운영종료시각] = '23:59:59' WHERE [기준ID] = 1;" > /dev/null || FAILED=1
fi
restore_opr() {
  [ -n "$OPRSAVE" ] || return 0
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -Q "UPDATE [dbo].[운영기준]
     SET [운영시작시각] = '${OPRSAVE%%|*}', [운영종료시각] = '${OPRSAVE##*|}'
   WHERE [기준ID] = 1;" > /dev/null || return 1
}
trap 'restore_opr' EXIT

# 접수마감 가드는 R13 이 걷었다. CWR-006(성공)·CWR-009(마감경과)이 [운영기준] 의 마감을
# 자기 시나리오 안에서 옮겼다 되돌리므로, 둘 다 하루 중 언제 돌려도 성립한다.
# 마감 **경계** 는 여기가 아니라 RUL-T07·T08·T11·T12 가 TVF 에 시각을 주입해 진짜 값으로 잰다.

# 업무일 여부. 요일·휴무일은 00 이 **날짜로** 정한 것이라 넓히지 않는다 — 일요일·휴무일 회차는
# 여전히 SKIP 이고, 그 자리는 OFF-308-01 이 휴무일을 심어 반대편에서 메운다.
DAYOK=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON;
  SELECT CASE WHEN DATEPART(WEEKDAY, SYSDATETIME()) BETWEEN 2 AND 7
              AND NOT EXISTS (SELECT 1 FROM dbo.휴무일
                               WHERE [휴무일자] = CONVERT(DATE, SYSDATETIME()) AND [사용여부] = 1)
         THEN 1 ELSE 0 END;" | tr -d ' \r')

# SEL-025 정원 일치. 조회 SP 의 Capacity 와 저장 SP 의 상한이 갈리면 화면은 "자리 있음" 을
# 보여주고 저장은 305 를 낸다 (06 §43-21). 정원 20 은 조회 SP 8곳·저장 SP 2곳에 따로 박혀 있고
# 어떤 시험도 둘의 일치를 보지 않았다.
#   조회 쪽은 여기서 매 회귀 고정한다 (SELECT SP 라 시각과 무관하다).
#   저장 쪽은 CON-002(19/20 → 20)·CON-008(20/20 → 305)이 경계에서 고정한다.
#   한쪽만 바뀌면 둘 중 하나가 반드시 깨진다.
CAPQ="SET NOCOUNT ON; DECLARE @P BIGINT = (SELECT TOP (1) [수검자ID] FROM [dbo].[수검자] ORDER BY [수검자ID]); EXEC [dbo].[USP_HC_예약가능정보_조회] @P, NULL, NULL, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0;"
CAPP=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -s"|" -Q "$CAPQ" 2>/dev/null \
       | grep -E '^(AM|PM)\|' | cut -d'|' -f3 | sort -u | tr -d ' \r' | tr '\n' ' ' | sed 's/ *$//')
if [ "$CAPP" = "20" ]; then
  echo "PASS SEL-025 조회 SP 의 Capacity = 20 (AM·PM 동일) — 저장 쪽은 CON-002·008 이 고정" >> "$OUT"
else
  echo "FAIL SEL-025 조회 SP 의 Capacity 가 [$CAPP] 다 (기대 20)" >> "$OUT"; FAILED=1
fi

for f in tests/contract/*.sql; do
  k=$(basename "$f" .sql)
  # SELECT SP 계약은 시간대와 무관하다. Write SP 계약만 가드한다 (T36 이 추가한다).
  case "$k" in
    # [R12] PWR-* 는 가드하지 않는다 — 수검자 Write 는 308/309 를 내지 않는다 (05 §10.1·§10.2).
    RWR-*|CWR-*)
      if [ "${DAYOK:-0}" -ne 1 ]; then
        echo "SKIP $k 업무일 밖 — 예약·접수 Write 는 308 을 업무 Rule 보다 먼저 판정한다" >> "$OUT"
        continue
      fi ;;
  esac
  # OFF-* 는 자기 조건을 자기가 만든다 — 308 은 오늘을 휴무일로 심고, 309 는 창을 좁힌다.
  # 시각 가드가 필요 없는 이유가 그것이다. 309 만 업무일이어야 한다: 일요일·휴무일에는
  # 오늘업무일=0 이 먼저 판정돼 308 이 나오고 309 에 닿지 못한다 (UFN_HC_일정확인 업무가능코드 CASE).
  case "$k" in
    OFF-309-*) if [ "${DAYOK:-0}" -ne 1 ]; then
        echo "SKIP $k 업무일이 아님 — 309 는 업무일에만 나온다 (그날은 308 이 먼저다)" >> "$OUT"; continue; fi ;;
  esac
  # -W -w 65535 를 빼지 않는다. 기본 폭 80 에서 줄이 접히면 파서가 무너진다(실측 확인).
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -W -w 65535 -s"|" \
         -i "$f" -o "artifacts/logs/rs_${k}.txt" || FAILED=1
  node tools/verify-contract.js "artifacts/logs/rs_${k}.txt" "$k" >> "$OUT" 2>&1 || FAILED=1
done

# OFF-308 이 심은 임시 휴무일이 남아 있으면 뒤따르는 모든 회차가 308 이 된다.
# 시험 파일이 지우지만 한 번 더 확인한다 — 남은 채로 green 을 내지 않는다.
LEFT=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON;
  DELETE FROM dbo.휴무일 WHERE [휴무일명] = N'OFF-308 시험용 임시 휴무일';
  SELECT CONVERT(VARCHAR(5), @@ROWCOUNT) + '/' + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM dbo.휴무일));" | tr -d ' \r')
# [X] 여기서 총계까지 세면 Seed 건수의 **세 번째 사본**이 된다(ROOT AGENTS.md §6).
#     총계 판정은 SED-008 · VER-007 · RBD-004 가 이미 한다. 이 게이트가 지키는 것은
#     "OFF-308 이 심은 임시 행이 남지 않았다" 하나뿐이므로 잔여만 본다.
#     R8 에서 Seed 가 2 -> 41 이 되며 이 줄이 거짓이 되었고, 아무도 그것을 보지 않았다.
case "$LEFT" in
  0/*) echo "PASS OFF-308-CLEAN 임시 휴무일 잔여 0건 (총계 $LEFT — 총계는 SED-008·VER-007 이 판정한다)" >> "$OUT" ;;
  *)   echo "FAIL OFF-308-CLEAN 임시 휴무일이 남았다 — 잔여/총계 = $LEFT" >> "$OUT"; FAILED=1 ;;
esac

cat "$OUT"
echo "contract-verify FAILED=$FAILED"
exit $FAILED
