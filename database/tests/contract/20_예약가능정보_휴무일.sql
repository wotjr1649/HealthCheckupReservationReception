SET NOCOUNT ON;
-- 휴무일은 SP 실패가 아니다 (05 §3.3). RS0 결과코드=0 + RS1 저장가능=0 + 차단코드=302 다.
DECLARE @Pn BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_예약가능정보_조회] @Pn, NULL, NULL, 'NORMAL', '2026-12-25', 'AM', 0,0,0,0,0,0,0;
GO
