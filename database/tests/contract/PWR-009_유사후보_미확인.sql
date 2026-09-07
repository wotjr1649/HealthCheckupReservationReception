SET NOCOUNT ON;
-- T001 과 이름·산출 생년월일(20061002)이 같고 주민번호만 다르다.
-- 실패(203)인데 RS1 후보를 동반하는 두 번째 경우다 (05 §3.5).
EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, N'테스트일구', '0610024000015',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
GO
