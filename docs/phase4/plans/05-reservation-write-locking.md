# Stage 7 — 예약 Write Stored Procedure 3개 · Slot 잠금

**Index:** `2026-09-04-phase4-database-implementation.md`
**Spec:** `../06_DB_Transaction_Security_Seed_CANDIDATE.md`
**Tasks:** `T25` ~ `T27`

생성 파일은 `deploy/06_Procedures_Reservation_Write.sql`, 테스트는 `tests/06_Reservation_Write_Tests.sql` 하나다. 세 SP 모두 index 문서의 Write SP 공통 Template을 사용한다.

## 공통 — AEX 7 BIT → 행집합 변환 (`04` §3.8)

```sql
DECLARE @RequestAex TABLE (ExamItemCode VARCHAR(10) NOT NULL PRIMARY KEY);

INSERT INTO @RequestAex (ExamItemCode)
SELECT m.[ExamItemCode]
FROM (VALUES ('OPT01', @AexOpt01Selected), ('OPT02', @AexOpt02Selected), ('OPT03', @AexOpt03Selected),
             ('OPT04', @AexOpt04Selected), ('OPT05', @AexOpt05Selected), ('OPT06', @AexOpt06Selected),
             ('OPT07', @AexOpt07Selected)) v (OptionCode, IsSelected)
JOIN [dbo].[MST_EXAM_ITEMS] m ON m.[AdditionalExamCode] = v.OptionCode
WHERE v.IsSelected = 1;
```

CSV·XML·JSON·`STRING_SPLIT`·비트마스크·TVP를 사용하지 않는다.

## 공통 — Slot 잠금 자원 생성

```sql
DECLARE @ResSlot NVARCHAR(255) =
    N'HC|SLOT|' + CONVERT(CHAR(8), @ReservationDate, 112) + N'|' + @TimeSlot;
```

문자열 오름차순이 곧 (날짜, 시간대) 오름차순이므로, 예약 이동에서 두 Slot을 잡을 때 **`ORDER BY 자원명 ASC`** 로 획득하면 교차이동 교착이 구조적으로 사라진다.

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


## Task T25: `[dbo].[USP_HC_INSERT_예약]`

**목적:** Normal/WalkIn 신규 예약을 Work + NEX/AEX Detail과 함께 원자적으로 저장한다.

**관련 Baseline 위치:** `05` §11.1, `00` RP-01~08, 스펙 §21.2·§24.1·§30.

**선행조건:** `T24` 완료.

**Files:**
- Create: `deploy/06_Procedures_Reservation_Write.sql`
- Create: `tests/06_Reservation_Write_Tests.sql`

**Interfaces:**
- Consumes: 4개 TVF
- Produces: RS0 + RS1 `(WorkId BIGINT, Status CHAR(3), RowVersion BINARY(8))`
- Parameter 11개: `@PatientId BIGINT`, `@ReservationType VARCHAR(10)`, `@ReservationDate DATE`, `@TimeSlot CHAR(2)`, `@AexOpt01Selected`~`@AexOpt07Selected BIT`

**applock:** `HC|PAT|{PatientId}` → `HC|SLOT|{yyyyMMdd}|{AM|PM}`

**허용 Code:** `0, 100~102, 200, 300~306, 308~309, 400~401, 410~412, 700~701`

**금지사항:** 조회 SP의 결과를 신뢰하지 않는다 — 모든 조건을 Transaction 안에서 다시 검증한다. NEX 없이 AEX만 저장하지 않는다.

- [ ] **Step 1: RED — 경계 테스트**

`[X]` **`INSERT … EXEC` 는 쓸 수 없다**(스펙 §33.1a). RS0 `Code` 판정은 계약 시나리오로, DB 결과 판정은 이 파일로 나눈다.

**계약 시나리오** — Parameter 순서(`05` §9.1): `@PatientId, @ReservationType, @ReservationDate, @TimeSlotCode, @Opt01, @Opt02, @Opt03, @Opt04, @Opt05, @Opt06, @Opt07`

