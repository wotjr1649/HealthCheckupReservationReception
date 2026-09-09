SET NOCOUNT ON;
-- [R13] AM Work 다. 접수 AM 마감을 이 시나리오 동안만 앞당겨 하루 중 언제 돌려도 마감 경과가
--       되게 한다 (04 §8.7 [운영기준]). 예전에는 11:10 이후 회차에서만 판정됐다.
--       [운영기준] 을 되돌리는 것은 이 파일의 책임이고, 되돌아왔는지는 회차 끝에서
--       scripts/verify-operating-baseline.sh OPR-G4 가 따로 판정한다.
DECLARE @Basic NVARCHAR(100) = N'', @BC VARCHAR(10);
DECLARE @B TABLE ([코드] VARCHAR(10) PRIMARY KEY);
INSERT INTO @B ([코드]) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] = 'NEX-01';
WHILE EXISTS (SELECT 1 FROM @B) BEGIN SELECT TOP (1) @BC = [코드] FROM @B ORDER BY [코드]; SET @Basic = @Basic + @BC + N','; DELETE FROM @B WHERE [코드] = @BC; END
SET @Basic = LEFT(@Basic, LEN(@Basic) - 1);
DECLARE @OprAm TIME(0) = (SELECT [접수AM마감] FROM [dbo].[운영기준] WHERE [기준ID] = 1);
UPDATE [dbo].[운영기준] SET [접수AM마감] = '00:00:01' WHERE [기준ID] = 1;
DECLARE @P BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T010');
DELETE FROM [dbo].[예약접수] WHERE [수검자ID] = @P;
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
VALUES (@P, CONVERT(DATE, SYSDATETIME()), 'AM', 'RSV', @Basic, NULL);
DECLARE @W BIGINT = SCOPE_IDENTITY();
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_접수_완료] @W, @Rv, N'TEST';
UPDATE [dbo].[운영기준] SET [접수AM마감] = @OprAm WHERE [기준ID] = 1;
GO
