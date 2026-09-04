# Phase 4 R3 개편 — `04` 재설계 초안 작성 후 중단

작업 디렉터리: `D:\AIDEV\HealthCheckupReservationReception\database`
선행 세션: `database-38` (T01~T07 실행 완료 + R3 개편 grilling 완료, SQL·문서 모두 R2 상태로 봉인)
복원점: git tag `phase4-r2-t07-complete` (branch `phase4-database`, commit 9개)

---

## 1. 목표

**`04_DB_Design.md` 의 R3 초안을 별도 파일로 작성하고, 그 초안을 grilling 한 뒤 멈춘다.**

`03`·`05`·`06` 스펙·계획 9개·`verify-docs.js`·실물 재배포로 넘어가지 않는다. 초안이 흔들린 채 하위 문서를 고치면 전부 두 번 쓰게 된다.

R3 개편의 **설계 결정은 이미 끝났다**(§3). 이 세션은 그 결정을 문서로 옮기고, §4의 미결 3건을 초안 안에서 해소하고, 초안 자체를 압박하는 것이 전부다.

## 2. 지금 사실인 것

| 항목 | 상태 |
|---|---|
| git | ROOT `D:\AIDEV\HealthCheckupReservationReception` 에 저장소 존재. branch `phase4-database`, commit 9개, remote 없음 |
| tag | `baseline-HC-RSV-RCP-20260903-R2`(기준선 봉인) · `phase4-r2-t07-complete`(T01~T07 완료) |
| git config | repo-local `JS <JS@DESKTOP-DP7KRE4>` + `core.autocrlf=false`. global·system 미변경 |
| 기준선 문서 | `docs/baseline/00`~`05` — **R2 원본 그대로. 아직 한 바이트도 안 바뀌었다** |
| 스펙 | `docs/phase4/06_DB_Transaction_Security_Seed_CANDIDATE.md` v0.3 — R2 기준 |
| 계획 | `docs/phase4/plans/` 9개 — R2 기준 |
| 배포된 DB | `.\SQLEXPRESS` 의 `HealthCheckupReservationReceptionDb` 에 **R2 스키마 7테이블이 올라가 있다**. R3 확정 시 폐기 대상 |
| 게이트 | `node tools/verify-docs.js` → PASS 15 / FAIL 0 · `./scripts/verify-baseline.sh` → 6/6 · `./scripts/verify-winforms-unchanged.sh` → 변경 0건. **세 개 모두 통과 상태** |
| 시험 | `tests/01_Schema_Tests.sql` → PASS 16 / FAIL 2 (`SCH-013`·`SCH-014` 는 TVF·SP 미구현이라 정상) |

**먼저 읽을 것** (순서대로):

1. `CLAUDE.md` — 이 디렉터리의 경계·금지사항·실행 규칙
2. `docs/prompts/2026-09-04-session-02-Phase4-DB-Implementation-Start.md` — T01~T07 인계문서
3. `docs/baseline/04_DB_Design.md` — 개편 대상 원본 (1650줄)
4. `docs/baseline/05_DB_Rule_SP_Contract.md` — SP·TVF 계약. R3 에서 대부분 유지된다
5. `docs/phase4/06_DB_Transaction_Security_Seed_CANDIDATE.md` §7 파일구조 · §9.2 허용목록 · §45.2 Test ID 카탈로그
6. `docs/phase4/plans/2026-09-04-phase4-database-implementation.md` — Global Constraints·실행 관용구

## 3. 확정된 R3 개편안 — 재논의 대상이 아니다

선행 세션에서 grilling 6라운드로 확정했다. 근거까지 함께 적으므로 **초안에 그 근거를 옮겨 적는다.**

### 3.1 테이블 6개

