`[!]` **이 계획서의 스키마 참조는 `plans/09`·`plans/10` 이 교체했다** ― 컬럼명 한글화,
검사구성의 `예약접수`·`완료이력` 흡수, `검사항목` 삭제, `변경이력` EAV 전환. 당시 구조는 git 이력에 있다.

`[R3]` **`T35`·`T36` 착수 시점(2026-09-07)에 R3 잔재를 정리했다.** 아래가 실행을 막고 있었다.

```text
SP 15                    -> 16. R3 재봉인이 SP-LOG-01 을 더했다 (05 §1.3 · 06 §18).
NCI 4                    -> 5.  IX_변경이력_TARGET 이 더해졌다 (04 §8.6.4).
지문 12칸                 -> 15칸. CHECK·DEFAULT·TVP 를 더했다. FK 는 여전히 6번째 칸이다.
업무시간 가드의 [Active]   -> [사용여부]
영문 컬럼명 ExamItemCode  -> 검사항목코드 등. plans/09 가 전부 한글화했다.
GRANT 15건 유지 확인       -> 사용자 결정(2026-09-07)으로 Security 미구현이라 기대는 0건이다.
                            RBD-008 은 덤프의 GRANT 구획이 재실행 전후로 같은지로 판정한다.
```

`[R3]` **`T36` 의 DMV 판정은 16/16 이 될 수 없다.** `sys.dm_exec_describe_first_result_set_for_object`
는 `sp_getapplock` 을 부르는 SP 에서 `Msg 11520` 으로 실패하고, Write SP 8개가 전부 그 부류다(실측).
스펙 §36 이 판정 경로를 셋으로 나눴다 — SELECT SP 8개는 DMV, Write SP 8개는 계약 시나리오의 실측
출력, 타입은 `verify-docs.js` 의 `V17` 이 배포 SQL 의 `CAST` 패턴을 정적으로 대조한다.

`[R3]` **회차 사이 비교는 `scripts/clean-rebuild-verify.sh` 로 옮겼다.** `tests/14` 는 한 회차의
지문·덤프만 낸다. `RBD-002`·`003`·`005`·`007`·`008`·`009` 가 그 스크립트에 있고 `RBD-001` 은
인스턴스가 1개뿐이라 `NOT RUN` 이다.
# Stage 10~12 — Rollback · Concurrency · Clean Rebuild · 문서 최종화

**Index:** `2026-09-04-phase4-database-implementation.md`
**Spec:** `../06_DB_Transaction_Security_Seed_CANDIDATE.md` §37·§38·§40·§41·§42
**Tasks:** `T33` ~ `T37`

---

> ## ⚠ 테스트 패턴 — `INSERT … EXEC` 금지 (스펙 §33.1a, index 문서 "테스트 작성 규칙")
>
> 이 파일의 `INSERT INTO @RS0 EXEC [dbo].[USP_HC_…]` 형태 예시는 **그대로 실행하면 안 된다.**
> 실측: RS가 2개 이상인 SP는 `Msg 213`, 내부 `ROLLBACK` 은 `Msg 3915`, 진입 시 `@@TRANCOUNT=1`.
>
> 아래 예시는 **각 시나리오의 기대 ResultCode 를 문서화**하는 용도다. 실제 구현은 이렇게 나눈다.
>
> | 판정 대상 | 구현 위치 |
> |---|---|
> | RS0 의 `Success`·`Code`, RS 개수·컬럼·행수 | `tests/contract/<NN>_<시나리오>.sql` (`EXEC` 한 번) + `tools/verify-contract.js` + `expected-contracts.json` |
> | DB 상태 불변조건 (행수·`StatusCode`·`RowVersion`·Detail 집합) | 이 파일의 `tests/<NN>_*.sql` |
>
> 예시의 `IF ((SELECT Code FROM @RS0) = NNN)` 는 `expected-contracts.json` 의 `"rs0Code": NNN` 으로 옮긴다.


## Task T33: `tests/08_Rollback_Tests.sql` — 부분저장 0건

**목적:** 업무실패 시 Master/Detail이 **전혀** 바뀌지 않았음을 증명한다 (G10).

**관련 Baseline 위치:** 스펙 §21.3·§37.

**선행조건:** `T32` 완료.

**Files:** Create `tests/08_Rollback_Tests.sql`

**Interfaces:** Produces `PASS RBK-001` ~ `PASS RBK-008`

**금지사항:** 테스트를 통과시키려고 SP의 실패 조건을 완화하지 않는다.

- [ ] **Step 1: 스냅샷 → 실패 유도 → 비교 패턴**

**계약 시나리오**: `RBK-001` = `EXEC [dbo].[USP_HC_INSERT_예약] @P, 'NORMAL', '2026-11-17', 'AM', 0,0,1,0,0,0,0;` (남성 `T009` + OPT03) → 기대 RS0 `Code=411`.

**DB 상태 단언** — 부분저장이 없었는지는 SP 반환값이 아니라 **행수**로만 증명된다.

```sql
SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- 업무시간 가드 (스펙 §33.2a)
IF NOT (DATEPART(WEEKDAY, SYSDATETIME()) BETWEEN 2 AND 7
        AND CONVERT(TIME(0), SYSDATETIME()) >= '09:00:00'
        AND CONVERT(TIME(0), SYSDATETIME()) <  '18:00:00'
        AND NOT EXISTS (SELECT 1 FROM [dbo].[휴무일]
                         WHERE [휴무일자] = CONVERT(DATE, SYSDATETIME()) AND [사용여부] = 1))
BEGIN
    PRINT 'SKIP 08_Rollback_Tests 업무시간(월~토 09:00~18:00, 비휴무일) 밖';
    RETURN;
END

-- RBK-001 INSERT_예약 이 AEX 성별 위반(411)으로 실패 → Work·Detail 신규 0건
DECLARE @P  BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = 'T009');
DECLARE @W0 INT = (SELECT COUNT(*) FROM [dbo].[예약접수]);
DECLARE @D0 NVARCHAR(160) = (SELECT ISNULL([국가검사항목],N'') FROM [dbo].[예약접수] WHERE [업무ID] = (SELECT MIN([업무ID]) FROM [dbo].[예약접수]));

EXEC [dbo].[USP_HC_INSERT_예약] @P, 'NORMAL', '2026-11-17', 'AM', 0,0,1,0,0,0,0;  -- 남성 + OPT03

IF ((SELECT COUNT(*) FROM [dbo].[예약접수])      = @W0
    AND (SELECT ISNULL([국가검사항목],N'') FROM [dbo].[예약접수] WHERE [업무ID] = (SELECT MIN([업무ID]) FROM [dbo].[예약접수])) = @D0)
    PRINT 'PASS RBK-001 AEX 실패 시 Work·Detail 신규 0건';
ELSE BEGIN PRINT 'FAIL RBK-001 부분저장 발생'; SET @Fail += 1; END
```

`RBK-002`~`RBK-008` 은 위 `RBK-001` 과 같은 구조다 — 실패를 유도하는 `EXEC` 한 번과, 아래 표의 불변조건을 행수·값으로 확인하는 `IF` 하나. RS 는 받지 않는다.

| Test ID | 실패 유도 | 불변조건 |
|---|---|---|
| `RBK-002` | `UPDATE_예약변경` 예약일 변경 후 TGT 비대상 `400` | Work의 `ReservationDate`·`TimeSlotCode`·`RowVersion` 및 Detail 전량 동일 |
| `RBK-003` | `UPDATE_예약변경` 정원 마감 `305` | 동일 |
| `RBK-004` | `UPDATE_접수추가검사` 성별 위반 `411` | AEX Detail 집합·Work `RowVersion` 동일 |
| `RBK-005` | `UPDATE_수검자정보` 주민번호 변경 차단 `205` | `수검자` 행 전체·`LastEditDate` 동일 |
| `RBK-006` | `INSERT_수검자` ChartNo 중복 `201` | 신규 Patient 0건 (Sequence 결번은 허용) |
| `RBK-007` | 위 모든 실패 직후 | `@@TRANCOUNT = 0` |
| `RBK-008` | 실패 응답 | Result Set 1개 (RS0만). `202`/`203` 만 예외 |

`RBK-002` 의 불변 비교는 실패 전후의 `RowVersion` 을 직접 비교한다.

```sql
-- 검사구성 스냅샷. 검사구성이 예약접수 행의 컬럼 2개이므로 문자열 하나로 비교한다 (04 §8.2.2).
--   코드 오름차순 정규 순서로 조립되므로 집합 동일 판정이 문자열 비교다 (05 §11.2).
--   FOR XML PATH 는 쓰지 않는다 — 스펙 §9.2 허용목록에 없고 index 금지 목록에 XML 이 있다.
DECLARE @RvBefore BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID]=@W);
DECLARE @Before NVARCHAR(160) =
    (SELECT ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
       FROM [dbo].[예약접수] WHERE [업무ID] = @W);

-- … 실패 유도 …

DECLARE @Same BIT =
    CASE WHEN @Before = (SELECT ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
                           FROM [dbo].[예약접수] WHERE [업무ID] = @W)
         THEN 1 ELSE 0 END;
-- @Same = 1 이고 RowVersion 이 @RvBefore 와 같아야 한다
```

`[X]` **초안은 `FOR XML PATH` 를 쓰면서 "SQL Server 2005부터 있으므로 허용목록에 부합한다"고 적었다.** 허용목록 방식은 *"아래에 없는 기능은 쓰지 않는다"* 이고 `FOR XML PATH` 는 목록에 없다. 버전이 오래된 것은 허용 근거가 아니다. index 문서의 절대 금지 목록에도 `XML` 이 있다.

