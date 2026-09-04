# Stage 3~4 — Master Seed · Test Fixture · Rule TVF 4개

**Index:** `2026-09-04-phase4-database-implementation.md`
**Spec:** `../06_DB_Transaction_Security_Seed_CANDIDATE.md`
**Tasks:** `T08` ~ `T14`, `T14b`

---

## Task T08: `deploy/02_Seed.sql` — Exam 19행 · Holiday 2행

**목적:** `04` §4.6의 통합 검사 Master 19행과 `00` HOL-05가 요구하는 휴무일 2건을 배포한다 (G07).

**관련 Baseline 위치:** `04` §4.6, `00` §7.3.1·HOL-05, 스펙 §13·§14.

**선행조건:** `T07` 완료.

**Files:**
- Create: `deploy/02_Seed.sql`

**Interfaces:**
- Produces: `MST_EXAM_ITEMS` 19행, `MST_HOLIDAYS` 2행

**금지사항:** `MERGE`·존재검사 가드를 쓰지 않는다(clean-create라 테이블이 비어 있다). 검사 코드·이름·역할을 바꾸지 않는다. 휴무일을 상대날짜로 계산하지 않는다.

- [ ] **Step 1: `deploy/02_Seed.sql` 작성 (UTF-8 with BOM)**

**선행 `DELETE` 를 넣지 않는다.** clean-create(`01_Schema.sql` 이 `DROP`→`CREATE`)이므로 테이블이 항상 비어 있어 효과가 없고, Fixture 배치 후 이 파일만 단독 재실행하면 `INFO_CHECKUP_WORK_EXAMS`·`INFO_PATIENT_EXAM_EXCLUSIONS` 의 FK 때문에 **`Msg 547`** 로 실패한다. 멱등성은 **"clean-create 직후 한정"** 이다.

```sql
SET NOCOUNT ON;
PRINT '--- 02_Seed 시작 ---';
GO
-- clean-create 전제. 비어 있지 않으면 배포 순서가 잘못된 것이므로 즉시 중단한다.
IF EXISTS (SELECT 1 FROM [dbo].[MST_EXAM_ITEMS]) OR EXISTS (SELECT 1 FROM [dbo].[MST_HOLIDAYS])
    THROW 51002, N'02_Seed: Master 테이블이 비어 있지 않습니다. 01_Schema 를 먼저 실행하십시오.', 1;
GO
INSERT INTO [dbo].[MST_EXAM_ITEMS]
    ([ExamItemCode], [ExamItemName], [NexRuleCode], [AdditionalExamCode], [AdditionalGenderCode], [AdditionalActive])
VALUES
    ('EX001', N'문진/진찰',     'NEX-01', NULL,    NULL, 0),
    ('EX002', N'신체계측',      'NEX-01', NULL,    NULL, 0),
    ('EX003', N'혈압',          'NEX-01', NULL,    NULL, 0),
    ('EX004', N'시력·청력',     'NEX-01', NULL,    NULL, 0),
    ('EX005', N'흉부 X-ray',    'NEX-01', NULL,    NULL, 0),
    ('EX006', N'요검사',        'NEX-01', NULL,    NULL, 0),
    ('EX007', N'혈액검사',      'NEX-01', NULL,    NULL, 0),
    ('EX008', N'구강검진',      'NEX-01', NULL,    NULL, 0),
    ('EX009', N'이상지질혈증',  'NEX-02', NULL,    NULL, 0),
    ('EX010', N'B형간염',       'NEX-03', NULL,    NULL, 0),
    ('EX011', N'C형간염',       'NEX-04', NULL,    NULL, 0),
    ('EX012', N'골밀도검사',    'NEX-05', 'OPT04', 'A',  1),
    ('EX013', N'폐기능',        'NEX-06', NULL,    NULL, 0),
    ('EX014', N'복부초음파',    NULL,     'OPT01', 'A',  1),
    ('EX015', N'갑상선초음파',  NULL,     'OPT02', 'A',  1),
    ('EX016', N'유방초음파',    NULL,     'OPT03', 'F',  1),
    ('EX017', N'PSA',           NULL,     'OPT05', 'M',  1),
    ('EX018', N'HbA1c',         NULL,     'OPT06', 'A',  1),
    ('EX019', N'HPV 검사',      NULL,     'OPT07', 'F',  1);
GO
INSERT INTO [dbo].[MST_HOLIDAYS] ([HolidayDate], [HolidayName], [Active], [Memo])
VALUES
    ('2026-12-25', N'성탄절',     1, N'평일(금) 휴무일 — HOL-05 테스트용'),
    ('2026-12-26', N'센터 휴진일', 1, N'토요일 휴무일 — HOL-05 테스트용');
GO
PRINT 'PASS SEED-DEPLOY Exam ' + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM [dbo].[MST_EXAM_ITEMS]))
    + ' / Holiday ' + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM [dbo].[MST_HOLIDAYS]));
GO
```

- [ ] **Step 2: 실행**

```bash
head -c 3 deploy/02_Seed.sql | od -An -tx1
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i deploy/02_Seed.sql -o artifacts/logs/02_seed.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/02_seed.log | tail -3
```

Expected: exit 0, `PASS SEED-DEPLOY Exam 19 / Holiday 2`

- [ ] **Step 3: 멱등성 경계 확인 — clean-create 직후에만 재실행 가능**

```bash
# (a) 이미 Seed 된 상태에서 단독 재실행 → 51002 로 중단되어야 한다
RC=0
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/02_Seed.sql || RC=$?
echo "재실행 exit=$RC   (1 이어야 한다)"

# (b) 스키마부터 다시 → 정상
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/01_Schema.sql
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/02_Seed.sql
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W \
  -Q "SELECT 'Exam='+CONVERT(varchar(5),COUNT(*)) FROM MST_EXAM_ITEMS;"
```

Expected: (a) `exit=1` + `Msg 51002`, (b) exit 0 + `Exam=19`.

- [ ] **Step 4: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/02_Seed.sql
git commit -m "feat(phase4): 검사 Master 19행 및 휴무일 2행 Seed 추가"
```

**회귀시험:** `T09`

**로그 경로:** `artifacts/logs/02_seed.log`

**Rollback/Cleanup:** `./scripts/rebuild.sh`

**완료조건:** 두 번 연속 실행 후에도 Exam 19 / Holiday 2.

---

## Task T09: `tests/02_Seed_Tests.sql` — Seed 검증

**목적:** Seed의 개수·역할 배분·`EX012` 이중역할·요일이 정확한지 자동 검증한다 (G07).

**관련 Baseline 위치:** `04` §4.6, 스펙 §13·§14.

**선행조건:** `T08` 완료.

**Files:**
- Create: `tests/02_Seed_Tests.sql`

**Interfaces:**
- Produces: `PASS SED-001` ~ `PASS SED-010`

- [ ] **Step 1: RED — 기대값을 20으로 두고 실패를 확인**

`SED-001`만 먼저 작성하되 `= 20` 으로 둔다.

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/02_Seed_Tests.sql -o artifacts/logs/test_02_red.log
echo "exit=$?"
```

Expected: exit **1**, `FAIL SED-001`.

- [ ] **Step 2: 전체 테스트 작성**

```sql
SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- SED-001 Exam Master 19행 **전건 값** EXCEPT 양방향  (04 §4.6)
--   개수·표본 확인으로는 어떤 행의 역할·성별·Active 가 틀려도 통과한다.
DECLARE @ExpExam TABLE (Code VARCHAR(10) PRIMARY KEY, Nm NVARCHAR(100),
                        Nex VARCHAR(10) NULL, Aex VARCHAR(10) NULL, G CHAR(1) NULL, Act BIT);
INSERT INTO @ExpExam VALUES
 ('EX001', N'문진/진찰',    'NEX-01', NULL,    NULL, 0), ('EX002', N'신체계측',     'NEX-01', NULL,    NULL, 0),
 ('EX003', N'혈압',         'NEX-01', NULL,    NULL, 0), ('EX004', N'시력·청력',    'NEX-01', NULL,    NULL, 0),
 ('EX005', N'흉부 X-ray',   'NEX-01', NULL,    NULL, 0), ('EX006', N'요검사',       'NEX-01', NULL,    NULL, 0),
 ('EX007', N'혈액검사',     'NEX-01', NULL,    NULL, 0), ('EX008', N'구강검진',     'NEX-01', NULL,    NULL, 0),
 ('EX009', N'이상지질혈증', 'NEX-02', NULL,    NULL, 0), ('EX010', N'B형간염',      'NEX-03', NULL,    NULL, 0),
 ('EX011', N'C형간염',      'NEX-04', NULL,    NULL, 0), ('EX012', N'골밀도검사',   'NEX-05', 'OPT04', 'A',  1),
 ('EX013', N'폐기능',       'NEX-06', NULL,    NULL, 0), ('EX014', N'복부초음파',   NULL,     'OPT01', 'A',  1),
 ('EX015', N'갑상선초음파', NULL,     'OPT02', 'A',  1), ('EX016', N'유방초음파',   NULL,     'OPT03', 'F',  1),
 ('EX017', N'PSA',          NULL,     'OPT05', 'M',  1), ('EX018', N'HbA1c',        NULL,     'OPT06', 'A',  1),
 ('EX019', N'HPV 검사',     NULL,     'OPT07', 'F',  1);

IF NOT EXISTS (SELECT Code,Nm,Nex,Aex,G,Act FROM @ExpExam
               EXCEPT SELECT [ExamItemCode],[ExamItemName],[NexRuleCode],[AdditionalExamCode],
                             [AdditionalGenderCode],[AdditionalActive] FROM [dbo].[MST_EXAM_ITEMS])
   AND NOT EXISTS (SELECT [ExamItemCode],[ExamItemName],[NexRuleCode],[AdditionalExamCode],
                          [AdditionalGenderCode],[AdditionalActive] FROM [dbo].[MST_EXAM_ITEMS]
                   EXCEPT SELECT Code,Nm,Nex,Aex,G,Act FROM @ExpExam)
    PRINT 'PASS SED-001 Exam Master 19행 전건 값 일치';
ELSE
BEGIN
    PRINT 'FAIL SED-001 Exam Master 불일치';
    SELECT '기대에만' AS Side, * FROM (SELECT Code,Nm,Nex,Aex,G,Act FROM @ExpExam
        EXCEPT SELECT [ExamItemCode],[ExamItemName],[NexRuleCode],[AdditionalExamCode],
                      [AdditionalGenderCode],[AdditionalActive] FROM [dbo].[MST_EXAM_ITEMS]) a;
    SET @Fail += 1;
END

IF ((SELECT COUNT(*) FROM [dbo].[MST_EXAM_ITEMS] WHERE [NexRuleCode] IS NOT NULL) = 13)
    PRINT 'PASS SED-002 NEX 역할 13행';
ELSE BEGIN PRINT 'FAIL SED-002 NEX 역할 행수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[MST_EXAM_ITEMS] WHERE [AdditionalExamCode] IS NOT NULL) = 7)
    PRINT 'PASS SED-003 AEX 역할 7행';
ELSE BEGIN PRINT 'FAIL SED-003 AEX 역할 행수 불일치'; SET @Fail += 1; END

IF EXISTS (SELECT 1 FROM [dbo].[MST_EXAM_ITEMS]
            WHERE [ExamItemCode] = 'EX012' AND [NexRuleCode] = 'NEX-05'
              AND [AdditionalExamCode] = 'OPT04' AND [AdditionalGenderCode] = 'A' AND [AdditionalActive] = 1)
    PRINT 'PASS SED-004 EX012 가 NEX-05 와 OPT04 역할을 동시에 가짐';
ELSE BEGIN PRINT 'FAIL SED-004 EX012 이중역할 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(DISTINCT [AdditionalExamCode]) FROM [dbo].[MST_EXAM_ITEMS] WHERE [AdditionalExamCode] IS NOT NULL) = 7
    AND NOT EXISTS (SELECT [AdditionalExamCode] FROM [dbo].[MST_EXAM_ITEMS] WHERE [AdditionalExamCode] IS NOT NULL
                    EXCEPT SELECT v.c FROM (VALUES ('OPT01'),('OPT02'),('OPT03'),('OPT04'),('OPT05'),('OPT06'),('OPT07')) v(c)))
    PRINT 'PASS SED-005 AEX 코드가 OPT01~OPT07 정확히 7개';
ELSE BEGIN PRINT 'FAIL SED-005 AEX 코드 집합 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[MST_EXAM_ITEMS] WHERE [NexRuleCode] = 'NEX-01') = 8)
    PRINT 'PASS SED-006 NEX-01 기본검사 8행';
ELSE BEGIN PRINT 'FAIL SED-006 NEX-01 행수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[MST_EXAM_ITEMS] WHERE [NexRuleCode] IN ('NEX-02','NEX-03','NEX-04','NEX-05','NEX-06')) = 5)
    PRINT 'PASS SED-007 조건부 NEX 5행';
ELSE BEGIN PRINT 'FAIL SED-007 조건부 NEX 행수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[MST_HOLIDAYS] WHERE [Active] = 1) = 2)
    PRINT 'PASS SED-008 활성 휴무일 2행';
ELSE BEGIN PRINT 'FAIL SED-008 휴무일 행수 불일치'; SET @Fail += 1; END

IF EXISTS (SELECT 1 FROM [dbo].[MST_HOLIDAYS] WHERE [HolidayDate] = '2026-12-25' AND DATEDIFF(DAY,0,[HolidayDate])%7 = 4)
    PRINT 'PASS SED-009 평일(금) 휴무일 존재';
ELSE BEGIN PRINT 'FAIL SED-009 평일 휴무일 없음 또는 요일 불일치'; SET @Fail += 1; END

IF EXISTS (SELECT 1 FROM [dbo].[MST_HOLIDAYS] WHERE [HolidayDate] = '2026-12-26' AND DATEDIFF(DAY,0,[HolidayDate])%7 = 5)
    PRINT 'PASS SED-010 토요일 휴무일 존재';
ELSE BEGIN PRINT 'FAIL SED-010 토요일 휴무일 없음 또는 요일 불일치'; SET @Fail += 1; END

-- SED-011 AEX 7종이 전부 AdditionalActive=1 인가
--   CORRUPT-3 이 tests/03 에서 일시적으로 0 으로 바꾸고 ROLLBACK 하므로,
--   이 검사는 그 오염이 남지 않았음을 보증한다.
IF ((SELECT COUNT(*) FROM [dbo].[MST_EXAM_ITEMS]
      WHERE [AdditionalExamCode] IS NOT NULL AND [AdditionalActive] = 1) = 7)
    PRINT 'PASS SED-011 AEX 7종 전부 AdditionalActive=1';
ELSE BEGIN PRINT 'FAIL SED-011 AEX Active 오염'; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 02_Seed_Tests 완료 ===';
GO
```

