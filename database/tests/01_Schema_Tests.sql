SET NOCOUNT ON;
DECLARE @Fail INT = 0;

DECLARE @Expected TABLE (Name SYSNAME PRIMARY KEY);
INSERT INTO @Expected (Name) VALUES
 ('INFO_PATIENTS'),('INFO_CHECKUP_WORKS'),('INFO_CHECKUP_WORK_EXAMS'),
 ('MST_EXAM_ITEMS'),('MST_HOLIDAYS'),
 ('HIS_GENERAL_CHECKUP_COMPLETIONS'),('INFO_PATIENT_EXAM_EXCLUSIONS');

-- SCH-001 테이블 수
IF ((SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) = 7)
    PRINT 'PASS SCH-001 사용자 테이블 7개';
ELSE BEGIN PRINT 'FAIL SCH-001 사용자 테이블 수 불일치'; SET @Fail += 1; END

-- SCH-002 테이블 이름 집합 정확 일치
IF NOT EXISTS (SELECT Name FROM @Expected EXCEPT SELECT name FROM sys.tables WHERE is_ms_shipped = 0)
   AND NOT EXISTS (SELECT name FROM sys.tables WHERE is_ms_shipped = 0 EXCEPT SELECT Name FROM @Expected)
    PRINT 'PASS SCH-002 테이블 이름 집합 일치';
ELSE BEGIN PRINT 'FAIL SCH-002 테이블 이름 집합 불일치'; SET @Fail += 1; END

