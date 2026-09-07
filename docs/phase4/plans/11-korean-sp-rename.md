# Stage 11 ― SP·Parameter·Result Set 전면 한글화 (R4)

**Index:** `2026-09-04-phase4-database-implementation.md`
**Spec:** `../06_DB_Transaction_Security_Seed.md`
**전제:** `plans/01`~`10` 완료. 태그 `phase4-db-final-english` (`4ae40f6`) 가 되돌림 지점이다.

`[!]` **이 파일은 승인 전 산출물이다.** 사용자가 §3~§6 매핑표를 승인하기 전에는
`deploy/`·`tests/`·`tools/`·기준선 어느 것도 열지 않는다.

`[X]` **이 문서의 펜스는 전부 `text` 다.** 새 이름은 기준선 04·05 재봉인 전까지 실재하지 않으므로
`V14`(계획 SQL 의 대괄호 식별자가 기준선에 실재하는가)가 판정할 근거가 없다 ― `plans/09` §2.3 선례.
재봉인 뒤 마지막 Task 가 필요한 펜스를 `sql` 로 되돌리고 `V14` green 을 확인한다.

---

## 0. 무엇을 바꾸고 무엇을 안 바꾸는가

```text
바꾼다    SP 16개 이름의 접두사 뒷부분      USP_HC_SELECT_수검자목록 -> USP_HC_수검자목록_조회
          SP Parameter 99건 (고유 34종)
          Result Set 컬럼 (고유 73종)
          TVF 4개의 Parameter·반환 컬럼      (§7 결정 D2 에 따름)
          기준선 04 §3.3 · 05 §1.2 §1.6 §3.1 §6~§13
          tools/expected-contracts.json      계약 110종의 컬럼 이름

안 바꾼다 USP_HC_ · UFN_HC_ · SEQ_HC_ 접두사   게이트 5개가 name LIKE 'USP[_]HC[_]%' 로 계약 SP 를 고른다
          TVF 4개 이름                        이미 한글이다
          물리 테이블 6개 · 컬럼 48개          plans/09 가 이미 한글화했다
          제약·인덱스 이름                     04 §3.3 대로 본체는 영문 SNAKE_CASE 유지
          ResultCode 값 체계 (0~701)          C# DbCode Enum 의 이름은 05 §16.1 그대로 영문
          상태코드 4종 · 시간대 2종 · 정원 20   업무 규칙
          검사항목코드 EX001~EX019 · OPT01~OPT07 · NEX-01~NEX-06   데이터 값이지 식별자가 아니다
          Scope 값 ALL/SLOT/EXTRA/SLOT_EXTRA/NONE                  같은 이유
          ExamType 값 BASIC/CONDITIONAL                            같은 이유
          ActionCode 값 EDIT_RESERVATION 등 5종                     같은 이유
```

`[!]` **값과 이름을 구분한다.** 한글화 대상은 **식별자**뿐이다. 문자열 리터럴로 저장·비교되는 코드값
(`'RSV'`, `'AM'`, `'EX001'`, `'BASIC'`, `'ALL'`, `'EDIT_RESERVATION'`)은 데이터이며
`CK` 제약·Seed·테스트 기대값이 전부 그 값을 고정하고 있다. 건드리면 계약 파괴다.

---

## 1. 실측 근거

### 1.1 계약 식별자 실측 (2026-09-08)

```text
SP Parameter        99건 등장 / 고유 34종      기준선 05 §7~§12 도출 = 배포 구현 signature 일치
Result Set 컬럼     고유 73종                  기준선 05 §3.1·§7~§12 도출
                                               배포 deploy/04~07 의 별칭 73종과 diff 0
TVF Parameter       고유 2종 추가              @CutoffType · @UseSavedExams
                                               (@ServerTime 은 RS0 컬럼과 이름이 같다)
TVF 반환 컬럼        고유 3종 추가              WorkCode · WorkMessage · CanUse
Parameter <-> RS 겹침  21종

한글화 대상 고유 식별자 합계   91종
```

