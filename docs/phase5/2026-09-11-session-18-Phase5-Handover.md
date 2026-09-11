# Phase 5 인계 — 2026-09-11 (session 18)

- **대상:** 다음 세션. `winforms/` 기능 구현은 끝났고 남은 것은 **마무리 넷**이다
- **앞 문서:** `2026-09-10-session-17-Phase5-UI-Overhaul.md` — **설계 결정과 그 근거는 전부 거기 §4.1~§4.19 다.**
  이 문서는 그것을 베끼지 않는다 (ROOT `AGENTS.md` §6). 「왜 이렇게 되어 있나」는 §4.x 를 봐라
- **브랜치:** `phase5-start` — `bb628f6`, `main` 보다 81 커밋 앞. **origin 과 같다 (2026-09-11 푸시 완료)**

---

## 1. 지금 참인 것 — 읽지 말고 돌려서 봐라

```bash
cd winforms && ./scripts/test.sh        # 게이트 13 · red 0
MSB=$(cat /tmp/msb.txt); VST=$(cat /tmp/vst.txt)   # 경로 캐시가 없으면 다시 찾아라
"$VST" tests/HealthCheckupReservationReception.Tests/bin/Debug/HealthCheckupReservationReception.Tests.dll
```

```text
253 통과 (DB 통합 10 포함)     실패 0
게이트 red 0                    ← 2026-09-11 부터 그렇다. §4.19 를 봐라
SP 계약 20 / 실제 호출 20       빠진 것 없음
03 화면 14종 중 13종 구현       DLG-PAT-02 만 없다 (§4.9 에서 의도적으로 지웠다)
리본 명령 열넷 전부 동작        barBtnNotImplemented 는 지웠다
```

`[!]` **`scripts/test.sh` 는 이제 green 이어야 한다.** 「red 는 그 하나」라는 옛 면제는
2026-09-11 에 사라졌다 — 어떤 red 든 진짜 결함이다.

---

## 2. 남은 일 — 이 순서대로

### 2-1. DB 전체 회귀 `[사용자 승인 있음]`

```bash
cd database && ./scripts/test.sh
```

**`Rebuild.sql` 이 `DROP DATABASE` 를 한다.** 그래서 이 한 번이 네 가지를 동시에 처리한다:

| 항목 | 지금 | 회귀 뒤 |
|---|---|---|
| 운영기준 창 | `00:00~23:59` (개발용으로 넓혀 둠, §4.8) | `00` 값 복귀 — `OPR-G4` 가 판정 |
| 시험 데이터 | 수검자 191 · 변경이력 1244 | 초기화 + fixture(`T020` 등) 심어짐 |
| `verify-contract-all.sh` | 188 red (fixture 가 없어서다) | 판정 가능 — RS5 절 포함 |
| R18 증거 | `NOT RUN` (`06` R18 개정 범위) | 닫힘 |

`[X]` **회귀 뒤 `06` 의 R18 개정 범위에서 `NOT RUN` 문장을 걷어야 한다.** 그건 봉인 문서라
**재봉인**이다 — 회차 이름을 붙이고 `05`·`06` 해시와 매니페스트를 같은 커밋에 넣는다
(ROOT `AGENTS.md` §2.2). 문장만 고치고 해시를 안 고치면 게이트가 red 가 된다.

`[!]` **사용자가 화요일 전에 운영기준을 되돌리겠다고 했다.** 회귀가 그것을 대신하므로,
회귀를 돌렸으면 따로 할 일이 없다. 안 돌렸으면 §4.8 의 SQL 로 되돌린다.

### 2-2. `verify-winforms-unchanged.sh` 매니페스트 되살리기

```bash
cd database && ./scripts/verify-winforms-unchanged.sh init
```

같은 커밋에 매니페스트를 넣고 **`winforms/AGENTS.md` 의 「Suspended for the overhaul」 문단을
걷는다.** 문단을 안 걷으면 규칙이 거짓말로 남는다. 대개편이 끝났으므로 되살릴 때다.

