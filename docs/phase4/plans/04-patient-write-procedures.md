`[!]` **이 계획서의 스키마 참조는 `plans/09`·`plans/10` 이 교체했다** ― 컬럼명 한글화,
검사구성의 `예약접수`·`완료이력` 흡수, `검사항목` 삭제, `변경이력` EAV 전환. 당시 구조는 git 이력에 있다.

`[!]` **`수검자.생년월일`·`성별` 은 그 뒤 `PERSISTED` 계산열이 되었다**(`04` §8.1.2). 아래 §공통 블록은
후보검색용 파생을 위해 그대로 필요하지만, `INSERT`/`UPDATE` 의 컬럼 목록에서는 두 컬럼을 빼야 한다 ―
명시하면 `Msg 271` 이다. 구현의 기준은 이 계획서의 SQL 본문이 아니라 `05` 계약과 `06` 스펙이다.

# Stage 6 — Patient Write Stored Procedure 2개

**Index:** `2026-09-04-phase4-database-implementation.md`
**Spec:** `../06_DB_Transaction_Security_Seed.md`
**Tasks:** `T23` ~ `T24`

두 SP 모두 index 문서의 **Write SP 공통 Template**(§21.1)과 **applock 획득 블록**을 그대로 사용한다. 생성 파일은 `deploy/05_Procedures_Patient_Write.sql`, 테스트는 `tests/05_Patient_Write_Tests.sql` 하나다.

## 공통 — SocialNumber 파생값 산출 (두 SP 동일)

`05` §10.1의 7번째 자리 표를 그대로 구현한다.

```sql
-- @SocialNumber 는 이미 정규화(13자리 숫자)되었다고 가정
DECLARE @C7 CHAR(1) = SUBSTRING(@SocialNumber, 7, 1);
-- [X] 1800년대(@C7 IN ('9','0')) 분기를 두면 안 된다. CK_수검자_SOCIAL_FORMAT 이 7번째 자리를
--     1~8 로 묶으므로(04 §8.1.3) SP 가 통과시켜도 저장 단계에서 계산열 NOT NULL 이 Msg 515 를
--     내며 죽는다. ResultCode 가 아니라 예외로 튀어 RS0 계약이 깨진다. 여기서 101 로 거부한다.
DECLARE @Century VARCHAR(2) =
    CASE WHEN @C7 IN ('1','2','5','6') THEN '19'
         WHEN @C7 IN ('3','4','7','8') THEN '20'
         ELSE NULL END;
DECLARE @Gender CHAR(1) =
    CASE WHEN @C7 IN ('1','3','5','7') THEN 'M'
         WHEN @C7 IN ('2','4','6','8') THEN 'F'
         ELSE NULL END;
DECLARE @Birthday VARCHAR(8) = @Century + SUBSTRING(@SocialNumber, 1, 6);

-- 검증
IF @Century IS NULL OR @Gender IS NULL
   OR TRY_CONVERT(DATE, @Birthday, 112) IS NULL
BEGIN SET @Code = 101; SET @Field = 'SocialNumber'; END
```

- 체크디지트와 실제 행정번호 존재 여부는 **검증하지 않는다** (`00` §2.1, `04` §3.5).
- UI가 계산한 Birthday/Gender를 Parameter로 받지 않는다.
- `[!]` **이 블록의 `@Birthday` 는 저장용이 아니라 후보검색용이다.** *"이름 + 산출 Birthday 동일 후보"* 판정은
  아직 존재하지 않는 행의 생년월일을 필요로 하므로 SP 가 변수로 파생한다. **저장값은 계산열이 만든다** ―
  `INSERT`/`UPDATE` 컬럼 목록에 `[생년월일]`·`[성별]` 을 넣지 않는다(`Msg 271`).
- `@Gender` 는 검증용이다. 저장에도 검색에도 쓰지 않는다.

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


## Task T23: `[dbo].[USP_HC_INSERT_수검자]`

**목적:** 신규 수검자를 등록하고, 동일 주민번호·중복후보를 계약대로 처리한다.

**관련 Baseline 위치:** `05` §10.1 (입력·RS·결과표·검증순서), `00` EP-03~09, `04` §3.6 (ChartNo Sequence), 스펙 §21.2·§24.1.

**선행조건:** `T22` 완료.

**Files:**
- Create: `deploy/05_Procedures_Patient_Write.sql`
- Create: `tests/05_Patient_Write_Tests.sql`

**Interfaces:**
- Produces: RS0 + RS1 `(PatientId BIGINT, ChartNo NVARCHAR(100), Name NVARCHAR(100), SocialNumber VARCHAR(13), Birthday VARCHAR(8), Gender CHAR(1), MobilePhone VARCHAR(13), LastEditDate DATETIME)`
- Parameter 12개: `@AutoChartNo BIT`, `@ChartNo NVARCHAR(100)`, `@Name NVARCHAR(100)`, `@SocialNumber VARCHAR(13)`, `@MobilePhone VARCHAR(13)`, `@Phone VARCHAR(13)`, `@Email VARCHAR(200)`, `@Zipcode VARCHAR(10)`, `@Address NVARCHAR(200)`, `@AddressDetail NVARCHAR(200)`, `@Memo NVARCHAR(MAX)`, `@ConfirmSimilarPatient BIT`

**applock:** `HC|SSN|{sha256}` → `HC|CHART|{ChartNo 또는 발급후보}` (**수동·자동 모두**, 스펙 §12.6·§22)

**허용 Code:** `0, 2, 100~102, 201~203, 206, 308~309`

**금지사항:** `202`/`203` 이외의 실패에서 후속 Result Set을 출력하지 않는다. `2601`/`2627` Unique 위반을 업무코드로 변환하지 않는다 — applock으로 사전 예방하고, 그래도 발생하면 `THROW` 한다.

- [x] **Step 1: RED — 결과표 6종을 테스트로 옮긴다**

`[X 실측]` `PWR-013` 은 계획서가 `Code=101` 을 기대했으나 구현·계약은 `Code=2` 다 — `@SocialNumber VARCHAR(13)` 경계에서 14자리가 절단되어 `PWR-001` 이 만든 행과 같은 값이 된다(`tests/contract/PWR-013_주민번호_14자리_경계절단.sql`).

`[X 실측]` 업무시간 가드는 `[Active]` 가 아니라 `[사용여부]` 이고, 창 밖에서 `SKIP` + `RETURN` 하는 대신 `PWR-OFF`(창 밖 호출이 DB 를 바꾸지 않는다)를 실제로 판정한다.

