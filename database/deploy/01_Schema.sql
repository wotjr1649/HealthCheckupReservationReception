-- 필터형 인덱스는 QUOTED_IDENTIFIER ON 을 요구한다. sqlcmd 기본값은 OFF 라서(SSMS 와 다르다)
-- 이 SET 이 없으면 CREATE INDEX 가 Msg 1934 로 실패한다(실측 확인).
-- parse 시점에 적용되므로 반드시 앞선 배치에 두고 GO 로 끊는다.
SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
PRINT N'--- 01_Schema 시작 ---';
GO
-- [변경이력] 은 이 목록에 없다. 감사 기록은 배포로 지워지지 않는다 (CLAUDE.md §8 의 유일한 예외).
-- 수검자가 새로 만들어지면 [대상키] 는 사라진 행을 가리키게 되는데, 그것이 04 §8.6.3 이 정한
-- "감사 기록은 대상 행보다 오래 산다" 의 의미다.
DROP TABLE IF EXISTS [dbo].[완료이력];
DROP TABLE IF EXISTS [dbo].[예약접수];
DROP TABLE IF EXISTS [dbo].[휴무일];
DROP TABLE IF EXISTS [dbo].[검사코드];
DROP TABLE IF EXISTS [dbo].[수검자];
DROP SEQUENCE IF EXISTS [dbo].[SEQ_HC_CHART_NO];
GO
CREATE TABLE [dbo].[수검자]
(
    [수검자ID]        BIGINT          IDENTITY(1,1) NOT NULL,
    [차트번호]        NVARCHAR(100)   NOT NULL,
    [성명]            NVARCHAR(100)   NOT NULL,
    [주민번호]        VARCHAR(13)     NOT NULL,
    -- 생년월일·성별은 주민번호에서 DB 가 유도한다. 저장 SP 가 산출해 넣지 않는다 (04 §8.1.2).
    -- 세 값이 서로 어긋난 상태를 표현할 수 없게 만드는 것이 목적이다.
    -- INSERT/UPDATE 에 이 두 컬럼을 명시하면 Msg 271 로 거부된다.
    -- [X] CONVERT 로 감싸지 않으면 LEFT 의 폭 전파로 varchar(14) 가 되어 계약 타입이 깨진다(실측 확인).
    --     PERSISTED 만 쓰면 is_nullable = 1 이 되므로 NOT NULL 을 함께 붙인다.
    [생년월일] AS CONVERT(VARCHAR(8),
        CASE WHEN SUBSTRING([주민번호], 7, 1) IN ('1','2','5','6') THEN '19'
             WHEN SUBSTRING([주민번호], 7, 1) IN ('3','4','7','8') THEN '20' END
        + LEFT([주민번호], 6)) PERSISTED NOT NULL,
    [성별] AS CONVERT(CHAR(1),
        CASE WHEN SUBSTRING([주민번호], 7, 1) IN ('1','3','5','7') THEN 'M'
             WHEN SUBSTRING([주민번호], 7, 1) IN ('2','4','6','8') THEN 'F' END) PERSISTED NOT NULL,
    [이메일]          VARCHAR(200)    NULL,
    [휴대전화]        VARCHAR(13)     NULL,
    [전화번호]        VARCHAR(13)     NULL,
    [우편번호]        VARCHAR(10)     NULL,
    [주소]            NVARCHAR(200)   NULL,
    [상세주소]        NVARCHAR(200)   NULL,
    [B형간염제외여부] BIT             NOT NULL CONSTRAINT [DF_수검자_HEPATITIS_B_EXCLUDED] DEFAULT (0),
    [비고]            NVARCHAR(MAX)   NULL,
    [생성일시]        DATETIME        NOT NULL CONSTRAINT [DF_수검자_CREATION_DATE]        DEFAULT (GETDATE()),
    [최종수정일시]    DATETIME        NOT NULL CONSTRAINT [DF_수검자_LAST_EDIT_DATE]       DEFAULT (GETDATE()),

    CONSTRAINT [PK_수검자] PRIMARY KEY CLUSTERED ([수검자ID]),
    CONSTRAINT [UQ_수검자_CHART_NO]       UNIQUE ([차트번호]),
    CONSTRAINT [UQ_수검자_SOCIAL_NUMBER]  UNIQUE ([주민번호]),

    CONSTRAINT [CK_수검자_CHART_NO_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([차트번호]))) > 0),
    CONSTRAINT [CK_수검자_NAME_NOT_BLANK]     CHECK (LEN(LTRIM(RTRIM([성명]))) > 0),
    -- 7번째 자리 1~8 은 생년월일·성별 계산열의 전제다. 9·0(1800년대생)은 유도 결과가 NULL 이 된다.
    -- [X] 이 CHECK 가 Msg 547 로 먼저 걸리기를 의도했으나 실제로는 계산열의 NOT NULL 이 먼저
    --     평가되어 Msg 515 가 나온다(실측 확인). 제약 평가 순서는 보장되지 않는다.
    --     그래도 허용 도메인을 여기에 적어 두는 값어치는 남는다 - 스키마만 읽어도 범위를 알 수 있다.
    -- 04 §1.5 가 실제 주민등록번호를 저장하지 않는다고 못박았으므로 임의 테스트값의 범위에 맞다.
    -- [X] LEN 은 문자 수, DATALENGTH 는 바이트 수다. 정렬이 Korean_Wansung_CI_AS 라
    --     [^0-9] 가 전각 숫자를 잡지 못한다(실측 확인). 지금 전각이 막히는 이유는 이 제약이
    --     아니라 VARCHAR(13) 폭이 13자를 14바이트로 만들기 때문이며, 폭이 넓어지면 조용히 열린다.
    --     DATALENGTH = 13 이면 13자가 전부 단일바이트여야 하므로 제약 자체가 막는다.
    CONSTRAINT [CK_수검자_SOCIAL_FORMAT]      CHECK (LEN([주민번호]) = 13 AND DATALENGTH([주민번호]) = 13
                                                    AND [주민번호] NOT LIKE '%[^0-9]%'
                                                    AND SUBSTRING([주민번호], 7, 1) IN ('1','2','3','4','5','6','7','8')),
    -- 계산열이라 8자리 숫자는 구조적으로 보장된다. 이 CHECK 가 실제로 잡는 것은
    -- 주민번호 앞 6자리가 달력에 없는 날짜인 경우다(예: 991332 -> 19991332).
    CONSTRAINT [CK_수검자_BIRTHDAY]           CHECK (LEN([생년월일]) = 8 AND [생년월일] NOT LIKE '%[^0-9]%' AND TRY_CONVERT(DATE, [생년월일], 112) IS NOT NULL),
    CONSTRAINT [CK_수검자_GENDER]             CHECK ([성별] IN ('M','F')),
    -- [X] 하이픈을 뺀 결과만 검사하면 빈 문자열·하이픈만인 값이 전부 통과한다(실측 확인).
    --     빈 문자열은 어떤 LIKE 패턴에도 걸리지 않기 때문이다. 자릿수를 함께 본다.
    --     표시값 그대로 저장하므로(04 §8.1.2) 하이픈을 뺀 자릿수가 10~11 이어야 한다.
    --     그리고 [^0-9] 는 전각 숫자를 잡지 못한다. '0１012345678' 은 11자라 자릿수도 통과했다
    --     (실측 확인). 이 컬럼은 폭에도 걸리지 않아 정렬 누수가 실제 저장까지 도달하던 유일한 곳이다.
    --     DATALENGTH = LEN 이면 전부 단일바이트다.
    CONSTRAINT [CK_수검자_CEL_DIGIT]          CHECK ([휴대전화] IS NULL
                                                    OR (REPLACE([휴대전화], '-', '') NOT LIKE '%[^0-9]%'
                                                        AND LEN(REPLACE([휴대전화], '-', '')) BETWEEN 10 AND 11
                                                        AND DATALENGTH(REPLACE([휴대전화], '-', ''))
                                                          = LEN(REPLACE([휴대전화], '-', '')))),
    CONSTRAINT [CK_수검자_EDIT_DATE]          CHECK ([최종수정일시] >= [생성일시])
);
GO
CREATE NONCLUSTERED INDEX [IX_수검자_NAME_BIRTHDAY]
    ON [dbo].[수검자] ([성명], [생년월일])
    INCLUDE ([수검자ID], [차트번호], [성별], [휴대전화]);
