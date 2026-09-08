# 검진 예약·접수 관리 프로그램 — DB Transaction·잠금·보안·Seed 구현 계약서

- **문서명:** `06_DB_Transaction_Security_Seed.md`
- **상태:** `FINAL / GO / READ-ONLY` — SQL 실행검증 완료. §42 Gate 판정은 전부 실측이다
- **위치:** `docs/baseline/` — 2026-09-08 입주. 이제 이 문서를 고치는 것도 재봉인이다 (ROOT `AGENTS.md` §2)
- **문서 버전:** v1.4  (v1.0 → R4 한글화 반영. v1.1 → R5 에서 §42 Gate 세 칸을 실측에 맞췄다. v1.2 → R6 에서 §42 의 미래 단정 한 줄을 걷었다. v1.3 → R7 계약 재설계)
- **기준일:** 2026-09-08  ·  **실행검증일:** 2026-09-07(R3 회귀) · 2026-09-08(R4 회귀 · R5 회귀 회차 `C`)
- **기준선 ID:** `HC-RSV-RCP-20260908-R7`  (직전 `HC-RSV-RCP-20260908-R6`)
- **R7 반영 범위:** 계약 재설계다. 휴무일이 `휴무구분`·감사·`행버전`을 얻고 자체휴무일 CRUD SP 4개가 생겨 SP 16 → 20 이 되었다. 동시성 토큰이 `행버전` 하나로 통일되며 §26 의 `+4ms` 단조증가가 **사라진다**. 공휴일 Seed 가 2년치로 늘고 만료 경고 게이트가 붙는다. §42 `G05` 는 수치 사본을 걷고 포인터가 된다
- **R5 반영 범위:** 계약은 한 줄도 바뀌지 않았다. §42 의 `G00`·`G13`·`G16` 이 실제 게이트와 어긋나 있던 것을 고치고 회차 `C` 를 등재했다 — 이 문서가 봉인 안으로 들어와 `06` 이 7번째 봉인이 되었는데 `G00` 은 여전히 `6/6` 이었고, `G13 (c)` 는 이 문서가 요구한 증거 파일 없이 `REVIEWED` 였으며, `G16` 은 `v1.0` 을 근거로 들고 있었다
- **R4 반영 범위:** 이 문서의 SP 이름·Parameter·Result Set 컬럼 표기를 `05` v3.0 에 맞췄다. Transaction·잠금·Seed·시험 계약의 **내용**은 바뀌지 않는다 — 이름만 바뀌었다. 절차·건수·판정은 그대로다
- **대상 SQL Server:** `.\SQLEXPRESS` — Microsoft SQL Server 2025 Express `17.0.1125.2` (RTM), 로컬 전용
- **Database:** `HealthCheckupReservationReceptionDb`
- **Source of Truth:** `00_Project_Policy.md` → `01_Process_Definition.md` → `02_Function_Definition.xlsx` → `03_Wireframe_Definition.md` → `04_DB_Design.md` → `05_DB_Rule_SP_Contract.md`. 버전·상태·기준선 ID 는 **§4.1 한 곳에만** 적는다 — 예전에 여기에도 적어 두었다가 `00`·`01`·`02`·`03` 이 `v1.2` 로 굳은 채 남았다 (ROOT `AGENTS.md` §6)
- **변경 통제:** 본 문서는 00~05의 객체명·Parameter·Result Set·ResultCode·업무의미를 변경하지 않는다. 본 문서가 확정하는 것은 Transaction·잠금·권한·Seed·테스트·배포의 **구현 상세**뿐이다.

## 라벨 규약

| 라벨 | 의미 |
|---|---|
| `[B]` | Baseline 00~05에서 직접 확정된 사실 |
| `[D]` | 사용자와 grilling으로 확정한 결정 |
| `[I]` | Baseline을 변경하지 않는 구현 상세 |
| `[A]` | 아직 승인받지 않은 가정 — **본 문서 0건** |
| `[X]` | 충돌·이탈·차단사항 |

---

# 1. 문서정보

| 항목 | 값 |
|---|---|
| 문서명 | `06_DB_Transaction_Security_Seed.md` |
| 상태 | `FINAL / GO / READ-ONLY` |
| 버전 | v1.1 |
| 기준일 | 2026-09-08 (실행검증 2026-09-07 R3 · 2026-09-08 R4) |
| 기준선 ID | `HC-RSV-RCP-20260908-R4` |
| 위치 | `docs/baseline/` — 2026-09-08 입주 (ROOT `AGENTS.md` §2.1) |
| 대상 SQL Server | SQL Server 2025 Express 17.0.1125.2 / 인스턴스 `.\SQLEXPRESS` |
| 선행 문서 | `04_DB_Design.md`(Phase 2), `05_DB_Rule_SP_Contract.md`(Phase 3) |
| 후속 문서 | `07_UI_DB_Matrix_Final_Validation.md` (Phase 5) |

---

# 2. 목적과 Phase 4 범위

## 2.1 목적

`04`가 확정한 7개 물리 테이블과 `05`가 확정한 4개 Inline TVF·15개 Stored Procedure 계약을 **변경하지 않고**, 실제로 배포·실행·검증 가능한 SQL 구현 계약을 확정한다.

## 2.2 Phase 4 범위 (IN SCOPE)

```text
물리 스키마 DDL   6 Table + PK 6 / FK 2 / UQ 2 / UX 1 / NCI 4 / Sequence 1
Master Seed       검사코드 19행 + 휴무일 40행 (§14)
Inline TVF 4개 구현
Stored Procedure 20개 구현
Write SP 11개의 Transaction 경계·오류 처리·부분저장 차단
Patient / 주민번호 / 차트번호 / Work / 시간대 / 휴무일 직렬화 및 잠금 획득 총순서
행버전 단일 동시성 토큰, No-op 경계
Database Role / User WITHOUT LOGIN / GRANT EXECUTE 15건
Test Fixture, Rule Test, SP Contract Test, Rollback / Concurrency / Security / Clean Rebuild Test
배포·재구축 Script, 로그·증거 산출물
```

## 2.3 산출 위치

```text
D:\AIDEV\HealthCheckupReservationReception\database\**       SQL·Script·도구·산출물
D:\AIDEV\HealthCheckupReservationReception\docs\phase4\**    스펙·계획 문서
```

---

# 3. Out of Scope

```text
00~05 기준선 수정                         절대 금지
WinForms Source·App.config 수정           Phase 5 범위
C# DTO·Enum·Repository 구현               Phase 5 범위
실제 Login / Password / Credential 생성    서버 수준 변경. Phase 5 배포 시 사용자 결정
운영 DB 접속, Linked Server, 외부 네트워크 호출
sa 사용, sysadmin 부여, TRUSTWORTHY ON, xp_cmdshell, Ad Hoc Distributed Queries, CLR
추가 물리 테이블, TVP, Trigger, DELETE SP, 범용 Rule Engine, 공통 코드 테이블
성능 튜닝 목적의 보조 Index (04 §0.4.2 조건 충족 전까지)
Extended Events / trace flag 세션 (서버 수준 객체)
```

---

# 4. Baseline 검증결과

## 4.1 파일·버전·상태

| 파일 | 버전 | 상태 | 기준선 ID |
|---|---|---|---|
| `00_Project_Policy.md` | v2.1 | FINAL / GO / READ-ONLY | `HC-RSV-RCP-20260908-R7` |
| `01_Process_Definition.md` | v2.0 | FINAL / GO / READ-ONLY | `HC-RSV-RCP-20260904-R3` |
| `02_Function_Definition.xlsx` | v2.1 | FINAL / GO / READ-ONLY | `HC-RSV-RCP-20260908-R7` |
| `03_Wireframe_Definition.md` | v1.4 | FINAL / GO / READ-ONLY | `HC-RSV-RCP-20260908-R7` |
| `04_DB_Design.md` | v3.2 | FINAL / GO / READ-ONLY | `HC-RSV-RCP-20260908-R7` |
| `05_DB_Rule_SP_Contract.md` | v3.1 | FINAL / GO / READ-ONLY | `HC-RSV-RCP-20260908-R7` |
| `06_DB_Transaction_Security_Seed.md` | v1.4 | FINAL / GO / READ-ONLY | `HC-RSV-RCP-20260908-R7` |

`[!]` **기준선 ID 는 문서마다 "마지막 봉인 회차" 다** — 세트 하나에 ID 하나가 아니다.
R7 은 여섯을 함께 열었으므로 `01` 만 R3 표기를 유지한다 — `01_Process_Definition.md` 는 한 바이트도 열지 않았다 (ROOT `AGENTS.md` §2.2).

동일 디렉터리 및 `docs` 전체에서 `(1)`·`Candidate`·`후보`·`개선본`·`백업`·`old`·`copy` 사본 **0건**을 확인했다.

## 4.2 SHA-256 — 값은 여기 적지 않는다

**단일 출처는 `database/scripts/verify-baseline.sh` 다.** 이 문서는 값을 복사하지 않는다.

```
./scripts/verify-baseline.sh        # 파일별 실측 해시와 판정을 출력한다
```

`[X]` **예전에는 이 자리에 6개 파일의 SHA-256 을 옮겨 적고 "게이트의 하드코딩 값과 일치한다" 고
선언했다.** R4 재봉인으로 `04`·`05` 의 해시가 바뀌자 그 문장이 **거짓이 되었고 아무도 몰랐다.**
같은 값을 두 곳에 두면 한쪽은 반드시 뒤처진다 — ROOT `AGENTS.md` §6 이 이 사건에서 나왔다.

기준선 `.md` 는 전부 **CR=0 (LF 전용)** 이라 CRLF 정규화가 no-op 이며 raw hash 와 정규화 hash 가
같다. R2 시점 `03` 불일치 판정은 §44 `D4-001` 에 보존한다.

## 4.3 XLSX 전수검증

`02_Function_Definition.xlsx`에는 `dimension` 요소가 없어 셀 실측으로 Used Range를 산출했다.

| 순서 | Sheet | 기대 Used Range | 실측 | 판정 |
|---:|---|---|---|:---:|
| 1 | `문서정보` | `A1:F8` | `A1:F8` | OK |
| 2 | `기능정의` | `A1:I39` | `A1:I39` | OK |
| 3 | `업무Rule` | `A1:J39` | `A1:J39` | OK |
| 4 | `개발범위` | `A1:H32` | `A1:H32` | OK |
| 5 | `설계근거` | `A1:F25` | `A1:F25` | OK |
| 6 | `DB추적` | `A1:G20` | `A1:G20` | OK |
| 7 | `최종검수` | `A1:E33` | `A1:E33` | OK |

`[R7]` 위 표는 R3 시점 실측이다. R7 에서 `기능정의`·`DB추적`·`업무Rule` 이 한 행씩 늘고 `문서정보` 머리가 바뀌었다 — 현재 값은 `tools/docgen/xlsx/build_02.js` 가 매 실행에서 파싱해 단언한다(38행 / 9열 / 고유 ID 18).

고정값 실측 결과:

```text
Function ID 18개          F-PAT 3 / F-RSV 3 / F-RCP 3 / F-COM 9
기능행 38행               행 4~41
TGT 5 / NEX 7 / AEX 5 / HOL 6
AEX Master OPT01~OPT07 7종
DB추적 6 Table + SEQ_HC_CHART_NO
"NEX 실제 8~11행"          DB추적 F-COM-004 비고에 명시
"예약변경 현재 Work 제외"   DB추적 F-COM-005 비고에 명시
```

---

# 5. 환경조사 결과

