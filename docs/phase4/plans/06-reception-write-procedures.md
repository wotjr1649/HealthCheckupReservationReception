# Stage 8 — 접수 Write Stored Procedure 3개

**Index:** `2026-09-04-phase4-database-implementation.md`
**Spec:** `../06_DB_Transaction_Security_Seed_CANDIDATE.md`
**Tasks:** `T28` ~ `T30`

생성 파일은 `deploy/07_Procedures_Reception_Write.sql`, 테스트는 `tests/07_Reception_Write_Tests.sql` 하나다.

**`UPDATE_접수완료` 는 `PAT` → `WORK` → `SLOT` 을 잡는다**(스펙 §24.2 — `RSV→RCP` 가 인덱스 키를 뒤로 이동시켜 정원·중복·EP-08 COUNT 를 과소집계시킬 수 있다). 나머지 두 SP(`접수추가검사`·`접수취소`)는 **`HC|WORK|{WorkId}` 하나만** 잡는다.

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


## Task T28: `[dbo].[USP_HC_UPDATE_접수완료]`

**목적:** `RSV → RCP` 상태전이를 원자적으로 수행한다.

**관련 Baseline 위치:** `05` §12.1, `00` RCP-01~04, `00` §3장 (접수 마감 AM 11:00 / PM 16:00).

**선행조건:** `T27` 완료.

**Files:**
- Create: `deploy/07_Procedures_Reception_Write.sql`
- Create: `tests/07_Reception_Write_Tests.sql`

**Interfaces:** Produces RS0 + RS1 `(WorkId, Status, RowVersion)`. Parameter 2개: `@WorkId BIGINT`, `@RowVersion BINARY(8)`.

**허용 Code:** `0, 100, 304, 308~309, 500, 502~503, 601, 701`

**금지사항:** 접수 성공 시 `ReservationDate`·`TimeSlotCode`·NEX·AEX를 변경하지 않는다. 직접접수용 신규 Work를 만들지 않는다.

- [ ] **Step 1: RED**

**계약 시나리오** — `UPDATE_접수완료`. Parameter(`05` §11.1): `@WorkId, @RowVersion`

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `CWR-001` | `-1, 0x0000000000000001` | `500` | 미존재 `WorkId` |
| `CWR-002` | `@Wf, @Rf` (예약일 미래) | `503` | 오늘이 아닌 예약의 접수 |
| `CWR-003` | `@Wt, 0x0000000000000001` | `601` | stale `RowVersion` |
| `CWR-004` | `@Wcnl, @Rcnl` | `502` | `CNR` 상태 |
| `CWR-005` | `@Wc2, @Rc2` (`CORRUPT-2`) | `701` | 저장 NEX 0행 |
| `CWR-006` | `@Wt, @Rt` (오늘 RSV) | `0` | 접수 성공 → `RCP` 전이 |
| `CWR-007` | `@Wt, @Rt2` (이미 RCP) | `502` | 재접수 |
| `CWR-008` | `@Wpast, @Rpast` | `503` | **과거** 예약일 접수 (`05` §17, 스펙 §33.4) |
| `CWR-009` | `@Wt, @Rt` (마감시각 경과) | `304` | 접수 마감 경계 |
| `CWR-010` | `@Wcnl, @Rcnl` | `502` | `CNR` Work 접수 — `CWR-004` 와 같은 Code, 다른 진입 |
| `CWR-011` | `@Wc4, @Rc4` (`CORRUPT-4`) | `701` | `ExamSourceCode` ↔ Master 역할 불일치 |

`[X]` **`CWR-002` 의 기대는 `503` 하나다.** 초안은 `IN (503, 308, 309)` 였는데, 그러면 업무시간 밖 실행에서 `308`/`309` 로도 PASS 해 **미래 예약일 검증이 실제로 일어났는지 알 수 없다.** 업무시간 가드가 파일 머리에 있으므로 `503` 만 인정한다.

**DB 상태 단언**

