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
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_UPDATE_접수완료]
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
    DECLARE @rc INT, @ResPat NVARCHAR(255), @ResWork NVARCHAR(255), @ResSlot NVARCHAR(255);

    DECLARE @PatientId BIGINT = NULL, @CurPatient BIGINT = NULL;
    DECLARE @CurDate DATE, @CurSlot CHAR(2), @CurStatus CHAR(3), @CurRv BINARY(8);
    DECLARE @CurNex NVARCHAR(100), @CurAex NVARCHAR(50);
    DECLARE @NexN INT, @AexN INT;

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 필수값. 허용 Code 는 0, 100, 304, 308~309, 500, 502~503, 601, 701 이다.
    ----------------------------------------------------------------------------
    SET @OperatorName = NULLIF(LTRIM(RTRIM(@OperatorName)), N'');

    IF @WorkId IS NULL
    BEGIN SET @Code = 100; SET @Field = 'WorkId'; END
    ELSE IF @RowVersion IS NULL
    BEGIN SET @Code = 100; SET @Field = 'RowVersion'; END
    ELSE IF @OperatorName IS NULL
    BEGIN SET @Code = 100; SET @Field = 'OperatorName'; END

    IF @Code = 100 SET @Msg = N'필수값을 입력하십시오.';

    ----------------------------------------------------------------------------
    -- [2] Transaction 밖 : PAT 자원명을 만들 PatientId 확보 (스펙 §23.2)
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
    -- [3]~[5] Transaction. 전역 순서 PAT(3) -> WORK(4) -> SLOT(5) (스펙 §23·§24.2)
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
            ELSE IF @CurPatient <> @PatientId
            BEGIN
                SET @Success = 0; SET @Code = 701; SET @Field = 'WorkId';
                SET @Msg = N'예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다.';
            END

            IF @Code = 0
            BEGIN
                SET @ResSlot = N'HC|SLOT|' + CONVERT(CHAR(8), @CurDate, 112) + N'|' + @CurSlot;
                EXEC @rc = sp_getapplock @Resource = @ResSlot, @LockMode = 'Exclusive',
                                         @LockOwner = 'Transaction', @LockTimeout = 5000;
                PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
                IF @rc < 0
                BEGIN
                    ROLLBACK TRANSACTION;
                    IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                    THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
                END

                -- [4] 재검증 — 05 §12.1 검증순서
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

            -- Work 검사구성 무결성 3종 (스펙 §21.2a). 빈 문자열은 0개다.
            IF @Code = 0
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

            IF @Code = 0
                SELECT @Code = s.WorkCode, @Msg = s.WorkMessage
                  FROM [dbo].[UFN_HC_일정확인](@ServerTime, @Today, 'AM', 'NONE') s
                 WHERE s.CanWorkNow = 0;
            IF @Code IN (308, 309) BEGIN SET @Success = 0; SET @Field = NULL; END

            -- 예약일이 오늘인 업무만 접수할 수 있다 (05 §12.1)
            IF @Code = 0 AND @CurDate <> @Today
            BEGIN
                SET @Success = 0; SET @Code = 503; SET @Field = 'WorkId';
                SET @Msg = N'예약일이 오늘인 업무만 접수할 수 있습니다.';
            END

            -- 접수마감 (AM 11:00 / PM 16:00). CutoffType='RECEPTION' 이다 (05 §2.4).
            IF @Code = 0
            BEGIN
                IF EXISTS (SELECT 1 FROM [dbo].[UFN_HC_일정확인](@ServerTime, @CurDate, @CurSlot, 'RECEPTION') s
                            WHERE s.CutoffPassed = 1)
                BEGIN
                    SET @Success = 0; SET @Code = 304; SET @Field = 'TimeSlot';
                    SET @Msg = N'해당 시간대의 마감시간이 지났습니다.';
                END
            END

            IF @Code = 0
            BEGIN
                -- 조건부 UPDATE 표준형 (스펙 §24.4).
                -- 예약일·시간대코드·검사구성 두 컬럼은 건드리지 않는다 (05 §12.1).
                UPDATE [dbo].[예약접수]
                   SET [상태코드]     = 'RCP'
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
                (@StoredNow, @OperatorName, N'예약접수', @TargetKey, N'상태코드', N'RSV', N'RCP');
        END TRY
        BEGIN CATCH
        END CATCH
    END
