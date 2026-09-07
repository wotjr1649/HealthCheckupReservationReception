# Stage 9 ― 컬럼명 한글화 · `CelNumberS` 계산열 전환

**Index:** `2026-09-04-phase4-database-implementation.md`
**Spec:** `../06_DB_Transaction_Security_Seed.md`
**Tasks:** `T40` ~ `T48`
**전제:** `plans/01`~`03` 완료 (`phase4-r3-t22-complete`). `plans/04`~`08` 미착수.

---

`[!]` **이 계획은 `plans/10` 과 `plans/11` 이 일부를 뒤집었다.** §2.1 의 "Result Set 컬럼명은
영문을 유지한다" 는 R4 한글화(`plans/11`)가 뒤집었다. 아래 §0 의 "안 바꾼다" 목록에서
SP 이름·Parameter 이름·Result Set 컬럼명 세 줄은 **더 이상 참이 아니다.** `T47`·`T48` 은 폐기되어 `plans/10` 으로 이관되었고,
`수검자.CelNumberS` 는 계산열로 전환된 뒤 R3 재봉인에서 **삭제**되었다(`04` §8.1.2). 테이블도 7개에서 6개가 되었다.
아래 수치는 이 계획을 쓰던 시점의 값이다.

## 0. 무엇을 바꾸고 무엇을 안 바꾸는가

```text
바꾼다    물리 테이블 7개의 컬럼명 47개 (영문 PascalCase → 한글)
          수검자.CelNumberS 를 PERSISTED 계산열로 전환
          04_DB_Design.md §3.3 명명규칙 · §8.1~8.7 컬럼표

안 바꾼다 테이블명 7개          이미 한글이다
          SP 15개 · TVF 4개 이름
          SP Parameter 이름      @PatientId 등 ― 05 §1.6 계약
          Result Set 컬럼명      ChartNo · ResultCode 등 ― 05 계약
          ResultCode 값 체계
          정원 20 · 상태코드 4종 · 시간대 2종 등 업무 규칙
```

`[X]` **이 계획은 기준선 개정이다.** `04_DB_Design.md` §3.3 이 "컬럼명은 영문 PascalCase를 유지한다"
라고 고정해 두었고 `database/CLAUDE.md` §4 가 Column 을 계약으로 얼려 두었다.
`T47` 이 그 두 곳을 함께 푼다. `T47` 전에는 `verify-baseline` 이 red 가 되지 않도록
**기준선 파일을 건드리지 않는다** ― `T40`~`T46` 은 `database/**` 만 바꾼다.

`[!]` **그래서 `T40`~`T46` 동안 배포물과 기준선 문서가 어긋난 상태로 있다.**
이 어긋남은 `T47` 에서만 해소된다. 중간에 멈추면 문서가 거짓이 되므로
**`T47` 까지 가지 못할 것 같으면 `T40` 을 시작하지 않는다.**

---

## 1. 실측 근거 4건

### 1.1 "테이블 먼저, SP 나중" 은 불가능하다

```sql
CREATE PROCEDURE ... SELECT [완료일자] FROM [dbo].[완료이력]
  → Msg 207 : 열 이름 '완료일자'이(가) 유효하지 않습니다.
CREATE FUNCTION  ... SELECT [완료일자] FROM [dbo].[완료이력]
  → Msg 207 : 열 이름 '완료일자'이(가) 유효하지 않습니다.
```

지연 이름 해석(deferred name resolution)은 **없는 테이블**만 봐준다.
**있는 테이블의 없는 컬럼**은 `CREATE` 시점에 터진다. `Deploy.sql` 이
`01_Schema → 03_Functions → 04_Procedures` 를 한 번에 돌리므로 스키마만 바꾸면 그 배포가 죽는다.

→ **커밋 단위 = `01_Schema` + 그 테이블을 읽는 TVF/SP 전부 + 그 테이블을 읽는 테스트 전부.**
테이블 단위로는 쪼갤 수 있다. `T40`~`T46` 이 그 단위다.