`[!]` **73종 일치는 "구현이 계약과 같다"는 증거가 아니다.** 이름의 **집합**이 같다는 뜻일 뿐,
어느 Result Set 이 어느 컬럼을 몇 개 갖는가는 별개다 ― `06` §44.8 의
`SELECT_수검자상세` RS1 결함이 정확히 그 축에서 살아남았다.
따라서 `expected-contracts.json` 재작성은 **기준선 05 의 표에서** 하고 구현을 읽지 않는다.

### 1.2 한글 식별자는 안전하다 (선행 세션 실측)

```text
정상   Parameter · 지역변수 · 별칭 · 명명인수(@이름 = ...) · 대괄호 없는 정규식별자
       sys.parameters 메타데이터 · 오류 메시지
예산   sysname = nvarchar(128) -> 128 글자. 바이트가 아니라 글자다
함정   식별자가 아니라 N 없는 리터럴이다. V16 이 정적으로 잡는다
applock 자원명은 바이트 비교인데 DB 는 CI. 자원명 UPPER 는 이미 적용돼 있다
```

### 1.3 기준선 02·03 은 열지 않는다

```text
02_Function_Definition.xlsx   SP 이름 0건
03_Wireframe_Definition.md    SP 이름 0건
```

### 1.4 `V14` 의 기준선 수집 정규식이 ASCII 전용이다

`tools/verify-docs.js` 의 `V14` 는 기준선 05 에서 알려진 식별자를 이렇게 모은다.

```text
read(BASE05).matchAll(/`@?([A-Za-z][A-Za-z0-9_]*)`/g)
```

05 의 Parameter·Result 컬럼이 한글이 되면 **이 정규식이 한 건도 못 모은다.**
04 §8(한글 허용)과 06(대괄호, 한글 허용)만 남아 `V14` 의 판정 범위가 조용히 줄어든다.
재봉인 커밋에서 이 정규식을 `[A-Za-z가-힣]` 계열로 함께 넓힌다 ― 게이트가 fail-open 하는 것을 막는다.

---

## 2. 이름이 세 갈래인데 글자가 같다 ― R4 의 최대 함정

`plans/09` §2.2 가 컬럼 한글화 때 경고한 것이 R4 에서 **세 갈래 전부 같은 글자**가 되어 돌아온다.

| 네임스페이스 | R3 (지금) | R4 (이후) | 구분 표지 |
|---|---|---|---|
| DB 컬럼명 | `[차트번호]` | `[차트번호]` | 대괄호 + 테이블 별칭 |
| SP Parameter | `@ChartNo` | `@차트번호` | `@` |
| Result Set 컬럼 | `ChartNo` | `[차트번호]` | `=` 의 **왼쪽** |

`[X]` **별칭에 대괄호를 반드시 붙인다.** R3 는 `ChartNo = CAST(p.[차트번호] ...)` 로
왼쪽이 맨몸 영문이라 눈으로 구분됐다. R4 는 양쪽이 같은 글자라 대괄호가 유일한 표지다.

```text
R4 표준형   , [차트번호] = CAST(p.[차트번호] AS NVARCHAR(100))
금지형      , 차트번호   = CAST(p.[차트번호] AS NVARCHAR(100))
```

`[X]` **단순 정규식 치환을 쓰지 않는다.** 치환 단위는 다음 셋이며 서로 다른 패턴이다.

```text
Parameter    @ChartNo            -> @차트번호          '@' 로 시작하는 토큰만
별칭 좌변    ChartNo =           -> [차트번호] =       '=' 앞의 맨몸 영문 토큰만
AS 별칭      AS Code             -> AS [결과코드]      'AS ' 뒤의 맨몸 영문 토큰만
안 바꾼다    'ChartNo'  N'ChartNo'                     Field 값·변경이력 컬럼명 리터럴
```

`[!]` **`Field` 값과 `변경이력.컬럼명` 값은 리터럴이지 식별자가 아니다.**
`CAST('SocialNumber' AS VARCHAR(50)) AS Field` 의 `'SocialNumber'` 는 C# 이 읽는 **데이터**다.
이 값을 한글화할지는 §7 결정 D3 에서 따로 정한다 ― 자동 치환에 섞지 않는다.

