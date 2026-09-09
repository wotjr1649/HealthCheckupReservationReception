# 검진 예약·접수 관리 프로그램 — 저장소 규칙

이 파일이 **본문**이다. Codex 는 `AGENTS.md` 를, Claude Code 는 `CLAUDE.md` 를 읽으므로
`CLAUDE.md` 는 `@AGENTS.md` 한 줄로 이것을 가져온다. **규칙은 여기에만 적는다** —
`CLAUDE.md` 에 내용을 쓰면 Codex 가 그것을 영영 못 본다.

디렉터리별 작업 경계는 그 디렉터리의 `AGENTS.md` 가 따로 적는다(`database/AGENTS.md`).
여기서는 저장소 전체를 다스리는 것만 적는다.

## 1. 문서 체계 — 00 부터 07 까지 하나의 사슬

번호는 디렉터리별이 아니라 **한 줄기**다. `06` 이 `docs/phase4/` 에 있었던 것은 규칙이 아니라
입주 규칙이 없어서 남아 있던 것이고, 2026-09-08 에 `docs/baseline/` 으로 들였다.

```text
00_Project_Policy.md                업무정책 — 최상위. CP·EP·RP·RCP 정책과 TGT·NEX·AEX·HOL Rule
01_Process_Definition.md            업무프로세스 — P01~P03 의 단계·분기·상태전이
02_Function_Definition.xlsx         기능정의 — Function ID 와 기능행
03_Wireframe_Definition.md          화면설계 — 화면 구성·필드·검증·Ribbon
04_DB_Design.md                     DB 설계 — 테이블·컬럼·키·제약·인덱스
05_DB_Rule_SP_Contract.md           Rule·SP 계약 — SP·TVF·Parameter·Result Set·ResultCode
06_DB_Transaction_Security_Seed.md  DB 구현 계약 — Transaction·잠금·Seed·시험. Phase 4 실행검증 기록
07_UI_DB_Matrix_Final_Validation.md 화면↔DB 최종 대조 — Phase 5 검증 기록. **아직 없다**
```

충돌하면 **번호가 작은 쪽이 이긴다.** `00 → 07 → DB Script / C# Source` 순이다.

## 2. `docs/baseline/` 은 폴더가 아니라 봉인이다

여기 있는 파일은 전부 SHA-256 으로 얼려 있고 `database/scripts/verify-baseline.sh` 가 한 바이트라도
다르면 `exit 1` 을 낸다. **"여기 있는 건 다 봉인됐다" 가 이 폴더의 유일한 뜻이다.**

### 2.1 입주 규칙

문서는 **`FINAL` 이 되는 순간** `docs/baseline/` 으로 들어온다. 쓰는 동안에는 밖에 있다.

```text
쓰는 중   docs/phase4/  ·  docs/phase5/       게이트 밖. 자유롭게 고친다
FINAL     docs/baseline/                      + verify-baseline.sh 에 해시 한 줄
```

쓰는 중인 파일을 봉인 폴더에 두면 저장할 때마다 게이트가 red 가 된다. 그래서 순서가 이 방향이다.

### 2.2 재봉인

이미 입주한 문서를 고치는 것은 **재봉인**이며, 회차 이름을 붙이고 **파일과 해시를 같은 커밋에 묶는다.**
한 커밋 안에서 끝내지 못하면 되돌린다 — 게이트가 red 인 커밋을 남기지 않는다.

```text
R2  HC-RSV-RCP-20260903-R2
R3  HC-RSV-RCP-20260904-R3   00~05 전체
R4  HC-RSV-RCP-20260908-R4   04·05 만 (SP 전면 한글화)
R5  HC-RSV-RCP-20260908-R5   04·06 만 (§42 Gate 정정 · 상호참조를 06 §4.1 로 위임)
R6  HC-RSV-RCP-20260908-R6   06 만 (§42 의 미래 단정 한 줄 제거). 04 는 연쇄가 끊겨 안 열었다
```

**기준선 ID 는 세트가 아니라 문서마다 "마지막 봉인 회차" 를 뜻한다** — 문서마다 다른 것이 정상이다.
**어느 문서가 어느 회차인지는 `06` §4.1 표가 단일 출처다.** 여기에 베끼지 않는다 — 예전에 적어 두었다가
R5 재봉인에 거짓이 되었고, 같은 값을 베낀 `04` 도 함께 열어야 했다(§6).

### 2.3 무엇을 열 수 있는가

기본은 **읽기 전용**이다. 재봉인 절차를 밟는 그 커밋에서만, 그 절차가 지정한 파일만 연다.
목록 밖 파일이면 여전히 쓰지 않는다.

## 3. 산출물 — `docs/baseline/output/`