| 항목 | 실측값 |
|---|---|
| OS | Windows 11 Pro 10.0.26200 (`DESKTOP-DP7KRE4`) |
| Shell | Git Bash (MSYS2) |
| SQL Server | `.\SQLEXPRESS` — `17.0.1125.2` RTM, **SQL Server 2025 Express Edition (64-bit)**, EngineEdition 4 |
| 인스턴스 수 | 1개 (운영·공용 정황 없음) |
| Authentication | Mixed. Windows 통합인증 접속 성공 |
| 접속 계정 | `DESKTOP-DP7KRE4\JS` (sysadmin) |
| Server Collation | `Korean_Wansung_CI_AS` (`model`도 동일) |
| Recovery(model) | `SIMPLE` |
| 기본 데이터/로그 경로 | `코드:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\DATA\` |
| KST | `SYSDATETIMEOFFSET()` = `+09:00`, `DATEPART(TZOFFSET, …)` = **540** |
| 대상 DB | `HealthCheckupReservationReceptionDb` — **미존재** |
| 동일 인스턴스 타 DB | `Net461MvpSample` 1건 — **접근·변경 금지 대상** |
| `xp_cmdshell` / `clr enabled` | 둘 다 `0` (비활성). **활성화하지 않는다** |
| Compatibility Level | 지원 목록 8개 = `{100,110,120,130,140,150,160,170}`. 현재 모든 DB가 `170` |
| sqlcmd | `15.0.1300.359` (ODBC 계열, Go sqlcmd 아님) |
| 기타 도구 | `node v24.19.0` 있음 / `git 2.55.0` 있음 / **`python` 없음** / **`dotnet` 없음** |
| Git | ROOT·하위 전부 `.git` **미존재**. `user.name`·`user.email` 전역 미설정 |

## 5.1 sqlcmd 인코딩 실측 `[I]`

| 입력 방식 | 결과 |
|---|---|
| UTF-8 (BOM 없음), `-f` 없음 | **실패** — `Msg 105/102` 구문오류, 한글 깨짐 |
| UTF-8, `-f 65001` | 입력은 통과하나 출력이 mojibake — **불안정** |
| **UTF-8 + BOM, `-f` 없음** | **완전 왕복 성공 — 채택** |
| UTF-16LE + BOM | 완전 왕복 성공 (대안) |

| 출력 방식 | 결과 |
|---|---|
| `-o file` (기본) | CP949(ANSI) |
| **`-o file -u`** | UTF-16LE + BOM — 한글 정상. **채택** |

모든 `.sql`은 **UTF-8 with BOM**으로 저장한다. `05` §1.7 "SQL Script는 UTF-8로 저장한다"를 만족한다.

## 5.2 exit code 실측

| 상황 | `sqlcmd -b` exit code |
|---|---:|
| 정상 | 0 |
| `RAISERROR(...,16,1)` | 1 |
| `THROW` | 1 |
| 미존재 DB 접속 | 1 |

## 5.3 T-SQL 동작 실측 (설계 근거)

| # | 검증 대상 | 실측 결과 |
|---:|---|---|
| 1 | `sp_getapplock` `@LockOwner=Transaction` | 최초 rc=0, 동일 Transaction 재획득 rc=0, `sys.dm_tran_locks`에 `APPLICATION` 1건, `ROLLBACK` 시 자동 해제 |
| 2 | `HASHBYTES(SHA2_256, ssn)` + `CONVERT(CHAR(64),…,2)` | 자원명 71자 — **주민번호 원문이 DMV에 노출되지 않음** |
| 3 | `DATEDIFF(DAY,0,@d) % 7` | `SET DATEFIRST 7`과 `1`에서 **동일 결과** (월=0 … 토=5, 일=6) |
| 4 | `TRY` 블록 내 `ROLLBACK` 후 계속 진행 | `XACT_STATE()=0`, 후속 문 정상 실행 |
| 5 | `DATETIME` 정밀도 | `+1ms` → **변화 없음**, `+4ms` → `.000`에서 `.003`으로 전진 |
| 6 | `sys.dm_exec_describe_first_result_set` | 작동. `bit`/`int`/`nvarchar(300)`/`varchar(50)`/`datetime2(7)` 정확 반환 |
| 7 | sqlcmd 다중 Result Set 출력 | 각 RS가 헤더행 + 대시 구분선 + 데이터행. **0행 RS도 헤더·구분선 출력** |
| 8 | 요일 (`%7`) | `2026-12-25` → 4(**금**), `2026-12-26` → 5(**토**) |
| 9 | 만나이 공식 | `2006-10-01`생@`2026-10-01` → **20**, `2006-10-02`생 → **19**, `1972-02-29`생 → **54** |

## 5.4 WinForms 조사 (읽기 전용)

| 항목 | 실측 |
|---|---|
| Solution | `winforms/HealthCheckupReservationReception.sln` |
| Project | `HealthCheckupReservationReception.WinForms` |
| TargetFramework | `v4.6.1` |
| DevExpress 참조 | **없음** |
| `App.config` | `startup` 요소만 존재. **`connectionStrings` 섹션 없음** → `HealthCheckupDb` 키 미정의 |
| DB 호출 코드 | **0건** |

WinForms 현재 상태에 맞추어 DB 계약을 변경하지 않는다. Phase 4에서 WinForms는 **단 한 바이트도 수정하지 않는다.**

---

# 6. Decision Log

| ID | 결정 제목 | 선택안 | 배제한 대안 | 근거 | 승인 |
|---|---|---|---|---|:---:|
| `D4-001` | `03` baseline hash 불일치 판정 | 현재 `03` 파일을 기준본으로 확정하고 Deviation 기록 후 진행 | 원본 재공급 / `IMPLEMENTATION BLOCKED` / 인계문서 오류로 단정 | 개행 문제 아님이 확정됐으나 금지 marker 0건이라 §4.2-5 즉시중단 조건 미해당. Phase 4가 소비하는 `04`·`05`는 byte 일치. `03`의 DB 관련 수치가 `04`·`05`와 모순 없음 | **승인** |
| `D4-002` | Git·VCS 부재 처리 | ROOT `git init` + 00~05·winforms 초기 commit + tag + Phase 4 전용 branch | `database/`만 init / Git 미사용 | G00·G01을 git으로 기계 증명하고 12개 Stage 사이 rollback·diff 확보 | **승인** |
| `D4-003` | 대상 DB 생명주기 권한 | `HealthCheckupReservationReceptionDb` 생성 + Drop/Recreate 승인, 가드 필수 | 생성만 / 임시 DB 선행 / 전면 보류 | G03·G14를 실제 검증할 수 있는 유일한 안 | **승인** |
| `D4-004` | SQL Server 2012 호환성 실증 방법 | **대상을 SQL Server 2025로 고정**. Compatibility Level 미설정. **허용목록 방식**으로 2012 이후 기능 미사용. 별도 lint script 미작성 | compat 110 강제 + 금지구문 정적검사 / compat 110만 / 구버전 인스턴스 확보 | 기준선은 "2012 **이상**"이 하한선이며 compat 110을 요구하지 않음. compat 110은 신규 구문을 막지 못해 보증 효력이 없음. 사용자 지시 | **승인** |
| `D4-004b` | 2016 DDL 편의구문 허용 여부 | `CREATE OR ALTER`·`DROP … IF EXISTS` **허용** (배포 배관 한정) | 2012 방식으로 통일 | 19개 객체 재생성 보일러플레이트 약 76줄 제거 + `DROP`+`CREATE`가 `GRANT`를 잃는 문제 회피. 2025 전용이 아님 | **승인** |
| `D4-005` | 배포 전략 | **A. Clean-create** — `01_Schema.sql`이 FK 역순 `DROP IF EXISTS` 후 `CREATE` | 멱등 Deploy + 별도 Rebuild / SSDT·Migration Tool | 보존할 운영 데이터가 없어 멱등화의 유일한 이점이 무의미. G14 자동 충족 | **승인** |
| `D4-006` | Git identity | OS 계정 기반 `JS <JS@DESKTOP-DP7KRE4>`, **repo-local만** 설정 | 사용자 직접 지정 | remote·push 없음. `--global`은 건드리지 않음 | **승인** |
| `D4-007` | 잠금 전략 | **코드. `sp_getapplock` 직렬화 + 행버전·기대상태 조건부 UPDATE** | A. `UPDLOCK/HOLDLOCK` 중심 / B. applock 단독 | `04` §3.1의 14·15번이 이미 조건부 UPDATE + 직렬화를 확정. applock은 실행계획 비의존이고 교착 회피가 자원명 정렬 규칙 하나로 환원됨 | **승인** |
| `D4-008` | applock timeout | **5000ms** | 10000ms / 3000ms | 정상 경합을 전부 흡수하면서 매달린 세션은 사용자 인내 한계 전에 실패 | **승인** |
| `D4-009` | 후속 Result Set 계약 검증 | **node 파서 도입** (`tools/verify-contract.js`, 표준 라이브러리만, npm 의존성 0) | SQL만 사용 / Phase 5로 이월 | `dotnet`·`python` 미설치로 인계문서 §7.3.K의 B·코드안 실행 불가. G09를 실제 PASS로 만드는 유일한 안 | **승인** |
| `D4-010` | Section 1 Environment·Deployment | 승인 | — | — | **승인** |
| `D4-011` | Section 2·3 Transaction·Locking | 승인 | — | — | **승인** |
| `D4-012` | Section 4·5 Seed·Contract 검증 | 승인 | — | — | **승인** |
| `D4-013` | Section 6·7 Security·Test | 승인 | — | — | **승인** |

미결정 항목: **0건**.

---

# 7. 최종 Database 파일구조

```text
database/
├─ AGENTS.md                          작업 경계·금지사항 요약 (Codex 가 읽는 본문)
├─ CLAUDE.md                          `@AGENTS.md` 한 줄 (Claude 가 읽는 입구)
├─ README.md                          실행 방법
├─ Deploy.sql                         진입점 1 — 대상 DB 내부 전체 재배포
├─ Rebuild.sql                        진입점 2 — DB 자체 Drop/Create 후 Deploy
│
├─ deploy/
│  ├─ 00_Preflight.sql                서버·DB·KST·버전·안전가드 검증
│  ├─ 01_Schema.sql                   FK 역순 DROP IF EXISTS → 6 Table + 제약 + Index + Sequence
│  ├─ 02_Seed.sql                     검사코드 19행 + 휴무일 40행
│  ├─ 03_Functions.sql                Inline TVF 4개 (CREATE OR ALTER)
│  ├─ 04_Procedures_Select.sql        SELECT SP 8개 (SP-LOG-01 포함)
│  ├─ 05_Procedures_Patient_Write.sql Patient Write SP 2개
│  ├─ 06_Procedures_Reservation_Write.sql  예약 Write SP 3개
│  ├─ 07_Procedures_Reception_Write.sql    접수 Write SP 3개
│  └─ 08_Verify.sql                   객체 인벤토리 + 제약 수 검증
│
├─ tests/
│  ├─ 00_Test_Harness.sql             Fixture 직접 INSERT (수검자·완료이력·기존 예약)
│  ├─ 00b_Test_Harness_RCP.sql         RCP 상태 Fixture 분리 배치 (00 의 전체 DELETE 이후에 실행)
│  ├─ 01_Schema_Tests.sql
│  ├─ 02_Seed_Tests.sql               19/2행 + 주민번호 체크디지트 무효 검수
│  ├─ 03_Rule_Tests.sql               TVF 4종 결정적 경계시험
│  ├─ 04_Select_SP_Tests.sql
│  ├─ 05_Patient_Write_Tests.sql
│  ├─ 06_Reservation_Write_Tests.sql
│  ├─ 07_Reception_Write_Tests.sql
│  ├─ 08_Rollback_Tests.sql
│  ├─ 09_Concurrency_Setup.sql
│  ├─ 10_Concurrency_Session_A.sql
│  ├─ 11_Concurrency_Session_B.sql
│  ├─ 12_Concurrency_Verify.sql
│  ├─ 13_Security_Tests.sql
│  ├─ 14_Clean_Rebuild_Verify.sql
│  └─ contract/                       SP별 호출 시나리오 — EXEC 한 번, DB 상태 단언 없음
│     └─ 01_*.sql ~ 25_*.sql · PWR-*·SEL-02* 등 Test ID 명 20개 SP 전건. RS0 결과코드·RS 형상 판정용 원본
│
├─ scripts/
│  ├─ deploy.sh  rebuild.sh  test.sh  concurrency-test.sh
│  ├─ verify-baseline.sh              00~05 SHA-256 재검증
│  ├─ verify-winforms-unchanged.sh    WinForms hash manifest 대조
│  ├─ verify-contract-all.sh          tests/contract/* 전건 실행 → verify-contract.js
│  ├─ clean-rebuild-verify.sh         RBD-002·003·005·007·008·009. 회차 사이 덤프 diff
│  ├─ verify-no-secret.sh             SEC-010. 배포 원본 + 로그 + 보고서 secret 스캔
│  ├─ verify-tsql-allowlist.sh        G13-b. .sql 만 · 주석 제외 → 허용목록(§9.2) 밖 0건
│  ├─ make-summary.sh                 T37 Step 2. 회귀 로그 → artifacts/reports/test-summary.txt
│  ├─ verify-red.sh                   RED-001~004 음성시험 (폐기용 DB · §40a)
│  ├─ verify-csharp-call.sh           R4-3. csc.exe 로 Probe.cs 를 컴파일해 실제 ADO.NET 호출
│  ├─ copy-completion.sql/.sh         RCP 업무 전체를 완료이력으로 복사 (배포물 아님)
│  └─ dev-completion-sp.sql/.sh       DEV_완료이력_등록 — 한 사람의 한 날짜를 콕 집어 (배포물 아님)
│
├─ tools/
│  ├─ verify-contract.js              node, 의존성 0
│  ├─ verify-docs.js                  스펙↔계획 정합성 게이트 (§45.3)
│  ├─ verify-rs-contract.js           R4-2. 기대값 Result Set ↔ 기준선 05 의 표 직접 대조
│  ├─ verify-schema-doc.js            G05. 실측 스키마 ↔ 04 §8 양방향
│  ├─ r4-rename.js / r4-rename-map.json   R4 개명의 단일 출처와 자체검증 (§46.3)
│  ├─ csharp-probe/Probe.cs           R4-3 호출 탐침. UTF-8 with BOM
│  ├─ expected-contracts.json         16 SP의 기대 Result Set 형상·RS0 결과코드
│  └─ allowed-codes.json              `05` §13 SP별 허용 ResultCode 집합
│
└─ artifacts/
   ├─ logs/                           sqlcmd -u 원본 출력. .gitignore
   └─ reports/                        커밋 대상 증거
```

`[I]` 원칙: 파일 하나당 명확한 책임 / 객체 의존순서 명시 / Deploy entry 1개 / Clean Rebuild entry 별도 / Master Seed와 Test Fixture 분리 / 테스트 전용 영구 Table 0개 / 로그와 Source 분리.

---

# 8. 배포·Clean Rebuild 전략

## 8.1 `Deploy.sql` (진입점 1) `[D4-005]`

대상 DB가 **이미 존재한다**고 가정하고 그 안의 모든 객체를 재구성한다.

```text
00_Preflight  → 01_Schema → 02_Seed → 03_Functions
→ 04_Procedures_Select → 05~07_Procedures_*_Write → 08_Verify
```

- `01_Schema.sql`은 **FK 역순 `DROP IF EXISTS` 후 `CREATE`** 한다. 즉 실행할 때마다 기존 테이블·데이터가 초기화된다.
- `[X]` **`변경이력`은 예외다.** 이 문서 초안이 clean-create 의 근거로 든 *"보존할 운영 데이터가 없다"* 가 그 테이블에는 성립하지 않는다. 감사 기록은 배포로 지워지지 않아야 하므로 `DROP` 대상에서 빼고 `IF OBJECT_ID(...) IS NULL` 가드로 만든다(`04` §8.6.3). 물리 테이블 6개 중 유일한 예외이고, 나머지 5개에는 `IF NOT EXISTS` 가드를 두지 않는다.
- 나머지 다섯 테이블은 보존할 운영 데이터가 없으므로 손실이 없고, 가드 보일러플레이트가 사라진다.
- Function·Procedure는 `CREATE OR ALTER`이므로 **DB를 초기화하지 않고 `03~07`만 단독 재실행**할 수 있다.

Drop 역순 (생성 순서 `04` §9.2의 역):

```text
완료이력 → 예약접수 → 휴무일 → 검사코드 → 수검자
→ SEQ_HC_CHART_NO
(변경이력은 Drop 하지 않는다)
```

## 8.2 `Rebuild.sql` (진입점 2)

`master` 컨텍스트에서 대상 DB 자체를 Drop/Create한 뒤 `Deploy.sql`을 실행한다.

```sql
-- 개념
IF DB_ID(N'HealthCheckupReservationReceptionDb') IS NOT NULL
BEGIN
    ALTER DATABASE [HealthCheckupReservationReceptionDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [HealthCheckupReservationReceptionDb];
END
CREATE DATABASE [HealthCheckupReservationReceptionDb] COLLATE Korean_Wansung_CI_AS;
```

`[I]` Collation을 **명시 고정**한다. `model` 상속에 맡기면 `model`이 바뀔 때 Rebuild 결과가 달라져 G14가 흔들린다. 한글은 전부 `NVARCHAR`이고 `VARCHAR` 컬럼은 전부 ASCII이므로 업무 영향이 없다.

`[D4-004]` **Compatibility Level은 설정하지 않는다.** `model` 기본값 170을 상속하며 Preflight가 실측값을 증거로 기록한다.

## 8.3 파괴작업 안전가드 `[D4-003]`

두 파일은 **실행 컨텍스트가 다르므로 가드도 다르다.** `00_Preflight.sql`은 대상 DB 안에서, `Rebuild.sql`은 `master`에서 실행된다. 같은 표를 양쪽에 적용하면 `DB_ID() > 4`가 `master`(=1)에서 항상 실패한다.

### `00_Preflight.sql` — 대상 DB 컨텍스트

| # | 가드 | 실패 시 |
|---:|---|---|
| 1 | `SERVERPROPERTY('InstanceName') = 'SQLEXPRESS'` exact match | `THROW 50010` |
| 2 | `DB_NAME() = 'HealthCheckupReservationReceptionDb'` exact match | `THROW 50011` |
| 3 | `SERVERPROPERTY('MachineName') = 'DESKTOP-DP7KRE4'` (승인된 호스트) | `THROW 50012` |
| 4 | `DATEPART(TZOFFSET, SYSDATETIMEOFFSET()) = 540` (KST) | `THROW 50013` |
| 5 | `SERVERPROPERTY('ProductMajorVersion') >= 11` | `THROW 50014` |
| 6 | 대상 DB의 `is_read_committed_snapshot_on = 0` | `THROW 50015` |

### `Rebuild.sql` — `master` 컨텍스트

| # | 가드 | 실패 시 |
|---:|---|---|
| 1 | `SERVERPROPERTY('InstanceName') = 'SQLEXPRESS'` exact match | `THROW 50020` |
| 2 | `DB_NAME() = 'master'` (여기서 실행해야 한다) | `THROW 50021` |
| 3 | 기존 대상 DB에 **`04` §8 계약 밖 사용자 Table 이 0개** (`DROP DATABASE` 직전 확인) | `THROW 50022` |
| 4 | `DATEPART(TZOFFSET, SYSDATETIMEOFFSET()) = 540` (KST) | `THROW 50023` |
| 5 | `SERVERPROPERTY('ProductMajorVersion') >= 11` | `THROW 50024` |
| 6 | `DROP`/`CREATE` 대상이 하드코딩 식별자 하나뿐임 (동적 SQL 미사용) | 구조적 |

`[X]` **가드 번호대를 분리한 이유**: 초안은 두 파일이 같은 `50010~50015`를 쓰고 `50011`을 `DECLARE @TargetDb = N'…'; IF @TargetDb <> N'…'`로 구현했다. 이는 **항상 거짓인 모순 명제**라 절대 발화하지 않는 죽은 코드였고, `50012`가 두 파일에서 서로 다른 의미를 가져 로그만으로 원인을 구분할 수 없었다. 번호대를 나누고 실제로 판정 가능한 조건으로 교체했다.

`[X 수정 2차]` **`DB_ID() > 4`·`database_id > 4` 도 같은 종류의 죽은 가드였다.** 사용자 DB는 `database_id`가 항상 5 이상이므로(실측: `Net461MvpSample` = 5) 고정된 비시스템 DB 이름에 대해 이 조건은 **항상 참**이고, 바로 앞 가드가 이름을 이미 확정했으므로 어떤 입력으로도 발화하지 않는다. 초안이 `50011`을 죽은 코드라고 지적하면서 같은 형태를 두 개 남긴 셈이다. 실제 위험에 맞춰 교체했다.

| 교체 전 | 실제 위험 | 교체 후 |
|---|---|---|
| `DB_ID() > 4` | `InstanceName`은 **다른 PC에서도 `SQLEXPRESS`** 다. 이름만으로는 호스트를 구별하지 못한다 | `MachineName` 대조 |
| `database_id > 4` | `DROP DATABASE` 직전에 **그 DB가 정말 Phase 4 DB인지** 아무도 확인하지 않는다. 동명 DB가 다른 내용을 담고 있으면 그대로 삭제된다 | 계약 밖 사용자 Table 0개 확인 |

`[I]` 호스트명 `DESKTOP-DP7KRE4`는 `SERVERPROPERTY('MachineName')` 실측값이다. 다른 PC로 옮기면 `50012`/`50020`이 발화하며, 이는 **의도된 동작**이다 — 승인되지 않은 장비에서 파괴작업을 실행하지 않는다.

`Net461MvpSample` 등 다른 DB는 이름이 다르므로 구조적으로 도달 불가하다. `DROP DATABASE` 문은 `Rebuild.sql` 단 한 곳에만 **대괄호 하드코딩 식별자**로 존재하며 `Deploy.sql`에는 없다. 변수 DB명으로 동적 SQL을 만들지 않는다.

`[한계]` 인스턴스가 1개뿐이므로 `50020`(잘못된 서버명) 경로는 **음성 시험이 불가능**하다. `NOT RUN`으로 기록한다.

## 8.4 실행 Script

| Script | 동작 | 실패 처리 |
|---|---|---|
| `scripts/deploy.sh` | `sqlcmd -b -u` 로 `Deploy.sql` 실행, 로그 기록 | exit code != 0 이면 즉시 중단 |
| `scripts/rebuild.sh` | 대상 DB명 재확인 후 `Rebuild.sql` 실행 | 동일 |
| `scripts/test.sh` | rebuild → deploy → `tests/01`~`14` 순차 → `verify-contract.js` | 첫 실패에서 중단, 로그 보존 |
| `scripts/concurrency-test.sh` | barrier 시각 계산 후 두 세션 동시 실행 → verify | 동일 |

shell script는 역할에 따라 두 가지를 쓴다.

| 역할 | 설정 | 이유 |
|---|---|---|
| **오케스트레이터** (`deploy.sh`·`rebuild.sh`·`test.sh`·`concurrency-test.sh`) | `set -uo pipefail` + 명시적 `RC=0; cmd \|\| RC=$?` + `exit $RC` | `set -e` 하에서는 `sqlcmd` 가 1을 반환한 그 줄에서 셸이 끝나 **`RC=$?` 도 진단 로그 출력도 실행되지 않는다**(§41.1-2 실측). 정확히 실패했을 때만 원인이 사라진다 |
| **순수 검증 스크립트** (`verify-baseline.sh`·`verify-winforms-unchanged.sh`) | `set -euo pipefail` | 첫 불일치에서 즉시 중단하는 것이 안전하고, 진단은 중단 지점 자체다 |

`[X 수정 2차]` 초안은 §8.4가 *"모든 shell script는 `set -euo pipefail`"* 이라고 하고 계획은 오케스트레이터에서 `-e` 를 뺐다. **두 문서가 정반대였다.** 구현자가 §8.4를 따르면 §41.1이 막으려던 진단 소실이 그대로 재발한다.

---

# 9. SQL Server 버전·호환성 규칙 `[D4-004]`

## 9.1 재정의 근거

- `04` §3.9: *"SEQUENCE, TRY_CONVERT, THROW, ROWVERSION 및 filtered index를 사용하므로 **SQL Server 2012 이상을 최소 기준으로 한다**"* — **하한선** 선언이다.
- `04` §3.9: *"실제 DB Script 작성 전에 서버 버전과 Database Compatibility Level을 **확인하되**, 이 확인은 6개 테이블 논리모델을 변경하는 사유가 아니다"* — 확인을 요구했을 뿐 110 강제를 요구하지 않는다.
- `05` header: *"Microsoft SQL Server 2012 이상"* — 동일.
- 따라서 SQL Server 2025에서 운영하는 것은 **기준선 위반이 아니다.**
- Compatibility Level 110 강제와 원안 G13은 **인계 프롬프트가 추가한 조건**이며 00~05에 근거가 없다.
- 또한 compat 110은 목적을 달성하지 못한다. `STRING_AGG`는 어떤 compat level에서도 동작하고 `CREATE OR ALTER`·`DROP … IF EXISTS`는 DDL이라 compat과 무관하다.

## 9.2 작성 규칙 — 허용목록 방식

**금지목록이 아니라 허용목록으로 관리한다.** 아래에 없는 기능은 쓰지 않는다.

| 허용 (SQL Server 2012 이하부터 존재) | 용도 |
|---|---|
| `SEQUENCE`, `NEXT VALUE FOR` | 차트번호 자동발급 |
| `TRY_CONVERT` | 날짜·숫자 형식 검증 |
| `THROW` | 예상치 못한 오류 전달 |
| `ROWVERSION` | Work Aggregate 동시성 |
| Filtered Index | `UX_검사코드_AEX_CODE` |
| `PERSISTED` 계산열 | `수검자.생년월일`·`수검자.성별` 을 `주민번호` 에서 유도. 세 값의 모순을 표현 불가능하게 만든다 (`04` §8.1.2). 인덱스 키로 쓸 수 있음을 실측 확인 |
| `sp_getapplock` / `sp_releaseapplock` | 논리 자원 직렬화 |
| `HASHBYTES('SHA2_256', …)` | 잠금 자원명 마스킹 (SHA2는 2012부터) |
| `EXCEPT` / `INTERSECT` | AEX·수검자 필드 NULL-safe 집합 비교, 인벤토리 양방향 대조 |
| `VALUES` 행 생성자 | 7개 BIT → 행집합 변환, 기대값 인라인 테이블 |
| Table Variable, CTE, `OUTER APPLY`, `CROSS APPLY` | Rule 구현. **상관 인자를 받는 TVF 는 반드시 `APPLY`** (`JOIN` 은 `Msg 4104`) |
| `TOP (n)` + `ORDER BY` | 스칼라 서브쿼리·최신 1건 조회. **`TOP` 없는 스칼라 서브쿼리 금지** (`Msg 512` 위험) |
| `WHILE` 루프 | 차트번호 발급, 시간대 자원 정렬 획득, 검사구성 문자열 조립 |
| 쉼표 구분 문자열 + 양끝 패딩 `LIKE` | 검사구성 저장·전개. `검사코드`를 `JOIN` 하면 단일 `SELECT` 로 편다. 파서가 필요 없어 인라인 TVF 안에서도 성립한다 (`plans/10` §2.1 실측) |
| `OFFSET … FETCH`, `IIF`, `CONCAT`, `FORMAT` | 필요 시 |
| `DBCC CHECKIDENT(…, RESEED, n)` | **재배포 시 IDENTITY 시드 이어받기.** `수검자`·`예약접수` 는 clean-create 라 재배포마다 키가 1부터 다시 나가는데 `변경이력` 만 보존된다 — 그러면 새로 등록한 사람에게 **이전 세대의 감사기록이 붙는다**(§43-16). 배포 마지막에 `변경이력` 의 최대 `대상키` 뒤로 시드를 밀어 재사용을 구조적으로 없앤다. SQL Server 2000 부터 있고 2012 이하 호환 조건을 만족한다 |

`[I]` **`CURSOR` 와 `FOR XML PATH` 는 허용목록에 없다.**
- 초안은 `UPDATE_예약변경` 의 시간대 2개 정렬 획득에 `CURSOR` 를, `G14` 지문 생성에 `FOR XML PATH` 를 썼다. 버전 호환성 문제는 없지만 **허용목록 방식("아래에 없는 기능은 쓰지 않는다")의 문자 그대로 위반**이다.
- `CURSOR` → `WHILE` + `MIN(Res) > @Prev` (자원이 최대 2개이므로 `IF @Res1 <= @Res2` 분기 2줄로도 충분). applock 실패로 `THROW` 할 때 `CLOSE`/`DEALLOCATE` 누락 문제도 사라진다.
- `FOR XML PATH` → 정렬된 행을 그대로 파일에 출력해 `diff`.
| `EXECUTE AS USER` / `REVERT`, `ALTER ROLE … ADD MEMBER` | 보안 |
| `sys.dm_exec_describe_first_result_set(_for_object)` | 계약 검증 |
| `WAITFOR TIME` | 동시성 barrier |
| **예외적 허용 (SQL 2016 DDL 배관)** `CREATE OR ALTER`, `DROP … IF EXISTS` | 배포 script 전용. 업무로직에는 사용하지 않음 `[D4-004b]` |

| 금지 (근거) |
|---|
| `STRING_SPLIT`, XML 파싱, JSON 함수/타입, `OPENJSON`, `FOR JSON` — `04` §3.8·§15.5 |
| 사용자 정의 Table Type(TVP) — `04` §3.8 |
| 비트마스크 — `04` §15.5 |
| 업무 Trigger, FK Cascade — `04` §3.1-17 |
| `SESSION_CONTEXT`, `AT TIME ZONE`, `STRING_AGG`, `TRIM()`, `CONCAT_WS`, `TRANSLATE`, `DATEDIFF_BIG`, `COMPRESS`, `GREATEST`/`LEAST`, `GENERATE_SERIES`, 정규식 함수, 벡터 타입 — 2012 이후 기능 |
| `sa` 사용, `sysadmin` 부여, `TRUSTWORTHY ON`, `xp_cmdshell`, Ad Hoc Distributed Queries, CLR, Linked Server |

허용목록은 사용자 규칙("SQL Server 2025 전용 문법·컬럼 미사용")보다 **더 엄격**하므로 자동으로 만족한다.

## 9.3 별도 lint script 미작성 · G13 증거의 한계 `[D4-004]` `[X 수정]`

규칙이 허용목록이고 대상 객체가 30개(Table 6 + TVF 4 + SP 20)뿐이므로 별도 정적검사 script를 만들지 않는다. 객체 수가 크게 늘거나 다수 인원이 SQL을 추가하게 되면 그때 도입한다.

`[X]` **다만 블랙리스트 `grep` 0건을 "허용목록 준수 PASS"로 승격하지 않는다.** 초안의 `T37` Step 4는 알려진 신기능 문자열 일부만 `grep` 하고 그 결과 0건을 G13 증거로 삼았다. 논리적으로 성립하지 않는다 — grep 목록에 없는 2012 이후 기능은 그대로 통과하고, 주석·문자열 안의 금지 단어는 오탐한다.

**G13 판정 방식** (§42 참조):

```text
(a) 대상 2025 인스턴스에서 전체 배포·테스트 성공          → 기계적 증거, PASS/FAIL
(b) 블랙리스트 grep 0건                                  → 보조 증거, PASS/FAIL
(c) §9.2 허용목록 준수                                   → 코드리뷰 확인. "정적 검토 완료"로만 기록
```

`(c)` 를 `PASS` 로 쓰지 않고 **`REVIEWED`** 로 기록한다. 자동 판정이 불가능한 것을 자동 판정한 것처럼 적지 않는다.

`[X 수정 2차]` **다만 `REVIEWED` 가 무증거 자기선언이 되면 `PASS` 로 적은 것과 다를 바 없다.** `T37` 의 `artifacts/reports/allowlist-review.md` 에 아래를 남기지 않으면 `(c)` 는 `REVIEWED` 가 아니라 **`NOT RUN`** 이다.

| 기록 항목 | 내용 |
|---|---|
| 대상 파일 | `deploy/*.sql` · `Deploy.sql` · `Rebuild.sql` · `tests/*.sql` 전체 경로와 각 줄 수 |
| 대조 목록 | §9.2 허용목록 각 항목에 대해 사용/미사용 표기 |
| 허용목록 밖 발견 | 있으면 파일:줄 + 왜 필요한지 + 대체 가능 여부. 없으면 "0건" |
| 검토자·시각 | 누가 언제 |

---

# 10. 공통 SQL Coding Rule `[I]`

```text
파일 인코딩        UTF-8 with BOM (실측 필수 — BOM 없으면 sqlcmd가 한글 객체명을 깨뜨림)
개행               CRLF / LF 무관 (sqlcmd 영향 없음)
Schema 한정        모든 객체를 [dbo].[이름] 으로 명시              05 §1.7
대괄호             모든 식별자를 대괄호로 감싼다 (한글 객체명 안전)
sp_ 접두사         사용자 정의 객체에 사용 금지                     05 §1.7
배치 구분          객체 하나당 GO 하나
NOCOUNT            모든 SP 첫 줄 SET NOCOUNT ON
XACT_ABORT         모든 Write SP 두 번째 줄 SET XACT_ABORT ON
Isolation          기본 READ COMMITTED. SET TRANSACTION ISOLATION LEVEL 문을 쓰지 않는다
현재시각           SP 시작 시 SYSDATETIME() 한 번만 캡처하고 UDF에 전달   05 §2.3
문자열 정규화      LTRIM/RTRIM → 빈 문자열은 NULL → 영문 코드 UPPER      05 §2.2
전화번호           '-' 제거값을 CelNumberS / TelNumberS 에 저장          04 §1.5
요일 계산          DATEDIFF(DAY, 0, @d) % 7   (0=월 … 5=토, 6=일)       04 §2.3 / 실측 검증
만 나이            DATEDIFF(YEAR,@b,@d) - CASE WHEN (MONTH(@d)*100+DAY(@d)) < (MONTH(@b)*100+DAY(@b)) THEN 1 ELSE 0 END
주석               각 SP 머리에 목적 / 근거 문서 절 / Result Set 순서 / 잠금 순서를 기재
동적 SQL           production SP에 사용하지 않는다. tests/13_Security_Tests.sql 에만 존재
```

---

# 11. 6개 Table 구현 계약 `[B]`

`04` §8의 정의를 그대로 구현한다. 컬럼명·타입·NULL을 **한 글자도 바꾸지 않는다.**

| No | Table | 역할 | 컬럼 수 | PK |
|---:|---|---|---:|---|
| 1 | `수검자` | 수검자 Master | **17** | `수검자ID` `BIGINT IDENTITY(1,1)` Clustered |
| 2 | `예약접수` | 예약·접수 업무 Master + 검사구성 | 10 | `업무ID` `BIGINT IDENTITY(1,1)` Clustered |
| 3 | `검사코드` | 통합 검사 Master | 6 | `검사항목코드` Clustered |
| 4 | `휴무일` | 휴무일 Master | 8 | `휴무일자` Clustered |
| 5 | `완료이력` | TGT 완료이력 + 검사구성 | 4 | `(수검자ID, 완료일자)` Clustered |
| 6 | `변경이력` | 데이터 변경기록 (컬럼 단위) | 8 | `이력ID` `BIGINT IDENTITY(1,1)` Clustered |

생성 순서 (`04` §9.2):

```text
1 수검자  2 검사코드  3 휴무일
4 예약접수  5 완료이력  6 변경이력
```

추가 테이블을 만들지 않는다. 특히 `SEC_PATIENT_IDENTIFIERS`, `MST_NATIONAL_EXAMS`, `MST_ADDITIONAL_EXAMS`, 별도 접수 Master/Detail, 상태 History, 범용 Rule Engine, 공통 코드 테이블, 테스트 전용 영구 Table을 만들지 않는다.

---

# 12. PK / FK / UQ / CK / DF / Index / Sequence 구현 계약 `[B]`

## 12.1 수량 (G05 판정 기준)

| 구분 | 수량 |
|---|---:|
| Primary Key | 6 |
| Foreign Key | 2 (전부 `NO ACTION`) |
| 일반 Unique Constraint | 2 |
| Filtered Unique Index | 1 |
| 업무/조회 Nonclustered Index | 4 |
| Sequence | 1 |
| Trigger | **0** |
| 사용자 정의 Table Type | **0** |

## 12.2 Foreign Key 2개

| # | 이름 | 자식 | 부모 |
|---:|---|---|---|
| 1 | `FK_예약접수_수검자` | `예약접수.수검자ID` | `수검자.수검자ID` |
| 2 | `FK_완료이력_수검자` | `완료이력.수검자ID` | `수검자.수검자ID` |

검사구성이 `예약접수`·`완료이력`의 컬럼이 되면서 `검사코드`를 가리키던 FK 2개가 사라졌다 (`04` §9.1).

## 12.3 Unique 3종

| 구분 | 이름 | 대상 |
|---|---|---|
| UQ | `UQ_수검자_CHART_NO` | `차트번호` |
| UQ | `UQ_수검자_SOCIAL_NUMBER` | `주민번호` |
| UX | `UX_검사코드_AEX_CODE` | `추가검사코드` `WHERE 추가검사코드 IS NOT NULL` |

## 12.4 Nonclustered Index 4개

| 이름 | Key | INCLUDE / Filter |
|---|---|---|
| `IX_수검자_NAME_BIRTHDAY` | `성명, 생년월일` | `수검자ID, 차트번호, 성별, 휴대전화` |
| `IX_수검자_BIRTHDAY` | `생년월일` | `수검자ID, 차트번호, 성명, 성별, 휴대전화` |
| `IX_예약접수_SLOT` | `예약일, 시간대코드, 상태코드` | `수검자ID` |
| `IX_예약접수_PATIENT_STATE_DATE` | `수검자ID, 상태코드, 예약일` | `시간대코드` |

`IX_수검자_CEL_NUMBER_S`는 `CelNumberS` 컬럼과 함께 사라졌다 (`04` §8.1.5).

성능 목적의 보조 Index는 추가하지 않는다 (`04` §0.4.2 / §11.2).

## 12.5 CHECK 제약

`04` §8.1.3 / §8.2.3 / §8.3.3 / §8.4.2 / §8.5.3 / §8.6.3의 제약표를 **그대로** 구현한다.

이름과 정의를 여기에 옮겨 적지 않는다. 사본이 곧 드리프트의 발생원이고, 실제로 R2 이름 사본이 R3 재봉인을 통과해 남아 있었다. 수치 판정은 `G05`가 `04` 실측과 `EXCEPT` 양방향으로 대조하며, `V08`이 문서의 선언 수치를 `04` §8의 실측과 다시 대조한다.

## 12.6 Sequence

```sql
CREATE SEQUENCE [dbo].[SEQ_HC_CHART_NO]
    AS BIGINT START WITH 1 INCREMENT BY 1
    MINVALUE 1 MAXVALUE 999999 NO CYCLE CACHE 50;
```

변환: `N'코드' + RIGHT(N'000000' + CONVERT(NVARCHAR(6), @SequenceValue), 6)` → `C000123`. 결번을 허용한다.

`[I]` **자동발급 재시도**: `04` §3.6의 *"수동입력값과 충돌하면 다음 Sequence 값을 취득한다"*를 루프로 구현한다. **`206 ChartNoLimit`은 Sequence가 `MAXVALUE`(999999)에 도달했을 때만 반환한다.**

```text
루프 종료 조건
  ① 사용 가능한 후보를 찾음                       → 진행
  ② NEXT VALUE FOR 가 Msg 11728(MAXVALUE 도달)   → 206 ChartNoLimit
```

`[X]` **초안 오류**: 임의 상한 100회를 두고 초과 시 `206`을 반환하도록 했다. 그러면 `C000001`~`C000100`이 수동 입력되어 있고 `C000101`이 비어 있는 상황에서 **발급 가능한 번호가 있는데도 실패**한다. `05` §4.2가 정의한 `206 ChartNoLimit`의 의미는 "자동 차트번호 발급범위를 초과했습니다"이지 "구현 편의용 반복 횟수 초과"가 아니다.

`[I]` **`NEXT VALUE FOR`는 Transaction 밖(§21.1의 `[2]` 구간)에서 확보한다.** `SET XACT_ABORT ON` 트랜잭션 안에서 `Msg 11728`이 나면 `TRY/CATCH`로 잡아도 트랜잭션이 **doomed**(`XACT_STATE() = -1`)가 되어 이후 `COMMIT`이 `Msg 3930`으로 실패한다. Sequence 값은 롤백과 무관하게 소비되므로(`04` §3.6이 결번을 허용) 밖으로 빼도 계약에 어긋나지 않는다. 단 최종 고유성 확인과 `INSERT`는 `CHART` applock 안에서 수행한다.

`[I]` **자동발급 경로도 `HC|CHART|{후보}` 를 획득한다.** 잡지 않으면 다른 세션이 같은 값을 수동 입력해 `2627`이 발생할 수 있고, §20이 *"applock으로 사전 직렬화했으므로 2627 발생 시 설계 위반"*이라고 못박았으므로 그 구멍을 남길 수 없다. 획득 실패(다른 세션 점유) 시 다음 Sequence 값으로 진행한다.

---

# 13. `검사코드` Seed 19행 `[B]`

`04` §4.6을 그대로 구현한다. `추가검사사용여부`는 AEX 역할이 있으면 `1`, 없으면 `0`(`00` §7.3.1 "사용여부 기본값 Y").

| 검사항목코드 | 검사항목명 | 국가검사규칙코드 | 추가검사코드 | 추가검사성별코드 | 추가검사사용여부 |
|---|---|---|---|:---:|:---:|
| `EX001` | 문진/진찰 | `NEX-01` | NULL | NULL | 0 |
| `EX002` | 신체계측 | `NEX-01` | NULL | NULL | 0 |
| `EX003` | 혈압 | `NEX-01` | NULL | NULL | 0 |
| `EX004` | 시력·청력 | `NEX-01` | NULL | NULL | 0 |
| `EX005` | 흉부 X-ray | `NEX-01` | NULL | NULL | 0 |
| `EX006` | 요검사 | `NEX-01` | NULL | NULL | 0 |
| `EX007` | 혈액검사 | `NEX-01` | NULL | NULL | 0 |
| `EX008` | 구강검진 | `NEX-01` | NULL | NULL | 0 |
| `EX009` | 이상지질혈증 | `NEX-02` | NULL | NULL | 0 |
| `EX010` | B형간염 | `NEX-03` | NULL | NULL | 0 |
| `EX011` | 코드형간염 | `NEX-04` | NULL | NULL | 0 |
| **`EX012`** | 골밀도검사 | **`NEX-05`** | **`OPT04`** | `A` | 1 |
| `EX013` | 폐기능 | `NEX-06` | NULL | NULL | 0 |
| `EX014` | 복부초음파 | NULL | `OPT01` | `A` | 1 |
| `EX015` | 갑상선초음파 | NULL | `OPT02` | `A` | 1 |
| `EX016` | 유방초음파 | NULL | `OPT03` | `F` | 1 |
| `EX017` | PSA | NULL | `OPT05` | `M` | 1 |
| `EX018` | HbA1c | NULL | `OPT06` | `A` | 1 |
| `EX019` | HPV 검사 | NULL | `OPT07` | `F` | 1 |

검산: NEX 역할 13행(`EX001`~`EX013`) + AEX 역할 7행(`EX012`, `EX014`~`EX019`) − 공통 `EX012` 1행 = **19행**.

`[I]` **Seed 검수는 위 19행 전체를 기대 `VALUES` 로 두고 `EXCEPT` 양방향 대조한다.** 개수·일부 표본 확인으로는 어떤 행의 `국가검사규칙코드`·`추가검사코드`·`추가검사성별코드`·`추가검사사용여부` 가 틀려도 통과한다 — AEX/NEX 판정이 조용히 왜곡된다. `04` §14가 *"검사 Master가 정확히 19/13/7종 → Seed 검수 Script"* 로 위임한 항목이다.

`[I]` **`deploy/02_Seed.sql` 에 선행 `DELETE` 를 두지 않는다.** clean-create(`01_Schema.sql` 이 `DROP`→`CREATE`)이므로 테이블이 항상 비어 있어 `DELETE` 는 효과가 없고, Fixture 배치 후 이 파일만 단독 재실행하면 `검사항목` 의 FK 때문에 `Msg 547` 로 실패한다. **멱등성은 "clean-create 직후 한정"으로 명시**하고 단독 재실행을 요구하지 않는다.

`EX012` 한 행이 NEX 골밀도와 AEX `OPT04` 역할을 동시에 가진다. 검사구성이 코드 문자열이므로 같은 `EX012` 가 `국가검사항목`과 `추가검사항목`에 동시에 나타나는 저장을 구조적으로 차단한다.

Deploy가 clean-create이므로 Seed는 **단순 `INSERT`**만 사용한다. `MERGE`·존재검사가 필요 없다.

---

# 14. `휴무일` Seed 40행 `[I]`

`00` HOL-06 이 정한 등재 범위 **2026-01-01 ~ 2027-12-31** 을 편다. 법정공휴일·대체공휴일 39행과
자체휴무일 1행, 합계 **40행**이다.

## 14.1 근거 — 기억으로 적지 않는다

`[!]` **대체공휴일 규칙은 두 번 바뀌었고 2026 에 또 바뀌었다.** 이 표는 현행 법령과 월력요항을 조회해 만든 것이며
기억으로 적은 것이 아니다. 다음 갱신에서도 같은 절차를 밟는다.

| 확인 | 결과 |
|---|---|
| 대체공휴일 근거 | 「관공서의 공휴일에 관한 규정」 제3조 |
| 설날·추석 | **일요일과 겹칠 때만** 대체. 토요일과 겹쳐도 대체가 없다 |
| 현충일 | 토·일과 겹쳐도 대체가 **없다**. 2026-06-06(토)·2027-06-06(일) 둘 다 대체 0건으로 교차확인 |
| 제헌절 | 2008 년 공휴일에서 빠졌다가 **2026 년 복귀**. 「공휴일에 관한 법률」 개정, 시행 2026-05-11 |
| 노동절 | `근로자의 날` → `노동절` 개칭과 함께 관공서 공휴일 승격. 시행 2026-05-01 |
| 2026 월력요항 | 위 두 법 개정보다 **먼저** 나왔다. 그래서 2026 에는 제헌절(7/17 금)·노동절(5/1 금)을 더한다. 둘 다 금요일이라 대체공휴일은 늘지 않는다 |
| 2027 교차검증 | 일요일 52 + 비일요일 24 = 76, 설날·현충일·광복절·개천절이 일요일과 겹쳐 −4 = **72**. 월력요항의 산식과 일치한다 |

## 14.2 등재하지 않는 것

- **일요일은 Seed 하지 않는다.** HOL-01 의 요일 Rule(`%7 = 6`)이 이미 차단하므로 넣어도 판정이 달라지지 않는다.
  2026 은 `03-01`(삼일절)·`05-24`(부처님오신날) 둘, 2027 은 `02-07`(설날)·`06-06`(현충일)·`08-15`(광복절)·`10-03`(개천절) 넷이 여기서 빠진다.
- 임시공휴일·선거일은 넣지 않는다. 지정 시점이 유동적이라 다음 Seed 갱신에서 반영한다(`03` §24.7).

## 14.3 2026 — 법정·대체 19행

| 휴무일자 | 요일 | 휴무일명 | 휴무구분 |
|---|:---:|---|---|
| `2026-01-01` | 목 | 신정 | 법정공휴일 |
| `2026-02-16` | 월 | 설날 연휴 | 법정공휴일 |
| `2026-02-17` | 화 | 설날 | 법정공휴일 |
| `2026-02-18` | 수 | 설날 연휴 | 법정공휴일 |
| `2026-03-02` | 월 | 삼일절 대체공휴일 | 대체공휴일 |
| `2026-05-01` | 금 | 노동절 | 법정공휴일 |
| `2026-05-05` | 화 | 어린이날 | 법정공휴일 |
| `2026-05-25` | 월 | 부처님오신날 대체공휴일 | 대체공휴일 |
| `2026-06-06` | 토 | 현충일 | 법정공휴일 |
| `2026-07-17` | 금 | 제헌절 | 법정공휴일 |
| `2026-08-15` | 토 | 광복절 | 법정공휴일 |
| `2026-08-17` | 월 | 광복절 대체공휴일 | 대체공휴일 |
| `2026-09-24` | 목 | 추석 연휴 | 법정공휴일 |
| `2026-09-25` | 금 | 추석 | 법정공휴일 |
| `2026-09-26` | 토 | 추석 연휴 | 법정공휴일 |
| `2026-10-03` | 토 | 개천절 | 법정공휴일 |
| `2026-10-05` | 월 | 개천절 대체공휴일 | 대체공휴일 |
| `2026-10-09` | 금 | 한글날 | 법정공휴일 |
| `2026-12-25` | 금 | 성탄절 | 법정공휴일 |

## 14.4 2027 — 법정·대체 20행

| 휴무일자 | 요일 | 휴무일명 | 휴무구분 |
|---|:---:|---|---|
| `2027-01-01` | 금 | 신정 | 법정공휴일 |
| `2027-02-06` | 토 | 설날 연휴 | 법정공휴일 |
| `2027-02-08` | 월 | 설날 연휴 | 법정공휴일 |
| `2027-02-09` | 화 | 설날 대체공휴일 | 대체공휴일 |
| `2027-03-01` | 월 | 삼일절 | 법정공휴일 |
| `2027-05-01` | 토 | 노동절 | 법정공휴일 |
| `2027-05-03` | 월 | 노동절 대체공휴일 | 대체공휴일 |
| `2027-05-05` | 수 | 어린이날 | 법정공휴일 |
| `2027-05-13` | 목 | 부처님오신날 | 법정공휴일 |
| `2027-07-17` | 토 | 제헌절 | 법정공휴일 |
| `2027-07-19` | 월 | 제헌절 대체공휴일 | 대체공휴일 |
| `2027-08-16` | 월 | 광복절 대체공휴일 | 대체공휴일 |
| `2027-09-14` | 화 | 추석 연휴 | 법정공휴일 |
| `2027-09-15` | 수 | 추석 | 법정공휴일 |
| `2027-09-16` | 목 | 추석 연휴 | 법정공휴일 |
| `2027-10-04` | 월 | 개천절 대체공휴일 | 대체공휴일 |
| `2027-10-09` | 토 | 한글날 | 법정공휴일 |
| `2027-10-11` | 월 | 한글날 대체공휴일 | 대체공휴일 |
| `2027-12-25` | 토 | 성탄절 | 법정공휴일 |
| `2027-12-27` | 월 | 성탄절 대체공휴일 | 대체공휴일 |

## 14.5 자체휴무일 1행

| 휴무일자 | 요일 | 휴무일명 | 휴무구분 | 비고 |
|---|:---:|---|---|---|
| `2026-12-26` | 토 | 센터 휴진일 | 자체휴무일 | 토요일 휴무일 시험 대상 |

`00` HOL-05 가 요구하던 *"평일 1건 · 토요일 1건"* 은 이제 `2026-12-25`(금, 법정공휴일)와
`2026-12-26`(토, 자체휴무일)이 함께 만족한다. `RUL-D04`·`RUL-D05`·`SED-009`·`SED-010` 이 이 두 날짜를
그대로 붙들고 있으므로 시험을 옮기지 않아도 된다.

## 14.6 고정 날짜를 쓰는 이유

**배포 시각 기준 상대날짜로 계산하지 않는다.** 실행할 때마다 값이 달라지면 `G14`(Rebuild 2회 후 정렬 덤프 diff 0줄)가
성립하지 않는다. 요일은 `DATEDIFF(DAY, 0, 휴무일자) % 7` 로 시험이 실측 판정한다 — 위 표의 요일 칸을 사람이 믿지 않는다.

## 14.7 만료

`00` §7.4 가 등재 범위와 경고 임계(잔여 180일)를 정한다. 이 Seed 의 공휴일 최종일은 `2027-12-27` 이다.
잔여가 임계 미만이 되면 `scripts/verify-holiday-seed.sh` 가 **FAIL** 한다.

`[!]` **경고가 아니라 실패다.** `00` HOL-06 은 "경고한다" 고 적지만, 이 저장소에서 실패시키지 않는 검사는
초록인 채 아무것도 검증하지 않다가 잊힌다(§44.8 이 그 사건들이다). 잔여 180일은 사람이 Seed 를 갱신하기에
넉넉한 창이고, 그 창을 넘겼다면 회귀가 멈추는 편이 낫다.

---

# 15. Demo / Test Fixture 전략 `[D4-012]`

## 15.1 Seed 계층 — 4단계에서 2단계로 축소

| 인계 프롬프트 후보 | 결정 | 위치 |
|---|---|---|
| Deploy Master Seed | **유지** | `deploy/02_Seed.sql` |
| Demo Seed | **만들지 않음** — Test Fixture가 남긴 데이터가 데모를 겸함 | — |
| Automated Test Fixture | **유지** | `tests/00_Test_Harness.sql` |
| Corruption / Adversarial Fixture | **Test Fixture에 통합**, 파일 내 구획으로 분리 | `tests/00_Test_Harness.sql` §CORRUPT |

## 15.2 Fixture 생성 방식 — 핵심 분기

| 대상 | 방식 | 이유 |
|---|---|---|
| 수검자 · 완료이력 · **기존 예약 19건** | **dbo 직접 `INSERT`** | Write SP는 `308/309`(업무일·운영시간)를 검증하므로 **업무시간 밖에는 fixture 생성 자체가 실패**한다. 직접 INSERT는 시간 무관·결정적·빠르다 |
| Rule TVF 테스트 | **TVF 직접 호출** + `@서버시각` 주입 | 완전 결정적. `05` §17.1 시간경계 12건을 실제 시각과 무관하게 전부 검증 |
| Write SP 동작 테스트 | **SP 호출** | 실제 계약 검증. 업무시간 밖에서는 `308/309`가 정상 결과이므로 테스트가 이를 인지 |

정원 19/20 경합도 **19건을 직접 INSERT**한 뒤 20번째만 SP로 시도한다. 이는 production SP에 **테스트용 시간 주입 backdoor를 넣지 않는다**는 원칙(`인계문서 §7.3.J`)을 지키면서 결정적 시험을 얻는 유일한 방법이다.

## 15.3 손상 데이터 Fixture (701 검증용)

정상 SP로는 만들 수 없으므로 dbo 직접 INSERT로 생성한다.

```text
CORRUPT-1  동일 Patient에 유효업무 2건            → 701 WorkDataError
CORRUPT-2  Work의 국가검사항목이 빈 문자열 (저장 NEX 0개)  → 701
CORRUPT-3  추가검사사용여부=0인 AEX가 저장된 Work        → "재검증하지 않는다" 계약 검증
CORRUPT-4  AEX 전용 Master 코드가 국가검사항목에 저장됨    → 701 (역할 불일치)
CORRUPT-5  NEX 12행이 저장된 Work                 → 701 (상한 11 초과)
```

### CORRUPT-3 구현 — Master Seed 를 오염시키지 않는다 `[X 수정]`

Seed 19행은 AEX 7종을 전부 `추가검사사용여부=1` 로 만들므로(`00` §7.3.1) 어떤 입력으로도 `410 ExamOff` 가 발생하지 않는다. 그렇다고 `deploy/02_Seed.sql` 을 고치면 `04` §8.4.2 계약 위반이다.

```sql
-- tests/03_Rule_Tests.sql 안에서만, 트랜잭션으로 감싸고 즉시 되돌린다
BEGIN TRANSACTION;
    UPDATE [dbo].[검사코드] SET [추가검사사용여부] = 0 WHERE [추가검사코드] = 'OPT06';
    -- RUL-A06 : 비활성 AEX 요청 → 410 ExamOff 확인
    -- CORRUPT-3 : 이 AEX 가 이미 저장된 Work 에 대해
    --             시간대만 변경 / RCP AEX 동일집합 호출이 성공하고 [행버전]·Detail 이 불변인지 확인
ROLLBACK TRANSACTION;   -- Master Seed 원상복구
```

`[I]` **CORRUPT-3 이 검증하는 것은 `410` 자체가 아니라 "재검증하지 않는다"는 계약이다.** 정상 Master로 하는 `RWR-021`·`CWR-020`은 Detail 불변만 확인할 뿐, 구현이 No-op 이나 시간대 변경에서 Master를 잘못 재검증해도 정상 Master라 성공해 버린다. **저장 당시엔 유효했지만 지금은 무효인 AEX** 가 있어야 `00` §4.1 *"시간대만 변경 시 TGT/NEX/AEX를 재검증·재작성하지 않는다"* 를 실제로 시험할 수 있다.

`[X]` 초안은 CORRUPT-3 을 선언만 하고 배치하지 않았으며 `T10` 완료조건에 "손상 데이터 3종"을 적어 **달성 불가능한 조건**을 만들었다.

## 15.4 Cleanup — 제거

Deploy가 clean-create이므로 `scripts/test.sh`가 **rebuild → deploy → 전체 테스트** 순으로 돈다. 결과적으로 다음이 전부 불필요해진다.

```text
테스트별 cleanup 로직
IDENTITY 값 하드코딩 회피 규칙
안정키 재조회 헬퍼
```

테스트 수검자는 **수동 차트번호 `T001`~`T0nn` 고정**(안정키)으로 만들고, 자동발급 경로 검증용 1~2건만 `@차트번호자동발급여부=1`로 생성한다. 테스트가 `수검자ID`·`업무ID`를 하드코딩하지 않고 `차트번호` / `(수검자ID, 예약일, TimeSlotCode)`로 조회한다.

## 15.5 Rule Test 기준 예약일 `[I]` = `2026-10-01`

`UFN_HC_검진대상확인`·`UFN_HC_국가검사구성`은 `@예약일`만 받고 과거 여부를 판정하지 않으므로, 이 날짜가 나중에 과거가 되어도 **TGT/NEX 테스트는 영구히 결정적**이다. 실측 검증한 만나이 공식으로 생년월일을 역산한다.

주요 경계 프로필 (기준 예약일 `2026-10-01`):

| 목적 | 생년월일 | 성별 | 만 나이 | 기대 |
|---|---|:---:|---:|---|
| TGT 경계 | `2006-10-02` | — | 19 | `400 UnderAge` |
| TGT 경계 | `2006-10-01` | — | 20 | 대상 |
| NEX-02 남 | `2002-10-01` | M | 24 | `(24-24)%4=0` → 포함 |
| NEX-02 남 | `2003-10-01` | M | 23 | 제외 |
| NEX-02 여 | `1986-10-01` | F | 40 | `(40-40)%4=0` → 포함 |
| NEX-03 | `1986-10-01` | — | 40 | `B형간염제외여부=0` 이면 포함 / `1` 이면 제외 |
| NEX-04 | `1970-10-01` | — | 56 | 포함 |
| NEX-05 | `1972-10-01` | F | 54 | 포함 |
| NEX-06 | `1960-10-01` | — | 66 | 포함 |
| **조건부 3종 동시 → NEX 11행** | `1970-10-01` | F | 56 | NEX-02(`(56-40)%4=0`) + NEX-04(56) + NEX-06(56) |

TGT 완료이력 경계: 완료일 `2024-05-01`(2년 → 대상), `2025-05-01`(1년 → `401 NotDue`), `2026-10-01`(예약일 당일 → 제외), `2026-11-01`(예약일 이후 → 제외).

---

# 16. 실제 주민등록번호 미사용 검증 `[I]`

## 16.1 테스트값 생성규칙

```text
1~6자리    테스트가 요구하는 생년월일 (기준 예약일 2026-10-01 대비 역산)
7자리      세기·성별 코드   1900년대 남 1 / 여 2,  2000년대 남 3 / 여 4
8~12자리   '00001'부터 순번 — 지역 의미 없음
13자리     정상 체크디지트 계산값 + 1 (mod 10)   ← 의도적 무효화
```

- 실제 주민등록번호를 **복사하지 않는다.**
- 생성 상수는 미리 계산해 `tests/00_Test_Harness.sql`에 하드코딩한다. 결정적이며 재현 가능하다.
- 실제 행정번호 존재 여부와 검증번호 계산은 SP가 검증하지 않는다 (`00` §2.1, `04` §3.5).

## 16.2 검수 (`tests/02_Seed_Tests.sql`)

`수검자` 전 행에 대해 다음을 확인한다.

| # | 검증 | 실패 시 |
|---:|---|---|
| 1 | 13자리 숫자 형식 | FAIL |
| 2 | 7번째 자리로 세기를 결정해 조립한 `yyyyMMdd`가 `TRY_CONVERT(DATE, …, 112)` 가능 | FAIL |
| 3 | 7번째 자리가 `1,2,3,4` 중 하나 | FAIL |
| 4 | **체크디지트가 유효하지 않음** — 가중치 `2,3,4,5,6,7,8,9,2,3,4,5` 인라인 계산 후 `(11 - sum%11)%10` 과 13번째 자리 비교 | **FAIL** |

`4`가 "실제 주민등록번호 미사용"의 기계적 증거다. 이 계산은 **테스트 스크립트 안의 인라인 식**으로만 존재하며, 객체 계약을 지키기 위해 영구 DB 함수를 만들지 않는다.

---

# 17. 4개 Inline TVF 구현 전략 `[B]`

공통 제약 (`05` §1.4): Inline Table-Valued Function / 단일 `SELECT` 반환 / 데이터 변경 없음 / Transaction 없음 / `THROW` 없음 / C# 직접 호출 금지 / SP 내부 전용.

| TVF | 반환 Cardinality | 구현 골자 |
|---|---|---|
| `[dbo].[UFN_HC_일정확인]` | 정확히 1행 (11컬럼) | `휴무일` `LEFT JOIN` + `DATEDIFF(DAY,0,@예약일)%7` 요일 판정 + `CASE` 중첩으로 `사유코드` 우선순위 `300→301→302→303→304` 구현. 마감표는 `VALUES` 행 생성자로 인라인 |
| `[dbo].[UFN_HC_검진대상확인]` | Patient 존재 시 1행, 없으면 0행 | `수검자` + `OUTER APPLY (SELECT TOP 1 CompletionDate FROM 완료이력 WHERE 수검자ID=@수검자ID AND CompletionDate < @예약일 ORDER BY CompletionDate DESC)` |
| `[dbo].[UFN_HC_국가검사구성]` | TGT 비대상 0행 / 대상 **8~11행** | `검사코드 WHERE 국가검사규칙코드 IS NOT NULL` + `CROSS APPLY UFN_HC_검진대상확인` + 조건부 5종 `WHERE` 술어. 정렬 `검사항목코드 ASC` |
| `[dbo].[UFN_HC_추가검사확인]` | Master 정상 시 정확히 **7행** | `검사코드 WHERE 추가검사코드 IS NOT NULL` + 요청 7 BIT를 `VALUES` 행집합으로 변환해 `JOIN`. `@저장검사사용여부`에 따라 NEX 출처를 `UFN_HC_국가검사구성` 또는 `검사항목(NEX)`로 분기. 정렬 `추가검사코드 ASC` |

## 17.1 마감시각 인라인 테이블 (`04` §3장 / `05` §2.4)

| 시간대 | `NORMAL` 마감 | `RECEPTION` 마감 |
|---|---:|---:|
| `AM` | 10:00 | 11:00 |
| `PM` | 15:00 | 16:00 |

- `@마감구분='NONE'` 이면 `마감시각 = NULL`, `마감경과여부 = 0`.
- `마감시각`은 **요청일이 오늘일 때만** 반환한다 (`05` §6.1.3).
- 비교는 `@현재시각 < @마감시각` — **마감시각과 같은 시각부터 불가** (`00` §3장).
- 토요일 `PM`은 `운영여부 = 0` → `303 SlotClosed` (`00` §3장 "토요일 오후 예약 불가").

## 17.2 조건부 NEX 5종 술어 (`00` §7.2.2)

```text
NEX-02  EX009  남: 나이>=24 AND (나이-24)%4=0    여: 나이>=40 AND (나이-40)%4=0
NEX-03  EX010  나이=40 AND 수검자.B형간염제외여부 = 0
NEX-04  EX011  나이=56
NEX-05  EX012  성별='F' AND 나이 IN (54,60,66)
NEX-06  EX013  나이 IN (56,66)
```

동시 성립 최대 3종 → 기본 8종 + 0~3종 = **8~11행**.

## 17.2a `412 ExamDuplicate` 의 도달 가능 조건 `[X 수정 2차]`

`[X]` **§13 Seed 19행 중 `국가검사규칙코드` 와 `추가검사코드` 를 동시에 가진 행은 `EX012`(골밀도검사) 하나뿐이다.** `OPT01`·`02`·`03`·`05`·`06`·`07` 이 가리키는 `EX014`~`EX019` 는 `NexRuleCode` 가 `NULL` 이라 국가검사에 **절대** 나타나지 않는다.

따라서:

> **`412` 는 `OPT04` 로만 발생하고, `EX012` 를 가진 수검자에게만 발생한다.**
> `NEX-05` 술어가 `성별='F' AND 나이 IN (54,60,66)` 이므로 **여성 만 54·60·66세** 프로필이 아니면 `412` 를 관측할 수 없다.

`412` 를 기대하는 모든 테스트(`RUL-A07`·`RUL-A09`·`RWR-031`·`CWR-024`)는 이 조건을 만족하는 Fixture 를 써야 한다. 남성이나 다른 나이 프로필로 시험하면 **`412` 대신 `0` 이 나오고 테스트가 조용히 통과한다** — 중복 판정이 실제로 동작하는지 아무것도 증명하지 못한다.

| Fixture | 프로필 | `EX012` | 용도 |
|---|---|:---:|---|
| `T011` | F 만 54세 | **있음** | `RUL-A07`(산출 NEX) · `RUL-A09`(저장 NEX) · `CWR-024` |
| `T020` | F 1972-11-20생 | **예약일에 따라 바뀐다** | `RWR-031` — `2026-11-17` 만 53세(없음) → `2026-11-20` 만 54세(생김) |
| `T014` | **M** 만 56세 | **없음** | `412` 시험에 쓸 수 없다 |

`[I]` `T020` 이 `RWR-031` 의 핵심이다. `05` §17 의 *"예약일 변경 후 OPT04 vs EX012 충돌"* 은 **예약일을 바꾸면 만나이가 바뀌어 NEX 구성이 달라진다**는 뜻이고, 변경 전 구성으로 AEX 중복을 판정하면 이 시나리오가 통과해 버린다. 실측: `1972-11-20`생은 `2026-11-17` 에 만 53세, `2026-11-20` 에 만 54세다.

## 17.3 AEX 판정순서 (`05` §6.4.3)

```text
새 예약일 기준(@저장검사사용여부=0)  TGT 비대상 → 추가검사사용여부=0 → 성별 불충족 → NEX 동일 검사항목코드
저장 NEX 기준(@저장검사사용여부=1)   추가검사사용여부=0 → 성별 불충족 → 저장 NEX 동일 검사항목코드
```

선택하지 않은 무효 항목은 사유만 표시하고 저장을 차단하지 않는다. `요청선택여부=1`인 무효 항목만 Write를 차단한다.

---

# 18. 20개 SP 구현 Matrix `[B]`

| SP | 구분 | Param | Result Set | 관련 정책·Rule | Transaction | Lock | Test file |
|---|:---:|---:|:---:|---|:---:|---|---|
| `USP_HC_공통업무상태_조회` | SELECT | 0 | 2 (RS0,RS1) | CP-01~04, HOL | X | X | `04_Select_SP_Tests.sql` |
| `USP_HC_수검자목록_조회` | SELECT | 5 | 2 | EP-01, 검색계약 | X | X | `04_Select_SP_Tests.sql` |
| `USP_HC_수검자상세_조회` | SELECT | 1 | 2 | EP-01~02 | X | X | `04_Select_SP_Tests.sql` |
| `USP_HC_수검자_등록` | INSERT | 14 | 2 | EP-03~09 | **O** | `SSN` → `CHART` | `05_Patient_Write_Tests.sql` |
| `USP_HC_수검자정보_수정` | UPDATE | 14 | 2 | EP-04, EP-08, CP-06 | **O** | `SSN` → `CHART` → `PAT` | `05_Patient_Write_Tests.sql` |
| `USP_HC_수검자유효업무_조회` | SELECT | 1 | 2 | RP-06 | X | X | `04_Select_SP_Tests.sql` |
| `USP_HC_예약가능정보_조회` | SELECT | 13 | **6** (RS0~RS5) | RP-02~08, TGT/NEX/AEX/HOL | X | X | `04_Select_SP_Tests.sql` |
| `USP_HC_예약_등록` | INSERT | 12 | 2 | RP-01~08 | **O** | `PAT` → `SLOT` | `06_Reservation_Write_Tests.sql` |
| `USP_HC_예약_변경` | UPDATE | 12 | 2 | RP-03·06·09 | **O** | `PAT` → `WORK` → `SLOT`×n | `06_Reservation_Write_Tests.sql` |
| `USP_HC_예약_취소` | UPDATE | 3 | 2 | RP-10, CP-05 | **O** | `WORK` | `06_Reservation_Write_Tests.sql` |
| `USP_HC_예약접수목록_조회` | SELECT | 5 | 2 | CP-05, 검색계약 | X | X | `04_Select_SP_Tests.sql` |
| `USP_HC_예약접수상세_조회` | SELECT | 1 | **5** (RS0~RS4) | CP-05, 상태 Matrix | X | X | `04_Select_SP_Tests.sql` |
| `USP_HC_변경이력_조회` | SELECT | 2 | 2 | CP-06 | X | X | `04_Select_SP_Tests.sql` |
| `USP_HC_접수_완료` | UPDATE | 3 | 2 | RCP-01~04 | **O** | **`PAT` → `WORK` → `SLOT`** | `07_Reception_Write_Tests.sql` |
| `USP_HC_접수추가검사_변경` | UPDATE | 10 | 2 | RCP-05, AEX | **O** | `WORK` | `07_Reception_Write_Tests.sql` |
| `USP_HC_접수_취소` | UPDATE | 3 | 2 | RCP-06, CP-05 | **O** | `WORK` | `07_Reception_Write_Tests.sql` |
| `USP_HC_휴무일목록_조회` | SELECT | 3 | 3 (RS0~RS2) | HOL-03~06 | X | X | `15_Holiday_Tests.sql` |
| `USP_HC_자체휴무일_등록` | INSERT | 4 | 2 | HOL-04~05 | **O** | `HOL` | `15_Holiday_Tests.sql` |
| `USP_HC_자체휴무일_수정` | UPDATE | 5 | 2 | HOL-04~05 | **O** | `HOL` | `15_Holiday_Tests.sql` |
| `USP_HC_자체휴무일_삭제` | DELETE | 2 | 1 (RS0) | HOL-05 | **O** | `HOL` | `15_Holiday_Tests.sql` |

합계: SELECT 9 / INSERT 3 / UPDATE 7 / DELETE 1 = **20개**. Param 합계 **113개**(§36).

`[R7]` **DELETE SP 가 0개에서 1개가 되었다.** `§2.2`·`G06` 이 *"DELETE SP 0개"* 를 금지 조건으로 들고 있었는데, 그것은 **업무 데이터를 지우는 SP** 를 막는 규칙이었다(취소는 상태전이이지 삭제가 아니다 — `00` CP-05). `USP_HC_자체휴무일_삭제` 는 기준정보 1행을 지우며 업무 데이터를 건드리지 않는다. `G06` 의 조건을 *"업무 테이블(`수검자`·`예약접수`·`완료이력`)을 지우는 SP 0개"* 로 좁힌다.

`[R3]` **Param 열은 R3 재봉인을 반영한 값이다.** Write SP 8개에 `@조작자명` 이, 수검자 Write 2개에
`@B형간염제외여부` 가 더해졌다. `USP_HC_수검자정보_수정` 의 Lock 은 §24.1 이 조건부(`SSN?`·`CHART?`)에서
**조건 없이 항상**으로 바꿨다 — 변경 여부를 알려면 현재 행을 먼저 읽어야 하고 그러면 §23 전역 순서가 뒤집힌다.

---

# 19. 공통 RS0 구현 Pattern `[B]`

모든 외부 SP의 첫 번째 Result Set은 **정확히 1행**이며 컬럼 순서·타입을 고정한다.

```sql
SELECT
      CAST(@성공여부 AS BIT)          AS [성공여부]
    , CAST(@결과코드    AS INT)          AS [결과코드]
    , CAST(@결과메시지 AS NVARCHAR(300))AS [결과메시지]
    , CAST(@오류항목   AS VARCHAR(50))  AS [오류항목]
    , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
```

| 순서 | 컬럼 | 타입 | NULL |
|---:|---|---|:---:|
| 1 | `성공여부` | `BIT` | X |
| 2 | `결과코드` | `INT` | X |
| 3 | `결과메시지` | `NVARCHAR(300)` | X |
| 4 | `오류항목` | `VARCHAR(50)` | O |
| 5 | `서버시각` | `DATETIME2(7)` | X |

`[I]` **모든 컬럼에 명시적 `CAST`를 건다.**

`[X 실측]` **DMV 로는 16/16 을 볼 수 없다.** `sys.dm_exec_describe_first_result_set_for_object` 는
`sp_getapplock` 을 호출하는 SP 에서 **`Msg 11520`** 으로 실패한다 — 그 내부가 확장 프로시저
`sys.xp_userlock` 을 부르기 때문이다. `sp_getapplock` 은 §22·§24 가 Write SP 8개 전부에
의무화한 것이므로 이 미달은 구현 결함이 아니라 **스펙 내부 모순**이었다.
실측: 16개 중 8개 실패, 총 48행(기대 80).

판정 경로를 셋으로 나눈다.

```text
SELECT SP 8개   DMV 로 40행 · error_number 0건 · 5컬럼 타입 문자열 대조
Write SP 8개    verify-contract.js 가 계약 시나리오의 **실측 출력 헤더**로 컬럼명·순서를 판정
전체 20개       verify-docs.js V17 이 배포 SQL 의 RS0 CAST 패턴을 정적으로 대조해 타입을 메꾼다
```

`서버시각`은 SP 시작 시 캡처한 `@서버시각`을 그대로 사용하며, 실패 경로에서도 동일 값을 반환한다.

`오류항목`는 Parameter 이름에서 `@`를 제외한 문자열이며, 특정 입력 하나로 귀속할 수 없으면 `NULL`이다 (`05` §2.5).

---

# 20. 예상 업무실패와 THROW 경계 `[B]` `[I]`

| 사건 | 처리 | 근거 |
|---|---|---|
| 필수값 누락·형식 위반·조합 오류 | RS0 `100~104` | `05` §4.2 |
| Entity 부재·소유 불일치·상태·동시성·정원·Rule 위반 | RS0 해당 코드 | `05` §4.2 |
| 조회 0건 | RS0 `성공여부=1, 결과코드=0`, RS1 0행 | `05` §3.4 |
| `SELECT_예약가능정보`의 업무 불가 | RS0 성공 + RS1/RS2/RS3/RS5의 `차단코드`/`사유코드` | `05` §3.3 |
| `applock` rc `-3` (deadlock victim) | `ROLLBACK` 후 **`THROW 50002`** | §25 |
| `applock` rc `-1`(timeout) / `-2`(취소) / `-999`(호출오류) | `ROLLBACK` 후 **`THROW 50001`** | §25 |
| Write SP 진입 시 `@@TRANCOUNT > 0` | **`THROW 50003`** (트랜잭션을 열기 전이라 되돌릴 것이 없다) | §21.1a |
| Deadlock 1205, Lock timeout 1222 | `CATCH`에서 `ROLLBACK` 후 **`THROW`** (원본 유지) | `05` §3.6 |
| Unique 위반 2601/2627 | `CATCH`에서 `ROLLBACK` 후 **`THROW`** (원본 유지) | applock으로 사전 직렬화했으므로 발생 시 설계 위반. 숨기지 않는다 |
| 그 밖의 예상하지 못한 SQL/시스템 오류 | `CATCH`에서 `ROLLBACK` 후 **`THROW`** | `05` §3.6 |

**하지 않는 것** (`05` §3.6):

```text
업무결과를 OUTPUT Parameter 또는 SQL RETURN 값으로 전달
업무실패를 RAISERROR 로 전달
결과메시지 문자열 비교로 C# 분기
Catalog 38개에 없는 실패를 기존 업무코드에 억지 매핑
```

`[I]` 사용자 정의 오류번호:

| 번호 | 의미 |
|---:|---|
| `50001` | 잠금 **timeout** (`sp_getapplock` rc `-1`) 및 rc `-2`/`-999` |
| `50002` | 잠금 **deadlock victim** (`sp_getapplock` rc `-3`) |
| `50003` | Write SP 를 **호출자 트랜잭션 안**에서 실행하려 함 (§21.1a) |
| `50010`~`50015` | `00_Preflight.sql` 안전가드 위반 (§8.3) |
| `50020`~`50024` | `Rebuild.sql` 안전가드 위반 (§8.3) |
| `51000` | 테스트 파일 실패 집계 (테스트 스크립트 전용) |
| `51001` | 동시성 barrier 시각 경과 (테스트 스크립트 전용) |
| `51002` | `02_Seed.sql` 이 비어있지 않은 DB에 재실행됨 (배포 스크립트 전용, §8.1) |

`[I]` `50001`과 `50002`를 나눈 이유: 초안은 timeout과 deadlock을 한 번호로 뭉갰다. 그러면 `CON-007`의 판정 *"로그에 `Msg 1205` 0건"*이 **항상 참**이 된다 — applock 교착은 `1205`가 아니라 `50001`로 나오기 때문이다. 번호를 나누면 `CON-007`을 "`1205` 0건 **그리고** `50002` 0건"으로 실제 판정 가능하게 만들 수 있다. `05` §4.2 ResultCode Catalog와 무관한 시스템 오류번호이므로 계약 위반이 아니다.

---

# 21. 8개 Write SP Transaction Matrix `[I]`

## 21.1 공통 Template

```sql
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_...]
    ...
    , @조작자명 NVARCHAR(50)          -- Write SP 8개 공통. Parameter 목록 맨 끝
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @서버시각 DATETIME2(7) = SYSDATETIME();
    DECLARE @오늘날짜      DATE          = CONVERT(DATE, @서버시각);
    DECLARE @현재시각    TIME(7)       = CONVERT(TIME(7), @서버시각);
    DECLARE @저장시각  DATETIME2(0)  = CONVERT(DATETIME2(0), @서버시각);
    DECLARE @결과코드 INT = 0, @오류항목 VARCHAR(50) = NULL, @결과메시지 NVARCHAR(300) = NULL;
    DECLARE @대상키 BIGINT = NULL;      -- 감사 기록용. 확정되기 전에는 NULL

    -- [1] Transaction 밖: 입력 정규화 / 필수값 / 허용값 / 조합
    --     실패 시 RS0 1행만 SELECT → <감사 블록> → RETURN. 트랜잭션을 열지 않는다.

    -- [2] Transaction 밖: 잠금키 확보용 사전조회 ([업무ID] → [수검자ID] 등)
    --     결과는 stale 가능. [4]에서 반드시 재검증한다.
    --     실패 시 [1]과 같은 순서로 RS0 SELECT → <감사 블록> → RETURN.

    BEGIN TRY
        BEGIN TRANSACTION;

        -- [3] applock 획득 — 전역 순서(§23). 음수 [rc]면 즉시 THROW 50001
        --     applock 실패는 감사 기록하지 않는다 (`04` §14 L2)
        -- [4] 재검증: 존재 → 소유 → 상태 → 동시성 → Master 구성 → 업무 Rule → 정원
        IF @결과코드 <> 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT CAST(0 AS BIT)                  AS [성공여부]
                 , CAST(@결과코드 AS INT)              AS [결과코드]
                 , CAST(@결과메시지 AS NVARCHAR(300))     AS [결과메시지]
                 , CAST(@오류항목 AS VARCHAR(50))     AS [오류항목]
                 , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];   -- RS0만
            -- <감사 블록>  @@TRANCOUNT = 0 이므로 자동커밋이다
            RETURN;
        END

        -- [5] 저장 (No-op이면 아무것도 쓰지 않고 통과)
        --     INSERT 계열은 업무 INSERT 직후 SET @대상키 = SCOPE_IDENTITY(); 를 즉시 실행한다.
        --     감사 INSERT 뒤에 읽으면 HistoryId 가 나온다 (`04` §8.7.4)
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;     -- 예상하지 못한 오류는 감사 기록하지 않는다 (`04` §14 L3)
    END CATCH

    -- [6] COMMIT 이후에만 성공 Result Set 출력
    SELECT CAST(1 AS BIT)                    AS [성공여부]
         , CAST(@결과코드 AS INT)                AS [결과코드]   /* 0 또는 1 또는 2 */
         , CAST(@결과메시지 AS NVARCHAR(300))       AS [결과메시지]
         , CAST(NULL AS VARCHAR(50))         AS [오류항목]
         , CAST(@서버시각 AS DATETIME2(7)) AS [서버시각];
    SELECT ... RS1 ...;

    -- [7] <감사 블록>  성공 경로. RS0·RS1 을 모두 낸 뒤에 둔다
