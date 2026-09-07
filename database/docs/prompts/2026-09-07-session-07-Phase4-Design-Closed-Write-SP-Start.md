# Phase 4 DB — 설계 문서 전부 닫힘. 다음은 Write SP 구현이다

**작업 루트:** `D:\AIDEV\HealthCheckupReservationReception\database`
**브랜치:** `phase4-database` **HEAD:** `3a0c8bc`
**먼저 읽을 것:** `database/CLAUDE.md` (경계·sqlcmd 규약·컴파일 함정), `docs/baseline/04_DB_Design.md` §0.2 (R3 개정 요약)

---

## 1. 지금 참인 것

기준선 6종(`docs/baseline/00`~`05`)과 공개 산출물 4종(`docs/baseline/output/`)이 서로 정합하다. 설계 문서 쪽에 열린 불일치는 없다.

배포 스키마(실측):

```text
Table 6 · 컬럼 48 · PK 6 · FK 2 · UQ 2 · UX 1 · NCI 5
CHECK 23 · DEFAULT 8 · Trigger 0 · Sequence 1 · TVF 4
SP 계약 16개 / 배포 7개 (SELECT 계열만)
```

테이블: `수검자` `예약접수` `검사코드` `휴무일` `완료이력` `변경이력`

## 2. 마지막으로 통과한 검사 (`3a0c8bc` 시점)

```text
verify-baseline      6/6
verify-docs          18/0
verify-schema-doc    5/0
verify-contract-all  FAILED=0
verify-winforms      rc 0
inspect              rc 0
rebuild + 테스트     94 PASS / 1 FAIL
```

**`SCH-014` 1건이 red이고 이것은 예상된 것이다.** SP 16개를 기대하는데 배포가 7개다. 아래 3절을 끝내면 green이 된다. 다른 red가 생기면 그것은 새 결함이다.

재현: `./scripts/rebuild.sh` 후 `tests/00_Test_Harness.sql` → `01` → `02` → `00b` → `03` → `04` 순서로 실행. `scripts/test.sh`는 아직 없는 테스트 파일을 참조하므로 통째로 돌리면 실패한다.

## 3. 남은 작업

| # | 대상 | 계약 | 계획서 |
|---|---|---|---|
| 1 | `USP_HC_INSERT_수검자` (T23) | `05` §10.1 | `docs/phase4/plans/04-patient-write-procedures.md` |
| 2 | `USP_HC_UPDATE_수검자정보` (T24) | `05` §10.2 | 같음 |
| 3 | `USP_HC_SELECT_변경이력` (SP-LOG-01) | `05` §8.3 | 계획서 없음 — 계약만 있다 |
| 4 | 예약·접수 Write SP 6개 (T25~T29) | `05` §11·§12 | `plans/05`·`plans/06` |
| 5 | `tools/expected-contracts.json` + `tests/contract/` 계약 시험 | `06` §42 `G09` | `plans/08` |
| 6 | `deploy/08_Security.sql` · `09_Verify.sql` | `06` §39·§40 | `plans/07`·`plans/08` |

`deploy/05`~`09`는 현재 placeholder(각 100~122 B)다.

## 4. 구현 전에 반드시 알아야 할 결정 6가지

이것을 모르고 계획서만 따르면 깨진다.

**① `수검자.생년월일`·`성별`은 `PERSISTED` 계산열이다** (`04` §8.1.2). `주민번호`에서 DB가 유도한다. `INSERT`/`UPDATE` 컬럼 목록에 넣으면 **`Msg 271`**이다. SP는 후보검색(`Code=203` "이름+산출 Birthday 동일") 용으로만 `@Birthday`를 변수로 파생한다. 저장하지 않는다.

**② 주민번호 7번째 자리는 `1`~`8`만 허용**한다(`CK_수검자_SOCIAL_FORMAT`). SP가 `9`·`0`을 통과시키면 계산열 `NOT NULL`이 **`Msg 515`**를 내고 ResultCode가 아니라 예외로 튄다. SP에서 `Code=101`로 먼저 거부해야 한다. `plans/04` §공통 블록은 이미 고쳐 두었다.

**③ `변경이력`은 clean-create 대상이 아니다** (`04` §8.6.3, `CLAUDE.md` §8). `01_Schema.sql`의 `DROP` 목록에 없고 `IF OBJECT_ID(...) IS NULL` 가드로 만든다. `Deploy.sql` 재실행에도 보존된다. 물리 테이블 6개 중 유일한 예외다.

**④ `변경이력.대상키`는 `NOT NULL`이고 `대상테이블`은 `{N'수검자', N'예약접수'}` 둘뿐**이다. `완료이력`은 쓰는 Write SP가 0개라 제외했다. 감사 INSERT는 `@@TRANCOUNT = 0` 지점에서 자체 `TRY/CATCH`로 하고 실패해도 업무 결과를 바꾸지 않는다(`04` §8.6.5).

