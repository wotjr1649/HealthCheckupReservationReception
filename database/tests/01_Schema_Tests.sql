SET NOCOUNT ON;
DECLARE @Fail INT = 0;

DECLARE @Expected TABLE (Name SYSNAME PRIMARY KEY);
INSERT INTO @Expected (Name) VALUES
 ('수검자'),('예약접수'),('검사항목'),
 ('검사코드'),('휴무일'),
 ('완료이력'),('변경이력');

-- SCH-001 테이블 수
IF ((SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) = 7)
    PRINT 'PASS SCH-001 사용자 테이블 7개';
ELSE BEGIN PRINT 'FAIL SCH-001 사용자 테이블 수 불일치'; SET @Fail += 1; END

-- SCH-002 테이블 이름 집합 정확 일치
IF NOT EXISTS (SELECT Name FROM @Expected EXCEPT SELECT name FROM sys.tables WHERE is_ms_shipped = 0)
   AND NOT EXISTS (SELECT name FROM sys.tables WHERE is_ms_shipped = 0 EXCEPT SELECT Name FROM @Expected)
    PRINT 'PASS SCH-002 테이블 이름 집합 일치';
ELSE BEGIN PRINT 'FAIL SCH-002 테이블 이름 집합 불일치'; SET @Fail += 1; END

