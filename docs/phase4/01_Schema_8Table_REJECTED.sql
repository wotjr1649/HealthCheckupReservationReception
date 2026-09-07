-- ============================================================================
-- 기각 — 배포하지 않는다. deploy/ 에 두지 않는 이유가 이것이다.
--
-- 2026-09-06 검토. 외부 LLM 이 만든 8테이블 대안이며 다음 하드 게이트를 위반한다.
--   G06  금지 객체        Trigger 0 / 추가 Table 0   -> Trigger 3 · Table +2
--   SCH-010 Trigger 수 0                              -> 3
--   RBD-004 인벤토리      Table 6 / FK 2 / NCI 4      -> Table 8 / FK 6 / NCI 8
--   06 §9.2 허용목록      업무 Trigger 금지 (04 §3.1-17)
--
-- 계약 파괴: 컬럼명 17개 변경 · 4개 삭제 · 타입 18건 변경.
--   차트번호 NVARCHAR(100) -> BIGINT + NO MAXVALUE 는 @ChartNo 계약과
--   ResultCode 206 ChartNoLimit 을 동시에 죽인다.
--   생년월일 VARCHAR(8) -> DATE 는 Result Set Birthday 계약 6곳을 깬다.
--
-- 반영한 것: 없음(스키마). 앞뒤 공백 거부는 Write SP 입력 정규화로 흡수한다.
-- 확정 스키마는 database/deploy/01_Schema.sql 하나다.
-- ============================================================================
-- ============================================================================
-- HealthCheckupReservationReceptionDb
-- 01_Schema - 8 Table Final Candidate (Korean Column Names)
--
-- 설계 원칙
--   1) 기존 6개 테이블의 책임을 최대한 유지한다.
--   2) 예약/완료의 검사목록 CSV 저장만 관계형 상세 테이블로 정상화한다.
--   3) NEX/OPT/상태/시간대는 현재 범위에서 별도 Master 테이블로 분리하지 않는다.
--   4) 모든 업무 컬럼명은 한글로 작성한다.
--   5) SQL Server 2016+ 호환 문법만 사용한다.
-- ============================================================================

