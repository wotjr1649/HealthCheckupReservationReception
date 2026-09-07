SET NOCOUNT ON;
-- PWR 계약 시나리오는 순서 의존이다(glob 사전순 = ID 순). 이 파일이 그 전제를 매번 되돌려
-- 게이트를 재실행 가능하게 만든다. Result Set 을 내지 않는다.
DELETE FROM [dbo].[수검자]
 WHERE [주민번호] IN ('9001011000018', '0610024000015') AND [차트번호] LIKE 'C%';
UPDATE [dbo].[수검자] SET [차트번호] = N'F001'       WHERE [차트번호] = N'F001X';
UPDATE [dbo].[수검자] SET [차트번호] = N'T014'       WHERE [차트번호] = N'T014X';
UPDATE [dbo].[수검자] SET [성명]     = N'테스트사육' WHERE [차트번호] = N'T015';
GO