- [ ] **Step 3: GREEN 실행**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/02_Seed_Tests.sql -o artifacts/logs/test_02.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_02.log | grep -E '^(PASS|FAIL)'
```

Expected: exit 0, `PASS SED-001` ~ `PASS SED-010` 10건.

- [ ] **Step 4: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/tests/02_Seed_Tests.sql
git commit -m "test(phase4): Master Seed 검증 10건 추가"
```

**완료조건:** RED에서 exit 1 관측 + GREEN 10/10.

---

## Task T10: `tests/00_Test_Harness.sql` — Fixture · 주민번호 무효 검수

**목적:** 결정적 테스트 데이터를 만들고, 저장된 모든 주민번호의 체크디지트가 무효임을 증명한다 (스펙 §16).

**관련 Baseline 위치:** `00` §2.1, `05` §17.3~17.6, 스펙 §15·§16.

**선행조건:** `T09` 완료.

**Files:**
- Create: `tests/00_Test_Harness.sql`

**Interfaces:**
- Produces: 테스트 수검자 (`ChartNo` `T001`~`T019`, 정원용 `F001`~`F020`), 완료이력, 제외정보, 기존 예약 19건 + `F020` 의 `CNL` 1건, 손상 데이터 **2종**(`CORRUPT-1`·`CORRUPT-2`)

**금지사항:** 실제 주민등록번호를 사용하지 않는다. `PatientId`·`WorkId` 를 하드코딩하지 않는다. Fixture를 Write SP로 만들지 않는다(업무시간 밖에 실패한다).

- [ ] **Step 1: 체크디지트 무효화 식을 인라인으로 정의**

주민번호는 손으로 계산하지 않는다. **앞 12자리만 지정**하고 13번째 자리를 SQL이 계산한다.

```sql
-- Prefix12 = YYMMDD(6) + 세기·성별(1) + 순번(5)
-- 정상 체크디지트 = (11 - (가중합 % 11)) % 10
-- 무효 체크디지트 = (정상 + 1) % 10
-- 아래 식을 INSERT 의 SELECT 안에서 그대로 사용한다.
CONVERT(CHAR(1),
  ( ( ( 11 - (
        ( CAST(SUBSTRING(p.Prefix12, 1,1) AS INT)*2 + CAST(SUBSTRING(p.Prefix12, 2,1) AS INT)*3
        + CAST(SUBSTRING(p.Prefix12, 3,1) AS INT)*4 + CAST(SUBSTRING(p.Prefix12, 4,1) AS INT)*5
        + CAST(SUBSTRING(p.Prefix12, 5,1) AS INT)*6 + CAST(SUBSTRING(p.Prefix12, 6,1) AS INT)*7
        + CAST(SUBSTRING(p.Prefix12, 7,1) AS INT)*8 + CAST(SUBSTRING(p.Prefix12, 8,1) AS INT)*9
        + CAST(SUBSTRING(p.Prefix12, 9,1) AS INT)*2 + CAST(SUBSTRING(p.Prefix12,10,1) AS INT)*3
        + CAST(SUBSTRING(p.Prefix12,11,1) AS INT)*4 + CAST(SUBSTRING(p.Prefix12,12,1) AS INT)*5
        ) % 11 ) ) % 10 + 1 ) % 10 )
```

- [ ] **Step 2: Rule 경계 수검자 15명 INSERT**

기준 예약일 `2026-10-01` 대비 만 나이를 역산한 프로필이다.

| ChartNo | Prefix12 | 생년월일 | 성별 | 만나이 | 목적 |
|---|---|---|:---:|---:|---|
| `T001` | `061002300001` | 2006-10-02 | M | 19 | TGT `400 UnderAge` |
| `T002` | `061001300002` | 2006-10-01 | M | 20 | TGT 경계 충족 |
| `T003` | `031001300003` | 2003-10-01 | M | 23 | NEX-02 남 미해당 |
| `T004` | `021001300004` | 2002-10-01 | M | 24 | NEX-02 남 해당 |
| `T005` | `981001100005` | 1998-10-01 | M | 28 | NEX-02 남 해당 |
| `T006` | `871001200006` | 1987-10-01 | F | 39 | NEX-02 여 미해당 |
| `T007` | `861001200007` | 1986-10-01 | F | 40 | NEX-02 여 + NEX-03 (제외행 없음) |
| `T008` | `861001200008` | 1986-10-01 | F | 40 | NEX-03 제외행 있음 |
| `T009` | `711001100009` | 1971-10-01 | M | 55 | NEX-04 미해당 |
| `T010` | `701001200010` | 1970-10-01 | F | 56 | **NEX-02+04+06 → 11행** |
| `T011` | `721001200011` | 1972-10-01 | F | 54 | NEX-05 해당 |
| `T012` | `661001200012` | 1966-10-01 | F | 60 | NEX-05 해당 |
| `T013` | `601001200013` | 1960-10-01 | F | 66 | NEX-05 + NEX-06 |
| `T014` | `701001100014` | 1970-10-01 | M | 56 | 남 3종 → 11행 |
| `T015` | `801001100015` | 1980-10-01 | M | 46 | 일반 대상 (Write SP 테스트용) |

```sql
SET NOCOUNT ON;
PRINT '--- 00_Test_Harness 시작 ---';
GO
DELETE FROM [dbo].[INFO_CHECKUP_WORK_EXAMS];
DELETE FROM [dbo].[INFO_CHECKUP_WORKS];
DELETE FROM [dbo].[INFO_PATIENT_EXAM_EXCLUSIONS];
DELETE FROM [dbo].[HIS_GENERAL_CHECKUP_COMPLETIONS];
DELETE FROM [dbo].[INFO_PATIENTS];
GO
INSERT INTO [dbo].[INFO_PATIENTS]
    ([ChartNo], [Name], [SocialNumber], [Birthday], [Gender], [CelNumber], [CelNumberS])
SELECT
      p.ChartNo
    , p.Name
    , p.Prefix12 + CONVERT(CHAR(1),
        ( ( ( 11 - (
              ( CAST(SUBSTRING(p.Prefix12, 1,1) AS INT)*2 + CAST(SUBSTRING(p.Prefix12, 2,1) AS INT)*3
              + CAST(SUBSTRING(p.Prefix12, 3,1) AS INT)*4 + CAST(SUBSTRING(p.Prefix12, 4,1) AS INT)*5
              + CAST(SUBSTRING(p.Prefix12, 5,1) AS INT)*6 + CAST(SUBSTRING(p.Prefix12, 6,1) AS INT)*7
              + CAST(SUBSTRING(p.Prefix12, 7,1) AS INT)*8 + CAST(SUBSTRING(p.Prefix12, 8,1) AS INT)*9
              + CAST(SUBSTRING(p.Prefix12, 9,1) AS INT)*2 + CAST(SUBSTRING(p.Prefix12,10,1) AS INT)*3
              + CAST(SUBSTRING(p.Prefix12,11,1) AS INT)*4 + CAST(SUBSTRING(p.Prefix12,12,1) AS INT)*5
              ) % 11 ) ) % 10 + 1 ) % 10 )
    , p.Birthday
    , p.Gender
    , NULL, NULL
FROM (VALUES
      ('T001', N'테스트일구', '061002300001', '20061002', 'M')
    , ('T002', N'테스트이공', '061001300002', '20061001', 'M')
    , ('T003', N'테스트이삼', '031001300003', '20031001', 'M')
    , ('T004', N'테스트이사', '021001300004', '20021001', 'M')
    , ('T005', N'테스트이팔', '981001100005', '19981001', 'M')
    , ('T006', N'테스트삼구', '871001200006', '19871001', 'F')
    , ('T007', N'테스트사공', '861001200007', '19861001', 'F')
    , ('T008', N'테스트사영', '861001200008', '19861001', 'F')
    , ('T009', N'테스트오오', '711001100009', '19711001', 'M')
    , ('T010', N'테스트오육', '701001200010', '19701001', 'F')
    , ('T011', N'테스트오사', '721001200011', '19721001', 'F')
    , ('T012', N'테스트육공', '661001200012', '19661001', 'F')
    , ('T013', N'테스트육육', '601001200013', '19601001', 'F')
    , ('T014', N'테스트오륙남', '701001100014', '19701001', 'M')
    , ('T015', N'테스트사육', '801001100015', '19801001', 'M')
    -- [X] T016·T017 은 초안에서 "추가한다" 는 산문만 있고 이 VALUES 에 없었다.
    --     소비처(T13 의 401 시험, T26 의 RWR-006)가 NULL 을 얻어 무의미하게 통과한다.
    , ('T016', N'테스트삼육', '901001100016', '19901001', 'M')   -- TGT 401 NotDue 전담 (1년차 완료이력)
    , ('T017', N'테스트일팔', '071118300017', '20071118', 'M')   -- TGT 400 UnderAge 전담
    -- [X] RUL-N04(여 만 44세) 와 RUL-N08(남 만 54세) 이 쓸 프로필이 없어 술어를 끝까지 시험할 수 없었다.
    --     기준일 2026-10-01 기준 나이다(§15.5).
    , ('T018', N'테스트사사', '821001200018', '19821001', 'F')   -- 만 44세 여 → NEX-02 (44-40)%4=0 성립
    , ('T019', N'테스트오사남', '721001100019', '19721001', 'M') -- 만 54세 남 → NEX-05 는 여성 전용이므로 EX012 없음
    -- [X] RWR-031(예약일 변경으로 NEX 구성이 바뀌어 OPT04 가 EX012 와 충돌) 전용.
    --     예약일을 옮기면 만나이가 경계를 넘어 EX012 가 새로 생기는 프로필이 필요하다.
    --     실측: 1972-11-20 생은 2026-11-17 에 만 53세(EX012 없음), 2026-11-20 에 만 54세(EX012 생김).
    , ('T020', N'테스트경계녀', '721120200020', '19721120', 'F')
) p (ChartNo, Name, Prefix12, Birthday, Gender);
GO
```

`Birthday` 는 Fixture가 직접 지정한다 — Write SP가 산출하는 값과 동일해야 하며, `T23` 의 테스트가 그 일치를 별도로 검증한다.

`[X]` **`T017` 의 생년월일은 `SYSDATETIME()` 기반 상대식으로 만들지 않는다.** 초안은 `DATEADD(YEAR, -19, CONVERT(DATE, SYSDATETIME()))` 로 "오늘 기준 만 18세" 를 만들려 했으나 (1) `-19` 는 만 **19**세를 만들고, (2) 배포 시각에 따라 값이 달라져 §15.5 의 고정날짜 원칙과 G14(Rebuild 후 동일 결과)를 깬다.

**고정 리터럴 `2007-11-18` 을 쓴다.** 유일한 소비처는 `RWR-006`(예약일 `2026-11-17`, `400 UnderAge` 기대)이고, 만나이는 그날 **18세**다(19세가 되는 날은 `2026-11-18`). 예약일을 옮기면 이 값도 함께 옮겨야 하며, `RWR-006` 이 `400` 대신 `200` 을 받으면 그 신호다.

- [ ] **Step 3: 정원용 수검자 19명 + 기존 예약 19건**

`RP-06` 때문에 한 수검자는 유효업무를 둘 이상 가질 수 없으므로 19명이 필요하다.

`ROW_NUMBER() OVER (ORDER BY (SELECT 1)) FROM sys.all_objects` 를 쓰지 않는다 — `TOP (19)` 에 외부 `ORDER BY` 가 없어 `n` 이 `1..19` 라는 보장이 없고, `n >= 1000` 이 뽑히면 `CONVERT(VARCHAR(3), n)` 이 `'*'` 를 반환해 `ChartNo` 가 중복된다. **`VALUES` 로 고정한다.**

