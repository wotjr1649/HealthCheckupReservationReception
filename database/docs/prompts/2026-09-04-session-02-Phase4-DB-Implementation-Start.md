# Phase 4 DB 구현 착수 — T01~T07 실행 후 중단

작업 디렉터리: `D:\AIDEV\HealthCheckupReservationReception\database`
선행 세션: `5917d432-1a6a-43b0-af93-1d55c13917ec` (문서 확정까지 완료, SQL 미실행)

---

## 1. 목표

계획 `T01`~`T07` **만** 실행하고 멈춘다. `T08` 이후로 넘어가지 않는다.

배포 배관(git·Preflight·Rebuild·deploy.sh·스키마)이 실제로 도는지 먼저 확인하고 그 결과를 사용자에게 보고하는 것이 이 세션의 전부다. 전체를 한 번에 돌리면 어디서 어긋났는지 분리할 수 없다.

## 2. 지금 사실인 것

| 항목 | 상태 |
|---|---|
| 스펙 | `docs/phase4/06_DB_Transaction_Security_Seed_CANDIDATE.md` **v0.3** (`CANDIDATE / IMPLEMENTATION READY`) |
| 구현계획 | `docs/phase4/plans/` 9개 파일. 진입점은 `2026-09-04-phase4-database-implementation.md` |
| Task | `T01`~`T37` + `T14b` (38개) |
| 문서 게이트 | `tools/verify-docs.js` — **PASS 15 / FAIL 0, exit 0** (2026-09-04 마지막 실행) |
| SQL 실행 | **0회.** 어떤 DB 객체도 아직 만들어지지 않았다 |
| `database/` 실물 | `docs/prompts/*.md`, `tools/verify-docs.js` **뿐** |
| git | ROOT에 저장소 **없음**. `T01`이 최초로 만든다 |

**먼저 읽을 것** (순서대로):
1. `docs/phase4/plans/2026-09-04-phase4-database-implementation.md` — Global Constraints·금지목록·실행 관용구
2. `docs/phase4/plans/01-preflight-schema.md` — `T01`~`T07` 전문
3. 스펙 §6 Decision Log, §7 파일구조, §8 배포전략, §45.2 Test ID 카탈로그, §45.3 게이트

## 3. 승인된 결정 (스펙 §6 Decision Log에 기록됨 — 이 세션이 새로 받을 필요 없다)

| ID | 승인 내용 |
|---|---|
| `D4-002` | ROOT `git init` + `00`~`05`·winforms 초기 commit + tag + `phase4-database` branch |
| `D4-003` | 대상 DB `HealthCheckupReservationReceptionDb` **생성 + Drop/Recreate 승인**, 가드 필수 |
| `D4-004` / `D4-004b` | 대상 SQL Server 2025 고정, 허용목록 방식. `CREATE OR ALTER`·`DROP … IF EXISTS` 배포 배관 한정 허용 |
| `D4-005` | Clean-create — `01_Schema.sql`이 FK 역순 `DROP IF EXISTS` 후 `CREATE` |
| `D4-006` | git identity `JS <JS@DESKTOP-DP7KRE4>`, **repo-local만** |

`T01`의 ROOT `git init`과 `T04`의 대상 DB 생성은 되돌리기 어려운 작업이다. 위 결정이 근거이므로 진행하되, **실행 직전에 무엇을 하는지 한 줄로 알리고** 진행한다.

## 4. 절대 금지

```
git config --global 변경 / remote 추가 / push
git reset · clean · checkout -- · force push · 기존 tag 이동
docs/baseline/** 파일 내용 수정·이동·삭제      (git add 는 내용을 바꾸지 않으므로 허용)
winforms/** 수정
Net461MvpSample DB 접속·변경·삭제              (같은 인스턴스에 있다)
sa 사용/활성화 · sysadmin 부여 · TRUSTWORTHY ON · xp_cmdshell · CLR · Linked Server
실제 주민등록번호를 Seed·Test 에 사용
비밀정보를 파일·로그·명령행에 평문 저장
T08 이후 Task 진행
```

## 5. 다른 세션과의 충돌 — 반드시 지킬 것

`docs/baseline/output/` 에 **다른 세션(`48b4d967-…`)이 지금 xlsx·pptx 를 쓰고 있다.**

`T01` Step 5 의 `git add` 는 **이미 기준선 6개 파일만 경로로 명시하도록 고쳐져 있다.** 그 형태를 바꾸지 마라. `git add docs/baseline` 처럼 디렉터리를 통째로 넣으면:

1. 기준선 tag 에 기준선이 아닌 진행중 파일이 들어가고
2. 쓰기 도중의 blob 이 봉인될 수 있으며
3. `verify-baseline.sh` 는 6개 파일만 대조하므로 그 손상을 **잡지 못한다**

`.gitignore` 에 `docs/baseline/output/` 이 들어 있다. `git status --short` 에 `docs/baseline/output/` 이 보이면 그 자체가 오류다.

## 6. T01~T07 이 만드는 것

| Task | 산출 | 되돌리기 |
|---|---|---|
| `T01` | `scripts/verify-baseline.sh`, ROOT `.gitignore`, git init·commit·tag `baseline-HC-RSV-RCP-20260903-R2`·branch `phase4-database` | **어렵다** |
| `T02` | `scripts/verify-winforms-unchanged.sh`, `artifacts/reports/winforms-manifest.txt` | 쉽다 |
| `T03` | `CLAUDE.md`, `README.md` | 쉽다 |
| `T04` | `deploy/00_Preflight.sql`, **대상 DB 생성**, `artifacts/reports/otherdb_before.txt` | DB는 `T05` Rebuild가 관리 |
| `T05` | `Rebuild.sql`, `Deploy.sql`, `scripts/deploy.sh`·`rebuild.sh`·`test.sh`·`concurrency-test.sh`, `deploy/01`~`09` stub | 쉽다 |
| `T06` | `deploy/01_Schema.sql` — 7 Table + PK 7 / FK 6 / UQ 2 / Filtered UX 1 / NCI 5 / Sequence 1 / CHECK 22 / DEFAULT 14 | Rebuild |
| `T07` | `tests/01_Schema_Tests.sql` — `SCH-001`~`SCH-018` | 쉽다 |

