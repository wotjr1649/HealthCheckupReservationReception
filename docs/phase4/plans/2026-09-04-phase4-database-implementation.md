# Phase 4 Database Implementation Plan — Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `HealthCheckupReservationReceptionDb`에 7개 물리 테이블·4개 Inline TVF·15개 Stored Procedure를 배포하고, 8개 Write SP의 Transaction·잠금·동시성·권한·Seed·테스트를 실행 증거와 함께 완성한다.

**Architecture:** 배포는 clean-create 방식이다 — `01_Schema.sql`이 FK 역순 `DROP IF EXISTS` 후 `CREATE`하므로 `Deploy.sql`은 항상 재실행 가능하고 항상 동일한 결과를 만든다. 동시성은 `sp_getapplock`으로 논리 자원(SSN/CHART/PAT/WORK/SLOT)을 전역 순서대로 직렬화하고, 행 상태·동시성은 기대상태·`RowVersion` 조건부 `UPDATE` + `@@ROWCOUNT`로 보장한다. 시간에 의존하는 Rule은 `@ServerTime`을 파라미터로 받는 Inline TVF로 결정적으로 시험하고, Write SP는 실제 서버시각으로 통합시험한다.

**Tech Stack:** Microsoft SQL Server 2025 Express (`.\SQLEXPRESS`, `17.0.1125.2`) / T-SQL / `sqlcmd 15.0.1300.359` / Git Bash / `node v24.19.0` (표준 라이브러리만) / `git 2.55.0`

**Spec:** `../06_DB_Transaction_Security_Seed_CANDIDATE.md` (v0.4, `CANDIDATE / IMPLEMENTATION READY`)

**Test ID 는 이 계획에 없다.** 스펙 **§45.2 카탈로그**가 유일한 출처다(234건). 완료조건은 건수를 다시 적지 않고 *"§45.2 의 `SEL` 전건 PASS"* 로 쓴다. `node tools/verify-docs.js` 가 카탈로그↔배치를 양방향 대조한다(§45.3).

---

## Global Constraints

모든 Task의 요구사항에 아래가 암묵적으로 포함된다. 값은 스펙에서 그대로 복사했다.

```text
기준선 ID            HC-RSV-RCP-20260904-R3
Database             HealthCheckupReservationReceptionDb
Instance             .\SQLEXPRESS   (SQL Server 2025 Express 17.0.1125.2)
Collation            Korean_Wansung_CI_AS   (CREATE DATABASE 에 명시 고정)
Compatibility Level  설정하지 않음 (model 상속 = 170). Preflight 가 실측값만 기록
KST                  DATEPART(TZOFFSET, SYSDATETIMEOFFSET()) = 540
.sql 파일 인코딩      UTF-8 with BOM   (BOM 없으면 sqlcmd 가 한글 객체명을 깨뜨림 — 실측 확인)
로그 출력             sqlcmd -u  (UTF-16LE)
exit code             sqlcmd -b -I  → 오류 시 1
applock Timeout      5000 ms,  @LockMode='Exclusive',  @LockOwner='Transaction'
applock rc 로깅       PRINT 'INFO applock rc=' + rc 만. 자원명(@Res) 출력 금지 — 개인정보
applock rc = -3       ROLLBACK 후 THROW 50002   (deadlock victim)
applock rc < 0 기타   ROLLBACK 후 THROW 50001   (timeout / 취소 / 호출오류)
Preflight 가드 위반   THROW 50010 ~ 50015   (00_Preflight.sql, 대상 DB 컨텍스트)
Rebuild 가드 위반     THROW 50020 ~ 50024   (Rebuild.sql, master 컨텍스트)
테스트 파일 실패      THROW 51000
barrier 시각 경과     THROW 51001
정원                  Capacity = 20  (RSV + RCP, CNR·CNC 제외)
NEX Cardinality      TGT 대상 8~11행 / 비대상 0행
AEX                   OPT01~OPT07 7개 BIT, NULL 불허, 0개 이상 선택 허용
ResultCode Catalog    정확히 38개. 새 코드를 추가하지 않는다
RS0                   Success BIT / Code INT / Message NVARCHAR(300) / Field VARCHAR(50) / ServerTime DATETIME2(7)
Rule Test 기준 예약일  2026-10-01
휴무일 Seed           2026-12-25(금, 평일 휴무) / 2026-12-26(토, 토요일 휴무)
요일 계산             DATEDIFF(DAY, 0, @d) % 7    (0=월 … 5=토, 6=일)
만 나이               DATEDIFF(YEAR,@b,@d) - CASE WHEN (MONTH(@d)*100+DAY(@d)) < (MONTH(@b)*100+DAY(@b)) THEN 1 ELSE 0 END
Patient 동시성        수검자.LastEditDate DATETIME. 증가하지 않으면 DATEADD(MILLISECOND, 4, @Old)
Work 동시성           예약접수.RowVersion BINARY(8)
```

