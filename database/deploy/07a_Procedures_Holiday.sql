SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
PRINT N'--- 07a_Procedures_Holiday 시작 ---';
GO
----------------------------------------------------------------------------
-- 기준정보 SP 4개 (05 §12.4~§12.8 · 06 §18).
--
-- 업무 SP 와 다른 점 셋을 여기 적어 둔다.
--   [1] 공통 업무 가능조건(308·309)을 적용하지 않는다. 휴무일 관리는 검진 업무가 아니라
--       기준정보 정비이며, 운영시간 안으로 묶으면 "내일이 휴무가 되었다" 를 오늘 18:00 이후에
--       넣을 수 없다 - 정비가 필요한 바로 그 시각에 막히는 셈이다 (03 §24.2).
--   [2] @조작자명 을 받지 않는다. [변경이력] 의 대상 테이블은 수검자·예약접수 둘뿐이라
--       (04 §8.6.3) 기록할 곳이 없다. 받아 두고 버리는 Parameter 를 계약에 남기지 않는다.
--   [3] 파일 이름이 07a 다. 08_Verify.sql 을 09 로 밀면 06 §7 트리·계획·게이트의 경로 참조가
--       함께 어긋난다. tests/00b_Test_Harness_RCP.sql 이 이미 쓰는 방식이다.
----------------------------------------------------------------------------
GO
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_휴무일목록_조회]
    @시작일자 DATE,
    @종료일자 DATE,
    @휴무구분 NVARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();
    DECLARE @오늘날짜 DATE = CONVERT(DATE, @서버시각);

    -- 00 §7.4 가 정한 경고 임계의 **사본**이다. 화면이 임계 숫자를 갖지 않도록 RS2 로 함께 내보낸다.
    -- 사본과 00 이 어긋나면 scripts/verify-holiday-seed.sh 가 잡는다 (ROOT AGENTS.md §6).
    DECLARE @경고임계일수 INT = 180;

    DECLARE @성공여부 BIT = 1, @결과코드 INT = 0, @오류항목 NVARCHAR(50) = NULL;
    DECLARE @결과메시지 NVARCHAR(300) = N'정상 처리되었습니다.';

    SET @휴무구분 = NULLIF(LTRIM(RTRIM(@휴무구분)), N'');

    IF @시작일자 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'시작일자'; SET @결과메시지 = N'필수값을 입력하십시오.'; END
    ELSE IF @종료일자 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'종료일자'; SET @결과메시지 = N'필수값을 입력하십시오.'; END
    ELSE IF @휴무구분 IS NOT NULL AND @휴무구분 NOT IN (N'법정공휴일', N'대체공휴일', N'자체휴무일')
    BEGIN SET @결과코드 = 101; SET @오류항목 = N'휴무구분'; SET @결과메시지 = N'입력값이 올바르지 않습니다.'; END
    ELSE IF @시작일자 > @종료일자
    BEGIN SET @결과코드 = 104; SET @오류항목 = N'시작일자'; SET @결과메시지 = N'시작일은 종료일보다 늦을 수 없습니다.'; END

    IF @결과코드 <> 0 SET @성공여부 = 0;

    SELECT
          CAST(@성공여부   AS BIT)             AS [성공여부]
        , CAST(@결과코드   AS INT)             AS [결과코드]
        , CAST(@결과메시지 AS NVARCHAR(300))   AS [결과메시지]
        , CAST(@오류항목   AS NVARCHAR(50))    AS [오류항목]
        , CAST(@서버시각   AS DATETIME2(7))    AS [서버시각];

    IF @결과코드 <> 0 RETURN;

    -- RS1 - 휴무일 목록. 0건은 성공이다 (05 §3.4).
    SELECT
          [휴무일자] = CAST(h.[휴무일자] AS DATE)
        , [휴무일명] = CAST(h.[휴무일명] AS NVARCHAR(100))
        , [휴무구분] = CAST(h.[휴무구분] AS NVARCHAR(10))
        , [사용여부] = CAST(h.[사용여부] AS BIT)
        , [비고]     = CAST(h.[비고]     AS NVARCHAR(500))
        , [행버전]   = CAST(h.[행버전]   AS BINARY(8))
      FROM [dbo].[휴무일] h
     WHERE h.[휴무일자] BETWEEN @시작일자 AND @종료일자
       AND (@휴무구분 IS NULL OR h.[휴무구분] = @휴무구분)
     ORDER BY h.[휴무일자];

    -- RS2 - 공휴일 등재현황. **@휴무구분 필터의 영향을 받지 않는다** (05 §12.5).
    --       화면이 자체휴무일만 보고 있어도 만료 경고는 같아야 한다.
    DECLARE @공휴일최종일자 DATE =
        (SELECT MAX(h.[휴무일자]) FROM [dbo].[휴무일] h
          WHERE h.[휴무구분] IN (N'법정공휴일', N'대체공휴일'));

    SELECT
          [공휴일최종일자] = CAST(@공휴일최종일자 AS DATE)
        , [잔여일수]       = CAST(DATEDIFF(DAY, @오늘날짜, @공휴일최종일자) AS INT)
        , [경고임계일수]   = CAST(@경고임계일수 AS INT);
