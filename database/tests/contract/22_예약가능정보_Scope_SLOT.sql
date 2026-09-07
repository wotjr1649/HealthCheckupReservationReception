SET NOCOUNT ON;
-- T020 의 RSV Work(2026-11-17 AM, AEX=OPT04)에서 시간대만 PM 으로 바꾼다 → 변경범위=SLOT
-- 05 §9.11: RS2 2 / RS3 0 / RS4 0 / RS5 0
DECLARE @P BIGINT, @W BIGINT, @RV BINARY(8);
SELECT TOP (1) @P = w.[수검자ID], @W = w.[업무ID], @RV = w.[행버전]
  FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
 WHERE p.[차트번호] = N'T020' AND w.[상태코드] = 'RSV' ORDER BY w.[업무ID];
EXEC [dbo].[USP_HC_예약가능정보_조회] @P, @W, @RV, 'NORMAL', '2026-11-17', 'PM', 0,0,0,1,0,0,0;
GO