- [ ] **Step 2: 실행**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/08_Rollback_Tests.sql -o artifacts/logs/test_08.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_08.log | grep -E '^(PASS|FAIL)'
```

Expected: exit 0, 8건 PASS.

- [ ] **Step 3: Commit** — `test(phase4): Rollback 부분저장 0건 검증 8건 추가`

**완료조건:** 스펙 §45.2 의 `RBK` 전건 PASS. 하나라도 FAIL 이면 해당 SP 의 Transaction 경계를 고친다. 업무시간 밖 실행이면 `SKIP` 이며 `PASS` 로 승격하지 않는다.

---

## Task T34: 동시성 시험 — `tests/09`~`12` · `scripts/concurrency-test.sh`

**목적:** `05` §14가 고정한 7개 결과조건 + 스펙 §38.5 의 `CON-008`을 결정적으로 재현하고 검증한다 (G11).

**관련 Baseline 위치:** `05` §14, `04` §3.10, 스펙 §38.

**선행조건:** `T33` 완료.

**Files:**
- Create: `tests/09_Concurrency_Setup.sql`, `tests/10_Concurrency_Session_A.sql`,
  `tests/11_Concurrency_Session_B.sql`, `tests/12_Concurrency_Verify.sql`
- Create: `scripts/concurrency-test.sh`

**Interfaces:**
- Consumes: SQLCMD 변수 `$(BarrierTime)`, `$(Scenario)`
- Produces: `artifacts/logs/conc_A.log`, `conc_B.log`, `conc_verify.log`

**금지사항:** Extended Events 세션이나 trace flag 를 만들지 않는다. `WAITFOR DELAY` 로 경합을 흉내내지 않는다 — 두 세션이 **같은 절대시각**에 진입해야 한다.

`[R3]` **착수 차단 결함 4건 (2026-09-07 점검).**

```text
1  $(LockResource)          Step 3 의 선점 코드가 이 sqlcmd 변수를 쓰는데 concurrency-test.sh 가
                            넘기지 않는다. 정의되지 않은 변수는 sqlcmd 가 즉시 실패시킨다.
                            -> 자원명은 시나리오 번호로 **세션 스크립트 안에서** 만든다.
                               CON-001 은 HASHBYTES 로 SSN 해시까지 그 안에서 계산한다.
2  EXEC 예시의 인자 개수      R3 이 Write SP 8개에 @OperatorName 을 더했다. Step 2 의 예시는
                            그 전 형태라 그대로 쓰면 Msg 201 이다. 12/12/3/3/10/3/14/14 개다.
3  CON-004 판정식            "둘 다 성공 금지" 는 정상 직렬 결과를 FAIL 시킨다. 스펙 §38.6 으로 교체.
4  CON-005·CON-008 시각      접수완료 성공이 필요해 접수마감 전이어야 한다. PM Slot 으로 구성해
                            11:00~15:50 창을 쓴다 (스펙 §38.7). AM 은 09:00~10:50 뿐이다.
```

`[R3]` **`CON-006` 의 Session A 는 반드시 *실제 변경* 이어야 한다.** 동일 AEX 집합을 주면 A 가
No-op(`Code=1`)으로 끝나 `RowVersion` 이 그대로 남고, B 도 No-op 이 되어 **`601` 이 영영 나오지 않는다.**
A 는 AEX 를 실제로 바꾸고 B 는 그 전에 읽은(이제 stale 인) `RowVersion` 으로 또 다른 변경을 요청한다.

`[R3]` **`CON-001` 은 `Msg 2627` 0건을 함께 본다.** `UQ_수검자_SOCIAL_NUMBER` 가 applock 이 없어도
두 번째 INSERT 를 막아 주므로 "행 1건" 만으로는 잠금이 동작했는지 알 수 없다. 스펙 §20 은 `2627` 을
"applock 으로 사전 직렬화했으므로 발생 시 설계 위반" 으로 못박았다.

`[R3]` **`CON-007` 은 `rc=1` 을 함께 본다.** 경합이 실제로 일어나지 않으면 "교착 0건" 은 공허하다 —
자원 정렬 획득을 통째로 지워도 우연한 직렬 실행이면 PASS 한다.

- [ ] **Step 1: `scripts/concurrency-test.sh` 작성**

```bash
#!/usr/bin/env bash
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'
DB='HealthCheckupReservationReceptionDb'
SCEN="${1:?시나리오 번호를 지정하십시오 (1~8)}"
mkdir -p artifacts/logs

# setup 을 **먼저** 끝낸 뒤 barrier 를 계산한다.
sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -v Scenario="$SCEN" \
       -i tests/09_Concurrency_Setup.sql -o artifacts/logs/conc_setup.log \
  || { echo "setup 실패"; iconv -f UTF-16 -t UTF-8 artifacts/logs/conc_setup.log; exit 1; }

NOWH=$(date +%H)
[ "$NOWH" = "23" ] && { echo "자정 근처에서는 실행하지 않습니다."; exit 2; }
BARRIER=$(date -d '+5 seconds' +%H:%M:%S)
echo "barrier=$BARRIER scenario=$SCEN"

sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -v BarrierTime="$BARRIER" -v Scenario="$SCEN" \
       -i tests/10_Concurrency_Session_A.sql -o artifacts/logs/conc_A.log &
PIDA=$!
sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -v BarrierTime="$BARRIER" -v Scenario="$SCEN" \
       -i tests/11_Concurrency_Session_B.sql -o artifacts/logs/conc_B.log &
PIDB=$!

RCA=0; RCB=0
wait $PIDA || RCA=$?
wait $PIDB || RCB=$?
echo "session A exit=$RCA / session B exit=$RCB"

RC=0
sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -v Scenario="$SCEN" \
       -i tests/12_Concurrency_Verify.sql -o artifacts/logs/conc_verify.log || RC=$?
iconv -f UTF-16 -t UTF-8 artifacts/logs/conc_verify.log | grep -E '^(PASS|FAIL|INFO)' || true

# 로그 조건도 수치로 판정한다 (스펙 §38.3 · §38.4)
CONTEND=$(iconv -f UTF-16 -t UTF-8 artifacts/logs/conc_A.log artifacts/logs/conc_B.log 2>/dev/null \
          | grep -c 'applock rc=1' || true)
DL1205=$(iconv -f UTF-16 -t UTF-8 artifacts/logs/conc_A.log artifacts/logs/conc_B.log 2>/dev/null \
          | grep -c 'Msg 1205' || true)
DL50002=$(iconv -f UTF-16 -t UTF-8 artifacts/logs/conc_A.log artifacts/logs/conc_B.log 2>/dev/null \
          | grep -c 'Msg 50002' || true)
echo "INFO 경합(rc=1)=$CONTEND  엔진교착(1205)=$DL1205  applock교착(50002)=$DL50002"

[ "$DL1205"  -ne 0 ] && { echo "FAIL CON-$SCEN 엔진 교착 발생"; RC=1; }
[ "$DL50002" -ne 0 ] && { echo "FAIL CON-$SCEN applock 교착 발생"; RC=1; }
case "$SCEN" in
  2|3|5) [ "$CONTEND" -eq 0 ] && { echo "FAIL CON-$SCEN 경합 미발생 — 잠금 동작을 증명하지 못함"; RC=1; } ;;
esac
exit $RC
```

`[X]` **초안 오류 4건**
1. **barrier 를 setup 전에 계산했다.** setup 이 5초를 넘으면 `WAITFOR TIME` 이 **다음 날 그 시각까지** 대기하고, `wait` 에 timeout 이 없어 스크립트가 약 24시간 정지한다. setup 이후로 옮긴다. 세션 스크립트 머리에도 `IF CONVERT(TIME(0), SYSDATETIME()) >= '$(BarrierTime)' THROW 51001` 가드를 둔다.
2. `wait … || echo "…$?"` 는 `set -e` 와 조합에서 종료상태를 잃는다. 변수로 받는다.
3. **로그 조건을 `echo` 로만 출력했다.** `Msg 1205`·`50002`·경합 증거를 **수치 비교로 판정**하고 불일치 시 nonzero 종료한다.
4. `grep -c … || true` 로 받아야 한다. `grep -c` 는 0건일 때 exit 1 이다.

세션 A/B는 업무실패(`306`/`305`/`601` 등)로 끝날 수 있고 그것이 정상이므로 exit code를 치명적으로 다루지 않는다. **판정은 DB 최종 상태 + 로그 수치**로 한다.

- [ ] **Step 2: 세션 스크립트의 barrier 패턴**

`tests/10_Concurrency_Session_A.sql` 머리:

```sql
SET NOCOUNT ON;
DECLARE @Scen INT = CONVERT(INT, N'$(Scenario)');
WAITFOR TIME '$(BarrierTime)';        -- 두 세션이 같은 절대시각에 진입
PRINT 'INFO A 진입 ' + CONVERT(VARCHAR(30), SYSDATETIME(), 121);

IF @Scen = 1
BEGIN
    -- R3: @HepatitisBExcluded · @ConfirmSimilarPatient · @OperatorName 을 포함해 14개다.
    EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'동시등록', '9505051000019',
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'CONC-A';
END
ELSE IF @Scen = 2
BEGIN
    DECLARE @P BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호]='CONC1');   -- 09_Concurrency_Setup.sql 이 심는 전용 수검자
    -- R3: @OperatorName 을 포함해 12개다.
    EXEC [dbo].[USP_HC_INSERT_예약] @P, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0, N'CONC-A';