```sql
SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- 업무시간 가드 (스펙 §33.2a)
IF NOT (DATEPART(WEEKDAY, SYSDATETIME()) BETWEEN 2 AND 7
        AND CONVERT(TIME(0), SYSDATETIME()) >= '09:00:00'
        AND CONVERT(TIME(0), SYSDATETIME()) <  '18:00:00'
        AND NOT EXISTS (SELECT 1 FROM [dbo].[휴무일]
                         WHERE [HolidayDate] = CONVERT(DATE, SYSDATETIME()) AND [Active] = 1))
BEGIN
    PRINT 'SKIP 07_Reception_Write_Tests 업무시간(월~토 09:00~18:00, 비휴무일) 밖';
    RETURN;
END

-- CWR-001  미존재 WorkId — 아무 행도 만들지 않는다
DECLARE @W0 INT = (SELECT COUNT(*) FROM [dbo].[예약접수]);
EXEC [dbo].[USP_HC_UPDATE_접수완료] -1, 0x0000000000000001;
IF ((SELECT COUNT(*) FROM [dbo].[예약접수]) = @W0)
    PRINT 'PASS CWR-001 미존재 Work 호출이 행을 만들지 않았다';
ELSE BEGIN PRINT 'FAIL CWR-001'; SET @Fail += 1; END

-- CWR-002 / CWR-008  미래·과거 예약일은 접수되지 않는다 (StatusCode 불변)
DECLARE @Wf BIGINT = (SELECT TOP (1) [WorkId] FROM [dbo].[예약접수]
                       WHERE [StatusCode] = 'RSV' AND [ReservationDate] > CONVERT(DATE, SYSDATETIME())
                       ORDER BY [WorkId]);
DECLARE @Rf BINARY(8) = (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wf);
EXEC [dbo].[USP_HC_UPDATE_접수완료] @Wf, @Rf;
IF ((SELECT [StatusCode] FROM [dbo].[예약접수] WHERE [WorkId] = @Wf) = 'RSV')
    PRINT 'PASS CWR-002 미래 예약일이 RCP 로 전이되지 않았다';
ELSE BEGIN PRINT 'FAIL CWR-002 미래 예약이 접수됐다'; SET @Fail += 1; END

DECLARE @Wpast BIGINT = (SELECT TOP (1) [WorkId] FROM [dbo].[예약접수]
                          WHERE [StatusCode] = 'RSV' AND [ReservationDate] < CONVERT(DATE, SYSDATETIME())
                          ORDER BY [WorkId]);
IF @Wpast IS NULL
    PRINT 'SKIP CWR-008 과거 예약일 RSV Work 가 Fixture 에 없다';
ELSE
BEGIN
    DECLARE @Rpast BINARY(8) = (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wpast);
    EXEC [dbo].[USP_HC_UPDATE_접수완료] @Wpast, @Rpast;
    IF ((SELECT [StatusCode] FROM [dbo].[예약접수] WHERE [WorkId] = @Wpast) = 'RSV')
        PRINT 'PASS CWR-008 과거 예약일이 RCP 로 전이되지 않았다';
    ELSE BEGIN PRINT 'FAIL CWR-008 과거 예약이 접수됐다'; SET @Fail += 1; END
END

-- CWR-004 / CWR-010  CNR Work 는 접수되지 않는다
DECLARE @Wcnl BIGINT = (SELECT TOP (1) [WorkId] FROM [dbo].[예약접수]
                         WHERE [StatusCode] = 'CNR' ORDER BY [WorkId]);
DECLARE @Rcnl BINARY(8) = (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wcnl);
EXEC [dbo].[USP_HC_UPDATE_접수완료] @Wcnl, @Rcnl;
IF ((SELECT [StatusCode] FROM [dbo].[예약접수] WHERE [WorkId] = @Wcnl) = 'CNR')
    PRINT 'PASS CWR-004/010 CNR Work 가 접수되지 않았다';
ELSE BEGIN PRINT 'FAIL CWR-004/010 CNR 이 RCP 로 전이됐다'; SET @Fail += 1; END

-- CWR-005  CORRUPT-2 (NEX 0행) 는 701 로 막히고 상태가 바뀌지 않는다
DECLARE @Wc2 BIGINT = (SELECT TOP (1) w.[WorkId] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[PatientId] = w.[PatientId]
                       WHERE p.[ChartNo] = 'T010' AND w.[StatusCode] = 'RSV');
IF @Wc2 IS NULL
BEGIN PRINT 'FAIL CWR-005 사전조건 — CORRUPT-2 Work 가 없다'; SET @Fail += 1; END
ELSE
BEGIN
    DECLARE @Rc2 BINARY(8) = (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wc2);
    EXEC [dbo].[USP_HC_UPDATE_접수완료] @Wc2, @Rc2;
    IF ((SELECT [StatusCode] FROM [dbo].[예약접수] WHERE [WorkId] = @Wc2) = 'RSV')
        PRINT 'PASS CWR-005 NEX 0행 손상 Work 가 접수되지 않았다';
    ELSE BEGIN PRINT 'FAIL CWR-005 손상 Work 가 접수됐다'; SET @Fail += 1; END
END

-- CWR-011  CORRUPT-4 (ExamSourceCode ↔ Master 역할 불일치) → 701
DECLARE @Wc4 BIGINT = (SELECT TOP (1) w.[WorkId] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[PatientId] = w.[PatientId]
                       WHERE p.[ChartNo] = 'T009' AND w.[StatusCode] = 'RSV');
IF @Wc4 IS NULL
BEGIN PRINT 'FAIL CWR-011 사전조건 — CORRUPT-4 Work 가 없다'; SET @Fail += 1; END
ELSE
BEGIN
    DECLARE @Rc4 BINARY(8) = (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wc4);
    EXEC [dbo].[USP_HC_UPDATE_접수완료] @Wc4, @Rc4;
    IF ((SELECT [StatusCode] FROM [dbo].[예약접수] WHERE [WorkId] = @Wc4) = 'RSV')
        PRINT 'PASS CWR-011 역할 불일치 Work 가 접수되지 않았다';
    ELSE BEGIN PRINT 'FAIL CWR-011 역할 불일치 Work 가 접수됐다'; SET @Fail += 1; END
END

-- CWR-006 / CWR-007  오늘 RSV 접수 성공 → RCP 전이, Date/Slot/Detail 불변. 재접수는 502.
DECLARE @Wt BIGINT = (SELECT TOP (1) [WorkId] FROM [dbo].[예약접수]
                       WHERE [StatusCode] = 'RSV' AND [ReservationDate] = CONVERT(DATE, SYSDATETIME())
                       ORDER BY [WorkId]);
IF @Wt IS NULL
    PRINT 'SKIP CWR-006/007/009 오늘 날짜 RSV Work 가 없다 — Fixture 는 고정날짜를 쓴다(§15.5)';
ELSE
BEGIN
    DECLARE @Rt   BINARY(8) = (SELECT [RowVersion]      FROM [dbo].[예약접수] WHERE [WorkId] = @Wt);
    DECLARE @Dt   DATE      = (SELECT [ReservationDate] FROM [dbo].[예약접수] WHERE [WorkId] = @Wt);
    DECLARE @St   CHAR(2)   = (SELECT [TimeSlotCode]    FROM [dbo].[예약접수] WHERE [WorkId] = @Wt);
    DECLARE @Dtl  INT = (SELECT COUNT(*) FROM [dbo].[검사항목] WHERE [WorkId] = @Wt);

    -- CWR-003  stale RowVersion 은 전이시키지 않는다
    EXEC [dbo].[USP_HC_UPDATE_접수완료] @Wt, 0x0000000000000001;
    IF ((SELECT [StatusCode] FROM [dbo].[예약접수] WHERE [WorkId] = @Wt) = 'RSV')
        PRINT 'PASS CWR-003 stale RowVersion 이 접수를 막았다';
    ELSE BEGIN PRINT 'FAIL CWR-003'; SET @Fail += 1; END

    EXEC [dbo].[USP_HC_UPDATE_접수완료] @Wt, @Rt;
    IF ((SELECT [StatusCode]       FROM [dbo].[예약접수] WHERE [WorkId] = @Wt) = 'RCP'
        AND (SELECT [ReservationDate] FROM [dbo].[예약접수] WHERE [WorkId] = @Wt) = @Dt
        AND (SELECT [TimeSlotCode]    FROM [dbo].[예약접수] WHERE [WorkId] = @Wt) = @St
        AND (SELECT COUNT(*) FROM [dbo].[검사항목] WHERE [WorkId] = @Wt) = @Dtl)
        PRINT 'PASS CWR-006 접수 성공 — RCP 전이, Date/Slot/Detail 불변';
    ELSE BEGIN PRINT 'FAIL CWR-006'; SET @Fail += 1; END

    -- CWR-007  이미 RCP 인 Work 재접수 → 상태 그대로
    DECLARE @Rt2 BINARY(8) = (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wt);
    EXEC [dbo].[USP_HC_UPDATE_접수완료] @Wt, @Rt2;
    IF ((SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wt) = @Rt2)
        PRINT 'PASS CWR-007 이미 RCP 인 Work 재접수가 아무것도 바꾸지 않았다';
    ELSE BEGIN PRINT 'FAIL CWR-007'; SET @Fail += 1; END
END
```