END
GO
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_자체휴무일_등록]
    @휴무일자 DATE,
    @휴무일명 NVARCHAR(100),
    @사용여부 BIT,
    @비고     NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @@TRANCOUNT > 0
        THROW 50003, N'이 프로시저는 호출자 트랜잭션 안에서 실행할 수 없습니다.', 1;
    SET XACT_ABORT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();
    DECLARE @저장시각 DATETIME2(0) = CONVERT(DATETIME2(0), @서버시각);

    DECLARE @성공여부 BIT = 1, @결과코드 INT = 0, @오류항목 NVARCHAR(50) = NULL;
    DECLARE @결과메시지 NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @잠금결과 INT, @자원휴무일 NVARCHAR(255);
    DECLARE @결과행버전 BINARY(8) = NULL;

    SET @휴무일명 = NULLIF(LTRIM(RTRIM(@휴무일명)), N'');
    SET @비고     = NULLIF(LTRIM(RTRIM(@비고)), N'');

    IF @휴무일자 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'휴무일자'; END
    ELSE IF @휴무일명 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'휴무일명'; END
    ELSE IF @사용여부 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'사용여부'; END

    IF @결과코드 = 100 SET @결과메시지 = N'필수값을 입력하십시오.';
    IF @결과코드 <> 0 SET @성공여부 = 0;

    IF @결과코드 = 0
    BEGIN
        -- PK 가 중복을 막지만 잠금 없이 동시 INSERT 하면 한쪽이 Msg 2627 로 죽는다.
        -- G11 이 2627 **0건**을 요구하므로 제약 위반을 잡는 대신 앞에서 직렬화한다 (06 §23.0).
        SET @자원휴무일 = N'HC|HOL|' + CONVERT(NVARCHAR(8), @휴무일자, 112);

        BEGIN TRY
            BEGIN TRANSACTION;

            EXEC @잠금결과 = sp_getapplock @Resource = @자원휴무일, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
            IF @잠금결과 < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            IF EXISTS (SELECT 1 FROM [dbo].[휴무일] WHERE [휴무일자] = @휴무일자)
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 801; SET @오류항목 = N'휴무일자';
                SET @결과메시지 = N'이미 등록된 휴무일입니다.';
            END
            ELSE
            BEGIN
                -- [휴무구분] 은 Parameter 가 아니다. 이 SP 는 자체휴무일만 만든다 (00 HOL-05).
                INSERT INTO [dbo].[휴무일]
                    ([휴무일자], [생성일시], [최종수정일시], [휴무일명], [휴무구분], [사용여부], [비고])
                VALUES
                    (@휴무일자, @저장시각, @저장시각, @휴무일명, N'자체휴무일', @사용여부, @비고);

                SELECT @결과행버전 = h.[행버전] FROM [dbo].[휴무일] h WHERE h.[휴무일자] = @휴무일자;
            END

            IF @결과코드 = 0 COMMIT TRANSACTION;
            ELSE ROLLBACK TRANSACTION;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    SELECT
          CAST(@성공여부   AS BIT)             AS [성공여부]
        , CAST(@결과코드   AS INT)             AS [결과코드]
        , CAST(@결과메시지 AS NVARCHAR(300))   AS [결과메시지]
        , CAST(@오류항목   AS NVARCHAR(50))    AS [오류항목]
        , CAST(@서버시각   AS DATETIME2(7))    AS [서버시각];

    IF @결과코드 = 0
        SELECT
              [휴무일자] = CAST(@휴무일자   AS DATE)
            , [행버전]   = CAST(@결과행버전 AS BINARY(8));