END
-- … 시나리오 3~7
GO
```

`tests/11_Concurrency_Session_B.sql` 은 시나리오별로 A와 짝이 되는 호출을 한다.

- [ ] **Step 3: 시나리오 8종과 판정 (`tests/12_Concurrency_Verify.sql`)**

| # | Test ID | 시나리오 | Session A | Session B | DB 최종 상태 판정 |
|---:|---|---|---|---|---|
| 1 | `CON-001` | 동일 SocialNumber 동시등록 | `INSERT_수검자` | 동일 SSN | `SELECT COUNT(*) FROM 수검자 WHERE SocialNumber=…` = **1** |
| 2 | `CON-002` | 19/20 Slot 동시 신규예약 | `INSERT_예약` | 다른 Patient, 같은 Slot | 해당 Slot `RSV+RCP` = **20** |
| 3 | `CON-003` | 동일 Patient 다른 Slot 동시예약 | `INSERT_예약`(AM) | `INSERT_예약`(PM) | 해당 Patient 유효업무 = **1** |
| 4 | `CON-004` | 주민번호 변경 vs 신규예약 | `UPDATE_수검자정보`(만 46 -> 만 56 이 되는 SSN) | `INSERT_예약`(동일 Patient, 미래 예약일) | 저장된 `국가검사항목` 코드 집합 = 최종 수검자 상태 기준 `UFN_HC_국가검사구성` 결과 (`EXCEPT` 양방향 0). 스펙 §38.6 |
| 5 | `CON-005` | 접수완료 vs 예약취소 (**오늘 PM Slot · 16:00 전**) | `UPDATE_접수완료` | `UPDATE_예약취소` | Work `상태코드` ∈ {`RCP`,`CNC`} 이고 **정확히 하나만** 전이. 패자 로그에 `502` |
| 6 | `CON-006` | 같은 Work AEX 동시변경 | `UPDATE_접수추가검사` | 동일 stale `RowVersion` | 로그 중 하나에 **`601`** |
| 7 | `CON-007` | 예약 교차이동 | `UPDATE_예약변경` S1→S2 | `UPDATE_예약변경` S2→S1 | **`Msg 1205` 및 `Msg 50002` 각 0건** |
| **8** | **`CON-008`** | **접수완료 동시 실행** (스펙 §24.2·§38.5) **오늘 PM Slot · 16:00 전** | `INSERT_예약`(오늘 PM, `WALKIN`) | `UPDATE_접수완료`(같은 Slot 의 RSV 하나) | 해당 Slot `RSV+RCP` = **정확히 20**. 21이면 §24.2 결함 재발 |

`[I]` **`09_Concurrency_Setup.sql` 은 유효업무 없는 전용 수검자 `CONC1`·`CONC2` 를 심는다.** `F001`~`F019` 는 이미 유효업무를 가져 `306` 이 나오고, `T001`~`T017` 은 Rule 테스트가 쓰므로 오염시키면 안 된다. 주민번호는 `T10` 의 무효화 식으로 계산한 값을 쓴다.

`[I]` **`CON-002`·`CON-003`·`CON-005` 는 로그에 `applock rc=1`(대기 후 획득)이 최소 1건 있어야 성립**으로 인정한다(스펙 §38.4). 최종 DB 상태만으로는 "경합 발생"과 "우연한 직렬 실행"을 구분할 수 없어, applock 을 통째로 지워도 전건 PASS 할 수 있다.

`[I]` **`CON-005` 는 패자 세션이 `502` 를 반환했는지 로그에서 함께 확인한다.** 최종 상태가 단일 행이라 "정확히 하나만 성공"을 상태만으로 판정할 수 없다.

`[X]` **경합을 같은 절대시각 진입에만 맡기지 않는다**(스펙 §38.4). `WAITFOR TIME` 만으로는 두 세션이 우연히 직렬 실행될 수 있고, 그러면 `rc=1` 이 0건이 되어 **정상 구현이 FAIL** 한다. Session A 가 대상 자원을 **직접 선점**해 임계구역 체류시간을 강제한다. 이 선점 코드는 **테스트 세션 스크립트에만** 있고 production SP 에는 없다.

```sql
-- tests/10_Concurrency_Session_A.sql 머리
-- barrier 시각이 이미 지났으면 WAITFOR TIME 이 다음 날까지 대기한다. 즉시 중단한다.
IF CONVERT(TIME(0), SYSDATETIME()) >= CONVERT(TIME(0), '$(BarrierTime)')
    THROW 51001, N'barrier 시각이 이미 지났습니다. 다시 실행하십시오.', 1;

WAITFOR TIME '$(BarrierTime)';

BEGIN TRAN;
    -- 대상 자원을 SP 보다 먼저 잡아 B 가 반드시 대기하게 만든다 (rc=1 을 결정적으로 만든다)
    DECLARE @rc INT;
    -- [X] $(LockResource) 를 쓰지 않는다. concurrency-test.sh 가 그 변수를 넘기지 않아
    --     sqlcmd 가 정의되지 않은 변수로 즉시 실패한다. 시나리오 번호로 여기서 만든다.
    DECLARE @Res NVARCHAR(255) =
        CASE @Scen
            WHEN 1 THEN N'HC|SSN|' + CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @Ssn), 2)
            WHEN 2 THEN N'HC|SLOT|20261116|AM'
            WHEN 3 THEN N'HC|PAT|' + CONVERT(NVARCHAR(20), @P)
            WHEN 4 THEN N'HC|PAT|' + CONVERT(NVARCHAR(20), @P)
            WHEN 5 THEN N'HC|WORK|' + CONVERT(NVARCHAR(20), @W)
            WHEN 6 THEN N'HC|WORK|' + CONVERT(NVARCHAR(20), @W)
            WHEN 7 THEN N'HC|SLOT|' + CONVERT(CHAR(8), @D, 112) + N'|' + @S
            WHEN 8 THEN N'HC|SLOT|' + CONVERT(CHAR(8), CONVERT(DATE, SYSDATETIME()), 112) + N'|PM'
        END;
    EXEC @rc = sp_getapplock @Resource = @Res, @LockMode = 'Exclusive',
                             @LockOwner = 'Transaction', @LockTimeout = 5000;
    PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
    WAITFOR DELAY '00:00:03';        -- B 가 이 자원을 기다리는 구간
COMMIT;

-- 선점을 놓은 뒤 실제 SP 를 호출한다
EXEC [dbo].[USP_HC_INSERT_예약] ...;
```

`[X]` **로그 파일명에 run ID 와 시나리오 번호를 넣는다.** 초안은 `conc_A.log` 를 고정한 채 8회를 돌아 **시나리오 8의 로그만 남았다.** G15 가 요구한 *"각 회귀 실행의 run ID·시각"* 과 어긋나고, `rc=1` 이 어느 시나리오에서 났는지 사후 확인할 수 없다. → `conc_${RUN}_${SCEN}_A.log` 형식으로 저장한다.

`tests/12_Concurrency_Verify.sql` 예 (시나리오 2):

```sql
IF @Scen = 2
BEGIN
    DECLARE @Cnt INT = (SELECT COUNT(*) FROM [dbo].[예약접수]
                         WHERE [예약일]='2026-11-16' AND [시간대코드]='AM'
                           AND [상태코드] IN ('RSV','RCP'));
    IF @Cnt = 20 PRINT 'PASS CON-002 19/20 동시예약 2건 → 정확히 1건 성공 (최종 20)';
    ELSE BEGIN PRINT 'FAIL CON-002 최종 인원 ' + CONVERT(VARCHAR(5), @Cnt); SET @Fail += 1; END
END
```

- [ ] **Step 4: 8개 시나리오 순차 실행**

```bash
chmod +x scripts/concurrency-test.sh
for s in 1 2 3 4 5 6 7 8; do
  ./scripts/rebuild.sh >/dev/null
  sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i tests/00_Test_Harness.sql >/dev/null
  ./scripts/concurrency-test.sh "$s" || { echo "시나리오 $s 실패"; exit 1; }
done
```

각 시나리오 전에 rebuild + fixture 재배치를 하므로 **시나리오 간 오염이 없다.**

- [ ] **Step 5: 교착 검사 (시나리오 7)**

```bash
# concurrency-test.sh 가 이미 수치 판정한다. 여기서는 누적 확인만 한다.
iconv -f UTF-16 -t UTF-8 artifacts/logs/conc_A.log artifacts/logs/conc_B.log 2>/dev/null | grep -c 'Msg 1205' || true
```

Expected: `0`. 1205가 나오면 Slot 자원 정렬 획득이 제대로 구현되지 않은 것이다 — `T26` Step 5를 다시 본다.

- [ ] **Step 6: 잠금 timeout 확인**

```bash
iconv -f UTF-16 -t UTF-8 artifacts/logs/conc_A.log artifacts/logs/conc_B.log 2>/dev/null | grep -c 'Msg 50001' || true
```

`50001`(applock timeout)이 나오면 5000ms 안에 상대 세션이 끝나지 않은 것이다. 정상 시나리오에서는 0이어야 한다.

- [ ] **Step 7: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/tests/09_Concurrency_Setup.sql database/tests/10_Concurrency_Session_A.sql \
        database/tests/11_Concurrency_Session_B.sql database/tests/12_Concurrency_Verify.sql \
        database/scripts/concurrency-test.sh
git commit -m "test(phase4): 동시성 시나리오 8종 및 barrier 실행 스크립트 추가"
```

**로그 경로:** `artifacts/logs/conc_${RUN}_${SCEN}_A.log`, `…_B.log`, `…_verify.log` (`RUN` = `date +%Y%m%d_%H%M%S`)

**Rollback/Cleanup:** 각 시나리오가 rebuild로 시작하므로 cleanup이 필요 없다.

**완료조건:** 스펙 §45.2 의 `CON` 전건(`CON-001`~`CON-008`) PASS + `CON-001`~`005`·`007` 각각에서 **`applock rc=1` 최소 1건** + `Msg 1205`·`Msg 50002`·`Msg 2627`·`Msg 50001` **각 0건** (스펙 §38.4·§42 G11). 시나리오별 로그 8세트가 `artifacts/logs/conc_${RUN}_${SCEN}_*.log` 로 남아 있어야 한다.

---

## Task T35: `deploy/09_Verify.sql` · `tests/14_Clean_Rebuild_Verify.sql`

**목적:** 배포 직후 객체 인벤토리를 자동 검증하고, 연속 2회 rebuild가 동일 결과를 내는지 확인한다 (G03·G04·G14).

**관련 Baseline 위치:** 스펙 §40·§42.

**선행조건:** `T34` 완료.

**Files:** Create `deploy/09_Verify.sql`, `tests/14_Clean_Rebuild_Verify.sql`

- [ ] **Step 1: `deploy/09_Verify.sql` — 배포 말미 자동 검증**