| 신규명 | 원본 | 확정 근거 |
|---|---|---|
| `수검자` | `INFO_PATIENTS` | 01 정책 EP-01~10 · 03 "수검자 관리 Tab" · SP 5개가 전부 `수검자` |
| `예약접수` | `INFO_CHECKUP_WORKS` | SP `USP_HC_SELECT_예약접수목록`·`_예약접수상세` · 프로그램명 "검진 예약·접수 관리" |
| `검사항목` | `INFO_CHECKUP_WORK_EXAMS` | 05 RS2·RS3·RS4·RS5 의 "국가검사항목 / 추가검사항목". 05:1052 가 *"실제 저장된 `ExamSourceCode='NEX'`만 반환한다"* 고 명시 |
| `검사코드` | `MST_EXAM_ITEMS` | 04 §4.6 제목 *"공통 검사코드 19종"* · 04:652 *"검사코드와 NEX/AEX 역할의 단일 Seed 원천"* |
| `휴무일` | `MST_HOLIDAYS` | |
| `변경이력` | 신설 | |
| ~~`INFO_PATIENT_EXAM_EXCLUSIONS`~~ | **삭제** | → `수검자` BIT 컬럼. NEX-03 판정이 `NOT EXISTS(… x.ExamItemCode='EX010')` 에서 컬럼 비교로 바뀌지만 **의미는 동일**. Seed 1행뿐이고 `'EX010'` 이 규칙 코드에 리터럴로 박혀 범용성은 명목뿐이었다 |
| ~~`HIS_GENERAL_CHECKUP_COMPLETIONS`~~ | **삭제** | → `수검자.LastCheckupDate DATE NULL`. TGT 판정 조인이 1→0회. Fixture 4행이 전부 다른 수검자라 복수 이력은 한 번도 시험되지 않았고, 03 화면이 *"최근 완료연도 2024"* 단일 값만 표시한다 |

`휴무일`·`검사코드`·`검사항목`을 더 줄이려는 시도는 이미 검토해 기각했다 — `검사항목`을 `예약접수`에 합치면 한 건이 8~17행으로 중복되어 **정원 COUNT 가 깨지고**, `휴무일`은 수검자와 무관해 컬럼으로 옮길 수 없다.

### 3.2 `수검자` 18컬럼 (29 → 18)

**삭제 13**
`PassportNumber` `InsuranceNumber` `IsStudent` `IsVIP` `IsReceiveCall` `IsReceiveSMS` `IsReceiveEmail` `IsReceivePost` `IsMarketingConsent` `MConsentDate` `MCancelDate` — 03 이 전부 "UI 미사용"으로 열거(No 4·8·19~27)
`TelNumberS` — 검색 파라미터에도 Result Set 에도 없어 **읽는 계약이 0**. `CK_..._TEL_NORMALIZED`·`CK_..._TEL_DIGIT` 두 제약이 함께 사라진다
`Active` — 05 계약에 **0회** 등장. EP-10 이 삭제 SP 를 금지해 영원히 `1`

**신설 2**
`LastCheckupDate DATE NULL` — 이름이 05 §6.2.2 의 TVF 반환 컬럼과 이미 일치한다
B형간염 제외 `BIT NOT NULL DEFAULT 0` — 컬럼명은 §4.2 에서 정한다

**유지**
`SocialNumber` 는 **`VARCHAR(13)` 평문 그대로**. 128 로 넓히는 안은 얻는 것 없이 `CK_..._SOCIAL_FORMAT` 만 무력화하므로 기각했다. `CelNumberS` 는 `@MobilePhone` 검색과 filtered index 가 실제로 쓰므로 유지한다.

### 3.3 상태 모델 — `CHAR(3)` 5값

`RSV` 예약 · `RCP` 접수 · `FIN` 검진완료 · `CNR` 예약취소 · `CNC` 접수취소

- **`FIN` 은 `CHECK` 허용값에만 존재한다. 전이 SP 를 만들지 않는다.** 사용자가 SSMS 로 직접 `UPDATE` 해서 조회·통계 분류를 시험하는 용도다. 도달 경로가 있으므로 죽은 코드가 아니다
- 따라서 **SP 는 15개 그대로** — 05 §19.2 의 "15개 SP 이름과 수"가 열리지 않는다
- 따라서 **`01_Process`·`02_Function` 도 열지 않는다** — `FIN` 은 업무 단계가 아니라 데이터 상태값이다
- **정원(RP-03)은 `RSV+RCP` 그대로.** `FIN` 을 넣으면 기존 정원 시험(`CON-002` 19/20 등)이 테스트 조작에 흔들린다
- 취소 분리는 SP 를 늘리지 않는다 — `UPDATE_예약취소`(RSV→`CNR`)와 `UPDATE_접수취소`(RCP→`CNC`)가 이미 별개 SP다
- `FIN`·`CNR`·`CNC` 는 종결 상태로 어떤 Action 도 불허(`502 WrongStatus`). 중복 유효예약 판정은 `RSV`·`RCP` 만. Workbench `@Status` 필터는 5값 모두 허용. `StatusName` = 예약·접수·검진완료·예약취소·접수취소
- 노쇼는 도입하지 않는다

