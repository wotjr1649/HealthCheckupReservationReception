# 검진 예약·접수 관리 프로그램 — DB Rule·Stored Procedure 계약서

- **문서명:** `05_DB_Rule_SP_Contract.md`
- **상태:** FINAL / GO / READ-ONLY — Phase 3 Rule·Stored Procedure 계약 확정
- **문서 버전:** v1.1
- **기준일:** 2026-09-03
- **기준선 ID:** `HC-RSV-RCP-20260903-R2`
- **Solution:** `HealthCheckupReservationReception`
- **WinForms Project:** `HealthCheckupReservationReception.WinForms`
- **Database:** `HealthCheckupReservationReceptionDb`
- **DB Domain Prefix:** `HC = Health Checkup`
- **대상 환경:** C# WinForms / .NET Framework 4.6.1 / DevExpress Components 20.2 / Microsoft SQL Server 2012 이상 / Stored Procedure
- **확정 객체:** 외부 호출 Stored Procedure 15개 / 내부 Inline TVF 4개 / Trigger 0개 / TVP 0개 / DELETE SP 0개
- **기준문서:**
  - `00_Project_Policy.md` — FINAL / GO / READ-ONLY
  - `01_Process_Definition.md` — FINAL / GO / READ-ONLY
  - `02_Function_Definition.xlsx` — FINAL / GO / READ-ONLY
  - `03_Wireframe_Definition.md` — FINAL / GO / READ-ONLY
  - `04_DB_Design.md` — FINAL / GO / READ-ONLY
- **후속 문서:** `06_DB_Transaction_Security_Seed.md` → `07_UI_DB_Matrix_Final_Validation.md`
- **변경 통제:** 본 기준일 이후 본 문서를 수정하지 않는다. 후속 문서는 본 계약의 객체명, Parameter명·타입·NULL, Result Set 순서·컬럼, ResultCode, Rule 책임, 검증순서, 중복판정 제외조건 및 No-op 의미를 변경할 수 없다.

---

# 0. 문서 목적과 적용 경계

## 0.1 목적

본 문서는 `04_DB_Design.md`에서 확정한 7개 테이블을 변경하지 않고 다음 구현계약을 확정한다.

```text
Rule Function 이름·입력·출력·책임
Stored Procedure 이름·입력 Parameter
Stored Procedure Result Set 순서·컬럼·Cardinality
공통 ResultCode와 사용자 메시지
업무 실패 우선순위
예약일·시간대·TGT·NEX·AEX 평가순서
수검자·예약·접수 Write SP의 No-op 및 성공 반환
예약변경 중복판정의 현재 WorkId 제외
Phase 4 Transaction·잠금·권한·Seed 구현 인계
```

이 계약이 완료되면 C#과 DB 구현자는 추가 업무 해석 없이 동일한 인터페이스를 구현할 수 있어야 한다.

## 0.2 본 문서에서 변경하지 않는 항목

다음은 `04_DB_Design.md` 기준을 그대로 사용한다.

```text
물리 테이블 7개와 전체 컬럼
PK / FK / UQ / CK / DF / Sequence
INFO_PATIENTS.SocialNumber = 숫자 13자리 임의 테스트값
상태코드 RSV / RCP / CNL
시간대코드 AM / PM
AEX OPT01~OPT07의 7개 BIT 입력경계
Patient 동시성 = LastEditDate
Work Aggregate 동시성 = RowVersion
AEX 실제 변경 시 Work RowVersion 갱신
동일 AEX 집합 = No-op
예약변경 다른 유효업무 = 현재 WorkId 제외
NEX 실제 Cardinality = 8~11행
검색조건 AND / 정확검색 / 이름 접두검색
DB 서버 KST 기준시각
TVP / CSV / XML / JSON 미사용
Trigger 및 FK Cascade 미사용
```

## 0.3 본 문서의 후속 구현 경계

다음 상세 구현은 `06_DB_Transaction_Security_Seed.md`에서 확정한다.

```text
BEGIN TRAN / COMMIT / ROLLBACK 위치
SET XACT_ABORT ON
TRY/CATCH 및 THROW 본문
sp_getapplock 또는 UPDLOCK/HOLDLOCK 선택
잠금 Resource 문자열과 잠금 획득순서
SocialNumber 형식·고유성·실제 주민등록번호 사용 금지 검수 Script
검사·휴무일·완료이력·제외정보 Seed/Test Data
DB Role·GRANT EXECUTE·직접 DML 통제·배포 순서
실제 CREATE FUNCTION / CREATE PROCEDURE Script
```

후속 구현은 본 문서의 외부 계약을 바꾸지 않고 내부 SQL만 구체화한다.

## 0.4 Source of Truth

충돌 시 다음 순서를 적용한다.

```text
00_Project_Policy.md
→ 01_Process_Definition.md
→ 02_Function_Definition.xlsx
→ 03_Wireframe_Definition.md
→ 04_DB_Design.md
→ 05_DB_Rule_SP_Contract.md
→ 06~07 후속문서
→ DB Script / C# Source
```

# 1. 객체 및 네이밍 규칙

## 1.1 프로그램·DB 명칭

| 구분 | 확정 명칭 |
|---|---|
| Solution | `HealthCheckupReservationReception` |
| WinForms Project | `HealthCheckupReservationReception.WinForms` |
| Root Namespace | `HealthCheckupReservationReception` |
| Database | `HealthCheckupReservationReceptionDb` |
| Connection String Name | `HealthCheckupDb` |
| 사용자 화면 표시명 | `검진 예약·접수 관리 프로그램` |

## 1.2 DB 객체 접두사

| 접두사 | 의미 |
|---|---|
| `USP` | User Stored Procedure |
| `UFN` | User Function |
| `HC` | Health Checkup |

Stored Procedure는 다음 형식을 사용한다.

```text
[dbo].[USP_HC_{SELECT|INSERT|UPDATE}_{한글업무명}]
```

Function은 다음 형식을 사용한다.

```text
[dbo].[UFN_HC_{한글Rule명}]
```

## 1.3 외부 호출 Stored Procedure 15개

| ID | 물리 객체명 | 구분 | 책임 |
|---|---|:---:|---|
| SP-COM-01 | `[dbo].[USP_HC_SELECT_공통업무상태]` | SELECT | DB 현재 업무일·운영시간 상태 |
| SP-PAT-01 | `[dbo].[USP_HC_SELECT_수검자목록]` | SELECT | 수검자 검색 |
| SP-PAT-02 | `[dbo].[USP_HC_SELECT_수검자상세]` | SELECT | Patient 최신 상세 |
| SP-PAT-03 | `[dbo].[USP_HC_INSERT_수검자]` | INSERT | 신규등록·동일수검자·중복후보 처리 |
| SP-PAT-04 | `[dbo].[USP_HC_UPDATE_수검자정보]` | UPDATE | 수검자 수정·식별정보 검증 |
| SP-PAT-05 | `[dbo].[USP_HC_SELECT_수검자유효업무]` | SELECT | 현재일 이후 RSV/RCP 업무 확인 |
| SP-RSV-01 | `[dbo].[USP_HC_SELECT_예약가능정보]` | SELECT | 날짜·시간대·정원·TGT·NEX·AEX 사전정보 |
| SP-RSV-02 | `[dbo].[USP_HC_INSERT_예약]` | INSERT | Normal/WalkIn 예약 생성 |
| SP-RSV-03 | `[dbo].[USP_HC_UPDATE_예약변경]` | UPDATE | 예약일·시간대·AEX 영향범위 변경 |
| SP-RSV-04 | `[dbo].[USP_HC_UPDATE_예약취소]` | UPDATE | RSV → CNL |
| SP-WRK-01 | `[dbo].[USP_HC_SELECT_예약접수목록]` | SELECT | Workbench 공통 목록 |
| SP-WRK-02 | `[dbo].[USP_HC_SELECT_예약접수상세]` | SELECT | Work 상세·검사구성·Action 가능 여부 |
| SP-RCP-01 | `[dbo].[USP_HC_UPDATE_접수완료]` | UPDATE | RSV → RCP |
| SP-RCP-02 | `[dbo].[USP_HC_UPDATE_접수추가검사]` | UPDATE | RCP 상태 AEX 변경 |
| SP-RCP-03 | `[dbo].[USP_HC_UPDATE_접수취소]` | UPDATE | RCP → CNL |

## 1.4 내부 Inline TVF 4개

| ID | 물리 객체명 | 책임 |
|---|---|---|
| RF-COM-01 | `[dbo].[UFN_HC_일정확인]` | 업무일·운영시간·예약일·시간대·마감 확인 |
| RF-TGT-01 | `[dbo].[UFN_HC_검진대상확인]` | 예약일 기준 TGT 판정 |
| RF-NEX-01 | `[dbo].[UFN_HC_국가검사구성]` | 기본·조건부 NEX 구성 |
| RF-AEX-01 | `[dbo].[UFN_HC_추가검사확인]` | AEX 7종 선택 가능 여부와 유효 선택값 |

4개 Function은 모두 다음 제약을 따른다.

```text
Inline Table-Valued Function
단일 SELECT 반환
데이터 변경 없음
Transaction 없음
THROW 없음
C# 직접 호출 금지
Stored Procedure 내부에서만 사용
```

## 1.5 생성하지 않는 객체

```text
USP_HC_SELECT_수검자중복판정
USP_HC_SELECT_접수가능정보
USP_HC_SELECT_월간예약현황
USP_HC_DELETE_*
USP_HC_SAVE_*
USP_HC_PROCESS_*
@Mode 기반 범용 통합 SP
Scalar Wrapper UDF
업무 Trigger
```

## 1.6 Parameter·Result 컬럼 네이밍

외부 계약은 짧고 의미가 바로 드러나는 PascalCase 영문을 사용한다.

```text
PatientId      O
ReservationDate O
CurrentCount   O
CanSave        O
BlockMessage   O

Pid            X
RsvDt          X
CurCnt         X
SaveYn         X
BlkMsg         X
```

다음 긴 표현은 사용하지 않는다.

```text
EvaluationStatus
PrimaryBlockReason
EffectiveSelected
ProjectedReservationCount
ReservationContextCode
ResultKey
IsNoOp
ResultTarget
```

대신 다음을 사용한다.

```text
Scope
BlockCode / BlockMessage
Selected
AfterCount
ReservationType
Code
Field
```

## 1.7 SQL 식별자 작성 규칙

- 모든 DDL·호출 예시는 `[dbo].[객체명]` 형식으로 Schema를 명시한다.
- 한글 객체명에는 공백·하이픈·괄호·슬래시를 사용하지 않는다.
- SQL Script는 UTF-8로 저장한다.
- C# `CommandText`에는 `dbo.USP_HC_...` 전체 이름을 사용한다.
- 사용자 정의 객체에 `sp_` 접두사를 사용하지 않는다.

---

# 2. 공통 Parameter·값 정규화 계약

