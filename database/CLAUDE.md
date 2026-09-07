# database/ — Phase 4 작업 경계

## 1. 이 디렉터리의 책임

Phase 4 DB 의 SQL·실행 스크립트·테스트·증거만 여기에 둔다.
C# 코드, 화면, 문서 본문은 이 디렉터리의 책임이 아니다.

## 2. 읽기 전용 경계

`../winforms/` 는 **예외 없이 읽기만** 한다. `./scripts/verify-winforms-unchanged.sh` 가 exit 1 을 낸다.

`../docs/baseline/` 도 기본은 읽기 전용이다. 한 바이트라도 바뀌면 `./scripts/verify-baseline.sh` 가 exit 1 을 낸다.

**단 하나의 예외 — R3 재봉인.** `docs/phase4/04_DB_Design_R3_DRAFT.md` §3 의 진행안 4·5·6단계는 기준선을 R2 에서 R3 로 교체하는 단계이며, 그 단계에 한해 **해당 단계가 지정한 기준선 파일과 `scripts/verify-baseline.sh` 의 SHA-256 을 같은 커밋에 묶어** 바꾼다.

```text
4단계  00_Project_Policy.md · 01_Process_Definition.md · 02_Function_Definition.xlsx  + 해시 3개
5단계  03_Wireframe_Definition.md                                                      + 해시 1개
6단계  04_DB_Design.md · 05_DB_Rule_SP_Contract.md                                     + 해시 2개
```

그 단계가 아니거나 목록 밖 파일이면 **여전히 쓰지 않는다.** 파일과 해시를 한 커밋 안에서 함께 끝내지 못하면 되돌린다 — 게이트가 red 인 커밋을 남기지 않는다.

## 3. 쓰기 허용 경계

`database/**` 와 `../docs/phase4/` 를 쓴다. `../docs/baseline/` 은 §2 의 예외 조건에서만 쓴다.
그 밖의 경로에 쓰지 않는다. `../winforms/` 는 §2 대로 예외 없이 읽기만 한다.

**ROOT `tools/` 와 `../docs/baseline/output/` 은 2026-09-07 사용자 승인으로 이 계열이 인수했다.**
원래는 세션 `a97cfb9f` 몫이었고 `04_DB_Design_R3_DRAFT.md:1903` 이 그 순서를 정해 뒀는데,
그 세션이 존재하지 않아 R3 재봉인 뒤 산출물이 R2 에 머물러 있었다. 이제 여기서 고치고 돌린다.

```text
tools/docgen/**              쓴다 · 커밋한다
docs/baseline/output/**      쓴다 · 커밋하지 않는다 (ROOT .gitignore 대상)
```

`output/` 은 `.gitignore` 에 있어 어떤 커밋에도 남지 않는다. **재현 수단은 `tools/docgen` 뿐이므로
산출물을 고치려면 반드시 생성기를 고쳐서 다시 돌린다** — 바이너리를 손으로 만지지 않는다.
`build_02.js` 만 원본(`02_Function_Definition.xlsx`)을 실제로 읽고, 나머지 셋은 내용이
`build_00.js` 배열과 `proc/slides/*.js`·`wireframe/screens/*.js` 에 하드코딩돼 있다.
그래서 기준선을 고쳤다고 산출물이 따라오지 않는다. 기준선을 열었으면 생성기도 함께 연다.

`wireframe/build.js` 만 `pptxgenjs` 를 절대경로 상수 없이 bare `require` 한다 —
`NODE_PATH='D:/tmp/hcwork/gen/node_modules'` 없이 실행하면 `MODULE_NOT_FOUND` 로 죽는다(실측 확인).

## 4. Source of Truth 우선순위

```text
00 → 01 → 02 → 03 → 04 → 05 → 06 → SQL
```

앞 문서가 뒤 문서를 이긴다. SQL 과 기준선이 어긋나면 SQL 을 고친다.
계약(Table/Column/SP/TVF/Parameter/Result Set/ResultCode)은 바꾸지 않는다.

## 5. `.sql` 은 UTF-8 with BOM

BOM 이 없으면 sqlcmd 가 한글 객체명을 깨뜨려 `Msg 105/102` 구문오류가 난다(실측 확인).

```bash
head -c 3 <파일> | od -An -tx1     # ef bb bf 가 나와야 한다
```

한글·특수문자를 담는 `PRINT`·`THROW` 리터럴에는 **`N` 접두사**를 붙인다.
없으면 varchar 리터럴이 되어 `Korean_Wansung`(CP949) 에 없는 문자가 `?` 로 깨진다 —
한글은 살아남고 `—`(U+2014) 같은 기호만 조용히 사라지므로 눈치채기 어렵다(실측 확인).
`master` 컨텍스트에서 도는 `Rebuild.sql` 은 데이터 정렬이 다를 수 있어 더욱 필수다.

**어느 문자가 위험한지 눈으로 고르지 마라.** 실측한 결과다.

```text
불가  —(U+2014)  –(U+2013)
가능  ―(U+2015)  ·(U+00B7)  →(U+2192)  §  …  ≥  한글 전체(확장 음절 포함)
```

