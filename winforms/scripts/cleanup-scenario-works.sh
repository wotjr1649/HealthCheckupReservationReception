#!/usr/bin/env bash
# 시나리오 시험이 만든 오늘 업무를 **취소로 되돌린다** (2026-09-14).
#
# 왜: ScenarioDbTests 는 돌 때마다 새 수검자를 만들고 오늘 예약을 넣는다. 수검자는 겹치지
#     않지만 **시간대 정원 20 은 공유 자원**이라 반복 실행이 그것을 먹는다. 실제로 PM 이
#     20/20 이 되어 다음 실행이 통째로 Inconclusive 가 되었다.
#
# [X] 직접 UPDATE 하지 않는다. 취소도 업무이므로 계약대로 SP 를 부른다 —
#     USP_HC_예약_취소(RSV) · USP_HC_접수_취소(RCP). 그래야 변경기록도 남는다.
#
# 대상은 시험이 만든 수검자(성명이 `시나리오`로 시작)의 **오늘 유효업무**뿐이다.
# 점검용 D% 수검자의 업무는 건드리지 않는다 — seed-today.sh 가 세운 것이다.
set -u
cd "$(dirname "$0")/.."
mkdir -p artifacts/logs

SQL_FILE=artifacts/logs/cleanup-scenario.sql
LOG=artifacts/logs/cleanup-scenario.log
trap 'rm -f "$SQL_FILE"' EXIT

printf '\xEF\xBB\xBF' > "$SQL_FILE"
cat >> "$SQL_FILE" <<'SQL'
SET NOCOUNT ON;

DECLARE @업무ID BIGINT, @행버전 BINARY(8), @상태 CHAR(3), @지운수 INT = 0;

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT w.[업무ID], w.[행버전], w.[상태코드]
      FROM [dbo].[예약접수] w
      JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
     WHERE w.[예약일] = CAST(SYSDATETIME() AS DATE)
       AND w.[상태코드] IN ('RSV', 'RCP')
       AND p.[성명] LIKE N'시나리오%';

OPEN cur;
FETCH NEXT FROM cur INTO @업무ID, @행버전, @상태;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF @상태 = 'RSV'
        EXEC [dbo].[USP_HC_예약_취소] @업무ID = @업무ID, @행버전 = @행버전, @조작자명 = N'정리';
    ELSE
        EXEC [dbo].[USP_HC_접수_취소] @업무ID = @업무ID, @행버전 = @행버전, @조작자명 = N'정리';

    SET @지운수 = @지운수 + 1;
    FETCH NEXT FROM cur INTO @업무ID, @행버전, @상태;
END
CLOSE cur; DEALLOCATE cur;

PRINT N'되돌린 업무: ' + CAST(@지운수 AS NVARCHAR(10));
GO

SET NOCOUNT ON;
SELECT [시간대] = [시간대코드], [유효건수] = COUNT(*)
  FROM [dbo].[예약접수]
 WHERE [예약일] = CAST(SYSDATETIME() AS DATE) AND [상태코드] IN ('RSV', 'RCP')
 GROUP BY [시간대코드];
SQL

sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -W \
       -i "$SQL_FILE" -o "$LOG" -u
rc=$?
iconv -f UTF-16LE -t UTF-8 "$LOG" 2>/dev/null | grep -v "^$" | tail -20
exit $rc
