# Phase 5 인계 — UI 대개편 1구간 완료, 예약·접수로 넘어간다

**여는 한 줄** — 새 세션은 이것만 던지면 된다.

```text
docs/phase5/2026-09-10-session-17-Phase5-UI-Overhaul.md 를 읽고 §6 순서대로 이어서 한다.
```

이 문서 하나가 인계다. 별도 프롬프트 파일을 두지 않는다 — 두 벌이면 한쪽만 고쳤을 때
어긋난다(ROOT `AGENTS.md` §6).

---

## 0. 앞 문서에서 무엇이 낡았나

`2026-09-10-session-16-Phase5-R13-done.md` 는 DB 계열 R13 까지의 기록이고 UI 는 다루지
않는다. 그 뒤로 사용자 결정 둘이 났고 그것이 이 저장소의 작업 방식을 바꿨다.

`07_UI_DB_Matrix_Final_Validation.md` §3.1.2 의 「구현 현황」 표는 **이제 거짓이다** —
`XtraTabControl · 탭마다 × 닫기 ✅`, `Tab ↔ Ribbon Page 동기 ✅` 가 §2 에서 걷혔다.
고치지 않는다: `winforms/AGENTS.md` 가 `docs/phase5/` 를 갱신 의무 없는 기록으로 못박았다.
**`07` 을 현황의 출처로 읽지 마라.**

---

## 1. 무엇이 결정되었나 — 2026-09-10 사용자 결정

ROOT `AGENTS.md` §1.1 이 본문이다. 여기 베끼지 않는다. 요지만:

```text
03 → C# Source     더 이상 배치를 구속하지 않는다. 화면은 UX 판단으로 만든다
00 · 01 · 02       그대로 상위다 — 바뀌는 것은 "어떻게 보이는가" 뿐이다
04 · 05 · 06       DB 계약 동결. 이 결정의 대상이 아니다
```

커밋 `f517db2` 가 그 문단이다.

---

## 2. UI 대개편 1구간이 무엇을 바꿨나 (커밋 `1a1bf2d`)

```text
배치        절대좌표 → LayoutControl. SplitContainerControl · GroupControl 셋 ·
            LabelControl 열다섯 · 절대좌표 서른일곱 자리가 사라졌다
조회조건    차트번호 · 이름 · 주민번호 셋. [조회 조건] 드롭다운이 어느 칸을 낼지 정한다
컬럼설정    Ribbon [보기] 의 모달(FrmColumnChooser 삭제) → Grid 옆 드롭다운
Shell       XtraTabControl(tabBusiness) 제거 → PanelControl 하나. 다중 폼을 열지 않는다
            Ribbon [검색] 그룹의 [조회] 제거 — 같은 버튼이 두 곳에 있을 이유가 없다
Grid        컬럼마다 MinWidth. 가로 스크롤이 서는 유일한 지렛대다
```

### 2.1 실측으로만 알 수 있었던 것 넷

이것들은 문서에 없고 재 봐야 나온다. 같은 자리를 다시 밟지 마라.

```text
CheckedComboBoxEdit 을 못 쓴다
  QueryDisplayText 가 그 클래스에서 "not supported" 다 — 표시글을 고정할 수 없다
  → PopupContainerEdit + PopupContainerControl + CheckedListBoxControl 로 간다

RepositoryItem 의 CheckState 는 사용자 체크를 따라오지 않는다
  Designer 가 적어 둔 처음 값일 뿐이다. 지금 켜진 것은 체크 목록 컨트롤에만 있다

SimpleButton.PerformClick() 은 안 뜬 폼의 버튼에서 아무 일도 안 한다
  시험에서 클릭을 흉내 내려면 Designer 가 이름으로 잇는 핸들러를 직접 불러야 한다

가로 스크롤은 컬럼 폭으로 서지 않는다
  OptionsView.ColumnAutoWidth 기본값이 true 라 컬럼을 뷰 폭에 욱여넣는다
  (실측: 컬럼폭합 3000 · 뷰폭 1140 인데 가로 스크롤 숨김)
  → auto 를 끄면 컬럼이 적을 때 빈 공간이 남는다. 켠 채로 컬럼마다 MinWidth 를 준다
```