### 2-3. SP 성능 재측정 `[사용자 결정: 넣는다]`

`session-17` §4.16 에 경위가 있다 — **같은 측정을 되풀이하지 마라.** 요지:

```text
두 목록 SP 가 catch-all 술어 `(@p IS NULL OR col = @p)` 를 쓴다
OPTION (RECOMPILE) 을 제안했다가 접었다 — 통제된 비교에서 계획이 같았다
6행·191행에서는 어느 계획이든 비용이 같아 판정이 불가능하다
```

회귀가 DB 를 새로 세우는 그 시점이 **1년치 규모를 심어 다시 재기 가장 싼 때**다.

```text
심을 것   예약접수 ≈ 10,000행 (20명 × 2시간대 × 250일) · 수검자 ≈ 3,000행
재는 법   SET SHOWPLAN_TEXT ON 으로 USP_HC_예약접수목록_조회 를 조건 조합별로
          힌트 없이 / OPTION (RECOMPILE) 붙여 **같은 질의문으로** 견준다
```

`[X]` **통제되지 않은 비교를 다시 하지 마라.** 처음에 힌트 없는 SP 와 힌트 붙인 애드혹
질의를 견줘 틀린 결론을 냈다. 바꾸는 것은 힌트 **하나**여야 한다.

측정 결과가 이득을 보이면 재봉인이고(`06` §9.2 허용목록에 쿼리 힌트를 얹어야 한다 —
그 목록은 *"아래에 없는 기능은 쓰지 않는다"* 이다), 안 보이면 §4.16 에 수치를 적고 닫는다.

### 2-4. 인계 마무리

```text
07_UI_DB_Matrix_Final_Validation.md   그대로 둔다. ROOT §4 가 갱신 의무를 면제했다.
                                      「07 은 전반부 기록, 후반부는 session-17 §4.x」 한 줄만 적는다
공개본 03 화면설계서                   고치지 않는다 (§4.19). 인계 때 이 문장이 함께 가야 한다:
                                      **실행본이 참이고, 공개본 03 은 설계 시점의 기록이다**
```

---

## 3. 다음 사람이 밟기 쉬운 함정 — 넷

### 3-1. `DROP` 의 층이 셋이다

| 명령 | 사라지는 것 |
|---|---|
| `sqlcmd -i deploy/04_Procedures_Select.sql` | **SP 만.** 데이터는 그대로 — R18 때 쓴 길이다 |
| `scripts/deploy.sh` | 테이블 다섯 `DROP`+`CREATE`. `변경이력` 만 가드로 생존 |
| `scripts/rebuild.sh` | **`DROP DATABASE`.** 전부 |

SP 하나만 고칠 때 `deploy.sh` 를 부르면 데이터가 날아간다. `04_Procedures_Select.sql` 만
직접 먹여라.

### 3-2. 봉인을 여는 것은 한 커밋이다

`docs/baseline/` 이나 `database/deploy/` 를 건드리면 **같은 커밋에** 전부 들어가야 한다:

```text
회차 이름 (HC-RSV-RCP-YYYYMMDD-Rn)
문서 본문 + 머리말의 「Rn 개정 범위」
06 §4.1 회차표          ← 회차의 단일 출처. 05 를 열면 06 도 열린다
database/scripts/verify-baseline.sh 해시
winforms/scripts/verify-db-frozen.sh init → artifacts/db-frozen-manifest.txt
docs/phase4/plans/08-verification-finalization.md 버전 사슬   ← V06 이 본다
node tools/docgen/build_all.js            ← 공개본 여섯. 05 는 파싱형이라 돌리면 따라온다
```

`[X]` **R18 에서 `[R18]` 표지를 절 제목에 붙였다가 공개본으로 새어 나가 `verify_output` 이
잡았다.** 회차 표지는 §4.1 과 머리말이 갖는다 — 본문 제목에 적지 마라.

`[X]` **`build_05.js` 에 Result Set 행수 가드가 하드코딩돼 있다.** `05` 의 RS 행이 늘면
그 수도 같이 고쳐야 한다 (R18: 271 → 278).

