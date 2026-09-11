SET NOCOUNT ON;
-- INSERT_예약도 같은 순서다 - Patient 존재 · 검사 Master 다음이 공통 업무가능이다 (05 §11.1).
--
-- [X] 이 시험은 밤에 돌린 회차에서만 판정됐다. 주간 회차에서는 늘 SKIP 이고 SKIP 은 PASS 가
--     아니다 (CLAUDE.md §10). OFF-308-01 은 이미 휴무일을 **심어서** 308 을 결정적으로
--     만들었는데, 309 만 시계를 기다린 것은 값이 SP 안 리터럴이라 심을 데가 없어서였다.
-- [R13] 운영시간이 04 §8.7 [운영기준] 으로 나왔다. 창을 '지금이 아닌 1초' 로 좁힌다.
--     오전이면 23:00:00~23:00:01(지금보다 뒤), 오후면 11:00:00~11:00:01(지금보다 앞)이다.
--     둘 다 고정값이라 CK_운영기준_HOURS(시작 < 종료)를 항상 지나고, 배치가 정오나 자정을
--     넘겨도 고른 창이 지금을 품지 않는다 — 자정을 넘기면 오후 분기이고 00:00 은 11:00 앞이다.
-- [!] 심은 값은 이 파일에서 되돌린다. 남으면 뒤따르는 계약이 전부 309 가 된다.
--     verify-contract-all.sh 가 루프 뒤에 한 번 더 되돌리고, OPR-G4 가 회차 끝에서 판정한다.
DECLARE @OprFrom TIME(0), @OprTo TIME(0);
SELECT @OprFrom = [운영시작시각], @OprTo = [운영종료시각] FROM [dbo].[운영기준] WHERE [기준ID] = 1;
DECLARE @Am BIT = CASE WHEN CONVERT(TIME(0), SYSDATETIME()) < '12:00:00' THEN 1 ELSE 0 END;
UPDATE [dbo].[운영기준]
   SET [운영시작시각] = CASE WHEN @Am = 1 THEN '23:00:00' ELSE '11:00:00' END
     , [운영종료시각] = CASE WHEN @Am = 1 THEN '23:00:01' ELSE '11:00:01' END
 WHERE [기준ID] = 1;
DECLARE @P BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_예약_등록] @P, 'NORMAL', '2026-11-17', 'AM', 1,0,0,0,0,0,0, N'TEST';
UPDATE [dbo].[운영기준] SET [운영시작시각] = @OprFrom, [운영종료시각] = @OprTo WHERE [기준ID] = 1;
GO
