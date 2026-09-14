# 단위시험 정리와 기준선 시험계약 대조 (2026-09-15)

세 가지를 답한다.

```text
1  docs/baseline/ 이 「무엇을 검증할지」 정해 둔 것은 무엇인가
2  우리가 winforms 에서 단위시험한 것은 무엇인가
3  둘이 어디서 만나고, 어디서 만나지 않으며, 만나지 않는 것이 결함인가 아닌가
```

`[!]` **건수를 이 문서에 적지 않는다.** 앞선 `2026-09-14-Test-Scenarios.md` 가 「단위시험 277건」
이라 적었고 그 수는 하루 만에 썩었다. 세는 곳은 `vstest.console.exe` 하나다 (ROOT `AGENTS.md` §6).
다시 세는 법은 §7 에 있다.

---

## 1. 기준선이 정해 둔 시험 — **전부 DB 계층이다**

`docs/baseline/` 에서 「무엇을 검증할지」를 정하는 자리는 둘뿐이고, 둘 다 Stored Procedure·TVF·
Table 을 대상으로 한다.

| 자리 | 무엇을 정하는가 |
|---|---|
| `05` §17 적대적 테스트 계약 | 무엇을 적대적으로 찔러야 하는가 — 9절 (§17.1 시간경계 ~ §17.9 Result Set) |
| `06` §33~§40 Test Architecture | 그것을 어느 파일에서 어떻게 재는가 — 계층·Assertion 패턴·업무일 분기 |
| `06` §45.2 Test ID 카탈로그 | **Test ID 의 단일 출처.** `SCH`·`SED`·`SSN`·`RUL`·`SEL`·`PWR`·`RWR`·`CWR`·`RBK`·`CON`·`SEC`·`RED`·`OFF`·`VER`·`RBD` |
| `06` §33.4 | `05` §17 중 Test ID 가 없던 시나리오를 신설해 메운 표 |

`[!]` **기준선에는 화면·Presenter·Service 계층의 시험계약이 없다.** 우연이 아니라 명시다 —
`06` 머리말 「변경 통제」가 자기 범위를 *Transaction·잠금·권한·Seed·테스트·배포의 구현 상세* 로
못 박고, `06` §3 Out of Scope 가 **`WinForms Source 수정`·`C# DTO·Enum·Repository 구현`을 Phase 5
범위로 내보낸다.** 그래서 기준선 시험 ↔ winforms 단위시험은 **1:1 대응이 아니라 층이 다른
분업**이다. 대응표를 만들려 들면 없는 계약을 지어내게 된다.

마지막 DB 회귀는 회차 **R19**(`exit 0 · PASS 411 · FAIL 0 · SKIP 0 · NOT RUN 1`)이고 그 기록은
`docs/phase4/reseal-history.md` 와 `06` 머리말이 갖는다. **이 세션에서 다시 돌리지 않았다** —
`database/scripts/test.sh` 는 `DROP DATABASE` 를 하고, 계약·배포본은 동결이다
(`winforms/AGENTS.md`).

---

## 2. 우리가 단위시험한 것 — `winforms/tests/`

기준선이 비워 둔 층이 전부 여기다. 파일 하나가 화면 하나 또는 규칙 하나를 맡는다.

`[!]` **시험 하나하나의 내용은 이 문서가 갖지 않는다.**
`docs/phase5/output/P5_단위시험_목록.xlsx` 가 그 자리이고, 그것은 손으로 쓰는 것이 아니라
`winforms/tools/build-test-inventory.js` 가 세 곳에서 읽어 찍는다.

```text
시험 항목   메서드 이름의 `_` 를 띄어 문장으로 읽는다
대상        무엇을 테스트하는가          ┐
목적        왜 이 시험이 있는가          ├ [TestMethod] 위 주석의 세 머리말. 단일 출처는 코드다
확인 내용   무엇이 참이어야 하는가       ┘
근거        그 주석 안의 `05 §9.12` 같은 참조
결과        TestResults/*.trx 의 outcome · 케이스 수 · ms
```

`[!]` **셋은 서로 다른 질문이다** (2026-09-15 사용자 지시). 한 칸에 뭉쳐 두면 「동작을 다시
적은 문장」이 되고, 읽는 사람은 시험 이름에서 얻지 못한 것을 거기서도 얻지 못한다 — 초판이
그랬다. 세 머리말 중 하나라도 비면 `TI-001` 이 red 를 낸다.

