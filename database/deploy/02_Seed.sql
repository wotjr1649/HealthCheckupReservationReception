-- 필터형 인덱스 UX_검사코드_AEX_CODE 때문에 검사코드 INSERT 는 QUOTED_IDENTIFIER ON 을 요구한다.
-- sqlcmd 기본값은 OFF 라서(SSMS 와 다르다) 이 SET 이 없으면 Msg 1934 로 실패한다.
-- parse 시점에 적용되므로 반드시 앞선 배치에 두고 GO 로 끊는다.
SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
PRINT '--- 02_Seed 시작 ---';
GO
-- clean-create 전제. 비어 있지 않으면 배포 순서가 잘못된 것이므로 즉시 중단한다.
IF EXISTS (SELECT 1 FROM [dbo].[검사코드]) OR EXISTS (SELECT 1 FROM [dbo].[휴무일])
    THROW 51002, N'02_Seed: Master 테이블이 비어 있지 않습니다. 01_Schema 를 먼저 실행하십시오.', 1;
GO
INSERT INTO [dbo].[검사코드]
    ([검사항목코드], [검사항목명], [국가검사규칙코드], [추가검사코드], [추가검사성별코드], [추가검사사용여부])
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
INSERT INTO [dbo].[휴무일] ([휴무일자], [휴무일명], [사용여부], [비고])
VALUES
    ('2026-12-25', N'성탄절',     1, N'평일(금) 휴무일 — HOL-05 테스트용'),
    ('2026-12-26', N'센터 휴진일', 1, N'토요일 휴무일 — HOL-05 테스트용');
GO
-- PRINT 는 스칼라 식만 받는다. 하위 쿼리를 직접 넣으면 Msg 1046 + Msg 102 로 배치가 컴파일되지 않는다(실측).
DECLARE @ExamCount INT = (SELECT COUNT(*) FROM [dbo].[검사코드]);
DECLARE @HolidayCount INT = (SELECT COUNT(*) FROM [dbo].[휴무일]);
PRINT 'PASS SEED-DEPLOY Exam ' + CONVERT(VARCHAR(5), @ExamCount)
    + ' / Holiday ' + CONVERT(VARCHAR(5), @HolidayCount);
GO