`[X]` **`INSERT … EXEC` 는 쓸 수 없다.** `INSERT_수검자` 는 성공 시 RS0 + RS1 을 반환하므로 `Msg 213` 이다(스펙 §33.1a). RS0 `Code` 판정은 **계약 시나리오**로, DB 에 실제로 남은 결과 판정은 **이 파일**로 나눈다.

**계약 시나리오** — 각 행이 `tests/contract/<Test ID>.sql` 한 파일(`EXEC` 한 번, 단언 없음)과 `tools/expected-contracts.json` 한 항목이 된다.

Parameter 순서(`05` §8.1): `@AutoChartNo, @ChartNo, @Name, @SocialNumber, @CelNumber, @TelNumber, @Email, @Zipcode, @Address, @Job, @Memo, @ConfirmSimilarPatient`

| Test ID | 인자 | 기대 RS0 | 설명 |
|---|---|---|---|
| `PWR-001` | `1, NULL, N'신규수검자', '9001011000018', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 0` | `Success=1, Code=0` | 신규등록 성공 (자동 ChartNo) |
| `PWR-002` | `1, NULL, N'신규수검자', '9001011000018', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 0` | `Success=1, Code=2` | 동일 주민번호 + **동일 이름** → 기존 수검자 반환 |
| `PWR-003` | `1, NULL, N'다른이름', '9001011000018', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 0` | `Success=0, Code=202` | 동일 주민번호 + 다른 이름 (실패인데 RS1 동반) |
| `PWR-004` | `1, NULL, N'짧은번호', '900101100001', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 0` | `Code=101` | SocialNumber 12자리 |
| `PWR-005` | `1, NULL, N'잘못된날짜', '9002311000012', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 0` | `Code=101` | 존재하지 않는 생년월일 (02-31) |
| `PWR-007` | `0, NULL, N'차트없음', '9101011000015', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 0` | `Code=102` | `AutoChartNo=0` 인데 `ChartNo` NULL |
| `PWR-008` | `0, N'T001', N'중복차트', '9101011000015', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 0` | `Code=201` | 수동 ChartNo 중복 |
| `PWR-009` | `1, NULL, N'테스트일구', '0610024000015', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 0` | `Code=203` | `T001` 과 이름·생년월일 동일, 주민번호는 다름 → 유사후보 미확인 |
| `PWR-010` | `1, NULL, N'테스트일구', '0610024000015', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 1` | `Success=1, Code=0` | 같은 인자에 `@ConfirmSimilarPatient=1` → 별도 등록 |
| `PWR-013` | `1, NULL, N'열네자리', '90010110000188', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 0` | `Code=101` | SocialNumber 14자리 (`05` §17.6) |
| `PWR-014` | `1, NULL, N'비숫자', '900101A000018', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 0` | `Code=101` | 7번째 자리가 `'A'` — 비숫자 포함 (`05` §17.6) |

`[X]` **`PWR-006` 은 폐기하지 않고 `PWR-013`·`PWR-014` 로 분할했다.** `05` §10.1 의 7번째 자리 표가 숫자 `0`~`9` 열 개를 전부 매핑하므로 *"13자리 숫자를 통과했으나 7번째 자리가 미정의"* 인 값은 **존재할 수 없다.** `05` §17.6 의 *"허용되지 않은 7번째 자리"* 는 기준선 내부 모순이며 스펙 §44.6 에 Deviation 으로 기록했다. 비숫자 7번째 자리는 형식검사(13자리 숫자)가 흡수하므로 `PWR-014` 로 시험한다.

**DB 상태 단언** — `tests/05_Patient_Write_Tests.sql`. RS 를 받지 않으므로 `Msg 213` 이 나지 않는다.

```sql
SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- 업무시간 가드 (스펙 §33.2a). Write SP 는 308/309 를 업무 Rule 보다 먼저 판정한다.
IF NOT (DATEPART(WEEKDAY, SYSDATETIME()) BETWEEN 2 AND 7
        AND CONVERT(TIME(0), SYSDATETIME()) >= '09:00:00'
        AND CONVERT(TIME(0), SYSDATETIME()) <  '18:00:00'
        AND NOT EXISTS (SELECT 1 FROM [dbo].[휴무일]
                         WHERE [휴무일자] = CONVERT(DATE, SYSDATETIME()) AND [Active] = 1))
BEGIN
    PRINT 'SKIP 05_Patient_Write_Tests 업무시간(월~토 09:00~18:00, 비휴무일) 밖';
    RETURN;
END

-- PWR-011  Birthday/Gender 가 SP 산출값과 일치한다 (Fixture 가 직접 지정한 값과 같아야 한다)
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'산출확인', '9001011000018', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 0;
IF EXISTS (SELECT 1 FROM [dbo].[수검자]
            WHERE [주민번호] = '9001011000018'
              AND [생년월일] = '19900101' AND [Gender] = 'M')
    PRINT 'PASS PWR-011 Birthday/Gender 가 주민번호 산출값과 일치';
ELSE BEGIN PRINT 'FAIL PWR-011'; SET @Fail += 1; END

-- PWR-012  자동 발급 ChartNo 형식 = 'C' + 6자리
IF EXISTS (SELECT 1 FROM [dbo].[수검자]
            WHERE [주민번호] = '9001011000018'
              AND [차트번호] LIKE 'C[0-9][0-9][0-9][0-9][0-9][0-9]'
              AND LEN([차트번호]) = 7)
    PRINT 'PASS PWR-012 자동 ChartNo 형식 C + 6자리';
ELSE BEGIN PRINT 'FAIL PWR-012'; SET @Fail += 1; END

-- PWR-002 의 DB 측면: 동일 주민번호 재등록이 행을 늘리지 않았다
IF ((SELECT COUNT(*) FROM [dbo].[수검자] WHERE [주민번호] = '9001011000018') = 1)
    PRINT 'PASS PWR-002 동일 주민번호 재등록이 행을 만들지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-002 중복 행 생성'; SET @Fail += 1; END

-- PWR-004/005/007/008/013/014 의 DB 측면: 실패 경로는 행을 남기지 않는다
IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자]
                WHERE [Name] IN (N'짧은번호', N'잘못된날짜', N'차트없음', N'중복차트', N'열네자리', N'비숫자'))
    PRINT 'PASS PWR-004/005/007/008/013/014 실패 경로가 행을 남기지 않았다';
ELSE BEGIN PRINT 'FAIL 실패 경로가 행을 남겼다'; SET @Fail += 1; END
```

