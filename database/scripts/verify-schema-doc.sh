#!/usr/bin/env bash
# 배포된 스키마를 덤프하고 기준선 04 §8 과 대조한다.
cd "$(dirname "$0")/.."
mkdir -p artifacts/reports
printf '\xef\xbb\xbf' > artifacts/_sd.sql
cat >> artifacts/_sd.sql << 'EOF'
SET NOCOUNT ON;
SELECT 'C|' + t.name + '|' + CONVERT(VARCHAR(5), c.column_id) + '|' + c.name + '|'
     + y.name + '|' + CONVERT(VARCHAR(1), c.is_nullable)
  FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id
  JOIN sys.types y ON y.user_type_id = c.user_type_id
 WHERE t.is_ms_shipped = 0
 ORDER BY t.name, c.column_id;
SELECT 'O|' + CASE k.type WHEN 'PK' THEN 'PK' ELSE 'UQ' END + '|' + k.name FROM sys.key_constraints k;
SELECT 'O|FK|' + name FROM sys.foreign_keys;
SELECT 'O|CK|' + name FROM sys.check_constraints;
SELECT 'O|DF|' + name FROM sys.default_constraints;
SELECT 'O|' + CASE WHEN i.is_unique = 1 THEN 'UX' ELSE 'IX' END + '|' + i.name
  FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
 WHERE t.is_ms_shipped = 0 AND i.type = 2 AND i.is_primary_key = 0 AND i.is_unique_constraint = 0;

-- 여기부터는 이름이 아니라 **정의**다 (DOC-006~009).
-- [!] 서버에서 문자열을 잇지 않는다. sys.* 카탈로그는 Latin1_General_CI_AS_KS_WS 이고 DB 는
--     Korean_Wansung_CI_AS 라 delete_referential_action_desc 를 + 로 이으면 Msg 451 이 난다(실측).
--     COLLATE 는 스펙 §9.2 금지이므로 컬럼을 그대로 내보내고 sqlcmd -s 가 잇게 한다.
--     집계도 SQL 에서 하지 않는다 — FOR XML PATH · STRING_AGG 는 허용목록 밖이다.
SELECT 'F', f.name, CONVERT(VARCHAR(5), fc.constraint_column_id),
       pc.name, OBJECT_NAME(f.referenced_object_id), rc.name,
       f.delete_referential_action_desc, f.update_referential_action_desc
  FROM sys.foreign_keys f
  JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = f.object_id
  JOIN sys.columns pc ON pc.object_id = fc.parent_object_id     AND pc.column_id = fc.parent_column_id
  JOIN sys.columns rc ON rc.object_id = fc.referenced_object_id AND rc.column_id = fc.referenced_column_id
 ORDER BY f.name, fc.constraint_column_id;

-- 인덱스 Key/INCLUDE 구성. 04 §8 이 `기록일시 DESC` 처럼 방향을 적으므로 is_descending_key 도 낸다.
SELECT 'K', i.name, CONVERT(VARCHAR(1), ic.is_included_column),
       CONVERT(VARCHAR(5), CASE WHEN ic.is_included_column = 1 THEN ic.index_column_id ELSE ic.key_ordinal END),
       col.name, CONVERT(VARCHAR(1), ic.is_descending_key)
  FROM sys.indexes i
  JOIN sys.tables t ON t.object_id = i.object_id
  JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
  JOIN sys.columns col ON col.object_id = ic.object_id AND col.column_id = ic.column_id
 WHERE t.is_ms_shipped = 0 AND i.type > 0
 ORDER BY i.name, ic.is_included_column, ic.key_ordinal, ic.index_column_id;

SELECT 'W', i.name, ISNULL(i.filter_definition, N'-')
  FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
 WHERE t.is_ms_shipped = 0 AND i.type > 0 AND i.has_filter = 1 ORDER BY i.name;

-- CHECK/DEFAULT 의 정의. 소속 테이블까지 낸다 — DOC-009 는 가드로 보존되는 테이블만 본다.
SELECT 'X', OBJECT_NAME(c.parent_object_id), c.name, c.definition
  FROM sys.check_constraints c ORDER BY c.name;
SELECT 'X', OBJECT_NAME(d.parent_object_id), d.name, d.definition
  FROM sys.default_constraints d ORDER BY d.name;
EOF
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -W -h-1 -s '|' \
       -i artifacts/_sd.sql -o artifacts/_sd.log || { echo "덤프 실패"; exit 1; }
iconv -f UTF-16 -t UTF-8 artifacts/_sd.log | grep -E '^[COFKWX]\|' > artifacts/reports/schema-actual.txt
rm -f artifacts/_sd.sql artifacts/_sd.log
node tools/verify-schema-doc.js
