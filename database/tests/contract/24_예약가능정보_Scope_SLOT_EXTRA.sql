SET NOCOUNT ON;
-- 시간대와 AEX 를 동시에 바꾼다 → Scope=SLOT_EXTRA
-- 05 §9.11: RS2 2 / RS3 0 / RS4 0 / RS5 7
DECLARE @P BIGINT, @W BIGINT, @RV BINARY(8);
SELECT TOP (1) @P = w.[수검자ID], @W = w.[업무ID], @RV = w.[행버전]
  FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[PatientId] = w.[수검자ID]
 WHERE p.[ChartNo] = N'T020' AND w.[상태코드] = 'RSV' ORDER BY w.[업무ID];
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @P, @W, @RV, 'NORMAL', '2026-11-17', 'PM', 0,0,0,0,0,0,0;
GO
