SET NOCOUNT ON;
-- 2026-11-19 는 목요일이다. 시간대·AEX 는 그대로 두어 DateChanged 만 1 이 된다.
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID] WHERE p.[차트번호] = N'T015' AND w.[상태코드] = 'RSV' ORDER BY w.[업무ID] DESC);
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W, @Rv, '2026-11-19', 'PM', 1,1,0,0,0,0,0, N'TEST';
GO