-- SCH-003 수검자 컬럼 17개
IF ((SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[수검자]')) = 17)
    PRINT 'PASS SCH-003 수검자 17컬럼';
ELSE BEGIN PRINT 'FAIL SCH-003 수검자 컬럼 수 불일치'; SET @Fail += 1; END

-- SCH-004 PK 7
IF ((SELECT COUNT(*) FROM sys.key_constraints WHERE type = 'PK') = 7)
    PRINT 'PASS SCH-004 PK 7개';
ELSE BEGIN PRINT 'FAIL SCH-004 PK 수 불일치'; SET @Fail += 1; END

-- SCH-005 FK 4 + 전부 NO ACTION
IF ((SELECT COUNT(*) FROM sys.foreign_keys) = 4
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE delete_referential_action <> 0 OR update_referential_action <> 0))
    PRINT 'PASS SCH-005 FK 4개 / 전부 NO ACTION';
ELSE BEGIN PRINT 'FAIL SCH-005 FK 수 또는 Cascade 설정 불일치'; SET @Fail += 1; END

-- SCH-006 일반 UQ 2
IF ((SELECT COUNT(*) FROM sys.key_constraints WHERE type = 'UQ') = 2)
    PRINT 'PASS SCH-006 Unique Constraint 2개';
ELSE BEGIN PRINT 'FAIL SCH-006 Unique Constraint 수 불일치'; SET @Fail += 1; END

-- SCH-007 Filtered Unique Index 1   (사용자 테이블 한정 — is_ms_shipped 필터 필수)
IF ((SELECT COUNT(*) FROM sys.indexes i
      JOIN sys.tables t ON t.object_id = i.object_id AND t.is_ms_shipped = 0
     WHERE i.is_unique = 1 AND i.has_filter = 1) = 1)
    PRINT 'PASS SCH-007 Filtered Unique Index 1개';
ELSE BEGIN PRINT 'FAIL SCH-007 Filtered Unique Index 수 불일치'; SET @Fail += 1; END

-- SCH-008 업무/조회 NCI 5 (PK/UQ/UX 제외)
IF ((SELECT COUNT(*) FROM sys.indexes i
      JOIN sys.tables t ON t.object_id = i.object_id AND t.is_ms_shipped = 0
     WHERE i.type = 2 AND i.is_primary_key = 0 AND i.is_unique_constraint = 0 AND i.is_unique = 0) = 5)
    PRINT 'PASS SCH-008 업무/조회 Nonclustered Index 5개';
ELSE BEGIN PRINT 'FAIL SCH-008 Nonclustered Index 수 불일치'; SET @Fail += 1; END

-- SCH-009 Sequence 1 / MAXVALUE 999999
IF ((SELECT COUNT(*) FROM sys.sequences) = 1
    AND (SELECT CONVERT(BIGINT, maximum_value) FROM sys.sequences WHERE name = 'SEQ_HC_CHART_NO') = 999999)
    PRINT 'PASS SCH-009 Sequence 1개 / MAXVALUE 999999';
ELSE BEGIN PRINT 'FAIL SCH-009 Sequence 설정 불일치'; SET @Fail += 1; END

-- SCH-010 Trigger 0
IF ((SELECT COUNT(*) FROM sys.triggers WHERE is_ms_shipped = 0) = 0)
    PRINT 'PASS SCH-010 Trigger 0개';
ELSE BEGIN PRINT 'FAIL SCH-010 Trigger 가 존재합니다'; SET @Fail += 1; END

-- SCH-011 사용자 정의 Table Type 0
IF ((SELECT COUNT(*) FROM sys.table_types) = 0)
    PRINT 'PASS SCH-011 사용자 정의 Table Type 0개';
ELSE BEGIN PRINT 'FAIL SCH-011 Table Type 이 존재합니다'; SET @Fail += 1; END

-- SCH-012 DELETE SP 0
IF ((SELECT COUNT(*) FROM sys.procedures WHERE name LIKE 'USP[_]HC[_]DELETE[_]%') = 0)
    PRINT 'PASS SCH-012 DELETE SP 0개';
ELSE BEGIN PRINT 'FAIL SCH-012 DELETE SP 가 존재합니다'; SET @Fail += 1; END

-- SCH-013 Inline TVF 4  (T14 이후 통과. 그전까지는 FAIL 이 정상)
IF ((SELECT COUNT(*) FROM sys.objects WHERE type = 'IF' AND name LIKE 'UFN[_]HC[_]%') = 4)
    PRINT 'PASS SCH-013 Inline TVF 4개';
ELSE BEGIN PRINT 'FAIL SCH-013 Inline TVF 수 불일치 (T14 이전이면 정상)'; SET @Fail += 1; END

-- SCH-014 SP 15  (T30 이후 통과)
IF ((SELECT COUNT(*) FROM sys.procedures WHERE name LIKE 'USP[_]HC[_]%') = 15)
    PRINT 'PASS SCH-014 Stored Procedure 15개';
ELSE BEGIN PRINT 'FAIL SCH-014 SP 수 불일치 (T30 이전이면 정상)'; SET @Fail += 1; END

-- SCH-015 컬럼 47개 전건 EXCEPT 양방향  (04 §8)
DECLARE @ExpCol TABLE (T SYSNAME, C SYSNAME, Ty SYSNAME, Len INT, Nul BIT, PRIMARY KEY (T, C));
INSERT INTO @ExpCol (T, C, Ty, Len, Nul) VALUES
 -- 47행 전건. 기준선 04 §8 의 컬럼 표에서 기계 생성했다(개수·타입·길이·NULL 모두 그 표가 출처다).
 -- Len 은 문자·이진형만 채운다. nvarchar/nchar 는 문자 수, MAX 는 -1, 그 밖은 NULL.
 -- 수검자 17행
 (N'수검자', N'PatientId',            N'bigint',    NULL,  0),
 (N'수검자', N'ChartNo',              N'nvarchar',  100,   0),
 (N'수검자', N'Name',                 N'nvarchar',  100,   0),
 (N'수검자', N'SocialNumber',         N'varchar',   13,    0),
 (N'수검자', N'Birthday',             N'varchar',   8,     0),
 (N'수검자', N'Gender',               N'char',      1,     0),
 (N'수검자', N'EMail',                N'varchar',   200,   1),
 (N'수검자', N'CelNumberS',           N'varchar',   13,    1),
 (N'수검자', N'CelNumber',            N'varchar',   13,    1),
 (N'수검자', N'TelNumber',            N'varchar',   13,    1),
 (N'수검자', N'Zipcode',              N'varchar',   10,    1),
 (N'수검자', N'Address',              N'nvarchar',  200,   1),
 (N'수검자', N'AddressDetail',        N'nvarchar',  200,   1),
 (N'수검자', N'Memo',                 N'nvarchar',  -1,    1),
 (N'수검자', N'HepatitisBExcluded',   N'bit',       NULL,  0),
 (N'수검자', N'CreationDate',         N'datetime',  NULL,  0),
 (N'수검자', N'LastEditDate',         N'datetime',  NULL,  0),
 -- 예약접수 8행
 (N'예약접수', N'WorkId',               N'bigint',    NULL,  0),
 (N'예약접수', N'PatientId',            N'bigint',    NULL,  0),
 (N'예약접수', N'ReservationDate',      N'date',      NULL,  0),
 (N'예약접수', N'TimeSlotCode',         N'char',      2,     0),
 (N'예약접수', N'StatusCode',           N'char',      3,     0),
 (N'예약접수', N'CreationDate',         N'datetime2', NULL,  0),
 (N'예약접수', N'LastEditDate',         N'datetime2', NULL,  0),
 (N'예약접수', N'RowVersion',           N'timestamp', NULL,  0),
 -- 검사항목 3행
 (N'검사항목', N'WorkId',               N'bigint',    NULL,  0),
 (N'검사항목', N'ExamItemCode',         N'varchar',   10,    0),
 (N'검사항목', N'ExamSourceCode',       N'char',      3,     0),
 -- 검사코드 6행
 (N'검사코드', N'ExamItemCode',         N'varchar',   10,    0),
 (N'검사코드', N'ExamItemName',         N'nvarchar',  100,   0),
 (N'검사코드', N'NexRuleCode',          N'varchar',   10,    1),
 (N'검사코드', N'AdditionalExamCode',   N'varchar',   10,    1),
 (N'검사코드', N'AdditionalGenderCode', N'char',      1,     1),
 (N'검사코드', N'AdditionalActive',     N'bit',       NULL,  0),
 -- 휴무일 4행
 (N'휴무일', N'휴무일자',              N'date',      NULL,  0),
 (N'휴무일', N'휴무일명',              N'nvarchar',  100,   0),
 (N'휴무일', N'사용여부',              N'bit',       NULL,  0),
 (N'휴무일', N'비고',                  N'nvarchar',  500,   1),
 -- 완료이력 2행
 (N'완료이력', N'수검자ID',             N'bigint',    NULL,  0),
 (N'완료이력', N'완료일자',             N'date',      NULL,  0),
 -- 변경이력 7행
 (N'변경이력', N'이력ID',               N'bigint',    NULL,  0),
 (N'변경이력', N'기록일시',             N'datetime2', NULL,  0),
 (N'변경이력', N'조작자명',             N'nvarchar',  50,    1),
 (N'변경이력', N'업무코드',             N'varchar',   20,    0),
 (N'변경이력', N'대상테이블',           N'nvarchar',  10,    0),
 (N'변경이력', N'대상키',               N'bigint',    NULL,  1),
 (N'변경이력', N'결과코드',             N'int',       NULL,  0);

DECLARE @ActCol TABLE (T SYSNAME, C SYSNAME, Ty SYSNAME, Len INT, Nul BIT, PRIMARY KEY (T, C));
INSERT INTO @ActCol (T, C, Ty, Len, Nul)
SELECT t.name, c.name, y.name
     , CASE WHEN y.name IN ('nvarchar','nchar') AND c.max_length > 0 THEN c.max_length / 2
            WHEN y.name IN ('varchar','char','binary','varbinary') THEN c.max_length
            WHEN c.max_length = -1 THEN -1 ELSE NULL END
     , c.is_nullable
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types  y ON y.user_type_id = c.user_type_id
WHERE t.is_ms_shipped = 0;

IF NOT EXISTS (SELECT T,C,Ty,Len,Nul FROM @ExpCol EXCEPT SELECT T,C,Ty,Len,Nul FROM @ActCol)
   AND NOT EXISTS (SELECT T,C,Ty,Len,Nul FROM @ActCol EXCEPT SELECT T,C,Ty,Len,Nul FROM @ExpCol)
    PRINT 'PASS SCH-015 컬럼 47개 전건 일치';
ELSE
BEGIN
    PRINT 'FAIL SCH-015 컬럼 불일치';
    SELECT '기대에만 있음' AS Side, * FROM (SELECT T,C,Ty,Len,Nul FROM @ExpCol EXCEPT SELECT T,C,Ty,Len,Nul FROM @ActCol) a;
    SELECT '실측에만 있음' AS Side, * FROM (SELECT T,C,Ty,Len,Nul FROM @ActCol EXCEPT SELECT T,C,Ty,Len,Nul FROM @ExpCol) b;
    SET @Fail += 1;
END

-- SCH-016 CHECK 제약 이름 23개 EXCEPT 양방향  (04 §8.1.3 8 + §8.2.3 3 + §8.3.3 1 + §8.4.3 7 + §8.5.2 1 + §8.7.3 3)
DECLARE @ExpCk TABLE (N SYSNAME PRIMARY KEY);
INSERT INTO @ExpCk (N) VALUES
 (N'CK_수검자_CHART_NO_NOT_BLANK'), (N'CK_수검자_NAME_NOT_BLANK'),
 (N'CK_수검자_SOCIAL_FORMAT'),      (N'CK_수검자_BIRTHDAY'),
 (N'CK_수검자_GENDER'),             (N'CK_수검자_CEL_NORMALIZED'),
 (N'CK_수검자_CEL_DIGIT'),          (N'CK_수검자_EDIT_DATE'),
 (N'CK_예약접수_TIME_SLOT'),        (N'CK_예약접수_STATUS'),
 (N'CK_예약접수_EDIT_DATE'),        (N'CK_검사항목_SOURCE'),
 (N'CK_검사코드_CODE_NOT_BLANK'),   (N'CK_검사코드_NAME_NOT_BLANK'),
 (N'CK_검사코드_ROLE_REQUIRED'),    (N'CK_검사코드_NEX_RULE'),
 (N'CK_검사코드_AEX_CODE'),         (N'CK_검사코드_AEX_GENDER'),
 (N'CK_검사코드_AEX_GROUP'),        (N'CK_휴무일_NAME_NOT_BLANK'),
 (N'CK_변경이력_OPERATION'),        (N'CK_변경이력_RESULT_CODE'),
 (N'CK_변경이력_TARGET_KEY');

IF NOT EXISTS (SELECT N FROM @ExpCk EXCEPT SELECT name FROM sys.check_constraints)
   AND NOT EXISTS (SELECT name FROM sys.check_constraints EXCEPT SELECT N FROM @ExpCk)
    PRINT 'PASS SCH-016 CHECK 제약 이름 전건 일치';
ELSE BEGIN PRINT 'FAIL SCH-016 CHECK 제약 집합 불일치'; SET @Fail += 1; END

-- SCH-017 Default 제약 이름 8개 EXCEPT 양방향  (04 §8.1.4 3 + §8.2.3 2 + §8.4.3 1 + §8.5.2 1 + §8.7.3 1)
-- [X] 초안의 14개 이름은 기준선 04 §8 의 것이 아니었다(`DF_수검자_JOB` 은 존재하지 않는 Job 컬럼을
--     가리켰고 `..._GENDER`·`..._MEMO`·`..._ADDRESS` 등은 04 §8.1.4 에 없다). T06 의 DDL 이 기준선과 일치하므로
--     틀린 쪽은 이 기대값이다. 04 §8.1.4 / §8.2.3 / §8.4.3 / §8.5.2 의 이름을 그대로 옮겨 적는다.
DECLARE @ExpDf TABLE (N SYSNAME);
INSERT @ExpDf (N) VALUES
 (N'DF_수검자_HEPATITIS_B_EXCLUDED'), (N'DF_수검자_CREATION_DATE'),
 (N'DF_수검자_LAST_EDIT_DATE'),       (N'DF_예약접수_CREATION_DATE'),
 (N'DF_예약접수_LAST_EDIT_DATE'),     (N'DF_검사코드_AEX_ACTIVE'),
 (N'DF_휴무일_ACTIVE'),               (N'DF_변경이력_CREATION_DATE');

IF NOT EXISTS (SELECT N FROM @ExpDf EXCEPT SELECT name FROM sys.default_constraints)
   AND NOT EXISTS (SELECT name FROM sys.default_constraints EXCEPT SELECT N FROM @ExpDf)
    PRINT 'PASS SCH-017 Default 제약 이름 전건 일치';
ELSE BEGIN PRINT 'FAIL SCH-017 Default 제약 집합 불일치'; SET @Fail += 1; END

-- SCH-018 Nonclustered Index 5개 이름 + Key 컬럼 순서 EXCEPT 양방향  (04 §8.1.5 / §8.2.4)
--   COUNT 로는 (Name, Birthday) 가 (Name, Gender) 로 바뀐 것을 못 잡는다. 행 단위로 펼쳐 대조한다.
-- [X] 초안의 기대값은 존재하지 않는 Index 를 가리켰다(`IX_수검자_CHART_NO` 는 UQ 이지 NCI 가 아니고,
--     `IX_예약접수_DATE_SLOT_STATUS`·`..._PATIENT_STATUS`·`IX_검사항목_WORK` 는
--     04 §8 에 없는 이름이다). 04 §8.1.5 / §8.2.4 의 NCI 5개 · Key 10행이 실제 계약이다.
--     `검사항목` 는 04 §8.3.3 이 "별도 Nonclustered Index 를 만들지 않는다"고 못박았다.
DECLARE @ExpIx TABLE (IxName SYSNAME, Ord TINYINT, ColName SYSNAME, PRIMARY KEY (IxName, Ord));
INSERT @ExpIx (IxName, Ord, ColName) VALUES
 (N'IX_수검자_NAME_BIRTHDAY',              1, N'Name'),
 (N'IX_수검자_NAME_BIRTHDAY',              2, N'Birthday'),
 (N'IX_수검자_BIRTHDAY',                   1, N'Birthday'),
 (N'IX_수검자_CEL_NUMBER_S',               1, N'CelNumberS'),
 (N'IX_예약접수_SLOT',                  1, N'ReservationDate'),
 (N'IX_예약접수_SLOT',                  2, N'TimeSlotCode'),
 (N'IX_예약접수_SLOT',                  3, N'StatusCode'),
 (N'IX_예약접수_PATIENT_STATE_DATE',    1, N'PatientId'),
 (N'IX_예약접수_PATIENT_STATE_DATE',    2, N'StatusCode'),
 (N'IX_예약접수_PATIENT_STATE_DATE',    3, N'ReservationDate');

-- [X] 초안은 `;WITH Act AS (…) IF NOT EXISTS …` 였다. CTE 뒤에는 SELECT/INSERT/UPDATE/DELETE/MERGE 만
--     올 수 있어 `IF` 는 구문오류이고, 설령 통과해도 CTE 는 IF 본문까지 유효범위가 미치지 않는다.
--     실측값을 테이블 변수로 먼저 확정한 뒤 대조한다.
DECLARE @ActIx TABLE (IxName SYSNAME, Ord TINYINT, ColName SYSNAME, PRIMARY KEY (IxName, Ord));
INSERT @ActIx (IxName, Ord, ColName)
SELECT i.name, k.key_ordinal, c.name
FROM sys.indexes i
JOIN sys.tables  t ON t.object_id = i.object_id AND t.is_ms_shipped = 0
JOIN sys.index_columns k ON k.object_id = i.object_id AND k.index_id = i.index_id
JOIN sys.columns c ON c.object_id = k.object_id AND c.column_id = k.column_id
WHERE i.type_desc = 'NONCLUSTERED' AND i.is_unique = 0 AND k.is_included_column = 0;

IF NOT EXISTS (SELECT IxName, Ord, ColName FROM @ExpIx EXCEPT SELECT IxName, Ord, ColName FROM @ActIx)
   AND NOT EXISTS (SELECT IxName, Ord, ColName FROM @ActIx EXCEPT SELECT IxName, Ord, ColName FROM @ExpIx)
    PRINT 'PASS SCH-018 NCI 이름 + Key 컬럼 순서 전건 일치';
ELSE
BEGIN
    PRINT 'FAIL SCH-018 NCI Key 불일치';
    SELECT '기대에만 있음' AS Side, * FROM (SELECT IxName, Ord, ColName FROM @ExpIx EXCEPT SELECT IxName, Ord, ColName FROM @ActIx) a;
    SELECT '실측에만 있음' AS Side, * FROM (SELECT IxName, Ord, ColName FROM @ActIx EXCEPT SELECT IxName, Ord, ColName FROM @ExpIx) b;
    SET @Fail += 1;
END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 01_Schema_Tests 완료 ===';
GO
