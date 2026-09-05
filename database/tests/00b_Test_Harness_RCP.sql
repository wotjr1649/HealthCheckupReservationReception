SET NOCOUNT ON;
PRINT '--- 00b_Test_Harness_RCP 시작 ---';
GO
-- 재실행 가능하도록 기존 RCP fixture 를 먼저 제거한다
DELETE w FROM [dbo].[예약접수] w
  JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
 WHERE p.[차트번호] = 'T014' AND w.[상태코드] = 'RCP';
GO
-- T014 (남, 기준일 2026-10-01 에 만 56세) 의 RCP Work
--   ReservationDate 와 NEX 산출 기준일을 동일하게 '2026-10-01' 로 맞춘다.
DECLARE @Prcp BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = 'T014');
DECLARE @Nrcp NVARCHAR(100) = N'', @NC VARCHAR(10);
DECLARE @NCodes TABLE (C VARCHAR(10) PRIMARY KEY);
INSERT INTO @NCodes (C) SELECT n.[ExamCode] FROM [dbo].[UFN_HC_국가검사구성](@Prcp, '2026-10-01') n;
WHILE EXISTS (SELECT 1 FROM @NCodes)
BEGIN
    SELECT TOP (1) @NC = C FROM @NCodes ORDER BY C;
    SET @Nrcp = @Nrcp + @NC + N',';
    DELETE FROM @NCodes WHERE C = @NC;
END
SET @Nrcp = LEFT(@Nrcp, LEN(@Nrcp) - 1);
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
VALUES (@Prcp, '2026-10-01', 'AM', 'RCP', @Nrcp, N'EX014');   -- OPT01 복부초음파

DECLARE @Fail INT = 0;
IF (LEN(@Nrcp) - LEN(REPLACE(@Nrcp, N',', N'')) + 1 = 11)
    PRINT 'PASS FIX-RCP-001 NEX 11종 (T014 만 56세 조건부 3종)';
ELSE BEGIN PRINT 'FAIL FIX-RCP-001 NEX 종수 불일치'; SET @Fail += 1; END

IF ((SELECT [추가검사항목] FROM [dbo].[예약접수] WHERE [수검자ID] = @Prcp AND [상태코드] = 'RCP') = N'EX014')
    PRINT 'PASS FIX-RCP-002 AEX 1종 (OPT01)';
ELSE BEGIN PRINT 'FAIL FIX-RCP-002 AEX 불일치'; SET @Fail += 1; END

-- @Fail 은 이 배치에서만 산다. GO 뒤로 넘기면 Msg 137 이다.
IF @Fail > 0 THROW 51000, N'RCP Fixture 배치 실패', 1;

-- T011 (여, 기준일 2026-10-01 에 만 54세) 의 RCP Work
--   T014 는 남성이라 저장 NEX 에 EX012 가 없다. NEX-05 술어가 Gender='F' 를 요구하기 때문이다.
--   412 ExamDuplicate 는 EX012 로만 발생하므로(스펙 §17.2a) RUL-A09·CWR-024 는 이 Work 를 쓴다.
DELETE w FROM [dbo].[예약접수] w
 JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
 WHERE p.[차트번호] = 'T011' AND w.[상태코드] = 'RCP';
GO
DECLARE @P11 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = 'T011');
DECLARE @N11 NVARCHAR(100) = N'', @NC VARCHAR(10);
DECLARE @NCodes TABLE (C VARCHAR(10) PRIMARY KEY);
INSERT INTO @NCodes (C) SELECT n.[ExamCode] FROM [dbo].[UFN_HC_국가검사구성](@P11, '2026-10-01') n;
WHILE EXISTS (SELECT 1 FROM @NCodes)
BEGIN
    SELECT TOP (1) @NC = C FROM @NCodes ORDER BY C;
    SET @N11 = @N11 + @NC + N',';
    DELETE FROM @NCodes WHERE C = @NC;
END
SET @N11 = LEFT(@N11, LEN(@N11) - 1);
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
VALUES (@P11, '2026-10-01', 'AM', 'RCP', @N11, NULL);

DECLARE @F11 INT = 0;
IF (N',' + @N11 + N',' LIKE N'%,EX012,%')
    PRINT 'PASS FIX-RCP-003 T011 저장 NEX 에 EX012 포함 (412 시험 사전조건)';
ELSE BEGIN PRINT N'FAIL FIX-RCP-003 T011 저장 NEX 에 EX012 가 없다 — 412 를 관측할 수 없다'; SET @F11 += 1; END
IF @F11 > 0 THROW 51000, N'T011 RCP Fixture 사전조건 실패', 1;
GO
PRINT '=== 00b_Test_Harness_RCP 완료 ===';
GO
