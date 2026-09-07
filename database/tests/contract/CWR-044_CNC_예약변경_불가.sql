SET NOCOUNT ON;
-- CNC 는 예약변경으로도 되살아나지 않는다 (05 §12.3).
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID] WHERE p.[차트번호] = N'T014' AND w.[상태코드] IN ('RCP','CNC'));
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W, @Rv, '2026-11-18', 'AM', 0,0,0,0,0,0,0, N'TEST';
GO