같은 값을 두 곳에 두지 않으므로(ROOT `AGENTS.md` §6) 아래 표는 **파일이 무엇을 맡는가**만
적는다. 문구를 고칠 일이 생기면 주석을 고쳐 다시 찍는다 — 엑셀을 손으로 열지 않는다
(§3 과 같은 규칙).

### 2.1 Common — 값의 표기와 정규화

| 파일 | 재는 것 | 기준 |
|---|---|---|
| `clsPatientTextTests` | 전화번호 자리수별 하이픈, 주민번호 정규화, 생년월일·성별·주소 합성 표기 | `03` §5.6 · `05` §2.2 |
| `clsWorkTextTests` | 상태코드 넷의 화면 표시명, 정원 잔여 표기 | 게이트가 없는 자리를 시험이 대신 지킨다 |

### 2.2 Services — 업무 계층의 **성패 규약**

`[!]` 이 층의 시험은 **판정을 재지 않는다.** 정원·마감·TGT·AEX·행버전은 전부 SP 가 내는 것이고,
Service 가 그것을 **미리 접지 않는지**를 본다. R12 가 되돌린 형태가 정확히 그 반대였다.

| 파일 | 재는 것 |
|---|---|
| `PatientServiceTests` | 수검자 Write 의 정규화와 길이 검증 (`05` §10.1·§10.2 · `07` §7) |
| `ReservationServiceTests` | 예약 가능정보·예약 저장의 성패 규약 (`05` §9·§11.1) |
| `WorkServiceTests` | 접수완료·접수취소가 DB 판정을 가로채지 않는지 (`05` §12.1·§12.3) |
| `ChangeLogServiceTests` | **0건이 성공이다** — `200` 으로 바꾸지 않는다 (`05` §8.3) |
| `CommonStatusServiceTests` | 공통업무상태 조회 |

### 2.3 Presenters — 진행 단계와 배선

화면 계층에서 가장 두꺼운 자리다. 재는 것은 **DB 가 준 값을 접거나 다시 세지 않는가**이다.

| 파일 | 화면 |
|---|---|
| `ReservationPresenterTests` | WF-RSV-01 신규 예약 (`03` §8) |
| `WorkbenchPresenterTests` | WF-WRK-01 예약/접수 공통 Workbench |
| `PatientManagementPresenterTests` | WF-PAT-01 수검자 관리 — `[R21]` 상세는 목록 RS1 이 준 행이다 |
| `PatientEditorPresenterTests` | DLG-PAT-01 — 결과코드 `2`·`202`·`203` 분기 |
| `ReceptionPresenterTests` | DLG-RCP-01 — 접수 가능 여부는 RS4 `START_RECEPTION` 이 갖는다 |
| `ExtraExamPresenterTests` | DLG-RCP-02 — R18 이 연 RS5 가 서게 한 화면 |
| `HolidayPresenterTests` | DLG-HOL-01 — 임계 숫자를 화면이 갖지 않는다 |
| `ChangeLogPresenterTests` | DLG-LOG-01 |
| `MainPresenterTests` | 상단 Navigation 은 전부 「가는 곳」이다 |

### 2.4 Views — Presenter 시험이 **못 보는 것**만

| 파일 | 재는 것 | 왜 여기 있나 |
|---|---|---|
| `LayoutBaselineTests` | 모든 화면이 `LayoutControl` 로 배치하는가 | 문장으로 두면 절대좌표 화면이 들어와도 아무도 모른다 |
| `ScreenInitializationTests` | 화면이 열리면 스스로 조회하는가 | 실제 결함에서 나왔다 (`UcHoliday` 만 `OnLoad` 가 없었다) |
| `ActionLockTests` | 핸들러가 도는 동안 그 버튼이 잠기는가 | 실제 결함 — 여섯 경로가 잠글 컨트롤 없이 `clsBusyScope` 를 들고 있었다 |
| `clsBusyScopeTests` | 잠금 되돌리기가 Presenter 가 정한 `Enabled` 를 덮지 않는가 | 실제 결함 (2026-09-14 코드리뷰) |
| `clsActionRunnerTests` | 조회 재진입 가드 | 같은 18줄이 두 화면에 복사돼 있었고 아무 시험도 없었다 |
| `FrmPatientEditorTests` | `오류항목` 이 어느 입력칸을 가리키는가 (`05` §16.2) | Presenter 시험이 보지 못한다 |
| `FrmReservationTests` | `03` §8.10 폐기 확인 · 캡처 회귀가 사람 손을 부르지 않게 | |
| `UcPatientManagementTests` · `UcWorkbenchTests` | Grid 바인딩·조회조건·컬럼 | |
| `MainFormTests` | Ribbon Page 가 전부 「가는 곳」인가 | |