END
```

`[X]` **`plans/10` 이 `변경이력`을 EAV 로 바꾸면서 감사 지점이 넷에서 하나로 줄었다.**
`00` CP-06 이 "실제로 바꾼 컬럼마다 1행"으로 개정되어 데이터를 바꾸지 않은 호출은 기록하지 않는다.
`[1]` 입력검증 실패 · `[2]` 사전조회 실패 · `[4]` 업무실패는 데이터를 바꾸지 않으므로 **기록 지점이 아니다.**
남는 것은 `[7]` 성공 하나이며, 그 안에서 **실제로 값이 바뀐 컬럼마다** 1행을 넣는다.

```sql
BEGIN TRY
    INSERT INTO [dbo].[변경이력]
        ([기록일시], [조작자명], [대상테이블], [대상키], [컬럼명], [변경전], [변경후])
    SELECT @저장시각, @조작자명, N'예약접수', @대상키, V.[컬럼명], V.[변경전], V.[변경후]
      FROM (VALUES
              (N'상태코드', @Before상태, @적용후상태)
            , (N'예약일',   CONVERT(NVARCHAR(4000), @Before예약일), CONVERT(NVARCHAR(4000), @적용후예약일))
           ) V([컬럼명], [변경전], [변경후])
     WHERE ISNULL(V.[변경전], N'~NULL~') <> ISNULL(V.[변경후], N'~NULL~');   -- 실제로 바뀐 것만
END TRY
BEGIN CATCH
END CATCH
```

`VALUES` 행 생성자로 컬럼 목록을 세우고 `WHERE` 가 실제 변경분만 남긴다. 그래서 값이 같은 컬럼은 기록되지 않는다.
`NULL` 끼리도 같은 것으로 보아야 하므로 `ISNULL` 로 감싼다 (`05` §28.2 NULL-safe 비교와 같은 규칙).
`변경전`은 SP가 Transaction 안의 재검증 단계에서 이미 읽은 값을 변수에 담아 재사용한다. 추가 조회를 하지 않는다.

세 가지가 규칙이다.

| # | 규칙 | 근거 |
|---:|---|---|
| ① | 항상 **`@@TRANCOUNT = 0` 지점**에서만 실행한다 | `[7]`은 `COMMIT` 뒤다. 자동커밋이라 업무 트랜잭션에 영향을 줄 수 없다 |
| ② | 자체 `TRY/CATCH`로 감싸고 `CATCH`를 **비운다** | 기록 실패가 업무 호출 결과를 바꾸지 않는다. 바깥 `CATCH`에 도달하지 않으므로 계약된 RS0가 반드시 나간다 |
| ③ | **RS0** 의 `SELECT`를 먼저 낸 뒤, **RS1 보다 앞에서** 실행한다 | `RBK-008`(실패 응답 RS0 1개만)과 `contract/` 의 RS 개수 판정이 흔들리지 않는다 |

`[X 실측 2026-09-08]` **초안은 감사를 RS1 **뒤**에 두었고, 그러면 클라이언트가 RS0 만 읽고
끊었을 때(`ExecuteNonQuery` · 조기 `Dispose`) 업무는 커밋됐는데 감사행만 없는 상태가 된다.**
초안의 근거는 *"성공 경로에서 RS1 이 실패하면 감사행도 남지 않아 로그가 성공을 주장하지 않는다"* 였는데,
RS1 은 커밋 뒤의 단순 `SELECT` 하나라 사실상 실패하지 않는다. 반대 방향(정상 성공 + 감사 누락)은
검토된 적이 없었다.

`[I]` **기준선 `04` §8.6.5 ③ 은 "**해당** Result Set 의 `SELECT` 를 먼저 낸 뒤에" 로 단수다.**
같은 절의 목적은 `04` §14 L1 *"기록이 업무 호출을 실패시키는 경로를 만들지 않는다"* 이고,
감사를 RS0 뒤에 두어도 그 목적은 그대로다. 그래서 "해당 Result Set" 을 **결과를 보고하는 RS0** 로
읽고 `RS0 → 감사 → RS1` 로 배치한다. **기준선을 열지 않았다.**
관점에 따라 ③ 를 "모든 RS 뒤" 로 읽을 수 있으므로 이 해석을 여기 남긴다 — 다음 사람이 판단할 수 있게.

순서는 눈에 안 띄게 되돌아갈 수 있어 `V19` 가 정적으로 고정한다:
각 Write SP 본문에서 `-- [6]` < `-- [7] 감사 기록` < `-- [6b] RS1` 이어야 한다 (8개 전부).

`[X]` 초안은 이 Template의 `SELECT`에 **컬럼 별칭이 없었다.** 무명 컬럼은 `05` §3.1의 RS0 컬럼명 계약과 §36.2의 `sys.dm_exec_describe_first_result_set_for_object` 검증을 동시에 깨뜨린다. 5개 컬럼 전부에 `CAST … AS <이름>`을 건다.

핵심 5가지:

| # | 결정 | 근거 |
|---:|---|---|
| 1 | 성공 Result Set은 **`COMMIT` 이후**에만 출력 | Commit 전 출력 시 C#이 실패를 성공으로 읽는 위험 차단 |
| 2 | 예상 업무실패 = `ROLLBACK` → RS0 1행만 → `RETURN` | `05` §3.5 (`INSERT_수검자` 202/203만 RS1 동반) |
| 3 | 예상치 못한 오류 = `CATCH`에서 `ROLLBACK` 후 원본 `THROW` | `05` §3.6 |
| 4 | 입력검증은 **Transaction 밖** | 실패 대다수를 잠금 없이 종료 |
| 5 | **No-op도 Transaction·잠금 안에서 판정** 후 UPDATE 없이 `COMMIT` | 상태·동시성의 원자적 확인 보장 |

`ROLLBACK` 후 배치가 정상 진행되고 `XACT_STATE()=0`이 되는 것은 §5.3-4에서 실측 확인했다.

## 21.1a Write SP 진입 가드 `[신설 2026-09-07]`

`[X 실측]` **Write SP 8개 전부 진입 시 `@@TRANCOUNT` 를 보지 않았다.** `deploy/` 전체에서
`@@TRANCOUNT` 는 주석에만 있었다. savepoint 없이 `BEGIN`/`COMMIT`/`ROLLBACK` 을 맨몸으로 쓰므로
호출자가 트랜잭션을 열고 부르면 네 가지가 동시에 깨진다.

```text
업무실패   이름 없는 ROLLBACK 이 **바깥 트랜잭션까지** 되돌리고 EXEC 반환 시 Msg 266
성공       COMMIT 이 @@TRANCOUNT 를 2->1 로 줄일 뿐인데 RS0 성공여부=1 이 나간다
감사       §21.1 ① 의 "@@TRANCOUNT = 0 지점" 전제가 거짓이 되어 감사행이 함께 롤백된다
잠금       @LockOwner=Transaction 이라 applock 이 바깥 트랜잭션이 끝날 때까지 살아남는다
```

현재 호출 경로에서는 발생하지 않았다. 그러나 그것을 보장하던 것은 **호출자의 관습뿐** 이었다.
Phase 5 가 "예약 저장 + 애플리케이션 로그" 를 한 SqlTransaction 으로 묶는 순간 터진다.

```sql
IF @@TRANCOUNT > 0
    THROW 50003, N'이 프로시저는 호출자 트랜잭션 안에서 실행할 수 없습니다.', 1;
```

`[실측]` 호출자 트랜잭션 안 → `Msg 50003` 이고 **바깥 트랜잭션이 살아남는다**
(`@@TRANCOUNT=1` · `XACT_STATE=1`) — doomed 가 아니므로 호출자가 자기 작업을 되돌리거나
커밋할 수 있다. 트랜잭션 밖 호출은 그대로 동작한다.

`[I]` **Phase 5 는 Write SP 호출을 SqlTransaction 으로 감싸지 않는다.** SP 하나가 곧 한 업무단위다.

---

## 21.2 Transaction Matrix (인계문서 §9.4 형식)

| SP | 입력 정규화 | 사전조회 | Transaction 시작 | App Lock | Row Lock | 재검증 | 변경대상 | Commit | 성공 반환 | 예상 실패 |
|---|---|---|---|---|---|---|---|---|---|---|
| `INSERT_수검자` | Trim/NULL화/UPPER, 주민번호 13자리·날짜·세기·성별 검증, 생년월일/성별 산출 | 없음 | 잠금 직전 | `SSN` → `CHART`(수동) | 없음 | 공통 업무가능 → 동일 SSN 조회 → 성명+생년월일 후보 → 차트번호 고유성 → Sequence 발급 | `수검자` 1행 INSERT + `변경이력` 컬럼 수만큼(트랜잭션 밖) | O | RS0(0 또는 2) + RS1 수검자결과 | 100~102, 201~203, 206, 308~309 (202/203은 RS1 동반) |
| `UPDATE_수검자정보` | 동일 | `수검자ID`(입력) | 잠금 직전 | **`SSN` → `CHART` → `PAT`** (조건 없이 항상, §24.1) | 없음 | Patient 존재 → `행버전` → 실제 변경 여부(**NULL-safe**, §28.2) → 공통 업무가능 → 차트번호/SSN 고유성 → SSN 변경 시 RSV/RCP 부재 | `수검자` 1행 UPDATE (No-op이면 없음) + `변경이력` 바뀐 컬럼 수만큼(트랜잭션 밖) | O | RS0(0 또는 1) + RS1(수검자ID, 차트번호, 행버전) | 100~102, 200~201, 204~205, 601, 308~309 |
| `INSERT_예약` | 허용값·AEX 7 BIT NOT NULL·WalkIn 날짜 | 없음 | 잠금 직전 | `PAT` → `SLOT(신규)` | 없음 | Patient 존재 → 검사 Master 구성 → 공통 업무가능 → 다른 유효업무(2건↑ 701, 1건 306) → 일정·마감 → 정원 → TGT → NEX → AEX | `예약접수` INSERT(RSV) + `검사항목` NEX 전체 + 유효선택여부 AEX + `변경이력` 1행(트랜잭션 밖) | O | RS0(0) + RS1(업무ID, 상태코드, 행버전) | 100~102, 200, 300~306, 308~309, 400~401, 410~412, 700~701 |
| `UPDATE_예약변경` | 허용값·AEX 7 BIT NOT NULL | `업무ID` → `수검자ID`, 현재 Date/시간대/AEX 집합 | 잠금 직전 | `PAT` → `WORK` → `SLOT(기존·신규 정렬)` | 없음 | Work 존재 → 상태코드=RSV → 행버전 → 변경범위 계산 → 변경범위별 Rule(§29) | 변경범위에 따라 Work / NEX·AEX Detail (No-op이면 없음) + `변경이력` 1행(트랜잭션 밖) | O | RS0(0 또는 1) + RS1 | 100~102, 300~306, 308~309, 400~401, 410~412, 500, 502, 601, 700~701 |
| `UPDATE_예약취소` | 필수값 | `업무ID` → `수검자ID` | 잠금 직전 | `WORK` | 없음 | Work 존재 → 상태코드=RSV → 행버전 → 공통 업무가능 | `예약접수.StatusCode='CNR'` 조건부 UPDATE. Detail 보존 + `변경이력` 1행(트랜잭션 밖) | O | RS0(0) + RS1 | 100, 500, 502, 601, 308~309 |
| `UPDATE_접수완료` | 필수값 | `업무ID` → `수검자ID`, `예약일`, `TimeSlotCode` | 잠금 직전 | **`PAT` → `WORK` → `SLOT`** (§24.2) | 없음 | Work 존재 → 상태코드=RSV → 행버전 → 검사구성 무결성(NEX≥1 **및 Master 역할 일치**) → 공통 업무가능 → `예약일=@오늘날짜` → 접수마감 전 | `StatusCode='RCP'` 조건부 UPDATE. Date/시간대/NEX/AEX 불변 + `변경이력` 1행(트랜잭션 밖) | O | RS0(0) + RS1 | 100, 304, 308~309, 500, 502~503, 601, 701 |
| `UPDATE_접수추가검사` | AEX 7 BIT NOT NULL | `업무ID`, 현재 AEX 집합 | 잠금 직전 | `WORK` | 없음 | Work 존재 → 상태코드=RCP → 행버전 → 저장 NEX·AEX 확인 → 집합 비교 → 동일이면 No-op → 변경이면 Master 구성·성별·NEX 중복 | AEX Detail DELETE/INSERT + Work `최종수정일시` UPDATE (No-op이면 없음) + `변경이력` 1행(트랜잭션 밖) | O | RS0(0 또는 1) + RS1 | 100, 308~309, 410~412, 500, 502, 601, 700~701 |
| `UPDATE_접수취소` | 필수값 | `업무ID` | 잠금 직전 | `WORK` | 없음 | Work 존재 → 상태코드=RCP → 행버전 → 공통 업무가능 | `StatusCode='CNC'` 조건부 UPDATE. Detail 보존 + `변경이력` 1행(트랜잭션 밖) | O | RS0(0) + RS1 | 100, 308~309, 500, 502, 601 |
| `INSERT_자체휴무일` | 필수값 | `휴무일자` | 잠금 직전 | `HOL` | 없음 | 휴무일명 공백 아님 → 같은 날짜 부재 | `휴무일` 1행 INSERT (`휴무구분` = `자체휴무일` 고정). **`변경이력` 없음** | O | RS0(0) + RS1 | 100~101, 801 |
| `UPDATE_자체휴무일` | 필수값 | `휴무일자` | 잠금 직전 | `HOL` | 없음 | 휴무일 존재 → 휴무구분=자체휴무일 → 행버전 → 실제 변경 여부 | `휴무일` 1행 UPDATE (No-op이면 없음). **`변경이력` 없음** | O | RS0(0 또는 1) + RS1 | 100~101, 601, 800, 802 |
| `DELETE_자체휴무일` | 필수값 | `휴무일자` | 잠금 직전 | `HOL` | 없음 | 휴무일 존재 → 휴무구분=자체휴무일 → 행버전 | `휴무일` 1행 DELETE. **`변경이력` 없음** | O | RS0(0) | 100, 601, 800, 802 |

`Row Lock` 열이 전부 "없음"인 이유는 §24.2에 있다.

## 21.2a Work 검사구성 무결성 — `04` §14의 3개 불변조건 `[X 수정]`

`04` §14는 선언적 제약으로 보장할 수 없는 항목을 Write SP에 위임했다. 초안은 그중 **"Work에 NEX 최소 1건"만** 구현하고 나머지 둘을 빠뜨렸다.

`INSERT_예약`·`UPDATE_예약변경`·`UPDATE_접수완료`·`UPDATE_접수추가검사`가 저장 NEX/AEX를 읽을 때 **세 가지를 모두** 확인한다.

`[!]` 검사구성은 `예약접수` 행의 컬럼 2개다(`04` §8.2.2). 개수는 쉼표를 세고, 코드 존재·역할은
양끝 패딩 `LIKE` 로 `검사코드` 를 조인해 확인한다(`§9.2` 허용목록). **빈 문자열은 0개다** —
`LEN - LEN(REPLACE) + 1` 만 쓰면 빈 문자열을 1개로 세어 `CORRUPT-2` 를 놓친다.

```sql
DECLARE @국가검사 NVARCHAR(100), @추가검사 NVARCHAR(50);
SELECT @국가검사 = [국가검사항목], @추가검사 = [추가검사항목]
  FROM [dbo].[예약접수] WHERE [업무ID] = @업무ID;

