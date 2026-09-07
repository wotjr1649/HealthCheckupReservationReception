SET NOCOUNT ON;
-- 7번째 자리가 'A'. 13자리이지만 숫자가 아니다 (05 §17.6).
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'비숫자', '900101A000018',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
GO