### 2.2 LayoutControl 은 "어떤 배율이든 같은 x,y" 가 아니다

```text
                  100%          150%
lci Min/MaxSize   190×26   →    285×39      정확히 ×1.5
lci Bounds        X=110    →    X=165       정확히 ×1.5
```

**픽셀은 배율에 정비례해 커진다.** 고정되는 것은 논리 좌표(DIP)와 배치 관계다. 못 박은
`MinSize`/`MaxSize` 도 함께 커지므로 고배율에서 잘리지 않는다 — 절대좌표였으면 잘렸을
자리다. **150% 모니터 실측 완료(2026-09-10 사용자) — 폰트까지 정상이다.**

---

## 3. 지금 참인 것 — 읽지 말고 돌려서 봐라

```text
cd winforms
./scripts/test.sh            red 는 verify-screen-design.js 하나여야 한다
                             SCR-003 · SCR-004 가 §1.1 이 놓아 준 그 두 가지다
                             다른 게 red 면 진짜 결함이다 (winforms/AGENTS.md)
MSBuild + vstest             108/108 · warning 0
```

**SP 21개가 전부 배포돼 있다.** `SP-LOG-01` 포함. 남은 것은 전부 C# 이다 — DB 의존 0.

C# 이 부르는 SP 는 다섯뿐이다: `COM-01` · `PAT-01` · `PAT-02` · `PAT-03` · `PAT-04`.

---

## 4. 다음 구간의 설계 — grilling 으로 확정 (2026-09-10)

### 4.1 예약 진입점은 셋, 화면은 하나

```text
1. 리본 [신규 예약] Page       → BeginNewReservation(Normal, null,      Nav)
2. 수검자 관리 [신규예약]       → BeginNewReservation(Normal, PatientId, PAT)
3. 접수 관리 [현장 당일예약]    → 수검자 선택 → BeginNewReservation(WalkIn, PatientId, RCP)

되돌아오는 길 둘
4. 신규예약 중 기존 유효예약   → OpenWorkbench(Reservation, WorkId)
5. 신규예약 저장 성공          → OpenWorkbench(Reservation, WorkId) 자동선택
   단, WalkIn 은 Reception 이다 (03 §9.8 · §21.6)
```

`03` §3 의 호출계약 두 개를 **이름 그대로** `IMainView` 에 앉힌다. 관리할 것은 탭이 아니라
이 두 함수다.

`Source` 는 죽은 파라미터가 아니다 — `03` §8.10 의 폐기 트리거 「다른 Flow가 같은 화면을
재사용」이 같은 Flow 재진입과 갈리려면 필요하다. Normal/WalkIn 복귀 차이는 `Context` 가
가른다.

### 4.2 `[!]` 1구간에서 지운 것 중 되살려야 할 것

```text
_workbenchContext · NavigationOf 를 "Caption 에만 쓰인다" 고 읽고 지웠다.
Caption 은 탭이 없어졌으니 정말 필요 없다. 그러나 그 값이 나르던 Context 는 살아 있어야
한다 — 03 §9.6(예약)/§9.7(접수) 이 같은 Workbench 화면에 다른 Ribbon Action 을 요구한다.
→ OpenWorkbench(WorkContext, WorkId?) 가 그것을 나른다.
```

### 4.3 미저장 신규예약이 보이지 않는 문제

탭을 걷어서 생겼다. `MainForm._screens` 가 화면 인스턴스를 살려 두므로(조회해 둔 목록이
날아가지 않게 한 것) 미저장 입력은 실제로 살아 있는데 화면에 안 보인다.

`03` §8.10 폐기 Confirm 계약은 그대로 두고, 탭 헤더가 하던 시각 단서만 **상태바 한 칸**으로
되돌린다. 문구·위치는 구현자가 정하고 캡처로 보고한다.

### 4.4 WalkIn 은 `WF-RSV-01` 을 그대로 빌려 쓴다

`03` §9.8 계약이고, 정원·TGT·NEX·AEX 규칙이 Normal 과 같다. 두 번 구현하면 규칙이 두 곳에
생긴다(ROOT §6). 다른 것은 `Context` 하나와 복귀 지점뿐이다.

### 4.5 배치의 근거