## 2.1 명시적 Parameter 전달

- 외부 SP Parameter에는 업무 Default를 두지 않는다.
- C#은 선택값을 포함한 모든 Parameter를 매 호출마다 명시적으로 전달한다.
- 선택 Parameter는 누락하지 않고 `DBNull.Value`로 전달한다.
- AEX 7개 `BIT`는 NULL을 허용하지 않으며 모두 명시한다.

## 2.2 문자열 정규화

Stored Procedure 입구에서 다음 규칙을 적용한다.

```text
가변 문자열 → LTRIM/RTRIM
빈 문자열   → NULL
영문 코드   → UPPER
휴대전화·전화번호 검색값 → '-' 제거
주민등록번호 → C#/UI가 '-'를 제거한 숫자 13자리로 전달하고 SP가 형식·날짜·코드를 재검증
```

- `@SocialNumber`의 SQL 타입은 `VARCHAR(13)`이므로 하이픈이 포함된 14자리 표시값을 SP에 전달하지 않는다.
- UI는 `000000-0000000` 형태로 표시할 수 있으나 C# 호출 DTO에는 하이픈을 제거한 13자리 값만 설정한다.

허용 코드:

```text
ReservationType : NORMAL / WALKIN
TimeSlot        : AM / PM
Status          : RSV / RCP / CNL
Scope           : ALL / SLOT / EXTRA / SLOT_EXTRA / NONE
ExamType        : BASIC / CONDITIONAL
```

## 2.3 DB 기준시각

모든 외부 SP는 시작 시 DB 기준시각을 한 번만 캡처한다.

```sql
DECLARE @ServerTime DATETIME2(7) = SYSDATETIME();
DECLARE @Today      DATE         = CONVERT(DATE, @ServerTime);
DECLARE @NowTime    TIME(7)      = CONVERT(TIME(7), @ServerTime);
DECLARE @StoredNow  DATETIME2(0) = CONVERT(DATETIME2(0), @ServerTime);
```

- DB 서버 현지시각은 KST(UTC+09:00)다.
- 운영시간·마감 비교에는 `@NowTime`을 사용한다.
- Work 시각 저장에는 `@StoredNow`를 사용한다.
- 한 번의 SP에서 UDF마다 현재시각을 다시 구하지 않고 동일 `@ServerTime`을 전달한다.

## 2.4 시간 경계

```text
09:00 <= 현재시각 < 18:00 → 공통 업무 가능
현재시각 < 마감시각       → 마감 전
현재시각 = 마감시각       → 마감 경과
```

| 업무 | AM 마감 | PM 마감 |
|---|---:|---:|
| 일반 당일예약 | 10:00 | 15:00 |
| WalkIn 당일예약 | 11:00 | 16:00 |
| 접수 | 11:00 | 16:00 |

## 2.5 `Field` 표기

RS0의 `Field`는 Parameter 이름에서 `@`를 제외한 문자열을 사용한다.

```text
PatientId
WorkId
RowVersion
ReservationDate
TimeSlot
AexOpt04Selected
```

특정 입력 하나로 귀속할 수 없는 오류는 `NULL`을 반환한다.

---

# 3. 공통 Result Set·오류 처리 계약

## 3.1 RS0 — 처리결과

모든 외부 SP의 첫 번째 Result Set은 정확히 1행이며 다음 순서·타입을 사용한다.

| 순서 | 컬럼 | SQL 타입 | NULL | 의미 |
|---:|---|---|:---:|---|
| 1 | `Success` | `BIT` | X | 요청 업무 또는 조회계약 처리 성공 여부 |
| 2 | `Code` | `INT` | X | C# 분기용 ResultCode |
| 3 | `Message` | `NVARCHAR(300)` | X | 사용자·로그 기본 메시지 |
| 4 | `Field` | `VARCHAR(50)` | O | 문제가 있는 입력항목 |
| 5 | `ServerTime` | `DATETIME2(7)` | X | SP 시작 시 캡처한 KST DB 시각 |

## 3.2 기본 성공값

| Success | Code | 의미 |
|:---:|---:|---|
| 1 | 0 | 정상 처리 |
| 1 | 1 | Write 요청이 실제 데이터 변경 없이 종료됨 |
| 1 | 2 | 동일 주민번호·동일 이름의 기존 수검자를 사용함 |

## 3.3 업무 불가 평가와 SP 실패의 분리

`USP_HC_SELECT_예약가능정보`에서 휴무일·정원 마감·TGT 비대상은 SP 실패가 아니다.

```text
RS0.Success = 1
RS0.Code    = 0
RS1.CanSave = 0
RS1.BlockCode / BlockMessage = 실제 예약 차단사유
```

존재하지 않는 PatientId, 잘못된 Parameter 조합, Work 상태·동시성 오류는 SP 실패다.

```text
RS0.Success = 0
RS0.Code    = 해당 오류코드
```

## 3.4 조회 0건

검색 결과가 없는 것은 정상이다.

```text
RS0: Success=1, Code=0
RS1: 0행
```

## 3.5 실패 시 Result Set

기본 원칙:

```text
업무·호출계약 실패 → RS0만 반환
```

예외:

```text
USP_HC_INSERT_수검자
Code=202 또는 203
→ RS0 실패와 함께 RS1 수검자결과를 반환
```

이 예외는 기존 동일 주민번호 정보 확인 또는 중복후보 Dialog 표시를 위해 필요하다.

## 3.6 예상하지 못한 SQL 오류

```text
예상 가능한 업무 실패 → RS0
예상하지 못한 SQL/시스템 오류 → THROW
```

업무결과 전달에 다음을 사용하지 않는다.

```text
OUTPUT Parameter
SQL RETURN 값
업무실패용 RAISERROR
Message 문자열 비교에 의한 C# 분기
```

C#은 `Code`를 Enum으로 매핑하여 분기한다.

---

# 4. ResultCode 최종 Catalog

## 4.1 코드 영역

| 범위 | 영역 |
|---:|---|
| `0~9` | 성공·No-op·기존수검자 사용 |
| `100~199` | 입력값·검색조건·호출조합 |
| `200~299` | 수검자·식별정보 |
| `300~399` | 예약일·시간대·정원·마감 |
| `400~499` | 검진대상·검사항목 |
| `500~599` | Work·상태전이 |
| `600~699` | 낙관적 동시성 |
| `700~799` | Master·Aggregate 데이터 구성 |
| `900 이상` | 사용하지 않음. 예상하지 못한 오류는 THROW |

## 4.2 코드 목록

| Code | C# Enum | 기본 Message | 기본 Field |
|---:|---|---|---|
| 0 | `Ok` | 정상 처리되었습니다. | NULL |
| 1 | `NoChange` | 변경된 내용이 없습니다. | NULL |
| 2 | `ExistingPatient` | 동일한 수검자가 이미 등록되어 있어 기존 정보를 사용합니다. | NULL |
| 100 | `MissingValue` | 필수값을 입력하십시오. | 해당 Field |
| 101 | `BadValue` | 입력값이 올바르지 않습니다. | 해당 Field |
| 102 | `BadRequest` | 함께 사용할 수 없는 입력값 조합입니다. | 해당 Field 또는 NULL |
| 103 | `NeedSearchCondition` | 조회조건을 하나 이상 입력하십시오. | NULL |
| 104 | `BadDateRange` | 시작일은 종료일보다 늦을 수 없습니다. | `FromDate` |
| 200 | `PatientNotFound` | 수검자를 찾을 수 없습니다. | `PatientId` |
| 201 | `ChartNoUsed` | 이미 사용 중인 차트번호입니다. | `ChartNo` |
| 202 | `SameNumberDifferentName` | 동일한 주민등록번호의 기존 수검자와 이름이 다릅니다. | `Name` |
| 203 | `SimilarPatient` | 이름과 생년월일이 같은 수검자가 있습니다. | NULL |
| 204 | `SocialNumberUsed` | 다른 수검자가 사용 중인 주민등록번호입니다. | `SocialNumber` |
| 205 | `SocialChangeBlocked` | 예약 또는 접수완료 업무가 있어 주민등록번호를 변경할 수 없습니다. | `SocialNumber` |
| 206 | `ChartNoLimit` | 자동 차트번호 발급범위를 초과했습니다. | `ChartNo` |
| 300 | `PastDate` | 과거 날짜는 예약할 수 없습니다. | `ReservationDate` |
| 301 | `Sunday` | 일요일은 업무일이 아닙니다. | `ReservationDate` |
| 302 | `Holiday` | 선택한 날짜는 휴무일입니다. | `ReservationDate` |
| 303 | `SlotClosed` | 선택한 시간대는 운영하지 않습니다. | `TimeSlot` |
| 304 | `CutoffPassed` | 해당 시간대의 마감시간이 지났습니다. | `TimeSlot` |
| 305 | `SlotFull` | 해당 시간대의 예약 정원이 마감되었습니다. | `TimeSlot` |
| 306 | `OtherReservation` | 수검자에게 다른 유효 예약 또는 접수 업무가 있습니다. | `PatientId` |
| 307 | `NoOpenSlot` | 선택할 수 있는 시간대가 없습니다. | `TimeSlot` |
| 308 | `CenterClosed` | 오늘은 업무일이 아닙니다. | NULL |
| 309 | `OutsideHours` | 현재는 업무 운영시간이 아닙니다. | NULL |
| 400 | `UnderAge` | 예약일 기준 만 20세 미만으로 검진 대상이 아닙니다. | `ReservationDate` |
| 401 | `NotDue` | 일반건강검진 2년 주기가 도래하지 않았습니다. | `ReservationDate` |
| 410 | `ExamOff` | 현재 사용할 수 없는 추가검사입니다. | 해당 AEX Field |
| 411 | `WrongGender` | 성별 조건을 충족하지 않는 추가검사입니다. | 해당 AEX Field |
| 412 | `ExamDuplicate` | 일반건강검진에 포함된 검사입니다. | 해당 AEX Field |
| 500 | `WorkNotFound` | 예약·접수 업무를 찾을 수 없습니다. | `WorkId` |
| 501 | `WrongPatient` | 요청한 수검자와 예약·접수 업무의 수검자가 다릅니다. | `PatientId` |
| 502 | `WrongStatus` | 현재 상태에서는 요청한 업무를 처리할 수 없습니다. | `WorkId` |
| 503 | `NotToday` | 예약일이 오늘인 업무만 접수할 수 있습니다. | `WorkId` |
| 600 | `PatientChanged` | 다른 사용자가 수검자 정보를 변경했습니다. 최신 정보를 다시 조회하십시오. | `LastEditDate` |
| 601 | `WorkChanged` | 다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오. | `RowVersion` |
| 700 | `ExamSetupError` | 검사 Master 구성이 올바르지 않습니다. | NULL |
| 701 | `WorkDataError` | 예약·접수 업무의 검사구성 또는 유효업무 데이터가 올바르지 않습니다. | `WorkId` 또는 NULL |

