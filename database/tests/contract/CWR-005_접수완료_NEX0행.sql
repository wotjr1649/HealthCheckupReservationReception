SET NOCOUNT ON;
-- CORRUPT-2. 검사구성 무결성은 503 보다 앞이라 예약일이 미래여도 701 이다 (05 §12.1).
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                      JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                     WHERE p.[차트번호] = N'T013' AND w.[상태코드] = 'RSV');
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_UPDATE_접수완료] @W, @Rv, N'TEST';
GO