DECLARE @저장국가검사건수 INT = CASE WHEN LEN(ISNULL(@국가검사, N'')) = 0 THEN 0
                         ELSE LEN(@국가검사) - LEN(REPLACE(@국가검사, N',', N'')) + 1 END;
DECLARE @저장추가검사건수 INT = CASE WHEN LEN(ISNULL(@추가검사, N'')) = 0 THEN 0
                         ELSE LEN(@추가검사) - LEN(REPLACE(@추가검사, N',', N'')) + 1 END;

-- (1) NEX 개수가 8~11 범위인가            04 §2.3 / 05 §6.3.2
IF @저장국가검사건수 NOT BETWEEN 8 AND 11
    SET @결과코드 = 701;

-- (2) 저장된 코드가 Master 에 실재하며 역할이 맞는가   04 §8.2.3 / §14
--     FK 가 없으므로(04 §9.1) 이 확인이 그 자리를 대신한다.
--     역할이 맞는 코드만 세어 개수가 일치하지 않으면 손상이다.
IF ((SELECT COUNT(*) FROM [dbo].[검사코드] m
      WHERE m.[국가검사규칙코드] IS NOT NULL
        AND N',' + ISNULL(@국가검사, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%') <> @저장국가검사건수
 OR (SELECT COUNT(*) FROM [dbo].[검사코드] m
      WHERE m.[추가검사코드] IS NOT NULL
        AND N',' + ISNULL(@추가검사, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%') <> @저장추가검사건수)
    SET @결과코드 = 701;

-- (3) AEX 개수가 0~6 범위인가                       04 §2.3
IF @저장추가검사건수 > 6
    SET @결과코드 = 701;
```

`[X]` **초안 오류 2건**
1. `04` §14의 *"검사구성 코드가 `검사코드`에 실재 → NEX/AEX 저장검증"* 항목에 대응하는 검증이 스펙에도 계획에도 **없었다.** AEX 전용 Master 행을 `ExamSourceCode='NEX'` 로 저장한 손상 Work는 NEX 행수가 1 이상이라 접수완료를 그대로 통과한다.
2. NEX 검증이 `< 8` 만 확인했다. `05` §17.4가 *"모든 TGT 대상 결과가 8~11행 범위인지 검증"* 이라고 명시했는데 상한을 버려, TVF나 Master가 손상되어 12행을 반환해도 저장이 계속된다. `NOT BETWEEN 8 AND 11` 로 교체한다.

## 21.3 부분저장 차단

- `SET XACT_ABORT ON` + Master/Detail을 **단일 Transaction**에서만 기록한다.
- Detail 재작성은 `DELETE` → `INSERT` → Work `UPDATE`가 모두 같은 Transaction이다.
- 업무 데이터는 어떤 단계에서 실패해도 전량 롤백된다. `tests/08_Rollback_Tests.sql`이 "부분저장 0건"을 검증한다.
- **`변경이력` 1행은 이 규칙의 의도된 예외다.** 실패한 호출도 기록해야 하는데 트랜잭션 안에 두면 `ROLLBACK`과 함께 사라지므로, §21.1 `<감사 블록>` 은 항상 트랜잭션 밖 자동커밋으로 실행한다. `RBK` 계열은 `변경이력`을 "부분저장"으로 계산하지 않는다.

---

# 22. Lock Resource 네이밍 `[D4-007]`

```text
HC|SSN|{SHA2_256 hex 64자}          예: HC|SSN|E9F1…F3FD   (총 71자)
HC|CHART|{차트번호}
HC|PAT|{수검자ID}
HC|WORK|{업무ID}
HC|SLOT|{yyyyMMdd}|{AM|PM}          예: HC|SLOT|20260910|AM
HC|HOL|{yyyyMMdd}                   예: HC|HOL|20270314
```

| 항목 | 값 | 근거 |
|---|---|---|
| `@LockMode` | `Exclusive` | 모든 자원 |
| `@LockOwner` | `Transaction` | `COMMIT`/`ROLLBACK` 시 **자동 해제**. 명시적 `sp_releaseapplock` 불필요 (§5.3-1 실측) |
| `@LockTimeout` | **5000** ms | `[D4-008]` |
| `@DbPrincipal` | `public` (기본) | 별도 지정 없음 |

`[I]` **`SSN` 자원은 원문이 아니라 `HASHBYTES('SHA2_256', @주민번호)`의 64자 hex를 사용한다.** `sys.dm_tran_locks`·오류 메시지·DMV에 테스트 주민번호가 노출되지 않게 하기 위함이며, 자원명 71자는 `nvarchar(255)` 제한 안이다(§5.3-2 실측). `차트번호`는 개인식별 위험이 낮아 평문을 사용한다.

`[I]` **Lock Matrix** (인계문서 §9.5 형식)

| SP | Resource | Key 형식 | Mode | Owner | Timeout | 획득순서 | Release | 실패처리 |
|---|---|---|---|---|---:|---:|---|---|
| `INSERT_수검자` | SSN | `HC\|SSN\|{sha256}` | Exclusive | Transaction | 5000 | 1 | 자동 | `THROW 50001` |
| `INSERT_수검자` | CHART (**수동·자동 모두**) | `HC\|CHART\|{차트번호 또는 발급후보}` | Exclusive | Transaction | 5000 | 2 | 자동 | `THROW 50001` |
| `UPDATE_수검자정보` | SSN (**항상**) | `HC\|SSN\|{sha256}` | Exclusive | Transaction | 5000 | 1 | 자동 | `THROW 50001` |
| `UPDATE_수검자정보` | CHART (**항상**) | `HC\|CHART\|{차트번호}` | Exclusive | Transaction | 5000 | 2 | 자동 | `THROW 50001` |
| `UPDATE_수검자정보` | PAT | `HC\|PAT\|{수검자ID}` | Exclusive | Transaction | 5000 | 3 | 자동 | `THROW 50001` |
| `INSERT_예약` | PAT | `HC\|PAT\|{수검자ID}` | Exclusive | Transaction | 5000 | 3 | 자동 | `THROW 50001` |
| `INSERT_예약` | SLOT | `HC\|SLOT\|{yyyyMMdd}\|{AM\|PM}` | Exclusive | Transaction | 5000 | 5 | 자동 | `THROW 50001` |
| `UPDATE_예약변경` | PAT | `HC\|PAT\|{수검자ID}` | Exclusive | Transaction | 5000 | 3 | 자동 | `THROW 50001` |
| `UPDATE_예약변경` | WORK | `HC\|WORK\|{업무ID}` | Exclusive | Transaction | 5000 | 4 | 자동 | `THROW 50001` |
| `UPDATE_예약변경` | SLOT × 1~2 | `HC\|SLOT\|…` | Exclusive | Transaction | 5000 | 5 (**문자열 오름차순**) | 자동 | `THROW 50001` |
| `UPDATE_예약취소` | WORK | `HC\|WORK\|{업무ID}` | Exclusive | Transaction | 5000 | 4 | 자동 | `THROW 50001` |
| `UPDATE_접수완료` | PAT | `HC\|PAT\|{수검자ID}` | Exclusive | Transaction | 5000 | 3 | 자동 | `THROW 50001` |
| `UPDATE_접수완료` | WORK | `HC\|WORK\|{업무ID}` | Exclusive | Transaction | 5000 | 4 | 자동 | `THROW 50001` |
| `UPDATE_접수완료` | SLOT | `HC\|SLOT\|{yyyyMMdd}\|{AM\|PM}` | Exclusive | Transaction | 5000 | 5 | 자동 | `THROW 50001` |
| `UPDATE_접수추가검사` | WORK | `HC\|WORK\|{업무ID}` | Exclusive | Transaction | 5000 | 4 | 자동 | `THROW 50001` |
| `UPDATE_접수취소` | WORK | `HC\|WORK\|{업무ID}` | Exclusive | Transaction | 5000 | 4 | 자동 | `THROW 50001` |
| `INSERT_자체휴무일` | HOL | `HC\|HOL\|{yyyyMMdd}` | Exclusive | Transaction | 5000 | 6 | 자동 | `THROW 50001` |
| `UPDATE_자체휴무일` | HOL | `HC\|HOL\|{yyyyMMdd}` | Exclusive | Transaction | 5000 | 6 | 자동 | `THROW 50001` |
| `DELETE_자체휴무일` | HOL | `HC\|HOL\|{yyyyMMdd}` | Exclusive | Transaction | 5000 | 6 | 자동 | `THROW 50001` |

---

# 23. Lock 획득 총순서 `[I]`

**모든 Write SP가 동일한 전역 순서를 따른다.**

```text
1. SSN     HC|SSN|…
2. CHART   HC|CHART|…
3. PAT     HC|PAT|…
4. WORK    HC|WORK|…
5. SLOT    HC|SLOT|…      (동종 복수는 자원명 문자열 오름차순)
6. HOL     HC|HOL|…
```

## 23.0 `HOL` 이 왜 맨 뒤인가 `[R7]`

휴무일 Write SP 세 개는 **`HOL` 하나만** 잡고, 업무 SP 는 `HOL` 을 잡지 않는다. 두 무리가 만나지 않으므로
교착이 성립하지 않고 전역 순서에서 어디에 두든 결과가 같다. 순서를 정해 두는 것은 다음에 `HOL` 과 다른 자원을
함께 잡는 SP 가 생겼을 때 그 자리를 다시 논의하지 않기 위해서다.

`[!]` **PK 만으로 충분하지 않은가** — `휴무일자` 가 PK 라 중복 INSERT 는 어차피 막힌다. 그런데 잠금 없이
동시 INSERT 하면 한쪽이 `Msg 2627` 로 죽고, `G11` 이 `2627` **0건**을 요구한다. 제약 위반을 잡아 `801` 로
번역하는 대신 앞에서 직렬화한다 — 이 저장소가 `SSN`·`CHART` 에 쓰는 방식과 같다.

## 23.1 예약 이동의 결정적 순서

시간대 자원명이 `HC|SLOT|yyyyMMdd|AM|PM` 형식이므로 **문자열 오름차순 = (날짜, 시간대) 오름차순**이다.

```text
HC|SLOT|20260910|AM  <  HC|SLOT|20260910|PM  <  HC|SLOT|20260911|AM
```

두 세션이 서로 시간대을 교환하는 교차이동에서도 **양쪽이 동일한 순서로 획득**하므로 교착이 구조적으로 발생하지 않는다 (`04` §3.10 "결정적 정렬순서"의 구현).

## 23.2 `업무ID`만 받는 SP의 `수검자ID` 확보

`UPDATE_예약변경`은 `@업무ID`만 받으므로 `PAT` 자원명을 만들려면 `수검자ID`가 필요하다. 전역 순서상 `PAT`이 `WORK`보다 앞이므로 다음과 같이 처리한다.

```text
[Transaction 밖] SELECT @수검자ID = 수검자ID FROM 예약접수 WHERE 업무ID=@업무ID
                 없으면 500 WorkNotFound 반환 (트랜잭션을 열지 않음)
[Transaction 안] applock PAT → applock WORK → applock SLOT(정렬)
                 SELECT … FROM 예약접수 WHERE 업무ID=@업무ID
                 → 존재 / 수검자ID 동일 / 상태코드 / 행버전 전부 재검증
```

사전조회는 잠금 밖이라 stale할 수 있으나 잠금 후 재검증이 최종 판정이므로 안전하다. `수검자ID`는 `EP-02`에 의해 불변이고 예약변경은 `수검자ID`를 바꾸지 않는다.

---

# 24. Write SP별 Lock Sequence `[I]`

## 24.1 SP별 획득 목록

| SP | 획득 잠금 (순서대로) | 근거 |
|---|---|---|
| `INSERT_수검자` | `SSN` → `CHART` (**수동·자동 모두**, §12.6) | `05` §14 "차트번호, 주민번호" |
| `UPDATE_수검자정보` | `SSN` → `CHART` → `PAT` (**조건 없이 항상**) | `05` §14 "수검자ID, 변경 차트번호/주민번호" |
| `INSERT_예약` | `PAT` → `SLOT(신규)` | `05` §14 "수검자ID, 대상 Date+시간대" |
| `UPDATE_예약변경` | `PAT` → `WORK` → `SLOT(기존·신규 정렬)` | `05` §14 "업무ID, 수검자ID, 기존/신규 시간대" |
| `UPDATE_예약취소` | `WORK` | `05` §14 "업무ID" |
| **`UPDATE_접수완료`** | **`PAT` → `WORK` → `SLOT`** | `05` §14 "업무ID" **+ §24.2 (RSV→RCP 인덱스 이동)** |
| `UPDATE_접수추가검사` | `WORK` | `05` §14 "업무ID" |
| `INSERT_자체휴무일` | `HOL` | `05` §14 "휴무일자" |
| `UPDATE_자체휴무일` | `HOL` | `05` §14 "휴무일자" |
| `DELETE_자체휴무일` | `HOL` | `05` §14 "휴무일자" |
| `UPDATE_접수취소` | `WORK` | `05` §14 "업무ID" |

`[I]` **`UPDATE_수검자정보`는 SSN/CHART를 조건 없이 항상 잡는다.** "변경 시에만"으로 두면 변경 여부를 알기 위해 현재 행을 먼저 읽어야 하고, 자연스러운 구현이 `PAT`(랭크 3)을 먼저 잡은 뒤 `SSN`(랭크 1)을 잡게 되어 **§23 전역 순서가 뒤집힌다.** 요청값 `@주민번호`·`@차트번호`는 입력이므로 사전조회 없이 자원명을 만들 수 있다. 잠금 2개 추가 비용은 무의미하다.

## 24.2 `SLOT` 잠금이 필요한 전이와 그렇지 않은 전이 `[I]` `[X 수정]`

`05` §14의 논리 잠금영역 표는 **하한**이다. 상태전이가 인덱스에서 어떻게 움직이는지에 따라 추가 직렬화가 필요하다.

```text
IX_예약접수_SLOT            Key(예약일, TimeSlotCode, StatusCode)
IX_..._PATIENT_STATE_DATE             Key(수검자ID, StatusCode, 예약일)
StatusCode 정렬:  'CNC' < 'CNR' < 'RCP' < 'RSV'
COUNT 술어:       StatusCode IN ('RSV','RCP')  → 'RCP' 구간을 먼저, 'RSV' 구간을 나중에 스캔
```

| 전이 | 인덱스 상 이동 | 스캔이 놓쳤을 때 | SLOT 잠금 |
|---|---|---|:---:|
| `RSV → CNR` (예약취소) | 집합 **밖**으로 (뒤→앞) | 취소될 행을 안 셈 = 커밋 후 정답 | 불필요 |
| `RCP → CNC` (접수취소) | 집합 **밖**으로 (앞→더 앞) | 이미 세었으면 과대집계 = 보수적 | 불필요 |
| **`RSV → RCP` (접수완료)** | **집합 안에서 뒤 → 앞** | **`'RCP'` 구간을 이미 지난 뒤 `'RSV'` ghost 를 스킵 → 과소집계** | **필요** |
| `RCP → RCP` (AEX 변경) | `최종수정일시`만 변경 = 두 NCI 키 불변 | 이동 없음 | 불필요 |

**과소집계의 결과** — READ COMMITTED에서 스캔이 `'RSV'` 구간의 X 잠금에 걸려 대기하다가 접수완료가 커밋되면, 옛 레코드는 ghost가 되고 새 레코드는 이미 지나온 `'RCP'` 구간에 삽입되어 **행이 통째로 누락**된다.

| 누락 대상 COUNT | 귀결 | 위반 |
|---|---|---|
| 시간대 정원 (`INSERT_예약`·`UPDATE_예약변경`) | 21건 저장 | `00` RP-03 |
| 다른 유효업무 (`INSERT_예약`) | 동일 수검자 유효업무 2건 | `00` RP-06 |
| 활성 Work 존재 (`UPDATE_수검자정보`) | 주민번호 변경 허용 | `00` EP-08 |

따라서 **`UPDATE_접수완료`는 `PAT`과 `SLOT`도 획득한다.** 전역 순서 3→4→5를 그대로 지키므로 §23의 교착 분석이 무너지지 않는다. `예약일`·`TimeSlotCode`는 접수완료가 바꾸지 않으므로 stale 사전조회로 자원명을 만들어도 안전하며, `WORK` 획득 후 재검증에서 불일치하면 `502`/`601`로 종료한다.

`[X]` **초안 오류**: 이 절은 원래 *"최악의 결과는 305 SlotFull 이고 무결성 위반이 아니다"*라고 주장했다. 그 분석은 **취소(집합 밖으로 나가는 안전한 전이)만 검토했고 접수완료(집합 안에서 이동)를 빠뜨렸다.** 오차 방향은 보수적(과대)이 아니라 **과소**였다.

`[X 수정 2차]` **다만 *"`RCP` 구간을 먼저, `RSV` 구간을 나중에 스캔한다"* 는 단정은 과장이다.** 실제 접근경로와 스캔 방향은 실행계획이 정한다 — Clustered/heap 경로, 역방향 스캔, 다른 seek 순서, row versioning, 더 강한 격리수준은 모두 반례다. 정확한 서술은 **"해당 NCI를 `StatusCode` 오름차순으로 사용하는 계획에서는 발생 가능하다"** 이다.

그럼에도 잠금 확장 결론은 유지한다. READ COMMITTED 인덱스 스캔 중 키가 이미 읽은 위치로 이동하면 행을 놓칠 수 있다는 것은 SQL Server의 문서화된 동작이고, 실행계획을 고정할 수단이 없는 이상 **"발생 가능"만으로 정원 초과·RP-06 위반·EP-08 우회를 허용할 수 없기 때문이다.** `CON-008` 은 production 실행계획에 기대지 않고 테스트 세션이 접근경로와 교차시점을 직접 통제한다(§38.4).

`[미검증]` 이 시나리오는 논리 분석으로 도출했고 2세션 실측은 하지 않았다. 재현 절차: 접수완료의 `UPDATE` 직후에 `WAITFOR DELAY '00:00:03'`을 넣은 임시 프로시저와, 그 사이에 정원 `COUNT`를 실행하는 두 번째 세션. `T34`에 `CON-008`로 편성한다.

## 24.3 `UPDLOCK` 힌트를 쓰지 않는 이유 `[I]`

- `applock`이 논리 자원 직렬화를 담당하므로 "읽고 → 검사 → 쓰기" 사이의 창이 이미 닫혀 있다.
- 상태·동시성 원자성은 **조건부 UPDATE + `@@ROWCOUNT`** 로 보장한다 (`04` §3.1-14 "ROWVERSION + 기대상태 조건 UPDATE").
- 범위잠금은 인덱스·통계·실행계획에 따라 실제 획득 락이 달라져 동시성 테스트의 재현성을 떨어뜨린다.

## 24.4 조건부 UPDATE 표준형

```sql
UPDATE [dbo].[예약접수]
   SET StatusCode = 'RCP', [최종수정일시] = @저장시각
 WHERE [업무ID]     = @업무ID
   AND StatusCode = 'RSV'          -- 기대상태
   AND [[행버전]] = @행버전; -- 낙관적 동시성

IF @@ROWCOUNT = 0 -- 상태 또는 [행버전] 불일치 → 재조회하여 502 / 601 판정
```

**우선순위**: 상태와 행버전이 모두 달라졌으면 `502 WrongStatus`가 우선이고 `601`은 반환하지 않는다 (`05` §5).

---

# 25. Lock timeout · deadlock 처리 `[I]`

## 25.1 `sp_getapplock` 반환코드

| rc | 의미 | 처리 |
|---:|---|---|
| `0` | 즉시 획득 | 진행 |
| `1` | **대기 후** 획득 | 진행 — **경합이 실제로 발생했다는 유일한 증거**이므로 로그에 남긴다 |
| `-1` | timeout (5000ms 초과) | `ROLLBACK` → `THROW 50001` |
| `-2` | 취소됨 | `ROLLBACK` → `THROW 50001` |
| `-3` | **deadlock victim** | `ROLLBACK` → **`THROW 50002`** |
| `-999` | Parameter 검증 / 호출 오류 | `ROLLBACK` → `THROW 50001` |

```sql
DECLARE @잠금결과 INT;
EXEC @잠금결과 = sp_getapplock @Resource = @Res, @LockMode = 'Exclusive',
                         @LockOwner = 'Transaction', @LockTimeout = 5000;

PRINT 'INFO applock [rc]=' + CONVERT(VARCHAR(4), @잠금결과);  -- 경합 증거. 자원명은 찍지 않는다

IF @잠금결과 = -3
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50002, N'잠금 교착이 발생했습니다. 다시 시도하십시오.', 1;
END
IF @잠금결과 < 0
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, N'잠금 획득에 실패했습니다. 잠시 후 다시 시도하십시오.', 1;
END
```

`[I]` **`PRINT 'INFO applock rc=…'` 는 필수다.** 이것이 없으면 동시성 시험이 "불변조건이 지켜졌다"만 확인하고 **"잠금이 실제로 동작했다"는 증거를 하나도 생산하지 못한다** — applock을 통째로 지워도 우연한 직렬 실행으로 7개 시나리오가 전부 PASS할 수 있다. `rc=1`(대기 후 획득)이 최소 1건 관측되어야 경합이 성립한 것으로 본다(§38.1).

`@Res`에 `SSN` 자원이 들어가도 hash이므로 주민번호가 로그에 노출되지 않는다.

## 25.2 ResultCode로 매핑하지 않는 이유

ResultCode Catalog는 **정확히 38개로 고정**(`05` §4.2)이며 잠금실패 코드가 없다. 인계 프롬프트 §5.9는 *"Catalog에 없는 실패를 억지로 기존 업무코드에 매핑하지 않는다"*고 못박았고, `05` §3.6은 *"예상하지 못한 SQL/시스템 오류 → THROW"*이다. 따라서 **`THROW`가 `05` 계약을 변경하지 않는 유일한 정합적 처리**다.

C#은 이를 `SqlException`으로 받아 재시도 안내를 표시한다. **Number `50001`(timeout)과 `50002`(deadlock victim) 두 가지가 모두 온다** — 둘 다 "잠시 후 다시 시도" 안내로 처리하되 로그에는 번호를 구분해 남긴다. 이는 Phase 5 UI 처리 사항이며 `05`의 `DbCode` Enum을 확장하지 않는다.

## 25.3 엔진 Deadlock(1205)

`applock` 전역 순서로 구조적으로 방지하지만, 만일 발생하면 `CATCH`가 `ROLLBACK` 후 **원본 1205를 그대로 `THROW`** 한다. 별도 Extended Events 세션이나 trace flag 1222를 켜지 않는다 — `sqlcmd -b`가 exit 1을 반환하고 로그에 1205가 남는 것이 증거다.

---

# 26. `수검자.행버전` 낙관적 동시성 `[B]` `[I]`

`[R7]` **이 절은 원래 `수검자.최종수정일시` 단조증가(`+4ms`) 규칙이었다.** 그 규칙은 사라졌다.

무엇이 있었고 왜 없앴는지는 남긴다 — 되돌리려는 다음 사람이 같은 곳에 다시 빠지지 않도록.

```text
있었던 것   수검자 동시성 토큰 = 최종수정일시 (DATETIME)
문제        DATETIME 은 약 3.33ms 틱이라 같은 눈금 안에서 두 번 수정하면 토큰이 변하지 않는다
회피        새 시각이 기존값 이하이면 기존값 + 4ms 로 밀어 단조 증가시켰다
원인        감사 정보(언제 바뀌었나)와 동시성 토큰(누가 먼저 바꿨나)이 한 컬럼에 겹쳐 있었다
R7          두 역할을 분리했다. 토큰은 행버전, 최종수정일시는 감사 정보다 (04 §1.2)
```

`+4ms` 는 **저장된 시각을 사실과 다르게 만드는** 보정이기도 했다. 3ms 뒤로 밀린 `최종수정일시`는
그 행이 실제로 수정된 시각이 아니다. 감사 정보로서는 거짓이었고, 그 거짓의 유일한 이유가 동시성이었다.

## 26.1 지금 규칙

```sql
UPDATE [dbo].[수검자]
   SET ..., [최종수정일시] = CONVERT(DATETIME, @서버시각)
 WHERE [수검자ID] = @수검자ID
   AND [행버전]   = @행버전;              -- 낙관적 동시성

IF @@ROWCOUNT = 0 → 601 RowChanged
```

- `행버전`은 `ROWVERSION` 이라 **UPDATE 마다 DB 가 반드시 바꾼다.** 같은 값이 두 번 나오지 않는다.
- 밀어 올릴 것이 없으므로 `@기존최종수정일시` Parameter 도, `DATEADD(MILLISECOND, 4, …)` 도 없다.
- `CK_수검자_EDIT_DATE (최종수정일시 >= 생성일시)` 는 서버시각이 단조라 그대로 만족한다.
- 성공 시 RS1 로 **UPDATE 이후 다시 읽은 새 `행버전`** 을 반환한다 (`05` §16.3). `예약접수`와 같은 절차다(§27).

## 26.2 세 테이블이 같은 절차를 쓴다

| 테이블 | 토큰 | 실패 코드 |
|---|---|---|
| `수검자` | `행버전` | `601` |
| `예약접수` | `행버전` | `601` |
| `휴무일` | `행버전` | `601` |

`600 PatientChanged` 는 폐지했고 재사용하지 않는다 (`05` §4.4).

---

# 27. Work 행버전 처리 `[B]`

- `예약접수.행버전`은 Work Master뿐 아니라 **Work Aggregate 전체**의 동시성값이다 (`04` §1.2.1).
- `AEX Detail`이 실제로 변경되면 **같은 Transaction에서 `예약접수.최종수정일시`도 UPDATE** 한다. 그 UPDATE로 `행버전`이 자동 변경된다.
- 성공 응답의 RS1은 **UPDATE 이후 다시 읽은 새 `행버전`** 을 반환한다.
- `최종수정일시`가 `DATETIME2(0)`이라 같은 초에 두 번 변경되면 값이 같을 수 있으나, Work 동시성 기준은 `행버전`이므로 문제되지 않는다.
- C#은 Detail별 동시성값을 관리하지 않고 Work의 `행버전` 하나만 전달한다.

---

# 28. No-op 처리 `[B]` `[I]`

| SP | No-op 조건 | 동작 | 반환 |
|---|---|---|---|
| `UPDATE_수검자정보` | 12개 입력이 현재값과 전부 동일 | 행을 갱신하지 않음 | RS0 `결과코드=1`, RS1에 **기존 `행버전`** |
| `UPDATE_예약변경` | `예약일변경여부=0` ∧ `시간대변경여부=0` ∧ `추가검사변경여부=0` | Work·Detail 모두 미갱신 | RS0 `결과코드=1`, RS1에 **기존 `행버전`** |
| `UPDATE_접수추가검사` | 요청 AEX 집합 = 현재 AEX 집합 | Detail DELETE/INSERT 없음, Work UPDATE 없음 | RS0 `결과코드=1`, RS1에 **기존 `행버전`** |

`[I]` **No-op도 Transaction과 applock 안에서 판정한다.** 판정을 위해 현재 행을 읽어야 하고, 그 사이 다른 세션이 값을 바꾸면 잘못된 No-op이 될 수 있기 때문이다. 존재·상태·동시성 확인이 원자적으로 보장된다.

`UPDATE_접수추가검사`의 No-op에서는 현재 Master의 비활성·성별·중복 Rule을 **재평가하지 않는다** (`05` §12.2).

`결과코드=1 NoChange`는 **Write SP의 No-op에만** 사용한다. `SELECT_예약가능정보`의 `변경범위=NONE`은 조회 SP의 정상 결과이므로 `결과코드=0`이다 (`05` §9.4).

## 28.1 AEX 집합 비교 구현

```sql
IF NOT EXISTS (SELECT ExamItemCode FROM @CurrentAex EXCEPT SELECT ExamItemCode FROM @RequestAex)
   AND NOT EXISTS (SELECT ExamItemCode FROM @RequestAex EXCEPT SELECT ExamItemCode FROM @CurrentAex)
    SET @추가검사변경여부 = 0;
ELSE
    SET @추가검사변경여부 = 1;
```

`EXCEPT` 양방향 검사로 집합 동일성을 판정한다. 비트마스크·문자열 연결·정렬 비교를 사용하지 않는다.

## 28.2 `UPDATE_수검자정보` No-op 비교는 NULL-safe 여야 한다 `[I]` `[X 수정]`

12개 입력 중 8개가 nullable(`휴대전화`·`전화번호`·`이메일`·`우편번호`·`주소`·`상세주소`·`비고`, 그리고 정규화로 NULL이 되는 빈 문자열)이다. `<>` 로 비교하면 `NULL <> 'x'` 가 `UNKNOWN` 이라 **변경으로 인식되지 않는다.**

```text
현재  비고 = NULL,  이메일 = 'a@b.c'
요청  비고 = N'당뇨 병력',  이메일 = NULL      ← 사용자가 비고 입력 + 이메일 삭제

WHERE 비고 <> @비고 OR 이메일 <> @이메일   →  둘 다 UNKNOWN  →  "변경 없음"
→ 결과코드=1 반환, 행 미갱신, 행버전 불변
→ C#은 성공으로 처리하고 화면을 닫는다.  사용자 편집이 조용히 소실된다.
```

`INTERSECT`는 `NULL = NULL`을 참으로 취급하므로 NULL-safe 하다.

```sql
IF EXISTS (
    SELECT p.[[차트번호]], p.[[성명]], p.[[주민번호]], p.[[생년월일]], p.[[성별]]
         , p.[CelNumber], p.[TelNumber], p.[EMail], p.[[우편번호]], p.[[주소]], p.[[상세주소]]
      FROM [dbo].[수검자] p WHERE p.[[수검자ID]] = @수검자ID
    INTERSECT
    SELECT @차트번호, @성명, @주민번호, @생년월일, @성별
         , @휴대전화, @전화번호, @이메일, @우편번호, @주소, @상세주소
)
AND EXISTS (   -- [비고] 는 NVARCHAR(MAX) 라 INTERSECT 비교 대상이 될 수 없다
    SELECT 1 FROM [dbo].[수검자] p
     WHERE p.[[수검자ID]] = @수검자ID
       AND ((p.[[비고]] IS NULL AND @비고 IS NULL) OR p.[[비고]] = @비고)
)
    SET @결과코드 = 1;   -- No-op
```

`[I]` `비고`가 `NVARCHAR(MAX)`라 `INTERSECT`/`EXCEPT` 피연산자가 될 수 없으므로 **명시적 NULL-safe 비교로 분리**한다. 이 예외를 반드시 구현 주석에 남긴다.

`[X]` **초안 누락**: §28은 AEX 집합에만 `EXCEPT` 양방향(NULL-safe)을 규정했고 수검자 12필드에는 비교 방법을 정하지 않았다. 구현자가 자연스럽게 `<>` 를 쓰면 위 시나리오로 데이터가 소실된다.

---

# 29. 예약변경 변경범위 처리 `[B]`

SP가 **C#이 전달한 변경구분을 신뢰하지 않고** DB 현재값과 요청값을 비교해 세 값을 계산한다 (`04` §2.2.1).

```text
예약일변경여부   = (현재 예약일 <> @예약일)
시간대변경여부   = (현재 TimeSlotCode    <> @시간대코드)
추가검사변경여부  = (현재 AEX 집합        <> 요청 AEX 집합)     -- §28.1
```

| 실제 변경 | 변경범위 | 일정·마감·정원·중복 | TGT | NEX | AEX | 저장 대상 |
|---|---|:---:|:---:|:---:|:---:|---|
| 예약일 변경 포함 | `ALL` | O | O | O | O | Work + NEX/AEX Detail 전량 재작성 |
| 시간대만 변경 | `SLOT` | O | X | X | **X** | Work만 |
| AEX만 변경 | `EXTRA` | X | X | X | O | AEX Detail + Work `최종수정일시` |
| 시간대 + AEX | `SLOT_EXTRA` | O | X | X | O | Work + AEX Detail |
| 변경 없음 | `NONE` | X | X | X | X | No-op |

- **시간대만 변경**에서는 현재 AEX 코드를 `추가검사변경여부` 집합 비교에만 읽고, TGT/NEX/AEX Rule을 재평가하거나 Detail을 재작성하지 않는다 (`00` §4.1, `04` §2.2.1, `05` §11.2).
- **예약일 변경**이 포함되면 기존 선택 AEX를 새 예약일 기준 NEX와 다시 검증한다. TGT 비대상 또는 AEX 충돌이면 **기존 예약을 전혀 변경하지 않는다.**

---

# 30. 현재 `업무ID` 제외처리 `[B]`

`05` §9.4 / `04` §2.2.1에 따라 다른 유효업무 조회조건을 고정한다.

```sql
-- 신규예약
WHERE [수검자ID] = @수검자ID
  AND [예약일] >= @오늘날짜
  AND StatusCode IN ('RSV','RCP')

-- 예약변경 (현재 Work 제외)
WHERE [수검자ID] = @수검자ID
  AND [예약일] >= @오늘날짜
  AND StatusCode IN ('RSV','RCP')
  AND [업무ID] <> @업무ID          -- 필수
```

| 다른 유효업무 건수 | 처리 |
|---:|---|
| 0건 | 충돌 없음 → 정상 진행 |
| 1건 | `306 OtherReservation` (`SELECT_예약가능정보`는 `다른업무ID` 반환) |
| **2건 이상** | `701 WorkDataError` — RP-06 불변조건이 이미 손상된 상태 |

2건 이상일 때는 **잠금 획득 성공 여부와 무관하게** `701`을 반환한다 (`05` §14).

## 30.1 `적용후인원` 계산 (`05` §9.7)

```text
신규예약                          적용후인원 = 현재인원 + 1
기존 Work가 같은 날짜·시간대 유지    적용후인원 = 현재인원
기존 Work가 다른 시간대로 이동       적용후인원 = 현재인원 + 1
잔여자리 = MAX(0, 20 - 적용후인원)
SlotFull  = (적용후인원 > 20)
```

현재 Work가 이미 20/20 시간대에 포함되어 **같은 시간대을 유지**하는 경우 `적용후인원 = 20`, `선택가능 = 1`이다. 이를 21로 계산하지 않도록 `예약접수` COUNT에서 현재 `업무ID`를 제외한 뒤 이동 여부에 따라 `+1`한다.

---

# 31. Read SP consistency 전략 `[I]`

| 항목 | 결정 | 근거 |
|---|---|---|
| Isolation | **기본 READ COMMITTED 유지.** `SET TRANSACTION ISOLATION LEVEL` 문을 쓰지 않는다 | 조회는 좌석이나 Write 권한을 확보하지 않으며 Write SP가 Transaction 안에서 전부 재검증한다 (`05` §9.13 말미) |
| 명시적 read transaction | **사용하지 않음** | 위와 동일 |
| Snapshot Isolation / RCSI | **켜지 않음.** Preflight가 `READ_COMMITTED_SNAPSHOT = OFF`를 확인 | 과제 범위 대비 과도한 DB option. `04`에 근거 없음 |
| temp table / table variable materialization | **사용하지 않음** | 다중 Result Set 사이의 미세한 Snapshot 차이는 조회 안내값에만 영향을 주고 저장 판정에는 영향이 없다 |
| applock | **Read SP는 획득하지 않음** | 조회가 Write를 막지 않아야 한다 |

`SELECT_예약가능정보`(RS0~RS5)와 `SELECT_예약접수상세`(RS0~RS4)는 한 호출 안에서 Work/Detail/Action이 서로 다른 시점을 볼 수 있다. 이 값들은 **사전안내**이며, 최종 권한은 Write SP에 있다는 기준(`04` §1.2, `03` 21장)을 유지한다. 본 한계는 §43에 기록한다.

---

# 32. Security — Role / User / Grant / Deny `[폐기]`

**폐기 (2026-09-07 사용자 결정)** — 계정·권한(`ROLE`·`USER`·`GRANT`·`DENY`)은 이 프로젝트의 산출물이 아니다. `NOT RUN` 으로 미루지 않고 범위에서 **뺀다.** `SEC-010`(secret 스캔)만 남는다 — 그것은 계정·권한이 아니라 배포 원본·로그에 자격증명이 섞이지 않았는지 보는 검사다.

아래 설계는 **실행하지 않는다.** 실무 이관 시의 참고 기록으로만 남긴다.

## 32.1 구성

```sql
CREATE ROLE [HC_APP_ROLE];

GRANT EXECUTE ON [dbo].[USP_HC_공통업무상태_조회]   TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_수검자목록_조회]     TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_수검자상세_조회]     TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_수검자_등록]         TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_수검자정보_수정]     TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_수검자유효업무_조회] TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_예약가능정보_조회]   TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_예약_등록]           TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_예약_변경]       TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_예약_취소]       TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_예약접수목록_조회]   TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_예약접수상세_조회]   TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_접수_완료]       TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_접수추가검사_변경]   TO [HC_APP_ROLE];
GRANT EXECUTE ON [dbo].[USP_HC_접수_취소]       TO [HC_APP_ROLE];

