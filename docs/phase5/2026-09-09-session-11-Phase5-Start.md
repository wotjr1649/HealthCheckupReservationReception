# Phase 5 착수 인계 — 계약 재설계 뒤의 `07`, 킷 설치 뒤의 경계

2026-09-09 작성. 앞 인계문서(`2026-09-08-session-09-Phase5-Spec.md`)를 **대체한다.**

---

## 0. 무엇이 낡아서 이 문서를 쓰는가

앞 문서는 세션 09 시점의 판단이고 그때는 옳았다. **고쳐 쓰지 않고 그대로 둔다.**
다만 그 뒤에 계약 재설계(R7~R9)와 킷 설치가 있었고, 다섯 대목이 지금 거짓이다.

| 앞 문서 | 왜 낡았나 |
|---|---|
| §1 "DB 가 확정한 것" 수치 | R7 이 계약을 바꿨다. SP·Parameter·ResultCode·계약 시나리오가 전부 다르다 |
| §1 "이것이 `07` 의 오른쪽 절반이다" | 그 절반이 낡은 수치를 가리킨다. 그대로 쓰면 낡은 계약으로 대조한다 |
| §4 "킷을 아직 설치하지 마라" | 근거가 *"킷 `CLAUDE.md` 가 루트 `CLAUDE.md` 를 덮어쓴다"* 였다. v1.8 로컬 추가형은 그러지 않았고 설치는 끝났다 |
| §4 킷 경로 `.agents/contract/…` | 배포 레이아웃은 `.agents/kits/net461-dx20-mvp/contract/…` 다 |
| §6 "재설계는 `db-redesign` 이 맡는다" | 끝났고 `main` 에 병합됐다. 브랜치는 지웠다 |
| 머리말 "세션을 저장소 루트에서 연다" | §1 을 보라 — 이제 틀렸다 |

앞 문서 §1 이 스스로 경고한 함정(*"베껴 두었는데 재봉인에 전부 낡았다"*)에 자기가 빠졌다.
이 문서는 **수치를 하나도 적지 않는다.** 전부 도는 것을 가리킨다(루트 `AGENTS.md` §6).

---

## 1. 어디서 여는가 — `winforms/` 다

```text
D:/AIDEV/HealthCheckupReservationReception/winforms
```

앞 문서는 저장소 루트에서 열라고 했다. **지금은 `winforms/` 가 맞다.**

킷 본문이 *"Paths beginning with `.agents/` resolve from the project root"* 라고 하는데
저장소 루트에는 `.agents/` 가 **없다**(실측). 루트에서 열면 킷 안의 모든 경로 참조가 빗나간다.

루트 지침은 `winforms/` 에서 열어도 로드된다 — 상위 디렉터리를 거슬러 올라가며 읽는다.
`winforms/CLAUDE.md` 는 `@AGENTS.md` 를 맨 앞에 두어 `winforms/AGENTS.md` 도 함께 들어온다.

---

## 2. 지금 참인 것 — 읽지 말고 돌려서 봐라

```text
브랜치    main. db-redesign 은 병합 후 로컬·원격 모두 지웠다
봉인      docs/baseline/ 에 00~06.  07 은 없다 — 이번에 만든다
회차      어느 문서가 어느 회차인지는 06 §4.1 이 단일 출처다
실측·게이트 회차별 판정은 06 §42 가 단일 출처다
```

**이 문서에 회차 이름도 수치도 적지 않는다.** 앞 문서가 그렇게 하다 낡았다.

```bash
cd database
./scripts/test.sh                      # exit 0 · FAIL 0
node tools/verify-docs.js              # FAIL 0
./scripts/verify-baseline.sh           # 봉인 전건 일치
./scripts/verify-schema-doc.sh         # DOC-001~009
./scripts/verify-holiday-seed.sh       # HOL-G1~G6
./scripts/verify-winforms-unchanged.sh # manifest 일치
```

---

## 3. 킷은 어디까지 자동으로 들어오는가

**전부 들어오지 않는다.** 층이 셋이다.

```text
매 세션 자동     winforms/CLAUDE.md → @AGENTS.md · @PROJECT_INSTRUCTIONS.md   (Claude 는 import)
                 winforms/AGENTS.md 본문                                      (Codex 는 첫 호출 전 주입)
목록만           skills/winforms-devexpress-ui   이름과 설명만. 본문은 호출할 때 들어온다
트리거 때만      contract/*.md — repository · naming · service · build · report
```

`PROJECT_INSTRUCTIONS.md` 자신이 *"Read the following contract documents **only at their
stated triggers**"* 라고 적었다. 킷 검증 기록(`D:/AIDEV/winformdev_env/practical-validation/
additive-v1.8/results/validation.md`, 2026-09-08 합격 판정)도 `repository.md` 를 주입이 아니라
**실제 Read 로** 읽었다고 기록한다.

**그래서 계약 문서는 "언젠가 읽겠지" 가 아니라 트리거에서 읽어야 한다.**

---

## 4. 킷과 이 저장소가 어긋나는 두 곳

`winforms/AGENTS.md` 의 **`05` outranks the kit** 절이 이것을 한 줄로 덮는다. 충돌을 열거하지
않는 이유는 킷이 갱신되면 그 목록이 낡기 때문이다. 구체는 여기 적는다.

### 4.1 SP 계약이 어디 있는가 — 매 세션 들어오는 문장이다

```text
킷 PROJECT_INSTRUCTIONS.md §3
  "Its contract … lives in the task message or in docs/sp/<schema>.<name>.sql;
   match it exactly. A missing or unfit contract makes the affected requirement BLOCKED."
