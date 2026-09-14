#!/usr/bin/env bash
# TGT·NEX 판정을 실제로 밟는 점검용 수검자를 심는다 (2026-09-14 사용자 지시).
#
# 왜: 기존 점검용 10명은 **전원 완료이력이 없고 여성이 최대 37세**라, 돌려 보면 TGT-03
#     (최초 검진) 한 갈래와 NEX-02 하나밖에 안 밟힌다. 주기 판정(TGT-04)도, 여성 조건부
#     (NEX-05)도 한 번도 실행되지 않는다 — 실측으로 확인했다.
#
# 나이는 주민번호로 정한다. 생일을 01-15 로 고정해 **오늘이 몇 월이든 만나이가 확정**되게
# 했다. 규칙은 00 §7.2.2 가 갖고 여기에 베끼지 않는다 — 기대값은 아래 판정표가 DB 에서
# 직접 읽어 낸다.
#
# [X] 수검자는 SP 로 등록한다. 직접 INSERT 하면 차트번호 발급·중복검증 경로를 건너뛰어
#     「시드로만 존재하는 행」이 생긴다. 완료이력은 쓰기 SP 가 없는 Seed Data 라
#     (02 개발범위 시트) 직접 INSERT 가 유일한 길이다.
#
# 멱등이다. 이미 있으면 건너뛰고 판정표만 다시 낸다.
set -u
cd "$(dirname "$0")/.."
mkdir -p artifacts/logs

SQL_FILE=artifacts/logs/seed-tgt-cases.sql
LOG=artifacts/logs/seed-tgt-cases.log
trap 'rm -f "$SQL_FILE"' EXIT

# sqlcmd 는 BOM 이 없으면 -i 파일의 한글을 코드페이지로 읽는다 (session-19 §3-2).
printf '\xEF\xBB\xBF' > "$SQL_FILE"
cat >> "$SQL_FILE" <<'SQL'
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @사례 TABLE
(
    [차트번호] NVARCHAR(100),
    [성명]     NVARCHAR(100),
    [주민번호] VARCHAR(13),
    [휴대전화] VARCHAR(13),
    [노림수]   NVARCHAR(100)
);

INSERT INTO @사례 VALUES
  (N'D0011', N'남사십',  '8601151000021', '010-4001-0011', N'NEX-02+03 (만40 남)'),
  (N'D0012', N'남오육',  '7001151000022', '010-4001-0012', N'NEX-02+04+06 (만56 남, 조건부 최대 3종)'),
  (N'D0013', N'여오사',  '7201152000023', '010-4001-0013', N'NEX-05 (만54 여)'),
  (N'D0014', N'여육십',  '6601152000024', '010-4001-0014', N'NEX-02+05 (만60 여)'),
  (N'D0015', N'여육육',  '6001152000025', '010-4001-0015', N'NEX-05+06 (만66 여)'),
  (N'D0016', N'남십구',  '0701153000026', '010-4001-0016', N'TGT-01 비대상 400 (만19)'),
  (N'D0017', N'남사오',  '8101151000027', '010-4001-0017', N'TGT-04 비대상 401 (2025 완료)'),
  (N'D0018', N'여사오',  '8101152000028', '010-4001-0018', N'TGT-04 대상 (2023 완료, 3년차)');

DECLARE @차트 NVARCHAR(100), @성명 NVARCHAR(100), @주민 VARCHAR(13), @폰 VARCHAR(13);
DECLARE @결과코드 INT, @결과메시지 NVARCHAR(300), @수검자ID BIGINT, @발급차트 NVARCHAR(100);
DECLARE @심음 INT = 0, @있음 INT = 0;

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT [차트번호], [성명], [주민번호], [휴대전화] FROM @사례 ORDER BY [차트번호];
OPEN cur;
FETCH NEXT FROM cur INTO @차트, @성명, @주민, @폰;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [차트번호] = @차트)
    BEGIN
        SET @있음 = @있음 + 1;
    END
    ELSE
    BEGIN
        EXEC [dbo].[USP_HC_수검자_등록]
             @차트번호자동발급여부 = 0,
             @차트번호             = @차트,
             @성명                 = @성명,
             @주민번호             = @주민,
             @휴대전화             = @폰,
             @전화번호             = NULL,
             @이메일               = NULL,
             @우편번호             = NULL,
             @주소                 = NULL,
             @상세주소             = NULL,
             @비고                 = N'TGT·NEX 판정 점검용 (2026-09-14)',
             @B형간염제외여부      = 0,
             @유사수검자확인여부   = 1,
             @조작자명             = N'시드';
        SET @심음 = @심음 + 1;
    END
    FETCH NEXT FROM cur INTO @차트, @성명, @주민, @폰;
END
CLOSE cur; DEALLOCATE cur;

PRINT N'수검자 — 새로 심은 수: ' + CAST(@심음 AS NVARCHAR(10))
    + N' · 이미 있던 수: ' + CAST(@있음 AS NVARCHAR(10));

-- 완료이력. D0018 은 두 건을 심는다 — TGT-04 가 「가장 최근」을 고르는지가 그때만 드러난다.
DECLARE @이력 TABLE ([차트번호] NVARCHAR(100), [완료일자] DATE, [국가검사항목] NVARCHAR(100));
INSERT INTO @이력 VALUES
  (N'D0017', '2025-05-12', 'EX001,EX002,EX003,EX004,EX005,EX006,EX007,EX008'),
  (N'D0018', '2019-04-03', 'EX001,EX002,EX003,EX004,EX005,EX006,EX007,EX008'),
  (N'D0018', '2023-04-18', 'EX001,EX002,EX003,EX004,EX005,EX006,EX007,EX008');

INSERT INTO [dbo].[완료이력] ([수검자ID], [완료일자], [국가검사항목], [추가검사항목])
SELECT p.[수검자ID], h.[완료일자], h.[국가검사항목], NULL
  FROM @이력 h
  JOIN [dbo].[수검자] p ON p.[차트번호] = h.[차트번호]
 WHERE NOT EXISTS (SELECT 1 FROM [dbo].[완료이력] e
                    WHERE e.[수검자ID] = p.[수검자ID] AND e.[완료일자] = h.[완료일자]);

PRINT N'완료이력 — 새로 심은 행: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
GO

-- ── 판정표. 기대값을 손으로 적지 않는다. DB 가 지금 무엇이라 답하는지를 그대로 낸다.
SET NOCOUNT ON;
DECLARE @d DATE = CAST(SYSDATETIME() AS DATE);
SELECT
      [차트]       = p.[차트번호]
    , [성별]       = p.[성별]
    , [나이]       = t.[나이]
    , [TGT사유]    = t.[사유코드]
    , [최근완료]   = ISNULL(CONVERT(VARCHAR(10), t.[최근완료일자]), '-')
    , [NEX건수]    = (SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](p.[수검자ID], @d))
    , [조건부]     = ISNULL(STUFF((SELECT ',' + n.[국가검사규칙코드]
                                     FROM [dbo].[UFN_HC_국가검사구성](p.[수검자ID], @d) n
                                    WHERE n.[국가검사구분] = 'CONDITIONAL'
                                    ORDER BY n.[국가검사규칙코드]
                                      FOR XML PATH('')), 1, 1, ''), '-')
  FROM [dbo].[수검자] p
 CROSS APPLY [dbo].[UFN_HC_검진대상확인](p.[수검자ID], @d) t
 WHERE p.[차트번호] LIKE 'D00[12]%'
 ORDER BY p.[차트번호];
SQL

sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I \
       -i "$SQL_FILE" -o "$LOG"
rc=$?
cat "$LOG"
exit $rc
