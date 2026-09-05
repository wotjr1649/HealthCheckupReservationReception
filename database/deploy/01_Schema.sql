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
DROP TABLE IF EXISTS [dbo].[검사항목];
DROP TABLE IF EXISTS [dbo].[예약접수];
DROP TABLE IF EXISTS [dbo].[휴무일];
DROP TABLE IF EXISTS [dbo].[검사코드];
DROP TABLE IF EXISTS [dbo].[수검자];
DROP SEQUENCE IF EXISTS [dbo].[SEQ_HC_CHART_NO];
GO
CREATE TABLE [dbo].[수검자]
(
    [PatientId]          BIGINT          IDENTITY(1,1) NOT NULL,
    [ChartNo]            NVARCHAR(100)   NOT NULL,
    [Name]               NVARCHAR(100)   NOT NULL,
    [SocialNumber]       VARCHAR(13)     NOT NULL,
    [Birthday]           VARCHAR(8)      NOT NULL,
    [Gender]             CHAR(1)         NOT NULL,
    [EMail]              VARCHAR(200)    NULL,
    [CelNumberS]         VARCHAR(13)     NULL,
    [CelNumber]          VARCHAR(13)     NULL,
    [TelNumber]          VARCHAR(13)     NULL,
    [Zipcode]            VARCHAR(10)     NULL,
    [Address]            NVARCHAR(200)   NULL,
    [AddressDetail]      NVARCHAR(200)   NULL,
    [Memo]               NVARCHAR(MAX)   NULL,
    [HepatitisBExcluded] BIT             NOT NULL CONSTRAINT [DF_수검자_HEPATITIS_B_EXCLUDED] DEFAULT (0),
    [CreationDate]       DATETIME        NOT NULL CONSTRAINT [DF_수검자_CREATION_DATE]        DEFAULT (GETDATE()),
    [LastEditDate]       DATETIME        NOT NULL CONSTRAINT [DF_수검자_LAST_EDIT_DATE]       DEFAULT (GETDATE()),

    CONSTRAINT [PK_수검자] PRIMARY KEY CLUSTERED ([PatientId]),
    CONSTRAINT [UQ_수검자_CHART_NO]       UNIQUE ([ChartNo]),
    CONSTRAINT [UQ_수검자_SOCIAL_NUMBER]  UNIQUE ([SocialNumber]),

    CONSTRAINT [CK_수검자_CHART_NO_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([ChartNo]))) > 0),
    CONSTRAINT [CK_수검자_NAME_NOT_BLANK]     CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_수검자_SOCIAL_FORMAT]      CHECK (LEN([SocialNumber]) = 13 AND [SocialNumber] NOT LIKE '%[^0-9]%'),
    CONSTRAINT [CK_수검자_BIRTHDAY]           CHECK (LEN([Birthday]) = 8 AND [Birthday] NOT LIKE '%[^0-9]%' AND TRY_CONVERT(DATE, [Birthday], 112) IS NOT NULL),
    CONSTRAINT [CK_수검자_GENDER]             CHECK ([Gender] IN ('M','F')),
    CONSTRAINT [CK_수검자_CEL_NORMALIZED]     CHECK (([CelNumber] IS NULL AND [CelNumberS] IS NULL) OR ([CelNumber] IS NOT NULL AND [CelNumberS] = REPLACE([CelNumber], '-', ''))),
    CONSTRAINT [CK_수검자_CEL_DIGIT]          CHECK ([CelNumberS] IS NULL OR [CelNumberS] NOT LIKE '%[^0-9]%'),
    CONSTRAINT [CK_수검자_EDIT_DATE]          CHECK ([LastEditDate] >= [CreationDate])
);
GO
CREATE NONCLUSTERED INDEX [IX_수검자_NAME_BIRTHDAY]
    ON [dbo].[수검자] ([Name], [Birthday])
    INCLUDE ([PatientId], [ChartNo], [Gender], [CelNumber]);
