SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- SED-001 Exam Master 전건 값 EXCEPT 양방향  (04 §4.6)
--   개수·표본 확인으로는 어떤 행의 역할·성별·Active 가 틀려도 통과한다.
DECLARE @ExpExam TABLE (Cd VARCHAR(10) PRIMARY KEY, Nm NVARCHAR(100),
                        NexRule VARCHAR(10) NULL, AexCd VARCHAR(10) NULL, G CHAR(1) NULL, Act BIT);
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

IF NOT EXISTS (SELECT Cd,Nm,NexRule,AexCd,G,Act FROM @ExpExam
               EXCEPT SELECT [검사항목코드],[검사항목명],[국가검사규칙코드],[추가검사코드],
                             [추가검사성별코드],[추가검사사용여부] FROM [dbo].[검사코드])
   AND NOT EXISTS (SELECT [검사항목코드],[검사항목명],[국가검사규칙코드],[추가검사코드],
                          [추가검사성별코드],[추가검사사용여부] FROM [dbo].[검사코드]
                   EXCEPT SELECT Cd,Nm,NexRule,AexCd,G,Act FROM @ExpExam)
    PRINT 'PASS SED-001 Exam Master 전건 값 일치';
ELSE
BEGIN
    PRINT 'FAIL SED-001 Exam Master 불일치';
    SELECT '기대에만' AS Side, * FROM (SELECT Cd,Nm,NexRule,AexCd,G,Act FROM @ExpExam
        EXCEPT SELECT [검사항목코드],[검사항목명],[국가검사규칙코드],[추가검사코드],
                      [추가검사성별코드],[추가검사사용여부] FROM [dbo].[검사코드]) a;
    SET @Fail += 1;
END

IF ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [국가검사규칙코드] IS NOT NULL) = 13)
    PRINT 'PASS SED-002 NEX 역할 13행';
ELSE BEGIN PRINT 'FAIL SED-002 NEX 역할 행수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [추가검사코드] IS NOT NULL) = 7)
    PRINT 'PASS SED-003 AEX 역할 7행';
ELSE BEGIN PRINT 'FAIL SED-003 AEX 역할 행수 불일치'; SET @Fail += 1; END

IF EXISTS (SELECT 1 FROM [dbo].[검사코드]
            WHERE [검사항목코드] = 'EX012' AND [국가검사규칙코드] = 'NEX-05'
              AND [추가검사코드] = 'OPT04' AND [추가검사성별코드] = 'A' AND [추가검사사용여부] = 1)
    PRINT 'PASS SED-004 EX012 가 NEX-05 와 OPT04 역할을 동시에 가짐';
ELSE BEGIN PRINT 'FAIL SED-004 EX012 이중역할 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(DISTINCT [추가검사코드]) FROM [dbo].[검사코드] WHERE [추가검사코드] IS NOT NULL) = 7
    AND NOT EXISTS (SELECT [추가검사코드] FROM [dbo].[검사코드] WHERE [추가검사코드] IS NOT NULL
                    EXCEPT SELECT v.c FROM (VALUES ('OPT01'),('OPT02'),('OPT03'),('OPT04'),('OPT05'),('OPT06'),('OPT07')) v(c)))
    PRINT 'PASS SED-005 AEX 코드가 OPT01~OPT07 정확히 7개';
ELSE BEGIN PRINT 'FAIL SED-005 AEX 코드 집합 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [국가검사규칙코드] = 'NEX-01') = 8)
    PRINT 'PASS SED-006 NEX-01 기본검사 8행';
ELSE BEGIN PRINT 'FAIL SED-006 NEX-01 행수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [국가검사규칙코드] IN ('NEX-02','NEX-03','NEX-04','NEX-05','NEX-06')) = 5)
    PRINT 'PASS SED-007 조건부 NEX 5행';
ELSE BEGIN PRINT 'FAIL SED-007 조건부 NEX 행수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[휴무일] WHERE [사용여부] = 1) = 2)
    PRINT 'PASS SED-008 활성 휴무일 2행';
ELSE BEGIN PRINT 'FAIL SED-008 휴무일 행수 불일치'; SET @Fail += 1; END

IF EXISTS (SELECT 1 FROM [dbo].[휴무일] WHERE [휴무일자] = '2026-12-25' AND DATEDIFF(DAY,0,[휴무일자])%7 = 4)
    PRINT 'PASS SED-009 평일(금) 휴무일 존재';
ELSE BEGIN PRINT 'FAIL SED-009 평일 휴무일 없음 또는 요일 불일치'; SET @Fail += 1; END

IF EXISTS (SELECT 1 FROM [dbo].[휴무일] WHERE [휴무일자] = '2026-12-26' AND DATEDIFF(DAY,0,[휴무일자])%7 = 5)
    PRINT 'PASS SED-010 토요일 휴무일 존재';
ELSE BEGIN PRINT 'FAIL SED-010 토요일 휴무일 없음 또는 요일 불일치'; SET @Fail += 1; END

-- SED-011 AEX 7종이 전부 추가검사사용여부=1 인가
--   CORRUPT-3 이 tests/03 에서 일시적으로 0 으로 바꾸고 되돌리므로,
--   이 검사는 그 오염이 남지 않았음을 보증한다.
IF ((SELECT COUNT(*) FROM [dbo].[검사코드]
      WHERE [추가검사코드] IS NOT NULL AND [추가검사사용여부] = 1) = 7)
    PRINT N'PASS SED-011 AEX 7종 전부 추가검사사용여부=1';
ELSE BEGIN PRINT 'FAIL SED-011 AEX Active 오염'; SET @Fail += 1; END