### 1.2 WinForms 는 아직 컬럼명을 쓰지 않는다

```text
winforms/  파일 51개 (Program.cs · MainForm.cs · MainForm.Designer.cs · MainFormTests.cs)
DB 식별자(USP_HC_ · UFN_HC_ · SqlCommand · 컬럼명) 참조 0건
```

`04` §3.3 이 말한 "Result Set 컬럼명과 C# 의 1:1" 은 **설계 의도이고 그 C# 은 아직 없다.**
Phase 5 착수 전인 지금이 최저비용 시점이다.

### 1.3 파급 3,638곳

| 대상 | 컬럼명 등장 |
|---|---:|
| `database/deploy` | 515 |
| `database/tests` | 437 |
| `database/tools` | 58 |
| `04_DB_Design.md` §8 테이블 상세 | 163 |
| `04_DB_Design.md` 나머지 | 249 |
| `05_DB_Rule_SP_Contract.md` | 254 |
| `06_..._CANDIDATE.md` | 330 |
| `docs/phase4/plans` | 1,632 |

| 테이블 | 컬럼 | `deploy`+`tests` 참조 | Task |
|---|---:|---:|---|
| `변경이력` | 7 | 21 | `T40` |
| `완료이력` | 2 | 11 | `T41` |
| `휴무일` | 4 | 19 | `T42` |
| `검사항목` | 3 | 19 | `T43` |
| `검사코드` | 6 | 140 | `T44` |
| `예약접수` | 8 | 274 | `T45` |
| `수검자` | 17 | 452 | `T46` |

### 1.4 `CelNumberS` 계산열 전환은 가능하다

```text
[1] PERSISTED 계산열 생성                                    성공
[2] 계산열을 필터식에 쓴 필터형 인덱스                        실패 Msg 10609
[3] 계산열을 키로 · 기반열을 필터로                          성공
[4] CONVERT(VARCHAR(13), REPLACE(...)) → varchar / 13 / nullable
[5] 계산열에 직접 INSERT                                     Msg 271 거부
```

`REPLACE` 는 `VARCHAR(8000)` 을 돌려주므로 `CONVERT` 로 폭을 고정해야
`tests/01` 의 컬럼 지문(`varchar` / `13` / `nullable`)이 그대로 유지된다.
`[5]` 때문에 Write SP 가 값을 넘길 수 없고, 따라서 **표시값과 검색값이 어긋나는 상태가 존재할 수 없다.**

---

## 2. 결정사항

### 2.1 변형 1 ― Result Set 컬럼명은 영문을 유지한다

`[!]` **이 결정은 `plans/11`(R4, 2026-09-08)이 뒤집었다.** 아래 표의 "변형 2 (기각)" 이 채택돼
Result Set 컬럼·Parameter·SP 이름이 전부 한글이 되었고 `05` 계약서도 재봉인됐다.
아래는 **R3 시점의 판단 기록**이며 지금의 규칙이 아니다 — 현재 규칙은 `05` §1.6 과 `plans/11` §4 다.

| | 변형 1 (채택) | 변형 2 (기각) |
|---|---|---|
| DB 컬럼 | 한글 | 한글 |
| Result Set 컬럼 | **영문 유지** | 한글 |
| `05` 계약서 | 안 건드림 | 재봉인 |
| `tools/expected-contracts.json` | 안 건드림 | 25개 재생성 |
| SP 본문 | 별칭이 붙는다 | 별칭 없음 |

**채택 이유:** SP·Result Set·ResultCode 계약이 통째로 언 채 물리 스키마만 바뀐다.
되돌릴 때 스키마와 SQL 본문만 되돌리면 되고, `verify-contract` 25건이 전 구간에서 green 을 유지한다.

**대가:** `04` §3.3 이 피하려던 별칭이 SELECT 목록에 붙는다. 형식을 하나로 고정한다.

