SET NOCOUNT ON;
-- WALKIN 인데 요청일이 DB 오늘이 아니다 → 102
DECLARE @Pn BIGINT = (SELECT [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = N'T015');
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @Pn, NULL, NULL, 'WALKIN', '2026-11-16', 'AM', 0,0,0,0,0,0,0;
GO
