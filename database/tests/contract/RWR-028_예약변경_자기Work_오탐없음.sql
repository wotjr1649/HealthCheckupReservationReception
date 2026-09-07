SET NOCOUNT ON;
-- 유효업무가 자기 자신 1건뿐인 Work 의 시간대 변경. 306 이 나오면 스펙 §30 의
-- "현재 업무ID 제외" 가 빠진 것이다.
DECLARE @W BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID] WHERE p.[차트번호] = N'T015' AND w.[상태코드] = 'RSV' ORDER BY w.[업무ID] DESC);
DECLARE @Rv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_예약_변경] @W, @Rv, '2026-11-19', 'PM', 1,0,0,0,0,0,0, N'TEST';
GO
