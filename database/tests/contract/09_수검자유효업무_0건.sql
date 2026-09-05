SET NOCOUNT ON;
-- T015 는 Work 가 없다. 유효업무 0건은 실패가 아니다 (05 §3.4).
DECLARE @P0 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_SELECT_수검자유효업무] @P0;
GO