CREATE USER [HC_APP_TEST] WITHOUT LOGIN;
ALTER ROLE [HC_APP_ROLE] ADD MEMBER [HC_APP_TEST];
```

`[R3]` **위 `GRANT` 목록은 15건이라 `USP_HC_변경이력_조회`(SP-LOG-01)이 빠져 있다.** R3 재봉인으로
SP 가 20개가 되었으므로 구현한다면 20건이어야 한다 — `05` §1.3 과 §18 SP 구현 Matrix 를 참조한다.

`[사용자 결정 2026-09-07]` **Security(`T31`·`T32`)를 구현하지 않는다.** 과제 범위에서 권한 경계는
요구되지 않고 개발 속도만 늦춘다는 판단이다. `deploy/08_Security.sql` 은 **파일째 삭제했고**
배포 사슬은 `…07_Procedures_Reception_Write → 08_Verify` 로 번호를 당겼다 (2026-09-08). 그리고
`§45.2` 의 `SEC` 11건은 **범위 밖**이다 — `NOT RUN` 이며 `PASS` 로 승격하지 않는다.
Ownership chaining 전제(§32.2)와 Phase 5 연결 방법(§32.3)은 문서로만 남긴다.

## 32.2 결정 사항

| 항목 | 결정 | 근거 |
|---|---|---|
| Login·Password 생성 | **하지 않음** | 서버 수준 변경이며 인계 프롬프트 §2.2 금지. Phase 5 배포 시 사용자가 login을 만들어 `HC_APP_ROLE`에 넣는 방법만 문서화 |
| Table 권한 | **부여하지 않음** (SELECT 포함) | `04` §1.3 |
| TVF 권한 | **부여하지 않음** | `05` §1.4 "C# 직접 호출 금지" |
| Sequence 권한 | **부여하지 않음** | SP 내부 전용 |
| GRANT 범위 | **15개 개별**, `SCHEMA::dbo` 일괄 아님 | 명시성. 일괄 부여는 향후 추가 객체에 자동 권한을 준다 |
| `DENY` | **사용하지 않음** | 아무 권한도 주지 않으면 기본이 거부다. 불필요한 명시는 복잡도만 늘린다. 보안 테스트가 실제 거부를 확인하므로 회귀는 잡힌다 |
| Ownership chaining | **기본 소유권 체인 사용** | `dbo`가 SP와 테이블을 모두 소유하므로 SP 실행 시 테이블 권한이 필요 없다. **`TRUSTWORTHY ON`·`EXECUTE AS OWNER`가 불필요**하며 이는 §2.3 금지 항목을 자연스럽게 회피한다 |
| Secret | **없음** | 통합인증만 사용. 연결문자열에 비밀번호가 없어 Repository 저장 대상이 0건이다 |

## 32.3 Phase 5 연결 방법 (문서화만)

```sql
-- Phase 5 배포 시 사용자가 수행 (Phase 4에서 실행하지 않음)
CREATE USER [DOMAIN\AppServiceAccount] FOR LOGIN [DOMAIN\AppServiceAccount];
ALTER ROLE [HC_APP_ROLE] ADD MEMBER [DOMAIN\AppServiceAccount];
```

---

# 33. Test Architecture

## 33.1 계층

```text
tests/00_Test_Harness.sql     Fixture 직접 INSERT (§15.2) + 손상 데이터 구획
tests/01_Schema_Tests.sql     객체 인벤토리·제약 수·금지 객체 0건
tests/02_Seed_Tests.sql       Exam 19행 / Holiday 40행 / 주민번호 체크디지트 무효
tests/03_Rule_Tests.sql       TVF 4종 결정적 경계 (@서버시각 주입)
tests/04_Select_SP_Tests.sql  SELECT SP 8개 계약
tests/05~07_*_Write_Tests.sql Write SP 8개 계약
tests/08_Rollback_Tests.sql   부분저장 0건
tests/09~12_Concurrency_*.sql 2세션 경합
tests/13_Security_Tests.sql   EXECUTE AS USER 권한 경계
tests/14_Clean_Rebuild_Verify.sql  재구축 후 동일 인벤토리
tools/verify-contract.js      후속 Result Set 형상 검증
```

## 33.1a `INSERT … EXEC` 사용 금지 `[I]` `[X 수정]`

**테스트에서 `INSERT INTO @t EXEC <SP>` 를 쓰지 않는다.** 실측 결과:

| 실측 | 결과 |
|---|---|
| RS가 2개 이상인 SP를 `INSERT…EXEC` | **`Msg 213`** — 모든 Result Set을 대상 테이블에 넣으려 한다 |
| SP 내부 `ROLLBACK` | **`Msg 3915`** — INSERT-EXEC 문 내에서는 ROLLBACK 불가 |
| 진입 시 `@@TRANCOUNT` | **1** (평범한 `EXEC` 는 0) — **C# 호출과 다른 경로를 시험하게 된다** |

19개 SP 가 RS 를 2개 이상 반환하므로(`DELETE_자체휴무일` 만 RS0 하나다) 이 패턴은 **구조적으로 성립하지 않는다.** 초안은 *"`INSERT … EXEC` 는 첫 번째 Result Set만 받는다"* 라는 잘못된 전제 위에 약 50곳의 단언을 세웠다.

### 대체 구조 — 역할을 둘로 나눈다

```text
tests/contract/<NN>_<시나리오>.sql     SP 를 EXEC 로 한 번 호출하기만 한다
        ↓ sqlcmd -u -W -w 65535 -s"|" -o
tools/verify-contract.js               RS 개수·컬럼명·순서·행수 + RS0 의 성공여부/결과코드 판정
        ↓
tests/<NN>_*.sql                       DB 상태 불변조건만 단언 (행수·StatusCode·행버전·Detail 집합)
```

`tools/expected-contracts.json`에 시나리오별 `"rs0Success"`, `"rs0Code"` 필드를 추가한다. 이렇게 하면

- SP가 **`@@TRANCOUNT=0`에서 실행**되어 C# 호출과 동일한 경로를 시험한다.
- `ROLLBACK`을 그대로 유지할 수 있다(§21.1 변경 불필요).
- ResultCode 판정과 Result Set 형상 판정이 **한 도구에 통합**된다.
- 테스트 SQL이 짧아진다.

## 33.2 Assertion 패턴 `[I]`

```sql
DECLARE @Fail INT = 0;

IF (조건) PRINT 'PASS T-001 설명';
ELSE BEGIN PRINT 'FAIL T-001 설명'; SET @Fail += 1; END

-- … 반복 …

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
```

파일 끝에서 한 번만 `THROW` 한다. 첫 실패에서 즉시 중단하면 나머지 결과가 보이지 않고, 파일 단위 집계는 `sqlcmd -b`의 exit code(실측 확인)도 정확히 유지한다.

**기대 실패**를 검증하는 항목(권한 거부 등)은 `BEGIN TRY … END TRY BEGIN CATCH … END CATCH`로 감싸 기대 오류면 `PASS`로 계산한다.

## 33.2a 업무시간 분기 — 모든 Write 테스트 파일 공통 `[I]` `[X 수정 2차]`

Write SP는 검증순서에서 `308`/`309`(공통 업무 가능)를 **업무 Rule보다 먼저** 판정한다. 따라서 업무시간 밖에 실행하면 `300`·`305`·`411`·`0` 등을 기대하는 단언이 **전부 `308`/`309`를 받아 FAIL**한다. 초안은 `CWR-006` 한 곳에만 가드를 두고 나머지 Task의 기대 PASS 수를 무조건값으로 적었다.

`[X 2차]` **초안은 창 밖에서 파일을 통째로 `RETURN` 했다.** 그러면 밤·주말 회귀가 Write SP 에
대해 아무것도 증명하지 못하고, `308`·`309` 는 **영영 관측되지 않는 ResultCode** 로 남는다
(실측: Catalog 38개 중 미관측 7개에 둘 다 들어 있었다). `SKIP` 은 `PASS` 가 아니다(`database/AGENTS.md` §10)
— 그렇다고 판정할 수 있는 것을 안 하고 넘기라는 뜻도 아니다.

**창 밖에서도 판정할 것이 있다.** Write SP 가 `308`/`309` 를 내고 **아무것도 바꾸지 않는다** 는 것은
계약이다(`05` §5 우선순위 9번). `RETURN` 대신 분기한다.

```text
창 안   성공 경로와 업무 Rule 을 전부 판정한다 (지금까지와 동일)
창 밖   창 안이면 성공했을 인자로 Write SP 를 호출하고
        DB 지문(행수 · 상태코드 분포 · 최종수정일시 합)이 **불변** 임을 판정한다
        + @@TRANCOUNT = 0
        RS0 결과코드 는 이 파일에서 받을 수 없으므로(INSERT … EXEC 금지) 아래 계약 시나리오가 판정한다
```

`tests/contract/OFF-*` 가 결과코드 를 판정한다. `PWR`/`RWR`/`CWR` 과 **정확히 배타적**이라
어느 시각에 돌려도 Write SP 의 업무시간 계약이 한쪽에서 반드시 판정된다.

```text
BIZ=1                 PWR·RWR·CWR 실행        OFF-309-* SKIP
BIZ=0 · 업무일         OFF-309-* 실행 (309)    PWR·RWR·CWR SKIP
OFF-308-*             **언제나 실행**          아래 참조
```

`[X 실측]` **`OFF-308` 은 한 번도 판정된 적이 없었다.** 일요일이나 활성 휴무일을 기다리는
구성이라 평일 회차마다 `SKIP` 이었고, `SKIP` 은 `PASS` 가 아니다(database/AGENTS.md §10). 실제로 이
시험은 작성 이후 단 한 번도 실행되지 않았다.

**휴무일은 데이터다.** 시험이 오늘을 활성 휴무일로 직접 심고 지우면 SP·TVF 를 한 글자도
고치지 않고 `308` 경로가 결정적으로 성립한다. `오늘업무일 = 0` 은 `운영시간내여부` 보다 앞에서
판정되므로(`UFN_HC_일정확인` 의 `업무가능코드 CASE`) 하루 중 어느 시각에 돌려도 된다.

```text
심은 행은 같은 파일에서 지운다. 남으면 뒤따르는 PWR/RWR/CWR 계약이 전부 308 이 된다.
verify-contract-all.sh 가 루프 뒤에 한 번 더 지우고 '잔여 0건 / 휴무일 2건' 을 판정한다.
```

`[실측 2026-09-07 19:36]` `PASS OFF-308-01 … RS0 결과코드=308` · `PASS OFF-308-CLEAN 잔여 0건 · Seed 2건`.

`tests/05`·`06`·`07`·`08` 머리에 다음을 둔다.

```sql
DECLARE @NowT TIME(7) = CONVERT(TIME(7), SYSDATETIME());
DECLARE @Dow  INT     = DATEDIFF(DAY, 0, CONVERT(DATE, SYSDATETIME())) % 7;
DECLARE @Biz  BIT     = CASE WHEN @Dow <> 6
                              AND @NowT >= CONVERT(TIME(7),'09:00:00')
                              AND @NowT <  CONVERT(TIME(7),'18:00:00')
                              AND NOT EXISTS (SELECT 1 FROM [dbo].[휴무일]
                                               WHERE [휴무일자] = CONVERT(DATE, SYSDATETIME())
                                                 AND [사용여부] = 1)
                             THEN 1 ELSE 0 END;
PRINT 'INFO 실행시각 ' + CONVERT(VARCHAR(30), SYSDATETIME(), 121) + ' 업무가능=' + CONVERT(VARCHAR(1), @Biz);

IF @Biz = 0
BEGIN
    -- 지문을 재고 → 창 안이면 성공했을 인자로 호출 → 다시 재서 같은지 본다
    -- 그리고 NOT RUN <창 안 전용 ID 대역> 을 남긴다. SKIP 을 PASS 로 승격하지 않는다.
    RETURN;
END
```

**완료조건 표기는 "PASS n건"이 아니라 "업무시간 내 실행 시 PASS n건 / 밖이면 창 밖 분기 PASS 1건 + `NOT RUN` 1건 + FAIL 0건"으로 기술한다.** `T37`은 최종 Gate를 선언하기 전에 **업무시간 내 실행 로그가 존재하는지** 확인하고, 없으면 해당 Gate를 `NOT RUN`으로 남긴다 — `SKIP`을 `PASS`로 승격하지 않는다.

## 33.3 Test Matrix 형식 (인계문서 §9.6)

각 테스트는 다음 열을 갖는다. 전체 목록은 구현계획 문서에서 Task별로 전개한다.

| Test ID | 기준문서 | 사전조건 | Session | 실행 | 기대 결과코드/행수 | DB 불변조건 | Cleanup | Evidence |
|---|---|---|---|---|---|---|---|---|

## 33.4 `05` §17 적대적 테스트 추적 — 누락 보완 `[X 수정]`

초안은 §34~§40이 `05` §17을 "전건 매핑"한다고 주장했으나 아래 시나리오에 대응 Test ID가 없었다. **Test ID가 없으면 구현 누락도 Gate를 통과한다.**

| `05` §17 항목 | 신설 Test ID | 배치 |
|---|---|---|
| §17.6 주민번호 **14자리** 입력 | `PWR-013` | `tests/05` |
| §17.6 주민번호 **비숫자 포함** 입력 | `PWR-014` | `tests/05` |
| §17.6 주민번호 변경 + **`RCP` 존재** → `205` | `PWR-027` | `tests/05` |
| §17.6 차트번호 변경 + 활성 Work 존재 → 차단 **안 함** | `PWR-028` | `tests/05` |
| §17.7 예약일 변경 후 기존 AEX 가 **새 NEX 와 충돌** (`OPT04` vs `EX012`) | `RWR-031` | `tests/06` |
| §17.7 예약변경 시 타 유효업무 **1건 → 306** | `RWR-032` | `tests/06` |
| §17.7 예약변경 시 타 유효업무 **2건 → 701** | `RWR-033` | `tests/06` |
| §17.8 **과거** RSV 접수 → `503` | `CWR-008` | `tests/07` |
| §17.8 접수마감 **경계** (10:59:59 / 11:00:00) | `CWR-009` | `tests/07` |
| §17.8 `CNR`·`CNC` 접수 시도 → `502` | `CWR-010` | `tests/07` |
| `04` §14 ExamSource ↔ Master 역할 불일치 → `701` | `CWR-011` | `tests/07` (CORRUPT-4) |
| `05` §17.4 NEX 12행 손상 → `701` | `RWR-034` | `tests/06` (CORRUPT-5) |

`04` §14 의 마지막 불변조건(*"`변경이력`이 Write SP마다 정확히 1행"*)은 선언적으로 보장되지 않으므로 Write SP 8개마다 전용 Test 를 둔다. 기존 Prefix 의 **새 대역**을 쓴다 — 번호 밀림이 없다.

| 검증 | 신설 Test ID | 배치 |
|---|---|---|
| `INSERT_수검자` 성공·업무실패 각각 `변경이력` 정확히 1행 (`PAT_INSERT`) | `PWR-030` | `tests/05` |
| `UPDATE_수검자정보` 성공·업무실패 각각 `변경이력` 정확히 1행 (`PAT_UPDATE`) | `PWR-031` | `tests/05` |
| `INSERT_예약` 성공·업무실패 각각 `변경이력` 정확히 1행 (`RSV_INSERT`) | `RWR-050` | `tests/06` |
| `UPDATE_예약변경` 성공·업무실패 각각 `변경이력` 정확히 1행 (`RSV_UPDATE`) | `RWR-051` | `tests/06` |
| `UPDATE_예약취소` 성공·업무실패 각각 `변경이력` 정확히 1행 (`RSV_CANCEL`) | `RWR-052` | `tests/06` |
| `UPDATE_접수완료` 성공·업무실패 각각 `변경이력` 정확히 1행 (`RCP_ACCEPT`) | `CWR-050` | `tests/07` |
| `UPDATE_접수추가검사` 성공·업무실패 각각 `변경이력` 정확히 1행 (`RCP_AEX`) | `CWR-051` | `tests/07` |
| `UPDATE_접수취소` 성공·업무실패 각각 `변경이력` 정확히 1행 (`RCP_CANCEL`) | `CWR-052` | `tests/07` |

`[I]` 각 Test 는 `대상테이블`·`OperationCode`·`ResultCode` 값까지 단언한다. 행 수만 세면 SP 가 잘못된 업무코드를 써도 통과한다.

`[I]` **applock 실패와 예상하지 못한 오류는 기대 행수가 0이다**(`04` §14 L2·L3). `CON` 계열이 경합을 만들 때 `변경이력` 행수를 성공 건수와 같게 기대하면 안 된다.

`[I]` 시간 경계(`CWR-009`)는 `UFN_HC_일정확인` 의 `@서버시각` 주입으로 **결정적으로** 시험한다. Write SP 경로는 실제 시각에 의존하므로 §33.2a SKIP 가드를 따른다.

`[X 실측]` **`CWR-006`(성공)과 `CWR-009`(마감경과)는 배타적이 아니다.** 배타성은 Rule 이 아니라
**둘 다 PM Work 를 쓴 선택**의 결과였다. 마감은 시간대 마다 다르다(AM 11:00 · PM 16:00).
TVF 에 시각을 주입해 실측했다.

```text
13:00 · AM Work   마감시각 11:00   마감경과여부 1   -> 304
13:00 · PM Work   마감시각 16:00   마감경과여부 0   -> 성공
```

`CWR-009` 를 **AM Work** 로 옮기면 `11:10~15:50` 에 둘 다 판정된다. 이전 구성에서 `CWR-009` 는
`16:00~18:00` 에만 돌아 **일반 회귀가 한 번도 닿지 못했다.** 가드도 시간대 별로 나눈다 —
`CUTPM`(15:50 전) · `CUTAM`(11:10 이후).

---

# 34. Schema Test (`tests/01_Schema_Tests.sql`)

| Test ID | 검증 | 기대 |
|---|---|---|
| `SCH-001` | `sys.tables` 사용자 테이블 수 | **7** |
| `SCH-002` | 테이블 이름 집합이 §11 목록과 정확히 일치 | 차집합 0 |
| `SCH-003` | `수검자` 컬럼 수 | **17** |
| `SCH-004` | Primary Key 수 | **7** |
| `SCH-005` | Foreign Key 수, 전부 `NO ACTION` | **4** / delete·update referential_action = 0 |
| `SCH-006` | 일반 Unique Constraint 수 | **2** |
| `SCH-007` | Filtered Unique Index 수 (`has_filter=1 AND is_unique=1`) | **1** |
| `SCH-008` | 업무/조회 Nonclustered Index 수 (PK/UQ/UX 제외) | **5** |
| `SCH-009` | Sequence 수 및 `MAXVALUE` | **1** / `999999` |
| `SCH-010` | Trigger 수 | **0** |
| `SCH-011` | 사용자 정의 Table Type 수 | **0** |
| `SCH-012` | `USP_HC_DELETE_%` 객체 수 | **0** |
| `SCH-013` | Inline TVF 수 (`type='IF'`) | **4** |
| `SCH-014` | Stored Procedure 수 (`USP_HC_%`) | **15** |
| `SCH-015` | **53개 컬럼 전부**의 `(테이블, 컬럼, 타입, 길이, NULL 허용)` 을 `04` §8 기대 `VALUES` 와 **`EXCEPT` 양방향** 대조 | 차집합 0 |
| `SCH-016` | **제약 이름 26종** 을 `04` §8 기대 `VALUES` 와 **`EXCEPT` 양방향** 대조 | 차집합 0 |
| `SCH-017` | Default 제약 이름 10개 `EXCEPT` 양방향 | 차집합 0 |
| `SCH-018` | 4개 Nonclustered Index 이름 + Key 컬럼 순서 `EXCEPT` 양방향 | 차집합 0 |

`[X]` **초안 오류**: `SCH-015`는 55개 컬럼 중 **2개**(`주민번호`, `행버전`)만 `EXISTS`로 확인했고, `SCH-016`은 `COUNT(*) >= 20` 이었다. 제약 하나가 사라져도 PASS하고 제약 **이름**은 대조하지 않았다. `04` §13 말미가 *"정확한 타입 길이·NULL·제약명은 8장 정의를 기준으로 한다"*고 못박았으므로 개수 비교를 "전건 일치" 증거로 쓸 수 없다. 모든 인벤토리 검증을 **`EXCEPT` 양방향**으로 통일한다.

`[I]` `SCH-007`(Filtered Unique Index)의 `sys.indexes` 조회에 `JOIN sys.tables t ON t.object_id = i.object_id AND t.is_ms_shipped = 0` 을 반드시 건다. 시스템 개체의 필터 인덱스가 섞이면 오탐한다.

---

# 35. Rule Test (`tests/03_Rule_Tests.sql`)

`@서버시각`을 주입하므로 **실제 시각과 무관하게 결정적**이다.

## 35.1 `UFN_HC_일정확인` — 시간 경계 (`05` §17.1)

| Test ID | `@서버시각` | 기대 |
|---|---|---|
| `RUL-T01` | `08:59:59.9999999` | `현재업무가능=0`, `업무가능코드=309` |
| `RUL-T02` | `09:00:00.0000000` | `현재업무가능=1` |
| `RUL-T03` | `17:59:59.9999999` | `현재업무가능=1` |
| `RUL-T04` | `18:00:00.0000000` | `현재업무가능=0`, `업무가능코드=309` |
| `RUL-T05` | `09:59:59` + `NORMAL`/`AM` | `마감경과여부=0` |
| `RUL-T06` | `10:00:00` + `NORMAL`/`AM` | `마감경과여부=1`, `사유코드=304` |
| `RUL-T07` | `10:59:59` + `RECEPTION`/`AM` | `마감경과여부=0` |
| `RUL-T08` | `11:00:00` + `RECEPTION`/`AM` | `사유코드=304` |
| `RUL-T09` | `14:59:59` + `NORMAL`/`PM` | `마감경과여부=0` |
| `RUL-T10` | `15:00:00` + `NORMAL`/`PM` | `사유코드=304` |
| `RUL-T11` | `15:59:59` + `RECEPTION`/`PM` | `마감경과여부=0` |
| `RUL-T12` | `16:00:00` + `RECEPTION`/`PM` | `사유코드=304` |

## 35.2 `UFN_HC_일정확인` — 일정 (`05` §17.2)

| Test ID | 요청일 | 기대 `사유코드` |
|---|---|---|
| `RUL-D01` | 과거 평일 | `300` |
| `RUL-D02` | 과거 일요일 | `300` (과거가 우선) |
| `RUL-D03` | 미래 일요일 | `301` |
| `RUL-D04` | `2026-12-25` (활성 평일 휴무일) | `302` |
| `RUL-D05` | `2026-12-26` (활성 토요일 휴무일) | `302` |
| `RUL-D06` | 미래 토요일 + `AM` | `0` |
| `RUL-D07` | 미래 토요일 + `PM` | `303` |
| `RUL-D08` | 미래 정상 평일 | `0` |
| `RUL-D09` | `SET DATEFIRST 1` / `7` 양쪽에서 `RUL-D03` 재실행 | 동일 결과 |

## 35.3 `UFN_HC_검진대상확인` (`05` §17.3)

| Test ID | 조건 (기준 `2026-10-01`) | 기대 |
|---|---|---|
| `RUL-G01` | 만 19세 | `검진대상여부=0`, `400` |
| `RUL-G02` | 만 20세 | `검진대상여부=1`, `0` |
| `RUL-G03` | 완료이력 없음 | `검진대상여부=1`, `최근완료일자=NULL` |
| `RUL-G04` | 최근 완료 `2025-05-01` (1년) | `검진대상여부=0`, `401` |
| `RUL-G05` | 최근 완료 `2024-05-01` (2년) | `검진대상여부=1` |
| `RUL-G06` | 완료일 = 예약일 당일 | 완료이력으로 사용하지 않음 |
| `RUL-G07` | 완료일 > 예약일 | 완료이력으로 사용하지 않음 |
| `RUL-G08` | 존재하지 않는 `수검자ID` | **0행** |

## 35.4 `UFN_HC_국가검사구성` (`05` §17.4)

| Test ID | 조건 | 기대 |
|---|---|---|
| `RUL-N01` | TGT 비대상 | **0행** |
| `RUL-N02` | TGT 대상, 조건부 0종 | **8행** |
| `RUL-N03` | 남 만 23 / 24 / 28세 | `EX009` 없음 / 있음 / 있음 |
| `RUL-N04` | 여 만 39 / 40 / 44세 | `EX009` 없음 / 있음 / 있음 |
| `RUL-N05` | 만 40세, `B형간염제외여부` 0 / 1 | `EX010` 있음 / 없음 |
| `RUL-N06` | 만 55 / 56세 | `EX011` 없음 / 있음 |
| `RUL-N07` | 여 만 54 / 60 / 66세 | `EX012` 있음 |
| `RUL-N08` | 남 만 54세 | `EX012` 없음 |
| `RUL-N09` | 만 56 / 66세 | `EX013` 있음 |
| `RUL-N10` | 여 만 56세 (NEX-02·04·06 동시) | **11행** |
| `RUL-N11` | 모든 TGT 대상 프로필 | 행수가 항상 **8~11** 범위 |
| `RUL-N12` | 정렬 | `검사항목코드 ASC` |

## 35.5 `UFN_HC_추가검사확인` (`05` §17.5)

| Test ID | 조건 | 기대 |
|---|---|---|
| `RUL-A01` | 정상 Master | **정확히 7행**, `추가검사코드 ASC` |
| `RUL-A02` | 전부 미선택 | `유효선택여부` 전부 0, `요청선택여부` 전부 0 |
| `RUL-A03` | 남성 + `OPT03`(유방초음파) 요청 | `선택가능=0`, `유효선택여부=0`, `411` |
| `RUL-A04` | 여성 + `OPT05`(PSA) 요청 | `411` |
| `RUL-A05` | 남성 + `OPT07`(HPV) 요청 | `411` |
| `RUL-A06` | `추가검사사용여부=0`인 항목 요청 | `410` |
| `RUL-A07` | NEX에 `EX012` 있고 `OPT04` 요청 | `412` |
| `RUL-A08` | TGT 비대상 (`@저장검사사용여부=0`) | 7행 전부 `선택가능=0`, `유효선택여부=0`, `400`/`401` |
| `RUL-A09` | `@저장검사사용여부=1` + 저장 NEX 기준 | 저장 NEX와만 중복 판정 |
| `RUL-A10` | 선택하지 않은 무효 항목 | 사유 표시하되 저장 차단하지 않음 |

---

# 36. Stored Procedure Contract Test

## 36.1 Parameter 검증 (SQL만) — `EXCEPT` 양방향 `[X 수정]`

`05` §7~§12의 Parameter 113개를 `(SpName, ParamOrdinal, ParamName, TypeName, IsNullable)` 기대 `VALUES` 인라인 테이블로 두고 `sys.parameters` + `sys.types` 실측과 **`EXCEPT` 양방향** 대조한다. 차집합이 한 건이라도 있으면 `FAIL`.

`[X]` 초안은 메타데이터를 `SELECT`만 하고 사람이 눈으로 보라고 했다. 자동 판정이 없으면 회귀에서 잡히지 않는다.

| SP별 Parameter 수 | 값 |
|---|---:|
| `SELECT_공통업무상태` / `SELECT_수검자상세` / `SELECT_수검자유효업무` / `SELECT_예약접수상세` | 0 / 1 / 1 / 1 |
| `SELECT_수검자목록` / `SELECT_예약접수목록` | 5 / 5 |
| `INSERT_수검자` / `UPDATE_수검자정보` | 12 / 12 |
| `SELECT_예약가능정보` | 13 |
| `INSERT_예약` / `UPDATE_예약변경` | 11 / 11 |
| `UPDATE_예약취소` / `UPDATE_접수완료` / `UPDATE_접수취소` | 2 / 2 / 2 |
| `UPDATE_접수추가검사` | 9 |
| **합계** | **87** |

## 36.2 RS0 검증 (SQL만) — NULL 안전 `[X 수정]`

```sql
DECLARE @Rs0 TABLE (SpName SYSNAME, Ordinal INT, ColName SYSNAME NULL,
                    TypeName NVARCHAR(256) NULL, ErrNo INT NULL);

INSERT INTO @Rs0
SELECT p.name, r.column_ordinal, r.name, r.system_type_name, r.error_number
FROM sys.procedures p
OUTER APPLY sys.dm_exec_describe_first_result_set_for_object(p.object_id, NULL) r
WHERE p.name LIKE 'USP[_]HC[_]%';

-- (1) 총 행수가 정확히 80(16 SP × 5컬럼)인가
-- (2) error_number 가 NOT NULL 인 행이 0건인가   ← DMV 가 결과셋을 결정하지 못한 SP
-- (3) 기대 5컬럼 집합과 EXCEPT 양방향 차집합이 0인가
```

`[X]` **초안 오류**: `CROSS APPLY` + `WHERE NOT (…)` 구조였다. DMV가 결과셋을 결정하지 못하면 `name`/`column_ordinal`이 `NULL`인 error 행을 돌려주는데, `NULL` 비교가 `UNKNOWN`이 되어 `NOT UNKNOWN = UNKNOWN` → `COUNT(*)`에 안 잡힌다. `CROSS APPLY`라 0행을 내는 SP는 아예 사라진다. **SP가 14개여도, RS0이 완전히 깨져 있어도 `@Bad = 0` → PASS** 하는 거짓 양성이었다. `OUTER APPLY` + 총 행수 75 단언 + `error_number` 검사로 교체한다.

## 36.3 후속 Result Set 검증 (`tools/verify-contract.js`) `[D4-009]`

```text
1. tests/contract/NN_<시나리오>.sql 이 SP 를 EXEC 로 한 번 호출한다  (INSERT..EXEC 금지, §33.1a)
2. sqlcmd -u -W -w 65535 -s"|" -o artifacts/logs/rs_<NN>.txt        ← -w 필수
3. node tools/verify-contract.js 가 대시 구분선을 마커로 RS 경계를 찾아
   [RS 개수, 각 RS 의 컬럼명 순서, 각 RS 의 행수, RS0 의 성공여부/결과코드] 를 추출
