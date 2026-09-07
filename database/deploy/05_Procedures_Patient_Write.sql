SET QUOTED_IDENTIFIER ON;   -- 01_Schema.sql 과 같은 설정으로 객체를 만든다 (CLAUDE.md §6)
GO
SET NOCOUNT ON;
GO
-- 수검자 Write SP 2개 (05 §10). Write SP 공통 Template 은 스펙 §21.1 이다.
--   SET XACT_ABORT ON · @ServerTime 1회 캡처 · 입력검증은 Transaction 밖 · RS 는 Transaction 이 닫힌 뒤.
--   applock 은 전역 순서 SSN(1) → CHART(2) → PAT(3) 을 지킨다 (스펙 §23). Owner 가 Transaction 이라
--   COMMIT/ROLLBACK 이 자동 해제한다 — sp_releaseapplock 을 부르지 않는다.
--
-- [!] 생년월일·성별은 PERSISTED 계산열이다 (04 §8.1.2). INSERT/UPDATE 의 컬럼 목록에 넣으면
--     Msg 271 로 죽는다. 아래 @Birthday 는 저장용이 아니라 203 후보검색용 파생값이고
--     @Gender 는 7번째 자리 검증에만 쓴다. 저장값은 DB 가 주민번호에서 유도한다.
--
-- [!] 휴대전화·전화번호의 '-' 는 제거하지 않는다. 05 §2.2 가 '-' 제거를 **검색값**으로 한정했고
--     04 §8.1.2 는 표시값 그대로 저장한다고 정했다. CK_수검자_CEL_DIGIT 도 REPLACE 로 비교한다.
--     plans/04 Step 2 의 "MobilePhone/Phone 에서 '-' 제거" 는 기준선과 어긋나 따르지 않는다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_INSERT_수검자]
    @AutoChartNo           BIT,
    @ChartNo               NVARCHAR(100),
    @Name                  NVARCHAR(100),
    @SocialNumber          VARCHAR(13),
    @MobilePhone           VARCHAR(13),
    @Phone                 VARCHAR(13),
    @Email                 VARCHAR(200),
    @Zipcode               VARCHAR(10),
    @Address               NVARCHAR(200),
    @AddressDetail         NVARCHAR(200),
    @Memo                  NVARCHAR(MAX),
    @HepatitisBExcluded    BIT,
    @ConfirmSimilarPatient BIT,
    @OperatorName          NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- [X] 호출자 트랜잭션 안에서 실행할 수 없다. Write SP 는 savepoint 없이 BEGIN/COMMIT/ROLLBACK 을
    --     맨몸으로 쓰므로 @@TRANCOUNT > 0 으로 진입하면 네 가지가 동시에 깨진다.
    --       업무실패  이름 없는 ROLLBACK 이 **바깥 트랜잭션까지** 되돌리고 EXEC 반환 시 Msg 266
    --       성공      COMMIT 이 카운트만 줄여 아무것도 확정되지 않은 채 Success=1 이 나간다
    --       감사      스펙 §21.1 ① 의 '@@TRANCOUNT = 0 지점' 전제가 거짓이 되어 함께 롤백된다
    --       잠금      @LockOwner='Transaction' 이라 applock 이 바깥 트랜잭션까지 살아남는다
    --     진입에서 자른다. 스펙 §20 에 50003 으로 등재했다.
    IF @@TRANCOUNT > 0
        THROW 50003, N'이 프로시저는 호출자 트랜잭션 안에서 실행할 수 없습니다.', 1;
    SET XACT_ABORT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
    DECLARE @Today      DATE         = CONVERT(DATE, @ServerTime);
    DECLARE @StoredNow  DATETIME2(0) = CONVERT(DATETIME2(0), @ServerTime);

    DECLARE @Success BIT = 1, @Code INT = 0, @Field VARCHAR(50) = NULL;
    DECLARE @Msg NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @TargetKey  BIGINT = NULL;   -- 감사 기록 대상키. 업무 INSERT 직후 확정한다
    DECLARE @RowId      BIGINT = NULL;   -- RS1 이 낼 행. 신규 또는 기존
    DECLARE @ExistingId BIGINT = NULL, @ExistingName NVARCHAR(100) = NULL;
    DECLARE @Seq BIGINT = NULL, @Cand NVARCHAR(100) = NULL, @Done BIT = 0;
    DECLARE @rc INT, @ResSsn NVARCHAR(255), @ResChart NVARCHAR(255);
    DECLARE @C7 CHAR(1) = NULL, @Century VARCHAR(2) = NULL;
    DECLARE @Gender CHAR(1) = NULL, @Birthday VARCHAR(8) = NULL;

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 정규화 → 필수값 → 값 형식 → 조합 (05 §2.2 · §5)
    ----------------------------------------------------------------------------
    SET @ChartNo       = NULLIF(LTRIM(RTRIM(@ChartNo)), N'');
    SET @Name          = NULLIF(LTRIM(RTRIM(@Name)), N'');
    SET @SocialNumber  = NULLIF(REPLACE(LTRIM(RTRIM(@SocialNumber)), '-', ''), '');
    SET @MobilePhone   = NULLIF(LTRIM(RTRIM(@MobilePhone)), '');
    SET @Phone         = NULLIF(LTRIM(RTRIM(@Phone)), '');
    SET @Email         = NULLIF(LTRIM(RTRIM(@Email)), '');
    SET @Zipcode       = NULLIF(LTRIM(RTRIM(@Zipcode)), '');
    SET @Address       = NULLIF(LTRIM(RTRIM(@Address)), N'');
    SET @AddressDetail = NULLIF(LTRIM(RTRIM(@AddressDetail)), N'');
    SET @Memo          = NULLIF(LTRIM(RTRIM(@Memo)), N'');
    SET @OperatorName  = NULLIF(LTRIM(RTRIM(@OperatorName)), N'');

    IF @Name IS NULL
    BEGIN SET @Code = 100; SET @Field = 'Name'; END
    ELSE IF @SocialNumber IS NULL
    BEGIN SET @Code = 100; SET @Field = 'SocialNumber'; END
    ELSE IF @AutoChartNo IS NULL
    BEGIN SET @Code = 100; SET @Field = 'AutoChartNo'; END
    ELSE IF @HepatitisBExcluded IS NULL
    BEGIN SET @Code = 100; SET @Field = 'HepatitisBExcluded'; END
    ELSE IF @ConfirmSimilarPatient IS NULL
    BEGIN SET @Code = 100; SET @Field = 'ConfirmSimilarPatient'; END
    ELSE IF @OperatorName IS NULL
    BEGIN SET @Code = 100; SET @Field = 'OperatorName'; END
    ELSE IF LEN(@SocialNumber) <> 13 OR DATALENGTH(@SocialNumber) <> 13
         OR @SocialNumber LIKE '%[^0-9]%'
    BEGIN SET @Code = 101; SET @Field = 'SocialNumber'; END
    ELSE
    BEGIN
        -- 주민번호 7번째 자리에서 출생세기·성별을 해석한다 (05 §10.1 의 표 그대로).
        -- 9·0(1800년대생)은 여기서 101 로 거부한다. 통과시키면 계산열의 NOT NULL 이
        -- Msg 515 를 내며 ResultCode 가 아니라 예외로 튄다 (04 §8.1.2 실측).
        SET @C7      = SUBSTRING(@SocialNumber, 7, 1);
        SET @Century = CASE WHEN @C7 IN ('1','2','5','6') THEN '19'
                            WHEN @C7 IN ('3','4','7','8') THEN '20' END;
        SET @Gender  = CASE WHEN @C7 IN ('1','3','5','7') THEN 'M'
                            WHEN @C7 IN ('2','4','6','8') THEN 'F' END;
        SET @Birthday = @Century + SUBSTRING(@SocialNumber, 1, 6);

        IF @Century IS NULL OR @Gender IS NULL OR TRY_CONVERT(DATE, @Birthday, 112) IS NULL
        BEGIN SET @Code = 101; SET @Field = 'SocialNumber'; END
        -- CK_수검자_CEL_DIGIT 를 SP 가 먼저 확인한다. 없으면 Msg 547 이 되어 RS0 계약이 깨진다.
        ELSE IF @MobilePhone IS NOT NULL
             AND (REPLACE(@MobilePhone, '-', '') LIKE '%[^0-9]%'
                  OR LEN(REPLACE(@MobilePhone, '-', '')) NOT BETWEEN 10 AND 11
                  OR DATALENGTH(REPLACE(@MobilePhone, '-', '')) <> LEN(REPLACE(@MobilePhone, '-', '')))
        BEGIN SET @Code = 101; SET @Field = 'MobilePhone'; END
        -- AutoChartNo 조합 (05 §10.1)
        ELSE IF (@AutoChartNo = 1 AND @ChartNo IS NOT NULL)
             OR (@AutoChartNo = 0 AND @ChartNo IS NULL)
        BEGIN SET @Code = 102; SET @Field = 'ChartNo'; END
    END

    IF @Code = 100 SET @Msg = N'필수값을 입력하십시오.';
    IF @Code = 101 SET @Msg = N'입력값이 올바르지 않습니다.';
    IF @Code = 102 SET @Msg = N'함께 사용할 수 없는 입력값 조합입니다.';
    IF @Code <> 0 SET @Success = 0;

    ----------------------------------------------------------------------------
    -- [2]~[5] 후보 1개당 Transaction 1개 (스펙 §12.6)
    --   NEXT VALUE FOR 는 반드시 Transaction 밖에서 부른다. Msg 11728(MAXVALUE 도달)이
    --   열린 Transaction 안에서 나면 XACT_ABORT ON 이 doomed 를 만들어 COMMIT 이 Msg 3930 이 된다.
    --   Sequence 값은 롤백과 무관하게 소비되며 04 §3.6 이 결번을 허용한다.
    ----------------------------------------------------------------------------
    -- [X] 05 §10.1 검증순서는 '자동 ChartNo 발급' 을 '현재 공통 업무 가능' **뒤**에 둔다.
    --     구현은 Msg 11728 회피를 위해 발급을 Transaction 밖으로 뺐는데, 그러면서 업무시간
    --     검사보다 **앞**으로 나가 버렸다. 그 결과 업무시간 밖 자동등록 호출이 실패하면서도
    --     차트번호를 하나씩 먹는다 — MAXVALUE 999999 이고 고갈되면 재배포 없이 복구되지 않는다.
    --     Transaction 안의 재검증이 여전히 권위값이다. 여기서는 **소모 전에** 미리 자른다.
    IF @Code = 0 AND @AutoChartNo = 1
        SELECT @Code = s.WorkCode, @Msg = s.WorkMessage
          FROM [dbo].[UFN_HC_일정확인](@ServerTime, @Today, 'AM', 'NONE') s
         WHERE s.CanWorkNow = 0;
    IF @Code IN (308, 309) BEGIN SET @Success = 0; SET @Field = NULL; END

    IF @Code = 0
    BEGIN
        -- 자원명에 주민번호 원문을 싣지 않는다. DMV·오류 메시지 노출 차단 (스펙 §22).
        SET @ResSsn = N'HC|SSN|' + CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @SocialNumber), 2);
        IF @AutoChartNo = 0 SET @Cand = @ChartNo;

        BEGIN TRY
            WHILE @Done = 0
            BEGIN
                -- (a) Transaction 밖 : 다음 자동발급 후보를 확보한다
                IF @Cand IS NULL
                BEGIN
                    BEGIN TRY
                        SET @Seq  = NEXT VALUE FOR [dbo].[SEQ_HC_CHART_NO];
                        SET @Cand = N'C' + RIGHT(N'000000' + CONVERT(NVARCHAR(6), @Seq), 6);
                    END TRY
                    BEGIN CATCH
                        -- 206 은 Sequence 가 MAXVALUE 에 도달했을 때만이다. 반복 횟수 상한을 두지 않는다.
                        IF ERROR_NUMBER() = 11728
                        BEGIN
                            SET @Success = 0; SET @Code = 206; SET @Field = 'ChartNo';
                            SET @Msg  = N'자동 차트번호 발급범위를 초과했습니다.';
                            SET @Done = 1;
                        END
                        ELSE THROW;
                    END CATCH
                END

                IF @Done = 0
                BEGIN
                    BEGIN TRANSACTION;

                    -- [3] applock : SSN(1) → CHART(2)
                    EXEC @rc = sp_getapplock @Resource = @ResSsn, @LockMode = 'Exclusive',
                                             @LockOwner = 'Transaction', @LockTimeout = 5000;
                    PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
                    IF @rc < 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                        THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
                    END

                    -- 자동발급 경로도 후보마다 CHART 를 잡는다. 잡지 않으면 다른 세션의 수동입력과
                    -- 겹쳐 Msg 2627 이 나는데, 스펙 §20 은 그것을 설계 위반으로 규정했다.
                    -- [X] applock 은 바이트 비교, UQ_수검자_CHART_NO 는 CI·폭무시다 (실측).
                    --     정규화 없이 이어 붙이면 'c000001' 과 'C000001' 이 서로 다른 자원을 잠가
                    --     둘 다 EXISTS 를 통과하고 뒤쪽이 Msg 2627 로 죽는다 — 위 주석이 막겠다던 그 상황이다.
                    SET @ResChart = N'HC|CHART|' + UPPER(@Cand);
                    EXEC @rc = sp_getapplock @Resource = @ResChart, @LockMode = 'Exclusive',
                                             @LockOwner = 'Transaction', @LockTimeout = 5000;
                    PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
                    IF @rc < 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                        THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
                    END

                    -- [4] 재검증 — 05 §10.1 검증순서
                    -- 현재 공통 업무 가능
                    SELECT @Code = s.WorkCode, @Msg = s.WorkMessage
                      FROM [dbo].[UFN_HC_일정확인](@ServerTime, @Today, 'AM', 'NONE') s
                     WHERE s.CanWorkNow = 0;
                    IF @Code <> 0 SET @Success = 0;

                    -- 동일 주민번호 기존 수검자 (UQ_수검자_SOCIAL_NUMBER 로 최대 1행)
                    IF @Code = 0
                    BEGIN
                        SELECT @ExistingId = p.[수검자ID], @ExistingName = p.[성명]
                          FROM [dbo].[수검자] p
                         WHERE p.[주민번호] = @SocialNumber;

                        IF @ExistingId IS NOT NULL
                        BEGIN
                            SET @RowId = @ExistingId;
                            IF @ExistingName = @Name
                            BEGIN
                                SET @Success = 1; SET @Code = 2; SET @Field = NULL;
                                SET @Msg = N'동일한 수검자가 이미 등록되어 있어 기존 정보를 사용합니다.';
                            END
                            ELSE
                            BEGIN
                                SET @Success = 0; SET @Code = 202; SET @Field = 'Name';
                                SET @Msg = N'동일한 주민등록번호의 기존 수검자와 이름이 다릅니다.';
                            END
                        END
                    END

                    -- 이름 + 산출 Birthday 후보. ConfirmSimilarPatient=1 은 이번 요청의
                    -- Name + Birthday + SocialNumber 조합에만 유효하다 (05 §10.1).
                    IF @Code = 0 AND @ConfirmSimilarPatient = 0
                       AND EXISTS (SELECT 1 FROM [dbo].[수검자] p
                                    WHERE p.[성명] = @Name AND p.[생년월일] = @Birthday)
                    BEGIN
                        SET @Success = 0; SET @Code = 203; SET @Field = NULL;
                        SET @Msg = N'이름과 생년월일이 같은 수검자가 있습니다.';
                    END

                    -- 차트번호 고유성. 수동은 201, 자동은 잠금을 놓고 다음 후보로 간다.
                    IF @Code = 0 AND EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [차트번호] = @Cand)
                    BEGIN
                        IF @AutoChartNo = 0
                        BEGIN
                            SET @Success = 0; SET @Code = 201; SET @Field = 'ChartNo';
                            SET @Msg = N'이미 사용 중인 차트번호입니다.';
                        END
                        ELSE
                        BEGIN
                            ROLLBACK TRANSACTION;
                            SET @Cand = NULL;
                        END
                    END

                    -- [5] 저장. @Cand IS NULL 이면 위에서 ROLLBACK 하고 재시도로 넘어간 것이다.
                    IF @Cand IS NOT NULL
                    BEGIN
                        IF @Code = 0
                        BEGIN
                            -- 생년월일·성별은 계산열이라 이 목록에 없다 (Msg 271).
                            INSERT INTO [dbo].[수검자]
                                ([차트번호], [성명], [주민번호], [이메일], [휴대전화], [전화번호],
                                 [우편번호], [주소], [상세주소], [비고], [B형간염제외여부])
                            VALUES
                                (@Cand, @Name, @SocialNumber, @Email, @MobilePhone, @Phone,
                                 @Zipcode, @Address, @AddressDetail, @Memo, @HepatitisBExcluded);

                            -- 감사 INSERT 뒤의 SCOPE_IDENTITY() 는 이력ID 를 돌려준다 (04 §8.6.5).
                            SET @TargetKey = SCOPE_IDENTITY();
                            SET @RowId     = @TargetKey;
                            COMMIT TRANSACTION;
                        END
                        ELSE IF @Code = 2
                            COMMIT TRANSACTION;   -- 쓴 것이 없어 ROLLBACK 과 같으나 성공 경로라 COMMIT 한다
                        ELSE
                            ROLLBACK TRANSACTION;

                        SET @Done = 1;
                    END
                END
            END
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            THROW;   -- 예상하지 못한 오류는 감사 기록하지 않는다 (04 §14 L3)
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

    -- RS1 수검자결과 — 성공(0·2)과 실패 202·203 에만 낸다 (05 §3.5)
    -- [I] 여기는 커밋 뒤 재조회를 유지한다 (06 §43-18 의 예외).
    --     202·203 이 돌려주는 것은 **다른 사람의 행**이라 우리 잠금이 보호한 적이 없다 —
    --     포획해도 스냅샷일 뿐이고 살아 있는 값을 보여주는 편이 정직하다.
    --     0 은 방금 만든 행이라 그 PatientId 를 아는 세션이 아직 없다.
    IF @Code IN (0, 2, 202, 203)
        SELECT
              PatientId    = CAST(p.[수검자ID]     AS BIGINT)
            , ChartNo      = CAST(p.[차트번호]     AS NVARCHAR(100))
            , Name         = CAST(p.[성명]         AS NVARCHAR(100))
            , SocialNumber = CAST(p.[주민번호]     AS VARCHAR(13))
            , Birthday     = CAST(p.[생년월일]     AS VARCHAR(8))
            , Gender       = CAST(p.[성별]         AS CHAR(1))
            , MobilePhone  = CAST(p.[휴대전화]     AS VARCHAR(13))
            , LastEditDate = CAST(p.[최종수정일시] AS DATETIME)
          FROM [dbo].[수검자] p
         WHERE (@RowId IS NOT NULL AND p.[수검자ID] = @RowId)
            OR (@Code = 203 AND p.[성명] = @Name AND p.[생년월일] = @Birthday)
         ORDER BY p.[수검자ID] ASC;

    ----------------------------------------------------------------------------
    -- [7] 감사 기록 (04 §8.6.5 · 스펙 §21.1)
    --   ① @@TRANCOUNT = 0 지점의 자동커밋  ② 자체 TRY/CATCH 이고 CATCH 는 비운다
    --   ③ 해당 Result Set 을 먼저 낸 뒤에 실행한다
    --   INSERT 라 변경전은 항상 NULL 이다. 값이 없던 컬럼은 남기지 않는다 (04 §8.6.2).
    ----------------------------------------------------------------------------
    IF @Code = 0 AND @TargetKey IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            SELECT @StoredNow, @OperatorName, N'수검자', @TargetKey, V.[컬럼명], NULL, V.[변경후]
              FROM (VALUES
                      (N'차트번호',        CONVERT(NVARCHAR(4000), @Cand))
                    , (N'성명',            CONVERT(NVARCHAR(4000), @Name))
                    , (N'주민번호',        CONVERT(NVARCHAR(4000), @SocialNumber))
                    , (N'이메일',          CONVERT(NVARCHAR(4000), @Email))
                    , (N'휴대전화',        CONVERT(NVARCHAR(4000), @MobilePhone))
                    , (N'전화번호',        CONVERT(NVARCHAR(4000), @Phone))
                    , (N'우편번호',        CONVERT(NVARCHAR(4000), @Zipcode))
                    , (N'주소',            CONVERT(NVARCHAR(4000), @Address))
                    , (N'상세주소',        CONVERT(NVARCHAR(4000), @AddressDetail))
                    , (N'비고',            CONVERT(NVARCHAR(4000), @Memo))
                    , (N'B형간염제외여부', CONVERT(NVARCHAR(4000), @HepatitisBExcluded))
                   ) V([컬럼명], [변경후])
             WHERE V.[변경후] IS NOT NULL;
        END TRY
        BEGIN CATCH
        END CATCH
    END
