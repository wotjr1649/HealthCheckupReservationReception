SET NOCOUNT ON;
PRINT '--- 00_Test_Harness 시작 ---';
GO
-- 검사항목 테이블은 T52 에서 사라졌다. 검사구성은 예약접수의 컬럼이다.
DELETE FROM [dbo].[예약접수];
DELETE FROM [dbo].[변경이력];
DELETE FROM [dbo].[완료이력];
DELETE FROM [dbo].[수검자];
GO
-- Prefix12 = YYMMDD(6) + 세기·성별(1) + 순번(5)
-- 정상 체크디지트 = (11 - (가중합 % 11)) % 10   /   무효 체크디지트 = (정상 + 1) % 10
-- 주민번호를 손으로 계산하지 않는다. 13번째 자리는 SQL 이 만든다. SSN-006 이 무효임을 증명한다.
-- 생년월일·성별은 계산열이라 INSERT 에 넣을 수 없다 (Msg 271). 주민번호에서 DB 가 유도한다.
-- 아래 VALUES 의 Birthday·Gender 는 그 유도 결과의 기대값으로 남겨 FIX-DERIVE 가 대조한다.
INSERT INTO [dbo].[수검자]
    ([차트번호], [성명], [주민번호], [휴대전화])
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
              ) % 11 ) ) % 10 + 1 ) % 10 ) )   -- 마지막 ) 가 CONVERT(CHAR(1), … 를 닫는다
    , NULL
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
    , ('T016', N'테스트삼육', '901001100016', '19901001', 'M')   -- TGT 401 NotDue 전담 (1년차 완료이력)
    , ('T017', N'테스트일팔', '071118300017', '20071118', 'M')   -- TGT 400 UnderAge 전담. 고정 리터럴이다
    , ('T018', N'테스트사사', '821001200018', '19821001', 'F')   -- 만 44세 여 → NEX-02 (44-40)%4=0 성립
    , ('T019', N'테스트오사남', '721001100019', '19721001', 'M') -- 만 54세 남 → NEX-05 는 여성 전용이라 EX012 없음
    , ('T020', N'테스트경계녀', '721120200020', '19721120', 'F') -- RWR-031 전용. 11-17 에 만 53세, 11-20 에 만 54세
) p (ChartNo, Name, Prefix12, Birthday, Gender);
GO
-- 정원용 F001~F020. RP-06 때문에 한 수검자는 유효업무를 둘 이상 가질 수 없어
-- 2026-11-16 AM 슬롯을 19/20 으로 채우는 데만 19명이 필요하고, F020 은 RWR-012 가
-- CNR → RSV 로 뒤집어 20/20 을 만들 예비 1명이다.
-- ROW_NUMBER() OVER (ORDER BY (SELECT 1)) FROM sys.all_objects 를 쓰지 않는다 — n 이 1..20 이라는 보장이 없다.
INSERT INTO [dbo].[수검자] ([차트번호], [성명], [주민번호])
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
              ) % 11 ) ) % 10 + 1 ) % 10 ) )   -- 마지막 ) 가 CONVERT(CHAR(1), … 를 닫는다
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),
             (11),(12),(13),(14),(15),(16),(17),(18),(19),
             (20)) n(n)
CROSS APPLY (SELECT Prefix12 = '8001011' + RIGHT('00000' + CONVERT(VARCHAR(5), n.n), 5)) x;
GO
-- 기본검사(NEX-01) 문자열을 검사코드에서 유도한다. 손으로 적으면 Seed 와 어긋난다.
-- 검사구성이 예약접수의 컬럼이므로 INSERT 와 같은 배치에 있어야 한다 (GO 는 변수 경계다).
DECLARE @Basic NVARCHAR(100) = N'', @BC VARCHAR(10);
DECLARE @BCodes TABLE (C VARCHAR(10) PRIMARY KEY);
INSERT INTO @BCodes (C) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] = 'NEX-01';
WHILE EXISTS (SELECT 1 FROM @BCodes)
BEGIN
    SELECT TOP (1) @BC = C FROM @BCodes ORDER BY C;
    SET @Basic = @Basic + @BC + N',';
    DELETE FROM @BCodes WHERE C = @BC;
END
SET @Basic = LEFT(@Basic, LEN(@Basic) - 1);