4. tools/expected-contracts.json 과 대조. 불일치 시 stdout·stderr 양쪽에 FAIL 을 쓰고 exit 1
```

`[X]` **`-w 65535` 가 없으면 파서가 무너진다.** sqlcmd 기본 폭은 **80**이고 RS0 한 행만 해도 `bit + int + nvarchar(300) + varchar(50) + datetime2(7)` ≈ 400칸이라 헤더·구분선·데이터가 전부 접힌다(실측 확인). 접힌 조각이 컬럼명으로 읽히고 행수가 부풀려진다. `-W`(후행 공백 제거)도 함께 건다.

`[X]` **FAIL 을 `console.error`(stderr)로만 내면 안 된다.** 초안의 `for … done | tee file` 은 stdout만 잡으므로 **증거 파일에 구조적으로 PASS만 기록**된다. FAIL도 stdout에 쓰거나 `>> file 2>&1` 로 누적한다. 그리고 파이프라인 좌변은 서브셸이라 `|| exit 1` 이 스크립트를 끝내지 못하므로 **파이프를 쓰지 않는다.**

의존성 0(node 표준 `fs`만 사용). npm 설치를 하지 않는다.

**필수 검증 항목** (`05` §17.9):

| # | 검증 |
|---:|---|
| 1 | 모든 SP의 RS0가 정확히 1행 |
| 2 | 실패 시 불필요한 후속 Result Set이 출력되지 않음 |
| 3 | `INSERT_수검자` `202`/`203`에서만 RS1이 동반 출력 |
| 4 | `SELECT_예약접수상세` RS 개수 = 5, RS4 = 정확히 5행 |
| 5 | `SELECT_예약가능정보` RS 개수 = 6 |
| 6 | `SELECT_예약가능정보` 변경범위별 Cardinality (아래) |
| 7 | `SELECT_수검자유효업무` 0행/1행 정상, 2행이면 `701` |
| 8 | 검색 0건이 실패로 오인되지 않음 |

`SELECT_예약가능정보` 변경범위별 기대 Cardinality (`05` §9.11):

| 변경범위 | RS2 | RS3 | RS4 | RS5 |
|---|---:|---:|---:|---:|
| `ALL` | 2 | 0 또는 1 | 0 또는 8~11 | 0 또는 7 |
| `SLOT` | 2 | 0 | 0 | 0 |
| `EXTRA` | 0 | 0 | 0 | 7 |
| `SLOT_EXTRA` | 2 | 0 | 0 | 7 |
| `NONE` | 0 | 0 | 0 | 0 |

## 36.4 SP별 허용 ResultCode 검증 — 실제 Task 로 편성 `[X 수정]`

`expected-contracts.json` 의 각 시나리오에 `"sp"`, `"rs0Success"`, `"rs0Code"` 를 두고, `verify-contract.js` 가 다음 두 가지를 함께 판정한다.

```text
(1) 관측 RS0.결과코드 가 기대값과 일치하는가
(2) 관측 RS0.결과코드 가 05 §13 의 해당 SP 허용 집합 안에 있는가
```

`05` §13 허용 집합은 `tools/allowed-codes.json` 에 SP별 배열로 둔다. `05` §18이 *"사용되지 않는 ResultCode 0개"*를 PASS로 확정했으므로, **Catalog 38개 중 한 번도 관측되지 않은 코드 목록**도 함께 보고한다.

`[X]` 초안은 이 검증을 스펙에만 적고 계획에 대응 Task를 만들지 않아 실행되지 않았다.

## 36.5 Write SP 시나리오 커버리지 `[X 수정]`

Write SP도 **성공 경로와 실패 경로를 각각** 호출한다. 실패 경로는 RS0 1개만 나와야 한다(`INSERT_수검자` `202`/`203` 제외).

`[X]` 초안의 시나리오 목록에는 `UPDATE_예약취소`·`UPDATE_접수취소`가 없어 **15개가 아니라 13개 SP만** 검증됐다. 15/15로 채운다.

---

# 37. Rollback Test (`tests/08_Rollback_Tests.sql`)

| Test ID | 시나리오 | 기대 |
|---|---|---|
| `RBK-001` | `INSERT_예약`이 AEX 검증 단계(`412`)에서 실패 | `예약접수` 신규 행 **0건**, `검사항목` 신규 행 **0건** |
| `RBK-002` | `UPDATE_예약변경`이 예약일 변경 후 TGT 비대상(`400`)으로 실패 | Work의 `예약일`·`TimeSlotCode`·`행버전` 및 Detail 전량이 **변경 전과 동일** |
| `RBK-003` | `UPDATE_예약변경`이 정원 마감(`305`)으로 실패 | 동일 |
| `RBK-004` | `UPDATE_접수추가검사`가 성별 위반(`411`)으로 실패 | AEX Detail 및 Work `행버전` **불변** |
| `RBK-005` | `UPDATE_수검자정보`가 `205`로 실패 | `수검자` 행 **불변**, `최종수정일시` 불변 |
| `RBK-006` | `INSERT_수검자`가 `201`로 실패 | 신규 Patient 0건, Sequence 소비는 허용(결번) |
| `RBK-007` | 실패 직후 `@@TRANCOUNT` | **0** |
| `RBK-008` | 실패 응답의 Result Set 개수 | RS0 1개만 (202/203 제외) |

---

# 38. Concurrency Test (`tests/09`~`12`)

## 38.1 실행 방식 `[I]`

```bash
# scripts/concurrency-test.sh
BARRIER="HH:MM:SS"                       # 현재시각 + 5초
sqlcmd … -v BarrierTime="$BARRIER" -i tests/10_Concurrency_Session_A.sql -o logs/conc_A.log &
sqlcmd … -v BarrierTime="$BARRIER" -i tests/11_Concurrency_Session_B.sql -o logs/conc_B.log &
wait
sqlcmd … -i tests/12_Concurrency_Verify.sql
```

두 세션 모두 `WAITFOR TIME '$(BarrierTime)'` 이후 임계구역에 진입하므로 경합이 **우연이 아니라 결정적으로** 발생한다. 자정을 넘기지 않도록 barrier 시각 상한을 검사한다.

**판정은 로그의 ResultCode가 아니라 DB 최종 상태로 한다.**

## 38.2 시나리오 (`05` §14 고정 결과조건 전수)

| Test ID | 시나리오 | Session A | Session B | DB 최종 상태 판정 |
|---|---|---|---|---|
| `CON-001` | 동일 주민번호 동시등록 | `INSERT_수검자` | `INSERT_수검자` (동일 SSN) | 해당 SSN의 `수검자` 행 = **1** **그리고 로그에 `Msg 2627` 0건** |
| `CON-002` | 19/20 시간대 동시 신규예약 2건 | `INSERT_예약` | `INSERT_예약` (동일 시간대) | 해당 시간대의 `RSV+RCP` = **20** |
| `CON-003` | 동일 Patient 다른 시간대 동시예약 | `INSERT_예약`(AM) | `INSERT_예약`(PM) | 해당 Patient 유효업무 = **1** |
| `CON-004` | 주민번호 변경 vs 신규예약 | `UPDATE_수검자정보`(만나이 경계를 넘기는 SSN) | `INSERT_예약`(동일 Patient) | 저장된 `국가검사항목` = **최종 수검자 상태 기준 `UFN_HC_국가검사구성` 결과** (§38.6) |
| `CON-005` | 동일 RSV의 접수완료 vs 예약취소 | `UPDATE_접수완료` | `UPDATE_예약취소` | Work 상태 ∈ {`RCP`,`CNR`}, 성공 = **1** |
| `CON-006` | 같은 Work AEX 동시변경 | `UPDATE_접수추가검사` | 동일 (stale `행버전`) | 하나가 **`601 WorkChanged`** |
| `CON-007` | 예약 교차이동 (A: S1→S2, B: S2→S1) | `UPDATE_예약변경` | `UPDATE_예약변경` | 로그에 **`Msg 1205` 0건 · `Msg 50002` 0건** **그리고 `rc=1` 최소 1건** |

## 38.3 Deadlock 증거 `[X 수정]`

Extended Events 세션이나 trace flag 1222를 만들지 않는다. `CON-007` 판정 기준:

```text
두 로그 어디에도  Msg 1205 (엔진 교착)  0건
              그리고  Msg 50002 (applock 교착 victim)  0건
```

`[X]` 초안은 `1205` 만 봤다. applock 교착은 `sp_getapplock` rc `-3` 으로 반환되어 `THROW` 로 나오므로 **`1205` 는 절대 발생하지 않는다** — 즉 `CON-007` 이 항상 PASS 하는 무의미한 판정이었다. §20에서 `50002` 를 분리했으므로 이제 실제 판정이 가능하다.

## 38.4 "잠금이 동작했다"는 증거 `[X 수정]`

`[X]` **최종 DB 상태만으로는 잠금 동작을 증명할 수 없다.** 7개 시나리오의 기대 최종 상태는 "경합 발생"과 "우연한 직렬 실행"에서 **동일**하다. 세션 B가 3ms 늦게 도착해 A가 이미 커밋을 마쳤다면 applock 경합은 0회지만 모든 판정이 PASS 한다. **applock을 통째로 제거해도 7건 전부 PASS 할 수 있었다.**

보완 두 가지:

| # | 조치 |
|---:|---|
| 1 | §25.1의 `PRINT 'INFO applock rc=…'` 로그에서 **`rc=1`(대기 후 획득)이 최소 1건** 관측되어야 `CON-001`~`CON-005`·`CON-007`을 성립으로 인정한다. `CON-006`·`CON-008`은 조건부 `UPDATE`·정원 `COUNT` 가 상태로 판정되므로 제외한다 |
| 2 | 경합을 우연에 맡기지 않는다. 세션 스크립트가 `BEGIN TRAN` → 대상 자원을 `sp_getapplock` 으로 **직접 선점** → `WAITFOR DELAY` → 상대 SP 호출 순으로 임계구역 체류시간을 강제한다. **production SP 에는 어떤 지연도 넣지 않는다.** |

`CON-005`(접수완료 vs 예약취소)는 최종 상태가 단일 행이라 "정확히 하나만 성공"을 상태만으로 판정할 수 없다. **패자 세션이 `502` 를 반환했는지** 로그에서 함께 확인한다.

## 38.5 `CON-008` — 접수완료 동시 실행 (§24.2 대응) `[신설]`

| 항목 | 내용 |
|---|---|
| 사전조건 | 오늘 AM 시간대 이 `RSV` 20건, 그중 하나가 `W5`. 유효업무 없는 수검자 `P9` |
| Session A | `USP_HC_예약_등록(P9, 오늘, 'AM', 'WALKIN')` |
| Session B | `USP_HC_접수_완료(W5)` |
| 판정 | 해당 시간대의 `RSV+RCP` 가 **정확히 20** (21이면 §24.2 결함 재발) |
| 추가 | 같은 구조로 `INSERT_예약`(다른 유효업무 COUNT)·`UPDATE_수검자정보`(EP-08 COUNT) 대 접수완료 조합도 검증한다 |

## 38.6 `CON-004` 판정식 정정 `[X 수정]`

`[X]` **초안의 *"둘 다 성공 금지"* 는 정상 구현을 FAIL 시킨다.** `CONC` 수검자에게 유효업무가 없는
상태에서 A(주민번호 변경)가 먼저 커밋하면 그 시점에 `RSV`/`RCP` 가 0건이므로 변경이 허용되고,
이어서 B(신규예약)도 성공한다. **둘 다 성공한 이 결과는 완전한 직렬 실행이고 `00` EP-08 위반이
아니다** — EP-08 은 "활성 업무가 있으면 주민번호를 바꿀 수 없다" 이지 그 역이 아니다.

`[!]` **최종 상태로는 좋은 직렬과 나쁜 경합을 구분할 수 없다.** 둘 다 끝나면 "주민번호가 바뀌었고
Work 가 1건" 이라는 같은 모양이다. 구분되는 것은 **그 Work 의 검사구성이 어느 수검자 상태에서
계산됐는가** 하나뿐이다.

```text
① A -> B   주민번호 새 값 · Work 1건 · 검사구성은 **새** 생년월일 기준     정상
② B -> A   주민번호 옛 값 · Work 1건 · A 는 205 · 검사구성은 옛 기준       정상
③ 경합     주민번호 새 값 · Work 1건 · 검사구성은 **옛** 생년월일 기준     위반
```

③ 은 B 가 옛 생년월일로 NEX 를 계산해 두는 사이 A 가 주민번호를 바꿔 커밋한 것이다.
`05` §14 의 *"모순된 동시 성공"* 이 가리키는 상태가 바로 이것이며, `PAT` applock 이
B 의 읽기·계산·저장을 한 임계구역에 묶어 구조적으로 막는다.

판정:

```sql
-- 최종 수검자 상태로 다시 계산한 NEX 와 저장된 국가검사항목이 정확히 같아야 한다
DECLARE @국가검사 NVARCHAR(100) = (SELECT [국가검사항목] FROM [dbo].[예약접수] WHERE [수검자ID] = @P);
DECLARE @Now INT = (SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@P, @예약일));
DECLARE @Saved INT = CASE WHEN LEN(ISNULL(@국가검사, N'')) = 0 THEN 0
                          ELSE LEN(@국가검사) - LEN(REPLACE(@국가검사, N',', N'')) + 1 END;
-- 개수만으로는 약하다. 코드 집합까지 EXCEPT 양방향으로 본다.
```

`[!]` **검사가 물게 하려면 Setup 이 만나이 경계를 넘기는 주민번호를 골라야 한다.** `CONC1` 을
만 46세로 심고 A 가 만 56세가 되는 값으로 바꾸면 NEX 가 8행에서 11행(`EX009`·`EX011`·`EX013`)으로
움직인다. 같은 나이대 안에서 바꾸면 ③ 이 발생해도 결과가 같아 시험이 조용히 통과한다.

## 38.7 시각 제약 `[I]`

`CON-005`·`CON-008` 만 **접수완료 성공**을 필요로 하므로 예약일이 오늘이어야 하고 접수마감
전이어야 한다(`05` §2.4). 나머지 여섯은 미래 예약일을 쓰므로 업무시간(월~토 09:00~18:00)만 맞으면 된다.

```text
AM 시간대 을 쓰면   09:00 ~ 10:50      (마감 11:00, 경계 10분 여유)
PM 시간대 을 쓰면   11:00 ~ 15:50      (마감 16:00, 경계 10분 여유)   <- 창이 넓다
```

`CON-005`·`CON-008` 은 **PM 시간대** 으로 구성한다. AM 으로 두면 하루에 두 시간도 안 되는 창에
8개 시나리오 전체를 밀어넣어야 한다.

## 38.8 착수 후 실측으로 드러난 결함 `[신설]`

`T34` 를 실제로 돌려서야 보인 것들이다. 전부 **정적 검토로는 드러나지 않았다.**

| # | 결함 | 증상 | 조치 |
|---:|---|---|---|
| 1 | `sqlcmd -v` 값의 콜론 | `-v BarrierTime=17:50:00` 이 `':50:00'` 에서 잘려 `Sqlcmd: ':50:00': Invalid argument` 로 **접속 전에** 죽는다 | barrier 를 `HHMMSS` 로 넘기고 세션 스크립트가 `STUFF` 로 콜론을 끼운다 |
| 2 | `WAITFOR TIME @변수` 의 형 | `TIME(0)` 을 주면 `Msg 9815` 다 | `VARCHAR(8)` 로 둔다 |
| 3 | `CON-004` 인자 부족 | `UPDATE_수검자정보` 는 **전체치환형**이라 `@최종수정일시`·`@차트번호`·`@성명` 이 전부 필수다. 주민번호만 넣으면 `100` 으로 끝난다 | 현재 값을 barrier 앞에서 읽어 넘겨 **주민번호만** 바꾼다 |
| 4 | `100` 이 판정을 공허하게 만든다 | 세션이 `100` 으로 끝나면 업무경로에 닿지 못하는데 최종 상태는 "아무것도 안 바뀜" 이라 `tests/12` 가 **PASS 를 찍는다**. `CON-004` 가 실제로 그렇게 통과했다 | `concurrency-test.sh` 가 로그의 `100` 을 세어 1건이라도 있으면 FAIL 한다 |
| 5 | `CON-001` 의 주민번호 | 계획서의 `9505051000019` 는 체크디지트 **무효**라 `SSN-006` 에 걸려 중복검사에 닿지 못한다 | 유효값 `9505051000014` 를 쓴다 |
| 6 | `CON-008` 사전상태 | `F001`~`F019` 에 오늘 PM Work 를 **새로 넣으면** 유효업무가 2건이 되어 `RP-06` 위반 상태에서 시험하게 된다 | 기존 Work 를 오늘 PM 으로 **옮긴다** |
| 7 | 고정 로그 파일명 | 8회를 돌아도 마지막 시나리오 것만 남아 `rc=1` 이 어디서 났는지 알 수 없다 (G15) | `conc_${RUN}_${SCEN}_*.log` |

`[I]` **`100` 가드는 `CON` 전 시나리오에 건다.** 필수값 오류는 어느 시나리오에서든
"경합은 일어났는데 업무는 시작도 못 했다" 를 뜻하고, 그 결과는 항상 기대 최종 상태와 같다.

---

# 39. Security Test `[폐기]`

**폐기 (2026-09-07 사용자 결정)** — 계정·권한(`ROLE`·`USER`·`GRANT`·`DENY`)은 이 프로젝트의 산출물이 아니다. `NOT RUN` 으로 미루지 않고 범위에서 **뺀다.** `SEC-010`(secret 스캔)만 남는다 — 그것은 계정·권한이 아니라 배포 원본·로그에 자격증명이 섞이지 않았는지 보는 검사다. 아래 `SEC-001`~`009`·`011` 설계는 실행하지 않으며 `tests/13_Security_Tests.sql` 은 만들지 않는다.

## 39.1 대상 목록은 impersonation **밖**에서 확정한다 `[X 수정]`

```sql
-- dbo 컨텍스트에서 먼저 목록을 담는다
DECLARE @Obj TABLE (Kind VARCHAR(10), [성명] SYSNAME PRIMARY KEY);
INSERT INTO @Obj VALUES
 ('TABLE', N'수검자'), ('TABLE', N'예약접수'),
 ('TABLE', N'검사코드'), ('TABLE', N'휴무일'),
 ('TABLE', N'완료이력'), ('TABLE', N'변경이력'),
 ('TVF',   N'UFN_HC_일정확인'), ('TVF', N'UFN_HC_검진대상확인'),
 ('TVF',   N'UFN_HC_국가검사구성'), ('TVF', N'UFN_HC_추가검사확인');

EXECUTE AS USER = 'HC_APP_TEST';
    -- @Obj 를 순회한다.  sys.tables / sys.objects 를 여기서 읽지 않는다.
REVERT;
```

`[X]` **초안 오류**: 커서를 `EXECUTE AS` **안**에서 `SELECT name FROM sys.tables` 로 열었다. SQL Server 2005 이후 **메타데이터 가시성** 규칙상 권한 없는 주체에게는 `sys.tables` 가 **0행**이다(실측 확인: `S2) sys.tables 보이는 개수 = 0`). 커서가 한 번도 돌지 않아 `@Total = 0` 이 되고, 판정식에 따라 무조건 FAIL 하거나 **아무것도 시험하지 않고 PASS** 한다. 후자가 더 위험하다.

## 39.2 거부 판정은 `ERROR_NUMBER() = 229` 만 인정한다 `[X 수정]`

```sql
BEGIN TRY
    EXEC(N'SELECT TOP (1) * FROM [dbo].[' + @성명 + N']');
    SET @Fail += 1;                                   -- 성공하면 결함
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 229 SET @Denied += 1;         -- 권한 거부만 인정
    ELSE BEGIN PRINT 'FAIL 예상하지 못한 오류 ' + CONVERT(VARCHAR(10), ERROR_NUMBER()); SET @Fail += 1; END
END CATCH
```

`[X]` 초안은 `CATCH` 에 걸리기만 하면 무조건 "권한 거부"로 셌다. 객체명 오타·형변환 오류도 PASS가 된다.

`[I]` **DML 시도는 명시적 Transaction 안에서 하고 성공 여부와 무관하게 `ROLLBACK` 한다.** 권한이 잘못 부여된 상황이 바로 검수 대상인데, 그때 `INSERT`/`UPDATE`/`DELETE` 가 성공하면 초안에는 롤백이 없어 실제 데이터가 바뀐다.

## 39.3 Test 목록

| Test ID | 검증 | 기대 |
|---|---|---|
| `SEC-001` | `EXECUTE AS USER` 컨텍스트에서 `USER_NAME()` | `HC_APP_TEST` (실측 확인) |
| `SEC-002` | 동일 컨텍스트의 `IS_SRVROLEMEMBER('sysadmin')` | **`0`** (실측 확인) — dbo/sysadmin 오인 방지 증거. **FAIL이면 이후 전 항목 무의미하므로 즉시 중단** |
| `SEC-003` | 20개 SP 실행 | 전부 성공 (`Msg 229` 만 실패로 계산) |
| `SEC-004` | 6개 테이블 직접 `SELECT` | 전부 `Msg 229` |
| `SEC-005` | 6개 테이블 `INSERT`/`UPDATE`/`DELETE` (Transaction + `ROLLBACK`) | 전부 `Msg 229`, 데이터 변경 0 |
| `SEC-006` | 4개 TVF 직접 `SELECT` | 전부 `Msg 229` |
| `SEC-007` | `NEXT VALUE FOR [dbo].[SEQ_HC_CHART_NO]` | `Msg 229` (실측 확인) |
| `SEC-008` | Ownership chaining — SP 내부의 테이블·Sequence 접근 | 정상 동작. **Sequence 에도 체인이 적용됨을 실측 확인** → `GRANT UPDATE ON SEQUENCE` 불필요 |
| `SEC-009` | `TRUSTWORTHY` 설정 | `0` |
| `SEC-010` | **배포 산출물·로그에 비밀번호·연결 secret** | **0건.** 발견 시 `@Fail` 증가 + `THROW` |
| `SEC-011` | 서버 Login 미생성 | `sys.server_principals` 에 `HC[_]APP%` 0건 |

`[X]` 초안은 `SEC-010` ID를 "Login 미생성"에 붙여 스펙의 secret 검사를 밀어냈고, secret `grep` 결과에 PASS/FAIL 마커도 exit code 반영도 없었다. 두 검사를 `SEC-010`/`SEC-011`로 분리하고 둘 다 자동 판정한다.

`EXEC()` 동적 SQL은 이 파일에만 존재하며 production SP에는 없다.

---

# 40. Clean Rebuild Test (`tests/14_Clean_Rebuild_Verify.sql`)

| Test ID | 검증 | 기대 |
|---|---|---|
| Test ID | 검증 | 기대 |
|---|---|---|
| `RBD-001` | 잘못된 서버명에서 `Rebuild.sql` | **`NOT RUN`** — 인스턴스가 1개뿐이라 음성 시험 불가 |
| `RBD-002` | 대상 DB 컨텍스트에서 `Rebuild.sql` 실행 (master 아님) | `THROW 50021` 로 중단, DB 변경 0 |
| `RBD-003` | 빈 DB에서 `Deploy.sql` 전체 실행 | exit code 0 |
| `RBD-004` | 배포 직후 객체 인벤토리 | Table 6 / TVF 4 / SP 20 / Sequence 1 / PK 6 / FK 2 / UQ 2 / UX 1 / NCI 5 / Trigger 0 |
| `RBD-005` | **연속 2회 Rebuild** 후 **정렬된 객체·Seed 덤프를 `diff`** | 차이 0줄 |
| `RBD-006` | 2회 Rebuild 후 Seed 행수 + **19행 전건 값** | Exam 19 / Holiday 40, 값까지 동일 |
| `RBD-007` | `Deploy.sql` 단독 재실행 (DB 유지) | exit 0, 덤프 동일 |
| `RBD-008` | `03`~`07` Procedure 파일만 단독 재실행 | exit 0, `GRANT` **15건 유지** 확인 |
| `RBD-009` | `Net461MvpSample` | 존재 + 관찰한 메타데이터(`state_desc`, `user_access_desc`, `collation_name`, `is_read_only`) 전후 동일 |
| `RBD-010` | 로그·보고서에 secret | 0건 (`SEC-010`과 동일 판정 로직) |

`[X]` **초안 오류 3건**
1. `RBD-001`·`RBD-002` 라고 이름 붙인 단계가 실제로는 **둘 다 검증하지 않았다.** 실행한 것은 대상 DB 컨텍스트의 `Rebuild.sql` 이고 결과는 `50012` 였다. `50010`도 `50011`도 관측되지 않는데 두 Gate가 검증된 것처럼 기록됐다.
2. `RBD-002` 의 원래 가드 `IF @TargetDb <> N'HealthCheck…'` 는 바로 위에서 같은 리터럴을 대입했으므로 **항진 명제**였다 — 원리적으로 발화 불가능.
3. `RBD-005` 의 "인벤토리 완전 동일" 증거가 `COUNT(*)` 문자열 연결이었다. **객체명·컬럼·정의·권한·Seed 값이 달라도 개수만 같으면 동일 지문**이 되어, 잘못된 배포가 두 번 반복돼도 PASS 한다. 정렬된 메타데이터·Seed 덤프를 파일로 뽑아 `diff` 하는 방식으로 교체한다.

`[I]` `RBD-009` 는 **"존재 및 관찰한 메타데이터 불변"** 으로 표현을 제한한다. `SELECT name FROM sys.databases` 로 이름만 확인하는 것은 내용이 안 바뀌었다는 증거가 아니다. 다만 `DROP DATABASE` 식별자가 과제 DB로 하드코딩되어 있어 `Net461MvpSample` 을 직접 삭제하는 경로는 존재하지 않는다.

---

## 40a RED 음성시험 `[신설 2026-09-07]`

`[X 실측]` **계획서의 RED Step 5건은 증거가 남지 않아 닫을 수 없었다.** 일회성 관측이었고,
재현하려면 대상 DB 를 파괴하거나 커밋된 파일을 변조해야 해서 미이행으로 뒀다.

**폐기용 별도 DB 를 쓰면 둘 다 필요 없다.** 대상 DB 와 커밋된 파일을 손대지 않고
"시험이 실제로 실패를 잡는가" 를 매 회귀에서 판정한다.

```text
RED-001  객체가 하나도 없는 DB 에서 01_Schema_Tests 가 SCH 단언 16건을 실패로 잡는다
RED-002  TVF 가 없는 DB 에서 03_Rule_Tests 가 Msg 208/4121 로 죽는다
RED-003  테이블을 6 -> 5 로 줄이면 SCH-001 이 잡는다
RED-004  Seed 에 없는 행을 1건 넣으면 02_Seed_Tests 가 잡는다
RED-CLEAN   폐기용 DB 잔여 0건        — 남으면 다음 회차의 RBD-009 를 깨뜨린다
RED-TARGET  대상 DB 무사              — 이 스크립트가 대상을 건드렸다면 여기서 드러난다
```

`[X]` 처음에 `RED-001` 의 기대를 `Msg 208` 로 잡았다가 틀렸다. `sys.tables`·`sys.procedures` 는
**어느 DB 에나 있어** 개체오류가 아니라 단언 실패로 잡힌다. 실측이 옳고 기대가 틀렸다.

`[I]` 폐기용 DB 이름은 `HC_RedProbe` 다 — 대상 DB 와 접두사도 겹치지 않는다.
스크립트 시작과 끝에서 각각 지우므로 중간에 죽어도 잔여가 남지 않는다.

---

# 41. Log · Evidence 구조

```text
artifacts/
├─ logs/                              sqlcmd -u 원본 출력 (UTF-16LE). .gitignore
│  ├─ 00_preflight.log  01_schema.log  02_seed.log  03_functions.log
│  ├─ 04_select.log  05_patient.log  06_reservation.log  07_reception.log
│  ├─ 08_security.log  09_verify.log
│  ├─ test_01…test_14.log
│  ├─ conc_A.log  conc_B.log  conc_verify.log
│  └─ rs_<SP>.txt                     계약 검증용 다중 RS 출력
└─ reports/                           커밋 대상
   ├─ baseline-hash.txt               00~05 SHA-256 (작업 전 / 후 2회)
   ├─ winforms-manifest.txt           WinForms 전 파일 hash (G01 증거)
   ├─ object-inventory.txt            §40 RBD-004 결과
   ├─ contract-verify.txt             verify-contract.js 결과
   ├─ test-summary.txt                PASS/FAIL 집계 + 각 단계 exit code
   └─ phase4-report.md                최종 보고서