### 절대 금지 (모든 Task 공통)

```text
00~05 기준선 파일 수정·이동·삭제
../winforms/** 수정 (읽기만 허용)
Table / Column / SP / TVF / Parameter / Result Set / ResultCode 계약 변경
추가 물리 테이블, TVP, Trigger, DELETE SP, 범용 Rule Engine, 공통 코드 테이블
STRING_SPLIT / XML(FOR XML PATH 포함) / JSON / CSV / 비트마스크 / FK Cascade
CURSOR                              ← 스펙 §9.2 허용목록에 없다. WHILE 로 대체
INSERT ... EXEC <SP>                ← 테스트에서 금지. 아래 "테스트 작성 규칙" 참조
2012 이후 T-SQL 기능 (예외: 배포 배관의 CREATE OR ALTER, DROP … IF EXISTS)
sa 사용, sysadmin 부여, TRUSTWORTHY ON, xp_cmdshell, Ad Hoc Distributed Queries, CLR, Linked Server
Net461MvpSample 및 master/model/msdb/tempdb 에 대한 모든 변경
production SP 안의 동적 SQL, 테스트용 시간 주입 backdoor
실제 주민등록번호 사용
실행하지 않은 검증을 PASS 로 기록하는 것
SKIP 을 PASS 로 승격하는 것
개수 비교(COUNT)를 "전건 일치" 증거로 쓰는 것   ← EXCEPT 양방향을 쓴다
```

---

## Stage → 파일 매핑

| Stage | 내용 | 파일 | Task |
|---:|---|---|---|
| 0 | Baseline·Repository 보호 | `01-preflight-schema.md` | `T01`~`T03` |
| 1 | Preflight·배포 골격 | `01-preflight-schema.md` | `T04`~`T05` |
| 2 | Physical Schema | `01-preflight-schema.md` | `T06`~`T07` |
| 3 | Seed·Test Fixture (TVF 무관) | `02-seed-functions.md` | `T08`~`T10` |
| 4 | Rule TVF 4개 + RCP Fixture | `02-seed-functions.md` | `T11`~`T14`, **`T14b`** |
| 5 | SELECT SP 7개 + 계약 검증기 | `03-select-procedures.md` | `T15`~`T22` |
| 6 | Patient Write SP 2개 | `04-patient-write-procedures.md` | `T23`~`T24` |
| 7 | 예약 Write SP 3개 | `05-reservation-write-locking.md` | `T25`~`T27` |
| 8 | 접수 Write SP 3개 | `06-reception-write-procedures.md` | `T28`~`T30` |
| 9 | Security | `07-security.md` | `T31`~`T32` |
| 10 | Rollback·Concurrency | `08-verification-finalization.md` | `T33`~`T34` |
| 11 | Clean Rebuild | `08-verification-finalization.md` | `T35`~`T36` |
| 12 | 문서 최종화 | `08-verification-finalization.md` | `T37` |

Task ID는 전체 파일에서 유일하다. **순서대로 실행한다.**

