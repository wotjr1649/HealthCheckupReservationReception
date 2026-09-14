#!/usr/bin/env bash
# 마감시각 규칙을 **화면이 받는 값까지** 잰다 (session-19 §4-1 이 「없다」고 적은 실측).
#
# 사람이 10:00·11:00·15:00·16:00 을 지키고 앉지 않는다 (2026-09-14 사용자 지시).
# 옮기는 것은 시계가 아니라 [운영기준] 의 마감시각 넷이고, 끝나면 00 값으로 되돌린다.
#
# [!] **되돌림을 선언으로 두지 않는다.** 이 스크립트의 마지막이 복원이고, 그것이 맞는지는
#     database/scripts/verify-operating-baseline.sh OPR-G4 가 판정한다. 돌린 뒤 그것을 봐라.
#
# [!] 운영시간(09:00~18:00) 밖에서는 공통 업무조건이 309 로 먼저 막아 마감 판정에 닿지
#     못한다. 그때는 red 로 멈춘다 — 조용히 「통과」로 적지 않는다.
#
# 판정은 **상태로** 한다. 접수 SP 의 결과코드는 출력에 그대로 찍히지만, 시험이 거는 것은
# 「마감 뒤에는 RSV 로 남고 마감 앞에서는 RCP 가 된다」는 업무 사실이다.
set -u
cd "$(dirname "$0")/.."
mkdir -p artifacts/logs

SQL_FILE=artifacts/logs/measure-cutoff.sql
LOG=artifacts/logs/measure-cutoff.log
trap 'rm -f "$SQL_FILE"' EXIT

printf '\xEF\xBB\xBF' > "$SQL_FILE"
cat >> "$SQL_FILE" <<'SQL'
SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @열림 TIME(0) = '23:59:59';   -- 아직 안 지난 마감
DECLARE @닫힘 TIME(0) = '00:00:01';   -- 이미 지난 마감

-- 원본을 붙들어 둔다. 무슨 일이 나도 이 값으로 되돌린다.
DECLARE @원본예약AM TIME(0), @원본예약PM TIME(0), @원본접수AM TIME(0), @원본접수PM TIME(0);
SELECT @원본예약AM = [일반예약AM마감], @원본예약PM = [일반예약PM마감],
       @원본접수AM = [접수AM마감],     @원본접수PM = [접수PM마감]
  FROM [dbo].[운영기준] WHERE [기준ID] = 1;

DECLARE @오늘 DATE = CAST(SYSDATETIME() AS DATE);
DECLARE @결과 TABLE ([순서] INT IDENTITY(1,1), [경계] NVARCHAR(20), [상태] NVARCHAR(10),
                     [잰것] NVARCHAR(40), [값] NVARCHAR(40), [판정] NVARCHAR(10));

