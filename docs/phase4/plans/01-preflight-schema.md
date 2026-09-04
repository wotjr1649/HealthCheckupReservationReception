# Stage 0~2 — Repository 보호 · Preflight · Physical Schema

**Index:** `2026-09-04-phase4-database-implementation.md`
**Spec:** `../06_DB_Transaction_Security_Seed_CANDIDATE.md`
**Tasks:** `T01` ~ `T07`

작업 디렉터리는 항상 `D:\AIDEV\HealthCheckupReservationReception\database` 다. 아래의 모든 상대경로는 이 디렉터리 기준이다.

---

## Task T01: Repository 보호 — git init · baseline tag · hash manifest

**목적:** Phase 4 작업 전에 00~05와 WinForms의 현재 상태를 git으로 봉인해 G00·G01을 기계적으로 증명 가능하게 만든다.

**관련 Baseline 위치:** 없음 (Repository 작업). 스펙 §44.2 `D4-002`.

**선행조건:** 없음.

**Files:**
- Create: `../.gitignore` (ROOT)
- Create: `artifacts/reports/baseline-hash.txt`
- Create: `scripts/verify-baseline.sh`

**Interfaces:**
- Produces: git tag `baseline-HC-RSV-RCP-20260904-R3`, branch `phase4-database`, `scripts/verify-baseline.sh` (exit 0 = 6/6 일치)

**금지사항:**
- `git config --global` 을 건드리지 않는다. identity는 **repo-local** 로만 설정한다.
- `docs/baseline/**` 와 `winforms/**` 의 파일 내용을 수정하지 않는다. `git add` 는 내용을 바꾸지 않는다.
- `git reset` / `git clean` / `git checkout --` / force push 를 사용하지 않는다. remote 를 추가하지 않는다.

- [ ] **Step 1: RED — 검증 스크립트를 먼저 만들고 실패를 확인한다**

`scripts/verify-baseline.sh` 를 만든다.

```bash
#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
node -e '
const fs=require("fs"),c=require("crypto");
const exp={
 "00_Project_Policy.md":"5adba8d4001e8f7aa27091614df33922a7d3d7cf27965f9ccc60874316beaefc",
 "01_Process_Definition.md":"1b0d1c23cb8dda15c6c0ba46a86a48a2586608b42079ed96a837386ae35f69e6",
 "02_Function_Definition.xlsx":"ac7b362ea79b062a889cd296bada304db4f66cb850a2ab04ca999c7530a1654a",
 "03_Wireframe_Definition.md":"831b61f27e38d60e856e9309906af2df89c5632a732b8289fdb216b257c80024",
 "04_DB_Design.md":"8176d8a82ae360718f811d7f26481b6cb56778585aeebd3b38b42ec89950038f",
 "05_DB_Rule_SP_Contract.md":"b865c76fa5d041fa816e0d2380a7ca16306e80502652a9369201fdc956f71c24"};
let ok=0;
for(const [n,e] of Object.entries(exp)){
  const h=c.createHash("sha256").update(fs.readFileSync("../docs/baseline/"+n)).digest("hex");
  const m=h===e; if(m)ok++;
  console.log((m?"OK   ":"DIFF ")+n.padEnd(30)+h);
}
console.log("=== "+ok+"/6 ===");
if(ok!==6) process.exit(1);
'
```

`03_Wireframe_Definition.md` 의 기대값은 **인계문서 값이 아니라 `D4-001` 로 승인된 실측값**임에 유의한다.

- [ ] **Step 2: 검증 스크립트를 실행해 통과를 확인한다**

```bash
chmod +x scripts/verify-baseline.sh
RC=0
./scripts/verify-baseline.sh > artifacts/reports/baseline-hash.txt 2>&1 || RC=$?
cat artifacts/reports/baseline-hash.txt
echo "exit=$RC"
```

Expected: `=== 6/6 ===`, `exit=0`.

**`| tee` 를 쓰지 않는다.** `$?` 가 `tee` 의 것(항상 0)이라 6/6이 아니어도 `exit=0` 으로 보인다.

6/6이 아니면 **여기서 중단**하고 사용자에게 보고한다. 기준선이 흔들린 상태로 진행하지 않는다.

- [ ] **Step 3: ROOT `.gitignore` 작성**

`../.gitignore`:

```gitignore
# Build output
[Bb]in/
[Oo]bj/
.vs/
TestResults/
*.user

# 다른 세션이 생성하는 문서 산출물. Phase 4 DB 작업의 대상이 아니고
# 기준선 tag 에 진행중 파일이 섞이면 재현성이 깨진다.
docs/baseline/output/
*.suo

# Phase 4 로그 (보고서는 커밋한다)
database/artifacts/logs/
integration/artifacts/logs/
winforms/artifacts/logs/
```

- [ ] **Step 4: git init + repo-local identity**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git init
git config user.name  "JS"
git config user.email "JS@DESKTOP-DP7KRE4"
git config --get user.name && git config --get user.email
git config --global --get user.name || echo "global 미설정 유지 확인"
```

Expected: repo-local 값이 출력되고, global은 여전히 비어 있다.

- [ ] **Step 5: baseline 초기 commit + tag**

**`git add -A` 를 쓰지 않는다.** ROOT 전체를 훑어 무관한 파일과 잠재적 secret 까지 초기 commit 에 넣는다. **필요한 경로만 명시**한다.

```bash
cd /d/AIDEV/HealthCheckupReservationReception
# [X] `git add docs/baseline` 로 디렉터리를 통째로 넣지 않는다.
#     docs/baseline/output/ 에 다른 세션이 산출물(xlsx·pptx)을 쓰고 있으면
#     (1) 기준선 tag 에 기준선이 아닌 진행중 파일이 들어가고
#     (2) 쓰기 도중의 blob 을 커밋해 손상된 내용이 봉인될 수 있으며
#     (3) verify-baseline.sh 는 6개 파일만 대조하므로 그 손상을 잡지 못한다.
#     기준선 6개 파일만 경로로 명시한다.
git add .gitignore \
        docs/baseline/00_Project_Policy.md \
        docs/baseline/01_Process_Definition.md \
        docs/baseline/02_Function_Definition.xlsx \
        docs/baseline/03_Wireframe_Definition.md \
        docs/baseline/04_DB_Design.md \
        docs/baseline/05_DB_Rule_SP_Contract.md \
        winforms
git status --short
# 위 목록에 기준선 6개, winforms 소스, .gitignore 만 있는지 눈으로 확인한다.
# docs/baseline/output/ 이 보이면 위 명령이 잘못된 것이다. bin/obj/.vs/TestResults 가 보이면 .gitignore 를 고친다.
git commit -m "chore(baseline): HC-RSV-RCP-20260904-R3 기준선 및 WinForms 골격 봉인"
git tag -a baseline-HC-RSV-RCP-20260904-R3 -m "00~05 FINAL/GO/READ-ONLY + WinForms 골격"
git tag -l
```

- [ ] **Step 6: commit 직후 baseline hash 재검증**

```bash
cd /d/AIDEV/HealthCheckupReservationReception/database
./scripts/verify-baseline.sh
echo "exit=$?"
```

Expected: `=== 6/6 ===`, exit 0. `git add`/`commit` 이 파일 내용을 바꾸지 않았음을 증명한다.

- [ ] **Step 7: Phase 4 전용 branch 생성**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git switch -c phase4-database
git branch --show-current
```

