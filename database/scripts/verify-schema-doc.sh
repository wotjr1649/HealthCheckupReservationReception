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
EOF
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -W -h-1 \
       -i artifacts/_sd.sql -o artifacts/_sd.log || { echo "덤프 실패"; exit 1; }
iconv -f UTF-16 -t UTF-8 artifacts/_sd.log | grep -E '^[CO]\|' > artifacts/reports/schema-actual.txt
rm -f artifacts/_sd.sql artifacts/_sd.log
node tools/verify-schema-doc.js