BEGIN TRY
    -- 운영시간 밖이면 잴 수 없다.
    IF NOT EXISTS (SELECT 1 FROM [dbo].[UFN_HC_일정확인](SYSDATETIME(), @오늘, 'AM', 'NORMAL')
                    WHERE [업무일여부] = 1)
        THROW 50001, N'오늘은 업무일이 아니다 — 마감 판정에 닿지 못한다.', 1;

    -- ── 1. 예약 마감. 읽기만 하므로 상태를 바꾸지 않는다.
    DECLARE @시간대 CHAR(2), @마감컬럼 NVARCHAR(20), @경계 NVARCHAR(20);
    DECLARE @i INT = 0;
    WHILE @i < 2
    BEGIN
        SET @시간대 = CASE WHEN @i = 0 THEN 'AM' ELSE 'PM' END;
        SET @경계 = N'예약 ' + @시간대;

        -- 열림
        UPDATE [dbo].[운영기준]
           SET [일반예약AM마감] = CASE WHEN @시간대 = 'AM' THEN @열림 ELSE @원본예약AM END,
               [일반예약PM마감] = CASE WHEN @시간대 = 'PM' THEN @열림 ELSE @원본예약PM END
         WHERE [기준ID] = 1;

        INSERT INTO @결과 ([경계],[상태],[잰것],[값],[판정])
        SELECT @경계, N'마감 전', N'마감경과여부',
               CAST(s.[마감경과여부] AS NVARCHAR(10)),
               CASE WHEN s.[마감경과여부] = 0 THEN N'PASS' ELSE N'FAIL' END
          FROM [dbo].[UFN_HC_일정확인](SYSDATETIME(), @오늘, @시간대, 'NORMAL') s;

        -- 닫힘
        UPDATE [dbo].[운영기준]
           SET [일반예약AM마감] = CASE WHEN @시간대 = 'AM' THEN @닫힘 ELSE @원본예약AM END,
               [일반예약PM마감] = CASE WHEN @시간대 = 'PM' THEN @닫힘 ELSE @원본예약PM END
         WHERE [기준ID] = 1;

        INSERT INTO @결과 ([경계],[상태],[잰것],[값],[판정])
        SELECT @경계, N'마감 후', N'마감경과여부 · 업무가능코드',
               CAST(s.[마감경과여부] AS NVARCHAR(10)) + N' · ' + CAST(s.[업무가능코드] AS NVARCHAR(10)),
               CASE WHEN s.[마감경과여부] = 1 THEN N'PASS' ELSE N'FAIL' END
          FROM [dbo].[UFN_HC_일정확인](SYSDATETIME(), @오늘, @시간대, 'NORMAL') s;

        -- 화면이 실제로 부르는 SP 로 같은 상태를 한 번 더 낸다. RS2 의 그 시간대 행이
        -- `선택가능=0 · 차단코드=304` 여야 화면이 그 자리를 닫는다 (05 §9.7).
        -- 판정은 위 함수가 이미 걸었고, 이것은 **값을 눈에 보이게 하는 자리**다.
        DECLARE @수검자 BIGINT = (SELECT TOP (1) [수검자ID] FROM [dbo].[수검자]
                                   WHERE [차트번호] LIKE 'D001%' ORDER BY [수검자ID]);
        PRINT N'--- ' + @경계 + N' 마감 후 · 예약가능정보 조회 (SP-RSV-01)';
        EXEC [dbo].[USP_HC_예약가능정보_조회]
             @수검자ID = @수검자, @업무ID = NULL, @행버전 = NULL,
             @예약구분 = 'NORMAL', @예약일 = @오늘, @시간대코드 = @시간대,
             @추가검사01선택여부 = 0, @추가검사02선택여부 = 0, @추가검사03선택여부 = 0,
             @추가검사04선택여부 = 0, @추가검사05선택여부 = 0, @추가검사06선택여부 = 0,
             @추가검사07선택여부 = 0;

        SET @i = @i + 1;
    END

    -- 예약 마감을 원래대로 돌려 놓고 접수 쪽으로 간다.
    UPDATE [dbo].[운영기준]
       SET [일반예약AM마감] = @원본예약AM, [일반예약PM마감] = @원본예약PM
     WHERE [기준ID] = 1;

    -- ── 2. 접수 마감. 실제로 접수 SP 를 부른다 — 화면이 받는 결과코드가 여기서 나온다.
    --      오늘 RSV 인 건을 시간대별로 하나씩 빌린다. 마감 후 시도는 실패라 상태가 안 변하고,
    --      마감 전 시도만 RCP 가 된다.
    SET @i = 0;
    WHILE @i < 2
    BEGIN
        SET @시간대 = CASE WHEN @i = 0 THEN 'AM' ELSE 'PM' END;
        SET @경계 = N'접수 ' + @시간대;

        DECLARE @업무ID BIGINT, @행버전 BINARY(8), @상태 CHAR(3);
        SELECT TOP (1) @업무ID = w.[업무ID], @행버전 = w.[행버전]
          FROM [dbo].[예약접수] w
          JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
         WHERE w.[예약일] = @오늘 AND w.[시간대코드] = @시간대 AND w.[상태코드] = 'RSV'
           AND p.[차트번호] LIKE 'D%'
         ORDER BY w.[업무ID];

        IF @업무ID IS NULL
        BEGIN
            INSERT INTO @결과 ([경계],[상태],[잰것],[값],[판정])
            VALUES (@경계, N'-', N'대상 예약', N'없음', N'못 잼');
        END
        ELSE
        BEGIN
            -- 마감 후
            UPDATE [dbo].[운영기준]
               SET [접수AM마감] = CASE WHEN @시간대 = 'AM' THEN @닫힘 ELSE @원본접수AM END,
                   [접수PM마감] = CASE WHEN @시간대 = 'PM' THEN @닫힘 ELSE @원본접수PM END
             WHERE [기준ID] = 1;

            PRINT N'--- ' + @경계 + N' 마감 후 접수 시도 (업무ID ' + CAST(@업무ID AS NVARCHAR(20)) + N')';
            EXEC [dbo].[USP_HC_접수_완료] @업무ID = @업무ID, @행버전 = @행버전, @조작자명 = N'마감실측';

            SELECT @상태 = [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @업무ID;
            INSERT INTO @결과 ([경계],[상태],[잰것],[값],[판정])
            VALUES (@경계, N'마감 후', N'접수 뒤 상태코드', @상태,
                    CASE WHEN @상태 = 'RSV' THEN N'PASS' ELSE N'FAIL' END);

            -- 마감 전
            UPDATE [dbo].[운영기준]
               SET [접수AM마감] = CASE WHEN @시간대 = 'AM' THEN @열림 ELSE @원본접수AM END,
                   [접수PM마감] = CASE WHEN @시간대 = 'PM' THEN @열림 ELSE @원본접수PM END
             WHERE [기준ID] = 1;

            SELECT @행버전 = [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @업무ID;
            PRINT N'--- ' + @경계 + N' 마감 전 접수 시도 (업무ID ' + CAST(@업무ID AS NVARCHAR(20)) + N')';
            EXEC [dbo].[USP_HC_접수_완료] @업무ID = @업무ID, @행버전 = @행버전, @조작자명 = N'마감실측';

            SELECT @상태 = [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @업무ID;
            INSERT INTO @결과 ([경계],[상태],[잰것],[값],[판정])
            VALUES (@경계, N'마감 전', N'접수 뒤 상태코드', @상태,
                    CASE WHEN @상태 = 'RCP' THEN N'PASS' ELSE N'FAIL' END);
        END

        SET @i = @i + 1;
    END
END TRY
BEGIN CATCH
    INSERT INTO @결과 ([경계],[상태],[잰것],[값],[판정])
    VALUES (N'(오류)', N'-', ERROR_MESSAGE(), N'-', N'FAIL');
END CATCH

-- ── 3. 복원. 무슨 일이 있었든 여기로 온다.
UPDATE [dbo].[운영기준]
   SET [일반예약AM마감] = @원본예약AM, [일반예약PM마감] = @원본예약PM,
       [접수AM마감]     = @원본접수AM, [접수PM마감]     = @원본접수PM
 WHERE [기준ID] = 1;

SELECT [순서],[경계],[상태],[잰것],[값],[판정] FROM @결과 ORDER BY [순서];

SELECT [복원확인] = N'운영기준', [일반예약AM마감],[일반예약PM마감],[접수AM마감],[접수PM마감]
  FROM [dbo].[운영기준] WHERE [기준ID] = 1;
SQL

sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -W \
       -i "$SQL_FILE" -o "$LOG" -u
rc=$?
iconv -f UTF-16LE -t UTF-8 "$LOG" 2>/dev/null || cat "$LOG"

echo
echo "복원은 이것이 판정한다 →  cd ../database && ./scripts/verify-operating-baseline.sh"
exit $rc