Slot 날짜는 **리터럴 하나로 통일**한다. 변수를 선언해 놓고 리터럴을 쓰면 날짜를 옮길 때 조용히 어긋난다. `2026-11-16` 은 월요일임을 실측 확인했다.

```sql
INSERT INTO [dbo].[INFO_PATIENTS] ([ChartNo], [Name], [SocialNumber], [Birthday], [Gender])
SELECT
      'F' + RIGHT('000' + CONVERT(VARCHAR(3), n.n), 3)
    , N'정원채움' + CONVERT(NVARCHAR(3), n.n)
    , x.Prefix12 + CONVERT(CHAR(1),
        ( ( ( 11 - (
              ( CAST(SUBSTRING(x.Prefix12, 1,1) AS INT)*2 + CAST(SUBSTRING(x.Prefix12, 2,1) AS INT)*3
              + CAST(SUBSTRING(x.Prefix12, 3,1) AS INT)*4 + CAST(SUBSTRING(x.Prefix12, 4,1) AS INT)*5
              + CAST(SUBSTRING(x.Prefix12, 5,1) AS INT)*6 + CAST(SUBSTRING(x.Prefix12, 6,1) AS INT)*7
              + CAST(SUBSTRING(x.Prefix12, 7,1) AS INT)*8 + CAST(SUBSTRING(x.Prefix12, 8,1) AS INT)*9
              + CAST(SUBSTRING(x.Prefix12, 9,1) AS INT)*2 + CAST(SUBSTRING(x.Prefix12,10,1) AS INT)*3
              + CAST(SUBSTRING(x.Prefix12,11,1) AS INT)*4 + CAST(SUBSTRING(x.Prefix12,12,1) AS INT)*5
              ) % 11 ) ) % 10 + 1 ) % 10 )
    , '19800101'
    , 'M'
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),
             (11),(12),(13),(14),(15),(16),(17),(18),(19),
             (20)) n(n)   -- [X] F020 추가. RWR-012(305 SlotFull) 가 CNL→RSV 로 뒤집어 20/20 을 만든다.
CROSS APPLY (SELECT Prefix12 = '8001011' + RIGHT('00000' + CONVERT(VARCHAR(5), n.n), 5)) x;
GO
-- [X] LIKE 'F0%' 만 쓰면 F020 까지 RSV 가 되어 슬롯이 20/20 이 된다.
--     CON-002(19/20 경합 → 최종 20) 의 사전조건이 깨지므로 F020 을 제외하고, 별도로 CNL 을 넣는다.
INSERT INTO [dbo].[INFO_CHECKUP_WORKS] ([PatientId], [ReservationDate], [TimeSlotCode], [StatusCode])
SELECT [PatientId], '2026-11-16', 'AM', 'RSV'
FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] LIKE 'F0%' AND [ChartNo] <> 'F020';

INSERT INTO [dbo].[INFO_CHECKUP_WORKS] ([PatientId], [ReservationDate], [TimeSlotCode], [StatusCode])
SELECT [PatientId], '2026-11-16', 'AM', 'CNL'
FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'F020';
GO
-- T020 의 RSV Work. 2026-11-17 기준 만 53세라 EX012 가 없고 OPT04 를 선택할 수 있다.
-- RWR-031 이 이 Work 의 예약일을 2026-11-20 으로 옮기면 만 54세가 되어 EX012 가 생기고 OPT04 가 412 다.
-- 이 Work 는 2026-11-17 슬롯이므로 2026-11-16 정원(CON-002)과 무관하다.
INSERT INTO [dbo].[INFO_CHECKUP_WORKS] ([PatientId], [ReservationDate], [TimeSlotCode], [StatusCode])
SELECT [PatientId], '2026-11-17', 'AM', 'RSV'
FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T020';
GO
INSERT INTO [dbo].[INFO_CHECKUP_WORK_EXAMS] ([WorkId], [ExamItemCode], [ExamSourceCode])
SELECT w.[WorkId], e.[ExamItemCode], 'NEX'
FROM [dbo].[INFO_CHECKUP_WORKS] w
JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId] AND p.[ChartNo] = 'T020'
CROSS JOIN [dbo].[MST_EXAM_ITEMS] e
WHERE e.[NexRuleCode] = 'NEX-01';
INSERT INTO [dbo].[INFO_CHECKUP_WORK_EXAMS] ([WorkId], [ExamItemCode], [ExamSourceCode])
SELECT w.[WorkId], 'EX012', 'AEX'   -- OPT04 를 선택한 상태 (만 53세라 아직 중복이 아니다)
FROM [dbo].[INFO_CHECKUP_WORKS] w
JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId] AND p.[ChartNo] = 'T020';
GO
INSERT INTO [dbo].[INFO_CHECKUP_WORK_EXAMS] ([WorkId], [ExamItemCode], [ExamSourceCode])
SELECT w.[WorkId], e.[ExamItemCode], 'NEX'
FROM [dbo].[INFO_CHECKUP_WORKS] w
JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId] AND p.[ChartNo] LIKE 'F0%'
CROSS JOIN [dbo].[MST_EXAM_ITEMS] e
WHERE e.[NexRuleCode] = 'NEX-01';
GO
```

`F001`~`F020` 20건 × 8 NEX = **160행**. 그중 `F020` 은 `CNL` 이므로 정원 계수에서 빠져 이 Slot 은 **19/20** 상태가 되고, 이것이 `CON-002` 경합 시나리오의 사전조건이다.

`RWR-012` 는 이 파일이 아니라 `tests/06` 머리에서 `F020` 을 `RSV` 로 되돌려 20/20 을 만들고, 단언 후 다시 `CNL` 로 복원한다 — Fixture 파일을 오염시키지 않는다.

- [ ] **Step 4: 완료이력 · 제외정보**

`[X]` **`T003` 에 완료이력을 주면 안 된다.** `T003`(만 23세 남)의 프로필 목적은 "NEX-02 남 미해당 → NEX **8행**"인데, 1년차 완료이력을 붙이면 TGT `401 NotDue` 비대상이 되어 **NEX 0행**이 나온다. 두 용도가 양립하지 않는다. `401` 전담 수검자 **`T016`** 을 Step 2 표에 추가한다.

| ChartNo | Prefix12 | 생년월일 | 성별 | 만나이 | 목적 |
|---|---|---|:---:|---:|---|
| `T016` | `901001100016` | 1990-10-01 | M | 36 | **TGT `401 NotDue` 전담** (1년차 완료이력) |

```sql
INSERT INTO [dbo].[HIS_GENERAL_CHECKUP_COMPLETIONS] ([PatientId], [CompletionDate])
SELECT p.[PatientId], '2025-05-01' FROM [dbo].[INFO_PATIENTS] p WHERE p.[ChartNo] = 'T016';  -- 1년차 → 401 NotDue
INSERT INTO [dbo].[HIS_GENERAL_CHECKUP_COMPLETIONS] ([PatientId], [CompletionDate])
SELECT p.[PatientId], '2024-05-01' FROM [dbo].[INFO_PATIENTS] p WHERE p.[ChartNo] = 'T004';  -- 2년차 → 대상
INSERT INTO [dbo].[HIS_GENERAL_CHECKUP_COMPLETIONS] ([PatientId], [CompletionDate])
SELECT p.[PatientId], '2026-10-01' FROM [dbo].[INFO_PATIENTS] p WHERE p.[ChartNo] = 'T005';  -- 예약일 당일 → 제외
INSERT INTO [dbo].[HIS_GENERAL_CHECKUP_COMPLETIONS] ([PatientId], [CompletionDate])
SELECT p.[PatientId], '2026-11-01' FROM [dbo].[INFO_PATIENTS] p WHERE p.[ChartNo] = 'T006';  -- 예약일 이후 → 제외
GO
INSERT INTO [dbo].[INFO_PATIENT_EXAM_EXCLUSIONS] ([PatientId], [ExamItemCode], [Memo])
SELECT p.[PatientId], 'EX010', N'B형간염 제외 — NEX-03 테스트'
FROM [dbo].[INFO_PATIENTS] p WHERE p.[ChartNo] = 'T008';
GO
```

- [ ] **Step 5: 손상 데이터 2종 (구획 분리)**

`[X]` 초안은 `CORRUPT-3` 을 선언만 하고 배치하지 않은 채 완료조건에 *"손상 데이터 3종"* 을 적어 **달성 불가능한 조건**을 만들었다. 이 파일이 만드는 것은 `CORRUPT-1`·`CORRUPT-2` **2종**이고, 나머지 3종의 소유는 아래 표에 못박는다.

```sql
PRINT '--- CORRUPT 구획 (701 검증 전용) ---';
GO
-- CORRUPT-1: 동일 Patient 에 유효업무 2건  → 701
INSERT INTO [dbo].[INFO_CHECKUP_WORKS] ([PatientId], [ReservationDate], [TimeSlotCode], [StatusCode])
SELECT p.[PatientId], '2026-11-17', 'AM', 'RSV' FROM [dbo].[INFO_PATIENTS] p WHERE p.[ChartNo] = 'T012';
INSERT INTO [dbo].[INFO_CHECKUP_WORKS] ([PatientId], [ReservationDate], [TimeSlotCode], [StatusCode])
SELECT p.[PatientId], '2026-11-18', 'PM', 'RSV' FROM [dbo].[INFO_PATIENTS] p WHERE p.[ChartNo] = 'T012';
GO
-- CORRUPT-2: NEX 0행인 Work  → 701
INSERT INTO [dbo].[INFO_CHECKUP_WORKS] ([PatientId], [ReservationDate], [TimeSlotCode], [StatusCode])
SELECT p.[PatientId], '2026-11-19', 'AM', 'RSV' FROM [dbo].[INFO_PATIENTS] p WHERE p.[ChartNo] = 'T013';
GO
```

- [ ] **Step 5b: 손상 데이터 마무리**

```sql
PRINT 'PASS FIX-DEPLOY Fixture 배치 완료';
GO
```

`[X]` **RCP 상태 Work(`T29` 가 필요)는 이 파일에 넣지 않는다.** `UFN_HC_국가검사구성` 이 있어야 하므로 `T14` 이후여야 하고, 해당 블록에 `GO` 가 3개 있어 **`IF … BEGIN … END` 로 감쌀 수 없다**(`GO` 는 배치 구분자다). **`T14b` 가 별도 파일 `tests/00b_Test_Harness_RCP.sql` 로 만든다.**

**손상 Fixture 소유표** — 어느 파일이 만들고 어디서 소비하는지 한 곳에 못박는다.

| Fixture | 내용 | 만드는 곳 | 되돌리는 곳 | 소비 Test ID |
|---|---|---|---|---|
| `CORRUPT-1` | `T012` 에 유효업무 2건 | `tests/00` Step 5 | Rebuild | `RWR-011`·`RWR-033`·`SEL-010` |
| `CORRUPT-2` | `T013` 의 NEX 0행 Work | `tests/00` Step 5 | Rebuild | `CWR-005`·`SEL-015` |
| `CORRUPT-3` | `OPT06` 의 `AdditionalActive=0` | `tests/03` `RUL-A06` 안 | **같은 문장 직후 `UPDATE` 로 복원** | `RUL-A06` |
| `CORRUPT-4` | `T009` Work 의 `ExamSourceCode` 를 역할과 어긋나게 | `tests/07` `CWR-011` 앞 | 같은 파일 끝 | `CWR-011` |
| `CORRUPT-5` | `F002` Work 의 NEX 13행 | `tests/06` `RWR-034` 앞 | 같은 파일 끝 | `RWR-034` |

`[I]` `CORRUPT-3`~`5` 는 Master 또는 TVF 결과를 건드리므로 Fixture 파일에 두면 **뒤따르는 모든 테스트가 오염된다.** 소비 테스트 바로 앞에서 만들고 직후에 되돌린다(스펙 §15.3). `SED-011`(AEX 7건 전부 Active=1)과 `G07` 이 뒤에서 FAIL 하면 복원이 빠진 것이다.

`CORRUPT-4`·`CORRUPT-5` 생성/복원:

```sql
-- CORRUPT-4  tests/07 의 CWR-011 직전. ExamSourceCode 를 Master 역할과 어긋나게 만든다.
DECLARE @W4 BIGINT = (SELECT TOP (1) w.[WorkId] FROM [dbo].[INFO_CHECKUP_WORKS] w
                       JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId]
                      WHERE p.[ChartNo] = 'T009' AND w.[StatusCode] = 'RSV');
UPDATE [dbo].[INFO_CHECKUP_WORK_EXAMS] SET [ExamSourceCode] = 'AEX'
 WHERE [WorkId] = @W4 AND [ExamItemCode] = 'EX001';   -- EX001 은 Master 상 NEX 전용이다
-- … CWR-011 실행 …
UPDATE [dbo].[INFO_CHECKUP_WORK_EXAMS] SET [ExamSourceCode] = 'NEX'
 WHERE [WorkId] = @W4 AND [ExamItemCode] = 'EX001';

-- CORRUPT-5  tests/06 의 RWR-034 직전. NEX 를 12행 이상으로 만든다(상한 11 초과).
-- [X] 초안은 T011 의 RSV Work 를 찾았으나 **T011 에는 Work 를 만드는 코드가 없다** — @W5 가 NULL 이 되어
--     RWR-034 가 무의미하게 통과한다. 정원용 F002(만 46세 남, NEX 8행)의 Work 를 쓴다.
--     F002 는 2026-11-16 AM 슬롯이지만 RWR-034 는 변경이 **거부**되는지 보므로 슬롯이 움직이지 않는다.
DECLARE @W5 BIGINT = (SELECT TOP (1) w.[WorkId] FROM [dbo].[INFO_CHECKUP_WORKS] w
                       JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId]
                      WHERE p.[ChartNo] = 'F002' AND w.[StatusCode] = 'RSV');
INSERT INTO [dbo].[INFO_CHECKUP_WORK_EXAMS] ([WorkId], [ExamItemCode], [ExamSourceCode])
SELECT @W5, e.[ExamItemCode], 'NEX'
FROM [dbo].[MST_EXAM_ITEMS] e
WHERE e.[NexRuleCode] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[INFO_CHECKUP_WORK_EXAMS] x
                   WHERE x.[WorkId] = @W5 AND x.[ExamItemCode] = e.[ExamItemCode]);
-- 이 Work 의 NEX 가 12행 이상이 되는지 먼저 확인한다 — 아니면 시험이 성립하지 않는다.
IF ((SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORK_EXAMS]
      WHERE [WorkId] = @W5 AND [ExamSourceCode] = 'NEX') <= 11)
BEGIN PRINT 'FAIL CORRUPT-5 NEX 가 12행에 도달하지 못했다'; SET @Fail += 1; END
-- … RWR-034 실행 …
DELETE x FROM [dbo].[INFO_CHECKUP_WORK_EXAMS] x
 WHERE x.[WorkId] = @W5 AND x.[ExamSourceCode] = 'NEX'
   AND x.[ExamItemCode] NOT IN (SELECT [ExamCode] FROM [dbo].[UFN_HC_국가검사구성](
        (SELECT [PatientId] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @W5),
        (SELECT [ReservationDate] FROM [dbo].[INFO_CHECKUP_WORKS] WHERE [WorkId] = @W5)));
```

- [ ] **Step 6: 주민번호 무효 검수 (스펙 §16.2)**

같은 파일 끝에 붙인다.

```sql
DECLARE @Fail INT = 0;
DECLARE @Bad INT;

-- SSN-001 13자리 숫자
SELECT @Bad = COUNT(*) FROM [dbo].[INFO_PATIENTS]
 WHERE LEN([SocialNumber]) <> 13 OR [SocialNumber] LIKE '%[^0-9]%';
IF @Bad = 0 PRINT 'PASS SSN-001 전 행 13자리 숫자';
ELSE BEGIN PRINT 'FAIL SSN-001 형식 위반 ' + CONVERT(VARCHAR(5), @Bad) + '건'; SET @Fail += 1; END

-- SSN-002 7번째 자리 = 1/2/3/4
SELECT @Bad = COUNT(*) FROM [dbo].[INFO_PATIENTS]
 WHERE SUBSTRING([SocialNumber], 7, 1) NOT IN ('1','2','3','4');
IF @Bad = 0 PRINT 'PASS SSN-002 세기·성별 코드 유효';
ELSE BEGIN PRINT 'FAIL SSN-002 세기·성별 코드 위반 ' + CONVERT(VARCHAR(5), @Bad) + '건'; SET @Fail += 1; END

-- SSN-003 앞 6자리가 실제 날짜
SELECT @Bad = COUNT(*) FROM [dbo].[INFO_PATIENTS]
 WHERE TRY_CONVERT(DATE,
        CASE WHEN SUBSTRING([SocialNumber],7,1) IN ('1','2') THEN '19' ELSE '20' END
        + SUBSTRING([SocialNumber], 1, 6), 112) IS NULL;
IF @Bad = 0 PRINT 'PASS SSN-003 앞 6자리가 실제 날짜';
ELSE BEGIN PRINT 'FAIL SSN-003 날짜 아님 ' + CONVERT(VARCHAR(5), @Bad) + '건'; SET @Fail += 1; END

-- SSN-004 Birthday 파생값 일치
SELECT @Bad = COUNT(*) FROM [dbo].[INFO_PATIENTS]
 WHERE [Birthday] <> CASE WHEN SUBSTRING([SocialNumber],7,1) IN ('1','2') THEN '19' ELSE '20' END
                     + SUBSTRING([SocialNumber], 1, 6);
IF @Bad = 0 PRINT 'PASS SSN-004 Birthday 파생값 일치';
ELSE BEGIN PRINT 'FAIL SSN-004 Birthday 불일치 ' + CONVERT(VARCHAR(5), @Bad) + '건'; SET @Fail += 1; END

-- SSN-005 Gender 파생값 일치
SELECT @Bad = COUNT(*) FROM [dbo].[INFO_PATIENTS]
 WHERE [Gender] <> CASE WHEN SUBSTRING([SocialNumber],7,1) IN ('1','3') THEN 'M' ELSE 'F' END;
IF @Bad = 0 PRINT 'PASS SSN-005 Gender 파생값 일치';
ELSE BEGIN PRINT 'FAIL SSN-005 Gender 불일치 ' + CONVERT(VARCHAR(5), @Bad) + '건'; SET @Fail += 1; END

-- SSN-006 *** 체크디지트가 전 행 무효 — 실제 주민등록번호 미사용의 기계적 증거 ***
SELECT @Bad = COUNT(*)
FROM [dbo].[INFO_PATIENTS] s
CROSS APPLY (SELECT Valid =
      ( 11 - (
        ( CAST(SUBSTRING(s.[SocialNumber], 1,1) AS INT)*2 + CAST(SUBSTRING(s.[SocialNumber], 2,1) AS INT)*3
        + CAST(SUBSTRING(s.[SocialNumber], 3,1) AS INT)*4 + CAST(SUBSTRING(s.[SocialNumber], 4,1) AS INT)*5
        + CAST(SUBSTRING(s.[SocialNumber], 5,1) AS INT)*6 + CAST(SUBSTRING(s.[SocialNumber], 6,1) AS INT)*7
        + CAST(SUBSTRING(s.[SocialNumber], 7,1) AS INT)*8 + CAST(SUBSTRING(s.[SocialNumber], 8,1) AS INT)*9
        + CAST(SUBSTRING(s.[SocialNumber], 9,1) AS INT)*2 + CAST(SUBSTRING(s.[SocialNumber],10,1) AS INT)*3
        + CAST(SUBSTRING(s.[SocialNumber],11,1) AS INT)*4 + CAST(SUBSTRING(s.[SocialNumber],12,1) AS INT)*5
        ) % 11 ) ) % 10 ) c
WHERE CAST(SUBSTRING(s.[SocialNumber], 13, 1) AS INT) = c.Valid;   -- 유효하면 위반
IF @Bad = 0 PRINT 'PASS SSN-006 전 행 체크디지트 무효 — 실제 주민등록번호 미사용 증명';
ELSE BEGIN PRINT 'FAIL SSN-006 체크디지트가 유효한 행 ' + CONVERT(VARCHAR(5), @Bad) + '건'; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'Fixture 주민번호 검수 실패', 1;
PRINT '=== 00_Test_Harness 완료 ===';
GO
```

- [ ] **Step 7: 실행 — `SSN-006` 이 반드시 PASS 여야 한다**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/00_Test_Harness.sql -o artifacts/logs/test_00.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_00.log | grep -E '^(PASS|FAIL)'
```

Expected: exit 0, `PASS SSN-001` ~ `PASS SSN-006` 6건 + `PASS FIX-DEPLOY`.

`SSN-006` 이 FAIL이면 체크디지트 무효화 식이 틀린 것이다. **여기서 중단하고 식을 고친다.**

- [ ] **Step 8: Slot 요일 확인**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W -Q "
SELECT '2026-11-16 dow=' + CONVERT(varchar(2), DATEDIFF(DAY,0,CONVERT(DATE,'2026-11-16'))%7);
SELECT 'slot count=' + CONVERT(varchar(5), COUNT(*)) FROM INFO_CHECKUP_WORKS
 WHERE ReservationDate='2026-11-16' AND TimeSlotCode='AM' AND StatusCode IN ('RSV','RCP');"
```

Expected: `dow=0` (월요일 — 업무일), `slot count=19`.
`dow` 가 5(토) 또는 6(일)이면 다른 날짜를 골라 `@SlotDate` 를 수정한다.

- [ ] **Step 9: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/tests/00_Test_Harness.sql
git commit -m "test(phase4): Test Fixture 및 주민번호 무효 검수 추가"
```

**회귀시험:** 모든 테스트 실행이 이 파일로 시작한다.

**로그 경로:** `artifacts/logs/test_00.log`

**Rollback/Cleanup:** `./scripts/rebuild.sh` — 개별 cleanup 로직을 만들지 않는다.

**완료조건:** 스펙 §45.2 의 `SSN` 전건 PASS, `2026-11-16 AM` Slot 이 19/20, `CORRUPT-1`·`CORRUPT-2` 배치 완료.

---

## Task T11: `[dbo].[UFN_HC_일정확인]`

**목적:** 현재 공통 업무 가능 여부와 요청 예약일·시간대·마감 가능 여부를 한 행으로 반환한다.

**관련 Baseline 위치:** `05` §6.1 (입출력·책임·우선순위), `00` §3장 (마감표), §CP-02~04, HOL-01~02, 스펙 §17.1.

**선행조건:** `T10` 완료.

**Files:**
- Create: `deploy/03_Functions.sql` (이 Task에서 생성, `T12`~`T14`가 이어서 추가)
- Create: `tests/03_Rule_Tests.sql` (이 Task에서 생성, `T12`~`T14`가 이어서 추가)

**Interfaces:**
- Produces: `[dbo].[UFN_HC_일정확인](@ServerTime DATETIME2(7), @ReservationDate DATE, @TimeSlot CHAR(2), @CutoffType VARCHAR(10))` → 1행 11컬럼

**금지사항:** `SYSDATETIME()` 을 함수 안에서 호출하지 않는다(호출 SP가 캡처한 `@ServerTime` 만 사용). `SET DATEFIRST` 에 의존하지 않는다. `THROW`·Transaction·데이터 변경 금지.

- [ ] **Step 1: RED — 테스트를 먼저 작성한다**

`tests/03_Rule_Tests.sql` 을 만들고 시간경계 12건을 넣는다.

```sql
SET NOCOUNT ON;
DECLARE @Fail INT = 0;
DECLARE @Biz DATE = '2026-11-16';   -- 월요일, 휴무일 아님

