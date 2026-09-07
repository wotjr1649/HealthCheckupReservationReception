SET NOCOUNT ON;
-- [!] 400 UnderAge 는 예약변경으로 도달할 수 없다 - 미래로 옮길수록 나이가 늘기 때문이다.
--     401 NotDue 로 시험한다. T016 은 2025-05-01 완료이력이 있어 2026 의 어떤 예약일에도
--     2년 주기가 도래하지 않는다. TGT 비대상은 정상 SP 로 Work 를 만들 수 없어 직접 심는다.
DECLARE @Pn BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T016');
DECLARE @Basic NVARCHAR(100) = N'', @BC VARCHAR(10);
DECLARE @B TABLE (C VARCHAR(10) PRIMARY KEY);
INSERT INTO @B (C) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] = 'NEX-01';
WHILE EXISTS (SELECT 1 FROM @B)
BEGIN
    SELECT TOP (1) @BC = C FROM @B ORDER BY C;
    SET @Basic = @Basic + @BC + N',';
    DELETE FROM @B WHERE C = @BC;
END
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
VALUES (@Pn, '2026-11-24', 'AM', 'RSV', LEFT(@Basic, LEN(@Basic) - 1), NULL);
DECLARE @W BIGINT = SCOPE_IDENTITY();
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W, @Rv, '2026-11-25', 'AM', 0,0,0,0,0,0,0, N'TEST';
DELETE FROM [dbo].[예약접수] WHERE [업무ID] = @W;
GO