---

## 3. SP 16개 이름

형식은 `USP_HC_{한글업무명}_{동사}` 다. 개발 전용 `DEV_완료이력_등록` 과 같은 어순이다.

| ID | 현재 | 신규 |
|---|---|---|
| SP-COM-01 | `USP_HC_SELECT_공통업무상태` | `USP_HC_공통업무상태_조회` |
| SP-PAT-01 | `USP_HC_SELECT_수검자목록` | `USP_HC_수검자목록_조회` |
| SP-PAT-02 | `USP_HC_SELECT_수검자상세` | `USP_HC_수검자상세_조회` |
| SP-PAT-03 | `USP_HC_INSERT_수검자` | `USP_HC_수검자_등록` |
| SP-PAT-04 | `USP_HC_UPDATE_수검자정보` | `USP_HC_수검자정보_수정` |
| SP-PAT-05 | `USP_HC_SELECT_수검자유효업무` | `USP_HC_수검자유효업무_조회` |
| SP-RSV-01 | `USP_HC_SELECT_예약가능정보` | `USP_HC_예약가능정보_조회` |
| SP-RSV-02 | `USP_HC_INSERT_예약` | `USP_HC_예약_등록` |
| SP-RSV-03 | `USP_HC_UPDATE_예약변경` | `USP_HC_예약_변경` |
| SP-RSV-04 | `USP_HC_UPDATE_예약취소` | `USP_HC_예약_취소` |
| SP-WRK-01 | `USP_HC_SELECT_예약접수목록` | `USP_HC_예약접수목록_조회` |
| SP-WRK-02 | `USP_HC_SELECT_예약접수상세` | `USP_HC_예약접수상세_조회` |
| SP-RCP-01 | `USP_HC_UPDATE_접수완료` | `USP_HC_접수_완료` |
| SP-RCP-02 | `USP_HC_UPDATE_접수추가검사` | `USP_HC_접수추가검사_변경` |
| SP-RCP-03 | `USP_HC_UPDATE_접수취소` | `USP_HC_접수_취소` |
| SP-LOG-01 | `USP_HC_SELECT_변경이력` | `USP_HC_변경이력_조회` |

TVF 4개 이름은 이미 한글이라 바뀌지 않는다.

```text
UFN_HC_일정확인 · UFN_HC_검진대상확인 · UFN_HC_국가검사구성 · UFN_HC_추가검사확인
```

---

## 4. 명명 원칙

```text
1  물리 컬럼과 짝이 있으면 그 컬럼명을 그대로 쓴다      PatientId -> 수검자ID
2  BIT 는 능력이면 ~가능, 그 밖에는 ~여부              CanSave -> 저장가능 / IsToday -> 오늘여부
3  코드/메시지 짝은 접두어를 공유한다                   BlockCode·BlockMessage -> 차단코드·차단메시지
4  축약하지 않는다. 접두사를 붙이지 않는다               테이블·컬럼 규칙과 같다
5  공백·하이픈·괄호·슬래시를 쓰지 않는다                05 §1.7
6  최장 이름 11글자. 예산 128글자 대비 여유             추가검사01선택여부
```

---

## 5. Parameter 매핑 ― 고유 34종

### 5.1 수검자 (17)

| 현재 | 신규 | 물리 컬럼 짝 |
|---|---|:---:|
| `@PatientId` | `@수검자ID` | O |
| `@ChartNo` | `@차트번호` | O |
| `@AutoChartNo` | `@차트번호자동발급여부` | - |
| `@Name` | `@성명` | O |
| `@SocialNumber` | `@주민번호` | O |
| `@Birthday` | `@생년월일` | O |
| `@MobilePhone` | `@휴대전화` | O |
| `@Phone` | `@전화번호` | O |
| `@Email` | `@이메일` | O |
| `@Zipcode` | `@우편번호` | O |
| `@Address` | `@주소` | O |
| `@AddressDetail` | `@상세주소` | O |
| `@Memo` | `@비고` | O |
| `@HepatitisBExcluded` | `@B형간염제외여부` | O |
| `@ConfirmSimilarPatient` | `@유사수검자확인여부` | - |
| `@LastEditDate` | `@최종수정일시` | O |
| `@OperatorName` | `@조작자명` | O |

