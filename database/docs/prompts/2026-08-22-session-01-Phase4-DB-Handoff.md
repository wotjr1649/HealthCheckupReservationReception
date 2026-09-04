# Claude Code Phase 4 — Database 구현 인계 프롬프트

> 이 파일은 `D:\AIDEV\HealthCheckupReservationReception\database`에서 Claude Code를 실행한 뒤 **전체 내용을 한 번에 전달**하기 위한 마스터 프롬프트다.
>
> 현재 세션의 목적은 SQL을 즉시 작성하는 것이 아니다. 먼저 최종 기준선을 검증하고, 필요한 구현 결정을 grilling 방식으로 확정한 뒤, Superpowers의 `brainstorming`과 `writing-plans` workflow를 사용하여 **Phase 4 스펙과 실행계획을 완성하는 것**이다.

---

## 사용자 실행 방법

### 권장 실행 위치

```text
D:\AIDEV\HealthCheckupReservationReception\database
```

### 권장 실행 명령

Git Bash 기준:

```bash
cd /d/AIDEV/HealthCheckupReservationReception/database
claude --dangerously-skip-permissions \
  --add-dir ../docs/baseline ../docs/phase4 ../winforms
```

CMD 기준:

```cmd
cd /d D:\AIDEV\HealthCheckupReservationReception\database
claude --dangerously-skip-permissions --add-dir ..\docs\baseline ..\docs\phase4 ..\winforms
```

> `--add-dir`는 외부 디렉터리를 읽기 전용으로 만드는 옵션이 아니다. 현재 Claude Code가 bypass mode이므로 아래 프롬프트의 경계 규칙을 반드시 준수해야 한다.

---

# BEGIN PROMPT

## 0. 역할과 이번 세션의 단일 목표

당신은 `HealthCheckupReservationReception` 프로젝트의 **Phase 4 MSSQL 설계·구현 인수 담당자**다.

현재 작업 디렉터리는 다음 경로다.

```text
D:\AIDEV\HealthCheckupReservationReception\database
```

프로젝트 루트와 주요 경로는 다음과 같다.

```text
ROOT       = D:\AIDEV\HealthCheckupReservationReception
WORKSPACE  = D:\AIDEV\HealthCheckupReservationReception\database
BASELINE   = D:\AIDEV\HealthCheckupReservationReception\docs\baseline
PHASE4DOCS = D:\AIDEV\HealthCheckupReservationReception\docs\phase4
WINFORMS   = D:\AIDEV\HealthCheckupReservationReception\winforms
```

상대경로 기준:

```text
BASELINE   = ../docs/baseline
PHASE4DOCS = ../docs/phase4
WINFORMS   = ../winforms
```

현재 상태는 다음과 같다.

- `../docs/baseline`에는 확정된 00~05 기준선 문서가 있다.
- `../winforms`에는 WinForms Solution/Project 골격이 있다.
- `database`와 `../docs/phase4`의 Phase 4 산출물은 아직 없거나 빈 상태다.
- 00~05는 이미 사용자 승인을 마친 `FINAL / GO / READ-ONLY` 기준선이다.
- 현재 공식 작업은 Phase 4인 **DB Transaction·잠금·보안·Seed 및 실제 SQL 개발**이다.

이번 최초 세션의 단일 목표는 다음과 같다.

```text
기준선 무결성 검증
→ Repository/도구/SQL Server 환경 읽기 전용 조사
→ 구현 전제와 미결정 기술사항 grilling
→ 승인 가능한 Phase 4 기술 스펙 작성
→ Superpowers implementation plan 작성
→ 실제 SQL 구현 직전에서 중단
```

**이번 프롬프트를 받은 즉시 SQL 구현을 시작하지 마라.**

---

## 1. Superpowers 사용 의무

### 1.1 첫 행동

첫 substantive response를 작성하기 전에 반드시 Claude Code의 Skill 도구를 사용하여 다음 skill을 직접 호출하라.

```text
superpowers:brainstorming
```

이 작업은 단순한 bounded change가 아니다. 다음 특성을 가진 architectural/multi-stage database implementation으로 분류하라.

- 신규 DB Schema 구현
- 4개 Rule TVF 구현
- 15개 Stored Procedure 구현
- 8개 Write SP의 Transaction 설계
- Patient/Slot/Work 동시성 및 잠금 설계
- Seed/Test fixture 설계
- Database Role·권한 설계
- SQL Server 실제 배포·Rollback·동시성 시험
- 다중 Result Set 계약 검증

### 1.2 skill 호출 실패 처리

`superpowers:brainstorming`이 실제로 호출되지 않거나 plugin/skill을 찾지 못하면 다음을 수행하라.

1. Superpowers를 사용한 것처럼 가장하지 않는다.
2. 자체 방식으로 스펙·계획을 계속 작성하지 않는다.
3. plugin load 상태와 정확한 오류만 보고한다.
4. 사용자가 `/plugin list` 또는 `/reload-plugins`로 상태를 확인하도록 안내한다.
5. plugin 설치·업데이트·삭제를 자의적으로 수행하지 않는다.
6. 여기서 중단한다.

구식 redirect command나 제거된 alias에 의존하지 말고, 가능한 경우 위의 fully-qualified skill 이름을 직접 사용한다.

### 1.3 brainstorming 종료 후

Grilling과 설계 검토를 완료하고 사용자가 설계를 승인한 뒤에만 다음 skill을 직접 호출하라.

```text
superpowers:writing-plans
```

작성계획이 완성되더라도 다음 execution skill을 자동 호출하지 않는다.

```text
superpowers:executing-plans
superpowers:subagent-driven-development
superpowers:using-git-worktrees
```

실행방법은 계획 완료 후 사용자에게 선택지를 제시하고 명시적 승인을 받아야 한다.

---

## 2. bypassPermissions 환경의 강제 자기통제

현재 Claude Code는 bypassPermissions 모드다. 권한 확인창이 안전장치 역할을 하지 않으므로 아래 경계를 당신이 스스로 절대 준수하라.

### 2.1 이번 planning session에서 허용되는 작업

- 현재 작업 디렉터리와 상위 Repository 구조 조회
- `../docs/baseline` 00~05 읽기
- `02_Function_Definition.xlsx`의 모든 Sheet와 Used Range 읽기
- `../winforms` 구조를 읽기 전용으로 확인
- `git status`, `git log`, `git diff`, `git rev-parse` 등의 읽기 전용 Git 조사
- `where`, `which`, `command -v`, `--version`, `-?` 등의 도구 존재·버전 확인
- SQL Server 연결정보가 이미 안전하게 확정된 경우에 한해 읽기 전용 Preflight query 제안
- 사용자 승인 후 `../docs/phase4` 아래의 스펙·계획 문서 작성
- 사용자 승인 후 `database` 내부의 planning support 문서 작성

### 2.2 이번 planning session에서 금지되는 작업

사용자가 별도의 명시적 실행 승인을 하기 전까지 다음을 절대 수행하지 마라.

- `CREATE DATABASE`, `ALTER DATABASE`, `DROP DATABASE`
- Table, Function, Procedure, Role, User 생성·변경·삭제
- INSERT, UPDATE, DELETE, MERGE, TRUNCATE
- SQL Server 서비스 시작·중지·재시작
- SQL Server Instance 설정 변경
- Login, Credential, Password, Certificate 변경
- Registry, Firewall, Windows Service 변경
- 패키지·CLI·Plugin 설치 또는 업데이트
- Git commit, tag, branch, worktree 생성
- `git reset`, `git clean`, `git checkout --`, force push 등 파괴적 Git 작업
- `../docs/baseline/**` 수정·이름변경·이동·삭제
- `../winforms/**` 수정·이름변경·이동·삭제
- 기존 Solution/Project의 참조·App.config·Source 수정
- 작업공간 밖 파일 생성·수정
- 확인되지 않은 SQL Server 또는 운영·공용 Instance 접속
- 사용자 비밀정보를 파일·로그·명령행에 평문 저장

