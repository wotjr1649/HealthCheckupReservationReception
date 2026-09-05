SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- 이 파일은 DB 상태 불변조건만 판정한다. RS0 Code 와 RS 형상은
-- tests/contract/* + tools/verify-contract.js 가 판정한다 (스펙 §33.1a).
--
-- SELECT SP 는 읽기 전용이므로 여기서 판정할 것은 "아무것도 바뀌지 않았다" 하나다.
-- 단언이 하나도 없는 파일을 두지 않는다 — test.sh 의 grep 에 아무것도 걸리지 않아
-- 조용히 통과한다. plans/08 이 tests/14 에서 실제로 겪은 사고다.
-- 카탈로그에 없는 표식이므로 Test ID 를 쓰지 않는다 (FIX-DEPLOY·FIX-SSN-PRE 와 같은 부류).
--
-- 지문 식을 SP 마다 되풀이하지 않는다. 전 SELECT SP 를 한 번에 호출하고 앞뒤로 한 번씩만 잰다.
-- FAIL 이면 아래 EXEC 목록을 반씩 잘라 다시 돌리면 범인이 나온다.
-- 스칼라 UDF 로 묶지 않는다 — 계약(TVF 4 / SP 15) 밖이라 SCH-013·SCH-014 가 깨진다.

DECLARE @Before VARCHAR(100) =
      CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[수검자]))   + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[예약접수])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[검사항목])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[완료이력])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[변경이력])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[검사코드])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[휴무일]));

-- 성공 경로와 실패 경로를 모두 지난다. 실패 경로도 아무것도 쓰지 않아야 한다.
EXEC [dbo].[USP_HC_SELECT_공통업무상태];
EXEC [dbo].[USP_HC_SELECT_수검자목록] N'T001', NULL, NULL, NULL, NULL;
EXEC [dbo].[USP_HC_SELECT_수검자목록] NULL, NULL, NULL, NULL, NULL;

EXEC [dbo].[USP_HC_SELECT_수검자상세] NULL;
-- EXEC sp (SELECT …) 는 인자가 아니라 별도 SELECT 문으로 파싱된다 — SP 는 인자를 못 받아 실패한다(실측).
-- 인자는 변수로 받는다.
DECLARE @P15 BIGINT = (SELECT [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = N'T015');
EXEC [dbo].[USP_HC_SELECT_수검자상세] @P15;

DECLARE @P12 BIGINT = (SELECT [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = N'T012');
EXEC [dbo].[USP_HC_SELECT_수검자유효업무] @P15;
EXEC [dbo].[USP_HC_SELECT_수검자유효업무] @P12;

EXEC [dbo].[USP_HC_SELECT_예약접수목록] '2026-11-01', '2026-11-30', NULL, NULL, NULL;
EXEC [dbo].[USP_HC_SELECT_예약접수목록] NULL, NULL, NULL, NULL, NULL;
DECLARE @Wn BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[PatientId] = w.[수검자ID]
                       WHERE p.[ChartNo] = N'T020' AND w.[상태코드] = 'RSV' ORDER BY w.[업무ID]);
EXEC [dbo].[USP_HC_SELECT_예약접수상세] @Wn;
EXEC [dbo].[USP_HC_SELECT_예약접수상세] -1;

EXEC [dbo].[USP_HC_SELECT_예약가능정보] @P15, NULL, NULL, 'NORMAL', '2026-11-16', 'AM', 1,0,0,0,0,0,1;
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @P15, NULL, NULL, 'NORMAL', '2026-12-25', 'AM', 0,0,0,0,0,0,0;
EXEC [dbo].[USP_HC_SELECT_예약가능정보] @P15, NULL, NULL, 'WALKIN', '2026-11-16', 'AM', 0,0,0,0,0,0,0;

DECLARE @After VARCHAR(100) =
      CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[수검자]))   + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[예약접수])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[검사항목])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[완료이력])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[변경이력])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[검사코드])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[휴무일]));

IF @Before = @After
    PRINT 'PASS FIX-RO-01 SELECT SP 호출이 DB 상태를 바꾸지 않았다  ' + @After;
ELSE BEGIN PRINT 'FAIL FIX-RO-01 읽기전용 위반  before=' + @Before + '  after=' + @After; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 04_Select_SP_Tests 완료 ===';
GO
