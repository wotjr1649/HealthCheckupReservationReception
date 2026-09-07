# Stage 9 — Security

**Index:** `2026-09-04-phase4-database-implementation.md`
**Spec:** `../06_DB_Transaction_Security_Seed_CANDIDATE.md` §32 · §39
**Tasks:** `T31` ~ `T32`

---

## Task T31: `deploy/08_Security.sql` — Role · User · GRANT 15건

**목적:** 애플리케이션이 15개 SP만 실행할 수 있고 테이블·TVF·Sequence에는 접근할 수 없게 만든다 (G12).

**관련 Baseline 위치:** `04` §1.3, `05` §1.4, 스펙 §32.

**선행조건:** `T30` 완료 (15개 SP가 전부 존재해야 `GRANT` 가 성공한다).

**Files:**
- Create: `deploy/08_Security.sql`

**Interfaces:**
- Produces: `[HC_APP_ROLE]` (Database Role), `[HC_APP_TEST]` (User WITHOUT LOGIN, role 멤버), SP 15건 `GRANT EXECUTE`

**금지사항:**
- **Login·Password·Credential 을 만들지 않는다.** 서버 수준 변경이며 Phase 5 배포 시 사용자가 결정한다.
- `GRANT EXECUTE ON SCHEMA::dbo` 같은 일괄 부여를 쓰지 않는다 — 향후 추가 객체에 자동 권한이 생긴다.
- 테이블·TVF·Sequence 에 어떤 권한도 주지 않는다.
- `TRUSTWORTHY ON`, `EXECUTE AS OWNER`, `sysadmin` 부여를 쓰지 않는다. 기본 소유권 체인으로 충분하다.
- `DENY` 를 쓰지 않는다 — 권한을 주지 않으면 기본이 거부다.

- [ ] **Step 1: `deploy/08_Security.sql` 작성 (UTF-8 with BOM)**

```sql
SET NOCOUNT ON;
PRINT '--- 08_Security 시작 ---';
GO
-- 재실행 가능하도록 역순 정리
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HC_APP_TEST' AND type = 'S')
    DROP USER [HC_APP_TEST];
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'HC_APP_ROLE' AND type = 'R')
    DROP ROLE [HC_APP_ROLE];
GO
CREATE ROLE [HC_APP_ROLE];
GO
GRANT EXECUTE ON [dbo].[USP_HC_SELECT_공통업무상태]   TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_SELECT_수검자목록]     TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_SELECT_수검자상세]     TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_INSERT_수검자]         TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_UPDATE_수검자정보]     TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_SELECT_수검자유효업무] TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_SELECT_예약가능정보]   TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_INSERT_예약]           TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_UPDATE_예약변경]       TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_UPDATE_예약취소]       TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_SELECT_예약접수목록]   TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_SELECT_예약접수상세]   TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_UPDATE_접수완료]       TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_UPDATE_접수추가검사]   TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_UPDATE_접수취소]       TO [HC_APP_ROLE];
GO
CREATE USER [HC_APP_TEST] WITHOUT LOGIN;
ALTER ROLE [HC_APP_ROLE] ADD MEMBER [HC_APP_TEST];
GO
PRINT 'PASS SEC-DEPLOY Role/User/GRANT '
    + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM sys.database_permissions dp
                            JOIN sys.database_principals pr ON pr.principal_id = dp.grantee_principal_id
                           WHERE pr.name = N'HC_APP_ROLE' AND dp.permission_name = 'EXECUTE'
                             AND dp.state = 'G'))
    + '건';
GO
```

- [ ] **Step 2: 실행**

```bash
head -c 3 deploy/08_Security.sql | od -An -tx1
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i deploy/08_Security.sql -o artifacts/logs/08_security.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/08_security.log | tail -3
```

Expected: exit 0, `PASS SEC-DEPLOY Role/User/GRANT 15건`.

15가 아니면 SP 이름 오타를 확인한다. 한글 객체명이 BOM 문제로 깨졌을 수 있으니 `Msg 15151`(개체를 찾을 수 없음)을 먼저 살핀다.