## 7. 실행 관용구 (틀리면 진단이 사라진다)

```bash
# 오케스트레이터: -e 를 쓰지 않는다. 실패한 그 줄에서 셸이 끝나 RC 수집도 로그 출력도 안 된다(실측).
set -uo pipefail
RC=0
sqlcmd -S "$SRV" -E -d "$DB" -b -u -i "<파일>" -o "artifacts/logs/<로그>.log" || RC=$?
iconv -f UTF-16 -t UTF-8 "artifacts/logs/<로그>.log" | grep -E '^(PASS|FAIL|SKIP|INFO|Msg )' || true
echo "exit=$RC"
```

- SQL 입력 파일은 **UTF-8 with BOM**. 한글 객체명이 깨지지 않는 유일한 조합이다. `head -c 3 <파일> | od -An -tx1` → `ef bb bf` 로 확인한다.
- 로그는 `-u`(UTF-16 출력) + `iconv -f UTF-16`. **`UTF-16LE` 를 쓰지 마라** — sqlcmd 출력에는 BOM이 있다.
- `tee` 금지(파이프라인 종료코드가 흐려진다), `| grep -q` 금지(SIGPIPE로 `pipefail` fail-open — 실측 확인됨).
- `verify-baseline.sh`·`verify-winforms-unchanged.sh` 는 순수 검증이라 `set -euo pipefail` 이 **맞다**. 오케스트레이터만 `-e` 를 뺀다(스펙 §8.4).

## 8. 완료 판정 — 이것이 전부 참이어야 이 세션이 끝난다

```
1. node tools/verify-docs.js                     → exit 0 (PASS 15 / FAIL 0 유지)
2. ./scripts/verify-baseline.sh                  → "=== 6/6 ===", exit 0
3. ./scripts/verify-winforms-unchanged.sh        → exit 0
4. git tag -l                                    → baseline-HC-RSV-RCP-20260903-R2 하나
5. git status --short                            → docs/baseline/output/ 가 보이지 않는다
6. deploy/00_Preflight.sql (대상 DB)             → PRE-001~006 PASS, exit 0
7. master 컨텍스트에서 00_Preflight.sql          → exit 1 + Msg 50011  (음성 시험)
8. ./scripts/deploy.sh                           → exit 0
9. tests/01_Schema_Tests.sql                     → SCH-013·SCH-014 를 뺀 전건 PASS
10. Net461MvpSample 이 무사하다                   → sys.databases 조회로 확인
```

`SCH-013`(Inline TVF 4) 과 `SCH-014`(SP 15) 는 `T14`·`T30` 이전이므로 **FAIL 이 정상**이다. 그 둘이 PASS 면 오히려 이상하다.

`T07` Step 1 은 RED(잘못된 기대값으로 exit 1 관측)를 먼저 한다. 건너뛰지 마라 — 테스트가 실제로 실패할 수 있음을 증명하지 않으면 이후 PASS는 증거가 아니다.

## 9. 검증되지 않은 것 — 마주치면 이렇게 하라

| 항목 | 상태 | 대응 |
|---|---|---|
| 모든 SQL | **한 번도 실행된 적 없다** | 배포 중 실패는 예상 범위다. 실패하면 계획을 고치고 그 사유를 기록한 뒤 다시 돌린다 |
| `50012` 가드 | `SERVERPROPERTY('MachineName') = 'DESKTOP-DP7KRE4'` 대조 | 다른 PC에서 실행하면 **의도적으로** 실패한다. 값을 바꾸지 말고 사용자에게 알려라 |
| `50022` 가드 | `DROP DATABASE` 직전 계약 밖 사용자 Table 0개 확인 | 3부 이름(`[HealthCheckupReservationReceptionDb].[sys].[tables]`)으로 조회한다. 동적 SQL 금지 |
| `01_Schema.sql` DDL | 계획 `T06` 의 코드블록이 축약본일 수 있다 | 기준선 `docs/baseline/04_DB_Design.md` §8 이 컬럼·제약의 출처다. 계획과 다르면 **기준선을 따르고** 계획을 고친다 |
| `SCH-015` 의 55행 | 기준선 `04` §8 에서 기계 생성했다 | 실측 불일치가 나면 생성기가 아니라 DDL 을 의심하라 |
| 게이트의 한계 | `verify-docs.js` 는 **정합성만** 본다 | 시험이 의미적으로 성립하는지는 못 잡는다. 스펙 §44.7 "3차" 표에 그 부류 5건이 기록돼 있다 |

## 10. 하지 말아야 할 것

- `T08` 이후를 "이왕 하는 김에" 진행하는 것. 이 세션의 범위는 `T01`~`T07` 이다.
- 계획서를 고치지 않은 채 SQL만 다르게 쓰는 것. 계획과 실물이 어긋나면 다음 세션이 같은 함정에 빠진다.
- 완료조건에 건수를 다시 적는 것. Test ID 건수의 유일한 출처는 스펙 **§45.2** 이고 `V12` 가 기계적으로 막는다.
- 실패를 `SKIP` 이나 `NOT RUN` 으로 바꿔 통과시키는 것.

## 11. 끝나면

`T01`~`T07` 결과와 §8 판정 10개를 표로 보고하고 **멈춘다.** `T08` 진행 여부는 사용자가 정한다.
