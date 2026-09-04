# Stage 5 — SELECT Stored Procedure 7개 · Result Set 계약 검증기

**Index:** `2026-09-04-phase4-database-implementation.md`
**Spec:** `../06_DB_Transaction_Security_Seed_CANDIDATE.md`
**Tasks:** `T15` ~ `T22`

## 공통 규칙 (T15~T21 전부에 적용)

- 모든 SP는 `SET NOCOUNT ON;` 으로 시작한다. **SELECT SP는 `SET XACT_ABORT ON` 도 Transaction 도 applock 도 사용하지 않는다** (스펙 §31).
- 시작 시 `DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();` 을 한 번만 캡처하고 TVF에 그대로 전달한다.
- RS0의 5개 컬럼에 **반드시 명시적 `CAST`** 를 건다. 실패 시 RS0만 출력하고 `RETURN` 한다.
- 조회 0건은 실패가 아니다 — `Success=1, Code=0` + RS1 0행 (`05` §3.4).
- 생성 파일은 모두 `deploy/04_Procedures_Select.sql` 하나이며, 테스트는 `tests/04_Select_SP_Tests.sql` 하나다. 각 Task가 이어서 추가한다.
- 모든 파일은 **UTF-8 with BOM**.

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


## Task T15: `[dbo].[USP_HC_SELECT_공통업무상태]`

**목적:** DB 현재 업무일·운영시간 상태를 반환한다. 15개 SP 중 가장 단순하므로 **RS0 패턴의 기준 구현**이 된다.

**관련 Baseline 위치:** `05` §7.1, `00` §CP-01~04.

**선행조건:** `T14` 완료.

**Files:**
- Create: `deploy/04_Procedures_Select.sql`
- Create: `tests/04_Select_SP_Tests.sql`

**Interfaces:**
- Consumes: `[dbo].[UFN_HC_일정확인]`
- Produces: RS0 + RS1 `(Today DATE, DayName NVARCHAR(10), HolidayName NVARCHAR(100), OpenTime TIME(0), CloseTime TIME(0), IsBusinessDay BIT, WithinHours BIT, CanWorkNow BIT, BlockCode INT, BlockMessage NVARCHAR(300))`

**금지사항:** Parameter를 추가하지 않는다(0개). 업무시간 밖이라고 실패시키지 않는다 — 조회는 항상 성공한다.

- [ ] **Step 1: RED — 테스트 먼저**

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `SEL-001` | `(인자 없음)` | `0` | 공통업무상태 RS0 정확히 1행 성공 |

각 행은 `tests/contract/<Test ID>.sql` 한 파일(`EXEC` 한 번, 단언 없음)과 `tools/expected-contracts.json` 의 한 항목이 된다.

```sql
-- tests/contract/SEL-001.sql — 위 표의 Test ID 마다 파일 하나. 본문은 아래 두 줄이고 인자는 표에서 가져온다.
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_공통업무상태] (인자 없음);
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"SEL-001": { "sp": "USP_HC_SELECT_공통업무상태", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
```

`[X]` **초안의 이 자리에는 *"`INSERT … EXEC` 는 첫 번째 Result Set만 받는다"* 라고 적혀 있었다. 틀렸다.** SQL Server는 SP가 반환하는 **모든** Result Set을 대상 테이블에 넣으려 하고, 구조가 다르면 `Msg 213` 으로 배치가 죽는다(실측 확인). `USP_HC_SELECT_공통업무상태` 는 RS0(5컬럼) + RS1(10컬럼)을 항상 반환하므로 위 `SEL-001` 은 **첫 실행부터 실패한다.**

따라서 이 RED 단계는 다음으로 바꾼다.

```sql
-- tests/contract/01_공통업무상태.sql
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_공통업무상태];
GO
```

```json
// tools/expected-contracts.json
"01_공통업무상태": {
  "sp": "USP_HC_SELECT_공통업무상태",
  "rs0Success": 1,
  "rs0Code": 0,
  "resultSets": [
    { "columns": ["Success","Code","Message","Field","ServerTime"], "rows": 1 },
    { "columns": ["Today","DayName","HolidayName","OpenTime","CloseTime",
                  "IsBusinessDay","WithinHours","CanWorkNow","BlockCode","BlockMessage"], "rows": 1 }
  ]
}
```

`tests/04_Select_SP_Tests.sql` 에는 **DB 상태 단언만** 남긴다(이 SP 는 읽기 전용이라 남는 단언이 없다 — 파일에 `SEL-001` 을 두지 않는다).

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/04_Select_SP_Tests.sql -o artifacts/logs/test_04_red.log
echo "exit=$?"
```

Expected: exit **1**, `Msg 2812` — "USP_HC_SELECT_공통업무상태 저장 프로시저를 찾을 수 없습니다".

- [ ] **Step 2: 구현 (전체 SP 본문 — 이후 SP의 기준 패턴)**

```sql
SET NOCOUNT ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_SELECT_공통업무상태]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
    DECLARE @Today DATE = CONVERT(DATE, @ServerTime);

    -- RS0
    SELECT
          CAST(1 AS BIT)                    AS Success
        , CAST(0 AS INT)                    AS Code
        , CAST(N'정상 처리되었습니다.' AS NVARCHAR(300)) AS Message
        , CAST(NULL AS VARCHAR(50))         AS Field
        , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;

    -- RS1
    SELECT
          Today         = CAST(@Today AS DATE)
        , DayName       = CAST(CASE DATEDIFF(DAY, 0, @Today) % 7
                               WHEN 0 THEN N'월요일' WHEN 1 THEN N'화요일' WHEN 2 THEN N'수요일'
                               WHEN 3 THEN N'목요일' WHEN 4 THEN N'금요일' WHEN 5 THEN N'토요일'
                               ELSE N'일요일' END AS NVARCHAR(10))
        , HolidayName   = CAST(s.HolidayName AS NVARCHAR(100))
        , OpenTime      = CAST('09:00:00' AS TIME(0))
        , CloseTime     = CAST('18:00:00' AS TIME(0))
        , IsBusinessDay = CAST(s.IsBusinessDay AS BIT)
        , WithinHours   = CAST(CASE WHEN CONVERT(TIME(7), @ServerTime) >= CONVERT(TIME(7), '09:00:00')
                                     AND CONVERT(TIME(7), @ServerTime) <  CONVERT(TIME(7), '18:00:00')
                                    THEN 1 ELSE 0 END AS BIT)
        , CanWorkNow    = CAST(s.CanWorkNow AS BIT)
        , BlockCode     = CAST(s.WorkCode AS INT)
        , BlockMessage  = CAST(s.WorkMessage AS NVARCHAR(300))
    FROM [dbo].[UFN_HC_일정확인](@ServerTime, @Today, 'AM', 'NONE') s;
