SET NOCOUNT ON;
-- 대상 행이 없거나 기록이 0건이면 결과코드=0 + RS1 0행이다. 200 PatientNotFound 를 쓰지 않는다 —
-- 이 SP 는 대상 행의 존재를 확인하지 않는다 (05 §8.3 계약 경계 · 04 §8.6.3).
EXEC [dbo].[USP_HC_변경이력_조회] N'예약접수', -1;
GO