```sql
SELECT ChartNo = p.[차트번호]        ← 이 형식
     , Name    = p.[성명]
```

`SELECT p.[차트번호] AS ChartNo` 도 같은 뜻이지만 섞어 쓰지 않는다.

### 2.2 이름이 세 갈래인데 글자가 같다 ― 최대 함정

| 네임스페이스 | 예 | 이 계획에서 |
|---|---|:---:|
| **DB 컬럼명** | `[dbo].[수검자].[차트번호]` | **바뀐다** |
| SP Parameter | `@PatientId` · `@MobilePhone` (`05` §1.6) | 안 바뀐다 |
| Result Set 컬럼 | `RS0.Code` · `RS1.ChartNo` (`05` §3.1 등) | 안 바뀐다 |

`[!]` **RS0 의 컬럼은 `Code` 이지 `ResultCode` 가 아니다**(`05` §3.1 실측). `ResultCode` 는 `05` §4 Catalog 의
*개념* 이름이고 Result Set 컬럼명이 아니므로 `변경이력.ResultCode` 와 SQL 안에서 충돌하지 않는다.
충돌이 실재하는 것은 `PatientId`·`ChartNo`·`WorkId` 처럼 **컬럼이면서 Parameter 이거나 Result Set 컬럼**인 이름이다.

`[X]` **단순 정규식 치환을 쓰지 않는다.** 세 갈래를 구분하지 못한다.
치환은 **대괄호가 붙은 형태만** 노린다.

```text
바꾼다     [차트번호]        [p].[차트번호]     p.[차트번호]
안 바꾼다  @ChartNo         ChartNo =         'ChartNo'        N'ChartNo'
```

각 Task 는 치환 후 **`@` 로 시작하는 이름과 `=` 앞의 이름이 하나도 안 바뀌었는지**를 먼저 확인한다.

### 2.3 `V14` 게이트 취급

`tools/verify-docs.js` 의 `V14` 는 `docs/phase4/plans/**` 의 sql 펜스 안 대괄호 식별자가
기준선(`04` §8 · `05` · `06`)에 실재하는지 본다. **이 계획서의 새 이름은 `T47` 전까지 실재하지 않는다.**

`[X]` **그래서 이 계획서는 sql 펜스를 쓰지 않고 text 펜스를 쓴다.**
게이트를 우회하는 것이 아니라, `V14` 가 판정할 근거가 아직 없는 구간이라는 뜻이다.
**`T47` 이 기준선에 새 이름을 넣으면 `V14` 가 이 파일에 대해서도 다시 힘을 갖는다.**
`T48` 이 그때 이 파일의 펜스를 sql 로 되돌리고 `V14` green 을 확인한다.

`T40`~`T46` 이 만드는 `deploy/*.sql` 은 `V14` 의 대상이 아니다(`plans/**` 만 본다).

---

## 3. 명명표 ― 컬럼 47개

**이 표가 단일 출처다.** 각 Task 는 자기 테이블 행만 쓴다.
원칙: 테이블명과 같은 규칙(한글 · 접두사 없음 · 축약 없음), 예약어 회피, 대괄호 표기.

### 3.1 `수검자` (17)

| 현재 | 신규 | 현재 | 신규 |
|---|---|---|---|
| `PatientId` | `수검자ID` | `TelNumber` | `전화번호` |
| `ChartNo` | `차트번호` | `Zipcode` | `우편번호` |
| `Name` | `성명` | `Address` | `주소` |
| `SocialNumber` | `주민번호` | `AddressDetail` | `상세주소` |
| `Birthday` | `생년월일` | `Memo` | `비고` |
| `Gender` | `성별` | `HepatitisBExcluded` | `B형간염제외여부` |
| `EMail` | `이메일` | `CreationDate` | `생성일시` |
| `CelNumber` | `휴대전화` | `LastEditDate` | `최종수정일시` |
| `CelNumberS` | `휴대전화검색값` (계산열) | | |

