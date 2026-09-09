# Phase 5 이어받기 — 셸과 첫 화면이 선 뒤, 수검자 계열 한 벌

2026-09-09 작성. 앞 인계문서(`2026-09-09-session-11-Phase5-Start.md`)를 **대체한다.**

---

## 0. 무엇이 낡아서 이 문서를 쓰는가

앞 문서는 착수 시점의 판단이고 그때는 옳았다. **고쳐 쓰지 않고 그대로 둔다** — 그 문서 §9 에
무엇이 실제로 들어갔는지만 적어 두었다. 다만 세 대목이 지금 거짓이다.

| 앞 문서 | 왜 낡았나 |
|---|---|
| §6 "이번에 할 일" 네 항목 | 넷 다 끝났다. `07`·`App.config`·winforms 게이트·화면 둘 |
| §8 "빌드해 본 적이 없다 · `licenses.licx` 가 없다" | 솔루션은 MSBuild 로 돌고 `licenses.licx` 는 등재됐다 |
| §8 "어떤 게이트 집합을 회귀로 돌릴지 미정" | `winforms/scripts/test.sh` 가 그것이다 |

**이 문서도 수치를 하나도 적지 않는다.** 전부 도는 것을 가리킨다(루트 `AGENTS.md` §6).
건수·해시·회차를 여기서 읽지 말고 게이트를 돌려서 봐라.

---

## 1. 어디서 여는가 — `winforms/` 다

킷의 `.agents/` 경로가 거기서 풀린다. 저장소 루트에서 열면 킷이 안 들어온다.

```text
쓴다   winforms/**  ·  ../docs/phase5/
읽는다 ../docs/baseline/  ·  ../database/**
```

브랜치는 `phase5-start` 를 이어 쓴다. `origin` 에 올라가 있다.

---

## 2. 지금 참인 것 — 읽지 말고 돌려서 봐라

```bash
cd winforms && ./scripts/test.sh                 # 게이트 여덟 벌 · 전부 check + selftest
"<MSBuild>" HealthCheckupReservationReception.sln -p:Configuration=Debug -v:m
"<vstest>"  tests/…/bin/Debug/HealthCheckupReservationReception.Tests.dll
"<vstest>"  … /TestCaseFilter:"TestCategory=Db"  # SQLEXPRESS 가 있어야 돈다. test.sh 밖이다
cd ../database && ./scripts/verify-baseline.sh && node tools/verify-docs.js
```

MSBuild·vstest 경로는 `contract/build.md` 의 `vswhere` 줄이 찾아 준다.

`[X]` **`test.sh` 를 배경으로 돌리지 마라.** 여러 번 멈췄고, 멈춘 것을 죽이면 `exit 0` 으로
"완료" 보고된다 — `… | tail -N` 의 종료코드는 `tail` 것이다. **출력 0바이트에 exit 0** 이
나온다. 경위는 `07` §12.6, 경고는 `scripts/test.sh` 머리말.

**그리고 판정을 볼 때 `| tail` 로 자르지 마라.** 앞 세션이 그렇게 잘라서 회귀 `FAIL` 하나의
증거를 통째로 잃었다 — 어느 게이트가 왜 떨어졌는지조차 모른다.

```bash
./scripts/test.sh > /tmp/gate.log 2>&1; echo "exit=$?"; grep -n 'FAIL' /tmp/gate.log
```

---

## 3. 무엇이 서 있는가

```text
WF-00       MainForm 셸. RibbonPage 다섯 · 업무 Tab · 상태바 · SP-COM-01 결선
WF-PAT-01   수검자 관리. 조회조건 · Grid · 우측 상세 · SP-PAT-01/02 결선
컬럼설정    FrmColumnChooser. 체크박스 + [기본값 복원] (03 §18). Grid 하나를 받는다
계층        View → Presenter → Service → Repository → SP. 킷 §2 그대로
```

`07_UI_DB_Matrix_Final_Validation.md` 가 화면↔SP 대조와 실행 기록을 갖는다. **§3.2.1 의 표를
먼저 봐라** — 화면 하나가 무엇을 만족해야 끝난 것인지가 거기 있다.

---

## 4. 화면을 만드는 법 — 이 저장소가 비싸게 배운 것