## 4.3 동일 코드의 메시지 보정

`101`, `102`, `502`, `700`, `701`은 SP 문맥에 맞게 `Message`를 더 구체적으로 반환할 수 있다. C# 분기는 숫자 `Code`만 사용하며 메시지 문구를 비교하지 않는다.

# 5. 공통 오류 우선순위

여러 오류가 동시에 성립하면 다음 순서로 대표 오류 한 건을 선택한다.

```text
1. 필수값 누락
2. 값 형식·허용값
3. Parameter 조합
4. Entity 존재
5. Patient–Work 소유관계
6. 현재 Work 상태
7. 동시성값
8. Master·Aggregate 데이터 구성
9. 현재 업무일·운영시간
10. 기존 유효예약
11. 예약일
12. 시간대 운영 여부
13. 마감시간
14. 정원
15. 검진대상
16. 추가검사
```

예:

```text
다른 사용자가 RSV를 RCP로 변경하여 상태와 RowVersion이 모두 달라짐
→ 502 WrongStatus 우선
→ 601 WorkChanged는 반환하지 않음
```

상태가 바뀌었다는 사실이 사용자에게 더 직접적인 원인이기 때문이다.

---

# 6. Rule Function 계약

## 6.1 `[dbo].[UFN_HC_일정확인]`

### 6.1.1 목적

현재 공통 업무 가능 여부와 요청 예약일·시간대·마감 가능 여부를 한 행으로 반환한다.

### 6.1.2 입력

| Parameter | 타입 | NULL | 의미 |
|---|---|:---:|---|
| `@ServerTime` | `DATETIME2(7)` | X | 호출 SP가 한 번 캡처한 KST 시각 |
| `@ReservationDate` | `DATE` | X | 확인할 예약일 |
| `@TimeSlot` | `CHAR(2)` | X | `AM` / `PM` |
| `@CutoffType` | `VARCHAR(10)` | X | `NORMAL` / `RECEPTION` / `NONE` |

`@CutoffType` 의미:

| 값 | 적용업무 |
|---|---|
| `NORMAL` | 일반 당일예약·기존 예약의 일정변경 |
| `RECEPTION` | WalkIn 당일예약·접수 |
| `NONE` | 마감 미적용 조회 |

### 6.1.3 반환

정상 입력이면 정확히 1행을 반환한다.

| 순서 | 컬럼 | 타입 | NULL | 의미 |
|---:|---|---|:---:|---|
| 1 | `CanWorkNow` | `BIT` | X | 현재일·현재시각 기준 Write 업무 가능 여부 |
| 2 | `WorkCode` | `INT` | X | `0`, `308`, `309` |
| 3 | `WorkMessage` | `NVARCHAR(300)` | X | 현재 업무 가능 여부 설명 |
| 4 | `IsBusinessDay` | `BIT` | X | 요청일이 월~토이며 활성 휴무일이 아닌가 |
| 5 | `HolidayName` | `NVARCHAR(100)` | O | 활성 휴무일명 |
| 6 | `IsOpen` | `BIT` | X | 요청일에 해당 시간대를 운영하는가 |
| 7 | `CutoffTime` | `TIME(0)` | O | 요청일이 오늘이고 마감 적용 시각이 있을 때만 반환 |
| 8 | `CutoffPassed` | `BIT` | X | 현재시각이 마감시각 이상인가 |
| 9 | `CanUse` | `BIT` | X | 과거일·요일·휴무일·시간대·마감 통과 여부 |
| 10 | `ReasonCode` | `INT` | X | `0`, `300~304` |
| 11 | `ReasonMessage` | `NVARCHAR(300)` | X | 요청 일정 차단사유 |

### 6.1.4 책임

포함:

```text
현재일 업무일 여부
09:00 <= 현재시각 < 18:00
요청일 과거 여부
일요일
활성 휴무일
토요일 PM
NORMAL 10:00 / 15:00
RECEPTION 11:00 / 16:00
```

제외:

```text
시간대 정원
다른 유효예약
TGT / NEX / AEX
Work 상태와 RowVersion
Transaction과 잠금
```

### 6.1.5 우선순위

요청 일정 `ReasonCode`:

```text
300 PastDate
→ 301 Sunday
→ 302 Holiday
→ 303 SlotClosed
→ 304 CutoffPassed
```

요일 계산은 `SET DATEFIRST`에 의존하지 않는다.

---

## 6.2 `[dbo].[UFN_HC_검진대상확인]`

### 6.2.1 입력

| Parameter | 타입 | NULL | 의미 |
|---|---|:---:|---|
| `@PatientId` | `BIGINT` | X | 수검자 |
| `@ReservationDate` | `DATE` | X | 대상판정 기준일 |

### 6.2.2 반환

| 순서 | 컬럼 | 타입 | NULL | 의미 |
|---:|---|---|:---:|---|
| 1 | `Eligible` | `BIT` | X | 일반건강검진 대상 여부 |
| 2 | `Age` | `INT` | X | 예약일 기준 만 나이 |
| 3 | `LastCheckupDate` | `DATE` | O | 예약일 이전 가장 최근 완료일 |
| 4 | `ReasonCode` | `INT` | X | `0`, `400`, `401` |
| 5 | `ReasonMessage` | `NVARCHAR(300)` | X | 대상판정 설명 |

Cardinality:

```text
Patient 존재 → 정확히 1행
Patient 없음 → 0행
```

호출 SP가 Patient 존재를 먼저 확인한다.

### 6.2.3 판정식

```text
Age >= 20
AND
(
  예약일 이전 완료이력 없음
  OR YEAR(예약일) - YEAR(최근 완료일) >= 2
)
```

완료이력 범위:

```text
CompletionDate < ReservationDate
```

예약일 당일 및 이후 완료이력은 사용하지 않는다.

---

## 6.3 `[dbo].[UFN_HC_국가검사구성]`

### 6.3.1 입력

| Parameter | 타입 | NULL |
|---|---|:---:|
| `@PatientId` | `BIGINT` | X |
| `@ReservationDate` | `DATE` | X |

### 6.3.2 반환

| 순서 | 컬럼 | 타입 | NULL | 의미 |
|---:|---|---|:---:|---|
| 1 | `ExamCode` | `VARCHAR(10)` | X | EX001~EX013 |
| 2 | `ExamName` | `NVARCHAR(100)` | X | 검사명 |
| 3 | `ExamType` | `VARCHAR(12)` | X | `BASIC` / `CONDITIONAL` |
| 4 | `RuleCode` | `VARCHAR(10)` | X | NEX-01~NEX-06 |

Cardinality:

```text
TGT 비대상 → 0행
TGT 대상   → 8~11행
```

정렬:

```text
ExamCode ASC
```

### 6.3.3 기본검사

```text
EX001~EX008
ExamType=BASIC
RuleCode=NEX-01
```

### 6.3.4 조건부검사

| ExamCode | RuleCode | 조건 |
|---|---|---|
| EX009 | NEX-02 | 남성: 만 24세 이상이며 `(Age-24)%4=0`; 여성: 만 40세 이상이며 `(Age-40)%4=0` |
| EX010 | NEX-03 | 만 40세이며 B형간염 제외행이 없음 |
| EX011 | NEX-04 | 만 56세 |
| EX012 | NEX-05 | 여성 만 54·60·66세 |
| EX013 | NEX-06 | 만 56·66세 |

Function은 내부적으로 TGT를 확인하며 비대상자에게 NEX를 반환하지 않는다. 조건 조합상 동시에 추가될 수 있는 조건부 검사는 최대 3종이므로 실제 최대는 기본 8종을 포함한 11행이다.

## 6.4 `[dbo].[UFN_HC_추가검사확인]`

### 6.4.1 입력

| Parameter | 타입 | NULL | 의미 |
|---|---|:---:|---|
| `@PatientId` | `BIGINT` | X | 수검자 |
| `@ReservationDate` | `DATE` | X | 신규·예약일변경 시 기준일 |
| `@WorkId` | `BIGINT` | O | 저장된 NEX 사용 시 필수 |
| `@UseSavedExams` | `BIT` | X | 0=새 NEX, 1=Work의 저장 NEX |
| `@AexOpt01Selected` | `BIT` | X | OPT01 요청 선택값 |
| `@AexOpt02Selected` | `BIT` | X | OPT02 요청 선택값 |
| `@AexOpt03Selected` | `BIT` | X | OPT03 요청 선택값 |
| `@AexOpt04Selected` | `BIT` | X | OPT04 요청 선택값 |
| `@AexOpt05Selected` | `BIT` | X | OPT05 요청 선택값 |
| `@AexOpt06Selected` | `BIT` | X | OPT06 요청 선택값 |
| `@AexOpt07Selected` | `BIT` | X | OPT07 요청 선택값 |

조합:

```text
UseSavedExams=0 → WorkId NULL 허용, 예약일 기준 TGT/NEX 사용
UseSavedExams=1 → WorkId 필수, Work에 저장된 NEX 사용
```

호출 SP가 조합을 먼저 검증한다.

### 6.4.2 반환

검사 Master가 정상이라면 `OPT01~OPT07` 정확히 7행을 반환한다.

| 순서 | 컬럼 | 타입 | NULL | 의미 |
|---:|---|---|:---:|---|
| 1 | `OptionCode` | `VARCHAR(10)` | X | OPT01~OPT07 |
| 2 | `ExamCode` | `VARCHAR(10)` | X | 공통 검사코드 |
| 3 | `ExamName` | `NVARCHAR(100)` | X | 검사명 |
| 4 | `Requested` | `BIT` | X | 호출자가 요청한 선택값 |
| 5 | `Selected` | `BIT` | X | Rule 적용 후 실제 유효 선택값 |
| 6 | `CanSelect` | `BIT` | X | 화면 Checkbox 활성 여부 |
| 7 | `ReasonCode` | `INT` | X | `0`, `400`, `401`, `410~412` |
| 8 | `ReasonMessage` | `NVARCHAR(300)` | X | 선택불가 사유 |

정렬:

```text
OptionCode ASC
```

### 6.4.3 판정순서

새 예약일 기준:

```text
TGT 비대상
→ AdditionalActive=0
→ 성별 불충족
→ NEX 동일 ExamCode
```

저장된 NEX 기준:

```text
AdditionalActive=0
→ 성별 불충족
→ 저장된 NEX 동일 ExamCode
```

예:

```text
Requested=1
CanSelect=0
Selected=0
ReasonCode=412
ReasonMessage=일반건강검진에 포함된 검사입니다.
```

선택하지 않은 비활성·성별제한 항목은 행에 사유를 표시하지만 저장을 차단하지 않는다. `Requested=1`인 무효 항목만 Write를 차단한다.

---

# 7. 공통·수검자 SELECT SP 계약

## 7.1 `[dbo].[USP_HC_SELECT_공통업무상태]`

### 입력

```text
없음
```

### Result Set