### 2.3 절대 금지되는 위험 구성

Phase 4 실행 승인을 받은 뒤에도 다음은 사용자가 별도 승인하지 않는 한 금지다.

```text
sa 계정 사용 또는 활성화
sysadmin 권한 부여
TRUSTWORTHY ON
xp_cmdshell 활성화
Ad Hoc Distributed Queries 활성화
CLR 활성화
외부 네트워크 호출
Linked Server 생성
운영 DB 접속
실제 주민등록번호 Seed/Test 사용
```

### 2.4 파일 소유권 경계

```text
읽기 전용:
- ../docs/baseline/**
- ../winforms/**

Planning 후 쓰기 허용:
- ../docs/phase4/**
- ./CLAUDE.md
- ./README.md
- ./artifacts/reports/**

실제 구현 승인 후 쓰기 허용:
- ./Deploy.sql
- ./deploy/**
- ./tests/**
- ./scripts/**
- ./artifacts/**
```

기준선이나 WinForms에서 결함을 발견해도 직접 고치지 않는다. `Deviation` 또는 `Implementation Blocker`로 보고한다.

---

## 3. Source of Truth와 충돌 처리

### 3.1 유일한 기준선

다음 여섯 파일만 구현 기준으로 사용한다.

```text
../docs/baseline/00_Project_Policy.md
../docs/baseline/01_Process_Definition.md
../docs/baseline/02_Function_Definition.xlsx
../docs/baseline/03_Wireframe_Definition.md
../docs/baseline/04_DB_Design.md
../docs/baseline/05_DB_Rule_SP_Contract.md
```

파일명에 아래 문자열이 포함된 과거·후보·복사본은 구현 기준으로 사용하지 않는다.

```text
(1)
(2)
(3)
Candidate
CANDIDATE
후보
개선본
백업
old
copy
```

### 3.2 우선순위

문서 간 충돌이 실제로 발견될 경우 다음 순서로 판단한다.

```text
00_Project_Policy.md
→ 01_Process_Definition.md
→ 02_Function_Definition.xlsx
→ 03_Wireframe_Definition.md
→ 04_DB_Design.md
→ 05_DB_Rule_SP_Contract.md
→ Phase 4 문서
→ SQL Script
→ WinForms Source
```

다만 하위 문서가 상위 문서의 의미를 변경하지 않고 물리 구현을 구체화한 경우에는 하위의 구체적 계약을 구현한다.

### 3.3 충돌·모호성 처리

- 문서 내용을 조용히 보정하거나 일반지식으로 대체하지 않는다.
- 기준선에 없는 업무정책을 창작하지 않는다.
- 구현 편의를 위해 테이블·컬럼·SP·Parameter·Result Set·ResultCode를 바꾸지 않는다.
- 구현이 불가능하다고 판단되면 재현 근거를 제시하고 `IMPLEMENTATION BLOCKED`로 판정한다.
- 해결 가능한 내부 구현 선택은 Phase 4 설계결정으로 분류한다.
- 모든 항목을 다음 중 하나로 라벨링한다.

```text
[B] Baseline에서 직접 확정된 사실
[D] 사용자와 grilling으로 확정한 결정
[I] Baseline을 변경하지 않는 구현 상세
[A] 아직 승인받지 않은 가정
[X] 충돌·이탈·차단사항
```

---

## 4. 기준선 무결성 검증 — 구현·질문보다 먼저 수행

### 4.1 파일 존재와 중복본 조사

다음을 확인하라.

1. 여섯 파일이 모두 존재하는가.
2. 동일 디렉터리에 과거 사본이 존재하는가.
3. 파일명과 내부 `문서명`이 일치하는가.
4. 상태가 모두 `FINAL / GO / READ-ONLY`인가.
5. 기준선 ID가 모두 `HC-RSV-RCP-20260903-R2`인가.
6. 문서 버전이 다음과 일치하는가.

| 파일 | 기대 버전 |
|---|---:|
| `00_Project_Policy.md` | v1.2 |
| `01_Process_Definition.md` | v1.2 |
| `02_Function_Definition.xlsx` | v1.2 |
| `03_Wireframe_Definition.md` | v1.2 |
| `04_DB_Design.md` | v1.1 |
| `05_DB_Rule_SP_Contract.md` | v1.1 |

### 4.2 Hash 검증

텍스트 파일은 CRLF/LF 차이를 제거한 LF 기준으로 검증하고, XLSX는 바이너리 그대로 검증한다.

기대 SHA-256:

| 파일 | SHA-256 |
|---|---|
| `00_Project_Policy.md` | `5adba8d4001e8f7aa27091614df33922a7d3d7cf27965f9ccc60874316beaefc` |
| `01_Process_Definition.md` | `1b0d1c23cb8dda15c6c0ba46a86a48a2586608b42079ed96a837386ae35f69e6` |
| `02_Function_Definition.xlsx` | `ac7b362ea79b062a889cd296bada304db4f66cb850a2ab04ca999c7530a1654a` |
| `03_Wireframe_Definition.md` | `5426e642863bfcc6a637a38e216b68f013679fd5791ca7b63e34c40b367b2e2d` |
| `04_DB_Design.md` | `8176d8a82ae360718f811d7f26481b6cb56778585aeebd3b38b42ec89950038f` |
| `05_DB_Rule_SP_Contract.md` | `b865c76fa5d041fa816e0d2380a7ca16306e80502652a9369201fdc956f71c24` |

권장 검증 방식:

```python
from pathlib import Path
import hashlib

root = Path("../docs/baseline")
text_files = [
    "00_Project_Policy.md",
    "01_Process_Definition.md",
    "03_Wireframe_Definition.md",
    "04_DB_Design.md",
    "05_DB_Rule_SP_Contract.md",
]

for name in text_files:
    data = (root / name).read_bytes().replace(b"\r\n", b"\n")
    print(name, hashlib.sha256(data).hexdigest())

name = "02_Function_Definition.xlsx"
data = (root / name).read_bytes()
print(name, hashlib.sha256(data).hexdigest())
```

Hash가 다르면 다음 순서로 처리하라.

1. 단순 CRLF/LF 차이인지 재검증한다.
2. 내부 버전·기준선 ID·핵심 수치를 확인한다.
3. 정상화 이후에도 다르면 어떤 파일이 다른지 보고한다.
4. 사용자의 확인 전에는 기준선을 신뢰하고 진행하지 않는다.
5. 과거 8개 테이블·암호화 문서가 섞인 경우 즉시 `IMPLEMENTATION BLOCKED`로 중단한다.

### 4.3 XLSX 전수검증

`02_Function_Definition.xlsx`를 파일명만 확인하거나 첫 Sheet만 읽지 마라. 모든 Sheet와 Used Range를 실제로 읽어라.

기대 Sheet:

| 순서 | Sheet | 기대 Used Range |
|---:|---|---|
| 1 | `문서정보` | `A1:F8` |
| 2 | `기능정의` | `A1:I39` |
| 3 | `업무Rule` | `A1:J39` |
| 4 | `개발범위` | `A1:H32` |
| 5 | `설계근거` | `A1:F25` |
| 6 | `DB추적` | `A1:G19` |
| 7 | `최종검수` | `A1:E33` |

확인할 고정값:

```text
16개 Function ID
36개 기능행
TGT 5개
NEX 7개
AEX 5개
HOL 5개
7개 DB Table 추적
예약변경 현재 WorkId 제외
NEX 실제 8~11행
```

XLSX reader library가 없더라도 사용자에게 CSV 변환을 요구하기 전에 ZIP/XML 또는 설치된 안전한 라이브러리로 읽을 방법을 조사하라. 원본 XLSX는 수정하지 않는다.

---

## 5. 구현 전 반드시 정확히 인지해야 하는 고정 계약