GO
CREATE NONCLUSTERED INDEX [IX_수검자_BIRTHDAY]
    ON [dbo].[수검자] ([생년월일])
    INCLUDE ([수검자ID], [차트번호], [성명], [성별], [휴대전화]);
GO
CREATE TABLE [dbo].[검사코드]
(
    [검사항목코드]     VARCHAR(10)   NOT NULL,
    [검사항목명]       NVARCHAR(100) NOT NULL,
    [국가검사규칙코드] VARCHAR(10)   NULL,
    [추가검사코드]     VARCHAR(10)   NULL,
    [추가검사성별코드] CHAR(1)       NULL,
    [추가검사사용여부] BIT           NOT NULL CONSTRAINT [DF_검사코드_AEX_ACTIVE] DEFAULT (0),

    CONSTRAINT [PK_검사코드] PRIMARY KEY CLUSTERED ([검사항목코드]),
    CONSTRAINT [CK_검사코드_CODE_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([검사항목코드]))) > 0),
    -- Master 의 코드 도메인을 검사구성 문자열의 도메인과 맞춘다. 이것이 없으면 'EX-001' 이나
    -- '검사01' 이 Master 에는 들어가는데 예약접수 검사구성에는 저장할 수 없는 코드가 된다(실측 확인).
    -- 선행공백 코드가 별개 PK 행이 되는 것도 함께 막힌다.
    -- [X] 정렬이 CI 라 [A-Z0-9] 가 소문자까지 포함한다 - 'ex001' 은 이 제약을 통과한다(실측 확인).
    --     DATALENGTH = LEN 이 전각 영숫자만 닫고, 소문자는 COLLATE 없이 못 막는다.
    --     COLLATE 는 06 §9.2 허용목록 밖이다. 검사코드는 Seed 19행 고정이고 관리자 CRUD 가
    --     없으므로(04 §8.3.1) 남는 위험은 저장 SP 를 우회한 직접 DML 뿐이다.
    --     [X] 여기 'SEC-004·005 가 막는다' 라고 적혀 있었으나 그 방어는 존재하지 않는다 —
    --         User·GRANT 는 2026-09-07 사용자 결정으로 범위 밖이다(06 §32 · §43-7).
    --         같은 파일 CK_예약접수_EXAM_PAIR 주석은 이미 그렇게 갱신돼 있었고 여기만 누락됐다.
    CONSTRAINT [CK_검사코드_CODE_FORMAT]    CHECK ([검사항목코드] NOT LIKE '%[^A-Z0-9]%'
                                                   AND DATALENGTH([검사항목코드]) = LEN([검사항목코드])),
    CONSTRAINT [CK_검사코드_NAME_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([검사항목명]))) > 0),
    CONSTRAINT [CK_검사코드_ROLE_REQUIRED]  CHECK ([국가검사규칙코드] IS NOT NULL OR [추가검사코드] IS NOT NULL),
    CONSTRAINT [CK_검사코드_NEX_RULE]       CHECK ([국가검사규칙코드] IS NULL OR [국가검사규칙코드] IN ('NEX-01','NEX-02','NEX-03','NEX-04','NEX-05','NEX-06')),
    CONSTRAINT [CK_검사코드_AEX_CODE]       CHECK ([추가검사코드] IS NULL OR [추가검사코드] IN ('OPT01','OPT02','OPT03','OPT04','OPT05','OPT06','OPT07')),
    CONSTRAINT [CK_검사코드_AEX_GENDER]     CHECK ([추가검사성별코드] IS NULL OR [추가검사성별코드] IN ('A','M','F')),
    CONSTRAINT [CK_검사코드_AEX_GROUP]      CHECK
    (
        ([추가검사코드] IS NULL     AND [추가검사성별코드] IS NULL     AND [추가검사사용여부] = 0)
     OR ([추가검사코드] IS NOT NULL AND [추가검사성별코드] IS NOT NULL)
    )
);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_검사코드_AEX_CODE]
    ON [dbo].[검사코드] ([추가검사코드])
    WHERE [추가검사코드] IS NOT NULL;
