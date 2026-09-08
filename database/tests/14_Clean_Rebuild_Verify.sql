SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- Clean Rebuild 검증 (스펙 §40). 이 파일은 **한 회차의 인벤토리 지문과 정렬 덤프**를 낸다.
-- 회차 사이 비교(RBD-002·003·005·007·008)는 SQL 로 할 수 없으므로
-- scripts/clean-rebuild-verify.sh 가 이 파일을 여러 번 돌려 diff 한다.
--
-- [!] 개수 문자열 지문만으로는 부족하다. 객체명·컬럼·정의·Seed 값이 달라도 개수만 같으면
--     동일 지문이 되어 잘못된 배포가 두 번 반복돼도 PASS 한다. 아래 (4) 덤프가 그것을 잡는다.
--
-- [!] 덤프에서 sysname 컬럼을 문자열 연결하지 않는다. catalog collation 과 DB collation 이
--     '+' 에서 충돌해 Msg 451 이 난다(실측 확인 · plans/08 RBD-009 가 같은 함정을 기록했다).
--     COLLATE 는 06 §9.2 허용목록 밖이므로 쓸 수 없다. 컬럼을 그대로 나열하고
--     sqlcmd 의 -s"|" 가 구분자를 붙이게 한다.

----------------------------------------------------------------------------
-- (1) RBD-004  배포 직후 객체 인벤토리
----------------------------------------------------------------------------
DECLARE @FP VARCHAR(200) = 'INVENTORY|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0))                    + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.objects WHERE type = 'IF' AND name LIKE 'UFN[_]HC[_]%')) + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.procedures WHERE name LIKE 'USP[_]HC[_]%'))         + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.sequences))                                          + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.key_constraints WHERE type = 'PK'))                  + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.foreign_keys))                                       + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.key_constraints WHERE type = 'UQ'))                  + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
                             WHERE t.is_ms_shipped = 0 AND i.is_unique = 1 AND i.has_filter = 1))         + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
                             WHERE t.is_ms_shipped = 0 AND i.type = 2
                               AND i.is_primary_key = 0 AND i.is_unique_constraint = 0 AND i.is_unique = 0)) + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.check_constraints))                                  + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.default_constraints))                                + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.triggers WHERE is_ms_shipped = 0))                   + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.table_types))                                        + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM [dbo].[검사코드]))                                        + '|'
     + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM [dbo].[휴무일]));

-- Table 6 | TVF 4 | SP 16 | 순번값 1 | PK 6 | FK 2 | UQ 2 | UX 1 | NCI 5 | CK 24 | DF 8 | Trg 0 | TVP 0 | Exam 19 | Hol 2
IF @FP = 'INVENTORY|6|4|20|1|6|2|2|1|5|26|10|0|0|19|41'
    PRINT 'PASS RBD-004 인벤토리 지문 일치  ' + @FP;
ELSE BEGIN PRINT 'FAIL RBD-004 인벤토리 지문 불일치  ' + @FP; SET @Fail += 1; END

----------------------------------------------------------------------------
-- (2) RBD-006  Seed 의 **구조**를 본다.
--     [!] 19행의 이름·코드 값 자체는 여기에 사본하지 않는다. deploy/02_Seed.sql 이 출처이고
--         SED-001~011 이 그것을 판정한다. 사본을 두면 그 사본이 곧 드리프트의 발생원이다.
--         회차 사이의 값 동일성은 (4) 덤프를 clean-rebuild-verify.sh 가 diff 해서 본다.
--     역할 분포는 06 §13 과 §17.2a 가 고정한 구조적 사실이다.
----------------------------------------------------------------------------
DECLARE @국가검사 INT = (SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [국가검사규칙코드] IS NOT NULL);
DECLARE @추가검사 INT = (SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [추가검사코드] IS NOT NULL);
DECLARE @Both INT = (SELECT COUNT(*) FROM [dbo].[검사코드]
                      WHERE [국가검사규칙코드] IS NOT NULL AND [추가검사코드] IS NOT NULL);
DECLARE @ActiveAex INT = (SELECT COUNT(*) FROM [dbo].[검사코드] WHERE [추가검사사용여부] = 1);

-- NEX 역할 13 + AEX 역할 7 - 겸용 1 = 19행. 겸용이 EX012 하나뿐인 것이 412 의 유일한 발생조건이다.
IF (@국가검사 = 13 AND @추가검사 = 7 AND @Both = 1 AND @ActiveAex = 7)
    PRINT 'PASS RBD-006 Seed 역할 분포 NEX 13 / AEX 7 / 겸용 1 / AEX Active 7';
ELSE BEGIN PRINT 'FAIL RBD-006 Seed 역할 분포 불일치'
                 + ' NEX=' + CONVERT(VARCHAR(5), @국가검사)
                 + ' AEX=' + CONVERT(VARCHAR(5), @추가검사)
                 + ' 겸용=' + CONVERT(VARCHAR(5), @Both)
                 + ' Active=' + CONVERT(VARCHAR(5), @ActiveAex); SET @Fail += 1; END

