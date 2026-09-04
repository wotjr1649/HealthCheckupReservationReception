# Phase 4 — T08~T14b: Seed · Test Harness · Rule Function 4개

작업 디렉터리: `D:\AIDEV\HealthCheckupReservationReception\database`
작업 branch: **`phase4-database`**. 이미 체크아웃돼 있다
선행 세션: `database-4d` — R3 재봉인 4~8단계 완주 + 게이트 통과 후 결함 5건 회수
복원점: tag `baseline-HC-RSV-RCP-20260904-R3` · 실물 이전 상태는 `fdfb8e6`

---

## 1. 목표

**`docs/phase4/plans/02-seed-functions.md` 의 T08~T14b 를 구현한다.**

```text
T08   deploy/02_Seed.sql          검사코드 19행 · 휴무일 2행
T09   tests/02_Seed_Tests.sql     SED 11건 · SSN 6건
T10   tests/00_Test_Harness.sql   Fixture (T001~T020 · F001~F020) + CORRUPT 2종
T11   [dbo].[UFN_HC_일정확인]
T12   [dbo].[UFN_HC_검진대상확인]
T13   [dbo].[UFN_HC_국가검사구성]
T14   [dbo].[UFN_HC_추가검사확인]
T14b  tests/00b_Test_Harness_RCP.sql   RCP 상태 Work Fixture
```

`plans/03` (T15~) 로 넘어가지 않는다. TVF 4개가 완성되면 `SCH-013` 이 PASS 로 바뀌는 것이 이 구간의 끝이다.

## 2. 지금 상태 — 여기서 출발한다

R3 재봉인이 끝났다. 기준선 6개·스펙·계획 9개·게이트 도구·실물 스키마가 전부 R3 다.

```text
라이브 DB   HealthCheckupReservationReceptionDb (.\SQLEXPRESS)
            7 Table 한글 · 47컬럼 · PK 7 / FK 4 / UQ 2 / UX 1 / NCI 5 / CHECK 23 / DF 8
            SP 0 · TVF 0 · 전 테이블 0행
게이트      verify-baseline 6/6 · verify-docs PASS 17 / FAIL 0 · verify-winforms exit 0
tests/01    SCH 16 PASS · SCH-013(TVF 4)·SCH-014(SP 15) 만 FAIL — T14·T30 이전이므로 정상
placeholder deploy/02~09 는 아직 3줄짜리다. T08 이 02 를, T11~T14 가 03 을 채운다
```

`deploy/01_Schema.sql` 과 `tests/01_Schema_Tests.sql` 은 **R3 로 이미 배포·검증됐다.** 다시 만들지 않는다.

## 3. R3 가 이 구간에 미치는 것 — 계획서에 이미 반영돼 있다

| 항목 | R2 | R3 |
|---|---|---|
| 테이블명 | `MST_EXAM_ITEMS` 등 | `검사코드`·`휴무일`·`수검자`·`예약접수`·`검사항목`·`완료이력`·`변경이력` |
| 제외정보 | `INFO_PATIENT_EXAM_EXCLUSIONS` 테이블 | **`수검자.HepatitisBExcluded BIT`** — T10 은 `UPDATE … SET HepatitisBExcluded = 1` 로 심는다 |
| NEX-03 술어 | `NOT EXISTS(제외행)` | `t.[HepatitisBExcluded] = 0` — T13 의 CTE 가 `i.[HepatitisBExcluded]` 를 함께 뽑는다 |
| 상태값 | `CNL` | `CNR`(예약취소) / `CNC`(접수취소). T10 의 `F020` 은 **`CNR`** 이다 |
| Test Harness DELETE | 제외정보 포함 | **`변경이력`** 포함 |

`plans/02` 를 그대로 따르면 된다. **계획서가 실행의 단일 출처다.**

## 4. 실행 함정 — `database/CLAUDE.md` 를 먼저 읽고 아래를 더한다

`CLAUDE.md` 의 BOM · `-b -I -u` · `N` 접두사 · `set -e` 회피 · 허용 T-SQL 목록을 **먼저 읽는다.** 아래는 이번 세션에서 실측한 것이다.