```text
RS0 처리결과
RS1 공통업무상태
```

RS1:

| 순서 | 컬럼 | 타입 | NULL |
|---:|---|---|:---:|
| 1 | `Today` | `DATE` | X |
| 2 | `DayName` | `NVARCHAR(10)` | X |
| 3 | `HolidayName` | `NVARCHAR(100)` | O |
| 4 | `OpenTime` | `TIME(0)` | X |
| 5 | `CloseTime` | `TIME(0)` | X |
| 6 | `IsBusinessDay` | `BIT` | X |
| 7 | `WithinHours` | `BIT` | X |
| 8 | `CanWorkNow` | `BIT` | X |
| 9 | `BlockCode` | `INT` | X |
| 10 | `BlockMessage` | `NVARCHAR(300)` | X |

```text
OpenTime=09:00
CloseTime=18:00
BlockCode=0 / 308 / 309
```

조회 자체는 업무시간 밖에도 허용한다.

---

## 7.2 `[dbo].[USP_HC_SELECT_수검자목록]`

### 입력

| Parameter | 타입 | NULL | 검색방식 |
|---|---|:---:|---|
| `@ChartNo` | `NVARCHAR(100)` | O | 정확검색 |
| `@Name` | `NVARCHAR(100)` | O | 접두검색 |
| `@SocialNumber` | `VARCHAR(13)` | O | C#에서 `-` 제거 후 정확검색 |
| `@Birthday` | `VARCHAR(8)` | O | 정확검색 |
| `@MobilePhone` | `VARCHAR(13)` | O | `-` 제거 후 정확검색 |

- 입력된 조건은 모두 `AND`로 결합한다.
- 모든 조건이 NULL이면 `Code=103`이다.
- 주민번호 검색값은 숫자 13자리 형식을 검증하며 실제 주민등록번호를 입력하지 않는다.

### Result Set

```text
RS0 처리결과
RS1 수검자목록
```

RS1:

| 컬럼 | 타입 | NULL | 물리 출처 |
|---|---|:---:|---|
| `PatientId` | `BIGINT` | X | PatientId |
| `ChartNo` | `NVARCHAR(100)` | X | ChartNo |
| `Name` | `NVARCHAR(100)` | X | Name |
| `SocialNumber` | `VARCHAR(13)` | X | SocialNumber |
| `Birthday` | `VARCHAR(8)` | X | Birthday |
| `Gender` | `CHAR(1)` | X | Gender |
| `MobilePhone` | `VARCHAR(13)` | O | CelNumber |
| `Phone` | `VARCHAR(13)` | O | TelNumber |
| `Email` | `VARCHAR(200)` | O | EMail |
| `Zipcode` | `VARCHAR(10)` | O | Zipcode |
| `Address` | `NVARCHAR(200)` | O | Address |

정렬:

```text
Name ASC, Birthday ASC, ChartNo ASC
```

`SocialNumber`는 과제에서 사용하는 임의 테스트값이며 화면 표시 위치에서는 전체 13자리를 제공한다.

## 7.3 `[dbo].[USP_HC_SELECT_수검자상세]`

### 입력

| Parameter | 타입 | NULL |
|---|---|:---:|
| `@PatientId` | `BIGINT` | X |

### Result Set

```text
RS0 처리결과
RS1 수검자상세
```

RS1은 정확히 1행이다.

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `PatientId` | `BIGINT` | X |
| `ChartNo` | `NVARCHAR(100)` | X |
| `Name` | `NVARCHAR(100)` | X |
| `SocialNumber` | `VARCHAR(13)` | X |
| `Birthday` | `VARCHAR(8)` | X |
| `Gender` | `CHAR(1)` | X |
| `MobilePhone` | `VARCHAR(13)` | O |
| `Phone` | `VARCHAR(13)` | O |
| `Email` | `VARCHAR(200)` | O |
| `Zipcode` | `VARCHAR(10)` | O |
| `Address` | `NVARCHAR(200)` | O |
| `AddressDetail` | `NVARCHAR(200)` | O |
| `Memo` | `NVARCHAR(MAX)` | O |
| `LastEditDate` | `DATETIME` | X |

Patient가 없으면 `Code=200`이다.

---

## 7.4 `[dbo].[USP_HC_SELECT_수검자유효업무]`

### 입력

| Parameter | 타입 | NULL |
|---|---|:---:|
| `@PatientId` | `BIGINT` | X |

### 조회범위

```text
PatientId 일치
AND ReservationDate >= DB Today
AND Status IN ('RSV','RCP')
```

### Result Set

```text
RS0 처리결과
RS1 유효업무
```

RS1:

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `WorkId` | `BIGINT` | X |
| `ReservationDate` | `DATE` | X |
| `TimeSlot` | `CHAR(2)` | X |
| `Status` | `CHAR(3)` | X |
| `StatusName` | `NVARCHAR(10)` | X |
| `IsToday` | `BIT` | X |
| `RowVersion` | `BINARY(8)` | X |

정상 Cardinality:

```text
0행 또는 1행
```

2행 이상이면 RP-06 불변조건 위반으로 `Code=701`을 반환한다.

---

# 8. Workbench SELECT SP 계약

## 8.1 `[dbo].[USP_HC_SELECT_예약접수목록]`

### 입력

| Parameter | 타입 | NULL | 의미 |
|---|---|:---:|---|
| `@FromDate` | `DATE` | O | 시작일 포함 |
| `@ToDate` | `DATE` | O | 종료일 포함 |
| `@Status` | `CHAR(3)` | O | NULL / RSV / RCP / CNL |
| `@ChartNo` | `NVARCHAR(100)` | O | 정확검색 |
| `@Name` | `NVARCHAR(100)` | O | 접두검색 |

규칙:

```text
FromDate > ToDate → 104
모든 조건 없음   → 103
Status=NULL은 조회조건으로 보지 않음
입력된 조건은 AND
```

### Result Set

```text
RS0 처리결과
RS1 예약접수목록
```

RS1:

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `WorkId` | `BIGINT` | X |
| `PatientId` | `BIGINT` | X |
| `ReservationDate` | `DATE` | X |
| `TimeSlot` | `CHAR(2)` | X |
| `Status` | `CHAR(3)` | X |
| `StatusName` | `NVARCHAR(10)` | X |
| `Name` | `NVARCHAR(100)` | X |
| `ChartNo` | `NVARCHAR(100)` | X |
| `Gender` | `CHAR(1)` | X |
| `Birthday` | `VARCHAR(8)` | X |
| `MobilePhone` | `VARCHAR(13)` | O |

정렬:

```text
ReservationDate ASC, TimeSlot ASC, Name ASC, WorkId ASC
```

---

## 8.2 `[dbo].[USP_HC_SELECT_예약접수상세]`

### 입력

| Parameter | 타입 | NULL |
|---|---|:---:|
| `@WorkId` | `BIGINT` | X |

### Result Set 순서

```text
RS0 처리결과
RS1 업무상세
RS2 국가검사항목
RS3 추가검사항목
RS4 가능한업무
```

### RS1 업무상세

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `WorkId` | `BIGINT` | X |
| `PatientId` | `BIGINT` | X |
| `ChartNo` | `NVARCHAR(100)` | X |
| `Name` | `NVARCHAR(100)` | X |
| `Birthday` | `VARCHAR(8)` | X |
| `Gender` | `CHAR(1)` | X |
| `MobilePhone` | `VARCHAR(13)` | O |
| `ReservationDate` | `DATE` | X |
| `TimeSlot` | `CHAR(2)` | X |
| `Status` | `CHAR(3)` | X |
| `StatusName` | `NVARCHAR(10)` | X |
| `Capacity` | `INT` | X |
| `CurrentCount` | `INT` | X |
| `SeatsLeft` | `INT` | X |
| `RowVersion` | `BINARY(8)` | X |

```text
Capacity=20
CurrentCount=같은 날짜·시간대의 RSV+RCP
SeatsLeft=MAX(0, 20-CurrentCount)
```

### RS2 국가검사항목

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `ExamCode` | `VARCHAR(10)` | X |
| `ExamName` | `NVARCHAR(100)` | X |
| `ExamType` | `VARCHAR(12)` | X |
| `RuleCode` | `VARCHAR(10)` | X |

실제 저장된 `ExamSourceCode='NEX'`만 반환한다.

### RS3 추가검사항목

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `OptionCode` | `VARCHAR(10)` | X |
| `ExamCode` | `VARCHAR(10)` | X |
| `ExamName` | `NVARCHAR(100)` | X |

실제 저장된 `ExamSourceCode='AEX'`만 반환한다.

### RS4 가능한업무

정확히 5행을 반환한다.

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `ActionCode` | `VARCHAR(30)` | X |
| `Allowed` | `BIT` | X |
| `ReasonCode` | `INT` | X |
| `ReasonMessage` | `NVARCHAR(300)` | X |

고정 `ActionCode`:

```text
EDIT_RESERVATION
CANCEL_RESERVATION
START_RECEPTION
EDIT_EXTRA
CANCEL_RECEPTION
```

허용조건:

| ActionCode | 허용조건 | 차단 우선순위 |
|---|---|---|
| EDIT_RESERVATION | RSV + 현재 공통 업무 가능 | 502 → 308/309 |
| CANCEL_RESERVATION | RSV + 현재 공통 업무 가능 | 502 → 308/309 |
| START_RECEPTION | RSV + 예약일=오늘 + 현재 공통 업무 가능 + 접수마감 전 | 502 → 308/309 → 503 → 304 |
| EDIT_EXTRA | RCP + 현재 공통 업무 가능 | 502 → 308/309 |
| CANCEL_RECEPTION | RCP + 현재 공통 업무 가능 | 502 → 308/309 |

조회는 업무시간 밖에도 성공하며 `Allowed=0`으로 반환한다.

Work가 없으면 `500`, 저장 NEX가 없거나 검사구성이 손상됐으면 `701`이다.

---

# 9. `[dbo].[USP_HC_SELECT_예약가능정보]` 계약

## 9.1 목적

WinForms의 날짜 선택·시간대 선택·예약변경 화면에서 다음 정보를 한 번에 반환한다.

```text
선택일 업무 가능 여부
AM/PM 현재 인원·적용 후 인원·잔여자리
일반/WalkIn 마감
기존 유효예약 충돌
TGT 대상판정
NEX 구성
AEX 7종 선택 가능 여부
현재 입력으로 저장 가능한지
```

달력 Cell Paint·Hover에서는 호출하지 않고 날짜 선택이 확정된 시점에 호출한다.

`@TimeSlot`이 NULL이어도 SP 내부에서는 `[dbo].[UFN_HC_일정확인]`을 `AM`, `PM`으로 각각 호출하여 두 시간대 정보를 만든다.

## 9.2 입력 Signature