HL7 FHIR `Appointment.status`(`booked`·`checked-in`·`fulfilled`·`cancelled`·`noshow`)가 같은 단일 enum 구조를 쓴다는 것이 이 설계의 도메인 근거다.

### 3.4 `변경이력`

- **Write SP 8개 내부에서 명시적으로 기록한다. 트리거를 쓰지 않는다.** → `Trigger 0` 원칙(04·05·06 15곳)과 배포된 `SCH-010` 이 그대로 산다
- 실측: AFTER 트리거는 바깥 `UPDATE` 의 `@@ROWCOUNT` 를 훼손하지 않지만(`SET NOCOUNT ON` 유무 무관), **트리거가 쓴 로그 행은 `ROLLBACK` 과 함께 사라진다.** 실패한 시도를 트리거로 남기는 것은 구조적으로 불가능하다
- 업무 단위 **1행**(시각·조작자·업무명·대상 테이블·대상 키·`ResultCode`). 컬럼 단위 EAV 가 아니다
- **성공 + 업무실패 둘 다 기록한다.** 실패는 `ROLLBACK` 되므로 **Transaction 밖**에서 써야 살아남는다 — 06 §21.1 Write SP 템플릿의 실패 분기 `[1]`·`[4]` 전부에 기록 지점이 붙는다
- 조작자는 `@OperatorName NVARCHAR(50)` 자유 문자열. 직원 Master 를 만들지 않는다(테이블이 9개가 되고 04 §4.1·§10.1·§15.4 가 전부 재작성된다)
- clean-create 로 **매 배포 초기화**한다. `Rebuild.sql` 이 DB 를 통째로 DROP 하므로 "보존"은 deploy 경로에서만 성립하는 반쪽 보장이다
- **`T35` 의 `G14`(2회 rebuild 지문 동일) 비교 대상에서 `변경이력` 을 제외해야 한다.** 로그 행의 시각이 매번 달라 지문이 어긋난다

### 3.5 명명 규칙

| 대상 | 규칙 |
|---|---|
| 테이블 | 한글, 접두사 없음. 전부 2형태소 |
| 컬럼 | **영문 PascalCase 유지** — 05 §1.6 의 Result Set 이름과 1:1 이라 SP 본문에 별칭이 붙지 않는다 |
| 제약 | 접두사 영문 + 본체 한글 (`PK_수검자`, `FK_예약접수_수검자`) |
| SP / TVF | **변경 없음** — 한글 15개 + 4개 |
| 상태값 | `CHAR(3)` 영문 5값 |

실측 확인: 대상 DB(`Korean_Wansung_CI_AS`) 안에서 한글 테이블·컬럼·제약·인덱스 이름이 **대괄호 없이** 생성되고, `EXCEPT` 양방향 대조에 `COLLATE` 가 **불필요**하다. `SCH-002`·`016`·`017`·`018` 형태 모두 통과를 확인했다.

### 3.6 기준선 절차

`03`·`04`·`05` 를 R3 로 재봉인한다. `00`·`01`·`02` 는 열지 않는다. `baseline-HC-RSV-RCP-20260903-R2` tag 는 이력으로 보존한다.

## 4. 이 세션이 결정해야 할 것

### 4.1 제약·인덱스 이름에 컬럼명이 들어갈 때의 표기

테이블명은 한글이고 컬럼명은 영문이라 `IX_예약접수_SLOT` · `CK_예약접수_StatusCode` 처럼 섞인다. 일관된 규칙을 초안 §3.3(명명규칙)에 명시하고 §8 의 모든 제약 이름을 그 규칙으로 생성한다. 대상은 PK 6 / FK 3 / UQ 2 / UX 1 / NCI 5 / CK 20 내외 / DF 7 내외.

### 4.2 B형간염 제외 컬럼의 최종 이름

