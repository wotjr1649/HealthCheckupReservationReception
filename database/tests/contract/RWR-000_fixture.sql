SET NOCOUNT ON;
-- RWR 계약 시나리오는 순서 의존이다(glob 사전순 = ID 순). 이 파일이 전제를 매번 되돌리고
-- RWR-099 가 뒤처리를 한다. 그래야 게이트를 두 번 돌려도 같은 판정이 나온다. Result Set 은 없다.
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