-- LIKE 'F0%' 만 쓰면 F020 까지 RSV 가 되어 슬롯이 20/20 이 된다.
-- CON-002(19/20 경합 → 최종 20) 의 사전조건이 깨지므로 F020 을 제외하고, 별도로 CNR 을 넣는다.
-- Slot 날짜는 리터럴 하나로 통일한다. 2026-11-16 은 월요일이다.
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
SELECT [수검자ID], '2026-11-16', 'AM', 'RSV', @Basic, NULL
FROM [dbo].[수검자] WHERE [차트번호] LIKE 'F0%' AND [차트번호] <> 'F020';

INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
SELECT [수검자ID], '2026-11-16', 'AM', 'CNR', @Basic, NULL
FROM [dbo].[수검자] WHERE [차트번호] = 'F020';

-- T020 의 RSV Work. 2026-11-17 기준 만 53세라 EX012 가 없고 OPT04 를 선택할 수 있다.
-- RWR-031 이 이 Work 의 예약일을 2026-11-20 으로 옮기면 만 54세가 되어 EX012 가 생기고 OPT04 가 412 다.
-- 이 Work 는 2026-11-17 슬롯이므로 2026-11-16 정원(CON-002)과 무관하다.
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
SELECT [수검자ID], '2026-11-17', 'AM', 'RSV', @Basic, N'EX012'
FROM [dbo].[수검자] WHERE [차트번호] = 'T020';

DECLARE @BDrift INT = 0;
SELECT @BDrift = COUNT(*) FROM (
    SELECT m.[검사항목코드] FROM [dbo].[검사코드] m
     WHERE m.[국가검사규칙코드] = 'NEX-01'
       AND N',' + @Basic + N',' NOT LIKE N'%,' + m.[검사항목코드] + N',%'
    UNION ALL
    SELECT m.[검사항목코드] FROM [dbo].[검사코드] m
     WHERE N',' + @Basic + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
       AND ISNULL(m.[국가검사규칙코드], '') <> 'NEX-01'
) a;
IF (@BDrift = 0) PRINT 'PASS FIX-EXAM-001 기본검사 문자열 = 검사코드 NEX-01 (양방향)';
ELSE BEGIN PRINT 'FAIL FIX-EXAM-001 기본검사 문자열 어긋남'; THROW 51010, N'기본검사 유도 실패', 1; END
GO
-- T003 에 완료이력을 주면 안 된다. T003(만 23세 남)의 목적은 "NEX-02 남 미해당 → NEX 8행" 인데
-- 1년차 완료이력을 붙이면 TGT 401 NotDue 비대상이 되어 NEX 0행이 나온다. 401 전담은 T016 이다.
-- 기본검사(NEX-01) 문자열을 검사코드에서 유도한다. 손으로 적으면 Seed 와 어긋난다.
DECLARE @Basic NVARCHAR(100) = N'', @BC VARCHAR(10);
DECLARE @BCodes TABLE (C VARCHAR(10) PRIMARY KEY);
INSERT INTO @BCodes (C) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] = 'NEX-01';
WHILE EXISTS (SELECT 1 FROM @BCodes)
BEGIN
    SELECT TOP (1) @BC = C FROM @BCodes ORDER BY C;
    SET @Basic = @Basic + @BC + N',';
    DELETE FROM @BCodes WHERE C = @BC;
END
SET @Basic = LEFT(@Basic, LEN(@Basic) - 1);

-- T016 · T004 는 검사 내용을 아는 이력, T005 · T006 은 모르는 이력(외부 기관)이다.
INSERT INTO [dbo].[완료이력] ([수검자ID], [완료일자], [국가검사항목], [추가검사항목])
SELECT p.[수검자ID], '2025-05-01', @Basic, N'EX014' FROM [dbo].[수검자] p WHERE p.[차트번호] = 'T016';  -- 1년차 → 401 NotDue
INSERT INTO [dbo].[완료이력] ([수검자ID], [완료일자], [국가검사항목], [추가검사항목])
SELECT p.[수검자ID], '2024-05-01', @Basic, NULL    FROM [dbo].[수검자] p WHERE p.[차트번호] = 'T004';  -- 2년차 → 대상
INSERT INTO [dbo].[완료이력] ([수검자ID], [완료일자], [국가검사항목], [추가검사항목])
SELECT p.[수검자ID], '2026-10-01', NULL,   NULL    FROM [dbo].[수검자] p WHERE p.[차트번호] = 'T005';  -- 예약일 당일 → 제외
INSERT INTO [dbo].[완료이력] ([수검자ID], [완료일자], [국가검사항목], [추가검사항목])
SELECT p.[수검자ID], '2026-11-01', NULL,   NULL    FROM [dbo].[수검자] p WHERE p.[차트번호] = 'T006';  -- 예약일 이후 → 제외

