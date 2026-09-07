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
    -- [X] @Name 을 LIKE 에 그대로 이어 붙이면 접두검색이 아니다. '%' 한 글자면 조건이 '있는'
    --     것으로 103 가드를 통과하고 수검자 전건이 주민번호와 함께 반환된다 (실측).
    --     04 §11.3 과 이 파일 머리 주석이 '조건 없는 전체조회는 금지' 라고 못박은 그 상태다.
    --     메타문자 세 개를 대괄호로 이스케이프한다. '[' 를 먼저 바꿔야 뒤 치환이 낳는 괄호를 안 건드린다.
    --     변수로 올려 IX_수검자_NAME_BIRTHDAY seek 을 잃지 않게 한다.
    DECLARE @NameLike NVARCHAR(200) = CASE WHEN @Name IS NULL THEN NULL ELSE REPLACE(REPLACE(REPLACE(@Name, N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%' END;
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
          PatientId    = CAST(p.[수검자ID]    AS BIGINT)
        , ChartNo      = CAST(p.[차트번호]      AS NVARCHAR(100))
        , Name         = CAST(p.[성명]         AS NVARCHAR(100))
        , SocialNumber = CAST(p.[주민번호] AS VARCHAR(13))
        , Birthday     = CAST(p.[생년월일]     AS VARCHAR(8))
        , Gender       = CAST(p.[성별]       AS CHAR(1))
        , MobilePhone  = CAST(p.[휴대전화]    AS VARCHAR(13))
        , Phone        = CAST(p.[전화번호]    AS VARCHAR(13))
        , Email        = CAST(p.[이메일]        AS VARCHAR(200))
        , Zipcode      = CAST(p.[우편번호]      AS VARCHAR(10))
        , Address      = CAST(p.[주소]      AS NVARCHAR(200))
    FROM [dbo].[수검자] p
    WHERE (@ChartNo      IS NULL OR p.[차트번호]      =  @ChartNo)
      AND (@Name         IS NULL OR p.[성명]         LIKE @NameLike       )
      AND (@SocialNumber IS NULL OR p.[주민번호] =  @SocialNumber)
      AND (@Birthday     IS NULL OR p.[생년월일]     =  @Birthday)
      AND (@MobilePhone  IS NULL OR REPLACE(p.[휴대전화], '-', '') = @MobilePhone)
    ORDER BY p.[성명] ASC, p.[생년월일] ASC, p.[차트번호] ASC;
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

    IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [수검자ID] = @PatientId)
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

    -- RS1 (15컬럼, 정확히 1행)
    SELECT
          PatientId     = CAST(p.[수검자ID]     AS BIGINT)
        , ChartNo       = CAST(p.[차트번호]       AS NVARCHAR(100))
        , Name          = CAST(p.[성명]          AS NVARCHAR(100))
        , SocialNumber  = CAST(p.[주민번호]  AS VARCHAR(13))
        , Birthday      = CAST(p.[생년월일]      AS VARCHAR(8))
        , Gender        = CAST(p.[성별]        AS CHAR(1))
        , MobilePhone   = CAST(p.[휴대전화]     AS VARCHAR(13))
        , Phone         = CAST(p.[전화번호]     AS VARCHAR(13))
        , Email         = CAST(p.[이메일]         AS VARCHAR(200))
        , Zipcode       = CAST(p.[우편번호]       AS VARCHAR(10))
        , Address       = CAST(p.[주소]       AS NVARCHAR(200))
        , AddressDetail = CAST(p.[상세주소] AS NVARCHAR(200))
        , Memo          = CAST(p.[비고]          AS NVARCHAR(MAX))
        -- [X] 15컬럼이다. R3 이 05 §9.2 RS1 에 HepatitisBExcluded 를 넣었는데 SQL 이 따라오지 않았고,
        --     expected-contracts.json 도 14컬럼으로 같이 틀려 있어 게이트가 영원히 못 잡았다.
        --     DLG-PAT-01 수정 화면이 현재값을 못 받으면 @HepatitisBExcluded(NULL 불가)에
        --     체크박스 초기값 0 이 실려 제외 플래그가 조용히 1->0 이 된다 (05 §9.2 명문).
        , HepatitisBExcluded = CAST(p.[B형간염제외여부] AS BIT)
        , LastEditDate  = CAST(p.[최종수정일시]  AS DATETIME)
    FROM [dbo].[수검자] p
    WHERE p.[수검자ID] = @PatientId;
END
GO
-- 허용 Code 0 / 100 / 200 / 701. RP-06 은 유효업무를 0~1건으로 제한한다.
-- 2건 이상은 조회로 고칠 수 없는 데이터 손상이므로 701 로 알린다 (00 RP-06, 05 §7.4).
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_SELECT_수검자유효업무]
    @PatientId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
    DECLARE @Today DATE = CONVERT(DATE, @ServerTime);

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

    IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [수검자ID] = @PatientId)
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(200 AS INT)                  AS Code
            , CAST(N'수검자를 찾을 수 없습니다.' AS NVARCHAR(300)) AS Message
            , CAST('PatientId' AS VARCHAR(50))  AS Field
            , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    DECLARE @Cnt INT = (SELECT COUNT(*) FROM [dbo].[예약접수] w
                         WHERE w.[수검자ID] = @PatientId
                           AND w.[예약일] >= @Today
                           AND w.[상태코드] IN ('RSV','RCP'));
    IF @Cnt >= 2
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(701 AS INT)                  AS Code
            , CAST(N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.' AS NVARCHAR(300)) AS Message
            , CAST('WorkId' AS VARCHAR(50))     AS Field
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

    -- RS1 (7컬럼, 0행 또는 1행)
    SELECT
          WorkId          = CAST(w.[업무ID] AS BIGINT)
        , ReservationDate = CAST(w.[예약일] AS DATE)
        , TimeSlot        = CAST(w.[시간대코드] AS CHAR(2))
        , Status          = CAST(w.[상태코드] AS CHAR(3))
        , StatusName      = CAST(CASE w.[상태코드]
                                     WHEN 'RSV' THEN N'예약'
                                     WHEN 'RCP' THEN N'접수완료'
                                     WHEN 'CNR' THEN N'예약취소'
                                     ELSE N'접수취소' END AS NVARCHAR(10))
        , IsToday         = CAST(CASE WHEN w.[예약일] = @Today THEN 1 ELSE 0 END AS BIT)
        , RowVersion      = CAST(w.[행버전] AS BINARY(8))
    FROM [dbo].[예약접수] w
    WHERE w.[수검자ID] = @PatientId
      AND w.[예약일] >= @Today
      AND w.[상태코드] IN ('RSV','RCP');
END
GO
-- 허용 Code 0 / 101 / 103 / 104 (05 §8.1).
-- 05 §8.1 은 "Status=NULL 은 조회조건으로 보지 않음" 이라고 NULL 을 한정했다.
-- 값이 있는 @Status 는 실질 조건으로 센다 — 상태만으로 전체 RSV 를 조회하는 것은 막지 않는다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_SELECT_예약접수목록]
    @FromDate DATE,
    @ToDate   DATE,
    @Status   CHAR(3),
    @ChartNo  NVARCHAR(100),
    @Name     NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();

    -- 1. 정규화
    SET @Status  = NULLIF(UPPER(LTRIM(RTRIM(@Status))), '');
    SET @ChartNo = NULLIF(LTRIM(RTRIM(@ChartNo)), N'');
    SET @Name    = NULLIF(LTRIM(RTRIM(@Name)), N'');
    -- [X] @Name 을 LIKE 에 그대로 이어 붙이면 접두검색이 아니다. '%' 한 글자면 조건이 '있는'
    --     것으로 103 가드를 통과하고 수검자 전건이 주민번호와 함께 반환된다 (실측).
    --     04 §11.3 과 이 파일 머리 주석이 '조건 없는 전체조회는 금지' 라고 못박은 그 상태다.
    --     메타문자 세 개를 대괄호로 이스케이프한다. '[' 를 먼저 바꿔야 뒤 치환이 낳는 괄호를 안 건드린다.
    --     변수로 올려 IX_수검자_NAME_BIRTHDAY seek 을 잃지 않게 한다.
    DECLARE @NameLike NVARCHAR(200) = CASE WHEN @Name IS NULL THEN NULL ELSE REPLACE(REPLACE(REPLACE(@Name, N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%' END;

    -- 2. 허용값
    IF @Status IS NOT NULL AND @Status NOT IN ('RSV','RCP','CNR','CNC')
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(101 AS INT)                  AS Code
            , CAST(N'입력값이 올바르지 않습니다.' AS NVARCHAR(300)) AS Message
            , CAST('Status' AS VARCHAR(50))     AS Field
            , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- 3. 날짜 범위
    IF @FromDate IS NOT NULL AND @ToDate IS NOT NULL AND @FromDate > @ToDate
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(104 AS INT)                  AS Code
            , CAST(N'시작일은 종료일보다 늦을 수 없습니다.' AS NVARCHAR(300)) AS Message
            , CAST('FromDate' AS VARCHAR(50))   AS Field
            , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- 4. 조회조건
    IF @FromDate IS NULL AND @ToDate IS NULL AND @Status IS NULL
       AND @ChartNo IS NULL AND @Name IS NULL
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

    -- RS1 (11컬럼, 05 §8.1)
    SELECT
          WorkId          = CAST(w.[업무ID] AS BIGINT)
        , PatientId       = CAST(w.[수검자ID] AS BIGINT)
        , ReservationDate = CAST(w.[예약일] AS DATE)
        , TimeSlot        = CAST(w.[시간대코드] AS CHAR(2))
        , Status          = CAST(w.[상태코드] AS CHAR(3))
        , StatusName      = CAST(CASE w.[상태코드]
                                     WHEN 'RSV' THEN N'예약'
                                     WHEN 'RCP' THEN N'접수완료'
                                     WHEN 'CNR' THEN N'예약취소'
                                     ELSE N'접수취소' END AS NVARCHAR(10))
        , Name            = CAST(p.[성명] AS NVARCHAR(100))
        , ChartNo         = CAST(p.[차트번호] AS NVARCHAR(100))
        , Gender          = CAST(p.[성별] AS CHAR(1))
        , Birthday        = CAST(p.[생년월일] AS VARCHAR(8))
        , MobilePhone     = CAST(p.[휴대전화] AS VARCHAR(13))
    FROM [dbo].[예약접수] w
    JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
    WHERE (@FromDate IS NULL OR w.[예약일] >= @FromDate)
      AND (@ToDate   IS NULL OR w.[예약일] <= @ToDate)
      AND (@Status   IS NULL OR w.[상태코드]      =  @Status)
      AND (@ChartNo  IS NULL OR p.[차트번호]         =  @ChartNo)
      AND (@Name     IS NULL OR p.[성명]            LIKE @NameLike       )
    ORDER BY w.[예약일] ASC, w.[시간대코드] ASC, p.[성명] ASC, w.[업무ID] ASC;
END
GO
-- 허용 Code 0 / 100 / 500 / 701 (05 §8.2). RS0~RS4 다섯 개를 반환한다.
-- RS4 는 고정 5행이다. 업무시간 밖이라고 실패시키지 않는다 — Allowed=0 으로 반환한다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_SELECT_예약접수상세]
    @WorkId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
    DECLARE @Today DATE = CONVERT(DATE, @ServerTime);

    IF @WorkId IS NULL
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(100 AS INT)                  AS Code
            , CAST(N'필수값을 입력하십시오.' AS NVARCHAR(300)) AS Message
            , CAST('WorkId' AS VARCHAR(50))     AS Field
            , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[예약접수] WHERE [업무ID] = @WorkId)
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(500 AS INT)                  AS Code
            , CAST(N'예약·접수 업무를 찾을 수 없습니다.' AS NVARCHAR(300)) AS Message
            , CAST('WorkId' AS VARCHAR(50))     AS Field
            , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- 저장 NEX 가 비면 검사구성 손상이다 (CORRUPT-2 가 이것을 만든다).
    -- 빈 문자열이 손상의 유일한 표현이다 (plans/10 §1).
    IF EXISTS (SELECT 1 FROM [dbo].[예약접수] w
                WHERE w.[업무ID] = @WorkId AND LEN(ISNULL(w.[국가검사항목], N'')) = 0)
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(701 AS INT)                  AS Code
            , CAST(N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.' AS NVARCHAR(300)) AS Message
            , CAST('WorkId' AS VARCHAR(50))     AS Field
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

    -- RS1 업무상세 (15컬럼)
    SELECT
          WorkId          = CAST(w.[업무ID] AS BIGINT)
        , PatientId       = CAST(w.[수검자ID] AS BIGINT)
        , ChartNo         = CAST(p.[차트번호] AS NVARCHAR(100))
        , Name            = CAST(p.[성명] AS NVARCHAR(100))
        , Birthday        = CAST(p.[생년월일] AS VARCHAR(8))
        , Gender          = CAST(p.[성별] AS CHAR(1))
        , MobilePhone     = CAST(p.[휴대전화] AS VARCHAR(13))
        , ReservationDate = CAST(w.[예약일] AS DATE)
        , TimeSlot        = CAST(w.[시간대코드] AS CHAR(2))
        , Status          = CAST(w.[상태코드] AS CHAR(3))
        , StatusName      = CAST(CASE w.[상태코드]
                                     WHEN 'RSV' THEN N'예약'
                                     WHEN 'RCP' THEN N'접수완료'
                                     WHEN 'CNR' THEN N'예약취소'
                                     ELSE N'접수취소' END AS NVARCHAR(10))
        , Capacity        = CAST(20 AS INT)
        , CurrentCount    = CAST(c.Cnt AS INT)
        , SeatsLeft       = CAST(CASE WHEN 20 - c.Cnt < 0 THEN 0 ELSE 20 - c.Cnt END AS INT)
        , RowVersion      = CAST(w.[행버전] AS BINARY(8))
    FROM [dbo].[예약접수] w
    JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
    CROSS APPLY (SELECT Cnt = COUNT(*) FROM [dbo].[예약접수] x
                  WHERE x.[예약일] = w.[예약일]
                    AND x.[시간대코드]    = w.[시간대코드]
                    AND x.[상태코드] IN ('RSV','RCP')) c
    WHERE w.[업무ID] = @WorkId;

    -- RS2 국가검사항목 — 실제 저장된 NEX 만
    SELECT
          ExamCode = CAST(m.[검사항목코드] AS VARCHAR(10))
        , ExamName = CAST(m.[검사항목명] AS NVARCHAR(100))
        , ExamType = CAST(CASE WHEN m.[국가검사규칙코드] = 'NEX-01' THEN 'BASIC' ELSE 'CONDITIONAL' END AS VARCHAR(12))
        , RuleCode = CAST(m.[국가검사규칙코드] AS VARCHAR(10))
    FROM [dbo].[예약접수] w
    JOIN [dbo].[검사코드] m
      ON N',' + ISNULL(w.[국가검사항목], N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
    WHERE w.[업무ID] = @WorkId
    ORDER BY m.[검사항목코드] ASC;

    -- RS3 추가검사항목 — 실제 저장된 AEX 만
    SELECT
          OptionCode = CAST(m.[추가검사코드] AS VARCHAR(10))
        , ExamCode   = CAST(m.[검사항목코드] AS VARCHAR(10))
        , ExamName   = CAST(m.[검사항목명] AS NVARCHAR(100))
    FROM [dbo].[예약접수] w
    JOIN [dbo].[검사코드] m
      ON N',' + ISNULL(w.[추가검사항목], N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
    WHERE w.[업무ID] = @WorkId
    ORDER BY m.[추가검사코드] ASC;

    -- RS4 가능한업무 — 정확히 5행. 순서는 05 §8.2 의 고정 목록이므로 Ord 로 강제한다.
    --   상관 인자를 받는 TVF 는 CROSS APPLY 여야 한다. CROSS JOIN 으로 쓰면
    --   같은 FROM 절 다른 테이블의 컬럼을 인자로 못 받아 Msg 4104 가 난다.
    SELECT
          ActionCode    = CAST(a.Code AS VARCHAR(30))
        , Allowed       = CAST(CASE WHEN r.Rc = 0 THEN 1 ELSE 0 END AS BIT)
        , ReasonCode    = CAST(r.Rc AS INT)
        , ReasonMessage = CAST(CASE r.Rc
                                   WHEN 304 THEN N'해당 시간대의 마감시간이 지났습니다.'
                                   WHEN 308 THEN N'오늘은 업무일이 아닙니다.'
                                   WHEN 309 THEN N'현재는 업무 운영시간이 아닙니다.'
                                   WHEN 502 THEN N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.'
                                   WHEN 503 THEN N'예약일이 오늘인 업무만 접수할 수 있습니다.'
                                   ELSE N'' END AS NVARCHAR(300))
    FROM [dbo].[예약접수] w
    CROSS JOIN (VALUES (1,'EDIT_RESERVATION'),(2,'CANCEL_RESERVATION'),(3,'START_RECEPTION'),
                       (4,'EDIT_EXTRA'),(5,'CANCEL_RECEPTION')) a(Ord, Code)
    CROSS APPLY [dbo].[UFN_HC_일정확인](@ServerTime, w.[예약일], w.[시간대코드], 'RECEPTION') s
    CROSS APPLY (SELECT Rc =
          CASE
              WHEN a.Code IN ('EDIT_RESERVATION','CANCEL_RESERVATION','START_RECEPTION')
                   AND w.[상태코드] <> 'RSV'                                   THEN 502
              WHEN a.Code IN ('EDIT_EXTRA','CANCEL_RECEPTION')
                   AND w.[상태코드] <> 'RCP'                                   THEN 502
              WHEN s.WorkCode <> 0                                                THEN s.WorkCode
              WHEN a.Code = 'START_RECEPTION' AND w.[예약일] <> @Today   THEN 503
              WHEN a.Code = 'START_RECEPTION' AND s.CutoffPassed = 1              THEN 304
              ELSE 0
          END) r
    WHERE w.[업무ID] = @WorkId
    ORDER BY a.Ord;
END
GO
-- 허용 Code 0 / 100~102 / 200 / 500~502 / 601 / 700~701 (05 §9).
-- 휴무일·정원마감·TGT 비대상은 SP 실패가 아니다 — RS0 Success=1 Code=0 + RS1 CanSave=0 + BlockCode 다 (05 §3.3).
-- Scope=NONE 에 Code=1 을 쓰지 않는다. Code=1 은 Write SP No-op 전용이다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_SELECT_예약가능정보]
    @PatientId        BIGINT,
    @WorkId           BIGINT,
    @RowVersion       BINARY(8),
    @ReservationType  VARCHAR(10),
    @ReservationDate  DATE,
    @TimeSlot         CHAR(2),
    @AexOpt01Selected BIT,
    @AexOpt02Selected BIT,
    @AexOpt03Selected BIT,
    @AexOpt04Selected BIT,
    @AexOpt05Selected BIT,
    @AexOpt06Selected BIT,
    @AexOpt07Selected BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
    DECLARE @Today DATE = CONVERT(DATE, @ServerTime);

    DECLARE @Scope        VARCHAR(12);
    DECLARE @DateChanged  BIT = NULL, @SlotChanged BIT = NULL, @ExtraChanged BIT = NULL;
    DECLARE @WDate DATE, @WSlot CHAR(2), @WStatus CHAR(3), @WPatient BIGINT;
    DECLARE @OtherWorkId  BIGINT = NULL;
    DECLARE @Req TABLE (OptionCode VARCHAR(10) PRIMARY KEY);

    -- 1. 문자열 정규화 (05 §2.2)
    -- [X] 16개 SP 중 이 하나만 정규화가 없었다. 그래서 ' NORMAL'(앞 공백)이 여기서는 101 인데
    --     같은 값을 USP_HC_INSERT_예약 은 정규화해 성공시켰다 - 사전조회와 저장의 판정이 갈렸다.
    SET @ReservationType = NULLIF(UPPER(LTRIM(RTRIM(@ReservationType))), '');
    SET @TimeSlot        = NULLIF(UPPER(LTRIM(RTRIM(@TimeSlot))), '');

    -- 2. 필수값 (05 §5: 필수값 → 값 형식·허용값 순서)
    --    ReservationType·ReservationDate 가 빠지면 NOT IN 이 UNKNOWN 이라 101 도 안 나고
    --    NULL 이 Scope 계산까지 흘러들어간다. 여기서 막는다.
    IF @PatientId IS NULL OR @ReservationType IS NULL OR @ReservationDate IS NULL
       OR @AexOpt01Selected IS NULL OR @AexOpt02Selected IS NULL OR @AexOpt03Selected IS NULL
       OR @AexOpt04Selected IS NULL OR @AexOpt05Selected IS NULL OR @AexOpt06Selected IS NULL
       OR @AexOpt07Selected IS NULL
       OR (@WorkId IS NOT NULL AND @RowVersion IS NULL)
    BEGIN
    -- [X] Field 식을 SELECT 안에 인라인으로 두면 RS0 블록이 길어져 V17(5컬럼 명시 CAST)의
    --     검사 창 밖으로 ServerTime 이 밀린다. 변수로 올려 RS0 블록을 5줄로 유지한다.
    DECLARE @F100 VARCHAR(50) =
        CASE WHEN @PatientId        IS NULL THEN 'PatientId'
             WHEN @ReservationType  IS NULL THEN 'ReservationType'
             WHEN @ReservationDate  IS NULL THEN 'ReservationDate'
             WHEN @WorkId IS NOT NULL AND @RowVersion IS NULL THEN 'RowVersion'
             WHEN @AexOpt01Selected IS NULL THEN 'AexOpt01Selected'
             WHEN @AexOpt02Selected IS NULL THEN 'AexOpt02Selected'
             WHEN @AexOpt03Selected IS NULL THEN 'AexOpt03Selected'
             WHEN @AexOpt04Selected IS NULL THEN 'AexOpt04Selected'
             WHEN @AexOpt05Selected IS NULL THEN 'AexOpt05Selected'
             WHEN @AexOpt06Selected IS NULL THEN 'AexOpt06Selected'
             WHEN @AexOpt07Selected IS NULL THEN 'AexOpt07Selected'
             ELSE 'RowVersion' END;
        SELECT CAST(0 AS BIT) AS Success, CAST(100 AS INT) AS Code
             , CAST(N'필수값을 입력하십시오.' AS NVARCHAR(300)) AS Message
             , CAST(@F100 AS VARCHAR(50)) AS Field
             , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- 3. 허용값
    -- [X] @TimeSlot 허용값 검증이 없었다. 'XX' 를 넣으면 @SelBlock 이 NULL 이 되고
    --     NULL <> 0 이 UNKNOWN 이라 우선순위 CASE 를 그대로 통과해
    --     RS0 Success=1 Code=0 · RS1 CanSave=0 BlockCode=0 BlockMessage='' 가 나갔다.
    --     화면은 저장 버튼이 죽어 있는데 사유를 표시할 수 없다. 101 로 잘라낸다 (05 §5 2단계).
    IF @TimeSlot IS NOT NULL AND @TimeSlot NOT IN ('AM','PM')
    BEGIN
        SELECT CAST(0 AS BIT) AS Success, CAST(101 AS INT) AS Code
             , CAST(N'입력값이 올바르지 않습니다.' AS NVARCHAR(300)) AS Message
             , CAST('TimeSlot' AS VARCHAR(50)) AS Field
             , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    IF @ReservationType NOT IN ('NORMAL','WALKIN')
    BEGIN
        SELECT CAST(0 AS BIT) AS Success, CAST(101 AS INT) AS Code
             , CAST(N'입력값이 올바르지 않습니다.' AS NVARCHAR(300)) AS Message
             , CAST('ReservationType' AS VARCHAR(50)) AS Field
             , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- 4. Parameter 조합 (05 §9.3). 나머지 한 종("기존 날짜 유지 + TimeSlot NULL")은
    --    Work 행이 있어야 판정할 수 있으므로 6단계 뒤로 미룬다.
    IF (@WorkId IS NULL AND @RowVersion IS NOT NULL)
       OR (@WorkId IS NOT NULL AND @ReservationType = 'WALKIN')
       OR (@ReservationType = 'WALKIN' AND @ReservationDate <> @Today)
    BEGIN
        SELECT CAST(0 AS BIT) AS Success, CAST(102 AS INT) AS Code
             , CAST(N'함께 사용할 수 없는 입력값 조합입니다.' AS NVARCHAR(300)) AS Message
             , CAST(CASE WHEN @WorkId IS NULL AND @RowVersion IS NOT NULL THEN 'RowVersion'
                         WHEN @WorkId IS NOT NULL THEN 'ReservationType'
                         ELSE 'ReservationDate' END AS VARCHAR(50)) AS Field
             , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- 5. Patient 존재
    IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [수검자ID] = @PatientId)
    BEGIN
        SELECT CAST(0 AS BIT) AS Success, CAST(200 AS INT) AS Code
             , CAST(N'수검자를 찾을 수 없습니다.' AS NVARCHAR(300)) AS Message
             , CAST('PatientId' AS VARCHAR(50)) AS Field
             , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- 6. Work 존재 → 소유 → 상태 → 동시성
    IF @WorkId IS NOT NULL
    BEGIN
        SELECT @WPatient = w.[수검자ID], @WDate = w.[예약일]
             , @WSlot = w.[시간대코드], @WStatus = w.[상태코드]
        FROM [dbo].[예약접수] w WHERE w.[업무ID] = @WorkId;

        IF @WPatient IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Success, CAST(500 AS INT) AS Code
                 , CAST(N'예약·접수 업무를 찾을 수 없습니다.' AS NVARCHAR(300)) AS Message
                 , CAST('WorkId' AS VARCHAR(50)) AS Field
                 , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
            RETURN;
        END
        IF @WPatient <> @PatientId
        BEGIN
            SELECT CAST(0 AS BIT) AS Success, CAST(501 AS INT) AS Code
                 , CAST(N'요청한 수검자와 예약·접수 업무의 수검자가 다릅니다.' AS NVARCHAR(300)) AS Message
                 , CAST('PatientId' AS VARCHAR(50)) AS Field
                 , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
            RETURN;
        END
        IF @WStatus <> 'RSV'
        BEGIN
            SELECT CAST(0 AS BIT) AS Success, CAST(502 AS INT) AS Code
                 , CAST(N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.' AS NVARCHAR(300)) AS Message
                 , CAST('WorkId' AS VARCHAR(50)) AS Field
                 , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
            RETURN;
        END
        IF NOT EXISTS (SELECT 1 FROM [dbo].[예약접수]
                        WHERE [업무ID] = @WorkId AND [행버전] = @RowVersion)
        BEGIN
            SELECT CAST(0 AS BIT) AS Success, CAST(601 AS INT) AS Code
                 , CAST(N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.' AS NVARCHAR(300)) AS Message
                 , CAST('RowVersion' AS VARCHAR(50)) AS Field
                 , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
            RETURN;
        END
        IF @ReservationDate = @WDate AND @TimeSlot IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS Success, CAST(102 AS INT) AS Code
                 , CAST(N'함께 사용할 수 없는 입력값 조합입니다.' AS NVARCHAR(300)) AS Message
                 , CAST('TimeSlot' AS VARCHAR(50)) AS Field
                 , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
            RETURN;
        END
    END

    -- 7. Scope 계산 (05 §9.4). ExtraChanged 는 §28.1 대로 EXCEPT 양방향이다.
    INSERT INTO @Req (OptionCode)
    SELECT v.c FROM (VALUES ('OPT01',@AexOpt01Selected),('OPT02',@AexOpt02Selected),('OPT03',@AexOpt03Selected),
                            ('OPT04',@AexOpt04Selected),('OPT05',@AexOpt05Selected),('OPT06',@AexOpt06Selected),
                            ('OPT07',@AexOpt07Selected)) v(c, b)
     WHERE v.b = 1;

    IF @WorkId IS NULL
        SET @Scope = 'ALL';
    ELSE
    BEGIN
        SET @DateChanged = CASE WHEN @ReservationDate <> @WDate THEN 1 ELSE 0 END;
        SET @SlotChanged = CASE WHEN @TimeSlot IS NOT NULL AND @TimeSlot <> @WSlot THEN 1 ELSE 0 END;
        SET @ExtraChanged = CASE WHEN EXISTS (
                SELECT OptionCode FROM @Req
                EXCEPT
                SELECT m.[추가검사코드] FROM [dbo].[예약접수] w
                  JOIN [dbo].[검사코드] m
                    ON N',' + ISNULL(w.[추가검사항목], N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
                 WHERE w.[업무ID] = @WorkId)
            OR EXISTS (
                SELECT m.[추가검사코드] FROM [dbo].[예약접수] w
                  JOIN [dbo].[검사코드] m
                    ON N',' + ISNULL(w.[추가검사항목], N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
                 WHERE w.[업무ID] = @WorkId
                EXCEPT
                SELECT OptionCode FROM @Req)
            THEN 1 ELSE 0 END;

        SET @Scope = CASE WHEN @DateChanged = 1                              THEN 'ALL'
                          WHEN @SlotChanged = 1 AND @ExtraChanged = 1        THEN 'SLOT_EXTRA'
                          WHEN @SlotChanged = 1                             THEN 'SLOT'
                          WHEN @ExtraChanged = 1                            THEN 'EXTRA'
                          ELSE 'NONE' END;
    END

    -- 9/10. 검사 Master 무결성
    IF @Scope IN ('ALL','EXTRA','SLOT_EXTRA')
       AND ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [국가검사규칙코드] IS NOT NULL) < 8
            OR (SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [추가검사코드] IS NOT NULL) <> 7)
    BEGIN
        SELECT CAST(0 AS BIT) AS Success, CAST(700 AS INT) AS Code
             , CAST(N'검사 Master 구성이 올바르지 않습니다.' AS NVARCHAR(300)) AS Message
             , CAST(NULL AS VARCHAR(50)) AS Field
             , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END
    IF @Scope IN ('EXTRA','SLOT_EXTRA')
       AND EXISTS (SELECT 1 FROM [dbo].[예약접수]
                    WHERE [업무ID] = @WorkId AND LEN(ISNULL([국가검사항목], N'')) = 0)
    BEGIN
        SELECT CAST(0 AS BIT) AS Success, CAST(701 AS INT) AS Code
             , CAST(N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.' AS NVARCHAR(300)) AS Message
             , CAST('WorkId' AS VARCHAR(50)) AS Field
             , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- 11. 현재 공통 업무 가능 여부
    DECLARE @CanWorkNow BIT, @WorkCode INT;
    SELECT @CanWorkNow = s.CanWorkNow, @WorkCode = s.WorkCode
      FROM [dbo].[UFN_HC_일정확인](@ServerTime, @Today, 'AM', 'NONE') s;

    -- 12. 다른 유효업무
    IF @Scope IN ('ALL','SLOT','SLOT_EXTRA')
    BEGIN
        DECLARE @OtherCnt INT = (SELECT COUNT(*) FROM [dbo].[예약접수] w
                                  WHERE w.[수검자ID] = @PatientId
                                    AND w.[예약일] >= @Today
                                    AND w.[상태코드] IN ('RSV','RCP')
                                    AND (@WorkId IS NULL OR w.[업무ID] <> @WorkId));
        IF @OtherCnt >= 2
        BEGIN
            SELECT CAST(0 AS BIT) AS Success, CAST(701 AS INT) AS Code
                 , CAST(N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.' AS NVARCHAR(300)) AS Message
                 , CAST('WorkId' AS VARCHAR(50)) AS Field
                 , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
            RETURN;
        END
        SELECT TOP (1) @OtherWorkId = w.[업무ID] FROM [dbo].[예약접수] w
         WHERE w.[수검자ID] = @PatientId
           AND w.[예약일] >= @Today
           AND w.[상태코드] IN ('RSV','RCP')
           AND (@WorkId IS NULL OR w.[업무ID] <> @WorkId)
         ORDER BY w.[업무ID];
    END

    -- 요청일 자체의 판정(300/301/302). 슬롯·마감과 무관한 날짜 수준 차단이다.
    DECLARE @DateBlock INT = (SELECT ReasonCode FROM
        [dbo].[UFN_HC_일정확인](@ServerTime, @ReservationDate, 'AM', 'NONE'));
    DECLARE @ScheduleOk BIT = CASE WHEN @DateBlock = 0 THEN 1 ELSE 0 END;
    DECLARE @CutoffType VARCHAR(10) = CASE WHEN @ReservationType = 'WALKIN' THEN 'RECEPTION' ELSE 'NORMAL' END;

    -- AM/PM 두 행. 05 §9.7 의 세 경우를 한 식으로 만족한다.
    DECLARE @Slots TABLE (
        TimeSlot CHAR(2) PRIMARY KEY, SlotName NVARCHAR(10), Capacity INT,
        CurrentCount INT, AfterCount INT, SeatsLeft INT, IsOpen BIT,
        CutoffTime TIME(0), CutoffPassed BIT, CanSelect BIT, BlockCode INT, BlockMessage NVARCHAR(300));

    INSERT INTO @Slots
    SELECT
          v.Slot
        , CASE v.Slot WHEN 'AM' THEN N'오전' ELSE N'오후' END
        , 20
        , c.Cnt
        , a.After
        , CASE WHEN 20 - a.After < 0 THEN 0 ELSE 20 - a.After END
        , s.IsOpen
        , s.CutoffTime
        , s.CutoffPassed
        , CASE WHEN s.ReasonCode = 0 AND a.After <= 20 THEN 1 ELSE 0 END
        , CASE WHEN s.ReasonCode <> 0 THEN s.ReasonCode
               WHEN a.After > 20 THEN 305 ELSE 0 END
        , CASE WHEN s.ReasonCode <> 0 THEN s.ReasonMessage
               WHEN a.After > 20 THEN N'해당 시간대의 예약 정원이 마감되었습니다.'
               ELSE N'' END
    FROM (VALUES ('AM'),('PM')) v(Slot)
    CROSS APPLY [dbo].[UFN_HC_일정확인](@ServerTime, @ReservationDate, v.Slot, @CutoffType) s
    CROSS APPLY (SELECT Cnt = COUNT(*) FROM [dbo].[예약접수] x
                  WHERE x.[예약일] = @ReservationDate
                    AND x.[시간대코드] = v.Slot
                    AND x.[상태코드] IN ('RSV','RCP')) c
    CROSS APPLY (SELECT After = c.Cnt
                   - CASE WHEN @WorkId IS NOT NULL
                           AND EXISTS (SELECT 1 FROM [dbo].[예약접수] y
                                        WHERE y.[업무ID] = @WorkId
                                          AND y.[예약일] = @ReservationDate
                                          AND y.[시간대코드] = v.Slot
                                          AND y.[상태코드] IN ('RSV','RCP'))
                          THEN 1 ELSE 0 END + 1) a;

    -- TGT / NEX / AEX
    DECLARE @Eligible BIT = NULL, @TgtReason INT = NULL;
    IF @Scope = 'ALL' AND @ScheduleOk = 1
        SELECT @Eligible = g.Eligible, @TgtReason = g.ReasonCode
          FROM [dbo].[UFN_HC_검진대상확인](@PatientId, @ReservationDate) g;

    DECLARE @NexCnt INT = 0;
    IF @Scope = 'ALL' AND @ScheduleOk = 1 AND @Eligible = 1
        SET @NexCnt = (SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@PatientId, @ReservationDate));

    DECLARE @AexOn BIT = CASE WHEN @Scope IN ('EXTRA','SLOT_EXTRA')
                                OR (@Scope = 'ALL' AND @ScheduleOk = 1) THEN 1 ELSE 0 END;
    DECLARE @UseSaved BIT = CASE WHEN @Scope IN ('EXTRA','SLOT_EXTRA') THEN 1 ELSE 0 END;

    DECLARE @Aex TABLE (OptionCode VARCHAR(10) PRIMARY KEY, ExamCode VARCHAR(10), ExamName NVARCHAR(100),
                        Requested BIT, Selected BIT, CanSelect BIT, ReasonCode INT, ReasonMessage NVARCHAR(300));
    IF @AexOn = 1
        INSERT INTO @Aex
        SELECT x.OptionCode, x.ExamCode, x.ExamName, x.Requested, x.Selected, x.CanSelect, x.ReasonCode, x.ReasonMessage
          FROM [dbo].[UFN_HC_추가검사확인](@PatientId, @ReservationDate, @WorkId, @UseSaved,
                 @AexOpt01Selected, @AexOpt02Selected, @AexOpt03Selected, @AexOpt04Selected,
                 @AexOpt05Selected, @AexOpt06Selected, @AexOpt07Selected) x;

    DECLARE @BadAex INT = (SELECT COUNT(*) FROM @Aex WHERE Requested = 1 AND CanSelect = 0);
    DECLARE @BadAexCode INT = (SELECT TOP (1) ReasonCode FROM @Aex
                                WHERE Requested = 1 AND CanSelect = 0 ORDER BY OptionCode);

    -- RS1 의 대표 BlockCode (05 §9.6 우선순위)
    DECLARE @SelBlock INT = (SELECT BlockCode FROM @Slots WHERE TimeSlot = @TimeSlot);
    DECLARE @AnySlot  BIT = CASE WHEN EXISTS (SELECT 1 FROM @Slots WHERE CanSelect = 1) THEN 1 ELSE 0 END;
    DECLARE @BlockCode INT =
        CASE WHEN @Scope = 'NONE'                                    THEN 0
             WHEN @CanWorkNow = 0                                    THEN @WorkCode
             WHEN @OtherWorkId IS NOT NULL                           THEN 306
             WHEN @TimeSlot IS NOT NULL AND @SelBlock <> 0           THEN @SelBlock
             WHEN @TimeSlot IS NULL AND @AnySlot = 0                 THEN 307
             WHEN @Scope = 'ALL' AND @ScheduleOk = 1
              AND ISNULL(@Eligible, 1) = 0                           THEN @TgtReason
             WHEN @BadAex > 0                                        THEN @BadAexCode
             ELSE 0 END;

    DECLARE @CanSave BIT =
        CASE @Scope
            WHEN 'NONE' THEN 0
            WHEN 'ALL'  THEN CASE WHEN @CanWorkNow = 1 AND @OtherWorkId IS NULL
                                   AND @TimeSlot IS NOT NULL
                                   AND (SELECT CanSelect FROM @Slots WHERE TimeSlot = @TimeSlot) = 1
                                   AND @Eligible = 1 AND @NexCnt >= 8 AND @BadAex = 0
                              THEN 1 ELSE 0 END
            WHEN 'SLOT' THEN CASE WHEN @CanWorkNow = 1 AND @OtherWorkId IS NULL
                                   AND @TimeSlot IS NOT NULL
                                   AND (SELECT CanSelect FROM @Slots WHERE TimeSlot = @TimeSlot) = 1
                              THEN 1 ELSE 0 END
            WHEN 'EXTRA' THEN CASE WHEN @CanWorkNow = 1 AND @BadAex = 0 AND @ExtraChanged = 1
                              THEN 1 ELSE 0 END
            ELSE CASE WHEN @CanWorkNow = 1 AND @OtherWorkId IS NULL
                       AND @TimeSlot IS NOT NULL
                       AND (SELECT CanSelect FROM @Slots WHERE TimeSlot = @TimeSlot) = 1
                       AND @BadAex = 0
                  THEN 1 ELSE 0 END
        END;

    -- RS0
    SELECT CAST(1 AS BIT) AS Success, CAST(0 AS INT) AS Code
         , CAST(N'정상 처리되었습니다.' AS NVARCHAR(300)) AS Message
         , CAST(NULL AS VARCHAR(50)) AS Field
         , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;

    -- RS1 예약요약 (14컬럼, 정확히 1행)
    SELECT
          Scope           = CAST(@Scope AS VARCHAR(12))
        , PatientId       = CAST(@PatientId AS BIGINT)
        , WorkId          = CAST(@WorkId AS BIGINT)
        , ReservationType = CAST(@ReservationType AS VARCHAR(10))
        , ReservationDate = CAST(@ReservationDate AS DATE)
        , TimeSlot        = CAST(@TimeSlot AS CHAR(2))
        , DateChanged     = CAST(@DateChanged AS BIT)
        , SlotChanged     = CAST(@SlotChanged AS BIT)
        , ExtraChanged    = CAST(@ExtraChanged AS BIT)
        , CanWorkNow      = CAST(@CanWorkNow AS BIT)
        , OtherWorkId     = CAST(@OtherWorkId AS BIGINT)
        , CanSave         = CAST(@CanSave AS BIT)
        , BlockCode       = CAST(@BlockCode AS INT)
        , BlockMessage    = CAST(CASE @BlockCode
                                     WHEN 0   THEN N''
                                     WHEN 300 THEN N'과거 날짜는 예약할 수 없습니다.'
                                     WHEN 301 THEN N'일요일은 업무일이 아닙니다.'
                                     WHEN 302 THEN N'선택한 날짜는 휴무일입니다.'
                                     WHEN 303 THEN N'선택한 시간대는 운영하지 않습니다.'
                                     WHEN 304 THEN N'해당 시간대의 마감시간이 지났습니다.'
                                     WHEN 305 THEN N'해당 시간대의 예약 정원이 마감되었습니다.'
                                     WHEN 306 THEN N'수검자에게 다른 유효 예약 또는 접수 업무가 있습니다.'
                                     WHEN 307 THEN N'선택할 수 있는 시간대가 없습니다.'
                                     WHEN 308 THEN N'오늘은 업무일이 아닙니다.'
                                     WHEN 309 THEN N'현재는 업무 운영시간이 아닙니다.'
                                     WHEN 400 THEN N'예약일 기준 만 20세 미만으로 검진 대상이 아닙니다.'
                                     WHEN 401 THEN N'일반건강검진 2년 주기가 도래하지 않았습니다.'
                                     WHEN 410 THEN N'현재 사용할 수 없는 추가검사입니다.'
                                     WHEN 411 THEN N'성별 조건을 충족하지 않는 추가검사입니다.'
                                     WHEN 412 THEN N'일반건강검진에 포함된 검사입니다.'
                                     ELSE N'' END AS NVARCHAR(300));

    -- RS2 시간대정보 — ALL/SLOT/SLOT_EXTRA 는 2행, EXTRA/NONE 은 같은 Schema 의 0행
    SELECT
          TimeSlot     = CAST(s.TimeSlot AS CHAR(2))
        , SlotName     = CAST(s.SlotName AS NVARCHAR(10))
        , Capacity     = CAST(s.Capacity AS INT)
        , CurrentCount = CAST(s.CurrentCount AS INT)
        , AfterCount   = CAST(s.AfterCount AS INT)
        , SeatsLeft    = CAST(s.SeatsLeft AS INT)
        , IsOpen       = CAST(s.IsOpen AS BIT)
        , CutoffTime   = CAST(s.CutoffTime AS TIME(0))
        , CutoffPassed = CAST(s.CutoffPassed AS BIT)
        , CanSelect    = CAST(s.CanSelect AS BIT)
        , BlockCode    = CAST(s.BlockCode AS INT)
        , BlockMessage = CAST(s.BlockMessage AS NVARCHAR(300))
    FROM @Slots s
    WHERE @Scope IN ('ALL','SLOT','SLOT_EXTRA')
    ORDER BY s.TimeSlot ASC;

    -- RS3 검진대상 — ALL 에서 일정 평가가 가능할 때만 1행
    SELECT
          Eligible        = CAST(g.Eligible AS BIT)
        , Age             = CAST(g.Age AS INT)
        , LastCheckupDate = CAST(g.LastCheckupDate AS DATE)
        , ReasonCode      = CAST(g.ReasonCode AS INT)
        , ReasonMessage   = CAST(g.ReasonMessage AS NVARCHAR(300))
    FROM [dbo].[UFN_HC_검진대상확인](@PatientId, @ReservationDate) g
    WHERE @Scope = 'ALL' AND @ScheduleOk = 1;

    -- RS4 국가검사항목 — ALL + TGT 대상일 때만 8~11행
    SELECT
          ExamCode = CAST(n.ExamCode AS VARCHAR(10))
        , ExamName = CAST(n.ExamName AS NVARCHAR(100))
        , ExamType = CAST(n.ExamType AS VARCHAR(12))
        , RuleCode = CAST(n.RuleCode AS VARCHAR(10))
    FROM [dbo].[UFN_HC_국가검사구성](@PatientId, @ReservationDate) n
    WHERE @Scope = 'ALL' AND @ScheduleOk = 1
    ORDER BY n.ExamCode ASC;

    -- RS5 추가검사항목 — AEX 를 실제 평가하는 Scope 에서만 7행
    SELECT
          OptionCode    = CAST(a.OptionCode AS VARCHAR(10))
        , ExamCode      = CAST(a.ExamCode AS VARCHAR(10))
        , ExamName      = CAST(a.ExamName AS NVARCHAR(100))
        , Requested     = CAST(a.Requested AS BIT)
        , Selected      = CAST(a.Selected AS BIT)
        , CanSelect     = CAST(a.CanSelect AS BIT)
        , ReasonCode    = CAST(a.ReasonCode AS INT)
        , ReasonMessage = CAST(a.ReasonMessage AS NVARCHAR(300))
    FROM @Aex a
    ORDER BY a.OptionCode ASC;
END
GO
-- SP-LOG-01 (05 §8.3). 00 CP-06 이 변경기록을 열람용으로 규정한 것을 받는 유일한 조회 SP 다.
--   대상 행 1개의 변경 내역만 낸다 - 기간·조작자 전체 검색은 제공하지 않는다.
--   IX_변경이력_TARGET 의 Key 순서(대상테이블, 대상키, 기록일시 DESC)가 술어와 정렬을 그대로 덮는다.
--   [!] 대상 행의 존재를 확인하지 않는다. 감사 기록은 대상 행보다 오래 살기 때문이다 (04 §8.6.3).
--       기록이 0건이면 200 이 아니라 Code=0 + RS1 0행이다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_SELECT_변경이력]
    @TargetTable NVARCHAR(10),
    @TargetKey   BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
    DECLARE @Code INT = 0, @Field VARCHAR(50) = NULL;
    DECLARE @Msg NVARCHAR(300) = N'정상 처리되었습니다.';

    -- 1. 정규화 (05 §2.2). 대상테이블은 한글 값이라 UPPER 하지 않는다.
    SET @TargetTable = NULLIF(LTRIM(RTRIM(@TargetTable)), N'');

    -- 2. 필수값 -> 값 형식 (05 §5)
    IF @TargetTable IS NULL
    BEGIN SET @Code = 100; SET @Field = 'TargetTable'; SET @Msg = N'필수값을 입력하십시오.'; END
    ELSE IF @TargetKey IS NULL
    BEGIN SET @Code = 100; SET @Field = 'TargetKey'; SET @Msg = N'필수값을 입력하십시오.'; END
    -- CK_변경이력_TARGET_TABLE 과 같은 도메인이다 (04 §8.6.3). 완료이력은 쓰는 Write SP 가 0개다.
    ELSE IF @TargetTable NOT IN (N'수검자', N'예약접수')
    BEGIN SET @Code = 101; SET @Field = 'TargetTable'; SET @Msg = N'입력값이 올바르지 않습니다.'; END

    -- RS0
    SELECT
          CAST(CASE WHEN @Code = 0 THEN 1 ELSE 0 END AS BIT) AS Success
        , CAST(@Code AS INT)                  AS Code
        , CAST(@Msg AS NVARCHAR(300))         AS Message
        , CAST(@Field AS VARCHAR(50))         AS Field
        , CAST(@ServerTime AS DATETIME2(7))   AS ServerTime;

    IF @Code <> 0 RETURN;

    -- RS1 변경이력 - 대상테이블은 싣지 않는다. 호출자가 이미 알고 넘긴 값이다 (05 §8.3).
    SELECT
          LogId        = CAST(h.[이력ID]   AS BIGINT)
        , RecordedAt   = CAST(h.[기록일시] AS DATETIME2(0))
        , OperatorName = CAST(h.[조작자명] AS NVARCHAR(50))
        , ColumnName   = CAST(h.[컬럼명]   AS NVARCHAR(30))
        , BeforeValue  = CAST(h.[변경전]   AS NVARCHAR(4000))
        , AfterValue   = CAST(h.[변경후]   AS NVARCHAR(4000))
      FROM [dbo].[변경이력] h
     WHERE h.[대상테이블] = @TargetTable
       AND h.[대상키]     = @TargetKey
     ORDER BY h.[기록일시] DESC, h.[이력ID] DESC;
END
GO