요일은 실측 확인했다: `2026-11-16` 월 · `2026-11-17` 화 · `2026-11-21` **토** · `2026-11-22` **일** · `2026-12-25` 금(휴무일 Seed).

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `RWR-001` | `@Pt, 'NORMAL', '2020-01-06', 'AM', 0,0,0,0,0,0,0` | `300` | 과거일 |
| `RWR-002` | `@Pt, 'NORMAL', '2026-11-22', 'AM', 0,0,0,0,0,0,0` | `301` | 일요일 |
| `RWR-003` | `@Pt, 'NORMAL', '2026-12-25', 'AM', 0,0,0,0,0,0,0` | `302` | 활성 휴무일 |
| `RWR-004` | `@Pt, 'NORMAL', '2026-11-21', 'PM', 0,0,0,0,0,0,0` | `303` | 토요일 PM |
| `RWR-005` | `@Pt, 'WALKIN', '2026-11-16', 'AM', 0,0,0,0,0,0,0` | `102` | WALKIN 인데 오늘이 아님 |
| `RWR-006` | `@P19, 'NORMAL', '2026-11-17', 'AM', 0,0,0,0,0,0,0` | `400` | `T017` 만 18세 → TGT 비대상 |
| `RWR-007` | `@Pt, 'NORMAL', '2026-11-17', 'AM', 0,0,1,0,0,0,0` | `411` | 남성이 OPT03(여성 전용) 요청 |
| `RWR-008` | `@Pt, 'NORMAL', '2026-11-17', 'AM', 1,0,0,0,0,0,0` | `0` | 신규예약 성공 |
| `RWR-010` | `@Pt, 'NORMAL', '2026-11-18', 'AM', 0,0,0,0,0,0,0` | `306` | 같은 Patient 재예약 (RP-06) |
| `RWR-011` | `@P2, 'NORMAL', '2026-11-19', 'AM', 0,0,0,0,0,0,0` | `701` | `CORRUPT-1`(`T012` 유효업무 2건) |
| `RWR-012` | `@Pf, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0` | `305` | 20/20 Slot |
| `RWR-032` | `@Pt, 'NORMAL', '2026-11-19', 'AM', 0,0,0,0,0,0,0` | `306` | 타 유효업무 **1건** (`05` §17, 스펙 §33.4) |
| `RWR-033` | `@P2, 'NORMAL', '2026-11-20', 'AM', 0,0,0,0,0,0,0` | `701` | 타 유효업무 **2건** — 1건과 2건은 다른 Code 다 |

`[X]` **`RWR-006` 은 `T001` 을 쓸 수 없다.** `T001`(2006-10-02생)은 Fixture 기준일 `2026-10-01` 에만 만 19세이고, `2026-11-17` 을 주면 만 20세라 `400` 이 아니라 예약이 **실제로 생성**되어 이후 상태를 오염시킨다. Write SP 는 과거일을 `300` 으로 막으므로 `2026-10-01` 도 쓸 수 없다. → 전용 수검자 `T017`(생년월일 `2007-11-18` 고정 리터럴, `T10` Step 2)을 쓴다. `2026-11-17` 에 만 **18**세이고 `2026-11-18` 에 19세가 된다(실측).

`[X]` **`RWR-012` 는 20/20 을 만드는 방법을 정한다.** SP 로 1건을 더 넣으면 업무시간 의존이 생기고, 직접 `INSERT` 하면 `RWR-043`(취소)과 실행 순서가 엮인다. → `T10` 이 `F020` 을 `CNL` 로 심어 두고(`CON-002` 의 19/20 사전조건), 이 파일이 `RSV` 로 뒤집어 20/20 을 만든 뒤 단언하고 다시 `CNL` 로 되돌린다 — **같은 파일 안에서 완결**한다.

**DB 상태 단언** — `tests/06_Reservation_Write_Tests.sql`