**⑤ 검사구성은 CSV 컬럼 2개**(`국가검사항목`·`추가검사항목`)다. `검사항목` 테이블은 없다. `예약접수.국가검사항목`의 **빈 문자열이 `CORRUPT-2`(저장 NEX 0)의 유일한 표현**이고 판정식은 `LEN(...) = 0`이다. 전개는 양끝 패딩 `LIKE` + `검사코드` 조인이다(`06` §9.2 허용, `deploy/03_Functions.sql:218` 참고).

**⑥ DB 정렬이 `Korean_Wansung_CI_AS`라 `[A-Z0-9]`가 소문자를 통과시킨다**(실측). `CK_..._EXAM_FORMAT`은 "A-Z0-9만"을 뜻하지 않는다. 전각은 `DATALENGTH = LEN`으로 닫아 두었다. `COLLATE`는 `06` §9.2 허용목록 밖이라 소문자는 열려 있고, 실질 방어는 저장 SP와 `SEC-004`/`SEC-005`다.

## 5. 계획서를 그대로 믿지 말 것

`plans/03`·`05`·`06`·`08`은 머리에 배너가 있다 — *"이 계획서의 스키마 참조는 `plans/09`·`plans/10`이 교체했다"*. 그 문서들의 SQL 본문은 **삭제된 `검사항목` 테이블과 `ExamSourceCode` 컬럼을 아직 서술한다.** 실행하면 깨진다.

**구현의 기준은 `05` 계약과 `06` 스펙이다.** 계획서는 Test ID 카탈로그와 `[X]` 리뷰 노트를 얻는 용도로만 쓴다. `plans/04`(T23·T24)만 착수 차단 결함을 제거해 두었고 나머지는 해당 Task 시점에 다시 써야 한다.

`06` §21의 CORRUPT 검증 SQL은 CSV 형태로 다시 써 두었다(§21 참조).

## 6. 경계

- `../winforms/`는 **예외 없이 읽기만** 한다. `./scripts/verify-winforms-unchanged.sh`가 exit 1을 낸다.
- `docs/baseline/`은 읽기 전용이다. 고치려면 **파일과 `scripts/verify-baseline.sh`의 SHA-256을 같은 커밋에** 넣는다. 못 묶으면 되돌린다.
- ROOT `tools/`와 `docs/baseline/output/`은 **2026-09-07에 이 계열이 인수**했다(`CLAUDE.md` §3). `tools/`는 커밋하고 `output/`은 `.gitignore` 대상이라 커밋되지 않는다.
- **산출물을 고치려면 생성기를 고쳐 다시 돌린다.** `build_02.js`만 원본을 읽고 나머지 셋은 내용이 JS에 하드코딩돼 있다. 기준선을 고쳤다고 산출물이 따라오지 않는다.
- `wireframe/build.js`만 `pptxgenjs`를 bare `require` 하므로 `NODE_PATH='D:/tmp/hcwork/gen/node_modules'`가 필요하다(실측).

## 7. sqlcmd 규약 (틀리면 조용히 깨진다)

```text
sqlcmd -S '.\SQLEXPRESS' -E -d HealthCheckupReservationReceptionDb -b -I -u
```

`-b` 없으면 exit code가 안 나온다. `-I`(`QUOTED_IDENTIFIER ON`) 없으면 필터형 인덱스가 있는 `검사코드`의 DML이 `Msg 1934`로 실패한다. 로그는 UTF-16이라 `iconv -f UTF-16 -t UTF-8`로 읽는다.

`.sql`은 **UTF-8 with BOM**이어야 하고(`head -c 3 | od -An -tx1` → `ef bb bf`), 한글·기호를 담는 `PRINT`·`THROW` 리터럴에는 **`N` 접두사**를 붙인다. `—`(U+2014)·`–`(U+2013)는 `.sql`에 쓰지 않는다. 자세한 것은 `CLAUDE.md` §5·§6·§11.

## 8. 확인하지 않은 것

- **`SP-LOG-01`은 계약만 있고 구현·시험·계획서가 없다.** `05` §8.3을 읽고 새로 쓴다. `IX_변경이력_TARGET`은 배포돼 있다.
- **`plans/05`~`08`의 R3 정합은 검사하지 않았다.** 해당 Task 착수 전에 `plans/04`에 한 것과 같은 점검이 필요하다.
- `06` §12·§21의 일부 서술이 R2 영문 컬럼명을 남기고 있을 수 있다. `V15`는 수치만 보고 식별자는 계획서 SQL(`V14`)만 본다.
- `winforms/`의 `.cs` 6개는 전부 스캐폴딩이다. 화면 구현은 시작되지 않았다.

## 9. 첫 행동

`docs/baseline/05_DB_Rule_SP_Contract.md` §10.1과 `docs/phase4/plans/04-patient-write-procedures.md`를 읽고 `T23` `USP_HC_INSERT_수검자`를 `deploy/05_Procedures_Patient_Write.sql`에 쓴다. 계산열 때문에 `05` §10.1 검증순서의 *"Birthday/Gender 산출 → 저장"*에서 **저장 쪽이 빠진다.**

끝나면 `./scripts/rebuild.sh` → 위 6개 테스트 → 게이트 6종을 돌리고, `SCH-014` 외에 새 red가 없는지 확인한다.