### 3.2 `예약접수` (8)

| 현재 | 신규 | 현재 | 신규 |
|---|---|---|---|
| `WorkId` | `업무ID` | `StatusCode` | `상태코드` |
| `PatientId` | `수검자ID` | `CreationDate` | `생성일시` |
| `ReservationDate` | `예약일` | `LastEditDate` | `최종수정일시` |
| `TimeSlotCode` | `시간대코드` | `RowVersion` | `행버전` |

### 3.3 `검사항목` (3)

| 현재 | 신규 |
|---|---|
| `WorkId` | `업무ID` |
| `ExamItemCode` | `검사항목코드` |
| `ExamSourceCode` | `검사출처코드` |

`[X]` **`검사코드` 를 컬럼명으로 쓰지 않는다** ― 테이블 `검사코드` 와 이름이 겹친다. `검사항목코드` 로 한다.

### 3.4 `검사코드` (6)

| 현재 | 신규 |
|---|---|
| `ExamItemCode` | `검사항목코드` |
| `ExamItemName` | `검사항목명` |
| `NexRuleCode` | `국가검사규칙코드` |
| `AdditionalExamCode` | `추가검사코드` |
| `AdditionalGenderCode` | `추가검사성별코드` |
| `AdditionalActive` | `추가검사사용여부` |

`[X]` **`AdditionalActive` 를 없애고 전 검사 공통의 `사용가능` 으로 바꾸지 않는다.**
`00_Project_Policy.md` `NEX-07` 이 "NEX는 시스템이 자동 생성·재구성하며 사용자가 임의 추가·삭제할 수 없다"
라고 못박아, 국가검사를 끄는 규칙이 상위 문서 어디에도 없다. 공통 스위치를 만들면
Seed 한 행으로 국가검사가 조용히 사라지는데 그것을 막는 규칙도 잡는 테스트도 없다.
**이름을 `추가검사사용여부` 로 바꾸면 `0` 이 "꺼짐"이 아니라 "추가검사가 아님"으로 읽힌다 ― 그것으로 충분하다.**

`[X]` **`카테고리` 컬럼을 신설하지 않는다.** `NexRuleCode`·`AdditionalExamCode` 의 NULL 조합에서
파생되는 값이다(`04` §1.6 계산값·영속값 분리). `scripts/inspect.sql` 이 `Role` 로 이미 보여준다.

### 3.5 `휴무일` (4)

| 현재 | 신규 | 현재 | 신규 |
|---|---|---|---|
| `HolidayDate` | `휴무일자` | `Active` | `사용여부` |
| `HolidayName` | `휴무일명` | `Memo` | `비고` |

### 3.6 `완료이력` (2)

| 현재 | 신규 |
|---|---|
| `PatientId` | `수검자ID` |
| `CompletionDate` | `완료일자` |

### 3.7 `변경이력` (7)

| 현재 | 신규 | 현재 | 신규 |
|---|---|---|---|
| `HistoryId` | `이력ID` | `TargetTable` | `대상테이블` |
| `CreationDate` | `기록일시` | `TargetKey` | `대상키` |
| `OperatorName` | `조작자명` | `ResultCode` | `결과코드` |
| `OperationCode` | `업무코드` | | |

### 3.8 제약·인덱스 이름은 바뀌지 않는다

`04` §3.3 의 형식에서 **본체는 "술어의 의미 태그이며 컬럼명의 기계적 전개가 아니다"** 라고 이미 정해져 있다.
`CK_수검자_SOCIAL_FORMAT` 은 컬럼명이 `주민번호` 가 되어도 그대로다.

**단 하나 예외:** `IX_수검자_CEL_NUMBER_S` 는 `T46` 에서 필터식이 바뀌므로 재생성되지만 **이름은 유지한다.**

---

## 4. `CelNumberS` 계산열 전환 (`T46` 안에서)

