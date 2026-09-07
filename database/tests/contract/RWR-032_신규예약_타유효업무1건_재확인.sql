SET NOCOUNT ON;
-- 1건과 2건은 다른 결과코드 다. 이 파일이 1건, RWR-033 이 2건이다 (스펙 §30).
DECLARE @Pt BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_예약_등록] @Pt, 'NORMAL', '2026-11-24', 'AM', 0,0,0,0,0,0,0, N'TEST';
GO