END
GO
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_자체휴무일_수정]
    @휴무일자 DATE,
    @행버전   BINARY(8),
    @휴무일명 NVARCHAR(100),
    @사용여부 BIT,
    @비고     NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @@TRANCOUNT > 0
        THROW 50003, N'이 프로시저는 호출자 트랜잭션 안에서 실행할 수 없습니다.', 1;
    SET XACT_ABORT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();
    DECLARE @저장시각 DATETIME2(0) = CONVERT(DATETIME2(0), @서버시각);

    DECLARE @성공여부 BIT = 1, @결과코드 INT = 0, @오류항목 NVARCHAR(50) = NULL;
    DECLARE @결과메시지 NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @잠금결과 INT, @자원휴무일 NVARCHAR(255);
    DECLARE @결과행버전 BINARY(8) = NULL;
    DECLARE @현재구분 NVARCHAR(10) = NULL, @현재행버전 BINARY(8) = NULL;
    DECLARE @현재명 NVARCHAR(100) = NULL, @현재사용 BIT = NULL, @현재비고 NVARCHAR(500) = NULL;

    SET @휴무일명 = NULLIF(LTRIM(RTRIM(@휴무일명)), N'');
    SET @비고     = NULLIF(LTRIM(RTRIM(@비고)), N'');

    IF @휴무일자 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'휴무일자'; END
    ELSE IF @행버전 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'행버전'; END
    ELSE IF @휴무일명 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'휴무일명'; END
    ELSE IF @사용여부 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'사용여부'; END

    IF @결과코드 = 100 SET @결과메시지 = N'필수값을 입력하십시오.';
    IF @결과코드 <> 0 SET @성공여부 = 0;

    IF @결과코드 = 0
    BEGIN
        SET @자원휴무일 = N'HC|HOL|' + CONVERT(NVARCHAR(8), @휴무일자, 112);

        BEGIN TRY
            BEGIN TRANSACTION;

            EXEC @잠금결과 = sp_getapplock @Resource = @자원휴무일, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
            IF @잠금결과 < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            SELECT @현재구분 = h.[휴무구분], @현재행버전 = h.[행버전]
                 , @현재명 = h.[휴무일명], @현재사용 = h.[사용여부], @현재비고 = h.[비고]
              FROM [dbo].[휴무일] h WHERE h.[휴무일자] = @휴무일자;

            IF @현재구분 IS NULL
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 800; SET @오류항목 = N'휴무일자';
                SET @결과메시지 = N'휴무일을 찾을 수 없습니다.';
            END
            -- 휴무구분 확인이 행버전보다 **앞**이다. 법정공휴일 행에 낡은 토큰으로 요청이 와도
            -- 601 이 아니라 802 를 돌려주는 편이 화면에 도움이 된다 - 다시 조회해도 결과가
            -- 달라지지 않기 때문이다 (05 §12.7).
            ELSE IF @현재구분 <> N'자체휴무일'
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 802; SET @오류항목 = N'휴무구분';
                SET @결과메시지 = N'법정공휴일·대체공휴일은 화면에서 변경할 수 없습니다.';
            END
            ELSE IF @현재행버전 <> @행버전
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 601; SET @오류항목 = N'행버전';
                SET @결과메시지 = N'다른 사용자가 먼저 변경했습니다. 최신 정보를 다시 조회하십시오.';
            END
            -- 실제 변경 여부는 NULL-safe 로 본다 (06 §28.2).
            ELSE IF @현재명 = @휴무일명 AND @현재사용 = @사용여부
                AND ((@현재비고 IS NULL AND @비고 IS NULL) OR @현재비고 = @비고)
            BEGIN
                SET @결과코드 = 1;
                SET @결과메시지 = N'변경된 내용이 없습니다.';
                SET @결과행버전 = @현재행버전;
            END
            ELSE
            BEGIN
                UPDATE [dbo].[휴무일]
                   SET [휴무일명]     = @휴무일명
                     , [사용여부]     = @사용여부
                     , [비고]         = @비고
                     , [최종수정일시] = @저장시각
                 WHERE [휴무일자] = @휴무일자
                   AND [행버전]   = @행버전;

                IF @@ROWCOUNT = 0
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 601; SET @오류항목 = N'행버전';
                    SET @결과메시지 = N'다른 사용자가 먼저 변경했습니다. 최신 정보를 다시 조회하십시오.';
                END
                ELSE
                    SELECT @결과행버전 = h.[행버전] FROM [dbo].[휴무일] h WHERE h.[휴무일자] = @휴무일자;
            END

            IF @결과코드 IN (0, 1) COMMIT TRANSACTION;
            ELSE ROLLBACK TRANSACTION;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    SELECT
          CAST(@성공여부   AS BIT)             AS [성공여부]
        , CAST(@결과코드   AS INT)             AS [결과코드]
        , CAST(@결과메시지 AS NVARCHAR(300))   AS [결과메시지]
        , CAST(@오류항목   AS NVARCHAR(50))    AS [오류항목]
        , CAST(@서버시각   AS DATETIME2(7))    AS [서버시각];

    IF @결과코드 IN (0, 1)
        SELECT
              [휴무일자] = CAST(@휴무일자   AS DATE)
            , [행버전]   = CAST(@결과행버전 AS BINARY(8));
