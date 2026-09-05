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

    DECLARE @Cnt INT = (SELECT COUNT(*) FROM [dbo].[예약접수] w
                         WHERE w.[PatientId] = @PatientId
                           AND w.[ReservationDate] >= @Today
                           AND w.[StatusCode] IN ('RSV','RCP'));
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
          WorkId          = CAST(w.[WorkId] AS BIGINT)
        , ReservationDate = CAST(w.[ReservationDate] AS DATE)
        , TimeSlot        = CAST(w.[TimeSlotCode] AS CHAR(2))
        , Status          = CAST(w.[StatusCode] AS CHAR(3))
        , StatusName      = CAST(CASE w.[StatusCode]
                                     WHEN 'RSV' THEN N'예약'
                                     WHEN 'RCP' THEN N'접수완료'
                                     WHEN 'CNR' THEN N'예약취소'
                                     ELSE N'접수취소' END AS NVARCHAR(10))
        , IsToday         = CAST(CASE WHEN w.[ReservationDate] = @Today THEN 1 ELSE 0 END AS BIT)
        , RowVersion      = CAST(w.[RowVersion] AS BINARY(8))
    FROM [dbo].[예약접수] w
    WHERE w.[PatientId] = @PatientId
      AND w.[ReservationDate] >= @Today
      AND w.[StatusCode] IN ('RSV','RCP');
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
          WorkId          = CAST(w.[WorkId] AS BIGINT)
        , PatientId       = CAST(w.[PatientId] AS BIGINT)
        , ReservationDate = CAST(w.[ReservationDate] AS DATE)
        , TimeSlot        = CAST(w.[TimeSlotCode] AS CHAR(2))
        , Status          = CAST(w.[StatusCode] AS CHAR(3))
        , StatusName      = CAST(CASE w.[StatusCode]
                                     WHEN 'RSV' THEN N'예약'
                                     WHEN 'RCP' THEN N'접수완료'
                                     WHEN 'CNR' THEN N'예약취소'
                                     ELSE N'접수취소' END AS NVARCHAR(10))
        , Name            = CAST(p.[Name] AS NVARCHAR(100))
        , ChartNo         = CAST(p.[ChartNo] AS NVARCHAR(100))
        , Gender          = CAST(p.[Gender] AS CHAR(1))
        , Birthday        = CAST(p.[Birthday] AS VARCHAR(8))
        , MobilePhone     = CAST(p.[CelNumber] AS VARCHAR(13))
    FROM [dbo].[예약접수] w
    JOIN [dbo].[수검자] p ON p.[PatientId] = w.[PatientId]
    WHERE (@FromDate IS NULL OR w.[ReservationDate] >= @FromDate)
      AND (@ToDate   IS NULL OR w.[ReservationDate] <= @ToDate)
      AND (@Status   IS NULL OR w.[StatusCode]      =  @Status)
      AND (@ChartNo  IS NULL OR p.[ChartNo]         =  @ChartNo)
      AND (@Name     IS NULL OR p.[Name]            LIKE @Name + N'%')
    ORDER BY w.[ReservationDate] ASC, w.[TimeSlotCode] ASC, p.[Name] ASC, w.[WorkId] ASC;
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

    IF NOT EXISTS (SELECT 1 FROM [dbo].[예약접수] WHERE [WorkId] = @WorkId)
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS Success
            , CAST(500 AS INT)                  AS Code
            , CAST(N'예약·접수 업무를 찾을 수 없습니다.' AS NVARCHAR(300)) AS Message
            , CAST('WorkId' AS VARCHAR(50))     AS Field
            , CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- 저장 NEX 0행은 검사구성 손상이다 (CORRUPT-2 가 이것을 만든다)
    IF NOT EXISTS (SELECT 1 FROM [dbo].[검사항목] d
                    WHERE d.[WorkId] = @WorkId AND d.[ExamSourceCode] = 'NEX')
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
          WorkId          = CAST(w.[WorkId] AS BIGINT)
        , PatientId       = CAST(w.[PatientId] AS BIGINT)
        , ChartNo         = CAST(p.[ChartNo] AS NVARCHAR(100))
        , Name            = CAST(p.[Name] AS NVARCHAR(100))
        , Birthday        = CAST(p.[Birthday] AS VARCHAR(8))
        , Gender          = CAST(p.[Gender] AS CHAR(1))
        , MobilePhone     = CAST(p.[CelNumber] AS VARCHAR(13))
        , ReservationDate = CAST(w.[ReservationDate] AS DATE)
        , TimeSlot        = CAST(w.[TimeSlotCode] AS CHAR(2))
        , Status          = CAST(w.[StatusCode] AS CHAR(3))
        , StatusName      = CAST(CASE w.[StatusCode]
                                     WHEN 'RSV' THEN N'예약'
                                     WHEN 'RCP' THEN N'접수완료'
                                     WHEN 'CNR' THEN N'예약취소'
                                     ELSE N'접수취소' END AS NVARCHAR(10))
        , Capacity        = CAST(20 AS INT)
        , CurrentCount    = CAST(c.Cnt AS INT)
        , SeatsLeft       = CAST(CASE WHEN 20 - c.Cnt < 0 THEN 0 ELSE 20 - c.Cnt END AS INT)
        , RowVersion      = CAST(w.[RowVersion] AS BINARY(8))
    FROM [dbo].[예약접수] w
    JOIN [dbo].[수검자] p ON p.[PatientId] = w.[PatientId]
    CROSS APPLY (SELECT Cnt = COUNT(*) FROM [dbo].[예약접수] x
                  WHERE x.[ReservationDate] = w.[ReservationDate]
                    AND x.[TimeSlotCode]    = w.[TimeSlotCode]
                    AND x.[StatusCode] IN ('RSV','RCP')) c
    WHERE w.[WorkId] = @WorkId;

    -- RS2 국가검사항목 — 실제 저장된 NEX 만
    SELECT
          ExamCode = CAST(m.[ExamItemCode] AS VARCHAR(10))
        , ExamName = CAST(m.[ExamItemName] AS NVARCHAR(100))
        , ExamType = CAST(CASE WHEN m.[NexRuleCode] = 'NEX-01' THEN 'BASIC' ELSE 'CONDITIONAL' END AS VARCHAR(12))
        , RuleCode = CAST(m.[NexRuleCode] AS VARCHAR(10))
    FROM [dbo].[검사항목] d
    JOIN [dbo].[검사코드] m ON m.[ExamItemCode] = d.[ExamItemCode]
    WHERE d.[WorkId] = @WorkId AND d.[ExamSourceCode] = 'NEX'
    ORDER BY m.[ExamItemCode] ASC;

    -- RS3 추가검사항목 — 실제 저장된 AEX 만
    SELECT
          OptionCode = CAST(m.[AdditionalExamCode] AS VARCHAR(10))
        , ExamCode   = CAST(m.[ExamItemCode] AS VARCHAR(10))
        , ExamName   = CAST(m.[ExamItemName] AS NVARCHAR(100))
    FROM [dbo].[검사항목] d
    JOIN [dbo].[검사코드] m ON m.[ExamItemCode] = d.[ExamItemCode]
    WHERE d.[WorkId] = @WorkId AND d.[ExamSourceCode] = 'AEX'
    ORDER BY m.[AdditionalExamCode] ASC;

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
    CROSS APPLY [dbo].[UFN_HC_일정확인](@ServerTime, w.[ReservationDate], w.[TimeSlotCode], 'RECEPTION') s
    CROSS APPLY (SELECT Rc =
          CASE
              WHEN a.Code IN ('EDIT_RESERVATION','CANCEL_RESERVATION','START_RECEPTION')
                   AND w.[StatusCode] <> 'RSV'                                   THEN 502
              WHEN a.Code IN ('EDIT_EXTRA','CANCEL_RECEPTION')
                   AND w.[StatusCode] <> 'RCP'                                   THEN 502
              WHEN s.WorkCode <> 0                                                THEN s.WorkCode
              WHEN a.Code = 'START_RECEPTION' AND w.[ReservationDate] <> @Today   THEN 503
              WHEN a.Code = 'START_RECEPTION' AND s.CutoffPassed = 1              THEN 304
              ELSE 0
          END) r
    WHERE w.[WorkId] = @WorkId
    ORDER BY a.Ord;
END
GO
