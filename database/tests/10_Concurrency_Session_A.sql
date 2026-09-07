SET NOCOUNT ON;
-- 동시성 Session A (스펙 §38 · plans/08 T34 Step 2·3).
--
-- 역할: A 는 **선점자**다. 대상 자원을 production SP 보다 먼저 sp_getapplock 으로 잡고
--       3초 머문 뒤 놓는다. 그 사이 B 의 SP 가 같은 자원에서 대기하므로 §25.1 의
--       PRINT 'INFO applock 잠금결과=1' 이 B 의 로그에 결정적으로 남는다 (스펙 §38.4-2).
--       선점을 놓은 뒤 A 도 같은 SP 를 부른다. 따라서 실제 성공 순서는 **B -> A** 다.
--       production SP 에는 어떤 지연도 넣지 않는다 - 선점은 이 파일에만 있다.
--
-- [X] $(LockResource) 를 쓰지 않는다. concurrency-test.sh 가 그 변수를 넘기지 않아
--     sqlcmd 가 "정의되지 않은 변수" 로 즉시 실패한다 (plans/08 T34 착수차단 결함 1).
--     자원명을 시나리오 번호로 여기서 만든다. 형식은 deploy/05~07 과 글자 단위로 같아야 한다.
--
-- [X] 조회는 전부 barrier **앞**에서 끝낸다. WAITFOR TIME 뒤에 읽으면 그 조회 시간만큼
--     A 와 B 의 진입이 어긋나 경합이 우연에 맡겨진다.
DECLARE @Scen INT = CONVERT(INT, N'$(Scenario)');

-- barrier 시각이 이미 지났으면 WAITFOR TIME 은 **다음 날 그 시각까지** 대기한다.
-- concurrency-test.sh 의 wait 에는 timeout 이 없어 스크립트가 약 24시간 정지한다. 즉시 중단한다.
-- [X] sqlcmd 는 -v 값에 콜론이 있으면 '17:50:00' 을 쪼개 ':50:00' 을 별도 인수로 보고
--     "Sqlcmd: ':50:00': Invalid argument" 로 즉시 죽는다 (실측). barrier 는 HHMMSS 로 받아
--     여기서 콜론을 끼운다. WAITFOR TIME 은 TIME 형을 거부하므로(Msg 9815) VARCHAR 로 둔다.
DECLARE @Barrier VARCHAR(8) = STUFF(STUFF('$(BarrierTime)', 5, 0, ':'), 3, 0, ':');

IF CONVERT(TIME(0), SYSDATETIME()) >= CONVERT(TIME(0), @Barrier)
    THROW 51001, N'barrier 시각이 이미 지났습니다. 다시 실행하십시오.', 1;

DECLARE @P1  BIGINT      = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'CONC1');
DECLARE @P2  BIGINT      = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'CONC2');
DECLARE @Ssn VARCHAR(13) = '9505051000014';   -- 1995-05-05 남. 체크디지트 **유효**.
                                              -- 계획서의 9505051000019 는 무효라 SSN-006 에 걸려
                                              -- CON-001 이 중복검사에 닿지도 못했다 (실측).
DECLARE @오늘날짜 DATE = CONVERT(DATE, SYSDATETIME());
DECLARE @W BIGINT, @Rv BINARY(8), @Res NVARCHAR(255), @잠금결과 INT;
-- [X] CON-004 의 @최종수정일시 를 NULL 로 넘기면 SP 가 100(필수값)으로 즉시 끝나 주민번호
--     변경에 닿지도 못한다. 저장 NEX 는 당연히 그대로라 판정이 PASS 로 보이지만 **공허하다**
--     (실측: 0|100|필수값을 입력하십시오.|최종수정일시). barrier 앞에서 현재 값을 읽어 넘긴다.
--     USP_HC_수검자정보_수정 는 **전체치환형**이라 차트번호·성명·B형간염제외여부 도 필수다.
--     주민번호 하나만 넣고 나머지를 NULL 로 두면 100(차트번호)에서 끝난다 (실측).
--     현재 값을 그대로 읽어 넘겨 **주민번호만** 바뀌게 한다.
DECLARE @Led DATETIME, @Chart NVARCHAR(100), @Nm NVARCHAR(100), @Hb BIT;
SELECT @Led = [최종수정일시], @Chart = [차트번호], @Nm = [성명], @Hb = [B형간염제외여부]
  FROM [dbo].[수검자] WHERE [수검자ID] = @P1;

IF @Scen = 5
    SELECT @W = w.[업무ID], @Rv = w.[행버전] FROM [dbo].[예약접수] w
     WHERE w.[수검자ID] = @P1 AND w.[예약일] = @오늘날짜 AND w.[시간대코드] = 'PM' AND w.[상태코드] = 'RSV';
IF @Scen = 6
    SELECT @W = w.[업무ID], @Rv = w.[행버전] FROM [dbo].[예약접수] w
     WHERE w.[수검자ID] = @P1 AND w.[상태코드] = 'RCP';