Expected: `phase4-database`

- [ ] **Step 8: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/scripts/verify-baseline.sh database/artifacts/reports/baseline-hash.txt .gitignore
git commit -m "chore(phase4): baseline hash verifier 및 gitignore 추가"
```

**회귀시험:** `./scripts/verify-baseline.sh` — 이후 모든 Task 종료 시 재실행한다.

**로그 경로:** `artifacts/reports/baseline-hash.txt`

**Rollback/Cleanup:** `git init` 을 되돌리려면 `.git` 을 지우면 된다. 다만 **`rm -rf ../.git` 을 무조건 실행하지 않는다** — 이 Task 가 만든 repo 인지 먼저 확인한다. 상태가 바뀐 뒤에 실행하면 복구 불가능하게 이력을 없앤다.

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git log --oneline | wc -l                       # 1이어야 한다 (초기 commit 하나)
git tag -l | grep -c baseline-HC-RSV-RCP        # 1이어야 한다
git remote -v                                   # 비어 있어야 한다
# 세 조건이 모두 맞을 때만 rm -rf .git
```

추적 대상 파일 자체는 변경되지 않는다.

**완료조건:** baseline 6/6 · tag 존재 · branch `phase4-database` · global git config 미변경.

---

## Task T02: WinForms 불변 manifest

**목적:** WinForms 소스가 Phase 4 동안 한 바이트도 바뀌지 않았음을 git 외에 hash로도 이중 증명한다 (G01).

**관련 Baseline 위치:** 스펙 §5.4, §42 G01.

**선행조건:** `T01` 완료.

**Files:**
- Create: `scripts/verify-winforms-unchanged.sh`
- Create: `artifacts/reports/winforms-manifest.txt`

**Interfaces:**
- Produces: `scripts/verify-winforms-unchanged.sh` (exit 0 = 변경 0건)

**금지사항:** `../winforms/**` 에 어떤 쓰기도 하지 않는다.

- [ ] **Step 1: manifest 생성 스크립트 작성**

`scripts/verify-winforms-unchanged.sh`:

```bash
#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
MANIFEST="artifacts/reports/winforms-manifest.txt"

gen() {
  find ../winforms -type f \
    -not -path '*/obj/*' -not -path '*/bin/*' \
    -not -path '*/.vs/*'  -not -path '*/TestResults/*' \
    -print0 | sort -z | xargs -0 sha256sum
}

if [ "${1:-check}" = "init" ]; then
  gen > "$MANIFEST"
  echo "manifest 생성: $(wc -l < "$MANIFEST") 파일"
else
  gen > /tmp/winforms-now.txt
  if diff -u "$MANIFEST" /tmp/winforms-now.txt; then
    echo "PASS WinForms 변경 0건"
  else
    echo "FAIL WinForms 변경 감지"; exit 1
  fi
fi
```

- [ ] **Step 2: 최초 manifest 생성**

```bash
chmod +x scripts/verify-winforms-unchanged.sh
./scripts/verify-winforms-unchanged.sh init
cat artifacts/reports/winforms-manifest.txt | head -5
```

Expected: `manifest 생성: N 파일` (N >= 5)

- [ ] **Step 3: GREEN — 즉시 재검증**

```bash
./scripts/verify-winforms-unchanged.sh
echo "exit=$?"
```

Expected: `PASS WinForms 변경 0건`, exit 0.

- [ ] **Step 4: 음성 검증 — 변경을 감지하는지 확인**

```bash
cp artifacts/reports/winforms-manifest.txt /tmp/wf-manifest.bak
sed -i '1s/^/# TEMP\n/' artifacts/reports/winforms-manifest.txt
RC=0; ./scripts/verify-winforms-unchanged.sh || RC=$?
echo "변조 상태 exit=$RC   (1 이어야 한다)"
cp /tmp/wf-manifest.bak artifacts/reports/winforms-manifest.txt
RC=0; ./scripts/verify-winforms-unchanged.sh || RC=$?
echo "복구 상태 exit=$RC   (0 이어야 한다)"
```

Expected: 변조 시 `FAIL WinForms 변경 감지` + exit 1, 복구 후 `PASS` + exit 0. 검증기가 실제로 동작함을 확인한다.

**`git checkout --` 를 쓰지 않는다.** `T01` 금지사항이 명시적으로 금지한 명령이다. 단순 파일 복사로 대체한다.