END
GO
```

`UFN_HC_일정확인` 의 `HolidayName`·`IsBusinessDay` 는 **요청일** 기준인데, 여기서는 요청일 = 오늘이므로 그대로 사용할 수 있다.

`[X]` **`WithinHours` 를 `WorkCode` 로 역산하면 안 된다.** TVF의 `WorkCode` 는 `TodayBiz=0 → 308` 이 `WithinHours=0 → 309` 보다 **먼저** 걸리므로, 휴무일이나 일요일에는 시각과 무관하게 `308` 이 나온다. 초안의 `CASE WHEN s.WorkCode = 309 THEN 0 ELSE 1 END` 는 **`2026-12-25`(금, 휴무일) 새벽 3시에 `WithinHours=1`** 을 보고한다. `CanWorkNow=0` 이라 업무는 막히지만 화면에 표시되는 값이 거짓이다. **SP 가 `@ServerTime` 으로 직접 계산한다.**

- [ ] **Step 3: GREEN 실행**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i deploy/04_Procedures_Select.sql -o artifacts/logs/04_select.log
echo "exit=$?"
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/04_Select_SP_Tests.sql -o artifacts/logs/test_04.log
echo "exit=$?"
```

Expected: 둘 다 exit 0, `PASS SEL-001`.

- [ ] **Step 4: RS0 메타데이터 확인**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W -Q "
SELECT p.name + ' | ' + CONVERT(varchar(3), r.column_ordinal) + ' | ' + r.name + ' | ' + r.system_type_name
FROM sys.procedures p
CROSS APPLY sys.dm_exec_describe_first_result_set_for_object(p.object_id, NULL) r
WHERE p.name = N'USP_HC_SELECT_공통업무상태' ORDER BY r.column_ordinal;"
```

Expected 정확히 5줄:

```text
1 | Success    | bit
2 | Code       | int
3 | Message    | nvarchar(300)
4 | Field      | varchar(50)
5 | ServerTime | datetime2(7)
```

- [ ] **Step 5: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/04_Procedures_Select.sql database/tests/04_Select_SP_Tests.sql
git commit -m "feat(phase4): USP_HC_SELECT_공통업무상태 구현 및 RS0 패턴 확립"
```

**완료조건:** RED에서 `Msg 2812` 관측, RS0 메타데이터 5컬럼 정확 일치.

---

## Task T16: `[dbo].[USP_HC_SELECT_수검자목록]`

**목적:** 5개 조건 `AND` 검색으로 수검자 목록을 반환한다.

**관련 Baseline 위치:** `05` §7.2, `04` §11.3 (검색연산 확정계약).

**선행조건:** `T15` 완료.

**Files:** Modify `deploy/04_Procedures_Select.sql`, `tests/04_Select_SP_Tests.sql`

**Interfaces:**
- Produces: RS0 + RS1 11컬럼 `(PatientId, ChartNo, Name, SocialNumber, Birthday, Gender, MobilePhone, Phone, Email, Zipcode, Address)`, 정렬 `Name ASC, Birthday ASC, ChartNo ASC`

**금지사항:** 이름 `%검색어%` 포함검색 금지 — **접두검색** `Name LIKE @Name + '%'` 만 사용. 조건 없는 전체조회 금지.

**Parameter (05 §7.2 그대로 5개):** `@ChartNo NVARCHAR(100)`, `@Name NVARCHAR(100)`, `@SocialNumber VARCHAR(13)`, `@Birthday VARCHAR(8)`, `@MobilePhone VARCHAR(13)` — 전부 NULL 허용.

- [ ] **Step 1: RED — 테스트 4건 추가**

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `SEL-002` | `NULL, NULL, NULL, NULL, NULL` | `103` | 조회조건 없음 103 |
| `SEL-003` | `N'T001', NULL, NULL, NULL, NULL` | `0` | ChartNo 정확검색 |
| `SEL-004` | `N'ZZZZ9999', NULL, NULL, NULL, NULL` | `0` | 조회 0건은 성공 |
| `SEL-005` | `NULL, NULL, '12345', NULL, NULL` | `101` | SocialNumber 형식 101 |

각 행은 `tests/contract/<Test ID>.sql` 한 파일(`EXEC` 한 번, 단언 없음)과 `tools/expected-contracts.json` 의 한 항목이 된다.

```sql
-- tests/contract/SEL-002.sql — 위 표의 Test ID 마다 파일 하나. 본문은 아래 두 줄이고 인자는 표에서 가져온다.
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_수검자목록] NULL, NULL, NULL, NULL, NULL;
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"SEL-002": { "sp": "USP_HC_SELECT_수검자목록", "rs0Success": 0, "rs0Code": 103 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-003": { "sp": "USP_HC_SELECT_수검자목록", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-004": { "sp": "USP_HC_SELECT_수검자목록", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-005": { "sp": "USP_HC_SELECT_수검자목록", "rs0Success": 0, "rs0Code": 101 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
```

- [ ] **Step 2: 구현 명세**

정규화 → 검증 → 조회 순서:

```text
1. 모든 문자열 파라미터에 LTRIM/RTRIM 적용, 빈 문자열은 NULL 로 변환
2. @SocialNumber, @MobilePhone 에서 '-' 제거
3. @SocialNumber 가 NOT NULL 인데 13자리 숫자가 아니면  → 101 BadValue, Field='SocialNumber'
4. @Birthday   가 NOT NULL 인데 8자리 숫자·실제 날짜가 아니면 → 101, Field='Birthday'
5. 5개가 모두 NULL 이면                                  → 103 NeedSearchCondition, Field=NULL
6. RS0 성공 + RS1 조회
```

