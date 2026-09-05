SET NOCOUNT ON;
-- DB 현재값과 요청값이 완전히 같다 → Scope=NONE. 조회 SP 의 정상 결과다 (RS0 Code=0, Code=1 이 아니다).
-- 05 §9.11: RS2 0 / RS3 0 / RS4 0 / RS5 0
DECLARE @P BIGINT, @W BIGINT, @RV BINARY(8);
SELECT TOP (1) @P = w.[수검자ID], @W = w.[업무ID], @RV = w.[행버전]
  FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[PatientId] = w.[수검자ID]
 WHERE p.[ChartNo] = N'T020' AND w.[상태코드] = 'RSV' ORDER BY w.[업무ID];
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @P, @W, @RV, 'NORMAL', '2026-11-17', 'AM', 0,0,0,1,0,0,0;
GO
