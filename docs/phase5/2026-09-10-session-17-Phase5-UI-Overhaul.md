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

### 2.1 실측으로만 알 수 있었던 것

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

서비스를 생성자로 받는 화면은 VS 디자이너가 못 연다
  「디자이너에 대한 문서를 로드하지 않았으므로 디자이너를 표시할 수 없습니다」
  → 디자이너가 설계 대상을 매개변수 없는 생성자로 만들기 때문이다. 셋이 그랬다
    (MainForm · FrmPatientEditor · FrmPatientSelect). 컴파일도 시험도 통과하므로
    디자이너를 여는 사람만 만난다
  → **새 화면(WF-RSV-01 · WF-WRK-01)에도 반드시 단다.** MainFormTests 의
    `모든_화면이_디자이너용_생성자를_갖는다` 가 이제 이것을 잰다

LayoutControl 은 한 줄 안의 라벨 폭을 가장 긴 것에 맞춰 통일한다
  `예약/접수일` 옆의 `~` 한 글자짜리 라벨까지 그 폭을 떠안고, 그만큼이 입력칸에서 깎인다
  (실측 2026-09-10: 종료일 DateEdit 이 54px 로 눌려 잘린 채 사용자에게 보고됐다)
  → 항목마다 `TextAlignMode = TextAlignModeItem.AutoSize`
  → **라벨 길이가 제각각인 줄에서만 드러난다.** WF-PAT-01 은 라벨 셋이 전부 같은 폭이라
    이 함정을 밟은 적이 없다 — 그 화면을 본떴다고 안전한 것이 아니다
  → 세로로 쌓인 상세 그룹에서는 반대로 그 통일이 필요하다(`AlignWithChildren`).
    끄고 켜는 기준은 "한 줄인가 한 열인가" 다

성공한 0건과 실패는 화면에서 같은 그림이다
  빈 Grid 는 고장과 구별되지 않는다 — 예약접수 0행인 DB 에서 조회가 정상 성공했는데
  "조회가 안 된다" 로 보고됐다(실측 2026-09-10). SP 는 성공여부=1 · 0행을 냈다
  → Grid 빈 자리에 이유를 적는다 (`clsGridColumns.ShowEmptyText`)
  → 조회 실패는 모달이 아니라 Inline 이다. 모달이면 창을 열자마자 뜨는 것을 막느라
    조용히 삼켜야 하고, 그러면 실패가 아예 보이지 않는다
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
                             화면이 늘면 그 화면 이름으로 같은 두 ID 가 더 뜬다 — 같은 것이다
                             다른 게 red 면 진짜 결함이다 (winforms/AGENTS.md)