`[I]` `CWR-009`(접수 마감 경계)는 `UFN_HC_일정확인` 의 `RECEPTION` 마감(AM 11:00 / PM 16:00)에 걸리는 시각에 실행해야 한다. 시각을 주입하는 뒷문을 production SP 에 두지 않으므로 `RUL-T07`·`RUL-T08`·`RUL-T11`·`RUL-T12` 가 TVF 수준에서 같은 경계를 결정적으로 증명하고, `CWR-009` 는 실행 시각이 마감 후일 때만 성립한다 — 아니면 `SKIP` 이며 `PASS` 로 승격하지 않는다.

`CWR-006` 은 **업무시간(월~토 09:00~18:00) 안에서만** 완전히 검증된다. 밖이면 `308`/`309` 가 정상 결과이므로 테스트가 이를 인지하고 SKIP 로그를 남긴다.

```sql
DECLARE @Now TIME(7) = CONVERT(TIME(7), SYSDATETIME());
DECLARE @Dow INT = DATEDIFF(DAY, 0, CONVERT(DATE, SYSDATETIME())) % 7;
IF @Dow = 6 OR @Now < '09:00' OR @Now >= '11:00'
    PRINT 'SKIP CWR-006 접수 가능 시간대(월~토 09:00~11:00 AM)가 아님 — 통합시험 한계';
ELSE
BEGIN
    -- 실제 접수 시나리오 실행 및 판정
END
```

- [ ] **Step 2: 구현 — 검증순서 (`05` §12.1 그대로)**

