SET NOCOUNT ON;
DECLARE @P2 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T012');
EXEC [dbo].[USP_HC_예약_등록] @P2, 'NORMAL', '2026-11-25', 'AM', 0,0,0,0,0,0,0, N'TEST';
GO