`03` 을 출발점으로 읽되 배치는 UX 판단으로 만들고 캡처로 보고한다. **`03` §8.5~8.9 의
정원·TGT·NEX·AEX 는 배치가 아니라 업무 규칙이라 그대로 구속력이 있다.** §1.1 이 놓아 준
것은 "어떻게 보이는가" 뿐이다.

---

## 5. 무엇을 기록하지 않기로 했나

**조회조건에서 생년월일·휴대전화를 뺀 것은 `01` P01-01 · `02` F-PAT-001 과 어긋난 상태다.**
§1.1 이 면제한 것은 `03` 뿐이므로 이것은 상위 문서와의 이탈이다.

사용자 결정으로 **문서를 고치지 않는다.** 기록은 `IPatientManagementView.cs` 상단 주석
하나뿐이고, `01`·`02` 만 읽는 사람은 모르는 채로 남는다. 알고 받아들인 것이다.

`SP-PAT-01` 은 두 조건을 여전히 받고 DB 가 검증까지 한다 — 되살리려면 드롭다운 항목 둘과
입력칸 둘이면 된다.

---

## 6. 다음에 할 일 — 이 순서대로

```text
0. (사용자만 가능) VS 디자이너로 UcPatientManagement · MainForm 을 열어 저장한다
   → 손으로 쓴 Designer.cs 가 전면 재직렬화된다. 기계적이고 피할 수 없다
   → 그 재직렬화만 따로 커밋한다. 나중에 열면 diff 가 네 배가 되고 기능 변경이 묻힌다
   → 이때 생기는 licenses.licx · FrmXxx.resx 의 csproj 등록을 확인한다

1. IMainView 에 BeginNewReservation / OpenWorkbench 두 메서드
   + Workbench Context 복구 (§4.2)

2. WF-WRK-01 조회 골격
   구조가 수검자 관리와 같다(조회조건 + Grid + 상세 + Ribbon).
   1구간의 LayoutControl 패턴이 진짜 재사용되는지 여기서 드러난다

3. WF-RSV-01 Normal
   정원 · TGT · NEX · AEX · 2단계 저장. 새 개념이 전부 여기 몰려 있다
   §8.11 저장 성공 → Workbench 자동선택까지 닫는다

4. DLG-LOG-01 변경이력
   §23.2 의 진입점 둘(WF-PAT-01 · WF-WRK-01)이 이 시점에 다 서 있다 —
   모달 하나를 두 곳에 한 번에 붙인다. SP-LOG-01 은 이미 배포돼 있다

5. 상태바 미저장 표시 (§4.3)
```

그다음 구간: WalkIn · `DLG-RCP-01`/`02` 접수 · `DLG-RSV-01` 예약변경 · `CNF` 둘 ·
`DLG-HOL-01` 휴무일.

### 6.1 코드를 쓰기 전에

`verify-rs-columns.sh` 가 다시 돈다(2026-09-10 되살림). 예약·접수 Repository 는 `GetOrdinal`
문자열이 수십 개다 — **단위시험이 못 보는 유일한 고리이고 실행할 때만 터진다.** 회귀를
돌리면서 쓴다.

---

## 7. 절대 어기면 안 되는 것

```text
docs/baseline/ · database/deploy/ 를 열지 않는다   winforms/AGENTS.md — DBF-001 · DBF-002
같은 값을 두 곳에 두지 않는다                      ROOT AGENTS.md §6 — 두 번 물린 함정이다
수치를 문서에 적으려면 재는 검사를 함께 만든다      검사를 못 만들면 적지 않는다
test.sh red 는 verify-screen-design.js 하나다      다른 게 red 면 진짜 결함이다
C# 은 UTF-8 BOM + CRLF                            verify-ui-baseline.sh UIB-005
```

---

## 8. 확인되지 않은 것

없다. 150% 배율까지 실측으로 닫혔다.

다만 **앱을 사람이 직접 클릭해 본 범위는 수검자 관리 하나뿐이다.** 예약·접수를 세운 뒤에는
같은 확인을 사용자에게 요청한다 — 시험과 캡처가 못 보는 것이 있다(1구간에서 실제로
`PerformClick` 이 그랬다).