### 2.5 Visual · Integration — 단위시험이 아닌 이웃

| 파일 | 성격 |
|---|---|
| `ShellCaptureTests` | 화면을 PNG 로 떠 배치를 눈으로 본다. 판정이 아니라 증거다 |
| `PatientRepositoryDbTests` · `SelectRepositoryDbTests` | **실물 DB** 에 붙는다. 컬럼 이름 오타는 fake 시험을 통과하고 실행할 때만 터진다 |
| `ScenarioDbTests` | 과제 브리프 §3·§4 를 실물 DB 로 끝까지 밟는다 — `S01`~`S07` (`2026-09-14-Test-Scenarios.md`) |

---

## 3. 대조 — `05` §17 항목별로 **누가 재는가**

| `05` §17 | DB 계층 (`06` §45.2) | winforms 계층 | 겹치는가 |
|---|---|---|---|
| §17.1 시간 경계 | `RUL-T01`~`T12` — TVF 에 `@서버시각` 주입, 결정적 | 없음. 화면은 마감을 계산하지 않는다 | **아니다 — 의도한 것** |
| §17.2 일정 (과거·일요일·휴무일·토요일) | `RUL-D01`~`D09` | 없음 | 아니다 |
| §17.3 TGT | `RUL-G01`~`G08` | `ReservationPresenterTests` 는 **문구만** 만든다 (`03` §8.7) | 판정은 DB, 표기는 화면 |
| §17.4 NEX | `RUL-N01`~`N12` | 없음 | 아니다 |
| §17.5 AEX | `RUL-A01`~`A10` | `ExtraExamPresenterTests` 는 RS5 가 준 가용성을 **다시 세지 않는지** | 판정은 DB |
| §17.6 수검자 | `PWR-*` | `clsPatientTextTests`(정규화·하이픈 제거) · `PatientServiceTests`(길이) · `PatientEditorPresenterTests`(`2`·`202`·`203` 분기) · `FrmPatientEditorTests`(`오류항목`) | **여기가 가장 많이 겹친다** |
| §17.7 예약 | `RWR-*` | `ReservationPresenterTests` · `ReservationServiceTests` — 접지 않는지만 | 판정은 DB |
| §17.8 접수 | `CWR-*` | `ReceptionPresenterTests` · `WorkServiceTests` — 막지 않는지만 | 판정은 DB |
| §17.9 Result Set | `tests/contract/*` + `tools/verify-contract.js` | `SelectRepositoryDbTests` · `PatientRepositoryDbTests` — 계약이 아니라 **실제로 그 이름으로 오는지** | 대상이 다르다 |

읽는 법 한 줄: **기준선 §17 은 「DB 가 옳은 답을 내는가」를 재고, winforms 단위시험은 「화면이
그 답을 그대로 쓰는가」를 잰다.** 같은 것을 두 번 재는 자리가 §17.6 하나인 이유도 그것이다 —
정규화와 길이는 계약상 C# 이 먼저 하는 일이라 양쪽에 다 있다 (`05` §2.2 · `07` §7).

### 3.1 기준선에 대응이 **없는** winforms 시험

`03` 이 구현을 구속하지 않게 된 뒤(ROOT `AGENTS.md` §1.1) 화면 규칙을 지킬 것이 시험밖에 남지
않았다. 아래는 전부 그 자리에서 생긴 것이고, 기준선에 대응 항목이 없는 것이 정상이다.

```text
LayoutControl 배치       ScreenInitialization      ActionLock / BusyScope
clsActionRunner 재진입   MainForm Ribbon 이동      ShellCapture 캡처
```

절반 이상이 **실제 결함에서 역으로 만들어진 시험**이다 — 각 파일 머리말이 그 결함을 적는다.

### 3.2 기준선에 있는데 winforms 에 대응이 **없는** 것

| 기준선 | 왜 없는가 |
|---|---|
| `CON-001`~`008` 동시성 | 2세션 경합은 DB 층의 일이다. 화면은 `행버전` 충돌 결과코드를 받는 쪽이고 그 분기는 Presenter 시험에 있다 |
| `SCH`·`SED`·`SSN`·`RBD`·`VER`·`RED` | 스키마·Seed·배포 재현성. 화면과 무관하다 |
| `RBK` 부분저장 차단 | Transaction 경계는 SP 안이다 |