```sql
SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- 업무시간 가드 (스펙 §33.2a)
IF NOT (DATEPART(WEEKDAY, SYSDATETIME()) BETWEEN 2 AND 7
        AND CONVERT(TIME(0), SYSDATETIME()) >= '09:00:00'
        AND CONVERT(TIME(0), SYSDATETIME()) <  '18:00:00'
        AND NOT EXISTS (SELECT 1 FROM [dbo].[MST_HOLIDAYS]
                         WHERE [HolidayDate] = CONVERT(DATE, SYSDATETIME()) AND [IsActive] = 1))
BEGIN
    PRINT 'SKIP 06_Reservation_Write_Tests 업무시간(월~토 09:00~18:00, 비휴무일) 밖';
    RETURN;
END

DECLARE @Pt  BIGINT = (SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T015');
DECLARE @P19 BIGINT = (SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T017');
DECLARE @P2  BIGINT = (SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T012');
DECLARE @Pf  BIGINT = (SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'F020');

IF @Pt IS NULL OR @P19 IS NULL OR @P2 IS NULL OR @Pf IS NULL
BEGIN
    PRINT 'FAIL 사전조건 — T015/T017/T012/F020 중 없는 Fixture 가 있다 (tests/00 을 먼저 실행했는가)';
    SET @Fail += 1;
END

-- RWR-001~007 실패 경로: Work 를 만들지 않았다
DECLARE @Before INT = (SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [PatientId] = @Pt);
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2020-01-06', 'AM', 0,0,0,0,0,0,0;
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-11-22', 'AM', 0,0,0,0,0,0,0;
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-12-25', 'AM', 0,0,0,0,0,0,0;
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-11-21', 'PM', 0,0,0,0,0,0,0;
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'WALKIN', '2026-11-16', 'AM', 0,0,0,0,0,0,0;
EXEC [dbo].[USP_HC_INSERT_예약] @P19,'NORMAL', '2026-11-17', 'AM', 0,0,0,0,0,0,0;
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-11-17', 'AM', 0,0,1,0,0,0,0;
IF ((SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [PatientId] = @Pt) = @Before
    AND NOT EXISTS (SELECT 1 FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [PatientId] = @P19))
    PRINT 'PASS RWR-001~007 실패 경로가 Work 를 만들지 않았다';
ELSE BEGIN PRINT 'FAIL RWR-001~007 실패 경로가 Work 를 남겼다'; SET @Fail += 1; END

-- RWR-008 신규예약 성공 → Work 1건 증가
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-11-17', 'AM', 1,0,0,0,0,0,0;
DECLARE @W BIGINT = (SELECT TOP (1) [WorkId] FROM [dbo].[INFO_CHECKUP_WORKS]
                      WHERE [PatientId] = @Pt AND [StatusCode] = 'RSV' ORDER BY [WorkId] DESC);
IF (@W IS NOT NULL
    AND (SELECT [ReservationDate] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @W) = '2026-11-17'
    AND (SELECT [TimeSlotCode]    FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @W) = 'AM')
    PRINT 'PASS RWR-008 신규예약이 RSV Work 로 저장됐다';
ELSE BEGIN PRINT 'FAIL RWR-008'; SET @Fail += 1; END

-- RWR-009 저장된 검사구성 = NEX 8~11행 + AEX 1행
IF ((SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORK_EXAMS] WHERE [WorkId] = @W AND [ExamSourceCode] = 'NEX') BETWEEN 8 AND 11
    AND (SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORK_EXAMS] WHERE [WorkId] = @W AND [ExamSourceCode] = 'AEX') = 1)
    PRINT 'PASS RWR-009 Detail NEX 8~11 + AEX 1';
ELSE BEGIN PRINT 'FAIL RWR-009'; SET @Fail += 1; END

-- RWR-010 / RWR-032  타 유효업무 1건이면 306 — Work 가 늘지 않는다
SET @Before = (SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [PatientId] = @Pt);
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-11-18', 'AM', 0,0,0,0,0,0,0;
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-11-19', 'AM', 0,0,0,0,0,0,0;
IF ((SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [PatientId] = @Pt) = @Before)
    PRINT 'PASS RWR-010/032 유효업무 1건 보유 시 재예약이 저장되지 않았다';
ELSE BEGIN PRINT 'FAIL RWR-010/032 RP-06 위반'; SET @Fail += 1; END

-- RWR-011 / RWR-033  CORRUPT-1 (유효업무 2건) 은 306 이 아니라 701 이고, 역시 저장하지 않는다
SET @Before = (SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [PatientId] = @P2);
EXEC [dbo].[USP_HC_INSERT_예약] @P2, 'NORMAL', '2026-11-19', 'AM', 0,0,0,0,0,0,0;
EXEC [dbo].[USP_HC_INSERT_예약] @P2, 'NORMAL', '2026-11-20', 'AM', 0,0,0,0,0,0,0;
IF ((SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [PatientId] = @P2) = @Before)
    PRINT 'PASS RWR-011/033 유효업무 2건 손상 상태에서 저장되지 않았다';
ELSE BEGIN PRINT 'FAIL RWR-011/033'; SET @Fail += 1; END

-- RWR-012  F020 을 RSV 로 뒤집어 20/20 을 만든 뒤 305 를 확인하고 되돌린다
UPDATE [dbo].[INFO_CHECKUP_WORKS] SET [StatusCode] = 'RSV'
 WHERE [PatientId] = @Pf AND [ReservationDate] = '2026-11-16' AND [TimeSlotCode] = 'AM';

DECLARE @Slot INT = (SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORKS]
                      WHERE [ReservationDate] = '2026-11-16' AND [TimeSlotCode] = 'AM'
                        AND [StatusCode] IN ('RSV', 'RCP'));
IF @Slot = 20 PRINT 'PASS RWR-012 사전조건 20/20 성립';
ELSE BEGIN PRINT 'FAIL RWR-012 사전조건 Slot=' + CONVERT(VARCHAR(5), @Slot); SET @Fail += 1; END

DECLARE @P21 BIGINT = (SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T013');
EXEC [dbo].[USP_HC_INSERT_예약] @P21, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0;
IF ((SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORKS]
      WHERE [ReservationDate] = '2026-11-16' AND [TimeSlotCode] = 'AM'
        AND [StatusCode] IN ('RSV', 'RCP')) = 20)
    PRINT 'PASS RWR-012 정원 20 초과 저장이 차단됐다 (RP-03)';
ELSE BEGIN PRINT 'FAIL RWR-012 정원 21건 — RP-03 위반'; SET @Fail += 1; END

UPDATE [dbo].[INFO_CHECKUP_WORKS] SET [StatusCode] = 'CNL'
 WHERE [PatientId] = @Pf AND [ReservationDate] = '2026-11-16' AND [TimeSlotCode] = 'AM';
```

`[I]` `RWR-043`(예약취소)이 취소할 Work 는 `ORDER BY WorkId` 로 `F001` 을 고정한다 — `F020` 을 건드리지 않는다.

- [ ] **Step 2: 구현 — 검증순서 (`05` §11.1 그대로)**