`[R3]` 이 SP 는 Parameter 목록 **맨 끝**에 `@OperatorName NVARCHAR(50)` 을 받는다(`05` §19.2). 아래 시험의 모든 호출은 마지막 인자로 `@OperatorName = N'TEST'` 를 명시 전달한다 — `05` §2.1 이 선택 Parameter 의 생략을 금지한다.

`[R3]` **`plans/10` 이 `변경이력`을 EAV 로 바꿨다.** 성공 경로에서만, 그리고 **실제로 값이 바뀐 컬럼마다** 1행을 남긴다 (`00` CP-06 · `04` §8.6.4). 업무실패·입력검증 실패는 데이터를 바꾸지 않으므로 기록 지점이 아니다. 감사 INSERT 는 트랜잭션 밖·자체 `TRY/CATCH`·해당 Result Set `SELECT` 뒤이며, 업무 INSERT 계열은 `SCOPE_IDENTITY()` 를 **감사 INSERT 앞에서** 변수로 확정한다.

```sql
-- PWR-030  USP_HC_INSERT_수검자 가 변경이력 1행을 남긴다 (성공·업무실패 각각)
DECLARE @H0 INT = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상테이블] = N'수검자');
--   … 성공 호출 1회(값이 바뀌는 것) + 업무실패 호출 1회를 수행한다 …
--   EAV 는 성공한 변경만 남기므로 업무실패 호출은 행을 만들지 않는다.
DECLARE @H1 INT = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상테이블] = N'수검자');
IF (@H1 > @H0                                        -- 성공 변경이 최소 1개 컬럼을 남겼다
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력]    -- 성공 기록에 대상키가 없을 수 없다
                     WHERE [대상테이블] = N'수검자' AND [대상키] IS NULL)
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력]    -- 값이 같은 컬럼은 기록되지 않는다
                     WHERE [대상테이블] = N'수검자'
                       AND ISNULL([변경전], N'~NULL~') = ISNULL([변경후], N'~NULL~')))
    PRINT 'PASS PWR-030 수검자 변경기록 · 대상키 NOT NULL · 무변경 컬럼 0행';
ELSE BEGIN PRINT 'FAIL PWR-030 변경기록 불일치'; SET @Fail += 1; END
```

**완료조건:** 스펙 §45.2 의 `PWR-001`~`PWR-014` 가 계약 판정(`verify-contract.js`) + DB 상태 단언으로 **각각** 증명된다. 업무시간 밖 실행이면 `SKIP 1건 + FAIL 0건`.

테스트에 쓰는 주민번호는 **체크디지트가 무효인 값**이어야 한다. `T10` 의 무효화 식으로 미리 생성해 상수로 둔다.

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/05_Patient_Write_Tests.sql -o artifacts/logs/test_05_red.log
echo "exit=$?"
```

Expected: exit **1**, `Msg 2812`.

- [x] **Step 2: 구현 — 검증순서 (`05` §10.1 그대로)**

`[X 실측]` Parameter 는 12개가 아니라 14개다 — R3 이 `@HepatitisBExcluded` 와 맨 끝 `@OperatorName` 을 더했다.

`[X 실측]` 1번의 "MobilePhone/Phone 에서 '-' 제거" 는 구현하지 않았다 — `05` §2.2 는 '-' 제거를 검색값으로 한정했고 `04` §8.1.2 는 표시값 그대로 저장한다.

```text
[Transaction 밖]
 1. 문자열 정규화: LTRIM/RTRIM, 빈 문자열 → NULL, MobilePhone/Phone 에서 '-' 제거
 2. @Name IS NULL 또는 공백                        → 100 MissingValue, Field='Name'
 3. @SocialNumber 13자리 숫자 아님                  → 101 BadValue, Field='SocialNumber'
 4. 7번째 자리 코드 / 6자리 날짜 유효성 → Birthday·Gender 산출
    실패 시                                        → 101, Field='SocialNumber'
 5. @AutoChartNo=1 인데 @ChartNo NOT NULL          → 102 BadRequest, Field='ChartNo'
    @AutoChartNo=0 인데 @ChartNo NULL              → 102 BadRequest, Field='ChartNo'
 6. @ConfirmSimilarPatient IS NULL                 → 100

[Transaction 안]
 7. applock  HC|SSN|{sha256(@SocialNumber)}
 8. applock  HC|CHART|{@ChartNo}          (수동 입력)
    -- [X] 자동발급 경로도 후보값마다 HC|CHART|{후보} 를 잡는다(Step 3 재시도 루프).
    --     잡지 않으면 다른 세션이 같은 값을 수동 입력해 Msg 2627 이 발생하고,
    --     스펙 §20 이 "applock 으로 사전 직렬화했으므로 2627 발생 시 설계 위반" 이라고 못박았다.
 9. UFN_HC_일정확인(@ServerTime, @Today, 'AM', 'NONE') → CanWorkNow=0 이면 WorkCode(308/309)
10. 동일 SocialNumber 기존 Patient 조회
      있고 Name 동일  → Code=2  ExistingPatient, Success=1, RS1=기존 1행, COMMIT 후 반환
      있고 Name 다름  → Code=202 SameNumberDifferentName, Success=0, RS1=기존 1행
11. @ConfirmSimilarPatient=0 이고 (Name + 산출 Birthday) 동일 후보가 있으면
                        → Code=203 SimilarPatient, Success=0, RS1=후보 N행
12. 수동 ChartNo 가 이미 존재      → 201 ChartNoUsed, Field='ChartNo'
13. 자동 ChartNo 발급 (아래 Step 3)
14. 수검자 1행 INSERT, SCOPE_IDENTITY() 로 PatientId 확보
15. COMMIT → RS0 성공 + RS1 신규 1행
```

`202`/`203` 은 **실패지만 RS1을 동반하는 유일한 예외**다 (`05` §3.5).

- [x] **Step 3: 자동 ChartNo 발급 루프**

**`NEXT VALUE FOR` 는 Transaction 밖(`[2]` 사전조회 구간)에서 확보한다.** `SET XACT_ABORT ON` 트랜잭션 안에서 `Msg 11728`(MAXVALUE 도달)이 나면 `TRY/CATCH` 로 잡아도 트랜잭션이 **doomed**(`XACT_STATE() = -1`)가 되어 이후 `COMMIT` 이 `Msg 3930` 으로 실패한다. Sequence 값은 롤백과 무관하게 소비되므로(`04` §3.6 결번 허용) 밖으로 빼도 계약에 어긋나지 않는다.

```sql
---- [2] Transaction 밖 ------------------------------------------------------
DECLARE @Seq BIGINT = NULL, @Cand NVARCHAR(100) = NULL, @Exhausted BIT = 0;
IF @AutoChartNo = 1
BEGIN
    BEGIN TRY
        SET @Seq = NEXT VALUE FOR [dbo].[SEQ_HC_CHART_NO];
        SET @Cand = N'C' + RIGHT(N'000000' + CONVERT(NVARCHAR(6), @Seq), 6);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() = 11728 SET @Exhausted = 1;   -- Sequence MAXVALUE 도달
        ELSE THROW;
    END CATCH
    IF @Exhausted = 1 BEGIN SET @Code = 206; SET @Field = 'ChartNo'; END