`tests/01_Schema_Tests.sql` 은 한글 `PRINT` 를 `N` 없이 쓰고도 통과한다 — 한글이 CP949 에 있기 때문이다.
그 관행을 따라가다 `—` 하나를 섞는 순간 깨지는데 **exit code 는 0 이고 `PASS` 건수도 그대로다.**
실행으로는 절대 드러나지 않으므로 `V16` 이 이것을 검사한다(`node tools/verify-docs.js`).

## 6. sqlcmd 는 항상 `-b -I -u`

exit code 가 유일한 자동 판정 근거다. `-b` 없이 실행하지 않는다.

`-I`(`SET QUOTED_IDENTIFIER ON`)도 필수다. sqlcmd 는 SSMS 와 달리 **OFF** 로 접속하는데,
`검사코드` 에 필터형 인덱스(`UX_검사코드_AEX_CODE`)가 있어
**그 테이블의 `INSERT`/`UPDATE`/`DELETE` 가 `Msg 1934` 로 실패한다**(실측 확인).
인덱스를 만들 때만이 아니라 데이터를 바꿀 때마다 요구된다.
배포 `.sql` 은 자체적으로도 첫 배치에 `SET QUOTED_IDENTIFIER ON;` + `GO` 를 둔다 —
`SET` 은 parse 시점에 적용되므로 같은 배치 안에서는 소급되지 않는다.
오케스트레이터 스크립트는 `set -e` 를 쓰지 않는다 — 실패한 그 줄에서 셸이 끝나 RC 수집도 로그 출력도 안 된다.

## 7. 허용 T-SQL 목록 = 스펙 §9.2

목록 밖 기능을 쓰지 않는다. `CURSOR`·`STRING_SPLIT`·`FOR XML`·JSON·TVP·Trigger·동적 SQL 은 전부 밖이다.
예외는 배포 배관 한정 `CREATE OR ALTER` 와 `DROP … IF EXISTS` 뿐이다.

## 8. `Deploy.sql` 은 매번 초기화한다 — `변경이력` 하나만 빼고

`01_Schema.sql` 이 FK 역순 `DROP IF EXISTS` 후 `CREATE` 하는 clean-create 방식이다.
재실행하면 다섯 테이블의 스키마와 데이터가 항상 같은 상태로 돌아간다.

**`변경이력` 은 예외다.** 감사 기록은 배포로 지워지지 않아야 하므로 `DROP` 대상에서 빼고
`IF OBJECT_ID(...) IS NULL` 가드로 만든다(`04` §8.6.3 · `06` §8.1). 물리 테이블 6개 중
이 하나뿐이며, 나머지 다섯에는 `IF NOT EXISTS` 가드를 넣지 않는다.

이 가드가 옛 구조를 조용히 유지하는 드리프트는 `./scripts/verify-schema-doc.sh` 의
`DOC-001`~`DOC-005` 양방향 대조가 잡는다. 별도 검사를 추가하지 않는 이유다.
`Rebuild.sql` 은 DB 를 통째로 `DROP` 하므로 그 경로에서는 보존되지 않는다 — 개발 전용 진입점이다.

## 9. `DROP DATABASE` 는 `Rebuild.sql` 에만 있다

대상 DB 이름은 하드코딩 대괄호 식별자다. 변수·동적 SQL 로 만들지 않는다.
가드 `50020`~`50024` 가 호스트·인스턴스·컨텍스트·계약 밖 Table 을 먼저 확인한다.

## 10. 실행하지 않은 검증을 `PASS` 로 기록하지 않는다

실행 전에는 `PLANNED` / `NOT RUN` / `BLOCKED` 를 쓴다.
`SKIP` 은 `PASS` 가 아니다. 업무시간 밖 SKIP 은 `NOT RUN` 으로 남긴다.

## 11. 컴파일 함정 — 실행 전에는 안 보이고, 터지면 배치가 통째로 죽는다

**`PRINT` 는 스칼라 식만 받는다.** 인자에 하위 쿼리를 넣으면 `Msg 1046` + `Msg 102` 로
**그 배치 전체가 컴파일 실패**한다. 앞선 단언이 한 줄도 실행되지 않은 채 exit 1 이 나오므로
RED 시험이 우연히 통과한 것처럼 보인다. 이 프로젝트에서 세 번 나왔다(`00_Preflight` · `02_Seed` · `09_Verify`).

```sql
-- 금지
PRINT 'INFO n=' + CONVERT(VARCHAR(5), (SELECT COUNT(*) FROM [dbo].[검사코드]));
-- 사용
DECLARE @n INT = (SELECT COUNT(*) FROM [dbo].[검사코드]);
PRINT 'INFO n=' + CONVERT(VARCHAR(5), @n);
```

**`GO` 는 변수 경계다.** 앞 배치에서 `DECLARE` 한 `@Fail` 을 `GO` 뒤에서 쓰면 `Msg 137` 이다.
집계 변수를 쓰는 `THROW` 는 그 변수를 선언한 배치 안에 둔다. 파일 끝에 몰아 두지 않는다.

`Msg 102`(`',' 근처의 구문이 잘못되었습니다`)가 괄호와 무관한 줄을 가리키면 **괄호부터 센다.**
`CONVERT(CHAR(1), <식>` 처럼 닫는 괄호가 하나 모자라면, `CONVERT` 가 인자를 3개까지 받는 탓에
다음 컬럼이 style 인자로 먹히고 그 **다음** 쉼표에서 터진다(실측 확인).
