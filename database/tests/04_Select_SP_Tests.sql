SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- 이 파일은 DB 상태 불변조건만 판정한다. RS0 Code 와 RS 형상은
-- tests/contract/* + tools/verify-contract.js 가 판정한다 (스펙 §33.1a).
--
-- SELECT SP 는 읽기 전용이므로 여기서 판정할 것은 "아무것도 바뀌지 않았다" 하나다.
-- 단언이 하나도 없는 파일을 두지 않는다 — test.sh 의 grep 에 아무것도 걸리지 않아
-- 조용히 통과한다. plans/08 이 tests/14 에서 실제로 겪은 사고다.
-- 카탈로그에 없는 표식이므로 Test ID 를 쓰지 않는다 (FIX-DEPLOY·FIX-SSN-PRE 와 같은 부류).

DECLARE @Before VARCHAR(100) =
      CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[수검자]))   + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[예약접수])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[검사항목])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[완료이력])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[변경이력])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[검사코드])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[휴무일]));

EXEC [dbo].[USP_HC_SELECT_공통업무상태];

DECLARE @After VARCHAR(100) =
      CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[수검자]))   + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[예약접수])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[검사항목])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[완료이력])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[변경이력])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[검사코드])) + '|'
    + CONVERT(VARCHAR(10), (SELECT COUNT(*) FROM [dbo].[휴무일]));

IF @Before = @After
    PRINT 'PASS FIX-RO-01 공통업무상태 호출이 DB 상태를 바꾸지 않았다  ' + @After;
ELSE BEGIN PRINT 'FAIL FIX-RO-01 읽기전용 위반  before=' + @Before + '  after=' + @After; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 04_Select_SP_Tests 완료 ===';
GO