GO
CREATE TABLE [dbo].[휴무일]
(
    [휴무일자] DATE          NOT NULL,
    [휴무일명] NVARCHAR(100) NOT NULL,
    [사용여부] BIT           NOT NULL CONSTRAINT [DF_휴무일_ACTIVE] DEFAULT (1),
    [비고]     NVARCHAR(500) NULL,

    CONSTRAINT [PK_휴무일] PRIMARY KEY CLUSTERED ([휴무일자]),
    CONSTRAINT [CK_휴무일_NAME_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([휴무일명]))) > 0)
);
GO
CREATE TABLE [dbo].[예약접수]
(
    [업무ID]       BIGINT       IDENTITY(1,1) NOT NULL,
    [수검자ID]     BIGINT       NOT NULL,
    [예약일]       DATE         NOT NULL,
    [시간대코드]   CHAR(2)      NOT NULL,
    [상태코드]     CHAR(3)      NOT NULL,
    -- 검사구성. 검사항목코드를 오름차순으로 쉼표로 잇는다 (04 §8.2.2).
    -- [X] NOT NULL 은 "국가검사가 반드시 있다" 를 보증하지 않는다. 저장 NEX 가 0 인 손상 상태를
    --     표현할 수 있어야 하므로 빈 문자열이 통과한다. 빈 문자열이 검사구성 손상의 유일한
    --     표현이고 판정식은 LEN([국가검사항목]) = 0 이다. NULL 과 빈 문자열이 둘 다 손상을
    --     뜻하는 상태를 만들지 않으려고 NOT NULL 을 건다.
    [국가검사항목] NVARCHAR(100) NOT NULL,
    [추가검사항목] NVARCHAR(50)  NULL,
    [생성일시]     DATETIME2(0) NOT NULL CONSTRAINT [DF_예약접수_CREATION_DATE]  DEFAULT (SYSDATETIME()),
    [최종수정일시] DATETIME2(0) NOT NULL CONSTRAINT [DF_예약접수_LAST_EDIT_DATE] DEFAULT (SYSDATETIME()),
    [행버전]       ROWVERSION   NOT NULL,
    CONSTRAINT [PK_예약접수] PRIMARY KEY CLUSTERED ([업무ID]),
    CONSTRAINT [FK_예약접수_수검자] FOREIGN KEY ([수검자ID])
        REFERENCES [dbo].[수검자] ([수검자ID]) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT [CK_예약접수_TIME_SLOT] CHECK ([시간대코드] IN ('AM','PM')),
    CONSTRAINT [CK_예약접수_STATUS]    CHECK ([상태코드] IN ('RSV','RCP','CNR','CNC')),
    CONSTRAINT [CK_예약접수_EDIT_DATE] CHECK ([최종수정일시] >= [생성일시]),
    -- 코드 존재·중복·정렬은 보증하지 못한다(검사코드 FK 를 잃은 대가, 04 §8.2.3). 문법만 막는다.
    -- [X] 문자 집합만 보면 빈 토큰과 선행·후행 쉼표가 전부 통과한다(실측 확인).
    --     빈 문자열은 손상 표현이라 계속 통과해야 하는데, 아래 세 패턴은 빈 문자열에 걸리지 않는다.
    -- [X] DB 정렬이 Korean_Wansung_CI_AS 라 [A-Z0-9] 범위가 소문자·악센트 라틴·전각 영숫자까지
    --     포함한다(실측 확인). 이 CHECK 는 "A-Z0-9 만" 을 뜻하지 않는다. 정렬 무관 판정에는
    --     COLLATE 가 필요한데 06 §9.2 허용목록 밖이라 여기서는 닫지 않는다.
    CONSTRAINT [CK_예약접수_EXAM_FORMAT] CHECK
    (
        [국가검사항목] NOT LIKE '%[^A-Z0-9,]%'
    AND [국가검사항목] NOT LIKE '%,,%'
    AND [국가검사항목] NOT LIKE ',%'
    AND [국가검사항목] NOT LIKE '%,'
    AND ([추가검사항목] IS NULL
         OR (LEN([추가검사항목]) > 0
         AND [추가검사항목] NOT LIKE '%[^A-Z0-9,]%'
         AND [추가검사항목] NOT LIKE '%,,%'
         AND [추가검사항목] NOT LIKE ',%'
         AND [추가검사항목] NOT LIKE '%,'))
    ),
    -- 추가검사는 국가검사 위에 얹는 것이라 NEX 없이 AEX 만 있는 Work 는 성립하지 않는다
    -- (04 §2.3 "TGT 대상이면 기본 8종을 항상 구성한다", 05 §11.1 저장 규칙).
    -- [X] EXAM_FORMAT 은 두 컬럼을 따로만 보므로 ('', 'EX014') 조합이 통과했다(실측 확인).
    --     Write SP 4개가 저장 NEX 8~11 을 확인해 막고 있었지만 스키마는 열려 있었고,
    --     그 구멍의 유일한 방어로 적혀 있던 SEC-004·005 는 구현 범위에서 빠졌다.
    --     CORRUPT-2 는 ('', NULL) 이라 이 제약을 그대로 통과한다 - 손상 표현은 살아 있다.
    CONSTRAINT [CK_예약접수_EXAM_PAIR] CHECK ([추가검사항목] IS NULL OR LEN([국가검사항목]) > 0)
);
GO
CREATE NONCLUSTERED INDEX [IX_예약접수_SLOT]
    ON [dbo].[예약접수] ([예약일], [시간대코드], [상태코드])
    INCLUDE ([수검자ID]);