- [ ] **Step 3: 재실행 가능성 확인**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/08_Security.sql
echo "exit=$?"
```

Expected: exit 0. `DROP USER`/`DROP ROLE` 선행 덕분에 두 번째 실행도 성공한다.

- [ ] **Step 4: `CREATE OR ALTER` 가 권한을 유지하는지 확인**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u -i deploy/04_Procedures_Select.sql
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W -Q "
SELECT 'grants=' + CONVERT(varchar(5), COUNT(*))
FROM sys.database_permissions dp
JOIN sys.database_principals pr ON pr.principal_id = dp.grantee_principal_id
WHERE pr.name = N'HC_APP_ROLE' AND dp.permission_name='EXECUTE' AND dp.state='G';"
```

Expected: `grants=15` — `CREATE OR ALTER` 로 SP를 다시 배포해도 `GRANT` 가 살아남는다. `DROP`+`CREATE` 였다면 15 − 7 = **8건**으로 줄었을 것이다. 이것이 `D4-004b` 를 채택한 실질적 이유다.

- [ ] **Step 5: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/08_Security.sql
git commit -m "feat(phase4): Database Role 및 15개 SP GRANT EXECUTE 추가"
```

**회귀시험:** `T32`

**로그 경로:** `artifacts/logs/08_security.log`

**Rollback/Cleanup:** 재실행이 곧 롤백이다.

**완료조건:** `GRANT` 15건, 재실행 성공, SP 재배포 후에도 15건 유지.

---

## Task T32: `tests/13_Security_Tests.sql` — 권한 경계 10건

**목적:** 애플리케이션 principal이 SP만 실행할 수 있고 나머지는 전부 거부되는지 실증한다.

**관련 Baseline 위치:** 스펙 §39, 인계문서 §12.7.

**선행조건:** `T31` 완료.

**Files:**
- Create: `tests/13_Security_Tests.sql`

**Interfaces:**
- Produces: `PASS SEC-001`~`PASS SEC-009`, `PASS SEC-011` (SQL) / `PASS SEC-010` (셸, 별도 스크립트) — 스펙 §45.2

**금지사항:** 동적 SQL(`EXEC()`)은 **이 파일에만** 존재한다. production SP에는 절대 넣지 않는다. `dbo`/`sysadmin` 컨텍스트에서 실행한 결과를 앱 권한 테스트로 오인하지 않는다.

- [ ] **Step 1: RED — 컨텍스트 전환이 실제로 권한을 제한하는지 먼저 확인**

```sql
SET NOCOUNT ON;
DECLARE @Fail INT = 0;
DECLARE @UserName SYSNAME, @IsSa INT;

EXECUTE AS USER = 'HC_APP_TEST';
    SELECT @UserName = USER_NAME(), @IsSa = ISNULL(IS_SRVROLEMEMBER('sysadmin'), 0);
REVERT;

PRINT 'INFO EXECUTE AS 컨텍스트: ' + @UserName + ' / sysadmin=' + CONVERT(VARCHAR(5), @IsSa);

IF @UserName = N'HC_APP_TEST'
    PRINT 'PASS SEC-001 EXECUTE AS USER 컨텍스트 전환 확인';
ELSE BEGIN PRINT 'FAIL SEC-001 컨텍스트 전환 실패'; SET @Fail += 1; END

IF @IsSa = 0
    PRINT N'PASS SEC-002 앱 컨텍스트에서 sysadmin 아님 — dbo 오인 배제';
