SET NOCOUNT ON;
-- CK_변경이력_TARGET_TABLE 과 같은 도메인이다. 완료이력은 쓰는 Write SP 가 0개라 제외됐다.
EXEC [dbo].[USP_HC_SELECT_변경이력] N'완료이력', 1;
GO