아래 값은 grilling 대상이 아니다. 질문하거나 변경안을 제시하지 말고 기준선 검증값으로 사용한다.

### 5.1 프로그램·DB 환경

```text
Solution              = HealthCheckupReservationReception
WinForms Project      = HealthCheckupReservationReception.WinForms
Database              = HealthCheckupReservationReceptionDb
Connection String Key = HealthCheckupDb
Framework             = .NET Framework 4.6.1
UI                     = WinForms + DevExpress 20.2
DB                     = Microsoft SQL Server 2012 이상
DB Access              = Stored Procedure 중심
```

### 5.2 물리 스키마 수치

```text
물리 Table                    7개
Primary Key                   7개
Foreign Key                   6개
일반 Unique Constraint       2개
Filtered Unique Index         1개
업무/조회 Nonclustered Index 5개
Sequence                      1개
Trigger                       0개
TVP                           0개
DELETE SP                     0개
```

### 5.3 물리 Table 7개

```text
INFO_PATIENTS
INFO_CHECKUP_WORKS
INFO_CHECKUP_WORK_EXAMS
MST_EXAM_ITEMS
MST_HOLIDAYS
HIS_GENERAL_CHECKUP_COMPLETIONS
INFO_PATIENT_EXAM_EXCLUSIONS
```

추가 테이블을 만들지 않는다. 특히 다음은 금지다.

```text
SEC_PATIENT_IDENTIFIERS
MST_NATIONAL_EXAMS
MST_ADDITIONAL_EXAMS
별도 접수 Master/Detail
상태 History
범용 Rule Engine
공통 코드 테이블
테스트 전용 영구 Table
```

### 5.4 Sequence

```text
[dbo].[SEQ_HC_CHART_NO]
형식: C + 6자리 일련번호
예: C000001
결번 허용
MAXVALUE 999999
```

### 5.5 Inline TVF 4개

```text
[dbo].[UFN_HC_일정확인]
[dbo].[UFN_HC_검진대상확인]
[dbo].[UFN_HC_국가검사구성]
[dbo].[UFN_HC_추가검사확인]
```

공통 계약:

```text
Inline Table-Valued Function
단일 SELECT 반환
데이터 변경 없음
Transaction 없음
THROW 없음
C# 직접 호출 금지
Stored Procedure 내부 사용
```

### 5.6 외부 호출 Stored Procedure 15개

```text
[dbo].[USP_HC_SELECT_공통업무상태]
[dbo].[USP_HC_SELECT_수검자목록]
[dbo].[USP_HC_SELECT_수검자상세]
[dbo].[USP_HC_INSERT_수검자]
[dbo].[USP_HC_UPDATE_수검자정보]
[dbo].[USP_HC_SELECT_수검자유효업무]
[dbo].[USP_HC_SELECT_예약가능정보]
[dbo].[USP_HC_INSERT_예약]
[dbo].[USP_HC_UPDATE_예약변경]
[dbo].[USP_HC_UPDATE_예약취소]
[dbo].[USP_HC_SELECT_예약접수목록]
[dbo].[USP_HC_SELECT_예약접수상세]
[dbo].[USP_HC_UPDATE_접수완료]
[dbo].[USP_HC_UPDATE_접수추가검사]
[dbo].[USP_HC_UPDATE_접수취소]
```

이름·개수·업무 책임을 변경하지 않는다.

### 5.7 Write SP 8개

```text
USP_HC_INSERT_수검자
USP_HC_UPDATE_수검자정보
USP_HC_INSERT_예약
USP_HC_UPDATE_예약변경
USP_HC_UPDATE_예약취소
USP_HC_UPDATE_접수완료
USP_HC_UPDATE_접수추가검사
USP_HC_UPDATE_접수취소
```

모든 Write SP는 최소 다음 원칙을 계획해야 한다.

```text
SET NOCOUNT ON
SET XACT_ABORT ON
예상 업무오류는 RS0 ResultCode
예상하지 못한 SQL 오류는 THROW
필요한 범위의 명시적 Transaction
상태·동시성·Rule의 저장시점 재검증
부분저장 금지
성공 후 최신 동시성값 반환
```

### 5.8 공통 RS0

모든 외부 SP의 첫 Result Set은 정확히 1행이다.

```text
Success    BIT NOT NULL
Code       INT NOT NULL
Message    NVARCHAR(300) NOT NULL
Field      VARCHAR(50) NULL
ServerTime DATETIME2(7) NOT NULL
```

### 5.9 ResultCode

- ResultCode Catalog는 정확히 38개다.
- 정의된 숫자와 C# Enum 의미를 변경하지 않는다.
- 새로운 ResultCode를 임의로 추가하지 않는다.
- Message 문자열로 C# 분기하지 않는다.
- Lock timeout, deadlock, 시스템 오류 등 Catalog에 없는 실패를 억지로 기존 업무코드에 매핑하지 않는다. 처리방식은 Phase 4 설계에서 확정하되 05 계약을 변경하지 않는다.

### 5.10 SocialNumber 처리

```text
INFO_PATIENTS.SocialNumber VARCHAR(13) NOT NULL
'-' 제거 숫자 13자리
임의 테스트값만 사용
실제 주민등록번호 사용 금지
암호화 없음
복호화 없음
HMAC 없음
검색 Token 없음
보조 Security Table 없음
```

Write SP가 최종 검증·산출할 항목:

```text
13자리 숫자 형식
앞 6자리 실제 날짜
7번째 자리 세기·성별 코드
Birthday yyyyMMdd
Gender M/F
SocialNumber 고유성
```

체크디지트와 실제 행정번호 존재 여부는 검증하지 않는다.

테스트값은 날짜·성별 코드는 유효하되, 실제 주민등록번호로 사용될 가능성을 줄이기 위해 **체크디지트가 의도적으로 유효하지 않은 값**으로 생성하는 방식을 우선 검토하라. 실제 번호를 복사하지 않는다.

### 5.11 검사 구성

```text
MST_EXAM_ITEMS Seed        19행
NEX 역할                   13종
AEX 역할                   7종
EX012                      NEX 골밀도 + AEX OPT04 공통
TGT 대상 NEX 실제 결과     8~11행
AEX 실제 선택 가능 최대    6행
Work 검사구성 실제 최대    17행
Master 기준 보수 상한       19행
AEX 입력                    OPT01~OPT07 BIT 7개
```

TVP, CSV, XML, JSON, `STRING_SPLIT`, 비트마스크를 사용하지 않는다.

### 5.12 상태전이

```text
RSV → RSV : 예약변경
RSV → RCP : 접수완료
RSV → CNL : 예약취소
RCP → RCP : AEX 실제변경
RCP → CNL : 접수취소
CNL → *   : 금지
```

물리삭제와 복원은 없다.

### 5.13 변경영향 Matrix

```text
예약일 변경 포함  → 일정 + 중복 + 정원 + TGT + NEX + AEX
시간대만 변경     → 일정 + 중복 + 정원만
AEX만 변경        → 저장 NEX 기준 AEX만
시간대+AEX        → 일정 + 중복 + 정원 + 저장 NEX 기준 AEX
변경 없음         → 상태·동시성 확인 후 No-op
```

예약변경의 다른 유효업무 조회에서는 반드시 다음 조건을 적용한다.

```sql
WorkId <> @WorkId
```

결과:

```text
다른 유효업무 0건 → 정상
다른 유효업무 1건 → 306 OtherReservation
다른 유효업무 2건 이상 → 701 WorkDataError
```

### 5.14 동시성값

```text
Patient Aggregate = INFO_PATIENTS.LastEditDate DATETIME
Work Aggregate    = INFO_CHECKUP_WORKS.RowVersion BINARY(8)
```

