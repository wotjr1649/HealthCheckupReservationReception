SET QUOTED_IDENTIFIER ON;   -- 01_Schema.sql 과 같은 설정으로 객체를 만든다 (CLAUDE.md §6)
GO
SET NOCOUNT ON;
GO
-- 예약 Write SP 3개 (05 §11). Write SP 공통 Template 은 스펙 §21.1 이다.
--   applock 전역 순서 SSN(1) → CHART(2) → PAT(3) → WORK(4) → SLOT(5) (스펙 §23).
--   SLOT 자원명이 HC|SLOT|yyyyMMdd|AM/PM 이라 문자열 오름차순 = (날짜, 시간대) 오름차순이고,
--   교차이동에서 양쪽이 같은 순서로 잡으므로 교착이 구조적으로 발생하지 않는다 (§23.1).
--
-- [!] 검사구성은 예약접수 행의 컬럼 2개다 (04 §8.2.2). 검사항목 Detail 테이블은 없다.
--     저장이 언제나 예약접수 한 행의 UPDATE/INSERT 이므로 Master/Detail 을 묶는 단계가 없다.
--     문자열 조립은 WHILE 루프로 한다 — STRING_AGG·FOR XML PATH 는 스펙 §9.2 허용목록 밖이고,
--     ORDER BY 를 곁들인 변수 누적 SELECT 는 순서가 보장되지 않는다.
--
-- [!] 저장 검사구성 무결성 3종(스펙 §21.2a)은 NEX 개수 8~11 · Master 역할 일치 · AEX 개수 0~6 이다.
--     AEX 상한 6 은 성별 제약에서 나온다 — OPT03·OPT07 은 여성 전용, OPT05 는 남성 전용이라
--     한 수검자가 7종을 모두 고를 수 없다 (04 §2.3 "NEX 11 + AEX 6 = 17행").
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_예약_등록]
    @수검자ID        BIGINT,
    @예약구분  VARCHAR(10),
    @예약일  DATE,
    @시간대코드         CHAR(2),
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
    DECLARE @잠금결과 INT, @자원수검자 NVARCHAR(255), @자원시간대 NVARCHAR(255);
    DECLARE @국가검사 NVARCHAR(100) = NULL, @추가검사 NVARCHAR(50) = NULL;
    DECLARE @코드문자열 NVARCHAR(200), @코드 VARCHAR(10);
    DECLARE @마감구분 VARCHAR(10);
    DECLARE @다른업무건수 INT, @국가검사건수 INT, @시간대인원 INT;
    DECLARE @코드목록 TABLE ([코드] VARCHAR(10) PRIMARY KEY);
    DECLARE @추가검사평가 TABLE ([추가검사코드] VARCHAR(10) PRIMARY KEY, [검사항목코드] VARCHAR(10),
                            [요청선택여부] BIT, [유효선택여부] BIT, [선택가능] BIT,
                            [사유코드] INT, [사유메시지] NVARCHAR(300));

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 정규화 → 필수값 → 허용값 → 조합 (05 §2.2 · §5)
    ----------------------------------------------------------------------------
    SET @예약구분 = NULLIF(UPPER(LTRIM(RTRIM(@예약구분))), '');
    SET @시간대코드        = NULLIF(UPPER(LTRIM(RTRIM(@시간대코드))), '');
    SET @조작자명    = NULLIF(LTRIM(RTRIM(@조작자명)), N'');

    IF @수검자ID IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'수검자ID'; END
    ELSE IF @예약구분 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'예약구분'; END
    ELSE IF @예약일 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'예약일'; END
    ELSE IF @시간대코드 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'시간대코드'; END
    -- AEX 7 BIT 는 NULL 을 허용하지 않으며 C# 이 매 호출마다 명시한다 (05 §2.1)
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
    ELSE IF @예약구분 NOT IN ('NORMAL', 'WALKIN')
    BEGIN SET @결과코드 = 101; SET @오류항목 = N'예약구분'; END
    ELSE IF @시간대코드 NOT IN ('AM', 'PM')
    BEGIN SET @결과코드 = 101; SET @오류항목 = N'시간대코드'; END
    -- WalkIn 은 DB 오늘날짜 만 허용한다 (05 §11.1)
    ELSE IF @예약구분 = 'WALKIN' AND @예약일 <> @오늘날짜
    BEGIN SET @결과코드 = 102; SET @오류항목 = N'예약일'; END

    IF @결과코드 = 100 SET @결과메시지 = N'필수값을 입력하십시오.';
    IF @결과코드 = 101 SET @결과메시지 = N'입력값이 올바르지 않습니다.';
    IF @결과코드 = 102 SET @결과메시지 = N'함께 사용할 수 없는 입력값 조합입니다.';
    IF @결과코드 <> 0 SET @성공여부 = 0;

    ----------------------------------------------------------------------------
    -- [3]~[5] Transaction. 자원명은 전부 입력값으로 만들 수 있어 사전조회가 없다.
    ----------------------------------------------------------------------------
    IF @결과코드 = 0
    BEGIN
        SET @자원수검자  = N'HC|PAT|' + CONVERT(NVARCHAR(20), @수검자ID);
        SET @자원시간대 = N'HC|SLOT|' + CONVERT(CHAR(8), @예약일, 112) + N'|' + @시간대코드;
        SET @마감구분 = CASE WHEN @예약구분 = 'WALKIN' THEN 'RECEPTION' ELSE 'NORMAL' END;

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

            EXEC @잠금결과 = sp_getapplock @Resource = @자원시간대, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
            IF @잠금결과 < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            -- [4] 재검증 — 05 §11.1 검증순서. 조회 SP 결과를 신뢰하지 않고 전부 다시 본다.
            -- 4-1 Patient 존재
            IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [수검자ID] = @수검자ID)
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 200; SET @오류항목 = N'수검자ID';
                SET @결과메시지 = N'수검자를 찾을 수 없습니다.';
            END

            -- 4-2 검사 Master 구성
            IF @결과코드 = 0
               AND ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [국가검사규칙코드] IS NOT NULL) < 8
                 OR (SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [추가검사코드] IS NOT NULL) <> 7)
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 700; SET @오류항목 = NULL;
                SET @결과메시지 = N'검사 Master 구성이 올바르지 않습니다.';
            END

            -- 4-3 현재 공통 업무 가능
            IF @결과코드 = 0
                SELECT @결과코드 = s.[업무가능코드], @결과메시지 = s.[업무가능메시지]
                  FROM [dbo].[UFN_HC_일정확인](@서버시각, @오늘날짜, 'AM', 'NONE') s
                 WHERE s.[현재업무가능] = 0;
            IF @결과코드 IN (308, 309) BEGIN SET @성공여부 = 0; SET @오류항목 = NULL; END

            -- 4-4 다른 유효업무 (스펙 §30). 2건 이상은 RP-06 불변조건이 이미 깨진 상태다.
            IF @결과코드 = 0
            BEGIN
                SET @다른업무건수 = (SELECT COUNT(*) FROM [dbo].[예약접수]
                                  WHERE [수검자ID] = @수검자ID
                                    AND [예약일] >= @오늘날짜
                                    AND [상태코드] IN ('RSV', 'RCP'));
                IF @다른업무건수 >= 2
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 701; SET @오류항목 = NULL;
                    SET @결과메시지 = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
                END
                ELSE IF @다른업무건수 = 1
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 306; SET @오류항목 = N'수검자ID';
                    SET @결과메시지 = N'수검자에게 다른 유효 예약 또는 접수 업무가 있습니다.';
                END
            END

            -- 4-5 요청 일정·시간대 운영·마감 (300~304)
            IF @결과코드 = 0
                SELECT @결과코드 = s.[사유코드], @결과메시지 = s.[사유메시지]
                  FROM [dbo].[UFN_HC_일정확인](@서버시각, @예약일, @시간대코드, @마감구분) s
                 WHERE s.[일정가능] = 0;
            IF @결과코드 BETWEEN 300 AND 304
            BEGIN
                SET @성공여부 = 0;
                SET @오류항목 = CASE WHEN @결과코드 IN (300, 301, 302) THEN N'예약일' ELSE N'시간대코드' END;
            END

            -- 4-6 정원 (00 RP-03). 정원 20 고정 (05 §9.7)
            IF @결과코드 = 0
            BEGIN
                SET @시간대인원 = (SELECT COUNT(*) FROM [dbo].[예약접수]
                                 WHERE [예약일] = @예약일
                                   AND [시간대코드] = @시간대코드
                                   AND [상태코드] IN ('RSV', 'RCP'));
                IF @시간대인원 + 1 > 20
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 305; SET @오류항목 = N'시간대코드';
                    SET @결과메시지 = N'해당 시간대의 예약 정원이 마감되었습니다.';
                END
            END

            -- 4-7 TGT (400/401)
            IF @결과코드 = 0
                SELECT @결과코드 = g.[사유코드], @결과메시지 = g.[사유메시지]
                  FROM [dbo].[UFN_HC_검진대상확인](@수검자ID, @예약일) g
                 WHERE g.[검진대상여부] = 0;
            IF @결과코드 IN (400, 401) BEGIN SET @성공여부 = 0; SET @오류항목 = N'예약일'; END

            -- 4-8 NEX 개수 (스펙 §21.2a-(1)). 상한 11 을 버리면 TVF·Master 손상으로
            --     12행이 나와도 저장이 계속된다 (05 §17.4).
            IF @결과코드 = 0
            BEGIN
                SET @국가검사건수 = (SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@수검자ID, @예약일));
                IF @국가검사건수 NOT BETWEEN 8 AND 11
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 701; SET @오류항목 = NULL;
                    SET @결과메시지 = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
                END
            END

            -- 4-9 AEX. 요청선택여부=1 인데 선택가능=0 인 항목만 저장을 막는다 (05 §6.4.3).
            IF @결과코드 = 0
            BEGIN
                INSERT INTO @추가검사평가 ([추가검사코드], [검사항목코드], [요청선택여부], [유효선택여부], [선택가능], [사유코드], [사유메시지])
                SELECT x.[추가검사코드], x.[검사항목코드], x.[요청선택여부], x.[유효선택여부], x.[선택가능], x.[사유코드], x.[사유메시지]
                  FROM [dbo].[UFN_HC_추가검사확인](@수검자ID, @예약일, NULL, 0,
                         @추가검사01선택여부, @추가검사02선택여부, @추가검사03선택여부, @추가검사04선택여부,
                         @추가검사05선택여부, @추가검사06선택여부, @추가검사07선택여부) x;

                SELECT TOP (1)
                       @결과코드 = a.[사유코드]
                     , @결과메시지  = a.[사유메시지]
                     , @오류항목 = N'추가검사' + RIGHT(a.[추가검사코드], 2) + N'선택여부'
                  FROM @추가검사평가 a
                 WHERE a.[요청선택여부] = 1 AND a.[선택가능] = 0
                 ORDER BY a.[추가검사코드];
                IF @결과코드 <> 0 SET @성공여부 = 0;
            END

            -- [5] 저장. 검사구성 문자열은 검사항목코드 오름차순 쉼표 연결이다 (04 §8.2.2).
            IF @결과코드 = 0
            BEGIN
                DELETE FROM @코드목록;
                INSERT INTO @코드목록 ([코드])
                SELECT n.[검사항목코드] FROM [dbo].[UFN_HC_국가검사구성](@수검자ID, @예약일) n;
                SET @코드문자열 = N'';
                WHILE EXISTS (SELECT 1 FROM @코드목록)
                BEGIN
                    SELECT TOP (1) @코드 = [코드] FROM @코드목록 ORDER BY [코드];
                    SET @코드문자열 = @코드문자열 + @코드 + N',';
                    DELETE FROM @코드목록 WHERE [코드] = @코드;
                END
                SET @국가검사 = LEFT(@코드문자열, LEN(@코드문자열) - 1);   -- 4-8 이 8~11행을 보장해 비어 있지 않다

                DELETE FROM @코드목록;
                INSERT INTO @코드목록 ([코드]) SELECT a.[검사항목코드] FROM @추가검사평가 a WHERE a.[유효선택여부] = 1;
                SET @코드문자열 = N'';
                WHILE EXISTS (SELECT 1 FROM @코드목록)
                BEGIN
                    SELECT TOP (1) @코드 = [코드] FROM @코드목록 ORDER BY [코드];
                    SET @코드문자열 = @코드문자열 + @코드 + N',';
                    DELETE FROM @코드목록 WHERE [코드] = @코드;
                END
                -- 추가검사 0개는 NULL 이다. 빈 문자열은 국가검사항목에서만 손상을 뜻한다 (04 §8.2.2).
                SET @추가검사 = CASE WHEN LEN(@코드문자열) = 0 THEN NULL ELSE LEFT(@코드문자열, LEN(@코드문자열) - 1) END;

                INSERT INTO [dbo].[예약접수]
                    ([수검자ID], [예약일], [시간대코드], [상태코드],
                     [국가검사항목], [추가검사항목], [생성일시], [최종수정일시])
                VALUES
                    (@수검자ID, @예약일, @시간대코드, 'RSV',
                     @국가검사, @추가검사, @저장시각, @저장시각);

                SET @대상키 = SCOPE_IDENTITY();
                -- [X] RS1 을 COMMIT **뒤에** 다시 읽으면 그 사이 다른 세션이 바꾼 값이 나간다.
                --     호출자는 자기 것이 아닌 행버전 을 받고, 그 값으로 보낸 다음 요청이
                --     601 로 막혔어야 하는데 통과한다 — 낙관적 동시성의 유일한 방어선이 뚫린다.
                --     잠금 안에서 포획하고 COMMIT 뒤에는 변수를 낸다. RS 를 트랜잭션 밖에서
                --     낸다는 스펙 §21.1 [6] 은 그대로다 (06 §43-18).
                IF @결과코드 = 0
                    SELECT @결과상태코드 = w.[상태코드], @결과행버전 = w.[행버전]
                      FROM [dbo].[예약접수] w WHERE w.[업무ID] = @대상키;

                COMMIT TRANSACTION;
            END
            ELSE
                ROLLBACK TRANSACTION;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH
    END

    ----------------------------------------------------------------------------
    -- [6] Transaction 이 닫힌 뒤에만 Result Set 을 낸다
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
    -- [7] 감사 기록 (04 §8.6.5). INSERT 라 변경전은 항상 NULL 이다.
    ----------------------------------------------------------------------------
    IF @결과코드 = 0 AND @대상키 IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            SELECT @저장시각, @조작자명, N'예약접수', @대상키, V.[컬럼명], NULL, V.[변경후]
              FROM (VALUES
                      (N'수검자ID',     CONVERT(NVARCHAR(4000), @수검자ID))
                    , (N'예약일',       CONVERT(NVARCHAR(4000), @예약일, 23))
                    , (N'시간대코드',   CONVERT(NVARCHAR(4000), @시간대코드))
                    , (N'상태코드',     CONVERT(NVARCHAR(4000), 'RSV'))
                    , (N'국가검사항목', CONVERT(NVARCHAR(4000), @국가검사))
                    , (N'추가검사항목', CONVERT(NVARCHAR(4000), @추가검사))
                   ) V([컬럼명], [변경후])
             WHERE V.[변경후] IS NOT NULL;
        END TRY
        BEGIN CATCH
        END CATCH
    END

    -- [6b] RS1 — 감사 **뒤**에 낸다. 순서가 뒤집히면 RS0 만 읽은 호출에서 감사가 사라진다 (§43-19).
    -- RS1 Work결과 (05 §11). 행버전 은 COMMIT 이후 다시 읽는다.
    IF @결과코드 = 0
        SELECT
              [업무ID]     = CAST(@대상키   AS BIGINT)
            , [상태코드]     = CAST(@결과상태코드 AS CHAR(3))
            , [행버전] = CAST(@결과행버전     AS BINARY(8));

