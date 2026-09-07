/*  dev-completion-sp.sql ― 완료이력을 손으로 넣고 지우는 개발 전용 SP

    [!] 배포물이 아니다. Deploy.sql 이 참조하지 않고 WinForms 도 부르지 않는다.
        시나리오 시험에서 "과거에 검진을 받은 수검자" 상태를 만들 때만 쓴다.

    이름이 USP_HC_ 접두사 **밖**인 것은 우연이 아니다. 계약 개수를 세는 게이트가 전부
    `name LIKE 'USP[_]HC[_]%'` 로 거르므로(SCH-014 · VER-003 · RBD-004 지문 · SCH-019),
    그 밖에 두면 16개 계약을 한 글자도 건드리지 않는다.
    scripts/test.sh 는 언제나 rebuild.sh(DROP DATABASE)로 시작하므로 이 SP 가 회귀에
    섞이는 일도 없다 ― tests/14 의 OBJECTS 덤프에 나타날 수 없다.

    기존 scripts/copy-completion.sql 은 "RCP 업무 전체를 한꺼번에 복사" 다.
    이 SP 는 그 반대로 "한 사람의 한 날짜를 콕 집어" 만들고 지운다.

    설치:  ./scripts/dev-completion.sh
    사용:  EXEC [dbo].[DEV_완료이력_등록] @ChartNo = N'T001', @CompletionDate = '2024-05-01';
           EXEC [dbo].[DEV_완료이력_등록] @ChartNo = N'T001', @CompletionDate = '2024-05-01', @Delete = 1;
*/
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[DEV_완료이력_등록]
(
    @ChartNo        NVARCHAR(100),
    @CompletionDate DATE,
    @Nex            NVARCHAR(100) = NULL,   -- NULL 이면 현재 Master 의 NEX-01 기본검사로 채운다
    @Aex            NVARCHAR(50)  = NULL,   -- 예: N'EX014' 또는 N'EX014,EX015'
    @Unknown        BIT           = 0,      -- 1 이면 검사구성을 모르는 외부 기관 이력 (둘 다 NULL)
    @Delete         BIT           = 0       -- 1 이면 그 (수검자, 완료일자) 를 지운다
)
AS
BEGIN
    SET NOCOUNT ON;

    -- 계약 SP 가 아니므로 RS0 형상을 흉내내지 않는다. 사람이 읽는 한 줄을 돌려준다.
    DECLARE @Pid BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = @ChartNo);
    IF @Pid IS NULL
    BEGIN
        SELECT N'실패' AS [결과], N'차트번호를 찾을 수 없습니다: ' + ISNULL(@ChartNo, N'(NULL)') AS [사유];
        RETURN;
    END

    IF @Delete = 1
    BEGIN
        DELETE FROM [dbo].[완료이력] WHERE [수검자ID] = @Pid AND [완료일자] = @CompletionDate;
        SELECT CASE WHEN @@ROWCOUNT = 0 THEN N'없음' ELSE N'삭제' END AS [결과]
             , @ChartNo AS [차트번호], @CompletionDate AS [완료일자];
        RETURN;
    END

    -- @Unknown 은 "외부 기관에서 받았고 내용을 모른다" 다. 스키마가 둘 다 NULL 을 허용한다 (04 §8.5.2).
    -- [X] 초안은 @Unknown=1 일 때 @Nex·@Aex 를 **조용히 버렸다.** 실측에서 @Unknown=1 + @Aex='EX014'
    --     호출이 아무 말 없이 (NULL, NULL) 로 저장됐다. 시나리오를 세우는 도구가 입력을 삼키면
    --     세운 상태와 의도한 상태가 갈린다 — 모순된 입력은 거절한다.
    IF @Unknown = 1 AND (@Nex IS NOT NULL OR @Aex IS NOT NULL)
    BEGIN
        SELECT N'실패' AS [결과], N'@Unknown=1 은 검사구성을 모른다는 뜻입니다. @Nex·@Aex 와 함께 쓸 수 없습니다.' AS [사유];
        RETURN;
    END

    IF @Unknown = 1
    BEGIN
        SET @Nex = NULL;
        SET @Aex = NULL;
    END
    ELSE IF @Nex IS NULL
    BEGIN
        -- 기본검사(NEX-01)를 검사코드에서 유도한다. 손으로 적으면 Seed 와 어긋난다.
        -- STRING_AGG·FOR XML 은 허용목록 밖이라 WHILE 로 잇는다 (06 §9.2).
        DECLARE @Csv NVARCHAR(200) = N'', @C VARCHAR(10);
        DECLARE @B TABLE (C VARCHAR(10) PRIMARY KEY);
        INSERT INTO @B (C) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] = 'NEX-01';
        WHILE EXISTS (SELECT 1 FROM @B)
        BEGIN
            SELECT TOP (1) @C = C FROM @B ORDER BY C;
            SET @Csv = @Csv + @C + N',';
            DELETE FROM @B WHERE C = @C;
        END
        SET @Nex = LEFT(@Csv, LEN(@Csv) - 1);
    END

    -- 스키마에는 완료이력용 EXAM_PAIR 제약이 없어 (NEX 없이 AEX 만) 이 통과한다.
    -- 04 §2.3 "추가검사는 국가검사 위에 얹는 것" 에 어긋나므로 여기서 막는다.
    IF @Aex IS NOT NULL AND @Nex IS NULL
    BEGIN
        SELECT N'실패' AS [결과], N'국가검사 없이 추가검사만 넣을 수 없습니다 (04 §2.3).' AS [사유];
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM [dbo].[완료이력] WHERE [수검자ID] = @Pid AND [완료일자] = @CompletionDate)
        UPDATE [dbo].[완료이력]
           SET [국가검사항목] = @Nex, [추가검사항목] = @Aex
         WHERE [수검자ID] = @Pid AND [완료일자] = @CompletionDate;
    ELSE
        INSERT INTO [dbo].[완료이력] ([수검자ID], [완료일자], [국가검사항목], [추가검사항목])
        VALUES (@Pid, @CompletionDate, @Nex, @Aex);

    SELECT N'저장' AS [결과], @ChartNo AS [차트번호], @CompletionDate AS [완료일자]
         , @Nex AS [국가검사항목], @Aex AS [추가검사항목]
         , (SELECT COUNT(*) FROM [dbo].[완료이력] WHERE [수검자ID] = @Pid) AS [이 수검자 이력수];
END
GO
PRINT N'INFO DEV_완료이력_등록 설치 완료 (개발 전용 · 배포물 아님)';
GO