-- 필터형 인덱스는 QUOTED_IDENTIFIER ON 을 요구한다.
-- parse 시점 적용을 보장하기 위해 선행 배치에서 설정한다.
USE [HealthCheckupReservationReceptionDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
GO

PRINT N'--- 01_Schema 시작 ---';
GO

-- ============================================================================
-- DROP : FK 의존성 역순
-- ============================================================================
DROP TABLE IF EXISTS [dbo].[변경이력];
DROP TABLE IF EXISTS [dbo].[완료검사항목];
DROP TABLE IF EXISTS [dbo].[완료이력];
DROP TABLE IF EXISTS [dbo].[예약검사항목];
DROP TABLE IF EXISTS [dbo].[예약접수];
DROP TABLE IF EXISTS [dbo].[휴무일];
DROP TABLE IF EXISTS [dbo].[검사코드];
DROP TABLE IF EXISTS [dbo].[수검자];
GO

DROP SEQUENCE IF EXISTS [dbo].[SEQ_수검자_차트번호];
GO

-- ============================================================================
-- SEQUENCE : 업무상 차트번호
-- DB 내부 PK(수검자식별번호)와 업무상 번호(차트번호)를 분리한다.
-- ============================================================================
CREATE SEQUENCE [dbo].[SEQ_수검자_차트번호]
    AS BIGINT
    START WITH 1
    INCREMENT BY 1
    MINVALUE 1
    NO MAXVALUE
    NO CYCLE
    CACHE 50;
GO

-- ============================================================================
-- 1. 수검자
-- ============================================================================
CREATE TABLE [dbo].[수검자]
(
    [수검자식별번호]    BIGINT         IDENTITY(1,1) NOT NULL,
    [차트번호]          BIGINT         NOT NULL
        CONSTRAINT [DF_수검자_차트번호]
        DEFAULT (NEXT VALUE FOR [dbo].[SEQ_수검자_차트번호]),

    [성명]              NVARCHAR(100)  NOT NULL,
    [주민등록번호]      CHAR(13)       NOT NULL,
    [생년월일]          DATE           NOT NULL,
    [성별코드]          CHAR(1)        NOT NULL,

    [이메일주소]        VARCHAR(254)   NULL,
    [휴대전화번호]      VARCHAR(15)    NULL,
    [전화번호]          VARCHAR(15)    NULL,
    [우편번호]          VARCHAR(10)    NULL,
    [주소]              NVARCHAR(200)  NULL,
    [상세주소]          NVARCHAR(200)  NULL,
    [비고]              NVARCHAR(2000) NULL,

    [비형간염제외여부]   BIT            NOT NULL
        CONSTRAINT [DF_수검자_비형간염제외여부] DEFAULT (0),

    [생성일시]          DATETIME2(3)   NOT NULL
        CONSTRAINT [DF_수검자_생성일시] DEFAULT (SYSDATETIME()),

    [최종수정일시]      DATETIME2(3)   NOT NULL
        CONSTRAINT [DF_수검자_최종수정일시] DEFAULT (SYSDATETIME()),

    CONSTRAINT [PK_수검자]
        PRIMARY KEY CLUSTERED ([수검자식별번호]),

    CONSTRAINT [UQ_수검자_차트번호]
        UNIQUE ([차트번호]),

    CONSTRAINT [UQ_수검자_주민등록번호]
        UNIQUE ([주민등록번호]),

    CONSTRAINT [CK_수검자_성명]
        CHECK
        (
            DATALENGTH(LTRIM(RTRIM([성명]))) > 0
            AND DATALENGTH([성명]) = DATALENGTH(LTRIM(RTRIM([성명])))
        ),

    CONSTRAINT [CK_수검자_주민등록번호]
        CHECK
        (
            [주민등록번호] NOT LIKE '%[^0-9]%'
        ),

    CONSTRAINT [CK_수검자_성별코드]
        CHECK ([성별코드] IN ('M', 'F')),

    CONSTRAINT [CK_수검자_이메일주소]
        CHECK
        (
            [이메일주소] IS NULL
            OR
            (
                LEN([이메일주소]) > 0
                AND [이메일주소] NOT LIKE '% %'
            )
        ),

    -- 전화번호는 표시용 '-' 없이 숫자만 저장한다.
    CONSTRAINT [CK_수검자_휴대전화번호]
        CHECK
        (
            [휴대전화번호] IS NULL
            OR
            (
                LEN([휴대전화번호]) BETWEEN 10 AND 15
                AND [휴대전화번호] NOT LIKE '%[^0-9]%'
            )
        ),

    CONSTRAINT [CK_수검자_전화번호]
        CHECK
        (
            [전화번호] IS NULL
            OR
            (
                LEN([전화번호]) BETWEEN 7 AND 15
                AND [전화번호] NOT LIKE '%[^0-9]%'
            )
        ),

    CONSTRAINT [CK_수검자_우편번호]
        CHECK
        (
            [우편번호] IS NULL
            OR LEN(LTRIM(RTRIM([우편번호]))) > 0
        ),

    CONSTRAINT [CK_수검자_주소]
        CHECK
        (
            [주소] IS NULL
            OR LEN(LTRIM(RTRIM([주소]))) > 0
        ),

    CONSTRAINT [CK_수검자_상세주소]
        CHECK
        (
            [상세주소] IS NULL
            OR LEN(LTRIM(RTRIM([상세주소]))) > 0
        ),

    CONSTRAINT [CK_수검자_최종수정일시]
        CHECK ([최종수정일시] >= [생성일시])
);
GO

CREATE NONCLUSTERED INDEX [IX_수검자_성명_생년월일]
    ON [dbo].[수검자] ([성명], [생년월일])
    INCLUDE ([차트번호], [성별코드], [휴대전화번호]);
GO

CREATE NONCLUSTERED INDEX [IX_수검자_생년월일]
    ON [dbo].[수검자] ([생년월일])
    INCLUDE ([차트번호], [성명], [성별코드], [휴대전화번호]);
GO

-- ============================================================================
-- 2. 검사코드
-- 검사 Master + 현재 프로젝트에서 필요한 NEX/OPT 역할을 한 테이블에 유지한다.
-- NEX/OPT가 작은 고정 코드 집합이므로 별도 Master 테이블로 과분리하지 않는다.
-- ============================================================================
CREATE TABLE [dbo].[검사코드]
(
    [검사항목코드]        VARCHAR(10)   NOT NULL,
    [검사항목명]          NVARCHAR(100) NOT NULL,

    [국가검사규칙코드]    VARCHAR(6)    NULL,

    [추가검사옵션코드]    VARCHAR(5)    NULL,
    [추가검사적용성별코드] CHAR(1)      NULL,
    [추가검사사용여부]    BIT           NOT NULL
        CONSTRAINT [DF_검사코드_추가검사사용여부] DEFAULT (0),

    -- 검사 항목 자체의 전체 사용 여부.
    [사용여부]            BIT           NOT NULL
        CONSTRAINT [DF_검사코드_사용여부] DEFAULT (1),

    CONSTRAINT [PK_검사코드]
        PRIMARY KEY CLUSTERED ([검사항목코드]),

    CONSTRAINT [CK_검사코드_검사항목코드]
        CHECK
        (
            DATALENGTH(LTRIM(RTRIM([검사항목코드]))) > 0
            AND DATALENGTH([검사항목코드]) = DATALENGTH(LTRIM(RTRIM([검사항목코드])))
        ),

    CONSTRAINT [CK_검사코드_검사항목명]
        CHECK
        (
            DATALENGTH(LTRIM(RTRIM([검사항목명]))) > 0
            AND DATALENGTH([검사항목명]) = DATALENGTH(LTRIM(RTRIM([검사항목명])))
        ),

    -- 이 프로젝트에 등록되는 검사코드는 국가검사 또는 추가검사 중 하나 이상의 역할을 가진다.
    CONSTRAINT [CK_검사코드_역할필수]
        CHECK
        (
            [국가검사규칙코드] IS NOT NULL
            OR [추가검사옵션코드] IS NOT NULL
        ),

    CONSTRAINT [CK_검사코드_국가검사규칙코드]
        CHECK
        (
            [국가검사규칙코드] IS NULL
            OR [국가검사규칙코드] IN
            (
                'NEX-01', 'NEX-02', 'NEX-03',
                'NEX-04', 'NEX-05', 'NEX-06'
            )
        ),

    CONSTRAINT [CK_검사코드_추가검사옵션코드]
        CHECK
        (
            [추가검사옵션코드] IS NULL
            OR [추가검사옵션코드] IN
            (
                'OPT01', 'OPT02', 'OPT03', 'OPT04',
                'OPT05', 'OPT06', 'OPT07'
            )
        ),

    CONSTRAINT [CK_검사코드_추가검사적용성별코드]
        CHECK
        (
            [추가검사적용성별코드] IS NULL
            OR [추가검사적용성별코드] IN ('A', 'M', 'F')
        ),

    -- 추가검사 역할이 없으면 추가검사 전용 속성도 존재할 수 없다.
    -- 추가검사 역할이 있으면 적용성별은 반드시 지정한다.
    CONSTRAINT [CK_검사코드_추가검사그룹]
        CHECK
        (
            (
                [추가검사옵션코드] IS NULL
                AND [추가검사적용성별코드] IS NULL
                AND [추가검사사용여부] = 0
            )
            OR
            (
                [추가검사옵션코드] IS NOT NULL
                AND [추가검사적용성별코드] IS NOT NULL
            )
        )
);
GO

-- 하나의 OPT 코드가 두 개 이상의 검사 항목을 가리키는 모호한 상태를 금지한다.
CREATE UNIQUE NONCLUSTERED INDEX [UX_검사코드_추가검사옵션코드]
    ON [dbo].[검사코드] ([추가검사옵션코드])
    WHERE [추가검사옵션코드] IS NOT NULL;
GO

-- ============================================================================
-- 3. 휴무일
-- ============================================================================
CREATE TABLE [dbo].[휴무일]
(
    [휴무일자] DATE          NOT NULL,
    [휴무일명] NVARCHAR(100) NOT NULL,
    [사용여부] BIT           NOT NULL
        CONSTRAINT [DF_휴무일_사용여부] DEFAULT (1),
    [비고]     NVARCHAR(500) NULL,

    CONSTRAINT [PK_휴무일]
        PRIMARY KEY CLUSTERED ([휴무일자]),

    CONSTRAINT [CK_휴무일_휴무일명]
        CHECK
        (
            DATALENGTH(LTRIM(RTRIM([휴무일명]))) > 0
            AND DATALENGTH([휴무일명]) = DATALENGTH(LTRIM(RTRIM([휴무일명])))
        )
);
GO

-- ============================================================================
-- 4. 예약접수
-- 검사구성 CSV 컬럼은 제거하고 [예약검사항목] 상세 테이블로 이동한다.
-- ============================================================================
CREATE TABLE [dbo].[예약접수]
(
    [업무식별번호]      BIGINT       IDENTITY(1,1) NOT NULL,
    [수검자식별번호]    BIGINT       NOT NULL,
    [예약일자]          DATE         NOT NULL,
    [시간대코드]        CHAR(2)      NOT NULL,
    [상태코드]          CHAR(3)      NOT NULL,

    [생성일시]          DATETIME2(3) NOT NULL
        CONSTRAINT [DF_예약접수_생성일시] DEFAULT (SYSDATETIME()),

    [최종수정일시]      DATETIME2(3) NOT NULL
        CONSTRAINT [DF_예약접수_최종수정일시] DEFAULT (SYSDATETIME()),

    [행버전]            ROWVERSION   NOT NULL,

    CONSTRAINT [PK_예약접수]
        PRIMARY KEY CLUSTERED ([업무식별번호]),

    CONSTRAINT [FK_예약접수_수검자]
        FOREIGN KEY ([수검자식별번호])
        REFERENCES [dbo].[수검자] ([수검자식별번호])
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    CONSTRAINT [CK_예약접수_시간대코드]
        CHECK ([시간대코드] IN ('AM', 'PM')),

    CONSTRAINT [CK_예약접수_상태코드]
        CHECK ([상태코드] IN ('RSV', 'RCP', 'CNR', 'CNC')),

    CONSTRAINT [CK_예약접수_최종수정일시]
        CHECK ([최종수정일시] >= [생성일시])
);
GO

CREATE NONCLUSTERED INDEX [IX_예약접수_예약일자_시간대코드_상태코드]
    ON [dbo].[예약접수] ([예약일자], [시간대코드], [상태코드])
    INCLUDE ([수검자식별번호]);
GO

CREATE NONCLUSTERED INDEX [IX_예약접수_수검자식별번호_상태코드_예약일자]
    ON [dbo].[예약접수] ([수검자식별번호], [상태코드], [예약일자])
    INCLUDE ([시간대코드]);
GO

-- ============================================================================
-- 5. 예약검사항목
-- 국가검사(N)와 추가검사(A)를 하나의 상세 테이블에서 관리한다.
-- 동일 검사 항목이 국가/추가 양쪽 역할을 가지는 경우 역할별 1행씩 저장 가능하다.
-- ============================================================================
CREATE TABLE [dbo].[예약검사항목]
(
    [업무식별번호] BIGINT       NOT NULL,
    [검사구분코드] CHAR(1)      NOT NULL,
    [검사항목코드] VARCHAR(10)   NOT NULL,
    [등록일시]     DATETIME2(3) NOT NULL
        CONSTRAINT [DF_예약검사항목_등록일시] DEFAULT (SYSDATETIME()),

    CONSTRAINT [PK_예약검사항목]
        PRIMARY KEY CLUSTERED
        (
            [업무식별번호],
            [검사구분코드],
            [검사항목코드]
        ),

    CONSTRAINT [FK_예약검사항목_예약접수]
        FOREIGN KEY ([업무식별번호])
        REFERENCES [dbo].[예약접수] ([업무식별번호])
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    CONSTRAINT [FK_예약검사항목_검사코드]
        FOREIGN KEY ([검사항목코드])
        REFERENCES [dbo].[검사코드] ([검사항목코드])
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    CONSTRAINT [CK_예약검사항목_검사구분코드]
        CHECK ([검사구분코드] IN ('N', 'A'))
);
GO

CREATE NONCLUSTERED INDEX [IX_예약검사항목_검사항목코드_검사구분코드]
    ON [dbo].[예약검사항목] ([검사항목코드], [검사구분코드]);
GO

-- ============================================================================
-- 6. 완료이력
-- 기존의 (수검자, 완료일자) 복합 PK를 유지한다.
-- 검사목록을 알 수 없는 외부 완료건은 완료검사항목이 0행인 상태로 표현한다.
-- ============================================================================
CREATE TABLE [dbo].[완료이력]
(
    [수검자식별번호] BIGINT NOT NULL,
    [완료일자]       DATE   NOT NULL,

    CONSTRAINT [PK_완료이력]
        PRIMARY KEY CLUSTERED
        (
            [수검자식별번호],
            [완료일자]
        ),

    CONSTRAINT [FK_완료이력_수검자]
        FOREIGN KEY ([수검자식별번호])
        REFERENCES [dbo].[수검자] ([수검자식별번호])
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

-- 최근 완료이력 조회는 PK의 선두키가 수검자이므로 충분히 지원된다.
-- 전체 수검자의 완료일자 기준 조회를 위한 별도 인덱스만 추가한다.
CREATE NONCLUSTERED INDEX [IX_완료이력_완료일자]
    ON [dbo].[완료이력] ([완료일자] DESC);
GO

-- ============================================================================
-- 7. 완료검사항목
-- 예약검사항목과 동일한 형태로 완료 당시 검사구성을 관계형으로 저장한다.
-- ============================================================================
CREATE TABLE [dbo].[완료검사항목]
(
    [수검자식별번호] BIGINT       NOT NULL,
    [완료일자]       DATE         NOT NULL,
    [검사구분코드]   CHAR(1)      NOT NULL,
    [검사항목코드]   VARCHAR(10)   NOT NULL,
    [등록일시]       DATETIME2(3) NOT NULL
        CONSTRAINT [DF_완료검사항목_등록일시] DEFAULT (SYSDATETIME()),

    CONSTRAINT [PK_완료검사항목]
        PRIMARY KEY CLUSTERED
        (
            [수검자식별번호],
            [완료일자],
            [검사구분코드],
            [검사항목코드]
        ),

    CONSTRAINT [FK_완료검사항목_완료이력]
        FOREIGN KEY
        (
            [수검자식별번호],
            [완료일자]
        )
        REFERENCES [dbo].[완료이력]
        (
            [수검자식별번호],
            [완료일자]
        )
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    CONSTRAINT [FK_완료검사항목_검사코드]
        FOREIGN KEY ([검사항목코드])
        REFERENCES [dbo].[검사코드] ([검사항목코드])
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    CONSTRAINT [CK_완료검사항목_검사구분코드]
        CHECK ([검사구분코드] IN ('N', 'A'))
);
GO

CREATE NONCLUSTERED INDEX [IX_완료검사항목_검사항목코드_검사구분코드]
    ON [dbo].[완료검사항목] ([검사항목코드], [검사구분코드]);
GO

-- ============================================================================
-- 8. 변경이력
-- 복합키가 생겼으므로 대상키는 숫자형 1개가 아니라 문자열 표현으로 보관한다.
-- 예: 업무식별번호=100
--     수검자식별번호=10;완료일자=2026-09-06;검사구분코드=N;검사항목코드=EX001
-- ============================================================================
CREATE TABLE [dbo].[변경이력]
(
    [이력식별번호] BIGINT         IDENTITY(1,1) NOT NULL,
    [기록일시]     DATETIME2(3)   NOT NULL
        CONSTRAINT [DF_변경이력_기록일시] DEFAULT (SYSDATETIME()),

    [조작자명]     NVARCHAR(100)  NULL,
    [대상테이블명] NVARCHAR(128)  NOT NULL,
    [대상키값]     NVARCHAR(500)  NOT NULL,
    [대상컬럼명]   NVARCHAR(128)  NOT NULL,
    [변경전값]     NVARCHAR(4000) NULL,
    [변경후값]     NVARCHAR(4000) NULL,

    CONSTRAINT [PK_변경이력]
        PRIMARY KEY CLUSTERED ([이력식별번호]),

    CONSTRAINT [CK_변경이력_대상테이블명]
        CHECK (LEN(LTRIM(RTRIM([대상테이블명]))) > 0),

    CONSTRAINT [CK_변경이력_대상키값]
        CHECK (LEN(LTRIM(RTRIM([대상키값]))) > 0),

    CONSTRAINT [CK_변경이력_대상컬럼명]
        CHECK (LEN(LTRIM(RTRIM([대상컬럼명]))) > 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_변경이력_대상테이블명_대상키값_기록일시]
    ON [dbo].[변경이력]
    (
        [대상테이블명],
        [대상키값],
        [기록일시] DESC
    )
    INCLUDE ([대상컬럼명], [조작자명]);
GO

-- ============================================================================
-- 역할 무결성 Guard
-- 8개 테이블 구조를 유지하면서 예약/완료 상세의 N/A 구분이 검사코드 역할과
-- 어긋나는 것을 DB 레벨에서 차단한다.
-- ============================================================================
CREATE TRIGGER [dbo].[TR_예약검사항목_역할검증]
ON [dbo].[예약검사항목]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS I
        INNER JOIN [dbo].[검사코드] AS E
            ON E.[검사항목코드] = I.[검사항목코드]
        WHERE
               (I.[검사구분코드] = 'N' AND E.[국가검사규칙코드] IS NULL)
            OR (I.[검사구분코드] = 'A' AND E.[추가검사옵션코드] IS NULL)
    )
    BEGIN
        THROW 51001, N'예약검사항목의 검사구분코드와 검사코드의 역할이 일치하지 않습니다.', 1;
    END;
END;
GO

CREATE TRIGGER [dbo].[TR_완료검사항목_역할검증]
ON [dbo].[완료검사항목]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS I
        INNER JOIN [dbo].[검사코드] AS E
            ON E.[검사항목코드] = I.[검사항목코드]
        WHERE
               (I.[검사구분코드] = 'N' AND E.[국가검사규칙코드] IS NULL)
            OR (I.[검사구분코드] = 'A' AND E.[추가검사옵션코드] IS NULL)
    )
    BEGIN
        THROW 51002, N'완료검사항목의 검사구분코드와 검사코드의 역할이 일치하지 않습니다.', 1;
    END;
END;
GO

-- 이미 예약/완료 상세에서 사용하는 역할을 검사 Master에서 제거하는 역방향 손상을 차단한다.
CREATE TRIGGER [dbo].[TR_검사코드_역할제거방지]
ON [dbo].[검사코드]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS I
        INNER JOIN deleted AS D
            ON D.[검사항목코드] = I.[검사항목코드]
        WHERE
            D.[국가검사규칙코드] IS NOT NULL
            AND I.[국가검사규칙코드] IS NULL
            AND
            (
                EXISTS
                (
                    SELECT 1
                    FROM [dbo].[예약검사항목] AS R
                    WHERE R.[검사항목코드] = I.[검사항목코드]
                      AND R.[검사구분코드] = 'N'
                )
                OR
                EXISTS
                (
                    SELECT 1
                    FROM [dbo].[완료검사항목] AS C
                    WHERE C.[검사항목코드] = I.[검사항목코드]
                      AND C.[검사구분코드] = 'N'
                )
            )
    )
    BEGIN
        THROW 51003, N'사용 중인 국가검사 역할은 제거할 수 없습니다.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS I
        INNER JOIN deleted AS D
            ON D.[검사항목코드] = I.[검사항목코드]
        WHERE
            D.[추가검사옵션코드] IS NOT NULL
            AND I.[추가검사옵션코드] IS NULL
            AND
            (
                EXISTS
                (
                    SELECT 1
                    FROM [dbo].[예약검사항목] AS R
                    WHERE R.[검사항목코드] = I.[검사항목코드]
                      AND R.[검사구분코드] = 'A'
                )
                OR
                EXISTS
                (
                    SELECT 1
                    FROM [dbo].[완료검사항목] AS C
                    WHERE C.[검사항목코드] = I.[검사항목코드]
                      AND C.[검사구분코드] = 'A'
                )
            )
    )
    BEGIN
        THROW 51004, N'사용 중인 추가검사 역할은 제거할 수 없습니다.', 1;
    END;
END;
GO

PRINT N'PASS SCH-DEPLOY 8개 테이블 스키마 배포 완료';
GO