`[X 수정]` **`T10` 은 TVF 에 의존하지 않는 Fixture 만 만든다.** RCP 상태 Work(`T29` 가 필요)는 `UFN_HC_국가검사구성` 을 쓰므로 `T14b` 에서 **별도 파일 `tests/00b_Test_Harness_RCP.sql`** 로 만든다. 초안은 이를 `T10` 안에 `IF OBJECT_ID(...)` 로 감싸려 했는데, 해당 블록에 `GO` 가 3개 있어 **단일 `IF … BEGIN … END` 로 감쌀 수 없다**(`GO` 는 배치 구분자다). 파일을 나누면 이 문제가 사라진다.

*(참고: `IF OBJECT_ID` 가드 자체는 미존재 TVF 참조에서도 정상 동작함을 실측 확인했다. 문제는 가드가 아니라 `GO` 였다.)*

---

## 의존성

```text
T01 ─ T02 ─ T03            (Repository 보호 — DB 접속 없음)
      │
      └─ T04 ─ T05 ─ T06 ─ T07              Schema
                            │
                            ├─ T08 ─ T09     Master Seed
                            └─ T10           Test Fixture (TVF 무관)
                                  │
                                  ├─ T11 ─ T12 ─ T13 ─ T14 ─ T14b   TVF, 그다음 RCP Fixture
                                          │
                                          └─ T15 … T21       SELECT SP
                                                   │
                                                   ├─ T22    계약 검증기
                                                   ├─ T23 ─ T24   Patient Write
                                                   ├─ T25 ─ T26 ─ T27   예약 Write
                                                   └─ T28 ─ T29 ─ T30   접수 Write
                                                            │
                                                            ├─ T31 ─ T32   Security
                                                            └─ T33 ─ T34 ─ T35 ─ T36 ─ T37
```

`T13`(`UFN_HC_국가검사구성`)은 `T12`(`UFN_HC_검진대상확인`)를 내부에서 호출한다. `T14`(`UFN_HC_추가검사확인`)는 `T13`을 호출한다. `T21`(`SELECT_예약가능정보`)은 4개 TVF를 모두 사용한다.

---

## 공통 실행 규칙

### 파일 저장

모든 `.sql` 파일은 **UTF-8 with BOM**으로 저장한다. 저장 후 반드시 확인한다.

```bash
head -c 3 <파일> | od -An -tx1        # ef bb bf 가 나와야 한다
```

BOM이 없으면 sqlcmd가 한글 객체명을 깨뜨려 `Msg 105/102` 구문오류가 난다(실측 확인).

### 실행 명령 원형 — `set -e` 회피가 필수다

```bash
SRV='.\SQLEXPRESS'
DB='HealthCheckupReservationReceptionDb'

RC=0
sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -i "<파일>" -o "artifacts/logs/<로그>.log" || RC=$?
iconv -f UTF-16 -t UTF-8 "artifacts/logs/<로그>.log" | grep -E '^(PASS|FAIL|SKIP|INFO|Msg )' || true
echo "exit=$RC"
```

`-b` 없이 실행하지 않는다. exit code가 유일한 자동 판정 근거다.

**`-I` 없이 실행하지 않는다 (`SET QUOTED_IDENTIFIER ON`).** `[X]` 초안의 모든 호출에 이 옵션이 빠져 있었다. sqlcmd 는 SSMS 와 달리 `QUOTED_IDENTIFIER` **OFF** 로 접속하는데, 필터형 인덱스(`IX_수검자_CEL_NUMBER_S`·`UX_검사코드_AEX_CODE`)는 생성 시점뿐 아니라 **그 테이블에 대한 모든 `INSERT`/`UPDATE`/`DELETE` 시점에도** ON 을 요구한다. 실측:

```text
CREATE INDEX (필터형)                       → Msg 1934
INSERT INTO 검사코드                  → Msg 1934
INSERT INTO 수검자                   → Msg 1934
```