```text
[Transaction 밖]
 0. 필수값 → 100
 1. 사전조회: SELECT @PatientId, @ResDate, @Slot FROM 예약접수 WHERE WorkId=@WorkId
      없으면 → 500 (트랜잭션을 열지 않는다)
      접수완료는 ReservationDate·TimeSlotCode 를 바꾸지 않으므로 stale 이어도 자원명이 안전하고,
      WORK 잠금 후 재검증에서 불일치하면 502/601 로 종료한다.

[Transaction 안]
 2. applock  HC|PAT|{@PatientId}         ← 전역 순서 3
 3. applock  HC|WORK|{@WorkId}           ← 전역 순서 4
 4. applock  HC|SLOT|{yyyyMMdd}|{slot}   ← 전역 순서 5
 5. Work 재조회, PatientId·ReservationDate·TimeSlotCode 가 사전조회값과 동일한지 확인
      다르면 **계약된 Code 로 종료**한다 — StatusCode 가 바뀌었으면 502, 그 밖의 불일치는 601.
      [X] 초안은 "설계 위반이므로 THROW" 였다. 그러나 트랜잭션 밖 사전조회 뒤 다른 세션이
          정상적으로 예약일·시간대를 바꾸는 것은 **정상적인 stale-read 경쟁**이지 설계 위반이 아니다.
          THROW 하면 계약 밖 예외가 C# 에 노출된다. 스펙 §24.2 도 "502/601 로 종료" 라고 못박았다.
          불변인 PatientId 가 달라진 경우만 실제로 불가능하므로 그때만 THROW 한다.
 6. StatusCode <> 'RSV'                          → 502
 7. RowVersion 불일치                             → 601
 8. Work 검사구성 무결성 3종 (스펙 §21.2a)
       (a) NEX 개수 NOT BETWEEN 8 AND 11          → 701
       (b) ExamSourceCode 가 검사코드 역할과 불일치 → 701
       (c) AEX 개수 > 6                           → 701
 9. UFN_HC_일정확인(@ServerTime, @Today, TimeSlot, 'NONE') CanWorkNow=0 → 308 / 309
10. ReservationDate <> @Today                     → 503 NotToday, Field='WorkId'
11. UFN_HC_일정확인(@ServerTime, @Today, TimeSlot, 'RECEPTION')
       CutoffPassed=1                             → 304 CutoffPassed, Field='TimeSlot'
12. 조건부 UPDATE
       SET StatusCode='RCP', LastEditDate=@StoredNow
       WHERE WorkId=@WorkId AND StatusCode='RSV' AND RowVersion=@RowVersion
       @@ROWCOUNT=0 → 재조회 후 502 우선, 그다음 601
13. COMMIT → RS0 + RS1 (새 RowVersion)
```

### `[X]` 왜 접수완료가 `PAT`·`SLOT` 까지 잡는가 (스펙 §24.2)

`RSV → RCP` 는 **집합 안에 남으면서 인덱스 키를 뒤로 이동**시키는 유일한 전이다.

```text
IX_예약접수_SLOT           Key(ReservationDate, TimeSlotCode, StatusCode)
IX_..._PATIENT_STATE_DATE            Key(PatientId, StatusCode, ReservationDate)
'CNC' < 'CNR' < 'RCP' < 'RSV'
COUNT 술어 StatusCode IN ('RSV','RCP') → 'RCP' 를 먼저, 'RSV' 를 나중에 스캔
```

READ COMMITTED 스캔이 `'RCP'` 구간을 지난 뒤 `'RSV'` 에서 X 잠금에 걸려 대기하다가 접수완료가 커밋되면, 옛 레코드는 ghost 가 되고 새 레코드는 이미 지나온 구간에 삽입되어 **행이 통째로 누락**된다. 접수완료가 `WORK` 만 잡으면 그 스캔들과 직렬화되지 않는다.

| 누락되는 COUNT | 귀결 | 위반 |
|---|---|---|
| `INSERT_예약`·`UPDATE_예약변경` 의 Slot 정원 | 21건 저장 | `00` RP-03 |
| `INSERT_예약` 의 다른 유효업무 | 동일 수검자 유효업무 2건 | `00` RP-06 |
| `UPDATE_수검자정보` 의 활성 Work 존재 | 주민번호 변경 허용 | `00` EP-08 |

취소 계열(`RSV→CNR`·`RCP→CNC`)은 집합 **밖**으로 나가므로 놓쳐도 보수적이라 `WORK` 만 잡는다. `RCP→RCP`(AEX 변경)는 `LastEditDate` 만 바꿔 두 NCI 키가 불변이라 이동이 없다.

`05` §14 의 논리 잠금영역 표는 **하한**이므로 잠금 확장은 계약 위반이 아니다. 전역 순서 3→4→5 를 그대로 지켜 교착 분석도 유지된다.

`ReservationDate`·`TimeSlotCode`·Detail은 **UPDATE 대상에서 제외**한다.

- [ ] **Step 3: GREEN + Commit** — `feat(phase4): USP_HC_UPDATE_접수완료 구현 및 접수 경계 테스트 7건`

`[R3]` 이 SP 는 Parameter 목록 **맨 끝**에 `@OperatorName NVARCHAR(50)` 을 받는다(`05` §19.2). 아래 시험의 모든 호출은 마지막 인자로 `@OperatorName = N'TEST'` 를 명시 전달한다 — `05` §2.1 이 선택 Parameter 의 생략을 금지한다.

`[R3]` 성공·업무실패 두 경로 모두 `04` §8.7.4 의 `<감사 블록>` 을 통과해 `변경이력` 1행을 남긴다. 감사 INSERT 는 트랜잭션 밖·자체 `TRY/CATCH`·해당 Result Set `SELECT` 뒤이며, 업무 INSERT 계열은 `SCOPE_IDENTITY()` 를 **감사 INSERT 앞에서** 변수로 확정한다.

