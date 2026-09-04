# database/ — Phase 4 작업 경계

## 1. 이 디렉터리의 책임

Phase 4 DB 의 SQL·실행 스크립트·테스트·증거만 여기에 둔다.
C# 코드, 화면, 문서 본문은 이 디렉터리의 책임이 아니다.

## 2. 읽기 전용 경계

`../docs/baseline/` 와 `../winforms/` 는 **읽기만** 한다.
한 바이트라도 바뀌면 `./scripts/verify-baseline.sh` 와 `./scripts/verify-winforms-unchanged.sh` 가 exit 1 을 낸다.

## 3. 쓰기 허용 경계

`database/**` 와 `../docs/phase4/` 만 쓴다. 그 밖의 경로에 쓰지 않는다.

## 4. Source of Truth 우선순위

```text
00 → 01 → 02 → 03 → 04 → 05 → 06 CANDIDATE → SQL
```

앞 문서가 뒤 문서를 이긴다. SQL 과 기준선이 어긋나면 SQL 을 고친다.
계약(Table/Column/SP/TVF/Parameter/Result Set/ResultCode)은 바꾸지 않는다.

## 5. `.sql` 은 UTF-8 with BOM

BOM 이 없으면 sqlcmd 가 한글 객체명을 깨뜨려 `Msg 105/102` 구문오류가 난다(실측 확인).

```bash
head -c 3 <파일> | od -An -tx1     # ef bb bf 가 나와야 한다
```

## 6. sqlcmd 는 항상 `-b -u`

exit code 가 유일한 자동 판정 근거다. `-b` 없이 실행하지 않는다.
오케스트레이터 스크립트는 `set -e` 를 쓰지 않는다 — 실패한 그 줄에서 셸이 끝나 RC 수집도 로그 출력도 안 된다.

## 7. 허용 T-SQL 목록 = 스펙 §9.2

목록 밖 기능을 쓰지 않는다. `CURSOR`·`STRING_SPLIT`·`FOR XML`·JSON·TVP·Trigger·동적 SQL 은 전부 밖이다.
예외는 배포 배관 한정 `CREATE OR ALTER` 와 `DROP … IF EXISTS` 뿐이다.

## 8. `Deploy.sql` 은 매번 초기화한다

`01_Schema.sql` 이 FK 역순 `DROP IF EXISTS` 후 `CREATE` 하는 clean-create 방식이다.
재실행하면 스키마와 데이터가 항상 같은 상태로 돌아간다.

## 9. `DROP DATABASE` 는 `Rebuild.sql` 에만 있다

대상 DB 이름은 하드코딩 대괄호 식별자다. 변수·동적 SQL 로 만들지 않는다.
가드 `50020`~`50024` 가 호스트·인스턴스·컨텍스트·계약 밖 Table 을 먼저 확인한다.

## 10. 실행하지 않은 검증을 `PASS` 로 기록하지 않는다

실행 전에는 `PLANNED` / `NOT RUN` / `BLOCKED` 를 쓴다.
`SKIP` 은 `PASS` 가 아니다. 업무시간 밖 SKIP 은 `NOT RUN` 으로 남긴다.
