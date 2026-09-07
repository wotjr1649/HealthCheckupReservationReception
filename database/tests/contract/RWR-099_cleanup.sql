SET NOCOUNT ON;
-- RWR 시나리오가 바꾼 상태를 되돌린다. SEL-021~024 와 다음 회차의 01~25·PWR-* 가
-- T015 의 유효업무 0건 · 2026-11 Work 24건 · F001 의 RSV 를 전제한다. Result Set 은 없다.
DELETE FROM [dbo].[예약접수]
 WHERE [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자]
                       WHERE [차트번호] IN (N'T015', N'T016', N'T017', N'T018'));
UPDATE [dbo].[예약접수] SET [상태코드] = 'RSV'
 WHERE [예약일] = '2026-11-16' AND [시간대코드] = 'AM' AND [상태코드] = 'CNR'
   AND [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자]
                       WHERE [차트번호] LIKE 'F0%' AND [차트번호] <> N'F020');
UPDATE [dbo].[예약접수] SET [상태코드] = 'CNR'
 WHERE [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'F020');
UPDATE [dbo].[예약접수] SET [추가검사항목] = NULL
 WHERE [추가검사항목] IS NOT NULL
   AND [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] LIKE 'F0%');
UPDATE [dbo].[예약접수] SET [예약일] = '2026-11-17', [시간대코드] = 'AM', [추가검사항목] = N'EX012'
 WHERE [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T020');
GO