GO
CREATE NONCLUSTERED INDEX [IX_수검자_BIRTHDAY]
    ON [dbo].[수검자] ([Birthday])
    INCLUDE ([PatientId], [ChartNo], [Name], [Gender], [CelNumber]);
GO
CREATE NONCLUSTERED INDEX [IX_수검자_CEL_NUMBER_S]
    ON [dbo].[수검자] ([CelNumberS])
    INCLUDE ([PatientId], [ChartNo], [Name], [Birthday], [Gender], [CelNumber])
    WHERE [CelNumberS] IS NOT NULL;
GO
CREATE TABLE [dbo].[검사코드]
(
    [ExamItemCode]         VARCHAR(10)   NOT NULL,
    [ExamItemName]         NVARCHAR(100) NOT NULL,
    [NexRuleCode]          VARCHAR(10)   NULL,
    [AdditionalExamCode]   VARCHAR(10)   NULL,
    [AdditionalGenderCode] CHAR(1)       NULL,
    [AdditionalActive]     BIT           NOT NULL CONSTRAINT [DF_검사코드_AEX_ACTIVE] DEFAULT (0),

    CONSTRAINT [PK_검사코드] PRIMARY KEY CLUSTERED ([ExamItemCode]),
    CONSTRAINT [CK_검사코드_CODE_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([ExamItemCode]))) > 0),
    CONSTRAINT [CK_검사코드_NAME_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([ExamItemName]))) > 0),
    CONSTRAINT [CK_검사코드_ROLE_REQUIRED]  CHECK ([NexRuleCode] IS NOT NULL OR [AdditionalExamCode] IS NOT NULL),
    CONSTRAINT [CK_검사코드_NEX_RULE]       CHECK ([NexRuleCode] IS NULL OR [NexRuleCode] IN ('NEX-01','NEX-02','NEX-03','NEX-04','NEX-05','NEX-06')),
    CONSTRAINT [CK_검사코드_AEX_CODE]       CHECK ([AdditionalExamCode] IS NULL OR [AdditionalExamCode] IN ('OPT01','OPT02','OPT03','OPT04','OPT05','OPT06','OPT07')),
    CONSTRAINT [CK_검사코드_AEX_GENDER]     CHECK ([AdditionalGenderCode] IS NULL OR [AdditionalGenderCode] IN ('A','M','F')),
    CONSTRAINT [CK_검사코드_AEX_GROUP]      CHECK
    (
        ([AdditionalExamCode] IS NULL     AND [AdditionalGenderCode] IS NULL     AND [AdditionalActive] = 0)
     OR ([AdditionalExamCode] IS NOT NULL AND [AdditionalGenderCode] IS NOT NULL)
    )
);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_검사코드_AEX_CODE]
    ON [dbo].[검사코드] ([AdditionalExamCode])
    WHERE [AdditionalExamCode] IS NOT NULL;
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
    [WorkId]          BIGINT       IDENTITY(1,1) NOT NULL,
    [PatientId]       BIGINT       NOT NULL,
    [ReservationDate] DATE         NOT NULL,
    [TimeSlotCode]    CHAR(2)      NOT NULL,
    [StatusCode]      CHAR(3)      NOT NULL,
    [CreationDate]    DATETIME2(0) NOT NULL CONSTRAINT [DF_예약접수_CREATION_DATE]  DEFAULT (SYSDATETIME()),
    [LastEditDate]    DATETIME2(0) NOT NULL CONSTRAINT [DF_예약접수_LAST_EDIT_DATE] DEFAULT (SYSDATETIME()),
    [RowVersion]      ROWVERSION   NOT NULL,

    CONSTRAINT [PK_예약접수] PRIMARY KEY CLUSTERED ([WorkId]),
    CONSTRAINT [FK_예약접수_수검자] FOREIGN KEY ([PatientId])
        REFERENCES [dbo].[수검자] ([PatientId]) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT [CK_예약접수_TIME_SLOT] CHECK ([TimeSlotCode] IN ('AM','PM')),
    CONSTRAINT [CK_예약접수_STATUS]    CHECK ([StatusCode] IN ('RSV','RCP','CNR','CNC')),
    CONSTRAINT [CK_예약접수_EDIT_DATE] CHECK ([LastEditDate] >= [CreationDate])
);
GO
CREATE NONCLUSTERED INDEX [IX_예약접수_SLOT]
    ON [dbo].[예약접수] ([ReservationDate], [TimeSlotCode], [StatusCode])
    INCLUDE ([PatientId]);
