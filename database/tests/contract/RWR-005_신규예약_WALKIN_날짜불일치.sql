SET NOCOUNT ON;
-- WalkIn 은 DB Today 만 허용한다 (05 §11.1).
DECLARE @Pt BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'WALKIN', '2026-11-16', 'AM', 0,0,0,0,0,0,0, N'TEST';
GO