```sql
-- CWR-050  USP_HC_UPDATE_접수완료 가 변경이력 1행을 남긴다 (성공·업무실패 각각)
DECLARE @H0 INT = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [OperationCode] = 'RCP_ACCEPT');
--   … 성공 호출 1회 + 업무실패 호출 1회를 수행한다 …
IF ((SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [OperationCode] = 'RCP_ACCEPT') = @H0 + 2
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력]
                     WHERE [OperationCode] = 'RCP_ACCEPT' AND [TargetTable] <> N'예약접수')
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력]
                     WHERE [OperationCode] = 'RCP_ACCEPT' AND [ResultCode] < 100 AND [TargetKey] IS NULL))
    PRINT 'PASS CWR-050 RCP_ACCEPT 감사 2행 · TargetTable · 성공행 TargetKey NOT NULL';
ELSE BEGIN PRINT 'FAIL CWR-050 감사 기록 불일치'; SET @Fail += 1; END
```

**완료조건:** `CWR-001`~`CWR-007` PASS (`CWR-006` 은 시간대에 따라 SKIP 허용).

---

## Task T29: `[dbo].[USP_HC_UPDATE_접수추가검사]`

**목적:** `RCP` 상태에서 AEX만 변경하고, 동일 집합이면 No-op으로 종료한다.

**관련 Baseline 위치:** `05` §12.2, `00` RCP-05·AEX-05, `04` §1.2.1, 스펙 §27·§28.

**선행조건:** `T28` 완료.

**Interfaces:** Produces RS0 + RS1 `(WorkId, Status, RowVersion)`. Parameter 9개: `@WorkId`, `@RowVersion`, `@AexOpt01Selected`~`@AexOpt07Selected`.

**허용 Code:** `0, 1, 100, 308~309, 410~412, 500, 502, 601, 700~701`

**금지사항:** 예약일·시간대·TGT·NEX를 변경하거나 재평가하지 않는다. No-op에서 Master 비활성·성별·중복 Rule을 재평가하지 않는다.

- [ ] **Step 1: RED — No-op 과 실제 변경을 구분한다**

RCP 상태 Work 는 **`T14b` 가 만든 `tests/00b_Test_Harness_RCP.sql`** 의 것을 쓴다 (`ChartNo='T014'`, NEX 11행 + AEX `OPT01` 1행). 업무시간과 무관하게 존재한다.

**계약 시나리오** — `UPDATE_접수추가검사`. Parameter 순서(`05` §12.2): `@WorkId, @RowVersion, @Opt01..@Opt07`

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `CWR-020` | `@Wr, @Rv, 1,0,0,0,0,0,0` | `1` | 동일 AEX 집합 → No-op |
| `CWR-021` | `@Wr, @Rv, 1,1,0,0,0,0,0` | `0` | OPT01 → OPT01+OPT02 실제 변경 |
| `CWR-023` | `@Wr, @Rv, 0,0,1,0,0,0,0` | `411` | 남성이 여성 전용 OPT03 요청 |
| `CWR-024` | **`@W11`**, `@Rv11, 0,0,0,1,0,0,0` | `412` | 이미 NEX 에 있는 항목(`EX012`)을 `OPT04` 로 요청 — **`T011`(여 54)** 의 RCP Work |
| `CWR-025` | `@Wrsv, @Rvsv, 1,0,0,0,0,0,0` | `502` | `RSV` 상태에서 호출 |
| `CWR-026` | `@Wr, 0x0000000000000001, 1,1,0,0,0,0,0` | `601` | stale `RowVersion` |

**DB 상태 단언**