IF @Scen = 7
    SELECT @W = w.[업무ID], @Rv = w.[행버전] FROM [dbo].[예약접수] w
     WHERE w.[수검자ID] = @P1 AND w.[예약일] = '2026-11-18' AND w.[시간대코드] = 'AM';

SET @Res = CASE @Scen
        WHEN 1 THEN N'HC|SSN|' + CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @Ssn), 2)
        WHEN 2 THEN N'HC|SLOT|20261116|AM'
        WHEN 3 THEN N'HC|PAT|' + CONVERT(NVARCHAR(20), @P1)
        WHEN 4 THEN N'HC|PAT|' + CONVERT(NVARCHAR(20), @P1)
        WHEN 5 THEN N'HC|WORK|' + CONVERT(NVARCHAR(20), @W)
        WHEN 6 THEN N'HC|WORK|' + CONVERT(NVARCHAR(20), @W)
        WHEN 7 THEN N'HC|SLOT|20261118|AM'
        WHEN 8 THEN N'HC|SLOT|' + CONVERT(CHAR(8), @오늘날짜, 112) + N'|PM'
    END;
IF @Res IS NULL THROW 51002, N'선점 자원명을 만들지 못했습니다. 09_Concurrency_Setup 사전상태를 확인하십시오.', 1;

WAITFOR TIME @Barrier;
PRINT 'INFO A 진입 ' + CONVERT(VARCHAR(30), SYSDATETIME(), 121);

-- 선점. 잠금결과 는 0(즉시 획득)이 정상이다. 여기서 1 이 나오면 이전 회차가 자원을 물고 있는 것이다.
BEGIN TRAN;
    EXEC @잠금결과 = sp_getapplock @Resource = @Res, @LockMode = 'Exclusive',
                             @LockOwner = 'Transaction', @LockTimeout = 5000;
    PRINT 'INFO A 선점 rc=' + CONVERT(VARCHAR(4), @잠금결과);   -- 'applock 잠금결과=' 로 쓰지 않는다.
                                                          -- 그 문자열은 §38.4 의 경합 증거 전용이다.
    IF @잠금결과 < 0 THROW 51003, N'A 선점 실패', 1;
    WAITFOR DELAY '00:00:03';        -- B 의 SP 가 같은 자원에서 대기하는 구간
COMMIT;
PRINT 'INFO A 선점 해제 ' + CONVERT(VARCHAR(30), SYSDATETIME(), 121);

-- 선점을 놓은 뒤 A 도 SP 를 부른다. B 가 먼저 자원을 가져가므로 A 는 보통 업무실패로 끝난다.
-- 업무실패(305/306/205/502)는 정상이며 판정은 tests/12 와 로그 수치가 한다.
IF @Scen = 1
    EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, N'동시등록에이', @Ssn,
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, N'CONC-A';
ELSE IF @Scen = 2
    EXEC [dbo].[USP_HC_예약_등록] @P1, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0, N'CONC-A';
ELSE IF @Scen = 3
    EXEC [dbo].[USP_HC_예약_등록] @P1, 'NORMAL', '2026-11-18', 'AM', 0,0,0,0,0,0,0, N'CONC-A';
ELSE IF @Scen = 4
    -- 만 46세 -> 만 56세. B 가 먼저 예약을 만들면 EP-08 로 205 가 정상이다 (스펙 §38.6 ②).
    EXEC [dbo].[USP_HC_수검자정보_수정] @P1, @Led, @Chart, @Nm, '7003011000016',
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, @Hb, N'CONC-A';
ELSE IF @Scen = 5
    EXEC [dbo].[USP_HC_접수_완료] @W, @Rv, N'CONC-A';
ELSE IF @Scen = 6
    -- barrier 앞에서 읽은 @Rv 는 B 가 먼저 바꾸고 나면 stale 이다 -> 601 이 나와야 한다.
    -- 현재 AEX 는 EX014(OPT01) 하나다. A 는 OPT01+OPT06 을 요구해 실제 변경을 만든다.
    EXEC [dbo].[USP_HC_접수추가검사_변경] @W, @Rv, 1,0,0,0,0,1,0, N'CONC-A';
ELSE IF @Scen = 7
    -- CONC1 을 AM -> PM 으로. B 는 CONC2 를 PM -> AM 으로 옮긴다 (교차).
    EXEC [dbo].[USP_HC_예약_변경] @W, @Rv, '2026-11-18', 'PM', 0,0,0,0,0,0,0, N'CONC-A';
ELSE IF @Scen = 8
    -- WalkIn 신규. B 가 같은 시간대 의 RSV 하나를 RCP 로 옮기는 사이 정원 COUNT 가
    -- 그 행을 놓치면 21건이 저장된다 (스펙 §24.2).
    EXEC [dbo].[USP_HC_예약_등록] @P1, 'WALKIN', @오늘날짜, 'PM', 0,0,0,0,0,0,0, N'CONC-A';

PRINT 'INFO A 종료 ' + CONVERT(VARCHAR(30), SYSDATETIME(), 121);
GO
