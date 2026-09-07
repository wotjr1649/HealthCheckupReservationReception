SET NOCOUNT ON;
-- T020(1972-11-20생 여)은 2026-11-17 에 만 53세, 2026-11-20 에 만 54세다(실측).
-- 만 54세가 되면 NEX-05 로 EX012 가 생겨 이미 선택된 OPT04(EX012)가 412 다.
-- 변경 **전** 구성으로 판정하면 이 시나리오가 조용히 통과한다.
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                      JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                     WHERE p.[차트번호] = N'T020' AND w.[상태코드] = 'RSV');
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W, @Rv, '2026-11-20', 'AM', 0,0,0,1,0,0,0, N'TEST';
GO