```sql
CREATE PROCEDURE [dbo].[USP_HC_SELECT_예약가능정보]
    @PatientId             BIGINT,
    @WorkId                BIGINT,
    @RowVersion            BINARY(8),
    @ReservationType       VARCHAR(10),
    @ReservationDate       DATE,
    @TimeSlot              CHAR(2),
    @AexOpt01Selected      BIT,
    @AexOpt02Selected      BIT,
    @AexOpt03Selected      BIT,
    @AexOpt04Selected      BIT,
    @AexOpt05Selected      BIT,
    @AexOpt06Selected      BIT,
    @AexOpt07Selected      BIT
AS
BEGIN
    SET NOCOUNT ON;
END;
```

## 9.3 NULL·조합 계약

| 업무 | WorkId | RowVersion | ReservationType | TimeSlot |
|---|---|---|---|---|
| 신규 일반예약 | NULL | NULL | NORMAL | 날짜 선택 중 NULL 가능 |
| 신규 WalkIn | NULL | NULL | WALKIN | 날짜 선택 중 NULL 가능 |
| 예약변경 | 필수 | 필수 | NORMAL | 예약일 변경 직후 재선택 중 NULL 가능 |

오류 조합:

```text
WorkId NULL + RowVersion NOT NULL       → 102
WorkId NOT NULL + RowVersion NULL       → 100
WorkId NOT NULL + ReservationType=WALKIN→ 102
ReservationType=WALKIN + Date<>DB Today → 102
기존 날짜 유지 + TimeSlot NULL          → 102
AEX BIT 중 하나라도 NULL                → 100
```

## 9.4 예약변경 Scope

SP가 DB 현재값과 요청값을 비교한다.

```text
DateChanged
SlotChanged
ExtraChanged
```

| 실제 변경 | Scope | 실행 Rule |
|---|---|---|
| 신규예약 | ALL | 일정 + TGT + NEX + AEX |
| 예약일 변경 포함 | ALL | 일정 + TGT + NEX + AEX |
| 시간대만 변경 | SLOT | 일정·정원·중복만 |
| AEX만 변경 | EXTRA | 저장 NEX 기준 AEX만 |
| 시간대+AEX | SLOT_EXTRA | 일정·정원·중복 + 저장 NEX 기준 AEX |
| 변경 없음 | NONE | 존재·상태·동시성 확인 후 종료 |

`Scope=NONE`은 조회 SP의 정상 결과이므로 `RS0.Success=1`, `RS0.Code=0`으로 반환한다. `Code=1`은 실제 Write SP의 No-op에만 사용한다.

시간대만 변경하는 경우 현재 AEX 코드는 `ExtraChanged` 집합 비교에만 사용한다. TGT/NEX/AEX Rule을 재평가하지 않고 NEX/AEX Detail을 재작성하지 않는다.

다른 유효업무 조회조건은 다음과 같이 고정한다.

```text
신규예약:
PatientId 일치
AND ReservationDate >= DB Today
AND StatusCode IN ('RSV','RCP')

예약변경:
위 조건
AND WorkId <> @WorkId
```

- 예약변경 대상 Work 자신은 `OtherWorkId` 후보에서 제외한다.
- 다른 유효업무 0건이면 충돌 없음, 1건이면 `OtherWorkId`를 반환한다.
- 다른 유효업무가 2건 이상이면 RP-06 불변조건이 이미 손상된 상태이므로 `RS0.Code=701`을 반환한다.

## 9.5 Result Set 순서

```text
RS0 처리결과
RS1 예약요약
RS2 시간대정보
RS3 검진대상
RS4 국가검사항목
RS5 추가검사항목
```

## 9.6 RS1 예약요약

정확히 1행이다.

| 순서 | 컬럼 | 타입 | NULL |
|---:|---|---|:---:|
| 1 | `Scope` | `VARCHAR(12)` | X |
| 2 | `PatientId` | `BIGINT` | X |
| 3 | `WorkId` | `BIGINT` | O |
| 4 | `ReservationType` | `VARCHAR(10)` | X |
| 5 | `ReservationDate` | `DATE` | X |
| 6 | `TimeSlot` | `CHAR(2)` | O |
| 7 | `DateChanged` | `BIT` | O |
| 8 | `SlotChanged` | `BIT` | O |
| 9 | `ExtraChanged` | `BIT` | O |
| 10 | `CanWorkNow` | `BIT` | X |
| 11 | `OtherWorkId` | `BIGINT` | O |
| 12 | `CanSave` | `BIT` | X |
| 13 | `BlockCode` | `INT` | X |
| 14 | `BlockMessage` | `NVARCHAR(300)` | X |

신규예약의 `DateChanged`, `SlotChanged`, `ExtraChanged`는 NULL이다. `OtherWorkId`는 예약변경 시 현재 Work를 제외한 다른 유효업무 ID만 반환한다.

대표 `BlockCode` 우선순위:

```text
308/309 현재 공통 업무 불가
→ 306 다른 유효예약
→ 선택한 Slot의 300~305
→ TimeSlot 미선택이고 AM/PM 모두 불가이면 307
→ 400/401 TGT 비대상
→ 요청한 무효 AEX 410~412, OptionCode ASC
```

시간대를 아직 선택하지 않았으나 선택 가능한 Slot이 하나 이상이면:

```text
CanSave=0
BlockCode=0
BlockMessage=''
```

## 9.7 RS2 시간대정보

`ALL`, `SLOT`, `SLOT_EXTRA`에서 AM/PM 정확히 2행을 반환한다. `EXTRA`, `NONE`에서는 동일 Schema의 0행을 반환한다.

| 순서 | 컬럼 | 타입 | NULL |
|---:|---|---|:---:|
| 1 | `TimeSlot` | `CHAR(2)` | X |
| 2 | `SlotName` | `NVARCHAR(10)` | X |
| 3 | `Capacity` | `INT` | X |
| 4 | `CurrentCount` | `INT` | X |
| 5 | `AfterCount` | `INT` | X |
| 6 | `SeatsLeft` | `INT` | X |
| 7 | `IsOpen` | `BIT` | X |
| 8 | `CutoffTime` | `TIME(0)` | O |
| 9 | `CutoffPassed` | `BIT` | X |
| 10 | `CanSelect` | `BIT` | X |
| 11 | `BlockCode` | `INT` | X |
| 12 | `BlockMessage` | `NVARCHAR(300)` | X |

정원:

```text
Capacity=20
CurrentCount=같은 날짜+시간대이며 Status IN ('RSV','RCP')
```

`AfterCount`:

```text
신규예약                       = CurrentCount + 1
기존 Work가 같은 날짜·시간대 유지 = CurrentCount
기존 Work가 다른 시간대로 이동     = CurrentCount + 1
```

```text
SeatsLeft=MAX(0, Capacity-AfterCount)
SlotFull은 AfterCount>Capacity일 때 성립
```

현재 Work가 이미 20/20 Slot에 포함되어 같은 Slot을 유지하는 경우 `AfterCount=20`, `CanSelect=1`이다.

`CanSelect`는 요청일·시간대 운영·마감·정원만 반영한다. 현재 공통 업무 가능 여부, 다른 유효예약, TGT, AEX는 RS1에서 결합한다.

## 9.8 RS3 검진대상

`ALL`에서 일정 평가가 가능한 경우 정확히 1행을 반환한다. 그 외에는 0행이다.

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `Eligible` | `BIT` | X |
| `Age` | `INT` | X |
| `LastCheckupDate` | `DATE` | O |
| `ReasonCode` | `INT` | X |
| `ReasonMessage` | `NVARCHAR(300)` | X |

## 9.9 RS4 국가검사항목

`ALL`에서 TGT 대상이면 8~11행, 비대상이거나 다른 Scope이면 0행이다.

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `ExamCode` | `VARCHAR(10)` | X |
| `ExamName` | `NVARCHAR(100)` | X |
| `ExamType` | `VARCHAR(12)` | X |
| `RuleCode` | `VARCHAR(10)` | X |

## 9.10 RS5 추가검사항목

AEX를 실제 평가하는 `ALL`, `EXTRA`, `SLOT_EXTRA`이면 정확히 7행이다. `SLOT`, `NONE` 또는 일정 자체가 평가 불가능하면 0행이다.

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `OptionCode` | `VARCHAR(10)` | X |
| `ExamCode` | `VARCHAR(10)` | X |
| `ExamName` | `NVARCHAR(100)` | X |
| `Requested` | `BIT` | X |
| `Selected` | `BIT` | X |
| `CanSelect` | `BIT` | X |
| `ReasonCode` | `INT` | X |
| `ReasonMessage` | `NVARCHAR(300)` | X |

TGT 비대상인 `ALL`에서는 7행을 반환하되 모두 `CanSelect=0`, `Selected=0`으로 표시한다.

## 9.11 Scope별 Cardinality

| Scope | RS2 | RS3 | RS4 | RS5 |
|---|---:|---:|---:|---:|
| ALL | 2 | 0 또는 1 | 0 또는 8~11 | 0 또는 7 |
| SLOT | 2 | 0 | 0 | 0 |
| EXTRA | 0 | 0 | 0 | 7 |
| SLOT_EXTRA | 2 | 0 | 0 | 7 |
| NONE | 0 | 0 | 0 | 0 |

## 9.12 `CanSave`

신규예약·예약일 변경:

```text
CanWorkNow
AND OtherWorkId IS NULL
AND TimeSlot 선택
AND 선택 Slot.CanSelect=1
AND Eligible=1
AND NEX 8건 이상
AND Requested=1인 모든 AEX가 CanSelect=1
```

시간대만 변경:

```text
CanWorkNow
AND OtherWorkId IS NULL
AND 선택 Slot.CanSelect=1
```

AEX만 변경:

```text
CanWorkNow
AND Requested=1인 모든 AEX가 CanSelect=1
AND ExtraChanged=1
```

시간대+AEX:

```text
CanWorkNow
AND OtherWorkId IS NULL
AND 선택 Slot.CanSelect=1
AND Requested=1인 모든 AEX가 CanSelect=1
```

변경 없음:

```text
Scope=NONE
CanSave=0
BlockCode=0
```

## 9.13 호출·평가 순서

```text
1. ServerTime 캡처
2. 필수값
3. 허용값
4. Parameter 조합
5. Patient 존재
6. WorkId가 있으면 Work 존재 → Patient 일치 → RSV 상태 → RowVersion
7. Scope 계산
8. Scope=NONE이면 `RS0.Code=0`, `CanSave=0`의 조회결과 반환
9. ALL이면 검사 Master 구성 확인
10. EXTRA/SLOT_EXTRA이면 AEX Master와 저장 NEX 무결성 확인
11. 현재 공통 업무 가능 여부
12. ALL/SLOT/SLOT_EXTRA이면 현재 Work를 제외한 다른 유효예약 → 일정 → AM/PM 정원
13. ALL이고 선택 일정이 진행 가능하면 TGT → NEX → AEX
14. EXTRA/SLOT_EXTRA이면 저장된 NEX 기준 AEX
15. RS0~RS5 반환
```

