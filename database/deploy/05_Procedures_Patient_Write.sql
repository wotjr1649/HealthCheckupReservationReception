SET QUOTED_IDENTIFIER ON;   -- 01_Schema.sql 과 같은 설정으로 객체를 만든다 (CLAUDE.md §6)
GO
SET NOCOUNT ON;
GO
-- 수검자 Write SP 2개 (05 §10). Write SP 공통 Template 은 스펙 §21.1 이다.
--   SET XACT_ABORT ON · @서버시각 1회 캡처 · 입력검증은 Transaction 밖 · RS 는 Transaction 이 닫힌 뒤.
--   applock 은 전역 순서 SSN(1) → CHART(2) → PAT(3) 을 지킨다 (스펙 §23). Owner 가 Transaction 이라
--   COMMIT/ROLLBACK 이 자동 해제한다 — sp_releaseapplock 을 부르지 않는다.
--
-- [!] 생년월일·성별은 PERSISTED 계산열이다 (04 §8.1.2). INSERT/UPDATE 의 컬럼 목록에 넣으면
--     Msg 271 로 죽는다. 아래 @생년월일 는 저장용이 아니라 203 후보검색용 파생값이고
--     @성별 는 7번째 자리 검증에만 쓴다. 저장값은 DB 가 주민번호에서 유도한다.
--
-- [!] 휴대전화·전화번호의 '-' 는 제거하지 않는다. 05 §2.2 가 '-' 제거를 **검색값**으로 한정했고
--     04 §8.1.2 는 표시값 그대로 저장한다고 정했다. CK_수검자_CEL_DIGIT 도 REPLACE 로 비교한다.
--     plans/04 Step 2 의 "휴대전화/전화번호 에서 '-' 제거" 는 기준선과 어긋나 따르지 않는다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_수검자_등록]
    @차트번호자동발급여부           BIT,
    @차트번호               NVARCHAR(100),
    @성명                  NVARCHAR(100),
    @주민번호          VARCHAR(13),
    @휴대전화           VARCHAR(13),
    @전화번호                 VARCHAR(13),
    @이메일                 VARCHAR(200),
    @우편번호               VARCHAR(10),
    @주소               NVARCHAR(200),
    @상세주소         NVARCHAR(200),
    @비고                  NVARCHAR(MAX),
    @B형간염제외여부    BIT,
    @유사수검자확인여부 BIT,
    @조작자명          NVARCHAR(50)
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
    DECLARE @오늘날짜      DATE         = CONVERT(DATE, @서버시각);
    -- [R12] 이 SP 에서는 쓰이지 않는다. UFN_HC_일정확인 호출이 유일한 소비처였다.
    --       05 §2.3 이 정한 표준 서두라 지우지 않는다 — 날짜 판정이 남아 있다는 뜻이 아니다.
    DECLARE @저장시각  DATETIME2(0) = CONVERT(DATETIME2(0), @서버시각);

    DECLARE @성공여부 BIT = 1, @결과코드 INT = 0, @오류항목 NVARCHAR(50) = NULL;
    DECLARE @결과메시지 NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @대상키  BIGINT = NULL;   -- 감사 기록 대상키. 업무 INSERT 직후 확정한다
    DECLARE @결과행ID      BIGINT = NULL;   -- RS1 이 낼 행. 신규 또는 기존
    DECLARE @기존수검자ID BIGINT = NULL, @기존성명 NVARCHAR(100) = NULL;
    DECLARE @순번값 BIGINT = NULL, @후보차트번호 NVARCHAR(100) = NULL, @완료여부 BIT = 0;
    DECLARE @잠금결과 INT, @자원주민번호 NVARCHAR(255), @자원차트번호 NVARCHAR(255);
    DECLARE @주민7번째자리 CHAR(1) = NULL, @출생세기 VARCHAR(2) = NULL;
    DECLARE @성별 CHAR(1) = NULL, @생년월일 VARCHAR(8) = NULL;

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 정규화 → 필수값 → 값 형식 → 조합 (05 §2.2 · §5)
    ----------------------------------------------------------------------------
    SET @차트번호       = NULLIF(LTRIM(RTRIM(@차트번호)), N'');
    SET @성명          = NULLIF(LTRIM(RTRIM(@성명)), N'');
    SET @주민번호  = NULLIF(REPLACE(LTRIM(RTRIM(@주민번호)), '-', ''), '');
    SET @휴대전화   = NULLIF(LTRIM(RTRIM(@휴대전화)), '');
    SET @전화번호         = NULLIF(LTRIM(RTRIM(@전화번호)), '');
    SET @이메일         = NULLIF(LTRIM(RTRIM(@이메일)), '');
    SET @우편번호       = NULLIF(LTRIM(RTRIM(@우편번호)), '');
    SET @주소       = NULLIF(LTRIM(RTRIM(@주소)), N'');
    SET @상세주소 = NULLIF(LTRIM(RTRIM(@상세주소)), N'');
    SET @비고          = NULLIF(LTRIM(RTRIM(@비고)), N'');
    SET @조작자명  = NULLIF(LTRIM(RTRIM(@조작자명)), N'');

    IF @성명 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'성명'; END
    ELSE IF @주민번호 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'주민번호'; END
    ELSE IF @차트번호자동발급여부 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'차트번호자동발급여부'; END
    ELSE IF @B형간염제외여부 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'B형간염제외여부'; END
    ELSE IF @유사수검자확인여부 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'유사수검자확인여부'; END
    ELSE IF @조작자명 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'조작자명'; END
    ELSE IF LEN(@주민번호) <> 13 OR DATALENGTH(@주민번호) <> 13
         OR @주민번호 LIKE '%[^0-9]%'
    BEGIN SET @결과코드 = 101; SET @오류항목 = N'주민번호'; END
    ELSE
    BEGIN
        -- 주민번호 7번째 자리에서 출생세기·성별을 해석한다 (05 §10.1 의 표 그대로).
        -- 9·0(1800년대생)은 여기서 101 로 거부한다. 통과시키면 계산열의 NOT NULL 이
        -- Msg 515 를 내며 ResultCode 가 아니라 예외로 튄다 (04 §8.1.2 실측).
        SET @주민7번째자리      = SUBSTRING(@주민번호, 7, 1);
        SET @출생세기 = CASE WHEN @주민7번째자리 IN ('1','2','5','6') THEN '19'
                            WHEN @주민7번째자리 IN ('3','4','7','8') THEN '20' END;
        SET @성별  = CASE WHEN @주민7번째자리 IN ('1','3','5','7') THEN 'M'
                            WHEN @주민7번째자리 IN ('2','4','6','8') THEN 'F' END;
        SET @생년월일 = @출생세기 + SUBSTRING(@주민번호, 1, 6);

        IF @출생세기 IS NULL OR @성별 IS NULL OR TRY_CONVERT(DATE, @생년월일, 112) IS NULL
        BEGIN SET @결과코드 = 101; SET @오류항목 = N'주민번호'; END
        -- CK_수검자_CEL_DIGIT 를 SP 가 먼저 확인한다. 없으면 Msg 547 이 되어 RS0 계약이 깨진다.
        ELSE IF @휴대전화 IS NOT NULL
             AND (REPLACE(@휴대전화, '-', '') LIKE '%[^0-9]%'
                  OR LEN(REPLACE(@휴대전화, '-', '')) NOT BETWEEN 10 AND 11
                  OR DATALENGTH(REPLACE(@휴대전화, '-', '')) <> LEN(REPLACE(@휴대전화, '-', '')))
        BEGIN SET @결과코드 = 101; SET @오류항목 = N'휴대전화'; END
        -- 차트번호자동발급여부 조합 (05 §10.1)
        ELSE IF (@차트번호자동발급여부 = 1 AND @차트번호 IS NOT NULL)
             OR (@차트번호자동발급여부 = 0 AND @차트번호 IS NULL)
        BEGIN SET @결과코드 = 102; SET @오류항목 = N'차트번호'; END
    END

    IF @결과코드 = 100 SET @결과메시지 = N'필수값을 입력하십시오.';
    IF @결과코드 = 101 SET @결과메시지 = N'입력값이 올바르지 않습니다.';
    IF @결과코드 = 102 SET @결과메시지 = N'함께 사용할 수 없는 입력값 조합입니다.';
    IF @결과코드 <> 0 SET @성공여부 = 0;

    ----------------------------------------------------------------------------
    -- [2]~[5] 후보 1개당 Transaction 1개 (스펙 §12.6)
    --   NEXT VALUE FOR 는 반드시 Transaction 밖에서 부른다. Msg 11728(MAXVALUE 도달)이
    --   열린 Transaction 안에서 나면 XACT_ABORT ON 이 doomed 를 만들어 COMMIT 이 Msg 3930 이 된다.
    --   Sequence 값은 롤백과 무관하게 소비되며 04 §3.6 이 결번을 허용한다.
    ----------------------------------------------------------------------------
    -- [R12] 공통 업무 가능조건(308·309)을 여기서 보지 않는다. 수검자 Master 는 검진 업무가
    --       아니라 기준정보이며 05 §10.1 이 R12 에서 검증순서에서 그 줄을 뺐다 — 휴무일 SP 넷이
    --       07a 에서 같은 이유로 이미 그렇게 되어 있다.
    --       이 자리에 있던 사전 차단은 "업무시간 밖 실패가 차트번호 Sequence 를 먹는다" 를
    --       막던 것이다. 그 실패 자체가 사라졌으므로 함께 걷는다. 다른 실패(201·202·203)가
    --       번호를 먹는 것은 R12 이전과 같고 04 §3.6 이 결번을 허용한다.

    IF @결과코드 = 0
    BEGIN
        -- 자원명에 주민번호 원문을 싣지 않는다. DMV·오류 메시지 노출 차단 (스펙 §22).
        SET @자원주민번호 = N'HC|SSN|' + CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @주민번호), 2);
        IF @차트번호자동발급여부 = 0 SET @후보차트번호 = @차트번호;

        BEGIN TRY
            WHILE @완료여부 = 0
            BEGIN
                -- (a) Transaction 밖 : 다음 자동발급 후보를 확보한다
                IF @후보차트번호 IS NULL
                BEGIN
                    BEGIN TRY
                        SET @순번값  = NEXT VALUE FOR [dbo].[SEQ_HC_CHART_NO];
                        SET @후보차트번호 = N'C' + RIGHT(N'000000' + CONVERT(NVARCHAR(6), @순번값), 6);
                    END TRY
                    BEGIN CATCH
                        -- 206 은 Sequence 가 MAXVALUE 에 도달했을 때만이다. 반복 횟수 상한을 두지 않는다.
                        IF ERROR_NUMBER() = 11728
                        BEGIN
                            SET @성공여부 = 0; SET @결과코드 = 206; SET @오류항목 = N'차트번호';
                            SET @결과메시지  = N'자동 차트번호 발급범위를 초과했습니다.';
                            SET @완료여부 = 1;
                        END
                        ELSE THROW;
                    END CATCH
                END

                IF @완료여부 = 0
                BEGIN
                    BEGIN TRANSACTION;

                    -- [3] applock : SSN(1) → CHART(2)
                    EXEC @잠금결과 = sp_getapplock @Resource = @자원주민번호, @LockMode = 'Exclusive',
                                             @LockOwner = 'Transaction', @LockTimeout = 5000;
                    PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
                    IF @잠금결과 < 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                        THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
                    END

                    -- 자동발급 경로도 후보마다 CHART 를 잡는다. 잡지 않으면 다른 세션의 수동입력과
                    -- 겹쳐 Msg 2627 이 나는데, 스펙 §20 은 그것을 설계 위반으로 규정했다.
                    -- [X] applock 은 바이트 비교, UQ_수검자_CHART_NO 는 CI·폭무시다 (실측).
                    --     정규화 없이 이어 붙이면 'c000001' 과 'C000001' 이 서로 다른 자원을 잠가
                    --     둘 다 EXISTS 를 통과하고 뒤쪽이 Msg 2627 로 죽는다 — 위 주석이 막겠다던 그 상황이다.
                    SET @자원차트번호 = N'HC|CHART|' + UPPER(@후보차트번호);
                    EXEC @잠금결과 = sp_getapplock @Resource = @자원차트번호, @LockMode = 'Exclusive',
                                             @LockOwner = 'Transaction', @LockTimeout = 5000;
                    PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
                    IF @잠금결과 < 0
                    BEGIN
                        ROLLBACK TRANSACTION;
                        IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                        THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
                    END

                    -- [4] 재검증 — 05 §10.1 검증순서
                    -- [R12] '현재 공통 업무 가능' 은 검증순서에서 빠졌다. 기준정보 정비이므로
                    --       308·309 의 대상이 아니다 (05 §10.1 · 00 §1.1).

                    -- 동일 주민번호 기존 수검자 (UQ_수검자_SOCIAL_NUMBER 로 최대 1행)
                    IF @결과코드 = 0
                    BEGIN
                        SELECT @기존수검자ID = p.[수검자ID], @기존성명 = p.[성명]
                          FROM [dbo].[수검자] p
                         WHERE p.[주민번호] = @주민번호;

                        IF @기존수검자ID IS NOT NULL
                        BEGIN
                            SET @결과행ID = @기존수검자ID;
                            IF @기존성명 = @성명
                            BEGIN
                                SET @성공여부 = 1; SET @결과코드 = 2; SET @오류항목 = NULL;
                                SET @결과메시지 = N'동일한 수검자가 이미 등록되어 있어 기존 정보를 사용합니다.';
                            END
                            ELSE
                            BEGIN
                                SET @성공여부 = 0; SET @결과코드 = 202; SET @오류항목 = N'성명';
                                SET @결과메시지 = N'동일한 주민등록번호의 기존 수검자와 이름이 다릅니다.';
                            END
                        END
                    END

                    -- 이름 + 산출 생년월일 후보. 유사수검자확인여부=1 은 이번 요청의
                    -- 성명 + 생년월일 + 주민번호 조합에만 유효하다 (05 §10.1).
                    IF @결과코드 = 0 AND @유사수검자확인여부 = 0
                       AND EXISTS (SELECT 1 FROM [dbo].[수검자] p
                                    WHERE p.[성명] = @성명 AND p.[생년월일] = @생년월일)
                    BEGIN
                        SET @성공여부 = 0; SET @결과코드 = 203; SET @오류항목 = NULL;
                        SET @결과메시지 = N'이름과 생년월일이 같은 수검자가 있습니다.';
                    END

                    -- 차트번호 고유성. 수동은 201, 자동은 잠금을 놓고 다음 후보로 간다.
                    IF @결과코드 = 0 AND EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [차트번호] = @후보차트번호)
                    BEGIN
                        IF @차트번호자동발급여부 = 0
                        BEGIN
                            SET @성공여부 = 0; SET @결과코드 = 201; SET @오류항목 = N'차트번호';
                            SET @결과메시지 = N'이미 사용 중인 차트번호입니다.';
                        END
                        ELSE
                        BEGIN
                            ROLLBACK TRANSACTION;
                            SET @후보차트번호 = NULL;
                        END
                    END

                    -- [5] 저장. @후보차트번호 IS NULL 이면 위에서 ROLLBACK 하고 재시도로 넘어간 것이다.
                    IF @후보차트번호 IS NOT NULL
                    BEGIN
                        IF @결과코드 = 0
                        BEGIN
                            -- 생년월일·성별은 계산열이라 이 목록에 없다 (Msg 271).
                            -- [X] 감사시각을 DEFAULT (GETDATE()) 에 맡기면 안 된다.
                            --     UPDATE 는 CONVERT(DATETIME, @서버시각) = SYSDATETIME() 계열을 쓰는데
                            --     GETDATE() 와 SYSDATETIME() 은 **다른 클럭 소스**다. 두 값을
                            --     CK_수검자_EDIT_DATE ([최종수정일시] >= [생성일시]) 가 비교하므로
                            --     GETDATE() 가 앞서는 호스트에서는 등록 직후 수정이 Msg 547 로 죽는다.
                            --     R7 이전에는 +4ms 단조증가 보정이 이것을 우연히 가리고 있었고,
                            --     그 보정을 걷으면서 방어가 사라졌다. 두 경로를 한 시계로 모은다.
                            INSERT INTO [dbo].[수검자]
                                ([차트번호], [성명], [주민번호], [이메일], [휴대전화], [전화번호],
                                 [우편번호], [주소], [상세주소], [비고], [B형간염제외여부],
                                 [생성일시], [최종수정일시])
                            VALUES
                                (@후보차트번호, @성명, @주민번호, @이메일, @휴대전화, @전화번호,
                                 @우편번호, @주소, @상세주소, @비고, @B형간염제외여부,
                                 CONVERT(DATETIME, @서버시각), CONVERT(DATETIME, @서버시각));

                            -- 감사 INSERT 뒤의 SCOPE_IDENTITY() 는 이력ID 를 돌려준다 (04 §8.6.5).
                            SET @대상키 = SCOPE_IDENTITY();
                            SET @결과행ID     = @대상키;
                            COMMIT TRANSACTION;
                        END
                        ELSE IF @결과코드 = 2
                            COMMIT TRANSACTION;   -- 쓴 것이 없어 ROLLBACK 과 같으나 성공 경로라 COMMIT 한다
                        ELSE
                            ROLLBACK TRANSACTION;

                        SET @완료여부 = 1;
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
    -- [7] 감사 기록 (04 §8.6.5 · 스펙 §21.1)
    --   ① @@TRANCOUNT = 0 지점의 자동커밋  ② 자체 TRY/CATCH 이고 CATCH 는 비운다
    --   ③ 해당 Result Set 을 먼저 낸 뒤에 실행한다
    --   INSERT 라 변경전은 항상 NULL 이다. 값이 없던 컬럼은 남기지 않는다 (04 §8.6.2).
    ----------------------------------------------------------------------------
    IF @결과코드 = 0 AND @대상키 IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            SELECT @저장시각, @조작자명, N'수검자', @대상키, V.[컬럼명], NULL, V.[변경후]
              FROM (VALUES
                      (N'차트번호',        CONVERT(NVARCHAR(4000), @후보차트번호))
                    , (N'성명',            CONVERT(NVARCHAR(4000), @성명))
                    , (N'주민번호',        CONVERT(NVARCHAR(4000), @주민번호))
                    , (N'이메일',          CONVERT(NVARCHAR(4000), @이메일))
                    , (N'휴대전화',        CONVERT(NVARCHAR(4000), @휴대전화))
                    , (N'전화번호',        CONVERT(NVARCHAR(4000), @전화번호))
                    , (N'우편번호',        CONVERT(NVARCHAR(4000), @우편번호))
                    , (N'주소',            CONVERT(NVARCHAR(4000), @주소))
                    , (N'상세주소',        CONVERT(NVARCHAR(4000), @상세주소))
                    , (N'비고',            CONVERT(NVARCHAR(4000), @비고))
                    , (N'B형간염제외여부', CONVERT(NVARCHAR(4000), @B형간염제외여부))
                   ) V([컬럼명], [변경후])
             WHERE V.[변경후] IS NOT NULL;
        END TRY
        BEGIN CATCH
        END CATCH
    END

    -- [6b] RS1 — 감사 **뒤**에 낸다. 순서가 뒤집히면 RS0 만 읽은 호출에서 감사가 사라진다 (§43-19).
    -- RS1 수검자결과 — 성공(0·2)과 실패 202·203 에만 낸다 (05 §3.5)
    -- [I] 여기는 커밋 뒤 재조회를 유지한다 (06 §43-18 의 예외).
    --     202·203 이 돌려주는 것은 **다른 사람의 행**이라 우리 잠금이 보호한 적이 없다 —
    --     포획해도 스냅샷일 뿐이고 살아 있는 값을 보여주는 편이 정직하다.
    --     0 은 방금 만든 행이라 그 수검자ID 를 아는 세션이 아직 없다.
    IF @결과코드 IN (0, 2, 202, 203)
        SELECT
              [수검자ID]    = CAST(p.[수검자ID]     AS BIGINT)
            , [차트번호]      = CAST(p.[차트번호]     AS NVARCHAR(100))
            , [성명]         = CAST(p.[성명]         AS NVARCHAR(100))
            , [주민번호] = CAST(p.[주민번호]     AS VARCHAR(13))
            , [생년월일]     = CAST(p.[생년월일]     AS VARCHAR(8))
            , [성별]       = CAST(p.[성별]         AS CHAR(1))
            , [휴대전화]  = CAST(p.[휴대전화]     AS VARCHAR(13))
            , [행버전]       = CAST(p.[행버전]       AS BINARY(8))
          FROM [dbo].[수검자] p
         WHERE (@결과행ID IS NOT NULL AND p.[수검자ID] = @결과행ID)
            OR (@결과코드 = 203 AND p.[성명] = @성명 AND p.[생년월일] = @생년월일)
         ORDER BY p.[수검자ID] ASC;

