SET NOCOUNT ON;
-- 날짜·시간대는 그대로 두고 AEX 만 뺀다 (OPT04 해제) → Scope=EXTRA
-- 05 §9.11: RS2 0 / RS3 0 / RS4 0 / RS5 7
DECLARE @P BIGINT, @W BIGINT, @RV BINARY(8);
SELECT TOP (1) @P = w.[수검자ID], @W = w.[업무ID], @RV = w.[행버전]
  FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[PatientId] = w.[수검자ID]
 WHERE p.[ChartNo] = N'T020' AND w.[상태코드] = 'RSV' ORDER BY w.[업무ID];
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @P, @W, @RV, 'NORMAL', '2026-11-17', 'AM', 0,0,0,0,0,0,0;
GO