-- SCH-003 INFO_PATIENTS 컬럼 29개
IF ((SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[INFO_PATIENTS]')) = 29)
    PRINT 'PASS SCH-003 INFO_PATIENTS 29컬럼';
ELSE BEGIN PRINT 'FAIL SCH-003 INFO_PATIENTS 컬럼 수 불일치'; SET @Fail += 1; END

-- SCH-004 PK 7
IF ((SELECT COUNT(*) FROM sys.key_constraints WHERE type = 'PK') = 7)
    PRINT 'PASS SCH-004 PK 7개';
ELSE BEGIN PRINT 'FAIL SCH-004 PK 수 불일치'; SET @Fail += 1; END

-- SCH-005 FK 6 + 전부 NO ACTION
IF ((SELECT COUNT(*) FROM sys.foreign_keys) = 6
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE delete_referential_action <> 0 OR update_referential_action <> 0))
    PRINT 'PASS SCH-005 FK 6개 / 전부 NO ACTION';
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

-- SCH-015 컬럼 55개 전건 EXCEPT 양방향  (04 §8)
DECLARE @ExpCol TABLE (T SYSNAME, C SYSNAME, Ty SYSNAME, Len INT, Nul BIT, PRIMARY KEY (T, C));
INSERT INTO @ExpCol (T, C, Ty, Len, Nul) VALUES
 -- 55행 전건. 기준선 04 §8 의 컬럼 표에서 기계 생성했다(개수·타입·길이·NULL 모두 그 표가 출처다).
 -- Len 은 문자·이진형만 채운다. nvarchar/nchar 는 문자 수, MAX 는 -1, 그 밖은 NULL.
 -- INFO_PATIENTS 29행
 (N'INFO_PATIENTS', N'PatientId',            N'bigint',    NULL,  0),
 (N'INFO_PATIENTS', N'ChartNo',              N'nvarchar',  100,   0),
 (N'INFO_PATIENTS', N'Name',                 N'nvarchar',  100,   0),
 (N'INFO_PATIENTS', N'PassportNumber',       N'varchar',   10,    1),
 (N'INFO_PATIENTS', N'SocialNumber',         N'varchar',   13,    0),
 (N'INFO_PATIENTS', N'Birthday',             N'varchar',   8,     0),
 (N'INFO_PATIENTS', N'Gender',               N'char',      1,     0),
 (N'INFO_PATIENTS', N'InsuranceNumber',      N'varchar',   100,   1),
 (N'INFO_PATIENTS', N'EMail',                N'varchar',   200,   1),
 (N'INFO_PATIENTS', N'CelNumberS',           N'varchar',   13,    1),
 (N'INFO_PATIENTS', N'TelNumberS',           N'varchar',   13,    1),
 (N'INFO_PATIENTS', N'TelNumber',            N'varchar',   13,    1),
 (N'INFO_PATIENTS', N'CelNumber',            N'varchar',   13,    1),
 (N'INFO_PATIENTS', N'Zipcode',              N'varchar',   10,    1),
 (N'INFO_PATIENTS', N'Address',              N'nvarchar',  200,   1),
 (N'INFO_PATIENTS', N'AddressDetail',        N'nvarchar',  200,   1),
 (N'INFO_PATIENTS', N'Memo',                 N'nvarchar',  -1,    1),
 (N'INFO_PATIENTS', N'Active',               N'bit',       NULL,  0),
 (N'INFO_PATIENTS', N'IsStudent',            N'bit',       NULL,  0),
 (N'INFO_PATIENTS', N'IsVIP',                N'bit',       NULL,  0),
 (N'INFO_PATIENTS', N'IsReceiveCall',        N'bit',       NULL,  0),
 (N'INFO_PATIENTS', N'IsReceiveSMS',         N'bit',       NULL,  0),
 (N'INFO_PATIENTS', N'IsReceiveEmail',       N'bit',       NULL,  0),
 (N'INFO_PATIENTS', N'IsReceivePost',        N'bit',       NULL,  0),
 (N'INFO_PATIENTS', N'IsMarketingConsent',   N'bit',       NULL,  0),
 (N'INFO_PATIENTS', N'MConsentDate',         N'datetime',  NULL,  1),
 (N'INFO_PATIENTS', N'MCancelDate',          N'datetime',  NULL,  1),
 (N'INFO_PATIENTS', N'CreationDate',         N'datetime',  NULL,  0),
 (N'INFO_PATIENTS', N'LastEditDate',         N'datetime',  NULL,  0),
 -- INFO_CHECKUP_WORKS 8행
 (N'INFO_CHECKUP_WORKS', N'WorkId',               N'bigint',    NULL,  0),
 (N'INFO_CHECKUP_WORKS', N'PatientId',            N'bigint',    NULL,  0),
 (N'INFO_CHECKUP_WORKS', N'ReservationDate',      N'date',      NULL,  0),
 (N'INFO_CHECKUP_WORKS', N'TimeSlotCode',         N'char',      2,     0),
 (N'INFO_CHECKUP_WORKS', N'StatusCode',           N'char',      3,     0),
 (N'INFO_CHECKUP_WORKS', N'CreationDate',         N'datetime2', NULL,  0),
 (N'INFO_CHECKUP_WORKS', N'LastEditDate',         N'datetime2', NULL,  0),
 (N'INFO_CHECKUP_WORKS', N'RowVersion',           N'timestamp', NULL,  0),
 -- INFO_CHECKUP_WORK_EXAMS 3행
 (N'INFO_CHECKUP_WORK_EXAMS', N'WorkId',               N'bigint',    NULL,  0),
 (N'INFO_CHECKUP_WORK_EXAMS', N'ExamItemCode',         N'varchar',   10,    0),
 (N'INFO_CHECKUP_WORK_EXAMS', N'ExamSourceCode',       N'char',      3,     0),
 -- MST_EXAM_ITEMS 6행
 (N'MST_EXAM_ITEMS', N'ExamItemCode',         N'varchar',   10,    0),
 (N'MST_EXAM_ITEMS', N'ExamItemName',         N'nvarchar',  100,   0),
 (N'MST_EXAM_ITEMS', N'NexRuleCode',          N'varchar',   10,    1),
 (N'MST_EXAM_ITEMS', N'AdditionalExamCode',   N'varchar',   10,    1),
 (N'MST_EXAM_ITEMS', N'AdditionalGenderCode', N'char',      1,     1),
 (N'MST_EXAM_ITEMS', N'AdditionalActive',     N'bit',       NULL,  0),
 -- MST_HOLIDAYS 4행
 (N'MST_HOLIDAYS', N'HolidayDate',          N'date',      NULL,  0),
 (N'MST_HOLIDAYS', N'HolidayName',          N'nvarchar',  100,   0),
 (N'MST_HOLIDAYS', N'Active',               N'bit',       NULL,  0),
 (N'MST_HOLIDAYS', N'Memo',                 N'nvarchar',  500,   1),
 -- HIS_GENERAL_CHECKUP_COMPLETIONS 2행
 (N'HIS_GENERAL_CHECKUP_COMPLETIONS', N'PatientId',            N'bigint',    NULL,  0),
 (N'HIS_GENERAL_CHECKUP_COMPLETIONS', N'CompletionDate',       N'date',      NULL,  0),
 -- INFO_PATIENT_EXAM_EXCLUSIONS 3행
 (N'INFO_PATIENT_EXAM_EXCLUSIONS', N'PatientId',            N'bigint',    NULL,  0),
 (N'INFO_PATIENT_EXAM_EXCLUSIONS', N'ExamItemCode',         N'varchar',   10,    0),
 (N'INFO_PATIENT_EXAM_EXCLUSIONS', N'Memo',                 N'nvarchar',  500,   1);

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
    PRINT 'PASS SCH-015 컬럼 55개 전건 일치';
ELSE
BEGIN
    PRINT 'FAIL SCH-015 컬럼 불일치';
    SELECT '기대에만 있음' AS Side, * FROM (SELECT T,C,Ty,Len,Nul FROM @ExpCol EXCEPT SELECT T,C,Ty,Len,Nul FROM @ActCol) a;
    SELECT '실측에만 있음' AS Side, * FROM (SELECT T,C,Ty,Len,Nul FROM @ActCol EXCEPT SELECT T,C,Ty,Len,Nul FROM @ExpCol) b;
    SET @Fail += 1;
END

-- SCH-016 CHECK 제약 이름 22개 EXCEPT 양방향  (04 §8.1.3 10 + §8.2.3 3 + §8.3.3 1 + §8.4.3 7 + §8.5.2 1)
DECLARE @ExpCk TABLE (N SYSNAME PRIMARY KEY);
INSERT INTO @ExpCk (N) VALUES
 (N'CK_INFO_PATIENTS_CHART_NO_NOT_BLANK'), (N'CK_INFO_PATIENTS_NAME_NOT_BLANK'),
 (N'CK_INFO_PATIENTS_SOCIAL_FORMAT'),      (N'CK_INFO_PATIENTS_BIRTHDAY'),
 (N'CK_INFO_PATIENTS_GENDER'),             (N'CK_INFO_PATIENTS_CEL_NORMALIZED'),
 (N'CK_INFO_PATIENTS_TEL_NORMALIZED'),     (N'CK_INFO_PATIENTS_CEL_DIGIT'),
 (N'CK_INFO_PATIENTS_TEL_DIGIT'),          (N'CK_INFO_PATIENTS_EDIT_DATE'),
 (N'CK_INFO_CHECKUP_WORKS_TIME_SLOT'),     (N'CK_INFO_CHECKUP_WORKS_STATUS'),
 (N'CK_INFO_CHECKUP_WORKS_EDIT_DATE'),     (N'CK_INFO_CHECKUP_WORK_EXAMS_SOURCE'),
 (N'CK_MST_EXAM_ITEMS_CODE_NOT_BLANK'),    (N'CK_MST_EXAM_ITEMS_NAME_NOT_BLANK'),
 (N'CK_MST_EXAM_ITEMS_ROLE_REQUIRED'),     (N'CK_MST_EXAM_ITEMS_NEX_RULE'),
 (N'CK_MST_EXAM_ITEMS_AEX_CODE'),          (N'CK_MST_EXAM_ITEMS_AEX_GENDER'),
 (N'CK_MST_EXAM_ITEMS_AEX_GROUP'),         (N'CK_MST_HOLIDAYS_NAME_NOT_BLANK');

IF NOT EXISTS (SELECT N FROM @ExpCk EXCEPT SELECT name FROM sys.check_constraints)
   AND NOT EXISTS (SELECT name FROM sys.check_constraints EXCEPT SELECT N FROM @ExpCk)
    PRINT 'PASS SCH-016 CHECK 제약 이름 전건 일치';
ELSE BEGIN PRINT 'FAIL SCH-016 CHECK 제약 집합 불일치'; SET @Fail += 1; END

-- SCH-017 Default 제약 이름 14개 EXCEPT 양방향  (04 §8.1.4 10 + §8.2.3 2 + §8.4.3 1 + §8.5.2 1)
DECLARE @ExpDf TABLE (N SYSNAME);
INSERT @ExpDf (N) VALUES
 (N'DF_INFO_PATIENTS_ACTIVE'),               (N'DF_INFO_PATIENTS_IS_STUDENT'),
 (N'DF_INFO_PATIENTS_IS_VIP'),               (N'DF_INFO_PATIENTS_RECEIVE_CALL'),
 (N'DF_INFO_PATIENTS_RECEIVE_SMS'),          (N'DF_INFO_PATIENTS_RECEIVE_EMAIL'),
 (N'DF_INFO_PATIENTS_RECEIVE_POST'),         (N'DF_INFO_PATIENTS_MARKETING_CONSENT'),
 (N'DF_INFO_PATIENTS_CREATION_DATE'),        (N'DF_INFO_PATIENTS_LAST_EDIT_DATE'),
 (N'DF_INFO_CHECKUP_WORKS_CREATION_DATE'),   (N'DF_INFO_CHECKUP_WORKS_LAST_EDIT_DATE'),
 (N'DF_MST_EXAM_ITEMS_AEX_ACTIVE'),          (N'DF_MST_HOLIDAYS_ACTIVE');

IF NOT EXISTS (SELECT N FROM @ExpDf EXCEPT SELECT name FROM sys.default_constraints)
   AND NOT EXISTS (SELECT name FROM sys.default_constraints EXCEPT SELECT N FROM @ExpDf)
    PRINT 'PASS SCH-017 Default 제약 이름 전건 일치';
ELSE BEGIN PRINT 'FAIL SCH-017 Default 제약 집합 불일치'; SET @Fail += 1; END

-- SCH-018 Nonclustered Index 5개 이름 + Key 컬럼 순서 EXCEPT 양방향  (04 §8.1.5 / §8.2.4)
--   COUNT 로는 (Name, Birthday) 가 (Name, Gender) 로 바뀐 것을 못 잡는다. 행 단위로 펼쳐 대조한다.
DECLARE @ExpIx TABLE (IxName SYSNAME, Ord TINYINT, ColName SYSNAME, PRIMARY KEY (IxName, Ord));
INSERT @ExpIx (IxName, Ord, ColName) VALUES
 (N'IX_INFO_PATIENTS_NAME_BIRTHDAY',              1, N'Name'),
 (N'IX_INFO_PATIENTS_NAME_BIRTHDAY',              2, N'Birthday'),
 (N'IX_INFO_PATIENTS_BIRTHDAY',                   1, N'Birthday'),
 (N'IX_INFO_PATIENTS_CEL_NUMBER_S',               1, N'CelNumberS'),
 (N'IX_INFO_CHECKUP_WORKS_SLOT',                  1, N'ReservationDate'),
 (N'IX_INFO_CHECKUP_WORKS_SLOT',                  2, N'TimeSlotCode'),
 (N'IX_INFO_CHECKUP_WORKS_SLOT',                  3, N'StatusCode'),
 (N'IX_INFO_CHECKUP_WORKS_PATIENT_STATE_DATE',    1, N'PatientId'),
 (N'IX_INFO_CHECKUP_WORKS_PATIENT_STATE_DATE',    2, N'StatusCode'),
 (N'IX_INFO_CHECKUP_WORKS_PATIENT_STATE_DATE',    3, N'ReservationDate');

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
