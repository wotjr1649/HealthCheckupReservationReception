SET NOCOUNT ON;
-- 인자는 파일 안에서 Fixture(tests/00)로부터 뽑는다. 단독 실행되므로 외부 변수를 쓸 수 없다.
DECLARE @Pid BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_수검자상세_조회] @Pid;
GO