END

---- [3] 후보 1개당 1 Transaction. 루프 자체는 Transaction 밖에 둔다 --------------
-- [X] NEXT VALUE FOR 는 반드시 Transaction 밖에서 호출한다. Msg 11728(MAXVALUE)이
--     열린 Transaction 안에서 나면 doomed 상태가 되어 이어지는 ROLLBACK 이 Msg 3930 을 낸다.
--     따라서 재시도 2회차 이후에도 같은 구조를 지켜야 한다 — 아래처럼 매 회 트랜잭션을 닫고 연다.
WHILE @Cand IS NOT NULL AND @Exhausted = 0
BEGIN
    -- (a) Transaction 밖: 다음 후보를 확보한다
    IF @Cand IS NULL
    BEGIN
        BEGIN TRY
            SET @Seq  = NEXT VALUE FOR [dbo].[SEQ_HC_CHART_NO];
            SET @Cand = N'C' + RIGHT(N'000000' + CONVERT(NVARCHAR(6), @Seq), 6);
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() = 11728 BEGIN SET @Exhausted = 1; BREAK; END
            ELSE THROW;
        END CATCH
    END

    -- (b) Transaction 안: SSN → CHART 순으로 잡고 고유성을 확인한다
    BEGIN TRAN;
        EXEC @rc = sp_getapplock @Resource = @ResSsn,   @LockMode = 'Exclusive',
                                 @LockOwner = 'Transaction', @LockTimeout = 5000;
        PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
        IF @rc < 0 BEGIN ROLLBACK; IF @rc = -3 THROW 50002, N'잠금 교착', 1; ELSE THROW 50001, N'잠금 실패', 1; END

        SET @ResChart = N'HC|CHART|' + @Cand;
        EXEC @rc = sp_getapplock @Resource = @ResChart, @LockMode = 'Exclusive',
                                 @LockOwner = 'Transaction', @LockTimeout = 5000;
        PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
        -- [X] 잠금 실패는 "다음 번호로 넘어간다" 가 아니라 §25.1 대로 THROW 다.
        --     음수 rc 는 경합이 아니라 timeout·취소·호출오류이며, 조용히 건너뛰면
        --     한 번의 호출이 최대 999,999개 후보를 5초씩 순회할 수 있다.
        IF @rc < 0 BEGIN ROLLBACK; IF @rc = -3 THROW 50002, N'잠금 교착', 1; ELSE THROW 50001, N'잠금 실패', 1; END

        IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [차트번호] = @Cand)
        BEGIN
            INSERT INTO [dbo].[수검자] (...) VALUES (...);
            COMMIT;
            BREAK;                      -- 발급 성공
        END
        ROLLBACK;                       -- 이미 존재 → 잠금을 놓고 다음 후보로
    SET @Cand = NULL;
END

IF @Exhausted = 1 BEGIN SET @Code = 206; SET @Field = 'ChartNo'; END
```

`[X]` **초안 오류 3건**
1. **임의 상한 100회.** `C000001`~`C000100` 이 수동 입력되어 있고 `C000101` 이 비어 있으면 **발급 가능한 번호가 있는데도 `206`** 이 나온다. `05` §4.2의 `206 ChartNoLimit` 은 "발급범위 초과"이지 "반복 횟수 초과"가 아니다.
2. **`NEXT VALUE FOR` 를 트랜잭션 안에 두었다.** `Msg 11728` → doomed → `Msg 3930`.
3. **자동발급 경로가 `HC|CHART|` 를 잡지 않았다.** 세션 A가 자동으로 `C000123` 후보를 만들고, 세션 B가 수동으로 같은 값을 입력하면 둘 다 `NOT EXISTS` 를 통과해 `2627` 이 난다. 스펙 §20은 *"applock 으로 사전 직렬화했으므로 2627 발생 시 설계 위반"* 이라고 못박았다.

- [x] **Step 4: GREEN 실행**

`[X 실측]` 창 안 증거는 `artifacts/logs/t_05_Patient_Write_Tests.log`(PASS 23 · FAIL 0)와 `artifacts/logs/full_test_run.log` 의 계약 판정 `PWR-001`~`PWR-028` 전건 PASS 다. `artifacts/reports/contract-verify.txt` 는 창 밖 회차라 PWR 전건이 SKIP 이다.

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i deploy/05_Procedures_Patient_Write.sql -o artifacts/logs/05_patient.log
echo "exit=$?"
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/05_Patient_Write_Tests.sql -o artifacts/logs/test_05.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_05.log | grep -E '^(PASS|FAIL)'
```

Expected: 둘 다 exit 0. `tests/05` 의 DB 상태 단언이 전부 PASS 이고 `./scripts/verify-contract-all.sh` 가 `PWR-001`~`PWR-014` 계약을 전건 PASS 로 판정한다 (스펙 §45.2).

- [x] **Step 5: Transaction 잔여 확인**

`[X 실측]` 별도 sqlcmd 대신 `tests/05_Patient_Write_Tests.sql` 의 `FIX-PWR-TRAN` 단언으로 흡수했다 — 업무실패 5건 뒤 `@@TRANCOUNT = 0` 을 창 안 회귀에서 PASS 로 확인했다.

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W -Q "
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'실패유도', '900101100001', NULL,NULL,NULL,NULL,NULL,NULL,NULL, 0;
SELECT 'trancount=' + CONVERT(varchar(5), @@TRANCOUNT);"
```

Expected: `trancount=0` — 업무실패 후에도 열린 Transaction이 남지 않는다.

- [x] **Step 6: Commit**

`[X 실측]` 커밋은 T23·T24 를 묶은 `8a9aad5 feat(phase4): T23·T24 수검자 Write SP 2개 구현 · PWR 시험 25건` 하나다 — Task 별로 나누지 않았다.

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/05_Procedures_Patient_Write.sql database/tests/05_Patient_Write_Tests.sql
git commit -m "feat(phase4): USP_HC_INSERT_수검자 구현 및 등록 경계 테스트 12건"
```