```sql
-- CWR-020  동일 AEX 집합 → RowVersion·Detail 불변
DECLARE @Wr BIGINT = (SELECT TOP (1) w.[WorkId] FROM [dbo].[예약접수] w
                       JOIN [dbo].[수검자] p ON p.[PatientId] = w.[PatientId]
                      WHERE p.[ChartNo] = 'T014' AND w.[StatusCode] = 'RCP'
                      ORDER BY w.[WorkId]);
IF @Wr IS NULL
BEGIN PRINT 'FAIL CWR-020 사전조건 — T014 의 RCP Work 가 없다 (tests/00b 를 먼저 실행했는가)'; SET @Fail += 1; END
ELSE
BEGIN
    DECLARE @Rv  BINARY(8) = (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wr);
    DECLARE @Cnt INT = (SELECT COUNT(*) FROM [dbo].[검사항목] WHERE [WorkId] = @Wr);
    DECLARE @Nex INT = (SELECT COUNT(*) FROM [dbo].[검사항목]
                         WHERE [WorkId] = @Wr AND [ExamSourceCode] = 'NEX');

    -- Fixture 는 OPT01 만 선택된 상태이므로 동일 집합 = (1,0,0,0,0,0,0)
    EXEC [dbo].[USP_HC_UPDATE_접수추가검사] @Wr, @Rv, 1,0,0,0,0,0,0;
    IF ((SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wr) = @Rv
        AND (SELECT COUNT(*) FROM [dbo].[검사항목] WHERE [WorkId] = @Wr) = @Cnt)
        PRINT 'PASS CWR-020 동일 AEX 집합 No-op — RowVersion·Detail 불변';
    ELSE BEGIN PRINT 'FAIL CWR-020'; SET @Fail += 1; END

    -- CWR-021  실제 변경 → RowVersion 이 반드시 바뀐다
    EXEC [dbo].[USP_HC_UPDATE_접수추가검사] @Wr, @Rv, 1,1,0,0,0,0,0;
    DECLARE @Rv2 BINARY(8) = (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wr);
    IF (@Rv2 <> @Rv)
        PRINT 'PASS CWR-021 AEX 실제변경 시 Work RowVersion 갱신';
    ELSE BEGIN PRINT 'FAIL CWR-021'; SET @Fail += 1; END

    -- CWR-022  실제 변경 시 NEX Detail 은 불변 — AEX 만 바뀌어야 한다
    IF ((SELECT COUNT(*) FROM [dbo].[검사항목]
          WHERE [WorkId] = @Wr AND [ExamSourceCode] = 'NEX') = @Nex)
        PRINT 'PASS CWR-022 AEX 변경이 NEX Detail 을 건드리지 않았다';
    ELSE BEGIN PRINT 'FAIL CWR-022 NEX 가 재계산됐다'; SET @Fail += 1; END

    -- CWR-023  성별 위반 AEX → Detail 완전 보존
    DECLARE @Snap INT = (SELECT COUNT(*) FROM [dbo].[검사항목] WHERE [WorkId] = @Wr);
    EXEC [dbo].[USP_HC_UPDATE_접수추가검사] @Wr, @Rv2, 0,0,1,0,0,0,0;
    IF ((SELECT COUNT(*) FROM [dbo].[검사항목] WHERE [WorkId] = @Wr) = @Snap
        AND (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wr) = @Rv2)
        PRINT 'PASS CWR-023 성별 위반 AEX 요청이 Detail 을 보존했다';
    ELSE BEGIN PRINT 'FAIL CWR-023 부분저장 발생'; SET @Fail += 1; END

    -- CWR-025 는 아래에서 별도로 다룬다.
END

-- CWR-024  NEX 중복 AEX 요청(412) — T011(여 만 54세)의 RCP Work 를 쓴다
-- [X] T014 는 **남성** 이라 저장 NEX 에 EX012 가 없다. NEX-05 술어가 Gender='F' 를 요구하기 때문이다.
--     412 는 EX012 로만 발생하므로(스펙 §17.2a) T014 로 시험하면 412 가 아니라 0 이 나오고
--     "중복 판정이 동작한다" 를 아무것도 증명하지 못한 채 조용히 통과한다.
DECLARE @W11 BIGINT = (SELECT TOP (1) w.[WorkId] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[PatientId] = w.[PatientId]
                       WHERE p.[ChartNo] = 'T011' AND w.[StatusCode] = 'RCP' ORDER BY w.[WorkId]);
IF @W11 IS NULL
BEGIN PRINT 'FAIL CWR-024 사전조건 — T011 의 RCP Work 가 없다 (tests/00b)'; SET @Fail += 1; END
ELSE IF NOT EXISTS (SELECT 1 FROM [dbo].[검사항목]
                     WHERE [WorkId] = @W11 AND [ExamSourceCode] = 'NEX' AND [ExamItemCode] = 'EX012')
BEGIN PRINT 'FAIL CWR-024 사전조건 — T011 저장 NEX 에 EX012 가 없다. 412 를 관측할 수 없다'; SET @Fail += 1; END
ELSE
BEGIN
    DECLARE @Rv11 BINARY(8) = (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @W11);
    DECLARE @Sn11 INT = (SELECT COUNT(*) FROM [dbo].[검사항목] WHERE [WorkId] = @W11);
    EXEC [dbo].[USP_HC_UPDATE_접수추가검사] @W11, @Rv11, 0,0,0,1,0,0,0;   -- OPT04 = EX012 중복
    IF ((SELECT COUNT(*) FROM [dbo].[검사항목] WHERE [WorkId] = @W11) = @Sn11
        AND (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @W11) = @Rv11)
        PRINT 'PASS CWR-024 NEX 중복 AEX 요청이 Detail 을 보존했다';
    ELSE BEGIN PRINT 'FAIL CWR-024 중복 AEX 가 저장됐다'; SET @Fail += 1; END
END

-- CWR-026  stale RowVersion 은 아무것도 바꾸지 않는다. T014 Work 를 다시 읽어 자립적으로 판정한다.
IF @Wr IS NOT NULL
BEGIN
    DECLARE @Rv3 BINARY(8) = (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wr);

    EXEC [dbo].[USP_HC_UPDATE_접수추가검사] @Wr, 0x0000000000000001, 1,1,1,0,0,0,0;
    IF ((SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wr) = @Rv3)
        PRINT 'PASS CWR-026 stale RowVersion 이 변경을 막았다';
    ELSE BEGIN PRINT 'FAIL CWR-026 낙관적 동시성 위반'; SET @Fail += 1; END
END

-- CWR-025  RSV 상태 Work 에 접수추가검사 호출 → 상태·Detail 불변
DECLARE @Wrsv BIGINT = (SELECT TOP (1) [WorkId] FROM [dbo].[예약접수]
                         WHERE [StatusCode] = 'RSV' ORDER BY [WorkId]);
DECLARE @Rvsv BINARY(8) = (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wrsv);
EXEC [dbo].[USP_HC_UPDATE_접수추가검사] @Wrsv, @Rvsv, 1,0,0,0,0,0,0;
IF ((SELECT [StatusCode] FROM [dbo].[예약접수] WHERE [WorkId] = @Wrsv) = 'RSV'
    AND (SELECT [RowVersion] FROM [dbo].[예약접수] WHERE [WorkId] = @Wrsv) = @Rvsv)
    PRINT 'PASS CWR-025 RSV 상태에서는 접수추가검사가 아무것도 바꾸지 않는다';
ELSE BEGIN PRINT 'FAIL CWR-025'; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 07_Reception_Write_Tests 완료 ===';
```

