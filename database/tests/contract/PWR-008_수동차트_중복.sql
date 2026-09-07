SET NOCOUNT ON;
-- T001 은 tests/00 Fixture 의 차트번호다.
EXEC [dbo].[USP_HC_INSERT_수검자] 0, N'T001', N'중복차트', '9101011000015',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
GO