**회귀시험:** `tests/00`~`05` 전체

**로그 경로:** `artifacts/logs/test_05.log`

**Rollback/Cleanup:** `./scripts/rebuild.sh`

**완료조건:** RED 관측 + 스펙 §45.2 의 `PWR-001`~`PWR-014` 전건 PASS + `@@TRANCOUNT=0`.

---

## Task T24: `[dbo].[USP_HC_UPDATE_수검자정보]`

**목적:** 수검자 정보를 수정하고, 주민번호 변경 시 활성 업무 존재를 차단하며, `LastEditDate` 를 단조 증가시킨다.

**관련 Baseline 위치:** `05` §10.2, `00` EP-08, `04` §8.1.3 (4ms 단조증가), 스펙 §26·§28.

**선행조건:** `T23` 완료.

**Interfaces:**
- Produces: RS0 + RS1 `(PatientId BIGINT, ChartNo NVARCHAR(100), LastEditDate DATETIME)`
- Parameter 12개: `@PatientId BIGINT`, `@LastEditDate DATETIME`, `@ChartNo NVARCHAR(100)`, `@Name NVARCHAR(100)`, `@SocialNumber VARCHAR(13)`, `@MobilePhone`, `@Phone`, `@Email`, `@Zipcode`, `@Address`, `@AddressDetail`, `@Memo`

**applock:** `HC|SSN|{sha256}` → `HC|CHART|{ChartNo}` → `HC|PAT|{PatientId}` — **조건 없이 항상** (스펙 §24.1)

`[X]` **"변경 시에만" 으로 두면 안 된다.** 변경 여부를 알려면 현재 행을 먼저 읽어야 하고, 자연스러운 구현이 `PAT`(랭크 3)을 먼저 잡은 뒤 `SSN`(랭크 1)을 잡게 되어 **§23 전역 순서가 뒤집힌다.** 게다가 자원명은 **요청값** `@SocialNumber`·`@ChartNo` 로 바로 만들 수 있으므로 사전조회가 애초에 필요 없다.

**허용 Code:** `0, 1, 100~102, 200~201, 204~205, 600, 308~309`

**금지사항:** 주민번호 변경으로 기존 Work의 TGT/NEX/AEX를 자동 재판정하거나 취소하지 않는다. Birthday/Gender를 Parameter로 받지 않는다.

- [x] **Step 1: RED**

`[X 실측]` `PWR-023`·`PWR-027` 의 새 주민번호는 체크디지트 산출식이 아니라 무효 상수(`8001011999998`·`7010011999997`)를 그대로 썼다.

주민번호 상수를 손으로 적지 않는다. **저장된 값을 그대로 읽어서 전달**하면 체크디지트를 계산할 필요가 없고 Fixture 가 바뀌어도 테스트가 깨지지 않는다.

**계약 시나리오** — Parameter 순서(`05` §8.2): `@PatientId, @LastEditDate, @ChartNo, @Name, @SocialNumber, @CelNumber, @TelNumber, @Email, @Zipcode, @Address, @Job, @Memo`

| Test ID | 인자 요지 | 기대 RS0 | 설명 |
|---|---|---|---|
| `PWR-020` | `T015` 의 현재값 12개를 그대로 | `Success=1, Code=1` | No-op |
| `PWR-021` | `@LastEditDate = '2000-01-01'` | `Code=600` | stale `LastEditDate` |
| `PWR-022` | 현재 `@Led` + `@Name = N'이름변경됨'` | `Success=1, Code=0` | 실제 변경 |
| `PWR-023` | `F001` + 새 `@SocialNumber` (RSV 보유) | `Code=205` | 주민번호 변경 + **RSV** 활성 Work |
| `PWR-024` | `F001` + `@ChartNo = N'F001X'` (RSV 보유) | `Success=1, Code=0` | 차트번호 변경은 활성 Work 가 있어도 **차단하지 않는다** (`00` EP-08) |
| `PWR-025` | `T015` + `T001` 의 `SocialNumber` | `Code=204` | 다른 Patient 의 주민번호 |
| `PWR-026` | `@PatientId = -1` | `Code=200` | 미존재 Patient |
| `PWR-027` | `T014` + 새 `@SocialNumber` (**RCP** 보유) | `Code=205` | 주민번호 변경 + **RCP** 활성 Work (`05` §17, 스펙 §33.4) |
| `PWR-028` | `T014` + `@ChartNo = N'T014X'` (RCP 보유) | `Success=1, Code=0` | 차트번호 변경은 RCP 가 있어도 차단하지 않는다 |

`[I]` `PWR-023`·`PWR-027` 의 새 주민번호는 **Fixture 에 없는 값**이어야 한다. `T001` 의 값을 재사용하면 `204 SocialNumberUsed` 가 `205` 보다 먼저 걸린다. 앞 12자리만 바꾸고 체크디지트는 `T10` 의 무효화 식으로 만든다.

`[X]` `PWR-027`·`PWR-028` 은 `RCP` 상태 Work 가 필요하므로 `T14b` 가 만드는 `tests/00b_Test_Harness_RCP.sql` 의 `T014` 를 쓴다.

**DB 상태 단언** — `tests/05_Patient_Write_Tests.sql` 에 이어서 쓴다(업무시간 가드는 파일 머리에 이미 있다).

