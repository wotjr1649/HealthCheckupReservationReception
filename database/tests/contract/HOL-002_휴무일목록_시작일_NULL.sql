SET NOCOUNT ON;
-- 필수값 누락은 100 이다. RS0 하나만 나가고 목록·등재현황은 나가지 않는다 (05 §12.5).
EXEC [dbo].[USP_HC_휴무일목록_조회] NULL, '2026-12-31', NULL;
GO
