-- 필터형 인덱스는 QUOTED_IDENTIFIER ON 을 요구한다. sqlcmd 기본값은 OFF 라서(SSMS 와 다르다)
-- 이 SET 이 없으면 CREATE INDEX 가 Msg 1934 로 실패한다(실측 확인).
-- parse 시점에 적용되므로 반드시 앞선 배치에 두고 GO 로 끊는다.
SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
PRINT N'--- 01_Schema 시작 ---';
GO
DROP TABLE IF EXISTS [dbo].[변경이력];
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
    [생년월일]        VARCHAR(8)      NOT NULL,
    [성별]            CHAR(1)         NOT NULL,
    [이메일]          VARCHAR(200)    NULL,
    [휴대전화]        VARCHAR(13)     NULL,
    [전화번호]        VARCHAR(13)     NULL,
    [우편번호]        VARCHAR(10)     NULL,
    [주소]            NVARCHAR(200)   NULL,
    [상세주소]        NVARCHAR(200)   NULL,
    [비고]            NVARCHAR(MAX)   NULL,
    [B형간염제외여부] BIT             NOT NULL CONSTRAINT [DF_수검자_HEPATITIS_B_EXCLUDED] DEFAULT (0),
    [생성일시]        DATETIME        NOT NULL CONSTRAINT [DF_수검자_CREATION_DATE]        DEFAULT (GETDATE()),
    [최종수정일시]    DATETIME        NOT NULL CONSTRAINT [DF_수검자_LAST_EDIT_DATE]       DEFAULT (GETDATE()),

    CONSTRAINT [PK_수검자] PRIMARY KEY CLUSTERED ([수검자ID]),
    CONSTRAINT [UQ_수검자_CHART_NO]       UNIQUE ([차트번호]),
    CONSTRAINT [UQ_수검자_SOCIAL_NUMBER]  UNIQUE ([주민번호]),

    CONSTRAINT [CK_수검자_CHART_NO_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([차트번호]))) > 0),
    CONSTRAINT [CK_수검자_NAME_NOT_BLANK]     CHECK (LEN(LTRIM(RTRIM([성명]))) > 0),
    CONSTRAINT [CK_수검자_SOCIAL_FORMAT]      CHECK (LEN([주민번호]) = 13 AND [주민번호] NOT LIKE '%[^0-9]%'),
    CONSTRAINT [CK_수검자_BIRTHDAY]           CHECK (LEN([생년월일]) = 8 AND [생년월일] NOT LIKE '%[^0-9]%' AND TRY_CONVERT(DATE, [생년월일], 112) IS NOT NULL),
    CONSTRAINT [CK_수검자_GENDER]             CHECK ([성별] IN ('M','F')),
    -- CelNumberS 를 제거했으므로 CK_수검자_CEL_NORMALIZED 는 지킬 대상이 없어 사라진다.
    -- 숫자 보증은 잃지 않는다 - 표시값에서 '-' 를 뺀 결과를 직접 검사한다.
    CONSTRAINT [CK_수검자_CEL_DIGIT]          CHECK ([휴대전화] IS NULL OR REPLACE([휴대전화], '-', '') NOT LIKE '%[^0-9]%'),
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
    [생성일시]     DATETIME2(0) NOT NULL CONSTRAINT [DF_예약접수_CREATION_DATE]  DEFAULT (SYSDATETIME()),
    [최종수정일시] DATETIME2(0) NOT NULL CONSTRAINT [DF_예약접수_LAST_EDIT_DATE] DEFAULT (SYSDATETIME()),
    [행버전]       ROWVERSION   NOT NULL,
    -- 검사구성. 검사항목코드를 오름차순으로 쉼표로 잇는다 (plans/10 §1).
    -- [X] NOT NULL 은 "국가검사가 반드시 있다" 를 보증하지 않는다. CORRUPT-2(저장 NEX 0행)를
    --     만들 수 있어야 하므로 빈 문자열이 통과한다. 빈 문자열이 검사구성 손상의 유일한
    --     표현이고 판정식은 LEN([국가검사항목]) = 0 이다. NULL 과 빈 문자열이 둘 다 손상을
    --     뜻하는 상태를 만들지 않으려고 NOT NULL 을 건다.
    [국가검사항목] NVARCHAR(100) NOT NULL,
    [추가검사항목] NVARCHAR(50)  NULL,

    CONSTRAINT [PK_예약접수] PRIMARY KEY CLUSTERED ([업무ID]),
    CONSTRAINT [FK_예약접수_수검자] FOREIGN KEY ([수검자ID])
        REFERENCES [dbo].[수검자] ([수검자ID]) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT [CK_예약접수_TIME_SLOT] CHECK ([시간대코드] IN ('AM','PM')),
    CONSTRAINT [CK_예약접수_STATUS]    CHECK ([상태코드] IN ('RSV','RCP','CNR','CNC')),
    CONSTRAINT [CK_예약접수_EDIT_DATE] CHECK ([최종수정일시] >= [생성일시]),
    -- 코드 존재는 보증하지 못한다(FK_검사항목_검사코드 를 잃은 대가). 형식만 막는다.
    CONSTRAINT [CK_예약접수_EXAM_FORMAT] CHECK
    (
        [국가검사항목] NOT LIKE '%[^A-Z0-9,]%'
    AND ([추가검사항목] IS NULL OR [추가검사항목] NOT LIKE '%[^A-Z0-9,]%')
    )
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
    -- 검사구성. 예약접수와 같은 형식이다 (plans/10 §1 · §4.3).
    -- 외부 기관 검진은 검사 내용을 모를 수 있으므로 둘 다 NULL 을 허용한다.
    [국가검사항목] NVARCHAR(100) NULL,
    [추가검사항목] NVARCHAR(50)  NULL,

    CONSTRAINT [PK_완료이력] PRIMARY KEY CLUSTERED ([수검자ID], [완료일자]),
    CONSTRAINT [FK_완료이력_수검자] FOREIGN KEY ([수검자ID])
        REFERENCES [dbo].[수검자] ([수검자ID]) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT [CK_완료이력_EXAM_FORMAT] CHECK
    (
        ([국가검사항목] IS NULL OR [국가검사항목] NOT LIKE '%[^A-Z0-9,]%')
    AND ([추가검사항목] IS NULL OR [추가검사항목] NOT LIKE '%[^A-Z0-9,]%')
    )
);
GO
CREATE TABLE [dbo].[변경이력]
(
    -- 성공한 데이터 변경만 기록한다. 바뀐 컬럼 1개당 1행이다 (plans/10 §4.5, 00 CP-06).
    -- 실패한 호출은 데이터를 바꾸지 않으므로 남기지 않는다.
    [이력ID]     BIGINT         IDENTITY(1,1) NOT NULL,
    [기록일시]   DATETIME2(0)   NOT NULL CONSTRAINT [DF_변경이력_CREATION_DATE] DEFAULT (SYSDATETIME()),
    [조작자명]   NVARCHAR(50)   NULL,
    [대상테이블] NVARCHAR(10)   NOT NULL,
    [대상키]     BIGINT         NULL,
    [컬럼명]     NVARCHAR(30)   NOT NULL,
    -- 감사 기록은 복원 근거가 아니라 열람용이라 4000자에서 자른다 (00 CP-06).
    [변경전]     NVARCHAR(4000) NULL,
    [변경후]     NVARCHAR(4000) NULL,

    CONSTRAINT [PK_변경이력] PRIMARY KEY CLUSTERED ([이력ID]),
    CONSTRAINT [CK_변경이력_TARGET_TABLE] CHECK ([대상테이블] IN (N'수검자', N'예약접수', N'완료이력')),
    CONSTRAINT [CK_변경이력_COLUMN_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([컬럼명]))) > 0)
);
GO
CREATE SEQUENCE [dbo].[SEQ_HC_CHART_NO]
    AS BIGINT START WITH 1 INCREMENT BY 1
    MINVALUE 1 MAXVALUE 999999 NO CYCLE CACHE 50;
GO
PRINT N'PASS SCH-DEPLOY 스키마 배포 완료';
GO