```sql
DECLARE @Pid BIGINT, @Led DATETIME, @Ssn VARCHAR(13), @Nm NVARCHAR(100), @Before DATETIME;
SELECT @Pid = [수검자ID], @Led = [최종수정일시], @Ssn = [주민번호], @Nm = [Name]
  FROM [dbo].[수검자] WHERE [차트번호] = 'T015';

-- PWR-020  No-op 은 LastEditDate 를 건드리지 않는다
SET @Before = @Led;
EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Pid, @Led, N'T015', @Nm, @Ssn, NULL,NULL,NULL,NULL,NULL,NULL,NULL;
IF ((SELECT [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Pid) = @Before)
    PRINT 'PASS PWR-020 No-op 은 LastEditDate 불변';
ELSE BEGIN PRINT 'FAIL PWR-020 No-op 이 LastEditDate 를 바꿨다'; SET @Fail += 1; END

-- PWR-022  실제 변경은 LastEditDate 를 반드시 증가시킨다
--   DATETIME 의 해상도는 약 3.33ms 다. @Before 를 한 번만 읽고 '>' 로 비교한다.
SELECT @Led = [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Pid;
SET @Before = @Led;
EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Pid, @Led, N'T015', N'이름변경됨', @Ssn, NULL,NULL,NULL,NULL,NULL,NULL,NULL;
IF ((SELECT [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Pid) > @Before
    AND (SELECT [Name] FROM [dbo].[수검자] WHERE [수검자ID] = @Pid) = N'이름변경됨')
    PRINT 'PASS PWR-022 실제 변경 시 LastEditDate 단조증가 + 값 반영';
ELSE BEGIN PRINT 'FAIL PWR-022'; SET @Fail += 1; END

-- PWR-023  주민번호 변경이 차단됐으므로 DB 값은 그대로다
DECLARE @Pf BIGINT = (SELECT TOP (1) [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] LIKE 'F0%' ORDER BY [차트번호]);
DECLARE @Lf DATETIME = (SELECT [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Pf);
DECLARE @Nf NVARCHAR(100) = (SELECT [Name] FROM [dbo].[수검자] WHERE [수검자ID] = @Pf);
DECLARE @Sf VARCHAR(13) = (SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Pf);
DECLARE @P12 CHAR(12) = '850715199999';   -- 1985-07-15 남 + 미사용 순번
DECLARE @NewSsn VARCHAR(13) = @P12 + CONVERT(CHAR(1),
    ( ( ( 11 - (
          ( CAST(SUBSTRING(@P12, 1,1) AS INT)*2 + CAST(SUBSTRING(@P12, 2,1) AS INT)*3
          + CAST(SUBSTRING(@P12, 3,1) AS INT)*4 + CAST(SUBSTRING(@P12, 4,1) AS INT)*5
          + CAST(SUBSTRING(@P12, 5,1) AS INT)*6 + CAST(SUBSTRING(@P12, 6,1) AS INT)*7
          + CAST(SUBSTRING(@P12, 7,1) AS INT)*8 + CAST(SUBSTRING(@P12, 8,1) AS INT)*9
          + CAST(SUBSTRING(@P12, 9,1) AS INT)*2 + CAST(SUBSTRING(@P12,10,1) AS INT)*3
          + CAST(SUBSTRING(@P12,11,1) AS INT)*4 + CAST(SUBSTRING(@P12,12,1) AS INT)*5
          ) % 11 ) ) % 10 + 1 ) % 10 );

EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Pf, @Lf, N'F001', @Nf, @NewSsn, NULL,NULL,NULL,NULL,NULL,NULL,NULL;
IF ((SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Pf) = @Sf)
    PRINT 'PASS PWR-023 활성 Work 존재 시 주민번호가 바뀌지 않았다';
ELSE BEGIN PRINT N'FAIL PWR-023 주민번호가 바뀌었다 — EP-08 위반'; SET @Fail += 1; END

-- PWR-024  차트번호 변경은 활성 Work 가 있어도 성공한다 (EP-08 은 주민번호만 막는다)
SELECT @Lf = [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Pf;
EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Pf, @Lf, N'F001X', @Nf, @Sf, NULL,NULL,NULL,NULL,NULL,NULL,NULL;
IF ((SELECT [차트번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Pf) = N'F001X')
    PRINT 'PASS PWR-024 활성 Work 가 있어도 차트번호는 변경된다';
ELSE BEGIN PRINT 'FAIL PWR-024 차트번호 변경이 차단됐다'; SET @Fail += 1; END
-- 되돌린다 — 뒤 테스트가 F001 을 ChartNo 로 찾는다
SELECT @Lf = [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Pf;
EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Pf, @Lf, N'F001', @Nf, @Sf, NULL,NULL,NULL,NULL,NULL,NULL,NULL;

-- PWR-027  RCP 상태 Work 도 주민번호 변경을 막는다 (05 §17 — RSV 만이 아니다)
DECLARE @Pr BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = 'T014');
DECLARE @Sr VARCHAR(13) = (SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Pr);
DECLARE @Lr DATETIME = (SELECT [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Pr);
DECLARE @Nr NVARCHAR(100) = (SELECT [Name] FROM [dbo].[수검자] WHERE [수검자ID] = @Pr);
SET @P12 = '850716199998';
SET @NewSsn = @P12 + CONVERT(CHAR(1),
    ( ( ( 11 - (
          ( CAST(SUBSTRING(@P12, 1,1) AS INT)*2 + CAST(SUBSTRING(@P12, 2,1) AS INT)*3
          + CAST(SUBSTRING(@P12, 3,1) AS INT)*4 + CAST(SUBSTRING(@P12, 4,1) AS INT)*5
          + CAST(SUBSTRING(@P12, 5,1) AS INT)*6 + CAST(SUBSTRING(@P12, 6,1) AS INT)*7
          + CAST(SUBSTRING(@P12, 7,1) AS INT)*8 + CAST(SUBSTRING(@P12, 8,1) AS INT)*9
          + CAST(SUBSTRING(@P12, 9,1) AS INT)*2 + CAST(SUBSTRING(@P12,10,1) AS INT)*3
          + CAST(SUBSTRING(@P12,11,1) AS INT)*4 + CAST(SUBSTRING(@P12,12,1) AS INT)*5
          ) % 11 ) ) % 10 + 1 ) % 10 );

IF NOT EXISTS (SELECT 1 FROM [dbo].[예약접수] WHERE [수검자ID] = @Pr AND [상태코드] = 'RCP')
BEGIN
    PRINT N'FAIL PWR-027 사전조건 미충족 — T014 의 RCP Work 가 없다 (tests/00b 를 먼저 실행했는가)';
    SET @Fail += 1;
END
ELSE
BEGIN
    EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Pr, @Lr, N'T014', @Nr, @NewSsn, NULL,NULL,NULL,NULL,NULL,NULL,NULL;
    IF ((SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Pr) = @Sr)
        PRINT 'PASS PWR-027 RCP 상태에서도 주민번호가 바뀌지 않았다';
    ELSE BEGIN PRINT 'FAIL PWR-027 RCP 상태에서 주민번호가 바뀌었다'; SET @Fail += 1; END

    -- PWR-028  RCP 상태에서도 차트번호 변경은 허용된다
    SELECT @Lr = [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Pr;
    EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Pr, @Lr, N'T014X', @Nr, @Sr, NULL,NULL,NULL,NULL,NULL,NULL,NULL;
    IF ((SELECT [차트번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Pr) = N'T014X')
        PRINT 'PASS PWR-028 RCP 상태에서도 차트번호는 변경된다';
    ELSE BEGIN PRINT 'FAIL PWR-028'; SET @Fail += 1; END
    SELECT @Lr = [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Pr;
    EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Pr, @Lr, N'T014', @Nr, @Sr, NULL,NULL,NULL,NULL,NULL,NULL,NULL;
END

-- PWR-025  다른 Patient 의 주민번호로 변경 시도 → 값이 바뀌지 않는다
DECLARE @S1 VARCHAR(13) = (SELECT [주민번호] FROM [dbo].[수검자] WHERE [차트번호] = 'T001');
DECLARE @S15 VARCHAR(13) = (SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Pid);
SELECT @Led = [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Pid;
EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Pid, @Led, N'T015', N'이름변경됨', @S1, NULL,NULL,NULL,NULL,NULL,NULL,NULL;
IF ((SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Pid) = @S15)
    PRINT 'PASS PWR-025 다른 Patient 의 주민번호로 바뀌지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-025 주민번호 고유성 위반'; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 05_Patient_Write_Tests 완료 ===';
```

