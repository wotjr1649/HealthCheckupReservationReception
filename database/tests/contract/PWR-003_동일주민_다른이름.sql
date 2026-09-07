SET NOCOUNT ON;
-- 실패(202)인데 RS1 을 동반하는 두 경우 중 하나다 (05 §3.5).
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'다른이름', '9001011000018',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
GO
