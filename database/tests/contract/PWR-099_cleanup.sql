SET NOCOUNT ON;
-- PWR 시나리오가 바꾼 차트번호·성명을 되돌린다. glob 사전순은 CWR < PWR < RWR < SEL 이라
-- PWR-028 이 남긴 T014X 를 되돌리지 않으면 **다음 회차의 CWR 블록**이 T014 를 못 찾는다
-- (실측: CWR-007·020~026·041~044 가 업무ID NULL 로 100 을 낸다). Result Set 은 없다.
DELETE FROM [dbo].[수검자]
 WHERE [주민번호] IN ('9001011000018', '0610024000015') AND [차트번호] LIKE 'C%';
UPDATE [dbo].[수검자] SET [차트번호] = N'F001'       WHERE [차트번호] = N'F001X';
UPDATE [dbo].[수검자] SET [차트번호] = N'T014'       WHERE [차트번호] = N'T014X';
UPDATE [dbo].[수검자] SET [성명]     = N'테스트사육' WHERE [차트번호] = N'T015';
GO
