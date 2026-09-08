#!/usr/bin/env bash
# 동시성 시나리오 1건 실행 (G11 · 스펙 §38 · plans/08 T34).
#   사용법: ./scripts/concurrency-test.sh <1~8>
#   호출 전에 rebuild + tests/00_Test_Harness.sql 가 끝나 있어야 한다 (scripts/test.sh 가 한다).
#
# set -e 를 쓰지 않는다 — 실패한 줄에서 셸이 끝나 RC 수집도 로그 출력도 안 된다 (CLAUDE.md §6).
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'
DB='HealthCheckupReservationReceptionDb'
# --check <n> : 업무시간 게이트만 보고 끝낸다. scripts/test.sh 가 rebuild 8회를 헛돌지 않도록
#               실행 전에 먼저 묻는 용도다. 게이트 로직을 두 곳에 복사하지 않는다.
CHECKONLY=0
if [ "${1:-}" = "--check" ]; then CHECKONLY=1; shift; fi
SCEN="${1:?시나리오 번호를 지정하십시오 (1~8)}"
case "$SCEN" in 1|2|3|4|5|6|7|8) ;; *) echo "시나리오는 1~8 입니다"; exit 2 ;; esac
mkdir -p artifacts/logs
RUN=$(date +%Y%m%d_%H%M%S)
L="artifacts/logs/conc_${RUN}_${SCEN}"      # [X] 고정 파일명을 쓰면 8회를 돌아도 마지막 것만 남아
                                            #     rc=1 이 어느 시나리오에서 났는지 사후 확인이 안 된다 (G15).

q() { sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON; $1" 2>/dev/null | tr -d ' \r' | head -1; }
cat16() { iconv -f UTF-16 -t UTF-8 "$@" 2>/dev/null; }

# ── 업무시간 게이트. Write SP 전부가 308/309 를 내므로 창 밖에서는 어떤 시나리오도 성립하지 않는다.
#    실행하지 않은 검증을 PASS 로 적지 않는다 (CLAUDE.md §10) — exit 3 = NOT RUN 이다.
#    상한은 17:58 이다. 한 시나리오의 실측 주기가 약 16초다 (rebuild+harness 2s · setup 1s ·
#    barrier 8s · A 선점 3s · 판정 2s). 2분이면 주기의 7배라 18:00 을 넘길 수 없다.
#    10분 여유를 두면 쓸 수 있는 창을 그냥 버린다.
BIZ=$(q "SELECT CASE WHEN DATEPART(WEEKDAY, SYSDATETIME()) BETWEEN 2 AND 7
                      AND CONVERT(TIME(0), SYSDATETIME()) >= '09:00:00'
                      AND CONVERT(TIME(0), SYSDATETIME()) <  '17:58:00'
                      AND NOT EXISTS (SELECT 1 FROM [dbo].[휴무일]
                                       WHERE [휴무일자] = CONVERT(DATE, SYSDATETIME()) AND [사용여부] = 1)
                     THEN 1 ELSE 0 END;")
if [ "${BIZ:-0}" -ne 1 ]; then
  echo "NOT RUN CON-00$SCEN 업무시간(월~토 09:00~17:58, 비휴무일) 밖 — Write SP 가 308/309 를 낸다"
  exit 3
fi
# CON-005·CON-008 은 접수완료 성공이 필요해 PM 접수마감(16:00) 전이어야 한다 (스펙 §38.7).
# 10분 여유를 둔다 — 게이트 시작 후 마감을 넘어가면 304 로 시나리오가 무너진다.
if [ "$SCEN" = "5" ] || [ "$SCEN" = "8" ]; then
  CUT=$(q "SELECT CASE WHEN CONVERT(TIME(0), SYSDATETIME()) >= '11:00:00'
                        AND CONVERT(TIME(0), SYSDATETIME()) <  '15:50:00' THEN 1 ELSE 0 END;")
  if [ "${CUT:-0}" -ne 1 ]; then
    echo "NOT RUN CON-00$SCEN PM Slot 창(11:00~15:50) 밖 — 접수완료 성공 경로가 성립하지 않는다 (스펙 §38.7)"
    exit 3
  fi
fi

[ "$CHECKONLY" -eq 1 ] && exit 0

# ── 사전상태
sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -v Scenario="$SCEN" \
       -i tests/09_Concurrency_Setup.sql -o "${L}_setup.log"
if [ $? -ne 0 ]; then echo "FAIL CON-00$SCEN setup 실패"; cat16 "${L}_setup.log"; exit 1; fi

# ── barrier 는 **setup 이 끝난 뒤에** 계산한다.
#    앞에서 계산하면 setup 이 그 시각을 넘겼을 때 WAITFOR TIME 이 다음 날까지 대기하고,
#    wait 에 timeout 이 없어 스크립트가 약 24시간 정지한다.
NOWH=$(date +%H)
[ "$NOWH" = "23" ] && { echo "NOT RUN CON-00$SCEN 자정 근처에서는 실행하지 않습니다"; exit 3; }
# [X] -v 값에 콜론을 넣지 않는다. sqlcmd 가 ':MM:SS' 를 별도 인수로 잘라 즉시 죽는다 (실측).
#     세션 스크립트가 STUFF 로 콜론을 다시 끼운다.
BARRIER=$(date -d '+8 seconds' +%H%M%S)
echo "INFO CON-00$SCEN run=$RUN barrier=$BARRIER"

sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -h-1 -W -s"|" -v BarrierTime="$BARRIER" -v Scenario="$SCEN" \
       -i tests/10_Concurrency_Session_A.sql -o "${L}_A.log" &
PIDA=$!
sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -h-1 -W -s"|" -v BarrierTime="$BARRIER" -v Scenario="$SCEN" \
       -i tests/11_Concurrency_Session_B.sql -o "${L}_B.log" &
PIDB=$!

# [X] wait … || echo "…$?" 는 종료상태를 잃는다. 변수로 받는다.
RCA=0; RCB=0
wait $PIDA || RCA=$?
wait $PIDB || RCB=$?
echo "INFO session A exit=$RCA / session B exit=$RCB   (SP 가 THROW 50001/50002 로 끝나는 것은 정상이다)"

RC=0
# ── barrier 를 이미 지나 세션이 즉시 중단됐다면 경합 자체가 없었다. 판정으로 넘어가지 않는다.
BARRMISS=$(cat16 "${L}_A.log" "${L}_B.log" | grep -c '51001' || true)
if [ "$BARRMISS" -ne 0 ]; then
  echo "FAIL CON-00$SCEN barrier 시각을 이미 지나 세션이 중단됐다 (Msg 51001) — 경합이 발생하지 않았다"
  exit 1
fi

# ── [X] 세션이 0 이 아닌 코드로 끝나는 것을 통째로 "업무실패" 로 읽고 있었다.
#    업무실패는 Result Set 으로 돌아오므로 exit 0 이다. exit != 0 의 정당한 원인은
#    SP 가 THROW 한 50001(잠금실패)·50002(교착) 둘뿐이고, 그 밖의 오류는 **SP 에 닿기도 전에
#    배치가 죽은 것**이다. 그러면 아무도 쓰지 않았으므로 뒤따르는 "값이 안 바뀌었다" 단언이
#    전부 참이 되어 시나리오가 실행되지 않은 채 PASS 가 나간다.
#    R7 에서 CON-004 가 그랬다 — 세션 A 가 Msg 257 로 죽었는데 PASS 였다.
chk_session() {                       # chk_session <A|B> <rc>
  [ "$2" -eq 0 ] && return 0
  if cat16 "${L}_$1.log" | grep -qE 'Msg (50001|50002)'; then return 0; fi
  echo "FAIL CON-00$SCEN session $1 이 exit=$2 인데 THROW 50001/50002 가 없다 — 시나리오 전에 배치가 죽었다"
  cat16 "${L}_$1.log" | grep -iE 'HResult|^Msg |변환할 수 없습니다|구문' | head -3
  return 1
}
chk_session A "$RCA" || RC=1
chk_session B "$RCB" || RC=1

# ── DB 최종 상태 판정
sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -v Scenario="$SCEN" \
       -i tests/12_Concurrency_Verify.sql -o "${L}_verify.log" || RC=1
cat16 "${L}_verify.log" | grep -E '^(PASS|FAIL|INFO)' || true

# ── 로그 수치 판정 (스펙 §38.3 · §38.4). grep -c 는 0건일 때 exit 1 이므로 || true 로 받는다.
AB=$(cat16 "${L}_A.log" "${L}_B.log")
n() { echo "$AB" | grep -cE "$1" || true; }
CONTEND=$(n 'applock rc=1')
DL1205=$(n 'Msg 1205')
DL50002=$(n 'Msg 50002')
TMO50001=$(n 'Msg 50001')
DUP2627=$(n 'Msg 2627')
C601=$(n '^[01][|]601[|]')
C502=$(n '^[01][|]502[|]')
# [X] 100(필수값)이 나오면 그 세션은 업무경로에 **닿지도 못한** 것이다. 그래도 최종 상태는
#     "아무것도 안 바뀜" 이라 tests/12 가 PASS 를 찍는다. CON-004 가 실제로 그랬다 —
#     @LastEditDate·@ChartNo 를 NULL 로 넘겨 100 으로 끝났는데 판정은 통과였다 (실측).
C100=$(n '^[01][|]100[|]')
echo "INFO 경합(applock rc=1)=$CONTEND  엔진교착(1205)=$DL1205  applock교착(50002)=$DL50002  applock timeout(50001)=$TMO50001  중복키(2627)=$DUP2627  601=$C601  502=$C502  100=$C100"
[ "$C100" -ne 0 ] && { echo "FAIL CON-00$SCEN 세션이 100(필수값)으로 끝났다 — 업무경로에 닿지 못해 판정이 공허하다"; RC=1; }

[ "$DL1205"   -ne 0 ] && { echo "FAIL CON-00$SCEN 엔진 교착 발생 (Msg 1205)"; RC=1; }
[ "$DL50002"  -ne 0 ] && { echo "FAIL CON-00$SCEN applock 교착 발생 (Msg 50002)"; RC=1; }
[ "$TMO50001" -ne 0 ] && { echo "FAIL CON-00$SCEN applock timeout (Msg 50001) — 상대 세션이 5초 안에 끝나지 않았다"; RC=1; }
# 스펙 §20 — UQ 가 막아 주더라도 2627 이 나오면 applock 사전 직렬화가 실패한 것이다.
[ "$DUP2627"  -ne 0 ] && { echo "FAIL CON-00$SCEN Msg 2627 발생 — applock 사전 직렬화 실패 (스펙 §20)"; RC=1; }

# §38.4-1: CON-001~005·007 은 rc=1 이 최소 1건 있어야 성립으로 인정한다.
#   최종 상태만으로는 "경합 발생" 과 "우연한 직렬 실행" 을 구분할 수 없어 applock 을 통째로
#   지워도 전건 PASS 할 수 있었다. CON-006·008 은 조건부 UPDATE·정원 COUNT 가 상태로 판정한다.
case "$SCEN" in
  1|2|3|4|5|7)
    if [ "$CONTEND" -eq 0 ]; then
      echo "FAIL CON-00$SCEN applock rc=1 이 0건 — 경합이 일어나지 않아 잠금 동작을 증명하지 못했다"; RC=1
    else
      echo "PASS CON-00$SCEN-LOCK applock 대기 후 획득 $CONTEND 건 (경합 실측)"
    fi ;;
esac

# CON-005 는 최종 상태가 단일 행이라 "정확히 하나만 성공" 을 상태로 판정할 수 없다. 패자의 502 를 본다.
if [ "$SCEN" = "5" ]; then
  if [ "$C502" -ge 1 ]; then echo "PASS CON-005-LOSER 패자 세션이 502 를 반환했다"
  else echo "FAIL CON-005 패자의 502 가 로그에 없다 — 두 세션이 모두 성공했을 수 있다"; RC=1; fi
fi
# CON-006 의 601 은 상태로 볼 수 없다. stale RowVersion 요청이 실제로 거절됐는지 로그에서 본다.
if [ "$SCEN" = "6" ]; then
  if [ "$C601" -ge 1 ]; then echo "PASS CON-006-STALE stale RowVersion 요청이 601 로 거절됐다"
  else echo "FAIL CON-006 601 이 로그에 없다 — 두 요청이 모두 반영됐을 수 있다"; RC=1; fi
fi

[ "$RC" -eq 0 ] && echo "=== CON-00$SCEN 통과 ===" || echo "=== CON-00$SCEN 실패 ==="
exit $RC