END
GO
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_자체휴무일_삭제]
    @휴무일자 DATE,
    @행버전   BINARY(8)
AS
BEGIN
    SET NOCOUNT ON;

    IF @@TRANCOUNT > 0
        THROW 50003, N'이 프로시저는 호출자 트랜잭션 안에서 실행할 수 없습니다.', 1;
    SET XACT_ABORT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();

    DECLARE @성공여부 BIT = 1, @결과코드 INT = 0, @오류항목 NVARCHAR(50) = NULL;
    DECLARE @결과메시지 NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @잠금결과 INT, @자원휴무일 NVARCHAR(255);
    DECLARE @현재구분 NVARCHAR(10) = NULL, @현재행버전 BINARY(8) = NULL;

    IF @휴무일자 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'휴무일자'; END
    ELSE IF @행버전 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'행버전'; END

    IF @결과코드 = 100 SET @결과메시지 = N'필수값을 입력하십시오.';
    IF @결과코드 <> 0 SET @성공여부 = 0;

    IF @결과코드 = 0
    BEGIN
        SET @자원휴무일 = N'HC|HOL|' + CONVERT(NVARCHAR(8), @휴무일자, 112);

        BEGIN TRY
            BEGIN TRANSACTION;

            EXEC @잠금결과 = sp_getapplock @Resource = @자원휴무일, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
            IF @잠금결과 < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            SELECT @현재구분 = h.[휴무구분], @현재행버전 = h.[행버전]
              FROM [dbo].[휴무일] h WHERE h.[휴무일자] = @휴무일자;

            IF @현재구분 IS NULL
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 800; SET @오류항목 = N'휴무일자';
                SET @결과메시지 = N'휴무일을 찾을 수 없습니다.';
            END
            ELSE IF @현재구분 <> N'자체휴무일'
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 802; SET @오류항목 = N'휴무구분';
                SET @결과메시지 = N'법정공휴일·대체공휴일은 화면에서 변경할 수 없습니다.';
            END
            ELSE IF @현재행버전 <> @행버전
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 601; SET @오류항목 = N'행버전';
                SET @결과메시지 = N'다른 사용자가 먼저 변경했습니다. 최신 정보를 다시 조회하십시오.';
            END
            ELSE
            BEGIN
                -- 물리 삭제다. 사용여부 = 0 은 삭제가 아니라 일시 무효화이며 그 날짜를 계속 점유한다.
                DELETE FROM [dbo].[휴무일]
                 WHERE [휴무일자] = @휴무일자
                   AND [행버전]   = @행버전;

                IF @@ROWCOUNT = 0
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 601; SET @오류항목 = N'행버전';
                    SET @결과메시지 = N'다른 사용자가 먼저 변경했습니다. 최신 정보를 다시 조회하십시오.';
                END
            END

            IF @결과코드 = 0 COMMIT TRANSACTION;
            ELSE ROLLBACK TRANSACTION;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    -- RS1 이 없다. 지운 행의 행버전 을 돌려줄 이유가 없다 (05 §12.8).
    SELECT
          CAST(@성공여부   AS BIT)             AS [성공여부]
        , CAST(@결과코드   AS INT)             AS [결과코드]
        , CAST(@결과메시지 AS NVARCHAR(300))   AS [결과메시지]
        , CAST(@오류항목   AS NVARCHAR(50))    AS [오류항목]
        , CAST(@서버시각   AS DATETIME2(7))    AS [서버시각];
END
GO
PRINT N'PASS PROC-HOL 휴무일 SP 4개 배포 완료';
GO
