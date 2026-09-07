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
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_INSERT_예약]
    @PatientId        BIGINT,
    @ReservationType  VARCHAR(10),
    @ReservationDate  DATE,
    @TimeSlot         CHAR(2),
    @AexOpt01Selected BIT,
    @AexOpt02Selected BIT,
    @AexOpt03Selected BIT,
    @AexOpt04Selected BIT,
    @AexOpt05Selected BIT,
    @AexOpt06Selected BIT,
    @AexOpt07Selected BIT,
    @OperatorName     NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
    DECLARE @Today      DATE         = CONVERT(DATE, @ServerTime);
    DECLARE @StoredNow  DATETIME2(0) = CONVERT(DATETIME2(0), @ServerTime);

    DECLARE @Success BIT = 1, @Code INT = 0, @Field VARCHAR(50) = NULL;
    DECLARE @Msg NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @TargetKey BIGINT = NULL;
    DECLARE @rc INT, @ResPat NVARCHAR(255), @ResSlot NVARCHAR(255);
    DECLARE @Nex NVARCHAR(100) = NULL, @Aex NVARCHAR(50) = NULL;
    DECLARE @Csv NVARCHAR(200), @C VARCHAR(10);
    DECLARE @CutoffType VARCHAR(10);
    DECLARE @OtherCnt INT, @NexCnt INT, @SlotCnt INT;
    DECLARE @Codes TABLE (C VARCHAR(10) PRIMARY KEY);
    DECLARE @AexEval TABLE (OptionCode VARCHAR(10) PRIMARY KEY, ExamCode VARCHAR(10),
                            Requested BIT, Selected BIT, CanSelect BIT,
                            ReasonCode INT, ReasonMessage NVARCHAR(300));

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 정규화 → 필수값 → 허용값 → 조합 (05 §2.2 · §5)
    ----------------------------------------------------------------------------
    SET @ReservationType = NULLIF(UPPER(LTRIM(RTRIM(@ReservationType))), '');
    SET @TimeSlot        = NULLIF(UPPER(LTRIM(RTRIM(@TimeSlot))), '');
    SET @OperatorName    = NULLIF(LTRIM(RTRIM(@OperatorName)), N'');

    IF @PatientId IS NULL
    BEGIN SET @Code = 100; SET @Field = 'PatientId'; END
    ELSE IF @ReservationType IS NULL
    BEGIN SET @Code = 100; SET @Field = 'ReservationType'; END
    ELSE IF @ReservationDate IS NULL
    BEGIN SET @Code = 100; SET @Field = 'ReservationDate'; END
    ELSE IF @TimeSlot IS NULL
    BEGIN SET @Code = 100; SET @Field = 'TimeSlot'; END
    -- AEX 7 BIT 는 NULL 을 허용하지 않으며 C# 이 매 호출마다 명시한다 (05 §2.1)
    ELSE IF @AexOpt01Selected IS NULL OR @AexOpt02Selected IS NULL OR @AexOpt03Selected IS NULL
         OR @AexOpt04Selected IS NULL OR @AexOpt05Selected IS NULL OR @AexOpt06Selected IS NULL
         OR @AexOpt07Selected IS NULL
    BEGIN SET @Code = 100; SET @Field = CASE WHEN @AexOpt01Selected IS NULL THEN 'AexOpt01Selected'
                  WHEN @AexOpt02Selected IS NULL THEN 'AexOpt02Selected'
                  WHEN @AexOpt03Selected IS NULL THEN 'AexOpt03Selected'
                  WHEN @AexOpt04Selected IS NULL THEN 'AexOpt04Selected'
                  WHEN @AexOpt05Selected IS NULL THEN 'AexOpt05Selected'
                  WHEN @AexOpt06Selected IS NULL THEN 'AexOpt06Selected'
                  ELSE 'AexOpt07Selected' END; END
    ELSE IF @OperatorName IS NULL
    BEGIN SET @Code = 100; SET @Field = 'OperatorName'; END
    ELSE IF @ReservationType NOT IN ('NORMAL', 'WALKIN')
    BEGIN SET @Code = 101; SET @Field = 'ReservationType'; END
    ELSE IF @TimeSlot NOT IN ('AM', 'PM')
    BEGIN SET @Code = 101; SET @Field = 'TimeSlot'; END
    -- WalkIn 은 DB Today 만 허용한다 (05 §11.1)
    ELSE IF @ReservationType = 'WALKIN' AND @ReservationDate <> @Today
    BEGIN SET @Code = 102; SET @Field = 'ReservationDate'; END

    IF @Code = 100 SET @Msg = N'필수값을 입력하십시오.';
    IF @Code = 101 SET @Msg = N'입력값이 올바르지 않습니다.';
    IF @Code = 102 SET @Msg = N'함께 사용할 수 없는 입력값 조합입니다.';
    IF @Code <> 0 SET @Success = 0;

    ----------------------------------------------------------------------------
    -- [3]~[5] Transaction. 자원명은 전부 입력값으로 만들 수 있어 사전조회가 없다.
    ----------------------------------------------------------------------------
    IF @Code = 0
    BEGIN
        SET @ResPat  = N'HC|PAT|' + CONVERT(NVARCHAR(20), @PatientId);
        SET @ResSlot = N'HC|SLOT|' + CONVERT(CHAR(8), @ReservationDate, 112) + N'|' + @TimeSlot;
        SET @CutoffType = CASE WHEN @ReservationType = 'WALKIN' THEN 'RECEPTION' ELSE 'NORMAL' END;

        BEGIN TRY
            BEGIN TRANSACTION;

            EXEC @rc = sp_getapplock @Resource = @ResPat, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
            IF @rc < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            EXEC @rc = sp_getapplock @Resource = @ResSlot, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
            IF @rc < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            -- [4] 재검증 — 05 §11.1 검증순서. 조회 SP 결과를 신뢰하지 않고 전부 다시 본다.
            -- 4-1 Patient 존재
            IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [수검자ID] = @PatientId)
            BEGIN
                SET @Success = 0; SET @Code = 200; SET @Field = 'PatientId';
                SET @Msg = N'수검자를 찾을 수 없습니다.';
            END

            -- 4-2 검사 Master 구성
            IF @Code = 0
               AND ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [국가검사규칙코드] IS NOT NULL) < 8
                 OR (SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [추가검사코드] IS NOT NULL) <> 7)
            BEGIN
                SET @Success = 0; SET @Code = 700; SET @Field = NULL;
                SET @Msg = N'검사 Master 구성이 올바르지 않습니다.';
            END

            -- 4-3 현재 공통 업무 가능
            IF @Code = 0
                SELECT @Code = s.WorkCode, @Msg = s.WorkMessage
                  FROM [dbo].[UFN_HC_일정확인](@ServerTime, @Today, 'AM', 'NONE') s
                 WHERE s.CanWorkNow = 0;
            IF @Code IN (308, 309) BEGIN SET @Success = 0; SET @Field = NULL; END

            -- 4-4 다른 유효업무 (스펙 §30). 2건 이상은 RP-06 불변조건이 이미 깨진 상태다.
            IF @Code = 0
            BEGIN
                SET @OtherCnt = (SELECT COUNT(*) FROM [dbo].[예약접수]
                                  WHERE [수검자ID] = @PatientId
                                    AND [예약일] >= @Today
                                    AND [상태코드] IN ('RSV', 'RCP'));
                IF @OtherCnt >= 2
                BEGIN
                    SET @Success = 0; SET @Code = 701; SET @Field = NULL;
                    SET @Msg = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
                END
                ELSE IF @OtherCnt = 1
                BEGIN
                    SET @Success = 0; SET @Code = 306; SET @Field = 'PatientId';
                    SET @Msg = N'수검자에게 다른 유효 예약 또는 접수 업무가 있습니다.';
                END
            END

            -- 4-5 요청 일정·시간대 운영·마감 (300~304)
            IF @Code = 0
                SELECT @Code = s.ReasonCode, @Msg = s.ReasonMessage
                  FROM [dbo].[UFN_HC_일정확인](@ServerTime, @ReservationDate, @TimeSlot, @CutoffType) s
                 WHERE s.CanUse = 0;
            IF @Code BETWEEN 300 AND 304
            BEGIN
                SET @Success = 0;
                SET @Field = CASE WHEN @Code IN (300, 301, 302) THEN 'ReservationDate' ELSE 'TimeSlot' END;
            END

            -- 4-6 정원 (00 RP-03). Capacity 20 고정 (05 §9.7)
            IF @Code = 0
            BEGIN
                SET @SlotCnt = (SELECT COUNT(*) FROM [dbo].[예약접수]
                                 WHERE [예약일] = @ReservationDate
                                   AND [시간대코드] = @TimeSlot
                                   AND [상태코드] IN ('RSV', 'RCP'));
                IF @SlotCnt + 1 > 20
                BEGIN
                    SET @Success = 0; SET @Code = 305; SET @Field = 'TimeSlot';
                    SET @Msg = N'해당 시간대의 예약 정원이 마감되었습니다.';
                END
            END

            -- 4-7 TGT (400/401)
            IF @Code = 0
                SELECT @Code = g.ReasonCode, @Msg = g.ReasonMessage
                  FROM [dbo].[UFN_HC_검진대상확인](@PatientId, @ReservationDate) g
                 WHERE g.Eligible = 0;
            IF @Code IN (400, 401) BEGIN SET @Success = 0; SET @Field = 'ReservationDate'; END

            -- 4-8 NEX 개수 (스펙 §21.2a-(1)). 상한 11 을 버리면 TVF·Master 손상으로
            --     12행이 나와도 저장이 계속된다 (05 §17.4).
            IF @Code = 0
            BEGIN
                SET @NexCnt = (SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@PatientId, @ReservationDate));
                IF @NexCnt NOT BETWEEN 8 AND 11
                BEGIN
                    SET @Success = 0; SET @Code = 701; SET @Field = NULL;
                    SET @Msg = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
                END
            END

            -- 4-9 AEX. Requested=1 인데 CanSelect=0 인 항목만 저장을 막는다 (05 §6.4.3).
            IF @Code = 0
            BEGIN
                INSERT INTO @AexEval (OptionCode, ExamCode, Requested, Selected, CanSelect, ReasonCode, ReasonMessage)
                SELECT x.OptionCode, x.ExamCode, x.Requested, x.Selected, x.CanSelect, x.ReasonCode, x.ReasonMessage
                  FROM [dbo].[UFN_HC_추가검사확인](@PatientId, @ReservationDate, NULL, 0,
                         @AexOpt01Selected, @AexOpt02Selected, @AexOpt03Selected, @AexOpt04Selected,
                         @AexOpt05Selected, @AexOpt06Selected, @AexOpt07Selected) x;

                SELECT TOP (1)
                       @Code = a.ReasonCode
                     , @Msg  = a.ReasonMessage
                     , @Field = 'AexOpt' + RIGHT(a.OptionCode, 2) + 'Selected'
                  FROM @AexEval a
                 WHERE a.Requested = 1 AND a.CanSelect = 0
                 ORDER BY a.OptionCode;
                IF @Code <> 0 SET @Success = 0;
            END

            -- [5] 저장. 검사구성 문자열은 검사항목코드 오름차순 쉼표 연결이다 (04 §8.2.2).
            IF @Code = 0
            BEGIN
                DELETE FROM @Codes;
                INSERT INTO @Codes (C)
                SELECT n.ExamCode FROM [dbo].[UFN_HC_국가검사구성](@PatientId, @ReservationDate) n;
                SET @Csv = N'';
                WHILE EXISTS (SELECT 1 FROM @Codes)
                BEGIN
                    SELECT TOP (1) @C = C FROM @Codes ORDER BY C;
                    SET @Csv = @Csv + @C + N',';
                    DELETE FROM @Codes WHERE C = @C;
                END
                SET @Nex = LEFT(@Csv, LEN(@Csv) - 1);   -- 4-8 이 8~11행을 보장해 비어 있지 않다

                DELETE FROM @Codes;
                INSERT INTO @Codes (C) SELECT a.ExamCode FROM @AexEval a WHERE a.Selected = 1;
                SET @Csv = N'';
                WHILE EXISTS (SELECT 1 FROM @Codes)
                BEGIN
                    SELECT TOP (1) @C = C FROM @Codes ORDER BY C;
                    SET @Csv = @Csv + @C + N',';
                    DELETE FROM @Codes WHERE C = @C;
                END
                -- 추가검사 0개는 NULL 이다. 빈 문자열은 국가검사항목에서만 손상을 뜻한다 (04 §8.2.2).
                SET @Aex = CASE WHEN LEN(@Csv) = 0 THEN NULL ELSE LEFT(@Csv, LEN(@Csv) - 1) END;

                INSERT INTO [dbo].[예약접수]
                    ([수검자ID], [예약일], [시간대코드], [상태코드],
                     [국가검사항목], [추가검사항목], [생성일시], [최종수정일시])
                VALUES
                    (@PatientId, @ReservationDate, @TimeSlot, 'RSV',
                     @Nex, @Aex, @StoredNow, @StoredNow);

                SET @TargetKey = SCOPE_IDENTITY();
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
          CAST(@Success AS BIT)               AS Success
        , CAST(@Code    AS INT)               AS Code
        , CAST(@Msg     AS NVARCHAR(300))     AS Message
        , CAST(@Field   AS VARCHAR(50))       AS Field
        , CAST(@ServerTime AS DATETIME2(7))   AS ServerTime;

    -- RS1 Work결과 (05 §11). RowVersion 은 COMMIT 이후 다시 읽는다.
    IF @Code = 0
        SELECT
              WorkId     = CAST(w.[업무ID]   AS BIGINT)
            , Status     = CAST(w.[상태코드] AS CHAR(3))
            , RowVersion = CAST(w.[행버전]   AS BINARY(8))
          FROM [dbo].[예약접수] w
         WHERE w.[업무ID] = @TargetKey;

    ----------------------------------------------------------------------------
    -- [7] 감사 기록 (04 §8.6.5). INSERT 라 변경전은 항상 NULL 이다.
    ----------------------------------------------------------------------------
    IF @Code = 0 AND @TargetKey IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            SELECT @StoredNow, @OperatorName, N'예약접수', @TargetKey, V.[컬럼명], NULL, V.[변경후]
              FROM (VALUES
                      (N'수검자ID',     CONVERT(NVARCHAR(4000), @PatientId))
                    , (N'예약일',       CONVERT(NVARCHAR(4000), @ReservationDate, 23))
                    , (N'시간대코드',   CONVERT(NVARCHAR(4000), @TimeSlot))
                    , (N'상태코드',     CONVERT(NVARCHAR(4000), 'RSV'))
                    , (N'국가검사항목', CONVERT(NVARCHAR(4000), @Nex))
                    , (N'추가검사항목', CONVERT(NVARCHAR(4000), @Aex))
                   ) V([컬럼명], [변경후])
             WHERE V.[변경후] IS NOT NULL;
        END TRY
        BEGIN CATCH
        END CATCH
    END