ELSE BEGIN PRINT 'FAIL SEC-002 sysadmin 권한이 남아 있어 보안 테스트가 무의미함'; SET @Fail += 1; END
```

`SEC-002` 가 FAIL이면 **이후 모든 보안 테스트가 무의미하므로 즉시 중단**하고 원인을 조사한다.

- [ ] **Step 2: 거부되어야 하는 접근 검증 (`SEC-004`~`SEC-007`)**

**대상 목록을 `EXECUTE AS` 밖에서 확정한다.** SQL Server 2005 이후 **메타데이터 가시성** 규칙상 권한 없는 주체에게 `sys.tables` 는 **0행**이다(실측: `sys.tables 보이는 개수 = 0`). 초안처럼 impersonation 안에서 커서를 열면 한 번도 돌지 않아 `@Total=0` 이 되고, 판정식에 따라 무조건 FAIL 하거나 **아무것도 시험하지 않고 PASS** 한다. 후자가 더 위험하다.

**`CURSOR` 도 쓰지 않는다**(허용목록 §9.2). `WHILE` + `MIN(Name) > @Prev` 로 순회한다.

```sql
-- (1) dbo 컨텍스트에서 목록 확정 — SCH-002 가 이미 고정한 7개 이름과 동일하다
DECLARE @Obj TABLE (Kind VARCHAR(6), Name SYSNAME, PRIMARY KEY (Kind, Name));
INSERT INTO @Obj VALUES
 ('TABLE', N'수검자'), ('TABLE', N'예약접수'), ('TABLE', N'검사항목'),
 ('TABLE', N'검사코드'), ('TABLE', N'휴무일'),
 ('TABLE', N'완료이력'), ('TABLE', N'변경이력');

DECLARE @Denied INT = 0, @Total INT = 0, @Prev SYSNAME = N'', @T SYSNAME;

EXECUTE AS USER = 'HC_APP_TEST';

WHILE 1 = 1
BEGIN
    SELECT @T = MIN(Name) FROM @Obj WHERE Kind = 'TABLE' AND Name > @Prev;
    IF @T IS NULL BREAK;
    SET @Prev = @T; SET @Total += 1;
    BEGIN TRY
        EXEC(N'SELECT TOP (1) * FROM [dbo].[' + @T + N']');
        -- 성공하면 결함이다. @Denied 를 올리지 않는다.
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() = 229 SET @Denied += 1;              -- 권한 거부만 인정
        ELSE PRINT 'FAIL SEC-004 예상하지 못한 오류 ' + CONVERT(VARCHAR(10), ERROR_NUMBER()) + ' on ' + @T;
    END CATCH
END

REVERT;

IF @Denied = 6 AND @Total = 6
    PRINT 'PASS SEC-004 6개 테이블 직접 SELECT 전부 Msg 229 거부';
ELSE BEGIN PRINT 'FAIL SEC-004 거부 ' + CONVERT(VARCHAR(5), @Denied) + '/' + CONVERT(VARCHAR(5), @Total); SET @Fail += 1; END
```

`[X]` **`CATCH` 에 걸리기만 하면 "권한 거부"로 세면 안 된다.** 객체명 오타·형변환 오류도 PASS 가 된다. **`ERROR_NUMBER() = 229` 만** 인정한다.

`[X]` **`SEC-005`(DML)는 명시적 Transaction 안에서 시도하고 성공 여부와 무관하게 `ROLLBACK` 한다.** 권한이 잘못 부여된 상황이 바로 검수 대상인데, 그때 `INSERT`/`UPDATE`/`DELETE` 가 성공하면 초안에는 롤백이 없어 실제 데이터가 바뀐다. 전후 행수 지문도 비교한다.

```sql
-- SEC-005  6개 테이블 × INSERT/UPDATE/DELETE = 18건 전부 거부
--          권한이 잘못 부여됐다면 여기서 실제로 쓰이므로 트랜잭션으로 감싼다.
DECLARE @Stmt NVARCHAR(400), @Tbl SYSNAME, @Col SYSNAME, @Op VARCHAR(10), @Prev SYSNAME = N'';
DECLARE @D5 INT = 0, @T5 INT = 0;

-- 테이블별 실존 컬럼 하나씩. EXECUTE AS 밖에서 확보한다 — 안에서는 메타데이터 가시성 때문에 0행이다.
DECLARE @ObjC TABLE (T SYSNAME, C SYSNAME, PRIMARY KEY (T, C));
INSERT INTO @ObjC (T, C)
SELECT t.name, c.name
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
WHERE t.is_ms_shipped = 0 AND c.is_computed = 0
  AND TYPE_NAME(c.system_type_id) <> 'timestamp';   -- ROWVERSION 은 UPDATE 대상이 될 수 없다

