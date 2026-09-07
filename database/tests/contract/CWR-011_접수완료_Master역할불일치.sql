SET NOCOUNT ON;
-- CORRUPT-4. AEX 자리에 NEX 전용 코드(EX001)가 저장된 역할 불일치다 (스펙 §21.2a-(2)).
DECLARE @Basic NVARCHAR(100) = N'', @BC VARCHAR(10);
DECLARE @B TABLE (C VARCHAR(10) PRIMARY KEY);
INSERT INTO @B (C) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] = 'NEX-01';
WHILE EXISTS (SELECT 1 FROM @B) BEGIN SELECT TOP (1) @BC = C FROM @B ORDER BY C; SET @Basic = @Basic + @BC + N','; DELETE FROM @B WHERE C = @BC; END
SET @Basic = LEFT(@Basic, LEN(@Basic) - 1);
DECLARE @P BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T009');
DELETE FROM [dbo].[예약접수] WHERE [수검자ID] = @P;
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
VALUES (@P, CONVERT(DATE, SYSDATETIME()), 'PM', 'RSV', @Basic, N'EX001');
DECLARE @W BIGINT = SCOPE_IDENTITY();
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_UPDATE_접수완료] @W, @Rv, N'TEST';
DELETE FROM [dbo].[예약접수] WHERE [업무ID] = @W;
GO
