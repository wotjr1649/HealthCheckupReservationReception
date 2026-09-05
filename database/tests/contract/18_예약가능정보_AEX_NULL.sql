SET NOCOUNT ON;
-- AEX BIT 중 하나가 NULL → 100
DECLARE @Pn BIGINT = (SELECT [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = N'T015');
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @Pn, NULL, NULL, 'NORMAL', '2026-11-16', 'AM', NULL,0,0,0,0,0,0;
GO
