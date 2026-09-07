SET NOCOUNT ON;
-- 900231 = 2월 31일. 달력에 없는 날짜다.
EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, N'잘못된날짜', '9002311000012',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
GO
