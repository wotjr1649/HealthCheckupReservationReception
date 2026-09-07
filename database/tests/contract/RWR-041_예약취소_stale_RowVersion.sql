SET NOCOUNT ON;
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                      JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                     WHERE p.[차트번호] LIKE 'F0%' AND p.[차트번호] <> N'F020'
                       AND w.[상태코드] = 'RSV' ORDER BY w.[업무ID]);
EXEC [dbo].[USP_HC_UPDATE_예약취소] @W, 0x0000000000000001, N'TEST';
GO