- [ ] **Step 2: 구현 — 검증순서 (`05` §12.2 그대로)**

```text
[Transaction 밖]  필수값 (AEX 7 BIT NULL 불허) → 100
[Transaction 안]
 1. applock HC|WORK|{@WorkId}
 2. Work 존재                    → 500
 3. StatusCode <> 'RCP'          → 502
 4. RowVersion 불일치             → 601
 5. 저장 NEX·AEX 집합 조회
 6. 요청 7 BIT → @RequestAex 변환 후 EXCEPT 양방향 비교
 7. 동일집합                     → Code=1, UPDATE 없이 COMMIT, RS1 에 기존 RowVersion
                                   (Master 비활성·성별·중복 Rule 을 재평가하지 않는다)
 8. 실제 변경이면
      저장 Work 무결성 3종 (스펙 §21.2a) — No-op 판정보다 **먼저** 한다
       (a) NEX 개수 NOT BETWEEN 8 AND 11                        → 701
       (b) ExamSourceCode 가 Master 의 NEX/AEX 역할과 불일치      → 701
       (c) AEX 개수 > 6                                          → 701
    [X] 초안은 "저장 NEX 행수 < 1" 만 봤다. 그러면 NEX 12행·역할 불일치·AEX 7행이 통과한다.
        또한 No-op(Code=1)을 무결성 검사보다 먼저 반환하면 손상 Work 를 그대로 승인하게 된다.
        "비활성·성별 재평가를 하지 않는다" 와 "구조적 무결성을 확인한다" 는 다른 계약이다.
      AEX Master 구성 이상                        → 700
      공통 업무 가능 여부                          → 308 / 309
      UFN_HC_추가검사확인(@PatientId, @WorkReservationDate, @WorkId, 1 /*@UseSavedExams*/, 7 BIT)
        — @PatientId 와 @WorkReservationDate 는 6번에서 읽은 Work 행의 값이다
        Requested=1 인데 CanSelect=0 → 410 / 411 / 412 (OptionCode ASC 첫 건)
 9. AEX Detail DELETE (ExamSourceCode='AEX' 만) → INSERT (Selected=1)
10. UPDATE 예약접수 SET LastEditDate=@StoredNow
       WHERE WorkId=@WorkId AND StatusCode='RCP' AND RowVersion=@RowVersion
       @@ROWCOUNT=0 → 502 / 601
11. COMMIT → RS0 + RS1 (새 RowVersion)
```

**핵심:** `DELETE` 는 `ExamSourceCode='AEX'` 행만 지운다. NEX Detail은 절대 건드리지 않는다.

- [ ] **Step 3: GREEN + Commit** — `feat(phase4): USP_HC_UPDATE_접수추가검사 구현 및 No-op·RowVersion 테스트 7건`

`[R3]` 이 SP 는 Parameter 목록 **맨 끝**에 `@OperatorName NVARCHAR(50)` 을 받는다(`05` §19.2). 아래 시험의 모든 호출은 마지막 인자로 `@OperatorName = N'TEST'` 를 명시 전달한다 — `05` §2.1 이 선택 Parameter 의 생략을 금지한다.

`[R3]` 성공·업무실패 두 경로 모두 `04` §8.7.4 의 `<감사 블록>` 을 통과해 `변경이력` 1행을 남긴다. 감사 INSERT 는 트랜잭션 밖·자체 `TRY/CATCH`·해당 Result Set `SELECT` 뒤이며, 업무 INSERT 계열은 `SCOPE_IDENTITY()` 를 **감사 INSERT 앞에서** 변수로 확정한다.

```sql
-- CWR-051  USP_HC_UPDATE_접수추가검사 가 변경이력 1행을 남긴다 (성공·업무실패 각각)
DECLARE @H0 INT = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [OperationCode] = 'RCP_AEX');
--   … 성공 호출 1회 + 업무실패 호출 1회를 수행한다 …
IF ((SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [OperationCode] = 'RCP_AEX') = @H0 + 2
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력]
                     WHERE [OperationCode] = 'RCP_AEX' AND [TargetTable] <> N'예약접수')
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력]
                     WHERE [OperationCode] = 'RCP_AEX' AND [ResultCode] < 100 AND [TargetKey] IS NULL))
    PRINT 'PASS CWR-051 RCP_AEX 감사 2행 · TargetTable · 성공행 TargetKey NOT NULL';
ELSE BEGIN PRINT 'FAIL CWR-051 감사 기록 불일치'; SET @Fail += 1; END
```