- [ ] **Step 5: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/scripts/verify-winforms-unchanged.sh database/artifacts/reports/winforms-manifest.txt
git commit -m "chore(phase4): WinForms 불변 manifest 및 검증 스크립트 추가"
```

**회귀시험:** `./scripts/verify-winforms-unchanged.sh` — 이후 모든 Task 종료 시 재실행.

**Rollback/Cleanup:** manifest 파일 삭제 후 `init` 재실행.

**완료조건:** 검증기가 PASS와 FAIL을 모두 실제로 만들어 낸다.

---

## Task T03: `database/CLAUDE.md` · `README.md`

**목적:** 이 디렉터리에서 일하는 사람(사람 또는 에이전트)이 경계와 실행 방법을 즉시 알 수 있게 한다.

**관련 Baseline 위치:** 스펙 §3, §7, §10.

**선행조건:** `T02` 완료.

**Files:**
- Create: `CLAUDE.md`
- Create: `README.md`

**금지사항:** 기준선 내용을 복사하지 않는다. 참조와 경계만 적는다.

- [ ] **Step 1: `CLAUDE.md` 작성**

포함할 내용 (각 항목 1~3줄):

```text
1. 이 디렉터리의 책임 = Phase 4 DB SQL·Script·테스트·증거
2. 읽기 전용 경계   ../docs/baseline/**, ../winforms/**
3. 쓰기 허용 경계   database/**, ../docs/phase4/**
4. Source of Truth  00 → 01 → 02 → 03 → 04 → 05 → 06 CANDIDATE → SQL
5. 모든 .sql 은 UTF-8 with BOM. BOM 없으면 sqlcmd 가 한글 객체명을 깨뜨린다
6. 모든 sqlcmd 는 -b -I -u 사용. exit code 가 유일한 자동 판정 근거
7. 허용 T-SQL 목록 = 스펙 §9.2. 그 밖의 기능을 쓰지 않는다
8. Deploy.sql 은 실행할 때마다 스키마와 데이터를 초기화한다
9. DROP DATABASE 는 Rebuild.sql 에만 존재한다
10. 실행하지 않은 검증을 PASS 로 기록하지 않는다
```

- [ ] **Step 2: `README.md` 작성**

포함할 내용:

```text
개요 / 대상 환경 (인스턴스·DB명·Collation)
빠른 시작   ./scripts/rebuild.sh  →  ./scripts/test.sh
파일 구조   스펙 §7 트리 요약
로그 읽는 법 iconv -f UTF-16 -t UTF-8 artifacts/logs/*.log
Gate 표     스펙 §42 링크
```

- [ ] **Step 3: 링크·경로 검증**

```bash
grep -oE '\.\./[a-zA-Z0-9_/.-]+' CLAUDE.md README.md | sort -u | while read -r p; do
  [ -e "$p" ] && echo "OK   $p" || echo "MISS $p"
done
```

Expected: `MISS` 0건.

- [ ] **Step 4: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/CLAUDE.md database/README.md
git commit -m "docs(phase4): database 작업 경계 및 실행 안내 추가"
```

**완료조건:** 두 문서에 깨진 경로 0건.

---

## Task T04: `deploy/00_Preflight.sql` — 안전가드 6종

**목적:** 잘못된 서버·DB에서 파괴적 작업이 실행될 가능성을 구조적으로 차단하고 KST·버전을 확인한다 (G02).

**관련 Baseline 위치:** `04` §3.4 (KST), 스펙 §8.3.

**선행조건:** `T03` 완료.

**Files:**
- Create: `deploy/00_Preflight.sql`

**Interfaces:**
- Produces: 실패 시 `THROW 50010`~`50015`, 성공 시 `PRINT 'PASS PRE-00x …'` 6줄

**금지사항:** Preflight는 **읽기 전용**이다. 어떤 객체도 만들거나 바꾸지 않는다.

- [ ] **Step 1: RED — 가드가 실제로 막는지 먼저 확인한다**

아직 파일이 없으므로, 잘못된 DB에서 실행할 때 막히는지 확인할 대상이 없다. 먼저 `master` 에서 실행하면 `PRE-002`(대상 DB exact match)가 먼저 걸려 **`THROW 50011`** 이 나야 한다는 기대를 적어 둔다.

`[X]` 초안은 여기서 `50012`(시스템 DB 방어)를 기대했다. 그러나 `PRE-002` 가 이름을 이미 확정하므로 `master` 에서는 `50011` 이 먼저 발화한다 — `50012` 로는 도달할 수 없다. 이것이 `PRE-003` 이 죽은 가드였다는 증거이기도 하다.

- [ ] **Step 2: `deploy/00_Preflight.sql` 작성 (UTF-8 with BOM)**

```sql
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
-- [X] 초안은 PRINT 인자에 `(SELECT compatibility_level FROM sys.databases …)` 를 직접 넣었다.
--     PRINT 는 스칼라 식만 받으므로 Msg 1046 으로 **배치 전체가 컴파일 실패**한다(실측 확인).
--     PRE-001~006 이 한 줄도 실행되지 않은 채 exit 1 이 나와 RED 시험이 우연히 통과한 것처럼 보였다.
--     하위 쿼리는 변수에 먼저 담는다.
SELECT @CompatLevel = CONVERT(VARCHAR(10), compatibility_level) FROM sys.databases WHERE name = @TargetDb;

PRINT 'INFO Edition           = ' + CONVERT(VARCHAR(80), SERVERPROPERTY('Edition'));
PRINT 'INFO CompatibilityLevel= ' + ISNULL(@CompatLevel, '(미확인)');
PRINT 'INFO Collation         = ' + ISNULL(CONVERT(VARCHAR(80), DATABASEPROPERTYEX(@TargetDb, 'Collation')), '(미확인)');
PRINT 'INFO ServerTime        = ' + CONVERT(VARCHAR(40), SYSDATETIMEOFFSET(), 126);
PRINT 'INFO LoginName         = ' + SUSER_SNAME();
GO
```

- [ ] **Step 3: BOM 확인**

```bash
head -c 3 deploy/00_Preflight.sql | od -An -tx1
```

Expected: `ef bb bf`

- [ ] **Step 4: RED 검증 — `master` 에서 실행하면 막히는가**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d master -b -I -u -i deploy/00_Preflight.sql -o artifacts/logs/pre_red.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/pre_red.log
```

Expected: exit **1**, 로그에 `Msg 50011` (대상 Database 가 아님). `master` 는 `DB_ID()=1` 이지만 PRE-002가 먼저 걸린다.

- [ ] **Step 5: 대상 DB 생성 (최초 1회)**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d master -b -I -Q "IF DB_ID(N'HealthCheckupReservationReceptionDb') IS NULL CREATE DATABASE [HealthCheckupReservationReceptionDb] COLLATE Korean_Wansung_CI_AS;"
echo "exit=$?"
```

Expected: exit 0.

- [ ] **Step 6: GREEN — 대상 DB에서 실행**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i deploy/00_Preflight.sql -o artifacts/logs/00_preflight.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/00_preflight.log
```

Expected: exit **0**, `PASS PRE-001` ~ `PASS PRE-006` 6줄 + `INFO` 5줄.

- [ ] **Step 7: 타 DB 메타데이터 사전 스냅샷 (`RBD-009` 의 diff 대상)**

`[X]` `T35` 의 `RBD-009` 는 `otherdb_before.txt` 와 `diff` 하는데, 초안에는 그 파일을 **만드는 Step 이 어디에도 없었다.** 여기서 만든다 — `Rebuild.sql` 이 한 번이라도 돌기 **전**이어야 의미가 있다.

`[X]` 초안은 `Net461MvpSample` **한 행**을 `collation_name` 과 함께 찍었는데, `T35` Step 6 은 **`HealthCheckupReservationReceptionDb` 를 뺀 전 DB** 를 **문자열 연결** 형태로 찍어 `diff` 한다. 컬럼 구성도 행 집합도 달라 **어떤 경우에도 일치할 수 없었다.** 아래 명령은 `T35` Step 6 과 **한 글자도 다르지 않아야 한다.**

`[X]` `collation_name` 을 뺀다. SQL Server Express 는 `model` 로부터 **`AUTO_CLOSE ON`** 을 상속하고, DB 가 닫혀 있는 동안 `sys.databases.collation_name` 과 `DATABASEPROPERTYEX(…,'Collation')` 은 **`NULL`** 을 돌려준다(실측: 대상 DB 에 접속한 세션에서만 `Korean_Wansung_CI_AS` 가 보인다). 누군가 그 DB 를 열어 둔 채 후속 스냅샷을 찍으면 `RBD-009` 가 Rebuild 와 무관하게 FAIL 한다. 대신 `create_date` 를 넣는다 — Drop/Recreate 를 잡아내는 더 강한 증거이고 열림 여부와 무관하다.

`[X]` `tempdb` 를 제외한다. 서비스 재시작마다 재생성되므로 `create_date` 가 바뀌고, 불변을 주장하는 것 자체가 성립하지 않는다.

`[X]` 문자열 연결(`name + '|' + …`)을 쓰지 않는다. `sys.databases.name` 은 catalog collation(`Latin1_General_CI_AS_KS_WS`)이고 리터럴은 DB collation(`Korean_Wansung_CI_AS`)이라 `add` 연산자에서 **`Msg 451`** 로 실패한다(실측 확인). sqlcmd `-s"|"` 컬럼 구분자를 쓰면 이 문제가 없다. 비교 연산자(`NOT IN`)는 암시적 변환이 되므로 그대로 둔다.

```bash
mkdir -p artifacts/reports
sqlcmd -S '.\SQLEXPRESS' -E -d master -b -I -h -1 -W -s"|" \
  -Q "SET NOCOUNT ON; SELECT name, state_desc, user_access_desc, CONVERT(VARCHAR(1), CONVERT(INT, is_read_only)), CONVERT(VARCHAR(30), create_date, 126) FROM sys.databases WHERE name NOT IN (N'HealthCheckupReservationReceptionDb', N'tempdb') ORDER BY database_id;" \
  -o artifacts/reports/otherdb_before.txt
echo "exit=$?"
cat artifacts/reports/otherdb_before.txt
```

Expected: `master` · `model` · `msdb` · `Net461MvpSample` 네 행. 이 파일은 **커밋한다** — `RBD-009` 의 유일한 비교 기준이다.

- [ ] **Step 8: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/00_Preflight.sql database/artifacts/reports/otherdb_before.txt
git commit -m "feat(phase4): Preflight 안전가드 6종 + 타 DB 사전 스냅샷"
```

**회귀시험:** 모든 배포·테스트 실행이 `00_Preflight.sql` 로 시작한다.

**로그 경로:** `artifacts/logs/00_preflight.log`

**Rollback/Cleanup:** Preflight는 상태를 바꾸지 않으므로 롤백 대상이 없다. Step 5로 만든 DB는 `T05` 의 `Rebuild.sql` 이 관리한다.

**완료조건:** 잘못된 DB에서 exit 1 + `Msg 50011`, 대상 DB에서 exit 0 + `PASS` 6줄. 둘 다 실제로 관측했을 때만 완료다.

---

## Task T05: `Deploy.sql` · `Rebuild.sql` · 실행 스크립트

**목적:** 배포 진입점을 두 개로 고정하고, `DROP DATABASE` 를 `Rebuild.sql` 한 곳에만 둔다.

**관련 Baseline 위치:** 스펙 §8.1, §8.2, §8.4.

**선행조건:** `T04` 완료.

**Files:**
- Create: `Deploy.sql`, `Rebuild.sql`
- Create: `scripts/deploy.sh`, `scripts/rebuild.sh`, `scripts/test.sh`

**Interfaces:**
- Produces: `./scripts/rebuild.sh` (DB Drop/Create 후 전체 배포), `./scripts/deploy.sh` (DB 유지, 객체 재배포)

**금지사항:** `Deploy.sql` 에 `DROP DATABASE` 를 넣지 않는다. `Rebuild.sql` 의 대상 DB명을 변수로 외부에서 주입하지 않는다 (하드코딩 + exact match).

- [ ] **Step 1: `Deploy.sql` 작성 (UTF-8 with BOM)**

```sql
:setvar DeployDir "deploy"
SET NOCOUNT ON;
PRINT '=== Deploy 시작 ' + CONVERT(VARCHAR(40), SYSDATETIMEOFFSET(), 126) + ' ===';
GO
:r $(DeployDir)\00_Preflight.sql
:r $(DeployDir)\01_Schema.sql
:r $(DeployDir)\02_Seed.sql
:r $(DeployDir)\03_Functions.sql
:r $(DeployDir)\04_Procedures_Select.sql
:r $(DeployDir)\05_Procedures_Patient_Write.sql
:r $(DeployDir)\06_Procedures_Reservation_Write.sql
:r $(DeployDir)\07_Procedures_Reception_Write.sql
:r $(DeployDir)\08_Security.sql
:r $(DeployDir)\09_Verify.sql
GO
PRINT '=== Deploy 완료 ===';
GO
```

`:r` 은 sqlcmd 모드에서만 동작한다. `scripts/deploy.sh` 가 항상 sqlcmd로 실행하므로 문제없다. `:r` 가 UTF-8 BOM 파일을 정상 포함함은 실측 확인했다.

**`01`~`09` 를 전부 빈 스텁으로 지금 만든다.** 그러지 않으면 `T34`(동시성 7회 rebuild)가 아직 없는 `09_Verify.sql` 을 `:r` 하다가 조용히 어긋난다. `>/dev/null` 로 출력을 버리는 루프라 아무도 눈치채지 못한다.

```bash
mkdir -p deploy
for f in 01_Schema 02_Seed 03_Functions 04_Procedures_Select \
         05_Procedures_Patient_Write 06_Procedures_Reservation_Write \
         07_Procedures_Reception_Write 08_Security 09_Verify; do
  [ -f "deploy/${f}.sql" ] && continue
  printf '\xEF\xBB\xBF' > "deploy/${f}.sql"
  # [X] PRINT 리터럴에 N 접두사를 붙인다. 없으면 varchar 리터럴이 되어 Korean_Wansung(CP949) 에
  #     없는 U+2014 EM DASH 가 '?' 로 깨진다(실측 확인). 한글은 살아남지만 특수문자는 아니다.
  printf "SET NOCOUNT ON;\nPRINT N'INFO %s placeholder — 아직 구현되지 않았습니다.';\nGO\n" "$f" >> "deploy/${f}.sql"
done
ls -1 deploy/
```

각 Task 가 해당 파일을 실제 내용으로 덮어쓴다. 스텁 상태에서도 `Deploy.sql` 전체 실행이 exit 0 이므로 `T34` 가 막히지 않는다.

- [ ] **Step 2: `Rebuild.sql` 작성 (UTF-8 with BOM)**

가드 번호대는 **`50020~50024`** 다. Preflight(`50010~50015`)와 섞지 않는다 — 같은 번호가 두 파일에서 다른 의미를 가지면 로그만으로 원인을 알 수 없다.

```sql
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
```

**`DECLARE @TargetDb = N'…'; IF @TargetDb <> N'…'` 형태를 쓰지 않는다.** 바로 위에서 같은 리터럴을 대입했으므로 **항진 명제**이고 절대 발화하지 않는 죽은 코드다. 초안의 `50011` 이 정확히 그 형태였다.

`DROP DATABASE` 대상은 **하드코딩된 대괄호 식별자**다. 변수 DB명으로 동적 SQL을 만들지 않는다 — 오타나 주입으로 다른 DB가 지워질 경로 자체를 없앤다.

한글 `PRINT` 에는 `N` 접두사를 붙인다. `master` 의 데이터 정렬이 `Korean_Wansung` 이 아닐 수 있다(대상 DB 만 명시 고정했다).

- [ ] **Step 3: `scripts/deploy.sh` 작성**

```bash
#!/usr/bin/env bash
set -uo pipefail          # -e 는 쓰지 않는다. 실패해도 로그를 반드시 출력해야 한다
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'
DB='HealthCheckupReservationReceptionDb'
mkdir -p artifacts/logs

RC=0
sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -i Deploy.sql -o artifacts/logs/deploy_full.log || RC=$?
iconv -f UTF-16 -t UTF-8 artifacts/logs/deploy_full.log | tail -40
echo "deploy exit=$RC"
exit $RC
```

**`sqlcmd …` 다음 줄에 `RC=$?` 를 두면 안 된다.** `set -e` 하에서 sqlcmd 가 1을 반환하면 그 줄에서 셸이 끝나 `RC=$?` 도 `iconv | tail` 도 실행되지 않는다(실측 확인). **정확히 실패했을 때만** 진단이 사라진다.

- [ ] **Step 4: `scripts/rebuild.sh` 작성**

`[X]` `deploy.sh` 와 동일하게 `-e` 를 쓰지 않는다. `Rebuild.sql` 가드(`50020`~`50024`)가 걸렸을 때 **정확히 그때만** `Msg` 번호를 볼 수 없게 되는 것이 `set -e` 의 실패 방식이다(스펙 §8.4).

```bash
#!/usr/bin/env bash
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'
mkdir -p artifacts/logs
echo "!! 이 작업은 HealthCheckupReservationReceptionDb 를 삭제하고 다시 만듭니다."
sqlcmd -S "$SRV" -E -d master -b -I -u -i Rebuild.sql -o artifacts/logs/rebuild.log
iconv -f UTF-16 -t UTF-8 artifacts/logs/rebuild.log
./scripts/deploy.sh
```

- [ ] **Step 5: `scripts/test.sh` 작성**

```bash
#!/usr/bin/env bash
set -uo pipefail
cd "$(dirname "$0")/.."
SRV='.\SQLEXPRESS'
DB='HealthCheckupReservationReceptionDb'
mkdir -p artifacts/logs artifacts/reports
FAILED=0

./scripts/rebuild.sh || { echo "rebuild 실패"; exit 1; }

run() {                       # run <파일> <로그번호>
  local f="$1" log="artifacts/logs/test_${2}.log" rc=0
  echo "--- $f"
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -u -i "$f" -o "$log" || rc=$?
  iconv -f UTF-16 -t UTF-8 "$log" | grep -E '^(PASS|FAIL|SKIP|INFO|Msg )' || true
  [ "$rc" -ne 0 ] && { echo "!! $f exit=$rc"; FAILED=1; }
  return 0
}

run tests/00_Test_Harness.sql             00
run tests/01_Schema_Tests.sql             01
run tests/02_Seed_Tests.sql               02
run tests/00b_Test_Harness_RCP.sql        00b     # T14b — TVF 배포 이후이고 tests/03 보다 앞이어야 한다
                                                  #   RUL-A09 가 T011 의 RCP Work(저장 NEX)를 읽는다
run tests/03_Rule_Tests.sql               03
run tests/04_Select_SP_Tests.sql          04
run tests/05_Patient_Write_Tests.sql      05
run tests/06_Reservation_Write_Tests.sql  06
run tests/07_Reception_Write_Tests.sql    07
run tests/08_Rollback_Tests.sql           08
run tests/13_Security_Tests.sql           13
run tests/14_Clean_Rebuild_Verify.sql     14

# 계약 검증 (G09) — 스펙 §8.4 가 test.sh 범위로 지정했다
./scripts/verify-contract-all.sh || FAILED=1

# 동시성 (G11) — 8개 시나리오
for s in 1 2 3 4 5 6 7 8; do
  # [X] 시나리오마다 rebuild + fixture 재배치. T34 Step 4 가 "시나리오 간 오염이 없다"의 근거로 삼은 절차다.
  #     이것이 없으면 tests/01~14 가 이미 변형한 DB 위에서 CON-002(19/20) 가 첫 실행부터 FAIL 한다.
  ./scripts/rebuild.sh > /dev/null 2>&1 || { FAILED=1; continue; }
  sqlcmd -S "$SRV" -E -d "$DB" -b -I -i tests/00_Test_Harness.sql > /dev/null 2>&1 || FAILED=1
  ./scripts/concurrency-test.sh "$s" || FAILED=1
done

# SEC-010 (secret 스캔) 은 SQL 이 아니라 셸이다. 전체 회귀에 반드시 포함한다.
./scripts/verify-no-secret.sh || FAILED=1

# 문서 정합성 게이트 (스펙 §45.3)
node tools/verify-docs.js || FAILED=1

if [ "$FAILED" -eq 0 ]; then echo "=== 전체 테스트 통과 ==="; exit 0
else echo "=== 실패한 단계가 있습니다 ==="; exit 1; fi
```

`[X]` **초안 오류 2건**
1. `set -e` 로 첫 실패에서 중단시켜 **뒤 단계의 결과를 전혀 볼 수 없었다.** 플래그로 집계하고 끝에서 판정한다.
2. **동시성 `09`~`12` 와 계약 검증기를 실행하지 않으면서 `"=== 전체 테스트 통과 ==="` 를 출력**했다. G09·G11이 한 번도 안 돌아도 `T37` 의 "전체 회귀 exit 0" 이 성립해 버린다. 스펙 §8.4가 `test.sh` 범위를 *"rebuild → deploy → tests/01~14 → verify-contract.js"* 로 정의했으므로 둘 다 포함한다.

- [ ] **Step 6: 실행 권한 + BOM 확인**

```bash
chmod +x scripts/*.sh
for f in Deploy.sql Rebuild.sql; do printf '%-14s ' "$f"; head -c 3 "$f" | od -An -tx1; done
```

Expected: 둘 다 `ef bb bf`

- [ ] **Step 7: `Rebuild.sql` 단독 검증 (DB만 재생성)**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d master -b -I -u -i Rebuild.sql -o artifacts/logs/rebuild.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/rebuild.log
sqlcmd -S '.\SQLEXPRESS' -E -b -I -h -1 -W -Q "SELECT name FROM sys.databases ORDER BY database_id;"
```

Expected: exit 0, `PASS RBD-CREATE`, DB 목록에 `Net461MvpSample` 이 **그대로 있고** `HealthCheckupReservationReceptionDb` 가 있다.

- [ ] **Step 8: 음성 검증 — 잘못된 컨텍스트에서 막히는가**

```bash
RC=0
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i Rebuild.sql -o artifacts/logs/rebuild_red.log || RC=$?
echo "exit=$RC"
iconv -f UTF-16 -t UTF-8 artifacts/logs/rebuild_red.log
sqlcmd -S '.\SQLEXPRESS' -E -b -I -h -1 -W -Q "SELECT name FROM sys.databases ORDER BY database_id;"
```

Expected: `exit=1`, `Msg 50021` (master 컨텍스트 필요). DB 목록에 `HealthCheckupReservationReceptionDb` 와 `Net461MvpSample` 이 **둘 다 그대로** 있다.

`[X]` 이 단계는 `RBD-002`(master 컨텍스트 강제)만 검증한다. **`RBD-001`(잘못된 서버명 = `50020`)은 인스턴스가 1개뿐이라 음성 시험이 불가능하므로 `NOT RUN` 으로 기록한다.** 초안은 이 한 번의 실행으로 `RBD-001`·`RBD-002` 두 Gate를 모두 검증한 것처럼 적었다.

- [ ] **Step 9: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/Deploy.sql database/Rebuild.sql database/scripts/
git commit -m "feat(phase4): Deploy/Rebuild 진입점 및 실행 스크립트 추가"
```

**회귀시험:** `T35` 에서 연속 2회 rebuild 후 인벤토리 동일성을 검증한다.

**로그 경로:** `artifacts/logs/rebuild.log`, `artifacts/logs/deploy_full.log`

**Rollback/Cleanup:** 대상 DB는 언제든 `Rebuild.sql` 로 재생성 가능하다.

**완료조건:** `Rebuild.sql` 이 정상 경로에서 exit 0, 잘못된 컨텍스트에서 exit 1 + `Msg 50021`. **둘 다 실제로 관측**했을 때만 완료다. `Net461MvpSample` 이 무사한 것을 확인한다.

---

## Task T06: `deploy/01_Schema.sql` — 7 Table · 제약 · Index · Sequence

**목적:** `04` §8의 물리 스키마를 한 글자도 바꾸지 않고 구현한다 (G05, G06).

**관련 Baseline 위치:** `04` §8.1~§8.7 (테이블 정의), §9 (FK), §10.1 (수량), §11.1 (Index), §12 (Sequence).

**선행조건:** `T05` 완료.

**Files:**
- Create: `deploy/01_Schema.sql`

**Interfaces:**
- Produces: 7 Table / PK 7 / FK 6 / UQ 2 / UX 1 / NCI 5 / Sequence 1. Trigger 0 / TVP 0.

**금지사항:** 컬럼 추가·삭제·이름변경·타입변경 금지. Trigger·FK Cascade·추가 Index 금지. `IF NOT EXISTS` 가드를 넣지 않는다 (clean-create).

- [ ] **Step 1: RED — 아직 아무 객체도 없음을 확인**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W \
  -Q "SELECT 'tables=' + CONVERT(varchar(5), COUNT(*)) FROM sys.tables;"
```

Expected: `tables=0`

- [ ] **Step 2: `deploy/01_Schema.sql` 헤더 — FK 역순 DROP**

```sql
SET NOCOUNT ON;
PRINT '--- 01_Schema 시작 ---';
GO
DROP TABLE IF EXISTS [dbo].[변경이력];
DROP TABLE IF EXISTS [dbo].[완료이력];
DROP TABLE IF EXISTS [dbo].[검사항목];
DROP TABLE IF EXISTS [dbo].[예약접수];
DROP TABLE IF EXISTS [dbo].[휴무일];
DROP TABLE IF EXISTS [dbo].[검사코드];
DROP TABLE IF EXISTS [dbo].[수검자];
DROP SEQUENCE IF EXISTS [dbo].[SEQ_HC_CHART_NO];
GO
```

- [ ] **Step 3: `수검자` — 17 컬럼**

```sql
CREATE TABLE [dbo].[수검자]
(
    [PatientId]          BIGINT          IDENTITY(1,1) NOT NULL,
    [ChartNo]            NVARCHAR(100)   NOT NULL,
    [Name]               NVARCHAR(100)   NOT NULL,
    [SocialNumber]       VARCHAR(13)     NOT NULL,
    [Birthday]           VARCHAR(8)      NOT NULL,
    [Gender]             CHAR(1)         NOT NULL,
    [EMail]              VARCHAR(200)    NULL,
    [CelNumberS]         VARCHAR(13)     NULL,
    [CelNumber]          VARCHAR(13)     NULL,
    [TelNumber]          VARCHAR(13)     NULL,
    [Zipcode]            VARCHAR(10)     NULL,
    [Address]            NVARCHAR(200)   NULL,
    [AddressDetail]      NVARCHAR(200)   NULL,
    [Memo]               NVARCHAR(MAX)   NULL,
    [HepatitisBExcluded] BIT             NOT NULL CONSTRAINT [DF_수검자_HEPATITIS_B_EXCLUDED] DEFAULT (0),
    [CreationDate]       DATETIME        NOT NULL CONSTRAINT [DF_수검자_CREATION_DATE]        DEFAULT (GETDATE()),
    [LastEditDate]       DATETIME        NOT NULL CONSTRAINT [DF_수검자_LAST_EDIT_DATE]       DEFAULT (GETDATE()),

    CONSTRAINT [PK_수검자] PRIMARY KEY CLUSTERED ([PatientId]),
    CONSTRAINT [UQ_수검자_CHART_NO]       UNIQUE ([ChartNo]),
    CONSTRAINT [UQ_수검자_SOCIAL_NUMBER]  UNIQUE ([SocialNumber]),

    CONSTRAINT [CK_수검자_CHART_NO_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([ChartNo]))) > 0),
    CONSTRAINT [CK_수검자_NAME_NOT_BLANK]     CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_수검자_SOCIAL_FORMAT]      CHECK (LEN([SocialNumber]) = 13 AND [SocialNumber] NOT LIKE '%[^0-9]%'),
    CONSTRAINT [CK_수검자_BIRTHDAY]           CHECK (LEN([Birthday]) = 8 AND [Birthday] NOT LIKE '%[^0-9]%' AND TRY_CONVERT(DATE, [Birthday], 112) IS NOT NULL),
    CONSTRAINT [CK_수검자_GENDER]             CHECK ([Gender] IN ('M','F')),
    CONSTRAINT [CK_수검자_CEL_NORMALIZED]     CHECK (([CelNumber] IS NULL AND [CelNumberS] IS NULL) OR ([CelNumber] IS NOT NULL AND [CelNumberS] = REPLACE([CelNumber], '-', ''))),
    CONSTRAINT [CK_수검자_CEL_DIGIT]          CHECK ([CelNumberS] IS NULL OR [CelNumberS] NOT LIKE '%[^0-9]%'),
    CONSTRAINT [CK_수검자_EDIT_DATE]          CHECK ([LastEditDate] >= [CreationDate])
);
GO
CREATE NONCLUSTERED INDEX [IX_수검자_NAME_BIRTHDAY]
    ON [dbo].[수검자] ([Name], [Birthday])
    INCLUDE ([PatientId], [ChartNo], [Gender], [CelNumber]);
GO
CREATE NONCLUSTERED INDEX [IX_수검자_BIRTHDAY]
    ON [dbo].[수검자] ([Birthday])
    INCLUDE ([PatientId], [ChartNo], [Name], [Gender], [CelNumber]);
GO
CREATE NONCLUSTERED INDEX [IX_수검자_CEL_NUMBER_S]
    ON [dbo].[수검자] ([CelNumberS])
    INCLUDE ([PatientId], [ChartNo], [Name], [Birthday], [Gender], [CelNumber])
    WHERE [CelNumberS] IS NOT NULL;
GO
```

- [ ] **Step 4: `검사코드` · `휴무일`**

```sql
CREATE TABLE [dbo].[검사코드]
(
    [ExamItemCode]         VARCHAR(10)   NOT NULL,
    [ExamItemName]         NVARCHAR(100) NOT NULL,
    [NexRuleCode]          VARCHAR(10)   NULL,
    [AdditionalExamCode]   VARCHAR(10)   NULL,
    [AdditionalGenderCode] CHAR(1)       NULL,
    [AdditionalActive]     BIT           NOT NULL CONSTRAINT [DF_검사코드_AEX_ACTIVE] DEFAULT (0),

    CONSTRAINT [PK_검사코드] PRIMARY KEY CLUSTERED ([ExamItemCode]),
    CONSTRAINT [CK_검사코드_CODE_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([ExamItemCode]))) > 0),
    CONSTRAINT [CK_검사코드_NAME_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([ExamItemName]))) > 0),
    CONSTRAINT [CK_검사코드_ROLE_REQUIRED]  CHECK ([NexRuleCode] IS NOT NULL OR [AdditionalExamCode] IS NOT NULL),
    CONSTRAINT [CK_검사코드_NEX_RULE]       CHECK ([NexRuleCode] IS NULL OR [NexRuleCode] IN ('NEX-01','NEX-02','NEX-03','NEX-04','NEX-05','NEX-06')),
    CONSTRAINT [CK_검사코드_AEX_CODE]       CHECK ([AdditionalExamCode] IS NULL OR [AdditionalExamCode] IN ('OPT01','OPT02','OPT03','OPT04','OPT05','OPT06','OPT07')),
    CONSTRAINT [CK_검사코드_AEX_GENDER]     CHECK ([AdditionalGenderCode] IS NULL OR [AdditionalGenderCode] IN ('A','M','F')),
    CONSTRAINT [CK_검사코드_AEX_GROUP]      CHECK
    (
        ([AdditionalExamCode] IS NULL     AND [AdditionalGenderCode] IS NULL     AND [AdditionalActive] = 0)
     OR ([AdditionalExamCode] IS NOT NULL AND [AdditionalGenderCode] IS NOT NULL)
    )
);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_검사코드_AEX_CODE]
    ON [dbo].[검사코드] ([AdditionalExamCode])
    WHERE [AdditionalExamCode] IS NOT NULL;
GO
CREATE TABLE [dbo].[휴무일]
(
    [HolidayDate] DATE          NOT NULL,
    [HolidayName] NVARCHAR(100) NOT NULL,
    [Active]      BIT           NOT NULL CONSTRAINT [DF_휴무일_ACTIVE] DEFAULT (1),
    [Memo]        NVARCHAR(500) NULL,

    CONSTRAINT [PK_휴무일] PRIMARY KEY CLUSTERED ([HolidayDate]),
    CONSTRAINT [CK_휴무일_NAME_NOT_BLANK] CHECK (LEN(LTRIM(RTRIM([HolidayName]))) > 0)
);
GO
```

- [ ] **Step 5: `예약접수` · `검사항목`**

```sql
CREATE TABLE [dbo].[예약접수]
(
    [WorkId]          BIGINT       IDENTITY(1,1) NOT NULL,
    [PatientId]       BIGINT       NOT NULL,
    [ReservationDate] DATE         NOT NULL,
    [TimeSlotCode]    CHAR(2)      NOT NULL,
    [StatusCode]      CHAR(3)      NOT NULL,
    [CreationDate]    DATETIME2(0) NOT NULL CONSTRAINT [DF_예약접수_CREATION_DATE]  DEFAULT (SYSDATETIME()),
    [LastEditDate]    DATETIME2(0) NOT NULL CONSTRAINT [DF_예약접수_LAST_EDIT_DATE] DEFAULT (SYSDATETIME()),
    [RowVersion]      ROWVERSION   NOT NULL,

    CONSTRAINT [PK_예약접수] PRIMARY KEY CLUSTERED ([WorkId]),
    CONSTRAINT [FK_예약접수_수검자] FOREIGN KEY ([PatientId])
        REFERENCES [dbo].[수검자] ([PatientId]) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT [CK_예약접수_TIME_SLOT] CHECK ([TimeSlotCode] IN ('AM','PM')),
    CONSTRAINT [CK_예약접수_STATUS]    CHECK ([StatusCode] IN ('RSV','RCP','CNR','CNC')),
    CONSTRAINT [CK_예약접수_EDIT_DATE] CHECK ([LastEditDate] >= [CreationDate])
);
GO
CREATE NONCLUSTERED INDEX [IX_예약접수_SLOT]
    ON [dbo].[예약접수] ([ReservationDate], [TimeSlotCode], [StatusCode])
    INCLUDE ([PatientId]);
GO
CREATE NONCLUSTERED INDEX [IX_예약접수_PATIENT_STATE_DATE]
    ON [dbo].[예약접수] ([PatientId], [StatusCode], [ReservationDate])
    INCLUDE ([TimeSlotCode]);
GO
CREATE TABLE [dbo].[검사항목]
(
    [WorkId]         BIGINT      NOT NULL,
    [ExamItemCode]   VARCHAR(10) NOT NULL,
    [ExamSourceCode] CHAR(3)     NOT NULL,

    CONSTRAINT [PK_검사항목] PRIMARY KEY CLUSTERED ([WorkId], [ExamItemCode]),
    CONSTRAINT [FK_검사항목_예약접수] FOREIGN KEY ([WorkId])
        REFERENCES [dbo].[예약접수] ([WorkId]) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT [FK_검사항목_검사코드] FOREIGN KEY ([ExamItemCode])
        REFERENCES [dbo].[검사코드] ([ExamItemCode]) ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT [CK_검사항목_SOURCE] CHECK ([ExamSourceCode] IN ('NEX','AEX'))
);
GO
```

- [ ] **Step 6: `완료이력` · `변경이력` · Sequence**

```sql
CREATE TABLE [dbo].[완료이력]
(
    [PatientId]      BIGINT NOT NULL,
    [CompletionDate] DATE   NOT NULL,

    CONSTRAINT [PK_완료이력] PRIMARY KEY CLUSTERED ([PatientId], [CompletionDate]),
    CONSTRAINT [FK_완료이력_수검자] FOREIGN KEY ([PatientId])
        REFERENCES [dbo].[수검자] ([PatientId]) ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO
CREATE TABLE [dbo].[변경이력]
(
    [HistoryId]     BIGINT        IDENTITY(1,1) NOT NULL,
    [CreationDate]  DATETIME2(0)  NOT NULL CONSTRAINT [DF_변경이력_CREATION_DATE] DEFAULT (SYSDATETIME()),
    [OperatorName]  NVARCHAR(50)  NULL,
    [OperationCode] VARCHAR(20)   NOT NULL,
    [TargetTable]   NVARCHAR(10)  NOT NULL,
    [TargetKey]     BIGINT        NULL,
    [ResultCode]    INT           NOT NULL,

    CONSTRAINT [PK_변경이력] PRIMARY KEY CLUSTERED ([HistoryId]),
    CONSTRAINT [CK_변경이력_OPERATION] CHECK (
        ([TargetTable] = N'수검자'   AND [OperationCode] IN ('PAT_INSERT','PAT_UPDATE'))
     OR ([TargetTable] = N'예약접수' AND [OperationCode] IN ('RSV_INSERT','RSV_UPDATE','RSV_CANCEL','RCP_ACCEPT','RCP_AEX','RCP_CANCEL'))),
    CONSTRAINT [CK_변경이력_RESULT_CODE] CHECK ([ResultCode] BETWEEN 0 AND 9 OR [ResultCode] BETWEEN 100 AND 799),
    CONSTRAINT [CK_변경이력_TARGET_KEY]  CHECK ([ResultCode] >= 100 OR [TargetKey] IS NOT NULL)
);
GO
CREATE SEQUENCE [dbo].[SEQ_HC_CHART_NO]
    AS BIGINT START WITH 1 INCREMENT BY 1
    MINVALUE 1 MAXVALUE 999999 NO CYCLE CACHE 50;
GO
PRINT N'PASS SCH-DEPLOY 스키마 배포 완료';
GO
```

- [ ] **Step 7: BOM 확인 후 실행**

```bash
head -c 3 deploy/01_Schema.sql | od -An -tx1
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i deploy/01_Schema.sql -o artifacts/logs/01_schema.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/01_schema.log | tail -5
```

Expected: `ef bb bf`, exit 0, `PASS SCH-DEPLOY`.

- [ ] **Step 8: 재실행 가능성 확인 (clean-create)**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i deploy/01_Schema.sql -o artifacts/logs/01_schema_2nd.log
echo "exit=$?"
```

Expected: exit 0. 두 번째 실행도 성공해야 한다 (`DROP IF EXISTS` → `CREATE`).

- [ ] **Step 9: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/deploy/01_Schema.sql
git commit -m "feat(phase4): 7개 물리 테이블 스키마 배포 스크립트 추가"
```

**회귀시험:** `T07` 의 `01_Schema_Tests.sql`.

**로그 경로:** `artifacts/logs/01_schema.log`

**Rollback/Cleanup:** `./scripts/rebuild.sh` 로 언제든 초기화.

**완료조건:** 최초 실행과 재실행이 모두 exit 0.

---

## Task T07: `tests/01_Schema_Tests.sql` — 인벤토리 16건

**목적:** 스키마가 `04` §10.1 수량과 정확히 일치하고 금지 객체가 0건임을 자동 검증한다 (G04, G05, G06).

**관련 Baseline 위치:** `04` §10.1, 스펙 §34.

**선행조건:** `T06` 완료.

**Files:**
- Create: `tests/01_Schema_Tests.sql`

**Interfaces:**
- Consumes: `T06` 이 만든 7 Table + Sequence
- Produces: `PASS SCH-001` ~ `PASS SCH-016`

**금지사항:** 스키마를 고쳐서 테스트를 통과시키지 않는다. 불일치가 나오면 `04` §8을 다시 읽고 스키마를 바로잡는다.

- [ ] **Step 1: RED — 테스트를 먼저 쓰고 일부러 틀린 기대값으로 실패를 본다**

먼저 `SCH-001` 만 작성하되 기대값을 `8` 로 둔다.

```sql
SET NOCOUNT ON;
DECLARE @Fail INT = 0;
IF ((SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) = 8)
    PRINT 'PASS SCH-001 사용자 테이블 8개';
ELSE BEGIN PRINT 'FAIL SCH-001 사용자 테이블 수 불일치'; SET @Fail += 1; END
IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
GO
```

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/01_Schema_Tests.sql -o artifacts/logs/test_01_red.log
echo "exit=$?"
```

Expected: exit **1**, `FAIL SCH-001`. 테스트 하네스가 실제로 실패를 잡아낸다는 증거다.

- [ ] **Step 2: 기대값을 7로 고치고 나머지 15건 추가**

```sql
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
 (N'휴무일', N'HolidayDate',          N'date',      NULL,  0),
 (N'휴무일', N'HolidayName',          N'nvarchar',  100,   0),
 (N'휴무일', N'Active',               N'bit',       NULL,  0),
 (N'휴무일', N'Memo',                 N'nvarchar',  500,   1),
 -- 완료이력 2행
 (N'완료이력', N'PatientId',            N'bigint',    NULL,  0),
 (N'완료이력', N'CompletionDate',       N'date',      NULL,  0),
 -- 변경이력 7행
 (N'변경이력', N'HistoryId',            N'bigint',    NULL,  0),
 (N'변경이력', N'CreationDate',         N'datetime2', NULL,  0),
 (N'변경이력', N'OperatorName',         N'nvarchar',  50,    1),
 (N'변경이력', N'OperationCode',        N'varchar',   20,    0),
 (N'변경이력', N'TargetTable',          N'nvarchar',  10,    0),
 (N'변경이력', N'TargetKey',            N'bigint',    NULL,  1),
 (N'변경이력', N'ResultCode',           N'int',       NULL,  0);

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
```

`[X]` **초안 오류**: `SCH-015` 는 55개 컬럼 중 **2개**만 `EXISTS` 로 확인했고 `SCH-016` 은 `COUNT(*) >= 20` 이었다. 제약 하나가 사라져도 PASS 하고 제약 **이름**은 대조하지 않았다. 스펙 §34가 "전건 일치"를 약속했으므로 개수 비교를 증거로 쓸 수 없다. **`EXCEPT` 양방향 + 불일치 시 차집합 출력**으로 교체한다.

`[I]` `@ExpCol` 의 47행은 `04` §8을 **한 줄씩 옮겨 적는다.** 축약하지 않는다 — 이 표가 곧 회귀 방지 장치다.

- [ ] **Step 3: 실행 — `SCH-013`·`SCH-014` 만 FAIL 이어야 한다**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u \
       -i tests/01_Schema_Tests.sql -o artifacts/logs/test_01.log
echo "exit=$?"
iconv -f UTF-16 -t UTF-8 artifacts/logs/test_01.log | grep -E '^(PASS|FAIL)'
```

Expected: exit **1**. `SCH-001`~`SCH-012`, `SCH-015`~`SCH-018` **16건 PASS**, `SCH-013`·`SCH-014` FAIL (TVF·SP 미구현이므로 정상). 2026-09-04 R3 배포에서 실측 확인했다.

다른 항목이 FAIL이면 스키마를 `04` §8 기준으로 바로잡는다.

- [ ] **Step 4: 객체 인벤토리 보고서 생성**

```bash
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -h -1 -W -Q "
SET NOCOUNT ON;
SELECT 'Table       = ' + CONVERT(varchar(5), COUNT(*)) FROM sys.tables WHERE is_ms_shipped=0;
SELECT 'PK          = ' + CONVERT(varchar(5), COUNT(*)) FROM sys.key_constraints WHERE type='PK';
SELECT 'FK          = ' + CONVERT(varchar(5), COUNT(*)) FROM sys.foreign_keys;
SELECT 'UQ          = ' + CONVERT(varchar(5), COUNT(*)) FROM sys.key_constraints WHERE type='UQ';
SELECT 'FilteredUX  = ' + CONVERT(varchar(5), COUNT(*)) FROM sys.indexes WHERE is_unique=1 AND has_filter=1;
SELECT 'Sequence    = ' + CONVERT(varchar(5), COUNT(*)) FROM sys.sequences;
SELECT 'Trigger     = ' + CONVERT(varchar(5), COUNT(*)) FROM sys.triggers WHERE is_ms_shipped=0;
SELECT 'TVF         = ' + CONVERT(varchar(5), COUNT(*)) FROM sys.objects WHERE type='IF';
SELECT 'Procedure   = ' + CONVERT(varchar(5), COUNT(*)) FROM sys.procedures;
" > artifacts/reports/object-inventory.txt
cat artifacts/reports/object-inventory.txt
```

- [ ] **Step 5: Commit**

```bash
cd /d/AIDEV/HealthCheckupReservationReception
git add database/tests/01_Schema_Tests.sql database/artifacts/reports/object-inventory.txt
git commit -m "test(phase4): 스키마 인벤토리 검증 16건 추가"
```

**회귀시험:** 이후 모든 배포 후 `tests/01_Schema_Tests.sql` 재실행.

**로그 경로:** `artifacts/logs/test_01.log`

**Rollback/Cleanup:** 없음 (읽기 전용 테스트).

**완료조건:** RED(잘못된 기대값)에서 exit 1을 실제로 관측했고, 수정 후 스펙 §45.2 의 `SCH` 전건이 PASS. 단 `SCH-013`(TVF 4) 과 `SCH-014`(SP 15) 는 `T14`·`T30` 이전이면 FAIL 이 정상이다.