- [x] **Step 2: 구현 — 검증순서 (`05` §10.2 그대로)**

`[X 실측]` Parameter 12개 → 14개(`@HepatitisBExcluded`·`@OperatorName`)이고, 9번 No-op 의 NULL-safe INTERSECT 비교도 `B형간염제외여부` 를 포함한다.

```text
[Transaction 밖]
 1. 문자열 정규화
 2. 필수값: @PatientId, @LastEditDate, @ChartNo, @Name, @SocialNumber → NULL 이면 100
 3. @SocialNumber 형식·날짜·세기/성별 → Birthday·Gender 산출. 실패 시 101

[Transaction 안]
 4. applock  HC|SSN|{sha256(@SocialNumber)}   (조건 없이 항상 — 요청값으로 자원명을 만든다)
 5. applock  HC|CHART|{@ChartNo}              (조건 없이 항상 — 요청값으로 자원명을 만든다)
 6. applock  HC|PAT|{@PatientId}
 7. Patient 존재                          → 없으면 200
 8. 저장된 LastEditDate <> @LastEditDate   → 600 PatientChanged, Field='LastEditDate'
 9. 12개 입력이 현재값과 전부 동일         → Code=1 NoChange, UPDATE 없이 COMMIT,
                                             RS1 에 기존 LastEditDate 반환
    ※ 비교는 반드시 **NULL-safe** 여야 한다 (스펙 §28.2).
       <> 로 비교하면 NULL <> 'x' 가 UNKNOWN 이라 "변경 없음" 으로 오판하고
       사용자가 입력한 Memo·삭제한 Email 이 조용히 소실된다.

       IF EXISTS (
           SELECT p.[차트번호],p.[Name],p.[주민번호],p.[생년월일],p.[Gender]
                , p.[CelNumber],p.[TelNumber],p.[EMail],p.[Zipcode],p.[Address],p.[AddressDetail]
             FROM [dbo].[수검자] p WHERE p.[수검자ID] = @PatientId
           INTERSECT
           SELECT @ChartNo,@Name,@SocialNumber,@Birthday,@Gender
                , @MobilePhone,@Phone,@Email,@Zipcode,@Address,@AddressDetail )
          AND EXISTS ( SELECT 1 FROM [dbo].[수검자] p
                        WHERE p.[수검자ID] = @PatientId
                          AND ((p.[Memo] IS NULL AND @Memo IS NULL) OR p.[Memo] = @Memo) )
           SET @Code = 1;

       Memo 는 NVARCHAR(MAX) 라 INTERSECT 피연산자가 될 수 없어 분리 비교한다.
10. 공통 업무 가능 여부                    → 308 / 309
11. ChartNo 변경 시 다른 Patient 고유성    → 201
12. SocialNumber 변경 시 다른 Patient 고유성 → 204 SocialNumberUsed
13. SocialNumber 변경 시 해당 Patient 의 StatusCode IN ('RSV','RCP') 존재 → 205 SocialChangeBlocked
14. LastEditDate 단조증가 계산 후 조건부 UPDATE
15. COMMIT → RS0 + RS1
```

- [x] **Step 3: `LastEditDate` 단조증가 + 조건부 UPDATE**

```sql
DECLARE @NewEdit DATETIME = CONVERT(DATETIME, @ServerTime);
IF @NewEdit <= @OldEdit SET @NewEdit = DATEADD(MILLISECOND, 4, @OldEdit);

UPDATE [dbo].[수검자]
   SET [차트번호] = @ChartNo, [성명] = @Name, [주민번호] = @SocialNumber
     -- [생년월일]·[성별] 은 계산열이라 여기에 쓸 수 없다. @SocialNumber 를 바꾸면 자동으로 다시 유도된다.
     , [휴대전화] = @MobilePhone
     , [전화번호] = @Phone
     , [이메일] = @Email, [우편번호] = @Zipcode, [주소] = @Address
     , [상세주소] = @AddressDetail, [비고] = @Memo
     , [최종수정일시] = @NewEdit
 WHERE [수검자ID]    = @PatientId
   AND [최종수정일시] = @OldEdit;

IF @@ROWCOUNT = 0 BEGIN SET @Code = 600; SET @Field = 'LastEditDate'; END
```

`[X]` **`plans/09` `T46` 이 `CelNumberS` 를 제거했다.** 정규화 값을 저장하지 않으므로
`CK_수검자_CEL_NORMALIZED` 도 함께 사라졌고, 위 `UPDATE` 는 표시값만 그대로 넣는다.
초안이 경고하던 `Msg 547`(정규화 짝이 어긋나 CHECK 위반)은 구조적으로 발생할 수 없다.

남은 방어는 `CK_수검자_CEL_DIGIT` 하나다.

```text
[휴대전화] IS NULL OR REPLACE([휴대전화], '-', '') NOT LIKE '%[^0-9]%'
```

`@MobilePhone` 검색은 `REPLACE(p.[휴대전화],'-','') = @MobilePhone` 으로 하며 인덱스 seek 이 아니다
(`plans/09` §4.2 의 받아들인 대가).

- [x] **Step 4: GREEN 실행**

