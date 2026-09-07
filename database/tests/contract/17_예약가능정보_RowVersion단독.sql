SET NOCOUNT ON;
-- 업무ID 는 NULL 인데 행버전 이 있다 (05 §9.3 오류조합) → 102
DECLARE @Pn BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_예약가능정보_조회] @Pn, NULL, 0x0000000000000001, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0;
GO