```sql
SET NOCOUNT ON;
DECLARE @Fail INT = 0;

IF ((SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0) = 6) PRINT 'PASS VER-001 Table 6';
ELSE BEGIN PRINT 'FAIL VER-001 Table'; SET @Fail += 1; END
IF ((SELECT COUNT(*) FROM sys.objects WHERE type='IF' AND name LIKE 'UFN[_]HC[_]%') = 4) PRINT 'PASS VER-002 TVF 4';
ELSE BEGIN PRINT 'FAIL VER-002 TVF'; SET @Fail += 1; END
IF ((SELECT COUNT(*) FROM sys.procedures WHERE name LIKE 'USP[_]HC[_]%') = 16) PRINT 'PASS VER-003 SP 16';
ELSE BEGIN PRINT 'FAIL VER-003 SP'; SET @Fail += 1; END
IF ((SELECT COUNT(*) FROM sys.sequences) = 1) PRINT 'PASS VER-004 Sequence 1';
ELSE BEGIN PRINT 'FAIL VER-004 Sequence'; SET @Fail += 1; END
IF ((SELECT COUNT(*) FROM sys.key_constraints WHERE type='PK') = 6
    AND (SELECT COUNT(*) FROM sys.foreign_keys) = 2
    AND (SELECT COUNT(*) FROM sys.key_constraints WHERE type='UQ') = 2
    AND (SELECT COUNT(*) FROM sys.indexes WHERE is_unique=1 AND has_filter=1) = 1)
    PRINT 'PASS VER-005 PK6/FK2/UQ2/UX1';   -- 사용자 테이블 한정(is_ms_shipped=0). SCH-007·RBD-004 와 같은 기준을 쓴다.
ELSE BEGIN PRINT 'FAIL VER-005 제약 수'; SET @Fail += 1; END
IF ((SELECT COUNT(*) FROM sys.triggers WHERE is_ms_shipped=0) = 0
    AND (SELECT COUNT(*) FROM sys.table_types) = 0)
    PRINT 'PASS VER-006 Trigger 0 / TVP 0';
ELSE BEGIN PRINT 'FAIL VER-006 금지 객체 존재'; SET @Fail += 1; END
IF ((SELECT COUNT(*) FROM [dbo].[검사코드]) = 19 AND (SELECT COUNT(*) FROM [dbo].[휴무일]) = 2)
    PRINT 'PASS VER-007 Seed Exam 19 / Holiday 2';
ELSE BEGIN PRINT 'FAIL VER-007 Seed'; SET @Fail += 1; END

-- [X] 초안은 PRINT 인자에 (SELECT compatibility_level …) 를 직접 넣었다.
--     PRINT 는 스칼라 식만 받으므로 Msg 1046 + Msg 102 로 **배치 전체가 컴파일 실패**하고
--     VER-001~007 이 한 줄도 실행되지 않은 채 exit 1 이 난다.
--     이 파일은 Deploy.sql 이 매 배포 끝마다 부르므로 모든 배포 검증이 깨진다.
--     plans/01 §T04 가 Preflight 에서 같은 실수를 이미 실측으로 기록했는데 여기에 사본이 남아 있었다.
--     하위 쿼리는 변수에 먼저 담는다. DATABASEPROPERTYEX 는 함수라 그대로 둔다.
DECLARE @Compat VARCHAR(10);
SELECT @Compat = CONVERT(VARCHAR(10), compatibility_level) FROM sys.databases WHERE name = DB_NAME();
PRINT 'INFO CompatibilityLevel = ' + @Compat;
PRINT 'INFO Collation          = ' + CONVERT(VARCHAR(80), DATABASEPROPERTYEX(DB_NAME(), 'Collation'));

IF @Fail > 0 THROW 51000, N'배포 검증 실패', 1;
PRINT '=== 09_Verify 완료 ===';
GO
```

- [ ] **Step 2: 전체 배포 실행 (G03)**

```bash
./scripts/rebuild.sh
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/deploy_full.log | grep -E '^(PASS|FAIL|INFO)'
```

Expected: exit 0, `VER-001`~`VER-007` 7건 PASS.

- [ ] **Step 3: `tests/14_Clean_Rebuild_Verify.sql` — 인벤토리 지문 비교**

`[X]` **개수 문자열 지문을 쓰지 않는다.** 객체명·컬럼·정의·권한·Seed 값이 달라도 개수만 같으면 동일 지문이 되어, **잘못된 배포가 두 번 반복되어도 G14 가 PASS** 한다. 정렬된 메타데이터·Seed 덤프를 그대로 출력해 `diff` 한다.

또한 이 파일에는 **단언이 하나도 없었다.** `test.sh` 가 실행하고 `grep '^(PASS|FAIL)'` 로 판정하는데 아무 출력이 없어 항상 조용히 통과했다. 지문 비교 단언을 파일 안에 넣는다.

```sql
SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- (1) 개수 지문 — 빠른 1차 판정
DECLARE @FP VARCHAR(200) = 'INVENTORY|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)) + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.objects WHERE type='IF'))      + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.procedures))                    + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.sequences))                     + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.key_constraints WHERE type='PK')) + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.foreign_keys))                  + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.key_constraints WHERE type='UQ')) + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.indexes i2 JOIN sys.tables t2 ON t2.object_id=i2.object_id
                             AND t2.is_ms_shipped=0 WHERE i2.is_unique=1 AND i2.has_filter=1)) + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id
                             AND t.is_ms_shipped=0
                             WHERE i.type=2 AND i.is_primary_key=0 AND i.is_unique_constraint=0 AND i.is_unique=0)) + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.triggers WHERE is_ms_shipped=0)) + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM [dbo].[검사코드]))            + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM [dbo].[휴무일]));

IF @FP = 'INVENTORY|6|4|16|1|6|2|2|1|5|24|8|0|0|19|2'
    PRINT 'PASS RBD-004 인벤토리 지문 일치  ' + @FP;
ELSE BEGIN PRINT 'FAIL RBD-004 인벤토리 지문 불일치  ' + @FP; SET @Fail += 1; END

-- (2) 정렬된 메타데이터 덤프 — diff 용. 개수로는 못 잡는 차이를 잡는다.
PRINT '--- OBJECTS ---';
SELECT 'OBJ|' + o.type + '|' + o.name
FROM sys.objects o WHERE o.is_ms_shipped = 0 AND o.type IN ('U','P','IF','SO','PK','UQ','C','D','F')
ORDER BY o.type, o.name;

PRINT '--- COLUMNS ---';
SELECT 'COL|' + t.name + '|' + c.name + '|' + y.name + '|'
     + CONVERT(VARCHAR(6), c.max_length) + '|' + CONVERT(VARCHAR(1), c.is_nullable)
FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id
JOIN sys.types y ON y.user_type_id=c.user_type_id
WHERE t.is_ms_shipped = 0 ORDER BY t.name, c.column_id;

PRINT '--- INDEXES ---';
SELECT 'IDX|' + t.name + '|' + i.name + '|' + CONVERT(VARCHAR(1), i.is_unique)
     + '|' + CONVERT(VARCHAR(1), i.has_filter)
     + '|' + CONVERT(VARCHAR(3), k.key_ordinal) + '|' + c2.name
FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id AND t.is_ms_shipped=0
-- [X] 초안은 CROSS APPLY (SELECT name_list = MAX(c2.name) … WHERE k.key_ordinal = 1) 이었다.
--     첫 Key 컬럼 하나만 기록하므로 (Name, Birthday) 가 (Name, Gender) 로 바뀌어도 지문이 동일하다.
--     이름이 name_list 라 전건 목록으로 오해하기도 쉽다. INCLUDE 컬럼도 전혀 잡지 못한다.
--     → index_columns 를 행 단위로 펼쳐 key_ordinal 과 컬럼명을 그대로 덤프한다.
JOIN sys.index_columns k
             JOIN sys.columns c2 ON c2.object_id=k.object_id AND c2.column_id=k.column_id
             WHERE k.object_id=i.object_id AND k.index_id=i.index_id AND k.key_ordinal=1) ic
WHERE i.name IS NOT NULL ORDER BY t.name, i.name;

PRINT '--- GRANTS ---';
SELECT 'GRT|' + pr.name + '|' + dp.permission_name + '|' + OBJECT_NAME(dp.major_id)
FROM sys.database_permissions dp
JOIN sys.database_principals pr ON pr.principal_id = dp.grantee_principal_id
WHERE dp.state = 'G' AND dp.major_id > 0 ORDER BY pr.name, OBJECT_NAME(dp.major_id);

PRINT '--- SEED ---';
SELECT 'SEED|EXAM|' + [검사항목코드] + '|' + [검사항목명] + '|' + ISNULL([국가검사규칙코드],'-')
     + '|' + ISNULL([추가검사코드],'-') + '|' + ISNULL([추가검사성별코드],'-')
     + '|' + CONVERT(VARCHAR(1), [추가검사사용여부])
FROM [dbo].[검사코드] ORDER BY [검사항목코드];
SELECT 'SEED|HOL|' + CONVERT(VARCHAR(10), [휴무일자], 23) + '|' + [휴무일명]
     + '|' + CONVERT(VARCHAR(1), [사용여부])
FROM [dbo].[휴무일] ORDER BY [휴무일자];

IF @Fail > 0 THROW 51000, N'Clean Rebuild 검증 실패', 1;
PRINT '=== 14_Clean_Rebuild_Verify 완료 ===';
GO
```

- [ ] **Step 4: 연속 2회 rebuild 후 지문 비교 (G14)**

```bash
./scripts/rebuild.sh >/dev/null
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W \
       -i tests/14_Clean_Rebuild_Verify.sql > artifacts/reports/inventory_run1.txt

./scripts/rebuild.sh >/dev/null
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W \
       -i tests/14_Clean_Rebuild_Verify.sql > artifacts/reports/inventory_run2.txt

diff -u artifacts/reports/inventory_run1.txt artifacts/reports/inventory_run2.txt \
  && echo "PASS RBD-005 2회 rebuild 인벤토리 동일" \
  || { echo "FAIL RBD-005 인벤토리 불일치"; exit 1; }
cp artifacts/reports/inventory_run2.txt artifacts/reports/object-inventory.txt
```

Expected: `PASS RBD-005`, 지문 = `INVENTORY|6|4|16|1|6|2|2|1|5|24|8|0|0|19|2`

- [ ] **Step 5: 안전가드 음성 검증 — `RBD-002` 만 (RBD-001 은 `NOT RUN`)**

```bash
RC=0
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i Rebuild.sql \
  -o artifacts/logs/rebuild_red.log || RC=$?
iconv -f UTF-16 -t UTF-8 artifacts/logs/rebuild_red.log | grep 'Msg 50021' \
  && echo "PASS RBD-002 master 컨텍스트 강제 (Msg 50021)" \
  || { echo "FAIL RBD-002"; exit 1; }
echo "exit=$RC   (1 이어야 한다)"
echo "NOT RUN RBD-001 잘못된 서버명(50020) — 인스턴스가 1개뿐이라 음성 시험 불가"
```

