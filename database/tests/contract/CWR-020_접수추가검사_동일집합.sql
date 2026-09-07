SET NOCOUNT ON;
-- T014 의 저장 AEX 는 EX014(OPT01)다. 동일집합이면 결과코드=1 이고 Master Rule 을 재평가하지 않는다 (05 §12.2).
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID] WHERE p.[차트번호] = N'T014' AND w.[상태코드] IN ('RCP','CNC'));
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_접수추가검사_변경] @W, @Rv, 1,0,0,0,0,0,0, N'TEST';
GO