### 4.1 현재

```sql
[CelNumberS] VARCHAR(13) NULL                                    ← Write SP 가 계산해서 저장
CK_수검자_CEL_NORMALIZED   CelNumberS = REPLACE(CelNumber,'-','')   ← 어긋남을 사후 검사
CK_수검자_CEL_DIGIT        CelNumberS 는 숫자만
IX_수검자_CEL_NUMBER_S     ON (CelNumberS) WHERE CelNumberS IS NOT NULL
```

### 4.2 계산열 전환안 (기각)

```text
휴대전화검색값 AS (CONVERT(VARCHAR(13), REPLACE(휴대전화, '-', ''))) PERSISTED
IX_수검자_CEL_NUMBER_S  ON (휴대전화검색값)
                        INCLUDE (...) WHERE 휴대전화 IS NOT NULL    ← 필터를 기반열로
CK_수검자_CEL_NORMALIZED   삭제                                     ← 어긋남이 구조적으로 불가능
CK_수검자_CEL_DIGIT        유지 (계산열 대상)
```

`[X]` **이 안은 채택되지 않았다.** 사용자가 컬럼 완전 제거를 택했다(`T46`). 실제 결과는 다음과 같다.

```text
컬럼                     17 -> 16   휴대전화검색값 없음
CK_수검자_CEL_NORMALIZED 삭제       지킬 대상이 사라졌다
CK_수검자_CEL_DIGIT      유지       술어를 휴대전화 기준으로 다시 썼다
IX_수검자_CEL_NUMBER_S   삭제       Key 컬럼이 없어졌다. NCI 5 -> 4
@MobilePhone 검색        REPLACE([휴대전화],'-','') = @MobilePhone   비-SARGable 전체 스캔
```

수검자 규모가 커져 스캔 비용이 문제가 되면 위 계산열 안이 되돌릴 지점이다.

### 4.3 사용자 요청과의 차이

사용자 요청은 `CelNumberS` **완전 제거**였다. 제거할 수 없다.

```text
05 §7.2   @MobilePhone VARCHAR(13) O   '-' 제거 후 정확검색       ← 계약 Parameter
04_Procedures_Select.sql:133
          AND (@MobilePhone IS NULL OR p.[CelNumberS] = @MobilePhone)
```

지우면 `REPLACE(p.[휴대전화],'-','') = @MobilePhone` 이 되어 **비-SARGable 전체 스캔**이고
인덱스를 걸 방법이 없다. `TelNumberS` 는 읽는 계약이 없어 R3 가 지웠지만(`04` §1.5) 여기는 다르다.

**계산열 전환이 요청의 취지(중복 저장값 제거)를 계약을 깨지 않고 달성한다.**

| | 제거 | 계산열 전환 (채택) |
|---|:---:|:---:|
| 중복 저장값 | 없어짐 | 없어짐 |
| `@MobilePhone` 검색 | 전체 스캔 | 인덱스 seek 유지 |
| `CK_수검자_CEL_NORMALIZED` | 없어짐 | 없어짐 (불필요해짐) |
| Write SP 의 계산 책임 | 없어짐 | 없어짐 (`Msg 271` 이 강제) |
| `tests/01` 컬럼 지문 | 1행 삭제 | **변화 없음** |
| `05` 계약 | **깨짐** | 유지 |

### 4.4 `[!]` 배포 순서 함정

계산열은 `CREATE TABLE` 안에서 기반열보다 **뒤에** 와야 한다.
현재 순서가 `CelNumberS` → `CelNumber` 이므로 **둘을 맞바꿔야 한다.**
`tests/01` 은 컬럼 순서를 보지 않지만 `04` §8.1.2 표는 `T47` 에서 함께 고친다.

---

## 5. Task

### 공통 절차 (`T40`~`T46` 전부)