`[X]` **초안은 이 한 번의 실행으로 `RBD-001`·`RBD-002` 두 Gate 를 모두 검증한 것처럼 적었다.** 실제로 관측되는 것은 `50012`(당시 번호) 하나뿐이고 `50010`·`50011` 은 발화하지 않았다. 게다가 `50011` 가드는 `DECLARE @TargetDb = N'…'; IF @TargetDb <> N'…'` 라는 **항진 명제**여서 원리적으로 발화 불가능했다. 가드 번호대를 `50020~50024` 로 분리하고(스펙 §8.3), `RBD-001` 은 정직하게 `NOT RUN` 으로 남긴다.

- [ ] **Step 5b: 나머지 Clean Rebuild 계약 (`RBD-003`·`RBD-006`·`RBD-007`·`RBD-008`·`RBD-010`)**

`[X]` 초안은 `RBD-001`·`002`·`004`·`005`·`009` 만 명시 판정했다. 스펙 §40 은 10건이고, 특히 `RBD-007`(Deploy 단독 재실행)·`RBD-008`(Procedure 파일만 재실행 후 GRANT 유지)은 **재배포 안전성**을 보는 유일한 시험이라 빠지면 G14 가 성립하지 않는다.

```bash
RC=0

# RBD-003  빈 DB 에서 Deploy.sql 전체 실행 → exit 0
./scripts/rebuild.sh > artifacts/logs/rbd003.log 2>&1; RC=$?
[ "$RC" -eq 0 ] && echo "PASS RBD-003 빈 DB 에서 Deploy 전체 실행 exit 0" \
                || { echo "FAIL RBD-003 exit=$RC"; exit 1; }

# RBD-006  2회 Rebuild 후 Seed 19행 전건 값 동일 (개수가 아니라 값이다)
sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -o artifacts/logs/seed_now.txt \
  -Q "SET NOCOUNT ON; SELECT ExamItemCode+'|'+ExamItemName+'|'+ISNULL(NexRuleCode,'-')+'|'+CONVERT(VARCHAR(1),AdditionalActive) FROM dbo.검사코드 ORDER BY ExamItemCode;
      SELECT CONVERT(VARCHAR(10),HolidayDate,120)+'|'+HolidayName FROM dbo.휴무일 ORDER BY HolidayDate;"
diff artifacts/logs/seed_first.txt artifacts/logs/seed_now.txt \
  && echo "PASS RBD-006 2회 Rebuild 후 Seed 19+2행 값까지 동일" \
  || { echo "FAIL RBD-006 Seed 값 불일치"; RC=1; }

# RBD-007  Deploy.sql 단독 재실행 (DB 유지) → exit 0 + 덤프 동일
./scripts/deploy.sh > artifacts/logs/rbd007.log 2>&1; D7=$?
sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -o artifacts/logs/dump_after7.txt -i tests/14_Clean_Rebuild_Verify.sql
if [ "$D7" -eq 0 ] && diff artifacts/logs/dump_second.txt artifacts/logs/dump_after7.txt > /dev/null; then
  echo "PASS RBD-007 Deploy 단독 재실행 exit 0 + 덤프 동일"
else
  echo "FAIL RBD-007 exit=$D7 또는 덤프 불일치"; RC=1
fi

# RBD-008  03~07 Procedure 파일만 단독 재실행 → exit 0 + 덤프의 GRANT 구획 전후 동일
for f in deploy/03_*.sql deploy/04_*.sql deploy/05_*.sql deploy/06_*.sql deploy/07_*.sql; do
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -i "$f" >> artifacts/logs/rbd008.log 2>&1 || RC=1
done
G=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON;
     SELECT COUNT(*) FROM sys.database_permissions dp
     JOIN sys.objects o ON o.object_id = dp.major_id
     JOIN sys.database_principals u ON u.principal_id = dp.grantee_principal_id
     WHERE dp.permission_name='EXECUTE' AND dp.state='G' AND o.type='P' AND u.name='HC_APP_TEST';" | tr -d ' \r')
[ "${G:-0}" -eq 15 ] && echo "PASS RBD-008 Procedure 재실행 후 GRANT 15건 유지" \
                     || { echo "FAIL RBD-008 GRANT ${G}/15 — CREATE OR ALTER 가 권한을 지웠다"; RC=1; }

# RBD-010  로그·보고서 secret — SEC-010 과 동일 판정 로직을 재사용한다
./scripts/verify-no-secret.sh && echo "PASS RBD-010 Rebuild 산출물에 secret 0건" || RC=1

exit $RC
```

`[I]` `RBD-008` 이 `GRANT` 를 세는 이유: `CREATE OR ALTER PROCEDURE` 는 기존 권한을 **유지**하지만 `DROP` + `CREATE` 는 **지운다**. 배포 스크립트가 어느 쪽인지 문서가 아니라 숫자로 확인한다.

`[I]` `seed_first.txt`·`dump_second.txt` 는 `Step 4`(2회 Rebuild 비교)에서 만든 파일을 그대로 쓴다.

- [ ] **Step 6: 타 DB 무사 확인 (`RBD-009`)**

아래 명령은 `T04` Step 7 과 **한 글자도 다르지 않아야 한다.** 출력 파일 이름만 `_after` 다.

`[X]` 초안은 (1) 문자열 연결(`name + '|' + …`)이라 `sysname` 의 catalog collation 과 리터럴의 DB collation 이 `add` 연산자에서 충돌해 **`Msg 451`** 로 실패했고(실측 확인), (2) `collation_name` 이 `AUTO_CLOSE ON` 때문에 DB 개폐 상태에 따라 값이 바뀌었으며, (3) 행 집합(`<> 대상DB`)과 컬럼 구성이 `T04` Step 7 과 달라 **`diff` 가 성립할 수 없었다.** 세 가지를 `T04` 쪽에 맞춰 고쳤다.

```bash
# rebuild 전에 한 번, 후에 한 번 찍어 diff 한다. 이름 존재 확인만으로는 증거가 되지 않는다.
sqlcmd -S '.\SQLEXPRESS' -E -d master -b -I -h -1 -W -s"|" \
  -Q "SET NOCOUNT ON; SELECT name, state_desc, user_access_desc, CONVERT(VARCHAR(1), CONVERT(INT, is_read_only)), CONVERT(VARCHAR(30), create_date, 126) FROM sys.databases WHERE name NOT IN (N'HealthCheckupReservationReceptionDb', N'tempdb') ORDER BY database_id;" \
  -o artifacts/reports/otherdb_after.txt
diff -u artifacts/reports/otherdb_before.txt artifacts/reports/otherdb_after.txt \
  && echo "PASS RBD-009 타 DB 메타데이터 불변" \
  || { echo "FAIL RBD-009 타 DB 변경 감지"; exit 1; }
```

Expected: `PASS RBD-009`. `otherdb_before.txt` 는 **`T04` Step 7 이 만든다** — 이 Task 가 아니라 거기에 생성 Step 이 있어야 하고, 없으면 `diff` 대상이 없어 `RBD-009` 는 실행 불가다.

`[X]` 초안은 `SELECT name FROM sys.databases` 로 **이름 존재만** 확인하고 "변경 0건" 이라고 적었다. DB 가 존재한다는 사실은 상태·옵션·접근모드가 안 바뀌었다는 증거가 아니다. 다만 `DROP DATABASE` 식별자가 과제 DB 로 하드코딩되어 있어 `Net461MvpSample` 을 직접 삭제하는 경로는 존재하지 않는다 — 표현을 **"존재 및 관찰한 메타데이터 불변"** 으로 제한한다.

- [ ] **Step 7: Commit** — `test(phase4): 배포 검증 및 Clean Rebuild 재현성 확인 추가`

**완료조건:** `VER-001`~`VER-007` PASS, 2회 rebuild 지문 동일, `Net461MvpSample` 무사.

---

## Task T36: 16개 SP 전체 계약 검증

**목적:** `T22` 의 검증기를 16개 SP 전부로 확장해 G09를 충족한다.

**관련 Baseline 위치:** `05` §17.9, 스펙 §36.

**선행조건:** `T35` 완료.

**Files:** Modify `tools/expected-contracts.json`, Create `tests/contract/12_*.sql` ~ `20_*.sql`

- [ ] **Step 1: Write SP 시나리오 추가**

Write SP는 **성공 경로와 실패 경로를 각각** 호출한다. 실패 경로는 RS0 1개만 나와야 한다.

```text
12_INSERT_수검자_성공        RS0 + RS1(1행)
13_INSERT_수검자_202         RS0 + RS1(1행)   ← 실패인데 RS1 동반
14_INSERT_수검자_100         RS0 만
15_UPDATE_수검자정보_성공     RS0 + RS1(1행)
16_INSERT_예약_성공          RS0 + RS1(1행)
17_INSERT_예약_실패          RS0 만
18_UPDATE_예약변경_Noop      RS0 + RS1(1행)
19_UPDATE_접수완료_실패       RS0 만
20_UPDATE_접수추가검사_Noop   RS0 + RS1(1행)
21_UPDATE_예약취소_성공       RS0 + RS1(1행)   ← [X] 초안 누락
22_UPDATE_접수취소_성공       RS0 + RS1(1행)   ← [X] 초안 누락
```

`[X]` **초안은 15개가 아니라 13개 SP 만 검증했다.** `T22` 의 SELECT 7종 + 위 목록의 Write 6종 = 13이고, `UPDATE_예약취소`·`UPDATE_접수취소` 가 빠져 있었다. 스펙 §42 G09 의 *"15/15 SP"* 가 계획대로 실행해도 충족되지 않는다. 두 시나리오를 추가해 15/15 로 채운다.

- [ ] **Step 2: Parameter 계약 검증 (SQL만)**

