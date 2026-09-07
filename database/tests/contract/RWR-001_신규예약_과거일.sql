SET NOCOUNT ON;
DECLARE @Pt BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_예약_등록] @Pt, 'NORMAL', '2020-01-06', 'AM', 0,0,0,0,0,0,0, N'TEST';
GO