----------------------------------------------------------------------------
-- (3) RBD-010  로그·보고서 secret 0건은 셸이 판정한다 (SEC-010 과 같은 로직).
--     DB 안에서 확인할 수 있는 것은 "연결 secret 을 담을 수 있는 객체가 없다" 뿐이다.
----------------------------------------------------------------------------
IF ((SELECT COUNT(*) FROM sys.credentials) = 0
    AND (SELECT COUNT(*) FROM sys.symmetric_keys WHERE name NOT LIKE '##%') = 0
    AND (SELECT COUNT(*) FROM sys.server_principals WHERE type = 'S' AND name LIKE 'HC[_]%') = 0)
    PRINT 'PASS RBD-010 DB 에 credential·대칭키·HC Login 0건 (파일 스캔은 verify-no-secret.sh)';
ELSE BEGIN PRINT 'FAIL RBD-010 secret 을 담을 수 있는 객체가 있다'; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'Clean Rebuild 검증 실패', 1;

----------------------------------------------------------------------------
-- (4) 정렬된 메타데이터·Seed 덤프 — 회차 간 diff 용. 개수로는 못 잡는 차이를 잡는다.
--     sqlcmd -h-1 -W -s"|" 로 실행한다. 문자열 연결을 쓰지 않는 이유는 파일 머리 참조.
----------------------------------------------------------------------------
PRINT '--- OBJECTS ---';
SELECT 'OBJ', o.type, o.name
  FROM sys.objects o
 WHERE o.is_ms_shipped = 0 AND o.type IN ('U','P','IF','SO','PK','UQ','C','D','F')
 ORDER BY o.type, o.name;

PRINT '--- COLUMNS ---';
SELECT 'COL', t.name, c.column_id, c.name, y.name, c.max_length, c.is_nullable
  FROM sys.tables t
  JOIN sys.columns c ON c.object_id = t.object_id
  JOIN sys.types  y ON y.user_type_id = c.user_type_id
 WHERE t.is_ms_shipped = 0
 ORDER BY t.name, c.column_id;

-- [!] Key 컬럼을 하나만 찍으면 (성명, 생년월일) 이 (성명, 성별) 로 바뀌어도 지문이 같다.
--     index_columns 를 행 단위로 펼쳐 key_ordinal 과 INCLUDE 여부까지 그대로 덤프한다.
PRINT '--- INDEXES ---';
SELECT 'IDX', t.name, i.name, i.is_unique, i.has_filter, k.is_included_column, k.key_ordinal, c2.name
  FROM sys.indexes i
  JOIN sys.tables t ON t.object_id = i.object_id AND t.is_ms_shipped = 0
  JOIN sys.index_columns k ON k.object_id = i.object_id AND k.index_id = i.index_id
  JOIN sys.columns c2 ON c2.object_id = k.object_id AND c2.column_id = k.column_id
 WHERE i.name IS NOT NULL
 ORDER BY t.name, i.name, k.is_included_column, k.key_ordinal, c2.name;

PRINT '--- PARAMETERS ---';
SELECT 'PRM', p.name, pa.parameter_id, pa.name, y.name, pa.max_length
  FROM sys.procedures p
  JOIN sys.parameters pa ON pa.object_id = p.object_id
  JOIN sys.types y ON y.user_type_id = pa.user_type_id
 WHERE p.name LIKE 'USP[_]HC[_]%'
 ORDER BY p.name, pa.parameter_id;

-- GRANT 는 현재 0건이다. Security 구현이 범위 밖이므로(06 §32) 이 구획은 비어 있어야 하고,
-- 비어 있지 않으면 의도하지 않은 권한 부여이므로 diff 가 잡는다.
PRINT '--- GRANTS ---';
SELECT 'GRT', pr.name, dp.permission_name, OBJECT_NAME(dp.major_id)
  FROM sys.database_permissions dp
  JOIN sys.database_principals pr ON pr.principal_id = dp.grantee_principal_id
 WHERE dp.state = 'G' AND dp.major_id > 0
 ORDER BY pr.name, OBJECT_NAME(dp.major_id);

PRINT '--- SEED ---';
SELECT 'SEED', 'EXAM', [검사항목코드], [검사항목명], ISNULL([국가검사규칙코드], '-')
     , ISNULL([추가검사코드], '-'), ISNULL([추가검사성별코드], '-'), [추가검사사용여부]
  FROM [dbo].[검사코드] ORDER BY [검사항목코드];
SELECT 'SEED', 'HOL', CONVERT(VARCHAR(10), [휴무일자], 23), [휴무일명], [사용여부]
  FROM [dbo].[휴무일] ORDER BY [휴무일자];

PRINT '=== 14_Clean_Rebuild_Verify 완료 ===';
GO