END
GO
-- 05 §12.2. RCP 상태에서 추가검사만 바꾼다.
--   예약일·시간대·TGT·NEX 를 변경하지도 재평가하지도 않는다.
--   [!] No-op 에서는 현재 Master 의 비활성·성별·중복 Rule 을 **재평가하지 않는다** (05 §12.2, 스펙 §28).
--       저장 당시엔 유효했지만 지금은 무효인 AEX 가 있어도 동일집합 호출은 성공해야 한다.
--       그래서 무결성·Master·AEX 검증을 전부 No-op 판정 **뒤에** 둔다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_UPDATE_접수추가검사]
    @WorkId           BIGINT,
    @RowVersion       BINARY(8),
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
    DECLARE @rc INT, @ResWork NVARCHAR(255);

    DECLARE @PatientId BIGINT, @CurDate DATE, @CurStatus CHAR(3), @CurRv BINARY(8);
    DECLARE @CurNex NVARCHAR(100), @CurAex NVARCHAR(50), @NewAex NVARCHAR(50) = NULL;
    DECLARE @ExtraChanged BIT = 0, @NexN INT, @AexN INT;
    DECLARE @Csv NVARCHAR(200), @C VARCHAR(10);
    DECLARE @Codes TABLE (C VARCHAR(10) PRIMARY KEY);
    DECLARE @Req TABLE (OptionCode VARCHAR(10) PRIMARY KEY);
    DECLARE @AexEval TABLE (OptionCode VARCHAR(10) PRIMARY KEY, ExamCode VARCHAR(10),
                            Requested BIT, Selected BIT, CanSelect BIT,
                            ReasonCode INT, ReasonMessage NVARCHAR(300));

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 필수값
    ----------------------------------------------------------------------------
    SET @OperatorName = NULLIF(LTRIM(RTRIM(@OperatorName)), N'');

    IF @WorkId IS NULL
    BEGIN SET @Code = 100; SET @Field = 'WorkId'; END
    ELSE IF @RowVersion IS NULL
    BEGIN SET @Code = 100; SET @Field = 'RowVersion'; END
    ELSE IF @AexOpt01Selected IS NULL OR @AexOpt02Selected IS NULL OR @AexOpt03Selected IS NULL
         OR @AexOpt04Selected IS NULL OR @AexOpt05Selected IS NULL OR @AexOpt06Selected IS NULL
         OR @AexOpt07Selected IS NULL
    BEGIN SET @Code = 100; SET @Field = 'AexOptSelected'; END
    ELSE IF @OperatorName IS NULL
    BEGIN SET @Code = 100; SET @Field = 'OperatorName'; END

    IF @Code = 100 BEGIN SET @Msg = N'필수값을 입력하십시오.'; SET @Success = 0; END

    ----------------------------------------------------------------------------
    -- [3]~[5] Transaction. WORK 만 잡는다 (스펙 §24.1). RCP -> RCP 는 두 NCI 키가 불변이다.
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

            SELECT @PatientId = w.[수검자ID], @CurDate = w.[예약일]
                 , @CurStatus = w.[상태코드], @CurRv = w.[행버전]
                 , @CurNex = w.[국가검사항목], @CurAex = w.[추가검사항목]
              FROM [dbo].[예약접수] w WHERE w.[업무ID] = @WorkId;

            IF @CurStatus IS NULL
            BEGIN
                SET @Success = 0; SET @Code = 500; SET @Field = 'WorkId';
                SET @Msg = N'예약·접수 업무를 찾을 수 없습니다.';
            END
            ELSE IF @CurStatus <> 'RCP'
            BEGIN
                SET @Success = 0; SET @Code = 502; SET @Field = 'WorkId';
                SET @Msg = N'현재 상태에서는 요청한 업무를 처리할 수 없습니다.';
            END
            ELSE IF @CurRv <> @RowVersion
            BEGIN
                SET @Success = 0; SET @Code = 601; SET @Field = 'RowVersion';
                SET @Msg = N'다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.';
            END

            -- 현재 AEX 집합과 요청 AEX 집합 비교 (스펙 §28.1 EXCEPT 양방향)
            IF @Code = 0
            BEGIN
                INSERT INTO @Req (OptionCode)
                SELECT v.c FROM (VALUES ('OPT01',@AexOpt01Selected),('OPT02',@AexOpt02Selected),
                                        ('OPT03',@AexOpt03Selected),('OPT04',@AexOpt04Selected),
                                        ('OPT05',@AexOpt05Selected),('OPT06',@AexOpt06Selected),
                                        ('OPT07',@AexOpt07Selected)) v(c, b)
                 WHERE v.b = 1;

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

                -- No-op. 여기서 끝나므로 아래의 Master·성별·중복 Rule 을 지나지 않는다.
                IF @ExtraChanged = 0
                BEGIN
                    SET @Success = 1; SET @Code = 1; SET @Field = NULL;
                    SET @Msg = N'변경된 내용이 없습니다.';
                END
            END

            -- 실제 변경일 때만 저장 검사구성 무결성 3종과 Master 구성을 본다 (05 §12.2)
            IF @Code = 0
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
                ELSE IF (SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [추가검사코드] IS NOT NULL) <> 7
                BEGIN
                    SET @Success = 0; SET @Code = 700; SET @Field = NULL;
                    SET @Msg = N'검사 Master 구성이 올바르지 않습니다.';
                END
            END

            IF @Code = 0
                SELECT @Code = s.WorkCode, @Msg = s.WorkMessage
                  FROM [dbo].[UFN_HC_일정확인](@ServerTime, @Today, 'AM', 'NONE') s
                 WHERE s.CanWorkNow = 0;
            IF @Code IN (308, 309) BEGIN SET @Success = 0; SET @Field = NULL; END

            -- AEX 성별·중복 Rule 은 **저장된 NEX** 기준이다 (05 §6.4.3, UseSavedExams=1)
            IF @Code = 0
            BEGIN
                INSERT INTO @AexEval (OptionCode, ExamCode, Requested, Selected, CanSelect, ReasonCode, ReasonMessage)
                SELECT x.OptionCode, x.ExamCode, x.Requested, x.Selected, x.CanSelect, x.ReasonCode, x.ReasonMessage
                  FROM [dbo].[UFN_HC_추가검사확인](@PatientId, @CurDate, @WorkId, 1,
                         @AexOpt01Selected, @AexOpt02Selected, @AexOpt03Selected, @AexOpt04Selected,
                         @AexOpt05Selected, @AexOpt06Selected, @AexOpt07Selected) x;

                SELECT TOP (1)
                       @Code  = a.ReasonCode
                     , @Msg   = a.ReasonMessage
                     , @Field = 'AexOpt' + RIGHT(a.OptionCode, 2) + 'Selected'
                  FROM @AexEval a
                 WHERE a.Requested = 1 AND a.CanSelect = 0
                 ORDER BY a.OptionCode;
                IF @Code <> 0 SET @Success = 0;
            END

            IF @Code = 0
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

                -- 조건부 UPDATE 표준형 (스펙 §24.4). 예약일·시간대·국가검사항목은 건드리지 않는다.
                UPDATE [dbo].[예약접수]
                   SET [추가검사항목] = @NewAex
                     , [최종수정일시] = @StoredNow
                 WHERE [업무ID]   = @WorkId
                   AND [상태코드] = 'RCP'
                   AND [행버전]   = @RowVersion;

                IF @@ROWCOUNT = 0
                BEGIN
                    SET @Success = 0;
                    IF EXISTS (SELECT 1 FROM [dbo].[예약접수] WHERE [업무ID] = @WorkId AND [상태코드] <> 'RCP')
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
    -- [6] Result Set. No-op 은 기존 RowVersion 을 유지한다 (05 §12.2).
    ----------------------------------------------------------------------------
    SELECT
          CAST(@Success AS BIT)               AS Success
        , CAST(@Code    AS INT)               AS Code
        , CAST(@Msg     AS NVARCHAR(300))     AS Message
        , CAST(@Field   AS VARCHAR(50))       AS Field
        , CAST(@ServerTime AS DATETIME2(7))   AS ServerTime;

    IF @Code IN (0, 1)
        SELECT
              WorkId     = CAST(w.[업무ID]   AS BIGINT)
            , Status     = CAST(w.[상태코드] AS CHAR(3))
            , RowVersion = CAST(w.[행버전]   AS BINARY(8))
          FROM [dbo].[예약접수] w
         WHERE w.[업무ID] = @WorkId;

    ----------------------------------------------------------------------------
    -- [7] 감사 기록. 바뀐 것은 추가검사항목 하나다.
    ----------------------------------------------------------------------------
    IF @Code = 0 AND @TargetKey IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            SELECT @StoredNow, @OperatorName, N'예약접수', @TargetKey, N'추가검사항목'
                 , CONVERT(NVARCHAR(4000), @CurAex), CONVERT(NVARCHAR(4000), @NewAex)
             WHERE ISNULL(CONVERT(NVARCHAR(4000), @CurAex), N'~NULL~')
                <> ISNULL(CONVERT(NVARCHAR(4000), @NewAex), N'~NULL~');
        END TRY
        BEGIN CATCH
        END CATCH
    END
