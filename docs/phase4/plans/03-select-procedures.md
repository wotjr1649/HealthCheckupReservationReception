`[!]` **이 계획서의 스키마 참조는 `plans/09`·`plans/10` 이 교체했다** ― 컬럼명 한글화,
검사구성의 `예약접수`·`완료이력` 흡수, `검사항목` 삭제, `변경이력` EAV 전환. 당시 구조는 git 이력에 있다.

# Stage 5 — SELECT Stored Procedure 8개 · Result Set 계약 검증기

**Index:** `2026-09-04-phase4-database-implementation.md`
**Spec:** `../06_DB_Transaction_Security_Seed_CANDIDATE.md`
**Tasks:** `T15` ~ `T22` · `T22b`

## 공통 규칙 (T15~T21 전부에 적용)

- 모든 SP는 `SET NOCOUNT ON;` 으로 시작한다. **SELECT SP는 `SET XACT_ABORT ON` 도 Transaction 도 applock 도 사용하지 않는다** (스펙 §31).
- 시작 시 `DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();` 을 한 번만 캡처하고 TVF에 그대로 전달한다.
- RS0의 5개 컬럼에 **반드시 명시적 `CAST`** 를 건다. 실패 시 RS0만 출력하고 `RETURN` 한다.
- 조회 0건은 실패가 아니다 — `Success=1, Code=0` + RS1 0행 (`05` §3.4).
- 생성 파일은 모두 `deploy/04_Procedures_Select.sql` 하나이며, 테스트는 `tests/04_Select_SP_Tests.sql` 하나다. 각 Task가 이어서 추가한다.
- 모든 파일은 **UTF-8 with BOM**.

### 계약 시나리오 규약 — 한 곳에만 적는다

`[X]` **초안은 `T15`~`T21` 각각에서 `tests/contract/<Test ID>.sql` 이라 하고 `T22` 와 스펙은
`tests/contract/<NN>_<시나리오>.sql` 이라 해 명명이 두 갈래였다.** 스펙 §7 트리와 §36.3 이 후자이고
`06 CANDIDATE` 가 계획을 이기므로(`database/CLAUDE.md` §4) **후자로 통일한다.**

`[X]` **`T22` 는 시나리오를 *"SELECT SP 11개"* 로 셌다.** §45.2 는 `SEL` **전건**의 RS0 Code 판정을
`contract/` 에 맡긴다. SP 단위 11개로는 `SEL` 20건 중 9건이 기계 판정 없이 남아 §45.2 를 어긴다.
**시나리오는 `SEL` 1건당 파일 하나(20개) + `05` §9.11 Scope Cardinality 를 덮는 5개 = 25개다.**

```text
파일명   tests/contract/<NN>_<시나리오>.sql      NN 은 01~20 이 SEL-001~SEL-020 과 1:1, 21~25 가 Scope. SEL-021~024 는 Test ID 명이다
키       expected-contracts.json 의 키 = 파일명(확장자 제외). verify-contract-all.sh 가 basename 을 그대로 쓴다
본문     SET NOCOUNT ON; → (필요하면) DECLARE 로 인자 확보 → EXEC 한 번 → GO
소유     시나리오 .sql 은 그 SP 를 만드는 Task 가 함께 만든다 — RED 를 그 파일로 관측하기 때문이다.
         T22 는 검증기·기대값·러너를 만들고 Scope 전용 21~25 를 더한다.
```

`[X]` **계약 파일은 단독 실행된다.** 초안 예시는 `@Pid`·`@P0`·`@P2`·`@Wc`·`@Wn`·`@Pn` 을 선언 없이 썼는데
`verify-contract-all.sh` 가 파일 하나씩 `sqlcmd -i` 로 돌리므로 `Msg 137` 이 난다.
인자는 파일 안에서 Fixture(`tests/00`·`00b`)로부터 `DECLARE` 로 뽑는다.

`[X]` **`T15`~`T21` 의 완료조건은 계약 PASS 를 요구할 수 없다.** 검증기가 `T22` 라서 그 시점에는 존재하지 않는다
(`T12` 가 `T13` 산출물을, `T14b` 가 `T14` 를 요구했던 것과 같은 순서 함정이다).
`T15`~`T21` 은 **SP 배포 + RS0 메타데이터 관측**까지가 완료조건이고, §45.2 의 `SEL` 전건 판정은 `T22` 의 완료조건이다.

**시나리오 25개 — 이 표가 파일명과 키의 단일 출처다.** `T22` 가 이대로 만든다.

