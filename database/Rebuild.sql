SET NOCOUNT ON;

-- 50020  호스트 + 인스턴스
-- [X] InstanceName 만으로는 다른 PC 의 .\SQLEXPRESS 를 구별하지 못한다. DROP DATABASE 직전이므로 호스트를 함께 본다.
IF ISNULL(CONVERT(SYSNAME, SERVERPROPERTY('InstanceName')), N'') <> N'SQLEXPRESS'
   OR ISNULL(CONVERT(SYSNAME, SERVERPROPERTY('MachineName')), N'') <> N'DESKTOP-DP7KRE4'
    THROW 50020, N'Rebuild: 승인되지 않은 호스트 또는 인스턴스입니다.', 1;

-- 50021  master 컨텍스트에서만 실행
IF DB_NAME() <> N'master'
    THROW 50021, N'Rebuild: master 컨텍스트에서 실행해야 합니다.', 1;

-- 50022  기존 대상 DB 에 04 §8 계약 밖 사용자 Table 이 있으면 중단
-- [X] 초안은 'database_id <= 4' 였다. 사용자 DB 는 database_id 가 항상 5 이상이므로(실측: Net461MvpSample = 5)
--     고정된 비시스템 DB 이름에 대해 이 조건은 항상 거짓이다 — 절대 발화하지 않는 죽은 코드였고,
--     초안이 50011 을 죽은 코드라고 지적하면서 같은 형태를 남긴 셈이다.
--     DROP DATABASE 직전에 실제로 확인해야 하는 것은 "그 DB 가 정말 Phase 4 DB 인가" 다.
--     동명 DB 가 다른 내용을 담고 있으면 그대로 삭제된다.
IF DB_ID(N'HealthCheckupReservationReceptionDb') IS NOT NULL
BEGIN
    DECLARE @Expect TABLE (N SYSNAME);
    INSERT @Expect (N) VALUES
     (N'INFO_PATIENTS'), (N'INFO_CHECKUP_WORKS'), (N'INFO_CHECKUP_WORK_EXAMS'),
     (N'INFO_PATIENT_EXAM_EXCLUSIONS'), (N'HIS_GENERAL_CHECKUP_COMPLETIONS'),
     (N'MST_EXAM_ITEMS'), (N'MST_HOLIDAYS');

    DECLARE @Foreign INT;
    -- 동적 SQL 을 쓰지 않는다. 대상 DB 이름은 대괄호 하드코딩 3부 이름으로만 참조한다.
    SELECT @Foreign = COUNT(*)
    FROM [HealthCheckupReservationReceptionDb].[sys].[tables] t
    WHERE t.is_ms_shipped = 0
      AND t.name NOT IN (SELECT N FROM @Expect);

    IF @Foreign > 0
        THROW 50022, N'Rebuild: 대상 DB 에 Phase 4 계약 밖 Table 이 있습니다. 동명 DB 를 삭제하려는 것일 수 있습니다.', 1;
    PRINT N'INFO 50022 가드 통과 — 계약 밖 Table 0개';
END

-- 50023  KST
IF DATEPART(TZOFFSET, SYSDATETIMEOFFSET()) <> 540
    THROW 50023, N'Rebuild: DB 서버 시각이 KST(+09:00) 가 아닙니다.', 1;

-- 50024  버전 하한
IF CONVERT(INT, SERVERPROPERTY('ProductMajorVersion')) < 11
    THROW 50024, N'Rebuild: SQL Server 2012(11.x) 이상이 필요합니다.', 1;

IF DB_ID(N'HealthCheckupReservationReceptionDb') IS NOT NULL
BEGIN
    PRINT N'INFO 기존 Database 를 Drop 합니다: HealthCheckupReservationReceptionDb';
    ALTER DATABASE [HealthCheckupReservationReceptionDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [HealthCheckupReservationReceptionDb];
END

CREATE DATABASE [HealthCheckupReservationReceptionDb] COLLATE Korean_Wansung_CI_AS;
PRINT N'PASS RBD-CREATE Database 생성 완료';
GO