- Patient 수정 성공 시 `LastEditDate`는 이전값보다 반드시 증가해야 한다.
- DATETIME 정밀도 때문에 새 서버시각이 기존값보다 크지 않으면 최소 4ms 증가시키는 계약을 구현계획에 포함한다.
- AEX 실제변경 시 Work Master도 갱신되어 RowVersion이 바뀌어야 한다.
- 동일 AEX 집합은 Detail과 Work 모두 갱신하지 않는 No-op이다.

### 5.15 SQL Server 2012 호환성

최소 호환대상은 SQL Server 2012다. 따라서 구현계획에서 SQL Server 2012에 없는 문법·기능을 사용하지 않도록 검증하라.

예시로 다음은 기본 구현에 사용하지 않는다.

```text
CREATE OR ALTER
DROP ... IF EXISTS
STRING_SPLIT
SESSION_CONTEXT
JSON 함수
AT TIME ZONE
STRING_AGG
```

허용되는 확정 기능 예:

```text
SEQUENCE
TRY_CONVERT
THROW
ROWVERSION
Filtered Index
sp_getapplock
```

실제 서버가 최신 버전이어도 Compatibility Level 110에서 동작하도록 계획한다.

---

## 6. Repository와 WinForms 조사 규칙

### 6.1 Repository 조사

질문 전에 다음을 읽기 전용으로 조사하라.

```text
pwd
Repository root
.git 존재 여부
현재 branch
working tree clean 여부
tracked/untracked 파일
기존 CLAUDE.md 위치와 내용
기존 .claude 설정 위치
현재 database 디렉터리 내용
../docs/phase4 내용
../winforms Solution/Project 이름
```

파괴적 Git 명령을 사용하지 않는다.

### 6.2 WinForms 조사 경계

`../winforms`는 다음 사항만 읽기 전용으로 확인할 수 있다.

- 실제 Solution 이름
- 실제 Project 이름
- Target Framework
- DevExpress 참조 여부
- 기존 `App.config` 및 Connection String key 존재 여부
- 이미 작성된 DB 호출 코드 존재 여부

WinForms Source가 기준선과 다르더라도 DB 계약을 WinForms에 맞추어 변경하지 않는다. WinForms는 후속 구현 대상이고 00~05가 우선한다.

### 6.3 빈 database 상태 처리

`database`가 비어 있다는 사실을 결함으로 보지 않는다. 아직 구현되지 않은 상태로 정확히 보고한다. 존재하지 않는 SQL·테스트·로그를 PASS 처리하지 않는다.

---

## 7. Grilling Protocol — 질문을 통한 구현결정 확정

### 7.1 목적

Grilling은 이미 확정된 업무정책을 다시 논의하는 절차가 아니다. Phase 4에서 실제 SQL을 안전하고 재현 가능하게 구현하기 위해 필요한 **환경·배포·잠금·테스트·권한 결정을 빈틈없이 확정하는 기술 인터뷰**다.

### 7.2 질문 방식

반드시 다음 규칙을 따른다.

1. 파일이나 환경조사로 답을 얻을 수 있는 질문은 사용자에게 묻지 않는다.
2. 한 번에 핵심 질문 하나만 한다.
3. 질문마다 Decision ID를 부여한다.
4. 질문 전에 관찰된 사실을 짧게 제시한다.
5. 왜 결정이 필요한지 설명한다.
6. 가능한 대안 2~4개와 장단점을 제시한다.
7. 최종 추천안을 명확히 제시한다.
8. 사용자가 `추천안`, `권장안`, `그대로`라고 답하면 추천안을 채택한다.
9. 답을 받은 즉시 Decision Log에 기록한다.
10. 같은 질문을 반복하지 않는다.
11. 구현 차단사항이 0건이 될 때까지 진행한다.
12. 질문 중에는 SQL 구현을 시작하지 않는다.

질문 형식:

```text
[D4-001] 결정 제목

관찰된 사실:
- ...

결정이 필요한 이유:
- ...

대안:
A. ...
   장점:
   단점:
B. ...
   장점:
   단점:

추천:
- ...

질문:
- A/B 중 어느 방식으로 확정할까요?
```

### 7.3 반드시 조사·확정해야 할 주제

아래 항목은 **환경조사로 이미 확정되지 않은 경우에만** 한 문항씩 질문한다.

#### A. SQL Server 실행환경

- 실제 개발용 Instance 이름
- Local/Remote 여부
- 운영·공용 Instance가 아닌지
- SQL Server Edition과 Major Version
- Database Compatibility Level 110 적용 가능 여부
- `sqlcmd` 종류와 버전
- `sqlcmd` 실행경로
- Windows Authentication 또는 SQL Authentication
- 연결문자열을 어디에서 관리할지
- Secret을 Repository에 저장하지 않을 방법
- DB 서버의 `SYSDATETIMEOFFSET()` offset이 KST `+09:00`인지

권장 기본안:

```text
로컬 개발전용 SQL Server Developer/Express Instance
Windows Integrated Authentication
Database=HealthCheckupReservationReceptionDb
Compatibility Level=110
Secret 파일 Commit 금지
```

#### B. Database 생명주기와 파괴 작업

- Claude가 대상 Database를 생성해도 되는가
- Clean rebuild 시 대상 Database를 Drop/Recreate해도 되는가
- Drop 허용대상은 정확히 `HealthCheckupReservationReceptionDb` 하나인가
- 기존 데이터 보존이 필요한가
- `Deploy.sql`은 신규 배포 전용인가, 재실행 가능한가
- `rebuild.sh`는 어떤 안전확인 조건을 가져야 하는가
- 별도 Rollback/Undeploy script가 필요한가

권장 기본안:

```text
일반 Deploy = 비파괴·반복 검증 가능한 방식
Clean Rebuild = 명시적 별도 script
Drop 대상 DB 이름 exact-match 검증
master/system DB 대상 작업 금지
```

#### C. Git 작업방식

- 현재 Repository가 초기화되어 있는가
- 00~05가 Commit/Tag 되어 있는가
- Phase 4를 현재 branch에서 수행할지 전용 branch/worktree에서 수행할지
- Claude가 commit을 수행해도 되는지
- commit 단위와 메시지 규칙

권장 기본안:

```text
00~05 baseline commit/tag 보호
Phase 4 전용 branch 또는 worktree
단계별 작은 commit
reset/clean/force 금지
```

#### D. 배포 Script 구조와 인코딩

- 기존 제안 구조를 그대로 사용할지
- SQLCMD variable을 사용할지
- 한국어 객체명을 위한 UTF-8 입력/출력 방식을 어떻게 검증할지
- ODBC sqlcmd와 Go sqlcmd 차이를 어떻게 처리할지
- `GO` batch와 생성순서를 어떻게 고정할지
- 배포 실패 시 exit code와 log를 어떻게 남길지

최소 후보 구조:

```text
Deploy.sql
deploy/00_Preflight.sql
deploy/01_Schema.sql
deploy/02_Seed.sql
deploy/03_Functions.sql
deploy/04_Procedures_Select.sql
deploy/05_Procedures_Patient_Write.sql
deploy/06_Procedures_Reservation_Write.sql
deploy/07_Procedures_Reception_Write.sql
deploy/08_Security.sql
deploy/09_Verify.sql
```

#### E. Transaction 기본 Template

8개 Write SP에 대해 다음을 확정해야 한다.

- Transaction 시작시점
- `SET XACT_ABORT ON`
- `TRY/CATCH`
- `XACT_STATE()` 처리
- 예상 업무실패 반환 전 Rollback 방식
- 예상치 못한 오류의 `THROW`
- RS0/RS1 반환시점
- Commit 후 Result Set 반환 여부
- No-op에서 Transaction과 Lock 범위
- 부분저장 방지방법

#### F. Locking·Isolation 전략

최소 다음 세 접근을 비교하라.

```text
A. UPDLOCK/HOLDLOCK 중심
B. sp_getapplock 중심
C. 논리 Resource는 sp_getapplock + 행 상태는 조건부 UPDATE/UPDLOCK 혼합
```