END
GO
-- 05 §12.3. RCP -> CNC. 검사구성 두 컬럼은 보존하고, 접수 마감시각은 취소 가능조건이 아니다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_UPDATE_접수취소]
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
    -- [1] Transaction 밖 : 필수값. 허용 Code 는 0, 100, 308~309, 500, 502, 601 뿐이다.
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
    -- [3]~[5] Transaction. RCP -> CNC 는 집합 밖으로 나가는 전이라 WORK 만 잡는다 (스펙 §24.2).
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
            ELSE IF @CurStatus <> 'RCP'
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
                UPDATE [dbo].[예약접수]
                   SET [상태코드]     = 'CNC'
                     , [최종수정일시] = @StoredNow
                 WHERE [업무ID]   = @WorkId
                   AND [상태코드] = 'RCP'
                   AND [행버전]   = @RowVersion;

                IF @@ROWCOUNT = 0
                BEGIN
                    SET @Success = 0;
                    IF EXISTS (SELECT 1 FROM [dbo].[예약접수] WHERE [업무ID] = @WorkId AND [상태코드] <> 'RCP')
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
    -- [7] 감사 기록
    ----------------------------------------------------------------------------
    IF @Code = 0 AND @TargetKey IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            VALUES
                (@StoredNow, @OperatorName, N'예약접수', @TargetKey, N'상태코드', N'RCP', N'CNC');
        END TRY
        BEGIN CATCH
        END CATCH
    END
END
GO
