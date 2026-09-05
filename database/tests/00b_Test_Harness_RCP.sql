SET NOCOUNT ON;
PRINT '--- 00b_Test_Harness_RCP 시작 ---';
GO
-- 재실행 가능하도록 기존 RCP fixture 를 먼저 제거한다
DELETE d FROM [dbo].[검사항목] d
  JOIN [dbo].[예약접수] w ON w.[업무ID] = d.[업무ID]
  JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
 WHERE p.[차트번호] = 'T014' AND w.[상태코드] = 'RCP';
DELETE w FROM [dbo].[예약접수] w
  JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
 WHERE p.[차트번호] = 'T014' AND w.[상태코드] = 'RCP';
GO
-- T014 (남, 기준일 2026-10-01 에 만 56세) 의 RCP Work
--   ReservationDate 와 NEX 산출 기준일을 동일하게 '2026-10-01' 로 맞춘다.
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드])
SELECT p.[수검자ID], '2026-10-01', 'AM', 'RCP'
FROM [dbo].[수검자] p WHERE p.[차트번호] = 'T014';
GO
DECLARE @Wrcp BIGINT, @Prcp BIGINT;
SELECT TOP (1) @Wrcp = w.[업무ID], @Prcp = w.[수검자ID]
  FROM [dbo].[예약접수] w
  JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
 WHERE p.[차트번호] = 'T014' AND w.[상태코드] = 'RCP'
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
 JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
 WHERE p.[차트번호] = 'T011' AND w.[상태코드] = 'RCP';
DELETE w FROM [dbo].[예약접수] w
 JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
 WHERE p.[차트번호] = 'T011' AND w.[상태코드] = 'RCP';
GO
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드])
SELECT p.[수검자ID], '2026-10-01', 'AM', 'RCP'
FROM [dbo].[수검자] p WHERE p.[차트번호] = 'T011';
GO
DECLARE @W11 BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                       WHERE p.[차트번호] = 'T011' AND w.[상태코드] = 'RCP' ORDER BY w.[업무ID] DESC);
DECLARE @P11 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = 'T011');
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
-- 검사구성 문자열을 검사항목에서 유도한다. 손으로 적지 않는다 (plans/10 §5 T50).
-- 검사코드를 코드 오름차순으로 19번 돌며 덧붙이므로 순서가 결정적이다.
DECLARE @Codes TABLE (C VARCHAR(10) PRIMARY KEY);
INSERT INTO @Codes (C) SELECT [검사항목코드] FROM [dbo].[검사코드];
UPDATE [dbo].[예약접수] SET [국가검사항목] = N'', [추가검사항목] = N'';
DECLARE @C VARCHAR(10);
WHILE EXISTS (SELECT 1 FROM @Codes)
BEGIN
    SELECT TOP (1) @C = C FROM @Codes ORDER BY C;
    UPDATE w SET [국가검사항목] = w.[국가검사항목] + @C + N','
      FROM [dbo].[예약접수] w
     WHERE EXISTS (SELECT 1 FROM [dbo].[검사항목] d
                    WHERE d.[업무ID] = w.[업무ID] AND d.[검사항목코드] = @C AND d.[검사출처코드] = 'NEX');
    UPDATE w SET [추가검사항목] = w.[추가검사항목] + @C + N','
      FROM [dbo].[예약접수] w
     WHERE EXISTS (SELECT 1 FROM [dbo].[검사항목] d
                    WHERE d.[업무ID] = w.[업무ID] AND d.[검사항목코드] = @C AND d.[검사출처코드] = 'AEX');
    DELETE FROM @Codes WHERE C = @C;
END
UPDATE [dbo].[예약접수]
   SET [국가검사항목] = LEFT([국가검사항목], CASE WHEN LEN([국가검사항목]) = 0 THEN 0 ELSE LEN([국가검사항목]) - 1 END)
     , [추가검사항목] = NULLIF(LEFT([추가검사항목], CASE WHEN LEN([추가검사항목]) = 0 THEN 0 ELSE LEN([추가검사항목]) - 1 END), N'');
GO

-- 문자열과 검사항목이 양방향으로 일치하는가. 한쪽만 보면 누락도 잉여도 못 잡는다.
DECLARE @Drift INT = 0;
SELECT @Drift = COUNT(*) FROM (
    SELECT w.[업무ID], d.[검사항목코드] FROM [dbo].[검사항목] d
      JOIN [dbo].[예약접수] w ON w.[업무ID] = d.[업무ID]
     WHERE d.[검사출처코드] = 'NEX'
       AND N',' + ISNULL(w.[국가검사항목], N'') + N',' NOT LIKE N'%,' + d.[검사항목코드] + N',%'
    UNION ALL
    SELECT w.[업무ID], m.[검사항목코드] FROM [dbo].[예약접수] w
     CROSS JOIN [dbo].[검사코드] m
     WHERE N',' + ISNULL(w.[국가검사항목], N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
       AND NOT EXISTS (SELECT 1 FROM [dbo].[검사항목] d
                        WHERE d.[업무ID] = w.[업무ID] AND d.[검사항목코드] = m.[검사항목코드]
                          AND d.[검사출처코드] = 'NEX')
) a;
IF (@Drift = 0) PRINT 'PASS FIX-EXAM-001 국가검사항목 문자열 = 검사항목 NEX (양방향)';
ELSE BEGIN PRINT 'FAIL FIX-EXAM-001 국가검사항목 문자열 어긋남'; THROW 51010, N'국가검사항목 유도 실패', 1; END

SELECT @Drift = COUNT(*) FROM (
    SELECT w.[업무ID], d.[검사항목코드] FROM [dbo].[검사항목] d
      JOIN [dbo].[예약접수] w ON w.[업무ID] = d.[업무ID]
     WHERE d.[검사출처코드] = 'AEX'
       AND N',' + ISNULL(w.[추가검사항목], N'') + N',' NOT LIKE N'%,' + d.[검사항목코드] + N',%'
    UNION ALL
    SELECT w.[업무ID], m.[검사항목코드] FROM [dbo].[예약접수] w
     CROSS JOIN [dbo].[검사코드] m
     WHERE N',' + ISNULL(w.[추가검사항목], N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
       AND NOT EXISTS (SELECT 1 FROM [dbo].[검사항목] d
                        WHERE d.[업무ID] = w.[업무ID] AND d.[검사항목코드] = m.[검사항목코드]
                          AND d.[검사출처코드] = 'AEX')
) a;
IF (@Drift = 0) PRINT 'PASS FIX-EXAM-002 추가검사항목 문자열 = 검사항목 AEX (양방향)';
ELSE BEGIN PRINT 'FAIL FIX-EXAM-002 추가검사항목 문자열 어긋남'; THROW 51011, N'추가검사항목 유도 실패', 1; END
GO