`T08` Seed 부터 Write SP 시험·동시성 harness 까지 전부 이 벽에 부딪힌다. 배포 `.sql` 파일은 자체적으로도 첫 배치에 `SET QUOTED_IDENTIFIER ON;` + `GO` 를 둔다 — `SET` 은 parse 시점에 적용되므로 같은 배치 안에서는 소급되지 않는다.

**`sqlcmd … ; RC=$?` 는 쓰지 않는다.** `set -euo pipefail` 하에서 sqlcmd가 1을 반환하면 그 줄에서 셸이 즉시 끝나 `RC=$?` 도 로그 출력도 실행되지 않는다 — **정확히 실패했을 때만** 진단이 사라진다(실측 확인). 반드시 `|| RC=$?` 로 받는다.

### 로그 확인 — `iconv -f UTF-16` (LE 아님)

```bash
iconv -f UTF-16 -t UTF-8 artifacts/logs/<로그>.log | grep -E '^(PASS|FAIL|SKIP|INFO|Msg )'
```

**`-f UTF-16LE` 를 쓰지 않는다.** sqlcmd `-u` 출력의 BOM(`FF FE`)이 `EF BB BF` 로 남아 **첫 줄의 `^PASS` 가 매치되지 않는다.** 실측: 같은 파일에서 `-f UTF-16LE` → 1건, `-f UTF-16` → 2건. 모든 PASS 카운트가 1씩 적게 세어진다.

### 파이프와 exit code

```bash
# 금지 — $? 는 tee 의 것(항상 0)이라 회귀 실패가 exit 0 으로 기록된다
./scripts/test.sh 2>&1 | tee log ; echo "exit=$?"

# 사용
./scripts/test.sh > log 2>&1; RC=$?; cat log; echo "exit=$RC"
```

`for … done | tee file` 도 쓰지 않는다. 좌변이 서브셸이라 `|| exit 1` 이 스크립트를 끝내지 못하고, `tee` 는 stdout만 잡아 **stderr 로 나간 FAIL 이 증거 파일에 남지 않는다.**

`grep -c` 는 0건일 때 exit 1을 내므로 `|| true` 로 받는다. `|| echo 0` 은 `"0\n0"` 을 만든다.

### 계약 검증용 sqlcmd 호출

```bash
sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -W -w 65535 -s"|" -i "<시나리오>.sql" -o "artifacts/logs/rs_<NN>.txt"
```

**`-w 65535` 가 없으면 기본 폭 80에서 줄이 접혀 파서가 무너진다**(실측 확인). RS0 한 행만 해도 400칸을 넘는다.

### Assertion 패턴 (모든 테스트 파일 공통)

```sql
DECLARE @Fail INT = 0;

IF (<조건>) PRINT 'PASS <TestId> <설명>';
ELSE BEGIN PRINT 'FAIL <TestId> <설명>'; SET @Fail += 1; END

-- … 반복 …

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
```

파일 끝에서 한 번만 `THROW`한다. 중간에 중단하면 나머지 결과가 보이지 않는다.

### 테스트 작성 규칙 — `INSERT … EXEC` 금지 (스펙 §33.1a)

```text
금지   DECLARE @RS0 TABLE(...);  INSERT INTO @RS0 EXEC [dbo].[USP_HC_...];
```

실측 결과:

| 상황 | 결과 |
|---|---|
| RS가 2개 이상인 SP | **`Msg 213`** — 모든 Result Set을 대상 테이블에 넣으려 한다 |
| SP 내부 `ROLLBACK` | **`Msg 3915`** — INSERT-EXEC 문 내에서는 ROLLBACK 불가 |
| 진입 시 `@@TRANCOUNT` | **1** (평범한 `EXEC` 는 0) — C# 호출과 다른 경로를 시험하게 된다 |

15개 SP 전부가 RS를 2개 이상 반환하므로 이 패턴은 성립하지 않는다. **역할을 둘로 나눈다.**

