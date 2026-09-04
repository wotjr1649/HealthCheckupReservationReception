SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- SED-001 Exam Master 전건 값 EXCEPT 양방향  (04 §4.6)
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
                             [AdditionalGenderCode],[AdditionalActive] FROM [dbo].[검사코드])
   AND NOT EXISTS (SELECT [ExamItemCode],[ExamItemName],[NexRuleCode],[AdditionalExamCode],
                          [AdditionalGenderCode],[AdditionalActive] FROM [dbo].[검사코드]
                   EXCEPT SELECT Code,Nm,Nex,Aex,G,Act FROM @ExpExam)
    PRINT 'PASS SED-001 Exam Master 전건 값 일치';
ELSE
BEGIN
    PRINT 'FAIL SED-001 Exam Master 불일치';
    SELECT '기대에만' AS Side, * FROM (SELECT Code,Nm,Nex,Aex,G,Act FROM @ExpExam
        EXCEPT SELECT [ExamItemCode],[ExamItemName],[NexRuleCode],[AdditionalExamCode],
                      [AdditionalGenderCode],[AdditionalActive] FROM [dbo].[검사코드]) a;
    SET @Fail += 1;
END

IF ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [NexRuleCode] IS NOT NULL) = 13)
    PRINT 'PASS SED-002 NEX 역할 13행';
ELSE BEGIN PRINT 'FAIL SED-002 NEX 역할 행수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [AdditionalExamCode] IS NOT NULL) = 7)
    PRINT 'PASS SED-003 AEX 역할 7행';
ELSE BEGIN PRINT 'FAIL SED-003 AEX 역할 행수 불일치'; SET @Fail += 1; END

IF EXISTS (SELECT 1 FROM [dbo].[검사코드]
            WHERE [ExamItemCode] = 'EX012' AND [NexRuleCode] = 'NEX-05'
              AND [AdditionalExamCode] = 'OPT04' AND [AdditionalGenderCode] = 'A' AND [AdditionalActive] = 1)
    PRINT 'PASS SED-004 EX012 가 NEX-05 와 OPT04 역할을 동시에 가짐';
ELSE BEGIN PRINT 'FAIL SED-004 EX012 이중역할 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(DISTINCT [AdditionalExamCode]) FROM [dbo].[검사코드] WHERE [AdditionalExamCode] IS NOT NULL) = 7
    AND NOT EXISTS (SELECT [AdditionalExamCode] FROM [dbo].[검사코드] WHERE [AdditionalExamCode] IS NOT NULL
                    EXCEPT SELECT v.c FROM (VALUES ('OPT01'),('OPT02'),('OPT03'),('OPT04'),('OPT05'),('OPT06'),('OPT07')) v(c)))
    PRINT 'PASS SED-005 AEX 코드가 OPT01~OPT07 정확히 7개';
ELSE BEGIN PRINT 'FAIL SED-005 AEX 코드 집합 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [NexRuleCode] = 'NEX-01') = 8)
    PRINT 'PASS SED-006 NEX-01 기본검사 8행';
ELSE BEGIN PRINT 'FAIL SED-006 NEX-01 행수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [NexRuleCode] IN ('NEX-02','NEX-03','NEX-04','NEX-05','NEX-06')) = 5)
    PRINT 'PASS SED-007 조건부 NEX 5행';
ELSE BEGIN PRINT 'FAIL SED-007 조건부 NEX 행수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[휴무일] WHERE [Active] = 1) = 2)
    PRINT 'PASS SED-008 활성 휴무일 2행';
ELSE BEGIN PRINT 'FAIL SED-008 휴무일 행수 불일치'; SET @Fail += 1; END

IF EXISTS (SELECT 1 FROM [dbo].[휴무일] WHERE [HolidayDate] = '2026-12-25' AND DATEDIFF(DAY,0,[HolidayDate])%7 = 4)
    PRINT 'PASS SED-009 평일(금) 휴무일 존재';
ELSE BEGIN PRINT 'FAIL SED-009 평일 휴무일 없음 또는 요일 불일치'; SET @Fail += 1; END

IF EXISTS (SELECT 1 FROM [dbo].[휴무일] WHERE [HolidayDate] = '2026-12-26' AND DATEDIFF(DAY,0,[HolidayDate])%7 = 5)
    PRINT 'PASS SED-010 토요일 휴무일 존재';
ELSE BEGIN PRINT 'FAIL SED-010 토요일 휴무일 없음 또는 요일 불일치'; SET @Fail += 1; END

-- SED-011 AEX 7종이 전부 AdditionalActive=1 인가
--   CORRUPT-3 이 tests/03 에서 일시적으로 0 으로 바꾸고 되돌리므로,
--   이 검사는 그 오염이 남지 않았음을 보증한다.
IF ((SELECT COUNT(*) FROM [dbo].[검사코드]
      WHERE [AdditionalExamCode] IS NOT NULL AND [AdditionalActive] = 1) = 7)
    PRINT 'PASS SED-011 AEX 7종 전부 AdditionalActive=1';
ELSE BEGIN PRINT 'FAIL SED-011 AEX Active 오염'; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 02_Seed_Tests 완료 ===';
GO
