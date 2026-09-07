SET NOCOUNT ON;
-- 파일명이 Test ID 라 glob 순서상 PWR-* 뒤에 온다. 그 시점에는 Write SP 가 남긴
-- 변경이력이 반드시 있다. 비어 있다면 그 자체가 감사 기록 누락 신호다.
DECLARE @K BIGINT = (SELECT TOP (1) [대상키] FROM [dbo].[변경이력]
                      WHERE [대상테이블] = N'수검자' ORDER BY [이력ID]);
EXEC [dbo].[USP_HC_SELECT_변경이력] N'수검자', @K;
GO
