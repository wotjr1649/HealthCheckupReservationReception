SET NOCOUNT ON;
PRINT '--- 00b_Test_Harness_RCP 시작 ---';
GO
-- 재실행 가능하도록 기존 RCP fixture 를 먼저 제거한다
DELETE d FROM [dbo].[검사항목] d
  JOIN [dbo].[예약접수] w ON w.[업무ID] = d.[업무ID]
  JOIN [dbo].[수검자] p ON p.[PatientId] = w.[수검자ID]
 WHERE p.[ChartNo] = 'T014' AND w.[상태코드] = 'RCP';
DELETE w FROM [dbo].[예약접수] w
  JOIN [dbo].[수검자] p ON p.[PatientId] = w.[수검자ID]
 WHERE p.[ChartNo] = 'T014' AND w.[상태코드] = 'RCP';
GO
-- T014 (남, 기준일 2026-10-01 에 만 56세) 의 RCP Work
--   ReservationDate 와 NEX 산출 기준일을 동일하게 '2026-10-01' 로 맞춘다.
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드])
SELECT p.[PatientId], '2026-10-01', 'AM', 'RCP'
FROM [dbo].[수검자] p WHERE p.[ChartNo] = 'T014';
GO
DECLARE @Wrcp BIGINT, @Prcp BIGINT;
SELECT TOP (1) @Wrcp = w.[업무ID], @Prcp = w.[수검자ID]
  FROM [dbo].[예약접수] w
  JOIN [dbo].[수검자] p ON p.[PatientId] = w.[수검자ID]
 WHERE p.[ChartNo] = 'T014' AND w.[상태코드] = 'RCP'
 ORDER BY w.[업무ID] DESC;

INSERT INTO [dbo].[검사항목] ([업무ID], [검사항목코드], [검사출처코드])
SELECT @Wrcp, n.ExamCode, 'NEX'
FROM [dbo].[UFN_HC_국가검사구성](@Prcp, '2026-10-01') n;

INSERT INTO [dbo].[검사항목] ([업무ID], [검사항목코드], [검사출처코드])
VALUES (@Wrcp, 'EX014', 'AEX');   -- OPT01 복부초음파

DECLARE @Fail INT = 0;
IF ((SELECT COUNT(*) FROM [dbo].[검사항목] WHERE [업무ID]=@Wrcp AND [검사출처코드]='NEX') = 11)
    PRINT 'PASS FIX-RCP-001 NEX 11행 (T014 만 56세 조건부 3종)';
ELSE BEGIN PRINT 'FAIL FIX-RCP-001 NEX 행수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM [dbo].[검사항목] WHERE [업무ID]=@Wrcp AND [검사출처코드]='AEX') = 1)
    PRINT 'PASS FIX-RCP-002 AEX 1행 (OPT01)';
ELSE BEGIN PRINT 'FAIL FIX-RCP-002 AEX 행수 불일치'; SET @Fail += 1; END

-- @Fail 은 이 배치에서만 산다. GO 뒤로 넘기면 Msg 137 이다.
IF @Fail > 0 THROW 51000, N'RCP Fixture 배치 실패', 1;

-- T011 (여, 기준일 2026-10-01 에 만 54세) 의 RCP Work
--   T014 는 남성이라 저장 NEX 에 EX012 가 없다. NEX-05 술어가 Gender='F' 를 요구하기 때문이다.
--   412 ExamDuplicate 는 EX012 로만 발생하므로(스펙 §17.2a) RUL-A09·CWR-024 는 이 Work 를 쓴다.
DELETE x FROM [dbo].[검사항목] x
 JOIN [dbo].[예약접수] w ON w.[업무ID] = x.[업무ID]
 JOIN [dbo].[수검자] p ON p.[PatientId] = w.[수검자ID]
 WHERE p.[ChartNo] = 'T011' AND w.[상태코드] = 'RCP';
DELETE w FROM [dbo].[예약접수] w
 JOIN [dbo].[수검자] p ON p.[PatientId] = w.[수검자ID]
 WHERE p.[ChartNo] = 'T011' AND w.[상태코드] = 'RCP';
GO
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드])
SELECT p.[PatientId], '2026-10-01', 'AM', 'RCP'
FROM [dbo].[수검자] p WHERE p.[ChartNo] = 'T011';
GO
DECLARE @W11 BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[PatientId] = w.[수검자ID]
                       WHERE p.[ChartNo] = 'T011' AND w.[상태코드] = 'RCP' ORDER BY w.[업무ID] DESC);
DECLARE @P11 BIGINT = (SELECT [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = 'T011');
INSERT INTO [dbo].[검사항목] ([업무ID], [검사항목코드], [검사출처코드])
SELECT @W11, n.[ExamCode], 'NEX' FROM [dbo].[UFN_HC_국가검사구성](@P11, '2026-10-01') n;

DECLARE @F11 INT = 0;
IF EXISTS (SELECT 1 FROM [dbo].[검사항목]
            WHERE [업무ID] = @W11 AND [검사출처코드] = 'NEX' AND [검사항목코드] = 'EX012')
    PRINT 'PASS FIX-RCP-003 T011 저장 NEX 에 EX012 포함 (412 시험 사전조건)';
ELSE BEGIN PRINT N'FAIL FIX-RCP-003 T011 저장 NEX 에 EX012 가 없다 — 412 를 관측할 수 없다'; SET @F11 += 1; END
IF @F11 > 0 THROW 51000, N'T011 RCP Fixture 사전조건 실패', 1;
GO
PRINT '=== 00b_Test_Harness_RCP 완료 ===';
GO