| NN_시나리오 | Test ID | SP | 인자 출처 |
|---|---|---|---|
| `01_공통업무상태` | `SEL-001` | `공통업무상태` | 없음 |
| `02_수검자목록_조건없음` | `SEL-002` | `수검자목록` | 리터럴 |
| `03_수검자목록_ChartNo` | `SEL-003` | `수검자목록` | 리터럴 |
| `04_수검자목록_0건` | `SEL-004` | `수검자목록` | 리터럴 |
| `05_수검자목록_주민번호형식` | `SEL-005` | `수검자목록` | 리터럴 |
| `06_수검자상세_PatientId없음` | `SEL-006` | `수검자상세` | 리터럴 |
| `07_수검자상세_미존재` | `SEL-007` | `수검자상세` | 리터럴 |
| `08_수검자상세_정상` | `SEL-008` | `수검자상세` | `T015` |
| `09_수검자유효업무_0건` | `SEL-009` | `수검자유효업무` | `T015` |
| `10_수검자유효업무_2건701` | `SEL-010` | `수검자유효업무` | `T012` (CORRUPT-1) |
| `11_예약접수목록_날짜역전` | `SEL-011` | `예약접수목록` | 리터럴 |
| `12_예약접수목록_조건없음` | `SEL-012` | `예약접수목록` | 리터럴 |
| `13_예약접수목록_날짜범위` | `SEL-013` | `예약접수목록` | 리터럴 |
| `14_예약접수상세_미존재` | `SEL-014` | `예약접수상세` | 리터럴 |
| `15_예약접수상세_NEX0행701` | `SEL-015` | `예약접수상세` | `T013` (CORRUPT-2) |
| `16_예약접수상세_정상` | `SEL-016` | `예약접수상세` | `T020` |
| `17_예약가능정보_RowVersion단독` | `SEL-017` | `예약가능정보` | `T015` |
| `18_예약가능정보_AEX_NULL` | `SEL-018` | `예약가능정보` | `T015` |
| `19_예약가능정보_WALKIN_날짜불일치` | `SEL-019` | `예약가능정보` | `T015` |
| `20_예약가능정보_휴무일` | `SEL-020` | `예약가능정보` | `T015` |
| `21_예약가능정보_Scope_ALL` | — | `예약가능정보` | `T015` |
| `22_예약가능정보_Scope_SLOT` | — | `예약가능정보` | `T020` |
| `23_예약가능정보_Scope_EXTRA` | — | `예약가능정보` | `T020` |
| `24_예약가능정보_Scope_SLOT_EXTRA` | — | `예약가능정보` | `T020` |
| `25_예약가능정보_Scope_NONE` | — | `예약가능정보` | `T020` |
| `SEL-021_변경이력_정상` | `SEL-021` | `변경이력` | `변경이력` 최초 `수검자` 행 |
| `SEL-022_변경이력_기록0건` | `SEL-022` | `변경이력` | 리터럴 `-1` |
| `SEL-023_변경이력_TargetTable_허용밖` | `SEL-023` | `변경이력` | 리터럴 |
| `SEL-024_변경이력_TargetTable_NULL` | `SEL-024` | `변경이력` | 리터럴 |

`[R3]` **`SEL-021`~`024` 만 Test ID 로 이름 짓는다.** `SP-LOG-01` 은 `변경이력` 에 행이 있어야 정상 조회를
시험할 수 있는데, 그 행은 Write SP 가 만든다. `verify-contract-all.sh` 가 glob 사전순으로 도는 것을 이용해
`PWR-*` 뒤에 오도록 이름을 지었다(`P` < `S`). 숫자 접두사 `26_` 을 쓰면 `PWR-*` 앞으로 가서 0행이 된다.

`[X]` **Scope 시나리오의 `@WorkId` 는 `RSV` Work 여야 한다.** 표는 처음에 `23`·`24` 를 `T014` 로 적었는데
`T014` 의 Work 는 `tests/00b` 가 만든 `RCP` 라 SP 가 `502 WrongStatus` 로 끊는다. `T020` 의 `RSV` Work 를 쓴다.

`21`~`25` 는 Test ID 가 없다 — `05` §9.11 Scope Cardinality 를 덮는 형상 전용 시나리오이고,
카탈로그에 없는 키이므로 §45.2 건수에 넣지 않는다.

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
- Create: `tests/contract/01_공통업무상태.sql`

**Interfaces:**
- Consumes: `[dbo].[UFN_HC_일정확인]`
- Produces: RS0 + RS1 `(Today DATE, DayName NVARCHAR(10), HolidayName NVARCHAR(100), OpenTime TIME(0), CloseTime TIME(0), IsBusinessDay BIT, WithinHours BIT, CanWorkNow BIT, BlockCode INT, BlockMessage NVARCHAR(300))`

**금지사항:** Parameter를 추가하지 않는다(0개). 업무시간 밖이라고 실패시키지 않는다 — 조회는 항상 성공한다.

- [ ] **Step 1: RED — 테스트 먼저**

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `SEL-001` | `(인자 없음)` | `0` | 공통업무상태 RS0 정확히 1행 성공 |

각 행은 계약 시나리오 파일 하나와 `tools/expected-contracts.json` 의 한 항목이 된다.
파일명·키·본문 규약은 위 **계약 시나리오 규약**에 있다. 시나리오 `.sql` 은 이 Task 가 만들고, 기대값·검증기는 `T22` 가 만든다.

`[X]` **초안의 이 자리에는 *"`INSERT … EXEC` 는 첫 번째 Result Set만 받는다"* 라고 적혀 있었다. 틀렸다.**
SQL Server는 SP가 반환하는 **모든** Result Set을 대상 테이블에 넣으려 하고, 구조가 다르면 `Msg 213` 으로
배치가 죽는다(실측 확인). 그래서 계약 판정은 `INSERT … EXEC` 가 아니라 `EXEC` 한 번 + 출력 파싱이다.

`[X]` 초안의 예시 본문은 `EXEC [dbo].[USP_HC_SELECT_공통업무상태] (인자 없음);` 이었다 —
`(인자 없음)` 은 설명이지 SQL 이 아니라 그대로 실행하면 구문오류다. Parameter 가 0개면 인자를 쓰지 않는다.

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

`tests/04_Select_SP_Tests.sql` 에는 **DB 상태 단언만** 남긴다 — 파일에 `SEL-001` 을 두지 않는다.

`[X]` **그렇다고 단언이 하나도 없는 파일을 두지 않는다.** `test.sh` 가 이 파일을 돌리고 `grep '^(PASS|FAIL)'`
로 판정하는데 아무 출력이 없으면 **항상 조용히 통과한다** — `plans/08` 이 `tests/14` 에서 실제로 겪은 사고다.
SELECT SP 가 읽기 전용이라는 것 자체가 판정할 수 있는 DB 상태 불변조건이다. 7테이블 행수 지문을
호출 전후로 비교한다. 카탈로그에 없는 표식이므로 Test ID 를 쓰지 않는다(`FIX-DEPLOY`·`FIX-SSN-PRE` 와 같은 부류).

**지문 식을 SP 마다 되풀이하지 않는다.** 전 SELECT SP 를 한 번에 호출하고 앞뒤로 한 번씩만 잰다 —
`T16`~`T21` 은 `EXEC` 한 줄씩만 더한다. FAIL 이면 `EXEC` 목록을 반씩 잘라 다시 돌리면 범인이 나온다.
실패 경로(`101`·`103` 등)도 함께 지나가게 한다 — 실패해도 아무것도 쓰지 않아야 하기 때문이다.

