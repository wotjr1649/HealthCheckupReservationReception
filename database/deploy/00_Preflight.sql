SET NOCOUNT ON;
DECLARE @TargetDb SYSNAME = N'HealthCheckupReservationReceptionDb';
DECLARE @TargetInst SYSNAME = N'SQLEXPRESS';
DECLARE @CompatLevel VARCHAR(10);

-- PRE-001 인스턴스 exact match
IF ISNULL(CONVERT(SYSNAME, SERVERPROPERTY('InstanceName')), N'') <> @TargetInst
    THROW 50010, N'Preflight: 예상하지 않은 SQL Server 인스턴스입니다.', 1;
PRINT 'PASS PRE-001 인스턴스 ' + @TargetInst;

-- PRE-002 대상 DB exact match
IF DB_NAME() <> @TargetDb
    THROW 50011, N'Preflight: 대상 Database 가 아닙니다.', 1;
PRINT 'PASS PRE-002 Database ' + DB_NAME();

-- PRE-003 승인된 호스트
-- [X] 초안은 'DB_ID() <= 4' 였다. 바로 위 PRE-002 가 DB 이름을 확정한 뒤이므로
--     이 조건은 어떤 입력으로도 참이 될 수 없는 죽은 코드였다(사용자 DB 의 database_id 는 항상 5 이상).
--     실제 위험은 '같은 이름의 DB 가 다른 PC 에 있는 것' 이고, InstanceName 은 다른 PC 에서도 SQLEXPRESS 다.
IF ISNULL(CONVERT(SYSNAME, SERVERPROPERTY('MachineName')), N'') <> N'DESKTOP-DP7KRE4'
    THROW 50012, N'Preflight: 승인되지 않은 호스트입니다.', 1;
PRINT 'PASS PRE-003 호스트 ' + CONVERT(SYSNAME, SERVERPROPERTY('MachineName'));

-- PRE-004 KST
IF DATEPART(TZOFFSET, SYSDATETIMEOFFSET()) <> 540
    THROW 50013, N'Preflight: DB 서버 시각이 KST(+09:00) 가 아닙니다.', 1;
PRINT 'PASS PRE-004 KST +09:00';

-- PRE-005 버전 하한
IF CONVERT(INT, SERVERPROPERTY('ProductMajorVersion')) < 11
    THROW 50014, N'Preflight: SQL Server 2012(11.x) 이상이 필요합니다.', 1;
PRINT 'PASS PRE-005 ProductVersion ' + CONVERT(VARCHAR(30), SERVERPROPERTY('ProductVersion'));

-- PRE-006 RCSI OFF
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = @TargetDb AND is_read_committed_snapshot_on = 1)
    THROW 50015, N'Preflight: READ_COMMITTED_SNAPSHOT 이 켜져 있습니다.', 1;
PRINT 'PASS PRE-006 READ_COMMITTED_SNAPSHOT OFF';

-- 증거 기록 (판정하지 않고 값만 남긴다)
-- [X] 초안은 PRINT 인자에 (SELECT compatibility_level FROM sys.databases ...) 를 직접 넣었다.
--     PRINT 는 스칼라 식만 받으므로 Msg 1046 으로 배치 전체가 컴파일 실패한다(실측 확인).
SELECT @CompatLevel = CONVERT(VARCHAR(10), compatibility_level) FROM sys.databases WHERE name = @TargetDb;

PRINT 'INFO Edition           = ' + CONVERT(VARCHAR(80), SERVERPROPERTY('Edition'));
PRINT 'INFO CompatibilityLevel= ' + ISNULL(@CompatLevel, '(미확인)');
PRINT 'INFO Collation         = ' + ISNULL(CONVERT(VARCHAR(80), DATABASEPROPERTYEX(@TargetDb, 'Collation')), '(미확인)');
PRINT 'INFO ServerTime        = ' + CONVERT(VARCHAR(40), SYSDATETIMEOFFSET(), 126);
PRINT 'INFO LoginName         = ' + SUSER_SNAME();
GO
