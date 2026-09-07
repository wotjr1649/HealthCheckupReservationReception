SET NOCOUNT ON;
-- T011(여 만 54세)의 저장 NEX 에 EX012 가 있다. 그것을 OPT04 로 다시 요청하면 412 다.
-- 412 는 EX012 로만 발생한다 - Seed 19행 중 NEX·AEX 역할을 동시에 갖는 행이 그것뿐이다 (스펙 §17.2a).
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                      JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                     WHERE p.[차트번호] = N'T011' AND w.[상태코드] = 'RCP');
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_접수추가검사_변경] @W, @Rv, 0,0,0,1,0,0,0, N'TEST';
GO
