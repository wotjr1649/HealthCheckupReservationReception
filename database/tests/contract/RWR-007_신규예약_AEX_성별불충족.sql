SET NOCOUNT ON;
-- OPT03(EX016)은 여성 전용이다. T015 는 남성이다.
DECLARE @Pt BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-11-17', 'AM', 0,0,1,0,0,0,0, N'TEST';
GO