END
GO
-- 05 §10.2. 검증순서는 존재 → 행버전 → 실제 변경 여부 →
-- 차트번호 고유성 → 주민번호 고유성 → 주민번호 변경 시 RSV/RCP 부재 순이다.
-- [R12] `공통 업무가능` 이 이 순서에서 빠졌다. 수검자 Master 는 검진 업무가 아니라
--       기준정보이므로 308/309 의 대상이 아니다 (00 §1.1 · 05 §10.2).
--       No-op 과 308/309 의 상대 순서를 논하던 줄도 함께 걷는다 — 이 SP 는 그 둘을 내지 않는다.
--
-- [!] applock SSN → CHART → PAT 을 조건 없이 항상 잡는다 (스펙 §24.1). "변경 시에만" 으로 두면
--     변경 여부를 알기 위해 현재 행을 먼저 읽어야 하고, 그러면 PAT(랭크 3)을 SSN(랭크 1)보다
--     먼저 잡게 되어 §23 전역 순서가 뒤집힌다. 자원명은 요청값으로 바로 만든다.
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_수검자정보_수정]
    @수검자ID          BIGINT,
    @행버전             BINARY(8),
    @차트번호            NVARCHAR(100),
    @성명               NVARCHAR(100),
    @주민번호       VARCHAR(13),
    @휴대전화        VARCHAR(13),
    @전화번호              VARCHAR(13),
    @이메일              VARCHAR(200),
    @우편번호            VARCHAR(10),
    @주소            NVARCHAR(200),
    @상세주소      NVARCHAR(200),
    @비고               NVARCHAR(MAX),
    @B형간염제외여부 BIT,
    @조작자명       NVARCHAR(50)
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
    DECLARE @결과차트번호 NVARCHAR(100), @결과행버전 BINARY(8);
    DECLARE @오늘날짜      DATE         = CONVERT(DATE, @서버시각);
    -- [R12] 이 SP 에서는 쓰이지 않는다. UFN_HC_일정확인 호출이 유일한 소비처였다.
    --       05 §2.3 이 정한 표준 서두라 지우지 않는다 — 날짜 판정이 남아 있다는 뜻이 아니다.
    DECLARE @저장시각  DATETIME2(0) = CONVERT(DATETIME2(0), @서버시각);

    DECLARE @성공여부 BIT = 1, @결과코드 INT = 0, @오류항목 NVARCHAR(50) = NULL;
    DECLARE @결과메시지 NVARCHAR(300) = N'정상 처리되었습니다.';
    DECLARE @대상키 BIGINT = NULL;
    DECLARE @잠금결과 INT, @자원주민번호 NVARCHAR(255), @자원차트번호 NVARCHAR(255), @자원수검자 NVARCHAR(255);
    DECLARE @주민7번째자리 CHAR(1) = NULL, @출생세기 VARCHAR(2) = NULL;
    DECLARE @성별 CHAR(1) = NULL, @생년월일 VARCHAR(8) = NULL;

    -- 변경전 값. Transaction 안의 재검증 단계에서 한 번 읽어 No-op 판정과 감사 기록에 재사용한다
    -- (04 §8.6.5 "추가 조회를 하지 않는다").
    DECLARE @기존행버전             BINARY(8)     = NULL;
    DECLARE @기존차트번호    NVARCHAR(100) = NULL, @기존성명수정전  NVARCHAR(100) = NULL;
    DECLARE @기존주민번호        VARCHAR(13)   = NULL, @기존휴대전화 VARCHAR(13)  = NULL;
    DECLARE @기존전화번호      VARCHAR(13)   = NULL, @기존이메일 VARCHAR(200)  = NULL;
    DECLARE @기존우편번호        VARCHAR(10)   = NULL, @기존주소  NVARCHAR(200) = NULL;
    DECLARE @기존상세주소 NVARCHAR(200) = NULL, @기존비고  NVARCHAR(MAX) = NULL;
    DECLARE @기존B형간염제외여부        BIT           = NULL;

    ----------------------------------------------------------------------------
    -- [1] Transaction 밖 : 정규화 → 필수값 → 값 형식
    ----------------------------------------------------------------------------
    SET @차트번호       = NULLIF(LTRIM(RTRIM(@차트번호)), N'');
    SET @성명          = NULLIF(LTRIM(RTRIM(@성명)), N'');
    SET @주민번호  = NULLIF(REPLACE(LTRIM(RTRIM(@주민번호)), '-', ''), '');
    SET @휴대전화   = NULLIF(LTRIM(RTRIM(@휴대전화)), '');
    SET @전화번호         = NULLIF(LTRIM(RTRIM(@전화번호)), '');
    SET @이메일         = NULLIF(LTRIM(RTRIM(@이메일)), '');
    SET @우편번호       = NULLIF(LTRIM(RTRIM(@우편번호)), '');
    SET @주소       = NULLIF(LTRIM(RTRIM(@주소)), N'');
    SET @상세주소 = NULLIF(LTRIM(RTRIM(@상세주소)), N'');
    SET @비고          = NULLIF(LTRIM(RTRIM(@비고)), N'');
    SET @조작자명  = NULLIF(LTRIM(RTRIM(@조작자명)), N'');

    IF @수검자ID IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'수검자ID'; END
    ELSE IF @행버전 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'행버전'; END
    ELSE IF @차트번호 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'차트번호'; END
    ELSE IF @성명 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'성명'; END
    ELSE IF @주민번호 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'주민번호'; END
    ELSE IF @B형간염제외여부 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'B형간염제외여부'; END
    ELSE IF @조작자명 IS NULL
    BEGIN SET @결과코드 = 100; SET @오류항목 = N'조작자명'; END
    ELSE IF LEN(@주민번호) <> 13 OR DATALENGTH(@주민번호) <> 13
         OR @주민번호 LIKE '%[^0-9]%'
    BEGIN SET @결과코드 = 101; SET @오류항목 = N'주민번호'; END
    ELSE
    BEGIN
        SET @주민7번째자리      = SUBSTRING(@주민번호, 7, 1);
        SET @출생세기 = CASE WHEN @주민7번째자리 IN ('1','2','5','6') THEN '19'
                            WHEN @주민7번째자리 IN ('3','4','7','8') THEN '20' END;
        SET @성별  = CASE WHEN @주민7번째자리 IN ('1','3','5','7') THEN 'M'
                            WHEN @주민7번째자리 IN ('2','4','6','8') THEN 'F' END;
        SET @생년월일 = @출생세기 + SUBSTRING(@주민번호, 1, 6);

        IF @출생세기 IS NULL OR @성별 IS NULL OR TRY_CONVERT(DATE, @생년월일, 112) IS NULL
        BEGIN SET @결과코드 = 101; SET @오류항목 = N'주민번호'; END
        ELSE IF @휴대전화 IS NOT NULL
             AND (REPLACE(@휴대전화, '-', '') LIKE '%[^0-9]%'
                  OR LEN(REPLACE(@휴대전화, '-', '')) NOT BETWEEN 10 AND 11
                  OR DATALENGTH(REPLACE(@휴대전화, '-', '')) <> LEN(REPLACE(@휴대전화, '-', '')))
        BEGIN SET @결과코드 = 101; SET @오류항목 = N'휴대전화'; END
    END

    IF @결과코드 = 100 SET @결과메시지 = N'필수값을 입력하십시오.';
    IF @결과코드 = 101 SET @결과메시지 = N'입력값이 올바르지 않습니다.';
    IF @결과코드 <> 0 SET @성공여부 = 0;

    ----------------------------------------------------------------------------
    -- [3]~[5] Transaction
    ----------------------------------------------------------------------------
    IF @결과코드 = 0
    BEGIN
        SET @자원주민번호   = N'HC|SSN|' + CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @주민번호), 2);
        -- [X] 자원명은 UPPER 로 맞춘다. applock 은 바이트 비교, UQ 는 CI 다 (05 §22 · 실측).
        SET @자원차트번호 = N'HC|CHART|' + UPPER(@차트번호);
        SET @자원수검자   = N'HC|PAT|' + CONVERT(NVARCHAR(20), @수검자ID);

        BEGIN TRY
            BEGIN TRANSACTION;

            EXEC @잠금결과 = sp_getapplock @Resource = @자원주민번호, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
            IF @잠금결과 < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            EXEC @잠금결과 = sp_getapplock @Resource = @자원차트번호, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
            IF @잠금결과 < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            EXEC @잠금결과 = sp_getapplock @Resource = @자원수검자, @LockMode = 'Exclusive',
                                     @LockOwner = 'Transaction', @LockTimeout = 5000;
            PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @잠금결과);
            IF @잠금결과 < 0
            BEGIN
                ROLLBACK TRANSACTION;
                IF @잠금결과 = -3 THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
                THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
            END

            -- [4] 재검증. 행버전은 NOT NULL 이므로 NULL 은 곧 미존재다.
            SELECT @기존행버전             = p.[행버전]
                 , @기존차트번호    = p.[차트번호]
                 , @기존성명수정전       = p.[성명]
                 , @기존주민번호        = p.[주민번호]
                 , @기존휴대전화     = p.[휴대전화]
                 , @기존전화번호      = p.[전화번호]
                 , @기존이메일      = p.[이메일]
                 , @기존우편번호        = p.[우편번호]
                 , @기존주소       = p.[주소]
                 , @기존상세주소 = p.[상세주소]
                 , @기존비고       = p.[비고]
                 , @기존B형간염제외여부        = p.[B형간염제외여부]
              FROM [dbo].[수검자] p
             WHERE p.[수검자ID] = @수검자ID;

            IF @기존행버전 IS NULL
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 200; SET @오류항목 = N'수검자ID';
                SET @결과메시지 = N'수검자를 찾을 수 없습니다.';
            END
            ELSE IF @기존행버전 <> @행버전
            BEGIN
                SET @성공여부 = 0; SET @결과코드 = 601; SET @오류항목 = N'행버전';
                SET @결과메시지 = N'다른 사용자가 수검자 정보를 변경했습니다. 최신 정보를 다시 조회하십시오.';
            END
            -- 실제 변경 여부는 NULL-safe 여야 한다 (스펙 §28.2). <> 로 비교하면 NULL <> 'x' 가
            -- UNKNOWN 이라 "변경 없음" 이 되어 사용자 편집이 조용히 소실된다. INTERSECT 는
            -- NULL = NULL 을 참으로 다룬다. 비고만 NVARCHAR(MAX) 라 INTERSECT 피연산자가 될 수
            -- 없어 명시적 NULL-safe 비교로 분리한다.
            --
            -- [X] NULL-safe 만으로는 부족했다. 정렬이 Korean_Wansung_CI_AS 라 INTERSECT 가
            --     'hong@GMAIL.com' 과 'hong@gmail.com' 을 **같다고** 본다(실측). 대소문자만 고친
            --     이메일·주소·차트번호 수정이 결과코드=1 '변경된 내용이 없습니다' 로 돌아오고
            --     편집이 조용히 사라졌다 — NULL 함정을 막으려던 그 자리에 정렬 함정이 남아 있었다.
            --     COLLATE 는 06 §9.2 허용목록 밖이므로 VARBINARY 변환으로 바이트 비교한다.
            --     INTERSECT 의 NULL = NULL 성질은 그대로 유지된다 (CONVERT(VARBINARY, NULL) 도 NULL).
            ELSE IF EXISTS (
                        SELECT CONVERT(VARBINARY(400), @기존차트번호), CONVERT(VARBINARY(400), @기존성명수정전)
                             , CONVERT(VARBINARY(400), @기존주민번호),     CONVERT(VARBINARY(400), @기존휴대전화)
                             , CONVERT(VARBINARY(400), @기존전화번호),   CONVERT(VARBINARY(400), @기존이메일)
                             , CONVERT(VARBINARY(400), @기존우편번호),     CONVERT(VARBINARY(400), @기존주소)
                             , CONVERT(VARBINARY(400), @기존상세주소), CONVERT(VARBINARY(400), @기존B형간염제외여부)
                        INTERSECT
                        SELECT CONVERT(VARBINARY(400), @차트번호),    CONVERT(VARBINARY(400), @성명)
                             , CONVERT(VARBINARY(400), @주민번호), CONVERT(VARBINARY(400), @휴대전화)
                             , CONVERT(VARBINARY(400), @전화번호),      CONVERT(VARBINARY(400), @이메일)
                             , CONVERT(VARBINARY(400), @우편번호),    CONVERT(VARBINARY(400), @주소)
                             , CONVERT(VARBINARY(400), @상세주소), CONVERT(VARBINARY(400), @B형간염제외여부)
                    )
                    AND ((@기존비고 IS NULL AND @비고 IS NULL)
                         OR CONVERT(VARBINARY(MAX), @기존비고) = CONVERT(VARBINARY(MAX), @비고))
            BEGIN
                SET @성공여부 = 1; SET @결과코드 = 1; SET @오류항목 = NULL;
                SET @결과메시지 = N'변경된 내용이 없습니다.';
            END
            ELSE
            BEGIN
                -- [R12] '현재 공통 업무 가능' 은 05 §10.2 검증순서에서 빠졌다 (00 §1.1).

                IF @결과코드 = 0 AND @차트번호 <> @기존차트번호
                   AND EXISTS (SELECT 1 FROM [dbo].[수검자]
                                WHERE [차트번호] = @차트번호 AND [수검자ID] <> @수검자ID)
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 201; SET @오류항목 = N'차트번호';
                    SET @결과메시지 = N'이미 사용 중인 차트번호입니다.';
                END

                IF @결과코드 = 0 AND @주민번호 <> @기존주민번호
                   AND EXISTS (SELECT 1 FROM [dbo].[수검자]
                                WHERE [주민번호] = @주민번호 AND [수검자ID] <> @수검자ID)
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 204; SET @오류항목 = N'주민번호';
                    SET @결과메시지 = N'다른 수검자가 사용 중인 주민등록번호입니다.';
                END

                -- 00 EP-08. 차트번호 변경은 활성 업무가 있어도 차단하지 않는다.
                IF @결과코드 = 0 AND @주민번호 <> @기존주민번호
                   AND EXISTS (SELECT 1 FROM [dbo].[예약접수]
                                WHERE [수검자ID] = @수검자ID AND [상태코드] IN ('RSV','RCP'))
                BEGIN
                    SET @성공여부 = 0; SET @결과코드 = 205; SET @오류항목 = N'주민번호';
                    SET @결과메시지 = N'예약 또는 접수완료 업무가 있어 주민등록번호를 변경할 수 없습니다.';
                END

                IF @결과코드 = 0
                BEGIN
                    -- [R7] 여기 있던 최종수정일시 단조증가(+4ms) 보정이 사라졌다.
                    --      동시성 토큰이 [행버전](ROWVERSION)으로 옮겨 갔으므로 DATETIME 의
                    --      3.33ms 틱이 판정에 관여하지 않는다. 그 보정은 저장된 시각을
                    --      사실과 다르게 만들기도 했다 — 3ms 뒤로 밀린 값은 실제 수정 시각이
                    --      아니었고, 그 거짓의 유일한 이유가 동시성이었다 (06 §26).
                    -- 생년월일·성별은 계산열이라 SET 목록에 없다 (Msg 271).
                    -- 주민번호를 바꾸면 계산열이 자동으로 다시 유도한다.
                    UPDATE [dbo].[수검자]
                       SET [차트번호]        = @차트번호
                         , [성명]            = @성명
                         , [주민번호]        = @주민번호
                         , [이메일]          = @이메일
                         , [휴대전화]        = @휴대전화
                         , [전화번호]        = @전화번호
                         , [우편번호]        = @우편번호
                         , [주소]            = @주소
                         , [상세주소]        = @상세주소
                         , [비고]            = @비고
                         , [B형간염제외여부] = @B형간염제외여부
                         , [최종수정일시]    = CONVERT(DATETIME, @서버시각)
                     WHERE [수검자ID] = @수검자ID
                       AND [행버전]   = @기존행버전;   -- 낙관적 동시성

                    IF @@ROWCOUNT = 0
                    BEGIN
                        SET @성공여부 = 0; SET @결과코드 = 601; SET @오류항목 = N'행버전';
                        SET @결과메시지 = N'다른 사용자가 수검자 정보를 변경했습니다. 최신 정보를 다시 조회하십시오.';
                    END
                    ELSE
                        SET @대상키 = @수검자ID;
                END
            END

            -- [X] RS1 을 COMMIT **뒤에** 다시 읽으면 그 사이 다른 세션이 바꾼 행버전 이 나간다.
            --     호출자는 자기 것이 아닌 동시성 토큰을 받고, 그 값으로 보낸 다음 요청이
            --     601 로 막혔어야 하는데 통과한다. 잠금 안에서 포획한다 (06 §43-18).
            IF @결과코드 IN (0, 1)
                SELECT @결과차트번호 = p.[차트번호], @결과행버전 = p.[행버전]
                  FROM [dbo].[수검자] p WHERE p.[수검자ID] = @수검자ID;

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
    -- [7] 감사 기록. 실제로 값이 바뀐 컬럼만 남는다 (00 CP-06 · 04 §8.6.2).
    --   행버전·최종수정일시는 동시성·감사 값이지 사용자 데이터가 아니라 기록하지 않는다.
    ----------------------------------------------------------------------------
    IF @결과코드 = 0 AND @대상키 IS NOT NULL
    BEGIN
        BEGIN TRY
            INSERT INTO [dbo].[변경이력]
                ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
            SELECT @저장시각, @조작자명, N'수검자', @대상키, V.[컬럼명], V.[변경전], V.[변경후]
              FROM (VALUES
                      (N'차트번호',        CONVERT(NVARCHAR(4000), @기존차트번호),    CONVERT(NVARCHAR(4000), @차트번호))
                    , (N'성명',            CONVERT(NVARCHAR(4000), @기존성명수정전),       CONVERT(NVARCHAR(4000), @성명))
                    , (N'주민번호',        CONVERT(NVARCHAR(4000), @기존주민번호),        CONVERT(NVARCHAR(4000), @주민번호))
                    , (N'이메일',          CONVERT(NVARCHAR(4000), @기존이메일),      CONVERT(NVARCHAR(4000), @이메일))
                    , (N'휴대전화',        CONVERT(NVARCHAR(4000), @기존휴대전화),     CONVERT(NVARCHAR(4000), @휴대전화))
                    , (N'전화번호',        CONVERT(NVARCHAR(4000), @기존전화번호),      CONVERT(NVARCHAR(4000), @전화번호))
                    , (N'우편번호',        CONVERT(NVARCHAR(4000), @기존우편번호),        CONVERT(NVARCHAR(4000), @우편번호))
                    , (N'주소',            CONVERT(NVARCHAR(4000), @기존주소),       CONVERT(NVARCHAR(4000), @주소))
                    , (N'상세주소',        CONVERT(NVARCHAR(4000), @기존상세주소), CONVERT(NVARCHAR(4000), @상세주소))
                    , (N'비고',            CONVERT(NVARCHAR(4000), @기존비고),       CONVERT(NVARCHAR(4000), @비고))
                    , (N'B형간염제외여부', CONVERT(NVARCHAR(4000), @기존B형간염제외여부),        CONVERT(NVARCHAR(4000), @B형간염제외여부))
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

    -- [6b] RS1 — 감사 **뒤**에 낸다. 순서가 뒤집히면 RS0 만 읽은 호출에서 감사가 사라진다 (§43-19).
    -- RS1 수검자변경결과. No-op 은 갱신하지 않은 기존 행버전 이 그대로 나온다 (05 §10.2).
    -- 값은 잠금 안에서 포획한 것이다 (아래 [X] 참조). 커밋 뒤 재조회하지 않는다.
    IF @결과코드 IN (0, 1)
        SELECT
              [수검자ID]    = CAST(@수검자ID AS BIGINT)
            , [차트번호]      = CAST(@결과차트번호   AS NVARCHAR(100))
            , [행버전]       = CAST(@결과행버전          AS BINARY(8));

END
GO