END
GO
-- 05 §11.2. Phase 4 에서 가장 복잡한 Write SP 다.
--   C# 이 전달한 변경구분을 신뢰하지 않고 DB 현재값과 요청값을 비교해 Scope 를 직접 계산한다 (스펙 §29).
--   영향받는 Rule 만 재검증한다 — 시간대만 바뀌면 TGT/NEX/AEX 를 재평가하지도 재조립하지도 않는다.
--
-- [!] 현재 예약일·시간대는 WORK 잠금을 잡은 뒤에 읽는다. 사전조회값으로 SLOT 자원명을 만들면
--     다른 세션이 그 사이에 Work 를 옮겼을 때 엉뚱한 Slot 을 잠그고 정원 판정이 경합에 노출된다.
--     읽기는 잠금 획득이 아니므로 전역 순서 PAT(3) → WORK(4) → SLOT(5) 는 그대로 지켜진다.
--
-- [!] 501 WrongPatient 를 쓰지 않는다. 05 §13 의 허용 Code 에 501 이 없고, 이 SP 는 @PatientId
--     Parameter 자체가 없어 "요청 수검자와 다르다" 를 판정할 외부 입력이 존재하지 않는다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_UPDATE_예약변경]
    @WorkId           BIGINT,
    @RowVersion       BINARY(8),
    @ReservationDate  DATE,
    @TimeSlot         CHAR(2),
    @AexOpt01Selected BIT,
    @AexOpt02Selected BIT,
    @AexOpt03Selected BIT,
    @AexOpt04Selected BIT,
    @AexOpt05Selected BIT,
    @AexOpt06Selected BIT,
    @AexOpt07Selected BIT,
    @OperatorName     NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
    DECLARE @Today      DATE         = CONVERT(DATE, @ServerTime);
    DECLARE @StoredNow  DATETIME2(0) = CONVERT(DATETIME2(0), @ServerTime);

    DECLARE @Success BIT = 1, @Code INT = 0, @Field VARCHAR(50) = NULL;
    DECLARE @Msg NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @TargetKey BIGINT = NULL;
    DECLARE @rc INT, @ResPat NVARCHAR(255), @ResWork NVARCHAR(255);
    DECLARE @ResA NVARCHAR(255), @ResB NVARCHAR(255), @ResTmp NVARCHAR(255);

    DECLARE @PatientId BIGINT = NULL, @CurPatient BIGINT = NULL;
    DECLARE @CurDate DATE, @CurSlot CHAR(2), @CurStatus CHAR(3), @CurRv BINARY(8);
    DECLARE @CurNex NVARCHAR(100), @CurAex NVARCHAR(50);
    DECLARE @NewNex NVARCHAR(100) = NULL, @NewAex NVARCHAR(50) = NULL;

    DECLARE @DateChanged BIT = 0, @SlotChanged BIT = 0, @ExtraChanged BIT = 0;
    DECLARE @NexN INT, @AexN INT, @OtherCnt INT, @SlotCnt INT, @NexCnt INT;
    DECLARE @Csv NVARCHAR(200), @C VARCHAR(10);
    DECLARE @Codes TABLE (C VARCHAR(10) PRIMARY KEY);
    DECLARE @Req TABLE (OptionCode VARCHAR(10) PRIMARY KEY);
    DECLARE @AexEval TABLE (OptionCode VARCHAR(10) PRIMARY KEY, ExamCode VARCHAR(10),
                            Requested BIT, Selected BIT, CanSelect BIT,
                            ReasonCode INT, ReasonMessage NVARCHAR(300));

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 정규화 → 필수값 → 허용값
    ----------------------------------------------------------------------------
    SET @TimeSlot     = NULLIF(UPPER(LTRIM(RTRIM(@TimeSlot))), '');
    SET @OperatorName = NULLIF(LTRIM(RTRIM(@OperatorName)), N'');

    IF @WorkId IS NULL
    BEGIN SET @Code = 100; SET @Field = 'WorkId'; END
    ELSE IF @RowVersion IS NULL
    BEGIN SET @Code = 100; SET @Field = 'RowVersion'; END
    ELSE IF @ReservationDate IS NULL
    BEGIN SET @Code = 100; SET @Field = 'ReservationDate'; END
    ELSE IF @TimeSlot IS NULL
    BEGIN SET @Code = 100; SET @Field = 'TimeSlot'; END
    ELSE IF @AexOpt01Selected IS NULL OR @AexOpt02Selected IS NULL OR @AexOpt03Selected IS NULL
         OR @AexOpt04Selected IS NULL OR @AexOpt05Selected IS NULL OR @AexOpt06Selected IS NULL
         OR @AexOpt07Selected IS NULL
    BEGIN SET @Code = 100; SET @Field = CASE WHEN @AexOpt01Selected IS NULL THEN 'AexOpt01Selected'
                  WHEN @AexOpt02Selected IS NULL THEN 'AexOpt02Selected'
                  WHEN @AexOpt03Selected IS NULL THEN 'AexOpt03Selected'
                  WHEN @AexOpt04Selected IS NULL THEN 'AexOpt04Selected'
                  WHEN @AexOpt05Selected IS NULL THEN 'AexOpt05Selected'
                  WHEN @AexOpt06Selected IS NULL THEN 'AexOpt06Selected'
                  ELSE 'AexOpt07Selected' END; END
    ELSE IF @OperatorName IS NULL
    BEGIN SET @Code = 100; SET @Field = 'OperatorName'; END
    ELSE IF @TimeSlot NOT IN ('AM', 'PM')
    BEGIN SET @Code = 101; SET @Field = 'TimeSlot'; END

    IF @Code = 100 SET @Msg = N'필수값을 입력하십시오.';
    IF @Code = 101 SET @Msg = N'입력값이 올바르지 않습니다.';

    ----------------------------------------------------------------------------
    -- [2] Transaction 밖 : PAT 자원명을 만들 PatientId 확보 (스펙 §23.2).
    --     stale 할 수 있으나 잠금 후 재검증이 최종 판정이다. EP-02 로 PatientId 는 불변이다.
    ----------------------------------------------------------------------------
    IF @Code = 0
    BEGIN
        SELECT @PatientId = w.[수검자ID] FROM [dbo].[예약접수] w WHERE w.[업무ID] = @WorkId;
        IF @PatientId IS NULL
        BEGIN
            SET @Code = 500; SET @Field = 'WorkId';
            SET @Msg = N'예약·접수 업무를 찾을 수 없습니다.';
        END
    END

    IF @Code <> 0 SET @Success = 0;

    ----------------------------------------------------------------------------
    -- [3]~[5] Transaction
    ----------------------------------------------------------------------------
    IF @Code = 0
    BEGIN
        SET @ResPat  = N'HC|PAT|'  + CONVERT(NVARCHAR(20), @PatientId);
        SET @ResWork = N'HC|WORK|' + CONVERT(NVARCHAR(20), @WorkId);

        BEGIN TRY
            BEGIN TRANSACTION;

            EXEC @rc = sp_getapplock @Resource = @ResPat, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
            IF @rc < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            EXEC @rc = sp_getapplock @Resource = @ResWork, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
            IF @rc < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            -- WORK 를 잡은 뒤 읽으므로 이 값이 권위값이다.
            SELECT @CurPatient = w.[수검자ID], @CurDate = w.[예약일], @CurSlot = w.[시간대코드]
                 , @CurStatus = w.[상태코드], @CurRv = w.[행버전]
                 , @CurNex = w.[국가검사항목], @CurAex = w.[추가검사항목]
              FROM [dbo].[예약접수] w WHERE w.[업무ID] = @WorkId;

            IF @CurPatient IS NULL
            BEGIN
                SET @Success = 0; SET @Code = 500; SET @Field = 'WorkId';
                SET @Msg = N'예약·접수 업무를 찾을 수 없습니다.';
            END
            -- EP-02 로 수검자ID 는 불변이고 어떤 SP 도 이 컬럼을 UPDATE 하지 않는다.
            -- 그래도 달라졌다면 PAT 잠금을 엉뚱한 자원에 걸었다는 뜻이므로 손상으로 본다.
            ELSE IF @CurPatient <> @PatientId
            BEGIN
                SET @Success = 0; SET @Code = 701; SET @Field = 'WorkId';
                SET @Msg = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
            END

            IF @Code = 0
            BEGIN
                -- SLOT 자원 최대 2개를 자원명 오름차순으로 잡는다 (스펙 §23.1).
                -- CURSOR 를 쓰지 않는다 — 허용목록 밖이고, THROW 시 CLOSE/DEALLOCATE 가 남는다.
                SET @ResA = N'HC|SLOT|' + CONVERT(CHAR(8), @CurDate, 112) + N'|' + @CurSlot;
                SET @ResB = N'HC|SLOT|' + CONVERT(CHAR(8), @ReservationDate, 112) + N'|' + @TimeSlot;
                IF @ResA = @ResB SET @ResB = NULL;
                IF @ResB IS NOT NULL AND @ResB < @ResA
                BEGIN SET @ResTmp = @ResA; SET @ResA = @ResB; SET @ResB = @ResTmp; END

                EXEC @rc = sp_getapplock @Resource = @ResA, @LockMode = 'Exclusive',
                                         @LockOwner = 'Transaction', @LockTimeout = 5000;
                PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
                IF @rc < 0
                BEGIN
                    ROLLBACK TRANSACTION;
                    IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                    THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
                END

                IF @ResB IS NOT NULL
                BEGIN
                    EXEC @rc = sp_getapplock @Resource = @ResB, @LockMode = 'Exclusive',
                                             @LockOwner = 'Transaction', @LockTimeout = 5000;
                    PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
                    IF @rc < 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                        THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
                    END
                END

                -- [4] 재검증 — 05 §11.2 검증순서
                IF @CurStatus <> 'RSV'
                BEGIN
                    SET @Success = 0; SET @Code = 502; SET @Field = 'WorkId';
                    SET @Msg = N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.';
                END
                ELSE IF @CurRv <> @RowVersion
                BEGIN
                    SET @Success = 0; SET @Code = 601; SET @Field = 'RowVersion';
                    SET @Msg = N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.';
                END
            END

            -- Scope 계산 (스펙 §29). ExtraChanged 는 §28.1 대로 EXCEPT 양방향이다.
            IF @Code = 0
            BEGIN
                INSERT INTO @Req (OptionCode)
                SELECT v.c FROM (VALUES ('OPT01',@AexOpt01Selected),('OPT02',@AexOpt02Selected),
                                        ('OPT03',@AexOpt03Selected),('OPT04',@AexOpt04Selected),
                                        ('OPT05',@AexOpt05Selected),('OPT06',@AexOpt06Selected),
                                        ('OPT07',@AexOpt07Selected)) v(c, b)
                 WHERE v.b = 1;

                SET @DateChanged = CASE WHEN @CurDate <> @ReservationDate THEN 1 ELSE 0 END;
                SET @SlotChanged = CASE WHEN @CurSlot <> @TimeSlot THEN 1 ELSE 0 END;
                SET @ExtraChanged = CASE WHEN EXISTS (
                        SELECT OptionCode FROM @Req
                        EXCEPT
                        SELECT m.[추가검사코드] FROM [dbo].[검사코드] m
                         WHERE m.[추가검사코드] IS NOT NULL
                           AND N',' + ISNULL(@CurAex, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%')
                    OR EXISTS (
                        SELECT m.[추가검사코드] FROM [dbo].[검사코드] m
                         WHERE m.[추가검사코드] IS NOT NULL
                           AND N',' + ISNULL(@CurAex, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
                        EXCEPT
                        SELECT OptionCode FROM @Req)
                    THEN 1 ELSE 0 END;

                -- No-op 은 Transaction·잠금 안에서 판정한다 (스펙 §28)
                IF @DateChanged = 0 AND @SlotChanged = 0 AND @ExtraChanged = 0
                BEGIN
                    SET @Success = 1; SET @Code = 1; SET @Field = NULL;
                    SET @Msg = N'변경된 내용이 없습니다.';
                END
            END

            -- 검사 Master 구성 (700). 검사구성을 건드리는 Scope 에서만 본다.
            IF @Code = 0 AND (@DateChanged = 1 OR @ExtraChanged = 1)
               AND ((SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [국가검사규칙코드] IS NOT NULL) < 8
                 OR (SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [추가검사코드] IS NOT NULL) <> 7)
            BEGIN
                SET @Success = 0; SET @Code = 700; SET @Field = NULL;
                SET @Msg = N'검사 Master 구성이 올바르지 않습니다.';
            END

            -- 저장 검사구성 무결성 3종 (스펙 §21.2a). 빈 문자열은 0개다 —
            -- LEN - LEN(REPLACE) + 1 만 쓰면 빈 문자열을 1개로 세어 CORRUPT-2 를 놓친다.
            IF @Code = 0 AND (@DateChanged = 1 OR @ExtraChanged = 1)
            BEGIN
                SET @NexN = CASE WHEN LEN(ISNULL(@CurNex, N'')) = 0 THEN 0
                                 ELSE LEN(@CurNex) - LEN(REPLACE(@CurNex, N',', N'')) + 1 END;
                SET @AexN = CASE WHEN LEN(ISNULL(@CurAex, N'')) = 0 THEN 0
                                 ELSE LEN(@CurAex) - LEN(REPLACE(@CurAex, N',', N'')) + 1 END;

                IF @NexN NOT BETWEEN 8 AND 11
                   OR @AexN > 6
                   OR (SELECT COUNT(*) FROM [dbo].[검사코드] m
                        WHERE m.[국가검사규칙코드] IS NOT NULL
                          AND N',' + ISNULL(@CurNex, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%') <> @NexN
                   OR (SELECT COUNT(*) FROM [dbo].[검사코드] m
                        WHERE m.[추가검사코드] IS NOT NULL
                          AND N',' + ISNULL(@CurAex, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%') <> @AexN
                BEGIN
                    SET @Success = 0; SET @Code = 701; SET @Field = 'WorkId';
                    SET @Msg = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
                END
            END

            -- 현재 공통 업무 가능 (실제 변경이 있을 때만)
            IF @Code = 0
                SELECT @Code = s.WorkCode, @Msg = s.WorkMessage
                  FROM [dbo].[UFN_HC_일정확인](@ServerTime, @Today, 'AM', 'NONE') s
                 WHERE s.CanWorkNow = 0;
            IF @Code IN (308, 309) BEGIN SET @Success = 0; SET @Field = NULL; END

            -- 일정·중복·정원 — 예약일 또는 시간대가 바뀔 때만 (스펙 §29)
            IF @Code = 0 AND (@DateChanged = 1 OR @SlotChanged = 1)
            BEGIN
                -- 다른 유효업무. 현재 Work 제외가 필수다 (스펙 §30) — 빠지면 자기 자신을 306 으로 오인한다.
                SET @OtherCnt = (SELECT COUNT(*) FROM [dbo].[예약접수]
                                  WHERE [수검자ID] = @PatientId
                                    AND [예약일] >= @Today
                                    AND [상태코드] IN ('RSV', 'RCP')
                                    AND [업무ID] <> @WorkId);
                IF @OtherCnt >= 2
                BEGIN
                    SET @Success = 0; SET @Code = 701; SET @Field = 'WorkId';
                    SET @Msg = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
                END
                ELSE IF @OtherCnt = 1
                BEGIN
                    SET @Success = 0; SET @Code = 306; SET @Field = 'PatientId';
                    SET @Msg = N'수검자에게 다른 유효 예약 또는 접수 업무가 있습니다.';
                END

                IF @Code = 0
                    SELECT @Code = s.ReasonCode, @Msg = s.ReasonMessage
                      FROM [dbo].[UFN_HC_일정확인](@ServerTime, @ReservationDate, @TimeSlot, 'NORMAL') s
                     WHERE s.CanUse = 0;
                IF @Code BETWEEN 300 AND 304
                BEGIN
                    SET @Success = 0;
                    SET @Field = CASE WHEN @Code IN (300, 301, 302) THEN 'ReservationDate' ELSE 'TimeSlot' END;
                END

                -- 정원. 현재 Work 를 COUNT 에서 빼고 +1 한다 (스펙 §30.1) —
                -- 20/20 Slot 을 그대로 유지하는 변경을 21 로 계산하지 않기 위해서다.
                IF @Code = 0
                BEGIN
                    SET @SlotCnt = (SELECT COUNT(*) FROM [dbo].[예약접수]
                                     WHERE [예약일] = @ReservationDate
                                       AND [시간대코드] = @TimeSlot
                                       AND [상태코드] IN ('RSV', 'RCP')
                                       AND [업무ID] <> @WorkId);
                    IF @SlotCnt + 1 > 20
                    BEGIN
                        SET @Success = 0; SET @Code = 305; SET @Field = 'TimeSlot';
                        SET @Msg = N'해당 시간대의 예약 정원이 마감되었습니다.';
                    END
                END
            END

            -- TGT / NEX / AEX — 예약일이 바뀔 때만 전량 재판정한다
            IF @Code = 0 AND @DateChanged = 1
            BEGIN
                SELECT @Code = g.ReasonCode, @Msg = g.ReasonMessage
                  FROM [dbo].[UFN_HC_검진대상확인](@PatientId, @ReservationDate) g
                 WHERE g.Eligible = 0;
                IF @Code IN (400, 401) BEGIN SET @Success = 0; SET @Field = 'ReservationDate'; END

                IF @Code = 0
                BEGIN
                    SET @NexCnt = (SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@PatientId, @ReservationDate));
                    IF @NexCnt NOT BETWEEN 8 AND 11
                    BEGIN
                        SET @Success = 0; SET @Code = 701; SET @Field = 'WorkId';
                        SET @Msg = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
                    END
                END
            END

            -- AEX 평가. 예약일이 바뀌면 **변경 후** 예약일 기준 NEX 로 중복을 판정한다 —
            -- 변경 전 구성으로 판정하면 나이 경계를 넘는 이동에서 412 를 놓친다 (05 §17.7).
            IF @Code = 0 AND (@DateChanged = 1 OR @ExtraChanged = 1)
            BEGIN
                INSERT INTO @AexEval (OptionCode, ExamCode, Requested, Selected, CanSelect, ReasonCode, ReasonMessage)
                SELECT x.OptionCode, x.ExamCode, x.Requested, x.Selected, x.CanSelect, x.ReasonCode, x.ReasonMessage
                  FROM [dbo].[UFN_HC_추가검사확인](@PatientId
                         , CASE WHEN @DateChanged = 1 THEN @ReservationDate ELSE @CurDate END
                         , @WorkId
                         , CASE WHEN @DateChanged = 1 THEN 0 ELSE 1 END
                         , @AexOpt01Selected, @AexOpt02Selected, @AexOpt03Selected, @AexOpt04Selected
                         , @AexOpt05Selected, @AexOpt06Selected, @AexOpt07Selected) x;

                SELECT TOP (1)
                       @Code  = a.ReasonCode
                     , @Msg   = a.ReasonMessage
                     , @Field = 'AexOpt' + RIGHT(a.OptionCode, 2) + 'Selected'
                  FROM @AexEval a
                 WHERE a.Requested = 1 AND a.CanSelect = 0
                 ORDER BY a.OptionCode;
                IF @Code <> 0 SET @Success = 0;
            END

            -- [5] 저장. 언제나 예약접수 한 행의 UPDATE 다 (05 §11.2).
            IF @Code = 0
            BEGIN
                IF @DateChanged = 1
                BEGIN
                    DELETE FROM @Codes;
                    INSERT INTO @Codes (C)
                    SELECT n.ExamCode FROM [dbo].[UFN_HC_국가검사구성](@PatientId, @ReservationDate) n;
                    SET @Csv = N'';
                    WHILE EXISTS (SELECT 1 FROM @Codes)
                    BEGIN
                        SELECT TOP (1) @C = C FROM @Codes ORDER BY C;
                        SET @Csv = @Csv + @C + N',';
                        DELETE FROM @Codes WHERE C = @C;
                    END
                    SET @NewNex = LEFT(@Csv, LEN(@Csv) - 1);
                END

                IF @DateChanged = 1 OR @ExtraChanged = 1
                BEGIN
                    DELETE FROM @Codes;
                    INSERT INTO @Codes (C) SELECT a.ExamCode FROM @AexEval a WHERE a.Selected = 1;
                    SET @Csv = N'';
                    WHILE EXISTS (SELECT 1 FROM @Codes)
                    BEGIN
                        SELECT TOP (1) @C = C FROM @Codes ORDER BY C;
                        SET @Csv = @Csv + @C + N',';
                        DELETE FROM @Codes WHERE C = @C;
                    END
                    SET @NewAex = CASE WHEN LEN(@Csv) = 0 THEN NULL ELSE LEFT(@Csv, LEN(@Csv) - 1) END;
                END

                -- 조건부 UPDATE 표준형 (스펙 §24.4). Scope 밖 컬럼은 현재값을 그대로 둔다.
                UPDATE [dbo].[예약접수]
                   SET [예약일]       = CASE WHEN @DateChanged = 1 THEN @ReservationDate ELSE [예약일] END
                     , [시간대코드]   = CASE WHEN @SlotChanged = 1 THEN @TimeSlot ELSE [시간대코드] END
                     , [국가검사항목] = CASE WHEN @DateChanged = 1 THEN @NewNex ELSE [국가검사항목] END
                     , [추가검사항목] = CASE WHEN @DateChanged = 1 OR @ExtraChanged = 1
                                            THEN @NewAex ELSE [추가검사항목] END
                     , [최종수정일시] = @StoredNow
                 WHERE [업무ID]   = @WorkId
                   AND [상태코드] = 'RSV'
                   AND [행버전]   = @RowVersion;

                IF @@ROWCOUNT = 0
                BEGIN
                    -- WORK 잠금을 쥐고 있어 도달할 수 없다. 상태가 동시성값보다 우선이다 (05 §5).
                    SET @Success = 0;
                    IF EXISTS (SELECT 1 FROM [dbo].[예약접수] WHERE [업무ID] = @WorkId AND [상태코드] <> 'RSV')
                    BEGIN
                        SET @Code = 502; SET @Field = 'WorkId';
                        SET @Msg = N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.';
                    END
                    ELSE
                    BEGIN
                        SET @Code = 601; SET @Field = 'RowVersion';
                        SET @Msg = N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.';
                    END
                END
                ELSE
                    SET @TargetKey = @WorkId;
            END

            IF @Code IN (0, 1) COMMIT TRANSACTION;
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
          CAST(@Success AS BIT)               AS Success
        , CAST(@Code    AS INT)               AS Code
        , CAST(@Msg     AS NVARCHAR(300))     AS Message
        , CAST(@Field   AS VARCHAR(50))       AS Field
        , CAST(@ServerTime AS DATETIME2(7))   AS ServerTime;

    -- No-op 은 갱신하지 않은 기존 RowVersion 이 그대로 나온다 (05 §11.2).
    IF @Code IN (0, 1)
        SELECT
              WorkId     = CAST(w.[업무ID]   AS BIGINT)
            , Status     = CAST(w.[상태코드] AS CHAR(3))
            , RowVersion = CAST(w.[행버전]   AS BINARY(8))
          FROM [dbo].[예약접수] w
         WHERE w.[업무ID] = @WorkId;

    ----------------------------------------------------------------------------
    -- [7] 감사 기록. 실제로 값이 바뀐 컬럼만 남는다 (00 CP-06).
    ----------------------------------------------------------------------------
    IF @Code = 0 AND @TargetKey IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            SELECT @StoredNow, @OperatorName, N'예약접수', @TargetKey, V.[컬럼명], V.[변경전], V.[변경후]
              FROM (VALUES
                      (N'예약일',       CONVERT(NVARCHAR(4000), @CurDate, 23)
                                      , CONVERT(NVARCHAR(4000), CASE WHEN @DateChanged = 1 THEN @ReservationDate ELSE @CurDate END, 23))
                    , (N'시간대코드',   CONVERT(NVARCHAR(4000), @CurSlot)
                                      , CONVERT(NVARCHAR(4000), CASE WHEN @SlotChanged = 1 THEN @TimeSlot ELSE @CurSlot END))
                    , (N'국가검사항목', CONVERT(NVARCHAR(4000), @CurNex)
                                      , CONVERT(NVARCHAR(4000), CASE WHEN @DateChanged = 1 THEN @NewNex ELSE @CurNex END))
                    , (N'추가검사항목', CONVERT(NVARCHAR(4000), @CurAex)
                                      , CONVERT(NVARCHAR(4000), CASE WHEN @DateChanged = 1 OR @ExtraChanged = 1
                                                                     THEN @NewAex ELSE @CurAex END))
                   ) V([컬럼명], [변경전], [변경후])
             WHERE ISNULL(V.[변경전], N'~NULL~') <> ISNULL(V.[변경후], N'~NULL~');
        END TRY
        BEGIN CATCH
        END CATCH
    END
END
GO
-- 05 §11.3. RSV → CNR. 검사구성 두 컬럼은 지우지 않고, 마감시각은 취소 가능조건이 아니다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_UPDATE_예약취소]
    @WorkId       BIGINT,
    @RowVersion   BINARY(8),
    @OperatorName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
    DECLARE @Today      DATE         = CONVERT(DATE, @ServerTime);
    DECLARE @StoredNow  DATETIME2(0) = CONVERT(DATETIME2(0), @ServerTime);

    DECLARE @Success BIT = 1, @Code INT = 0, @Field VARCHAR(50) = NULL;
    DECLARE @Msg NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @TargetKey BIGINT = NULL;
    DECLARE @rc INT, @ResWork NVARCHAR(255);
    DECLARE @CurStatus CHAR(3) = NULL, @CurRv BINARY(8) = NULL;

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 필수값. 허용 Code 는 0, 100, 500, 502, 601, 308~309 뿐이다.
    ----------------------------------------------------------------------------
    SET @OperatorName = NULLIF(LTRIM(RTRIM(@OperatorName)), N'');

    IF @WorkId IS NULL
    BEGIN SET @Code = 100; SET @Field = 'WorkId'; END
    ELSE IF @RowVersion IS NULL
    BEGIN SET @Code = 100; SET @Field = 'RowVersion'; END
    ELSE IF @OperatorName IS NULL
    BEGIN SET @Code = 100; SET @Field = 'OperatorName'; END

    IF @Code = 100 SET @Msg = N'필수값을 입력하십시오.';
    IF @Code <> 0 SET @Success = 0;

    ----------------------------------------------------------------------------
    -- [3]~[5] Transaction. WORK 자원명이 입력값이라 사전조회가 없다 (스펙 §24.1).
    ----------------------------------------------------------------------------
    IF @Code = 0
    BEGIN
        SET @ResWork = N'HC|WORK|' + CONVERT(NVARCHAR(20), @WorkId);

        BEGIN TRY
            BEGIN TRANSACTION;

            EXEC @rc = sp_getapplock @Resource = @ResWork, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
            IF @rc < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            SELECT @CurStatus = w.[상태코드], @CurRv = w.[행버전]
              FROM [dbo].[예약접수] w WHERE w.[업무ID] = @WorkId;

            IF @CurStatus IS NULL
            BEGIN
                SET @Success = 0; SET @Code = 500; SET @Field = 'WorkId';
                SET @Msg = N'예약·접수 업무를 찾을 수 없습니다.';
            END
            ELSE IF @CurStatus <> 'RSV'
            BEGIN
                SET @Success = 0; SET @Code = 502; SET @Field = 'WorkId';
                SET @Msg = N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.';
            END
            ELSE IF @CurRv <> @RowVersion
            BEGIN
                SET @Success = 0; SET @Code = 601; SET @Field = 'RowVersion';
                SET @Msg = N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.';
            END

            IF @Code = 0
                SELECT @Code = s.WorkCode, @Msg = s.WorkMessage
                  FROM [dbo].[UFN_HC_일정확인](@ServerTime, @Today, 'AM', 'NONE') s
                 WHERE s.CanWorkNow = 0;
            IF @Code IN (308, 309) BEGIN SET @Success = 0; SET @Field = NULL; END

            IF @Code = 0
            BEGIN
                -- 조건부 UPDATE 표준형 (스펙 §24.4). 검사구성 두 컬럼은 건드리지 않는다.
                UPDATE [dbo].[예약접수]
                   SET [상태코드]     = 'CNR'
                     , [최종수정일시] = @StoredNow
                 WHERE [업무ID]   = @WorkId
                   AND [상태코드] = 'RSV'
                   AND [행버전]   = @RowVersion;

                IF @@ROWCOUNT = 0
                BEGIN
                    SET @Success = 0;
                    IF EXISTS (SELECT 1 FROM [dbo].[예약접수] WHERE [업무ID] = @WorkId AND [상태코드] <> 'RSV')
                    BEGIN
                        SET @Code = 502; SET @Field = 'WorkId';
                        SET @Msg = N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.';
                    END
                    ELSE
                    BEGIN
                        SET @Code = 601; SET @Field = 'RowVersion';
                        SET @Msg = N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.';
                    END
                END
                ELSE
                    SET @TargetKey = @WorkId;
            END

            IF @Code = 0 COMMIT TRANSACTION;
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
          CAST(@Success AS BIT)               AS Success
        , CAST(@Code    AS INT)               AS Code
        , CAST(@Msg     AS NVARCHAR(300))     AS Message
        , CAST(@Field   AS VARCHAR(50))       AS Field
        , CAST(@ServerTime AS DATETIME2(7))   AS ServerTime;

    IF @Code = 0
        SELECT
              WorkId     = CAST(w.[업무ID]   AS BIGINT)
            , Status     = CAST(w.[상태코드] AS CHAR(3))
            , RowVersion = CAST(w.[행버전]   AS BINARY(8))
          FROM [dbo].[예약접수] w
         WHERE w.[업무ID] = @WorkId;

    ----------------------------------------------------------------------------
    -- [7] 감사 기록. 바뀐 것은 상태코드 하나다.
    ----------------------------------------------------------------------------
    IF @Code = 0 AND @TargetKey IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            VALUES
                (@StoredNow, @OperatorName, N'예약접수', @TargetKey, N'상태코드', N'RSV', N'CNR');
        END TRY
        BEGIN CATCH
        END CATCH
    END
END
GO
