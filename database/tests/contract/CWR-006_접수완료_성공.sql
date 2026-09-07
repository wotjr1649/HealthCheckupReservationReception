SET NOCOUNT ON;
-- [!] 접수마감(AM 11:00 / PM 16:00) 전에만 성립한다. verify-contract-all.sh 가 마감 후에는 SKIP 한다.
--     CWR-009 와 배타적이다.
DECLARE @Basic NVARCHAR(100) = N'', @BC VARCHAR(10);
DECLARE @B TABLE ([코드] VARCHAR(10) PRIMARY KEY);
INSERT INTO @B ([코드]) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] = 'NEX-01';
WHILE EXISTS (SELECT 1 FROM @B) BEGIN SELECT TOP (1) @BC = [코드] FROM @B ORDER BY [코드]; SET @Basic = @Basic + @BC + N','; DELETE FROM @B WHERE [코드] = @BC; END
SET @Basic = LEFT(@Basic, LEN(@Basic) - 1);
DECLARE @P BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T010');
DELETE FROM [dbo].[예약접수] WHERE [수검자ID] = @P;
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
VALUES (@P, CONVERT(DATE, SYSDATETIME()),
        CASE WHEN CONVERT(TIME(0), SYSDATETIME()) < '11:00:00' THEN 'AM' ELSE 'PM' END,
        'RSV', @Basic, NULL);
DECLARE @W BIGINT = SCOPE_IDENTITY();
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_접수_완료] @W, @Rv, N'TEST';
GO