최종 스펙에서 반드시 확정할 내용:

- 채택방식과 이유
- Transaction Isolation Level
- Patient resource 형식
- SocialNumber resource 형식
- ChartNo resource 형식
- Slot resource 형식
- Work row 잠금방식
- `sp_getapplock` owner
- timeout 값
- 음수 반환코드 처리
- Deadlock/timeout을 ResultCode가 아닌 시스템 오류로 처리할지
- Lock 획득 총순서
- 예약이동 시 기존/new Slot 정렬 규칙
- Lock release 시점
- Lock resource에서 실제 테스트 SocialNumber를 직접 노출할지 hash할지

고정 결과조건:

```text
동일 SocialNumber 동시등록 → 신규 INSERT 정확히 1건
19/20 Slot 동시예약 2건 → 성공 정확히 1건
동일 Patient 다른 Slot 동시예약 → 성공 정확히 1건
주민번호 변경과 같은 Patient 신규예약 → 모순된 동시 성공 금지
동일 RSV 접수완료 vs 예약취소 → 성공 정확히 1건
같은 Work AEX 동시변경 → stale RowVersion 실패
예약 교차이동 → 결정적 Lock 순서로 Deadlock 방지
```

#### G. Read SP의 일관성

다중 Result Set SELECT SP에 대해 다음을 확정하라.

- 단순 READ COMMITTED로 충분한가
- 한 호출에서 Work/Detail/Action이 서로 다른 Snapshot을 볼 가능성을 어떻게 관리할가
- temp/table variable materialization이 필요한가
- 명시적 read transaction이 필요한가
- Snapshot Isolation/RCSI를 켜야 하는가
- 추가 DB option이 과제 범위에 비해 과도한가

조회값은 예약좌석이나 Write 권한을 확보하지 않으며 Write SP가 재검증한다는 기준을 유지한다.

#### H. SocialNumber Test Data 안전성

- 테스트 SocialNumber 생성규칙
- 체크디지트를 의도적으로 무효화하는 방식
- 생년월일/성별 경계값 생성방식
- 실제 번호를 수동 복사하지 않는 원칙
- Seed/Test report에서 실제 개인정보 미사용을 어떻게 검증할지

#### I. Seed와 Test Fixture 분리

다음을 구분할지 확정한다.

```text
Deploy Master Seed
Demo Seed
Automated Test Fixture
Corruption/Adversarial Fixture
```

검토할 내용:

- Exam Master 19행 idempotent seed
- 평일 휴무일 1건
- 토요일 휴무일 1건
- TGT 완료이력
- B형간염 제외정보
- 나이·성별 경계 수검자
- Slot 19/20 및 20/20 상태
- 손상 데이터 701 검증
- Test 후 데이터 정리 또는 DB rebuild
- Identity 값 하드코딩 금지
- PatientId/WorkId를 ChartNo 등 안정키로 조회하는 방식

#### J. 시간 경계 시험

Write SP signature는 변경할 수 없고 SP는 `SYSDATETIME()`을 사용한다. 따라서 다음을 확정한다.

- `UFN_HC_일정확인(@ServerTime, ...)`을 이용한 결정적 경계시험
- Write SP는 실제 서버시각을 사용한 smoke/integration test
- 운영시간 경계를 시험하기 위한 숨은 Test Clock, SESSION_CONTEXT, 서버시각 변경을 도입하지 않을 것
- 실제시각 때문에 자동화할 수 없는 경계의 증거수준과 보고방식

테스트 편의를 위한 비공개 시간 주입 Backdoor를 production SP에 넣지 않는다.

#### K. 다중 Result Set 계약 검증도구

SQL script만으로 모든 후속 Result Set의 이름·순서·타입을 자동 검증하기 어려울 수 있다. 환경조사 후 다음 후보를 비교한다.

```text
A. sqlcmd output + SQL assertion
B. 별도 경량 C# Console DB contract verifier
C. 설치되어 있는 Python/pyodbc verifier
D. Phase 5 WinForms integration에서 일부 보완
```

조건:

- WinForms Source를 수정하지 않는다.
- 불필요한 Framework를 추가하지 않는다.
- SP 15개의 RS0와 후속 Result Set 순서·컬럼·Cardinality를 재현 가능하게 검증한다.
- 검증도구가 필요한 경우 `database/tools` 아래에 격리한다.

#### L. Security Principal

다음을 확정한다.

- Application database role 이름
- 실제 login을 생성할지, role만 생성할지
- 테스트는 `CREATE USER ... WITHOUT LOGIN` + `EXECUTE AS USER`로 할지
- 15개 SP에 개별 EXECUTE를 부여할지
- Table 직접 SELECT도 차단할지
- Table INSERT/UPDATE/DELETE는 반드시 차단
- TVF 직접 호출권한 미부여
- dbo ownership chaining 사용 여부
- Secret 없는 security test

권장 기본안:

```text
Database Role + User WITHOUT LOGIN 테스트 사용자
15개 외부 SP만 개별 GRANT EXECUTE
Table 직접 DML/SELECT 권한 없음
TVF 직접 권한 없음
Login/Password 생성 없음
```

#### M. Test·Log·Evidence

- `sqlcmd -b` exit code 처리
- 단계별 로그 파일명
- PASS/FAIL marker
- 테스트 중 오류가 예상되는 경우 exit code를 어떻게 다룰지
- 동시 세션 결과를 어떻게 수집할지
- Clean rebuild evidence
- Object inventory evidence
- Baseline hash evidence
- 최종 Phase 4 report 구조

---

## 8. Brainstorming에서 반드시 비교할 설계 대안

`superpowers:brainstorming`의 설계단계에서 최소 다음 대안을 제시하고 장단점을 비교하라.

### 8.1 잠금전략

- `UPDLOCK/HOLDLOCK` 중심
- `sp_getapplock` 중심
- Hybrid

### 8.2 배포전략

- Clean-create 전용 Script
- Idempotent deploy + 별도 clean rebuild
- SSDT/Migration tool 도입

과제 규모에서는 과도한 Framework 도입을 경계한다.

### 8.3 테스트전략

- SQL-only
- SQL + 경량 contract verifier
- WinForms 통합시험 의존

### 8.4 Seed전략

- Master/Demo/Test 모두 한 파일
- Master Seed와 Test Fixture 분리
- 매 테스트 동적 Fixture 생성

### 8.5 개발실행전략

- 한 세션 일괄 구현
- Superpowers executing-plans batch
- Superpowers subagent-driven-development + 단계별 spec/code review

각 대안에 대해 다음 기준으로 평가한다.

```text
00~05 계약준수
SQL Server 2012 호환성
동시성 정확성
재현 가능한 테스트
과제 10근무일 범위
파일·객체 관리 단순성
WinForms 후속 연동 용이성
실패 시 복구·원인추적
```

최종 추천안을 제시하되 사용자의 승인을 받기 전 확정하지 않는다.

---

## 9. Phase 4 스펙 문서 작성계약

### 9.1 작성시점

다음 조건을 모두 만족한 뒤 작성한다.

```text
Baseline hash/metadata 검증 완료
XLSX 전수검증 완료
환경조사 완료
필수 grilling 결정 완료
설계안 section별 사용자 승인 완료
구현 차단 모호성 0건
```

### 9.2 파일 위치

다음 파일을 작성한다.

```text
../docs/phase4/06_DB_Transaction_Security_Seed_CANDIDATE.md
```

아직 SQL 실행검증 전이므로 `FINAL`이라고 표시하지 않는다.

문서 상태:

```text
CANDIDATE / IMPLEMENTATION READY 또는 BLOCKED
```

실제 배포·테스트가 끝난 뒤에만 다음 정식 파일로 최종화할 수 있다.

```text
../docs/phase4/06_DB_Transaction_Security_Seed.md
```

### 9.3 스펙 필수 목차

아래 항목을 누락 없이 포함한다.