**빈 자리가 아니라 경계다.** 여기에 winforms 시험을 만들면 판정이 두 곳이 된다.

---

## 4. 이 세션에서 잰 것과 재지 않은 것

| | 결과 |
|---|---|
| winforms 시험 **전건** (실물 DB 포함) | **PASS · FAIL 0 · 미실행 0.** 건수는 엑셀 요약 시트가 갖는다 |
| 마감시각 AM·PM 실측 | **8건 PASS** — 예약·접수 × AM·PM × 마감 전/후 (§4.1) |
| `winforms/scripts/test.sh` | PASS |
| `database/scripts/test.sh` | 돌리지 않았다 — 계약 동결이고 `DROP DATABASE` 를 한다 (§1) |

### 4.1 운영기준을 열어 두고 잰다 — `OPR-G4` 가 red 인 이유

**2026-09-15 사용자 결정: 실물 `운영기준` 의 운영시간을 `00:00:00`~`23:59:00` 으로 열어
두고 통합테스트까지 유지한다.** 마감 넷은 `00` 값 그대로다.

```text
verify-live-sync.sh (G17)   PASS   모듈(SP·TVF) 본문만 본다 — 데이터 변경은 대상이 아니다
OPR-G1 · G2 · G3            PASS   00 ↔ 02_Seed.sql 은 그대로다
OPR-G4                      FAIL   실물 [운영기준] 이 00 과 다르다
```

`[!]` **`OPR-G4` 의 red 는 결함이 아니라 표시다.** 그 게이트는 *시험이 창을 넓혔다
되돌리지 않았다* 를 잡으라고 만든 것이고(`06` §43-14 — 되돌리지 못한 회차는 다음 회차를
다른 제품의 시험으로 만든다), 지금이 정확히 그 「넓혀 둔 상태」다. **통합테스트가 끝나면
`00` 값으로 되돌리고 이 줄을 지운다.**

마감시각은 다르다 — **넓혔다 그 자리에서 되돌린다.** `scripts/measure-cutoff.sh` 가 시계가
아니라 `운영기준` 의 마감 넷을 옮겨 여덟 경계를 재고 마지막에 복원한다.

```text
1  예약 AM 마감 전    마감경과여부 0                PASS
2  예약 AM 마감 후    마감경과여부 1 · 업무가능 0    PASS
3  예약 PM 마감 전    마감경과여부 0                PASS
4  예약 PM 마감 후    마감경과여부 1 · 업무가능 0    PASS
5  접수 AM 마감 후    접수 뒤 상태 RSV              PASS
6  접수 AM 마감 전    접수 뒤 상태 RCP              PASS
7  접수 PM 마감 후    접수 뒤 상태 RSV              PASS
8  접수 PM 마감 전    접수 뒤 상태 RCP              PASS
```

접수 경계 둘은 **오늘자 `D%` RSV 예약**을 대상으로 삼는데 그 행이 없어 처음에는
「대상 예약 없음 · 못 잼」이었다. `scripts/seed-probe-works.sh` 를 새로 두어 AM·PM 한 건씩
세운다 — 예약 SP 로 만들므로 `검사구성`(NEX·AEX)이 계약대로 함께 서고, 그래야 접수완료가
`701`/NEX 0행으로 막히지 않아 마감을 잴 수 있다.

---

## 5. 이 세션이 잡은 결함 — **하루짜리 지뢰**

첫 실행에서 하나가 빨강이었다.

```text
ReservationPresenterTests.저장_버튼은_DB_의_저장가능_그대로다
  Assert.IsFalse 실패 — ReservationPresenterTests.cs:506
```

원인은 제품이 아니라 시험이다.

```text
FakeReservationView 의 기본 예약일    DateTime.Today
시험이 바꾸려 한 날                    Day.AddDays(1)   (Day = 2026-09-14 고정 상수)
2026-09-15 에는 이 둘이 같은 날이다
```

`ReservationPresenter.Ask` 는 `_askedDate` 와 같은 날이면 되돌아간다. 그래서 그날 하루만
**조회가 일어나지 않고** `SaveEnabled` 가 진입 때 값 `true` 에 머물렀다. 시험이 쓴 날짜 상수가
시계를 따라잡은 것이다.

고친 것은 한 줄이다 — **특정 날짜가 아니라 「물어본 날과 다른 날」이면 된다.**

