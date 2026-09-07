SET NOCOUNT ON;
-- PWR-009 와 같은 인자에 @유사수검자확인여부=1 만 다르다.
EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, N'테스트일구', '0610024000015',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, N'TEST';
GO
