SET QUOTED_IDENTIFIER ON;   -- 01_Schema.sql 과 같은 설정으로 객체를 만든다 (CLAUDE.md §6)
GO
SET NOCOUNT ON;
GO
-- SELECT SP 공통 (스펙 §31)
--   XACT_ABORT · Transaction · applock 을 쓰지 않는다. 읽기 전용이다.
--   @서버시각 을 한 번만 캡처해 TVF 에 그대로 넘긴다 — TVF 안에서 SYSDATETIME() 을 부르지 않는다.
--   RS0 5컬럼에 명시적 CAST 를 건다. 조회 0건은 실패가 아니다 (05 §3.4).
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_공통업무상태_조회]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();
    DECLARE @오늘날짜 DATE = CONVERT(DATE, @서버시각);

    -- RS0
    SELECT
          CAST(1 AS BIT)                    AS [성공여부]
        , CAST(0 AS INT)                    AS [결과코드]
        , CAST(N'정상 처리되었습니다.' AS NVARCHAR(300)) AS [결과메시지]
        , CAST(NULL AS NVARCHAR(50))         AS [오류항목]
        , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];

    -- RS1
    --   운영시간내여부 를 TVF 의 업무가능코드 로 역산하지 않는다. 업무가능코드 는 오늘업무일=0 → 308 이
    --   운영시간내여부=0 → 309 보다 먼저 걸리므로 휴무일·일요일에는 시각과 무관하게 308 이 나온다.
    --   그러면 2026-12-25(금, 휴무일) 새벽 3시에 운영시간내여부=1 을 보고하게 된다. 직접 계산한다.
    SELECT
          [오늘날짜]         = CAST(@오늘날짜 AS DATE)
        , [요일명]       = CAST(CASE DATEDIFF(DAY, 0, @오늘날짜) % 7
                               WHEN 0 THEN N'월요일' WHEN 1 THEN N'화요일' WHEN 2 THEN N'수요일'
                               WHEN 3 THEN N'목요일' WHEN 4 THEN N'금요일' WHEN 5 THEN N'토요일'
                               ELSE N'일요일' END AS NVARCHAR(10))
        , [휴무일명]   = CAST(s.[휴무일명] AS NVARCHAR(100))
        , [운영시작시각]      = CAST('09:00:00' AS TIME(0))
        , [운영종료시각]     = CAST('18:00:00' AS TIME(0))
        , [업무일여부] = CAST(s.[업무일여부] AS BIT)
        , [운영시간내여부]   = CAST(CASE WHEN CONVERT(TIME(7), @서버시각) >= CONVERT(TIME(7), '09:00:00')
                                     AND CONVERT(TIME(7), @서버시각) <  CONVERT(TIME(7), '18:00:00')
                                    THEN 1 ELSE 0 END AS BIT)
        , [현재업무가능]    = CAST(s.[현재업무가능] AS BIT)
        , [차단코드]     = CAST(s.[업무가능코드] AS INT)
        , [차단메시지]  = CAST(s.[업무가능메시지] AS NVARCHAR(300))
    FROM [dbo].[UFN_HC_일정확인](@서버시각, @오늘날짜, 'AM', 'NONE') s;