이 SELECT SP는 정원 자리를 확보하지 않으며 Write SP가 Transaction 안에서 다시 검증한다.

---

# 10. 수검자 Write SP 계약

## 10.1 `[dbo].[USP_HC_INSERT_수검자]`

### 입력

| Parameter | 타입 | NULL |
|---|---|:---:|
| `@AutoChartNo` | `BIT` | X |
| `@ChartNo` | `NVARCHAR(100)` | O |
| `@Name` | `NVARCHAR(100)` | X |
| `@SocialNumber` | `VARCHAR(13)` | X |
| `@MobilePhone` | `VARCHAR(13)` | O |
| `@Phone` | `VARCHAR(13)` | O |
| `@Email` | `VARCHAR(200)` | O |
| `@Zipcode` | `VARCHAR(10)` | O |
| `@Address` | `NVARCHAR(200)` | O |
| `@AddressDetail` | `NVARCHAR(200)` | O |
| `@Memo` | `NVARCHAR(MAX)` | O |
| `@ConfirmSimilarPatient` | `BIT` | X |

조합:

```text
AutoChartNo=1 → ChartNo=NULL
AutoChartNo=0 → ChartNo 필수
```

주민번호 처리:

```text
C# 전달값: '-'가 제거된 숫자 13자리
→ SP에서 길이·숫자 여부 재검증
→ 6자리 생년월일 실제 날짜 검증
→ 7번째 자리의 출생세기·성별 코드 해석
→ Birthday(yyyyMMdd) / Gender(M,F) 산출
```

| 7번째 자리 | 출생세기 | Gender | UI 표시 |
|---|---:|:---:|---|
| `9` | 1800년대 | M | 남 |
| `0` | 1800년대 | F | 여 |
| `1`, `5` | 1900년대 | M | 남 |
| `2`, `6` | 1900년대 | F | 여 |
| `3`, `7` | 2000년대 | M | 남 |
| `4`, `8` | 2000년대 | F | 여 |

- 실제 행정번호 존재 여부와 마지막 검증번호 계산은 수행하지 않는다.
- 위 산출계약은 `INSERT_수검자`와 `UPDATE_수검자정보`에 동일하게 적용한다.

### Result Set

```text
RS0 처리결과
RS1 수검자결과
```

RS1 Schema:

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `PatientId` | `BIGINT` | X |
| `ChartNo` | `NVARCHAR(100)` | X |
| `Name` | `NVARCHAR(100)` | X |
| `SocialNumber` | `VARCHAR(13)` | X |
| `Birthday` | `VARCHAR(8)` | X |
| `Gender` | `CHAR(1)` | X |
| `MobilePhone` | `VARCHAR(13)` | O |
| `LastEditDate` | `DATETIME` | X |

결과:

| 상황 | Success | Code | RS1 |
|---|:---:|---:|---:|
| 신규등록 | 1 | 0 | 신규 1행 |
| 동일 주민번호+동일 이름 | 1 | 2 | 기존 1행 |
| 동일 주민번호+다른 이름 | 0 | 202 | 기존 1행 |
| 이름+생년월일 후보, 미확인 | 0 | 203 | 후보 1행 이상 |
| 후보 확인 후 별도등록 | 1 | 0 | 신규 1행 |
| 그 외 실패 | 0 | 해당 코드 | 후속 Result Set 없음 |

### 검증순서

```text
필수값·문자열 정규화
→ SocialNumber 13자리·숫자·날짜·세기/성별 검증
→ Birthday/Gender 산출
→ AutoChartNo 조합
→ 현재 공통 업무 가능
→ 동일 SocialNumber 조회
→ 이름+산출 Birthday 후보
→ 수동 ChartNo 고유성
→ 자동 ChartNo 발급
→ INFO_PATIENTS 1행 Transaction 저장
```

- `ConfirmSimilarPatient=1`은 현재 요청의 Name+산출 Birthday+SocialNumber 조합에만 유효하다.
- C#은 세 값 중 하나가 바뀌면 확인값을 0으로 초기화한다.
- DB는 주민번호와 차트번호 고유성을 항상 다시 확인한다.
- UI가 계산한 Birthday/Gender를 입력 Parameter로 받지 않으며 DB가 산출한 값을 저장한다.

## 10.2 `[dbo].[USP_HC_UPDATE_수검자정보]`

### 입력

| Parameter | 타입 | NULL |
|---|---|:---:|
| `@PatientId` | `BIGINT` | X |
| `@LastEditDate` | `DATETIME` | X |
| `@ChartNo` | `NVARCHAR(100)` | X |
| `@Name` | `NVARCHAR(100)` | X |
| `@SocialNumber` | `VARCHAR(13)` | X |
| `@MobilePhone` | `VARCHAR(13)` | O |
| `@Phone` | `VARCHAR(13)` | O |
| `@Email` | `VARCHAR(200)` | O |
| `@Zipcode` | `VARCHAR(10)` | O |
| `@Address` | `NVARCHAR(200)` | O |
| `@AddressDetail` | `NVARCHAR(200)` | O |
| `@Memo` | `NVARCHAR(MAX)` | O |

### Result Set

```text
RS0 처리결과
RS1 수검자변경결과
```

RS1:

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `PatientId` | `BIGINT` | X |
| `ChartNo` | `NVARCHAR(100)` | X |
| `LastEditDate` | `DATETIME` | X |

### 변경판정

주민번호 변경 여부는 정규화된 값을 직접 비교한다.

```text
기존 SocialNumber = 요청 SocialNumber → 주민번호 변경 아님
기존 SocialNumber <> 요청 SocialNumber → 주민번호 변경
```

### 검증순서

```text
필수값·문자열 정규화
→ SocialNumber 13자리·숫자·날짜·세기/성별 검증
→ Birthday/Gender 산출
→ Patient 존재
→ LastEditDate
→ 실제 변경 여부
→ 변경 없음이면 Code=1 반환
→ 현재 공통 업무 가능
→ ChartNo 변경 시 다른 Patient 고유성
→ 주민번호 변경 시 다른 Patient SocialNumber 고유성
→ 주민번호 변경 시 대상 Patient의 모든 RSV/RCP 존재 확인
→ INFO_PATIENTS Transaction 수정
```

- No-op에서는 DB 행을 갱신하지 않고 기존 `LastEditDate`를 반환한다.
- 주민번호 변경으로 기존 Work의 TGT/NEX/AEX를 자동 재판정하거나 취소하지 않는다.
- Birthday/Gender는 요청값을 받지 않고 변경된 SocialNumber에서 다시 산출한다.

# 11. 예약 Write SP 계약

예약·접수 Write SP의 성공 RS1은 공통으로 다음 Schema를 사용한다.

| 컬럼 | 타입 | NULL |
|---|---|:---:|
| `WorkId` | `BIGINT` | X |
| `Status` | `CHAR(3)` | X |
| `RowVersion` | `BINARY(8)` | X |

## 11.1 `[dbo].[USP_HC_INSERT_예약]`

### 입력

| Parameter | 타입 | NULL |
|---|---|:---:|
| `@PatientId` | `BIGINT` | X |
| `@ReservationType` | `VARCHAR(10)` | X |
| `@ReservationDate` | `DATE` | X |
| `@TimeSlot` | `CHAR(2)` | X |
| `@AexOpt01Selected` | `BIT` | X |
| `@AexOpt02Selected` | `BIT` | X |
| `@AexOpt03Selected` | `BIT` | X |
| `@AexOpt04Selected` | `BIT` | X |
| `@AexOpt05Selected` | `BIT` | X |
| `@AexOpt06Selected` | `BIT` | X |
| `@AexOpt07Selected` | `BIT` | X |

`ReservationType`:

```text
NORMAL
WALKIN
```

WalkIn은 `ReservationDate=DB Today`여야 한다.

### Result Set

```text
RS0 처리결과
RS1 Work결과
```

### 검증순서

```text
필수값·허용값·조합
→ Patient 존재
→ 검사 Master 구성
→ 현재 공통 업무 가능
→ 다른 유효예약 수 확인: 2건 이상이면 701, 1건이면 306
→ 일정·마감
→ 시간대 정원
→ TGT
→ NEX
→ AEX
→ Work(RSV)+Exam Detail 같은 Transaction 저장
```

저장:

```text
INFO_CHECKUP_WORKS.StatusCode='RSV'
INFO_CHECKUP_WORK_EXAMS=NEX 전체 + Selected=1인 AEX
```

Write SP는 조회 SP 결과를 신뢰하지 않고 모든 조건을 다시 검증한다.

---

## 11.2 `[dbo].[USP_HC_UPDATE_예약변경]`

### 입력

| Parameter | 타입 | NULL |
|---|---|:---:|
| `@WorkId` | `BIGINT` | X |
| `@RowVersion` | `BINARY(8)` | X |
| `@ReservationDate` | `DATE` | X |
| `@TimeSlot` | `CHAR(2)` | X |
| `@AexOpt01Selected` | `BIT` | X |
| `@AexOpt02Selected` | `BIT` | X |
| `@AexOpt03Selected` | `BIT` | X |
| `@AexOpt04Selected` | `BIT` | X |
| `@AexOpt05Selected` | `BIT` | X |
| `@AexOpt06Selected` | `BIT` | X |
| `@AexOpt07Selected` | `BIT` | X |

### Result Set

```text
RS0 처리결과
RS1 Work결과
```

### 검증순서

```text
필수값·허용값
→ Work 존재
→ Status=RSV
→ RowVersion
→ 현재 예약일·시간대와 AEX 코드 집합으로 DateChanged / SlotChanged / ExtraChanged 계산
→ 변경 없음이면 Code=1, UPDATE 없음
→ 예약일 변경이면 검사 Master 구성 확인
→ AEX 변경이면 저장 NEX 무결성과 AEX Master 구성 확인
→ 실제 변경이면 현재 공통 업무 가능
→ 예약일 변경이면 일정·현재 Work 제외 중복(2건 이상 701, 1건 306)·정원·TGT·NEX·AEX 검증
→ 시간대만 변경이면 일정·현재 Work 제외 중복(2건 이상 701, 1건 306)·정원만 검증
→ AEX 변경이면 성별·저장 NEX 중복 검증
→ Work+영향 Detail 같은 Transaction 저장
```

### 변경 Matrix

| 실제 변경 | 일정·마감·정원·중복 | TGT | NEX | AEX | 저장 |
|---|:---:|:---:|:---:|:---:|---|
| 예약일 포함 | O | O | O | O | Work + NEX/AEX Detail |
| 시간대만 | O | X | X | X | Work만 |
| AEX만 | X | X | X | O | AEX Detail + Work 갱신 |
| 시간대+AEX | O | X | X | O | Work + AEX Detail |
| 없음 | X | X | X | X | No-op |

### 시간대만 변경

