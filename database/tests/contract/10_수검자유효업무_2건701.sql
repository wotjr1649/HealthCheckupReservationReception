SET NOCOUNT ON;
-- T012 는 CORRUPT-1 로 유효업무가 2건이다 (tests/00 Step 5). RP-06 위반이므로 701 이다.
DECLARE @P2 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T012');
EXEC [dbo].[USP_HC_수검자유효업무_조회] @P2;
GO