BEGIN TRAN;
EXECUTE AS USER = 'HC_APP_TEST';
WHILE 1 = 1
BEGIN
    SELECT @Tbl = MIN(N) FROM @ObjT WHERE N > @Prev;   -- 목록은 EXECUTE AS 밖에서 확정했다
    IF @Tbl IS NULL BREAK;
    SET @Prev = @Tbl;

    SELECT @Op = 'INSERT';
    WHILE @Op IS NOT NULL
    BEGIN
        -- [X] UPDATE 의 컬럼명에 테이블명을 넣으면 안 된다. 실측: 없는 컬럼은 컴파일 단계에서
        --     Msg 207 이 나고 이는 실행 시점 권한검사(Msg 229)보다 **먼저** 발생한다
        --     (없는 테이블 → Msg 208, 있는 테이블 + 없는 컬럼 → Msg 207 로 실측 확인).
        --     229 만 거부로 세므로 권한이 올바로 막혀 있어도 SEC-005 가 FAIL 한다.
        --     → 실존 컬럼명을 쓴다. 목록은 EXECUTE AS **밖**에서 확보했다(@ObjC).
        SET @Col = (SELECT TOP (1) C FROM @ObjC WHERE T = @Tbl ORDER BY C);
        SET @Stmt = CASE @Op
            WHEN 'INSERT' THEN N'INSERT INTO [dbo].[' + @Tbl + N'] DEFAULT VALUES;'
            WHEN 'UPDATE' THEN N'UPDATE [dbo].[' + @Tbl + N'] SET [' + @Col + N'] = [' + @Col + N'] WHERE 1 = 0;'
            ELSE               N'DELETE FROM [dbo].[' + @Tbl + N'] WHERE 1 = 0;' END;
        SET @T5 += 1;
        BEGIN TRY EXEC (@Stmt); END TRY
        BEGIN CATCH IF ERROR_NUMBER() = 229 SET @D5 += 1; END CATCH
        SET @Op = CASE @Op WHEN 'INSERT' THEN 'UPDATE' WHEN 'UPDATE' THEN 'DELETE' ELSE NULL END;
    END
END
REVERT;
IF @@TRANCOUNT > 0 ROLLBACK;

IF @D5 = 21 AND @T5 = 21 PRINT 'PASS SEC-005 DML 21건 전부 Msg 229 거부';
ELSE BEGIN PRINT 'FAIL SEC-005 거부 ' + CONVERT(VARCHAR(5), @D5) + '/' + CONVERT(VARCHAR(5), @T5); SET @Fail += 1; END

-- SEC-006  4개 TVF 직접 SELECT 거부
DECLARE @D6 INT = 0;
EXECUTE AS USER = 'HC_APP_TEST';
    BEGIN TRY SELECT TOP (1) 1 FROM [dbo].[UFN_HC_일정확인](SYSDATETIME(), '2026-11-16', 'AM', 'NONE'); END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @D6 += 1; END CATCH
    BEGIN TRY SELECT TOP (1) 1 FROM [dbo].[UFN_HC_검진대상확인](1, '2026-11-16'); END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @D6 += 1; END CATCH
    BEGIN TRY SELECT TOP (1) 1 FROM [dbo].[UFN_HC_국가검사구성](1, '2026-11-16'); END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @D6 += 1; END CATCH
    BEGIN TRY SELECT TOP (1) 1 FROM [dbo].[UFN_HC_추가검사확인](1, '2026-11-16'); END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @D6 += 1; END CATCH
REVERT;

IF @D6 = 4 PRINT 'PASS SEC-006 4개 TVF 직접 SELECT 전부 Msg 229 거부';
ELSE BEGIN PRINT 'FAIL SEC-006 거부 ' + CONVERT(VARCHAR(5), @D6) + '/4'; SET @Fail += 1; END