```text
[Transaction 밖]
 1. 필수값: @PatientId / @ReservationType / @ReservationDate / @TimeSlot / AEX 7 BIT → NULL 이면 100
 2. 허용값: @ReservationType ∈ {NORMAL, WALKIN} 아니면 101 / @TimeSlot ∈ {AM, PM} 아니면 101
 3. 조합:  WALKIN 인데 @ReservationDate <> @Today → 102, Field='ReservationDate'

[Transaction 안]
 4. applock  HC|PAT|{@PatientId}
 5. applock  HC|SLOT|{yyyyMMdd}|{slot}
 6. Patient 존재                                        → 200
 7. 검사 Master 구성 확인 (Exam 19행 / AEX 7종)          → 700 ExamSetupError
 8. UFN_HC_일정확인(@ServerTime, @Today, 'AM', 'NONE')
      CanWorkNow=0                                      → WorkCode(308/309)
 9. 다른 유효업무 COUNT
      >= 2                                              → 701
      =  1                                              → 306, Field='PatientId'
10. UFN_HC_일정확인(@ServerTime, @ReservationDate, @TimeSlot,
                   CASE WHEN @ReservationType='WALKIN' THEN 'RECEPTION' ELSE 'NORMAL' END)
      CanUse=0                                          → ReasonCode(300~304)
11. 정원 재조회
      COUNT(RSV+RCP) + 1 > 20                           → 305 SlotFull, Field='TimeSlot'
12. UFN_HC_검진대상확인  Eligible=0                      → ReasonCode(400/401)
13. UFN_HC_국가검사구성  행수 NOT BETWEEN 8 AND 11        → 701
    ※ 상한 11 을 버리면 TVF·Master 손상으로 12행이 나와도 저장이 계속된다.
      `05` §17.4 는 "모든 TGT 대상 결과가 8~11행 범위인지 검증" 을 요구한다.
      저장 후 Work 검사구성 무결성 3종(스펙 §21.2a)도 함께 확인한다.
14. UFN_HC_추가검사확인  Requested=1 인데 CanSelect=0 인 행 존재
      → 그 행의 ReasonCode(410/411/412), Field='AexOpt0nSelected' (OptionCode ASC 첫 건)
15. INSERT INFO_CHECKUP_WORKS (StatusCode='RSV', CreationDate/LastEditDate = @StoredNow)
    SCOPE_IDENTITY() → @WorkId
16. INSERT INFO_CHECKUP_WORK_EXAMS
      NEX 전체 (UFN_HC_국가검사구성 결과, ExamSourceCode='NEX')
      + Selected=1 인 AEX (ExamSourceCode='AEX')
17. COMMIT → RS0 + RS1 (WorkId, 'RSV', 새 RowVersion 재조회)
```

`RowVersion` 은 `COMMIT` **이후** `SELECT [RowVersion] FROM INFO_CHECKUP_WORKS WHERE WorkId=@WorkId` 로 다시 읽어 반환한다.

- [ ] **Step 3: GREEN 실행**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i deploy/06_Procedures_Reservation_Write.sql -o artifacts/logs/06_reservation.log
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/06_Reservation_Write_Tests.sql -o artifacts/logs/test_06.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_06.log | grep -E '^(PASS|FAIL)'
```

Expected: exit 0, `RWR-001`~`RWR-012` 12건 PASS.

- [ ] **Step 4: 요일 사전확인**

테스트가 쓰는 날짜의 요일을 먼저 확인하고, 다르면 날짜를 조정한다.

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W -Q "
SELECT CONVERT(varchar(10),d) + ' dow=' + CONVERT(varchar(2), DATEDIFF(DAY,0,d)%7)
FROM (VALUES (CONVERT(DATE,'2026-11-16')),(CONVERT(DATE,'2026-11-17')),(CONVERT(DATE,'2026-11-18')),
             (CONVERT(DATE,'2026-11-21')),(CONVERT(DATE,'2026-11-22'))) v(d);"
```

Expected: `2026-11-21 dow=5`(토), `2026-11-22 dow=6`(일), 나머지는 평일(0~4).

- [ ] **Step 5: Commit** — `feat(phase4): USP_HC_INSERT_예약 구현 및 예약 경계 테스트 12건`

**회귀시험:** `tests/00`~`06`

**완료조건:** 스펙 §45.2 의 `RWR-001`~`RWR-012` 전건 PASS + Detail 이 NEX 8~11 + AEX 정확히 저장.

---

## Task T26: `[dbo].[USP_HC_UPDATE_예약변경]`

**목적:** 실제 변경조합을 DB가 판정하고, 영향받는 Rule만 재검증한 뒤 저장한다. Phase 4에서 가장 복잡한 Write SP다.

**관련 Baseline 위치:** `05` §11.2 (검증순서·변경 Matrix), `00` §4.1, `04` §2.2.1, 스펙 §29·§30.

**선행조건:** `T25` 완료.

**Interfaces:**
- Produces: RS0 + RS1 `(WorkId, Status, RowVersion)`
- Parameter 11개: `@WorkId BIGINT`, `@RowVersion BINARY(8)`, `@ReservationDate DATE`, `@TimeSlot CHAR(2)`, `@AexOpt01Selected`~`@AexOpt07Selected BIT`