**완료조건:** `CWR-020`(No-op 시 RowVersion 불변)과 `CWR-021`(실제변경 시 RowVersion 갱신)이 **둘 다** PASS. 이 두 건이 `04` §1.2.1 Aggregate 동시성 계약의 핵심 증거다.

---

## Task T30: `[dbo].[USP_HC_UPDATE_접수취소]`

**목적:** `RCP → CNC` 전이를 수행한다.

**관련 Baseline 위치:** `05` §12.3, `00` RCP-06·CP-05.

**선행조건:** `T29` 완료.

**Interfaces:** Produces RS0 + RS1 `(WorkId, Status, RowVersion)`. Parameter 2개.

**허용 Code:** `0, 100, 308~309, 500, 502, 601`

**금지사항:** `RSV` 로 복원하지 않는다. Detail을 삭제하지 않는다. 접수 마감시각을 취소 조건으로 쓰지 않는다.

- [ ] **Step 1: RED**

```sql
-- CWR-040 RSV 상태에서 접수취소 호출 → 502
-- CWR-041 stale RowVersion → 601
-- CWR-042 정상 취소 → CNC 전이 + Detail 보존
-- CWR-043 CNC 에서 재취소 → 502
-- CWR-044 취소 후 CNC → RSV 복원 경로가 없음을 확인 (예약변경 호출 시 502)
```

- [ ] **Step 2: 구현 — 검증순서 (`05` §12.3)**

```text
[Transaction 밖]  필수값 → 100
[Transaction 안]
 1. applock HC|WORK|{@WorkId}
 2. Work 존재                → 500
 3. StatusCode <> 'RCP'      → 502
 4. RowVersion 불일치         → 601
 5. 공통 업무 가능 여부        → 308 / 309
 6. 조건부 UPDATE  SET StatusCode='CNC', LastEditDate=@StoredNow
                   WHERE WorkId=@WorkId AND StatusCode='RCP' AND RowVersion=@RowVersion
 7. COMMIT → RS0 + RS1
```

- [ ] **Step 3: GREEN 실행**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i deploy/07_Procedures_Reception_Write.sql -o artifacts/logs/07_reception.log
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/07_Reception_Write_Tests.sql -o artifacts/logs/test_07.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_07.log | grep -cE '^PASS'
```

Expected: exit 0, PASS **19건** (`CWR-001`~`007` + `CWR-020`~`026` + `CWR-040`~`044`).

- [ ] **Step 4: 회귀 — SP 15개 완성 확인**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/01_Schema_Tests.sql -o artifacts/logs/test_01.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_01.log | grep -E 'SCH-013|SCH-014'
```

Expected: exit **0**, `PASS SCH-013 Inline TVF 4개`, `PASS SCH-014 Stored Procedure 15개`. 이 시점에 `01_Schema_Tests.sql` 이 **16/16 전부 PASS** 해야 한다.

- [ ] **Step 5: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/07_Procedures_Reception_Write.sql database/tests/07_Reception_Write_Tests.sql
git commit -m "feat(phase4): USP_HC_UPDATE_접수취소 구현 — 15개 SP 완성"
```

**회귀시험:** `tests/00`~`07` 전체

**로그 경로:** `artifacts/logs/test_07.log`

`[R3]` 이 SP 는 Parameter 목록 **맨 끝**에 `@OperatorName NVARCHAR(50)` 을 받는다(`05` §19.2). 아래 시험의 모든 호출은 마지막 인자로 `@OperatorName = N'TEST'` 를 명시 전달한다 — `05` §2.1 이 선택 Parameter 의 생략을 금지한다.

`[R3]` 성공·업무실패 두 경로 모두 `04` §8.7.4 의 `<감사 블록>` 을 통과해 `변경이력` 1행을 남긴다. 감사 INSERT 는 트랜잭션 밖·자체 `TRY/CATCH`·해당 Result Set `SELECT` 뒤이며, 업무 INSERT 계열은 `SCOPE_IDENTITY()` 를 **감사 INSERT 앞에서** 변수로 확정한다.

```sql
-- CWR-052  USP_HC_UPDATE_접수취소 가 변경이력 1행을 남긴다 (성공·업무실패 각각)
DECLARE @H0 INT = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [OperationCode] = 'RCP_CANCEL');
--   … 성공 호출 1회 + 업무실패 호출 1회를 수행한다 …
IF ((SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [OperationCode] = 'RCP_CANCEL') = @H0 + 2
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력]
                     WHERE [OperationCode] = 'RCP_CANCEL' AND [TargetTable] <> N'예약접수')
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력]
                     WHERE [OperationCode] = 'RCP_CANCEL' AND [ResultCode] < 100 AND [TargetKey] IS NULL))
    PRINT 'PASS CWR-052 RCP_CANCEL 감사 2행 · TargetTable · 성공행 TargetKey NOT NULL';
ELSE BEGIN PRINT 'FAIL CWR-052 감사 기록 불일치'; SET @Fail += 1; END
```

**완료조건:** 스펙 §45.2 의 `CWR` 전건 PASS + `tests/01_Schema_Tests.sql` 의 `SCH` 전건 PASS (G04·G05·G06 충족).