-- SEC-007  Sequence 직접 소비 거부
DECLARE @D7 INT = 0;
EXECUTE AS USER = 'HC_APP_TEST';
    BEGIN TRY
        DECLARE @Seq BIGINT = NEXT VALUE FOR [dbo].[SEQ_HC_CHART_NO];
    END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @D7 += 1; END CATCH
REVERT;

IF @D7 = 1 PRINT 'PASS SEC-007 Sequence 직접 소비 Msg 229 거부';
ELSE BEGIN PRINT 'FAIL SEC-007 거부되지 않았다'; SET @Fail += 1; END
```

`[I]` `SEC-005` 의 `UPDATE … SET [테이블명] = NULL WHERE 1 = 0` 은 **컬럼이 존재하지 않아도 무방하다** — 권한 검사가 바인딩보다 먼저 일어나므로 권한이 없으면 `229`, 있으면 `207`(잘못된 열 이름)이 난다. `229` 만 거부로 세므로 `207` 은 **거부 실패**로 집계되어 정확히 우리가 원하는 판정이 된다.

`EXEC()` 동적 SQL을 쓰는 이유는 컴파일 시점이 아니라 **실행 시점에 권한 검사**가 일어나게 하기 위해서다.

- [ ] **Step 3: 허용되어야 하는 접근 검증 (`SEC-003`)**

`[X]` **빈 `CATCH` 는 권한거부(`229`)와 업무 `THROW`(`50001` 등)를 구분하지 못한다.** `SEC-004`~`SEC-007` 에는 `ERROR_NUMBER() = 229` 판정을 강제해 놓고 `SEC-003` 만 빈 `CATCH` 였다 — 같은 오판의 거울상이다. **`229` 만 실패로 센다.**

`[X]` **Write SP 8개를 실호출하므로 트랜잭션으로 감싸고 무조건 `ROLLBACK` 한다.** `SEC-005` 에 요구한 것과 같은 이유다 — 권한이 잘못 부여된 상황이 검수 대상인데, 그때 DML 이 성공하면 실데이터가 바뀐다.

```sql
DECLARE @Ok INT = 0, @Denied229 INT = 0;

BEGIN TRAN;   -- Write SP 8개가 실제로 쓸 수 있으므로 무조건 되돌린다
EXECUTE AS USER = 'HC_APP_TEST';

    BEGIN TRY EXEC [dbo].[USP_HC_SELECT_공통업무상태]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_SELECT_수검자목록]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_SELECT_수검자상세]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_SELECT_수검자유효업무]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_SELECT_예약가능정보]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_SELECT_예약접수목록]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_SELECT_예약접수상세]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_INSERT_수검자]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_UPDATE_수검자정보]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_INSERT_예약]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_UPDATE_예약변경]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_UPDATE_예약취소]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_UPDATE_접수완료]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_UPDATE_접수추가검사]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH
    BEGIN TRY EXEC [dbo].[USP_HC_UPDATE_접수취소]; SET @Ok += 1; END TRY
    BEGIN CATCH IF ERROR_NUMBER() = 229 SET @Denied229 += 1; ELSE SET @Ok += 1; END CATCH

REVERT;
IF @@TRANCOUNT > 0 ROLLBACK;   -- 성공했든 실패했든 데이터는 남기지 않는다

IF @Ok = 15 AND @Denied229 = 0
    PRINT 'PASS SEC-003 15개 SP 전부 실행 가능 (Msg 229 0건)';
ELSE BEGIN
    PRINT 'FAIL SEC-003 실행가능 ' + CONVERT(VARCHAR(5), @Ok) + '/15, 권한거부 ' + CONVERT(VARCHAR(5), @Denied229);
    SET @Fail += 1;