```sql
-- [X] 초안은 SELECT 로 덤프만 하고 눈으로 보라고 했다. 이름·순서·타입이 틀려도 조회는 정상 종료하므로
--     완료조건 "Parameter 95개 일치" 를 근거 없이 주장할 수 있다. EXCEPT 양방향 + THROW 로 바꾼다.
DECLARE @ExpParam TABLE (SpName SYSNAME, Ord INT, ParamName SYSNAME, TypeName VARCHAR(50), PRIMARY KEY (SpName, Ord));
INSERT INTO @ExpParam (SpName, Ord, ParamName, TypeName) VALUES
 -- 95행 전건. 기준선 05 §7~§12 에서 기계 생성했다(Write SP 8개의 @OperatorName 포함).
 -- USP_HC_SELECT_공통업무상태 는 무인자라 이 표에 나타나지 않는다 (14 SP × 95 Parameter).
 -- USP_HC_SELECT_수검자목록 5개
 (N'USP_HC_SELECT_수검자목록',  1, N'@ChartNo',               'nvarchar(100)'),
 (N'USP_HC_SELECT_수검자목록',  2, N'@Name',                  'nvarchar(100)'),
 (N'USP_HC_SELECT_수검자목록',  3, N'@SocialNumber',          'varchar(13)'),
 (N'USP_HC_SELECT_수검자목록',  4, N'@Birthday',              'varchar(8)'),
 (N'USP_HC_SELECT_수검자목록',  5, N'@MobilePhone',           'varchar(13)'),
 -- USP_HC_SELECT_수검자상세 1개
 (N'USP_HC_SELECT_수검자상세',  1, N'@PatientId',             'bigint'),
 -- USP_HC_SELECT_수검자유효업무 1개
 (N'USP_HC_SELECT_수검자유효업무',  1, N'@PatientId',             'bigint'),
 -- USP_HC_SELECT_예약접수목록 5개
 (N'USP_HC_SELECT_예약접수목록',  1, N'@FromDate',              'date'),
 (N'USP_HC_SELECT_예약접수목록',  2, N'@ToDate',                'date'),
 (N'USP_HC_SELECT_예약접수목록',  3, N'@Status',                'char(3)'),
 (N'USP_HC_SELECT_예약접수목록',  4, N'@ChartNo',               'nvarchar(100)'),
 (N'USP_HC_SELECT_예약접수목록',  5, N'@Name',                  'nvarchar(100)'),
 -- USP_HC_SELECT_예약접수상세 1개
 (N'USP_HC_SELECT_예약접수상세',  1, N'@WorkId',                'bigint'),
 -- USP_HC_SELECT_예약가능정보 13개
 (N'USP_HC_SELECT_예약가능정보',  1, N'@PatientId',             'bigint'),
 (N'USP_HC_SELECT_예약가능정보',  2, N'@WorkId',                'bigint'),
 (N'USP_HC_SELECT_예약가능정보',  3, N'@RowVersion',            'binary(8)'),
 (N'USP_HC_SELECT_예약가능정보',  4, N'@ReservationType',       'varchar(10)'),
 (N'USP_HC_SELECT_예약가능정보',  5, N'@ReservationDate',       'date'),
 (N'USP_HC_SELECT_예약가능정보',  6, N'@TimeSlot',              'char(2)'),
 (N'USP_HC_SELECT_예약가능정보',  7, N'@AexOpt01Selected',      'bit'),
 (N'USP_HC_SELECT_예약가능정보',  8, N'@AexOpt02Selected',      'bit'),
 (N'USP_HC_SELECT_예약가능정보',  9, N'@AexOpt03Selected',      'bit'),
 (N'USP_HC_SELECT_예약가능정보', 10, N'@AexOpt04Selected',      'bit'),
 (N'USP_HC_SELECT_예약가능정보', 11, N'@AexOpt05Selected',      'bit'),
 (N'USP_HC_SELECT_예약가능정보', 12, N'@AexOpt06Selected',      'bit'),
 (N'USP_HC_SELECT_예약가능정보', 13, N'@AexOpt07Selected',      'bit'),
 -- USP_HC_INSERT_수검자 12개
 (N'USP_HC_INSERT_수검자',  1, N'@AutoChartNo',           'bit'),
 (N'USP_HC_INSERT_수검자',  2, N'@ChartNo',               'nvarchar(100)'),
 (N'USP_HC_INSERT_수검자',  3, N'@Name',                  'nvarchar(100)'),
 (N'USP_HC_INSERT_수검자',  4, N'@SocialNumber',          'varchar(13)'),
 (N'USP_HC_INSERT_수검자',  5, N'@MobilePhone',           'varchar(13)'),
 (N'USP_HC_INSERT_수검자',  6, N'@Phone',                 'varchar(13)'),
 (N'USP_HC_INSERT_수검자',  7, N'@Email',                 'varchar(200)'),
 (N'USP_HC_INSERT_수검자',  8, N'@Zipcode',               'varchar(10)'),
 (N'USP_HC_INSERT_수검자',  9, N'@Address',               'nvarchar(200)'),
 (N'USP_HC_INSERT_수검자', 10, N'@AddressDetail',         'nvarchar(200)'),
 (N'USP_HC_INSERT_수검자', 11, N'@Memo',                  'nvarchar(max)'),
 (N'USP_HC_INSERT_수검자', 12, N'@ConfirmSimilarPatient', 'bit'),
 (N'USP_HC_INSERT_수검자', 13, N'@OperatorName',          'nvarchar(50)'),
 -- USP_HC_UPDATE_수검자정보 12개
 (N'USP_HC_UPDATE_수검자정보',  1, N'@PatientId',             'bigint'),
 (N'USP_HC_UPDATE_수검자정보',  2, N'@LastEditDate',          'datetime'),
 (N'USP_HC_UPDATE_수검자정보',  3, N'@ChartNo',               'nvarchar(100)'),
 (N'USP_HC_UPDATE_수검자정보',  4, N'@Name',                  'nvarchar(100)'),
 (N'USP_HC_UPDATE_수검자정보',  5, N'@SocialNumber',          'varchar(13)'),
 (N'USP_HC_UPDATE_수검자정보',  6, N'@MobilePhone',           'varchar(13)'),
 (N'USP_HC_UPDATE_수검자정보',  7, N'@Phone',                 'varchar(13)'),
 (N'USP_HC_UPDATE_수검자정보',  8, N'@Email',                 'varchar(200)'),
 (N'USP_HC_UPDATE_수검자정보',  9, N'@Zipcode',               'varchar(10)'),
 (N'USP_HC_UPDATE_수검자정보', 10, N'@Address',               'nvarchar(200)'),
 (N'USP_HC_UPDATE_수검자정보', 11, N'@AddressDetail',         'nvarchar(200)'),
 (N'USP_HC_UPDATE_수검자정보', 12, N'@Memo',                  'nvarchar(max)'),
 (N'USP_HC_UPDATE_수검자정보', 13, N'@OperatorName',          'nvarchar(50)'),
 -- USP_HC_INSERT_예약 11개
 (N'USP_HC_INSERT_예약',  1, N'@PatientId',             'bigint'),
 (N'USP_HC_INSERT_예약',  2, N'@ReservationType',       'varchar(10)'),
 (N'USP_HC_INSERT_예약',  3, N'@ReservationDate',       'date'),
 (N'USP_HC_INSERT_예약',  4, N'@TimeSlot',              'char(2)'),
 (N'USP_HC_INSERT_예약',  5, N'@AexOpt01Selected',      'bit'),
 (N'USP_HC_INSERT_예약',  6, N'@AexOpt02Selected',      'bit'),
 (N'USP_HC_INSERT_예약',  7, N'@AexOpt03Selected',      'bit'),
 (N'USP_HC_INSERT_예약',  8, N'@AexOpt04Selected',      'bit'),
 (N'USP_HC_INSERT_예약',  9, N'@AexOpt05Selected',      'bit'),
 (N'USP_HC_INSERT_예약', 10, N'@AexOpt06Selected',      'bit'),
 (N'USP_HC_INSERT_예약', 11, N'@AexOpt07Selected',      'bit'),
 (N'USP_HC_INSERT_예약', 12, N'@OperatorName',          'nvarchar(50)'),
 -- USP_HC_UPDATE_예약변경 11개
 (N'USP_HC_UPDATE_예약변경',  1, N'@WorkId',                'bigint'),
 (N'USP_HC_UPDATE_예약변경',  2, N'@RowVersion',            'binary(8)'),
 (N'USP_HC_UPDATE_예약변경',  3, N'@ReservationDate',       'date'),
 (N'USP_HC_UPDATE_예약변경',  4, N'@TimeSlot',              'char(2)'),
 (N'USP_HC_UPDATE_예약변경',  5, N'@AexOpt01Selected',      'bit'),
 (N'USP_HC_UPDATE_예약변경',  6, N'@AexOpt02Selected',      'bit'),
 (N'USP_HC_UPDATE_예약변경',  7, N'@AexOpt03Selected',      'bit'),
 (N'USP_HC_UPDATE_예약변경',  8, N'@AexOpt04Selected',      'bit'),
 (N'USP_HC_UPDATE_예약변경',  9, N'@AexOpt05Selected',      'bit'),
 (N'USP_HC_UPDATE_예약변경', 10, N'@AexOpt06Selected',      'bit'),
 (N'USP_HC_UPDATE_예약변경', 11, N'@AexOpt07Selected',      'bit'),
 (N'USP_HC_UPDATE_예약변경', 12, N'@OperatorName',          'nvarchar(50)'),
 -- USP_HC_UPDATE_예약취소 2개
 (N'USP_HC_UPDATE_예약취소',  1, N'@WorkId',                'bigint'),
 (N'USP_HC_UPDATE_예약취소',  2, N'@RowVersion',            'binary(8)'),
 (N'USP_HC_UPDATE_예약취소', 3, N'@OperatorName',          'nvarchar(50)'),
 -- USP_HC_UPDATE_접수완료 2개
 (N'USP_HC_UPDATE_접수완료',  1, N'@WorkId',                'bigint'),
 (N'USP_HC_UPDATE_접수완료',  2, N'@RowVersion',            'binary(8)'),
 (N'USP_HC_UPDATE_접수완료', 3, N'@OperatorName',          'nvarchar(50)'),
 -- USP_HC_UPDATE_접수추가검사 9개
 (N'USP_HC_UPDATE_접수추가검사',  1, N'@WorkId',                'bigint'),
 (N'USP_HC_UPDATE_접수추가검사',  2, N'@RowVersion',            'binary(8)'),
 (N'USP_HC_UPDATE_접수추가검사',  3, N'@AexOpt01Selected',      'bit'),
 (N'USP_HC_UPDATE_접수추가검사',  4, N'@AexOpt02Selected',      'bit'),
 (N'USP_HC_UPDATE_접수추가검사',  5, N'@AexOpt03Selected',      'bit'),
 (N'USP_HC_UPDATE_접수추가검사',  6, N'@AexOpt04Selected',      'bit'),
 (N'USP_HC_UPDATE_접수추가검사',  7, N'@AexOpt05Selected',      'bit'),
 (N'USP_HC_UPDATE_접수추가검사',  8, N'@AexOpt06Selected',      'bit'),
 (N'USP_HC_UPDATE_접수추가검사',  9, N'@AexOpt07Selected',      'bit'),
 (N'USP_HC_UPDATE_접수추가검사', 10, N'@OperatorName',          'nvarchar(50)'),
 -- USP_HC_UPDATE_접수취소 2개
 (N'USP_HC_UPDATE_접수취소',  1, N'@WorkId',                'bigint'),
 (N'USP_HC_UPDATE_접수취소',  2, N'@RowVersion',            'binary(8)'),
 (N'USP_HC_UPDATE_접수취소', 3, N'@OperatorName',          'nvarchar(50)');

;WITH Act AS (
    SELECT SpName = p.name, Ord = pa.parameter_id, ParamName = pa.name,
           TypeName = t.name + CASE WHEN t.name IN ('varchar','nvarchar','char','nchar','binary','varbinary')
                THEN '(' + CONVERT(VARCHAR(10), CASE WHEN pa.max_length = -1 THEN -1
                        WHEN t.name IN ('nvarchar','nchar') THEN pa.max_length / 2
                        ELSE pa.max_length END) + ')' ELSE '' END
    FROM sys.procedures p
    JOIN sys.parameters pa ON pa.object_id = p.object_id
    JOIN sys.types t ON t.user_type_id = pa.user_type_id
    WHERE p.name LIKE 'USP[_]HC[_]%'
)
IF NOT EXISTS (SELECT SpName, Ord, ParamName, TypeName FROM @ExpParam
               EXCEPT SELECT SpName, Ord, ParamName, TypeName FROM Act)
   AND NOT EXISTS (SELECT SpName, Ord, ParamName, TypeName FROM Act
                   EXCEPT SELECT SpName, Ord, ParamName, TypeName FROM @ExpParam)
    PRINT 'PASS Parameter 95개 이름·순서·타입 전건 일치';
ELSE
BEGIN
    PRINT N'FAIL Parameter 계약 불일치 — 아래 차집합';
    SELECT '기대에만 있음' AS Side, * FROM (SELECT SpName, Ord, ParamName, TypeName FROM @ExpParam
                                             EXCEPT SELECT SpName, Ord, ParamName, TypeName FROM Act) a;
    SELECT '실측에만 있음' AS Side, * FROM (SELECT SpName, Ord, ParamName, TypeName FROM Act
                                             EXCEPT SELECT SpName, Ord, ParamName, TypeName FROM @ExpParam) b;
    THROW 51000, N'Parameter 계약 불일치', 1;
END
```

