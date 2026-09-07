SET NOCOUNT ON;
-- INSERT_예약도 같은 순서다 - Patient 존재 · 검사 Master 다음이 공통 업무가능이다 (05 §11.1).
DECLARE @P BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_INSERT_예약] @P, 'NORMAL', '2026-11-17', 'AM', 1,0,0,0,0,0,0, N'TEST';
GO