```sql
SET NOCOUNT ON;
DECLARE @Fail INT = 0;

DECLARE @Before VARCHAR(100) =
      CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[수검자]))   + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[예약접수])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[완료이력])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[변경이력])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[검사코드])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[휴무일]));

-- T16~T21 은 여기에 EXEC 한 줄씩만 더한다. 성공 경로와 실패 경로를 모두 지난다.
-- [X] EXEC sp (SELECT …) 로 인자를 넘기지 않는다. 괄호 안이 인자가 아니라
--     별도 SELECT 문으로 파싱되어 SP 는 인자를 못 받고 "매개 변수가 필요하지만
--     제공되지 않았습니다" 로 실패한다(실측 확인). PRINT 의 하위 쿼리와 같은 계열이다.
--     Fixture 에서 뽑는 인자는 DECLARE 로 먼저 받는다.
EXEC [dbo].[USP_HC_SELECT_공통업무상태];

DECLARE @After VARCHAR(100) = /* @Before 와 같은 식 */ NULL;

IF @Before = @After
    PRINT 'PASS FIX-RO-01 SELECT SP 호출이 DB 상태를 바꾸지 않았다  ' + @After;
ELSE BEGIN PRINT 'FAIL FIX-RO-01 읽기전용 위반  before=' + @Before + '  after=' + @After; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 04_Select_SP_Tests 완료 ===';
GO
```

`@After` 는 `@Before` 와 **같은 식을 그대로 한 번 더 쓴다.** 헬퍼 함수로 묶지 않는다 — 스칼라 UDF 는
계약(TVF 4 / SP 15) 밖이고 `SCH-013`·`SCH-014` 의 개수 단언을 깨뜨린다.

`[X]` **초안은 `tests/04_Select_SP_Tests.sql` 을 돌려 `Msg 2812` 를 기대했다.** 그 파일에는 이 SP 를
참조하는 문장이 하나도 없으므로 `Msg 2812` 가 날 수 없다. RED 는 시나리오 파일로 관측한다.
SP 이름이 한글이므로 `-Q` 로 넘기지 않고 BOM 이 있는 파일로 넘긴다(`database/CLAUDE.md` §5).

```bash
RC=0
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/contract/01_공통업무상태.sql -o artifacts/logs/rs_01_red.txt || RC=$?
echo "exit=$RC"
iconv -f UTF-16 -t UTF-8 artifacts/logs/rs_01_red.txt | head -3
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
       -i tests/contract/01_공통업무상태.sql -o artifacts/logs/rs_01.txt
echo "exit=$?"
```

`[X]` 초안 Expected 는 `PASS SEL-001` 이었는데 바로 위에서 *"파일에 `SEL-001` 을 두지 않는다"* 고 정했다 —
`tests/04` 는 이 SP 에 대해 아무것도 출력하지 않는다. 계약 판정은 `T22` 의 검증기가 한다.

Expected: 배포·시나리오 둘 다 exit 0. `rs_01.txt` 에 RS 2개(5컬럼 + 10컬럼)가 보인다.

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

**Files:** Modify `deploy/04_Procedures_Select.sql` · Create `tests/contract/02_수검자목록_조건없음.sql`, `tests/contract/03_수검자목록_ChartNo.sql`, `tests/contract/04_수검자목록_0건.sql`, `tests/contract/05_수검자목록_주민번호형식.sql`

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

각 행은 계약 시나리오 파일 하나와 `tools/expected-contracts.json` 의 한 항목이 된다 — 파일명·키는 위 **계약 시나리오 규약**의 표가 정한다. 시나리오 `.sql` 은 이 Task 가 만들고, 기대값·검증기는 `T22` 가 만든다.