**`03_Wireframe_Definition.md` 는 배치를 담지 않는다.** 규칙과 필드만 적는다.
배치의 원본은 `../tools/docgen/wireframe/screens/<화면ID>.js` 다.

```text
screens/dlg_pat_01.js   DLG-PAT-01 의 배치
screens/dlg_pat_03.js   DLG-PAT-03
screens/dlg_pat_02.js   DLG-PAT-02
kit.js                  공통 부품과 NAV. spec.js 는 규격
```

`[X]` **초판 WF-00 은 `.md` 만 읽고 만들어 설계와 전혀 다르게 나왔다.** 사용자가 pptx 를
짚어 알려 줬다. 그 뒤 `tools/verify-screen-design.js` 가 생겨 `SCR-000`~`004` 로 대조한다 —
생성기를 실제로 돌려 `K.shell`·`K.panel`·`K.field`·`K.grid` 의 인자를 가로챈다. 산출된 pptx
를 역추출하지 않는다.

**C# 파일 머리에 `// 화면 ID: <ID>` 를 단다.** 게이트가 그것으로 설계와 구현을 짝짓는다.

### 배치를 어떻게 옮기는가

```text
구조와 가로 비율   설계 그대로 옮긴다 (좌 0.62 / 우 0.38 같은 것)
세로 실측값        옮기지 않는다 — spec.js 가 "96DPI 실측값이 아님" 이라 적는다.
                   실제 컨트롤 높이를 쓴다 (킷 references/wireframe-layout.md)
GroupControl       캡션 높이가 ClientRectangle 에서 빠지지 않는다. Dock 은 캡션 아래에
                   붙지만 **절대좌표 자식은 테두리부터 센다** — 안쪽 y 를 캡션만큼 내린다
```

---

## 5. 이미 실측했다 — 다시 하지 마라 (2026-09-09)

```text
SP-COM-01          C# 경유로 돈다 (07 §12.1 · §12.3)
SP-PAT-01/02       실물 DB 로 돈다. RS1 컬럼 11건 전건 · 200 실패 경로 (07 §12.5)
                   ReadRows 가 GetOrdinal 을 루프 밖에서 잡아 0건에도 컬럼 계약이 검증된다
배율 100% · 125%   잘림 0건. 사용자 실측 (07 §12.7). 화면이 늘면 다시 봐야 한다
GridView focus     행이 있으면 반드시 하나를 focus 한다. InvalidRowHandle 대입이 대입 직후
                   0 으로 돌아온다 — focus 를 없애는 대신 선택 표시를 끈다 (07 §14.3 A-10)
RibbonControl      Page 헤더 클릭 이벤트가 없다(20.2 XML 실측). 그래서 A-08 을 Action 쪽에서 푼다
DevExpress 고급    UseAdvancedCustomizationForm 은 체크박스 방식이 맞지만 [기본값 복원] 을
Customization Form 넣을 자리가 없다 — 그래서 FrmColumnChooser 를 따로 뒀다 (07 §3.2.2)
```

`[I]` **실행 화면은 시험 안에서 뜬다.** `tests/…/Visual/ShellCaptureTests.cs` 가 `DrawToBitmap`
으로 PNG 를 남긴다(`artifacts/logs/`, `.gitignore` 대상). **VS 디자인 표면은 실행 화면이
아니다** — `Program.cs` 도 Presenter 도 디자인타임에 돌지 않는다. 이 캡처가 시험이 못 보는
결함을 네 번 잡았다.

---

## 6. 이번에 할 일 — 수검자 계열 한 벌

2026-09-09 사용자 결정. **이 셋을 넘지 않는다.**

```text
DLG-PAT-01   수검자 등록·수정 Editor    03 §6.1~6.4 · 05 §10.1(SP-PAT-03) · §10.2(SP-PAT-04)
DLG-PAT-03   중복 후보 확인             03 §6.5. SP-PAT-03 이 203 과 함께 준 RS1 을 그린다
DLG-PAT-02   수검자 선택 Modal          03 §7. 조회계약은 WF-PAT-01 과 같다 (SP-PAT-01)
```

Action↔SP 대조는 **`07` §3.3 · §3.4 가 이미 전건 갖고 있다.** 새로 만들지 말고 그것을 따른다.