```text
현재 AEX 코드는 변경 여부 집합 비교에만 읽는다. TGT/NEX/AEX Rule 재평가와 NEX/AEX Detail 재작성은 하지 않는다.
```

### AEX 실제 변경

```text
AEX Detail 변경
+ INFO_CHECKUP_WORKS.LastEditDate 갱신
→ 새 RowVersion 반환
```

### AEX 동일집합

```text
Detail DELETE/INSERT 없음
Work UPDATE 없음
기존 RowVersion 유지
Code=1
```

예약일 변경 후 TGT 비대상 또는 AEX 충돌이면 기존 예약을 변경하지 않는다.

---

## 11.3 `[dbo].[USP_HC_UPDATE_예약취소]`

### 입력

| Parameter | 타입 | NULL |
|---|---|:---:|
| `@WorkId` | `BIGINT` | X |
| `@RowVersion` | `BINARY(8)` | X |

### Result Set

```text
RS0 처리결과
RS1 Work결과
```

### 검증순서

```text
필수값
→ Work 존재
→ Status=RSV
→ RowVersion
→ 현재 공통 업무 가능
→ RSV→CNL
```

- Detail은 삭제하지 않는다.
- 예약 마감시각은 취소 가능조건이 아니다.
- CNL에서 RSV로 복원하지 않는다.

---

# 12. 접수 Write SP 계약

## 12.1 `[dbo].[USP_HC_UPDATE_접수완료]`

### 입력

| Parameter | 타입 | NULL |
|---|---|:---:|
| `@WorkId` | `BIGINT` | X |
| `@RowVersion` | `BINARY(8)` | X |

### Result Set

```text
RS0 처리결과
RS1 Work결과
```

### 검증순서

```text
필수값
→ Work 존재
→ Status=RSV
→ RowVersion
→ Work 검사구성 무결성
→ 현재 공통 업무 가능
→ ReservationDate=DB Today
→ 해당 TimeSlot의 접수마감 전
→ RSV→RCP
```

접수 성공 시 다음은 변경하지 않는다.

```text
ReservationDate
TimeSlotCode
NEX
AEX
```

직접접수용 신규 Work를 만들지 않는다.

---

## 12.2 `[dbo].[USP_HC_UPDATE_접수추가검사]`

### 입력

| Parameter | 타입 | NULL |
|---|---|:---:|
| `@WorkId` | `BIGINT` | X |
| `@RowVersion` | `BINARY(8)` | X |
| `@AexOpt01Selected` | `BIT` | X |
| `@AexOpt02Selected` | `BIT` | X |
| `@AexOpt03Selected` | `BIT` | X |
| `@AexOpt04Selected` | `BIT` | X |
| `@AexOpt05Selected` | `BIT` | X |
| `@AexOpt06Selected` | `BIT` | X |
| `@AexOpt07Selected` | `BIT` | X |

### Result Set

```text
RS0 처리결과
RS1 Work결과
```

### 검증순서

```text
필수값
→ Work 존재
→ Status=RCP
→ RowVersion
→ 저장 NEX·AEX 집합 확인
→ 현재 AEX와 요청 AEX 비교
→ 동일집합이면 Code=1, UPDATE 없음
→ 실제 변경이면 저장 NEX 무결성과 AEX Master 구성 확인
→ 현재 공통 업무 가능
→ 저장 NEX 기준 AEX 성별·중복 Rule
→ AEX Detail 변경 + Work LastEditDate 갱신
```

No-op에서는 현재 Master 비활성·성별·중복 Rule을 재평가하지 않으며 기존 RowVersion을 유지한다.

실제 변경에서는 예약일·시간대·TGT·NEX를 변경하거나 재평가하지 않는다.

---

## 12.3 `[dbo].[USP_HC_UPDATE_접수취소]`

### 입력

| Parameter | 타입 | NULL |
|---|---|:---:|
| `@WorkId` | `BIGINT` | X |
| `@RowVersion` | `BINARY(8)` | X |

### Result Set

```text
RS0 처리결과
RS1 Work결과
```

### 검증순서

```text
필수값
→ Work 존재
→ Status=RCP
→ RowVersion
→ 현재 공통 업무 가능
→ RCP→CNL
```

- RSV로 복원하지 않는다.
- Detail은 보존한다.
- 접수 마감시각은 접수취소 가능조건이 아니다.

---

# 13. SP별 RS0 허용 ResultCode

| SP | 허용 Code |
|---|---|
| SELECT_공통업무상태 | 0 |
| SELECT_수검자목록 | 0, 101, 103 |
| SELECT_수검자상세 | 0, 100, 200 |
| INSERT_수검자 | 0, 2, 100~102, 201~203, 206, 308~309 |
| UPDATE_수검자정보 | 0, 1, 100~102, 200~201, 204~205, 600, 308~309 |
| SELECT_수검자유효업무 | 0, 100, 200, 701 |
| SELECT_예약가능정보 | 0, 100~102, 200, 500~502, 601, 700~701 |
| INSERT_예약 | 0, 100~102, 200, 300~306, 308~309, 400~401, 410~412, 700~701 |
| UPDATE_예약변경 | 0, 1, 100~102, 300~306, 308~309, 400~401, 410~412, 500, 502, 601, 700~701 |
| UPDATE_예약취소 | 0, 100, 500, 502, 601, 308~309 |
| SELECT_예약접수목록 | 0, 101, 103~104 |
| SELECT_예약접수상세 | 0, 100, 500, 701 |
| UPDATE_접수완료 | 0, 100, 304, 308~309, 500, 502~503, 601, 701 |
| UPDATE_접수추가검사 | 0, 1, 100, 308~309, 410~412, 500, 502, 601, 700~701 |
| UPDATE_접수취소 | 0, 100, 308~309, 500, 502, 601 |

위 표는 RS0 처리결과에 반환할 수 있는 코드만 나타낸다.

후속 Result Set의 안내·차단 코드는 다음과 같다.

| SP | 후속 Result Set Code |
|---|---|
| SELECT_공통업무상태 | RS1 `0`, `308`, `309` |
| SELECT_예약가능정보 | RS1/RS2 `0`, `300~309`; RS3 `0`, `400`, `401`; RS5 `0`, `400`, `401`, `410~412` |
| SELECT_예약접수상세 | RS4 `0`, `304`, `308`, `309`, `502`, `503` |

`SELECT_예약가능정보`의 업무 차단은 RS0 실패가 아니라 RS1/RS2/RS3/RS5의 `BlockCode` 또는 `ReasonCode`로 반환한다.

# 14. Transaction·잠금 Phase 4 인계

본 문서는 논리적 원자성 및 직렬화 대상을 고정한다. 실제 SQL 잠금기법은 `06_DB_Transaction_Security_Seed.md`에서 구현한다.

| Write SP | 같은 Transaction 대상 | 논리 잠금영역 |
|---|---|---|
| INSERT_수검자 | INFO_PATIENTS | ChartNo, SocialNumber |
| UPDATE_수검자정보 | INFO_PATIENTS | PatientId, 변경 ChartNo/SocialNumber |
| INSERT_예약 | Work + NEX/AEX Detail | PatientId, 대상 Date+Slot |
| UPDATE_예약변경 | Work + 영향 Detail | WorkId, PatientId, 기존/신규 Slot |
| UPDATE_예약취소 | Work | WorkId |
| UPDATE_접수완료 | Work | WorkId |
| UPDATE_접수추가검사 | AEX Detail + Work | WorkId |
| UPDATE_접수취소 | Work | WorkId |

고정 동시 실행 결과:

```text
동일 SocialNumber 수검자 동시등록 → 정확히 1건만 신규 INSERT
19/20 Slot에 동시 신규예약 2건 → 정확히 1건 성공
동일 Patient의 서로 다른 Slot 동시예약 → 정확히 1건 성공
주민번호 변경과 같은 Patient 신규예약 → 모순된 동시 성공 금지
예약변경은 현재 WorkId를 제외하고 다른 유효업무만 판정
같은 RSV의 접수완료와 예약취소 → 정확히 하나만 성공
같은 Work의 AEX 동시변경 → 오래된 RowVersion 요청 실패
```

예약 이동 시 기존·신규 Slot 잠금은 결정적 순서로 취득해야 한다. `OtherWorkId` 조회 결과가 2건 이상이면 잠금 성공 여부와 관계없이 `701 WorkDataError`로 처리한다.

# 15. Policy·Process·Function·UI·DB 객체 추적

| Function ID | Process | 주요 UI | 구현 객체 |
|---|---|---|---|
| F-PAT-001 | P01-01 | WF-PAT-01, DLG-PAT-02 | SELECT_수검자목록, SELECT_수검자상세 |
| F-PAT-002 | P01-02~03 | DLG-PAT-01, DLG-PAT-03 | INSERT_수검자 |
| F-PAT-003 | P01-04~05 | DLG-PAT-01 | UPDATE_수검자정보 |
| F-RSV-001 | P02-01~04 | WF-RSV-01 | SELECT_수검자유효업무, SELECT_예약가능정보, INSERT_예약 |
| F-RSV-002 | P02-05~06 | WF-WRK-01, DLG-RSV-01 | SELECT_예약접수목록, SELECT_예약접수상세, SELECT_예약가능정보, UPDATE_예약변경 |
| F-RSV-003 | P02-07 | CNF-RSV-01 | UPDATE_예약취소 |
| F-RCP-001 | P03-01~03 | DLG-RCP-01 | SELECT_수검자유효업무, SELECT_예약접수상세, UPDATE_접수완료 |
| F-RCP-002 | P03-05 | DLG-RCP-02 | SELECT_예약접수상세, UPDATE_접수추가검사 |
| F-RCP-003 | P03-06 | CNF-RCP-01 | UPDATE_접수취소 |
| F-COM-001 | P02-05, P03-04 | WF-WRK-01 | SELECT_예약접수목록, SELECT_예약접수상세 |
| F-COM-002 | P01-02~05 | 수검자 Modal | INSERT_수검자, UPDATE_수검자정보 |
| F-COM-003 | P02-02, P02-06 | 예약 화면 | UFN_HC_검진대상확인, SELECT_예약가능정보, 예약 Write SP |
| F-COM-004 | P02-02, P02-06, P03-05 | NEX/AEX 영역 | UFN_HC_국가검사구성, UFN_HC_추가검사확인, 예약·접수 AEX SP |
| F-COM-005 | P02-03~04 | Calendar/시간대 | UFN_HC_일정확인, SELECT_예약가능정보, 예약 Write SP |
| F-COM-006 | P03-02~03 | 접수 Modal | UFN_HC_일정확인, SELECT_예약접수상세, UPDATE_접수완료 |
| F-COM-007 | P01~P03 공통 | MainForm/Ribbon | SELECT_공통업무상태, UFN_HC_일정확인, 모든 Write SP |

16개 Function ID 모두 하나 이상의 DB 객체와 연결된다.

---

# 16. C# 호출 계약

## 16.1 ResultCode Enum