| 함정 | 실측 |
|---|---|
| `.sql` 을 Node 로 쓸 때 | `fs.writeFileSync(p, s, 'binary')` 는 UTF-8 을 깨뜨린다. `Buffer.concat([Buffer.from([0xEF,0xBB,0xBF]), Buffer.from(s,'utf8')])` 로 쓴다 |
| sqlcmd 로그 | `-o` 로 받으면 **UTF-16**. `iconv -f UTF-16 -t UTF-8` 로 읽는다. 콘솔 리다이렉트는 CP949 라 한글이 깨진다 |
| 한글 리터럴을 `-Q` 로 | 명령줄 인코딩에 의존한다. 파일로 만들어 BOM + `-i` 로 넘긴다 |
| 필터형 인덱스 | `수검자`·`검사코드` 의 `INSERT`/`UPDATE`/`DELETE` 마다 `SET QUOTED_IDENTIFIER ON` 이 필요하다. sqlcmd `-I` 를 빠뜨리면 `Msg 1934` |
| Bash 도구의 heredoc | `\\` 를 `\` 로 줄인다. 긴 스크립트·마크다운은 **Write 도구**로 만든다 |
| `bash -c`·`powershell` 중첩 | 훅이 막는다. 스크립트는 `bash <파일>` 로 |
| `rm -rf` | 훅이 막는다. 사용자에게 `! rm -rf <리터럴 절대경로>` 를 요청한다 |

## 5. 게이트 — 매번 세 개를 돌린다

```bash
./scripts/verify-baseline.sh            # 6/6
node tools/verify-docs.js               # PASS 17 / FAIL 0
./scripts/verify-winforms-unchanged.sh  # exit 0
```

`V15` 가 이번에 신설됐다. **R2 수치·식별자 사본을 잡는다** — `FK 6`·`Parameter 87행`·`CNL`·`INFO_*`·R2 기준선 ID. 예외는 `tools/verify-docs.js` 안에 정확한 문자열로 고정돼 있으니, 정당한 R2 이력 서술을 새로 쓸 때만 거기에 더한다. 늘리기 전에 정말 필요한지 본다.

`V14` 는 계획 SQL 의 대괄호 식별자를 기준선 `04` §8 과 대조한다. **새 컬럼명을 계획에 쓰면 `04` 에 없을 때 FAIL 한다** — 그것이 의도다.

## 6. 확정된 것 — 재논의 대상이 아니다

| 결정 | 근거 |
|---|---|
| 테이블 7개 한글 · 컬럼 영문 PascalCase | `04` §3.3 |
| 상태 `CHAR(3)` 4값. `FIN` 없음 | `04` §8.2.3 · 초안 §3.5 |
| `완료이력` 존치 (테이블 7개) | 초안 §3.4 — 컬럼 하나로는 필터 뒤의 MAX 를 표현할 수 없다 |
| 제외정보 → `수검자.HepatitisBExcluded` | 초안 §4.1 — NEX-03 문언이 정확히 Boolean 이다 |
| `변경이력` 은 Write SP 8개가 명시 기록. Trigger 0 | `04` §8.7.4 |
| Test ID 카탈로그 242건 | `06` §45.2 — 유일한 출처. 계획은 건수를 다시 적지 않는다 |

## 7. 미검증으로 남긴 것 — 마주치면 이렇게 하라

| 항목 | 상태 | 대응 |
|---|---|---|
| 초안 §13 의 **비수치 서술 변경** | **미검증.** `V15` 는 수치만 잡는다 | 구현 중 계획서 서술이 R3 설계와 어긋나면 `04`·`05` 를 기준으로 계획을 고친다. 기준선이 이긴다 |
| 개정의 **의미 정확성** | 사람이 읽고 판단했다 | `01`:649→`CNR` / `01`:899→`CNC`, `02` 시트6 `E16`, `01`:761 Mermaid 인용부호. 근거는 커밋 메시지에 있다 |
| `docs/baseline/output/` 재생성 | **미수행.** 소유 세션 `a97cfb9f` 가 존재하지 않는다 | 이 계열의 작업이 아니다. ROOT `tools/` 를 읽지도 쓰지도 커밋하지도 않는다 (`CLAUDE.md` §3) |
| ROOT `tools/` untracked | 그대로 둔다 | 위와 같다. `git status` 에 계속 보인다 |

`02_Function_Definition.xlsx` 의 Excel 열림은 **2026-09-04 사용자 확인 완료**다. 시트 7개와 표 서식이 온전하다.

## 8. 절대 금지

```text
docs/baseline/** 수정                      ← R3 로 봉인됐다
deploy/01_Schema.sql · tests/01_Schema_Tests.sql 재작성  ← 이미 R3 배포·검증 완료
기존 tag 이동 / git reset · clean · checkout -- / force push
ROOT tools/ 와 docs/baseline/output/ 실행·수정·커밋
실제 주민등록번호 사용
게이트를 약화시켜 통과시키는 것 — V15 예외를 늘려 FAIL 을 지우는 것 포함
```

## 9. 끝나면

`plans/02` 의 각 Task 완료조건을 표로 보고하고 멈춘다. `plans/03`(T15~) 진입은 사용자가 정한다.
전체 진행 현황은 `docs/phase4/plans/2026-09-04-phase4-database-implementation.md` 에 있다.