사내 공개용 `xlsx`·`pptx` 다. **원본과 층이 다르다** — 원본은 `docs/baseline/`, 공개본은 그 아래 `output/`.

```
node tools/docgen/build_all.js
```

공개본 전부를 만들고 **반드시** `tools/docgen/verify_output.js` 를 지난다. 하나씩 손으로 돌리면
마지막 검사를 빼먹는다. **종수를 여기 적지 않는다** — `build_all.js` 의 `STEPS` 가 단일 출처다.

- `output/` 은 **커밋한다** (2026-09-09 사용자 지시). 예전에는 `.gitignore` 대상이었고 그 시절
  문장이 여기 있었다 — 리뷰가 공개본을 볼 수 없다는 뜻이라 걷었다. PowerPoint 잠금 파일만 뺀다.
- 바이너리를 손으로 만지지 않는다. 산출물을 고치려면 **생성기를 고쳐 다시 돌린다.**
- **다시 돌리면 여섯 종이 전부 바뀐다.** 생성기가 결정적이지 않다 — `docProps` 에 생성 시각이
  들어간다. 그중 **내용이 바뀐 것은 일부**이며, 무엇이 실제로 바뀌었는지는 `docProps` 를 뺀
  파트 비교로 확인한다(R10 실측: `03` 의 슬라이드 한 장만 내용이 달랐고 나머지 다섯은 0건).
- **`03` 화면설계서의 배치 원본은 `tools/docgen/wireframe/` 다.** `screens/*.js` 가 화면 ID 마다
  한 파일이고 `kit.js` 가 공통 부품과 `NAV` 를, `spec.js` 가 규격을 갖는다. `03_Wireframe_Definition.md`
  는 규칙·필드를 적고 **배치는 적지 않는다** — 화면을 만들 때 `.md` 만 읽으면 배치를 유추하게 된다.
  `docs/phase5/07` §2.1 이 이 사고를 기록했다.
- **`06` 은 공개본을 만들지 않는다.** 내부 어휘 비중이 크고 값이 표가 아니라 산문에 있어
  `verify_output.js` 를 지날 수 없다. 원본보다 공개본이 하나 적은 **비대칭은 의도한 것**이다.
  개수를 여기 적지 않는다 — 예전에 `원본 8종 · 공개본 7종` 이라 적어 두었는데 둘 다 틀렸다(§6).
- 공개본에 저장소 통제 어휘를 내보내지 않는다. **금지 어휘 목록과 판정은 `verify_output.js` 가
  갖는다** — 여기에 목록을 베끼지 않는다. 한 건이라도 남으면 `exit 1` 이다.

## 4. 디렉터리 경계

```text
docs/baseline/      봉인된 계약 00~06 (+ 07 예정) · output/
docs/phase4/        Phase 4 **기록** — plans/ · 재봉인 이력. 계약은 여기 없다
docs/phase5/        Phase 5 작업 공간. 07 을 여기서 쓰고 완성되면 입주시킨다
docs/redesign/      계약 재설계 인계문서. Phase 4 도 5 도 아닌 작업이 여기 산다
database/           Phase 4 DB 의 SQL·스크립트·테스트·증거   → database/AGENTS.md
winforms/           C# WinForms. Phase 5 의 구현 대상
tools/docgen/       산출물 생성기. 커밋한다
```

`docs/phase4/plans/**` 는 죽은 기록이 아니다 — `database/tools/verify-docs.js` 가 매 회귀에서 읽고
판정한다. 지우거나 옮기지 않는다.

## 5. 이름은 계층마다 다르고, 그 대응은 한 곳에만 있다

R4 로 DB 계약이 한글이 되었고 화면·C# 계층은 영문을 유지한다(`05` §16.2).
`00`·`01`·`03` 이 `PatientId` 라 부르는 것을 `04`·`05`·`06` 은 `수검자ID` 라 부른다.

대응표의 **단일 출처는 `05` §16.5** 다. 다른 곳에 같은 표를 만들지 않는다 —
`verify-docs.js` 의 `V20` 이 그 표를 양방향으로 지킨다.

## 6. 같은 값을 두 곳에 두지 않는다

이 저장소에서 실제로 두 번 물린 함정이다.

- `06` 이 기준선 SHA-256 을 복사해 두고 "게이트와 일치한다" 고 적었는데, R4 재봉인 뒤 그 문장이
  거짓이 되었다. 지금은 값을 빼고 게이트를 가리킨다.
- 산출물 개수·Test 건수도 마찬가지다. 세는 곳을 하나로 두고 나머지는 그것을 가리킨다.

수치나 해시를 문서에 적고 싶으면 **그 값이 게이트와 일치하는지 보는 검사를 함께 만든다.**
검사를 만들 수 없으면 적지 않는다.