```csharp
public enum DbCode
{
    Ok = 0,
    NoChange = 1,
    ExistingPatient = 2,

    MissingValue = 100,
    BadValue = 101,
    BadRequest = 102,
    NeedSearchCondition = 103,
    BadDateRange = 104,

    PatientNotFound = 200,
    ChartNoUsed = 201,
    SameNumberDifferentName = 202,
    SimilarPatient = 203,
    SocialNumberUsed = 204,
    SocialChangeBlocked = 205,
    ChartNoLimit = 206,

    PastDate = 300,
    Sunday = 301,
    Holiday = 302,
    SlotClosed = 303,
    CutoffPassed = 304,
    SlotFull = 305,
    OtherReservation = 306,
    NoOpenSlot = 307,
    CenterClosed = 308,
    OutsideHours = 309,

    UnderAge = 400,
    NotDue = 401,
    ExamOff = 410,
    WrongGender = 411,
    ExamDuplicate = 412,

    WorkNotFound = 500,
    WrongPatient = 501,
    WrongStatus = 502,
    NotToday = 503,

    PatientChanged = 600,
    WorkChanged = 601,

    ExamSetupError = 700,
    WorkDataError = 701
}
```

## 16.2 RS0 DTO

```csharp
public sealed class DbResult
{
    public bool Success { get; set; }
    public int Code { get; set; }
    public string Message { get; set; }
    public string Field { get; set; }
    public DateTime ServerTime { get; set; }
}
```

## 16.3 동시성값

```text
INFO_PATIENTS.LastEditDate → C# DateTime
INFO_CHECKUP_WORKS.RowVersion → C# byte[8]
```

- DB에서 읽은 값을 문자열로 변환해 재전송하지 않는다.
- 성공 Write 응답의 새 값을 화면·DTO 원본값으로 교체한다.
- `Code=600/601`이면 최신 상세를 다시 조회하고 사용자 입력을 자동 덮어쓰지 않는다.

## 16.4 예약가능정보 Result Set 처리

```text
RS0 실패 → 후속 Result Set을 사용하지 않음
RS0 성공 → RS1~RS5를 고정 순서로 읽음
RS1.CanSave와 BlockCode로 저장버튼·안내문 결정
```

수검자 등록의 `Code=202/203`은 예외적으로 RS1 후보 데이터를 읽는다.

---

# 17. 적대적 테스트 계약

## 17.1 시간 경계

| 시각 | 기대 |
|---|---|
| 08:59:59.9999999 | 업무 불가 309 |
| 09:00:00.0000000 | 업무 가능 |
| 17:59:59.9999999 | 업무 가능 |
| 18:00:00.0000000 | 업무 불가 309 |
| 09:59:59.9999999 | AM 일반예약 마감 전 |
| 10:00:00.0000000 | AM 일반예약 304 |
| 10:59:59.9999999 | AM WalkIn/접수 마감 전 |
| 11:00:00.0000000 | AM WalkIn/접수 304 |
| 14:59:59.9999999 | PM 일반예약 마감 전 |
| 15:00:00.0000000 | PM 일반예약 304 |
| 15:59:59.9999999 | PM WalkIn/접수 마감 전 |
| 16:00:00.0000000 | PM WalkIn/접수 304 |

## 17.2 일정

```text
과거 평일
과거 일요일
미래 일요일
활성 평일 휴무일
활성 토요일 휴무일
토요일 AM
토요일 PM
미래 정상 평일
```

과거이면서 일요일이면 `300`이 우선이다.

## 17.3 TGT

```text
예약일 기준 만 19세 / 20세 경계
과거 완료이력 없음
최근 완료연도 차이 1년 / 2년
예약일 당일 완료이력 제외
예약일 이후 완료이력 제외
```

## 17.4 NEX

```text
남성 만 23/24/28세 이상지질혈증
여성 만 39/40/44세 이상지질혈증
만 40세 B형간염 제외행 있음/없음
만 55/56세 C형간염
여성 만 54/60/66세 골밀도
만 56/66세 폐기능
조건부 3종 동시 충족 시 NEX 11행
모든 TGT 대상 결과가 8~11행 범위인지 검증
```

## 17.5 AEX

```text
전부 미선택
남성 유방초음파 선택
여성 PSA 선택
남성 HPV 선택
비활성 AEX 선택
NEX EX012 + OPT04 선택
선택하지 않은 무효 AEX
예약일 변경 후 기존 AEX가 새 NEX와 충돌
```

## 17.6 수검자

```text
C#에서 SocialNumber 하이픈 제거 후 13자리로 SP 전달
12자리 / 14자리 / 숫자 외 문자를 포함한 입력
존재하지 않는 생년월일
허용되지 않은 7번째 자리
Birthday/Gender DB 산출값 확인
동일 주민번호+동일 이름 → Code=2
동일 주민번호+다른 이름 → Code=202 + 기존 1행
이름+생년월일 동일 후보 → Code=203 + 후보 N행
후보 확인 후 별도등록
수동 ChartNo 중복
자동 ChartNo 발급
수정 No-op
LastEditDate 충돌
주민번호 변경+RSV 존재
주민번호 변경+RCP 존재
차트번호 변경+활성 Work 존재 → 차트번호 중복만 검증
동일 SocialNumber 동시등록 → 1건만 신규등록
실제 주민등록번호를 Seed/Test Data에 사용하지 않았는지 검수
```

## 17.7 예약

```text
19/20 신규예약
20/20 신규예약
동일 Patient 미래 유효예약
예약변경 현재 Work만 존재 → 306 없이 정상 평가
예약변경 현재 Work 외 다른 유효업무 1건 → 306
예약변경 현재 Work 외 다른 유효업무 2건 이상 → 701
WalkIn 날짜가 오늘 아님
예약일 변경 후 TGT 비대상
예약일 변경 후 OPT04 중복
시간대만 변경
AEX만 변경
시간대+AEX 변경
변경 없음
RowVersion 충돌
현재 Work가 20/20 Slot을 그대로 유지
```

## 17.8 접수

```text
오늘 RSV 접수
과거 RSV 접수
미래 RSV 접수
RCP 재접수
CNL 접수
접수마감 경계
RCP AEX 동일집합 No-op
RCP AEX 실제 변경
RSV 접수와 예약취소 동시 실행
RCP 접수취소 후 RSV 복원 금지
```

## 17.9 Result Set

```text
모든 SP의 RS0 정확히 1행
예약불가는 RS0 성공 + BlockCode
검색 0건은 성공
수검자 Code=202/203은 후보 Result Set 존재
예약가능정보 성공 Result Set 순서 고정
Scope별 Cardinality 준수
NEX RS4가 0 또는 8~11행인지 검증
RowVersion BINARY(8)
LastEditDate DATETIME 원본 유지
```

# 18. Phase 3 적대적 최종검수

| 검수항목 | 결과 |
|---|:---:|
| 외부 Stored Procedure 수 | PASS — 15개 |
| 내부 Inline TVF 수 | PASS — 4개 |
| DELETE SP | PASS — 0개 |
| Trigger | PASS — 0개 |
| TVP/CSV/XML/JSON 입력 | PASS — 0개 |
| @Mode 범용 SP | PASS — 0개 |
| 객체명 중복 | PASS — 없음 |
| 미연결 Function ID | PASS — 0개 |
| Parameter 미확정 | PASS — 0개 |
| NULL 조합 미확정 | PASS — 0개 |
| Result Set 순서 미확정 | PASS — 0개 |
| Result 컬럼 미확정 | PASS — 0개 |
| ResultCode 없는 예상 실패 | PASS — 0개 |
| 사용되지 않는 ResultCode | PASS — 0개 |
| 예약불가와 SP 실패 혼합 | PASS — 분리 |
| 중복후보 후속 데이터 누락 | PASS — INSERT_수검자 예외계약 |
| SocialNumber 저장·검색 경계 | PASS — VARCHAR(13) 직접 정확검색·UQ |
| Birthday/Gender 최종 산출 책임 | PASS — Patient Write SP |
| 주민번호 전용 보조구조 잔존 | PASS — 없음 |
| 예약변경 현재 WorkId 제외 | PASS — SELECT/UPDATE 모두 명시 |
| 다른 유효업무 복수행 이상상태 | PASS — 701 |
| NEX 실제 Cardinality | PASS — 8~11행 |
| 시간대만 변경 시 TGT/NEX/AEX 재검증 | PASS — 금지 |
| AEX 실제 변경 RowVersion 갱신 | PASS — 명시 |
| AEX 동일집합 No-op | PASS — 명시 |
| 예약일 변경 영향범위 | PASS — ALL |
| 시간대+AEX 영향범위 | PASS — SLOT_EXTRA |
| 현재 Work 정원 중복계산 | PASS — AfterCount 규칙 확정 |
| 조회값을 저장권한으로 오인 | PASS — Write SP 재검증 |
| Patient/Work 동시성 구분 | PASS |
| 00~04 스키마 변경 | PASS — 0건 |
| Phase 4 인계 누락 | PASS — 없음 |
| 미확정 항목 | PASS — 0건 |
| Phase 4 진입 차단 결함 | PASS — 0건 |

# 19. Phase 4 인계 및 최종 선언

## 19.1 다음 문서

```text
06_DB_Transaction_Security_Seed.md
```

Phase 4는 다음을 구현하되 본 문서의 외부 계약을 변경하지 않는다.

```text
Transaction·잠금 상세 SQL
동시성 UPDATE 조건
SocialNumber 숫자 13자리·파생값·고유성 검증 SQL
실제 주민등록번호 금지와 임의 테스트값 Seed 검수
19개 검사 Master Seed
평일·토요일 휴무일 Seed
완료이력·제외정보 Test Data
DB 배포·검증 Script
DB Role·GRANT EXECUTE·직접 DML 통제
동시성·Deadlock·Rollback 실행 테스트
```

## 19.2 READ-ONLY 고정항목

본 문서 확정 후 다음은 변경하지 않는다.

```text
프로그램·DB 명칭
HC 접두사 의미
15개 Stored Procedure 이름과 수
4개 Inline TVF 이름과 수
각 SP Parameter 이름·타입·NULL
RS0 공통 컬럼
SP별 Result Set 순서·컬럼·Cardinality
3자리 ResultCode와 영역
오류 우선순위
SocialNumber 직접 저장·검색·파생값 계약
예약변경 Scope와 현재 WorkId 제외조건
NEX 8~11행 Cardinality
No-op 정의
수검자 후보 Result Set 예외
Patient LastEditDate / Work RowVersion 계약
Phase 4 논리 Transaction·잠금 인계
```

> **최종 판정: FINAL / GO / READ-ONLY — Phase 3 Rule·Stored Procedure 계약이 완결되었다. `HC-RSV-RCP-20260903-R2` 기준선의 00~05를 수정하지 않고 `06_DB_Transaction_Security_Seed.md`로 진행한다.**