END
```

`[I]` 인자를 주지 않으므로 대부분 `100 Required` 로 끝난다. **그것은 실행에 성공한 것**이다 — 이 시험은 `EXECUTE` 권한만 본다. 인자 조합별 동작은 `tests/04`~`07` 과 `tests/contract/` 의 몫이다.

Write SP는 인자 조합에 따라 업무 실패(`100`/`500` 등)를 반환할 수 있으나 **그것은 실행에 성공한 것**이다. 권한 오류(`Msg 229`)만 실패로 센다.

- [ ] **Step 4: 나머지 3건**

```sql
-- SEC-008 Ownership chaining: SEC-003 이 성공했다는 사실 자체가 증명이다
IF @Ok = 15 PRINT 'PASS SEC-008 소유권 체인으로 SP 내부 테이블 접근 성공';
ELSE BEGIN PRINT 'FAIL SEC-008'; SET @Fail += 1; END

-- SEC-009 TRUSTWORTHY OFF
IF ((SELECT is_trustworthy_on FROM sys.databases WHERE name = DB_NAME()) = 0)
    PRINT 'PASS SEC-009 TRUSTWORTHY OFF';
ELSE BEGIN PRINT 'FAIL SEC-009 TRUSTWORTHY 가 켜져 있음'; SET @Fail += 1; END

-- SEC-011 Login 미생성 확인   (스펙의 SEC-010 은 secret 검사다 — ID 를 분리했다)
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name LIKE 'HC[_]APP%')
    PRINT 'PASS SEC-011 서버 Login 을 만들지 않음';
ELSE BEGIN PRINT 'FAIL SEC-011 예상하지 않은 Login 존재'; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'보안 테스트 실패', 1;
PRINT '=== 13_Security_Tests 완료 ===';
GO
```

`[X]` **`SEC-010` ID 를 되돌린다.** 스펙 §39.3의 `SEC-010` 은 *"배포 산출물·로그에 비밀번호·연결 secret 0건"* 이다. 초안은 이 ID 를 "Login 미생성"에 붙여 secret 검사를 자동 판정에서 밀어냈고, Gate 추적성이 끊겼다. Login 검사는 `SEC-011` 로 옮긴다.

- [ ] **Step 5: GREEN 실행**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/13_Security_Tests.sql -o artifacts/logs/test_13.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_13.log | grep -E '^(PASS|FAIL|INFO)'
```

Expected: exit 0, SQL 소관 `SEC-001`~`SEC-009`·`SEC-011` PASS + `INFO EXECUTE AS 컨텍스트: HC_APP_TEST / sysadmin=0`.
(`SEC-010` 은 SQL 이 아니라 `scripts/verify-no-secret.sh` 가 판정한다 — 스펙 §45.2)

- [ ] **Step 6: 로그에 secret 이 없는지 확인**

**`SEC-010` 을 자동 판정으로 만든다.** 초안은 결과를 눈으로 보라고만 해서 Gate 증거가 되지 못했다.

`[X]` **`grep -q` 를 파이프 우변에 두면 안 된다.** `-q` 는 첫 매치에서 즉시 종료하고, 그러면 좌변 `iconv` 가 `SIGPIPE` 로 **141** 을 반환한다. `pipefail` 이 그 141 을 파이프라인 상태로 삼아 `if` 가 **거짓**이 된다 — **secret 이 있는데 발견하지 못하고 `PASS SEC-010` 을 찍는다.**

실측(BOM 포함 UTF-16 로그 7.6MB, 패턴 1건):

| secret 위치 | `grep -q` | `grep -icE` |
|---|---|---|
| 파일 끝 | `HIT` | 1 |
| **파일 앞** | **`MISS`** ← 놓친다 | 1 |
| 없음 | `MISS` | 0 |

secret 이 앞에 있을수록 놓친다. 보안 스캔이 최악 방향으로 실패한다. **개수를 변수로 받아 판정한다.**

`[X]` **검사 범위도 스펙 §39.3 과 달랐다.** 스펙은 *"배포 산출물·로그"* 인데 초안은 `artifacts/` 만 돌았다 — `deploy/*.sql`·`Deploy.sql`·`Rebuild.sql`·`scripts/` 에 값이 있어도 PASS 한다. 반대로 이 스크립트 자신과 자기 PASS 문구는 제외해야 자기검출을 피한다.