END
GO
-- 이름은 접두검색만 한다 (04 §11.3). '%검색어%' 포함검색과 조건 없는 전체조회는 금지다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_수검자목록_조회]
    @차트번호      NVARCHAR(100),
    @성명         NVARCHAR(100),
    @주민번호 VARCHAR(13),
    @생년월일     VARCHAR(8),
    @휴대전화  VARCHAR(13)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();

    -- 1. 정규화 — 공백 제거, 빈 문자열은 NULL, 전화·주민번호의 '-' 제거
    SET @차트번호      = NULLIF(LTRIM(RTRIM(@차트번호)), N'');
    SET @성명         = NULLIF(LTRIM(RTRIM(@성명)), N'');
    -- [X] @성명 을 LIKE 에 그대로 이어 붙이면 접두검색이 아니다. '%' 한 글자면 조건이 '있는'
    --     것으로 103 가드를 통과하고 수검자 전건이 주민번호와 함께 반환된다 (실측).
    --     04 §11.3 과 이 파일 머리 주석이 '조건 없는 전체조회는 금지' 라고 못박은 그 상태다.
    --     메타문자 세 개를 대괄호로 이스케이프한다. '[' 를 먼저 바꿔야 뒤 치환이 낳는 괄호를 안 건드린다.
    --     변수로 올려 IX_수검자_NAME_BIRTHDAY seek 을 잃지 않게 한다.
    DECLARE @성명패턴 NVARCHAR(200) = CASE WHEN @성명 IS NULL THEN NULL ELSE REPLACE(REPLACE(REPLACE(@성명, N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%' END;
    SET @주민번호 = NULLIF(REPLACE(LTRIM(RTRIM(@주민번호)), '-', ''), '');
    SET @생년월일     = NULLIF(LTRIM(RTRIM(@생년월일)), '');
    SET @휴대전화  = NULLIF(REPLACE(LTRIM(RTRIM(@휴대전화)), '-', ''), '');

    -- 2. 형식 검증 (05 §5: 필수값 → 값 형식 순서. 이 SP 에 필수값은 없다)
    IF @주민번호 IS NOT NULL
       AND (LEN(@주민번호) <> 13 OR @주민번호 LIKE '%[^0-9]%')
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(101 AS INT)                  AS [결과코드]
            , CAST(N'입력값이 올바르지 않습니다.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(N'주민번호' AS NVARCHAR(50)) AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    IF @생년월일 IS NOT NULL
       AND (LEN(@생년월일) <> 8 OR @생년월일 LIKE '%[^0-9]%'
            OR TRY_CONVERT(DATE, @생년월일, 112) IS NULL)
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(101 AS INT)                  AS [결과코드]
            , CAST(N'입력값이 올바르지 않습니다.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(N'생년월일' AS NVARCHAR(50))   AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- 3. 조회조건 — 5개가 전부 NULL 이면 전체조회가 되므로 막는다
    IF @차트번호 IS NULL AND @성명 IS NULL AND @주민번호 IS NULL
       AND @생년월일 IS NULL AND @휴대전화 IS NULL
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(103 AS INT)                  AS [결과코드]
            , CAST(N'조회조건을 하나 이상 입력하십시오.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(NULL AS NVARCHAR(50))         AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- RS0
    SELECT
          CAST(1 AS BIT)                    AS [성공여부]
        , CAST(0 AS INT)                    AS [결과코드]
        , CAST(N'정상 처리되었습니다.' AS NVARCHAR(300)) AS [결과메시지]
        , CAST(NULL AS NVARCHAR(50))         AS [오류항목]
        , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];

    -- RS1 — 조회 0건은 실패가 아니다 (05 §3.4)
    SELECT
          [수검자ID]    = CAST(p.[수검자ID]    AS BIGINT)
        , [차트번호]      = CAST(p.[차트번호]      AS NVARCHAR(100))
        , [성명]         = CAST(p.[성명]         AS NVARCHAR(100))
        , [주민번호] = CAST(p.[주민번호] AS VARCHAR(13))
        , [생년월일]     = CAST(p.[생년월일]     AS VARCHAR(8))
        , [성별]       = CAST(p.[성별]       AS CHAR(1))
        , [휴대전화]  = CAST(p.[휴대전화]    AS VARCHAR(13))
        , [전화번호]        = CAST(p.[전화번호]    AS VARCHAR(13))
        , [이메일]        = CAST(p.[이메일]        AS VARCHAR(200))
        , [우편번호]      = CAST(p.[우편번호]      AS VARCHAR(10))
        , [주소]      = CAST(p.[주소]      AS NVARCHAR(200))
    FROM [dbo].[수검자] p
    WHERE (@차트번호      IS NULL OR p.[차트번호]      =  @차트번호)
      AND (@성명         IS NULL OR p.[성명]         LIKE @성명패턴       )
      AND (@주민번호 IS NULL OR p.[주민번호] =  @주민번호)
      AND (@생년월일     IS NULL OR p.[생년월일]     =  @생년월일)
      AND (@휴대전화  IS NULL OR REPLACE(p.[휴대전화], '-', '') = @휴대전화)
    ORDER BY p.[성명] ASC, p.[생년월일] ASC, p.[차트번호] ASC;
END
GO
-- 허용 결과코드 0 / 100 / 200 (05 §13). RS1 은 정확히 1행이다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_수검자상세_조회]
    @수검자ID BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();

    IF @수검자ID IS NULL
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(100 AS INT)                  AS [결과코드]
            , CAST(N'필수값을 입력하십시오.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(N'수검자ID' AS NVARCHAR(50))  AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [수검자ID] = @수검자ID)
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(200 AS INT)                  AS [결과코드]
            , CAST(N'수검자를 찾을 수 없습니다.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(N'수검자ID' AS NVARCHAR(50))  AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- RS0
    SELECT
          CAST(1 AS BIT)                    AS [성공여부]
        , CAST(0 AS INT)                    AS [결과코드]
        , CAST(N'정상 처리되었습니다.' AS NVARCHAR(300)) AS [결과메시지]
        , CAST(NULL AS NVARCHAR(50))         AS [오류항목]
        , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];

    -- RS1 (15컬럼, 정확히 1행)
    SELECT
          [수검자ID]     = CAST(p.[수검자ID]     AS BIGINT)
        , [차트번호]       = CAST(p.[차트번호]       AS NVARCHAR(100))
        , [성명]          = CAST(p.[성명]          AS NVARCHAR(100))
        , [주민번호]  = CAST(p.[주민번호]  AS VARCHAR(13))
        , [생년월일]      = CAST(p.[생년월일]      AS VARCHAR(8))
        , [성별]        = CAST(p.[성별]        AS CHAR(1))
        , [휴대전화]   = CAST(p.[휴대전화]     AS VARCHAR(13))
        , [전화번호]         = CAST(p.[전화번호]     AS VARCHAR(13))
        , [이메일]         = CAST(p.[이메일]         AS VARCHAR(200))
        , [우편번호]       = CAST(p.[우편번호]       AS VARCHAR(10))
        , [주소]       = CAST(p.[주소]       AS NVARCHAR(200))
        , [상세주소] = CAST(p.[상세주소] AS NVARCHAR(200))
        , [비고]          = CAST(p.[비고]          AS NVARCHAR(MAX))
        -- [X] 15컬럼이다. R3 이 05 §9.2 RS1 에 B형간염제외여부 를 넣었는데 SQL 이 따라오지 않았고,
        --     expected-contracts.json 도 14컬럼으로 같이 틀려 있어 게이트가 영원히 못 잡았다.
        --     DLG-PAT-01 수정 화면이 현재값을 못 받으면 @B형간염제외여부(NULL 불가)에
        --     체크박스 초기값 0 이 실려 제외 플래그가 조용히 1->0 이 된다 (05 §9.2 명문).
        , [B형간염제외여부] = CAST(p.[B형간염제외여부] AS BIT)
        , [최종수정일시]  = CAST(p.[최종수정일시]  AS DATETIME)
    FROM [dbo].[수검자] p
    WHERE p.[수검자ID] = @수검자ID;
END
GO
-- 허용 결과코드 0 / 100 / 200 / 701. RP-06 은 유효업무를 0~1건으로 제한한다.
-- 2건 이상은 조회로 고칠 수 없는 데이터 손상이므로 701 로 알린다 (00 RP-06, 05 §7.4).
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_수검자유효업무_조회]
    @수검자ID BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();
    DECLARE @오늘날짜 DATE = CONVERT(DATE, @서버시각);

    IF @수검자ID IS NULL
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(100 AS INT)                  AS [결과코드]
            , CAST(N'필수값을 입력하십시오.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(N'수검자ID' AS NVARCHAR(50))  AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [수검자ID] = @수검자ID)
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(200 AS INT)                  AS [결과코드]
            , CAST(N'수검자를 찾을 수 없습니다.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(N'수검자ID' AS NVARCHAR(50))  AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    DECLARE @건수 INT = (SELECT COUNT(*) FROM [dbo].[예약접수] w
                         WHERE w.[수검자ID] = @수검자ID
                           AND w.[예약일] >= @오늘날짜
                           AND w.[상태코드] IN ('RSV','RCP'));
    IF @건수 >= 2
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(701 AS INT)                  AS [결과코드]
            , CAST(N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(N'업무ID' AS NVARCHAR(50))     AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- RS0
    SELECT
          CAST(1 AS BIT)                    AS [성공여부]
        , CAST(0 AS INT)                    AS [결과코드]
        , CAST(N'정상 처리되었습니다.' AS NVARCHAR(300)) AS [결과메시지]
        , CAST(NULL AS NVARCHAR(50))         AS [오류항목]
        , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];

    -- RS1 (7컬럼, 0행 또는 1행)
    SELECT
          [업무ID]          = CAST(w.[업무ID] AS BIGINT)
        , [예약일] = CAST(w.[예약일] AS DATE)
        , [시간대코드]        = CAST(w.[시간대코드] AS CHAR(2))
        , [상태코드]          = CAST(w.[상태코드] AS CHAR(3))
        , [상태명]      = CAST(CASE w.[상태코드]
                                     WHEN 'RSV' THEN N'예약'
                                     WHEN 'RCP' THEN N'접수완료'
                                     WHEN 'CNR' THEN N'예약취소'
                                     ELSE N'접수취소' END AS NVARCHAR(10))
        , [오늘여부]         = CAST(CASE WHEN w.[예약일] = @오늘날짜 THEN 1 ELSE 0 END AS BIT)
        , [행버전]      = CAST(w.[행버전] AS BINARY(8))
    FROM [dbo].[예약접수] w
    WHERE w.[수검자ID] = @수검자ID
      AND w.[예약일] >= @오늘날짜
      AND w.[상태코드] IN ('RSV','RCP');
END
GO
-- 허용 결과코드 0 / 101 / 103 / 104 (05 §8.1).
-- 05 §8.1 은 "상태코드=NULL 은 조회조건으로 보지 않음" 이라고 NULL 을 한정했다.
-- 값이 있는 @상태코드 는 실질 조건으로 센다 — 상태만으로 전체 RSV 를 조회하는 것은 막지 않는다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_예약접수목록_조회]
    @시작일 DATE,
    @종료일   DATE,
    @상태코드   CHAR(3),
    @차트번호  NVARCHAR(100),
    @성명     NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();

    -- 1. 정규화
    SET @상태코드  = NULLIF(UPPER(LTRIM(RTRIM(@상태코드))), '');
    SET @차트번호 = NULLIF(LTRIM(RTRIM(@차트번호)), N'');
    SET @성명    = NULLIF(LTRIM(RTRIM(@성명)), N'');
    -- [X] @성명 을 LIKE 에 그대로 이어 붙이면 접두검색이 아니다. '%' 한 글자면 조건이 '있는'
    --     것으로 103 가드를 통과하고 수검자 전건이 주민번호와 함께 반환된다 (실측).
    --     04 §11.3 과 이 파일 머리 주석이 '조건 없는 전체조회는 금지' 라고 못박은 그 상태다.
    --     메타문자 세 개를 대괄호로 이스케이프한다. '[' 를 먼저 바꿔야 뒤 치환이 낳는 괄호를 안 건드린다.
    --     변수로 올려 IX_수검자_NAME_BIRTHDAY seek 을 잃지 않게 한다.
    DECLARE @성명패턴 NVARCHAR(200) = CASE WHEN @성명 IS NULL THEN NULL ELSE REPLACE(REPLACE(REPLACE(@성명, N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%' END;

    -- 2. 허용값
    IF @상태코드 IS NOT NULL AND @상태코드 NOT IN ('RSV','RCP','CNR','CNC')
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(101 AS INT)                  AS [결과코드]
            , CAST(N'입력값이 올바르지 않습니다.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(N'상태코드' AS NVARCHAR(50))     AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- 3. 날짜 범위
    IF @시작일 IS NOT NULL AND @종료일 IS NOT NULL AND @시작일 > @종료일
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(104 AS INT)                  AS [결과코드]
            , CAST(N'시작일은 종료일보다 늦을 수 없습니다.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(N'시작일' AS NVARCHAR(50))   AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- 4. 조회조건
    IF @시작일 IS NULL AND @종료일 IS NULL AND @상태코드 IS NULL
       AND @차트번호 IS NULL AND @성명 IS NULL
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(103 AS INT)                  AS [결과코드]
            , CAST(N'조회조건을 하나 이상 입력하십시오.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(NULL AS NVARCHAR(50))         AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- RS0
    SELECT
          CAST(1 AS BIT)                    AS [성공여부]
        , CAST(0 AS INT)                    AS [결과코드]
        , CAST(N'정상 처리되었습니다.' AS NVARCHAR(300)) AS [결과메시지]
        , CAST(NULL AS NVARCHAR(50))         AS [오류항목]
        , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];

    -- RS1 (11컬럼, 05 §8.1)
    SELECT
          [업무ID]          = CAST(w.[업무ID] AS BIGINT)
        , [수검자ID]       = CAST(w.[수검자ID] AS BIGINT)
        , [예약일] = CAST(w.[예약일] AS DATE)
        , [시간대코드]        = CAST(w.[시간대코드] AS CHAR(2))
        , [상태코드]          = CAST(w.[상태코드] AS CHAR(3))
        , [상태명]      = CAST(CASE w.[상태코드]
                                     WHEN 'RSV' THEN N'예약'
                                     WHEN 'RCP' THEN N'접수완료'
                                     WHEN 'CNR' THEN N'예약취소'
                                     ELSE N'접수취소' END AS NVARCHAR(10))
        , [성명]            = CAST(p.[성명] AS NVARCHAR(100))
        , [차트번호]         = CAST(p.[차트번호] AS NVARCHAR(100))
        , [성별]          = CAST(p.[성별] AS CHAR(1))
        , [생년월일]        = CAST(p.[생년월일] AS VARCHAR(8))
        , [휴대전화]     = CAST(p.[휴대전화] AS VARCHAR(13))
    FROM [dbo].[예약접수] w
    JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
    WHERE (@시작일 IS NULL OR w.[예약일] >= @시작일)
      AND (@종료일   IS NULL OR w.[예약일] <= @종료일)
      AND (@상태코드   IS NULL OR w.[상태코드]      =  @상태코드)
      AND (@차트번호  IS NULL OR p.[차트번호]         =  @차트번호)
      AND (@성명     IS NULL OR p.[성명]            LIKE @성명패턴       )
    ORDER BY w.[예약일] ASC, w.[시간대코드] ASC, p.[성명] ASC, w.[업무ID] ASC;
END
GO
-- 허용 결과코드 0 / 100 / 500 / 701 (05 §8.2). RS0~RS4 다섯 개를 반환한다.
-- RS4 는 고정 5행이다. 업무시간 밖이라고 실패시키지 않는다 — 허용여부=0 으로 반환한다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_예약접수상세_조회]
    @업무ID BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();
    DECLARE @오늘날짜 DATE = CONVERT(DATE, @서버시각);

    IF @업무ID IS NULL
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(100 AS INT)                  AS [결과코드]
            , CAST(N'필수값을 입력하십시오.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(N'업무ID' AS NVARCHAR(50))     AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[예약접수] WHERE [업무ID] = @업무ID)
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(500 AS INT)                  AS [결과코드]
            , CAST(N'예약·접수 업무를 찾을 수 없습니다.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(N'업무ID' AS NVARCHAR(50))     AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- 저장 NEX 가 비면 검사구성 손상이다 (CORRUPT-2 가 이것을 만든다).
    -- 빈 문자열이 손상의 유일한 표현이다 (plans/10 §1).
    IF EXISTS (SELECT 1 FROM [dbo].[예약접수] w
                WHERE w.[업무ID] = @업무ID AND LEN(ISNULL(w.[국가검사항목], N'')) = 0)
    BEGIN
        SELECT
              CAST(0 AS BIT)                    AS [성공여부]
            , CAST(701 AS INT)                  AS [결과코드]
            , CAST(N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.' AS NVARCHAR(300)) AS [결과메시지]
            , CAST(N'업무ID' AS NVARCHAR(50))     AS [오류항목]
            , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- RS0
    SELECT
          CAST(1 AS BIT)                    AS [성공여부]
        , CAST(0 AS INT)                    AS [결과코드]
        , CAST(N'정상 처리되었습니다.' AS NVARCHAR(300)) AS [결과메시지]
        , CAST(NULL AS NVARCHAR(50))         AS [오류항목]
        , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];

    -- RS1 업무상세 (15컬럼)
    SELECT
          [업무ID]          = CAST(w.[업무ID] AS BIGINT)
        , [수검자ID]       = CAST(w.[수검자ID] AS BIGINT)
        , [차트번호]         = CAST(p.[차트번호] AS NVARCHAR(100))
        , [성명]            = CAST(p.[성명] AS NVARCHAR(100))
        , [생년월일]        = CAST(p.[생년월일] AS VARCHAR(8))
        , [성별]          = CAST(p.[성별] AS CHAR(1))
        , [휴대전화]     = CAST(p.[휴대전화] AS VARCHAR(13))
        , [예약일] = CAST(w.[예약일] AS DATE)
        , [시간대코드]        = CAST(w.[시간대코드] AS CHAR(2))
        , [상태코드]          = CAST(w.[상태코드] AS CHAR(3))
        , [상태명]      = CAST(CASE w.[상태코드]
                                     WHEN 'RSV' THEN N'예약'
                                     WHEN 'RCP' THEN N'접수완료'
                                     WHEN 'CNR' THEN N'예약취소'
                                     ELSE N'접수취소' END AS NVARCHAR(10))
        , [정원]        = CAST(20 AS INT)
        , [현재인원]    = CAST(c.[건수] AS INT)
        , [잔여자리]       = CAST(CASE WHEN 20 - c.[건수] < 0 THEN 0 ELSE 20 - c.[건수] END AS INT)
        , [행버전]      = CAST(w.[행버전] AS BINARY(8))
    FROM [dbo].[예약접수] w
    JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
    CROSS APPLY (SELECT [건수] = COUNT(*) FROM [dbo].[예약접수] x
                  WHERE x.[예약일] = w.[예약일]
                    AND x.[시간대코드]    = w.[시간대코드]
                    AND x.[상태코드] IN ('RSV','RCP')) c
    WHERE w.[업무ID] = @업무ID;

    -- RS2 국가검사항목 — 실제 저장된 NEX 만
    SELECT
          [검사항목코드] = CAST(m.[검사항목코드] AS VARCHAR(10))
        , [검사항목명] = CAST(m.[검사항목명] AS NVARCHAR(100))
        , [국가검사구분] = CAST(CASE WHEN m.[국가검사규칙코드] = 'NEX-01' THEN 'BASIC' ELSE 'CONDITIONAL' END AS VARCHAR(12))
        , [국가검사규칙코드] = CAST(m.[국가검사규칙코드] AS VARCHAR(10))
    FROM [dbo].[예약접수] w
    JOIN [dbo].[검사코드] m
      ON N',' + ISNULL(w.[국가검사항목], N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
    WHERE w.[업무ID] = @업무ID
    ORDER BY m.[검사항목코드] ASC;

    -- RS3 추가검사항목 — 실제 저장된 AEX 만
    SELECT
          [추가검사코드] = CAST(m.[추가검사코드] AS VARCHAR(10))
        , [검사항목코드]   = CAST(m.[검사항목코드] AS VARCHAR(10))
        , [검사항목명]   = CAST(m.[검사항목명] AS NVARCHAR(100))
    FROM [dbo].[예약접수] w
    JOIN [dbo].[검사코드] m
      ON N',' + ISNULL(w.[추가검사항목], N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
    WHERE w.[업무ID] = @업무ID
    ORDER BY m.[추가검사코드] ASC;

    -- RS4 가능한업무 — 정확히 5행. 순서는 05 §8.2 의 고정 목록이므로 정렬순서 로 강제한다.
    --   상관 인자를 받는 TVF 는 CROSS APPLY 여야 한다. CROSS JOIN 으로 쓰면
    --   같은 FROM 절 다른 테이블의 컬럼을 인자로 못 받아 Msg 4104 가 난다.
    SELECT
          [업무동작코드]    = CAST(a.[업무동작코드] AS VARCHAR(30))
        , [허용여부]       = CAST(CASE WHEN r.[산출사유코드] = 0 THEN 1 ELSE 0 END AS BIT)
        , [사유코드]    = CAST(r.[산출사유코드] AS INT)
        , [사유메시지] = CAST(CASE r.[산출사유코드]
                                   WHEN 304 THEN N'해당 시간대의 마감시간이 지났습니다.'
                                   WHEN 308 THEN N'오늘은 업무일이 아닙니다.'
                                   WHEN 309 THEN N'현재는 업무 운영시간이 아닙니다.'
                                   WHEN 502 THEN N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.'
                                   WHEN 503 THEN N'예약일이 오늘인 업무만 접수할 수 있습니다.'
                                   ELSE N'' END AS NVARCHAR(300))
    FROM [dbo].[예약접수] w
    CROSS JOIN (VALUES (1,'EDIT_RESERVATION'),(2,'CANCEL_RESERVATION'),(3,'START_RECEPTION'),
                       (4,'EDIT_EXTRA'),(5,'CANCEL_RECEPTION')) a([정렬순서], [업무동작코드])
    CROSS APPLY [dbo].[UFN_HC_일정확인](@서버시각, w.[예약일], w.[시간대코드], 'RECEPTION') s
    CROSS APPLY (SELECT [산출사유코드] =
          CASE
              WHEN a.[업무동작코드] IN ('EDIT_RESERVATION','CANCEL_RESERVATION','START_RECEPTION')
                   AND w.[상태코드] <> 'RSV'                                   THEN 502
              WHEN a.[업무동작코드] IN ('EDIT_EXTRA','CANCEL_RECEPTION')
                   AND w.[상태코드] <> 'RCP'                                   THEN 502
              WHEN s.[업무가능코드] <> 0                                                THEN s.[업무가능코드]
              WHEN a.[업무동작코드] = 'START_RECEPTION' AND w.[예약일] <> @오늘날짜   THEN 503
              WHEN a.[업무동작코드] = 'START_RECEPTION' AND s.[마감경과여부] = 1              THEN 304
              ELSE 0
          END) r
    WHERE w.[업무ID] = @업무ID
    ORDER BY a.[정렬순서];
END
GO
-- 허용 결과코드 0 / 100~102 / 200 / 500~502 / 601 / 700~701 (05 §9).
-- 휴무일·정원마감·TGT 비대상은 SP 실패가 아니다 — RS0 성공여부=1 결과코드=0 + RS1 저장가능=0 + 차단코드 다 (05 §3.3).
-- 변경범위=NONE 에 결과코드=1 을 쓰지 않는다. 결과코드=1 은 Write SP No-op 전용이다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_예약가능정보_조회]
    @수검자ID        BIGINT,
    @업무ID           BIGINT,
    @행버전       BINARY(8),
    @예약구분  VARCHAR(10),
    @예약일  DATE,
    @시간대코드         CHAR(2),
    @추가검사01선택여부 BIT,
    @추가검사02선택여부 BIT,
    @추가검사03선택여부 BIT,
    @추가검사04선택여부 BIT,
    @추가검사05선택여부 BIT,
    @추가검사06선택여부 BIT,
    @추가검사07선택여부 BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();
    DECLARE @오늘날짜 DATE = CONVERT(DATE, @서버시각);

    DECLARE @변경범위        VARCHAR(12);
    DECLARE @예약일변경여부  BIT = NULL, @시간대변경여부 BIT = NULL, @추가검사변경여부 BIT = NULL;
    DECLARE @업무예약일 DATE, @업무시간대코드 CHAR(2), @업무상태코드 CHAR(3), @업무수검자ID BIGINT;
    DECLARE @다른업무ID  BIGINT = NULL;
    DECLARE @요청목록 TABLE ([추가검사코드] VARCHAR(10) PRIMARY KEY);

    -- 1. 문자열 정규화 (05 §2.2)
    -- [X] 16개 SP 중 이 하나만 정규화가 없었다. 그래서 ' NORMAL'(앞 공백)이 여기서는 101 인데
    --     같은 값을 USP_HC_예약_등록 은 정규화해 성공시켰다 - 사전조회와 저장의 판정이 갈렸다.
    SET @예약구분 = NULLIF(UPPER(LTRIM(RTRIM(@예약구분))), '');
    SET @시간대코드        = NULLIF(UPPER(LTRIM(RTRIM(@시간대코드))), '');

    -- 2. 필수값 (05 §5: 필수값 → 값 형식·허용값 순서)
    --    예약구분·예약일 가 빠지면 NOT IN 이 UNKNOWN 이라 101 도 안 나고
    --    NULL 이 변경범위 계산까지 흘러들어간다. 여기서 막는다.
    IF @수검자ID IS NULL OR @예약구분 IS NULL OR @예약일 IS NULL
       OR @추가검사01선택여부 IS NULL OR @추가검사02선택여부 IS NULL OR @추가검사03선택여부 IS NULL
       OR @추가검사04선택여부 IS NULL OR @추가검사05선택여부 IS NULL OR @추가검사06선택여부 IS NULL
       OR @추가검사07선택여부 IS NULL
       OR (@업무ID IS NOT NULL AND @행버전 IS NULL)
    BEGIN
    -- [X] 오류항목 식을 SELECT 안에 인라인으로 두면 RS0 블록이 길어져 V17(5컬럼 명시 CAST)의
    --     검사 창 밖으로 서버시각 이 밀린다. 변수로 올려 RS0 블록을 5줄로 유지한다.
    DECLARE @오류항목100 NVARCHAR(50) =
        CASE WHEN @수검자ID        IS NULL THEN N'수검자ID'
             WHEN @예약구분  IS NULL THEN N'예약구분'
             WHEN @예약일  IS NULL THEN N'예약일'
             WHEN @업무ID IS NOT NULL AND @행버전 IS NULL THEN N'행버전'
             WHEN @추가검사01선택여부 IS NULL THEN N'추가검사01선택여부'
             WHEN @추가검사02선택여부 IS NULL THEN N'추가검사02선택여부'
             WHEN @추가검사03선택여부 IS NULL THEN N'추가검사03선택여부'
             WHEN @추가검사04선택여부 IS NULL THEN N'추가검사04선택여부'
             WHEN @추가검사05선택여부 IS NULL THEN N'추가검사05선택여부'
             WHEN @추가검사06선택여부 IS NULL THEN N'추가검사06선택여부'
             WHEN @추가검사07선택여부 IS NULL THEN N'추가검사07선택여부'
             ELSE N'행버전' END;
        SELECT CAST(0 AS BIT) AS [성공여부], CAST(100 AS INT) AS [결과코드]
             , CAST(N'필수값을 입력하십시오.' AS NVARCHAR(300)) AS [결과메시지]
             , CAST(@오류항목100 AS NVARCHAR(50)) AS [오류항목]
             , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- 3. 허용값
    -- [X] @시간대코드 허용값 검증이 없었다. 'XX' 를 넣으면 @선택차단코드 이 NULL 이 되고
    --     NULL <> 0 이 UNKNOWN 이라 우선순위 CASE 를 그대로 통과해
    --     RS0 성공여부=1 결과코드=0 · RS1 저장가능=0 차단코드=0 차단메시지='' 가 나갔다.
    --     화면은 저장 버튼이 죽어 있는데 사유를 표시할 수 없다. 101 로 잘라낸다 (05 §5 2단계).
    IF @시간대코드 IS NOT NULL AND @시간대코드 NOT IN ('AM','PM')
    BEGIN
        SELECT CAST(0 AS BIT) AS [성공여부], CAST(101 AS INT) AS [결과코드]
             , CAST(N'입력값이 올바르지 않습니다.' AS NVARCHAR(300)) AS [결과메시지]
             , CAST(N'시간대코드' AS NVARCHAR(50)) AS [오류항목]
             , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    IF @예약구분 NOT IN ('NORMAL','WALKIN')
    BEGIN
        SELECT CAST(0 AS BIT) AS [성공여부], CAST(101 AS INT) AS [결과코드]
             , CAST(N'입력값이 올바르지 않습니다.' AS NVARCHAR(300)) AS [결과메시지]
             , CAST(N'예약구분' AS NVARCHAR(50)) AS [오류항목]
             , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- 4. Parameter 조합 (05 §9.3). 나머지 한 종("기존 날짜 유지 + 시간대코드 NULL")은
    --    Work 행이 있어야 판정할 수 있으므로 6단계 뒤로 미룬다.
    IF (@업무ID IS NULL AND @행버전 IS NOT NULL)
       OR (@업무ID IS NOT NULL AND @예약구분 = 'WALKIN')
       OR (@예약구분 = 'WALKIN' AND @예약일 <> @오늘날짜)
    BEGIN
        SELECT CAST(0 AS BIT) AS [성공여부], CAST(102 AS INT) AS [결과코드]
             , CAST(N'함께 사용할 수 없는 입력값 조합입니다.' AS NVARCHAR(300)) AS [결과메시지]
             , CAST(CASE WHEN @업무ID IS NULL AND @행버전 IS NOT NULL THEN N'행버전'
                         WHEN @업무ID IS NOT NULL THEN N'예약구분'
                         ELSE N'예약일' END AS NVARCHAR(50)) AS [오류항목]
             , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- 5. Patient 존재
    IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [수검자ID] = @수검자ID)
    BEGIN
        SELECT CAST(0 AS BIT) AS [성공여부], CAST(200 AS INT) AS [결과코드]
             , CAST(N'수검자를 찾을 수 없습니다.' AS NVARCHAR(300)) AS [결과메시지]
             , CAST(N'수검자ID' AS NVARCHAR(50)) AS [오류항목]
             , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- 6. Work 존재 → 소유 → 상태 → 동시성
    IF @업무ID IS NOT NULL
    BEGIN
        SELECT @업무수검자ID = w.[수검자ID], @업무예약일 = w.[예약일]
             , @업무시간대코드 = w.[시간대코드], @업무상태코드 = w.[상태코드]
        FROM [dbo].[예약접수] w WHERE w.[업무ID] = @업무ID;

        IF @업무수검자ID IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS [성공여부], CAST(500 AS INT) AS [결과코드]
                 , CAST(N'예약·접수 업무를 찾을 수 없습니다.' AS NVARCHAR(300)) AS [결과메시지]
                 , CAST(N'업무ID' AS NVARCHAR(50)) AS [오류항목]
                 , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
            RETURN;
        END
        IF @업무수검자ID <> @수검자ID
        BEGIN
            SELECT CAST(0 AS BIT) AS [성공여부], CAST(501 AS INT) AS [결과코드]
                 , CAST(N'요청한 수검자와 예약·접수 업무의 수검자가 다릅니다.' AS NVARCHAR(300)) AS [결과메시지]
                 , CAST(N'수검자ID' AS NVARCHAR(50)) AS [오류항목]
                 , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
            RETURN;
        END
        IF @업무상태코드 <> 'RSV'
        BEGIN
            SELECT CAST(0 AS BIT) AS [성공여부], CAST(502 AS INT) AS [결과코드]
                 , CAST(N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.' AS NVARCHAR(300)) AS [결과메시지]
                 , CAST(N'업무ID' AS NVARCHAR(50)) AS [오류항목]
                 , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
            RETURN;
        END
        IF NOT EXISTS (SELECT 1 FROM [dbo].[예약접수]
                        WHERE [업무ID] = @업무ID AND [행버전] = @행버전)
        BEGIN
            SELECT CAST(0 AS BIT) AS [성공여부], CAST(601 AS INT) AS [결과코드]
                 , CAST(N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.' AS NVARCHAR(300)) AS [결과메시지]
                 , CAST(N'행버전' AS NVARCHAR(50)) AS [오류항목]
                 , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
            RETURN;
        END
        IF @예약일 = @업무예약일 AND @시간대코드 IS NULL
        BEGIN
            SELECT CAST(0 AS BIT) AS [성공여부], CAST(102 AS INT) AS [결과코드]
                 , CAST(N'함께 사용할 수 없는 입력값 조합입니다.' AS NVARCHAR(300)) AS [결과메시지]
                 , CAST(N'시간대코드' AS NVARCHAR(50)) AS [오류항목]
                 , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
            RETURN;
        END
    END

    -- 7. 변경범위 계산 (05 §9.4). 추가검사변경여부 는 §28.1 대로 EXCEPT 양방향이다.
    INSERT INTO @요청목록 ([추가검사코드])
    SELECT v.c FROM (VALUES ('OPT01',@추가검사01선택여부),('OPT02',@추가검사02선택여부),('OPT03',@추가검사03선택여부),
                            ('OPT04',@추가검사04선택여부),('OPT05',@추가검사05선택여부),('OPT06',@추가검사06선택여부),
                            ('OPT07',@추가검사07선택여부)) v(c, b)
     WHERE v.b = 1;

    IF @업무ID IS NULL
        SET @변경범위 = 'ALL';
    ELSE
    BEGIN
        SET @예약일변경여부 = CASE WHEN @예약일 <> @업무예약일 THEN 1 ELSE 0 END;
        SET @시간대변경여부 = CASE WHEN @시간대코드 IS NOT NULL AND @시간대코드 <> @업무시간대코드 THEN 1 ELSE 0 END;
        SET @추가검사변경여부 = CASE WHEN EXISTS (
                SELECT [추가검사코드] FROM @요청목록
                EXCEPT
                SELECT m.[추가검사코드] FROM [dbo].[예약접수] w
                  JOIN [dbo].[검사코드] m
                    ON N',' + ISNULL(w.[추가검사항목], N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
                 WHERE w.[업무ID] = @업무ID)
            OR EXISTS (
                SELECT m.[추가검사코드] FROM [dbo].[예약접수] w
                  JOIN [dbo].[검사코드] m
                    ON N',' + ISNULL(w.[추가검사항목], N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
                 WHERE w.[업무ID] = @업무ID
                EXCEPT
                SELECT [추가검사코드] FROM @요청목록)
            THEN 1 ELSE 0 END;

        SET @변경범위 = CASE WHEN @예약일변경여부 = 1                              THEN 'ALL'
                          WHEN @시간대변경여부 = 1 AND @추가검사변경여부 = 1        THEN 'SLOT_EXTRA'
                          WHEN @시간대변경여부 = 1                             THEN 'SLOT'
                          WHEN @추가검사변경여부 = 1                            THEN 'EXTRA'
                          ELSE 'NONE' END;
    END

    -- 9/10. 검사 Master 무결성
    IF @변경범위 IN ('ALL','EXTRA','SLOT_EXTRA')
       AND ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [국가검사규칙코드] IS NOT NULL) < 8
            OR (SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [추가검사코드] IS NOT NULL) <> 7)
    BEGIN
        SELECT CAST(0 AS BIT) AS [성공여부], CAST(700 AS INT) AS [결과코드]
             , CAST(N'검사 Master 구성이 올바르지 않습니다.' AS NVARCHAR(300)) AS [결과메시지]
             , CAST(NULL AS NVARCHAR(50)) AS [오류항목]
             , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END
    IF @변경범위 IN ('EXTRA','SLOT_EXTRA')
       AND EXISTS (SELECT 1 FROM [dbo].[예약접수]
                    WHERE [업무ID] = @업무ID AND LEN(ISNULL([국가검사항목], N'')) = 0)
    BEGIN
        SELECT CAST(0 AS BIT) AS [성공여부], CAST(701 AS INT) AS [결과코드]
             , CAST(N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.' AS NVARCHAR(300)) AS [결과메시지]
             , CAST(N'업무ID' AS NVARCHAR(50)) AS [오류항목]
             , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
        RETURN;
    END

    -- 11. 현재 공통 업무 가능 여부
    DECLARE @현재업무가능 BIT, @업무가능코드 INT;
    SELECT @현재업무가능 = s.[현재업무가능], @업무가능코드 = s.[업무가능코드]
      FROM [dbo].[UFN_HC_일정확인](@서버시각, @오늘날짜, 'AM', 'NONE') s;

    -- 12. 다른 유효업무
    IF @변경범위 IN ('ALL','SLOT','SLOT_EXTRA')
    BEGIN
        DECLARE @다른업무건수 INT = (SELECT COUNT(*) FROM [dbo].[예약접수] w
                                  WHERE w.[수검자ID] = @수검자ID
                                    AND w.[예약일] >= @오늘날짜
                                    AND w.[상태코드] IN ('RSV','RCP')
                                    AND (@업무ID IS NULL OR w.[업무ID] <> @업무ID));
        IF @다른업무건수 >= 2
        BEGIN
            SELECT CAST(0 AS BIT) AS [성공여부], CAST(701 AS INT) AS [결과코드]
                 , CAST(N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.' AS NVARCHAR(300)) AS [결과메시지]
                 , CAST(N'업무ID' AS NVARCHAR(50)) AS [오류항목]
                 , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
            RETURN;
        END
        SELECT TOP (1) @다른업무ID = w.[업무ID] FROM [dbo].[예약접수] w
         WHERE w.[수검자ID] = @수검자ID
           AND w.[예약일] >= @오늘날짜
           AND w.[상태코드] IN ('RSV','RCP')
           AND (@업무ID IS NULL OR w.[업무ID] <> @업무ID)
         ORDER BY w.[업무ID];
    END

    -- 요청일 자체의 판정(300/301/302). 슬롯·마감과 무관한 날짜 수준 차단이다.
    DECLARE @일정차단코드 INT = (SELECT [사유코드] FROM
        [dbo].[UFN_HC_일정확인](@서버시각, @예약일, 'AM', 'NONE'));
    DECLARE @일정통과여부 BIT = CASE WHEN @일정차단코드 = 0 THEN 1 ELSE 0 END;
    DECLARE @마감구분 VARCHAR(10) = CASE WHEN @예약구분 = 'WALKIN' THEN 'RECEPTION' ELSE 'NORMAL' END;

    -- AM/PM 두 행. 05 §9.7 의 세 경우를 한 식으로 만족한다.
    DECLARE @시간대목록 TABLE (
        [시간대코드] CHAR(2) PRIMARY KEY, [시간대명] NVARCHAR(10), [정원] INT,
        [현재인원] INT, [적용후인원] INT, [잔여자리] INT, [운영여부] BIT,
        [마감시각] TIME(0), [마감경과여부] BIT, [선택가능] BIT, [차단코드] INT, [차단메시지] NVARCHAR(300));

    INSERT INTO @시간대목록
    SELECT
          v.[시간대코드]
        , CASE v.[시간대코드] WHEN 'AM' THEN N'오전' ELSE N'오후' END
        , 20
        , c.[건수]
        , a.[적용후]
        , CASE WHEN 20 - a.[적용후] < 0 THEN 0 ELSE 20 - a.[적용후] END
        , s.[운영여부]
        , s.[마감시각]
        , s.[마감경과여부]
        , CASE WHEN s.[사유코드] = 0 AND a.[적용후] <= 20 THEN 1 ELSE 0 END
        , CASE WHEN s.[사유코드] <> 0 THEN s.[사유코드]
               WHEN a.[적용후] > 20 THEN 305 ELSE 0 END
        , CASE WHEN s.[사유코드] <> 0 THEN s.[사유메시지]
               WHEN a.[적용후] > 20 THEN N'해당 시간대의 예약 정원이 마감되었습니다.'
               ELSE N'' END
    FROM (VALUES ('AM'),('PM')) v([시간대코드])
    CROSS APPLY [dbo].[UFN_HC_일정확인](@서버시각, @예약일, v.[시간대코드], @마감구분) s
    CROSS APPLY (SELECT [건수] = COUNT(*) FROM [dbo].[예약접수] x
                  WHERE x.[예약일] = @예약일
                    AND x.[시간대코드] = v.[시간대코드]
                    AND x.[상태코드] IN ('RSV','RCP')) c
    CROSS APPLY (SELECT [적용후] = c.[건수]
                   - CASE WHEN @업무ID IS NOT NULL
                           AND EXISTS (SELECT 1 FROM [dbo].[예약접수] y
                                        WHERE y.[업무ID] = @업무ID
                                          AND y.[예약일] = @예약일
                                          AND y.[시간대코드] = v.[시간대코드]
                                          AND y.[상태코드] IN ('RSV','RCP'))
                          THEN 1 ELSE 0 END + 1) a;

    -- TGT / NEX / AEX
    DECLARE @검진대상여부 BIT = NULL, @검진대상사유코드 INT = NULL;
    IF @변경범위 = 'ALL' AND @일정통과여부 = 1
        SELECT @검진대상여부 = g.[검진대상여부], @검진대상사유코드 = g.[사유코드]
          FROM [dbo].[UFN_HC_검진대상확인](@수검자ID, @예약일) g;

    DECLARE @국가검사건수 INT = 0;
    IF @변경범위 = 'ALL' AND @일정통과여부 = 1 AND @검진대상여부 = 1
        SET @국가검사건수 = (SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@수검자ID, @예약일));

    DECLARE @추가검사평가여부 BIT = CASE WHEN @변경범위 IN ('EXTRA','SLOT_EXTRA')
                                OR (@변경범위 = 'ALL' AND @일정통과여부 = 1) THEN 1 ELSE 0 END;
    DECLARE @저장검사사용 BIT = CASE WHEN @변경범위 IN ('EXTRA','SLOT_EXTRA') THEN 1 ELSE 0 END;

    DECLARE @추가검사 TABLE ([추가검사코드] VARCHAR(10) PRIMARY KEY, [검사항목코드] VARCHAR(10), [검사항목명] NVARCHAR(100),
                        [요청선택여부] BIT, [유효선택여부] BIT, [선택가능] BIT, [사유코드] INT, [사유메시지] NVARCHAR(300));
    IF @추가검사평가여부 = 1
        INSERT INTO @추가검사
        SELECT x.[추가검사코드], x.[검사항목코드], x.[검사항목명], x.[요청선택여부], x.[유효선택여부], x.[선택가능], x.[사유코드], x.[사유메시지]
          FROM [dbo].[UFN_HC_추가검사확인](@수검자ID, @예약일, @업무ID, @저장검사사용,
                 @추가검사01선택여부, @추가검사02선택여부, @추가검사03선택여부, @추가검사04선택여부,
                 @추가검사05선택여부, @추가검사06선택여부, @추가검사07선택여부) x;

    DECLARE @무효추가검사건수 INT = (SELECT COUNT(*) FROM @추가검사 WHERE [요청선택여부] = 1 AND [선택가능] = 0);
    DECLARE @무효추가검사사유코드 INT = (SELECT TOP (1) [사유코드] FROM @추가검사
                                WHERE [요청선택여부] = 1 AND [선택가능] = 0 ORDER BY [추가검사코드]);

    -- RS1 의 대표 차단코드 (05 §9.6 우선순위)
    DECLARE @선택차단코드 INT = (SELECT [차단코드] FROM @시간대목록 WHERE [시간대코드] = @시간대코드);
    DECLARE @선택가능시간대존재  BIT = CASE WHEN EXISTS (SELECT 1 FROM @시간대목록 WHERE [선택가능] = 1) THEN 1 ELSE 0 END;
    DECLARE @차단코드 INT =
        CASE WHEN @변경범위 = 'NONE'                                    THEN 0
             WHEN @현재업무가능 = 0                                    THEN @업무가능코드
             WHEN @다른업무ID IS NOT NULL                           THEN 306
             WHEN @시간대코드 IS NOT NULL AND @선택차단코드 <> 0           THEN @선택차단코드
             WHEN @시간대코드 IS NULL AND @선택가능시간대존재 = 0                 THEN 307
             WHEN @변경범위 = 'ALL' AND @일정통과여부 = 1
              AND ISNULL(@검진대상여부, 1) = 0                           THEN @검진대상사유코드
             WHEN @무효추가검사건수 > 0                                        THEN @무효추가검사사유코드
             ELSE 0 END;

    DECLARE @저장가능 BIT =
        CASE @변경범위
            WHEN 'NONE' THEN 0
            WHEN 'ALL'  THEN CASE WHEN @현재업무가능 = 1 AND @다른업무ID IS NULL
                                   AND @시간대코드 IS NOT NULL
                                   AND (SELECT [선택가능] FROM @시간대목록 WHERE [시간대코드] = @시간대코드) = 1
                                   AND @검진대상여부 = 1 AND @국가검사건수 >= 8 AND @무효추가검사건수 = 0
                              THEN 1 ELSE 0 END
            WHEN 'SLOT' THEN CASE WHEN @현재업무가능 = 1 AND @다른업무ID IS NULL
                                   AND @시간대코드 IS NOT NULL
                                   AND (SELECT [선택가능] FROM @시간대목록 WHERE [시간대코드] = @시간대코드) = 1
                              THEN 1 ELSE 0 END
            WHEN 'EXTRA' THEN CASE WHEN @현재업무가능 = 1 AND @무효추가검사건수 = 0 AND @추가검사변경여부 = 1
                              THEN 1 ELSE 0 END
            ELSE CASE WHEN @현재업무가능 = 1 AND @다른업무ID IS NULL
                       AND @시간대코드 IS NOT NULL
                       AND (SELECT [선택가능] FROM @시간대목록 WHERE [시간대코드] = @시간대코드) = 1
                       AND @무효추가검사건수 = 0
                  THEN 1 ELSE 0 END
        END;

    -- RS0
    SELECT CAST(1 AS BIT) AS [성공여부], CAST(0 AS INT) AS [결과코드]
         , CAST(N'정상 처리되었습니다.' AS NVARCHAR(300)) AS [결과메시지]
         , CAST(NULL AS NVARCHAR(50)) AS [오류항목]
         , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];

    -- RS1 예약요약 (14컬럼, 정확히 1행)
    SELECT
          [변경범위]           = CAST(@변경범위 AS VARCHAR(12))
        , [수검자ID]       = CAST(@수검자ID AS BIGINT)
        , [업무ID]          = CAST(@업무ID AS BIGINT)
        , [예약구분] = CAST(@예약구분 AS VARCHAR(10))
        , [예약일] = CAST(@예약일 AS DATE)
        , [시간대코드]        = CAST(@시간대코드 AS CHAR(2))
        , [예약일변경여부]     = CAST(@예약일변경여부 AS BIT)
        , [시간대변경여부]     = CAST(@시간대변경여부 AS BIT)
        , [추가검사변경여부]    = CAST(@추가검사변경여부 AS BIT)
        , [현재업무가능]      = CAST(@현재업무가능 AS BIT)
        , [다른업무ID]     = CAST(@다른업무ID AS BIGINT)
        , [저장가능]         = CAST(@저장가능 AS BIT)
        , [차단코드]       = CAST(@차단코드 AS INT)
        , [차단메시지]    = CAST(CASE @차단코드
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
          [시간대코드]     = CAST(s.[시간대코드] AS CHAR(2))
        , [시간대명]     = CAST(s.[시간대명] AS NVARCHAR(10))
        , [정원]     = CAST(s.[정원] AS INT)
        , [현재인원] = CAST(s.[현재인원] AS INT)
        , [적용후인원]   = CAST(s.[적용후인원] AS INT)
        , [잔여자리]    = CAST(s.[잔여자리] AS INT)
        , [운영여부]       = CAST(s.[운영여부] AS BIT)
        , [마감시각]   = CAST(s.[마감시각] AS TIME(0))
        , [마감경과여부] = CAST(s.[마감경과여부] AS BIT)
        , [선택가능]    = CAST(s.[선택가능] AS BIT)
        , [차단코드]    = CAST(s.[차단코드] AS INT)
        , [차단메시지] = CAST(s.[차단메시지] AS NVARCHAR(300))
    FROM @시간대목록 s
    WHERE @변경범위 IN ('ALL','SLOT','SLOT_EXTRA')
    ORDER BY s.[시간대코드] ASC;

    -- RS3 검진대상 — ALL 에서 일정 평가가 가능할 때만 1행
    SELECT
          [검진대상여부]        = CAST(g.[검진대상여부] AS BIT)
        , [나이]             = CAST(g.[나이] AS INT)
        , [최근완료일자] = CAST(g.[최근완료일자] AS DATE)
        , [사유코드]      = CAST(g.[사유코드] AS INT)
        , [사유메시지]   = CAST(g.[사유메시지] AS NVARCHAR(300))
    FROM [dbo].[UFN_HC_검진대상확인](@수검자ID, @예약일) g
    WHERE @변경범위 = 'ALL' AND @일정통과여부 = 1;

    -- RS4 국가검사항목 — ALL + TGT 대상일 때만 8~11행
    SELECT
          [검사항목코드] = CAST(n.[검사항목코드] AS VARCHAR(10))
        , [검사항목명] = CAST(n.[검사항목명] AS NVARCHAR(100))
        , [국가검사구분] = CAST(n.[국가검사구분] AS VARCHAR(12))
        , [국가검사규칙코드] = CAST(n.[국가검사규칙코드] AS VARCHAR(10))
    FROM [dbo].[UFN_HC_국가검사구성](@수검자ID, @예약일) n
    WHERE @변경범위 = 'ALL' AND @일정통과여부 = 1
    ORDER BY n.[검사항목코드] ASC;

    -- RS5 추가검사항목 — AEX 를 실제 평가하는 변경범위 에서만 7행
    SELECT
          [추가검사코드]    = CAST(a.[추가검사코드] AS VARCHAR(10))
        , [검사항목코드]      = CAST(a.[검사항목코드] AS VARCHAR(10))
        , [검사항목명]      = CAST(a.[검사항목명] AS NVARCHAR(100))
        , [요청선택여부]     = CAST(a.[요청선택여부] AS BIT)
        , [유효선택여부]      = CAST(a.[유효선택여부] AS BIT)
        , [선택가능]     = CAST(a.[선택가능] AS BIT)
        , [사유코드]    = CAST(a.[사유코드] AS INT)
        , [사유메시지] = CAST(a.[사유메시지] AS NVARCHAR(300))
    FROM @추가검사 a
    ORDER BY a.[추가검사코드] ASC;
END
GO
-- SP-LOG-01 (05 §8.3). 00 CP-06 이 변경기록을 열람용으로 규정한 것을 받는 유일한 조회 SP 다.
--   대상 행 1개의 변경 내역만 낸다 - 기간·조작자 전체 검색은 제공하지 않는다.
--   IX_변경이력_TARGET 의 Key 순서(대상테이블, 대상키, 기록일시 DESC)가 술어와 정렬을 그대로 덮는다.
--   [!] 대상 행의 존재를 확인하지 않는다. 감사 기록은 대상 행보다 오래 살기 때문이다 (04 §8.6.3).
--       기록이 0건이면 200 이 아니라 결과코드=0 + RS1 0행이다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_변경이력_조회]
    @대상테이블 NVARCHAR(10),
    @대상키   BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();
    DECLARE @결과코드 INT = 0, @오류항목 NVARCHAR(50) = NULL;
    DECLARE @결과메시지 NVARCHAR(300) = N'정상 처리되었습니다.';

    -- 1. 정규화 (05 §2.2). 대상테이블은 한글 값이라 UPPER 하지 않는다.
    SET @대상테이블 = NULLIF(LTRIM(RTRIM(@대상테이블)), N'');

    -- 2. 필수값 -> 값 형식 (05 §5)
    IF @대상테이블 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'대상테이블'; SET @결과메시지 = N'필수값을 입력하십시오.'; END
    ELSE IF @대상키 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'대상키'; SET @결과메시지 = N'필수값을 입력하십시오.'; END
    -- CK_변경이력_TARGET_TABLE 과 같은 도메인이다 (04 §8.6.3). 완료이력은 쓰는 Write SP 가 0개다.
    ELSE IF @대상테이블 NOT IN (N'수검자', N'예약접수')
    BEGIN SET @결과코드 = 101; SET @오류항목 = N'대상테이블'; SET @결과메시지 = N'입력값이 올바르지 않습니다.'; END

    -- RS0
    SELECT
          CAST(CASE WHEN @결과코드 = 0 THEN 1 ELSE 0 END AS BIT) AS [성공여부]
        , CAST(@결과코드 AS INT)                  AS [결과코드]
        , CAST(@결과메시지 AS NVARCHAR(300))         AS [결과메시지]
        , CAST(@오류항목 AS NVARCHAR(50))         AS [오류항목]
        , CAST(@서버시각 AS DATETIME2(7))   AS [서버시각];

    IF @결과코드 <> 0 RETURN;

    -- RS1 변경이력 - 대상테이블은 싣지 않는다. 호출자가 이미 알고 넘긴 값이다 (05 §8.3).
    SELECT
          [이력ID]        = CAST(h.[이력ID]   AS BIGINT)
        , [기록일시]   = CAST(h.[기록일시] AS DATETIME2(0))
        , [조작자명] = CAST(h.[조작자명] AS NVARCHAR(50))
        , [컬럼명]   = CAST(h.[컬럼명]   AS NVARCHAR(30))
        , [변경전]  = CAST(h.[변경전]   AS NVARCHAR(4000))
        , [변경후]   = CAST(h.[변경후]   AS NVARCHAR(4000))
      FROM [dbo].[변경이력] h
     WHERE h.[대상테이블] = @대상테이블
       AND h.[대상키]     = @대상키
     ORDER BY h.[기록일시] DESC, h.[이력ID] DESC;
END
GO
