SET NOCOUNT ON;
-- PWR-001 과 완전히 같은 인자. 기존 수검자를 쓴다 (Code=2, Success=1).
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'신규수검자', '9001011000018',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
GO
