SET NOCOUNT ON;
-- 동시성 Session B (스펙 §38 · plans/08 T34 Step 2·3).
--
-- 역할: B 는 **경합자**다. 선점도 지연도 하지 않고 barrier 직후 곧바로 SP 를 부른다.
--       A 가 자원을 3초 쥐고 있으므로 B 의 SP 안에서 sp_getapplock 이 대기했다가 획득하고,
--       §25.1 의 PRINT 가 로그에 'INFO applock 잠금결과=1' 을 남긴다. 이것이 §38.4 의 경합 증거다.
--       그래서 실제 성공 순서는 B -> A 이며 A 가 업무실패로 끝나는 것이 정상이다.
--
-- [X] 조회는 전부 barrier 앞에서 끝낸다. CON-006 의 @Rv 도 여기서 읽어야 A 와 **같은** 값이
--     되어 stale 행버전 경합이 성립한다.
DECLARE @Scen INT = CONVERT(INT, N'$(Scenario)');

-- [X] sqlcmd 는 -v 값에 콜론이 있으면 '17:50:00' 을 쪼개 ':50:00' 을 별도 인수로 보고
--     "Sqlcmd: ':50:00': Invalid argument" 로 즉시 죽는다 (실측). barrier 는 HHMMSS 로 받아
--     여기서 콜론을 끼운다. WAITFOR TIME 은 TIME 형을 거부하므로(Msg 9815) VARCHAR 로 둔다.
DECLARE @Barrier VARCHAR(8) = STUFF(STUFF('$(BarrierTime)', 5, 0, ':'), 3, 0, ':');

IF CONVERT(TIME(0), SYSDATETIME()) >= CONVERT(TIME(0), @Barrier)
    THROW 51001, N'barrier 시각이 이미 지났습니다. 다시 실행하십시오.', 1;

DECLARE @P1  BIGINT      = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'CONC1');
DECLARE @P2  BIGINT      = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'CONC2');
DECLARE @Ssn VARCHAR(13) = '9505051000014';   -- A 와 동일한 주민번호. CON-001 의 전부다.
DECLARE @오늘날짜 DATE = CONVERT(DATE, SYSDATETIME());
DECLARE @W BIGINT, @Rv BINARY(8);

IF @Scen = 5
    SELECT @W = w.[업무ID], @Rv = w.[행버전] FROM [dbo].[예약접수] w
     WHERE w.[수검자ID] = @P1 AND w.[예약일] = @오늘날짜 AND w.[시간대코드] = 'PM' AND w.[상태코드] = 'RSV';
IF @Scen = 6
    SELECT @W = w.[업무ID], @Rv = w.[행버전] FROM [dbo].[예약접수] w
     WHERE w.[수검자ID] = @P1 AND w.[상태코드] = 'RCP';
IF @Scen = 7
    SELECT @W = w.[업무ID], @Rv = w.[행버전] FROM [dbo].[예약접수] w
     WHERE w.[수검자ID] = @P2 AND w.[예약일] = '2026-11-18' AND w.[시간대코드] = 'PM';
IF @Scen = 8
    -- 같은 시간대 의 RSV 하나. CONC2 것을 고정으로 집어 회차마다 같은 행을 쓴다.
    SELECT @W = w.[업무ID], @Rv = w.[행버전] FROM [dbo].[예약접수] w
     WHERE w.[수검자ID] = @P2 AND w.[예약일] = @오늘날짜 AND w.[시간대코드] = 'PM' AND w.[상태코드] = 'RSV';

IF @Scen IN (5,6,7,8) AND @W IS NULL
    THROW 51002, N'B 의 대상 Work 를 찾지 못했습니다. 09_Concurrency_Setup 사전상태를 확인하십시오.', 1;

WAITFOR TIME @Barrier;
PRINT 'INFO B 진입 ' + CONVERT(VARCHAR(30), SYSDATETIME(), 121);

IF @Scen = 1
    EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, N'동시등록비', @Ssn,
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, N'CONC-B';
ELSE IF @Scen = 2
    -- A 와 **다른** 수검자로 **같은** 시간대 을 노린다. 19/20 이라 한쪽만 들어가야 한다.
    EXEC [dbo].[USP_HC_예약_등록] @P2, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0, N'CONC-B';
ELSE IF @Scen = 3
    -- A 와 **같은** 수검자로 **다른** 시간대. RP-06 상 유효업무는 1건뿐이어야 한다.
    EXEC [dbo].[USP_HC_예약_등록] @P1, 'NORMAL', '2026-11-18', 'PM', 0,0,0,0,0,0,0, N'CONC-B';
ELSE IF @Scen = 4
    EXEC [dbo].[USP_HC_예약_등록] @P1, 'NORMAL', '2026-11-18', 'AM', 0,0,0,0,0,0,0, N'CONC-B';
ELSE IF @Scen = 5
    EXEC [dbo].[USP_HC_예약_취소] @W, @Rv, N'CONC-B';
ELSE IF @Scen = 6
    -- A 와 같은 stale @Rv. 먼저 도는 쪽이 성공하고 나중이 601 이다.
    -- A 는 OPT01+OPT06, B 는 OPT01+OPT02 로 서로 다른 실제 변경을 요구한다.
    -- [!] 어느 한쪽이라도 현재 집합(EX014=OPT01)과 같으면 No-op 으로 끝나 601 이 나오지 않는다.
    EXEC [dbo].[USP_HC_접수추가검사_변경] @W, @Rv, 1,1,0,0,0,0,0, N'CONC-B';
ELSE IF @Scen = 7
    EXEC [dbo].[USP_HC_예약_변경] @W, @Rv, '2026-11-18', 'AM', 0,0,0,0,0,0,0, N'CONC-B';
ELSE IF @Scen = 8
    EXEC [dbo].[USP_HC_접수_완료] @W, @Rv, N'CONC-B';

PRINT 'INFO B 종료 ' + CONVERT(VARCHAR(30), SYSDATETIME(), 121);
GO