DECLARE @BDrift INT = 0;
SELECT @BDrift = COUNT(*) FROM (
    SELECT m.[검사항목코드] FROM [dbo].[검사코드] m
     WHERE m.[국가검사규칙코드] = 'NEX-01'
       AND N',' + @Basic + N',' NOT LIKE N'%,' + m.[검사항목코드] + N',%'
    UNION ALL
    SELECT m.[검사항목코드] FROM [dbo].[검사코드] m
     WHERE N',' + @Basic + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
       AND ISNULL(m.[국가검사규칙코드], '') <> 'NEX-01'
) a;
IF (@BDrift = 0) PRINT 'PASS FIX-EXAM-003 완료이력 기본검사 문자열 = 검사코드 NEX-01 (양방향)';
ELSE BEGIN PRINT 'FAIL FIX-EXAM-003 완료이력 기본검사 문자열 어긋남'; THROW 51012, N'완료이력 유도 실패', 1; END
GO
UPDATE [dbo].[수검자] SET [B형간염제외여부] = 1
 WHERE [차트번호] = 'T008';   -- B형간염 제외 — NEX-03 테스트
GO
PRINT '--- CORRUPT 구획 (701 검증 전용) ---';
GO
-- CORRUPT-1: 동일 Patient 에 유효업무 2건  → 701
DECLARE @Basic NVARCHAR(100) = N'', @BC VARCHAR(10);
DECLARE @BCodes TABLE (C VARCHAR(10) PRIMARY KEY);
INSERT INTO @BCodes (C) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] = 'NEX-01';
WHILE EXISTS (SELECT 1 FROM @BCodes)
BEGIN
    SELECT TOP (1) @BC = C FROM @BCodes ORDER BY C;
    SET @Basic = @Basic + @BC + N',';
    DELETE FROM @BCodes WHERE C = @BC;
END
SET @Basic = LEFT(@Basic, LEN(@Basic) - 1);
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
SELECT p.[수검자ID], '2026-11-17', 'AM', 'RSV', @Basic, NULL FROM [dbo].[수검자] p WHERE p.[차트번호] = 'T012';
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
SELECT p.[수검자ID], '2026-11-18', 'PM', 'RSV', @Basic, NULL FROM [dbo].[수검자] p WHERE p.[차트번호] = 'T012';

-- CORRUPT-2: 저장 NEX 가 빈 Work  → 701. 빈 문자열이 손상의 유일한 표현이다 (04 §8.2.2).
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
SELECT p.[수검자ID], '2026-11-19', 'AM', 'RSV', N'', NULL FROM [dbo].[수검자] p WHERE p.[차트번호] = 'T013';
GO
-- FIX-DERIVE  생년월일·성별 계산열이 주민번호에서 기대값을 만들어 내는가 (04 §8.1.2).
--   19xx/20xx 와 M/F 네 조합을 표본으로 짚는다. 유도가 틀리면 만나이 Rule 이 통째로 흔들린다.
DECLARE @DFail INT = 0;
IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [차트번호]='T001' AND [생년월일]='20061002' AND [성별]='M') SET @DFail += 1;
IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [차트번호]='T005' AND [생년월일]='19981001' AND [성별]='M') SET @DFail += 1;
IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [차트번호]='T006' AND [생년월일]='19871001' AND [성별]='F') SET @DFail += 1;
IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [차트번호]='F001' AND [생년월일]='19800101' AND [성별]='M') SET @DFail += 1;
IF @DFail = 0 PRINT 'PASS FIX-DERIVE 계산열 유도값이 기대 생년월일·성별과 일치';
ELSE BEGIN PRINT 'FAIL FIX-DERIVE 계산열 유도값 불일치'; THROW 51900, N'계산열 유도값이 기대와 다릅니다.', 1; END
GO
PRINT 'PASS FIX-DEPLOY Fixture 배치 완료';
PRINT '=== 00_Test_Harness 완료 ===';
GO
