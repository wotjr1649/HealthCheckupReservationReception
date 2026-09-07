SET NOCOUNT ON;
-- F020 을 RSV 로 뒤집어 2026-11-16 AM 을 20/20 으로 만든 뒤 21번째를 시도하고 되돌린다.
-- 대상 수검자는 유효업무가 없어야 한다 - 있으면 305 보다 306 이 먼저 걸린다 (05 §11.1).
DECLARE @Pf BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'F020');
DECLARE @Pc BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T018');
UPDATE [dbo].[예약접수] SET [상태코드] = 'RSV' WHERE [수검자ID] = @Pf;
EXEC [dbo].[USP_HC_예약_등록] @Pc, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0, N'TEST';
UPDATE [dbo].[예약접수] SET [상태코드] = 'CNR' WHERE [수검자ID] = @Pf;
GO