기대값 (`05` 계약):

| SP | Parameter 수 |
|---|---:|
| `USP_HC_SELECT_공통업무상태` | 0 |
| `USP_HC_SELECT_수검자목록` | 5 |
| `USP_HC_SELECT_수검자상세` | 1 |
| `USP_HC_INSERT_수검자` | 12 |
| `USP_HC_UPDATE_수검자정보` | 12 |
| `USP_HC_SELECT_수검자유효업무` | 1 |
| `USP_HC_SELECT_예약가능정보` | 13 |
| `USP_HC_INSERT_예약` | 11 |
| `USP_HC_UPDATE_예약변경` | 11 |
| `USP_HC_UPDATE_예약취소` | 2 |
| `USP_HC_SELECT_예약접수목록` | 5 |
| `USP_HC_SELECT_예약접수상세` | 1 |
| `USP_HC_UPDATE_접수완료` | 2 |
| `USP_HC_UPDATE_접수추가검사` | 9 |
| `USP_HC_UPDATE_접수취소` | 2 |
| **합계** | **87** |

- [ ] **Step 3: RS0 메타데이터 15/15 검증 (SQL만)**

```sql
DECLARE @Rs0 TABLE (SpName SYSNAME, Ordinal INT NULL, ColName SYSNAME NULL,
                    TypeName NVARCHAR(256) NULL, ErrNo INT NULL);

INSERT INTO @Rs0 (SpName, Ordinal, ColName, TypeName, ErrNo)
SELECT p.name, r.column_ordinal, r.name, r.system_type_name, r.error_number
FROM sys.procedures p
OUTER APPLY sys.dm_exec_describe_first_result_set_for_object(p.object_id, NULL) r
WHERE p.name LIKE 'USP[_]HC[_]%';

DECLARE @Expected TABLE (Ordinal INT PRIMARY KEY, ColName SYSNAME, TypeName NVARCHAR(256));
INSERT INTO @Expected VALUES
 (1, N'Success',    N'bit'),          (2, N'Code',       N'int'),
 (3, N'Message',    N'nvarchar(300)'),(4, N'Field',      N'varchar(50)'),
 (5, N'ServerTime', N'datetime2(7)');

-- (1) 총 행수 = 15 SP × 5컬럼 = 75
-- [X] ELSE 쪽 PRINT 인자에 (SELECT COUNT(*) FROM @Rs0) 이 들어 있었다. Msg 1046 은 컴파일 오류라
--     ELSE 를 타지 않아도 이 배치가 통째로 죽는다. 개수를 변수에 먼저 담는다.
DECLARE @Rs0Cnt INT = (SELECT COUNT(*) FROM @Rs0);
IF (@Rs0Cnt = 75) PRINT 'PASS CTR-RS0-A 총 75행';
ELSE BEGIN PRINT 'FAIL CTR-RS0-A 총 ' + CONVERT(VARCHAR(5), @Rs0Cnt) + '행'; SET @Fail += 1; END

-- (2) DMV 가 결과셋을 결정하지 못한 SP 0건
IF NOT EXISTS (SELECT 1 FROM @Rs0 WHERE ErrNo IS NOT NULL OR Ordinal IS NULL)
    PRINT 'PASS CTR-RS0-B 전 SP 의 RS0 를 DMV 가 결정함';
ELSE
BEGIN
    PRINT 'FAIL CTR-RS0-B RS0 결정 실패 SP 존재';
    SELECT SpName, ErrNo FROM @Rs0 WHERE ErrNo IS NOT NULL OR Ordinal IS NULL;
    SET @Fail += 1;
END

-- (3) 각 SP 의 5컬럼 집합이 기대와 EXCEPT 양방향 일치
IF NOT EXISTS (SELECT SpName FROM @Rs0 s
                WHERE EXISTS (SELECT e.Ordinal, e.ColName, e.TypeName FROM @Expected e
                              EXCEPT SELECT r.Ordinal, r.ColName, r.TypeName FROM @Rs0 r WHERE r.SpName = s.SpName)
                   OR EXISTS (SELECT r.Ordinal, r.ColName, r.TypeName FROM @Rs0 r WHERE r.SpName = s.SpName
                              EXCEPT SELECT e.Ordinal, e.ColName, e.TypeName FROM @Expected e))
    PRINT 'PASS CTR-RS0-C 16개 SP 의 RS0 컬럼·순서·타입 전건 일치';
ELSE BEGIN PRINT 'FAIL CTR-RS0-C RS0 스키마 불일치 SP 존재'; SET @Fail += 1; END
```

`[X]` **초안의 `CROSS APPLY` + `WHERE NOT (…)` 는 거짓 양성이었다.** DMV가 결과셋을 결정하지 못하면 `name`/`column_ordinal` 이 `NULL` 인 error 행을 돌려주는데, `NULL` 비교가 `UNKNOWN` → `NOT UNKNOWN = UNKNOWN` 이라 `COUNT(*)` 에 잡히지 않는다. `CROSS APPLY` 라 0행을 내는 SP 는 아예 사라진다. **SP 가 14개여도, RS0 이 완전히 깨져 있어도 `@Bad = 0` → PASS** 했다. `OUTER APPLY` + 총 행수 75 + `error_number` + `EXCEPT` 양방향 세 단계로 교체한다.

- [ ] **Step 4: 후속 RS 전체 검증 실행**

```bash
: > artifacts/reports/contract-verify.txt
for f in tests/contract/*.sql; do
  k=$(basename "$f" .sql)
  sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -W -w 65535 -s"|" \
         -i "$f" -o "artifacts/logs/rs_${k}.txt"
  # [X] 2>&1 이 없으면 verify-contract.js 가 console.error 로 내는 FAIL 이 증거파일에 안 남는다.
  #     T22 가 지적한 "증거파일에 구조적으로 PASS 만 기록된다" 와 같은 결함이다.
  #     실제로는 scripts/verify-contract-all.sh 를 쓴다 — 그쪽이 이미 올바른 형태다.
  node tools/verify-contract.js "artifacts/logs/rs_${k}.txt" "$k" >> artifacts/reports/contract-verify.txt 2>&1 || RC=1
done
cat artifacts/reports/contract-verify.txt
grep -c '^PASS' artifacts/reports/contract-verify.txt
```

Expected: 전부 `PASS`, exit 0.

- [ ] **Step 5: Commit** — `test(phase4): 16개 SP 전체 Result Set·Parameter 계약 검증`

**완료조건:** Parameter 95행 `EXCEPT` 차집합 0, RS0 메타 75행 + `error_number` 0건, **15/15 SP** 의 후속 RS 시나리오 전부 PASS, 관측 RS0 `Code` 가 전건 `tools/allowed-codes.json` 의 해당 SP 허용집합 안. **G09 충족.**

- [ ] **Step 5: `tools/allowed-codes.json` 생성 및 미관측 Code 보고**

`[X]` 스펙 §36.4 가 요구한 **`allowed-codes.json` 이 계획 전체에 한 번도 없었다.** 이것이 없으면 `verify-contract.js` 는 컬럼명·RS 개수·행수만 보고 **RS0 의 `Success`·`Code` 를 한 번도 읽지 않는다** — G09 의 절반이 비어 있다.