`[X]` **`scripts/inspect.sql` 을 열거 범위에 반드시 넣는다.** 어느 게이트도 이 파일을 돌리지 않아
`T41` 이 여기의 `완료이력` 참조를 놓쳤고 `T42` 에서야 드러났다. 8단계에 `inspect.sh` 를 넣는다.

```text
1  RED    해당 테이블의 컬럼 하나를 새 이름으로 참조하는 배포를 시도해 Msg 207 을 관측한다
2  치환   01_Schema.sql 의 그 테이블 정의 ― 대괄호 형태만
3  치환   그 테이블을 읽는 03_Functions.sql · 04_Procedures_Select.sql
          Result Set 목록에는 `Result Set 컬럼명 = [새이름]` 별칭을 붙인다 (§2.1)
4  치환   그 테이블을 읽는 tests/00 · 00b · 01 · 02 · 03 · 04 · contract/**
5  확인   @ 로 시작하는 이름 0건 변경 · Result Set 별칭 0건 변경 (§2.2)
5b 확인   그 테이블 전용 토큰의 잔존 0건을 grep 으로 단언한다
          (T43 에서 inspect.sql 20행의 별칭 없는 [업무ID] 를 이렇게 잡았다)
6  실행   ./scripts/test.sh 전건
7  실행   ./scripts/verify-contract-all.sh 25건
8  실행   verify-baseline · verify-winforms · verify-docs · ./scripts/inspect.sh
9  커밋   한 커밋. 게이트 red 를 남기지 않는다
```

### `T40` `변경이력` 7컬럼 ― 예행연습

- Produces: `deploy/01_Schema.sql` · `tests/01_Schema_Tests.sql`
- **읽는 SP·TVF 가 0개다.** 실패해도 잃을 것이 없고 BOM·`N` 접두사·`V16`·게이트가 실제로 작동하는지 한 바퀴 검증된다.
- `[!]` `ResultCode` → `결과코드` 는 **`변경이력` 의 컬럼만** 바꾼다. RS0 의 `ResultCode` 는 그대로다.
- 완료조건: `test.sh` PASS 91 유지 · 계약 25/25 · 게이트 4종 green

### `T41` `완료이력` 2컬럼

- 읽는 곳: `03_Functions.sql` `UFN_HC_검진대상확인` 1곳 · `tests/00`·`01`·`03`·`04`
- 완료조건: `RUL-G03`~`G07` 5건 PASS 유지

### `T42` `휴무일` 4컬럼

- 읽는 곳: `03_Functions.sql` `UFN_HC_일정확인` · `02_Seed.sql` · `tests/01`·`02`·`03`
- 완료조건: `SEED-DEPLOY Holiday 2` · `HOL` 계열 PASS 유지

### `T43` `검사항목` 3컬럼

- 읽는 곳: `03_Functions.sql` 1곳 · `04_Procedures_Select.sql` 6곳 · `tests/00`·`00b`·`01`·`04`
- `[!]` RS2·RS3 의 계약 컬럼이 `ExamCode`·`ExamName` 이므로 별칭이 붙는다
- 완료조건: 계약 `14`~`16` PASS · `RUL-A09` PASS

### `T44` `검사코드` 6컬럼

- 읽는 곳: `02_Seed.sql` · `03_Functions.sql` 2곳 · `04_Procedures_Select.sql` 6곳 · `tests/01`·`02`·`03`
- `[!]` `CK_검사코드_AEX_GROUP`·`CK_검사코드_ROLE_REQUIRED` 의 식이 컬럼명을 담는다. 함께 바꾼다
- `[!]` `UX_검사코드_AEX_CODE` 는 필터형 인덱스라 `SET QUOTED_IDENTIFIER ON` 이 필요하다 (`CLAUDE.md` §6)
- 완료조건: `SED-001`~`011` PASS · `RUL-A01`~`A09` PASS

### `T45` `예약접수` 8컬럼

