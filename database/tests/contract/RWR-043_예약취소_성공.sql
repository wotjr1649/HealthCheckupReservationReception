SET NOCOUNT ON;
-- ORDER BY 업무ID 로 F001 을 고정한다. F020 은 정원 시나리오 전용이라 건드리지 않는다.
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                      JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                     WHERE p.[차트번호] LIKE 'F0%' AND p.[차트번호] <> N'F020'
                       AND w.[상태코드] = 'RSV' ORDER BY w.[업무ID]);
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_UPDATE_예약취소] @W, @Rv, N'TEST';
GO
