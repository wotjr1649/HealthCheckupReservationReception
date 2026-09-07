SET NOCOUNT ON;
-- RWR-008 이 만든 Work 가 있으므로 재예약은 306 이다 (00 RP-06).
DECLARE @Pt BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-11-18', 'AM', 0,0,0,0,0,0,0, N'TEST';
GO
