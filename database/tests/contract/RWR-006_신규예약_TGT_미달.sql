SET NOCOUNT ON;
-- T017(2007-11-18생)은 2026-11-17 에 만 18세라 검진 대상이 아니다 (실측).
DECLARE @P18 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T017');
EXEC [dbo].[USP_HC_예약_등록] @P18, 'NORMAL', '2026-11-17', 'AM', 0,0,0,0,0,0,0, N'TEST';
GO
