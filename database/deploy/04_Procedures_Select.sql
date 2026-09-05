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
-- 이름은 접두검색만 한다 (04 §11.3). '%검색어%' 포함검색과 조건 없는 전체조회는 금지다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_SELECT_수검자목록]
    @ChartNo      NVARCHAR(100),
    @Name         NVARCHAR(100),
    @SocialNumber VARCHAR(13),
    @Birthday     VARCHAR(8),
    @MobilePhone  VARCHAR(13)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();

    -- 1. 정규화 — 공백 제거, 빈 문자열은 NULL, 전화·주민번호의 '-' 제거
    SET @ChartNo      = NULLIF(LTRIM(RTRIM(@ChartNo)), N'');
    SET @Name         = NULLIF(LTRIM(RTRIM(@Name)), N'');
    SET @SocialNumber = NULLIF(REPLACE(LTRIM(RTRIM(@SocialNumber)), '-', ''), '');
    SET @Birthday     = NULLIF(LTRIM(RTRIM(@Birthday)), '');
    SET @MobilePhone  = NULLIF(REPLACE(LTRIM(RTRIM(@MobilePhone)), '-', ''), '');

    -- 2. 형식 검증 (05 §5: 필수값 → 값 형식 순서. 이 SP 에 필수값은 없다)
    IF @SocialNumber IS NOT NULL
       AND (LEN(@SocialNumber) <> 13 OR @SocialNumber LIKE '%[^0-9]%')
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(101 AS INT)                  AS Code
            , CAST(N'입력값이 올바르지 않습니다.' AS NVARCHAR(300)) AS Message
            , CAST('SocialNumber' AS VARCHAR(50)) AS Field
            , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    IF @Birthday IS NOT NULL
       AND (LEN(@Birthday) <> 8 OR @Birthday LIKE '%[^0-9]%'
            OR TRY_CONVERT(DATE, @Birthday, 112) IS NULL)
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(101 AS INT)                  AS Code
            , CAST(N'입력값이 올바르지 않습니다.' AS NVARCHAR(300)) AS Message
            , CAST('Birthday' AS VARCHAR(50))   AS Field
            , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- 3. 조회조건 — 5개가 전부 NULL 이면 전체조회가 되므로 막는다
    IF @ChartNo IS NULL AND @Name IS NULL AND @SocialNumber IS NULL
       AND @Birthday IS NULL AND @MobilePhone IS NULL
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(103 AS INT)                  AS Code
            , CAST(N'조회조건을 하나 이상 입력하십시오.' AS NVARCHAR(300)) AS Message
            , CAST(NULL AS VARCHAR(50))         AS Field
            , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- RS0
    SELECT
          CAST(1 AS BIT)                    AS Success
        , CAST(0 AS INT)                    AS Code
        , CAST(N'정상 처리되었습니다.' AS NVARCHAR(300)) AS Message
        , CAST(NULL AS VARCHAR(50))         AS Field
        , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;

    -- RS1 — 조회 0건은 실패가 아니다 (05 §3.4)
    SELECT
          PatientId    = CAST(p.[PatientId]    AS BIGINT)
        , ChartNo      = CAST(p.[ChartNo]      AS NVARCHAR(100))
        , Name         = CAST(p.[Name]         AS NVARCHAR(100))
        , SocialNumber = CAST(p.[SocialNumber] AS VARCHAR(13))
        , Birthday     = CAST(p.[Birthday]     AS VARCHAR(8))
        , Gender       = CAST(p.[Gender]       AS CHAR(1))
        , MobilePhone  = CAST(p.[CelNumber]    AS VARCHAR(13))
        , Phone        = CAST(p.[TelNumber]    AS VARCHAR(13))
        , Email        = CAST(p.[EMail]        AS VARCHAR(200))
        , Zipcode      = CAST(p.[Zipcode]      AS VARCHAR(10))
        , Address      = CAST(p.[Address]      AS NVARCHAR(200))
    FROM [dbo].[수검자] p
    WHERE (@ChartNo      IS NULL OR p.[ChartNo]      =  @ChartNo)
      AND (@Name         IS NULL OR p.[Name]         LIKE @Name + N'%')
      AND (@SocialNumber IS NULL OR p.[SocialNumber] =  @SocialNumber)
      AND (@Birthday     IS NULL OR p.[Birthday]     =  @Birthday)
      AND (@MobilePhone  IS NULL OR p.[CelNumberS]   =  @MobilePhone)
    ORDER BY p.[Name] ASC, p.[Birthday] ASC, p.[ChartNo] ASC;
END
GO
-- 허용 Code 0 / 100 / 200 (05 §13). RS1 은 정확히 1행이다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_SELECT_수검자상세]
    @PatientId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();

    IF @PatientId IS NULL
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(100 AS INT)                  AS Code
            , CAST(N'필수값을 입력하십시오.' AS NVARCHAR(300)) AS Message
            , CAST('PatientId' AS VARCHAR(50))  AS Field
            , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [PatientId] = @PatientId)
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(200 AS INT)                  AS Code
            , CAST(N'수검자를 찾을 수 없습니다.' AS NVARCHAR(300)) AS Message
            , CAST('PatientId' AS VARCHAR(50))  AS Field
            , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- RS0
    SELECT
          CAST(1 AS BIT)                    AS Success
        , CAST(0 AS INT)                    AS Code
        , CAST(N'정상 처리되었습니다.' AS NVARCHAR(300)) AS Message
        , CAST(NULL AS VARCHAR(50))         AS Field
        , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;

    -- RS1 (14컬럼, 정확히 1행)
    SELECT
          PatientId     = CAST(p.[PatientId]     AS BIGINT)
        , ChartNo       = CAST(p.[ChartNo]       AS NVARCHAR(100))
        , Name          = CAST(p.[Name]          AS NVARCHAR(100))
        , SocialNumber  = CAST(p.[SocialNumber]  AS VARCHAR(13))
        , Birthday      = CAST(p.[Birthday]      AS VARCHAR(8))
        , Gender        = CAST(p.[Gender]        AS CHAR(1))
        , MobilePhone   = CAST(p.[CelNumber]     AS VARCHAR(13))
        , Phone         = CAST(p.[TelNumber]     AS VARCHAR(13))
        , Email         = CAST(p.[EMail]         AS VARCHAR(200))
        , Zipcode       = CAST(p.[Zipcode]       AS VARCHAR(10))
        , Address       = CAST(p.[Address]       AS NVARCHAR(200))
        , AddressDetail = CAST(p.[AddressDetail] AS NVARCHAR(200))
        , Memo          = CAST(p.[Memo]          AS NVARCHAR(MAX))
        , LastEditDate  = CAST(p.[LastEditDate]  AS DATETIME)
    FROM [dbo].[수검자] p
    WHERE p.[PatientId] = @PatientId;
END
GO
