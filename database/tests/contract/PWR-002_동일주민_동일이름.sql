SET NOCOUNT ON;
-- PWR-001 과 완전히 같은 인자. 기존 수검자를 쓴다 (결과코드=2, 성공여부=1).
EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, N'신규수검자', '9001011000018',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
GO
