SET NOCOUNT ON;
-- T020 의 RSV Work(2026-11-17 AM, AEX=OPT04)에서 시간대만 PM 으로 바꾼다 → Scope=SLOT
-- 05 §9.11: RS2 2 / RS3 0 / RS4 0 / RS5 0
DECLARE @P BIGINT, @W BIGINT, @RV BINARY(8);
SELECT TOP (1) @P = w.[PatientId], @W = w.[WorkId], @RV = w.[RowVersion]
  FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[PatientId] = w.[PatientId]
 WHERE p.[ChartNo] = N'T020' AND w.[StatusCode] = 'RSV' ORDER BY w.[WorkId];
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @P, @W, @RV, 'NORMAL', '2026-11-17', 'PM', 0,0,0,1,0,0,0;
GO