```text
ResultCode·Result Set 형상 판정   tests/contract/<NN>_<시나리오>.sql  (EXEC 한 번)
                                  → sqlcmd -u -W -w 65535 -s"|" -o
                                  → node tools/verify-contract.js
DB 상태 불변조건 판정              tests/<NN>_*.sql  (행수 · StatusCode · RowVersion · Detail 집합)
```

`tools/expected-contracts.json` 의 각 시나리오에 `"sp"`, `"rs0Success"`, `"rs0Code"`, `"resultSets"` 를 둔다.

### 업무시간 SKIP 가드 — `tests/05`·`06`·`07`·`08` 머리에 필수

```sql
DECLARE @NowT TIME(7) = CONVERT(TIME(7), SYSDATETIME());
DECLARE @Dow  INT     = DATEDIFF(DAY, 0, CONVERT(DATE, SYSDATETIME())) % 7;
DECLARE @Biz  BIT     = CASE WHEN @Dow <> 6
                              AND @NowT >= CONVERT(TIME(7),'09:00:00')
                              AND @NowT <  CONVERT(TIME(7),'18:00:00')
                              AND NOT EXISTS (SELECT 1 FROM [dbo].[휴무일]
                                               WHERE [HolidayDate] = CONVERT(DATE, SYSDATETIME())
                                                 AND [Active] = 1)
                             THEN 1 ELSE 0 END;
PRINT 'INFO 실행시각 ' + CONVERT(VARCHAR(30), SYSDATETIME(), 121) + ' 업무가능=' + CONVERT(VARCHAR(1), @Biz);
IF @Biz = 0
BEGIN
    PRINT 'SKIP <파일명> 업무시간(월~토 09:00~18:00, 비휴무일) 밖';
    RETURN;
END
```

Write SP는 `308`/`309`를 업무 Rule보다 **먼저** 판정하므로, 가드가 없으면 업무시간 밖 실행에서 모든 단언이 FAIL한다. 완료조건은 **"업무시간 내 실행 시 PASS n건 / 밖이면 SKIP 1건 + FAIL 0건"** 으로 기술한다. `T37`은 업무시간 내 실행 로그가 없으면 해당 Gate를 `NOT RUN`으로 남긴다.

### Write SP 공통 Template

모든 Write SP는 아래 골격을 따른다. 스펙 §21.1과 동일하다.

```sql
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_...]
    <파라미터 — 05 계약 그대로>
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
    DECLARE @Today      DATE          = CONVERT(DATE, @ServerTime);
    DECLARE @NowTime    TIME(7)       = CONVERT(TIME(7), @ServerTime);
    DECLARE @StoredNow  DATETIME2(0)  = CONVERT(DATETIME2(0), @ServerTime);
    DECLARE @Code INT = 0, @Field VARCHAR(50) = NULL, @Msg NVARCHAR(300) = NULL;

    -- [1] Transaction 밖: 입력 정규화 / 필수값 / 허용값 / 조합
    IF @Code <> 0
    BEGIN
        SELECT CAST(0 AS BIT) AS Success, CAST(@Code AS INT) AS Code,
               CAST(@Msg AS NVARCHAR(300)) AS Message,
               CAST(@Field AS VARCHAR(50)) AS Field,
               CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
        RETURN;
    END

    -- [2] Transaction 밖: 잠금키 확보용 사전조회

    BEGIN TRY
        BEGIN TRANSACTION;

        -- [3] applock 획득 — 전역 순서 SSN → CHART → PAT → WORK → SLOT
        -- [4] 재검증: 존재 → 소유 → 상태 → 동시성 → Master 구성 → Rule → 정원
        --     Work 검사구성은 세 가지 전부 확인한다 (스펙 §21.2a)
        --       (1) NEX 개수 NOT BETWEEN 8 AND 11  → 701
        --       (2) ExamSourceCode 가 검사코드 역할과 불일치 → 701
        --       (3) AEX 개수 > 6 → 701
        IF @Code <> 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT CAST(0 AS BIT) AS Success, CAST(@Code AS INT) AS Code,
                   CAST(@Msg AS NVARCHAR(300)) AS Message,
                   CAST(@Field AS VARCHAR(50)) AS Field,
                   CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
            RETURN;
        END

        -- [5] 저장 (No-op 이면 아무것도 쓰지 않는다)
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    -- [6] COMMIT 이후에만 성공 Result Set 출력
    SELECT CAST(1 AS BIT) AS Success, CAST(@Code AS INT) AS Code,
           CAST(@Msg AS NVARCHAR(300)) AS Message,
           CAST(NULL AS VARCHAR(50)) AS Field,
           CAST(@ServerTime AS DATETIME2(7)) AS ServerTime;
    SELECT <RS1 — 05 계약 그대로>;
END
```