```

이 저장소에 `docs/sp/` 는 **없다.** 단일 출처는 `docs/baseline/05_DB_Rule_SP_Contract.md` 다.
그대로 두면 세션이 계약을 못 찾아 요구사항을 `BLOCKED` 로 적는다 — 킷이 그렇게 지시하고 있다.

### 4.2 업무결과를 무엇으로 판정하는가 — 리포지토리를 쓰기 직전에 들어온다

```text
킷 contract/repository.md
  "Decide an expected business outcome from the procedure's RETURN and OUTPUT values"

05 §3.6
  업무결과 전달에 다음을 사용하지 않는다.
    OUTPUT Parameter / SQL RETURN 값 / 업무실패용 RAISERROR / 결과메시지 문자열 비교
  C#은 결과코드를 Enum으로 매핑하여 분기한다.
```

정면으로 반대다. **`05` 가 이긴다.** RS0 의 `결과코드` 를 읽어 분기한다.

이 문장은 매 세션 들어오지 않고 **리포지토리를 쓰기 직전에만** 들어온다. 읽히는 순간이 곧
적용되는 순간이므로, 그 순간에 `05` 를 이미 펼쳐 두어야 한다.

킷 자신이 이 처리를 지시한다 — *"Do not … change the DBA contract to make the project fit
this kit"*, *"Keep project-specific exceptions in the existing project instructions."*

---

## 5. 이미 실측했다 — 다시 하지 마라 (2026-09-09)

```text
DevExpress 20.2   C:/Program Files (x86)/DevExpress 20.2/Components/Bin/Framework  DLL 257개
Visual Studio     Professional 2019 하나. MSBuild 는 vswhere 로 찾는다 (contract/build.md)
                  …/2019/Professional/MSBuild/Current/Bin/MSBuild.exe
csproj            TargetFrameworkVersion v4.6.1 · LangVersion 미지정(= C# 7.3 기본)
                  DevExpress 참조는 아직 0개다 — 이번에 넣는다
테스트            MSTest.TestFramework · TestAdapter 2.2.10 · net461. 킷이 지정한 그 버전이다
App.config        <connectionStrings> 가 아직 없다
```

빌드는 **이 세션에서 돌리지 않았다.** `Build Verified` 를 주장하지 마라.

---

## 6. 이번에 할 일

**1. `07_UI_DB_Matrix_Final_Validation.md` 를 `docs/phase5/` 에 CANDIDATE 로 쓴다.**
`06` 이 정확히 그 경로를 밟았다. 오른쪽 절반(DB 가 확정한 것)은 **`06` §42 와 `05` 를 읽어**
채운다 — 앞 문서의 수치를 옮겨 오지 마라.

**2. `App.config` 에 `<connectionStrings>` 의 `AppDb` 를 넣는다.** 통합인증만 쓴다.
`user id=`·`uid=`·`integrated security=false` 는 금지값이다(`winforms/AGENTS.md`).

**3. winforms 계열의 secret 스캔을 만든다.** `database/scripts/verify-no-secret.sh` 는
`Deploy.sql`·`deploy/`·`scripts/`·`tests/`·`tools/`·`artifacts/` 만 훑는다(실측). **winforms 는
스캔 범위 밖이다.** 연결문자열이 들어가는 순간 이 저장소에서 그것을 보는 게이트가 하나도 없다.

**4. 화면.** `03` 의 화면 목록을 따른다. `DLG-HOL-01` 휴무일 관리는 R7 에서 신설돼 설계까지만
되어 있다. 와이어프레임 생성기에는 컨트롤 겹침 검사가 R9 에 들어갔다.

---

## 7. 절대 어기면 안 되는 것

```text
docs/baseline/ 는 봉인이다. 여는 것은 루트 AGENTS.md §2 의 재봉인이며 database 계열의 절차다
계약이 틀렸다고 판단되면 고치지 말고 멈추고 사용자에게 보고한다
실행하지 않은 검증을 PASS 로 적지 않는다                database/AGENTS.md §10
같은 값을 두 곳에 두지 않는다                          루트 AGENTS.md §6
winforms 를 바꾼 커밋에서 manifest 를 함께 갱신한다      winforms/AGENTS.md
머신 시각은 사람이 옮기고 되돌린다. 옮긴 동안에는 커밋하지 않는다
```

---

## 8. 확인하지 않은 것

```text
이 솔루션을 MSBuild 로 빌드해 본 적이 없다 — csproj 에 DevExpress 참조가 아직 없다
licenses.licx 가 없다. DevExpress 컨트롤을 넣는 순간 필요해진다 (contract/build.md)
docs/sp/ 부재가 실제 세션에서 BLOCKED 판정을 유발하는지 관측한 적 없다 — §4.1 로 막았다고 본다
Phase 5 계열이 어떤 게이트 집합을 회귀로 돌릴지 아직 정하지 않았다
```
