SET NOCOUNT ON;
-- T012 는 CORRUPT-1 로 유효업무가 2건이다 (tests/00 Step 5). RP-06 위반이므로 701 이다.
DECLARE @P2 BIGINT = (SELECT [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = N'T012');
EXEC [dbo].[USP_HC_SELECT_수검자유효업무] @P2;
GO