GO
CREATE NONCLUSTERED INDEX [IX_예약접수_PATIENT_STATE_DATE]
    ON [dbo].[예약접수] ([수검자ID], [상태코드], [예약일])
    INCLUDE ([시간대코드]);
GO
CREATE TABLE [dbo].[완료이력]
(
    [수검자ID]     BIGINT        NOT NULL,
    [완료일자]     DATE          NOT NULL,
    -- 검사구성. 예약접수와 같은 형식이다 (04 §8.5.2).
    -- 외부 기관 검진은 검사 내용을 모를 수 있으므로 둘 다 NULL 을 허용한다.
    -- 예약접수와 달리 빈 문자열은 막는다. 여기서는 "모름" 이 NULL 하나로 고정이고
    -- 손상 상태를 표현할 이유가 없다.
    [국가검사항목] NVARCHAR(100) NULL,
    [추가검사항목] NVARCHAR(50)  NULL,

    CONSTRAINT [PK_완료이력] PRIMARY KEY CLUSTERED ([수검자ID], [완료일자]),
    CONSTRAINT [FK_완료이력_수검자] FOREIGN KEY ([수검자ID])
        REFERENCES [dbo].[수검자] ([수검자ID]) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT [CK_완료이력_EXAM_FORMAT] CHECK
    (
        ([국가검사항목] IS NULL
         OR (LEN([국가검사항목]) > 0
         AND [국가검사항목] NOT LIKE '%[^A-Z0-9,]%'
         AND [국가검사항목] NOT LIKE '%,,%'
         AND [국가검사항목] NOT LIKE ',%'
         AND [국가검사항목] NOT LIKE '%,'))
    AND ([추가검사항목] IS NULL
         OR (LEN([추가검사항목]) > 0
         AND [추가검사항목] NOT LIKE '%[^A-Z0-9,]%'
         AND [추가검사항목] NOT LIKE '%,,%'
         AND [추가검사항목] NOT LIKE ',%'
         AND [추가검사항목] NOT LIKE '%,'))
    )
);
GO
-- 감사 기록은 배포로 지워지지 않는다. 이 테이블만 clean-create 대상이 아니며
-- CLAUDE.md §8 의 유일한 예외다. 06 §8.1 이 clean-create 의 근거로 든
-- "보존할 운영 데이터가 없다" 가 이 테이블에는 성립하지 않는다.
-- 이 가드가 옛 구조를 조용히 유지하는 드리프트는 verify-schema-doc 의
-- DOC-001~005 양방향 대조가 잡는다. 그래서 별도 검사를 추가하지 않는다.
IF OBJECT_ID(N'[dbo].[변경이력]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[변경이력]
    (
        -- 성공한 데이터 변경만 기록한다. 바뀐 컬럼 1개당 1행이다 (04 §8.6.1, 00 CP-06).
        -- 실패한 호출은 데이터를 바꾸지 않으므로 남기지 않는다.
        [이력ID]     BIGINT         IDENTITY(1,1) NOT NULL,
        [기록일시]   DATETIME2(0)   NOT NULL CONSTRAINT [DF_변경이력_CREATION_DATE] DEFAULT (SYSDATETIME()),
        [조작자명]   NVARCHAR(50)   NULL,
        [대상테이블] NVARCHAR(10)   NOT NULL,
        -- 대상 테이블 둘 다 BIGINT 단일 PK 이고 성공한 변경만 기록하므로 키를 모르는 경로가 없다.
        [대상키]     BIGINT         NOT NULL,
        [컬럼명]     NVARCHAR(30)   NOT NULL,
        -- 감사 기록은 복원 근거가 아니라 열람용이라 4000자에서 자른다 (00 CP-06).
        [변경전]     NVARCHAR(4000) NULL,
        [변경후]     NVARCHAR(4000) NULL,

        CONSTRAINT [PK_변경이력] PRIMARY KEY CLUSTERED ([이력ID]),
        -- 완료이력을 쓰는 Write SP 가 0개다. scripts/copy-completion.sql 은 사람이 돌리는
        -- 스크립트라 변경이력을 남기지 않는다. 게다가 완료이력 PK 는 (수검자ID, 완료일자)
        -- 복합키라 BIGINT 한 컬럼인 [대상키] 로는 그 행을 특정하지 못한다.
        CONSTRAINT [CK_변경이력_TARGET_TABLE] CHECK ([대상테이블] IN (N'수검자', N'예약접수')),
        CONSTRAINT [CK_변경이력_COLUMN_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([컬럼명]))) > 0)
    );
END
GO
-- 대상 행 하나의 변경 내역을 최신순으로 읽는다. USP_HC_SELECT_변경이력 의 유일한 조회 경로다
-- (04 §8.6.4). 이 테이블은 clean-create 대상이 아니라 CREATE TABLE 가드 밖에서 따로 만든다 -
-- 테이블이 이미 있고 인덱스만 없는 상태가 가능하기 때문이다.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'[dbo].[변경이력]', N'U') AND name = N'IX_변경이력_TARGET')
    CREATE NONCLUSTERED INDEX [IX_변경이력_TARGET]
        ON [dbo].[변경이력] ([대상테이블], [대상키], [기록일시] DESC);
GO
CREATE SEQUENCE [dbo].[SEQ_HC_CHART_NO]
    AS BIGINT START WITH 1 INCREMENT BY 1
    MINVALUE 1 MAXVALUE 999999 NO CYCLE CACHE 50;
GO
PRINT N'PASS SCH-DEPLOY 스키마 배포 완료';
GO