```bash
#!/usr/bin/env bash
# scripts/verify-no-secret.sh  — SEC-010. test.sh 와 T37 이 호출한다.
set -uo pipefail
cd "$(dirname "$0")/.."

# 단어 'secret' 단독은 문서·주석에 흔해 오탐이 많다. 구조적 패턴만 본다.
PAT='password[[:space:]]*=|pwd[[:space:]]*=|user[[:space:]]+id[[:space:]]*=|integrated[[:space:]]+security[[:space:]]*=|connection[[:space:]]*string[[:space:]]*=|Data[[:space:]]+Source[[:space:]]*=.*Password'
HITS=0

scan() {   # $1=파일  $2=UTF-16 여부
  local c
  if [ "$2" = "u16" ]; then
    c=$(iconv -f UTF-16 -t UTF-8 "$1" 2>/dev/null | grep -icE "$PAT" || true)
  else
    c=$(grep -icE "$PAT" "$1" || true)
  fi
  # grep -c 는 0건일 때 0 을 출력하고 exit 1 이므로 || true 로 상태만 덮는다. 값은 항상 숫자다.
  if [ "${c:-0}" -ne 0 ]; then
    echo "FAIL SEC-010 secret 패턴 ${c}건: $1"
    HITS=1
  fi
}

# 1) 배포 원본 (스펙 §39.3 "배포 산출물")
for f in Deploy.sql Rebuild.sql deploy/*.sql tests/*.sql tests/contract/*.sql tools/*.js tools/*.json; do
  [ -f "$f" ] || continue
  scan "$f" utf8
done
# 2) 스크립트 — 자기 자신은 PAT 문자열을 담고 있으므로 제외한다
for f in scripts/*.sh; do
  [ -f "$f" ] || continue
  case "$f" in */verify-no-secret.sh) continue ;; esac
  scan "$f" utf8
done
# 3) 로그 (sqlcmd -u → BOM 포함 UTF-16)
for f in artifacts/logs/*.log; do [ -f "$f" ] || continue; scan "$f" u16; done
# 4) 보고서 — 자기 PASS/FAIL 문구가 재실행에서 자기검출되지 않도록 SEC-010 줄을 뺀다
for f in artifacts/reports/*; do
  [ -f "$f" ] || continue
  c=$(grep -v 'SEC-010' "$f" | grep -icE "$PAT" || true)
  if [ "${c:-0}" -ne 0 ]; then echo "FAIL SEC-010 secret 패턴 ${c}건: $f"; HITS=1; fi
done

[ "$HITS" -eq 0 ] && echo "PASS SEC-010 배포 원본·로그·보고서에 secret 0건"
exit $HITS
```

```bash
chmod +x scripts/verify-no-secret.sh
./scripts/verify-no-secret.sh
echo "exit=$?"
```

Expected: `PASS SEC-010 로그·보고서에 secret 0건`, `exit=0`.

- [ ] **Step 7: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/tests/13_Security_Tests.sql
git commit -m "test(phase4): 권한 경계 검증 10건 추가"
```

**회귀시험:** `tests/13_Security_Tests.sql`

**로그 경로:** `artifacts/logs/test_13.log`

**완료조건:** 스펙 §45.2 의 `SEC` 중 SQL 소관 전건(`SEC-001`~`009`, `011`) PASS + `scripts/verify-no-secret.sh` 가 `SEC-010` PASS.

`[X]` **`SEC-002`(sysadmin=0)가 FAIL 이면 그 자리에서 `THROW` 한다.** 초안은 *"즉시 중단"* 이라고 써 놓고 코드는 `@Fail` 만 올리고 계속했다. sysadmin 컨텍스트에서 나온 `SEC-003`~`SEC-011` PASS 는 **전부 무효**인데 로그에는 PASS 로 남아 G12 판정을 오염시킨다.

```sql
IF IS_SRVROLEMEMBER('sysadmin') <> 0
BEGIN
    REVERT;   -- 컨텍스트를 먼저 되돌린다
    THROW 51000, N'SEC-002 실패: sysadmin 컨텍스트에서는 권한 경계를 검증할 수 없습니다. 이후 결과는 무효입니다.', 1;
END
```
