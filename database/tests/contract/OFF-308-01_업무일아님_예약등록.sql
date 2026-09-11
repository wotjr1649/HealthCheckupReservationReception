SET NOCOUNT ON;
-- 308 은 "오늘이 업무일이 아니다" 다 (일요일 또는 활성 휴무일).
-- [X] 일요일을 기다리는 구성이었다. 그 결과 이 시험은 **한 번도 판정된 적이 없다** —
--     평일 회차에서는 늘 SKIP 이고, SKIP 은 PASS 가 아니다 (CLAUDE.md §10).
--     휴무일은 **데이터**다. 오늘을 활성 휴무일로 심으면 SP·TVF 를 한 글자도 고치지 않고
--     308 경로가 결정적으로 성립한다. 오늘업무일=0 은 운영시간내여부 보다 앞에서 판정되므로
--     (UFN_HC_일정확인 업무가능코드 CASE) 하루 중 어느 시각에 돌려도 된다.
--
-- [!] 심은 행은 이 파일에서 반드시 지운다. 남으면 뒤따르는 PWR/RWR/CWR 계약이 전부 308 이 된다.
--     verify-contract-all.sh 가 루프 뒤에 한 번 더 지우고 휴무일 2건을 확인한다.
--
DECLARE @D DATE = CONVERT(DATE, SYSDATETIME());
DECLARE @Seeded BIT = 0;

IF EXISTS (SELECT 1 FROM [dbo].[휴무일] WHERE [휴무일자] = @D AND [사용여부] = 1)
    SET @Seeded = 0;                       -- 오늘이 이미 휴무일이다. 그대로 쓴다
ELSE IF EXISTS (SELECT 1 FROM [dbo].[휴무일] WHERE [휴무일자] = @D)
    THROW 51040, N'오늘 날짜에 비활성 휴무일 행이 있습니다. 손대지 않고 중단합니다.', 1;
ELSE
BEGIN
    -- [휴무구분] 은 NOT NULL 이고 Default 가 없다 (04 §8.4.3). 시험이 심는 행도 구분을 명시한다.
    INSERT INTO [dbo].[휴무일] ([휴무일자], [휴무일명], [휴무구분], [사용여부])
    VALUES (@D, N'OFF-308 시험용 임시 휴무일', N'자체휴무일', 1);
    SET @Seeded = 1;
END

-- [R12] 대상을 수검자등록에서 **예약등록**으로 옮겼다. 수검자 Write 는 R12 부터 308/309 를
--       내지 않는다 (05 §10.1). 308 을 재는 계약시험이 이것 하나뿐이므로 폐기하지 않고 옮긴다.
-- 예약 Write SP 는 공통 업무가능을 업무 Rule 보다 먼저 판정하므로 입력이 완전해도 308 이다
-- (05 §11.1 검증순서 · §5 우선순위 9번).
DECLARE @P BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
EXEC [dbo].[USP_HC_예약_등록] @P, 'NORMAL', '2026-11-17', 'AM', 1,0,0,0,0,0,0, N'TEST';

IF @Seeded = 1
    DELETE FROM [dbo].[휴무일]
     WHERE [휴무일자] = @D AND [휴무일명] = N'OFF-308 시험용 임시 휴무일';
GO