끝나면 `WF-PAT-01` 의 Ribbon 스텁이 세 개 닫힌다 — `[신규등록]`·`[정보수정]`·`[신규예약]` 중
앞의 둘. 지금은 `barBtnNotImplemented_ItemClick` 이 "아직 만들지 않았습니다" 를 띄운다.

### 6.1 첫 턴에 사용자에게 물을 것 — `A-09`

**서비스 주입을 어떻게 할지 정해야 한다.** `07` §14.3 `A-09` 가 *"셋째 화면에서 다시 본다"*
고 적었고 **그 셋째 화면이 바로 이것**이다. 지금은 `MainForm` 생성자가 서비스를 받는데 쓰기
서비스가 붙으면 또 는다. 킷 §2 가 *"같은 모양이 구체 화면 둘에 생기기 전에는 추상을 만들지
않는다"* 고 하고, 이제 둘이 된다. **앞 세션이 일부러 정하지 않고 넘겼다.**

### 6.2 이 계열이 처음 만나는 것

```text
쓰기 SP        지금까지 전부 조회였다. 트랜잭션·감사·변경이력이 여기서 처음 걸린다
RowVersion     수검자 동시성 토큰. PatientDetailDto.RowVersion 이 이미 담고 있다
               R11 이 01 §P01-04 · 03 §6.4 · §16 을 여기에 맞췄다 (07 §14.1)
202 · 203      실패인데 후속 Result Set 을 읽는 유일한 예외다 (05 §16.4)
확인값         성명 + 산출 생년월일 + 주민번호 조합 하나뿐. 셋 중 하나가 바뀌면 C# 이 0 으로
               되돌린다 (05 §10.1 · 03 §6.5)
SP-PAT-02 RS1  열다섯 컬럼이 아직 실행으로 안 재졌다 — 수검자 한 행이 있어야 한다.
               SP-PAT-03 이 생기는 순간 닫을 수 있다 (07 §12.5)
```

---

## 7. 절대 어기면 안 되는 것

```text
docs/baseline/ 는 봉인이다. 여는 것은 루트 AGENTS.md §2 의 재봉인이다
계약이 틀려 보이면 고치지 말고 멈추고 사용자에게 보고한다      winforms/AGENTS.md
실행하지 않은 검증을 PASS 로 적지 않는다                       database/AGENTS.md §10
같은 값을 두 곳에 두지 않는다 — 적고 싶으면 재는 게이트를 함께 만든다   루트 AGENTS.md §6
winforms 를 바꾼 커밋에서 manifest 를 함께 갱신한다             winforms/AGENTS.md
  cd ../database && ./scripts/verify-winforms-unchanged.sh init
05 가 SP 계약에서 킷을 이긴다                                   winforms/AGENTS.md
줄바꿈: .cs 와 csproj 는 UTF-8 BOM + CRLF, 나머지는 LF          winforms/.editorconfig · UIB-005
push · PR · 봉인 열기는 사용자 승인을 따로 받는다
```

`[X]` **재봉인이 필요해 보이면 그 자리에서 멈추고 물어라.** 앞 세션이 `X-02` 를 "구현이
막히지 않으니 기록만" 으로 닫았다가 두 회차 뒤에 다시 열어야 했다. 얕게 읽고 넘긴 값이다.

---

## 8. 확인하지 않은 것

```text
verify-screen-design.js 가 왜 간헐적으로 멈추는지 — 경계만 뒀다 (07 §12.6).
  파지 않기로 했다(2026-09-09 사용자 결정). 다시 나면 증거를 §12.6 에 보태라
SP-PAT-03/04 를 C# 으로 불러 본 적이 없다 — 쓰기 SP 는 전부 미결선이다
수검자 테이블이 0행이다. 02_Seed.sql 은 수검자를 넣지 않는다(확인함)
DLG-PAT-01 을 실물 DB 로 돌리면 데이터가 남는다 — 정리 방법을 정하지 않았다.
  03 §5.1 이 삭제를 제공하지 않는다
남은 화면 아홉(WF-RSV-01 · WF-WRK-01 · DLG-RSV-01 · DLG-RCP-01/02 · CNF 둘 ·
  DLG-LOG-01 · DLG-HOL-01)은 계약만 있고 구현이 없다
```