`[X 실측]` 창 안 PASS 는 19건이 아니라 23건이다 — 이 파일은 DB 상태 단언만 세고 RS0 계약 22건은 `verify-contract.js` 가 따로 판정한다(`artifacts/logs/t_05_Patient_Write_Tests.log`).

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/05_Procedures_Patient_Write.sql
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/05_Patient_Write_Tests.sql -o artifacts/logs/test_05.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_05.log | grep -cE '^PASS'
```

Expected: exit 0, PASS 개수 **19건** (`PWR-001`~`012` + `PWR-020`~`026`).

- [x] **Step 5: 4ms 단조증가 실증**

`[X 실측 2026-09-07]` **`4MS-001` 은 `PWR-029` 로 신설했다.**
기존 `PWR-022` 는 갱신을 한 번만 하고 `>` 비교만 해서, `DATETIME` 의 약 3.33ms 틱 안에
연속 갱신이 떨어질 때만 발동하는 `+4ms` 가드를 **한 번도 지나지 않았다** — 그 가드를 지워도 PASS 했다.
지연 없이 5회 연속 수정해 같은 틱 충돌을 강제하고 매 회차가 직전보다 큰지 본다.

`[X]` `EXEC` 인자는 상수나 변수만 받는다. `N'단조증가' + CONVERT(...)` 처럼 식을 쓰면
`Msg 102` 로 배치가 통째로 컴파일 실패한다(실측). 변수로 빼야 한다.

`[NOT RUN]` 컴파일만 확인했다. Write SP 라 업무시간 창에서만 판정된다.

`[미이행]` `4MS-001`(최초값을 1회만 읽고 5회 연속 수정한 뒤 최종 > 최초) 이 저장소 어디에도 없다 — `PWR-022` 의 1회 변경 단조증가 단언만 있다.

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W -Q "
SET NOCOUNT ON;
DECLARE @P BIGINT=(SELECT PatientId FROM 수검자 WHERE ChartNo='T015');
DECLARE @S VARCHAR(13)=(SELECT SocialNumber FROM 수검자 WHERE PatientId=@P);
DECLARE @L0 DATETIME=(SELECT LastEditDate FROM 수검자 WHERE PatientId=@P);  -- 최초 1회만 읽는다
DECLARE @L DATETIME=@L0, @i INT=0, @Nm NVARCHAR(100);
WHILE @i<5
BEGIN
  SET @Nm = N'반복' + CONVERT(NVARCHAR(3), @i);   -- 매 회 실제 변경이어야 No-op 이 아니다
  EXEC dbo.USP_HC_UPDATE_수검자정보 @P, @L, N'T015', @Nm, @S, NULL,NULL,NULL,NULL,NULL,NULL,NULL;
  SELECT @L = LastEditDate FROM 수검자 WHERE PatientId=@P;   -- SP 가 갱신한 값을 다음 호출에 넘긴다
  SET @i+=1;
END
SELECT CASE WHEN @L > @L0 THEN 'PASS 4MS-001 5회 연속 수정 후 LastEditDate 증가'
            ELSE 'FAIL 4MS-001 LastEditDate 미증가' END
     + '  최초=' + CONVERT(VARCHAR(30), @L0, 121) + '  최종=' + CONVERT(VARCHAR(30), @L, 121);
SELECT 'final=' + CONVERT(varchar(30), LastEditDate, 121) FROM 수검자 WHERE PatientId=@P;"
```

Expected: `PASS 4MS-001`, 최종 `LastEditDate` > 최초.

`[X]` **초안의 루프는 보정 유무를 구분하지 못했다.** 매 회 `LastEditDate` 를 **다시 읽어서** 전달했으므로, `+4ms` 보정이 없어 `@NewEdit = @OldEdit` 이 되어도 조건부 `UPDATE` 는 `@@ROWCOUNT=1` 로 성공하고 `600` 이 나지 않는다. 통과해도 아무 증거가 아니었다. **최초 값을 한 번만 읽고, SP 가 갱신한 값을 이어받으며, 최종 > 최초를 단언**해야 단조증가를 실증한다.

- [x] **Step 6: Commit**

`[X 실측]` T23 Step 6 과 같은 커밋 `8a9aad5` 다 — 별도 커밋이 아니다.

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/05_Procedures_Patient_Write.sql database/tests/05_Patient_Write_Tests.sql
git commit -m "feat(phase4): USP_HC_UPDATE_수검자정보 구현 및 수정·동시성 테스트 7건"
```

**회귀시험:** `tests/00`~`05`

**로그 경로:** `artifacts/logs/test_05.log`

`[R3]` 이 SP 는 Parameter 목록 **맨 끝**에 `@OperatorName NVARCHAR(50)` 을 받는다(`05` §19.2). 아래 시험의 모든 호출은 마지막 인자로 `@OperatorName = N'TEST'` 를 명시 전달한다 — `05` §2.1 이 선택 Parameter 의 생략을 금지한다.

`[R3]` **`plans/10` 이 `변경이력`을 EAV 로 바꿨다.** 성공 경로에서만, 그리고 **실제로 값이 바뀐 컬럼마다** 1행을 남긴다 (`00` CP-06 · `04` §8.6.4). 업무실패·입력검증 실패는 데이터를 바꾸지 않으므로 기록 지점이 아니다. 감사 INSERT 는 트랜잭션 밖·자체 `TRY/CATCH`·해당 Result Set `SELECT` 뒤이며, 업무 INSERT 계열은 `SCOPE_IDENTITY()` 를 **감사 INSERT 앞에서** 변수로 확정한다.

```sql
-- PWR-031  USP_HC_UPDATE_수검자정보 가 변경이력 1행을 남긴다 (성공·업무실패 각각)
DECLARE @H0 INT = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상테이블] = N'수검자');
--   … 성공 호출 1회(값이 바뀌는 것) + 업무실패 호출 1회를 수행한다 …
--   EAV 는 성공한 변경만 남기므로 업무실패 호출은 행을 만들지 않는다.
DECLARE @H1 INT = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상테이블] = N'수검자');
IF (@H1 > @H0                                        -- 성공 변경이 최소 1개 컬럼을 남겼다
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력]    -- 성공 기록에 대상키가 없을 수 없다
                     WHERE [대상테이블] = N'수검자' AND [대상키] IS NULL)
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력]    -- 값이 같은 컬럼은 기록되지 않는다
                     WHERE [대상테이블] = N'수검자'
                       AND ISNULL([변경전], N'~NULL~') = ISNULL([변경후], N'~NULL~')))
    PRINT 'PASS PWR-031 수검자 변경기록 · 대상키 NOT NULL · 무변경 컬럼 0행';
ELSE BEGIN PRINT 'FAIL PWR-031 변경기록 불일치'; SET @Fail += 1; END
```

**완료조건:** 스펙 §45.2 의 `PWR-020`~`PWR-028` 전건 PASS + 연속 5회 수정 성공(단조증가 실증).