### 5.2 업무·예약·접수 (10)

| 현재 | 신규 | 물리 컬럼 짝 |
|---|---|:---:|
| `@WorkId` | `@업무ID` | O |
| `@RowVersion` | `@행버전` | O |
| `@ReservationType` | `@예약구분` | - |
| `@ReservationDate` | `@예약일` | O |
| `@TimeSlot` | `@시간대코드` | O |
| `@Status` | `@상태코드` | O |
| `@AexOpt01Selected` | `@추가검사01선택여부` | - |
| `@AexOpt02Selected` | `@추가검사02선택여부` | - |
| `@AexOpt03Selected` | `@추가검사03선택여부` | - |
| `@AexOpt04Selected` | `@추가검사04선택여부` | - |
| `@AexOpt05Selected` | `@추가검사05선택여부` | - |
| `@AexOpt06Selected` | `@추가검사06선택여부` | - |
| `@AexOpt07Selected` | `@추가검사07선택여부` | - |

### 5.3 조회조건·이력 (4)

| 현재 | 신규 | 물리 컬럼 짝 |
|---|---|:---:|
| `@FromDate` | `@시작일` | - |
| `@ToDate` | `@종료일` | - |
| `@TargetTable` | `@대상테이블` | O |
| `@TargetKey` | `@대상키` | O |

### 5.4 TVF 전용 (3)

| 현재 | 신규 |
|---|---|
| `@ServerTime` | `@서버시각` |
| `@CutoffType` | `@마감구분` |
| `@UseSavedExams` | `@저장검사사용여부` |

`@ServerTime` 은 RS0 컬럼 `ServerTime` 과 같은 글자이므로 고유 이름 수에는 한 번만 센다.

---

## 6. Result Set 컬럼 매핑 ― 고유 73종

### 6.1 RS0 처리결과 (5) ― 모든 SP 공통

| 순서 | 현재 | 신규 | 타입 |
|---:|---|---|---|
| 1 | `Success` | `성공여부` | `BIT` |
| 2 | `Code` | `결과코드` | `INT` |
| 3 | `Message` | `결과메시지` | `NVARCHAR(300)` |
| 4 | `Field` | `오류항목` | `VARCHAR(50)` |
| 5 | `ServerTime` | `서버시각` | `DATETIME2(7)` |

### 6.2 공통업무상태 RS1 (10)

| 현재 | 신규 | 현재 | 신규 |
|---|---|---|---|
| `Today` | `오늘날짜` | `WithinHours` | `운영시간내여부` |
| `DayName` | `요일명` | `CanWorkNow` | `현재업무가능` |
| `HolidayName` | `휴무일명` | `BlockCode` | `차단코드` |
| `OpenTime` | `운영시작시각` | `BlockMessage` | `차단메시지` |
| `CloseTime` | `운영종료시각` | | |
| `IsBusinessDay` | `업무일여부` | | |

### 6.3 수검자 (15) ― 전부 물리 컬럼과 짝이 있다

| 현재 | 신규 | 현재 | 신규 |
|---|---|---|---|
| `PatientId` | `수검자ID` | `Zipcode` | `우편번호` |
| `ChartNo` | `차트번호` | `Address` | `주소` |
| `Name` | `성명` | `AddressDetail` | `상세주소` |
| `SocialNumber` | `주민번호` | `Memo` | `비고` |
| `Birthday` | `생년월일` | `HepatitisBExcluded` | `B형간염제외여부` |
| `Gender` | `성별` | `LastEditDate` | `최종수정일시` |
| `MobilePhone` | `휴대전화` | | |
| `Phone` | `전화번호` | | |
| `Email` | `이메일` | | |

### 6.4 업무 (7)

| 현재 | 신규 | 물리 컬럼 짝 |
|---|---|:---:|
| `WorkId` | `업무ID` | O |
| `ReservationDate` | `예약일` | O |
| `TimeSlot` | `시간대코드` | O |
| `Status` | `상태코드` | O |
| `StatusName` | `상태명` | - |
| `IsToday` | `오늘여부` | - |
| `RowVersion` | `행버전` | O |

