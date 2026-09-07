SET NOCOUNT ON;
-- CORRUPT-5. 저장 NEX 를 13종으로 만들어 상한 11 을 넘긴 뒤 변경을 시도하고 되돌린다 (스펙 §21.2a).
-- 소비 시나리오 안에서 만들고 즉시 복원한다 - Fixture 파일에 두면 뒤 시험이 전부 오염된다.
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                      JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                     WHERE p.[차트번호] = N'F002' AND w.[상태코드] = 'RSV');
DECLARE @Save NVARCHAR(100) = (SELECT [국가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
DECLARE @All NVARCHAR(100) = N'', @코드 VARCHAR(10);
DECLARE @N TABLE ([코드] VARCHAR(10) PRIMARY KEY);
INSERT INTO @N ([코드]) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] IS NOT NULL;
WHILE EXISTS (SELECT 1 FROM @N)
BEGIN
    SELECT TOP (1) @코드 = [코드] FROM @N ORDER BY [코드];
    SET @All = @All + @코드 + N',';
    DELETE FROM @N WHERE [코드] = @코드;
END
UPDATE [dbo].[예약접수] SET [국가검사항목] = LEFT(@All, LEN(@All) - 1) WHERE [업무ID] = @W;
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_예약_변경] @W, @Rv, '2026-11-19', 'AM', 0,0,0,0,0,0,0, N'TEST';
UPDATE [dbo].[예약접수] SET [국가검사항목] = @Save WHERE [업무ID] = @W;
GO