**RS0의 5개 컬럼에 반드시 명시적 `CAST`를 건다.** `sys.dm_exec_describe_first_result_set_for_object`가 15개 SP 전부에서 동일한 타입 문자열을 보고해야 G09가 성립한다.

### applock 획득 블록

```sql
DECLARE @rc INT, @Res NVARCHAR(255);

SET @Res = N'HC|PAT|' + CONVERT(NVARCHAR(20), @PatientId);
EXEC @rc = sp_getapplock @Resource = @Res, @LockMode = 'Exclusive',
                         @LockOwner = 'Transaction', @LockTimeout = 5000;

PRINT 'INFO applock rc=' + CONVERT(VARCHAR(4), @rc);   -- 경합 증거. 생략 금지
-- 자원명(@Res)은 찍지 않는다. HC|CHART|C000123 은 ChartNo 평문이고 HC|PAT|… 는 내부 식별자다.
-- 스펙 §41 "로그에 실제 개인정보를 남기지 않는다" 위반. rc 만으로 경합 증거(rc=1)는 충분하다.

IF @rc = -3                                   -- deadlock victim
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
END
IF @rc < 0                                    -- -1 timeout / -2 취소 / -999 호출오류
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
END
```

**`PRINT 'INFO applock rc=…'` 를 생략하지 않는다.** 이것이 없으면 동시성 시험이 "불변조건이 지켜졌다"만 확인하고 **"잠금이 실제로 동작했다"는 증거를 하나도 생산하지 못한다** — applock을 통째로 지워도 우연한 직렬 실행으로 7개 시나리오가 전부 PASS할 수 있다. `rc=1`(대기 후 획득)이 최소 1건 관측되어야 경합이 성립한 것으로 본다.

**`-3`(deadlock)과 나머지 음수를 분리한다.** 하나로 뭉개면 `CON-007`의 판정 *"`Msg 1205` 0건"* 이 항상 참이 된다 — applock 교착은 `1205`가 아니라 `THROW`로 나오기 때문이다.

`SSN` 자원만 원문이 아닌 hash를 쓴다. `@Res` 를 `PRINT` 해도 주민번호가 노출되지 않는다.

```sql
SET @Res = N'HC|SSN|' + CONVERT(CHAR(64), HASHBYTES('SHA2_256', @SocialNumber), 2);
```

### Slot 자원 2개 정렬 획득 — `CURSOR` 대신 `WHILE`

```sql
-- 자원이 최대 2개이므로 분기 2줄로 충분하다. CURSOR 를 쓰지 않는다 (허용목록 §9.2)
DECLARE @ResA NVARCHAR(255) = N'HC|SLOT|' + CONVERT(CHAR(8), @CurDate, 112)         + N'|' + @CurSlot;
DECLARE @ResB NVARCHAR(255) = N'HC|SLOT|' + CONVERT(CHAR(8), @ReservationDate, 112) + N'|' + @TimeSlot;

IF @ResA = @ResB SET @ResB = NULL;                      -- 같은 Slot 이면 하나만
IF @ResB IS NOT NULL AND @ResB < @ResA                  -- 문자열 오름차순으로 정렬
BEGIN
    DECLARE @Tmp NVARCHAR(255) = @ResA; SET @ResA = @ResB; SET @ResB = @Tmp;
END
-- @ResA 획득 → (있으면) @ResB 획득
```