### 6.5 정원 (4)

| 현재 | 신규 |
|---|---|
| `Capacity` | `정원` |
| `CurrentCount` | `현재인원` |
| `AfterCount` | `적용후인원` |
| `SeatsLeft` | `잔여자리` |

### 6.6 검사 (5)

| 현재 | 신규 | 물리 컬럼 짝 |
|---|---|:---:|
| `ExamCode` | `검사항목코드` | O |
| `ExamName` | `검사항목명` | O |
| `ExamType` | `국가검사구분` | - |
| `RuleCode` | `국가검사규칙코드` | O |
| `OptionCode` | `추가검사코드` | O |

### 6.7 가능한업무 RS4 (4)

| 현재 | 신규 |
|---|---|
| `ActionCode` | `업무동작코드` |
| `Allowed` | `허용여부` |
| `ReasonCode` | `사유코드` |
| `ReasonMessage` | `사유메시지` |

### 6.8 변경이력 RS1 (6) ― 전부 물리 컬럼과 짝이 있다

| 현재 | 신규 | 현재 | 신규 |
|---|---|---|---|
| `LogId` | `이력ID` | `ColumnName` | `컬럼명` |
| `RecordedAt` | `기록일시` | `BeforeValue` | `변경전` |
| `OperatorName` | `조작자명` | `AfterValue` | `변경후` |

### 6.9 예약가능정보 RS1 예약요약 (7 신규)

| 현재 | 신규 |
|---|---|
| `Scope` | `변경범위` |
| `ReservationType` | `예약구분` |
| `DateChanged` | `예약일변경여부` |
| `SlotChanged` | `시간대변경여부` |
| `ExtraChanged` | `추가검사변경여부` |
| `OtherWorkId` | `다른업무ID` |
| `CanSave` | `저장가능` |

나머지 7컬럼(`PatientId`·`WorkId`·`ReservationDate`·`TimeSlot`·`CanWorkNow`·`BlockCode`·`BlockMessage`)은 위 절의 이름을 그대로 쓴다.

### 6.10 예약가능정보 RS2 시간대정보 (5 신규)

| 현재 | 신규 |
|---|---|
| `SlotName` | `시간대명` |
| `IsOpen` | `운영여부` |
| `CutoffTime` | `마감시각` |
| `CutoffPassed` | `마감경과여부` |
| `CanSelect` | `선택가능` |

### 6.11 예약가능정보 RS3 검진대상 (3 신규)

| 현재 | 신규 |
|---|---|
| `Eligible` | `검진대상여부` |
| `Age` | `나이` |
| `LastCheckupDate` | `최근완료일자` |

### 6.12 예약가능정보 RS5 추가검사항목 (2 신규)

| 현재 | 신규 | 의미 |
|---|---|---|
| `Requested` | `요청선택여부` | 호출자가 요청한 선택값 |
| `Selected` | `유효선택여부` | Rule 적용 후 실제 유효 선택값 |

### 6.13 TVF 전용 반환 컬럼 (3 신규)

| 현재 | 신규 | TVF |
|---|---|---|
| `WorkCode` | `업무가능코드` | `UFN_HC_일정확인` |
| `WorkMessage` | `업무가능메시지` | `UFN_HC_일정확인` |
| `CanUse` | `일정가능` | `UFN_HC_일정확인` |

---


## 7. 승인 결과 (2026-09-08 사용자 승인)

```text
D1 SP 이름 형식      USP_HC_{한글업무명}_{동사}          §3 표 그대로 채택
D2 TVF 4개           한글화한다                          Parameter 3종 · 반환 컬럼 3종 추가
D3 Field 값          한글로 맞춘다                       RS0.오류항목 = N'주민번호' 형태
D4 SP 내부 지역변수   전부 한글화                         제안(영문 유지)과 다른 사용자 결정
```

## 8. D4 가 드러낸 추가 범위 ― 착수 전 표에 없던 것

`D4` 는 "지역변수" 한 줄이었지만 실측하니 네 갈래였다. 전부 매핑에 넣었다.