**applock:** `HC|PAT|{PatientId}` → `HC|WORK|{WorkId}` → `HC|SLOT|…` (기존·신규, **자원명 오름차순**)

**허용 Code:** `0, 1, 100~102, 300~306, 308~309, 400~401, 410~412, 500, 502, 601, 700~701`

**금지사항:** C#이 전달한 변경구분을 신뢰하지 않는다. 시간대만 변경할 때 TGT/NEX/AEX를 재평가하거나 Detail을 재작성하지 않는다.

- [ ] **Step 1: RED — 변경 Matrix 5분기 + 경합**

```sql
-- RWR-020 변경 없음 → Code=1, RowVersion 불변
-- RWR-021 시간대만 변경 → Work 만 UPDATE, NEX/AEX Detail 행 불변
-- RWR-022 AEX만 변경   → AEX Detail 변경 + RowVersion 변경, NEX Detail 불변
-- RWR-023 예약일 변경  → NEX/AEX Detail 전량 재작성
-- RWR-024 시간대+AEX   → 일정·정원 + AEX 만
-- RWR-025 stale RowVersion → 601
-- RWR-026 Status=CNL 인 Work 변경 → 502
-- RWR-027 미존재 WorkId → 500
-- RWR-028 예약변경에서 자기 Work 를 306 으로 오인하지 않는다  (핵심)
-- RWR-029 예약일 변경 후 TGT 비대상 → 400 이고 기존 Work 완전 보존
-- RWR-030 20/20 Slot 을 그대로 유지하는 시간대 미변경 → AfterCount=20, 성공
```

`RWR-028` 구현 예:

**계약 시나리오** — Parameter 순서(`05` §9.2): `@WorkId, @RowVersion, @ReservationDate, @TimeSlotCode, @Opt01..@Opt07`

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `RWR-028` | `@Ws, @Rv, '2026-11-17', 'PM', 1,0,0,0,0,0,0` | `0` 또는 `1` | 자기 Work 를 `306` 으로 오인하지 않는다 |
| `RWR-031` | **`@W20`**, `@Rv20, '2026-11-20', 'AM', 0,0,0,1,0,0,0` | `412` | **`T020`**(1972-11-20생 여) 의 예약일을 `11-17`(만 53세) → `11-20`(만 54세) 으로 옮기면 `EX012` 가 새로 생겨 `OPT04` 와 중복 |
| `RWR-034` | `@Wx, @Rvx, '2026-11-19', 'AM', 0,0,0,0,0,0,0` | `701` | `CORRUPT-5`(**`F002`** 의 NEX 13행) Work 를 변경 시도 |

`[I]` `RWR-031` 은 `05` §17 의 *"예약일 변경 후 OPT04 vs EX012 충돌"* 이다(스펙 §33.4). AEX 중복 판정은 **변경 후 예약일 기준 NEX 구성**으로 다시 해야 하며, 변경 전 구성으로 판정하면 통과해 버린다.

