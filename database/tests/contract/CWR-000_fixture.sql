SET NOCOUNT ON;
-- CWR 계약 시나리오는 순서 의존이다(glob 사전순 = ID 순). CWR-099 가 뒤처리를 한다.
-- tests/07 과 같은 복원을 앞에서도 해 두어 게이트를 두 번 돌려도 판정이 같게 만든다. Result Set 은 없다.
DELETE FROM [dbo].[예약접수]
 WHERE [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] IN (N'T009', N'T010'));
UPDATE [dbo].[예약접수] SET [상태코드] = 'RCP', [추가검사항목] = N'EX014'
 WHERE [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T014');
UPDATE [dbo].[예약접수] SET [추가검사항목] = NULL
 WHERE [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T011');
GO
