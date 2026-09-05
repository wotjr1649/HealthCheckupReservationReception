SET NOCOUNT ON;
-- 신규예약이므로 Scope=ALL. 05 §9.11: RS2 2 / RS3 1 / RS4 8~11 / RS5 7
DECLARE @Pn BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @Pn, NULL, NULL, 'NORMAL', '2026-11-16', 'AM', 1,0,0,0,0,0,1;
GO