```text
순수 지역변수        93종      @Old*·@Cur*·@Rs*·@W* 계열이 대부분
테이블 변수 5종      @Slots·@Aex·@AexEval·@Req·@Codes 와 그 컬럼
파생테이블 컬럼 14종  After·Cutoff·NowTime·TodayBiz·SlotOpen·ReqDow·ReqHoliday·Rc
                     ReqBiz·TodayDow·TodayHoliday·RawCutoff·Ord·Slot
바꾸면 안 되는 것 4종 @Resource·@LockMode·@LockOwner·@LockTimeout
                     sp_getapplock 의 **시스템 Parameter 이름**이다. 지역변수처럼 보이지만
                     바꾸면 호출이 깨진다. r4-rename-map.json 의 forbidden 이 이것이다
```

`tools/r4-rename.js check` 의 `C2` 가 금지 이름이 `sp_getapplock` 호출 밖에서 쓰이지 않는지 본다.

## 9. 계약 타입 변경 1건 ― `오류항목`

```text
RS0.Field  VARCHAR(50)  ->  RS0.오류항목  NVARCHAR(50)
```

`D3` 으로 이 컬럼에 담기는 값이 한글 Parameter 이름이 되었다. 한글은 CP949 에 있어
`VARCHAR` 로도 살아남지만 그 우연에 계약을 걸지 않는다(`database/CLAUDE.md` §5).
`05` §3.1 과 `V17` 의 기대 타입을 함께 바꿨다. **R4 에서 타입이 바뀐 계약은 이 하나뿐이다.**

## 10. 배포 수렴 ― 옛 SP 16개를 지운다

SP 는 `CREATE OR ALTER` 라 새 이름으로 배포해도 옛 이름이 DB 에 그대로 남는다.
기존 DB 에 `Deploy.sql` 을 돌리면 `VER-003` 이 16 이 아니라 **32** 를 센다.

```text
deploy/01_Schema.sql 에 DROP PROCEDURE IF EXISTS 16줄 추가
rebuild.sh(DROP DATABASE) 경로에서는 아무 일도 하지 않는다
```

## 11. 게이트 보강 3건

```text
V14   기준선 05 에서 알려진 식별자를 모으는 정규식이 ASCII 전용이었다 (§1.4)
      한글이 되면 한 건도 못 모아 판정 범위가 조용히 줄어든다 -> [A-Za-z가-힣] 로 넓혔다
V17   RS0 5컬럼 기대값을 한글 이름과 NVARCHAR(50) 으로 갱신
V11   개명표의 '현재' 칸은 사라진 이름을 적을 수밖에 없다. 같은 줄이 살아 있는 이름을
      함께 적고 있으면 통과시킨다. 홀로 있는 미정의 이름은 그대로 걸린다(음성시험 확인)

신설  tools/verify-rs-contract.js
      expected-contracts.json 의 Result Set 컬럼을 **기준선 05 의 표**와 직접 대조한다.
      06 §44.8 결함(기대값이 구현과 같이 틀렸다)의 축을 아무도 보지 않고 있었다.
      실행 결과 170개 Result Set 전건 일치 ― 그 결함은 이미 고쳐져 있었다
```

## 12. 실행 증거

```text
2026-09-08 04:37  창 밖 회귀   exit 1(V11 만) · PASS 184 · FAIL 1 · SKIP 78 · NOT RUN 9
                  V11 을 고친 뒤 verify-docs PASS 21 / FAIL 0
                  R3 창 밖 기준선은 PASS 185 · FAIL 0 · SKIP 78 · NOT RUN 9 -> 동등
2026-09-08 04:41  verify-baseline 6/6 · verify-rs-contract 170/0 · r4-rename check FAIL 0
2026-09-08 04:45  csharp-probe CS-001~016 전건 PASS

창 안 회귀        NOT RUN ― 업무시간(월~토 11:10~15:50) 밖이다. 사람이 시계를 옮기거나
                  업무시간에 ./scripts/test.sh 를 한 번 더 돌려야 한다 (CLAUDE.md §10)
```