```sql
-- RWR-028  유효업무가 자기 자신 1건뿐인 Work 를 시간대만 변경 → 306 이 나오면 안 된다
DECLARE @Ws BIGINT = (SELECT TOP (1) w.[WorkId] FROM [dbo].[INFO_CHECKUP_WORKS] w
                       JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId]
                      WHERE p.[ChartNo] = 'T015' AND w.[StatusCode] = 'RSV');
DECLARE @Rv BINARY(8) = (SELECT [RowVersion] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @Ws);

EXEC [dbo].[USP_HC_UPDATE_예약변경] @Ws, @Rv, '2026-11-17', 'PM', 1,0,0,0,0,0,0;

IF ((SELECT [TimeSlotCode] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @Ws) = 'PM')
    PRINT 'PASS RWR-028 자기 Work 를 306 으로 오인하지 않고 변경했다';
ELSE BEGIN PRINT 'FAIL RWR-028 자기 Work 오탐 — 변경이 반영되지 않았다'; SET @Fail += 1; END

-- RWR-031  예약일 변경으로 만나이가 경계를 넘어 NEX 구성이 바뀐다 → OPT04 가 EX012 와 중복 → 412
-- [X] 초안은 @Ws(T015, **남** 만 46세)를 썼다. NEX-05(EX012) 술어가 Gender='F' AND Age IN (54,60,66)
--     이므로 T015 에는 EX012 가 어떤 날짜로도 생기지 않고, 412 대신 0 이 나와 테스트가 조용히 통과한다.
--     412 는 EX012 로만 발생한다(스펙 §17.2a) — Seed 19행 중 NexRuleCode 와 AdditionalExamCode 를
--     동시에 가진 행이 EX012 하나뿐이기 때문이다.
--     → T020(1972-11-20생 여)을 쓴다. 실측: 2026-11-17 만 53세, 2026-11-20 만 54세.
--       AEX 판정을 **변경 전** 구성으로 하면 이 시나리오가 통과해 버린다. 그것이 이 시험의 표적이다.
DECLARE @W20 BIGINT = (SELECT TOP (1) w.[WorkId] FROM [dbo].[INFO_CHECKUP_WORKS] w
                        JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId]
                       WHERE p.[ChartNo] = 'T020' AND w.[StatusCode] = 'RSV');
IF @W20 IS NULL
BEGIN PRINT 'FAIL RWR-031 사전조건 — T020 의 RSV Work 가 없다'; SET @Fail += 1; END
ELSE
BEGIN
    DECLARE @Rv20 BINARY(8) = (SELECT [RowVersion] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @W20);
    DECLARE @D20  DATE      = (SELECT [ReservationDate] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @W20);
    -- 사전조건: 현재 예약일(11-17, 만 53세)에는 EX012 가 없어야 한다. 있으면 시험이 성립하지 않는다.
    IF EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성](
                 (SELECT [PatientId] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @W20), @D20)
                WHERE ExamCode = 'EX012')
    BEGIN PRINT 'FAIL RWR-031 사전조건 — 변경 전에 이미 EX012 가 있다'; SET @Fail += 1; END
    ELSE
    BEGIN
        EXEC [dbo].[USP_HC_UPDATE_예약변경] @W20, @Rv20, '2026-11-20', 'AM', 0,0,0,1,0,0,0;
        IF ((SELECT [ReservationDate] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @W20) = @D20
            AND (SELECT [RowVersion] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @W20) = @Rv20)
            PRINT 'PASS RWR-031 변경 후 나이 기준으로 AEX 중복을 판정해 저장을 막았다';
        ELSE BEGIN PRINT 'FAIL RWR-031 변경 전 NEX 구성으로 판정해 중복 AEX 가 저장됐다'; SET @Fail += 1; END
    END
END

-- RWR-034  CORRUPT-5 (NEX 13행) Work 는 변경을 거부하고 아무것도 바꾸지 않는다
-- [X] 초안은 T011 을 지목했으나 T011 에는 RSV Work 를 만드는 코드가 없다 — @Wx 가 NULL 이 되어
--     RWR-034 가 사전조건 실패로만 끝난다. CORRUPT-5 는 F002(정원용, NEX 8행)의 Work 에 심는다.
DECLARE @Wx BIGINT = (SELECT TOP (1) w.[WorkId] FROM [dbo].[INFO_CHECKUP_WORKS] w
                       JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId]
                      WHERE p.[ChartNo] = 'F002' AND w.[StatusCode] = 'RSV');
IF @Wx IS NULL
BEGIN PRINT 'FAIL RWR-034 사전조건 — CORRUPT-5 대상 Work(F002)가 없다'; SET @Fail += 1; END
ELSE
BEGIN
    DECLARE @Rvx BINARY(8) = (SELECT [RowVersion] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @Wx);
    DECLARE @Dx  DATE      = (SELECT [ReservationDate] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @Wx);
    EXEC [dbo].[USP_HC_UPDATE_예약변경] @Wx, @Rvx, '2026-11-19', 'AM', 0,0,0,0,0,0,0;
    IF ((SELECT [ReservationDate] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @Wx) = @Dx
        AND (SELECT [RowVersion] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @Wx) = @Rvx)
        PRINT 'PASS RWR-034 NEX 12행 손상 Work 는 변경되지 않았다';
    ELSE BEGIN PRINT 'FAIL RWR-034 손상 Work 가 변경됐다'; SET @Fail += 1; END
END
```

- [ ] **Step 2: 구현 — Scope 계산**

```sql
DECLARE @DateChanged  BIT = CASE WHEN @CurDate <> @ReservationDate THEN 1 ELSE 0 END;
DECLARE @SlotChanged  BIT = CASE WHEN @CurSlot <> @TimeSlot        THEN 1 ELSE 0 END;
DECLARE @ExtraChanged BIT = CASE
    WHEN NOT EXISTS (SELECT ExamItemCode FROM @CurrentAex EXCEPT SELECT ExamItemCode FROM @RequestAex)
     AND NOT EXISTS (SELECT ExamItemCode FROM @RequestAex EXCEPT SELECT ExamItemCode FROM @CurrentAex)
    THEN 0 ELSE 1 END;
```

`@CurrentAex` 는 `INFO_CHECKUP_WORK_EXAMS WHERE WorkId=@WorkId AND ExamSourceCode='AEX'` 다.

- [ ] **Step 3: 구현 — 검증순서 (`05` §11.2 그대로)**

