SET NOCOUNT ON;
-- CORRUPT-1. T012 는 유효업무가 2건이라 306 이 아니라 701 이다 (스펙 §30).
DECLARE @P2 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T012');
EXEC [dbo].[USP_HC_INSERT_예약] @P2, 'NORMAL', '2026-11-19', 'AM', 0,0,0,0,0,0,0, N'TEST';
GO
