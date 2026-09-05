SET QUOTED_IDENTIFIER ON;   -- 01_Schema.sql 과 같은 설정으로 객체를 만든다 (CLAUDE.md §6)
GO
SET NOCOUNT ON;
GO
-- SELECT SP 공통 (스펙 §31)
--   XACT_ABORT · Transaction · applock 을 쓰지 않는다. 읽기 전용이다.
--   @ServerTime 을 한 번만 캡처해 TVF 에 그대로 넘긴다 — TVF 안에서 SYSDATETIME() 을 부르지 않는다.
--   RS0 5컬럼에 명시적 CAST 를 건다. 조회 0건은 실패가 아니다 (05 §3.4).
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_SELECT_공통업무상태]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
    DECLARE @Today DATE = CONVERT(DATE, @ServerTime);

    -- RS0
    SELECT
          CAST(1 AS BIT)                    AS Success
        , CAST(0 AS INT)                    AS Code
        , CAST(N'정상 처리되었습니다.' AS NVARCHAR(300)) AS Message
        , CAST(NULL AS VARCHAR(50))         AS Field
        , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;

    -- RS1
    --   WithinHours 를 TVF 의 WorkCode 로 역산하지 않는다. WorkCode 는 TodayBiz=0 → 308 이
    --   WithinHours=0 → 309 보다 먼저 걸리므로 휴무일·일요일에는 시각과 무관하게 308 이 나온다.
    --   그러면 2026-12-25(금, 휴무일) 새벽 3시에 WithinHours=1 을 보고하게 된다. 직접 계산한다.
    SELECT
          Today         = CAST(@Today AS DATE)
        , DayName       = CAST(CASE DATEDIFF(DAY, 0, @Today) % 7
                               WHEN 0 THEN N'월요일' WHEN 1 THEN N'화요일' WHEN 2 THEN N'수요일'
                               WHEN 3 THEN N'목요일' WHEN 4 THEN N'금요일' WHEN 5 THEN N'토요일'
                               ELSE N'일요일' END AS NVARCHAR(10))
        , HolidayName   = CAST(s.HolidayName AS NVARCHAR(100))
        , OpenTime      = CAST('09:00:00' AS TIME(0))
        , CloseTime     = CAST('18:00:00' AS TIME(0))
        , IsBusinessDay = CAST(s.IsBusinessDay AS BIT)
        , WithinHours   = CAST(CASE WHEN CONVERT(TIME(7), @ServerTime) >= CONVERT(TIME(7), '09:00:00')
                                     AND CONVERT(TIME(7), @ServerTime) <  CONVERT(TIME(7), '18:00:00')
                                    THEN 1 ELSE 0 END AS BIT)
        , CanWorkNow    = CAST(s.CanWorkNow AS BIT)
        , BlockCode     = CAST(s.WorkCode AS INT)
        , BlockMessage  = CAST(s.WorkMessage AS NVARCHAR(300))
    FROM [dbo].[UFN_HC_일정확인](@ServerTime, @Today, 'AM', 'NONE') s;
END
GO