- 읽는 곳: `04_Procedures_Select.sql` 13곳 · `tests/00`·`00b`·`01`·`03`·`04`·`contract/**`
- `[!]` `RowVersion` → `행버전`. `ROWVERSION` 은 **타입 이름**이고 컬럼명과 별개다
- 완료조건: 계약 `11`~`25` PASS · 정원·중복 테스트 PASS

### `T46` `수검자` 17컬럼 + `CelNumberS` 계산열 전환

- 읽는 곳: `03_Functions.sql` 3곳 · `04_Procedures_Select.sql` 7곳 · `tests/00`·`01`·`02`·`03`·`04`·`contract/**`
- §4 의 계산열 전환을 **같은 커밋**에서 한다 (인덱스 필터식이 함께 바뀌므로 나눌 수 없다)
- `[!]` 컬럼 순서: `휴대전화` → `휴대전화검색값` 로 맞바꾼다 (§4.4)
- 완료조건: `SCH-001`~`013` PASS(`SCH-014` 는 `T30` 이전이라 FAIL 유지) ·
  `@MobilePhone` 검색이 인덱스 seek 인지 실행계획으로 확인 · 계약 `02`~`08` PASS

### `T47` 기준선·문서 재봉인

- `04_DB_Design.md` §3.3 명명규칙 · §8.1~8.7 컬럼표 · §10 Key/Constraint 총괄
- `scripts/verify-baseline.sh` 의 `04_DB_Design.md` SHA-256 **1개**를 같은 커밋에서 교체
- `06_..._CANDIDATE.md` §9.2 허용목록에 **PERSISTED 계산열** 추가 (근거: §1.4 실측)
- `docs/phase4/plans/**` 1,632곳
- `[!]` `05_DB_Rule_SP_Contract.md` 는 **건드리지 않는다** ― 변형 1 이므로 Result Set·Parameter 가 그대로다.
  `verify-baseline` 의 나머지 5개 해시도 그대로다
- 완료조건: `verify-baseline` 6/6 · `verify-docs` PASS 18 / FAIL 0

### `T48` 최종 검증

- 이 계획서의 text 펜스를 sql 로 되돌리고 `V14` green 확인 (§2.3)
- `scripts/inspect.sql` 의 컬럼 참조 갱신 · `README.md` 표 갱신
- `./scripts/rebuild.sh` → `./scripts/test.sh` 전건 · 계약 25/25 · 게이트 4종
- 태그 `phase4-r3-t48-korean-columns`

---

## 6. 되돌리기

각 Task 가 한 커밋이므로 `git revert` 한 번으로 그 테이블만 되돌아온다.
`T47` 전까지는 기준선 파일이 그대로라 `verify-baseline` 이 항상 green 이다.

`T47` 이후에 되돌리려면 `T47` 과 `T46`~`T40` 을 **역순으로** revert 한다.
`verify-baseline.sh` 의 해시는 `T47` revert 에 함께 딸려 온다.

**DB 자체는 `./scripts/rebuild.sh` 가 항상 clean-create 하므로 별도 복구가 필요 없다** (`CLAUDE.md` §8).

---

## 7. 게이트

| 게이트 | `T40`~`T46` | `T47` | `T48` |
|---|---|---|---|
| `verify-baseline` | 6/6 (기준선 무변경) | 6/6 (해시 1개 교체) | 6/6 |
| `verify-winforms` | exit 0 | exit 0 | exit 0 |
| `verify-docs` | PASS 18 / FAIL 0 | PASS 18 / FAIL 0 | PASS 18 / FAIL 0 |
| `verify-contract-all` | 25 PASS / 0 FAIL | 해당 없음 | 25 PASS / 0 FAIL |
| `test.sh` | PASS 91 (`SCH-014` FAIL 1 유지) | 해당 없음 | PASS 91 |

`[X]` **실행하지 않은 검증을 `PASS` 로 기록하지 않는다** (`CLAUDE.md` §10).
PLANNED / NOT RUN / BLOCKED 를 쓴다.