1. 문서정보
   - 문서명
   - 상태
   - 버전
   - 기준일
   - 기준선 ID
   - 대상 SQL Server
   - Source of Truth
2. 목적과 Phase 4 범위
3. Out of Scope
4. Baseline 검증결과
   - 파일
   - 버전
   - Hash
   - XLSX Sheet
5. 환경조사 결과
   - OS/Shell
   - SQL Server Instance
   - Version/Edition
   - Authentication
   - sqlcmd
   - KST
   - Git
6. Decision Log
   - Decision ID
   - 선택안
   - 대안
   - 근거
   - 승인상태
7. 최종 Database 파일구조
8. 배포·Clean Rebuild 전략
9. SQL Server 2012 호환성 규칙
10. 공통 SQL Coding Rule
11. 7개 Table 구현 계약
12. PK/FK/UQ/CK/DF/Index/Sequence 구현 계약
13. 19행 Exam Master Seed
14. Holiday Master Seed
15. Demo/Test Fixture 전략
16. 실제 주민등록번호 미사용 검증
17. 4개 Inline TVF 구현 전략
18. 15개 SP 구현 Matrix
    - SP 이름
    - 구분
    - Parameter 수
    - Result Set 수
    - 관련 정책
    - Transaction 여부
    - Lock 여부
    - Test file
19. 공통 RS0 구현 Pattern
20. 예상 업무실패와 THROW 경계
21. 8개 Write SP Transaction Matrix
22. Lock Resource 네이밍
23. Lock 획득 총순서
24. Write SP별 Lock Sequence
25. Lock timeout·deadlock 처리
26. Patient LastEditDate 단조증가
27. Work RowVersion 처리
28. No-op 처리
29. 예약변경 Scope 처리
30. 현재 WorkId 제외처리
31. Read SP consistency 전략
32. Security Role·User·Grant·Deny
33. Test Architecture
34. Schema Test
35. Rule Test
36. Stored Procedure Contract Test
37. Rollback Test
38. Concurrency Test
39. Security Test
40. Clean Rebuild Test
41. Log·Evidence 구조
42. Phase 4 완료 Gate
43. 알려진 한계
44. Deviation/Blocker
45. 구현 승인 판정

### 9.4 Transaction Matrix 필수 형식

8개 Write SP 각각에 대해 다음 열을 작성한다.

| SP | 입력 정규화 | 사전조회 | Transaction 시작 | App Lock | Row Lock | 재검증 | 변경대상 | Commit | 성공 반환 | 예상 실패 |
|---|---|---|---|---|---|---|---|---|---|---|

### 9.5 Lock Matrix 필수 형식

| SP | Resource | Key 형식 | Mode | Owner | Timeout | 획득순서 | Release | 실패처리 |
|---|---|---|---|---|---:|---:|---|---|

### 9.6 Test Matrix 필수 형식

| Test ID | 기준문서 | 사전조건 | Session | 실행 | 기대 Code/행수 | DB 불변조건 | Cleanup | Evidence |
|---|---|---|---|---|---|---|---|---|

### 9.7 Baseline을 복제하지 말 것

스펙은 00~05 전체를 복사하지 않는다. Phase 4 구현에 필요한 고정계약을 정확히 참조·요약하고, 새로 확정한 Transaction·잠금·보안·Seed·테스트 상세에 집중한다.

---

## 10. Superpowers implementation plan 작성계약

### 10.1 작성시점

사용자가 `06_DB_Transaction_Security_Seed_CANDIDATE.md`를 승인한 뒤 다음을 직접 호출한다.

```text
superpowers:writing-plans
```

### 10.2 계획 파일 위치

기본 위치를 다음으로 override한다.

```text
../docs/phase4/plans/
```

권장 메인 파일:

```text
../docs/phase4/plans/2026-09-03-phase4-database-implementation.md
```

계획이 과도하게 길어질 경우 한 파일에 억지로 쓰지 않는다.

```text
../docs/phase4/plans/
├─ 2026-09-03-phase4-database-implementation.md   # Index/Overview
├─ 01-preflight-schema.md
├─ 02-seed-functions.md
├─ 03-select-procedures.md
├─ 04-patient-write-procedures.md
├─ 05-reservation-write-locking.md
├─ 06-reception-write-procedures.md
├─ 07-security.md
└─ 08-verification-finalization.md
```

분할 기준:

```text
한 파일이 약 600줄 또는 30,000자를 크게 초과할 것으로 예상되면 단계별로 분할
메인 Index에서 실행순서와 의존성을 연결
Task ID는 전체 파일에서 유일
```

### 10.3 계획의 작성 원칙

- 계획을 읽는 구현자는 코드베이스와 도메인을 모른다고 가정한다.
- 정확한 파일경로를 적는다.
- 각 Task는 독립 검증 가능한 크기로 분해한다.
- 각 Task에 실행명령과 기대결과를 적는다.
- 모든 Task에 실패 시 중단조건을 적는다.
- DRY, YAGNI를 적용한다.
- SQL Server 2012 호환성을 매 Task에서 확인한다.
- 구현 편의를 위한 계약변경을 계획에 넣지 않는다.
- WinForms Source 변경 Task를 포함하지 않는다.
- 00~05 수정 Task를 포함하지 않는다.
- 실제 실행 없이 PASS라고 기록하는 Task를 만들지 않는다.

### 10.4 SQL TDD 적용

Superpowers plan에 TDD가 누락되지 않도록 다음을 명시적으로 강제한다.

Rule·SP 단위 Task는 가능한 한 다음 Cycle을 사용한다.

```text
RED
- 해당 계약을 검증하는 SQL assertion/test를 먼저 작성
- 구현 전 실패를 실제 확인

GREEN
- 최소 SQL 구현
- 해당 test 통과 확인

REFACTOR
- 중복 제거
- 전체 회귀시험
```

Schema처럼 대상 객체가 없는 것이 정상인 최초 단계는 다음으로 조정한다.

```text
검증 Script/expected inventory 먼저 정의
→ Schema 구현
→ inventory·constraint test 통과
```

시간경계처럼 Write SP에서 Test Clock을 주입할 수 없는 항목은 UFN의 결정적 test와 실제시각 integration test를 분리하고 한계를 명시한다.

### 10.5 각 Task 필수 필드

모든 Task는 다음 형식을 사용한다.

```text
Task ID / 제목
목적
관련 Baseline 위치
선행조건
생성·수정 파일
금지사항
RED test
실행명령
예상 실패
구현 단계
GREEN test
예상 성공
회귀시험
로그 경로
Rollback/Cleanup
완료조건
Commit checkpoint
```

### 10.6 계획에 반드시 포함할 Stage

#### Stage 0 — Baseline·Repository 보호

- Hash verifier
- Git status
- WinForms unchanged guard
- phase4 directory 생성
- database CLAUDE.md 계획

#### Stage 1 — Preflight

- SQL Server 연결
- Version >= 11
- Edition
- Compatibility Level
- KST offset
- 대상 DB 이름 검증
- sqlcmd encoding
- 권한 확인

#### Stage 2 — Physical Schema

- 7개 Table
- 7 PK
- 6 FK
- 2 UQ
- 1 Filtered UX
- 5 NCI
- 1 Sequence
- Trigger/TVP 없음 검증

#### Stage 3 — Seed

- Exam Master 19행
- NEX 13역할
- AEX 7역할
- EX012 역할중첩
- 평일·토요일 Holiday
- Seed idempotency
- 실제 개인정보 미사용

#### Stage 4 — Rule TVF

- 일정확인
- 검진대상확인
- 국가검사구성
- 추가검사확인
- 시간·나이·성별·완료이력·제외정보 경계시험

#### Stage 5 — SELECT SP 7개

```text
USP_HC_SELECT_공통업무상태
USP_HC_SELECT_수검자목록
USP_HC_SELECT_수검자상세
USP_HC_SELECT_수검자유효업무
USP_HC_SELECT_예약가능정보
USP_HC_SELECT_예약접수목록
USP_HC_SELECT_예약접수상세
```