-- ── 주민번호 무효 검수 (스펙 §16.2) ──────────────────────────────────────────
-- 스펙 §45.2 는 SSN 의 산출 파일을 이 파일로 지정한다. Fixture 는 tests/00 이 만들고
-- scripts/test.sh 의 순서가 tests/00 → tests/01 → tests/02 이므로 여기서 검수한다.
DECLARE @Bad INT;

-- 사전조건 — 수검자가 0행이면 아래 COUNT 기반 검사가 전부 조용히 PASS 한다.
--   그 통과는 "실제 주민등록번호를 쓰지 않았다"를 하나도 증명하지 않는다.
--   카탈로그에 없는 표식이므로 Test ID 를 쓰지 않는다 (FIX-DEPLOY·FIX-RCP-001 과 같은 부류).
IF ((SELECT COUNT(*) FROM [dbo].[수검자]) > 0)
    PRINT N'PASS FIX-SSN-PRE 사전조건 — 수검자 Fixture 존재';
ELSE BEGIN PRINT N'FAIL FIX-SSN-PRE 수검자 0행 — tests/00_Test_Harness 를 먼저 실행하라'; SET @Fail += 1; END

-- SSN-001 13자리 숫자
SELECT @Bad = COUNT(*) FROM [dbo].[수검자]
 WHERE LEN([주민번호]) <> 13 OR [주민번호] LIKE '%[^0-9]%';
IF @Bad = 0 PRINT 'PASS SSN-001 전 행 13자리 숫자';
ELSE BEGIN PRINT 'FAIL SSN-001 형식 위반 ' + CONVERT(VARCHAR(5), @Bad) + '건'; SET @Fail += 1; END

-- SSN-002 7번째 자리 = 1/2/3/4
SELECT @Bad = COUNT(*) FROM [dbo].[수검자]
 WHERE SUBSTRING([주민번호], 7, 1) NOT IN ('1','2','3','4');
IF @Bad = 0 PRINT 'PASS SSN-002 세기·성별 코드 유효';
ELSE BEGIN PRINT 'FAIL SSN-002 세기·성별 코드 위반 ' + CONVERT(VARCHAR(5), @Bad) + '건'; SET @Fail += 1; END

-- SSN-003 앞 6자리가 실제 날짜
SELECT @Bad = COUNT(*) FROM [dbo].[수검자]
 WHERE TRY_CONVERT(DATE,
        CASE WHEN SUBSTRING([주민번호],7,1) IN ('1','2') THEN '19' ELSE '20' END
        + SUBSTRING([주민번호], 1, 6), 112) IS NULL;
IF @Bad = 0 PRINT 'PASS SSN-003 앞 6자리가 실제 날짜';
ELSE BEGIN PRINT 'FAIL SSN-003 날짜 아님 ' + CONVERT(VARCHAR(5), @Bad) + '건'; SET @Fail += 1; END

-- SSN-004 생년월일 파생값 일치
SELECT @Bad = COUNT(*) FROM [dbo].[수검자]
 WHERE [생년월일] <> CASE WHEN SUBSTRING([주민번호],7,1) IN ('1','2') THEN '19' ELSE '20' END
                     + SUBSTRING([주민번호], 1, 6);
IF @Bad = 0 PRINT 'PASS SSN-004 Birthday 파생값 일치';
ELSE BEGIN PRINT 'FAIL SSN-004 Birthday 불일치 ' + CONVERT(VARCHAR(5), @Bad) + '건'; SET @Fail += 1; END

-- SSN-005 성별 파생값 일치
SELECT @Bad = COUNT(*) FROM [dbo].[수검자]
 WHERE [성별] <> CASE WHEN SUBSTRING([주민번호],7,1) IN ('1','3') THEN 'M' ELSE 'F' END;
IF @Bad = 0 PRINT 'PASS SSN-005 Gender 파생값 일치';
ELSE BEGIN PRINT 'FAIL SSN-005 Gender 불일치 ' + CONVERT(VARCHAR(5), @Bad) + '건'; SET @Fail += 1; END

-- SSN-006 *** 체크디지트가 전 행 무효 — 실제 주민등록번호 미사용의 기계적 증거 ***
SELECT @Bad = COUNT(*)
FROM [dbo].[수검자] s
CROSS APPLY (SELECT Valid =
      ( 11 - (
        ( CAST(SUBSTRING(s.[주민번호], 1,1) AS INT)*2 + CAST(SUBSTRING(s.[주민번호], 2,1) AS INT)*3
        + CAST(SUBSTRING(s.[주민번호], 3,1) AS INT)*4 + CAST(SUBSTRING(s.[주민번호], 4,1) AS INT)*5
        + CAST(SUBSTRING(s.[주민번호], 5,1) AS INT)*6 + CAST(SUBSTRING(s.[주민번호], 6,1) AS INT)*7
        + CAST(SUBSTRING(s.[주민번호], 7,1) AS INT)*8 + CAST(SUBSTRING(s.[주민번호], 8,1) AS INT)*9
        + CAST(SUBSTRING(s.[주민번호], 9,1) AS INT)*2 + CAST(SUBSTRING(s.[주민번호],10,1) AS INT)*3
        + CAST(SUBSTRING(s.[주민번호],11,1) AS INT)*4 + CAST(SUBSTRING(s.[주민번호],12,1) AS INT)*5
        ) % 11 ) ) % 10 ) c
WHERE CAST(SUBSTRING(s.[주민번호], 13, 1) AS INT) = c.Valid;   -- 유효하면 위반
IF @Bad = 0 PRINT N'PASS SSN-006 전 행 체크디지트 무효 — 실제 주민등록번호 미사용 증명';
ELSE BEGIN PRINT 'FAIL SSN-006 체크디지트가 유효한 행 ' + CONVERT(VARCHAR(5), @Bad) + '건'; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 02_Seed_Tests 완료 ===';
GO
