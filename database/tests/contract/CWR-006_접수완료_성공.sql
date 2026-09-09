SET NOCOUNT ON;
-- [R13] 접수마감을 이 시나리오 동안만 늦춘다 (04 §8.7 [운영기준]). 예전에는 시계가 마감 뒤면
--       verify-contract-all.sh 가 SKIP 했고, SKIP 은 PASS 가 아니다 (CLAUDE.md §10).
--       마감 **경계** 는 RUL-T07·T08·T11·T12 가 TVF 에 시각을 주입해 진짜 값으로 잰다 —
--       여기서 재는 것은 경계가 아니라 '마감 전이면 RCP 로 전이한다' 다. CWR-009 와 배타적이지 않다.
DECLARE @OprAm TIME(0), @OprPm TIME(0);
SELECT @OprAm = [접수AM마감], @OprPm = [접수PM마감] FROM [dbo].[운영기준] WHERE [기준ID] = 1;
UPDATE [dbo].[운영기준] SET [접수AM마감] = '23:59:59', [접수PM마감] = '23:59:59' WHERE [기준ID] = 1;
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
UPDATE [dbo].[운영기준] SET [접수AM마감] = @OprAm, [접수PM마감] = @OprPm WHERE [기준ID] = 1;
GO
