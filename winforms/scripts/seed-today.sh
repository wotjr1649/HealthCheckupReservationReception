#!/usr/bin/env bash
# 점검용 예약접수를 **DB 오늘날짜**로 옮긴다 (docs/phase5/2026-09-11-Deadline-Field-Check.md §5).
#
# 대상은 차트번호 `D%` 로 심어 둔 점검용 행뿐이다. 하네스 fixture(`T0xx`)는 계약시험이
# 쓰므로 건드리지 않는다.
#
# [X] **날짜를 적지 않는다.** 원본이 며칠 자였는지 묻지 않고, 가장 늦은 예약일이 오늘이
#     되도록 전부를 같은 일수만큼 민다. 며칠 뒤에 다시 돌려도 같은 모양이 되고, 이미
#     오늘자면 0일을 밀어 아무것도 바뀌지 않는다 (멱등).
#
# [X] **오늘이 업무일이 아니면(일요일·휴무일) 옮기지 않는다.** 조회는 되지만 접수가 전부
#     막혀 점검이 안 된다 — 조용히 넘기지 않고 red 로 말한다.
#
# 되돌리기는 같은 문서 §5 의 DELETE 다.
set -u
cd "$(dirname "$0")/.."
mkdir -p artifacts/logs

SQL_FILE=artifacts/logs/seed-today.sql
LOG=artifacts/logs/seed-today.log
trap 'rm -f "$SQL_FILE"' EXIT

# sqlcmd 는 BOM 이 없으면 -i 파일의 한글을 코드페이지로 읽는다 (database/scripts 의 .sql 과 같다).
printf '\xEF\xBB\xBF' > "$SQL_FILE"
cat >> "$SQL_FILE" <<'SQL'
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @오늘 DATE = CAST(GETDATE() AS DATE);

-- 업무일 판정은 DB 가 이미 갖고 있다 (03_Functions.sql 의 UFN_HC_일정확인). 요일·휴무일
-- 규칙을 여기서 다시 쓰지 않는다. 쓰는 것은 [업무일여부] 하나뿐이라 시간대·마감구분은
-- 아무 유효값이나 넣는다.
IF NOT EXISTS (SELECT 1 FROM dbo.UFN_HC_일정확인(SYSDATETIME(), @오늘, 'AM', 'NORMAL')
                WHERE [업무일여부] = 1)
BEGIN
    RAISERROR(N'오늘은 업무일이 아니다 — 옮겨도 접수가 막힌다.', 16, 1);
    RETURN;
END

DECLARE @원본 DATE =
    (SELECT MAX(w.[예약일])
       FROM dbo.[예약접수] w
       JOIN dbo.[수검자] p ON p.[수검자ID] = w.[수검자ID]
      WHERE p.[차트번호] LIKE 'D%');

IF @원본 IS NULL
BEGIN
    RAISERROR(N'차트번호 D%% 점검용 데이타가 없다. 먼저 심어라.', 16, 1);
    RETURN;
END

BEGIN TRAN;

UPDATE w
   SET w.[예약일] = DATEADD(DAY, DATEDIFF(DAY, @원본, @오늘), w.[예약일])
  FROM dbo.[예약접수] w
  JOIN dbo.[수검자] p ON p.[수검자ID] = w.[수검자ID]
 WHERE p.[차트번호] LIKE 'D%';

COMMIT;

SELECT [원본] = @원본, [오늘] = @오늘, [옮긴날수] = DATEDIFF(DAY, @원본, @오늘);

SELECT w.[예약일], w.[시간대코드], w.[상태코드], [건수] = COUNT(*)
  FROM dbo.[예약접수] w
  JOIN dbo.[수검자] p ON p.[수검자ID] = w.[수검자ID]
 WHERE p.[차트번호] LIKE 'D%'
 GROUP BY w.[예약일], w.[시간대코드], w.[상태코드]
 ORDER BY 1, 2, 3;

-- 00 RP-06 은 유효업무 1건을 강제한다. 전원에게 오늘 예약을 주면 새 예약을 만들 대상이
-- 306/701 로 전부 막히므로, 비어 있는 수검자가 남아 있는지 함께 센다 (§5).
SELECT [유효업무_없는_수검자] = COUNT(*)
  FROM dbo.[수검자] p
 WHERE NOT EXISTS (SELECT 1 FROM dbo.[예약접수] w
                    WHERE w.[수검자ID] = p.[수검자ID]
                      AND w.[상태코드] IN ('RSV','RCP')
                      AND w.[예약일] >= CAST(GETDATE() AS DATE));
SQL

sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -W -s'|' \
       -i "$SQL_FILE" -o "$LOG"
RC=$?
iconv -f UTF-16 -t UTF-8 "$LOG"
exit $RC