후보: `HepatitisBExcluded` · `IsHepatitisBExcluded` · `ExcludeHepatitisB`. 남는 `BIT` 컬럼이 이것 하나뿐이라 기존 `Is` 접두사 관례(`IsStudent` 등)는 전부 삭제되고 없다.

### 4.3 R3 기준선 ID · 기준일 · tag 이름

현행 `HC-RSV-RCP-20260903-R2`. R3 의 ID 와 기준일을 정하고, 그 값이 `scripts/verify-baseline.sh` 와 `docs/baseline/*` 머리말과 `06` 스펙 Global Constraints 에 동시에 반영되어야 함을 초안에 적어 둔다.

### 4.4 `변경이력` DDL

컬럼·타입·PK·인덱스를 확정한다. `TargetTable` 을 `SYSNAME` 으로 둘지 고정 코드로 둘지, `Operation` 을 SP 이름으로 둘지 업무 코드로 둘지가 실제 결정이다. `06 §9.2` 허용목록에 JSON·XML·CSV·동적 SQL 이 없다는 제약 안에서 설계한다.

## 5. 절대 금지

```
docs/baseline/** 수정                    ← §6 참조. R3 확정 승인 전까지 한 바이트도 안 된다
docs/phase4/06_*.md 와 plans/** 수정      ← 이 세션 범위 밖. 04 초안이 확정된 뒤다
../winforms/** 수정
01_Process_Definition.md / 02_Function_Definition.xlsx 를 여는 것
ROOT tools/ 와 docs/baseline/output/ 을 커밋하는 것   ← 다른 세션(문서 생성)의 산출물이다
git config --global 변경 / remote 추가 / push
git reset · clean · checkout -- · force push · 기존 tag 이동
Net461MvpSample DB 접속·변경·삭제         ← 같은 인스턴스에 있다
실제 주민등록번호 사용
```

## 6. 게이트를 깨뜨리지 않는 법 — 반드시 지킬 것

**초안을 `docs/baseline/04_DB_Design.md` 에 덮어쓰면 게이트 두 개가 즉시 깨진다.**

1. `scripts/verify-baseline.sh` 는 6개 파일의 SHA-256 을 **스크립트 안에 하드코딩**해 대조한다. `04` 를 한 바이트라도 바꾸면 `=== 5/6 ===` 로 exit 1 이다.
2. `tools/verify-docs.js` 의 `V08` 은 `docs/baseline/04_DB_Design.md` 에서 `CK_`·`DF_` **이름을 실제로 세어** 스펙·계획이 적은 수치(현재 CK=22 DF=14)와 대조한다. `04` 만 먼저 바꾸면 스펙·계획은 옛 수치를 들고 있으므로 **FAIL 한다.**

그래서 **초안은 `docs/phase4/` 아래 별도 파일로 쓴다.** 예: `docs/phase4/04_DB_Design_R3_DRAFT.md`.
`verify-docs.js` 는 `docs/phase4/plans/` 와 `06_*_CANDIDATE.md` 만 읽으므로 `docs/phase4/` 바로 아래의 새 파일은 검사 대상이 아니다 — 게이트가 그대로 통과한다.

`03`·`04`·`05` 를 실제로 교체하는 것은 **R3 전체(스펙·계획·게이트·실물)를 한 번에 옮기는 시점**이며, 그때 `verify-baseline.sh` 의 기대 해시 3개를 새 값으로 갱신하고 새 tag 를 만든다. 이 세션의 일이 아니다.

각 작업 종료 시 아래 세 개가 그대로여야 한다.

```bash
node tools/verify-docs.js               # PASS 15 / FAIL 0, exit 0
./scripts/verify-baseline.sh            # === 6/6 ===, exit 0
./scripts/verify-winforms-unchanged.sh  # PASS WinForms 변경 0건, exit 0
```

## 7. 실측으로 확인된 함정

`CLAUDE.md` 에 BOM · `-b -I -u` · `N` 접두사 · `set -e` 회피 · `iconv -f UTF-16` 이 적혀 있다. **먼저 읽는다.** 아래는 거기 없는 것들이다.

