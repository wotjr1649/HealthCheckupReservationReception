SET NOCOUNT ON;
-- 휴무일은 SP 실패가 아니다 (05 §3.3). RS0 Code=0 + RS1 CanSave=0 + BlockCode=302 다.
DECLARE @Pn BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @Pn, NULL, NULL, 'NORMAL', '2026-12-25', 'AM', 0,0,0,0,0,0,0;
GO