```text
[Transaction 밖]
 1. 필수값·허용값 (AEX 7 BIT NULL 불허)
 2. 사전조회: SELECT @PatientId = PatientId FROM INFO_CHECKUP_WORKS WHERE WorkId=@WorkId
       없으면 → 500 WorkNotFound  (트랜잭션을 열지 않는다)

[Transaction 안]
 3. applock  HC|PAT|{@PatientId}
 4. applock  HC|WORK|{@WorkId}
 5. applock  HC|SLOT|… × (기존·신규, 중복 제거 후 자원명 오름차순)
 6. Work 재조회      없으면 500 WorkNotFound
    ※ `501 WrongPatient` 를 쓰지 않는다. `05` §13 의 `UPDATE_예약변경` 허용 Code 는
      `0, 1, 100~102, 300~306, 308~309, 400~401, 410~412, 500, 502, 601, 700~701` 로
      **501 이 없다**. 게다가 이 SP 는 `@PatientId` Parameter 자체가 없어(§11.2 는 11개)
      "PatientId 불일치" 를 판정할 외부 입력이 존재하지 않고, `EP-02` 에 의해 PatientId 는
      불변이므로 사전조회값과 재조회값이 달라질 수도 없다 — 초안의 501 분기는 계약 위반이자 죽은 코드였다.
      사전조회 PatientId 와 재조회 PatientId 가 다르면 그것은 설계 위반이므로 `THROW` 한다.
 7. StatusCode <> 'RSV'                          → 502 WrongStatus
 8. RowVersion <> @RowVersion                     → 601 WorkChanged
 9. Scope 계산 (Step 2)
10. 셋 다 0                                       → Code=1 NoChange, UPDATE 없이 COMMIT,
                                                    RS1 에 기존 RowVersion 반환
11. @DateChanged=1 이면 검사 Master 구성 확인      → 700
12. @ExtraChanged=1 이면 저장 NEX 무결성 + AEX Master → 700 / 701
13. 공통 업무 가능 여부                            → 308 / 309
14. @DateChanged=1 또는 @SlotChanged=1 이면
       다른 유효업무 (WorkId <> @WorkId)  2건↑ 701 / 1건 306
       UFN_HC_일정확인 (CutoffType='NORMAL')       → 300~304
       정원  AfterCount > 20                       → 305
15. @DateChanged=1 이면 TGT(400/401) → **저장 Work 무결성 3종**(스펙 §21.2a) → AEX(410~412)
       (a) NEX 개수 NOT BETWEEN 8 AND 11                        → 701
       (b) ExamSourceCode 가 Master 의 NEX/AEX 역할과 불일치      → 701
       (c) AEX 개수 > 6                                          → 701
    [X] 초안은 "NEX < 8" 하한만 봤다. 그러면 NEX 12행(CORRUPT-5)이 통과한다.
        §21.2a 는 INSERT_예약·UPDATE_예약변경·UPDATE_접수완료·UPDATE_접수추가검사
        네 SP 모두에 세 검사를 요구한다.
16. @DateChanged=0 이고 @ExtraChanged=1 이면 저장 NEX 기준 AEX (410~412)
17. 저장 — 변경 Matrix 대로
       예약일 포함 : Work UPDATE + Detail 전량 DELETE 후 재INSERT
       시간대만    : Work UPDATE 만
       AEX만       : AEX Detail DELETE/INSERT + Work LastEditDate UPDATE
       시간대+AEX  : Work UPDATE + AEX Detail DELETE/INSERT
18. COMMIT → RS0 + RS1 (새 RowVersion 재조회)
```

- [ ] **Step 4: `AfterCount` 계산 (스펙 §30.1)**

```sql
DECLARE @CurrentCount INT =
    (SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORKS]
      WHERE [ReservationDate] = @ReservationDate AND [TimeSlotCode] = @TimeSlot
        AND [StatusCode] IN ('RSV','RCP')
        AND [WorkId] <> @WorkId);                       -- 현재 Work 제외
DECLARE @AfterCount INT = @CurrentCount + 1;            -- 이 Slot 에 배치될 예정
IF @AfterCount > 20 BEGIN SET @Code = 305; SET @Field = 'TimeSlot'; END
```

현재 Work가 이미 20/20 Slot에 있고 같은 Slot을 유지하면 `@CurrentCount = 19`, `@AfterCount = 20` 이 되어 통과한다. **21로 계산하면 안 된다.**

- [ ] **Step 5: Slot 자원 정렬 획득**

**`CURSOR` 를 쓰지 않는다** — 스펙 §9.2 허용목록에 없고, applock 실패로 `THROW` 하면 `CLOSE`/`DEALLOCATE` 없이 커서가 남는다. 자원이 최대 2개이므로 분기 몇 줄로 충분하다.

```sql
DECLARE @ResA NVARCHAR(255) = N'HC|SLOT|' + CONVERT(CHAR(8), @CurDate, 112)         + N'|' + @CurSlot;
DECLARE @ResB NVARCHAR(255) = N'HC|SLOT|' + CONVERT(CHAR(8), @ReservationDate, 112) + N'|' + @TimeSlot;

IF @ResA = @ResB SET @ResB = NULL;                       -- 같은 Slot 유지면 하나만
IF @ResB IS NOT NULL AND @ResB < @ResA                   -- 문자열 오름차순 정렬
BEGIN
    DECLARE @Tmp NVARCHAR(255) = @ResA; SET @ResA = @ResB; SET @ResB = @Tmp;
END

-- @ResA 획득 (index 문서 "applock 획득 블록" 그대로: rc 로깅 + -3 은 50002)
-- IF @ResB IS NOT NULL 이면 @ResB 획득
```

**핵심은 자원명 오름차순 획득이며 이것이 교차이동 교착을 없앤다.** `HC|SLOT|yyyyMMdd|AM/PM` 형식이라 문자열 정렬이 곧 (날짜, 시간대) 정렬이다.