```json
// tools/allowed-codes.json — 기준선 05 §13 에서 기계 생성했다(범위 표기 a~b 는 전개했다).
{
  "USP_HC_SELECT_공통업무상태": [0],
  "USP_HC_SELECT_수검자목록": [0, 101, 103],
  "USP_HC_SELECT_수검자상세": [0, 100, 200],
  "USP_HC_INSERT_수검자": [0, 2, 100, 101, 102, 201, 202, 203, 206, 308, 309],
  "USP_HC_UPDATE_수검자정보": [0, 1, 100, 101, 102, 200, 201, 204, 205, 308, 309, 600],
  "USP_HC_SELECT_수검자유효업무": [0, 100, 200, 701],
  "USP_HC_SELECT_예약가능정보": [0, 100, 101, 102, 200, 500, 501, 502, 601, 700, 701],
  "USP_HC_INSERT_예약": [0, 100, 101, 102, 200, 300, 301, 302, 303, 304, 305, 306, 308, 309, 400, 401, 410, 411, 412, 700, 701],
  "USP_HC_UPDATE_예약변경": [0, 1, 100, 101, 102, 300, 301, 302, 303, 304, 305, 306, 308, 309, 400, 401, 410, 411, 412, 500, 502, 601, 700, 701],
  "USP_HC_UPDATE_예약취소": [0, 100, 308, 309, 500, 502, 601],
  "USP_HC_SELECT_예약접수목록": [0, 101, 103, 104],
  "USP_HC_SELECT_예약접수상세": [0, 100, 500, 701],
  "USP_HC_UPDATE_접수완료": [0, 100, 304, 308, 309, 500, 502, 503, 601, 701],
  "USP_HC_UPDATE_접수추가검사": [0, 1, 100, 308, 309, 410, 411, 412, 500, 502, 601, 700, 701],
  "USP_HC_UPDATE_접수취소": [0, 100, 308, 309, 500, 502, 601]
}
```

`verify-contract.js` 는 시나리오마다 (1) 관측 RS0 `Code` 가 `expected-contracts.json` 의 `rs0Code` 와 같은가, (2) 그 `Code` 가 해당 SP 의 허용집합 안인가 를 **함께** 판정한다. 마지막에 **Catalog 38개 중 한 번도 관측되지 않은 Code 목록**을 보고서에 남긴다 — `05` §18 이 *"사용되지 않는 ResultCode 0개"* 를 PASS 로 확정했으므로 미관측 목록이 비어 있지 않으면 그 자체가 정보다.

---

## Task T37: 문서 최종화 · 증거 정리

**목적:** 실제 구현과 일치하는 `06_DB_Transaction_Security_Seed.md` FINAL 후보를 작성하고, 기준선·WinForms 불변을 재증명한다 (G13·G15·G16).

**관련 Baseline 위치:** 스펙 §42·§43·§44·§45.

**선행조건:** `T36` 완료.

**Files:**
- Create: `../docs/phase4/06_DB_Transaction_Security_Seed.md`
- Create: `artifacts/reports/phase4-report.md`, `artifacts/reports/test-summary.txt`

**금지사항:** 실행하지 않은 검증을 `PASS` 로 기록하지 않는다. 실패한 항목을 숨기지 않는다.

- [ ] **Step 1: 전체 회귀 실행**

```bash
./scripts/test.sh > artifacts/logs/full_test_run.log 2>&1; RC=$?; cat artifacts/logs/full_test_run.log
echo "exit=$RC"   # tee 를 쓰면 $? 가 tee 의 것(항상 0)이라 회귀 실패가 성공으로 기록된다
```

Expected: exit 0, 모든 테스트 파일 통과.

- [ ] **Step 2: `test-summary.txt` 생성**

```bash
{
  echo "=== Phase 4 테스트 요약 ==="
  echo "생성시각: $(date -Iseconds)"
  echo
  for l in artifacts/logs/test_*.log; do
    p=$(iconv -f UTF-16 -t UTF-8 "$l" 2>/dev/null | grep -c '^PASS' || true)
    f=$(iconv -f UTF-16 -t UTF-8 "$l" 2>/dev/null | grep -c '^FAIL' || true)
    s=$(iconv -f UTF-16 -t UTF-8 "$l" 2>/dev/null | grep -c '^SKIP' || true)
    printf '%-40s PASS=%-4s FAIL=%-4s SKIP=%s\n' "$(basename "$l")" "$p" "$f" "$s"
  done
  echo
  echo "=== 계약 검증 ==="
  cat artifacts/reports/contract-verify.txt
  echo
  echo "=== 객체 인벤토리 ==="
  cat artifacts/reports/object-inventory.txt
} > artifacts/reports/test-summary.txt
cat artifacts/reports/test-summary.txt
```

- [ ] **Step 3: 기준선·WinForms 재검증 (G00·G01)**

```bash
RC=0; ./scripts/verify-baseline.sh >> artifacts/reports/baseline-hash.txt 2>&1 || RC=$?
tail -8 artifacts/reports/baseline-hash.txt
echo "baseline exit=$RC"
./scripts/verify-winforms-unchanged.sh
echo "winforms exit=$?"
cd /d/AIDEV/HealthCheckupReservationReception
git diff --stat baseline-HC-RSV-RCP-20260904-R3 -- docs/baseline winforms
echo "(위 diff 가 비어야 한다)"
```

Expected: baseline `=== 6/6 ===` exit 0, winforms `PASS 변경 0건` exit 0, git diff **빈 출력**.

- [ ] **Step 4: 2025 전용 기능 미사용 확인 (G13)**

G13 은 **세 갈래로 나누어** 기록한다(스펙 §9.3). 블랙리스트 grep 0건을 허용목록 준수 `PASS` 로 승격하지 않는다.

```bash
cd /d/AIDEV/HealthCheckupReservationReception/database

# (b) 블랙리스트 grep — 보조 증거
if grep -rniE 'STRING_SPLIT|STRING_AGG|OPENJSON|FOR JSON|FOR XML|JSON_VALUE|JSON_QUERY|JSON_MODIFY|SESSION_CONTEXT|AT TIME ZONE|CONCAT_WS|TRANSLATE\(|DATEDIFF_BIG|COMPRESS\(|DECOMPRESS\(|GREATEST\(|LEAST\(|GENERATE_SERIES|APPROX_COUNT_DISTINCT|REGEXP_|\bTRIM\(|DECLARE +[A-Za-z_]+ +CURSOR' \
        deploy/ tests/ tools/ Deploy.sql Rebuild.sql; then
  echo "FAIL G13-b 금지 기능 발견"
else
  echo "PASS G13-b 블랙리스트 0건"
fi

# CREATE OR ALTER 는 배포 배관 전용 허용 (D4-004b). deploy/ 밖에 있으면 결함이다.
grep -rl 'CREATE OR ALTER' tests/ tools/ 2>/dev/null && echo "FAIL G13-b CREATE OR ALTER 가 배포 밖에 있음" || echo "PASS G13-b CREATE OR ALTER 는 deploy/ 안에만"
```

`\bTRIM\(` 를 넣되 단어경계를 반드시 쓴다 — 없으면 `LTRIM(`/`RTRIM(` 이 전부 오탐된다. `FOR XML` 과 `CURSOR` 도 추가했다(스펙 §9.2에서 금지로 확정).

```text
(a) 대상 2025 인스턴스 전체 배포·테스트 성공   →  PASS / FAIL   (T35 · T37 Step 1)
(b) 블랙리스트 grep 0건                        →  PASS / FAIL   (위 스크립트)
(c) §9.2 허용목록 준수                          →  REVIEWED      (자동 판정 불가)
```

Expected: `PASS G13-b` 2줄. `(c)` 는 보고서에 **`REVIEWED`** 로만 적는다.

- [ ] **Step 5: `06_DB_Transaction_Security_Seed.md` FINAL 후보 작성**

`06_DB_Transaction_Security_Seed_CANDIDATE.md` 를 기반으로 다음을 갱신한다.

```text
- 상태          CANDIDATE → FINAL / GO (실행 검증 완료)
- 문서 버전      v0.4 → v1.0 (SQL 실행검증 완료 시점에 CANDIDATE 해제)
- §42 Gate 표    PLANNED → 실제 관측 결과(PASS / FAIL / SKIP)로 전부 교체
- §43 알려진 한계 실행 중 확인된 항목 추가 (예: 업무시간 밖 SKIP 건수)
- §44 Deviation  실행 중 새로 발견된 이탈 추가
- §45 판정       실제 증거 기반으로 재작성
- 각 §11~§32 의 구현 명세를 **실제 채택한 SQL과 대조**하여 차이가 있으면 문서를 수정한다
  (반대로 문서에 맞추려고 SQL 을 바꾸지 않는다 — 계약 위반이 아니면 실제 구현이 기준이다)
```

- [ ] **Step 6: `phase4-report.md` 작성**

```text
1. 실행 요약        수행 Task 수 / 총 테스트 건수 / PASS·FAIL·SKIP
2. Gate 판정표      G00~G16 실제 결과 + 증거 파일 경로
3. 실행 명령·exit code 목록
4. Deviation 4건 + 실행 중 추가 발견분
5. 알려진 한계
6. 잔여 결함
7. Phase 5 인계사항  (App.config 의 HealthCheckupDb 키 추가, HC_APP_ROLE 에 login 매핑 절차)
```

- [ ] **Step 7: 최종 Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add docs/phase4/ database/artifacts/reports/
git commit -m "docs(phase4): 06 FINAL 후보 및 Phase 4 실행 보고서 작성"
git log --oneline baseline-HC-RSV-RCP-20260904-R3..HEAD | head -50
```

- [ ] **Step 8: 완료 보고**

다음 형식으로 사용자에게 보고한다.

```text
Baseline integrity   VERIFIED (6/6)
WinForms 변경         0건
Object Inventory      Table 6 / TVF 4 / SP 16 / Sequence 1
Gate 판정             G00~G16 실제 결과
총 테스트             PASS n / FAIL n / SKIP n
Deviation             n건
Implementation blockers  n건
Verdict               PHASE 4 COMPLETE 또는 BLOCKED
```

**회귀시험:** `./scripts/test.sh` 전체 + `./scripts/concurrency-test.sh 1..8`

**로그 경로:** `artifacts/reports/` 전체

**Rollback/Cleanup:** `git switch main` 후 `git branch -D phase4-database` 로 Phase 4 작업 전체를 되돌릴 수 있다.

**완료조건:** baseline 6/6 · WinForms 0건 · G13 금지 기능 0건 · 전체 회귀 exit 0 · FINAL 후보 문서 작성 완료. **하나라도 미충족이면 `BLOCKED` 로 보고하고 `PHASE 4 COMPLETE` 를 선언하지 않는다.**