MSBuild + vstest             warning 0 · 전건 통과
```

**SP 21개가 전부 배포돼 있다.** `SP-LOG-01` 포함. 남은 것은 전부 C# 이다 — DB 의존 0.

**C# 이 지금 몇 개를 부르는지는 여기 적지 않는다** — `verify-layering.sh` 의 `LAY-002` 가
세어서 05 §1.3 과 대조한다. 예전에 `다섯뿐이다` 라 적어 두었다가 `WRK-01`·`WRK-02` 가
붙으면서 거짓이 되었다 (ROOT `AGENTS.md` §6). 시험 건수도 같은 이유로 뺐다 — 세는 게이트가
없는 수치다.

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
0. 끝났다 (2026-09-10). 다시 하지 마라 — 아래는 그때 실제로 일어난 일이다
   → UcPatientManagement.Designer.cs 는 저장 뒤에도 바뀌지 않았다. 우려하던 전면
     재직렬화가 일어나지 않았고 따로 끊을 커밋도 없었다
   → licenses.licx 에 세 줄이 들어왔다 (LayoutControl · GridControl · TextEdit)
   → MainForm 이 안 열려서 화면 셋에 디자이너용 생성자를 달았다 (커밋 c86e320)

1. 끝났다 (커밋 `a0e1de7`). `IMainView` 가 03 §3 의 호출계약 둘을 이름 그대로 갖는다
   → **Context 를 되살린 자리는 필드가 아니라 `OpenWorkbench` 의 매개변수다.** 값을
     두 곳에 두지 않으려는 것이고, §4.2 가 말한 「그 값이 나르던 Context」가 바로 이것이다
   → `ReservationContext`·`WorkContext`·`NavigationSource` 셋이 `IMainView.cs` 에 산다

2. 끝났다 (커밋 `e4015d4` · `01d28d3`). WF-WRK-01 이 조회·상세·Ribbon Action 까지 선다
   → 예약·접수 Page 의 `[검색]` 그룹과 `[컬럼설정]` 을 걷었다. 수검자 Page 가 먼저 간 길이다
   → `verify-work-actions.sh` 를 새로 두었다: 05 §8.2 의 업무동작코드 다섯 ↔ `DbWorkAction.cs`

   `[!]` **초판을 사용자가 눌러 보고 셋을 지적했다.** 여기 "새 함정은 없었다" 고 적어 두었던
   것은 거짓이었다 — 시험 전건 통과·게이트 green 인 화면에서 나온 것들이다. 함정 둘은
   §2.1 에 올렸다. 고친 결과:
   → 라벨 폭 통일로 종료일 칸이 잘렸다 → 항목마다 `TextAlignMode=AutoSize`
   → 예약접수 0행이라 빈 화면이었는데 화면이 이유를 말하지 않았다 → 빈 Grid 안내 ·
     조회 실패를 Inline 으로 · 기본 기간을 `오늘 ~ (비움)` 으로(§9.3 `From만 있으면 이후`)
   → 조회 UI 를 WF-PAT-01 과 통일하라는 지시 → `[조회 조건]` 드롭다운을 넣고 끝 셋을
     `[조회] [조회 조건] [컬럼 설정]` 순서로. 캡처로 재니 두 화면이 같은 x 좌표에 선다

   **부품 넷이 공통이 되었다** (킷 §2 — 같은 모양이 구체 화면 둘에 생겼다). 세 번째 화면은
   새로 만들지 말고 이것을 쓴다:

   ```text
   clsSearchConditions  조회조건 드롭다운. 칸 둘짜리 조건(기간)은 Add(...).Also(...) 로 잇는다
   clsColumnChooser     컬럼 드롭다운 + [기본값 복원]. 목록은 Grid 컬럼에서 만든다
   clsGridRowPicker     Grid 가 잡아 둔 행 ↔ 사용자가 고른 행
   clsGridColumns       컬럼 정렬 · 빈 목록 안내
   ```

   화면에는 Designer 가 이름으로 잇는 한 줄짜리 핸들러만 남긴다 — 킷 §5 배선 규약 그대로다.

   시험용 예약 4건을 `SP-RSV-02`·`SP-RSV-04` 로 넣어 두었다 (9/11 AM·PM · 9/12 AM · 9/14 취소).
   지우려면 `DELETE FROM dbo.예약접수; DELETE FROM dbo.변경이력;` 이다.

3. 끝났다 (커밋 `1dd2e7d` · `0fdcc5b`). 정원·TGT·NEX·AEX·2단계 저장이 다 섰고
   §8.11 저장 성공 → Workbench 자동선택까지 닫혔다
   → **화면이 다시 계산하는 것이 하나도 없다.** 마감시각 넷(03 §8.6 표)은 R13 에서 DB
     `운영기준` 으로 갔고 `RS2` 로 내려온다. `[예약저장]` 은 05 §9.12 의 `저장가능` 그대로다.
     화면 계산결과는 03 §8.7 판정문구 하나뿐이다
   → 중복판단(§8.5)은 `SP-PAT-05` 다. 수검자를 확정한 **직후**, 아직 예약일이 없을 때
     물어야 하므로 `SP-RSV-01` 의 `다른업무ID` 로는 대신할 수 없다
   → 03 §9.1 WorkId Targeted Navigation 도 함께 채웠다. 상세를 먼저 불러 그 업무의 날짜를
     알아낸 뒤 목록을 그 하루로 좁힌다 — 조회조건을 그대로 두면 방금 저장한 건이 목록에 없다
   → **아직 사람이 클릭해 보지 않았다** (§8)

   `[!]` 시험이 결함 둘을 잡았다. 둘 다 시험을 먼저 쓴 덕에 드러났고, 같은 모양이 다음
   화면에서도 나올 수 있다:
   - 되풀이 조회 가드의 열쇠를 **보낸 값**으로 잡으면 안 된다. 시간대를 고르지 않고 물으면
     DB 가 하나를 정해 돌려주고 화면이 그것을 든다(05 §9.6) — 열쇠는 **조회 뒤 화면 값**이다
   - 저장 실패 사유를 적고 나서 Refresh 를 부르면 `차단메시지`(대개 빈 값)가 그 칸을 덮어써
     사유가 사라진다 — Refresh 를 먼저 하고 사유를 마지막에 적는다

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

**WF-WRK-01 은 닫혔다** (2026-09-10 사용자 확인). 실물 DB 에 붙여 직접 눌렀고 어긋난 것이
없었다 — 남은 것은 아직 만들지 않은 업무 기능(`[예약변경]`·`[예약취소]` 등)뿐이며 그것은
§6 의 다음 구간들이다.

**같은 확인을 WF-RSV-01 에도 요청한다.** 첫 판을 눌러 봤을 때 시험 전건 통과·게이트 green
인데도 셋이 나왔다(§6 2번) — 시험과 캡처가 못 보는 자리가 실제로 있다.