- [ ] **Step 6: GREEN 실행 + Commit**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/06_Procedures_Reservation_Write.sql
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/06_Reservation_Write_Tests.sql -o artifacts/logs/test_06.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_06.log | grep -cE '^PASS'
```

Expected: exit 0, PASS **23건** (`RWR-001`~`012` + `RWR-020`~`030`).

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/06_Procedures_Reservation_Write.sql database/tests/06_Reservation_Write_Tests.sql
git commit -m "feat(phase4): USP_HC_UPDATE_예약변경 구현 및 변경 Matrix 테스트 11건"
```

**완료조건:** 스펙 §45.2 의 `RWR-020`~`RWR-034` 전건 PASS. 특히 `RWR-021`(시간대만 변경 시 Detail 불변), `RWR-028`(자기 Work 오탐 없음), `RWR-030`(20/20 유지 성공)이 반드시 PASS여야 한다.

---

## Task T27: `[dbo].[USP_HC_UPDATE_예약취소]`

**목적:** `RSV → CNL` 전이를 원자적으로 수행한다.

**관련 Baseline 위치:** `05` §11.3, `00` RP-10·CP-05.

**선행조건:** `T26` 완료.

**Interfaces:** Produces RS0 + RS1 `(WorkId, Status, RowVersion)`. Parameter 2개: `@WorkId BIGINT`, `@RowVersion BINARY(8)`.

**applock:** `HC|WORK|{WorkId}` 만.

**허용 Code:** `0, 100, 500, 502, 601, 308~309`

**금지사항:** Detail을 삭제하지 않는다. 예약 마감시각을 취소 가능조건으로 쓰지 않는다. `CNL → RSV` 복원 경로를 만들지 않는다.

- [ ] **Step 1: RED**

```sql
-- RWR-040 미존재 WorkId → 500
-- RWR-041 stale RowVersion → 601
-- RWR-042 이미 CNL → 502
-- RWR-043 정상 취소 성공 + Detail 보존
-- RWR-044 취소 후 정원 감소 확인
```

`RWR-043` 예:

**계약 시나리오**: `RWR-043` = `EXEC [dbo].[USP_HC_UPDATE_예약취소] @Wc, @RvC;` → 기대 RS0 `Success=1, Code=0`.

```sql
-- RWR-043 취소 성공 + Detail 보존
--   취소 대상은 ORDER BY WorkId 로 F001 을 고정한다. F020 은 RWR-012 전용이라 건드리지 않는다.
DECLARE @Wc BIGINT = (SELECT TOP (1) w.[WorkId] FROM [dbo].[INFO_CHECKUP_WORKS] w
                       JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId]
                      WHERE p.[ChartNo] LIKE 'F0%' AND p.[ChartNo] <> 'F020' AND w.[StatusCode] = 'RSV'
                      ORDER BY w.[WorkId]);
DECLARE @Dc INT = (SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORK_EXAMS] WHERE [WorkId] = @Wc);
DECLARE @RvC BINARY(8) = (SELECT [RowVersion] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @Wc);

EXEC [dbo].[USP_HC_UPDATE_예약취소] @Wc, @RvC;

IF ((SELECT [StatusCode] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @Wc) = 'CNL'
    AND (SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORK_EXAMS] WHERE [WorkId] = @Wc) = @Dc)
    PRINT 'PASS RWR-043 취소 성공 + Detail 보존';
ELSE BEGIN PRINT 'FAIL RWR-043'; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 06_Reservation_Write_Tests 완료 ===';
```

- [ ] **Step 2: 구현 — 검증순서 (`05` §11.3)**

```text
[Transaction 밖]  필수값 (@WorkId, @RowVersion) → 100
[Transaction 안]
 1. applock HC|WORK|{@WorkId}
 2. Work 존재                → 500
 3. StatusCode <> 'RSV'      → 502
 4. RowVersion 불일치         → 601
 5. 공통 업무 가능 여부        → 308 / 309
 6. 조건부 UPDATE
      SET StatusCode='CNL', LastEditDate=@StoredNow
      WHERE WorkId=@WorkId AND StatusCode='RSV' AND RowVersion=@RowVersion
      @@ROWCOUNT=0 이면 재조회하여 502 우선, 그다음 601
 7. COMMIT → RS0 + RS1 (새 RowVersion)
```

- [ ] **Step 3: GREEN + Commit**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/06_Procedures_Reservation_Write.sql
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/06_Reservation_Write_Tests.sql -o artifacts/logs/test_06.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_06.log | grep -cE '^PASS'
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/06_Procedures_Reservation_Write.sql database/tests/06_Reservation_Write_Tests.sql
git commit -m "feat(phase4): USP_HC_UPDATE_예약취소 구현 및 취소 테스트 5건"
```

Expected: PASS **28건**.

**회귀시험:** `tests/00`~`06` 전체 재실행

**로그 경로:** `artifacts/logs/test_06.log`

**완료조건:** 스펙 §45.2 의 `RWR` 전건 PASS, 취소 후 Detail 행수 불변.