### 3-3. 회귀가 사람 손을 기다리게 만들지 마라

화면이 사용자에게 묻는 길은 **한 곳으로 모아** 시험이 덮어쓸 수 있어야 한다.

```text
FrmReservation.Confirm      virtual — SilentReservationForm 이 덮어쓴다
IWorkbenchView.Confirm       Fake 가 답을 미리 정한다
```

`XtraMessageBox` 를 새로 부르는 코드를 쓸 때는 **그 길이 시험에서 답해질 수 있는지** 먼저
보라. 2026-09-11 에 캡처 시험이 사람이 Yes 를 누를 때까지 멈춰 있었다.

### 3-4. 판정을 화면에서 다시 하지 마라

이 저장소에서 되풀이해 물린 자리다. 허용여부·가능 여부·변경범위·No-op 은 전부 SP 가 낸다.

```text
RS4 허용여부        Ribbon · DLG-RCP-01 · DLG-RCP-02 가 그대로 그린다
저장가능/차단코드   WF-RSV-01 이 그대로 그린다
변경범위            SP-RSV-03 이 현재 행과 견줘 잰다 — 화면은 최종 상태만 보낸다
결과코드 1 No-op    SP-RCP-02 가 낸다
```

`[X]` **모르는 것은 불가가 아니다.** 조회가 실패해 판정을 못 받았으면 **열어 두고** DB 가
판정하게 한다. 닫아 버리면 DB 한 번 끊긴 것으로 기능이 통째로 막힌다.

---

## 4. 이번 세션이 한 일 — 한 줄씩

```text
9a20385  예약 버튼을 행 상태로 닫고, 정원 문구의 기준을 갈라 적는다
4278c42  상태 표시명을 화면이 갖는다 — 예약 → 예약완료
9db3c48  탭을 창구로 가른다 — 접수 관리가 오늘의 예약을 본다
3c6b3f4  수검자 상세에 예약·접수 이력 (메모 오른쪽)
37476fa  노쇼로 남은 지난 예약이 오늘 예약을 막지 않는다 (시험 둘)
7352db3  회귀가 사람의 손을 기다리지 않게 하고, 예약 판정 조회를 오늘부터로 좁힌다
7d8c08a  SP 최적화를 검토하고 접은 경위 (§4.16)
b256ef0  예약 없는 수검자만 을 조회 영역으로 꺼낸다 — 예약 관리에 예약은 두지 않는다
929e64d  DLG-LOG-01 계약 계층 (SP-LOG-01)
652ffa0  DLG-LOG-01 변경이력 열람 — 버튼 셋이 창 하나를 연다
eecf93e  Write SP 넷의 계약 계층 (SP-RSV-03·04 · SP-RCP-01·03)
ecf5654  예약취소·접수취소 — 03 §13 확인 둘
9599a2d  DLG-RCP-01 접수 처리
5776f13  DLG-RSV-01 예약 변경 — 같은 모달의 분기
3813099  reseal(R18) SELECT_예약접수상세 에 RS5 추가검사구성 7행 — 05·06 을 연다
18c8e59  DLG-RCP-02 추가검사 변경 — 리본에 미구현 버튼이 남지 않는다
3767b37  SELECT 계열을 실물 DB 에 붙이고, 파일 없는 화면 셋에 표지를 단다
bb628f6  멈춘 게이트를 갈라내 회귀 red 를 0 으로 만든다
```

---

## 5. 절대 어기면 안 되는 것

```text
docs/baseline/ · database/deploy/    동결. 열려면 §3-2 의 한 커밋 전부
연결문자열                            통합인증만. user id/uid 금지, integrated security 끄지 않는다
C# · csproj                          UTF-8 BOM + CRLF. 나머지는 LF     verify-ui-baseline.sh UIB-005
같은 값을 두 곳에 두지 않는다          적으려면 게이트를 함께 만든다      ROOT AGENTS.md §6
scripts/test.sh                      green 이어야 한다. red 는 전부 진짜 결함이다
```
