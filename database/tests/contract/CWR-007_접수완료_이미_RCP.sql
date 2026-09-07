SET NOCOUNT ON;
-- T014 의 RCP Work 를 쓴다. CWR-006 의 성공 여부에 기대지 않는다.
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID] WHERE p.[차트번호] = N'T014' AND w.[상태코드] = 'RCP');
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_UPDATE_접수완료] @W, @Rv, N'TEST';
GO