## 13. C# 호출 사전 검수 ― `③` 단계

WinForms 에 DB 식별자 참조가 0건이라 검수할 코드가 없다. 그래서 **실행 증거**를 만들었다.

```text
tools/csharp-probe/Probe.cs        UTF-8 with BOM. csc.exe 로 실제 컴파일한다
scripts/verify-csharp-call.sh      csc 가 없으면 NOT RUN(exit 3) ― SKIP 을 PASS 로 쓰지 않는다
```

| ID | 확인한 것 | 결과 |
|---|---|---|
| CS-001·003·013 | Result Set 컬럼 이름이 계약과 문자열까지 같다 | PASS |
| CS-002·004 | 이름으로 읽는다(`reader["성공여부"]`). 순서에 기대지 않는다 | PASS |
| CS-005·006 | 한글 `SqlParameter("@차트번호", …)` 로 값이 전달된다 | PASS |
| CS-007·008 | `최종수정일시`→`DateTime`, `B형간염제외여부`→`Boolean` | PASS |
| CS-009 | `행버전` → `byte[8]` (`05` §16.3) | PASS |
| CS-010·011 | 실패 경로의 `오류항목` 이 한글 Parameter 이름을 담는다 | PASS |
| CS-012 | `Parameters` 추가 순서를 뒤섞어도 **이름으로** 바인딩된다 | PASS |
| CS-014·015·016 | `sys.parameters` 99건 전건 · 한글 없는 Parameter 0 · 한글 없는 계약 SP 0 | PASS |

`[!]` **`@B형간염제외여부` 는 ASCII `B` 로 시작한다.** 초안의 `LIKE '@[가-힣]%'` 는 97 을 세어
FAIL 했다. 계약은 "첫 글자가 한글" 이 아니라 **"한글을 담는다"** 이다 — `CS-015` 가 그 형태다.

## 14. 산출물 ― `④` 단계

`build_04.js`·`build_05.js` 를 신설했다(사용자 승인). **내용을 하드코딩하지 않는다.**

```text
tools/docgen/xlsx/md.js         기준선 마크다운의 표·코드펜스를 뽑는 파서 + 공통 서식
tools/docgen/xlsx/build_04.js   04_검진_예약접수_DB설계서.xlsx     시트 5
tools/docgen/xlsx/build_05.js   05_검진_예약접수_SP계약서.xlsx     시트 5
```

`database/CLAUDE.md` §3 이 적은 "기준선을 고쳤다고 산출물이 따라오지 않는다" 가
`build_00.js`·`proc/slides`·`wireframe/screens` 의 하드코딩 때문이다. 04·05 는 원본을 읽으므로
그 어긋남이 원리적으로 생기지 않는다. 두 생성기는 마지막에 행 수를 기준선 선언값과 대조한다.

| 산출물 | 시트 | 실측 |
|---|---|---|
| `04_…DB설계서.xlsx` | 테이블 6 · 컬럼 48 · 제약 43 · 인덱스 8 · 명명규칙 12 | FAIL 0 |
| `05_…SP계약서.xlsx` | SP 16 · Parameter 118(SP 99 + TVF 19) · Result Set 238 · ResultCode 38 · TVF 51 | FAIL 0 |

`Result Set 238` 은 `RS0 16×5=80` + `RS1~RS5 158` 이다. 40개 (SP, RS) 조합 전부가 표에 있다.

`docs/baseline/output/` 여섯 종을 2026-09-08 에 전부 다시 생성했다. 이 디렉터리는
ROOT `.gitignore` 대상이라 **어떤 커밋에도 남지 않는다** — 재현 수단은 `tools/docgen` 뿐이다.

`[!]` `build_00.js` 의 자체 검증이 금지 문자열 `"최종 판정"` 1건을 낸다. **R4 이전부터 있던 것이고**
`00_Project_Policy.md` 는 R4 에서 한 바이트도 열지 않았다. 이 계열의 책임 밖이라 그대로 둔다.

## 15. 남은 일

```text
[ ] 창 안 회귀 1회 (업무시간 월~토 11:10~15:50) ― 이것만 남았다
```