- RS0
- 후속 Result Set 순서
- 0행 정상조회
- Cardinality
- Work data corruption 701

#### Stage 6 — Patient Write SP 2개

```text
USP_HC_INSERT_수검자
USP_HC_UPDATE_수검자정보
```

- SocialNumber 검증
- Birthday/Gender 산출
- ChartNo sequence
- 중복후보
- Existing patient
- LastEditDate
- 동일 SocialNumber 동시등록
- Patient 수정 vs 신규예약 경합

#### Stage 7 — Reservation Write SP 3개

```text
USP_HC_INSERT_예약
USP_HC_UPDATE_예약변경
USP_HC_UPDATE_예약취소
```

- Patient/Slot/Work Lock
- 19/20 경합
- 다른 유효업무
- 현재 Work 제외
- Date/Slot/AEX Scope
- NEX 8~11
- No-op
- RowVersion
- 기존/new Slot 정렬
- 교차이동 Deadlock 시험

#### Stage 8 — Reception Write SP 3개

```text
USP_HC_UPDATE_접수완료
USP_HC_UPDATE_접수추가검사
USP_HC_UPDATE_접수취소
```

- RSV→RCP
- RCP AEX 변경
- No-op
- RCP→CNL
- 접수완료 vs 예약취소 경합
- stale RowVersion

#### Stage 9 — Security

- Database Role
- User WITHOUT LOGIN 테스트 사용자
- 15개 SP 개별 EXECUTE
- Table 직접 접근 차단
- TVF 직접 접근 차단
- ownership chaining 검증
- 비밀정보 없음

#### Stage 10 — Rollback·Concurrency

- 부분저장 0건
- 2개 독립 Session
- barrier/synchronization 방식
- 정확히 하나 성공 검증
- Deadlock 로그
- cleanup

#### Stage 11 — Clean Rebuild

- 대상 DB exact match
- Drop/Recreate 사용자 승인 전제
- 전체 Deploy
- 전체 Test
- Object inventory
- 로그 보존

#### Stage 12 — 문서 최종화

- 실제 채택 SQL과 스펙 대조
- 실행로그 연결
- 잔여결함
- `06_DB_Transaction_Security_Seed.md` FINAL 후보 작성
- 00~05 Hash 재검증
- WinForms 변경 0건 검증

### 10.7 계획 종료 Gate

다음이 모두 충족되어야 `PHASE 4 IMPLEMENTATION READY`를 선언한다.

```text
승인된 Candidate Spec 존재
모든 grilling Decision 확정
모든 7 Table이 계획에 존재
모든 4 TVF가 계획에 존재
모든 15 SP가 계획에 존재
모든 8 Write SP Transaction/Lock Task 존재
모든 Result Set Contract 검증 Task 존재
모든 05 적대적 Test가 하나 이상의 Task에 추적
Clean Rebuild Task 존재
Security Task 존재
Baseline·WinForms 보호 Task 존재
실행 전 사용자 승인 Gate 존재
```

---

## 11. 구현계획에서 사용할 권장 Database 구조

이 구조를 시작점으로 사용하되 grilling에서 정당한 이유가 확인되면 `database` 내부만 조정할 수 있다. 기준선과 객체계약은 변경할 수 없다.

```text
database
├─ CLAUDE.md
├─ README.md
├─ Deploy.sql
├─ Rebuild.sql                     # 사용자 승인 후에만 실행 가능한 clean rebuild entry
│
├─ deploy
│  ├─ 00_Preflight.sql
│  ├─ 01_Schema.sql
│  ├─ 02_Seed.sql
│  ├─ 03_Functions.sql
│  ├─ 04_Procedures_Select.sql
│  ├─ 05_Procedures_Patient_Write.sql
│  ├─ 06_Procedures_Reservation_Write.sql
│  ├─ 07_Procedures_Reception_Write.sql
│  ├─ 08_Security.sql
│  └─ 09_Verify.sql
│
├─ tests
│  ├─ 00_Test_Harness.sql
│  ├─ 01_Schema_Tests.sql
│  ├─ 02_Seed_Tests.sql
│  ├─ 03_Rule_Tests.sql
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
│  └─ 14_Clean_Rebuild_Verify.sql
│
├─ scripts
│  ├─ deploy.sh
│  ├─ rebuild.sh
│  ├─ test.sh
│  ├─ concurrency-test.sh
│  ├─ verify-baseline.sh
│  └─ verify-winforms-unchanged.sh
│
├─ tools                            # 필요한 경우에만
│  └─ DbContractVerifier
│
└─ artifacts
   ├─ logs
   └─ reports
```

구조를 변경할 경우 다음 원칙을 지킨다.

```text
파일 하나당 명확한 책임
객체 의존순서 명확화
Deploy entry 1개
Clean rebuild entry 별도
Master Seed와 Test Fixture 분리
테스트용 영구 DB Table 추가 금지
로그와 Source 분리
```

---

## 12. 구현 전 반드시 해결할 기술 공격질문

Grilling과 스펙 self-review에서 아래 질문에 근거 있는 답이 있어야 한다.

### 12.1 Transaction·업무오류

- 업무오류를 발견한 시점에 Transaction이 열려 있다면 어떤 공통 패턴으로 Rollback하고 RS0를 반환하는가.
- CATCH에서 예상 가능한 Unique 충돌을 업무결과로 변환할지, 사전 Lock으로 방지할지.
- Commit 전에 Result Set을 출력하여 C#이 성공으로 읽을 위험은 없는가.
- No-op에서도 상태·동시성의 원자적 확인이 보장되는가.

### 12.2 Patient 동시성

- `LastEditDate` 비교와 행 잠금 순서는 무엇인가.
- DATETIME이 동일값을 재사용하지 않게 어떻게 단조 증가시키는가.
- 주민번호 변경이 활성 Work 부재를 확인한 뒤 신규예약이 끼어드는 경합을 어떻게 막는가.
- 동일 SocialNumber/ChartNo 동시등록이 SQL 2601/2627로 누출되지 않고 계약된 결과로 끝나는가.

### 12.3 Reservation 동시성

- 빈 Slot을 어떤 Resource로 직렬화하는가.
- 현재 Work가 20/20 Slot에 이미 포함될 때 AfterCount를 잘못 21로 계산하지 않는가.
- 예약변경에서 자기 Work를 306으로 오인하지 않는가.
- 두 예약이 서로 Slot을 교환할 때 Deadlock을 어떻게 방지하는가.
- Patient Lock과 Slot Lock의 총순서가 모든 SP에서 동일한가.

### 12.4 Aggregate

- Work Detail 실제변경 시 Work RowVersion이 반드시 변하는가.
- 동일 AEX 요청에서 Work/Detail이 전혀 갱신되지 않는가.
- 예약일 변경 실패 시 기존 Work와 Detail이 완전히 유지되는가.
- 접수·취소에서 Detail을 삭제하지 않는가.

### 12.5 Result Set

- 모든 외부 SP의 첫 Result Set이 RS0 1행인가.
- 실패 시 불필요한 후속 Result Set이 출력되지 않는가.
- INSERT_수검자의 202/203 예외 RS1은 정확히 반환되는가.
- `SELECT_예약가능정보` Scope별 0/2/7/8~11 Cardinality가 정확한가.
- SELECT 0건을 실패로 오인하지 않는가.

### 12.6 Seed

- Exam 19행이 정확한가.
- EX012가 한 행으로 NEX/AEX 역할을 동시에 표현하는가.
- AEX 코드가 OPT01~OPT07 정확히 7개인가.
- 실제 주민번호가 포함되지 않았음을 어떻게 검증하는가.
- 반복 실행 시 중복·오염이 없는가.

### 12.7 Security

