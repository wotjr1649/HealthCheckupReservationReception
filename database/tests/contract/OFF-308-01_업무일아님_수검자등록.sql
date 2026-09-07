SET NOCOUNT ON;
-- 업무일이 아닌 날(일요일 · 활성 휴무일)에만 성립한다. verify-contract-all.sh 가 그 밖에서는 SKIP 한다.
-- Write SP 는 공통 업무가능을 업무 Rule 보다 먼저 판정하므로 입력이 완전해도 308 이다 (05 §5 우선순위 9번).
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'업무일아님', '9701011000013',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
GO