END
GO
-- 05 §10.2. 검증순서는 존재 → LastEditDate → 실제 변경 여부 → 공통 업무가능 →
-- ChartNo 고유성 → SocialNumber 고유성 → 주민번호 변경 시 RSV/RCP 부재 순이다.
-- No-op(Code=1) 판정이 308/309 보다 앞이라는 것이 계약이므로 그대로 따른다.
--
-- [!] applock SSN → CHART → PAT 을 조건 없이 항상 잡는다 (스펙 §24.1). "변경 시에만" 으로 두면
--     변경 여부를 알기 위해 현재 행을 먼저 읽어야 하고, 그러면 PAT(랭크 3)을 SSN(랭크 1)보다
--     먼저 잡게 되어 §23 전역 순서가 뒤집힌다. 자원명은 요청값으로 바로 만든다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_UPDATE_수검자정보]
    @PatientId          BIGINT,
    @LastEditDate       DATETIME,
    @ChartNo            NVARCHAR(100),
    @Name               NVARCHAR(100),
    @SocialNumber       VARCHAR(13),
    @MobilePhone        VARCHAR(13),
    @Phone              VARCHAR(13),
    @Email              VARCHAR(200),
    @Zipcode            VARCHAR(10),
    @Address            NVARCHAR(200),
    @AddressDetail      NVARCHAR(200),
    @Memo               NVARCHAR(MAX),
    @HepatitisBExcluded BIT,
    @OperatorName       NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- [X] 호출자 트랜잭션 안에서 실행할 수 없다. Write SP 는 savepoint 없이 BEGIN/COMMIT/ROLLBACK 을
    --     맨몸으로 쓰므로 @@TRANCOUNT > 0 으로 진입하면 네 가지가 동시에 깨진다.
    --       업무실패  이름 없는 ROLLBACK 이 **바깥 트랜잭션까지** 되돌리고 EXEC 반환 시 Msg 266
    --       성공      COMMIT 이 카운트만 줄여 아무것도 확정되지 않은 채 Success=1 이 나간다
    --       감사      스펙 §21.1 ① 의 '@@TRANCOUNT = 0 지점' 전제가 거짓이 되어 함께 롤백된다
    --       잠금      @LockOwner='Transaction' 이라 applock 이 바깥 트랜잭션까지 살아남는다
    --     진입에서 자른다. 스펙 §20 에 50003 으로 등재했다.
    IF @@TRANCOUNT > 0
        THROW 50003, N'이 프로시저는 호출자 트랜잭션 안에서 실행할 수 없습니다.', 1;
    SET XACT_ABORT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
    DECLARE @RsChart NVARCHAR(100), @RsEdit DATETIME;
    DECLARE @Today      DATE         = CONVERT(DATE, @ServerTime);
    DECLARE @StoredNow  DATETIME2(0) = CONVERT(DATETIME2(0), @ServerTime);

    DECLARE @Success BIT = 1, @Code INT = 0, @Field VARCHAR(50) = NULL;
    DECLARE @Msg NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @TargetKey BIGINT = NULL;
    DECLARE @rc INT, @ResSsn NVARCHAR(255), @ResChart NVARCHAR(255), @ResPat NVARCHAR(255);
    DECLARE @C7 CHAR(1) = NULL, @Century VARCHAR(2) = NULL;
    DECLARE @Gender CHAR(1) = NULL, @Birthday VARCHAR(8) = NULL;
    DECLARE @NewEdit DATETIME = NULL;

    -- 변경전 값. Transaction 안의 재검증 단계에서 한 번 읽어 No-op 판정과 감사 기록에 재사용한다
    -- (04 §8.6.5 "추가 조회를 하지 않는다").
    DECLARE @OldEdit       DATETIME      = NULL;
    DECLARE @OldChartNo    NVARCHAR(100) = NULL, @OldName  NVARCHAR(100) = NULL;
    DECLARE @OldSsn        VARCHAR(13)   = NULL, @OldMobile VARCHAR(13)  = NULL;
    DECLARE @OldPhone      VARCHAR(13)   = NULL, @OldEmail VARCHAR(200)  = NULL;
    DECLARE @OldZip        VARCHAR(10)   = NULL, @OldAddr  NVARCHAR(200) = NULL;
    DECLARE @OldAddrDetail NVARCHAR(200) = NULL, @OldMemo  NVARCHAR(MAX) = NULL;
    DECLARE @OldHep        BIT           = NULL;

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 정규화 → 필수값 → 값 형식
    ----------------------------------------------------------------------------
    SET @ChartNo       = NULLIF(LTRIM(RTRIM(@ChartNo)), N'');
    SET @Name          = NULLIF(LTRIM(RTRIM(@Name)), N'');
    SET @SocialNumber  = NULLIF(REPLACE(LTRIM(RTRIM(@SocialNumber)), '-', ''), '');
    SET @MobilePhone   = NULLIF(LTRIM(RTRIM(@MobilePhone)), '');
    SET @Phone         = NULLIF(LTRIM(RTRIM(@Phone)), '');
    SET @Email         = NULLIF(LTRIM(RTRIM(@Email)), '');
    SET @Zipcode       = NULLIF(LTRIM(RTRIM(@Zipcode)), '');
    SET @Address       = NULLIF(LTRIM(RTRIM(@Address)), N'');
    SET @AddressDetail = NULLIF(LTRIM(RTRIM(@AddressDetail)), N'');
    SET @Memo          = NULLIF(LTRIM(RTRIM(@Memo)), N'');
    SET @OperatorName  = NULLIF(LTRIM(RTRIM(@OperatorName)), N'');

    IF @PatientId IS NULL
    BEGIN SET @Code = 100; SET @Field = 'PatientId'; END
    ELSE IF @LastEditDate IS NULL
    BEGIN SET @Code = 100; SET @Field = 'LastEditDate'; END
    ELSE IF @ChartNo IS NULL
    BEGIN SET @Code = 100; SET @Field = 'ChartNo'; END
    ELSE IF @Name IS NULL
    BEGIN SET @Code = 100; SET @Field = 'Name'; END
    ELSE IF @SocialNumber IS NULL
    BEGIN SET @Code = 100; SET @Field = 'SocialNumber'; END
    ELSE IF @HepatitisBExcluded IS NULL
    BEGIN SET @Code = 100; SET @Field = 'HepatitisBExcluded'; END
    ELSE IF @OperatorName IS NULL
    BEGIN SET @Code = 100; SET @Field = 'OperatorName'; END
    ELSE IF LEN(@SocialNumber) <> 13 OR DATALENGTH(@SocialNumber) <> 13
         OR @SocialNumber LIKE '%[^0-9]%'
    BEGIN SET @Code = 101; SET @Field = 'SocialNumber'; END
    ELSE
    BEGIN
        SET @C7      = SUBSTRING(@SocialNumber, 7, 1);
        SET @Century = CASE WHEN @C7 IN ('1','2','5','6') THEN '19'
                            WHEN @C7 IN ('3','4','7','8') THEN '20' END;
        SET @Gender  = CASE WHEN @C7 IN ('1','3','5','7') THEN 'M'
                            WHEN @C7 IN ('2','4','6','8') THEN 'F' END;
        SET @Birthday = @Century + SUBSTRING(@SocialNumber, 1, 6);

        IF @Century IS NULL OR @Gender IS NULL OR TRY_CONVERT(DATE, @Birthday, 112) IS NULL
        BEGIN SET @Code = 101; SET @Field = 'SocialNumber'; END
        ELSE IF @MobilePhone IS NOT NULL
             AND (REPLACE(@MobilePhone, '-', '') LIKE '%[^0-9]%'
                  OR LEN(REPLACE(@MobilePhone, '-', '')) NOT BETWEEN 10 AND 11
                  OR DATALENGTH(REPLACE(@MobilePhone, '-', '')) <> LEN(REPLACE(@MobilePhone, '-', '')))
        BEGIN SET @Code = 101; SET @Field = 'MobilePhone'; END
    END

    IF @Code = 100 SET @Msg = N'필수값을 입력하십시오.';
    IF @Code = 101 SET @Msg = N'입력값이 올바르지 않습니다.';
    IF @Code <> 0 SET @Success = 0;

    ----------------------------------------------------------------------------
    -- [3]~[5] Transaction
    ----------------------------------------------------------------------------
    IF @Code = 0
    BEGIN
        SET @ResSsn   = N'HC|SSN|' + CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @SocialNumber), 2);
        -- [X] 자원명은 UPPER 로 맞춘다. applock 은 바이트 비교, UQ 는 CI 다 (05 §22 · 실측).
        SET @ResChart = N'HC|CHART|' + UPPER(@ChartNo);
        SET @ResPat   = N'HC|PAT|' + CONVERT(NVARCHAR(20), @PatientId);

        BEGIN TRY
            BEGIN TRANSACTION;

            EXEC @rc = sp_getapplock @Resource = @ResSsn, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
            IF @rc < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            EXEC @rc = sp_getapplock @Resource = @ResChart, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
            IF @rc < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            EXEC @rc = sp_getapplock @Resource = @ResPat, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);
            IF @rc < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @rc = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            -- [4] 재검증. 최종수정일시는 NOT NULL 이므로 NULL 은 곧 미존재다.
            SELECT @OldEdit       = p.[최종수정일시]
                 , @OldChartNo    = p.[차트번호]
                 , @OldName       = p.[성명]
                 , @OldSsn        = p.[주민번호]
                 , @OldMobile     = p.[휴대전화]
                 , @OldPhone      = p.[전화번호]
                 , @OldEmail      = p.[이메일]
                 , @OldZip        = p.[우편번호]
                 , @OldAddr       = p.[주소]
                 , @OldAddrDetail = p.[상세주소]
                 , @OldMemo       = p.[비고]
                 , @OldHep        = p.[B형간염제외여부]
              FROM [dbo].[수검자] p
             WHERE p.[수검자ID] = @PatientId;

            IF @OldEdit IS NULL
            BEGIN
                SET @Success = 0; SET @Code = 200; SET @Field = 'PatientId';
                SET @Msg = N'수검자를 찾을 수 없습니다.';
            END
            ELSE IF @OldEdit <> @LastEditDate
            BEGIN
                SET @Success = 0; SET @Code = 600; SET @Field = 'LastEditDate';
                SET @Msg = N'다른 사용자가 수검자 정보를 변경했습니다. 최신 정보를 다시 조회하십시오.';
            END
            -- 실제 변경 여부는 NULL-safe 여야 한다 (스펙 §28.2). <> 로 비교하면 NULL <> 'x' 가
            -- UNKNOWN 이라 "변경 없음" 이 되어 사용자 편집이 조용히 소실된다. INTERSECT 는
            -- NULL = NULL 을 참으로 다룬다. 비고만 NVARCHAR(MAX) 라 INTERSECT 피연산자가 될 수
            -- 없어 명시적 NULL-safe 비교로 분리한다.
            --
            -- [X] NULL-safe 만으로는 부족했다. 정렬이 Korean_Wansung_CI_AS 라 INTERSECT 가
            --     'hong@GMAIL.com' 과 'hong@gmail.com' 을 **같다고** 본다(실측). 대소문자만 고친
            --     이메일·주소·차트번호 수정이 Code=1 '변경된 내용이 없습니다' 로 돌아오고
            --     편집이 조용히 사라졌다 — NULL 함정을 막으려던 그 자리에 정렬 함정이 남아 있었다.
            --     COLLATE 는 06 §9.2 허용목록 밖이므로 VARBINARY 변환으로 바이트 비교한다.
            --     INTERSECT 의 NULL = NULL 성질은 그대로 유지된다 (CONVERT(VARBINARY, NULL) 도 NULL).
            ELSE IF EXISTS (
                        SELECT CONVERT(VARBINARY(400), @OldChartNo), CONVERT(VARBINARY(400), @OldName)
                             , CONVERT(VARBINARY(400), @OldSsn),     CONVERT(VARBINARY(400), @OldMobile)
                             , CONVERT(VARBINARY(400), @OldPhone),   CONVERT(VARBINARY(400), @OldEmail)
                             , CONVERT(VARBINARY(400), @OldZip),     CONVERT(VARBINARY(400), @OldAddr)
                             , CONVERT(VARBINARY(400), @OldAddrDetail), CONVERT(VARBINARY(400), @OldHep)
                        INTERSECT
                        SELECT CONVERT(VARBINARY(400), @ChartNo),    CONVERT(VARBINARY(400), @Name)
                             , CONVERT(VARBINARY(400), @SocialNumber), CONVERT(VARBINARY(400), @MobilePhone)
                             , CONVERT(VARBINARY(400), @Phone),      CONVERT(VARBINARY(400), @Email)
                             , CONVERT(VARBINARY(400), @Zipcode),    CONVERT(VARBINARY(400), @Address)
                             , CONVERT(VARBINARY(400), @AddressDetail), CONVERT(VARBINARY(400), @HepatitisBExcluded)
                    )
                    AND ((@OldMemo IS NULL AND @Memo IS NULL)
                         OR CONVERT(VARBINARY(MAX), @OldMemo) = CONVERT(VARBINARY(MAX), @Memo))
            BEGIN
                SET @Success = 1; SET @Code = 1; SET @Field = NULL;
                SET @Msg = N'변경된 내용이 없습니다.';
            END
            ELSE
            BEGIN
                SELECT @Code = s.WorkCode, @Msg = s.WorkMessage
                  FROM [dbo].[UFN_HC_일정확인](@ServerTime, @Today, 'AM', 'NONE') s
                 WHERE s.CanWorkNow = 0;
                IF @Code <> 0 SET @Success = 0;

                IF @Code = 0 AND @ChartNo <> @OldChartNo
                   AND EXISTS (SELECT 1 FROM [dbo].[수검자]
                                WHERE [차트번호] = @ChartNo AND [수검자ID] <> @PatientId)
                BEGIN
                    SET @Success = 0; SET @Code = 201; SET @Field = 'ChartNo';
                    SET @Msg = N'이미 사용 중인 차트번호입니다.';
                END

                IF @Code = 0 AND @SocialNumber <> @OldSsn
                   AND EXISTS (SELECT 1 FROM [dbo].[수검자]
                                WHERE [주민번호] = @SocialNumber AND [수검자ID] <> @PatientId)
                BEGIN
                    SET @Success = 0; SET @Code = 204; SET @Field = 'SocialNumber';
                    SET @Msg = N'다른 수검자가 사용 중인 주민등록번호입니다.';
                END

                -- 00 EP-08. 차트번호 변경은 활성 업무가 있어도 차단하지 않는다.
                IF @Code = 0 AND @SocialNumber <> @OldSsn
                   AND EXISTS (SELECT 1 FROM [dbo].[예약접수]
                                WHERE [수검자ID] = @PatientId AND [상태코드] IN ('RSV','RCP'))
                BEGIN
                    SET @Success = 0; SET @Code = 205; SET @Field = 'SocialNumber';
                    SET @Msg = N'예약 또는 접수완료 업무가 있어 주민등록번호를 변경할 수 없습니다.';
                END

                IF @Code = 0
                BEGIN
                    -- LastEditDate 단조증가 (스펙 §26). DATETIME 은 약 3.33ms 틱이라 +1ms 는
                    -- 값이 변하지 않는다. +4ms 가 최소 안전값이다.
                    SET @NewEdit = CONVERT(DATETIME, @ServerTime);
                    IF @NewEdit <= @OldEdit SET @NewEdit = DATEADD(MILLISECOND, 4, @OldEdit);

                    -- 생년월일·성별은 계산열이라 SET 목록에 없다 (Msg 271).
                    -- 주민번호를 바꾸면 계산열이 자동으로 다시 유도한다.
                    UPDATE [dbo].[수검자]
                       SET [차트번호]        = @ChartNo
                         , [성명]            = @Name
                         , [주민번호]        = @SocialNumber
                         , [이메일]          = @Email
                         , [휴대전화]        = @MobilePhone
                         , [전화번호]        = @Phone
                         , [우편번호]        = @Zipcode
                         , [주소]            = @Address
                         , [상세주소]        = @AddressDetail
                         , [비고]            = @Memo
                         , [B형간염제외여부] = @HepatitisBExcluded
                         , [최종수정일시]    = @NewEdit
                     WHERE [수검자ID]     = @PatientId
                       AND [최종수정일시] = @OldEdit;   -- 낙관적 동시성

                    IF @@ROWCOUNT = 0
                    BEGIN
                        SET @Success = 0; SET @Code = 600; SET @Field = 'LastEditDate';
                        SET @Msg = N'다른 사용자가 수검자 정보를 변경했습니다. 최신 정보를 다시 조회하십시오.';
                    END
                    ELSE
                        SET @TargetKey = @PatientId;
                END
            END

            -- [X] RS1 을 COMMIT **뒤에** 다시 읽으면 그 사이 다른 세션이 바꾼 LastEditDate 가 나간다.
            --     호출자는 자기 것이 아닌 동시성 토큰을 받고, 그 값으로 보낸 다음 요청이
            --     600 으로 막혔어야 하는데 통과한다. 잠금 안에서 포획한다 (06 §43-18).
            IF @Code IN (0, 1)
                SELECT @RsChart = p.[차트번호], @RsEdit = p.[최종수정일시]
                  FROM [dbo].[수검자] p WHERE p.[수검자ID] = @PatientId;

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

    -- RS1 수검자변경결과. No-op 은 갱신하지 않은 기존 LastEditDate 가 그대로 나온다 (05 §10.2).
    -- 값은 잠금 안에서 포획한 것이다 (아래 [X] 참조). 커밋 뒤 재조회하지 않는다.
    IF @Code IN (0, 1)
        SELECT
              PatientId    = CAST(@PatientId AS BIGINT)
            , ChartNo      = CAST(@RsChart   AS NVARCHAR(100))
            , LastEditDate = CAST(@RsEdit    AS DATETIME);

    ----------------------------------------------------------------------------
    -- [7] 감사 기록. 실제로 값이 바뀐 컬럼만 남는다 (00 CP-06 · 04 §8.6.2).
    --   최종수정일시는 동시성 stamp 이지 사용자 데이터가 아니라 기록하지 않는다.
    ----------------------------------------------------------------------------
    IF @Code = 0 AND @TargetKey IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            SELECT @StoredNow, @OperatorName, N'수검자', @TargetKey, V.[컬럼명], V.[변경전], V.[변경후]
              FROM (VALUES
                      (N'차트번호',        CONVERT(NVARCHAR(4000), @OldChartNo),    CONVERT(NVARCHAR(4000), @ChartNo))
                    , (N'성명',            CONVERT(NVARCHAR(4000), @OldName),       CONVERT(NVARCHAR(4000), @Name))
                    , (N'주민번호',        CONVERT(NVARCHAR(4000), @OldSsn),        CONVERT(NVARCHAR(4000), @SocialNumber))
                    , (N'이메일',          CONVERT(NVARCHAR(4000), @OldEmail),      CONVERT(NVARCHAR(4000), @Email))
                    , (N'휴대전화',        CONVERT(NVARCHAR(4000), @OldMobile),     CONVERT(NVARCHAR(4000), @MobilePhone))
                    , (N'전화번호',        CONVERT(NVARCHAR(4000), @OldPhone),      CONVERT(NVARCHAR(4000), @Phone))
                    , (N'우편번호',        CONVERT(NVARCHAR(4000), @OldZip),        CONVERT(NVARCHAR(4000), @Zipcode))
                    , (N'주소',            CONVERT(NVARCHAR(4000), @OldAddr),       CONVERT(NVARCHAR(4000), @Address))
                    , (N'상세주소',        CONVERT(NVARCHAR(4000), @OldAddrDetail), CONVERT(NVARCHAR(4000), @AddressDetail))
                    , (N'비고',            CONVERT(NVARCHAR(4000), @OldMemo),       CONVERT(NVARCHAR(4000), @Memo))
                    , (N'B형간염제외여부', CONVERT(NVARCHAR(4000), @OldHep),        CONVERT(NVARCHAR(4000), @HepatitisBExcluded))
                   ) V([컬럼명], [변경전], [변경후])
             -- [X] No-op 판정과 **같은 기준**이어야 한다. 여기만 CI 로 두면 대소문자만 고친 수정이
             --     저장은 되는데 변경이력에 한 줄도 남지 않는다. 바이트 비교로 맞춘다.
             --     ISNULL 센티널 대신 NULL 경우를 명시한다 — VARBINARY 에는 안전한 센티널이 없다.
             WHERE (V.[변경전] IS NULL     AND V.[변경후] IS NOT NULL)
                OR (V.[변경전] IS NOT NULL AND V.[변경후] IS NULL)
                OR (V.[변경전] IS NOT NULL AND V.[변경후] IS NOT NULL
                    AND CONVERT(VARBINARY(MAX), V.[변경전]) <> CONVERT(VARBINARY(MAX), V.[변경후]));
        END TRY
        BEGIN CATCH
        END CATCH
    END
END
GO
