SET NOCOUNT ON;
PRINT N'--- 08_Verify 시작 ---';
GO
-- Deploy.sql 이 매 배포 끝에 부른다 (스펙 §42, VER-001~007).
-- 수치의 출처는 04 §8·§9·§10 과 06 §12.1 이다. 이 파일은 그 수치를 실측과 대조만 한다.
--
-- [!] PRINT 는 스칼라 식만 받는다. 인자에 하위 쿼리를 넣으면 Msg 1046 + Msg 102 로 배치 전체가
--     컴파일 실패하고 VER-001~007 이 한 줄도 실행되지 않은 채 exit 1 이 난다 (CLAUDE.md §11).
--     이 파일은 모든 배포의 마지막이라 그 실수 하나가 배포 검증 전체를 무력화한다.
--     하위 쿼리는 변수에 먼저 담는다. DATABASEPROPERTYEX 는 함수라 그대로 둔다.
DECLARE @Fail INT = 0;

IF ((SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) = 6)
    PRINT 'PASS VER-001 Table 6';
ELSE BEGIN PRINT 'FAIL VER-001 Table 수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM sys.objects WHERE type = 'IF' AND name LIKE 'UFN[_]HC[_]%') = 4)
    PRINT 'PASS VER-002 Inline TVF 4';
ELSE BEGIN PRINT 'FAIL VER-002 TVF 수 불일치'; SET @Fail += 1; END

-- 05 §1.3 의 외부 호출 SP 20개. SELECT 9 / INSERT 3 / UPDATE 7 / DELETE 1 (06 §18).
IF ((SELECT COUNT(*) FROM sys.procedures WHERE name LIKE 'USP[_]HC[_]%') = 20)
    PRINT 'PASS VER-003 Stored Procedure 20';
ELSE BEGIN PRINT 'FAIL VER-003 SP 수 불일치'; SET @Fail += 1; END

IF ((SELECT COUNT(*) FROM sys.sequences) = 1)
    PRINT 'PASS VER-004 Sequence 1';
ELSE BEGIN PRINT 'FAIL VER-004 Sequence 수 불일치'; SET @Fail += 1; END

-- 사용자 테이블 한정(is_ms_shipped = 0). SCH-007·RBD-004 와 같은 기준을 쓴다.
IF ((SELECT COUNT(*) FROM sys.key_constraints WHERE type = 'PK') = 6
    AND (SELECT COUNT(*) FROM sys.foreign_keys) = 2
    AND (SELECT COUNT(*) FROM sys.key_constraints WHERE type = 'UQ') = 2
    AND (SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
          WHERE t.is_ms_shipped = 0 AND i.is_unique = 1 AND i.has_filter = 1) = 1
    AND (SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
          WHERE t.is_ms_shipped = 0 AND i.type = 2
            AND i.is_primary_key = 0 AND i.is_unique_constraint = 0 AND i.is_unique = 0) = 5)
    PRINT 'PASS VER-005 PK 6 / FK 2 / UQ 2 / UX 1 / NCI 5';
ELSE BEGIN PRINT 'FAIL VER-005 Key·Index 수 불일치'; SET @Fail += 1; END

-- 04 §9.1 이 Trigger 0 · TVP 0 을 못박았다. 06 §9.2 허용목록 밖이기도 하다.
IF ((SELECT COUNT(*) FROM sys.triggers WHERE is_ms_shipped = 0) = 0
    AND (SELECT COUNT(*) FROM sys.table_types) = 0)
    PRINT 'PASS VER-006 Trigger 0 / TVP 0';
ELSE BEGIN PRINT 'FAIL VER-006 금지 객체 존재'; SET @Fail += 1; END

-- R7 값이다. 휴무일이 CK 2개(TYPE·EDIT_DATE)와 DF 2개(생성일시·최종수정일시)를 얻었다 (04 §10.1).
IF ((SELECT COUNT(*) FROM sys.check_constraints) = 26
    AND (SELECT COUNT(*) FROM sys.default_constraints) = 10
    AND (SELECT COUNT(*) FROM [dbo].[검사코드]) = 19
    AND (SELECT COUNT(*) FROM [dbo].[휴무일]) = 41)
    PRINT 'PASS VER-007 CHECK 26 / DEFAULT 10 / Seed Exam 19 / Holiday 41';
ELSE BEGIN PRINT 'FAIL VER-007 제약 또는 Seed 수 불일치'; SET @Fail += 1; END

DECLARE @Compat VARCHAR(10);
SELECT @Compat = CONVERT(VARCHAR(10), compatibility_level) FROM sys.databases WHERE name = DB_NAME();
PRINT 'INFO CompatibilityLevel = ' + @Compat;
PRINT 'INFO Collation          = ' + CONVERT(VARCHAR(80), DATABASEPROPERTYEX(DB_NAME(), 'Collation'));

-- @Fail 은 이 배치에서만 산다. GO 뒤로 넘기면 Msg 137 이다 (CLAUDE.md §11).
IF @Fail > 0 THROW 51000, N'배포 검증 실패', 1;
PRINT N'=== 08_Verify 완료 ===';
GO
