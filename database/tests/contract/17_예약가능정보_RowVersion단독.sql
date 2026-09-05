SET NOCOUNT ON;
-- WorkId 는 NULL 인데 RowVersion 이 있다 (05 §9.3 오류조합) → 102
DECLARE @Pn BIGINT = (SELECT [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = N'T015');
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @Pn, NULL, 0x0000000000000001, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0;
GO
