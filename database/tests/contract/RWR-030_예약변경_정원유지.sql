SET NOCOUNT ON;
-- 20/20 Slot 에 있는 Work 의 AEX 변경이 305 로 막히면 자기 자신을 정원에 이중계상한 것이다 (스펙 §30.1).
DECLARE @Pf BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'F020');
UPDATE [dbo].[예약접수] SET [상태코드] = 'RSV' WHERE [수검자ID] = @Pf;
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                      JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                     WHERE p.[차트번호] = N'F003' AND w.[상태코드] = 'RSV');
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W, @Rv, '2026-11-16', 'AM', 1,0,0,0,0,0,0, N'TEST';
UPDATE [dbo].[예약접수] SET [상태코드] = 'CNR' WHERE [수검자ID] = @Pf;
GO
