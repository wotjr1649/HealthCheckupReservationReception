SET NOCOUNT ON;
DECLARE @Fail INT = 0;

DECLARE @Expected TABLE ([객체명] SYSNAME PRIMARY KEY);
INSERT INTO @Expected ([객체명]) VALUES
 ('수검자'),('예약접수'),
 ('검사코드'),('휴무일'),
 ('완료이력'),('변경이력');

-- SCH-001 테이블 수
IF ((SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) = 6)
    PRINT 'PASS SCH-001 사용자 테이블 6개';
ELSE BEGIN PRINT 'FAIL SCH-001 사용자 테이블 수 불일치'; SET @Fail += 1; END

-- SCH-002 테이블 이름 집합 정확 일치
IF NOT EXISTS (SELECT [객체명] FROM @Expected EXCEPT SELECT name FROM sys.tables WHERE is_ms_shipped = 0)
   AND NOT EXISTS (SELECT name FROM sys.tables WHERE is_ms_shipped = 0 EXCEPT SELECT [객체명] FROM @Expected)
    PRINT 'PASS SCH-002 테이블 이름 집합 일치';
ELSE BEGIN PRINT 'FAIL SCH-002 테이블 이름 집합 불일치'; SET @Fail += 1; END

-- SCH-003 수검자 컬럼 17개 (R7 에서 [행버전] 신설, 04 §8.1.2)
IF ((SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[수검자]')) = 17)
    PRINT 'PASS SCH-003 수검자 17컬럼';
ELSE BEGIN PRINT 'FAIL SCH-003 수검자 컬럼 수 불일치'; SET @Fail += 1; END

-- SCH-004 PK 6
IF ((SELECT COUNT(*) FROM sys.key_constraints WHERE type = 'PK') = 6)
    PRINT 'PASS SCH-004 PK 6개';
ELSE BEGIN PRINT 'FAIL SCH-004 PK 수 불일치'; SET @Fail += 1; END

-- SCH-005 FK 2 + 전부 NO ACTION. 검사항목이 사라져 FK 2개도 함께 사라졌다 (plans/10 §3)
IF ((SELECT COUNT(*) FROM sys.foreign_keys) = 2
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE delete_referential_action <> 0 OR update_referential_action <> 0))
    PRINT 'PASS SCH-005 FK 2개 / 전부 NO ACTION';
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

-- SCH-008 업무/조회 NCI 5 (PK/UQ/UX 제외). IX_변경이력_TARGET 이 SP-LOG-01 조회 경로로 신설되었다
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

-- SCH-014 SP 20  (R7 에서 휴무일 SP 4개 신설, 05 §1.3)
IF ((SELECT COUNT(*) FROM sys.procedures WHERE name LIKE 'USP[_]HC[_]%') = 20)
    PRINT 'PASS SCH-014 Stored Procedure 20개';
ELSE BEGIN PRINT 'FAIL SCH-014 SP 수 불일치 (T30 이전이면 정상)'; SET @Fail += 1; END

-- SCH-015 컬럼 53개 전건 EXCEPT 양방향  (04 §8)
DECLARE @ExpCol TABLE (T SYSNAME, Col SYSNAME, Ty SYSNAME, Len INT, Nul BIT, PRIMARY KEY (T, Col));
INSERT INTO @ExpCol (T, Col, Ty, Len, Nul) VALUES
 -- 53행 전건. 기준선 04 §8 의 컬럼 표에서 기계 생성했다(개수·타입·길이·NULL 모두 그 표가 출처다).
 -- Len 은 문자·이진형만 채운다. nvarchar/nchar 는 문자 수, MAX 는 -1, 그 밖은 NULL.
 -- 수검자 17행
 (N'수검자', N'수검자ID',                 N'bigint',      NULL,  0)
,(N'수검자', N'생성일시',                  N'datetime',    NULL,  0)
,(N'수검자', N'최종수정일시',                N'datetime',    NULL,  0)
,(N'수검자', N'행버전',                   N'timestamp',   NULL,  0)
,(N'수검자', N'차트번호',                  N'nvarchar',    100,   0)
,(N'수검자', N'성명',                    N'nvarchar',    100,   0)
,(N'수검자', N'주민번호',                  N'varchar',     13,    0)
,(N'수검자', N'생년월일',                  N'varchar',     8,     0)
,(N'수검자', N'성별',                    N'char',        1,     0)
,(N'수검자', N'이메일',                   N'varchar',     200,   1)
,(N'수검자', N'휴대전화',                  N'varchar',     13,    1)
,(N'수검자', N'전화번호',                  N'varchar',     13,    1)
,(N'수검자', N'우편번호',                  N'varchar',     10,    1)
,(N'수검자', N'주소',                    N'nvarchar',    200,   1)
,(N'수검자', N'상세주소',                  N'nvarchar',    200,   1)
,(N'수검자', N'B형간염제외여부',              N'bit',         NULL,  0)
,(N'수검자', N'비고',                    N'nvarchar',    -1,    1)
 -- 예약접수 10행
,(N'예약접수', N'업무ID',                  N'bigint',      NULL,  0)
,(N'예약접수', N'생성일시',                  N'datetime2',   NULL,  0)
,(N'예약접수', N'최종수정일시',                N'datetime2',   NULL,  0)
,(N'예약접수', N'행버전',                   N'timestamp',   NULL,  0)
,(N'예약접수', N'수검자ID',                 N'bigint',      NULL,  0)
,(N'예약접수', N'예약일',                   N'date',        NULL,  0)
,(N'예약접수', N'시간대코드',                 N'char',        2,     0)
,(N'예약접수', N'상태코드',                  N'char',        3,     0)
,(N'예약접수', N'국가검사항목',                N'nvarchar',    100,   0)
,(N'예약접수', N'추가검사항목',                N'nvarchar',    50,    1)
 -- 검사코드 6행
,(N'검사코드', N'검사항목코드',                N'varchar',     10,    0)
,(N'검사코드', N'검사항목명',                 N'nvarchar',    100,   0)
,(N'검사코드', N'국가검사규칙코드',              N'varchar',     10,    1)
,(N'검사코드', N'추가검사코드',                N'varchar',     10,    1)
,(N'검사코드', N'추가검사성별코드',              N'char',        1,     1)
,(N'검사코드', N'추가검사사용여부',              N'bit',         NULL,  0)
 -- 휴무일 8행
,(N'휴무일', N'휴무일자',                  N'date',        NULL,  0)
,(N'휴무일', N'생성일시',                  N'datetime2',   NULL,  0)
,(N'휴무일', N'최종수정일시',                N'datetime2',   NULL,  0)
,(N'휴무일', N'행버전',                   N'timestamp',   NULL,  0)
,(N'휴무일', N'휴무일명',                  N'nvarchar',    100,   0)
,(N'휴무일', N'휴무구분',                  N'nvarchar',    10,    0)
,(N'휴무일', N'사용여부',                  N'bit',         NULL,  0)
,(N'휴무일', N'비고',                    N'nvarchar',    500,   1)
 -- 완료이력 4행
,(N'완료이력', N'수검자ID',                 N'bigint',      NULL,  0)
,(N'완료이력', N'완료일자',                  N'date',        NULL,  0)
,(N'완료이력', N'국가검사항목',                N'nvarchar',    100,   1)
,(N'완료이력', N'추가검사항목',                N'nvarchar',    50,    1)
 -- 변경이력 8행
,(N'변경이력', N'이력ID',                  N'bigint',      NULL,  0)
,(N'변경이력', N'기록일시',                  N'datetime2',   NULL,  0)
,(N'변경이력', N'조작자명',                  N'nvarchar',    50,    1)
,(N'변경이력', N'대상테이블',                 N'nvarchar',    10,    0)
,(N'변경이력', N'대상키',                   N'bigint',      NULL,  0)
,(N'변경이력', N'컬럼명',                   N'nvarchar',    30,    0)
,(N'변경이력', N'변경전',                   N'nvarchar',    4000,  1)
,(N'변경이력', N'변경후',                   N'nvarchar',    4000,  1);
DECLARE @ActCol TABLE (T SYSNAME, Col SYSNAME, Ty SYSNAME, Len INT, Nul BIT, PRIMARY KEY (T, Col));
INSERT INTO @ActCol (T, Col, Ty, Len, Nul)
SELECT t.name, c.name, y.name
     , CASE WHEN y.name IN ('nvarchar','nchar') AND c.max_length > 0 THEN c.max_length / 2
            WHEN y.name IN ('varchar','char','binary','varbinary') THEN c.max_length
            WHEN c.max_length = -1 THEN -1 ELSE NULL END
     , c.is_nullable
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types  y ON y.user_type_id = c.user_type_id
WHERE t.is_ms_shipped = 0;

IF NOT EXISTS (SELECT T,Col,Ty,Len,Nul FROM @ExpCol EXCEPT SELECT T,Col,Ty,Len,Nul FROM @ActCol)
   AND NOT EXISTS (SELECT T,Col,Ty,Len,Nul FROM @ActCol EXCEPT SELECT T,Col,Ty,Len,Nul FROM @ExpCol)
    PRINT 'PASS SCH-015 컬럼 53개 전건 일치';
ELSE
BEGIN
    PRINT 'FAIL SCH-015 컬럼 불일치';
    SELECT '기대에만 있음' AS Side, * FROM (SELECT T,Col,Ty,Len,Nul FROM @ExpCol EXCEPT SELECT T,Col,Ty,Len,Nul FROM @ActCol) a;
    SELECT '실측에만 있음' AS Side, * FROM (SELECT T,Col,Ty,Len,Nul FROM @ActCol EXCEPT SELECT T,Col,Ty,Len,Nul FROM @ExpCol) b;
    SET @Fail += 1;
END

-- SCH-016 CHECK 제약 이름 26개 EXCEPT 양방향  (04 §8.1.3 7 + §8.2.3 5 + §8.3.3 8 + §8.4.3 3 + §8.5.3 1 + §8.6.3 2)
DECLARE @ExpCk TABLE (N SYSNAME PRIMARY KEY);
INSERT INTO @ExpCk (N) VALUES
 (N'CK_수검자_CHART_NO_NOT_BLANK'), (N'CK_수검자_NAME_NOT_BLANK'),
 (N'CK_수검자_SOCIAL_FORMAT'),      (N'CK_수검자_BIRTHDAY'),
 (N'CK_수검자_GENDER'),             (N'CK_수검자_CEL_DIGIT'),
 (N'CK_수검자_EDIT_DATE'),
 (N'CK_예약접수_TIME_SLOT'),        (N'CK_예약접수_STATUS'),
 (N'CK_예약접수_EDIT_DATE'),        (N'CK_예약접수_EXAM_FORMAT'),
 (N'CK_예약접수_EXAM_PAIR'),
 (N'CK_완료이력_EXAM_FORMAT'),
 (N'CK_검사코드_CODE_NOT_BLANK'),   (N'CK_검사코드_CODE_FORMAT'),
 (N'CK_검사코드_NAME_NOT_BLANK'),
 (N'CK_검사코드_ROLE_REQUIRED'),    (N'CK_검사코드_NEX_RULE'),
 (N'CK_검사코드_AEX_CODE'),         (N'CK_검사코드_AEX_GENDER'),
 (N'CK_검사코드_AEX_GROUP'),        (N'CK_휴무일_NAME_NOT_BLANK'),
 (N'CK_휴무일_TYPE'),               (N'CK_휴무일_EDIT_DATE'),
 (N'CK_변경이력_TARGET_TABLE'),     (N'CK_변경이력_COLUMN_NOT_BLANK');

IF NOT EXISTS (SELECT N FROM @ExpCk EXCEPT SELECT name FROM sys.check_constraints)
   AND NOT EXISTS (SELECT name FROM sys.check_constraints EXCEPT SELECT N FROM @ExpCk)
    PRINT 'PASS SCH-016 CHECK 제약 이름 전건 일치';
ELSE BEGIN PRINT 'FAIL SCH-016 CHECK 제약 집합 불일치'; SET @Fail += 1; END

-- SCH-017 Default 제약 이름 10개 EXCEPT 양방향  (04 §8.1.4 3 + §8.2.3 2 + §8.3.3 1 + §8.4.3 3 + §8.6.3 1)
-- [X] 초안의 14개 이름은 기준선 04 §8 의 것이 아니었다(`DF_수검자_JOB` 은 존재하지 않는 Job 컬럼을
--     가리켰고 `..._GENDER`·`..._MEMO`·`..._ADDRESS` 등은 04 §8.1.4 에 없다). T06 의 DDL 이 기준선과 일치하므로
--     틀린 쪽은 이 기대값이다. 04 §8.1.4 / §8.2.3 / §8.4.3 / §8.5.2 의 이름을 그대로 옮겨 적는다.
DECLARE @ExpDf TABLE (N SYSNAME);
INSERT @ExpDf (N) VALUES
 (N'DF_수검자_HEPATITIS_B_EXCLUDED'), (N'DF_수검자_CREATION_DATE'),
 (N'DF_수검자_LAST_EDIT_DATE'),       (N'DF_예약접수_CREATION_DATE'),
 (N'DF_예약접수_LAST_EDIT_DATE'),     (N'DF_검사코드_AEX_ACTIVE'),
 (N'DF_휴무일_ACTIVE'),               (N'DF_휴무일_CREATION_DATE'),
 (N'DF_휴무일_LAST_EDIT_DATE'),       (N'DF_변경이력_CREATION_DATE');

IF NOT EXISTS (SELECT N FROM @ExpDf EXCEPT SELECT name FROM sys.default_constraints)
   AND NOT EXISTS (SELECT name FROM sys.default_constraints EXCEPT SELECT N FROM @ExpDf)
    PRINT 'PASS SCH-017 Default 제약 이름 전건 일치';
ELSE BEGIN PRINT 'FAIL SCH-017 Default 제약 집합 불일치'; SET @Fail += 1; END

-- SCH-018 Nonclustered Index 5개 이름 + Key 컬럼 순서 EXCEPT 양방향  (04 §8.1.5 / §8.2.4 / §8.6.3)
--   COUNT 로는 (성명, 생년월일) 이 (성명, 성별) 로 바뀐 것을 못 잡는다. 행 단위로 펼쳐 대조한다.
-- [X] 초안의 기대값은 존재하지 않는 Index 를 가리켰다(`IX_수검자_CHART_NO` 는 UQ 이지 NCI 가 아니고,
--     `IX_예약접수_DATE_SLOT_STATUS`·`..._PATIENT_STATUS`·`IX_검사항목_WORK` 는
--     04 §8 에 없는 이름이다). 04 §8.1.5 / §8.2.4 / §8.6.3 의 NCI 5개 · Key 12행이 실제 계약이다.
--     `IX_수검자_CEL_NUMBER_S` 는 `CelNumberS` 컬럼과 함께 사라졌다 (04 §8.1.5).
DECLARE @ExpIx TABLE (IxName SYSNAME, Ord TINYINT, ColName SYSNAME, PRIMARY KEY (IxName, Ord));
INSERT @ExpIx (IxName, Ord, ColName) VALUES
 (N'IX_수검자_NAME_BIRTHDAY',              1, N'성명'),
 (N'IX_수검자_NAME_BIRTHDAY',              2, N'생년월일'),
 (N'IX_수검자_BIRTHDAY',                   1, N'생년월일'),
 (N'IX_예약접수_SLOT',                  1, N'예약일'),
 (N'IX_예약접수_SLOT',                  2, N'시간대코드'),
 (N'IX_예약접수_SLOT',                  3, N'상태코드'),
 (N'IX_예약접수_PATIENT_STATE_DATE',    1, N'수검자ID'),
 (N'IX_예약접수_PATIENT_STATE_DATE',    2, N'상태코드'),
 (N'IX_예약접수_PATIENT_STATE_DATE',    3, N'예약일'),
 (N'IX_변경이력_TARGET',                1, N'대상테이블'),
 (N'IX_변경이력_TARGET',                2, N'대상키'),
 (N'IX_변경이력_TARGET',                3, N'기록일시');

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

-- SCH-019 SP 별 Parameter 를 (SP, 순번, 이름, 타입) 4-튜플로 EXCEPT 양방향 대조 (합계 113)
-- [X] G09 는 "Parameter 99 EXCEPT 양방향" 을 요구하는데 그것을 판정하는 검사가 없었다.
--     처음엔 SP 별 **개수**만 맞췄는데 그것으로는 이름이 바뀌거나 순서가 뒤바뀐 드리프트를 놓친다.
--     기대값 출처는 05 §10~§12 입력표 · 06 §18 SP Matrix 다.
DECLARE @ExpP TABLE (SpName SYSNAME, Ord INT, ParamName SYSNAME, TypeName SYSNAME,
                     PRIMARY KEY (SpName, Ord));
INSERT INTO @ExpP (SpName, Ord, ParamName, TypeName) VALUES
   (N'USP_HC_수검자_등록', 1, N'@차트번호자동발급여부', 'bit')
 , (N'USP_HC_수검자_등록', 2, N'@차트번호', 'nvarchar')
 , (N'USP_HC_수검자_등록', 3, N'@성명', 'nvarchar')
 , (N'USP_HC_수검자_등록', 4, N'@주민번호', 'varchar')
 , (N'USP_HC_수검자_등록', 5, N'@휴대전화', 'varchar')
 , (N'USP_HC_수검자_등록', 6, N'@전화번호', 'varchar')
 , (N'USP_HC_수검자_등록', 7, N'@이메일', 'varchar')
 , (N'USP_HC_수검자_등록', 8, N'@우편번호', 'varchar')
 , (N'USP_HC_수검자_등록', 9, N'@주소', 'nvarchar')
 , (N'USP_HC_수검자_등록', 10, N'@상세주소', 'nvarchar')
 , (N'USP_HC_수검자_등록', 11, N'@비고', 'nvarchar')
 , (N'USP_HC_수검자_등록', 12, N'@B형간염제외여부', 'bit')
 , (N'USP_HC_수검자_등록', 13, N'@유사수검자확인여부', 'bit')
 , (N'USP_HC_수검자_등록', 14, N'@조작자명', 'nvarchar')
 , (N'USP_HC_예약_등록', 1, N'@수검자ID', 'bigint')
 , (N'USP_HC_예약_등록', 2, N'@예약구분', 'varchar')
 , (N'USP_HC_예약_등록', 3, N'@예약일', 'date')
 , (N'USP_HC_예약_등록', 4, N'@시간대코드', 'char')
 , (N'USP_HC_예약_등록', 5, N'@추가검사01선택여부', 'bit')
 , (N'USP_HC_예약_등록', 6, N'@추가검사02선택여부', 'bit')
 , (N'USP_HC_예약_등록', 7, N'@추가검사03선택여부', 'bit')
 , (N'USP_HC_예약_등록', 8, N'@추가검사04선택여부', 'bit')
 , (N'USP_HC_예약_등록', 9, N'@추가검사05선택여부', 'bit')
 , (N'USP_HC_예약_등록', 10, N'@추가검사06선택여부', 'bit')
 , (N'USP_HC_예약_등록', 11, N'@추가검사07선택여부', 'bit')
 , (N'USP_HC_예약_등록', 12, N'@조작자명', 'nvarchar')
 , (N'USP_HC_변경이력_조회', 1, N'@대상테이블', 'nvarchar')
 , (N'USP_HC_변경이력_조회', 2, N'@대상키', 'bigint')
 , (N'USP_HC_수검자목록_조회', 1, N'@차트번호', 'nvarchar')
 , (N'USP_HC_수검자목록_조회', 2, N'@성명', 'nvarchar')
 , (N'USP_HC_수검자목록_조회', 3, N'@주민번호', 'varchar')
 , (N'USP_HC_수검자목록_조회', 4, N'@생년월일', 'varchar')
 , (N'USP_HC_수검자목록_조회', 5, N'@휴대전화', 'varchar')
 , (N'USP_HC_수검자상세_조회', 1, N'@수검자ID', 'bigint')
 , (N'USP_HC_수검자유효업무_조회', 1, N'@수검자ID', 'bigint')
 , (N'USP_HC_예약가능정보_조회', 1, N'@수검자ID', 'bigint')
 , (N'USP_HC_예약가능정보_조회', 2, N'@업무ID', 'bigint')
 , (N'USP_HC_예약가능정보_조회', 3, N'@행버전', 'binary')
 , (N'USP_HC_예약가능정보_조회', 4, N'@예약구분', 'varchar')
 , (N'USP_HC_예약가능정보_조회', 5, N'@예약일', 'date')
 , (N'USP_HC_예약가능정보_조회', 6, N'@시간대코드', 'char')
 , (N'USP_HC_예약가능정보_조회', 7, N'@추가검사01선택여부', 'bit')
 , (N'USP_HC_예약가능정보_조회', 8, N'@추가검사02선택여부', 'bit')
 , (N'USP_HC_예약가능정보_조회', 9, N'@추가검사03선택여부', 'bit')
 , (N'USP_HC_예약가능정보_조회', 10, N'@추가검사04선택여부', 'bit')
 , (N'USP_HC_예약가능정보_조회', 11, N'@추가검사05선택여부', 'bit')
 , (N'USP_HC_예약가능정보_조회', 12, N'@추가검사06선택여부', 'bit')
 , (N'USP_HC_예약가능정보_조회', 13, N'@추가검사07선택여부', 'bit')
 , (N'USP_HC_예약접수목록_조회', 1, N'@시작일', 'date')
 , (N'USP_HC_예약접수목록_조회', 2, N'@종료일', 'date')
 , (N'USP_HC_예약접수목록_조회', 3, N'@상태코드', 'char')
 , (N'USP_HC_예약접수목록_조회', 4, N'@차트번호', 'nvarchar')
 , (N'USP_HC_예약접수목록_조회', 5, N'@성명', 'nvarchar')
 , (N'USP_HC_예약접수상세_조회', 1, N'@업무ID', 'bigint')
 , (N'USP_HC_수검자정보_수정', 1, N'@수검자ID', 'bigint')
 , (N'USP_HC_수검자정보_수정', 2, N'@행버전', 'binary')
 , (N'USP_HC_수검자정보_수정', 3, N'@차트번호', 'nvarchar')
 , (N'USP_HC_수검자정보_수정', 4, N'@성명', 'nvarchar')
 , (N'USP_HC_수검자정보_수정', 5, N'@주민번호', 'varchar')
 , (N'USP_HC_수검자정보_수정', 6, N'@휴대전화', 'varchar')
 , (N'USP_HC_수검자정보_수정', 7, N'@전화번호', 'varchar')
 , (N'USP_HC_수검자정보_수정', 8, N'@이메일', 'varchar')
 , (N'USP_HC_수검자정보_수정', 9, N'@우편번호', 'varchar')
 , (N'USP_HC_수검자정보_수정', 10, N'@주소', 'nvarchar')
 , (N'USP_HC_수검자정보_수정', 11, N'@상세주소', 'nvarchar')
 , (N'USP_HC_수검자정보_수정', 12, N'@비고', 'nvarchar')
 , (N'USP_HC_수검자정보_수정', 13, N'@B형간염제외여부', 'bit')
 , (N'USP_HC_수검자정보_수정', 14, N'@조작자명', 'nvarchar')
 , (N'USP_HC_예약_변경', 1, N'@업무ID', 'bigint')
 , (N'USP_HC_예약_변경', 2, N'@행버전', 'binary')
 , (N'USP_HC_예약_변경', 3, N'@예약일', 'date')
 , (N'USP_HC_예약_변경', 4, N'@시간대코드', 'char')
 , (N'USP_HC_예약_변경', 5, N'@추가검사01선택여부', 'bit')
 , (N'USP_HC_예약_변경', 6, N'@추가검사02선택여부', 'bit')
 , (N'USP_HC_예약_변경', 7, N'@추가검사03선택여부', 'bit')
 , (N'USP_HC_예약_변경', 8, N'@추가검사04선택여부', 'bit')
 , (N'USP_HC_예약_변경', 9, N'@추가검사05선택여부', 'bit')
 , (N'USP_HC_예약_변경', 10, N'@추가검사06선택여부', 'bit')
 , (N'USP_HC_예약_변경', 11, N'@추가검사07선택여부', 'bit')
 , (N'USP_HC_예약_변경', 12, N'@조작자명', 'nvarchar')
 , (N'USP_HC_예약_취소', 1, N'@업무ID', 'bigint')
 , (N'USP_HC_예약_취소', 2, N'@행버전', 'binary')
 , (N'USP_HC_예약_취소', 3, N'@조작자명', 'nvarchar')
 , (N'USP_HC_접수_완료', 1, N'@업무ID', 'bigint')
 , (N'USP_HC_접수_완료', 2, N'@행버전', 'binary')
 , (N'USP_HC_접수_완료', 3, N'@조작자명', 'nvarchar')
 , (N'USP_HC_접수추가검사_변경', 1, N'@업무ID', 'bigint')
 , (N'USP_HC_접수추가검사_변경', 2, N'@행버전', 'binary')
 , (N'USP_HC_접수추가검사_변경', 3, N'@추가검사01선택여부', 'bit')
 , (N'USP_HC_접수추가검사_변경', 4, N'@추가검사02선택여부', 'bit')
 , (N'USP_HC_접수추가검사_변경', 5, N'@추가검사03선택여부', 'bit')
 , (N'USP_HC_접수추가검사_변경', 6, N'@추가검사04선택여부', 'bit')
 , (N'USP_HC_접수추가검사_변경', 7, N'@추가검사05선택여부', 'bit')
 , (N'USP_HC_접수추가검사_변경', 8, N'@추가검사06선택여부', 'bit')
 , (N'USP_HC_접수추가검사_변경', 9, N'@추가검사07선택여부', 'bit')
 , (N'USP_HC_접수추가검사_변경', 10, N'@조작자명', 'nvarchar')
 , (N'USP_HC_접수_취소', 1, N'@업무ID', 'bigint')
 , (N'USP_HC_접수_취소', 2, N'@행버전', 'binary')
 , (N'USP_HC_접수_취소', 3, N'@조작자명', 'nvarchar')
    
 , (N'USP_HC_휴무일목록_조회', 1, N'@시작일자', 'date')
 , (N'USP_HC_휴무일목록_조회', 2, N'@종료일자', 'date')
 , (N'USP_HC_휴무일목록_조회', 3, N'@휴무구분', 'nvarchar')
 , (N'USP_HC_자체휴무일_등록', 1, N'@휴무일자', 'date')
 , (N'USP_HC_자체휴무일_등록', 2, N'@휴무일명', 'nvarchar')
 , (N'USP_HC_자체휴무일_등록', 3, N'@사용여부', 'bit')
 , (N'USP_HC_자체휴무일_등록', 4, N'@비고', 'nvarchar')
 , (N'USP_HC_자체휴무일_수정', 1, N'@휴무일자', 'date')
 , (N'USP_HC_자체휴무일_수정', 2, N'@행버전', 'binary')
 , (N'USP_HC_자체휴무일_수정', 3, N'@휴무일명', 'nvarchar')
 , (N'USP_HC_자체휴무일_수정', 4, N'@사용여부', 'bit')
 , (N'USP_HC_자체휴무일_수정', 5, N'@비고', 'nvarchar')
 , (N'USP_HC_자체휴무일_삭제', 1, N'@휴무일자', 'date')
 , (N'USP_HC_자체휴무일_삭제', 2, N'@행버전', 'binary');

DECLARE @ActP TABLE (SpName SYSNAME, Ord INT, ParamName SYSNAME, TypeName SYSNAME,
                     PRIMARY KEY (SpName, Ord));
INSERT INTO @ActP (SpName, Ord, ParamName, TypeName)
SELECT o.name, pa.parameter_id, pa.name, TYPE_NAME(pa.user_type_id)
  FROM sys.procedures o
  JOIN sys.parameters pa ON pa.object_id = o.object_id
 WHERE o.name LIKE 'USP[_]HC[_]%';

DECLARE @SumP INT = (SELECT COUNT(*) FROM @ActP);
IF NOT EXISTS (SELECT SpName, Ord, ParamName, TypeName FROM @ExpP
               EXCEPT SELECT SpName, Ord, ParamName, TypeName FROM @ActP)
   AND NOT EXISTS (SELECT SpName, Ord, ParamName, TypeName FROM @ActP
                   EXCEPT SELECT SpName, Ord, ParamName, TypeName FROM @ExpP)
   AND @SumP = 113
    PRINT 'PASS SCH-019 SP 별 Parameter 이름·순번·타입 전건 일치 (합계 113)';
ELSE
BEGIN
    PRINT 'FAIL SCH-019 Parameter 불일치 (합계 ' + CONVERT(VARCHAR(5), ISNULL(@SumP, -1)) + ')';
    SELECT '기대에만 있음' AS Side, * FROM (SELECT SpName, Ord, ParamName, TypeName FROM @ExpP
        EXCEPT SELECT SpName, Ord, ParamName, TypeName FROM @ActP) a;
    SELECT '실측에만 있음' AS Side, * FROM (SELECT SpName, Ord, ParamName, TypeName FROM @ActP
        EXCEPT SELECT SpName, Ord, ParamName, TypeName FROM @ExpP) b;
    SET @Fail += 1;
END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 01_Schema_Tests 완료 ===';
GO
