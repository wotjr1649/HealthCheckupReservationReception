SET NOCOUNT ON;
-- CWR 시나리오가 바꾼 상태를 되돌린다. 뒤따르는 PWR-027(T014 의 RCP 보유 전제)과
-- 다음 회차의 01~25 가 이 상태를 전제한다. Result Set 은 없다.
DELETE FROM [dbo].[예약접수]
 WHERE [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] IN (N'T009', N'T010'));
UPDATE [dbo].[예약접수] SET [상태코드] = 'RCP', [추가검사항목] = N'EX014'
 WHERE [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T014');
UPDATE [dbo].[예약접수] SET [추가검사항목] = NULL
 WHERE [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T011');
GO
