# Phase 5 인계 — 2026-09-11 (session 19). **session 18 이 남긴 마무리 넷을 닫았다**

- **대상:** 다음 세션. `winforms/` 기능 구현도 마무리 넷도 끝났다
- **앞 문서:** `2026-09-11-session-18-Phase5-Handover.md`(할 일 목록) ·
  `2026-09-10-session-17-Phase5-UI-Overhaul.md` §4.1~§4.19(**설계 결정과 근거 전부**)
  이 문서는 그것을 베끼지 않는다 (ROOT `AGENTS.md` §6)
- **브랜치:** `phase5-start` — `221d57d`, `main` 보다 86 커밋 앞. **origin 은 아직 `98a6f6e` 다 (미푸시)**

---

## 1. 지금 참인 것 — 읽지 말고 돌려서 봐라

```bash
cd winforms  && ./scripts/test.sh          # 게이트 PASS 115 · FAIL 0
cd database  && ./scripts/test.sh          # 12분. DB 를 DROP 한다
MSB="C:/Program Files (x86)/Microsoft Visual Studio/2019/Professional/MSBuild/Current/Bin/MSBuild.exe"
VST="C:/Program Files (x86)/Microsoft Visual Studio/2019/Professional/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe"
"$MSB" winforms/HealthCheckupReservationReception.sln -t:Build -p:Configuration=Debug -v:m
"$VST" winforms/tests/HealthCheckupReservationReception.Tests/bin/Debug/HealthCheckupReservationReception.Tests.dll
```

```text
winforms 시험       253 통과 · 실패 0 · 건너뜀 0   (DB 통합 10 포함)
winforms 게이트     PASS 115 · FAIL 0
database 회귀       회차 R19 · exit 0 · PASS 411 · FAIL 0 · SKIP 0 · NOT RUN 1
                    NOT RUN 은 RBD-001 하나이고 인스턴스가 1개라 구조적으로 불가능하다
봉인                7/7 · 06 은 HC-RSV-RCP-20260911-R19 (v1.16)
운영기준            00 값 복귀 (09:00~18:00 · 예약 10:00/15:00 · 접수 11:00/16:00)
DB 데이터           fixture 만 (수검자 40 · 예약접수 26). 회귀 뒤 tests/00·00b 로 심었다
```

`[!]` **회귀는 DB 를 비운 채로 끝난다.** `clean-rebuild-verify.sh` 가 마지막에 새 DB 를
세우기 때문이다. 그 상태에서 winforms 의 `TestCategory=Db` 를 돌리면 `SP_WRK_02` 가
`Inconclusive` 가 된다 — **통과가 아니다.** 회귀 뒤에는 반드시 둘을 먹여라:

```bash
cd database
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -i tests/00_Test_Harness.sql  -o artifacts/logs/h1.log
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -i tests/00b_Test_Harness_RCP.sql -o artifacts/logs/h2.log
```

---

## 2. 이 세션이 닫은 넷

```text
254c3d3  매니페스트를 되살린다 — winforms/AGENTS.md 의 「Suspended for the overhaul」 문단을 걷었다
bd61691  1년치를 심어 SP 힌트를 다시 쟀다 (§4.16). 본전이라 봉인을 열지 않는다
65c3f3e  reseal(R19) — R18 이 NOT RUN 으로 미룬 전체 회귀를 돌려 닫고, §1 의 R4 잔재를 걷었다
221d57d  인계 문장 둘 — 07 의 자리 · 공개본 03 의 지위
```

수치와 경위는 각 커밋 메시지와 §4.16 · `06` 머리말 · `docs/phase4/reseal-history.md` 가
갖는다. 여기에 베끼지 않는다.

---

## 3. 다음 사람이 밟기 쉬운 함정 — session 18 의 넷에 더해 셋

### 3-1. `sqlcmd` 에 **절대경로를 주지 마라**

`-i` · `-o` 에 `/tmp/...` 나 `C:/Users/...` 를 주면 Git Bash 가 `C:/Users/...` 로 바꾸고
`sqlcmd` 가 그 안의 `/U` 를 옵션으로 읽어 이렇게 죽는다:

```text
Sqlcmd: The -E and the -U/-P options are mutually exclusive.
```

**상대경로만 쓴다.** `cd database && sqlcmd ... -i tests/00_Test_Harness.sql -o artifacts/logs/x.log`.
`-u`(유니코드 출력)도 같은 자리에서 `-U` 로 읽히므로 손으로 부를 때는 붙이지 않는다 —
`scripts/test.sh` 가 쓰는 것은 스크립트 안에서만 듣는다.

### 3-2. 쉘 히어독으로 만든 `.sql` 은 **BOM 이 없어 조용히 틀린다**

한글 식별자가 cp949 로 읽혀 깨지는데 `-b` 로도 종료코드가 0 이고 출력만 빈다. 실제로 한 번
빈 파일을 받았다. `.sql` 을 새로 만들 때는 `utf-8-sig` 로 쓴다 (`verify-docs` `V22` 가
`deploy/`·`tests/` 의 BOM 을 보지만, 임시 파일은 아무도 안 본다).

### 3-3. `SET SHOWPLAN_TEXT ON` 은 **지역변수를 모른다**

실행하지 않으므로 `DECLARE @성명패턴 = ...` 이 돌지 않고, `OPTION (RECOMPILE)` 이 붙은
문장은 그 변수를 `NULL` 로 접어 **`Constant Scan`** 을 낸다 — 계획이 아니라 인공물이다.
지역변수를 술어에 쓰는 SP 의 힌트 효과는 `SHOWPLAN_TEXT` 로 못 본다. 실제로 실행해
`STATISTICS IO` 와 `sys.dm_exec_procedure_stats` 로 재야 한다 (§4.16 이 그렇게 했다).

---

## 4. 남은 것 — 전부 「지금 고치지 않는다」로 닫혀 있다

| | 무엇 | 어디에 적혀 있나 |
|---|---|---|
| 1 | 마감시각 규칙(`03` §8.6)의 **화면 끝까지 통한 실측**이 없다. DB 쪽은 회귀가 판정한다 | §4.8 |
| 2 | `USP_HC_예약접수목록_조회` 에 `@수검자ID` 가 없고 `@상태코드` 가 한 값뿐이다 | §4.16 A·B |
| 3 | `완료이력` 을 읽는 SP 가 없어 「올해 검진 안 받은 사람」 목록을 못 만든다 | §4.17 틈 2 |
| 4 | 사람 없이 날짜 정원을 못 묻는다 (`SP-RSV-01` 이 `@수검자ID` 필수) | §4.17 틈 1 |
| 5 | 예약접수가 10만 행 대가 되면 `OPTION (RECOMPILE)` 결론이 뒤집힌다 | §4.16 |
| 6 | `verify-screen-design.js` 의 `SCR-002`·`003`·`004` 는 멈춘 채다 | `winforms/AGENTS.md` |

`[!]` 1 은 **이제 실측할 수 있다.** R19 회귀가 운영기준을 `00` 값으로 되돌렸으므로 PM
예약마감 `15:00` 과 접수마감 `16:00` 이 실제로 지나간다 — 그 사이에 실행본을 눌러야 한다.
개발용으로 창을 다시 넓히면(§4.8) 그 길은 또 시험되지 않은 채로 남는다.

---

## 5. 절대 어기면 안 되는 것 — 바뀐 것 하나

```text
docs/baseline/ · database/deploy/   동결. 열려면 reseal-history.md 의 R19 항목이 목록이다
winforms/ 를 고치면                 같은 커밋에 매니페스트를 동봉한다
                                    ← **보류가 끝났다.** session 18 까지는 면제였다
연결문자열                          통합인증만. user id/uid 금지, integrated security 끄지 않는다
C# · csproj                         UTF-8 BOM + CRLF. 나머지는 LF   verify-ui-baseline.sh UIB-005
같은 값을 두 곳에 두지 않는다        적으려면 게이트를 함께 만든다    ROOT AGENTS.md §6
scripts/test.sh                     green 이어야 한다. red 는 전부 진짜 결함이다
판정을 화면에서 다시 하지 않는다     허용여부·저장가능·변경범위·No-op 은 전부 SP 가 낸다
```