GO
CREATE NONCLUSTERED INDEX [IX_예약접수_PATIENT_STATE_DATE]
    ON [dbo].[예약접수] ([PatientId], [StatusCode], [ReservationDate])
    INCLUDE ([TimeSlotCode]);
GO
CREATE TABLE [dbo].[검사항목]
(
    [업무ID]       BIGINT      NOT NULL,
    [검사항목코드] VARCHAR(10) NOT NULL,
    [검사출처코드] CHAR(3)     NOT NULL,

    CONSTRAINT [PK_검사항목] PRIMARY KEY CLUSTERED ([업무ID], [검사항목코드]),
    CONSTRAINT [FK_검사항목_예약접수] FOREIGN KEY ([업무ID])
        REFERENCES [dbo].[예약접수] ([WorkId]) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT [FK_검사항목_검사코드] FOREIGN KEY ([검사항목코드])
        REFERENCES [dbo].[검사코드] ([ExamItemCode]) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT [CK_검사항목_SOURCE] CHECK ([검사출처코드] IN ('NEX','AEX'))
);
GO
CREATE TABLE [dbo].[완료이력]
(
    [수검자ID]  BIGINT NOT NULL,
    [완료일자]  DATE   NOT NULL,

    CONSTRAINT [PK_완료이력] PRIMARY KEY CLUSTERED ([수검자ID], [완료일자]),
    CONSTRAINT [FK_완료이력_수검자] FOREIGN KEY ([수검자ID])
        REFERENCES [dbo].[수검자] ([PatientId]) ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO
CREATE TABLE [dbo].[변경이력]
(
    [이력ID]     BIGINT        IDENTITY(1,1) NOT NULL,
    [기록일시]   DATETIME2(0)  NOT NULL CONSTRAINT [DF_변경이력_CREATION_DATE] DEFAULT (SYSDATETIME()),
    [조작자명]   NVARCHAR(50)  NULL,
    [업무코드]   VARCHAR(20)   NOT NULL,
    [대상테이블] NVARCHAR(10)  NOT NULL,
    [대상키]     BIGINT        NULL,
    [결과코드]   INT           NOT NULL,

    CONSTRAINT [PK_변경이력] PRIMARY KEY CLUSTERED ([이력ID]),
    CONSTRAINT [CK_변경이력_OPERATION] CHECK (
        ([대상테이블] = N'수검자'   AND [업무코드] IN ('PAT_INSERT','PAT_UPDATE'))
     OR ([대상테이블] = N'예약접수' AND [업무코드] IN ('RSV_INSERT','RSV_UPDATE','RSV_CANCEL','RCP_ACCEPT','RCP_AEX','RCP_CANCEL'))),
    CONSTRAINT [CK_변경이력_RESULT_CODE] CHECK ([결과코드] BETWEEN 0 AND 9 OR [결과코드] BETWEEN 100 AND 799),
    CONSTRAINT [CK_변경이력_TARGET_KEY]  CHECK ([결과코드] >= 100 OR [대상키] IS NOT NULL)
);
GO
CREATE SEQUENCE [dbo].[SEQ_HC_CHART_NO]
    AS BIGINT START WITH 1 INCREMENT BY 1
    MINVALUE 1 MAXVALUE 999999 NO CYCLE CACHE 50;
GO
PRINT N'PASS SCH-DEPLOY 스키마 배포 완료';
GO
