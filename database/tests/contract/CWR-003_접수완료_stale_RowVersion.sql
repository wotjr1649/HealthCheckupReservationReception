SET NOCOUNT ON;
-- 601 은 마감 판정보다 앞이라 시각과 무관하게 성립한다 (05 §12.1 검증순서).
DECLARE @Basic NVARCHAR(100) = N'', @BC VARCHAR(10);
DECLARE @B TABLE (C VARCHAR(10) PRIMARY KEY);
INSERT INTO @B (C) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] = 'NEX-01';
WHILE EXISTS (SELECT 1 FROM @B) BEGIN SELECT TOP (1) @BC = C FROM @B ORDER BY C; SET @Basic = @Basic + @BC + N','; DELETE FROM @B WHERE C = @BC; END
SET @Basic = LEFT(@Basic, LEN(@Basic) - 1);
DECLARE @P BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T010');
DELETE FROM [dbo].[예약접수] WHERE [수검자ID] = @P;
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
VALUES (@P, CONVERT(DATE, SYSDATETIME()),
        CASE WHEN CONVERT(TIME(0), SYSDATETIME()) < '11:00:00' THEN 'AM' ELSE 'PM' END,
        'RSV', @Basic, NULL);
DECLARE @W BIGINT = SCOPE_IDENTITY();
EXEC [dbo].[USP_HC_UPDATE_접수완료] @W, 0x0000000000000001, N'TEST';
GO