-- RUL-T01 08:59:59.9999999 → 309
IF ((SELECT WorkCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 08:59:59.9999999'), @Biz, 'AM', 'NONE')) = 309)
    PRINT 'PASS RUL-T01 08:59:59 업무불가 309';
ELSE BEGIN PRINT 'FAIL RUL-T01'; SET @Fail += 1; END

-- RUL-T02 09:00:00 → CanWorkNow=1
IF ((SELECT CanWorkNow FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 09:00:00.0000000'), @Biz, 'AM', 'NONE')) = 1)
    PRINT 'PASS RUL-T02 09:00:00 업무가능';
ELSE BEGIN PRINT 'FAIL RUL-T02'; SET @Fail += 1; END

-- RUL-T03 17:59:59.9999999 → CanWorkNow=1
IF ((SELECT CanWorkNow FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 17:59:59.9999999'), @Biz, 'AM', 'NONE')) = 1)
    PRINT 'PASS RUL-T03 17:59:59 업무가능';
ELSE BEGIN PRINT 'FAIL RUL-T03'; SET @Fail += 1; END

-- RUL-T04 18:00:00 → 309
IF ((SELECT WorkCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 18:00:00.0000000'), @Biz, 'AM', 'NONE')) = 309)
    PRINT 'PASS RUL-T04 18:00:00 업무불가 309';
ELSE BEGIN PRINT 'FAIL RUL-T04'; SET @Fail += 1; END

-- RUL-T05/T06 NORMAL AM 마감 10:00
IF ((SELECT CutoffPassed FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 09:59:59.9999999'), @Biz, 'AM', 'NORMAL')) = 0)
    PRINT 'PASS RUL-T05 09:59:59 NORMAL AM 마감 전';
ELSE BEGIN PRINT 'FAIL RUL-T05'; SET @Fail += 1; END
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:00:00.0000000'), @Biz, 'AM', 'NORMAL')) = 304)
    PRINT 'PASS RUL-T06 10:00:00 NORMAL AM 304';
ELSE BEGIN PRINT 'FAIL RUL-T06'; SET @Fail += 1; END

-- RUL-T07/T08 RECEPTION AM 마감 11:00
IF ((SELECT CutoffPassed FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:59:59.9999999'), @Biz, 'AM', 'RECEPTION')) = 0)
    PRINT 'PASS RUL-T07 10:59:59 RECEPTION AM 마감 전';
ELSE BEGIN PRINT 'FAIL RUL-T07'; SET @Fail += 1; END
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 11:00:00.0000000'), @Biz, 'AM', 'RECEPTION')) = 304)
    PRINT 'PASS RUL-T08 11:00:00 RECEPTION AM 304';
ELSE BEGIN PRINT 'FAIL RUL-T08'; SET @Fail += 1; END

-- RUL-T09 14:59:59.9999999 + NORMAL/PM → CutoffPassed=0
IF ((SELECT CutoffPassed FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 14:59:59.9999999'), @Biz, 'PM', 'NORMAL')) = 0)
    PRINT 'PASS RUL-T09 14:59:59 NORMAL PM 마감 전';
ELSE BEGIN PRINT 'FAIL RUL-T09'; SET @Fail += 1; END

-- RUL-T10 15:00:00 + NORMAL/PM → ReasonCode=304
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 15:00:00.0000000'), @Biz, 'PM', 'NORMAL')) = 304)
    PRINT 'PASS RUL-T10 15:00:00 NORMAL PM 304';
ELSE BEGIN PRINT 'FAIL RUL-T10'; SET @Fail += 1; END

-- RUL-T11 15:59:59.9999999 + RECEPTION/PM → CutoffPassed=0
IF ((SELECT CutoffPassed FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 15:59:59.9999999'), @Biz, 'PM', 'RECEPTION')) = 0)
    PRINT 'PASS RUL-T11 15:59:59 RECEPTION PM 마감 전';
ELSE BEGIN PRINT 'FAIL RUL-T11'; SET @Fail += 1; END

-- RUL-T12 16:00:00 + RECEPTION/PM → ReasonCode=304
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 16:00:00.0000000'), @Biz, 'PM', 'RECEPTION')) = 304)
    PRINT 'PASS RUL-T12 16:00:00 RECEPTION PM 304';
ELSE BEGIN PRINT 'FAIL RUL-T12'; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
GO
```

- [ ] **Step 2: RED 실행 — 함수가 없어 실패하는지 확인**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/03_Rule_Tests.sql -o artifacts/logs/test_03_red.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_03_red.log | head -5
```

Expected: exit **1**, `Msg 4121` 또는 `Msg 208` — "UFN_HC_일정확인 개체를 찾을 수 없습니다".

- [ ] **Step 3: `deploy/03_Functions.sql` 에 구현 (UTF-8 with BOM)**

```sql
SET NOCOUNT ON;
GO
CREATE OR ALTER FUNCTION [dbo].[UFN_HC_일정확인]
(
    @ServerTime      DATETIME2(7),
    @ReservationDate DATE,
    @TimeSlot        CHAR(2),
    @CutoffType      VARCHAR(10)
)
RETURNS TABLE
AS
RETURN
(
    SELECT
          CanWorkNow    = CONVERT(BIT, CASE WHEN d.TodayBiz = 1 AND d.WithinHours = 1 THEN 1 ELSE 0 END)
        , WorkCode      = CONVERT(INT, CASE WHEN d.TodayBiz = 0 THEN 308
                                            WHEN d.WithinHours = 0 THEN 309 ELSE 0 END)
        , WorkMessage   = CONVERT(NVARCHAR(300),
                            CASE WHEN d.TodayBiz = 0    THEN N'오늘은 업무일이 아닙니다.'
                                 WHEN d.WithinHours = 0 THEN N'현재는 업무 운영시간이 아닙니다.'
                                 ELSE N'' END)
        , IsBusinessDay = CONVERT(BIT, d.ReqBiz)
        , HolidayName   = CONVERT(NVARCHAR(100), d.ReqHoliday)
        , IsOpen        = CONVERT(BIT, CASE WHEN d.ReqBiz = 1 AND d.SlotOpen = 1 THEN 1 ELSE 0 END)
        , CutoffTime    = CONVERT(TIME(0), CASE WHEN d.SlotOpen = 0 THEN NULL ELSE d.Cutoff END)
        , CutoffPassed  = CONVERT(BIT, CASE WHEN d.Cutoff IS NOT NULL
                                             AND d.NowTime >= CONVERT(TIME(7), d.Cutoff) THEN 1 ELSE 0 END)
        , CanUse        = CONVERT(BIT, CASE WHEN d.ReasonCode = 0 THEN 1 ELSE 0 END)
        , ReasonCode    = CONVERT(INT, d.ReasonCode)
        , ReasonMessage = CONVERT(NVARCHAR(300),
                            CASE d.ReasonCode
                                WHEN 300 THEN N'과거 날짜는 예약할 수 없습니다.'
                                WHEN 301 THEN N'일요일은 업무일이 아닙니다.'
                                WHEN 302 THEN N'선택한 날짜는 휴무일입니다.'
                                WHEN 303 THEN N'선택한 시간대는 운영하지 않습니다.'
                                WHEN 304 THEN N'해당 시간대의 마감시간이 지났습니다.'
                                ELSE N'' END)
    FROM
    (
        SELECT c.*
             , ReasonCode = CASE WHEN @ReservationDate < c.Today                                   THEN 300
                                 WHEN c.ReqDow = 6                                                  THEN 301
                                 WHEN c.ReqHoliday IS NOT NULL                                      THEN 302
                                 WHEN c.SlotOpen = 0                                                THEN 303
                                 WHEN c.Cutoff IS NOT NULL
                                  AND c.NowTime >= CONVERT(TIME(7), c.Cutoff)                       THEN 304
                                 ELSE 0 END
        FROM
        (
            SELECT b.*
                 , TodayBiz   = CASE WHEN b.TodayDow <> 6 AND b.TodayHoliday IS NULL THEN 1 ELSE 0 END
                 , WithinHours= CASE WHEN b.NowTime >= CONVERT(TIME(7), '09:00:00')
                                      AND b.NowTime <  CONVERT(TIME(7), '18:00:00') THEN 1 ELSE 0 END
                 , ReqBiz     = CASE WHEN b.ReqDow <> 6 AND b.ReqHoliday IS NULL THEN 1 ELSE 0 END
                 , SlotOpen   = CASE WHEN b.ReqDow = 5 AND @TimeSlot = 'PM' THEN 0 ELSE 1 END
                 , Cutoff     = CASE WHEN @ReservationDate = b.Today THEN b.RawCutoff ELSE NULL END
            FROM
            (
                SELECT
                      Today        = CONVERT(DATE, @ServerTime)
                    , NowTime      = CONVERT(TIME(7), @ServerTime)
                    , TodayDow     = DATEDIFF(DAY, 0, CONVERT(DATE, @ServerTime)) % 7
                    , ReqDow       = DATEDIFF(DAY, 0, @ReservationDate) % 7
                    , TodayHoliday = (SELECT TOP (1) h.[HolidayName] FROM [dbo].[MST_HOLIDAYS] h
                                       WHERE h.[HolidayDate] = CONVERT(DATE, @ServerTime) AND h.[Active] = 1)
                    , ReqHoliday   = (SELECT TOP (1) h.[HolidayName] FROM [dbo].[MST_HOLIDAYS] h
                                       WHERE h.[HolidayDate] = @ReservationDate AND h.[Active] = 1)
                    , RawCutoff    = CASE WHEN @CutoffType = 'NORMAL'    AND @TimeSlot = 'AM' THEN CONVERT(TIME(0), '10:00:00')
                                          WHEN @CutoffType = 'NORMAL'    AND @TimeSlot = 'PM' THEN CONVERT(TIME(0), '15:00:00')
                                          WHEN @CutoffType = 'RECEPTION' AND @TimeSlot = 'AM' THEN CONVERT(TIME(0), '11:00:00')
                                          WHEN @CutoffType = 'RECEPTION' AND @TimeSlot = 'PM' THEN CONVERT(TIME(0), '16:00:00')
                                          ELSE NULL END
            ) b
        ) c
    ) d
);
GO
```

중첩 derived table을 쓴 이유는 Inline TVF가 단일 `SELECT` 여야 하고, `CASE` 결과를 다음 단계에서 재사용해야 하기 때문이다. CTE도 문법상 가능하지만 derived table이 어느 버전에서도 확실히 동작한다.

`[X]` **`CutoffTime` 은 `SlotOpen = 0` 이면 `NULL` 이다.** `RawCutoff` 가 요일을 보지 않으므로, 초안은 토요일 PM 요청에 `15:00`(NORMAL)·`16:00`(RECEPTION)을 반환했다. `00` §3장은 토요일 오후의 마감을 **"해당 없음"** 으로 확정했으므로 존재하지 않는 시각이다. `ReasonCode` 는 `SlotOpen=0` 에 의해 `303` 이 먼저 잡혀 판정 자체는 옳았지만, `USP_HC_SELECT_예약가능정보` RS2의 `CutoffTime` 을 통해 화면에 **없는 마감시각**이 표시된다. `05` §6.1.3의 *"마감 적용 시각이 있을 때만 반환"* 에도 어긋난다.

- [ ] **Step 4: GREEN 실행**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i deploy/03_Functions.sql -o artifacts/logs/03_functions.log
echo "exit=$?"
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/03_Rule_Tests.sql -o artifacts/logs/test_03.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_03.log | grep -E '^(PASS|FAIL)'
```

Expected: 배포 exit 0, 테스트 exit 0, `RUL-T01`~`RUL-T12` 12건 PASS.

- [ ] **Step 5: 일정 경계 9건 추가 (`RUL-D01`~`RUL-D09`)**

`UFN_HC_일정확인(@ServerTime, @ReservationDate, @TimeSlot, @CutoffType)` 의 `ReasonCode` 를 본다. 기준시각은 업무시간 안의 고정값 `2026-11-16 10:30:00` 을 쓴다 — 실행 시각에 의존하면 `309` 가 먼저 걸려 일정 판정에 도달하지 못한다.

| Test ID | `@ReservationDate` | `@TimeSlot` | 기대 `ReasonCode` | 근거 |
|---|---|---|---:|---|
| `RUL-D01` | `2020-01-06` (과거 월) | `AM` | `300` | 과거일 |
| `RUL-D02` | `2020-01-05` (과거 일) | `AM` | `300` | **과거가 일요일보다 우선** |
| `RUL-D03` | `2026-11-22` (미래 일) | `AM` | `301` | 일요일 |
| `RUL-D04` | `2026-12-25` (활성 평일 휴무일) | `AM` | `302` | 휴무일 |
| `RUL-D05` | `2026-12-26` (활성 토요일 휴무일) | `AM` | `302` | 휴무일이 토요일 규칙보다 우선 |
| `RUL-D06` | `2026-11-21` (미래 토) | `AM` | `0` | 토요일 오전은 운영 |
| `RUL-D07` | `2026-11-21` (미래 토) | `PM` | `303` | 토요일 오후는 미운영 |
| `RUL-D08` | `2026-11-17` (미래 화) | `AM` | `0` | 정상 평일 |

요일은 실측 확인했다: `2020-01-05` 일 · `2020-01-06` 월 · `2026-11-21` 토 · `2026-11-22` 일 · `2026-12-25` 금 · `2026-12-26` 토.

각 행은 아래 한 덩어리다 — 인자와 기대값만 표에서 가져온다.

```sql
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:30:00'), '2020-01-06', 'AM', 'NONE')) = 300)
    PRINT 'PASS RUL-D01 과거 평일 300';
ELSE BEGIN PRINT 'FAIL RUL-D01'; SET @Fail += 1; END
```

`RUL-D09` 는 `SET DATEFIRST 1` 과 `SET DATEFIRST 7` 양쪽에서 `RUL-D03` 을 재실행해 동일 결과를 확인한다.

```sql
SET DATEFIRST 1;
DECLARE @Sun1 INT = (SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:00:00'), CONVERT(DATE,'2026-11-22'), 'AM', 'NONE'));
SET DATEFIRST 7;
DECLARE @Sun7 INT = (SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:00:00'), CONVERT(DATE,'2026-11-22'), 'AM', 'NONE'));
IF @Sun1 = 301 AND @Sun7 = 301
    PRINT 'PASS RUL-D09 DATEFIRST 비종속 확인';
ELSE BEGIN PRINT 'FAIL RUL-D09 DATEFIRST 종속성 발견'; SET @Fail += 1; END
```

`2026-11-22` 가 일요일인지 먼저 확인한다: `DATEDIFF(DAY,0,'2026-11-22')%7` 이 `6` 이어야 한다.

- [ ] **Step 6: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/03_Functions.sql database/tests/03_Rule_Tests.sql
git commit -m "feat(phase4): UFN_HC_일정확인 구현 및 시간·일정 경계 테스트 21건"
```

**회귀시험:** `tests/03_Rule_Tests.sql`

**로그 경로:** `artifacts/logs/test_03.log`

**Rollback/Cleanup:** `CREATE OR ALTER` 이므로 재실행으로 덮어쓴다.

**완료조건:** RED에서 `Msg 4121/208` 관측, GREEN에서 스펙 §45.2 의 `RUL-T*`·`RUL-D*` 전건 PASS.

---

## Task T12: `[dbo].[UFN_HC_검진대상확인]`

**목적:** 예약일 기준 TGT 판정을 1행으로 반환한다.

**관련 Baseline 위치:** `05` §6.2, `00` §7.1 (TGT-01~05), 스펙 §17.

**선행조건:** `T11` 완료.

**Files:**
- Modify: `deploy/03_Functions.sql` (함수 추가)
- Modify: `tests/03_Rule_Tests.sql` (`RUL-G01`~`G08` 추가)

**Interfaces:**
- Produces: `[dbo].[UFN_HC_검진대상확인](@PatientId BIGINT, @ReservationDate DATE)` → Patient 존재 시 1행 `(Eligible BIT, Age INT, LastCheckupDate DATE, ReasonCode INT, ReasonMessage NVARCHAR(300))`, 없으면 0행

**금지사항:** 완료이력 범위를 `CompletionDate < @ReservationDate` 외로 넓히지 않는다. Work 상태(RSV/RCP/CNL)를 완료이력으로 쓰지 않는다.

- [ ] **Step 1: RED — `RUL-G01`~`G08` 을 먼저 작성**

스펙 §35.3 표대로 8건. 예시:

```sql
DECLARE @Ref DATE = '2026-10-01';
DECLARE @P BIGINT;

SELECT @P = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T001';   -- 만 19세
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_검진대상확인](@P, @Ref)) = 400)
    PRINT 'PASS RUL-G01 만 19세 400 UnderAge';
ELSE BEGIN PRINT 'FAIL RUL-G01'; SET @Fail += 1; END

SELECT @P = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T002';   -- 만 20세
IF ((SELECT Eligible FROM [dbo].[UFN_HC_검진대상확인](@P, @Ref)) = 1)
    PRINT 'PASS RUL-G02 만 20세 대상';
ELSE BEGIN PRINT 'FAIL RUL-G02'; SET @Fail += 1; END

-- RUL-G03 ~ RUL-G07  완료이력 판정 (스펙 §35.3)
--   T005(완료이력 없음) / T016(2025-05-01, 1년차) / T004(2024-05-01, 2년차) 를 쓴다.
--   기준 예약일은 §15.5 의 Rule Test 기준일 '2026-10-01' 이다.
DECLARE @Pg BIGINT;

SELECT @Pg = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T005';
IF ((SELECT Eligible FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2026-10-01')) = 1
    AND (SELECT LastCheckupDate FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2026-10-01')) IS NULL)
    PRINT 'PASS RUL-G03 완료이력 없음 → Eligible=1, LastCheckupDate NULL';
ELSE BEGIN PRINT 'FAIL RUL-G03'; SET @Fail += 1; END

SELECT @Pg = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T016';
IF ((SELECT Eligible FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2026-10-01')) = 0
    AND (SELECT ReasonCode FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2026-10-01')) = 401)
    PRINT 'PASS RUL-G04 1년차 완료이력 → 401 NotDue';
ELSE BEGIN PRINT 'FAIL RUL-G04'; SET @Fail += 1; END

SELECT @Pg = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T004';
IF ((SELECT Eligible FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2026-10-01')) = 1)
    PRINT 'PASS RUL-G05 2년차 완료이력 → 대상';
ELSE BEGIN PRINT 'FAIL RUL-G05'; SET @Fail += 1; END

-- RUL-G06 완료일 = 예약일 당일 → 완료이력으로 쓰지 않는다 (T016 의 완료일을 예약일로 준다)
SELECT @Pg = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T016';
IF ((SELECT Eligible FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2025-05-01')) = 1)
    PRINT 'PASS RUL-G06 완료일 당일은 완료이력으로 사용하지 않는다';
ELSE BEGIN PRINT 'FAIL RUL-G06'; SET @Fail += 1; END

-- RUL-G07 완료일 > 예약일 → 완료이력으로 쓰지 않는다
IF ((SELECT Eligible FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2025-04-30')) = 1)
    PRINT 'PASS RUL-G07 예약일 이후의 완료일은 완료이력으로 사용하지 않는다';
ELSE BEGIN PRINT 'FAIL RUL-G07'; SET @Fail += 1; END

-- RUL-G08 존재하지 않는 PatientId → 0행
IF ((SELECT COUNT(*) FROM [dbo].[UFN_HC_검진대상확인](-1, @Ref)) = 0)
    PRINT 'PASS RUL-G08 미존재 Patient 0행';
ELSE BEGIN PRINT 'FAIL RUL-G08'; SET @Fail += 1; END
```

RED 실행 → `Msg 4121/208`.

- [ ] **Step 2: 구현**

```sql
CREATE OR ALTER FUNCTION [dbo].[UFN_HC_검진대상확인]
(
    @PatientId       BIGINT,
    @ReservationDate DATE
)
RETURNS TABLE
AS
RETURN
(
    SELECT
          Eligible        = CONVERT(BIT, CASE WHEN x.ReasonCode = 0 THEN 1 ELSE 0 END)
        , Age             = CONVERT(INT, x.Age)
        , LastCheckupDate = CONVERT(DATE, x.LastCheckupDate)
        , ReasonCode      = CONVERT(INT, x.ReasonCode)
        , ReasonMessage   = CONVERT(NVARCHAR(300),
                              CASE x.ReasonCode
                                  WHEN 400 THEN N'예약일 기준 만 20세 미만으로 검진 대상이 아닙니다.'
                                  WHEN 401 THEN N'일반건강검진 2년 주기가 도래하지 않았습니다.'
                                  ELSE N'' END)
    FROM
    (
        SELECT a.Age, a.LastCheckupDate
             , ReasonCode = CASE WHEN a.Age < 20 THEN 400
                                 WHEN a.LastCheckupDate IS NOT NULL
                                  AND (YEAR(@ReservationDate) - YEAR(a.LastCheckupDate)) < 2 THEN 401
                                 ELSE 0 END
        FROM
        (
            SELECT
                  Age = DATEDIFF(YEAR, p.[Birthday_D], @ReservationDate)
                        - CASE WHEN (MONTH(@ReservationDate) * 100 + DAY(@ReservationDate))
                                  < (MONTH(p.[Birthday_D])  * 100 + DAY(p.[Birthday_D])) THEN 1 ELSE 0 END
                , LastCheckupDate = (SELECT TOP (1) h.[CompletionDate]
                                       FROM [dbo].[HIS_GENERAL_CHECKUP_COMPLETIONS] h
                                      WHERE h.[PatientId] = @PatientId
                                        AND h.[CompletionDate] < @ReservationDate
                                      ORDER BY h.[CompletionDate] DESC)
            FROM (SELECT [Birthday_D] = CONVERT(DATE, i.[Birthday], 112)
                    FROM [dbo].[INFO_PATIENTS] i WHERE i.[PatientId] = @PatientId) p
        ) a
    ) x
);
GO
```

- [ ] **Step 3: GREEN 실행**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/03_Functions.sql
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/03_Rule_Tests.sql -o artifacts/logs/test_03.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_03.log | grep -cE '^PASS'
```

Expected: exit 0, PASS 개수 = 21 + 8 = **29**.

- [ ] **Step 4: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/03_Functions.sql database/tests/03_Rule_Tests.sql
git commit -m "feat(phase4): UFN_HC_검진대상확인 구현 및 TGT 경계 테스트 8건"
```

**완료조건:** RED 관측 + 스펙 §45.2 의 `RUL-G*`·`RUL-N*` 전건 PASS.

---

## Task T13: `[dbo].[UFN_HC_국가검사구성]`

**목적:** TGT 대상의 NEX 구성을 8~11행으로 반환한다.

**관련 Baseline 위치:** `05` §6.3, `00` §7.2, 스펙 §17.2.

**선행조건:** `T12` 완료 (이 함수가 `UFN_HC_검진대상확인` 을 호출한다).

**Files:**
- Modify: `deploy/03_Functions.sql`, `tests/03_Rule_Tests.sql`

**Interfaces:**
- Consumes: `[dbo].[UFN_HC_검진대상확인]`
- Produces: `[dbo].[UFN_HC_국가검사구성](@PatientId BIGINT, @ReservationDate DATE)` → `(ExamCode VARCHAR(10), ExamName NVARCHAR(100), ExamType VARCHAR(12), RuleCode VARCHAR(10))`, `ExamCode ASC`

**금지사항:** TGT 비대상에게 행을 반환하지 않는다. 조건부 술어를 `00` §7.2.2에서 바꾸지 않는다.

- [ ] **Step 1: RED — `RUL-N01`~`N12` 작성 (스펙 §35.4)**

핵심 3건 예시:

```sql
SELECT @P = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T001';   -- 만 19세, 비대상
IF ((SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@P, @Ref)) = 0)
    PRINT 'PASS RUL-N01 TGT 비대상 0행';
ELSE BEGIN PRINT 'FAIL RUL-N01'; SET @Fail += 1; END

SELECT @P = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T002';   -- 만 20세, 조건부 0종
IF ((SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@P, @Ref)) = 8)
    PRINT 'PASS RUL-N02 기본 8행';
ELSE BEGIN PRINT 'FAIL RUL-N02'; SET @Fail += 1; END

SELECT @P = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T010';   -- 여 만 56세
IF ((SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@P, @Ref)) = 11)
    PRINT 'PASS RUL-N10 조건부 3종 동시 → 11행';
ELSE BEGIN PRINT 'FAIL RUL-N10'; SET @Fail += 1; END

-- RUL-N03 ~ N09, N12  조건부 NEX 5종 술어 (스펙 §17.2)
--   NEX-02 EX009  남 Age>=24 AND (Age-24)%4=0  /  여 Age>=40 AND (Age-40)%4=0
--   NEX-03 EX010  Age=40 AND 제외행 없음      NEX-04 EX011  Age=56
--   NEX-05 EX012  Gender='F' AND Age IN (54,60,66)   NEX-06 EX013  Age IN (56,66)

-- RUL-N03  남 23(T003) / 24(T004) / 28(T005) → EX009 없음 / 있음 / 있음
IF NOT EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T003'), @Ref) WHERE ExamCode='EX009') AND EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T004'), @Ref) WHERE ExamCode='EX009') AND EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T005'), @Ref) WHERE ExamCode='EX009')
    PRINT 'PASS RUL-N03 남 23/24/28 → EX009 없음/있음/있음';
ELSE BEGIN PRINT 'FAIL RUL-N03'; SET @Fail += 1; END

-- RUL-N04  여 39(T006) / 40(T007) / 44(T018) → EX009 없음 / 있음 / 있음
IF NOT EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T006'), @Ref) WHERE ExamCode='EX009') AND EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T007'), @Ref) WHERE ExamCode='EX009') AND EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T018'), @Ref) WHERE ExamCode='EX009')
    PRINT 'PASS RUL-N04 여 39/40/44 → EX009 없음/있음/있음';
ELSE BEGIN PRINT 'FAIL RUL-N04'; SET @Fail += 1; END

-- RUL-N05  만 40세, 제외행 없음(T007) → EX010 있음 / 제외행 있음(T008) → EX010 없음
IF EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T007'), @Ref) WHERE ExamCode='EX010') AND NOT EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T008'), @Ref) WHERE ExamCode='EX010')
    PRINT 'PASS RUL-N05 만 40세 — 제외행이 EX010 을 제거한다';
ELSE BEGIN PRINT 'FAIL RUL-N05'; SET @Fail += 1; END

-- RUL-N06  만 55(T009) / 56(T010) → EX011 없음 / 있음
IF NOT EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T009'), @Ref) WHERE ExamCode='EX011') AND EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T010'), @Ref) WHERE ExamCode='EX011')
    PRINT 'PASS RUL-N06 만 55/56 → EX011 없음/있음';
ELSE BEGIN PRINT 'FAIL RUL-N06'; SET @Fail += 1; END

-- RUL-N07  여 54(T011) / 60(T012) / 66(T013) → EX012 전부 있음
IF EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T011'), @Ref) WHERE ExamCode='EX012') AND EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T012'), @Ref) WHERE ExamCode='EX012') AND EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T013'), @Ref) WHERE ExamCode='EX012')
    PRINT 'PASS RUL-N07 여 54/60/66 → EX012 있음';
ELSE BEGIN PRINT 'FAIL RUL-N07'; SET @Fail += 1; END

-- RUL-N08  남 54(T019) → EX012 없음 (NEX-05 는 여성 전용)
IF NOT EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T019'), @Ref) WHERE ExamCode='EX012')
    PRINT 'PASS RUL-N08 남 54세는 EX012 비대상';
ELSE BEGIN PRINT 'FAIL RUL-N08 남성에게 EX012 가 나왔다'; SET @Fail += 1; END

-- RUL-N09  만 56(T010) / 66(T013) → EX013 있음
IF EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T010'), @Ref) WHERE ExamCode='EX013') AND EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성]((SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo]='T013'), @Ref) WHERE ExamCode='EX013')
    PRINT 'PASS RUL-N09 만 56/66 → EX013 있음';
ELSE BEGIN PRINT 'FAIL RUL-N09'; SET @Fail += 1; END

-- RUL-N12  정렬 = ExamCode ASC. 반환 순서와 정렬한 순서를 행번호로 맞대어 본다.
SELECT @P = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T010';
IF NOT EXISTS (
    SELECT 1
    FROM (SELECT ExamCode, rn = ROW_NUMBER() OVER (ORDER BY (SELECT NULL))
            FROM [dbo].[UFN_HC_국가검사구성](@P, @Ref)) a
    JOIN (SELECT ExamCode, rn = ROW_NUMBER() OVER (ORDER BY ExamCode)
            FROM [dbo].[UFN_HC_국가검사구성](@P, @Ref)) b
      ON a.rn = b.rn AND a.ExamCode <> b.ExamCode)
    PRINT 'PASS RUL-N12 ExamCode ASC 정렬';
ELSE BEGIN PRINT 'FAIL RUL-N12 정렬 위반'; SET @Fail += 1; END

-- RUL-N11 모든 TGT 대상 프로필의 행수가 8~11 범위인지 전수 확인
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[INFO_PATIENTS] p
    CROSS APPLY (SELECT Cnt = COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](p.[PatientId], @Ref)) n
    CROSS APPLY [dbo].[UFN_HC_검진대상확인](p.[PatientId], @Ref) g
    WHERE g.Eligible = 1 AND (n.Cnt < 8 OR n.Cnt > 11))
    PRINT 'PASS RUL-N11 전 대상자 NEX 행수 8~11 범위';
ELSE BEGIN PRINT 'FAIL RUL-N11 범위 벗어난 대상자 존재'; SET @Fail += 1; END
```

- [ ] **Step 2: 구현**

```sql
CREATE OR ALTER FUNCTION [dbo].[UFN_HC_국가검사구성]
(
    @PatientId       BIGINT,
    @ReservationDate DATE
)
RETURNS TABLE
AS
RETURN
(
    SELECT
          ExamCode = CONVERT(VARCHAR(10),  m.[ExamItemCode])
        , ExamName = CONVERT(NVARCHAR(100), m.[ExamItemName])
        , ExamType = CONVERT(VARCHAR(12), CASE WHEN m.[NexRuleCode] = 'NEX-01' THEN 'BASIC' ELSE 'CONDITIONAL' END)
        , RuleCode = CONVERT(VARCHAR(10),  m.[NexRuleCode])
    FROM [dbo].[MST_EXAM_ITEMS] m
    CROSS JOIN
    (
        SELECT g.Eligible, g.Age, i.[Gender]
        FROM [dbo].[INFO_PATIENTS] i
        CROSS APPLY [dbo].[UFN_HC_검진대상확인](i.[PatientId], @ReservationDate) g
        WHERE i.[PatientId] = @PatientId
    ) t
    WHERE m.[NexRuleCode] IS NOT NULL
      AND t.Eligible = 1
      AND
      (
            m.[NexRuleCode] = 'NEX-01'
        OR (m.[NexRuleCode] = 'NEX-02' AND
            (  (t.[Gender] = 'M' AND t.Age >= 24 AND (t.Age - 24) % 4 = 0)
            OR (t.[Gender] = 'F' AND t.Age >= 40 AND (t.Age - 40) % 4 = 0) ))
        OR (m.[NexRuleCode] = 'NEX-03' AND t.Age = 40
            AND NOT EXISTS (SELECT 1 FROM [dbo].[INFO_PATIENT_EXAM_EXCLUSIONS] x
                             WHERE x.[PatientId] = @PatientId AND x.[ExamItemCode] = 'EX010'))
        OR (m.[NexRuleCode] = 'NEX-04' AND t.Age = 56)
        OR (m.[NexRuleCode] = 'NEX-05' AND t.[Gender] = 'F' AND t.Age IN (54, 60, 66))
        OR (m.[NexRuleCode] = 'NEX-06' AND t.Age IN (56, 66))
      )
);
GO
```

정렬은 Inline TVF에서 `ORDER BY` 를 쓸 수 없으므로 **호출자가 `ORDER BY ExamCode ASC` 를 붙인다.** SP의 `SELECT` 에 반드시 포함한다.

- [ ] **Step 3: GREEN 실행 + 정렬 확인**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/03_Functions.sql
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/03_Rule_Tests.sql -o artifacts/logs/test_03.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_03.log | grep -cE '^PASS'
```

Expected: exit 0, PASS 개수 = 29 + 12 = **41**.

- [ ] **Step 4: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/03_Functions.sql database/tests/03_Rule_Tests.sql
git commit -m "feat(phase4): UFN_HC_국가검사구성 구현 및 NEX 경계 테스트 12건"
```

**완료조건:** 스펙 §45.2 의 `RUL-N01`~`RUL-N12` 전건 PASS. 특히 `RUL-N02` 8행, `RUL-N10` 11행, `RUL-N11` 전수 8~11 범위.

---

## Task T14: `[dbo].[UFN_HC_추가검사확인]`

**목적:** AEX 7종의 선택 가능 여부와 유효 선택값을 정확히 7행으로 반환한다.

**관련 Baseline 위치:** `05` §6.4, `00` §7.3, 스펙 §17.3.

**선행조건:** `T13` 완료.

**Files:**
- Modify: `deploy/03_Functions.sql`, `tests/03_Rule_Tests.sql`

**Interfaces:**
- Consumes: `[dbo].[UFN_HC_국가검사구성]`, `[dbo].[UFN_HC_검진대상확인]`
- Produces: `[dbo].[UFN_HC_추가검사확인](@PatientId, @ReservationDate, @WorkId, @UseSavedExams, @AexOpt01Selected … @AexOpt07Selected)` → 7행 8컬럼

**금지사항:** 요청 값을 `VALUES` 행 생성자 외의 방법으로 받지 않는다. 선택하지 않은 무효 항목이 저장을 차단하게 만들지 않는다.

- [ ] **Step 1: RED — `RUL-A01`~`A10` 작성 (스펙 §35.5)**

```sql
SELECT @P = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T015';   -- 남 만 46세, 대상
IF ((SELECT COUNT(*) FROM [dbo].[UFN_HC_추가검사확인](@P, @Ref, NULL, 0, 0,0,0,0,0,0,0)) = 7)
    PRINT 'PASS RUL-A01 정확히 7행';
ELSE BEGIN PRINT 'FAIL RUL-A01'; SET @Fail += 1; END

-- 남성이 OPT03(유방초음파) 요청 → 411
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_추가검사확인](@P, @Ref, NULL, 0, 0,0,1,0,0,0,0)
      WHERE OptionCode = 'OPT03') = 411)
    PRINT 'PASS RUL-A03 남성 OPT03 411 WrongGender';
ELSE BEGIN PRINT 'FAIL RUL-A03'; SET @Fail += 1; END

-- RUL-A02  전부 미선택 → Selected·Requested 전부 0
IF NOT EXISTS (SELECT 1 FROM [dbo].[UFN_HC_추가검사확인](@P, @Ref, NULL, 0, 0,0,0,0,0,0,0)
                WHERE Selected = 1 OR Requested = 1)
    PRINT 'PASS RUL-A02 전부 미선택 → Selected/Requested 전부 0';
ELSE BEGIN PRINT 'FAIL RUL-A02'; SET @Fail += 1; END

-- RUL-A04  여성(T010) + OPT05(PSA, 남성 전용) → 411
SELECT @P = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T010';
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_추가검사확인](@P, @Ref, NULL, 0, 0,0,0,0,1,0,0)
      WHERE OptionCode = 'OPT05') = 411)
    PRINT 'PASS RUL-A04 여성 OPT05 411 WrongGender';
ELSE BEGIN PRINT 'FAIL RUL-A04'; SET @Fail += 1; END

-- RUL-A05  남성(T015) + OPT07(HPV, 여성 전용) → 411
SELECT @P = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T015';
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_추가검사확인](@P, @Ref, NULL, 0, 0,0,0,0,0,0,1)
      WHERE OptionCode = 'OPT07') = 411)
    PRINT 'PASS RUL-A05 남성 OPT07 411 WrongGender';
ELSE BEGIN PRINT 'FAIL RUL-A05'; SET @Fail += 1; END

-- RUL-A06  AdditionalActive=0 인 항목 요청 → 410
--   Seed 는 7종 전부 Active=1 이므로(SED-011) 이 시험만 잠시 하나를 끄고 즉시 되돌린다.
--   되돌리지 않으면 SED-011 과 G07 이 뒤에서 FAIL 한다.
UPDATE [dbo].[MST_EXAM_ITEMS] SET [AdditionalActive] = 0 WHERE [ExamItemCode] = 'OPT06';
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_추가검사확인](@P, @Ref, NULL, 0, 0,0,0,0,0,1,0)
      WHERE OptionCode = 'OPT06') = 410)
    PRINT 'PASS RUL-A06 비활성 항목 요청 410 ExamInactive';
ELSE BEGIN PRINT 'FAIL RUL-A06'; SET @Fail += 1; END
UPDATE [dbo].[MST_EXAM_ITEMS] SET [AdditionalActive] = 1 WHERE [ExamItemCode] = 'OPT06';

-- RUL-A08  TGT 비대상(T001) + @UseSavedExams=0 → 7행 전부 CanSelect=0, Selected=0
SELECT @P = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T001';
IF ((SELECT COUNT(*) FROM [dbo].[UFN_HC_추가검사확인](@P, @Ref, NULL, 0, 1,1,1,1,1,1,1)) = 7
    AND NOT EXISTS (SELECT 1 FROM [dbo].[UFN_HC_추가검사확인](@P, @Ref, NULL, 0, 1,1,1,1,1,1,1)
                     WHERE CanSelect = 1 OR Selected = 1))
    PRINT 'PASS RUL-A08 TGT 비대상 → 7행 전부 CanSelect=0, Selected=0';
ELSE BEGIN PRINT 'FAIL RUL-A08'; SET @Fail += 1; END

-- RUL-A09  @UseSavedExams=1 → 산출 NEX 가 아니라 저장 NEX 기준으로 중복 판정한다
--   T011(여 만 54세)의 RCP Work 는 저장 NEX 에 EX012 를 갖는다(T14b). 그래서 OPT04 는 412 다.
-- [X] T014 는 **남성** 만 56세다. NEX-05(EX012) 술어가 Gender='F' 를 요구하므로
--     T014 의 저장 NEX 에는 EX012 가 없고 OPT04 는 412 가 아니라 0 이 나온다 — 시험이 성립하지 않는다.
--     412 는 EX012 를 가진 프로필(여 54·60·66세)에서만 관측할 수 있다(스펙 §17.2a).
--     → T011(여 만 54세)의 RCP Work 를 쓴다. tests/00b 가 만든다.
DECLARE @Pa BIGINT = (SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T011');
DECLARE @Wa BIGINT = (SELECT TOP (1) [WorkId] FROM [dbo].[INFO_CHECKUP_WORKS]
                       WHERE [PatientId] = @Pa AND [StatusCode] = 'RCP' ORDER BY [WorkId]);
IF @Wa IS NULL
BEGIN PRINT 'FAIL RUL-A09 사전조건 — T011 의 RCP Work 가 없다 (tests/00b 를 먼저 실행했는가)'; SET @Fail += 1; END
ELSE IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_추가검사확인](@Pa, @Ref, @Wa, 1, 0,0,0,1,0,0,0)
           WHERE OptionCode = 'OPT04') = 412)
    PRINT 'PASS RUL-A09 @UseSavedExams=1 은 저장 NEX 기준으로 중복 판정';
ELSE BEGIN PRINT 'FAIL RUL-A09'; SET @Fail += 1; END

-- RUL-A10  선택하지 않은 무효 항목은 사유만 표시하고 저장을 막지 않는다
SELECT @P = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T015';   -- 남 → OPT03 무효
IF EXISTS (SELECT 1 FROM [dbo].[UFN_HC_추가검사확인](@P, @Ref, NULL, 0, 1,0,0,0,0,0,0)
            WHERE OptionCode = 'OPT03' AND CanSelect = 0 AND Requested = 0)
    PRINT 'PASS RUL-A10 미선택 무효 항목은 사유만 표시된다';
ELSE BEGIN PRINT 'FAIL RUL-A10'; SET @Fail += 1; END

-- NEX 에 EX012 가 있는 여 만 54세(T011) 가 OPT04 요청 → 412
SELECT @P = [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T011';
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_추가검사확인](@P, @Ref, NULL, 0, 0,0,0,1,0,0,0)
      WHERE OptionCode = 'OPT04') = 412)
    PRINT 'PASS RUL-A07 NEX EX012 중복 412 ExamDuplicate';
ELSE BEGIN PRINT 'FAIL RUL-A07'; SET @Fail += 1; END
```

- [ ] **Step 2: 구현**

```sql
CREATE OR ALTER FUNCTION [dbo].[UFN_HC_추가검사확인]
(
    @PatientId        BIGINT,
    @ReservationDate  DATE,
    @WorkId           BIGINT,
    @UseSavedExams    BIT,
    @AexOpt01Selected BIT, @AexOpt02Selected BIT, @AexOpt03Selected BIT,
    @AexOpt04Selected BIT, @AexOpt05Selected BIT, @AexOpt06Selected BIT,
    @AexOpt07Selected BIT
)
RETURNS TABLE
AS
RETURN
(
    SELECT
          OptionCode    = CONVERT(VARCHAR(10),  b.[AdditionalExamCode])
        , ExamCode      = CONVERT(VARCHAR(10),  b.[ExamItemCode])
        , ExamName      = CONVERT(NVARCHAR(100), b.[ExamItemName])
        , Requested     = CONVERT(BIT, b.Requested)
        , Selected      = CONVERT(BIT, CASE WHEN b.Requested = 1 AND b.ReasonCode = 0 THEN 1 ELSE 0 END)
        , CanSelect     = CONVERT(BIT, CASE WHEN b.ReasonCode = 0 THEN 1 ELSE 0 END)
        , ReasonCode    = CONVERT(INT, b.ReasonCode)
        , ReasonMessage = CONVERT(NVARCHAR(300),
                            CASE b.ReasonCode
                                WHEN 400 THEN N'예약일 기준 만 20세 미만으로 검진 대상이 아닙니다.'
                                WHEN 401 THEN N'일반건강검진 2년 주기가 도래하지 않았습니다.'
                                WHEN 410 THEN N'현재 사용할 수 없는 추가검사입니다.'
                                WHEN 411 THEN N'성별 조건을 충족하지 않는 추가검사입니다.'
                                WHEN 412 THEN N'일반건강검진에 포함된 검사입니다.'
                                ELSE N'' END)
    FROM
    (
        SELECT m.[AdditionalExamCode], m.[ExamItemCode], m.[ExamItemName], r.Requested
             , ReasonCode =
                 CASE
                     WHEN @UseSavedExams = 0 AND t.Eligible = 0 THEN t.ReasonCode      -- 400 / 401
                     WHEN m.[AdditionalActive] = 0                                THEN 410
                     WHEN m.[AdditionalGenderCode] <> 'A'
                      AND m.[AdditionalGenderCode] <> t.[Gender]                  THEN 411
                     WHEN EXISTS
                          (
                              SELECT 1 FROM
                              (
                                  SELECT n.ExamCode FROM [dbo].[UFN_HC_국가검사구성](@PatientId, @ReservationDate) n
                                   WHERE @UseSavedExams = 0
                                  UNION ALL
                                  SELECT d.[ExamItemCode] FROM [dbo].[INFO_CHECKUP_WORK_EXAMS] d
                                   WHERE @UseSavedExams = 1 AND d.[WorkId] = @WorkId AND d.[ExamSourceCode] = 'NEX'
                              ) nx WHERE nx.ExamCode = m.[ExamItemCode]
                          )                                                       THEN 412
                     ELSE 0
                 END
        FROM [dbo].[MST_EXAM_ITEMS] m
        CROSS JOIN
        (
            SELECT i.[Gender]
                 , Eligible   = ISNULL(g.Eligible, CONVERT(BIT,0))
                 , ReasonCode = ISNULL(g.ReasonCode, 400)
            FROM [dbo].[INFO_PATIENTS] i
            OUTER APPLY [dbo].[UFN_HC_검진대상확인](i.[PatientId], @ReservationDate) g
            WHERE i.[PatientId] = @PatientId
        ) t
        JOIN
        (
            VALUES ('OPT01', @AexOpt01Selected), ('OPT02', @AexOpt02Selected), ('OPT03', @AexOpt03Selected),
                   ('OPT04', @AexOpt04Selected), ('OPT05', @AexOpt05Selected), ('OPT06', @AexOpt06Selected),
                   ('OPT07', @AexOpt07Selected)
        ) r (OptionCode, Requested) ON r.OptionCode = m.[AdditionalExamCode]
        WHERE m.[AdditionalExamCode] IS NOT NULL
    ) b
);
GO
```

정렬(`OptionCode ASC`)은 호출자가 붙인다.

- [ ] **Step 3: GREEN 실행**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/03_Functions.sql
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/03_Rule_Tests.sql -o artifacts/logs/test_03.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_03.log | grep -cE '^PASS'
```

Expected: exit 0, PASS 개수 = 41 + 10 = **51**.

- [ ] **Step 4: 회귀 — 스키마 테스트의 `SCH-013` 이 이제 PASS 인지 확인**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/01_Schema_Tests.sql -o artifacts/logs/test_01.log
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_01.log | grep -E 'SCH-013|SCH-014'
```

Expected: `PASS SCH-013 Inline TVF 4개`, `FAIL SCH-014` (SP는 아직 0개 — 정상).

- [ ] **Step 5: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/03_Functions.sql database/tests/03_Rule_Tests.sql
git commit -m "feat(phase4): UFN_HC_추가검사확인 구현 및 AEX 경계 테스트 10건"
```

**회귀시험:** `tests/01_Schema_Tests.sql` + `tests/03_Rule_Tests.sql`

**로그 경로:** `artifacts/logs/test_03.log`

**완료조건:** 스펙 §45.2 의 `RUL` 전건 PASS + `SCH-013` PASS.

---

## Task T14b: `tests/00b_Test_Harness_RCP.sql` — RCP 상태 Work Fixture

**목적:** `T29`(`USP_HC_UPDATE_접수추가검사`)가 필요로 하는 `RCP` 상태 Work를 **업무시간과 무관하게** 만든다.

**관련 Baseline 위치:** 스펙 §15.2 (Fixture 생성 방식), §33.2a.

**선행조건:** `T14` 완료 — `UFN_HC_국가검사구성` 이 존재해야 한다.

**Files:**
- Create: `tests/00b_Test_Harness_RCP.sql`

**Interfaces:**
- Consumes: `[dbo].[UFN_HC_국가검사구성]`
- Produces: `ChartNo='T014'` 의 `RCP` Work 1건 (NEX 11행 + AEX `OPT01` 1행), `ChartNo='T011'` 의 `RCP` Work 1건 (NEX 11행 — **`EX012` 포함**)

**금지사항:** SP 경로로 만들지 않는다 — `USP_HC_UPDATE_접수완료` 는 `308`/`309` 를 검증하므로 업무시간 밖에 실패한다. `T10` 파일에 합치지 않는다(`GO` 3개 때문에 `IF` 로 감쌀 수 없다).

- [ ] **Step 1: 파일 작성 (UTF-8 with BOM)**

```sql
SET NOCOUNT ON;
PRINT '--- 00b_Test_Harness_RCP 시작 ---';
GO
-- 재실행 가능하도록 기존 RCP fixture 를 먼저 제거한다
DELETE d FROM [dbo].[INFO_CHECKUP_WORK_EXAMS] d
  JOIN [dbo].[INFO_CHECKUP_WORKS] w ON w.[WorkId] = d.[WorkId]
  JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId]
 WHERE p.[ChartNo] = 'T014' AND w.[StatusCode] = 'RCP';
DELETE w FROM [dbo].[INFO_CHECKUP_WORKS] w
  JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId]
 WHERE p.[ChartNo] = 'T014' AND w.[StatusCode] = 'RCP';
GO
-- T014 (남, 기준일 2026-10-01 에 만 56세) 의 RCP Work
--   ReservationDate 와 NEX 산출 기준일을 동일하게 '2026-10-01' 로 맞춘다.
--   초안은 Work 날짜를 SYSDATETIME(), NEX 를 2026-10-01 기준으로 뽑아 서로 달랐다.
INSERT INTO [dbo].[INFO_CHECKUP_WORKS] ([PatientId], [ReservationDate], [TimeSlotCode], [StatusCode])
SELECT p.[PatientId], '2026-10-01', 'AM', 'RCP'
FROM [dbo].[INFO_PATIENTS] p WHERE p.[ChartNo] = 'T014';
GO
DECLARE @Wrcp BIGINT, @Prcp BIGINT;
SELECT TOP (1) @Wrcp = w.[WorkId], @Prcp = w.[PatientId]
  FROM [dbo].[INFO_CHECKUP_WORKS] w
  JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId]
 WHERE p.[ChartNo] = 'T014' AND w.[StatusCode] = 'RCP'
 ORDER BY w.[WorkId] DESC;

INSERT INTO [dbo].[INFO_CHECKUP_WORK_EXAMS] ([WorkId], [ExamItemCode], [ExamSourceCode])
SELECT @Wrcp, n.ExamCode, 'NEX'
FROM [dbo].[UFN_HC_국가검사구성](@Prcp, '2026-10-01') n;

INSERT INTO [dbo].[INFO_CHECKUP_WORK_EXAMS] ([WorkId], [ExamItemCode], [ExamSourceCode])
VALUES (@Wrcp, 'EX014', 'AEX');   -- OPT01 복부초음파

DECLARE @Fail INT = 0;
IF ((SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORK_EXAMS] WHERE [WorkId]=@Wrcp AND [ExamSourceCode]='NEX') = 11)
    PRINT 'PASS FIX-RCP-001 NEX 11행 (T014 만 56세 조건부 3종)';
ELSE BEGIN PRINT 'FAIL FIX-RCP-001 NEX 행수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[INFO_CHECKUP_WORK_EXAMS] WHERE [WorkId]=@Wrcp AND [ExamSourceCode]='AEX') = 1)
    PRINT 'PASS FIX-RCP-002 AEX 1행 (OPT01)';
ELSE BEGIN PRINT 'FAIL FIX-RCP-002 AEX 행수 불일치'; SET @Fail += 1; END

-- T011 (여, 기준일 2026-10-01 에 만 54세) 의 RCP Work
-- [X] T014 는 남성이라 저장 NEX 에 EX012 가 없다. NEX-05 술어가 Gender='F' 를 요구하기 때문이다.
--     412 ExamDuplicate 는 EX012 로만 발생하므로(스펙 §17.2a) RUL-A09·CWR-024 는 이 Work 를 쓴다.
DELETE x FROM [dbo].[INFO_CHECKUP_WORK_EXAMS] x
 JOIN [dbo].[INFO_CHECKUP_WORKS] w ON w.[WorkId] = x.[WorkId]
 JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId]
 WHERE p.[ChartNo] = 'T011' AND w.[StatusCode] = 'RCP';
DELETE w FROM [dbo].[INFO_CHECKUP_WORKS] w
 JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId]
 WHERE p.[ChartNo] = 'T011' AND w.[StatusCode] = 'RCP';
GO
INSERT INTO [dbo].[INFO_CHECKUP_WORKS] ([PatientId], [ReservationDate], [TimeSlotCode], [StatusCode])
SELECT p.[PatientId], '2026-10-01', 'AM', 'RCP'
FROM [dbo].[INFO_PATIENTS] p WHERE p.[ChartNo] = 'T011';
GO
DECLARE @W11 BIGINT = (SELECT TOP (1) w.[WorkId] FROM [dbo].[INFO_CHECKUP_WORKS] w
                        JOIN [dbo].[INFO_PATIENTS] p ON p.[PatientId] = w.[PatientId]
                       WHERE p.[ChartNo] = 'T011' AND w.[StatusCode] = 'RCP' ORDER BY w.[WorkId] DESC);
DECLARE @P11 BIGINT = (SELECT [PatientId] FROM [dbo].[INFO_PATIENTS] WHERE [ChartNo] = 'T011');
INSERT INTO [dbo].[INFO_CHECKUP_WORK_EXAMS] ([WorkId], [ExamItemCode], [ExamSourceCode])
SELECT @W11, n.[ExamCode], 'NEX' FROM [dbo].[UFN_HC_국가검사구성](@P11, '2026-10-01') n;

DECLARE @F11 INT = 0;
IF EXISTS (SELECT 1 FROM [dbo].[INFO_CHECKUP_WORK_EXAMS]
            WHERE [WorkId] = @W11 AND [ExamSourceCode] = 'NEX' AND [ExamItemCode] = 'EX012')
    PRINT 'PASS FIX-RCP-003 T011 저장 NEX 에 EX012 포함 (412 시험 사전조건)';
ELSE BEGIN PRINT 'FAIL FIX-RCP-003 T011 저장 NEX 에 EX012 가 없다 — 412 를 관측할 수 없다'; SET @F11 += 1; END
IF @F11 > 0 THROW 51000, N'T011 RCP Fixture 사전조건 실패', 1;
GO

IF @Fail > 0 THROW 51000, N'RCP Fixture 배치 실패', 1;
PRINT '=== 00b_Test_Harness_RCP 완료 ===';
GO
```

- [ ] **Step 2: 실행**

```bash
head -c 3 tests/00b_Test_Harness_RCP.sql | od -An -tx1
RC=0
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/00b_Test_Harness_RCP.sql -o artifacts/logs/test_00b.log || RC=$?
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_00b.log | grep -E '^(PASS|FAIL)'
echo "exit=$RC"
```

Expected: `ef bb bf`, `exit=0`, `PASS FIX-RCP-001` + `PASS FIX-RCP-002`.

`FIX-RCP-001` 이 11행이 아니면 `T13` 의 NEX-02/04/06 술어를 다시 본다 — `T014`(남, 만 56세)는 `(56-24)%4=0`, `56`, `56` 세 조건이 전부 성립한다.

- [ ] **Step 3: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/tests/00b_Test_Harness_RCP.sql
git commit -m "test(phase4): RCP 상태 Work Fixture 분리 (T29 선행조건)"
```

**회귀시험:** `scripts/test.sh` 가 `tests/03` 다음, `tests/04` 앞에서 실행한다.

**로그 경로:** `artifacts/logs/test_00b.log`

**Rollback/Cleanup:** 파일 자체가 선행 `DELETE` 를 포함하므로 재실행 가능하다.

**완료조건:** NEX 11행 + AEX 1행이 배치되고 `T29` 의 `CWR-020`/`CWR-021` 이 이 Work를 찾을 수 있다.
