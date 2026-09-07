SET NOCOUNT ON;
-- F003 의 Work 는 2026-11-16 이다. 예약일이 오늘인 업무만 접수할 수 있다 (05 §12.1).
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                      JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                     WHERE p.[차트번호] = N'F003' AND w.[상태코드] = 'RSV');
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_접수_완료] @W, @Rv, N'TEST';
GO
