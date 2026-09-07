SET QUOTED_IDENTIFIER ON;   -- 01_Schema.sql 과 같은 설정으로 객체를 만든다 (CLAUDE.md §6)
GO
SET NOCOUNT ON;
GO
-- 접수 Write SP 3개 (05 §12). Write SP 공통 Template 은 스펙 §21.1 이다.
--   접수는 예약일·시간대·검사구성을 바꾸지 않는다. 상태코드와 추가검사항목만 움직인다.
--
-- [!] UPDATE_접수완료 만 SLOT 잠금을 함께 잡는다 (스펙 §24.2). RSV -> RCP 는 상태코드가
--     IX_예약접수_SLOT 의 Key 안에서 뒤(RSV)에서 앞(RCP)으로 이동하는 유일한 전이라,
--     READ COMMITTED 스캔이 이미 지난 구간으로 행이 옮겨가 통째로 누락될 수 있다.
--     그 과소집계는 정원 초과 저장(00 RP-03)·RP-06 위반·EP-08 우회로 이어진다.
--     취소 계열(RSV->CNR, RCP->CNC)은 집합 밖으로 나가는 전이라 오차가 보수적이므로 WORK 만 잡는다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_접수_완료]
    @업무ID       BIGINT,
    @행버전   BINARY(8),
    @조작자명 NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- [X] 호출자 트랜잭션 안에서 실행할 수 없다. Write SP 는 savepoint 없이 BEGIN/COMMIT/ROLLBACK 을
    --     맨몸으로 쓰므로 @@TRANCOUNT > 0 으로 진입하면 네 가지가 동시에 깨진다.
    --       업무실패  이름 없는 ROLLBACK 이 **바깥 트랜잭션까지** 되돌리고 EXEC 반환 시 Msg 266
    --       성공      COMMIT 이 카운트만 줄여 아무것도 확정되지 않은 채 성공여부=1 이 나간다
    --       감사      스펙 §21.1 ① 의 '@@TRANCOUNT = 0 지점' 전제가 거짓이 되어 함께 롤백된다
    --       잠금      @LockOwner='Transaction' 이라 applock 이 바깥 트랜잭션까지 살아남는다
    --     진입에서 자른다. 스펙 §20 에 50003 으로 등재했다.
    IF @@TRANCOUNT > 0
        THROW 50003, N'이 프로시저는 호출자 트랜잭션 안에서 실행할 수 없습니다.', 1;
    SET XACT_ABORT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();
    DECLARE @결과상태코드 CHAR(3), @결과행버전 BINARY(8);
    DECLARE @오늘날짜      DATE         = CONVERT(DATE, @서버시각);
    DECLARE @저장시각  DATETIME2(0) = CONVERT(DATETIME2(0), @서버시각);

    DECLARE @성공여부 BIT = 1, @결과코드 INT = 0, @오류항목 NVARCHAR(50) = NULL;
    DECLARE @결과메시지 NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @대상키 BIGINT = NULL;
    DECLARE @잠금결과 INT, @자원수검자 NVARCHAR(255), @자원업무 NVARCHAR(255), @자원시간대 NVARCHAR(255);

    DECLARE @수검자ID BIGINT = NULL, @현재수검자ID BIGINT = NULL;
    DECLARE @현재예약일 DATE, @현재시간대코드 CHAR(2), @현재상태코드 CHAR(3), @현재행버전 BINARY(8);
    DECLARE @현재국가검사 NVARCHAR(100), @현재추가검사 NVARCHAR(50);
    DECLARE @저장국가검사건수 INT, @저장추가검사건수 INT;

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 필수값. 허용 결과코드 는 0, 100, 304, 308~309, 500, 502~503, 601, 701 이다.
    ----------------------------------------------------------------------------
    SET @조작자명 = NULLIF(LTRIM(RTRIM(@조작자명)), N'');

    IF @업무ID IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'업무ID'; END
    ELSE IF @행버전 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'행버전'; END
    ELSE IF @조작자명 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'조작자명'; END

    IF @결과코드 = 100 SET @결과메시지 = N'필수값을 입력하십시오.';

    ----------------------------------------------------------------------------
    -- [2] Transaction 밖 : PAT 자원명을 만들 수검자ID 확보 (스펙 §23.2)
    ----------------------------------------------------------------------------
    IF @결과코드 = 0
    BEGIN
        SELECT @수검자ID = w.[수검자ID] FROM [dbo].[예약접수] w WHERE w.[업무ID] = @업무ID;
        IF @수검자ID IS NULL
        BEGIN
            SET @결과코드 = 500; SET @오류항목 = N'업무ID';
            SET @결과메시지 = N'예약·접수 업무를 찾을 수 없습니다.';
        END
    END

    IF @결과코드 <> 0 SET @성공여부 = 0;

    ----------------------------------------------------------------------------
    -- [3]~[5] Transaction. 전역 순서 PAT(3) -> WORK(4) -> SLOT(5) (스펙 §23·§24.2)
    ----------------------------------------------------------------------------
    IF @결과코드 = 0
    BEGIN
        SET @자원수검자  = N'HC|PAT|'  + CONVERT(NVARCHAR(20), @수검자ID);
        SET @자원업무 = N'HC|WORK|' + CONVERT(NVARCHAR(20), @업무ID);

        BEGIN TRY
            BEGIN TRANSACTION;

            EXEC @잠금결과 = sp_getapplock @Resource = @자원수검자, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
            IF @잠금결과 < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            EXEC @잠금결과 = sp_getapplock @Resource = @자원업무, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
            IF @잠금결과 < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            -- WORK 를 잡은 뒤 읽으므로 이 값이 권위값이다.
            SELECT @현재수검자ID = w.[수검자ID], @현재예약일 = w.[예약일], @현재시간대코드 = w.[시간대코드]
                 , @현재상태코드 = w.[상태코드], @현재행버전 = w.[행버전]
                 , @현재국가검사 = w.[국가검사항목], @현재추가검사 = w.[추가검사항목]
              FROM [dbo].[예약접수] w WHERE w.[업무ID] = @업무ID;

            IF @현재수검자ID IS NULL
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 500; SET @오류항목 = N'업무ID';
                SET @결과메시지 = N'예약·접수 업무를 찾을 수 없습니다.';
            END
            ELSE IF @현재수검자ID <> @수검자ID
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 701; SET @오류항목 = N'업무ID';
                SET @결과메시지 = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
            END

            IF @결과코드 = 0
            BEGIN
                -- [X] @현재시간대코드 정규화. 06 과 같은 이유다 — applock 은 바이트, CK 의 IN 은 CI 다 (실측).
                SET @자원시간대 = N'HC|SLOT|' + CONVERT(CHAR(8), @현재예약일, 112) + N'|' + UPPER(@현재시간대코드);
                EXEC @잠금결과 = sp_getapplock @Resource = @자원시간대, @LockMode = 'Exclusive',
                                         @LockOwner = 'Transaction', @LockTimeout = 5000;
                PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
                IF @잠금결과 < 0
                BEGIN
                    ROLLBACK TRANSACTION;
                    IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                    THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
                END

                -- [4] 재검증 — 05 §12.1 검증순서
                IF @현재상태코드 <> 'RSV'
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 502; SET @오류항목 = N'업무ID';
                    SET @결과메시지 = N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.';
                END
                ELSE IF @현재행버전 <> @행버전
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 601; SET @오류항목 = N'행버전';
                    SET @결과메시지 = N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.';
                END
            END

            -- Work 검사구성 무결성 3종 (스펙 §21.2a). 빈 문자열은 0개다.
            IF @결과코드 = 0
            BEGIN
                SET @저장국가검사건수 = CASE WHEN LEN(ISNULL(@현재국가검사, N'')) = 0 THEN 0
                                 ELSE LEN(@현재국가검사) - LEN(REPLACE(@현재국가검사, N',', N'')) + 1 END;
                SET @저장추가검사건수 = CASE WHEN LEN(ISNULL(@현재추가검사, N'')) = 0 THEN 0
                                 ELSE LEN(@현재추가검사) - LEN(REPLACE(@현재추가검사, N',', N'')) + 1 END;

                IF @저장국가검사건수 NOT BETWEEN 8 AND 11
                   OR @저장추가검사건수 > 6
                   OR (SELECT COUNT(*) FROM [dbo].[검사코드] m
                        WHERE m.[국가검사규칙코드] IS NOT NULL
                          AND N',' + ISNULL(@현재국가검사, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%') <> @저장국가검사건수
                   OR (SELECT COUNT(*) FROM [dbo].[검사코드] m
                        WHERE m.[추가검사코드] IS NOT NULL
                          AND N',' + ISNULL(@현재추가검사, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%') <> @저장추가검사건수
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 701; SET @오류항목 = N'업무ID';
                    SET @결과메시지 = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
                END
            END

            IF @결과코드 = 0
                SELECT @결과코드 = s.[업무가능코드], @결과메시지 = s.[업무가능메시지]
                  FROM [dbo].[UFN_HC_일정확인](@서버시각, @오늘날짜, 'AM', 'NONE') s
                 WHERE s.[현재업무가능] = 0;
            IF @결과코드 IN (308, 309) BEGIN SET @성공여부 = 0; SET @오류항목 = NULL; END

            -- 예약일이 오늘인 업무만 접수할 수 있다 (05 §12.1)
            IF @결과코드 = 0 AND @현재예약일 <> @오늘날짜
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 503; SET @오류항목 = N'업무ID';
                SET @결과메시지 = N'예약일이 오늘인 업무만 접수할 수 있습니다.';
            END

            -- 접수마감 (AM 11:00 / PM 16:00). 마감구분='RECEPTION' 이다 (05 §2.4).
            IF @결과코드 = 0
            BEGIN
                IF EXISTS (SELECT 1 FROM [dbo].[UFN_HC_일정확인](@서버시각, @현재예약일, @현재시간대코드, 'RECEPTION') s
                            WHERE s.[마감경과여부] = 1)
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 304; SET @오류항목 = N'시간대코드';
                    SET @결과메시지 = N'해당 시간대의 마감시간이 지났습니다.';
                END
            END

            IF @결과코드 = 0
            BEGIN
                -- 조건부 UPDATE 표준형 (스펙 §24.4).
                -- 예약일·시간대코드·검사구성 두 컬럼은 건드리지 않는다 (05 §12.1).
                UPDATE [dbo].[예약접수]
                   SET [상태코드]     = 'RCP'
                     , [최종수정일시] = @저장시각
                 WHERE [업무ID]   = @업무ID
                   AND [상태코드] = 'RSV'
                   AND [행버전]   = @행버전;

                IF @@ROWCOUNT = 0
                BEGIN
                    SET @성공여부 = 0;
                    IF EXISTS (SELECT 1 FROM [dbo].[예약접수] WHERE [업무ID] = @업무ID AND [상태코드] <> 'RSV')
                    BEGIN
                        SET @결과코드 = 502; SET @오류항목 = N'업무ID';
                        SET @결과메시지 = N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.';
                    END
                    ELSE
                    BEGIN
                        SET @결과코드 = 601; SET @오류항목 = N'행버전';
                        SET @결과메시지 = N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.';
                    END
                END
                ELSE
                    SET @대상키 = @업무ID;
            END

            -- [X] RS1 을 COMMIT **뒤에** 다시 읽으면 그 사이 다른 세션이 바꾼 값이 나간다.
            --     호출자는 자기 것이 아닌 행버전 을 받고, 그 값으로 보낸 다음 요청이
            --     601 로 막혔어야 하는데 통과한다 — 낙관적 동시성의 유일한 방어선이 뚫린다.
            --     잠금 안에서 포획하고 COMMIT 뒤에는 변수를 낸다. RS 를 트랜잭션 밖에서
            --     낸다는 스펙 §21.1 [6] 은 그대로다 (06 §43-18).
            IF @결과코드 = 0
                SELECT @결과상태코드 = w.[상태코드], @결과행버전 = w.[행버전]
                  FROM [dbo].[예약접수] w WHERE w.[업무ID] = @업무ID;

            IF @결과코드 = 0 COMMIT TRANSACTION;
            ELSE ROLLBACK TRANSACTION;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    ----------------------------------------------------------------------------
    -- [6] Result Set
    ----------------------------------------------------------------------------
    SELECT
          CAST(@성공여부 AS BIT)               AS [성공여부]
        , CAST(@결과코드    AS INT)               AS [결과코드]
        , CAST(@결과메시지     AS NVARCHAR(300))     AS [결과메시지]
        , CAST(@오류항목   AS NVARCHAR(50))       AS [오류항목]
        , CAST(@서버시각 AS DATETIME2(7))   AS [서버시각];

    -- [X] 감사를 RS1 **뒤**에 두면, 클라이언트가 RS0 만 읽고 끊었을 때(ExecuteNonQuery·조기
    --     Dispose) 업무는 커밋됐는데 감사행만 없는 상태가 된다. 04 §8.6.5 ③ 은 "**해당**
    --     Result Set 의 SELECT 를 먼저 낸 뒤에" 로 **단수**이고, 같은 절의 목적은 §14 L1
    --     "기록이 업무 호출을 실패시키지 않는다" 이다. RS0 뒤에 두어도 그 목적은 그대로다.
    --     그래서 '해당 Result Set' 을 결과를 보고하는 RS0 로 읽고 감사를 RS0 뒤·RS1 앞에 둔다.
    --     감사는 여전히 @@TRANCOUNT = 0 지점 · 자체 TRY / 빈 CATCH 다 (06 §21.1 · §43-19).
    ----------------------------------------------------------------------------
    -- [7] 감사 기록. 바뀐 것은 상태코드 하나다.
    ----------------------------------------------------------------------------
    IF @결과코드 = 0 AND @대상키 IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            VALUES
                (@저장시각, @조작자명, N'예약접수', @대상키, N'상태코드', N'RSV', N'RCP');
        END TRY
        BEGIN CATCH
        END CATCH
    END

    -- [6b] RS1 — 감사 **뒤**에 낸다. 순서가 뒤집히면 RS0 만 읽은 호출에서 감사가 사라진다 (§43-19).
    IF @결과코드 = 0
        SELECT
              [업무ID]     = CAST(@업무ID   AS BIGINT)
            , [상태코드]     = CAST(@결과상태코드 AS CHAR(3))
            , [행버전] = CAST(@결과행버전     AS BINARY(8));

END
GO
-- 05 §12.2. RCP 상태에서 추가검사만 바꾼다.
--   예약일·시간대·TGT·NEX 를 변경하지도 재평가하지도 않는다.
--   [!] No-op 에서는 현재 Master 의 비활성·성별·중복 Rule 을 **재평가하지 않는다** (05 §12.2, 스펙 §28).
--       저장 당시엔 유효했지만 지금은 무효인 AEX 가 있어도 동일집합 호출은 성공해야 한다.
--       그래서 무결성·Master·AEX 검증을 전부 No-op 판정 **뒤에** 둔다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_접수추가검사_변경]
    @업무ID           BIGINT,
    @행버전       BINARY(8),
    @추가검사01선택여부 BIT,
    @추가검사02선택여부 BIT,
    @추가검사03선택여부 BIT,
    @추가검사04선택여부 BIT,
    @추가검사05선택여부 BIT,
    @추가검사06선택여부 BIT,
    @추가검사07선택여부 BIT,
    @조작자명     NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- [X] 호출자 트랜잭션 안에서 실행할 수 없다. Write SP 는 savepoint 없이 BEGIN/COMMIT/ROLLBACK 을
    --     맨몸으로 쓰므로 @@TRANCOUNT > 0 으로 진입하면 네 가지가 동시에 깨진다.
    --       업무실패  이름 없는 ROLLBACK 이 **바깥 트랜잭션까지** 되돌리고 EXEC 반환 시 Msg 266
    --       성공      COMMIT 이 카운트만 줄여 아무것도 확정되지 않은 채 성공여부=1 이 나간다
    --       감사      스펙 §21.1 ① 의 '@@TRANCOUNT = 0 지점' 전제가 거짓이 되어 함께 롤백된다
    --       잠금      @LockOwner='Transaction' 이라 applock 이 바깥 트랜잭션까지 살아남는다
    --     진입에서 자른다. 스펙 §20 에 50003 으로 등재했다.
    IF @@TRANCOUNT > 0
        THROW 50003, N'이 프로시저는 호출자 트랜잭션 안에서 실행할 수 없습니다.', 1;
    SET XACT_ABORT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();
    DECLARE @결과상태코드 CHAR(3), @결과행버전 BINARY(8);
    DECLARE @오늘날짜      DATE         = CONVERT(DATE, @서버시각);
    DECLARE @저장시각  DATETIME2(0) = CONVERT(DATETIME2(0), @서버시각);

    DECLARE @성공여부 BIT = 1, @결과코드 INT = 0, @오류항목 NVARCHAR(50) = NULL;
    DECLARE @결과메시지 NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @대상키 BIGINT = NULL;
    DECLARE @잠금결과 INT, @자원업무 NVARCHAR(255);

    DECLARE @수검자ID BIGINT, @현재예약일 DATE, @현재상태코드 CHAR(3), @현재행버전 BINARY(8);
    DECLARE @현재국가검사 NVARCHAR(100), @현재추가검사 NVARCHAR(50), @새추가검사 NVARCHAR(50) = NULL;
    DECLARE @추가검사변경여부 BIT = 0, @저장국가검사건수 INT, @저장추가검사건수 INT;
    DECLARE @코드문자열 NVARCHAR(200), @코드 VARCHAR(10);
    DECLARE @코드목록 TABLE ([코드] VARCHAR(10) PRIMARY KEY);
    DECLARE @요청목록 TABLE ([추가검사코드] VARCHAR(10) PRIMARY KEY);
    DECLARE @추가검사평가 TABLE ([추가검사코드] VARCHAR(10) PRIMARY KEY, [검사항목코드] VARCHAR(10),
                            [요청선택여부] BIT, [유효선택여부] BIT, [선택가능] BIT,
                            [사유코드] INT, [사유메시지] NVARCHAR(300));

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 필수값
    ----------------------------------------------------------------------------
    SET @조작자명 = NULLIF(LTRIM(RTRIM(@조작자명)), N'');

    IF @업무ID IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'업무ID'; END
    ELSE IF @행버전 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'행버전'; END
    ELSE IF @추가검사01선택여부 IS NULL OR @추가검사02선택여부 IS NULL OR @추가검사03선택여부 IS NULL
         OR @추가검사04선택여부 IS NULL OR @추가검사05선택여부 IS NULL OR @추가검사06선택여부 IS NULL
         OR @추가검사07선택여부 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = CASE WHEN @추가검사01선택여부 IS NULL THEN N'추가검사01선택여부'
                  WHEN @추가검사02선택여부 IS NULL THEN N'추가검사02선택여부'
                  WHEN @추가검사03선택여부 IS NULL THEN N'추가검사03선택여부'
                  WHEN @추가검사04선택여부 IS NULL THEN N'추가검사04선택여부'
                  WHEN @추가검사05선택여부 IS NULL THEN N'추가검사05선택여부'
                  WHEN @추가검사06선택여부 IS NULL THEN N'추가검사06선택여부'
                  ELSE N'추가검사07선택여부' END; END
    ELSE IF @조작자명 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'조작자명'; END

    IF @결과코드 = 100 BEGIN SET @결과메시지 = N'필수값을 입력하십시오.'; SET @성공여부 = 0; END

    ----------------------------------------------------------------------------
    -- [3]~[5] Transaction. WORK 만 잡는다 (스펙 §24.1). RCP -> RCP 는 두 NCI 키가 불변이다.
    ----------------------------------------------------------------------------
    IF @결과코드 = 0
    BEGIN
        SET @자원업무 = N'HC|WORK|' + CONVERT(NVARCHAR(20), @업무ID);

        BEGIN TRY
            BEGIN TRANSACTION;

            EXEC @잠금결과 = sp_getapplock @Resource = @자원업무, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
            IF @잠금결과 < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            SELECT @수검자ID = w.[수검자ID], @현재예약일 = w.[예약일]
                 , @현재상태코드 = w.[상태코드], @현재행버전 = w.[행버전]
                 , @현재국가검사 = w.[국가검사항목], @현재추가검사 = w.[추가검사항목]
              FROM [dbo].[예약접수] w WHERE w.[업무ID] = @업무ID;

            IF @현재상태코드 IS NULL
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 500; SET @오류항목 = N'업무ID';
                SET @결과메시지 = N'예약·접수 업무를 찾을 수 없습니다.';
            END
            ELSE IF @현재상태코드 <> 'RCP'
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 502; SET @오류항목 = N'업무ID';
                SET @결과메시지 = N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.';
            END
            ELSE IF @현재행버전 <> @행버전
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 601; SET @오류항목 = N'행버전';
                SET @결과메시지 = N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.';
            END

            -- 현재 AEX 집합과 요청 AEX 집합 비교 (스펙 §28.1 EXCEPT 양방향)
            IF @결과코드 = 0
            BEGIN
                INSERT INTO @요청목록 ([추가검사코드])
                SELECT v.c FROM (VALUES ('OPT01',@추가검사01선택여부),('OPT02',@추가검사02선택여부),
                                        ('OPT03',@추가검사03선택여부),('OPT04',@추가검사04선택여부),
                                        ('OPT05',@추가검사05선택여부),('OPT06',@추가검사06선택여부),
                                        ('OPT07',@추가검사07선택여부)) v(c, b)
                 WHERE v.b = 1;

                SET @추가검사변경여부 = CASE WHEN EXISTS (
                        SELECT [추가검사코드] FROM @요청목록
                        EXCEPT
                        SELECT m.[추가검사코드] FROM [dbo].[검사코드] m
                         WHERE m.[추가검사코드] IS NOT NULL
                           AND N',' + ISNULL(@현재추가검사, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%')
                    OR EXISTS (
                        SELECT m.[추가검사코드] FROM [dbo].[검사코드] m
                         WHERE m.[추가검사코드] IS NOT NULL
                           AND N',' + ISNULL(@현재추가검사, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
                        EXCEPT
                        SELECT [추가검사코드] FROM @요청목록)
                    THEN 1 ELSE 0 END;

                -- No-op. 여기서 끝나므로 아래의 Master·성별·중복 Rule 을 지나지 않는다.
                IF @추가검사변경여부 = 0
                BEGIN
                    SET @성공여부 = 1; SET @결과코드 = 1; SET @오류항목 = NULL;
                    SET @결과메시지 = N'변경된 내용이 없습니다.';
                END
            END

            -- 실제 변경일 때만 저장 검사구성 무결성 3종과 Master 구성을 본다 (05 §12.2)
            IF @결과코드 = 0
            BEGIN
                SET @저장국가검사건수 = CASE WHEN LEN(ISNULL(@현재국가검사, N'')) = 0 THEN 0
                                 ELSE LEN(@현재국가검사) - LEN(REPLACE(@현재국가검사, N',', N'')) + 1 END;
                SET @저장추가검사건수 = CASE WHEN LEN(ISNULL(@현재추가검사, N'')) = 0 THEN 0
                                 ELSE LEN(@현재추가검사) - LEN(REPLACE(@현재추가검사, N',', N'')) + 1 END;

                IF @저장국가검사건수 NOT BETWEEN 8 AND 11
                   OR @저장추가검사건수 > 6
                   OR (SELECT COUNT(*) FROM [dbo].[검사코드] m
                        WHERE m.[국가검사규칙코드] IS NOT NULL
                          AND N',' + ISNULL(@현재국가검사, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%') <> @저장국가검사건수
                   OR (SELECT COUNT(*) FROM [dbo].[검사코드] m
                        WHERE m.[추가검사코드] IS NOT NULL
                          AND N',' + ISNULL(@현재추가검사, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%') <> @저장추가검사건수
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 701; SET @오류항목 = N'업무ID';
                    SET @결과메시지 = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
                END
                ELSE IF (SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [추가검사코드] IS NOT NULL) <> 7
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 700; SET @오류항목 = NULL;
                    SET @결과메시지 = N'검사 Master 구성이 올바르지 않습니다.';
                END
            END

            IF @결과코드 = 0
                SELECT @결과코드 = s.[업무가능코드], @결과메시지 = s.[업무가능메시지]
                  FROM [dbo].[UFN_HC_일정확인](@서버시각, @오늘날짜, 'AM', 'NONE') s
                 WHERE s.[현재업무가능] = 0;
            IF @결과코드 IN (308, 309) BEGIN SET @성공여부 = 0; SET @오류항목 = NULL; END

            -- AEX 성별·중복 Rule 은 **저장된 NEX** 기준이다 (05 §6.4.3, 저장검사사용여부=1)
            IF @결과코드 = 0
            BEGIN
                INSERT INTO @추가검사평가 ([추가검사코드], [검사항목코드], [요청선택여부], [유효선택여부], [선택가능], [사유코드], [사유메시지])
                SELECT x.[추가검사코드], x.[검사항목코드], x.[요청선택여부], x.[유효선택여부], x.[선택가능], x.[사유코드], x.[사유메시지]
                  FROM [dbo].[UFN_HC_추가검사확인](@수검자ID, @현재예약일, @업무ID, 1,
                         @추가검사01선택여부, @추가검사02선택여부, @추가검사03선택여부, @추가검사04선택여부,
                         @추가검사05선택여부, @추가검사06선택여부, @추가검사07선택여부) x;

                SELECT TOP (1)
                       @결과코드  = a.[사유코드]
                     , @결과메시지   = a.[사유메시지]
                     , @오류항목 = N'추가검사' + RIGHT(a.[추가검사코드], 2) + N'선택여부'
                  FROM @추가검사평가 a
                 WHERE a.[요청선택여부] = 1 AND a.[선택가능] = 0
                 ORDER BY a.[추가검사코드];
                IF @결과코드 <> 0 SET @성공여부 = 0;
            END

            IF @결과코드 = 0
            BEGIN
                DELETE FROM @코드목록;
                INSERT INTO @코드목록 ([코드]) SELECT a.[검사항목코드] FROM @추가검사평가 a WHERE a.[유효선택여부] = 1;
                SET @코드문자열 = N'';
                WHILE EXISTS (SELECT 1 FROM @코드목록)
                BEGIN
                    SELECT TOP (1) @코드 = [코드] FROM @코드목록 ORDER BY [코드];
                    SET @코드문자열 = @코드문자열 + @코드 + N',';
                    DELETE FROM @코드목록 WHERE [코드] = @코드;
                END
                SET @새추가검사 = CASE WHEN LEN(@코드문자열) = 0 THEN NULL ELSE LEFT(@코드문자열, LEN(@코드문자열) - 1) END;

                -- 조건부 UPDATE 표준형 (스펙 §24.4). 예약일·시간대·국가검사항목은 건드리지 않는다.
                UPDATE [dbo].[예약접수]
                   SET [추가검사항목] = @새추가검사
                     , [최종수정일시] = @저장시각
                 WHERE [업무ID]   = @업무ID
                   AND [상태코드] = 'RCP'
                   AND [행버전]   = @행버전;

                IF @@ROWCOUNT = 0
                BEGIN
                    SET @성공여부 = 0;
                    IF EXISTS (SELECT 1 FROM [dbo].[예약접수] WHERE [업무ID] = @업무ID AND [상태코드] <> 'RCP')
                    BEGIN
                        SET @결과코드 = 502; SET @오류항목 = N'업무ID';
                        SET @결과메시지 = N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.';
                    END
                    ELSE
                    BEGIN
                        SET @결과코드 = 601; SET @오류항목 = N'행버전';
                        SET @결과메시지 = N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.';
                    END
                END
                ELSE
                    SET @대상키 = @업무ID;
            END

            -- [X] RS1 을 COMMIT **뒤에** 다시 읽으면 그 사이 다른 세션이 바꾼 값이 나간다.
            --     호출자는 자기 것이 아닌 행버전 을 받고, 그 값으로 보낸 다음 요청이
            --     601 로 막혔어야 하는데 통과한다 — 낙관적 동시성의 유일한 방어선이 뚫린다.
            --     잠금 안에서 포획하고 COMMIT 뒤에는 변수를 낸다. RS 를 트랜잭션 밖에서
            --     낸다는 스펙 §21.1 [6] 은 그대로다 (06 §43-18).
            IF @결과코드 IN (0, 1)
                SELECT @결과상태코드 = w.[상태코드], @결과행버전 = w.[행버전]
                  FROM [dbo].[예약접수] w WHERE w.[업무ID] = @업무ID;

            IF @결과코드 IN (0, 1) COMMIT TRANSACTION;
            ELSE ROLLBACK TRANSACTION;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    ----------------------------------------------------------------------------
    -- [6] Result Set. No-op 은 기존 행버전 을 유지한다 (05 §12.2).
    ----------------------------------------------------------------------------
    SELECT
          CAST(@성공여부 AS BIT)               AS [성공여부]
        , CAST(@결과코드    AS INT)               AS [결과코드]
        , CAST(@결과메시지     AS NVARCHAR(300))     AS [결과메시지]
        , CAST(@오류항목   AS NVARCHAR(50))       AS [오류항목]
        , CAST(@서버시각 AS DATETIME2(7))   AS [서버시각];

    -- [X] 감사를 RS1 **뒤**에 두면, 클라이언트가 RS0 만 읽고 끊었을 때(ExecuteNonQuery·조기
    --     Dispose) 업무는 커밋됐는데 감사행만 없는 상태가 된다. 04 §8.6.5 ③ 은 "**해당**
    --     Result Set 의 SELECT 를 먼저 낸 뒤에" 로 **단수**이고, 같은 절의 목적은 §14 L1
    --     "기록이 업무 호출을 실패시키지 않는다" 이다. RS0 뒤에 두어도 그 목적은 그대로다.
    --     그래서 '해당 Result Set' 을 결과를 보고하는 RS0 로 읽고 감사를 RS0 뒤·RS1 앞에 둔다.
    --     감사는 여전히 @@TRANCOUNT = 0 지점 · 자체 TRY / 빈 CATCH 다 (06 §21.1 · §43-19).
    ----------------------------------------------------------------------------
    -- [7] 감사 기록. 바뀐 것은 추가검사항목 하나다.
    ----------------------------------------------------------------------------
    IF @결과코드 = 0 AND @대상키 IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            SELECT @저장시각, @조작자명, N'예약접수', @대상키, N'추가검사항목'
                 , CONVERT(NVARCHAR(4000), @현재추가검사), CONVERT(NVARCHAR(4000), @새추가검사)
             WHERE ISNULL(CONVERT(NVARCHAR(4000), @현재추가검사), N'~NULL~')
                <> ISNULL(CONVERT(NVARCHAR(4000), @새추가검사), N'~NULL~');
        END TRY
        BEGIN CATCH
        END CATCH
    END

    -- [6b] RS1 — 감사 **뒤**에 낸다. 순서가 뒤집히면 RS0 만 읽은 호출에서 감사가 사라진다 (§43-19).
    IF @결과코드 IN (0, 1)
        SELECT
              [업무ID]     = CAST(@업무ID   AS BIGINT)
            , [상태코드]     = CAST(@결과상태코드 AS CHAR(3))
            , [행버전] = CAST(@결과행버전     AS BINARY(8));

END
GO
-- 05 §12.3. RCP -> CNC. 검사구성 두 컬럼은 보존하고, 접수 마감시각은 취소 가능조건이 아니다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_접수_취소]
    @업무ID       BIGINT,
    @행버전   BINARY(8),
    @조작자명 NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- [X] 호출자 트랜잭션 안에서 실행할 수 없다. Write SP 는 savepoint 없이 BEGIN/COMMIT/ROLLBACK 을
    --     맨몸으로 쓰므로 @@TRANCOUNT > 0 으로 진입하면 네 가지가 동시에 깨진다.
    --       업무실패  이름 없는 ROLLBACK 이 **바깥 트랜잭션까지** 되돌리고 EXEC 반환 시 Msg 266
    --       성공      COMMIT 이 카운트만 줄여 아무것도 확정되지 않은 채 성공여부=1 이 나간다
    --       감사      스펙 §21.1 ① 의 '@@TRANCOUNT = 0 지점' 전제가 거짓이 되어 함께 롤백된다
    --       잠금      @LockOwner='Transaction' 이라 applock 이 바깥 트랜잭션까지 살아남는다
    --     진입에서 자른다. 스펙 §20 에 50003 으로 등재했다.
    IF @@TRANCOUNT > 0
        THROW 50003, N'이 프로시저는 호출자 트랜잭션 안에서 실행할 수 없습니다.', 1;
    SET XACT_ABORT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();
    DECLARE @결과상태코드 CHAR(3), @결과행버전 BINARY(8);
    DECLARE @오늘날짜      DATE         = CONVERT(DATE, @서버시각);
    DECLARE @저장시각  DATETIME2(0) = CONVERT(DATETIME2(0), @서버시각);

    DECLARE @성공여부 BIT = 1, @결과코드 INT = 0, @오류항목 NVARCHAR(50) = NULL;
    DECLARE @결과메시지 NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @대상키 BIGINT = NULL;
    DECLARE @잠금결과 INT, @자원업무 NVARCHAR(255);
    DECLARE @현재상태코드 CHAR(3) = NULL, @현재행버전 BINARY(8) = NULL;

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 필수값. 허용 결과코드 는 0, 100, 308~309, 500, 502, 601 뿐이다.
    ----------------------------------------------------------------------------
    SET @조작자명 = NULLIF(LTRIM(RTRIM(@조작자명)), N'');

    IF @업무ID IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'업무ID'; END
    ELSE IF @행버전 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'행버전'; END
    ELSE IF @조작자명 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'조작자명'; END

    IF @결과코드 = 100 SET @결과메시지 = N'필수값을 입력하십시오.';
    IF @결과코드 <> 0 SET @성공여부 = 0;

    ----------------------------------------------------------------------------
    -- [3]~[5] Transaction. RCP -> CNC 는 집합 밖으로 나가는 전이라 WORK 만 잡는다 (스펙 §24.2).
    ----------------------------------------------------------------------------
    IF @결과코드 = 0
    BEGIN
        SET @자원업무 = N'HC|WORK|' + CONVERT(NVARCHAR(20), @업무ID);

        BEGIN TRY
            BEGIN TRANSACTION;

            EXEC @잠금결과 = sp_getapplock @Resource = @자원업무, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
            IF @잠금결과 < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            SELECT @현재상태코드 = w.[상태코드], @현재행버전 = w.[행버전]
              FROM [dbo].[예약접수] w WHERE w.[업무ID] = @업무ID;

            IF @현재상태코드 IS NULL
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 500; SET @오류항목 = N'업무ID';
                SET @결과메시지 = N'예약·접수 업무를 찾을 수 없습니다.';
            END
            ELSE IF @현재상태코드 <> 'RCP'
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 502; SET @오류항목 = N'업무ID';
                SET @결과메시지 = N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.';
            END
            ELSE IF @현재행버전 <> @행버전
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 601; SET @오류항목 = N'행버전';
                SET @결과메시지 = N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.';
            END

            IF @결과코드 = 0
                SELECT @결과코드 = s.[업무가능코드], @결과메시지 = s.[업무가능메시지]
                  FROM [dbo].[UFN_HC_일정확인](@서버시각, @오늘날짜, 'AM', 'NONE') s
                 WHERE s.[현재업무가능] = 0;
            IF @결과코드 IN (308, 309) BEGIN SET @성공여부 = 0; SET @오류항목 = NULL; END

            IF @결과코드 = 0
            BEGIN
                UPDATE [dbo].[예약접수]
                   SET [상태코드]     = 'CNC'
                     , [최종수정일시] = @저장시각
                 WHERE [업무ID]   = @업무ID
                   AND [상태코드] = 'RCP'
                   AND [행버전]   = @행버전;

                IF @@ROWCOUNT = 0
                BEGIN
                    SET @성공여부 = 0;
                    IF EXISTS (SELECT 1 FROM [dbo].[예약접수] WHERE [업무ID] = @업무ID AND [상태코드] <> 'RCP')
                    BEGIN
                        SET @결과코드 = 502; SET @오류항목 = N'업무ID';
                        SET @결과메시지 = N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.';
                    END
                    ELSE
                    BEGIN
                        SET @결과코드 = 601; SET @오류항목 = N'행버전';
                        SET @결과메시지 = N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.';
                    END
                END
                ELSE
                    SET @대상키 = @업무ID;
            END

            -- [X] RS1 을 COMMIT **뒤에** 다시 읽으면 그 사이 다른 세션이 바꾼 값이 나간다.
            --     호출자는 자기 것이 아닌 행버전 을 받고, 그 값으로 보낸 다음 요청이
            --     601 로 막혔어야 하는데 통과한다 — 낙관적 동시성의 유일한 방어선이 뚫린다.
            --     잠금 안에서 포획하고 COMMIT 뒤에는 변수를 낸다. RS 를 트랜잭션 밖에서
            --     낸다는 스펙 §21.1 [6] 은 그대로다 (06 §43-18).
            IF @결과코드 = 0
                SELECT @결과상태코드 = w.[상태코드], @결과행버전 = w.[행버전]
                  FROM [dbo].[예약접수] w WHERE w.[업무ID] = @업무ID;

            IF @결과코드 = 0 COMMIT TRANSACTION;
            ELSE ROLLBACK TRANSACTION;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    ----------------------------------------------------------------------------
    -- [6] Result Set
    ----------------------------------------------------------------------------
    SELECT
          CAST(@성공여부 AS BIT)               AS [성공여부]
        , CAST(@결과코드    AS INT)               AS [결과코드]
        , CAST(@결과메시지     AS NVARCHAR(300))     AS [결과메시지]
        , CAST(@오류항목   AS NVARCHAR(50))       AS [오류항목]
        , CAST(@서버시각 AS DATETIME2(7))   AS [서버시각];

    -- [X] 감사를 RS1 **뒤**에 두면, 클라이언트가 RS0 만 읽고 끊었을 때(ExecuteNonQuery·조기
    --     Dispose) 업무는 커밋됐는데 감사행만 없는 상태가 된다. 04 §8.6.5 ③ 은 "**해당**
    --     Result Set 의 SELECT 를 먼저 낸 뒤에" 로 **단수**이고, 같은 절의 목적은 §14 L1
    --     "기록이 업무 호출을 실패시키지 않는다" 이다. RS0 뒤에 두어도 그 목적은 그대로다.
    --     그래서 '해당 Result Set' 을 결과를 보고하는 RS0 로 읽고 감사를 RS0 뒤·RS1 앞에 둔다.
    --     감사는 여전히 @@TRANCOUNT = 0 지점 · 자체 TRY / 빈 CATCH 다 (06 §21.1 · §43-19).
    ----------------------------------------------------------------------------
    -- [7] 감사 기록
    ----------------------------------------------------------------------------
    IF @결과코드 = 0 AND @대상키 IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            VALUES
                (@저장시각, @조작자명, N'예약접수', @대상키, N'상태코드', N'RCP', N'CNC');
        END TRY
        BEGIN CATCH
        END CATCH
    END

    -- [6b] RS1 — 감사 **뒤**에 낸다. 순서가 뒤집히면 RS0 만 읽은 호출에서 감사가 사라진다 (§43-19).
    IF @결과코드 = 0
        SELECT
              [업무ID]     = CAST(@업무ID   AS BIGINT)
            , [상태코드]     = CAST(@결과상태코드 AS CHAR(3))
            , [행버전] = CAST(@결과행버전     AS BINARY(8));

END
GO
