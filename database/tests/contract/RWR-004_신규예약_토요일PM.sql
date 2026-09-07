SET NOCOUNT ON;
-- 토요일 오후는 운영하지 않는다 (00 §3장).
DECLARE @Pt BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-11-21', 'PM', 0,0,0,0,0,0,0, N'TEST';
GO