- Application principal이 15개 SP만 호출할 수 있는가.
- Table 직접 DML이 실패하는가.
- TVF 직접호출이 실패하는가.
- dbo/sysadmin으로 실행한 테스트를 앱권한 테스트로 오인하지 않는가.

### 12.8 Rebuild

- 잘못된 서버·DB에서 Drop이 실행될 가능성을 어떻게 차단하는가.
- 빈 DB에서 동일 결과로 재구축되는가.
- 실행 중간 실패를 성공으로 오인하지 않는가.
- 로그에 secret이 남지 않는가.

---

## 13. Phase 4 실행 완료 Gate — 계획에 반드시 포함

실제 구현단계의 최종 완료조건은 다음과 같다. 지금은 계획만 작성하지만 모든 Gate를 구현계획에 넣어라.

| Gate | 검증내용 | 필수 결과 |
|---|---|---|
| G00 | Baseline Hash | 6/6 일치 |
| G01 | WinForms 보호 | 변경 0건 |
| G02 | Preflight | 안전한 개발 Instance·KST·Version 확인 |
| G03 | Clean Deploy | 빈 DB 전체 배포 성공 |
| G04 | Object Inventory | Table 7, TVF 4, SP 15, Sequence 1 |
| G05 | Schema | PK 7, FK 6, UQ 2, UX 1, NCI 5 |
| G06 | 금지 객체 | Trigger 0, TVP 0, DELETE SP 0, 추가 Table 0 |
| G07 | Seed | Exam 19, NEX 역할 13, AEX 역할 7, Holiday 2+ |
| G08 | Rule | TGT/NEX/AEX/HOL 경계 통과 |
| G09 | SP Contract | Parameter·RS0·후속 RS 순서·타입·Cardinality 일치 |
| G10 | Rollback | 부분저장 0건 |
| G11 | Concurrency | 고정 경합 시나리오 전부 통과 |
| G12 | Security | SP 실행 가능, 직접 Table/TVF 접근 차단 |
| G13 | SQL 2012 | Compatibility 110에서 전체 실행 |
| G14 | Repeatability | Rebuild 후 동일 결과 |
| G15 | Evidence | 실행명령·exit code·로그·보고서 존재 |
| G16 | 06 문서 | 실제 구현과 일치하는 FINAL 후보 |

`PASS`는 실제 실행 증거가 있을 때만 사용한다. 실행 전에는 `PLANNED`, `NOT RUN`, `BLOCKED` 중 하나를 사용한다.

---

## 14. 이번 최초 응답의 정확한 형식

이 프롬프트를 받은 첫 응답에서는 다음 순서만 수행하라.

1. `superpowers:brainstorming` 호출 사실과 성공 여부를 명시한다.
2. 작업을 architectural/multi-stage로 분류한다.
3. 현재 작업경로와 읽기/쓰기 경계를 재진술한다.
4. Repository를 읽기 전용으로 조사한다.
5. 00~05 파일·버전·Hash·XLSX Sheet를 검증한다.
6. 고정 구현수치를 요약한다.
7. 현재 SQL/도구 환경에서 자동 확인한 사실을 요약한다.
8. 발견된 기준선 충돌 또는 차단사항을 요약한다.
9. 질문으로 확인해야 할 항목을 내부 Decision Queue로 구성한다.
10. **가장 우선순위가 높은 grilling 질문 하나만** 사용자에게 제시한다.
11. 여기서 중단한다.

첫 응답에서 금지:

- SQL 파일 생성
- Candidate spec 작성 완료
- Implementation plan 작성 완료
- Database 생성
- Git 변경
- 한꺼번에 여러 질문
- 사용자 승인 없이 잠금방식 확정
- 구현 시작

첫 응답의 종료 예시:

```text
Baseline: VERIFIED 또는 BLOCKED
Implementation work performed: NONE
Current phase: GRILLING
Open decision count: N
Next decision: D4-001 ...
```

---

## 15. Grilling 완료 후 진행순서

모든 질문이 끝나면 다음 순서로 진행하라.

### 15.1 설계안 제시

설계를 아래 section으로 나누어 한 번에 과도하게 던지지 말고, section별로 설명하고 승인을 받는다.

```text
1. Environment·Deployment
2. File Structure
3. Transaction Pattern
4. Locking·Isolation
5. Seed·Test Data
6. TVF·SP Implementation
7. Result Set Contract Verification
8. Security
9. Test·Concurrency·Rebuild
10. Documentation·Evidence·Git
```

각 section에는 다음을 포함한다.

- baseline 근거
- 선택안
- 대안과 배제이유
- 위험
- 검증방법
- 사용자 승인 질문

### 15.2 Candidate spec 작성

모든 section 승인 후:

```text
../docs/phase4/06_DB_Transaction_Security_Seed_CANDIDATE.md
```

를 작성한다.

작성 후 자체 검수:

- 필수 목차 누락 0건
- 미승인 가정 0건
- 7 Table/4 TVF/15 SP 추적
- 8 Write SP Transaction/Lock 추적
- 05 Test 계약 추적
- 00~05 변경 0건
- WinForms 변경 0건

### 15.3 Candidate spec 승인

사용자에게 다음을 보고한다.

```text
작성 파일
결정 수
미결정 수
Deviation 수
구현 차단사항
추천 판정
```

사용자가 명시적으로 승인하기 전 `writing-plans`로 넘어가지 않는다.

### 15.4 Implementation plan 작성

승인 후 `superpowers:writing-plans`를 호출하여 `../docs/phase4/plans`에 계획을 작성한다.

### 15.5 Plan self-review

다음 관점으로 계획을 적대적으로 검수한다.

```text
Contract completeness
Dependency order
SQL 2012 compatibility
TDD presence
Exact commands
Expected exit codes
Concurrency reproducibility
Clean rebuild safety
Baseline protection
WinForms isolation
Context/file size manageability
```

결함이 있으면 계획 자체를 수정한 뒤 다시 검수한다.

### 15.6 최종 중단

계획 완료 후 실제 SQL을 구현하지 말고 다음 형식으로 보고한다.

```text
Superpowers brainstorming: USED
Superpowers writing-plans: USED
Spec: <path>
Plan: <path 또는 index+files>
Baseline integrity: VERIFIED
Open decisions: 0
Implementation blockers: 0 또는 목록
Files changed: planning docs only
Verdict: PHASE 4 IMPLEMENTATION READY 또는 BLOCKED

Next action requiring user approval:
- Execute with subagent-driven-development
- Execute with executing-plans
- Revise spec/plan
```

---

## 16. 품질 원칙

- 추측보다 검증을 우선한다.
- 문서에서 확인 가능한 사실을 사용자에게 다시 묻지 않는다.
- 실제 실행하지 않은 것을 성공했다고 말하지 않는다.
- 복잡성을 과제로 정당화하지 않는다.
- 7개 Table·4개 TVF·15개 SP를 불필요하게 통합·분할하지 않는다.
- 범용 Framework, ORM, Migration Framework, Rule Engine을 무단 도입하지 않는다.
- SQL Server 2012 호환성을 희생하지 않는다.
- UI 편의를 이유로 DB 계약을 바꾸지 않는다.
- Test 편의를 이유로 production backdoor를 넣지 않는다.
- 안전을 이유로 사용자의 bypass mode를 신뢰하지 말고 범위를 스스로 통제한다.
- 보고는 한국어로 작성하되 DB 객체명·Parameter·Result 컬럼은 기준선 표기를 그대로 사용한다.
- 오류와 불확실성은 숨기지 않는다.

---

## 17. 지금 시작할 작업

이제 다음 순서로 시작하라.

```text
1. superpowers:brainstorming 직접 호출
2. Repository 읽기 전용 조사
3. 00~05 Hash·메타데이터·XLSX 전수검증
4. 고정계약 추출
5. 환경 자동조사
6. 첫 grilling 질문 1개
7. 중단
```

SQL 구현은 시작하지 마라.

# END PROMPT
