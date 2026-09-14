#!/usr/bin/env bash
# 마감 실측이 쓸 **오늘자 점검용 예약**을 세운다 (AM 한 건 · PM 한 건).
#
# 왜: `scripts/measure-cutoff.sh` 의 접수 경계 둘은 `차트번호 D%` 인 수검자의 **오늘 RSV**
#     를 대상으로 삼는다. 그 행이 없으면 「대상 예약 없음 · 못 잼」으로 접수 AM·PM 을
#     한 번도 밟지 못한다 — 실제로 그렇게 나왔다 (2026-09-15).
#
# `[!]` **예약 SP 로 만든다.** `2026-09-11-Deadline-Field-Check.md` §5 는 직접 INSERT 였는데,
#       그것은 운영시간 밖에서 심어야 했기 때문이다. 지금은 창이 열려 있어 SP 가 통과하고,
#       SP 로 만들면 `검사구성`(NEX·AEX)이 계약대로 함께 선다 — 직접 INSERT 한 행은
#       그 자식이 없어 접수완료가 `701`/NEX 0행으로 막히고, 그러면 마감을 재지 못한다.
#
# `[!]` **멱등이다.** 오늘 그 시간대에 D% 의 RSV 가 이미 있으면 만들지 않는다.
#       대상은 유효업무가 없는 D% 수검자 중에서 고른다 — 00 RP-06 이 1인 1건이므로
#       이미 예약이 있는 사람으로는 만들 수 없다.
#
# `[!]` **마감시각을 건드리지 않는다.** 옮기는 것은 `measure-cutoff.sh` 의 일이고, 이
#       스크립트는 지금 값 그대로 예약이 되는 시각에만 성공한다. 마감이 이미 지났으면
#       SP 가 `304` 로 막고 그것을 그대로 찍는다 — 조용히 통과로 적지 않는다.
set -u
cd "$(dirname "$0")/.."
mkdir -p artifacts/logs

SRV='.\SQLEXPRESS'
DB='HealthCheckupReservationReceptionDb'
SQL_FILE=artifacts/logs/seed-probe-works.sql
trap 'rm -f "$SQL_FILE"' EXIT

printf '\xEF\xBB\xBF' > "$SQL_FILE"
cat >> "$SQL_FILE" <<'SQL'
SET NOCOUNT ON;

-- [!] INSERT ... EXEC 를 쓰지 않는다. 그것이 암시적 트랜잭션을 열고, Write SP 진입 가드가
--     @@TRANCOUNT > 0 을 50003 으로 막는다 (06 §33.1a 가 금지한 형태다).
--     그래서 RS0 은 잡지 않고 클라이언트로 그대로 흘려보낸 뒤, 마지막에 상태를 다시 센다.

DECLARE @오늘 DATE = CAST(SYSDATETIME() AS DATE);
DECLARE @AM BIGINT, @PM BIGINT;

-- 유효업무가 없는 D% 수검자 둘. 00 RP-06 이 1인 1건이라 이미 예약이 있으면 못 쓴다.
;WITH [쓸수있는] AS (
    SELECT p.[수검자ID], p.[차트번호],
           ROW_NUMBER() OVER (ORDER BY p.[차트번호]) AS [순번]
      FROM [dbo].[수검자] p
     WHERE p.[차트번호] LIKE 'D%'
       AND NOT EXISTS (SELECT 1 FROM [dbo].[예약접수] w
                        WHERE w.[수검자ID] = p.[수검자ID]
                          AND w.[상태코드] IN ('RSV','RCP')
                          AND w.[예약일] >= @오늘)
)
SELECT @AM = MAX(CASE WHEN [순번] = 1 THEN [수검자ID] END),
       @PM = MAX(CASE WHEN [순번] = 2 THEN [수검자ID] END)
  FROM [쓸수있는];

-- 이미 오늘 그 시간대에 D% RSV 가 있으면 만들지 않는다 (멱등).
IF EXISTS (SELECT 1 FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
            WHERE w.[예약일] = @오늘 AND w.[시간대코드] = 'AM' AND w.[상태코드] = 'RSV'
              AND p.[차트번호] LIKE 'D%')
    SET @AM = NULL;
IF EXISTS (SELECT 1 FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
            WHERE w.[예약일] = @오늘 AND w.[시간대코드] = 'PM' AND w.[상태코드] = 'RSV'
              AND p.[차트번호] LIKE 'D%')
    SET @PM = NULL;

IF @AM IS NOT NULL
    EXEC [dbo].[USP_HC_예약_등록]
         @수검자ID = @AM, @예약구분 = 'NORMAL', @예약일 = @오늘, @시간대코드 = 'AM',
         @추가검사01선택여부 = 0, @추가검사02선택여부 = 0, @추가검사03선택여부 = 0,
         @추가검사04선택여부 = 0, @추가검사05선택여부 = 0, @추가검사06선택여부 = 0,
         @추가검사07선택여부 = 0, @조작자명 = N'점검';

IF @PM IS NOT NULL
    EXEC [dbo].[USP_HC_예약_등록]
         @수검자ID = @PM, @예약구분 = 'NORMAL', @예약일 = @오늘, @시간대코드 = 'PM',
         @추가검사01선택여부 = 0, @추가검사02선택여부 = 0, @추가검사03선택여부 = 0,
         @추가검사04선택여부 = 0, @추가검사05선택여부 = 0, @추가검사06선택여부 = 0,
         @추가검사07선택여부 = 0, @조작자명 = N'점검';

SELECT [시간대코드], COUNT(*) AS [오늘 D% RSV]
  FROM [dbo].[예약접수] w
  JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
 WHERE w.[예약일] = @오늘 AND w.[상태코드] = 'RSV' AND p.[차트번호] LIKE 'D%'
 GROUP BY [시간대코드];
SQL

RC=0
sqlcmd -S "$SRV" -E -d "$DB" -b -I -W -s'|' -i "$SQL_FILE" || RC=$?
echo "seed-probe-works exit=$RC"
exit $RC
