SET NOCOUNT ON;
-- F020 은 Fixture 상 CNR 이다. RSV 가 아닌 Work 는 502 다.
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID] WHERE p.[차트번호] = N'F020');
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W, @Rv, '2026-11-18', 'PM', 0,0,0,0,0,0,0, N'TEST';
GO
