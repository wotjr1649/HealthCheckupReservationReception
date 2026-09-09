SET NOCOUNT ON;
-- [R16] 조건이 하나도 없으면 전체를 조회한다 (05 §7.2). 예전에는 103 이었다.
EXEC [dbo].[USP_HC_수검자목록_조회] NULL, NULL, NULL, NULL, NULL;
GO