```csharp
view.ReserveDate = view.ReserveDate.AddDays(1);
```

`[!]` 같은 모양이 더 있는지 확인했다. `ReserveDate` 대입 여섯 곳 중 시계에 걸린 것은 이 한 곳
뿐이다. 고친 뒤 재실행에서 `failed="0"`.

---

## 6. 제출용 엑셀 — `docs/phase5/output/P5_단위시험_목록.xlsx`

공개본 6종과 **층이 다르다.** `tools/docgen/verify_output.js` 가 `PASS`·`FAIL`·`실측`·`게이트`
·`회귀` 를 금지 어휘로 잡으므로 시험 결과 엑셀은 `docs/baseline/output/` 에 구조적으로 들어갈
수 없다 — 결과 칸의 단어 자체가 걸린다. 그래서 `docs/phase5/output/` 에 따로 둔다.

```text
시트 1 요약   실행시각 · 결과파일 · 층별 시험/실행 케이스/PASS/FAIL/미실행 시험
시트 2 전건   층 · 화면/영역 · 시험 항목 · 대상 · 목적 · 확인 내용 · 근거 · 케이스 · 결과 · ms · 파일
```

생성기가 스스로 판정한다 — 게이트 스크립트를 따로 두지 않았다.

```text
TI-000  이유 주석이 [TestMethod] **아래**에 놓이면 exit 1 (C# 이 그 자리를 안 읽는다)
TI-001  대상·목적·확인 중 하나라도 빈 [TestMethod] 가 있으면 exit 1   scripts/test.sh 에서 돈다
TI-002  trx 에만 있고 코드에 없는 시험 = 옛 trx 로 찍고 있다           엑셀을 찍을 때
TI-003  trx 보다 나중에 고친 시험 소스가 있으면 exit 1                엑셀을 찍을 때
```

`[!]` **`TI-002`·`TI-003` 을 `scripts/test.sh` 에 두지 않았다.** `TestResults/` 는 `.gitignore`
대상이라 기계마다 있고 없다 — 저 회귀의 「어디서나 같은 판정」을 깬다. 그 둘이 막으려는 것은
*옛 결과로 산출물을 찍는 것* 이고, 그 일은 엑셀을 찍는 자리에서만 일어난다.

```bash
cd winforms
node tools/build-test-inventory.js selftest   # 게이트가 정말 red 를 내는가
node tools/build-test-inventory.js --check    # 대상·목적·확인만 판정. 엑셀을 쓰지 않는다
node tools/build-test-inventory.js            # 판정 + 엑셀 생성
```

---

## 7. 다시 세는 법 · 다시 돌리는 법

```bash
cd winforms
MSB="C:/Program Files (x86)/Microsoft Visual Studio/2019/Professional/MSBuild/Current/Bin/MSBuild.exe"
VST="C:/Program Files (x86)/Microsoft Visual Studio/2019/Professional/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe"
"$MSB" HealthCheckupReservationReception.sln -p:Configuration=Debug -v:m

# 단위시험만 (실물 DB 불필요) — 건수도 판정도 여기가 단일 출처다
"$VST" tests/HealthCheckupReservationReception.Tests/bin/Debug/HealthCheckupReservationReception.Tests.dll \
       /TestCaseFilter:"FullyQualifiedName!~Integration" /Logger:trx

# 실물 DB 계열 — 씨앗을 먼저 심는다 (전부 멱등)
./scripts/seed-tgt-cases.sh      # TGT·NEX 갈래를 밟는 수검자
./scripts/seed-probe-works.sh    # 마감 실측이 쓸 오늘자 AM·PM 예약
"$VST" tests/HealthCheckupReservationReception.Tests/bin/Debug/HealthCheckupReservationReception.Tests.dll \
       /TestCaseFilter:"FullyQualifiedName~Integration" /Logger:trx

# 마감시각 AM·PM 여덟 경계. 마감 넷을 옮겼다 되돌린다 — 복원은 OPR-G4 가 판정한다
./scripts/measure-cutoff.sh
cd ../database && ./scripts/verify-operating-baseline.sh
```

`[!]` 콘솔 요약은 한글이 깨져 나온다. **판정은 `TestResults/*.trx` 의 `<Counters .../>` 로 읽는다** —
깨진 요약에서 `실패: 1` 을 `건너뜀: 1` 로 잘못 읽는 일이 실제로 이 세션에서 한 번 있었다.
