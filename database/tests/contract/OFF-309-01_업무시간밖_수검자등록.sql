SET NOCOUNT ON;
-- 업무일이지만 09:00~18:00 밖일 때만 성립한다. verify-contract-all.sh 가 그 밖에서는 SKIP 한다.
-- 309 는 4-3 공통 업무가능에서 나오며 동일 주민번호·차트번호 판정보다 앞이다 (05 §10.1 검증순서).
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'시간밖등록', '9701011000013',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
GO
