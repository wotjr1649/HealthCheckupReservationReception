SET NOCOUNT ON;

-- 50020  호스트 + 인스턴스
-- [X] InstanceName 만으로는 다른 PC 의 .\SQLEXPRESS 를 구별하지 못한다. DROP DATABASE 직전이므로 호스트를 함께 본다.
IF ISNULL(CONVERT(SYSNAME, SERVERPROPERTY('InstanceName')), N'') <> N'SQLEXPRESS'
   OR ISNULL(CONVERT(SYSNAME, SERVERPROPERTY('MachineName')), N'') <> N'DESKTOP-DP7KRE4'
    THROW 50020, N'Rebuild: 승인되지 않은 호스트 또는 인스턴스입니다.', 1;

-- 50021  master 컨텍스트에서만 실행
IF DB_NAME() <> N'master'
    THROW 50021, N'Rebuild: master 컨텍스트에서 실행해야 합니다.', 1;

-- 50022  기존 대상 DB 가 04 §8 계약 집합과 정확히 같은지 확인한다
-- [X] 초안은 'database_id <= 4' 였다. 사용자 DB 는 database_id 가 항상 5 이상이므로(실측: Net461MvpSample = 5)
--     고정된 비시스템 DB 이름에 대해 이 조건은 항상 거짓이다 — 절대 발화하지 않는 죽은 코드였다.
--     DROP DATABASE 직전에 실제로 확인해야 하는 것은 "그 DB 가 정말 Phase 4 DB 인가" 다.
-- [R3] 정확 집합 판정이라 R3 이름과 R2 이름이 섞인 반쯤 마이그레이션된 DB 도 막는다 —
--      기존 NOT IN 형태는 "목록 밖 0개" 만 봐서 그것을 통과시켰다.
--      R2 → R3 전환에 쓴 이중 가드의 R2 집합 블록은 전환 완료(2026-09-04) 후 제거했다.
--      t.name IN (리터럴) 은 컬럼 대 리터럴 비교라 서버 정렬이 달라도 Msg 468 이 나지 않는다.
IF DB_ID(N'HealthCheckupReservationReceptionDb') IS NOT NULL
BEGIN
    DECLARE @Total INT, @R3 INT;

    -- 동적 SQL 을 쓰지 않는다. 대상 DB 이름은 대괄호 하드코딩 3부 이름으로만 참조한다.
    SELECT @Total = COUNT(*)
    FROM [HealthCheckupReservationReceptionDb].[sys].[tables] t
    WHERE t.is_ms_shipped = 0;

    SELECT @R3 = COUNT(*)
    FROM [HealthCheckupReservationReceptionDb].[sys].[tables] t
    WHERE t.is_ms_shipped = 0
      AND t.name IN (N'수검자', N'예약접수', N'검사항목', N'검사코드',
                     N'휴무일', N'완료이력', N'변경이력');

    -- 빈 DB(배포 실패 잔해) 또는 정확한 R3 집합만 허용한다
    IF NOT (@Total = 0 OR (@Total = 7 AND @R3 = 7))
        THROW 50022, N'Rebuild: 대상 DB 가 Phase 4 계약 집합과 다릅니다. 동명 DB 를 삭제하려는 것일 수 있습니다.', 1;
    PRINT N'INFO 50022 가드 통과 — Total=' + CONVERT(NVARCHAR(5), @Total)
        + N' R3=' + CONVERT(NVARCHAR(5), @R3);
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