| 함정 | 실측 결과 |
|---|---|
| `PRINT` 인자에 하위 쿼리 | **`Msg 1046`** 으로 **배치 전체가 컴파일 실패**한다. 앞 문장이 한 줄도 실행되지 않는데 exit 1 이라 시험이 통과한 것처럼 보인다. 변수에 먼저 담는다 |
| `master` 컨텍스트에서 `sys.databases.name + '문자열'` | **`Msg 451`** — catalog collation(`Latin1_General_CI_AS_KS_WS`)과 리터럴(`Korean_Wansung_CI_AS`) 충돌. sqlcmd `-s"|"` 컬럼 구분자를 쓰면 회피된다. **대상 DB 안에서는 일어나지 않는다** |
| `sys.databases.collation_name` | Express 가 `model` 로부터 **`AUTO_CLOSE ON`** 을 상속해, DB 가 닫혀 있는 동안 `NULL` 을 돌려준다. 개폐 상태에 따라 값이 바뀌므로 diff 기준으로 쓰면 안 된다 |
| 필터형 인덱스 | 생성 시점뿐 아니라 **그 테이블의 모든 `INSERT`/`UPDATE`/`DELETE` 시점에도** `QUOTED_IDENTIFIER ON` 을 요구한다. 없으면 `Msg 1934`. 그래서 모든 sqlcmd 호출에 `-I` 가 있고 배포 `.sql` 첫 배치에 `SET QUOTED_IDENTIFIER ON; GO` 가 있다 |
| `CHAR(3)` 에 `'DONE'` | 4글자라 들어가지 않는다. 상태값을 `FIN` 으로 정한 이유다 |
| SQL Server Agent | 이 Express 인스턴스에서 **Stopped**. CDC 불가, 배치 스케줄 인프라 없음 |

## 8. 완료 판정 — 전부 참이어야 이 세션이 끝난다

```
1. docs/phase4/04_DB_Design_R3_DRAFT.md 가 존재하고 §3 의 확정안을 전부 담는다
2. §4 의 미결 4건이 초안 안에서 해소됐다
3. 초안의 테이블·컬럼·제약·인덱스 수량표가 §8 상세정의와 자기정합이다 (04 §10.1 형식)
4. 초안이 04 원본의 어느 절을 어떻게 대체하는지 대응표가 있다
5. superpowers 또는 grilling 으로 초안을 압박했고 그 결과가 초안에 반영됐다
6. docs/baseline/** 이 한 바이트도 바뀌지 않았다        → verify-baseline.sh 6/6
7. node tools/verify-docs.js 가 PASS 15 / FAIL 0 이다
8. ./scripts/verify-winforms-unchanged.sh 가 exit 0 이다
9. git status --short 에 ROOT tools/ 와 docs/baseline/output/ 이 커밋되지 않았다
```

## 9. 검증되지 않은 것 — 마주치면 이렇게 하라

| 항목 | 상태 | 대응 |
|---|---|---|
| R3 스키마 | **한 번도 배포된 적 없다** | 초안 단계에서는 배포하지 않는다. DDL 문법 확인이 필요하면 scratch 이름(`ZZ` 접두사)으로 만들고 반드시 지운 뒤 `tests/01_Schema_Tests.sql` 로 원상복구를 확인한다 |
| Test ID 카탈로그 234건 | R3 에서 어떻게 변하는지 **미분석** | 초안 범위 밖이다. 다만 `SCH-015`(컬럼 55행)·`SCH-016`(CK 22)·`SCH-017`(DF 14)·`SCH-018`(NCI Key) 의 기대값이 전부 바뀐다는 사실은 초안에 기록한다 |
| `변경이력` DDL | **미설계** | §4.4 |
| 삭제 2테이블의 규칙 이관 | NEX-03 와 TGT 판정식이 바뀐다 | `05` §6.2.3(TGT)·`06` 의 `UFN_HC_국가검사구성` 구현이 함께 바뀐다는 것을 초안 §14(선언적 제약으로 보장하지 못하는 항목)에 반영한다 |
| 배포된 R2 DB | 아직 살아 있다 | 초안 작성 중에는 건드리지 않는다. `Rebuild.sql` 로 언제든 R2 스키마를 복원할 수 있다 |

## 10. 끝나면

초안 경로와 §8 판정 9개를 표로 보고하고 **멈춘다.** `03`·`05`·`06`·계획 9개·`verify-docs.js`·실물 재배포로 넘어갈지는 사용자가 정한다.