```

| 항목 | 규칙 |
|---|---|
| exit code | 모든 단계가 `sqlcmd -b` 사용 |
| **로그 디코딩** | **`iconv -f UTF-16`** 를 쓴다. **`-f UTF-16LE` 는 금지** |
| PASS/FAIL/SKIP marker | `PASS <TestId> <설명>` / `FAIL …` / `SKIP …` 접두 고정 → grep 집계 |
| 기대 실패 | `TRY/CATCH` + `ERROR_NUMBER()` 일치까지 확인해야 PASS 계상 |
| 한글 | 로그는 `-u`(UTF-16LE + BOM). 보고서는 UTF-8 |
| sqlcmd 출력 폭 | 계약 검증용 호출은 **`-W -w 65535`** 필수 (§36.3) |
| secret | 로그·보고서에 비밀번호·연결 secret·실제 개인정보를 남기지 않는다. 잠금 자원의 주민번호는 SHA-256 hash |

## 41.1 shell script 필수 관용구 `[X 수정]`

세 가지가 초안에서 **실패를 성공으로 보고**하게 만들었다. 전부 실측 확인했다.

| # | 초안 | 문제 | 교체 |
|---:|---|---|---|
| 1 | `iconv -f UTF-16LE … \| grep -c '^PASS'` | BOM 이 `EF BB BF` 로 남아 **첫 줄의 `^PASS` 가 매치되지 않는다.** 모든 PASS 카운트가 **-1** | `iconv -f UTF-16` (실측: 1건 → 2건) |
| 2 | `set -euo pipefail` … `sqlcmd …` / `RC=$?` | 실패 시 `set -e` 가 그 줄에서 종료 → **`RC=$?` 도 진단 로그 출력도 실행되지 않는다** | `RC=0; sqlcmd … \|\| RC=$?` 후 로그 출력, `exit $RC` |
| 3 | `cmd \| tee log` … `echo "exit=$?"` | `pipefail` **이 없으면** `$?` 는 마지막 명령인 `tee` 의 것(성공)이라 회귀 실패가 exit 0 으로 기록된다. `pipefail` 이 켜져 있으면 실패 상태가 전파되지만(실측: `false \| tee` → `$?=1`), 어느 쪽인지 읽는 사람이 알 수 없는 코드는 증거로 쓰지 않는다 | `cmd > log 2>&1; RC=$?; cat log` — 파이프를 없애면 조건 자체가 사라진다 |
| 4 | `for … done \| tee file` 안의 `\|\| exit 1` | 파이프 좌변은 서브셸 → 스크립트가 안 끝남. `tee` 는 stdout만 잡아 **stderr 로 나간 FAIL 이 증거에 안 남는다** | 파이프 제거, `>> file 2>&1` 누적, 실패 플래그를 루프 밖에서 집계 |
| 5 | `p=$(… \| grep -c '^PASS' \|\| echo 0)` | `grep -c` 는 0건일 때 `0` 출력 + exit 1 → `\|\| echo 0` 이 **추가로** 실행되어 `p="0\n0"` | `\|\| true` |

---

# 42. Phase 4 완료 Gate

**`PASS`는 실제 실행 증거가 있을 때만 사용한다.** 실행 전에는 `PLANNED` / `NOT RUN` / `BLOCKED` 중 하나를 사용한다.

아래는 **전부 실측**이다. 근거 회차는 셋이며, 시각 의존 경로가 서로 배타적이라 창 안·창 밖 둘 다 필요하다.

| 회차 | 시각 | 업무시간 | 결과 |
|---|---|---|---|
| `A` 창 안 | 2026-09-07 12:19 | 안 (월, 비휴무일 · PM 창 11:10~15:50) | `artifacts/logs/full_test_run.log` — exit 0 · PASS 357 · FAIL 0 · SKIP 2 · NOT RUN 1 |
| `B` 창 밖 | 2026-09-08 03:05 | 밖 | `artifacts/logs/full_test_run_off.log` — exit 0 · PASS 185 · FAIL 0 · SKIP 78 · NOT RUN 9 |
| `C` 창 안 | 2026-09-08 | 안 (화, 비휴무일 · PM 창) | `artifacts/logs/full_test_run_final.log` — exit 0 · PASS 363 · FAIL 0 · SKIP 2 · NOT RUN 1 |
| `R7` 창 안 | 2026-09-08 13:00 | 안 (화, 비휴무일 · PM 창 11:10~15:50) | `artifacts/logs/full_test_run_r7.log` — exit 0 · PASS 389 · FAIL 0 · SKIP 2 · NOT RUN 1 |

`[I]` 회차 `A` 는 사용자가 머신 시각을 6시간 뒤로 옮겨 만든 창이다. **코드는 한 글자도 바꾸지 않았다** —
`SYSDATETIME()` 이 시각의 유일한 입구이므로 SP·TVF·게이트가 전부 출하될 그대로 돌았다. 끝난 뒤 같은 크기로 되돌렸다.

`[!]` **회차 `A` 는 회차 `C` 로 대체됐다.** `A` 이후 판정 코드 자체가 두 번 바뀌었는데
(`tests/07` 의 `CWR-006` 단언 · `scripts/clean-rebuild-verify.sh` 의 `RBD-009` 스냅샷) 그 뒤로 창 안 회귀를
한 번도 돌리지 않아, `A` 는 **고치기 전 코드의 증거**였다. `C` 는 지금 출하되는 트리 그대로다.
`A` 행을 지우지 않는 이유는 그때 그 실측이 사실이기 때문이고, `B`(창 밖)는 여전히 유효하다 —
`OFF-309` 계열은 창 밖에서만 발화하므로 `C` 로 대체되지 않는다.

`[I]` **회차 `C` 는 이 표가 가리키는 그 로그를 실제로 만든 실행이다.** 재봉인은 `verify-baseline.sh` 의
해시 두 줄을 바꾸므로, 재봉인 **전에** 돈 회귀는 새 봉인 상태의 `G00` 을 판정하지 못한다. 그래서 순서를
뒤집었다 — 문서·해시를 먼저 확정하고, 이 표가 이미 가리키고 있는 파일명으로 회귀를 마지막에 돌렸다.
그래서 회차 `C` 는 **이 표·이 해시·이 `G00` 이 확정된 트리**를 판정했다.

`[!]` 여기에 "그 뒤로 아무것도 안 바뀌었다" 는 식으로 **미래를 단정하지 않는다.** R5 에서 그렇게
적었다가 다음 커밋 하나에 거짓이 됐고 R6 을 열어야 했다. 봉인 문서는 자기 밖의 상태를 주장할 수 없다 —
지금 트리가 이 표와 맞는지는 `./scripts/test.sh` 가 판정한다.

| Gate | 검증내용 | 필수 결과 | 결과 | 근거 |
|---|---|---|---|---|
| G00 | Baseline Hash | 봉인 **전건** 일치 · 미등록 0건 | **PASS** | `verify-baseline.sh` `=== 7/7 ===` (회차 `C`) · `test.sh` 안에서 매 회차 실행. **건수를 여기 적지 않는다** — `06` 입주로 6→7 이 된 뒤에도 이 칸이 `6/6` 이었다 (ROOT `AGENTS.md` §6) |
| G01 | WinForms 보호 | 변경 0건 (git diff + hash manifest 이중 증거) | **PASS** | `PASS WinForms 변경 0건` · `git diff --stat <R3 태그> -- winforms` **0줄** |
| G02 | Preflight | 가드 6종 통과 · KST 540 · Version >= 11 | **PASS** | `PRE-001`~`PRE-006` (인스턴스·DB·호스트 `DESKTOP-DP7KRE4`·KST +09:00·`17.0.1125.2`·RCSI OFF) |
| G03 | Clean Deploy | 빈 DB 전체 배포 성공 (exit 0) | **PASS** | `PASS RBD-003 빈 DB 에서 Deploy 전체 실행 exit 0` |
| G04 | Object Inventory | `05` §1.3·§1.4 가 선언한 객체가 전건 실재 | **PASS** (회차 `R7`) | `VER-001`~`VER-004` · `RBD-004` 지문. **수치를 여기 적지 않는다** — `SP 16` 이라 적어 두었다가 R7 에서 거짓이 되었다 (ROOT `AGENTS.md` §6) |
| G05 | Schema | `04` §8·§10.1 이 선언한 컬럼·Key·제약이 실제 DB 와 **양방향 차집합 0** | **PASS** (회차 `R7`) | `SCH-001`~`SCH-019` · `DOC-001`~`DOC-009`. **수치를 여기 적지 않는다** — `48컬럼·제약 24·Default 8` 이라 적어 두었다가 R7 에서 세 값이 **전부** 거짓이 되었다. 실측은 게이트가 낸다 (ROOT `AGENTS.md` §6) |
| G06 | 금지 객체 | Trigger 0 / TVP 0 / **업무 테이블을 지우는 SP 0** / 추가 Table 0 | **PASS** (회차 `R7`) | `VER-006` · `SCH-010`·`SCH-011`·`SCH-012`. R7 에서 `DELETE_자체휴무일` 이 생겨 조건을 좁혔다 — 금지의 뜻은 *업무 데이터를 지우지 않는다* 였다(§18) |
| G07 | Seed | `06` §13·§14 가 선언한 Seed 가 전건 값까지 일치 · 공휴일 등재 만료 전 | **PASS** (회차 `R7`) | `VER-007` · `RBD-006` · R7 부터 `verify-holiday-seed.sh` 가 §14 표와 `00` §7.4 임계를 함께 판정한다 |
| G08 | Rule | TGT/NEX/AEX/HOL 경계 전건 + `CORRUPT-3` 재검증금지 | **PASS** | `tests/03_Rule_Tests.sql` 51건 |
| G09 | SP Contract | `05` §7~§12 Parameter 와 `sys.parameters` 가 `EXCEPT` 양방향 0 · SP 전건 후속 RS · RS0 결과코드 가 `05` §13 허용집합 내 | **PASS** (회차 `R7`) | `SCH-019` · `V17` · `V18`. **건수를 여기 적지 않는다** — `Parameter 99`·`16/16` 이 R7 에서 거짓이 되었다 (ROOT `AGENTS.md` §6) |
| G10 | Rollback | 부분저장 0건 | **PASS** | `RBK-001`~`RBK-007` · `RBK-008` 은 T-SQL 로 RS 개수를 셀 수 없어 `V18` 이 정적으로 판정 |
| G11 | Concurrency | `CON-001`~`008` + `rc=1` 1건 이상 + `Msg 1205`·`50002` 각 0건 | **PASS** | **8/8** · `applock rc=1` 시나리오마다 1~2건 · `1205`·`50002`·`50001`·`2627`·`결과코드 100` 각 **0건** (회차 `A`) |
| G12 | Security | `SEC-010` | **PASS** | 계정·권한은 **폐기**했다 (2026-09-07 사용자 결정, §32). 남은 `SEC-010`(배포 원본·로그·보고서 secret 0건)은 매 회귀에서 PASS |
| **G13** | **SQL Server 호환성** `[D4-004]` | (a) 배포·시험 성공 · (b) 블랙리스트 0건 · (c) §9.2 준수 | **(a) PASS · (b) PASS · (c) REVIEWED** | (a) 회차 `A`·`B`·`C` exit 0 · (b) `verify-tsql-allowlist.sh` 0건 + `CREATE OR ALTER` 는 배포 안에만 · (c) `artifacts/reports/allowlist-review.md` — §9.2 허용 표를 파싱해 행마다 사용/미사용을 실측한다(사용 22 · 미사용 1 · 수동 0). **`tools/allowlist-review.js` 가 회귀 안에서 매 회차 재생성하고 목록 밖이 나오면 `FAIL` 이다**(`PASS G13-c`) — 그 BAN 에 없던 TVP·Trigger·FK Cascade 를 이것이 본다 |
| G14 | Repeatability | Rebuild 2회 후 정렬 덤프 `diff` 0줄 | **PASS** | `RBD-005` 덤프 완전 동일 · `RBD-007`(Deploy 단독) · `RBD-008`(Procedure 단독) 도 동일 |
| G15 | Evidence | 실행명령·exit code·로그·보고서 + run ID·시각·업무시간 | **PASS** | `conc_<RUN>_<SCEN>_*.log` · `test-summary.txt`(회차 시각 포함) · `phase4-report.md` · **`RED-001`~`004` 음성시험**(§40a)이 "시험이 실제로 실패를 잡는가" 를 매 회귀에서 판정한다 |
| G16 | 06 문서 | 실제 구현과 일치하는 FINAL | **PASS** | 본 문서 §4.1 이 `FINAL / GO / READ-ONLY`. **버전을 여기 적지 않는다** — `v1.0` 이라 적어 둔 채 v1.1 이 되어 한 판 뒤처졌다 (ROOT `AGENTS.md` §6) |

`[I]` **`SKIP` 은 `PASS` 가 아니다.** 회차 `A` 의 `SKIP` 은 `OFF-309-01`·`02` 둘뿐이고,
이는 업무시간 안에서 `309` 가 나올 수 없다는 **정의상의 배타성**이다. 회차 `B` 에서 `PASS` 로 판정됐다.
두 회차를 합치면 시각 의존 경로에 빈 구멍이 없다.

`[실측 최종]` 회차 `A` 의 `NOT RUN` 은 `RBD-001` 하나뿐이고 그것은 **구조적으로 불가능**하다
(인스턴스가 1개라 잘못된 서버명 `50020` 을 발화시킬 수 없다). 회차 `B` 의 `NOT RUN` 9 는
그 하나에 창 밖이라 돌 수 없는 `CON-001`~`008` 을 더한 것이다.

`[X 실측]` **G05 의 `NCI 4` 는 오기였다.** R3 재봉인이 인덱스를 하나 늘려 실측은 **5** 다
(`SCH-008`·`VER-005`·`RBD-004` 지문 모두 5). 표를 실측에 맞췄다.

`[X 실측]` **G00·G01·G05 는 회귀 밖에 있었다.** `test.sh` 가 `verify-baseline.sh`·
`verify-winforms-unchanged.sh`·`verify-schema-doc.sh` 를 부르지 않아, 이 표를 채우려면 손으로 돌려야 했다.
손으로 돌린 증거는 다음 회차에 썩는다 — `G13-b` 와 같은 실패 방식이다. 셋 다 `test.sh` 에 넣었다.

`[I]` **`./scripts/test.sh` 는 `tests/01`~`14` 뿐 아니라 동시성 `09`~`12` 와 `verify-contract.js` 까지 실행해야 "전체 회귀"다.** 초안은 `01`~`08`·`13`·`14` 만 돌고 `"=== 전체 테스트 통과 ==="` 를 출력해, G09·G11 이 실행되지 않아도 T37의 회귀가 exit 0 이었다.

---

# 43. 알려진 한계

| # | 한계 | 영향 | 완화 |
|---:|---|---|---|
| 1 | **업무시간 밖 Write SP 성공 경로 검증 불가** `[X 완화됨]` | 창 밖에서는 Write SP가 `308`/`309`로 끝나 성공 경로를 볼 수 없다 | 세 겹으로 덮었다. ① Rule TVF 가 `@서버시각` 주입으로 시간 경계를 결정적으로 전수 검증한다. ② `tests/05`~`08` 이 창 밖에서 **분기**로 "창 밖 호출이 DB 를 바꾸지 않았다"(`PWR/RWR/CWR/RBK-OFF`)를 단언하고 `OFF-309-*` 가 `309` 를 계약으로 판정한다 — SKIP 이 아니라 판정이다. ③ 성공 경로가 필요하면 **머신 시각을 옮긴다**(§43-14). production SP 에 시간 주입 backdoor 를 넣지 않는다 |
| 2 | **SQL Server 2012 실기 검증 없음** `[D4-004]` | 허용목록 준수는 코드리뷰로 확인하며 2012 인스턴스에서 실제 실행하지는 않는다 | 사용자 결정으로 대상이 SQL Server 2025로 고정되었다. 허용목록이 사용자 규칙보다 엄격하다 |
| 3 | **`03` baseline hash 불일치** `[D4-001]` | 인계문서 기대값과 실측값이 다르며 원인을 규명하지 못했다 | Phase 4가 소비하는 `04`·`05`는 byte 일치. `03`은 UI 계약이라 DB 객체계약에 영향이 없다 |
| 4 | 다중 Result Set의 Snapshot 일관성 | `SELECT_예약가능정보`·`SELECT_예약접수상세`에서 RS 간 미세한 시점 차이가 가능 | 조회값은 사전안내이며 Write SP가 Transaction 안에서 전부 재검증한다 (`04` §1.2) |
| 5 | **취소 SP만** `SLOT` 잠금 미획득 | 미커밋 취소로 정원이 실제보다 **많아** 보여 `305`가 나올 수 있다 | 오차 방향이 보수적(과대)이라 무결성 위반이 아니다(§24.2 표). **접수완료는 과소집계가 가능해 잠금을 확장했다** |
| 6 | `verify-contract.js`의 구분자 의존 | 데이터에 `\|`가 포함되면 파싱이 흔들릴 수 있다 | 검사명·상태명에 `\|`가 없음을 Seed 검수에서 확인한다. `-W -w 65535` 로 줄 접힘을 제거한다 |
| 7 | **계정·권한 폐기** `[사용자 결정]` | 2026-09-07 "Security 는 Grant·Role 같은 계정·권한이라면 폐기한다". DB 에 principal 이 없고 GRANT/DENY 도 0건이다 | **NOT RUN 이 아니라 범위에서 뺐다** — 미룬 것이 아니므로 게이트에 잔여가 남지 않는다. 그 결과 화면은 배포 계정(개발 환경에서는 sysadmin)으로 붙고, "SP 만 호출한다" 는 **강제되지 않는 관례**다. 실무라면 반드시 필요하다 |
| 8 | **`RBD-001`(잘못된 서버명) 음성 시험 불가** | 인스턴스가 1개뿐이라 `50020` 경로를 실제로 발화시킬 수 없다 | `NOT RUN` 으로 기록한다. `PASS` 로 쓰지 않는다 |
| 9 | ~~**§24.2 과소집계 시나리오 미실측**~~ `[해소 2026-09-07]` | — | `CON-008` 실측 완료. B 가 `접수완료`로 `RSV → RCP` 를 옮기는 사이 A 의 `WalkIn` 신규가 정원 20 을 보고 `305` 로 막혔다. 최종 20 (21 이면 결함 재발). `SLOT` 잠금 확장이 실제로 필요했음이 확인됐다 |
| 10 | **G13 (c) 허용목록 준수는 자동 판정 불가** | 블랙리스트 grep 0건이 허용목록 준수를 증명하지 않는다 | `REVIEWED` 로만 기록한다(§9.3) |
| 11 | **`05` §17.6 "허용되지 않은 7번째 자리" 는 구성 불가** | `05` §10.1 표가 숫자 `0~9` 열 개를 전부 매핑하므로 "미정의 숫자"가 존재하지 않는다 | 기준선을 고치지 않는다. §44.6에 Deviation 으로 기록하고, 비숫자 7번째 자리는 형식검사(`101`)로 흡수됨을 명시한다 |
| 12 | **`OFF-309-01`·`02` 는 창 밖에서만 판정된다** | 창 안 회차에서는 `SKIP` 이고, `SKIP` 은 `PASS` 가 아니다 | **정의상의 배타성**이다 — 업무시간 안에서 `309` 는 나올 수 없다. 창 밖 회차에서 `PASS` 로 판정되며, 두 회차를 합치면 빈 구멍이 없다. §42 가 두 회차를 함께 근거로 든다 |
| 13 | **`G09` 의 RS0 개수는 정적 검사로 판정한다** | `INSERT … EXEC` 가 금지(2개 이상 RS 에서 `Msg 213`)라 실행 중에 RS 개수를 셀 수 없다 | `V17`(RS0 42블록 5컬럼 CAST)·`V18`(실패 67건 RS0 1개)이 배포 원본을 정적으로 판정한다. 실행 판정은 `tests/contract/*` 가 sqlcmd 출력으로 한다 |
| 14 | **시각 의존 경로 검증은 사람이 시계를 옮겨야 한다** | `CON-005`·`CON-008`·`CWR-006`·`CWR-009` 등 성공 경로는 특정 시각창에서만 성립한다 | 시각은 `SYSDATETIME()` 하나로만 들어오므로 **OS 시각을 옮기면 코드를 한 글자도 안 바꾸고** 창을 만들 수 있다. 상대 이동(`Set-Date -Adjust`)으로 옮기고 같은 크기로 되돌리면 오차가 0 이다. **자동화하지 않는다** — 되돌리지 못한 채 죽는 스크립트를 남기지 않는다. TVF 상수를 고치는 우회는 금지다: `rebuild` 가 매 시나리오마다 되돌리므로 성립하지 않고, 배포 원본을 고치면 **다른 제품을 시험한 `PASS`** 가 된다 |
| 15 | ~~**접수취소 후 당일 재접수 불가**~~ `[한계가 아니었다 — 2026-09-08]` | — | **오판이었다.** 16:00 이후엔 접수완료도 당일예약도 `304` 라 그 시간에 오접수가 **생길 수 없고**, 다음 날 이후 예약은 `마감시각 NULL` 로 열려 있다(실측). 막히는 것은 당일 복구뿐이며 그것은 접수마감 정책이 의도한 바다. 각 규칙은 문서화돼 있었는데 그 **조합**을 잘못 읽어 구멍으로 분류했다 (§44.8-9) |
| 16 | ~~**`변경이력.대상키` 가 재배포 후 다른 사람을 가리킨다**~~ `[해소 2026-09-08]` | — | 배포 마지막에 `DBCC CHECKIDENT` 로 `수검자`·`예약접수` 의 IDENTITY 시드를 `변경이력` 의 최대 `대상키` 뒤로 민다. 재사용이 구조적으로 사라진다(실측: 최대 대상키 7 → 다음 수검자ID 8). `Rebuild` 경로는 변경이력도 지우므로 발동하지 않아 `RBD-005` 에 영향이 없다. **기준선 무변경** — `06` §9.2 허용목록에만 추가했다 |
| 17 | ~~**대소문자만 바꾼 수정이 조용히 버려진다**~~ `[해소 2026-09-07]` | — | No-op 판정과 감사 필터를 **함께** `CONVERT(VARBINARY(...))` 바이트 비교로 바꿨다. `COLLATE` 는 §9.2 밖이지만 VARBINARY 변환은 허용목록 안이고 정렬과 무관하다(실측: CI 는 같다고, VARBINARY 는 다르다고 본다). `INTERSECT` 의 NULL=NULL 성질은 유지된다. 한쪽만 고치면 저장은 되는데 이력에 안 남으므로 `PWR-032` 가 둘 다 본다 |
| 18 | ~~**RS1 을 트랜잭션·잠금 밖에서 재조회한다**~~ `[해소 2026-09-08]` | — | 잠금 안에서 변수로 **포획**하고 커밋 뒤에는 변수를 낸다. RS 를 트랜잭션 밖에서 낸다는 §21.1 `[6]` 은 그대로이고 계약(컬럼·타입)도 안 바뀐다. Write SP 7곳에 적용했다. 예외 하나 — `INSERT_수검자` 의 RS1 은 `202`·`203` 에서 **다른 사람의 행**을 돌려주므로 우리 잠금이 보호한 적이 없다. 살아 있는 값을 보여주는 편이 정직해 재조회를 유지하고 이유를 코드에 남겼다 |
| 19 | ~~**클라이언트가 Result Set 을 끝까지 읽지 않으면 감사행이 누락된다**~~ `[해소 2026-09-08]` | — | 감사를 `RS0 → 감사 → RS1` 로 옮겼다. RS0 를 받았다면 감사는 이미 실행됐다. `04` §8.6.5 ③ 의 "**해당** Result Set" 이 단수라 **기준선을 열지 않았다** (§21.1 에 해석 근거). `V19` 가 순서를 정적으로 고정한다 |
| 20 | **주민번호가 `변경이력` 에 평문으로 영구 보존된다** `[사용자 결정 — 그대로 둔다]` | `04` §8.6 "바뀐 컬럼마다 1행" 과 §22 "SSN 을 DMV 에 노출 금지" 가 서로 당긴다. 둘 다 지키면 감사에 평문이 남는다 | 2026-09-08 사용자 결정으로 **고치지 않는다.** `00` §2.1 이 임의 테스트 주민번호만 쓰게 해 과제 범위에서 실피해가 없다. **보유기간·삭제 정책이 없다** — 운영 전 반드시 결정해야 하며 Phase 5 인계에 넣었다. 고치려면 마스킹(`801225-1******`)이 유력하고 `04` §8.6.2 재봉인이 필요하다 |
| 21 | **정원 20 이 조회 SP 와 저장 SP 에 따로 박혀 있다** `[완화 2026-09-08]` | 조회 SP 8곳·저장 SP 2곳에 리터럴로 있다. 한쪽만 고치면 화면은 "자리 있음" 을 보여주고 저장은 `305` 를 낸다 | 설정 테이블은 오히려 기준선 위반이다(`00` RP-03 이 정책 수치로 확정 · 관리자 CRUD 는 §8.2 밖). 대신 **양쪽을 실행으로 고정**했다 — `SEL-025` 가 조회 SP 의 `정원` 를 매 회귀에서(시각 무관) 보고, `CON-002`(19/20→20)·`CON-008`(20/20→305)이 저장 쪽을 경계에서 본다. 한쪽만 바뀌면 둘 중 하나가 반드시 깨진다 |
| 22 | **`700` 과 `701` 의 상대 순서가 SP 마다 반대다** | `UPDATE_예약변경` 은 `700 → 701`, `UPDATE_접수추가검사` 는 `701 → 700` 이다. Master 손상 시 같은 원인에 다른 결과코드 가 나간다 | `05` §5 가 둘을 한 칸("Master·Aggregate 데이터 구성")에 묶어 두어 어느 쪽도 위반이 아니다. 각 SP 는 자기 계약(§11.2 vs §12.2)의 문장 순서를 정확히 따랐다 — **계약 문서가 갈라져 있다** |
| 23 | **"무변경 성공" 의 업무시간 취급이 SP 마다 다르다** | 창 밖에 무변경 저장을 누르면 수검자수정·예약변경·추가검사는 `결과코드=1` 로 성공하고, 동일 주민번호 재등록은 `308`/`309` 로 실패한다 | 셋은 `05` §10.2/§11.2/§12.2 를, 하나는 §10.1 을 각각 정확히 따른 결과다. 구현 버그가 아니라 계약의 비대칭이다 |

---

# 44. Deviation / Blocker

## 44.1 `[X]` D4-001 — `03_Wireframe_Definition.md` hash 불일치

| 항목 | 내용 |
|---|---|
| 실측 | `831b61f27e38d60e856e9309906af2df89c5632a732b8289fdb216b257c80024` |
| 인계문서 기대 | `5426e642863bfcc6a637a38e216b68f013679fd5791ca7b63e34c40b367b2e2d` |
| 개행 원인 여부 | **아님.** raw / LF변환 / CR제거 / CRLF변환 / 후행개행 정리 / BOM 제거 6변형 전부 불일치 |
| 내부 메타데이터 | 정상 — 문서명·`FINAL / GO / READ-ONLY`·`v1.2`·`2026-09-03`·`HC-RSV-RCP-20260904-R3` |
| 금지 marker | `암호화`·`복호화`·`HMAC`·`SEC_PATIENT_IDENTIFIERS`·`MST_NATIONAL_EXAMS`·`MST_ADDITIONAL_EXAMS` **0건** → 인계문서 §4.2-5 즉시중단 조건 미해당 |
| 내용 정합성 | 문서 완결(23장, 말미 "최종 판정: GO"). 정원 20 / NEX 8~11 / AEX 7종 / 주민번호 13자리 / `수검자` 16컬럼 / 예약변경 자기 Work 오탐 방지 / 취소 복원 없음 — 전부 `04`·`05`와 모순 없음 |
| 사본 존재 | `docs` 전체에서 0건 |
| **판정** | 사용자 결정으로 **현재 파일을 기준본으로 확정**. 원인은 규명되지 않은 채로 본 항목에 기록한다 |
| 영향 | Phase 4 DB 객체계약에 **영향 없음** — `04`·`05`가 byte 일치이며 `03`은 우선순위 4위 UI 계약서다 |

## 44.2 `[X]` D4-002 — Git repository 부재

ROOT·하위 어디에도 `.git`이 없었다. 인계문서 §7.3.코드·§10.5(Task별 Commit checkpoint)·§10.6 Stage 0·Stage 12가 모두 repo 존재를 전제한다. 사용자 승인으로 **ROOT에 `git init`** 하고 00~05·winforms를 초기 commit + tag한 뒤 Phase 4 전용 branch에서 작업한다.

## 44.3 `[X]` D4-004 — 인계문서 §5.15 / 원안 G13 변경

인계 프롬프트는 Compatibility Level 110 강제와 "Compatibility 110에서 전체 실행"을 G13으로 요구했다. 그러나:

1. **기준선 00~05에 근거가 없다.** `04` §3.9와 `05` header는 "SQL Server 2012 **이상**"이라는 하한선만 정한다.
2. **compat 110은 목적을 달성하지 못한다.** `STRING_AGG`는 어떤 compat level에서도 동작하고 `CREATE OR ALTER`·`DROP … IF EXISTS`는 DDL이라 compat과 무관하다.
3. 이 PC의 유일한 SQL Server가 2025이므로 실기 검증도 불가능하다.

사용자 결정으로 대상을 SQL Server 2025로 고정하고 §9.2 허용목록으로 관리하며 G13을 재정의했다. **00~05는 변경하지 않았다.**

## 44.4 `[X]` 도구 환경 제약

인계문서 §7.3.K가 제시한 계약 검증 후보 중 **B(C# Console verifier)는 `dotnet` 미설치**, **코드(Python/pyodbc)는 `python` 미설치**로 실행이 불가능하다. 사용자 승인으로 node 표준 라이브러리 기반 파서를 사용한다(`D4-009`).

## 44.5 `[X]` v0.2 적대적 검토에서 확정·수정한 설계·계획 결함

2026-09-04, 서브에이전트 4개 + `codex 0.152.0`(`gpt-5.6-sol`, read-only) 5중 적대적 검토를 수행했다. 원지적 약 96건 → 중복 제거 41건 → **반증 5건 / 확정 36건.**

### 반증한 지적 (실증으로 기각)

| 지적 | 실증 결과 |
|---|---|
| `IF OBJECT_ID` 가드가 미존재 TVF 의 배치 컴파일 오류를 못 막는다 | **작동함.** `INSERT…SELECT FROM tvf` 형태로도 exit 0 |
| Sequence 에 소유권 체인이 적용되지 않아 `GRANT UPDATE` 가 필요하다 | **적용됨.** SP 내부 `NEXT VALUE FOR` 성공, 직접 호출은 `Msg 229` → `SEC-007` 유효 |
| `sqlcmd :r` 가 UTF-8 BOM 파일에서 `Msg 102` | **정상 동작** |
| 계획서의 날짜 요일 가정 5건 | **전부 정확** |
| 교착·`05` §14 결과조건 7종·`적용후인원`·부분저장·오류 우선순위·`최종수정일시` 단조증가 | **전 축 방어 확인** (28쌍 전수, 전 틱 반례 없음) |

### 확정한 설계 결함 (본 문서 본문을 수정함)

| # | 결함 | 수정 위치 |
|---:|---|---|
| 1 | **`RSV→RCP` 인덱스 키 이동으로 정원·중복·EP-08 COUNT 과소집계** — §24.2의 "무결성 위반 아님" 주장이 반증됨 | §18 · §21.2 · §22 · §24.1 · §24.2 · §38.5 |
| 2 | `UPDATE_예약변경` 에 `05` §13 허용집합 밖 `501` 도입 | §21.2(제거) |
| 3 | `UPDATE_수검자정보` No-op 비교의 NULL 미규정 → 사용자 편집 소실 가능 | §28.2 |
| 4 | 조건부 `SSN`/`CHART` 잠금이 전역 순서를 뒤집을 수 있음 | §24.1 (항상 획득) |
| 5 | `04` §14의 **ExamSource↔Master 역할 일치** 구현 책임 누락, NEX 상한 11 미검증 | §21.2a |
| 6 | 자동 차트번호 100회 임의 상한이 `206` 의미를 왜곡. Sequence 취득이 doomed 트랜잭션 유발 | §12.6 |
| 7 | 자동 차트번호 경로가 `CHART` applock 미획득 → `2627` 누출 | §12.6 |
| 8 | applock timeout 과 deadlock 을 한 번호로 뭉개 `CON-007` 이 항상 PASS | §20 · §25.1 · §38.3 |
| 9 | Preflight/Rebuild 가드 혼용 — `DB_ID()>4` 가 master 에서 항상 실패, `50011` 은 항진 명제 | §8.3 |
| 10 | §21.1 Template 의 RS0 `SELECT` 에 컬럼 별칭 없음 | §21.1 |

### 확정한 검증층 결함 (테스트가 약속보다 적게 검사)

| # | 결함 | 수정 위치 |
|---:|---|---|
| 11 | **`INSERT … EXEC` 오해** — RS 2개 이상이면 `Msg 213`, 내부 `ROLLBACK` 은 `Msg 3915`, 진입 `@@TRANCOUNT=1`. 약 50곳 무력화 | §33.1a (테스트 아키텍처 교체) |
| 12 | `EXECUTE AS` 안에서 `sys.tables` 를 읽어 0행 → `SEC-004~006` 무의미 | §39.1 |
| 13 | 모든 예외를 권한거부로 오인, 실패 시 실제 DML 잔존 | §39.2 |
| 14 | `SCH-015`(2컬럼)·`SCH-016`(`COUNT>=20`) 이 "전건 일치" 를 증명하지 못함 | §34 |
| 15 | RS0 메타 검증이 `NULL` 때문에 거짓 양성 (SP 14개여도 PASS) | §36.2 |
| 16 | 계약 검증기가 15개 중 **13개** SP 만 호출, 타입 미검증, `-w` 미지정으로 80칸 접힘 | §36.3 · §36.5 |
| 17 | §36.4 SP별 허용 결과코드 검증에 대응 Task 없음 | §36.4 |
| 18 | G14 지문이 `COUNT` 문자열이라 내용 차이를 못 봄 | §40 `RBD-005` |
| 19 | G13 을 블랙리스트 grep 0건으로 PASS 처리 | §9.3 |
| 20 | `test.sh` 가 `09~12`·계약검증기를 빼고 "전체 통과" 출력 | §42 |
| 21 | 업무시간 SKIP 가드가 `CWR-006` 한 곳뿐 | §33.2a |
| 22 | `SKIP` 을 `PASS` 로 승격 가능 | §42 |
| 23 | 동시성 시험이 "잠금이 동작했다"를 증명하지 못함 (applock 제거해도 전건 PASS) | §38.4 |
| 24 | `CORRUPT-3` 미구현, `CORRUPT-4/5` 부재 | §15.3 |
| 25 | Seed 검수가 19행 전건을 보지 않음, `deploy/02_Seed.sql` 의 불필요한 `DELETE` 가 FK 로 실패 | §13 |
| 26 | `RBD-001/002` 가 이름과 다른 것을 검증 | §40 |
| 27 | `Net461MvpSample` "변경 0건" 증거가 이름 존재 확인뿐 | §40 `RBD-009` |
| 28 | `SEC-010` 이 secret 검사에서 Login 검사로 바꿔치기됨 | §39.3 |
| 29 | `05` §17 적대적 시나리오 12건에 Test ID 없음 | §33.4 |
| 30 | 허용목록에 없는 `CURSOR`·`FOR XML PATH` 사용 | §9.2 |

### 확정한 셸·도구 결함 (실패를 PASS 로 오인)

| # | 결함 | 실측 |
|---:|---|---|
| 31 | `iconv -f UTF-16LE` 가 BOM 잔류 → 첫 줄 `^PASS` 미매치, **모든 PASS 카운트 -1** | 1건 → 2건 확인 |
| 32 | `set -euo pipefail` 하에서 `RC=$?` 미실행 → 실패 시 진단 소실 | 확인 |
| 33 | `cmd \| tee` 의 `$?` 가 `pipefail` 없이는 tee 의 것 → 회귀 실패를 exit 0 으로 기록 | 확인 (초안 서술 *"항상 0"* 은 `pipefail` 하에서 거짓. §41.1-3 정정) |
| 34 | `for … done \| tee` 서브셸 + stderr 미포착 → 증거에 PASS 만 남음 | 확인 |
| 35 | `grep -c … \|\| echo 0` 이 `"0\n0"` 생성 | 확인 |
| 36 | sqlcmd 기본 `-w 80` 줄 접힘으로 파서 붕괴 | 확인 |

전부 §41.1에 관용구로 고정했다.

## 44.6 `[X]` 기준선 내부 모순 — `05` §17.6 "허용되지 않은 7번째 자리"

`05` §17.6은 적대적 테스트 항목으로 *"허용되지 않은 7번째 자리"*를 요구하지만, 같은 문서 §10.1의 세기·성별 표는 숫자 `9,0,1,5,2,6,3,7,4,8` — **`0~9` 열 개 전부**를 매핑한다. 13자리 숫자 검증을 통과한 값 중 7번째 자리가 미정의인 경우는 **존재할 수 없다.**

**기준선을 수정하지 않는다.** Phase 4는 다음으로 처리한다.

```text
비숫자 7번째 자리      → 형식검사(13자리 숫자)가 흡수 → 101 BadValue   (PWR-014 로 시험)
미정의 숫자 7번째 자리  → 구성 불가. 본 Deviation 으로 기록하고 시험하지 않는다
```

`INSERT_수검자`/`UPDATE_수검자정보` 구현의 `ELSE NULL` 분기는 도달 불가 코드이지만 **방어적으로 유지**한다(`05` §10.1 표가 바뀌면 즉시 드러난다).

## 44.6a `[X]` `PWR-013` "주민번호 14자리 → 101" 은 SP 경계에서 구성 불가

`§33.4`가 `05` §17.6의 *"주민번호 14자리 입력"*에 `PWR-013`을 배정하고 `plans/04`가 기대를 `101 BadValue`로 적었으나, **실행으로 성립하지 않는다.**

`@주민번호`의 SQL 타입은 `VARCHAR(13)`이다(`05` §10.1·§10.2). 14자리 값을 넘기면 SQL Server가 **SP 진입 전에 조용히 13자리로 절단**하므로 SP 본문의 `LEN(@주민번호) <> 13` 검사에 도달할 수 없다. `T23` 구현 중 실측으로 확인했다.

```text
EXEC USP_HC_수검자_등록 1, NULL, N'신규수검자', '90010110000188', … 
  → SP 가 보는 값 '9001011000018' (13자리)
  → RS0 결과코드 = 0 또는 2.  101 은 어떤 입력으로도 나오지 않는다
```

이것은 결함이 아니라 `05` §2.2가 *"하이픈이 포함된 14자리 표시값을 SP에 전달하지 않는다"*고 C#에 못박은 **이유 그 자체**다. 타입 경계가 이미 계약을 강제하고 있다.

**기준선을 수정하지 않는다.** `PWR-013`은 폐기하지 않고 **관측 가능한 계약**으로 다시 정의한다.

```text
PWR-013  14자리 표시값은 VARCHAR(13) 경계에서 절단된다
         계약 판정  tests/contract/PWR-013_주민번호_14자리_경계절단.sql → RS0 결과코드 = 2
                   (절단 결과가 PWR-001 이 만든 행과 같은 주민번호가 되므로 기존수검자 사용)
         DB 판정    tests/05 — 14자리 주민번호를 가진 행이 0건이고 대상 주민번호 행은 1건 그대로
```

`05` §17.6의 다른 두 항목(`PWR-014` 비숫자, `PWR-027` RCP 존재)은 영향이 없다. `§45.2`의 `PWR` 건수 25는 유지된다.

## 44.7 Implementation Blocker

**0건.** 위 36건은 전부 본 문서와 구현계획 수정으로 해소되며 기준선 00~05 변경을 요구하지 않는다.

---

## 44.8 실행검증 단계에서 새로 발견한 이탈 `[신설 2026-09-07]`

**Table/Column/SP/TVF/Parameter/Result Set/ResultCode 계약은 하나도 바뀌지 않았다.** 아래는 전부
**시험·게이트 설계**의 결함이며, 공통점은 *"게이트가 green 인데 아무것도 검증하지 않고 있었다"* 이다.

| # | 이탈 | 어떻게 green 이었나 | 조치 |
|---:|---|---|---|
| 1 | `OFF-308` 이 **한 번도 실행된 적이 없다** | 일요일·활성 휴무일을 기다리는 구성이라 평일 회차마다 `SKIP`. `SKIP` 은 집계에서 실패가 아니다 | 휴무일은 **데이터**다. 시험이 오늘을 활성 휴무일로 심고 지운다 → 아무 날 아무 시각에나 성립. 게이트가 루프 뒤에 잔여 0건·Seed 2건을 확인 |
| 2 | `CWR-006`·`CWR-009` 를 "배타적" 으로 단언 | 둘 다 **PM Work** 를 써서 그렇게 보였을 뿐이다. 마감은 시간대 마다 다르다 | `CWR-009` 를 **AM Work**(마감 11:00)로 옮겼다. `11:10~15:50` 에 둘 다 판정된다. 이전엔 `CWR-009` 가 `16:00~18:00` 에만 돌아 일반 회귀가 닿지 못했다 |
| 3 | `G09` 의 "Parameter 99 `EXCEPT` 양방향" 을 **아무도 판정하지 않았다** | 계약 시험은 RS0 만 본다. 개수는 문서에만 있었다 | `SCH-019` 신설 — SP 별 Parameter 를 `EXCEPT` 양방향으로 맞추고 합계 99 를 확인한다. 합계만 세면 한 SP 에서 늘고 다른 SP 에서 준 드리프트를 놓친다 |
| 4 | `G00`·`G01`·`G05` 가 **회귀 밖**에 있었다 | `test.sh` 가 세 스크립트를 부르지 않아 손으로 돌려야 했다 | 셋 다 `test.sh` 에 넣었다. 결과와 무관하게 `PASS` 를 찍지 않도록 셋 다 성공했을 때만 한 줄을 남긴다 |
| 5 | `G13-b` 블랙리스트 grep 이 **항상 FAIL** | 실측 27건이 전부 오탐 — 금지를 *설명하는* 주석과 JavaScript 의 `.trim(` 이 `TRIM(` 에 걸렸다 | `verify-tsql-allowlist.sh` 로 옮겨 `.sql` 만, 주석을 지운 뒤 검사. `CREATE DATABASE … COLLATE` 는 DB 정렬 지정이라 제외 |
| 6 | `CON-004` 가 **공허하게 PASS** | `@최종수정일시`·`@차트번호` 를 NULL 로 넘겨 `100` 으로 끝났는데, 최종 상태가 "아무것도 안 바뀜" 이라 판정이 통과였다 | 인자를 채워 실제 경로에 닿게 했다(A 는 `205`, EP-08). 그리고 **로그에 `결과코드 100` 이 1건이라도 있으면 FAIL** — 전 시나리오 공통 가드다 |
| 7 | `sqlcmd -v` 가 값의 콜론에서 인수를 자른다 | 착수 전에는 보이지 않는다. `-v BarrierTime=13:00:00` 이 접속 전에 죽는다 | barrier 를 `HHMMSS` 로 넘기고 `STUFF` 로 콜론을 끼운다. `WAITFOR TIME` 은 `TIME` 형을 거부한다(`Msg 9815`) → `VARCHAR(8)` |
| 8 | 회귀 요약이 **다른 회차의 로그를 센다** | `artifacts/logs/` 는 누적된다. 옛 RED 회차의 `test_01_red.log` 가 와일드카드에 걸렸다 | `test.sh` 가 자기가 쓴 로그를 `_manifest.txt` 에 남기고 요약은 그것만 읽는다. mtime 비교는 쓸 수 없다 — 회귀 로그가 끝까지 쓰여 가장 새 파일이 되므로 모든 시험 로그가 걸러진다 |

| 9 | **없는 구멍을 한계로 등재했다** | `§43-15`(접수취소 후 당일 재접수 불가)를 "복구 경로가 없다" 로 적었는데, 16:00 이후엔 접수완료도 당일예약도 `304` 라 **그 시간에 오접수가 생길 수 없고**, 다음 날 이후 예약은 `마감시각 NULL` 로 열려 있다(실측). 각 규칙은 문서화돼 있었고 **그 조합을 잘못 읽었다** — 시나리오를 끝까지 실행해 보지 않고 규칙 두 개를 머리로 합친 결과다 | 항목을 취소선 처리하고 이 줄을 남긴다. 규칙을 조합해 결론을 내면 **그 조합을 실제로 돌려 본다** |

`[I]` **공통 교훈.** `SKIP`·`NOT RUN`·"오탐이라 무시" 는 전부 *검증하지 않음*의 다른 이름이고,
집계에서 실패로 세지 않으므로 게이트를 green 으로 유지한다. 이 여덟 건은 모두 그 형태였다.

---

# 45. 구현 승인 판정

| 판정 항목 | 결과 |
|---|---|
| Baseline 무결성 | **VERIFIED** — 6개 파일 중 5개 hash 일치, `03`은 `D4-001`로 승인 처리 |
| XLSX 전수검증 | **PASS** — 7 Sheet / Used Range 7/7 / 고정값 전건 |
| 환경조사 | **완료** — SQL Server 2025 Express, KST 540, sqlcmd 인코딩·exit code·T-SQL 동작 9건 실측 |
| 필수 grilling 결정 | **13건 전부 확정** (`D4-001`~`D4-013`) |
| 미승인 가정 `[A]` | **0건** |
| Implementation Blocker | **0건** |
| 6 Table 추적 | **6/6** (§11) |
| 4 TVF 추적 | **4/4** (§17) |
| SP 추적 | **20/20** (§18) |
| 8 Write SP Transaction 추적 | **8/8** (§21.2) |
| 8 Write SP Lock 추적 | **8/8** (§22 · §24) |
| `05` 적대적 테스트 계약 추적 | **§33.4에 12건 보완 후 전건 매핑** |
| 5중 적대적 검토 | **수행 완료** — 확정 36건 전부 본문 반영, 반증 5건 기록 (§44.5) |
| 00~05 변경 | **0건** |
| WinForms 변경 | **0건** |

| **실행검증** | **완료 (2026-09-07)** — §42 Gate 16종 전부 실측. `G12` 만 사용자 결정으로 범위 밖 |
| 배포 | `Deploy.sql` 전체 exit 0 · 빈 DB 에서도 exit 0 (`RBD-003`) |
| 재현성 | 연속 2회 Rebuild 정렬 덤프 **diff 0줄** (`RBD-005`) · Deploy 단독·Procedure 단독 재실행도 동일 |
| 객체 | Table 6 / TVF 4 / SP 20 / Sequence 1 / PK 6 / FK 2 / UQ 2 / UX 1 / NCI 5 / CHECK 26 / Default 10 |
| 계약 | Parameter **113** `EXCEPT` 양방향 (`SCH-019`) · Result Set 계약 전건 일치 |
| 동시성 | `CON-001`~`008` **8/8** · `applock rc=1` 실측 · `Msg 1205`·`50002`·`50001`·`2627` 각 0건 |
| 기준선·WinForms | 실행검증 기간 중 변경 **0건** (`verify-baseline.sh` 6/6 · WinForms 태그 diff 0줄) |

> **판정: `FINAL / GO (v1.0)`** — SQL 실행검증을 마쳤다. §42 의 Gate 판정은 전부 실제 실행 증거를 근거로 하며,
> 실행하지 않은 것은 `NOT RUN` 으로 남겼다(`G12` Security · `RBD-001` · `G13(c)`). `SKIP` 을 `PASS` 로 승격하지 않았다.
>
> **남은 `NOT RUN` 은 셋이며 모두 이유가 기록돼 있다.**
>
> ```text
> G12 Security      2026-09-07 사용자 결정으로 범위 밖 (§32 · §43-7). SEC-010 만 범위 안이며 PASS
> RBD-001           인스턴스가 1개뿐이라 잘못된 서버명(50020) 음성 시험이 구조적으로 불가 (§43-8)
> G13 (c)           §9.2 허용목록 준수는 자동 판정 불가 — REVIEWED 로만 기록 (§9.3 · §43-10)
> ```
>
> Phase 5 는 §32.3 의 배포 절차(App.config 키 · login 매핑)를 인수한다.

## 44.7 v0.2 → v0.3 — 재검토 3종이 찾은 결함 `[X 수정 2차]`

v0.2 는 확정 결함 36건을 **스펙에만** 반영하고 `plans/` 를 v0.1 상태로 두었다. 독립 재검토 3종(계약 정합성 / 실행가능성 / 문서 정합성)이 서로 모르는 채 같은 진단에 도달했다.

> **표에 "반영 완료" 라고 적는 것과 문서를 고치는 것은 다르다.**

### 근본원인

Test ID·건수·계약 수치가 스펙과 9개 계획 문서에 **중복 기재**돼 있었다. 사본이 여럿이면 반드시 어긋난다.

| 값 | 스펙 | 계획 | 실측 |
|---|---:|---:|---:|
| CHECK 제약 | 21 | 21 | **22** |
| DEFAULT 제약 | 12 | 12 | **14** |
| `SED` | 11 | 10 | 11 |
| `SEC` | 1 | 1 | 1 |
| `CON` | 8 | 7 | 8 |
| `SCH` | 19 | 16 | 19 |

→ **§45.2 카탈로그를 유일한 출처로 삼고, 계획은 건수를 다시 적지 않는다.** `tools/verify-docs.js`(§45.3)가 기계적으로 강제한다.

### 실측으로 확인한 결함

| # | 결함 | 실측 증거 | 조치 |
|---:|---|---|---|
| 1 | `verify-no-secret.sh` 가 **secret 을 놓친다** | BOM 포함 UTF-16 7.6MB 로그에서 secret 이 **앞쪽**이면 `grep -q` 가 조기 종료 → `iconv` SIGPIPE(141) → `pipefail` → `if` 거짓 → `PASS SEC-010` 출력. 뒤쪽이면 HIT. **위치 의존** | `grep -icE` 로 개수를 변수에 받는다. 검사 범위를 배포 원본까지 확장 |
| 2 | G13-b `grep -rniE '\bTRIM\('` 이 `verify-contract.js` 의 `.trim()` 에 매치 | 재현 확인 — `.` 이 단어경계라 `-i` 와 함께 걸린다. `T37` 이 **결정적으로** FAIL | `--include='*.sql'` |
| 3 | `tee` 의 `$?` 가 *"항상 0"* 이라는 §41.1 서술 | `set -o pipefail; false \| tee` → **`$?=1`**. `pipefail` 하에서는 거짓 | 조건을 명시. 직접 redirection 권고는 유지 |
| 4 | `database_id <= 4` 가드(`50012`·`50022`) | `Net461MvpSample` = **id 5**. 사용자 DB 는 항상 ≥5 → 어떤 입력으로도 발화 불가. 초안이 `50011` 을 죽은 코드라 지적하며 같은 형태를 두 개 남겼다 | `MachineName` 대조 / `DROP DATABASE` 직전 계약 밖 Table 0개 확인 |
| 5 | `PRINT 'INFO applock rc=…' + ' res=' + @Res` | `HC\|CHART\|C000123` 은 **차트번호 평문**이고 `HC\|PAT\|` 는 내부 식별자다. §41 *"로그에 실제 개인정보를 남기지 않는다"* 위반 — **v0.2 가 새로 만든 위험** | `rc` 만 출력. 경합 증거(`rc=1`)에 자원명은 불필요 |
| 6 | `T017` 생년월일 상대식 | `DATEADD(YEAR,-19,…)` 는 만 **19**세를 만든다(만 18세가 아니다). 배포 시각에 따라 값이 달라져 §15.5·G14 위반 | 고정 리터럴 `2007-11-18`. 실측: `2026-11-17` → 18세, `2026-11-18` → 19세 |
| 7 | `RUL-N04`(여 44) · `RUL-N08`(남 54) 에 프로필 없음 | 기존 Fixture 17종에 해당 나이·성별 조합이 없어 술어를 끝까지 시험할 수 없다 | `T018`(1982-10-01 F) · `T019`(1972-10-01 M) 추가 |

### 계획 미반영 (스펙만 고쳐졌던 것)

`#4` SSN/CHART 항상 획득 · `#5` NEX 상한 · `#7` 자동 차트번호 잠금 · `#11` `INSERT..EXEC` **54곳** · `#14` `SCH-017`/`018` · `#16` 13→15 SP · `#17` `allowed-codes.json` · `#21` 업무시간 가드 · `#23` 자원 선점 · `#24` `CORRUPT-3/4/5` · `#29` Test ID 9건 — 전부 계획에 반영했다.

### 소비처에만 있고 생산 Task 가 없던 것

`T016`·`T017`·`F020`·`CONC1`·`CONC2` Fixture, `otherdb_before.txt`, `tools/allowed-codes.json` — 전부 생산 Step 을 만들었다.

### 기준선에서 기계 생성한 값

사람이 옮겨 적던 표는 전부 기준선에서 추출했다 — `SCH-015` 컬럼 **47행**(`04` §8), `SCH-017` DEFAULT **8행**, `SCH-018` NCI Key, Parameter **95행**(`05` §7~§12), `allowed-codes.json` **15 SP**(`05` §13, 범위 표기 전개).

### 3차 — 게이트 통과 후 추가로 찾은 결함 (게이트가 잡지 못하는 부류)

`tools/verify-docs.js` 가 PASS 14 / FAIL 0 이 된 **뒤에** 5건이 더 나왔다. **게이트는 문서 정합성을 보지, 시험이 의미적으로 성립하는지는 보지 못한다.**

| # | 결함 | 증거 | 조치 |
|---:|---|---|---|
| 1 | `RUL-A09`·`CWR-024` 가 `T014`(**남** 만 56세)로 `412` 를 기대 | `NEX-05`(EX012) 술어가 `성별='F'` 를 요구 → T014 에 EX012 가 없어 **`412` 대신 `0`** 이 나오고 테스트가 **조용히 통과**한다 | `T14b` 가 `T011`(여 54)의 RCP Work 를 만들고 그것을 쓴다 |
| 2 | `RWR-031` 이 `T015`(**남** 46)로 `412` 를 기대 | 같은 이유로 어떤 예약일로도 EX012 가 생기지 않는다 | `T020`(1972-11-20생 여) 신설. 실측 `2026-11-17` 만 53세 → `2026-11-20` 만 54세 |
| 3 | `CORRUPT-5`·`RWR-034` 가 `T011` 의 RSV Work 를 찾음 | **`T011` 에 Work 를 만드는 코드가 어디에도 없다** → `@Wx` NULL → 사전조건 실패로만 끝난다 | `F002` 의 Work 에 심는다 |
| 4 | `SEC-005` 의 `UPDATE … SET [테이블명] = NULL` | 실측: 없는 컬럼 → **`Msg 207`**(컴파일), 없는 테이블 → `Msg 208`. 둘 다 실행시점 권한검사(`229`)보다 **먼저** 난다 → 권한이 올바로 막혀 있어도 `SEC-005` 가 FAIL | 실존 컬럼명을 `EXECUTE AS` **밖**에서 확보해 쓴다 |
| 5 | `T01` 의 `git add docs/baseline` | 다른 세션이 `docs/baseline/output/` 에 xlsx·pptx 를 쓰는 중이다. 디렉터리를 통째로 add 하면 기준선 tag 에 진행중 파일이 섞이고 **쓰기 도중의 blob** 이 봉인될 수 있다. `verify-baseline.sh` 는 6개 파일만 대조하므로 그 손상을 잡지 못한다 | 기준선 6개 파일만 경로로 명시 + `.gitignore` 에 `docs/baseline/output/` |

`[I]` 1~3은 전부 **같은 오해**에서 나왔다 — *"OPT0n 을 요청하면 중복 판정이 일어난다"*. 실제로는 §13 Seed 19행 중 `국가검사규칙코드` 와 `추가검사코드` 를 동시에 가진 행이 `EX012` **하나뿐**이라 `412` 는 그 조합으로만 발생한다(§17.2a). `V13` 이 이 부류를 기계적으로 막는다.

`[I]` `INSERT … DEFAULT VALUES` 는 바인딩할 컬럼명이 없어 컴파일을 통과하므로 실행시점 `229` 가 먼저 난다 — `SEC-005` 의 `INSERT`·`DELETE` 경로는 안전하고 `UPDATE` 만 문제였다.

### 4차 — R3 재봉인 통과 후 찾은 결함 (게이트가 보지 않던 R2 수치 사본)

게이트 3종이 green 이 되고 실물 배포까지 끝난 **뒤에** 5건이 더 나왔다. **`V08` 은 `CHECK n개`·`DF n` 패턴만 보므로 산문에 흩어진 수치 사본을 아무도 검사하지 않았다.**

| # | 결함 | 증상 | 조치 |
|---:|---|---|---|
| 1 | `plans/08` `VER-005` 가 Foreign Key 수를 R2 값으로 단언 | **실행되는 조건문**이다. `08_Verify.sql`(당시 이름 `09_Verify.sql`)은 `Deploy.sql` 이 매 배포 마지막에 부르므로 R3 배포 검증이 무조건 FAIL 했을 것이다 | `4` 로 교체 |
| 2 | `plans/01` `Produces:` 요약의 Foreign Key 수 | 서술이라 실행을 깨뜨리지 않지만 계획을 읽는 사람이 R2 수치를 믿는다 | `4` 로 교체 |
| 3 | `plans/08` `@ExpParam` 주석의 행 수 | 표 자체는 `@조작자명` 8행을 더해 옳은데 주석만 옛 수치였다 | `95행` 으로 교체 |
| 4 | 이 문서 §44 "기준선에서 기계 생성한 값" 의 컬럼·DEFAULT·Parameter 수 | 초안 §13.2 가 명시적으로 지시한 항목인데 라인 지정 편집에서 빠졌다 | `47` / `8` / `95` 로 교체 |
| 5 | `plans/2026-09-04-…` 의 Test ID 총건수 | `V12` 는 §45.2 를 참조하는 줄을 통과시키므로 그 줄의 건수 자체는 검사되지 않았다 | `242건` 으로 교체 |

`[I]` **근본원인은 게이트 부재다.** 1~5 는 전부 "R2 값과 R3 값이 짝인 수치가 산문에 사본으로 남은 것"이고, `V15` 가 이 부류를 기계적으로 막는다. RED 5종(테이블명·기준선 ID·상태값·제약 수·Parameter 수 재주입)으로 포획을 확인했다.

`[I]` **`V15` 의 예외는 정확한 문자열로 고정한다.** *"같은 줄에 `R2` 가 있으면 통과"* 같은 휴리스틱은 그 문자열만 적으면 뚫리는 우회 통로가 된다. 현재 예외는 R2 시점을 서술하는 역사 기록 2종과, R2·R3 와 무관하게 *"만들지 않는다"* 로만 등장하는 테이블 이름 2종뿐이다.

### 기준선 위반

**0건.** 재검토 3종이 독립적으로 `00`/`04`/`05` 계약 전건을 대조했다. `UPDATE_접수완료` 잠금 확장은 `05` §14 를 상한이 아닌 **하한**으로 읽는 것이 타당하다 — `05` §0.3 이 잠금 자원·순서를 `06` 에 위임했고, §21 변경통제 금지목록에 잠금영역이 없으며, `04` §3.10 은 기법이 아니라 **결과조건**만 고정한다. `00` RP-03·RP-06·EP-08 이 `05` 보다 상위이므로 오히려 확장하지 않는 쪽이 위반이다.

---

## 45.2 Test ID 카탈로그 — **단일 출처** `[X 수정 2차]`

`[X]` **초안의 구조적 결함**: Test ID 목록과 건수가 스펙과 9개 계획 문서에 **중복 기재**돼 있었다. 사본이 여럿이면 반드시 어긋난다 — 실제로 `SED` 10↔11, `SEC` 10↔11, `CON` 7↔8, `SCH` 16↔18, `CHECK` 21↔22, `DF` 12↔14 가 전부 이 이유로 틀어졌고, §33.4가 신설한 9개 ID는 어느 계획에도 배치되지 않았다.

**이 표가 Test ID의 유일한 출처다.** 계획 문서는 건수를 **다시 적지 않고** 이 표를 참조한다. 완료조건은 *"PASS 20건"* 이 아니라 *"§45.2 의 `SEL` 전건 PASS"* 로 쓴다.

| Prefix | 범위 | 건수 | 산출 파일 | 검증 대상 | Gate |
|---|---|---:|---|---|---|
| `PRE` | `001`~`006` | 6 | `deploy/00_Preflight.sql` | 배포 안전가드 `50010`~`50015` (§8.3) | G03 |
| `SCH` | `001`~`019` | 19 | `tests/01_Schema_Tests.sql` | 6 Table · PK/FK/UQ/UX/NCI · 53컬럼 · 제약 26 · Default 10 · NCI Key · **SP 별 Parameter(합계 113)** (§34) | G05 |
| `SED` | `001`~`011` | 11 | `tests/02_Seed_Tests.sql` | `검사코드` 19행 · `휴무일` 2행 · AEX 7건 Active (§13·§14) | G07 |
| `SSN` | `001`~`006` | 6 | `tests/02_Seed_Tests.sql` | 실제 주민등록번호 미사용 — 체크디지트 전건 무효 (§16.2) | G12 |
| `RUL` | `T01`~`T12` `N01`~`N12` `A01`~`A10` `G01`~`G08` `D01`~`D09` | 51 | `tests/03_Rule_Tests.sql` | 4개 TVF 결정적 경계 — 마감시각 · NEX 술어 · AEX 판정순서 · 휴무일 · `DATEFIRST` 불변 (§35) | G08 |
| `SEL` | `001`~`025` | 25 | `tests/04_Select_SP_Tests.sql` + `tests/contract/` | 8개 SELECT SP — DB 상태 단언은 `tests/04`, RS0 결과코드·RS 형상은 `contract/` (§33.1a) | G09 |
| `PWR` | `001`~`014` `020`~`029` `030`~`032` | 27 | `tests/05_Patient_Write_Tests.sql` | `INSERT_수검자` · `UPDATE_수검자정보` (§33.4) | G06·G09 |
| `RWR` | `001`~`012` `020`~`034` `040`~`044` `050`~`052` | 35 | `tests/06_Reservation_Write_Tests.sql` | `INSERT_예약` · `UPDATE_예약변경` · `UPDATE_예약취소` (§33.4) | G06·G09 |
| `CWR` | `001`~`011` `020`~`026` `040`~`044` `050`~`052` | 26 | `tests/07_Reception_Write_Tests.sql` | `UPDATE_접수완료` · `UPDATE_접수추가검사` · `UPDATE_접수취소` (§33.4) | G06·G09 |
| `RBK` | `001`~`008` | 8 | `tests/08_Rollback_Tests.sql` + `tools/verify-docs.js` | 부분저장 차단 · `XACT_ABORT` · `@@TRANCOUNT` 복원 (§21.3). `RBK-008`(실패 응답 RS 개수)만 T-SQL 로 셀 수 없어 `V18` 이 `expected-contracts.json` 을 정적 대조한다 | G10 |
| `CON` | `001`~`008` | 8 | `tests/09`~`12` + `scripts/concurrency-test.sh` | 2세션 경합. `rc=1` 증거 ≥1 · `Msg 1205`·`50002` 각 0건 (§38) | G11 |
| `SEC` | `010` | 1 | `scripts/verify-no-secret.sh` | 배포 원본·로그·보고서 secret 0건 — **SQL 이 아니라 셸** | G12 |
| `RED` | `001`~`004` | 4 | `scripts/verify-red.sh` | **음성시험** — 폐기용 DB 에서 "객체가 없거나 값이 틀리면 시험이 실제로 실패하는가" 를 판정한다 (§40a) | G15 |
| `OFF` | `308`~`309` | 2 | `tests/contract/OFF-*` | `309` 는 업무시간 밖에서만 — `PWR`/`RWR`/`CWR` 과 배타적이라 어느 시각에 돌려도 한쪽이 판정된다. `308` 은 시험이 휴무일을 심어 **언제나** 판정된다 (§33.2a) | G09 |
| `VER` | `001`~`007` | 7 | `deploy/08_Verify.sql` | 배포 직후 객체 수량 자체검증 | G03 |
| `RBD` | `001`~`011` | 11 | `tests/14_Clean_Rebuild_Verify.sql` | Clean Rebuild 재현성 (§40) | G14 |
| **합계** | | **247** | | | |

`[I]` 범위가 전부 **연속**이다. 결번이 생기면 그 자체가 결함이다 — 계획에서 ID를 폐기할 때는 이 표에서도 지우고 뒤를 당기지 말고, 폐기 사유를 §45.4에 적는다.

`[I]` `PWR-006` 은 폐기하지 않고 `PWR-013`·`PWR-014` 로 **분할**했다(§33.4). `020`번대는 `UPDATE_수검자정보`, `040`번대는 취소·역방향 시나리오다.

## 45.3 문서 정합성 게이트 `tools/verify-docs.js` `[X 수정 2차]`

위 표가 지켜지는지 **사람이 확인하지 않는다.** `node tools/verify-docs.js` 가 판정하며 `test.sh` 와 `T37` 이 호출한다.

| ID | 검사 | FAIL 조건 |
|---|---|---|
| `V01` | 코드펜스 짝 | 열림/닫힘 불일치 |
| `V02` | placeholder | *"같은 패턴으로"* · *"그대로 옮긴다"* · *"나머지 N개도"* · `TBD`/`TODO` 잔존 |
| `V03` | Test ID 생산↔소비 | §45.2 카탈로그 ↔ 계획 배치 집합의 **양방향 차집합 ≠ 0** |
| `V04` | Fixture 생산↔소비 | `차트번호 = 'X'` 로 지목되지만 생성 코드가 없는 Fixture |
| `V05` | 금지 패턴 | `INSERT..EXEC` · `CURSOR` · `FOR XML PATH` · `\| grep -q` · `\| tee` · `iconv -f UTF-16LE` · `\|\| echo 0` · 오케스트레이터의 `set -e` |
| `V06` | 스펙 버전 참조 | 계획이 현재 스펙 버전이 아닌 값을 참조 |
| `V07` | 오류번호 | §20 표에 없는 `5xxxx` 사용 |
| `V08` | 제약 수치 | 문서의 CHECK·DEFAULT 개수 ≠ 기준선 `04` 실측 |
| `V09` | 산출물 | 계획이 만들지만 §7 트리에 없는 파일 |
| `V10` | 개인정보 로깅 | 잠금 자원명(`차트번호`·`수검자ID` 포함)을 `PRINT` 로 출력 |
| `V11` | 객체명 | 계획이 참조하는 SP/TVF/Sequence 가 스펙에, Table 이 기준선 `04` §7 에 없음. R3 에서 테이블명이 한글이 되어 접두사 정규식이 fail-open 하므로 `dbo` 스키마 한정 참조를 세고 R2 접두사 잔존은 별도로 잡는다 |
| `V12` | 완료조건 건수 | 계획의 완료조건이 §45.2 를 참조하지 않고 Test 건수를 다시 적음 |
| `V13` | `412` 프로필 | `412` 를 기대하는 블록의 문맥에 `EX012` 보유 Fixture(여 54/60/66)가 없음 (§17.2a) |
| `V14` | 계획 SQL 의 식별자 | 계획의 ```sql 펜스 안 대괄호 식별자가 기준선 `04` §8 · `05` · 이 스펙 · `sys` 뷰 · 같은 파일의 파생 별칭 어디에도 없음 |
| `V15` | R2 수치 사본 | 산문·SQL 에 R2 값(테이블명·기준선 ID·상태값·제약 수·컬럼 수·Parameter 수·Test 건수)이 남아 있음. 정당한 역사 서술은 정확한 문자열로 예외 고정. **구분자 문자열 안의 수치는 패턴이 보지 못하므로 `INVENTORY\|…` 지문은 위치로 짚어 기준선 `04` 의 Foreign Key 실측과 대조한다** — 칸 수가 달라지면 위치 가정이 깨진 것이므로 형상 자체를 FAIL 로 낸다(fail-open 금지) |
| `V16` | `N` 없는 리터럴의 문자 손실 | `Korean_Wansung`(CP949)에 없는 문자를 `N` 접두사 없는 SQL 리터럴이 담고 있음. varchar 리터럴로 해석되어 그 문자만 `?` 로 바뀌는데 **한글은 살아남고 exit code 도 0** 이라 실행으로는 드러나지 않는다. 자모집합은 하드코딩하지 않고 `TextDecoder('euc-kr')` 전수 디코드 ∪ 한글 음절 범위로 역산한다 — `—`·`–` 는 불가인데 `―`·`·`·`→`·`§`·`…`·`≥` 는 가능이라 손으로 나열하면 틀린다. 판정 수단이 없으면 PASS 가 아니라 FAIL 이다 |

`[I]` 이 게이트가 없으면 이번 라운드에서 확인된 실패가 반복된다 — 확정 결함 36건을 스펙에만 반영하고 계획을 그대로 둔 채 *"반영 완료"* 로 표에 적었고, 재검토 3종이 독립적으로 같은 미반영 11건을 찾아냈다. **표에 적는 것과 문서를 고치는 것은 다르다.**

## 45.1 v0.1 → v0.2 변경 요약

`v0.1`은 5중 적대적 검토에서 **36건의 확정 결함**이 나와 `IMPLEMENTATION READY` 판정을 철회했다. `v0.2`는 그 전부를 반영했다.

```text
설계 결함     10건  가장 큰 것은 §24.2 — 접수완료(RSV→RCP)가 인덱스 키를 뒤로 이동시켜
                   정원·중복·EP-08 COUNT 를 과소집계시킬 수 있다는 사실을 놓쳤다.
                   접수완료의 잠금을 PAT → WORK → SLOT 으로 확장했다.
검증층 결함   20건  테스트가 스펙의 약속보다 적게 검사했다. 인벤토리 검증을 전부
                   EXCEPT 양방향으로 바꾸고, INSERT..EXEC 기반 테스트를 폐기했다.
셸·도구 결함   6건  iconv BOM / set -e / tee / grep -c / sqlcmd -w. 전부 실측 확인 후 §41.1 에 관용구로 고정.
```

계약 자체(객체명 15/4/7/1, Parameter 95, Result Set, ResultCode 38, Seed 19행, 스키마 48컬럼)는 두 검토자가 독립적으로 기준선 전건 일치를 확인했고 변경이 없다.

다음 단계는 구현계획(`plans/`)의 동일 수정이며, 실제 SQL 구현은 사용자의 별도 실행 승인 이후에 시작한다.

# 46. R4 한글화 (2026-09-08) `[R4]`

`05` v3.0 이 SP 16개 이름·Parameter 99건·Result Set 컬럼 전건·TVF 4개를 한글로 바꿨다.
이 문서는 그 표기를 따라갔을 뿐이며 **잠금·Transaction·Seed·시험 계약의 내용은 한 줄도 바뀌지 않았다.**

## 46.1 바뀐 것

```text
SP 16개 이름         USP_HC_{한글업무명}_{한글동사}      05 §1.2 · §1.3
Parameter 99건        전부 한글                          05 §1.6
Result Set 컬럼       전부 한글                          05 §3.1 · §7~§13
TVF 4개               Parameter·반환 컬럼 한글           05 §6
SP 내부 지역변수      전부 한글 (사용자 결정 D4)          계약이 아니지만 함께 옮겼다
RS0.오류항목          VARCHAR(50) -> NVARCHAR(50)        R4 에서 타입이 바뀐 계약은 이 하나뿐
deploy/01_Schema.sql  옛 SP 16개 DROP IF EXISTS 추가      기존 DB 배포가 32개를 세지 않도록
```

## 46.2 바뀌지 않은 것

```text
USP_HC_ · UFN_HC_ · SEQ_HC_ 접두사      게이트 5개가 name LIKE 'USP[_]HC[_]%' 로 고른다
TVF 4개 이름                            이미 한글이었다
sp_getapplock 의 @Resource·@LockMode·@LockOwner·@LockTimeout
                                        시스템 Parameter 이름이라 바꾸면 호출이 깨진다
잠금 자원명 문자열 형식 HC|SLOT|yyyyMMdd|AM
ResultCode 값 체계 · C# DbCode Enum 이름 · DTO 프로퍼티 이름
Test ID 카탈로그 247건 · 계약 시나리오 110종 · 개수를 세는 기대값 전부
```

## 46.3 게이트

```text
tools/r4-rename-map.json      개명의 단일 출처. 손으로 치환하지 않는다
tools/r4-rename.js check      C1 빠짐 · C2 금지이름 · C3 이름 합쳐짐 · C4 예산 · C6 SP 전건
tools/verify-rs-contract.js   [신설] 기대값의 Result Set 컬럼을 기준선 05 의 **표**와 직접 대조
                              §44.8 이 등재한 "기대값이 구현과 같이 틀린다" 의 축이다
scripts/verify-csharp-call.sh [신설] csc.exe 로 Probe.cs 를 컴파일해 실제 ADO.NET 호출
```

`V14` 는 기준선 05 에서 알려진 식별자를 모으는 정규식이 ASCII 전용이라 한글화 뒤
**한 건도 못 모으고 fail-open** 할 뻔했다. 같은 커밋에서 `[A-Za-z가-힣]` 로 넓혔다.

## 46.4 실행 증거

```text
2026-09-08 04:37  창 밖 회귀   exit 0 · PASS 185 · FAIL 0 · SKIP 78 · NOT RUN 9
                  R3 창 밖 기준선 PASS 185 · FAIL 0 · SKIP 78 · NOT RUN 9  -> 동등
2026-09-08 12:59  창 안 회귀   exit 0 · PASS 357 · FAIL 0 · SKIP 2  · NOT RUN 1
                  R3 창 안 기준선 PASS 357 · FAIL 0 · SKIP 2  · NOT RUN 1  -> 동등
2026-09-08 05:12  창 밖 재회귀  exit 0 · PASS 187 · FAIL 0 · SKIP 78 · NOT RUN 9
                  +2 는 R4-2·R4-3 게이트를 test.sh 안으로 넣은 것이다 (아래)
                  CON-001~008 8/8. 001~005·007 에서 applock 대기 후 획득이 실측됐다
                  (경합이 실제로 일어났다는 증거다 — §38.4)
                  verify-baseline 6/6 · verify-docs 21/0 · contract 110/110
                  verify-rs-contract 170개 Result Set 전건 = 기준선 05
                  verify-schema-doc 5/0 · r4-rename check FAIL 0
                  csharp-probe CS-001~016 전건 PASS
```

두 회차의 `SKIP`·`NOT RUN` 이 R3 와 **같은 항목**이다. 한글화가 판정 대상을 줄이지 않았다.

```text
창 안 SKIP 2      OFF-309-01 · OFF-309-02   정의상 업무시간 안에서는 309 가 나올 수 없다
창 밖 SKIP 78     Write SP 계약 시나리오     308/309 를 업무 Rule 보다 먼저 판정한다
NOT RUN 1         RBD-001                   인스턴스가 1개라 잘못된 서버명 50020 을 발화시킬 수 없다
```

`[!]` **두 게이트를 처음에는 손으로 돌렸다.** 회귀 밖의 증거는 다음 회차에 썩는다 —
`G13-b`·`G00`·`G01`·`G05` 가 전부 같은 방식으로 한 번 실패했다(§45.3 주석). 그래서
`verify-rs-contract` 와 `verify-csharp-call` 을 `test.sh` 안에 넣었고, 그것이 `185 -> 187` 이다.
창 안 회차 `357` 은 넣기 **전**의 수치이므로 다음 창 안 회귀는 `359` 가 되어야 맞다.

`[!]` **창 안 회귀는 머신 시각을 옮겨 판정했다.** 2026-09-08 04:57 에 `Set-Date -Adjust +8h`
(사람이 실행)로 12:58 로 옮기고 셸·SQL Server 양쪽 시각을 확인한 뒤 돌렸으며, 끝난 뒤 같은
크기로 되돌렸다. 코드를 고치는 우회는 쓰지 않았다 — `rebuild` 가 매 시나리오마다 TVF 를
되돌리므로 성립하지 않고, 배포 원본을 고치면 **다른 제품을 시험한 `PASS`** 가 된다.