`SLOT` 자원은 문자열 오름차순으로 정렬해 획득한다.

```sql
SET @Res = N'HC|SLOT|' + CONVERT(CHAR(8), @ReservationDate, 112) + N'|' + @TimeSlot;
```

### 조건부 UPDATE 표준형

```sql
UPDATE [dbo].[예약접수]
   SET StatusCode = '<새 상태>', LastEditDate = @StoredNow
 WHERE WorkId       = @WorkId
   AND StatusCode   = '<기대 상태>'
   AND [RowVersion] = @RowVersion;

IF @@ROWCOUNT = 0
BEGIN
    -- 재조회하여 502 WrongStatus 우선, 그 다음 601 WorkChanged
    ...
END
```

### Commit 규칙

각 Task 끝에서 commit한다. 메시지 형식:

```text
<type>(phase4): <요약>

type = feat | test | fix | docs | chore
```

---

## Gate 추적

| Gate | 검증 Task | 증거 파일 |
|---|---|---|
| G00 Baseline Hash | `T01`, `T37` | `artifacts/reports/baseline-hash.txt` |
| G01 WinForms 보호 | `T02`, `T37` | `artifacts/reports/winforms-manifest.txt` |
| G02 Preflight | `T04` | `artifacts/logs/00_preflight.log` |
| G03 Clean Deploy | `T35` | `artifacts/logs/deploy_full.log` |
| G04 Object Inventory | `T07`, `T35` | `artifacts/reports/object-inventory.txt` |
| G05 Schema | `T07` | `artifacts/logs/test_01.log` |
| G06 금지 객체 | `T07` | `artifacts/logs/test_01.log` |
| G07 Seed | `T09` | `artifacts/logs/test_02.log` |
| G08 Rule | `T11`~`T14` | `artifacts/logs/test_03.log` |
| G09 SP Contract | `T22`, `T36` | `artifacts/reports/contract-verify.txt` |
| G10 Rollback | `T33` | `artifacts/logs/test_08.log` |
| G11 Concurrency | `T34` | `artifacts/logs/conc_*.log` |
| G12 Security | `T32` | `artifacts/logs/test_13.log` |
| G13 SQL Server 호환성 | `T35`, `T37` | `artifacts/reports/phase4-report.md` |
| G14 Repeatability | `T35` | `artifacts/reports/inventory_run1.txt` · `inventory_run2.txt` (diff) |
| G15 Evidence | `T37` | `artifacts/reports/test-summary.txt` |
| G16 06 문서 | `T37` | `docs/phase4/06_DB_Transaction_Security_Seed.md` |

**`PASS`는 실제 실행 증거가 있을 때만 기록한다.** 실행 전에는 `PLANNED` / `NOT RUN` / `BLOCKED`를 사용한다.

**`SKIP`은 `PASS`가 아니다.** 업무시간 밖 실행으로 SKIP된 Gate는 `NOT RUN`으로 남긴다. `PHASE 4 COMPLETE`를 선언하려면 업무시간 내 실행 로그가 반드시 하나 있어야 한다.

**개수 비교를 "전건 일치" 증거로 쓰지 않는다.** G05·G07·G09·G14는 전부 `EXCEPT` 양방향 또는 `diff` 로 판정한다.

**G13은 세 갈래로 나누어 기록한다.** (a) 전체 배포·테스트 성공 `PASS/FAIL` · (b) 블랙리스트 grep 0건 `PASS/FAIL` · (c) §9.2 허용목록 준수 **`REVIEWED`**(자동 판정 불가).

---

## 실행 전 사용자 승인 Gate

이 계획을 실행하려면 사용자의 **명시적 실행 승인**이 필요하다. 승인 없이 `T01`을 시작하지 않는다.

`T01`은 ROOT에 `git init`을 수행하고 `T35`는 `DROP DATABASE`를 수행한다. 두 작업 모두 `D4-002`·`D4-003`으로 승인되었으나, 계획 실행 자체에 대한 별도 승인이 선행되어야 한다.