RS1 `WHERE` 절 (전부 `AND` 결합, NULL 조건은 무시):

```sql
WHERE (@ChartNo      IS NULL OR p.[ChartNo]      =  @ChartNo)
  AND (@Name         IS NULL OR p.[Name]         LIKE @Name + N'%')
  AND (@SocialNumber IS NULL OR p.[SocialNumber] =  @SocialNumber)
  AND (@Birthday     IS NULL OR p.[Birthday]     =  @Birthday)
  AND (@MobilePhone  IS NULL OR p.[CelNumberS]   =  @MobilePhone)
ORDER BY p.[Name] ASC, p.[Birthday] ASC, p.[ChartNo] ASC;
```

RS1 컬럼 매핑 (`05` §7.2):

```text
PatientId ← PatientId       ChartNo ← ChartNo        Name ← Name
SocialNumber ← SocialNumber Birthday ← Birthday      Gender ← Gender
MobilePhone ← CelNumber     Phone ← TelNumber        Email ← EMail
Zipcode ← Zipcode           Address ← Address
```

- [ ] **Step 3: GREEN 실행 + 4건 PASS 확인**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/04_Procedures_Select.sql
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/04_Select_SP_Tests.sql -o artifacts/logs/test_04.log
echo "exit=$?"
```

- [ ] **Step 4: Commit** — `feat(phase4): USP_HC_SELECT_수검자목록 구현`

**완료조건:** `SEL-002`~`SEL-005` 의 계약 시나리오가 전부 PASS (스펙 §45.2).

---

## Task T17: `[dbo].[USP_HC_SELECT_수검자상세]`

**목적:** `@PatientId` 로 수검자 1행 상세를 반환한다.

**관련 Baseline 위치:** `05` §7.3.

**선행조건:** `T16` 완료.

**Interfaces:** Produces RS0 + RS1 14컬럼 `(… , Memo NVARCHAR(MAX), LastEditDate DATETIME)`, 정확히 1행.

**허용 Code:** `0, 100, 200` (`05` §13).

- [ ] **Step 1: RED**

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `SEL-006` | `NULL` | `100` | PatientId 필수 100 |
| `SEL-007` | `-1` | `200` | 미존재 Patient 200 |
| `SEL-008` | `@Pid` | `0` | 수검자상세 정상 |

각 행은 `tests/contract/<Test ID>.sql` 한 파일(`EXEC` 한 번, 단언 없음)과 `tools/expected-contracts.json` 의 한 항목이 된다.

```sql
-- tests/contract/SEL-006.sql — 위 표의 Test ID 마다 파일 하나. 본문은 아래 두 줄이고 인자는 표에서 가져온다.
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_수검자상세] NULL;
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"SEL-006": { "sp": "USP_HC_SELECT_수검자상세", "rs0Success": 0, "rs0Code": 100 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-007": { "sp": "USP_HC_SELECT_수검자상세", "rs0Success": 0, "rs0Code": 200 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-008": { "sp": "USP_HC_SELECT_수검자상세", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
```

- [ ] **Step 2: 구현 명세**

```text
1. @PatientId IS NULL       → 100 MissingValue, Field='PatientId'
2. Patient 미존재            → 200 PatientNotFound, Field='PatientId'
3. RS0 성공 + RS1 1행
```

RS1 컬럼: `PatientId, ChartNo, Name, SocialNumber, Birthday, Gender, MobilePhone(←CelNumber), Phone(←TelNumber), Email(←EMail), Zipcode, Address, AddressDetail, Memo, LastEditDate`

- [ ] **Step 3: GREEN + Commit** — `feat(phase4): USP_HC_SELECT_수검자상세 구현`

**완료조건:** `SEL-006`~`SEL-008` 의 계약 시나리오가 전부 PASS (스펙 §45.2).

---

## Task T18: `[dbo].[USP_HC_SELECT_수검자유효업무]`

**목적:** 현재일 이후 `RSV`/`RCP` 업무를 0~1행 반환하고, 2행 이상이면 `701` 을 반환한다.

**관련 Baseline 위치:** `05` §7.4, `00` RP-06.

**선행조건:** `T17` 완료.

**Interfaces:** Produces RS0 + RS1 7컬럼 `(WorkId, ReservationDate, TimeSlot, Status, StatusName, IsToday, RowVersion BINARY(8))`

**허용 Code:** `0, 100, 200, 701`

- [ ] **Step 1: RED — 손상 데이터로 701 을 검증한다**

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `SEL-009` | `@P0` | `0` | 유효업무 0건 정상 |
| `SEL-010` | `@P2` | `701` | 유효업무 2건 → 701 WorkDataError |

각 행은 `tests/contract/<Test ID>.sql` 한 파일(`EXEC` 한 번, 단언 없음)과 `tools/expected-contracts.json` 의 한 항목이 된다.

```sql
-- tests/contract/SEL-009.sql — 위 표의 Test ID 마다 파일 하나. 본문은 아래 두 줄이고 인자는 표에서 가져온다.
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_수검자유효업무] @P0;
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"SEL-009": { "sp": "USP_HC_SELECT_수검자유효업무", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-010": { "sp": "USP_HC_SELECT_수검자유효업무", "rs0Success": 0, "rs0Code": 701 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
```

- [ ] **Step 2: 구현 명세**

```sql
-- 조회범위
WHERE w.[PatientId] = @PatientId
  AND w.[ReservationDate] >= @Today
  AND w.[StatusCode] IN ('RSV','RCP')
```

```text
1. @PatientId IS NULL   → 100
2. Patient 미존재        → 200
3. 위 조건 COUNT >= 2   → 701 WorkDataError, Field='WorkId'
4. 그 외                → RS0 성공 + RS1 0행 또는 1행
StatusName  RSV=N'예약' / RCP=N'접수완료' / CNL=N'취소'
IsToday     CASE WHEN ReservationDate = @Today THEN 1 ELSE 0 END
```

- [ ] **Step 3: GREEN + Commit** — `feat(phase4): USP_HC_SELECT_수검자유효업무 구현`

**완료조건:** `SEL-009`·`SEL-010` PASS. 특히 `701` 이 실제로 반환되어야 한다.

---

## Task T19: `[dbo].[USP_HC_SELECT_예약접수목록]`

**목적:** Workbench 공통 목록을 반환한다.

**관련 Baseline 위치:** `05` §8.1, `04` §11.3.

**선행조건:** `T18` 완료.

**Interfaces:** Produces RS0 + RS1 11컬럼, 정렬 `ReservationDate ASC, TimeSlot ASC, Name ASC, WorkId ASC`

**허용 Code:** `0, 101, 103, 104`

- [ ] **Step 1: RED**

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `SEL-011` | `'2026-12-01', '2026-11-01', NULL, NULL, NULL` | `104` | FromDate>ToDate 104 |
| `SEL-012` | `NULL, NULL, NULL, NULL, NULL` | `103` | 조회조건 없음 103 |
| `SEL-013` | `'2026-11-01', '2026-11-30', NULL, NULL, NULL` | `0` | 날짜범위 조회 |

각 행은 `tests/contract/<Test ID>.sql` 한 파일(`EXEC` 한 번, 단언 없음)과 `tools/expected-contracts.json` 의 한 항목이 된다.

```sql
-- tests/contract/SEL-011.sql — 위 표의 Test ID 마다 파일 하나. 본문은 아래 두 줄이고 인자는 표에서 가져온다.
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_예약접수목록] '2026-12-01', '2026-11-01', NULL, NULL, NULL;
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"SEL-011": { "sp": "USP_HC_SELECT_예약접수목록", "rs0Success": 0, "rs0Code": 104 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-012": { "sp": "USP_HC_SELECT_예약접수목록", "rs0Success": 0, "rs0Code": 103 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-013": { "sp": "USP_HC_SELECT_예약접수목록", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
```

- [ ] **Step 2: 구현 명세**

```text
1. 문자열 정규화, @Status 는 UPPER
2. @Status 가 NOT NULL 인데 RSV/RCP/CNL 이 아니면 → 101, Field='Status'
3. @FromDate > @ToDate                          → 104 BadDateRange, Field='FromDate'
4. @FromDate/@ToDate/@ChartNo/@Name/@Status 가 **전부 NULL** → 103
   — `05` §8.1 은 "**Status=NULL은** 조회조건으로 보지 않음" 이라고 NULL 을 한정했다.
     값이 있는 `@Status` 는 실질 조건으로 센다. 초안은 한정어를 지워
     `EXEC … NULL, NULL, 'RSV', NULL, NULL`(상태만으로 전체 RSV 조회)을 103 으로 막았다.
5. RS0 성공 + RS1
```

```sql
WHERE (@FromDate IS NULL OR w.[ReservationDate] >= @FromDate)
  AND (@ToDate   IS NULL OR w.[ReservationDate] <= @ToDate)
  AND (@Status   IS NULL OR w.[StatusCode]      =  @Status)
  AND (@ChartNo  IS NULL OR p.[ChartNo]         =  @ChartNo)
  AND (@Name     IS NULL OR p.[Name]            LIKE @Name + N'%')
ORDER BY w.[ReservationDate], w.[TimeSlotCode], p.[Name], w.[WorkId];
```

- [ ] **Step 3: GREEN + Commit** — `feat(phase4): USP_HC_SELECT_예약접수목록 구현`

**완료조건:** `SEL-011`~`SEL-013` PASS.

---

## Task T20: `[dbo].[USP_HC_SELECT_예약접수상세]`

**목적:** Work 상세·검사구성·가능한 업무를 **5개 Result Set** 으로 반환한다.

**관련 Baseline 위치:** `05` §8.2.

**선행조건:** `T19` 완료.

**Interfaces:** Produces `RS0` + `RS1 업무상세(15컬럼)` + `RS2 국가검사항목(4컬럼)` + `RS3 추가검사항목(3컬럼)` + `RS4 가능한업무(4컬럼, 정확히 5행)`

**허용 Code:** `0, 100, 500, 701`

**금지사항:** `RS4` 를 5행이 아닌 개수로 반환하지 않는다. 업무시간 밖이라고 실패시키지 않는다 — `Allowed=0` 으로 반환한다.

- [ ] **Step 1: RED**

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `SEL-014` | `-1` | `500` | 미존재 Work 500 |
| `SEL-015` | `@Wc` | `701` | NEX 0행 Work → 701 |
| `SEL-016` | `@Wn` | `0` | 정상 Work 상세 |

각 행은 `tests/contract/<Test ID>.sql` 한 파일(`EXEC` 한 번, 단언 없음)과 `tools/expected-contracts.json` 의 한 항목이 된다.

```sql
-- tests/contract/SEL-014.sql — 위 표의 Test ID 마다 파일 하나. 본문은 아래 두 줄이고 인자는 표에서 가져온다.
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_예약접수상세] -1;
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"SEL-014": { "sp": "USP_HC_SELECT_예약접수상세", "rs0Success": 0, "rs0Code": 500 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-015": { "sp": "USP_HC_SELECT_예약접수상세", "rs0Success": 0, "rs0Code": 701 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-016": { "sp": "USP_HC_SELECT_예약접수상세", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
```

- [ ] **Step 2: 구현 명세**

```text
1. @WorkId IS NULL                                  → 100
2. Work 미존재                                       → 500
3. 저장 NEX 0행 또는 검사구성 손상                     → 701
4. RS0 성공 + RS1~RS4
```

`RS1` 정원 계산:

```sql
Capacity     = 20
CurrentCount = (SELECT COUNT(*) FROM INFO_CHECKUP_WORKS x
                 WHERE x.ReservationDate = w.ReservationDate
                   AND x.TimeSlotCode    = w.TimeSlotCode
                   AND x.StatusCode IN ('RSV','RCP'))
SeatsLeft    = CASE WHEN 20 - CurrentCount < 0 THEN 0 ELSE 20 - CurrentCount END
```

`RS2` = `ExamSourceCode='NEX'` 인 Detail 을 `MST_EXAM_ITEMS` 와 JOIN, `ExamCode ASC`.
`RS3` = `ExamSourceCode='AEX'` 인 Detail, `OptionCode ASC`.

`RS4` 는 **고정 5행**을 `VALUES` 로 만들고 각 행에 허용조건을 평가한다.

```sql
SELECT
      ActionCode    = CAST(a.Code AS VARCHAR(30))
    , Allowed       = CAST(... AS BIT)
    , ReasonCode    = CAST(... AS INT)
    , ReasonMessage = CAST(... AS NVARCHAR(300))
FROM [dbo].[INFO_CHECKUP_WORKS] w
CROSS JOIN (VALUES ('EDIT_RESERVATION'),('CANCEL_RESERVATION'),('START_RECEPTION'),
                   ('EDIT_EXTRA'),('CANCEL_RECEPTION')) a(Code)
CROSS APPLY [dbo].[UFN_HC_일정확인](@ServerTime, w.[ReservationDate], w.[TimeSlotCode], 'RECEPTION') s
WHERE w.[WorkId] = @WorkId
...
```

`[X]` **상관 인자를 받는 TVF 는 `CROSS APPLY` 여야 한다.** 초안은 `CROSS JOIN [dbo].[UFN_HC_일정확인](…, w.ReservationDate, …)` 였는데, 일반 `JOIN` 형태의 TVF 호출은 같은 `FROM` 절 다른 테이블의 컬럼을 인자로 받을 수 없어 **`Msg 4104 — 다중 파트 식별자 "w.ReservationDate"를 바인딩할 수 없습니다`** 가 난다. 게다가 `w` 가 `FROM` 절에 존재하지도 않았다. `T13`·`T14` 는 `CROSS APPLY`/`OUTER APPLY` 를 올바로 썼는데 여기만 어긋났다.

차단 우선순위 (`05` §8.2):

| ActionCode | 허용조건 | 우선순위 |
|---|---|---|
| `EDIT_RESERVATION` | `RSV` + 공통 업무 가능 | `502` → `308`/`309` |
| `CANCEL_RESERVATION` | `RSV` + 공통 업무 가능 | `502` → `308`/`309` |
| `START_RECEPTION` | `RSV` + 예약일=오늘 + 공통 업무 가능 + 접수마감 전 | `502` → `308`/`309` → `503` → `304` |
| `EDIT_EXTRA` | `RCP` + 공통 업무 가능 | `502` → `308`/`309` |
| `CANCEL_RECEPTION` | `RCP` + 공통 업무 가능 | `502` → `308`/`309` |

- [ ] **Step 3: GREEN + Commit** — `feat(phase4): USP_HC_SELECT_예약접수상세 구현 (RS0~RS4)`

**완료조건:** `SEL-014`~`SEL-016` PASS. `RS4` 5행은 `T22` 파서가 검증한다.

---

## Task T21: `[dbo].[USP_HC_SELECT_예약가능정보]`

**목적:** 예약 화면이 필요로 하는 모든 사전정보를 **6개 Result Set** 으로 반환한다. 15개 SP 중 가장 복잡하다.

**관련 Baseline 위치:** `05` §9 전체 (§9.2 signature, §9.3 NULL 조합, §9.4 Scope, §9.5 RS 순서, §9.6~§9.11 각 RS, §9.12 CanSave, §9.13 평가순서).

**선행조건:** `T20` 완료.

**Interfaces:**
- Consumes: 4개 TVF 전부
- Produces: `RS0` + `RS1 예약요약(14컬럼, 1행)` + `RS2 시간대정보(12컬럼)` + `RS3 검진대상(5컬럼)` + `RS4 국가검사항목(4컬럼)` + `RS5 추가검사항목(8컬럼)`

**허용 Code:** `0, 100~102, 200, 500~502, 601, 700~701`

**금지사항:** 휴무일·정원마감·TGT 비대상을 SP 실패로 만들지 않는다 — `RS0.Success=1, Code=0` + `RS1.CanSave=0` + `BlockCode` 로 반환한다 (`05` §3.3). `Scope=NONE` 에 `Code=1` 을 쓰지 않는다 (`Code=1` 은 Write SP No-op 전용).

- [ ] **Step 1: RED — NULL 조합 오류 6종 + Scope Cardinality**

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `SEL-017` | `@Pn, NULL, 0x0000000000000001, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0` | `102` | WorkId NULL + RowVersion 있음 102 |
| `SEL-018` | `@Pn, NULL, NULL, 'NORMAL', '2026-11-16', 'AM', NULL,0,0,0,0,0,0` | `100` | AEX BIT NULL 100 |
| `SEL-019` | `@Pn, NULL, NULL, 'WALKIN', '2026-11-16', 'AM', 0,0,0,0,0,0,0` | `102` | WALKIN 날짜 불일치 102 |
| `SEL-020` | `@Pn, NULL, NULL, 'NORMAL', '2026-12-25', 'AM', 0,0,0,0,0,0,0` | `0` | 휴무일은 SP 실패가 아님 |

각 행은 `tests/contract/<Test ID>.sql` 한 파일(`EXEC` 한 번, 단언 없음)과 `tools/expected-contracts.json` 의 한 항목이 된다.

```sql
-- tests/contract/SEL-017.sql — 위 표의 Test ID 마다 파일 하나. 본문은 아래 두 줄이고 인자는 표에서 가져온다.
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @Pn, NULL, 0x0000000000000001, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0;
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"SEL-017": { "sp": "USP_HC_SELECT_예약가능정보", "rs0Success": 0, "rs0Code": 102 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-018": { "sp": "USP_HC_SELECT_예약가능정보", "rs0Success": 0, "rs0Code": 100 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-019": { "sp": "USP_HC_SELECT_예약가능정보", "rs0Success": 0, "rs0Code": 102 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"SEL-020": { "sp": "USP_HC_SELECT_예약가능정보", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
```

- [ ] **Step 2: 구현 — `05` §9.13 평가순서를 그대로 따른다**

```text
 1. @ServerTime 캡처
 2. 필수값        @PatientId / @ReservationType / @ReservationDate 가 NULL → 100
                 AEX 7 BIT 중 하나라도 NULL → 100
                 @WorkId NOT NULL 인데 @RowVersion NULL → 100
                 ※ 초안은 @ReservationDate·@ReservationType 을 필수값 목록에서 빠뜨렸다.
                   @ReservationType NOT IN ('NORMAL','WALKIN') 는 NULL 에 대해 UNKNOWN 이라
                   101 도 발생하지 않아 NULL 이 Scope 계산까지 흘러들어간다.
                   05 §9.6 RS1 의 ReservationType·ReservationDate 는 NOT NULL 이고
                   05 §5 는 "1. 필수값 누락 → 2. 값 형식·허용값" 순서를 고정했다.
 3. 허용값        @ReservationType ∉ {NORMAL, WALKIN} → 101
 4. Parameter 조합 05 §9.3 오류표 6종 → 102
 5. Patient 존재  → 200
 6. @WorkId 있으면  Work 존재(500) → Patient 일치(501) → Status=RSV(502) → RowVersion(601)
 7. Scope 계산     DateChanged / SlotChanged / ExtraChanged (§28.1 EXCEPT 양방향)
 8. Scope=NONE     → RS0 Code=0, RS1 CanSave=0 BlockCode=0, RS2~RS5 전부 0행 후 종료
 9. ALL 이면        검사 Master 구성 확인 → 700 ExamSetupError
10. EXTRA/SLOT_EXTRA 이면 AEX Master + 저장 NEX 무결성 → 700 / 701
11. 현재 공통 업무 가능 여부 (UFN_HC_일정확인, @CutoffType='NONE')
12. ALL/SLOT/SLOT_EXTRA 이면 다른 유효업무(2건↑ 701, 1건 OtherWorkId) → 일정 → AM/PM 정원
13. ALL 이고 일정 진행 가능하면 TGT → NEX → AEX
14. EXTRA/SLOT_EXTRA 이면 저장 NEX 기준 AEX
15. RS0 ~ RS5 반환
```

`RS2` `AfterCount` (`05` §9.7 / 스펙 §30.1):

RS2는 AM·PM 두 행을 반환하며, **각 행을 "이 시간대를 선택한다면" 이라는 가정으로** 평가한다.

```sql
-- 표시용 CurrentCount 는 05 §9.7 대로 현재 Work 를 포함한 실제 인원이다
CurrentCount  = (SELECT COUNT(*) FROM INFO_CHECKUP_WORKS
                  WHERE ReservationDate = @ReservationDate
                    AND TimeSlotCode    = s.TimeSlot          -- 'AM' / 'PM'
                    AND StatusCode IN ('RSV','RCP'))

-- 계산용으로만 현재 Work 를 뺀다
ExcludingSelf = CurrentCount
                - CASE WHEN @WorkId IS NOT NULL
                        AND EXISTS (SELECT 1 FROM INFO_CHECKUP_WORKS
                                     WHERE WorkId = @WorkId
                                       AND ReservationDate = @ReservationDate
                                       AND TimeSlotCode    = s.TimeSlot
                                       AND StatusCode IN ('RSV','RCP'))
                       THEN 1 ELSE 0 END

AfterCount    = ExcludingSelf + 1
SeatsLeft     = CASE WHEN 20 - AfterCount < 0 THEN 0 ELSE 20 - AfterCount END
SlotFull      = (AfterCount > 20)
```

이 하나의 식이 `05` §9.7의 세 경우를 모두 만족한다.

| 경우 | `ExcludingSelf` | `AfterCount` | `05` §9.7 기대값 |
|---|---|---|---|
| 신규예약 (`@WorkId` NULL) | `CurrentCount` | `CurrentCount + 1` | `CurrentCount + 1` ✓ |
| 기존 Work 가 같은 Slot 유지 | `CurrentCount - 1` | `CurrentCount` | `CurrentCount` ✓ |
| 기존 Work 가 다른 Slot 으로 이동 | `CurrentCount` | `CurrentCount + 1` | `CurrentCount + 1` ✓ |

**주의:** 20/20 Slot 을 그대로 유지하는 Work 는 `ExcludingSelf=19`, `AfterCount=20`, `CanSelect=1` 이 된다. **21로 계산하면 안 된다.**

`RS1` `BlockCode` 우선순위 (`05` §9.6):

```text
308/309 → 306 → 선택 Slot 의 300~305 → (TimeSlot 미선택이고 AM/PM 모두 불가면) 307
        → 400/401 → 요청한 무효 AEX 410~412 (OptionCode ASC 첫 건)
```

`CanSave` 는 `05` §9.12의 Scope별 조건식을 그대로 구현한다.

Scope별 RS Cardinality (`05` §9.11) — 반드시 지킨다:

| Scope | RS2 | RS3 | RS4 | RS5 |
|---|---:|---:|---:|---:|
| `ALL` | 2 | 0 또는 1 | 0 또는 8~11 | 0 또는 7 |
| `SLOT` | 2 | 0 | 0 | 0 |
| `EXTRA` | 0 | 0 | 0 | 7 |
| `SLOT_EXTRA` | 2 | 0 | 0 | 7 |
| `NONE` | 0 | 0 | 0 | 0 |

0행을 반환할 때도 **동일한 컬럼 Schema** 를 유지해야 한다. `WHERE 1 = 0` 을 붙이거나 `TOP (0)` 을 사용한다.

- [ ] **Step 3: GREEN 실행**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/04_Procedures_Select.sql
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/04_Select_SP_Tests.sql -o artifacts/logs/test_04.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_04.log | grep -cE '^PASS'
```

Expected: exit 0, PASS **20건** (`SEL-001`~`SEL-020`).

- [ ] **Step 4: 회귀 — `SCH-014` 는 아직 FAIL (SP 7개)**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W \
  -Q "SELECT 'SP=' + CONVERT(varchar(5), COUNT(*)) FROM sys.procedures WHERE name LIKE 'USP[_]HC[_]%';"
```

Expected: `SP=7`

- [ ] **Step 5: Commit** — `feat(phase4): USP_HC_SELECT_예약가능정보 구현 (RS0~RS5)`

**완료조건:** SELECT SP 7개 배포 완료 + 스펙 §45.2 의 `SEL` 전건이 계약 판정으로 PASS.

---

## Task T22: `tools/verify-contract.js` — 후속 Result Set 계약 검증기

**목적:** T-SQL로는 불가능한 RS1~RS5의 순서·컬럼명·행수를 자동 검증한다 (G09).

**관련 Baseline 위치:** `05` §17.9, 스펙 §36.3.

**선행조건:** `T21` 완료.

**Files:**
- Create: `tools/verify-contract.js`
- Create: `tools/expected-contracts.json`
- Create: `tests/contract/01_공통업무상태.sql` … `tests/contract/07_예약가능정보_ALL.sql` 등 시나리오

**Interfaces:**
- Produces: `node tools/verify-contract.js <출력파일> <SP키>` → exit 0 = 일치, exit 1 = 불일치

**금지사항:** npm 패키지를 설치하지 않는다. node 표준 `fs` 만 사용한다. 검증을 통과시키려고 SP의 Result Set을 바꾸지 않는다.

- [ ] **Step 1: 시나리오 SQL 작성**

각 파일은 SP를 한 번 호출하기만 한다. 예: `tests/contract/01_공통업무상태.sql`

```sql
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_공통업무상태];
GO
```

`SELECT_예약가능정보` 는 Scope별로 5개 파일을 만든다 (`ALL` / `SLOT` / `EXTRA` / `SLOT_EXTRA` / `NONE`).

- [ ] **Step 2: `tools/expected-contracts.json` 작성**

```json
{
  "01_공통업무상태": {
    "resultSets": [
      { "columns": ["Success","Code","Message","Field","ServerTime"], "rows": 1 },
      { "columns": ["Today","DayName","HolidayName","OpenTime","CloseTime",
                    "IsBusinessDay","WithinHours","CanWorkNow","BlockCode","BlockMessage"], "rows": 1 }
    ]
  },
  "06_예약접수상세": {
    "resultSets": [
      { "columns": ["Success","Code","Message","Field","ServerTime"], "rows": 1 },
      { "columns": ["WorkId","PatientId","ChartNo","Name","Birthday","Gender","MobilePhone",
                    "ReservationDate","TimeSlot","Status","StatusName",
                    "Capacity","CurrentCount","SeatsLeft","RowVersion"], "rows": 1 },
      { "columns": ["ExamCode","ExamName","ExamType","RuleCode"], "rowsMin": 8, "rowsMax": 11 },
      { "columns": ["OptionCode","ExamCode","ExamName"], "rowsMin": 0, "rowsMax": 7 },
      { "columns": ["ActionCode","Allowed","ReasonCode","ReasonMessage"], "rows": 5 }
    ]
  }
}
```

**`T22` 에서는 SELECT SP 7개 분만 작성한다.** Write SP 분은 `T36` 에서 추가한다 — `T22` 시점에는 Write SP 가 아직 없다. `rows` 는 정확값, `rowsMin`/`rowsMax` 는 범위다.

- [ ] **Step 3: `tools/verify-contract.js` 작성**

`[X]` 파서는 컬럼명·RS 개수·행수뿐 아니라 **RS0 첫 행의 `Success`·`Code` 값을 읽어야 한다.** 스펙 §36.4 가 요구하는 것은 형상 일치가 아니라 *"관측 `Code` = 기대값"* 과 *"관측 `Code` ∈ `05` §13 허용집합"* 두 가지다. `expected-contracts.json` 의 각 항목은 `sp`·`rs0Success`·`rs0Code` 를 갖고, 허용집합은 `tools/allowed-codes.json` 에서 읽는다(`T36` Step 5).

RS0 첫 데이터행은 위에서 만든 `sets[0]` 의 헤더 바로 다음 줄이다 — `|` 로 분리해 `columns` 와 짝지으면 `Success`·`Code` 를 이름으로 꺼낼 수 있다.

```javascript
// 의존성 없음. node 표준 라이브러리만 사용.
const fs = require('fs');

const [, , outPath, key] = process.argv;
if (!outPath || !key) { console.error('usage: node verify-contract.js <sqlcmd출력파일> <SP키>'); process.exit(2); }

// sqlcmd -u 출력은 UTF-16LE + BOM
const buf = fs.readFileSync(outPath);
const text = buf.slice(0, 2).equals(Buffer.from([0xff, 0xfe]))
  ? buf.slice(2).toString('utf16le')
  : buf.toString('utf8');

const lines = text.split(/\r?\n/);
const isSep = (s) => /^-+( +-+)*\s*$/.test(s) || /^-+(\|-+)*\s*$/.test(s);

// 구분선을 마커로 Result Set 을 분리한다.
// 구분선 바로 위 줄이 헤더, 다음 구분선(또는 EOF)까지가 데이터행.
const sets = [];
for (let i = 0; i < lines.length; i++) {
  if (!isSep(lines[i]) || i === 0) continue;
  const columns = lines[i - 1].split('|').map((s) => s.trim()).filter((s) => s.length);
  let rows = 0;
  for (let j = i + 1; j < lines.length; j++) {
    if (j + 1 < lines.length && isSep(lines[j + 1])) break;   // 다음 RS 의 헤더
    if (!lines[j].trim()) continue;
    if (/^\(\d+ /.test(lines[j].trim())) continue;             // "(N rows affected)"
    rows++;
  }
  sets.push({ columns, rows });
  }

const expected = JSON.parse(fs.readFileSync(`${__dirname}/expected-contracts.json`, 'utf8'))[key];
if (!expected) { console.error(`FAIL 기대 계약 없음: ${key}`); process.exit(1); }

let fail = 0;
if (sets.length !== expected.resultSets.length) {
  console.error(`FAIL ${key} Result Set 개수 ${sets.length} != 기대 ${expected.resultSets.length}`);
  fail++;
}
expected.resultSets.forEach((e, i) => {
  const a = sets[i];
  if (!a) { console.error(`FAIL ${key} RS${i} 누락`); fail++; return; }
  if (a.columns.join(',') !== e.columns.join(',')) {
    console.error(`FAIL ${key} RS${i} 컬럼 불일치\n  실측: ${a.columns.join(',')}\n  기대: ${e.columns.join(',')}`);
    fail++;
  }
  const lo = e.rows !== undefined ? e.rows : e.rowsMin;
  const hi = e.rows !== undefined ? e.rows : e.rowsMax;
  if (a.rows < lo || a.rows > hi) {
    console.error(`FAIL ${key} RS${i} 행수 ${a.rows} 가 기대 ${lo}~${hi} 밖`);
    fail++;
  }
});

if (fail === 0) console.log(`PASS ${key} Result Set 계약 일치 (${sets.length}개 RS)`);
process.exit(fail === 0 ? 0 : 1);
```

- [ ] **Step 4: RED — 기대값을 일부러 틀리게 두고 실패를 확인**

`expected-contracts.json` 의 `01_공통업무상태` RS1 컬럼에서 `Today` 를 `Todayx` 로 바꾼 뒤 실행한다.

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -W -w 65535 -s"|" \
       -i tests/contract/01_공통업무상태.sql -o artifacts/logs/rs_01.txt
node tools/verify-contract.js artifacts/logs/rs_01.txt 01_공통업무상태
echo "exit=$?"
```

Expected: exit **1**, `FAIL … RS1 컬럼 불일치`. 파서가 실제로 동작함을 증명한다.

- [ ] **Step 5: GREEN — 기대값을 되돌리고 7개 SELECT SP 전부 검증**

재사용을 위해 **`scripts/verify-contract-all.sh`** 로 만든다. `test.sh` 가 이 스크립트를 호출한다(스펙 §8.4).

```bash
#!/usr/bin/env bash
# scripts/verify-contract-all.sh
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'; DB='HealthCheckupReservationReceptionDb'
OUT=artifacts/reports/contract-verify.txt
mkdir -p artifacts/logs artifacts/reports
: > "$OUT"
FAILED=0

# [X] Write SP 의 성공 시나리오는 업무시간(월~토 09:00~18:00, 비휴무일) 밖에서 RS0(308/309) 하나만
#     반환한다. expected-contracts.json 이 RS 2개를 기대하므로 야간 회귀는 반드시 FAIL 한다.
#     Write SP 계약은 업무시간에만 판정하고, 밖이면 SKIP 을 남긴다. SKIP 은 PASS 가 아니다.
BIZ=$(sqlcmd -S "$SRV" -E -d "$DB" -b -I -h-1 -W -Q "SET NOCOUNT ON;
  SELECT CASE WHEN DATEPART(WEEKDAY, SYSDATETIME()) BETWEEN 2 AND 7
              AND CONVERT(TIME(0), SYSDATETIME()) >= '09:00:00'
              AND CONVERT(TIME(0), SYSDATETIME()) <  '18:00:00'
              AND NOT EXISTS (SELECT 1 FROM dbo.MST_HOLIDAYS
                               WHERE HolidayDate = CONVERT(DATE, SYSDATETIME()) AND IsActive = 1)
         THEN 1 ELSE 0 END;" | tr -d ' \r')

for f in tests/contract/*.sql; do
  k=$(basename "$f" .sql)
  # SELECT SP 계약(SEL-*)은 시간대와 무관하다. Write SP 계약만 가드한다.
  case "$k" in
    PWR-*|RWR-*|CWR-*)
      if [ "${BIZ:-0}" -ne 1 ]; then
        echo "SKIP $k 업무시간 밖 — Write SP 는 308/309 를 업무 Rule 보다 먼저 판정한다" >> "$OUT"
        continue
      fi ;;
  esac
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -W -w 65535 -s"|" \
         -i "$f" -o "artifacts/logs/rs_${k}.txt" || FAILED=1
  node tools/verify-contract.js "artifacts/logs/rs_${k}.txt" "$k" >> "$OUT" 2>&1 || FAILED=1
done

cat "$OUT"
echo "contract-verify FAILED=$FAILED"
exit $FAILED
```

`[X]` **초안의 `for … done | tee file` 은 세 가지가 동시에 틀렸다.**
1. 파이프 좌변이 서브셸이라 `|| exit 1` 이 스크립트를 끝내지 못한다.
2. `$?` 는 `tee` 의 것(항상 0)이라 실패해도 `exit=0` 으로 보인다.
3. `verify-contract.js` 가 FAIL 을 `console.error`(stderr)로 내는데 `tee` 는 stdout만 잡는다 → **G09 증거파일에 구조적으로 PASS 만 기록된다.**

`>> "$OUT" 2>&1` 로 양쪽을 누적하고 플래그로 집계한다. `verify-contract.js` 도 FAIL 을 stdout 에 함께 쓰도록 고친다.

`-W -w 65535` 를 빼지 않는다 — 기본 폭 80에서 줄이 접히면 파서가 무너진다(실측 확인).

```bash
chmod +x scripts/verify-contract-all.sh
./scripts/verify-contract-all.sh
echo "exit=$?"
```

Expected: 전부 `PASS`, `exit=0`. 특히 `07_예약가능정보_ALL` 은 **RS 6개**, `06_예약접수상세` 는 **RS 5개 + RS4 정확히 5행**이어야 한다.

- [ ] **Step 6: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/tools/ database/tests/contract/ database/artifacts/reports/contract-verify.txt
git commit -m "test(phase4): 후속 Result Set 계약 검증기 도입 (node, 의존성 0)"
```

**회귀시험:** `T36` 에서 15개 SP 전부 재검증.

**로그 경로:** `artifacts/logs/rs_*.txt`, `artifacts/reports/contract-verify.txt`

**Rollback/Cleanup:** 검증기는 읽기 전용이다.

**완료조건:** RED에서 실제 FAIL 관측, GREEN에서 SELECT SP 11개 시나리오 전부 PASS.