```sql
-- tests/contract/02_수검자목록_조건없음.sql
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_수검자목록] NULL, NULL, NULL, NULL, NULL;
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"02_수검자목록_조건없음": { "sp": "USP_HC_SELECT_수검자목록", "rs0Success": 0, "rs0Code": 103 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"03_수검자목록_ChartNo": { "sp": "USP_HC_SELECT_수검자목록", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"04_수검자목록_0건": { "sp": "USP_HC_SELECT_수검자목록", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"05_수검자목록_주민번호형식": { "sp": "USP_HC_SELECT_수검자목록", "rs0Success": 0, "rs0Code": 101 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
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
WHERE (@ChartNo      IS NULL OR p.[차트번호]      =  @ChartNo)
  AND (@Name         IS NULL OR p.[Name]         LIKE @Name + N'%')
  AND (@SocialNumber IS NULL OR p.[주민번호] =  @SocialNumber)
  AND (@Birthday     IS NULL OR p.[생년월일]     =  @Birthday)
  AND (@MobilePhone  IS NULL OR p.[CelNumberS]   =  @MobilePhone)
ORDER BY p.[Name] ASC, p.[생년월일] ASC, p.[차트번호] ASC;
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

**완료조건:** SP 배포 exit 0 + 시나리오 4개 단독 실행 exit 0. 계약 판정은 `T22` 다.

---

## Task T17: `[dbo].[USP_HC_SELECT_수검자상세]`

**목적:** `@PatientId` 로 수검자 1행 상세를 반환한다.

**관련 Baseline 위치:** `05` §7.3.

**선행조건:** `T16` 완료.

**Files:** Modify `deploy/04_Procedures_Select.sql` · Create `tests/contract/06_수검자상세_PatientId없음.sql`, `tests/contract/07_수검자상세_미존재.sql`, `tests/contract/08_수검자상세_정상.sql`

**Interfaces:** Produces RS0 + RS1 14컬럼 `(… , Memo NVARCHAR(MAX), LastEditDate DATETIME)`, 정확히 1행.

**허용 Code:** `0, 100, 200` (`05` §13).

- [ ] **Step 1: RED**

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `SEL-006` | `NULL` | `100` | PatientId 필수 100 |
| `SEL-007` | `-1` | `200` | 미존재 Patient 200 |
| `SEL-008` | `@Pid` | `0` | 수검자상세 정상 |

각 행은 계약 시나리오 파일 하나와 `tools/expected-contracts.json` 의 한 항목이 된다 — 파일명·키는 위 **계약 시나리오 규약**의 표가 정한다. 시나리오 `.sql` 은 이 Task 가 만들고, 기대값·검증기는 `T22` 가 만든다.

```sql
-- tests/contract/06_수검자상세_PatientId없음.sql
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_수검자상세] NULL;
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"06_수검자상세_PatientId없음": { "sp": "USP_HC_SELECT_수검자상세", "rs0Success": 0, "rs0Code": 100 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"07_수검자상세_미존재": { "sp": "USP_HC_SELECT_수검자상세", "rs0Success": 0, "rs0Code": 200 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"08_수검자상세_정상": { "sp": "USP_HC_SELECT_수검자상세", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
```

- [ ] **Step 2: 구현 명세**

```text
1. @PatientId IS NULL       → 100 MissingValue, Field='PatientId'
2. Patient 미존재            → 200 PatientNotFound, Field='PatientId'
3. RS0 성공 + RS1 1행
```

RS1 컬럼: `PatientId, ChartNo, Name, SocialNumber, Birthday, Gender, MobilePhone(←CelNumber), Phone(←TelNumber), Email(←EMail), Zipcode, Address, AddressDetail, Memo, LastEditDate`

- [ ] **Step 3: GREEN + Commit** — `feat(phase4): USP_HC_SELECT_수검자상세 구현`

**완료조건:** SP 배포 exit 0 + 시나리오 3개 단독 실행 exit 0. 계약 판정은 `T22` 다.

---

## Task T18: `[dbo].[USP_HC_SELECT_수검자유효업무]`

**목적:** 현재일 이후 `RSV`/`RCP` 업무를 0~1행 반환하고, 2행 이상이면 `701` 을 반환한다.

**관련 Baseline 위치:** `05` §7.4, `00` RP-06.

**선행조건:** `T17` 완료.

**Files:** Modify `deploy/04_Procedures_Select.sql` · Create `tests/contract/09_수검자유효업무_0건.sql`, `tests/contract/10_수검자유효업무_2건701.sql`

**Interfaces:** Produces RS0 + RS1 7컬럼 `(WorkId, ReservationDate, TimeSlot, Status, StatusName, IsToday, RowVersion BINARY(8))`

**허용 Code:** `0, 100, 200, 701`

- [ ] **Step 1: RED — 손상 데이터로 701 을 검증한다**

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `SEL-009` | `@P0` | `0` | 유효업무 0건 정상 |
| `SEL-010` | `@P2` | `701` | 유효업무 2건 → 701 WorkDataError |

각 행은 계약 시나리오 파일 하나와 `tools/expected-contracts.json` 의 한 항목이 된다 — 파일명·키는 위 **계약 시나리오 규약**의 표가 정한다. 시나리오 `.sql` 은 이 Task 가 만들고, 기대값·검증기는 `T22` 가 만든다.

```sql
-- tests/contract/09_수검자유효업무_0건.sql
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_수검자유효업무] @P0;
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"09_수검자유효업무_0건": { "sp": "USP_HC_SELECT_수검자유효업무", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"10_수검자유효업무_2건701": { "sp": "USP_HC_SELECT_수검자유효업무", "rs0Success": 0, "rs0Code": 701 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
```

- [ ] **Step 2: 구현 명세**

```sql
-- 조회범위
WHERE w.[수검자ID] = @PatientId
  AND w.[예약일] >= @Today
  AND w.[상태코드] IN ('RSV','RCP')
```

```text
1. @PatientId IS NULL   → 100
2. Patient 미존재        → 200
3. 위 조건 COUNT >= 2   → 701 WorkDataError, Field='WorkId'
4. 그 외                → RS0 성공 + RS1 0행 또는 1행
StatusName  RSV=N'예약' / RCP=N'접수완료' / CNR=N'예약취소' / CNC=N'접수취소'
IsToday     CASE WHEN ReservationDate = @Today THEN 1 ELSE 0 END
```

- [ ] **Step 3: GREEN + Commit** — `feat(phase4): USP_HC_SELECT_수검자유효업무 구현`

**완료조건:** SP 배포 exit 0 + 시나리오 2개 단독 실행 exit 0. `10_수검자유효업무_2건701` 의 RS0 에 `701` 이 실제로 보여야 한다. 계약 판정은 `T22` 다.

---

## Task T19: `[dbo].[USP_HC_SELECT_예약접수목록]`

**목적:** Workbench 공통 목록을 반환한다.

**관련 Baseline 위치:** `05` §8.1, `04` §11.3.

**선행조건:** `T18` 완료.

**Files:** Modify `deploy/04_Procedures_Select.sql` · Create `tests/contract/11_예약접수목록_날짜역전.sql`, `tests/contract/12_예약접수목록_조건없음.sql`, `tests/contract/13_예약접수목록_날짜범위.sql`

**Interfaces:** Produces RS0 + RS1 11컬럼, 정렬 `ReservationDate ASC, TimeSlot ASC, Name ASC, WorkId ASC`

**허용 Code:** `0, 101, 103, 104`

- [ ] **Step 1: RED**

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `SEL-011` | `'2026-12-01', '2026-11-01', NULL, NULL, NULL` | `104` | FromDate>ToDate 104 |
| `SEL-012` | `NULL, NULL, NULL, NULL, NULL` | `103` | 조회조건 없음 103 |
| `SEL-013` | `'2026-11-01', '2026-11-30', NULL, NULL, NULL` | `0` | 날짜범위 조회 |

각 행은 계약 시나리오 파일 하나와 `tools/expected-contracts.json` 의 한 항목이 된다 — 파일명·키는 위 **계약 시나리오 규약**의 표가 정한다. 시나리오 `.sql` 은 이 Task 가 만들고, 기대값·검증기는 `T22` 가 만든다.

```sql
-- tests/contract/11_예약접수목록_날짜역전.sql
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_예약접수목록] '2026-12-01', '2026-11-01', NULL, NULL, NULL;
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"11_예약접수목록_날짜역전": { "sp": "USP_HC_SELECT_예약접수목록", "rs0Success": 0, "rs0Code": 104 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"12_예약접수목록_조건없음": { "sp": "USP_HC_SELECT_예약접수목록", "rs0Success": 0, "rs0Code": 103 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"13_예약접수목록_날짜범위": { "sp": "USP_HC_SELECT_예약접수목록", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
```

- [ ] **Step 2: 구현 명세**

```text
1. 문자열 정규화, @Status 는 UPPER
2. @Status 가 NOT NULL 인데 RSV/RCP/CNR/CNC 가 아니면 → 101, Field='Status'
3. @FromDate > @ToDate                          → 104 BadDateRange, Field='FromDate'
4. @FromDate/@ToDate/@ChartNo/@Name/@Status 가 **전부 NULL** → 103
   — `05` §8.1 은 "**Status=NULL은** 조회조건으로 보지 않음" 이라고 NULL 을 한정했다.
     값이 있는 `@Status` 는 실질 조건으로 센다. 초안은 한정어를 지워
     `EXEC … NULL, NULL, 'RSV', NULL, NULL`(상태만으로 전체 RSV 조회)을 103 으로 막았다.
5. RS0 성공 + RS1
```

```sql
WHERE (@FromDate IS NULL OR w.[예약일] >= @FromDate)
  AND (@ToDate   IS NULL OR w.[예약일] <= @ToDate)
  AND (@Status   IS NULL OR w.[상태코드]      =  @Status)
  AND (@ChartNo  IS NULL OR p.[차트번호]         =  @ChartNo)
  AND (@Name     IS NULL OR p.[성명]            LIKE @Name + N'%')
ORDER BY w.[예약일], w.[시간대코드], p.[성명], w.[업무ID];
```

- [ ] **Step 3: GREEN + Commit** — `feat(phase4): USP_HC_SELECT_예약접수목록 구현`

**완료조건:** SP 배포 exit 0 + 시나리오 3개 단독 실행 exit 0. 계약 판정은 `T22` 다.

---

## Task T20: `[dbo].[USP_HC_SELECT_예약접수상세]`

**목적:** Work 상세·검사구성·가능한 업무를 **5개 Result Set** 으로 반환한다.

**관련 Baseline 위치:** `05` §8.2.

**선행조건:** `T19` 완료.

**Files:** Modify `deploy/04_Procedures_Select.sql` · Create `tests/contract/14_예약접수상세_미존재.sql`, `tests/contract/15_예약접수상세_NEX0행701.sql`, `tests/contract/16_예약접수상세_정상.sql`

**Interfaces:** Produces `RS0` + `RS1 업무상세(15컬럼)` + `RS2 국가검사항목(4컬럼)` + `RS3 추가검사항목(3컬럼)` + `RS4 가능한업무(4컬럼, 정확히 5행)`

**허용 Code:** `0, 100, 500, 701`

**금지사항:** `RS4` 를 5행이 아닌 개수로 반환하지 않는다. 업무시간 밖이라고 실패시키지 않는다 — `Allowed=0` 으로 반환한다.

- [ ] **Step 1: RED**

| Test ID | 인자 | 기대 RS0 `Code` | 설명 |
|---|---|---:|---|
| `SEL-014` | `-1` | `500` | 미존재 Work 500 |
| `SEL-015` | `@Wc` | `701` | NEX 0행 Work → 701 |
| `SEL-016` | `@Wn` | `0` | 정상 Work 상세 |

각 행은 계약 시나리오 파일 하나와 `tools/expected-contracts.json` 의 한 항목이 된다 — 파일명·키는 위 **계약 시나리오 규약**의 표가 정한다. 시나리오 `.sql` 은 이 Task 가 만들고, 기대값·검증기는 `T22` 가 만든다.

```sql
-- tests/contract/14_예약접수상세_미존재.sql
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_예약접수상세] -1;
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"14_예약접수상세_미존재": { "sp": "USP_HC_SELECT_예약접수상세", "rs0Success": 0, "rs0Code": 500 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"15_예약접수상세_NEX0행701": { "sp": "USP_HC_SELECT_예약접수상세", "rs0Success": 0, "rs0Code": 701 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"16_예약접수상세_정상": { "sp": "USP_HC_SELECT_예약접수상세", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
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
CurrentCount = (SELECT COUNT(*) FROM 예약접수 x
                 WHERE x.ReservationDate = w.ReservationDate
                   AND x.TimeSlotCode    = w.TimeSlotCode
                   AND x.StatusCode IN ('RSV','RCP'))
SeatsLeft    = CASE WHEN 20 - CurrentCount < 0 THEN 0 ELSE 20 - CurrentCount END
```

`RS2` = `ExamSourceCode='NEX'` 인 Detail 을 `검사코드` 와 JOIN, `ExamCode ASC`.
`RS3` = `ExamSourceCode='AEX'` 인 Detail, `OptionCode ASC`.

`RS4` 는 **고정 5행**을 `VALUES` 로 만들고 각 행에 허용조건을 평가한다.

```sql
SELECT
      ActionCode    = CAST(a.Code AS VARCHAR(30))
    , Allowed       = CAST(... AS BIT)
    , ReasonCode    = CAST(... AS INT)
    , ReasonMessage = CAST(... AS NVARCHAR(300))
FROM [dbo].[예약접수] w
CROSS JOIN (VALUES ('EDIT_RESERVATION'),('CANCEL_RESERVATION'),('START_RECEPTION'),
                   ('EDIT_EXTRA'),('CANCEL_RECEPTION')) a(Code)
CROSS APPLY [dbo].[UFN_HC_일정확인](@ServerTime, w.[예약일], w.[시간대코드], 'RECEPTION') s
WHERE w.[업무ID] = @WorkId
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

**완료조건:** SP 배포 exit 0 + 시나리오 3개 단독 실행 exit 0. `RS4` 5행은 `T22` 파서가 검증한다.

---

## Task T21: `[dbo].[USP_HC_SELECT_예약가능정보]`

**목적:** 예약 화면이 필요로 하는 모든 사전정보를 **6개 Result Set** 으로 반환한다. 15개 SP 중 가장 복잡하다.

**관련 Baseline 위치:** `05` §9 전체 (§9.2 signature, §9.3 NULL 조합, §9.4 Scope, §9.5 RS 순서, §9.6~§9.11 각 RS, §9.12 CanSave, §9.13 평가순서).

**선행조건:** `T20` 완료.

**Files:** Modify `deploy/04_Procedures_Select.sql` · Create `tests/contract/17_예약가능정보_RowVersion단독.sql`, `tests/contract/18_예약가능정보_AEX_NULL.sql`, `tests/contract/19_예약가능정보_WALKIN_날짜불일치.sql`, `tests/contract/20_예약가능정보_휴무일.sql`

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

각 행은 계약 시나리오 파일 하나와 `tools/expected-contracts.json` 의 한 항목이 된다 — 파일명·키는 위 **계약 시나리오 규약**의 표가 정한다. 시나리오 `.sql` 은 이 Task 가 만들고, 기대값·검증기는 `T22` 가 만든다.

```sql
-- tests/contract/17_예약가능정보_RowVersion단독.sql
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @Pn, NULL, 0x0000000000000001, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0;
```

```json
// tools/expected-contracts.json 발췌. rs0Code 는 확정값이다. resultSets 는 05 §7~§12 가 정의한 RS 형상을 옮겨 적는다.
"17_예약가능정보_RowVersion단독": { "sp": "USP_HC_SELECT_예약가능정보", "rs0Success": 0, "rs0Code": 102 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"18_예약가능정보_AEX_NULL": { "sp": "USP_HC_SELECT_예약가능정보", "rs0Success": 0, "rs0Code": 100 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"19_예약가능정보_WALKIN_날짜불일치": { "sp": "USP_HC_SELECT_예약가능정보", "rs0Success": 0, "rs0Code": 102 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
"20_예약가능정보_휴무일": { "sp": "USP_HC_SELECT_예약가능정보", "rs0Success": 1, "rs0Code": 0 },   // resultSets 는 T22 Step 2 스키마에 SP 계약대로 채운다
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
CurrentCount  = (SELECT COUNT(*) FROM 예약접수
                  WHERE ReservationDate = @ReservationDate
                    AND TimeSlotCode    = s.TimeSlot          -- 'AM' / 'PM'
                    AND StatusCode IN ('RSV','RCP'))

-- 계산용으로만 현재 Work 를 뺀다
ExcludingSelf = CurrentCount
                - CASE WHEN @WorkId IS NOT NULL
                        AND EXISTS (SELECT 1 FROM 예약접수
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
for f in tests/contract/1[789]_*.sql tests/contract/20_*.sql; do
  RC=0
  sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
         -i "$f" -o "artifacts/logs/rs_$(basename "$f" .sql).txt" || RC=$?
  echo "$f exit=$RC"
done
```

`[X]` 초안 Expected 는 `tests/04_Select_SP_Tests.sql` 에서 `PASS 20건` 을 셌다. 계약 판정은 그 파일이 아니라
`contract/` + `T22` 검증기가 한다. 건수를 다시 적지도 않는다 — §45.2 가 단일 출처다.

Expected: 배포 exit 0, 시나리오 4개 전부 exit 0.

- [ ] **Step 4: 회귀 — `SCH-014` 는 아직 FAIL (SP 7개)**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W \
  -Q "SELECT 'SP=' + CONVERT(varchar(5), COUNT(*)) FROM sys.procedures WHERE name LIKE 'USP[_]HC[_]%';"
```

Expected: `SP=7`

- [ ] **Step 5: Commit** — `feat(phase4): USP_HC_SELECT_예약가능정보 구현 (RS0~RS5)`

**완료조건:** SELECT SP 7개 배포 완료 + 시나리오 4개 단독 실행 exit 0. 스펙 §45.2 의 `SEL` 전건 계약 판정은 `T22` 의 완료조건이다.

---

## Task T22: `tools/verify-contract.js` — 후속 Result Set 계약 검증기

**목적:** T-SQL로는 불가능한 RS1~RS5의 순서·컬럼명·행수를 자동 검증한다 (G09).

**관련 Baseline 위치:** `05` §17.9, 스펙 §36.3.

**선행조건:** `T21` 완료.

**Files:**
- Create: `tools/verify-contract.js`
- Create: `tools/expected-contracts.json`
- Create: `tests/contract/21_예약가능정보_Scope_ALL.sql` ~ `tests/contract/25_예약가능정보_Scope_NONE.sql` (Scope 전용 5개. `01`~`20` 은 `T15`~`T21` 이 이미 만들었다)
- Create: `scripts/verify-contract-all.sh`

**Interfaces:**
- Produces: `node tools/verify-contract.js <출력파일> <SP키>` → exit 0 = 일치, exit 1 = 불일치

**금지사항:** npm 패키지를 설치하지 않는다. node 표준 `fs` 만 사용한다. 검증을 통과시키려고 SP의 Result Set을 바꾸지 않는다.

- [ ] **Step 1: 시나리오 SQL 작성**

각 파일은 SP를 한 번 호출하기만 한다. `01`~`20` 은 `T15`~`T21` 이 이미 만들었으므로 여기서는 Scope 전용 `21`~`25` 만 만든다. 예: `tests/contract/01_공통업무상태.sql`

```sql
SET NOCOUNT ON;
EXEC [dbo].[USP_HC_SELECT_공통업무상태];
GO
```

`SELECT_예약가능정보` 는 Scope별로 5개 파일을 더 만든다 (`ALL` / `SLOT` / `EXTRA` / `SLOT_EXTRA` / `NONE`) — `05` §9.11 Cardinality 전용이라 Test ID 가 없다.

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

// [X] FAIL 을 stdout 으로 낸다. 초안은 console.error 만 써서 증거파일에 구조적으로 PASS 만 남았다.
//     stdout·stderr 양쪽에 쓰면 러너의 >> "$OUT" 2>&1 때문에 같은 FAIL 이 두 번 찍힌다(실측).
//     한 곳으로만 낸다. 판정은 exit code 가 한다.
const say = (m) => console.log(m);
const bad = (m) => console.log(m);

// sqlcmd -u 출력은 UTF-16LE + BOM
const buf = fs.readFileSync(outPath);
const text = buf.slice(0, 2).equals(Buffer.from([0xff, 0xfe]))
  ? buf.slice(2).toString('utf16le')
  : buf.toString('utf8');

const lines = text.split(/\r?\n/);
const isSep = (s) => /^-+( +-+)*\s*$/.test(s) || /^-+(\|-+)*\s*$/.test(s);

// 구분선을 마커로 Result Set 을 분리한다.
// 구분선 바로 위 줄이 헤더, 다음 구분선(또는 EOF)까지가 데이터행.
// RS0 의 첫 데이터행은 따로 보관한다 — 스펙 §36.4 는 형상이 아니라 Code 값 일치를 요구한다.
const sets = [];
for (let i = 0; i < lines.length; i++) {
  if (!isSep(lines[i]) || i === 0) continue;
  const columns = lines[i - 1].split('|').map((s) => s.trim()).filter((s) => s.length);
  let rows = 0, firstRow = null;
  for (let j = i + 1; j < lines.length; j++) {
    if (j + 1 < lines.length && isSep(lines[j + 1])) break;   // 다음 RS 의 헤더
    if (!lines[j].trim()) continue;
    if (/^\(\d+ /.test(lines[j].trim())) continue;             // "(N rows affected)"
    if (rows === 0) firstRow = lines[j].split('|').map((s) => s.trim());
    rows++;
  }
  sets.push({ columns, rows, firstRow });
}

const expected = JSON.parse(fs.readFileSync(`${__dirname}/expected-contracts.json`, 'utf8'))[key];
if (!expected) { bad(`FAIL 기대 계약 없음: ${key}`); process.exit(1); }

let fail = 0;

// (1) RS0 의 Success·Code 값
const rs0 = sets[0];
const pick = (name) => {
  if (!rs0 || !rs0.firstRow) return undefined;
  const k = rs0.columns.indexOf(name);
  return k < 0 ? undefined : rs0.firstRow[k];
};
const obsSuccess = pick('Success'), obsCode = pick('Code');
if (expected.rs0Success !== undefined && Number(obsSuccess) !== expected.rs0Success) {
  bad(`FAIL ${key} RS0.Success 관측 ${obsSuccess} != 기대 ${expected.rs0Success}`); fail++;
}
if (expected.rs0Code !== undefined && Number(obsCode) !== expected.rs0Code) {
  bad(`FAIL ${key} RS0.Code 관측 ${obsCode} != 기대 ${expected.rs0Code}`); fail++;
}

// (2) 허용집합 대조는 tools/allowed-codes.json 이 있을 때만 한다 (T36 Step 5 가 만든다).
//     없으면 조용히 통과시키지 않고 NOT RUN 으로 남긴다 — 미실행은 PASS 가 아니다 (CLAUDE.md §10).
const allowedPath = `${__dirname}/allowed-codes.json`;
if (fs.existsSync(allowedPath)) {
  const allowed = JSON.parse(fs.readFileSync(allowedPath, 'utf8'))[expected.sp];
  if (Array.isArray(allowed) && !allowed.includes(Number(obsCode))) {
    bad(`FAIL ${key} RS0.Code ${obsCode} 가 ${expected.sp} 의 허용집합 밖`); fail++;
  }
} else {
  say(`NOT RUN ${key} 허용집합 대조 — tools/allowed-codes.json 이 아직 없다 (T36)`);
}

// (3) RS 개수·컬럼·행수
if (sets.length !== expected.resultSets.length) {
  bad(`FAIL ${key} Result Set 개수 ${sets.length} != 기대 ${expected.resultSets.length}`);
  fail++;
}
expected.resultSets.forEach((e, i) => {
  const a = sets[i];
  if (!a) { bad(`FAIL ${key} RS${i} 누락`); fail++; return; }
  if (a.columns.join(',') !== e.columns.join(',')) {
    bad(`FAIL ${key} RS${i} 컬럼 불일치\n  실측: ${a.columns.join(',')}\n  기대: ${e.columns.join(',')}`);
    fail++;
  }
  const lo = e.rows !== undefined ? e.rows : e.rowsMin;
  const hi = e.rows !== undefined ? e.rows : e.rowsMax;
  if (a.rows < lo || a.rows > hi) {
    bad(`FAIL ${key} RS${i} 행수 ${a.rows} 가 기대 ${lo}~${hi} 밖`);
    fail++;
  }
});

if (fail === 0) say(`PASS ${key} Result Set 계약 일치 (${sets.length}개 RS · RS0 Code=${obsCode})`);
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
              AND NOT EXISTS (SELECT 1 FROM dbo.휴무일
                               WHERE HolidayDate = CONVERT(DATE, SYSDATETIME()) AND Active = 1)
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

`>> "$OUT" 2>&1` 로 양쪽을 누적하고 플래그로 집계한다. `verify-contract.js` 는 FAIL 을 **stdout 으로만** 낸다 —
Step 3 의 `[X]` 대로다. 양쪽에 쓰면 `2>&1` 때문에 증거파일에 같은 FAIL 이 두 번 찍힌다(실측 확인).

`-W -w 65535` 를 빼지 않는다 — 기본 폭 80에서 줄이 접히면 파서가 무너진다(실측 확인).

```bash
chmod +x scripts/verify-contract-all.sh
./scripts/verify-contract-all.sh
echo "exit=$?"
```

Expected: 전부 `PASS`, `exit=0`. 특히 `21_예약가능정보_Scope_ALL` 은 **RS 6개**, `16_예약접수상세_정상` 은 **RS 5개 + RS4 정확히 5행**이어야 한다.

- [ ] **Step 6: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/tools/ database/tests/contract/ database/artifacts/reports/contract-verify.txt
git commit -m "test(phase4): 후속 Result Set 계약 검증기 도입 (node, 의존성 0)"
```

**회귀시험:** `T36` 에서 15개 SP 전부 재검증.

**로그 경로:** `artifacts/logs/rs_*.txt`, `artifacts/reports/contract-verify.txt`

**Rollback/Cleanup:** 검증기는 읽기 전용이다.

**완료조건:** RED에서 실제 FAIL 관측 + 스펙 §45.2 의 `SEL` 전건이 계약 판정으로 PASS + Scope 5개 시나리오 PASS.

`[X]` 초안은 시나리오를 SP 단위 11개로 셌다. 그렇게 세면 `SEL` 20건 중 9건이 판정되지 않는다 — 위 **계약 시나리오 규약** 표가 단일 출처다.

---

## Task T22b: `[dbo].[USP_HC_SELECT_변경이력]` (`SP-LOG-01`)

**목적:** 대상 행 하나의 변경기록을 최신순으로 낸다. `00` CP-06 이 변경기록을 **열람용**으로 규정한 것을 받는 유일한 조회 SP 다.

**관련 Baseline 위치:** `05` §8.3 (입력·RS·정렬·계약 경계), `05` §13 (허용 Code `0`·`100`·`101`), `04` §8.6.3·§8.6.4, `03` §23 (`DLG-LOG-01`).

`[R3]` **이 Task 는 R3 재봉인으로 `SP-LOG-01` 이 신설된 뒤에 만들어졌다.** 초안 `T15`~`T22` 에는 없었고, `§45.2` 의 `SEL` 대역도 `001`~`020` 이라 8번째 SELECT SP 를 추적하지 못했다. `SEL-021`~`024` 로 대역을 넓히고 `SEL` 건수를 24 로, 카탈로그 합계를 246 으로 고쳤다.

**선행조건:** `T21` 완료. 정상 조회 시나리오는 Write SP 가 남긴 `변경이력` 행을 필요로 한다.

**Files:** Modify `deploy/04_Procedures_Select.sql` · `tests/04_Select_SP_Tests.sql` · `tools/expected-contracts.json`, Create `tests/contract/SEL-021_*.sql` ~ `SEL-024_*.sql`

**Interfaces:**
- Parameter 2개: `@TargetTable NVARCHAR(10)`, `@TargetKey BIGINT` (둘 다 `NOT NULL`)
- Produces: RS0 + RS1 `(LogId BIGINT, RecordedAt DATETIME2(0), OperatorName NVARCHAR(50), ColumnName NVARCHAR(30), BeforeValue NVARCHAR(4000), AfterValue NVARCHAR(4000))`
- 정렬 `RecordedAt DESC, LogId DESC` — `IX_변경이력_TARGET` 의 Key(`대상테이블, 대상키, 기록일시 DESC`)가 술어와 정렬을 그대로 덮는다

**허용 Code:** `0, 100~101`

**금지사항:**

```text
200 PatientNotFound      대상 행의 존재를 확인하지 않는다. 감사 기록은 대상 행보다 오래 산다 (04 §8.6.3)
대상테이블을 RS1 에 실음   호출자가 이미 알고 넘긴 값이다 (05 §8.3)
변경이력 기록             이 SP 는 데이터를 바꾸지 않으므로 남기지 않는다
기간·조작자 전체 검색      계약에 없다. 대상 행 1개 단위 조회만 제공한다
```

- [ ] **Step 1: RED — 계약 시나리오 4건**

| Test ID | 인자 | 기대 RS0 | 설명 |
|---|---|---|---|
| `SEL-021` | `N'수검자'`, 변경이력 최초 `수검자` 대상키 | `Success=1, Code=0` | 정상 조회. RS1 최소 1행 |
| `SEL-022` | `N'예약접수'`, `-1` | `Success=1, Code=0` | 기록 0건 → RS1 **0행**. `200` 이 아니다 |
| `SEL-023` | `N'완료이력'`, `1` | `Code=101` | `CK_변경이력_TARGET_TABLE` 도메인 밖 |
| `SEL-024` | `NULL`, `1` | `Code=100` | 필수값 누락 |

`[!]` **`SEL-021` 의 행수는 `rowsMin`/`rowsMax` 로 잡는다.** 대상키가 Fixture 가 아니라 Write SP 가 만든 값이라 회차마다 다르고, 그 행의 감사 행수도 어떤 Write SP 가 먼저 돌았느냐에 따라 달라진다. 계약이 요구하는 것은 *"정상 조회는 기록을 낸다"* 이므로 `rowsMin: 1` 이 그 계약이다.

- [ ] **Step 2: 구현**

```text
정규화       @TargetTable LTRIM/RTRIM, 빈 문자열 → NULL. 한글 값이라 UPPER 하지 않는다
필수값       @TargetTable NULL → 100 / @TargetKey NULL → 100
값 형식      @TargetTable NOT IN (N'수검자', N'예약접수') → 101
RS0          Code <> 0 이면 RS0 만 내고 RETURN
RS1          대상테이블·대상키로 집고 기록일시 DESC, 이력ID DESC 로 정렬
```

읽기 전용이므로 `XACT_ABORT`·Transaction·`applock` 을 쓰지 않는다 (스펙 §31).

- [ ] **Step 3: GREEN 실행**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i deploy/04_Procedures_Select.sql -o artifacts/logs/04_sel.log
echo "exit=$?"
./scripts/verify-contract-all.sh
```

Expected: `SCH-014` 가 `10/16` 으로 전진하고 `SEL-021`~`024` 가 전건 PASS.

**회귀시험:** `tests/00`~`05` 전체 + `verify-contract-all.sh`

**Rollback/Cleanup:** `CREATE OR ALTER` 이므로 이전 파일 재배포로 되돌아간다.

**완료조건:** `§45.2` 의 `SEL-021`~`024` 가 계약 판정으로 PASS + `tests/04` 의 읽기전용 불변조건이 이 SP 를 포함해 PASS.