END
GO
-- 05 §11.2. Phase 4 에서 가장 복잡한 Write SP 다.
--   C# 이 전달한 변경구분을 신뢰하지 않고 DB 현재값과 요청값을 비교해 변경범위 를 직접 계산한다 (스펙 §29).
--   영향받는 Rule 만 재검증한다 — 시간대만 바뀌면 TGT/NEX/AEX 를 재평가하지도 재조립하지도 않는다.
--
-- [!] 현재 예약일·시간대는 WORK 잠금을 잡은 뒤에 읽는다. 사전조회값으로 SLOT 자원명을 만들면
--     다른 세션이 그 사이에 Work 를 옮겼을 때 엉뚱한 시간대 을 잠그고 정원 판정이 경합에 노출된다.
--     읽기는 잠금 획득이 아니므로 전역 순서 PAT(3) → WORK(4) → SLOT(5) 는 그대로 지켜진다.
--
-- [!] 501 WrongPatient 를 쓰지 않는다. 05 §13 의 허용 결과코드 에 501 이 없고, 이 SP 는 @수검자ID
--     Parameter 자체가 없어 "요청 수검자와 다르다" 를 판정할 외부 입력이 존재하지 않는다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_예약_변경]
    @업무ID           BIGINT,
    @행버전       BINARY(8),
    @예약일  DATE,
    @시간대코드         CHAR(2),
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
    DECLARE @잠금결과 INT, @자원수검자 NVARCHAR(255), @자원업무 NVARCHAR(255);
    DECLARE @자원A NVARCHAR(255), @자원B NVARCHAR(255), @자원임시 NVARCHAR(255);

    DECLARE @수검자ID BIGINT = NULL, @현재수검자ID BIGINT = NULL;
    DECLARE @현재예약일 DATE, @현재시간대코드 CHAR(2), @현재상태코드 CHAR(3), @현재행버전 BINARY(8);
    DECLARE @현재국가검사 NVARCHAR(100), @현재추가검사 NVARCHAR(50);
    DECLARE @새국가검사 NVARCHAR(100) = NULL, @새추가검사 NVARCHAR(50) = NULL;

    DECLARE @예약일변경여부 BIT = 0, @시간대변경여부 BIT = 0, @추가검사변경여부 BIT = 0;
    DECLARE @저장국가검사건수 INT, @저장추가검사건수 INT, @다른업무건수 INT, @시간대인원 INT, @국가검사건수 INT;
    DECLARE @코드문자열 NVARCHAR(200), @코드 VARCHAR(10);
    DECLARE @코드목록 TABLE ([코드] VARCHAR(10) PRIMARY KEY);
    DECLARE @요청목록 TABLE ([추가검사코드] VARCHAR(10) PRIMARY KEY);
    DECLARE @추가검사평가 TABLE ([추가검사코드] VARCHAR(10) PRIMARY KEY, [검사항목코드] VARCHAR(10),
                            [요청선택여부] BIT, [유효선택여부] BIT, [선택가능] BIT,
                            [사유코드] INT, [사유메시지] NVARCHAR(300));

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 정규화 → 필수값 → 허용값
    ----------------------------------------------------------------------------
    SET @시간대코드     = NULLIF(UPPER(LTRIM(RTRIM(@시간대코드))), '');
    SET @조작자명 = NULLIF(LTRIM(RTRIM(@조작자명)), N'');

    IF @업무ID IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'업무ID'; END
    ELSE IF @행버전 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'행버전'; END
    ELSE IF @예약일 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'예약일'; END
    ELSE IF @시간대코드 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'시간대코드'; END
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
    ELSE IF @시간대코드 NOT IN ('AM', 'PM')
    BEGIN SET @결과코드 = 101; SET @오류항목 = N'시간대코드'; END

    IF @결과코드 = 100 SET @결과메시지 = N'필수값을 입력하십시오.';
    IF @결과코드 = 101 SET @결과메시지 = N'입력값이 올바르지 않습니다.';

    ----------------------------------------------------------------------------
    -- [2] Transaction 밖 : PAT 자원명을 만들 수검자ID 확보 (스펙 §23.2).
    --     stale 할 수 있으나 잠금 후 재검증이 최종 판정이다. EP-02 로 수검자ID 는 불변이다.
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
    -- [3]~[5] Transaction
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
            -- EP-02 로 수검자ID 는 불변이고 어떤 SP 도 이 컬럼을 UPDATE 하지 않는다.
            -- 그래도 달라졌다면 PAT 잠금을 엉뚱한 자원에 걸었다는 뜻이므로 손상으로 본다.
            ELSE IF @현재수검자ID <> @수검자ID
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 701; SET @오류항목 = N'업무ID';
                SET @결과메시지 = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
            END

            IF @결과코드 = 0
            BEGIN
                -- SLOT 자원 최대 2개를 자원명 오름차순으로 잡는다 (스펙 §23.1).
                -- CURSOR 를 쓰지 않는다 — 허용목록 밖이고, THROW 시 CLOSE/DEALLOCATE 가 남는다.
                -- [X] @현재시간대코드 은 DB 에서 읽은 값이라 정규화를 거치지 않았다. CK 의 IN 목록이 CI 라
                --     'am' 행이 존재할 수 있고(실측), 그러면 …|am 과 …|AM 이 다른 자원이 되어
                --     정원 직렬화가 통째로 사라진다. 요청값 @시간대코드 은 :57 에서 이미 UPPER 다.
                SET @자원A = N'HC|SLOT|' + CONVERT(CHAR(8), @현재예약일, 112) + N'|' + UPPER(@현재시간대코드);
                SET @자원B = N'HC|SLOT|' + CONVERT(CHAR(8), @예약일, 112) + N'|' + @시간대코드;
                IF @자원A = @자원B SET @자원B = NULL;
                IF @자원B IS NOT NULL AND @자원B < @자원A
                BEGIN SET @자원임시 = @자원A; SET @자원A = @자원B; SET @자원B = @자원임시; END

                EXEC @잠금결과 = sp_getapplock @Resource = @자원A, @LockMode = 'Exclusive',
                                         @LockOwner = 'Transaction', @LockTimeout = 5000;
                PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
                IF @잠금결과 < 0
                BEGIN
                    ROLLBACK TRANSACTION;
                    IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                    THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
                END

                IF @자원B IS NOT NULL
                BEGIN
                    EXEC @잠금결과 = sp_getapplock @Resource = @자원B, @LockMode = 'Exclusive',
                                             @LockOwner = 'Transaction', @LockTimeout = 5000;
                    PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
                    IF @잠금결과 < 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                        THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
                    END
                END

                -- [4] 재검증 — 05 §11.2 검증순서
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

            -- 변경범위 계산 (스펙 §29). 추가검사변경여부 는 §28.1 대로 EXCEPT 양방향이다.
            IF @결과코드 = 0
            BEGIN
                INSERT INTO @요청목록 ([추가검사코드])
                SELECT v.c FROM (VALUES ('OPT01',@추가검사01선택여부),('OPT02',@추가검사02선택여부),
                                        ('OPT03',@추가검사03선택여부),('OPT04',@추가검사04선택여부),
                                        ('OPT05',@추가검사05선택여부),('OPT06',@추가검사06선택여부),
                                        ('OPT07',@추가검사07선택여부)) v(c, b)
                 WHERE v.b = 1;

                SET @예약일변경여부 = CASE WHEN @현재예약일 <> @예약일 THEN 1 ELSE 0 END;
                SET @시간대변경여부 = CASE WHEN @현재시간대코드 <> @시간대코드 THEN 1 ELSE 0 END;
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

                -- No-op 은 Transaction·잠금 안에서 판정한다 (스펙 §28)
                IF @예약일변경여부 = 0 AND @시간대변경여부 = 0 AND @추가검사변경여부 = 0
                BEGIN
                    SET @성공여부 = 1; SET @결과코드 = 1; SET @오류항목 = NULL;
                    SET @결과메시지 = N'변경된 내용이 없습니다.';
                END
            END

            -- 검사 Master 구성 (700). 검사구성을 건드리는 변경범위 에서만 본다.
            IF @결과코드 = 0 AND (@예약일변경여부 = 1 OR @추가검사변경여부 = 1)
               AND ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [국가검사규칙코드] IS NOT NULL) < 8
                 OR (SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [추가검사코드] IS NOT NULL) <> 7)
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 700; SET @오류항목 = NULL;
                SET @결과메시지 = N'검사 Master 구성이 올바르지 않습니다.';
            END

            -- 저장 검사구성 무결성 3종 (스펙 §21.2a). 빈 문자열은 0개다 —
            -- LEN - LEN(REPLACE) + 1 만 쓰면 빈 문자열을 1개로 세어 CORRUPT-2 를 놓친다.
            IF @결과코드 = 0 AND (@예약일변경여부 = 1 OR @추가검사변경여부 = 1)
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

            -- 현재 공통 업무 가능 (실제 변경이 있을 때만)
            IF @결과코드 = 0
                SELECT @결과코드 = s.[업무가능코드], @결과메시지 = s.[업무가능메시지]
                  FROM [dbo].[UFN_HC_일정확인](@서버시각, @오늘날짜, 'AM', 'NONE') s
                 WHERE s.[현재업무가능] = 0;
            IF @결과코드 IN (308, 309) BEGIN SET @성공여부 = 0; SET @오류항목 = NULL; END

            -- 일정·중복·정원 — 예약일 또는 시간대가 바뀔 때만 (스펙 §29)
            IF @결과코드 = 0 AND (@예약일변경여부 = 1 OR @시간대변경여부 = 1)
            BEGIN
                -- 다른 유효업무. 현재 Work 제외가 필수다 (스펙 §30) — 빠지면 자기 자신을 306 으로 오인한다.
                SET @다른업무건수 = (SELECT COUNT(*) FROM [dbo].[예약접수]
                                  WHERE [수검자ID] = @수검자ID
                                    AND [예약일] >= @오늘날짜
                                    AND [상태코드] IN ('RSV', 'RCP')
                                    AND [업무ID] <> @업무ID);
                IF @다른업무건수 >= 2
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 701; SET @오류항목 = N'업무ID';
                    SET @결과메시지 = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
                END
                ELSE IF @다른업무건수 = 1
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 306; SET @오류항목 = N'수검자ID';
                    SET @결과메시지 = N'수검자에게 다른 유효 예약 또는 접수 업무가 있습니다.';
                END

                IF @결과코드 = 0
                    SELECT @결과코드 = s.[사유코드], @결과메시지 = s.[사유메시지]
                      FROM [dbo].[UFN_HC_일정확인](@서버시각, @예약일, @시간대코드, 'NORMAL') s
                     WHERE s.[일정가능] = 0;
                IF @결과코드 BETWEEN 300 AND 304
                BEGIN
                    SET @성공여부 = 0;
                    SET @오류항목 = CASE WHEN @결과코드 IN (300, 301, 302) THEN N'예약일' ELSE N'시간대코드' END;
                END

                -- 정원. 현재 Work 를 COUNT 에서 빼고 +1 한다 (스펙 §30.1) —
                -- 20/20 시간대 을 그대로 유지하는 변경을 21 로 계산하지 않기 위해서다.
                IF @결과코드 = 0
                BEGIN
                    SET @시간대인원 = (SELECT COUNT(*) FROM [dbo].[예약접수]
                                     WHERE [예약일] = @예약일
                                       AND [시간대코드] = @시간대코드
                                       AND [상태코드] IN ('RSV', 'RCP')
                                       AND [업무ID] <> @업무ID);
                    IF @시간대인원 + 1 > 20
                    BEGIN
                        SET @성공여부 = 0; SET @결과코드 = 305; SET @오류항목 = N'시간대코드';
                        SET @결과메시지 = N'해당 시간대의 예약 정원이 마감되었습니다.';
                    END
                END
            END

            -- TGT / NEX / AEX — 예약일이 바뀔 때만 전량 재판정한다
            IF @결과코드 = 0 AND @예약일변경여부 = 1
            BEGIN
                SELECT @결과코드 = g.[사유코드], @결과메시지 = g.[사유메시지]
                  FROM [dbo].[UFN_HC_검진대상확인](@수검자ID, @예약일) g
                 WHERE g.[검진대상여부] = 0;
                IF @결과코드 IN (400, 401) BEGIN SET @성공여부 = 0; SET @오류항목 = N'예약일'; END

                IF @결과코드 = 0
                BEGIN
                    SET @국가검사건수 = (SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@수검자ID, @예약일));
                    IF @국가검사건수 NOT BETWEEN 8 AND 11
                    BEGIN
                        SET @성공여부 = 0; SET @결과코드 = 701; SET @오류항목 = N'업무ID';
                        SET @결과메시지 = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
                    END
                END
            END

            -- AEX 평가. 예약일이 바뀌면 **변경 후** 예약일 기준 NEX 로 중복을 판정한다 —
            -- 변경 전 구성으로 판정하면 나이 경계를 넘는 이동에서 412 를 놓친다 (05 §17.7).
            IF @결과코드 = 0 AND (@예약일변경여부 = 1 OR @추가검사변경여부 = 1)
            BEGIN
                INSERT INTO @추가검사평가 ([추가검사코드], [검사항목코드], [요청선택여부], [유효선택여부], [선택가능], [사유코드], [사유메시지])
                SELECT x.[추가검사코드], x.[검사항목코드], x.[요청선택여부], x.[유효선택여부], x.[선택가능], x.[사유코드], x.[사유메시지]
                  FROM [dbo].[UFN_HC_추가검사확인](@수검자ID
                         , CASE WHEN @예약일변경여부 = 1 THEN @예약일 ELSE @현재예약일 END
                         , @업무ID
                         , CASE WHEN @예약일변경여부 = 1 THEN 0 ELSE 1 END
                         , @추가검사01선택여부, @추가검사02선택여부, @추가검사03선택여부, @추가검사04선택여부
                         , @추가검사05선택여부, @추가검사06선택여부, @추가검사07선택여부) x;

                SELECT TOP (1)
                       @결과코드  = a.[사유코드]
                     , @결과메시지   = a.[사유메시지]
                     , @오류항목 = N'추가검사' + RIGHT(a.[추가검사코드], 2) + N'선택여부'
                  FROM @추가검사평가 a
                 WHERE a.[요청선택여부] = 1 AND a.[선택가능] = 0
                 ORDER BY a.[추가검사코드];
                IF @결과코드 <> 0 SET @성공여부 = 0;
            END

            -- [5] 저장. 언제나 예약접수 한 행의 UPDATE 다 (05 §11.2).
            IF @결과코드 = 0
            BEGIN
                IF @예약일변경여부 = 1
                BEGIN
                    DELETE FROM @코드목록;
                    INSERT INTO @코드목록 ([코드])
                    SELECT n.[검사항목코드] FROM [dbo].[UFN_HC_국가검사구성](@수검자ID, @예약일) n;
                    SET @코드문자열 = N'';
                    WHILE EXISTS (SELECT 1 FROM @코드목록)
                    BEGIN
                        SELECT TOP (1) @코드 = [코드] FROM @코드목록 ORDER BY [코드];
                        SET @코드문자열 = @코드문자열 + @코드 + N',';
                        DELETE FROM @코드목록 WHERE [코드] = @코드;
                    END
                    SET @새국가검사 = LEFT(@코드문자열, LEN(@코드문자열) - 1);
                END

                IF @예약일변경여부 = 1 OR @추가검사변경여부 = 1
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
                END

                -- 조건부 UPDATE 표준형 (스펙 §24.4). 변경범위 밖 컬럼은 현재값을 그대로 둔다.
                UPDATE [dbo].[예약접수]
                   SET [예약일]       = CASE WHEN @예약일변경여부 = 1 THEN @예약일 ELSE [예약일] END
                     , [시간대코드]   = CASE WHEN @시간대변경여부 = 1 THEN @시간대코드 ELSE [시간대코드] END
                     , [국가검사항목] = CASE WHEN @예약일변경여부 = 1 THEN @새국가검사 ELSE [국가검사항목] END
                     , [추가검사항목] = CASE WHEN @예약일변경여부 = 1 OR @추가검사변경여부 = 1
                                            THEN @새추가검사 ELSE [추가검사항목] END
                     , [최종수정일시] = @저장시각
                 WHERE [업무ID]   = @업무ID
                   AND [상태코드] = 'RSV'
                   AND [행버전]   = @행버전;

                IF @@ROWCOUNT = 0
                BEGIN
                    -- WORK 잠금을 쥐고 있어 도달할 수 없다. 상태가 동시성값보다 우선이다 (05 §5).
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
    -- [6] Transaction 이 닫힌 뒤에만 Result Set 을 낸다
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
    -- [7] 감사 기록. 실제로 값이 바뀐 컬럼만 남는다 (00 CP-06).
    ----------------------------------------------------------------------------
    IF @결과코드 = 0 AND @대상키 IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            SELECT @저장시각, @조작자명, N'예약접수', @대상키, V.[컬럼명], V.[변경전], V.[변경후]
              FROM (VALUES
                      (N'예약일',       CONVERT(NVARCHAR(4000), @현재예약일, 23)
                                      , CONVERT(NVARCHAR(4000), CASE WHEN @예약일변경여부 = 1 THEN @예약일 ELSE @현재예약일 END, 23))
                    , (N'시간대코드',   CONVERT(NVARCHAR(4000), @현재시간대코드)
                                      , CONVERT(NVARCHAR(4000), CASE WHEN @시간대변경여부 = 1 THEN @시간대코드 ELSE @현재시간대코드 END))
                    , (N'국가검사항목', CONVERT(NVARCHAR(4000), @현재국가검사)
                                      , CONVERT(NVARCHAR(4000), CASE WHEN @예약일변경여부 = 1 THEN @새국가검사 ELSE @현재국가검사 END))
                    , (N'추가검사항목', CONVERT(NVARCHAR(4000), @현재추가검사)
                                      , CONVERT(NVARCHAR(4000), CASE WHEN @예약일변경여부 = 1 OR @추가검사변경여부 = 1
                                                                     THEN @새추가검사 ELSE @현재추가검사 END))
                   ) V([컬럼명], [변경전], [변경후])
             WHERE ISNULL(V.[변경전], N'~NULL~') <> ISNULL(V.[변경후], N'~NULL~');
        END TRY
        BEGIN CATCH
        END CATCH
    END

    -- [6b] RS1 — 감사 **뒤**에 낸다. 순서가 뒤집히면 RS0 만 읽은 호출에서 감사가 사라진다 (§43-19).
    -- No-op 은 갱신하지 않은 기존 행버전 이 그대로 나온다 (05 §11.2).
    IF @결과코드 IN (0, 1)
        SELECT
              [업무ID]     = CAST(@업무ID   AS BIGINT)
            , [상태코드]     = CAST(@결과상태코드 AS CHAR(3))
            , [행버전] = CAST(@결과행버전     AS BINARY(8));

END
GO
-- 05 §11.3. RSV → CNR. 검사구성 두 컬럼은 지우지 않고, 마감시각은 취소 가능조건이 아니다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_예약_취소]
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
    -- [1] Transaction 밖 : 필수값. 허용 결과코드 는 0, 100, 500, 502, 601, 308~309 뿐이다.
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
    -- [3]~[5] Transaction. WORK 자원명이 입력값이라 사전조회가 없다 (스펙 §24.1).
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
            ELSE IF @현재상태코드 <> 'RSV'
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
                -- 조건부 UPDATE 표준형 (스펙 §24.4). 검사구성 두 컬럼은 건드리지 않는다.
                UPDATE [dbo].[예약접수]
                   SET [상태코드]     = 'CNR'
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
                (@저장시각, @조작자명, N'예약접수', @대상키, N'상태코드', N'RSV', N'CNR');
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
