SET NOCOUNT ON;
-- 인자는 파일 안에서 Fixture(tests/00)로부터 뽑는다. 단독 실행되므로 외부 변수를 쓸 수 없다.
DECLARE @Pid BIGINT = (SELECT [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = N'T015');
EXEC [dbo].[USP_HC_SELECT_수검자상세] @Pid;
GO
