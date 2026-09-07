# Phase 4 인계 — SP 전면 한글화 · 기준선 04·05 재봉인 · 00~05 산출물

작업 디렉터리는 `D:\AIDEV\HealthCheckupReservationReception\database` 다.
`database/CLAUDE.md` 를 먼저 읽어라. 이 문서는 그 위에 얹는 것이지 대체하지 않는다.

---

## 1. 지금 참인 것

```text
브랜치        phase4-database
마지막 커밋   4ae40f6  fix(phase4): C·D 네 건 해결 — 기준선을 한 글자도 열지 않았다
작업트리      clean
```

DB 구현은 **끝났고 검증됐다.** 두 회차 모두 최종 코드다.

```text
창 안  2026-09-07 12:19   exit 0 · PASS 357 · FAIL 0 · SKIP 2  · NOT RUN 1
창 밖  2026-09-08 03:05   exit 0 · PASS 185 · FAIL 0 · SKIP 78 · NOT RUN 9

Table 6 · TVF 4 · SP 16 · Sequence 1 · PK6 FK2 UQ2 UX1 NCI5 · CHECK 24 · Default 8
Parameter 99 (SP·순번·이름·타입 4-튜플 EXCEPT 양방향) · 계약 시나리오 110종
CON-001~008 8/8 · verify-docs 21/0 · Test ID 카탈로그 247 · 계획서 미체크 0
```

`SKIP 2` 는 `OFF-309-01`·`02` 로, 업무시간 안에서는 정의상 `309` 가 나올 수 없어 창 밖 회차가 판정한다.
`NOT RUN` 은 `RBD-001`(인스턴스가 1개라 잘못된 서버명 `50020` 을 발화시킬 수 없다) 하나다.

`docs/phase4/06_DB_Transaction_Security_Seed.md` 는 **v1.0 `FINAL / GO`** 이고
`artifacts/reports/phase4-report.md` 가 Gate 판정과 Phase 5 인계 13항을 담고 있다.

---

## 2. 이번에 할 일 — 네 단계, 이 순서로

### ① SP 전면 한글화

`USP_HC_` **접두사는 유지**하고 그 뒤를 한글로 바꾼다. 파라미터와 Result Set 컬럼까지 전부다.

```sql
-- 목표 형태
CREATE OR ALTER PROCEDURE [dbo].[USP_HC_수검자_등록]
(
    @차트번호자동발급 BIT,
    @차트번호         NVARCHAR(100),
    @성명             NVARCHAR(100),
    @주민번호         VARCHAR(13),
    ...
)
AS
BEGIN
    ...
    SELECT
          CAST(@Success AS BIT)  AS [처리결과]
        , CAST(@Code    AS INT)  AS [결과코드]
        ...
END
```

**접두사를 유지하는 이유는 취향이 아니다.** 개수를 세는 게이트 다섯이 전부
`name LIKE 'USP[_]HC[_]%'` 로 계약 SP 를 골라낸다 — `SCH-014` · `VER-003` · `SCH-019` ·
`RBD-004` 지문 · `tests/14` 의 `PARAMETERS` 덤프. 개발 전용 `DEV_완료이력_등록` 이
계약 밖임을 구분하는 근거도 이것이다.

### ② 기준선 04·05 재봉인

**`02`·`03` 은 열지 않는다.** SP 이름이 `02_Function_Definition.xlsx` 에 0건,
`03_Wireframe_Definition.md` 에 0건임을 확인했다. 여는 것은 두 파일뿐이다.

```text
04_DB_Design.md          §3.3 명명규칙
05_DB_Rule_SP_Contract.md §1.6 Parameter·Result 네이밍 + §7~§13 SP 계약 전문
```

절차는 `database/CLAUDE.md` §2 와 R3 선례 그대로다.

```text
1  docs/baseline/04_DB_Design.md · 05_DB_Rule_SP_Contract.md 수정
2  scripts/verify-baseline.sh 의 그 두 SHA-256 교체 (스크립트에 하드코딩돼 있다)
3  **같은 커밋**으로 묶는다 — 게이트가 red 인 커밋을 남기지 않는다
4  CLAUDE.md §2 에 이 예외(R4 한글화)를 R3 항목과 같은 형식으로 추가
5  기준선 버전·태그·§0 이력을 함께 올린다 (제안: R4 · 태그 baseline-HC-RSV-RCP-2026MMDD-R4)
```

### ③ 구조 확정 후 C# 호출 사전 검수

한글화가 끝나면 테이블 구조와 SP 를 **확정**하고, C# 이 부를 때 문제가 없는지 검증한다.
`SqlParameter("@성명", ...)` · 명명인수 · `DataReader` 컬럼명이 대상이다.

### ④ 00~05 최종 검증 + 산출물 문서화

문서가 실제 구조와 맞는지, 바뀐 구조가 반영됐는지 최종 대조한 뒤
`docs/baseline/output/` 에 사람이 보기 좋은 형태로 낸다.

```text
현재 output   00_…업무정책.xlsx · 01_…업무프로세스.pptx
              02_…기능정의.xlsx · 03_…화면설계서.pptx
없는 것       04 · 05 → tools/docgen/build_04.js · build_05.js 를 **신설**한다
```

`04` 는 테이블 6 · 컬럼 48 · 제약 24 · 인덱스 · FK · 명명규칙,
`05` 는 SP 16 · Parameter 99 · Result Set · ResultCode · Rule 이라 둘 다 표가 많다 — `xlsx` 가 맞다.

---

## 3. 착수 전 첫 산출물 — 매핑표. **사용자 승인 없이 코드를 건드리지 마라**

파라미터 99개와 Result Set 컬럼 전건의 한글 이름을 **표로 먼저 만들어 승인받는다.**
분량이 130개를 넘고, `수검자`·`예약접수` 컬럼과 짝이 없는 이름들은 새로 지어야 한다.

```text
짝이 있는 것    @PatientId → @수검자ID · @ChartNo → @차트번호 · @SocialNumber → @주민번호
짝이 없는 것    @AutoChartNo · @ConfirmSimilarPatient · @AexOpt01Selected~07 · @OperatorName
                @LastEditDate · @RowVersion · @Scope · @CanSave · @BlockCode · @SeatsLeft
                @CutoffPassed · @Eligible · @ReasonCode · @OtherWorkId  …
```

표의 출처는 `docs/baseline/05_DB_Rule_SP_Contract.md` §7~§13 과
`docs/phase4/06_DB_Transaction_Security_Seed.md` §18 SP Matrix 다.

---

## 4. 절대 어기면 안 되는 것

**기대값은 구현이 아니라 재봉인된 `05` 에서 도출한다.**
구현을 읽어 `tools/expected-contracts.json` 을 돌려 만들면 게이트가 구현과 **같이 틀린다.**
이번 프로젝트에서 실제로 그렇게 살아남은 결함이 있다 — `USP_HC_SELECT_수검자상세` 의 RS1 이
계약보다 한 컬럼 모자랐는데 `expected-contracts.json` 도 같은 오류를 담고 있어
계약 시험 110종이 전부 green 이었다. `06` §44.8 에 등재돼 있다.

그 밖에 `database/CLAUDE.md` 가 정한 것들 — `.sql` 은 UTF-8 with BOM,
한글·기호 리터럴에 `N` 접두사, `sqlcmd` 는 항상 `-b -I -u`,
허용 T-SQL 목록은 `06` §9.2, 실행하지 않은 검증을 `PASS` 로 적지 않는다.

**커밋 전에 `git status --short` 로 삭제·이름변경을 먼저 본다.**
이 프로젝트에서 배포 파일이 깨진 채 커밋된 적이 있다 — 최종 diff 를 보지 않고
`git add database` 로 통째로 담은 결과였고, 회귀 한 번이면 그 자리에서 잡혔다.

---

## 5. 이미 실측했다 — 다시 하지 마라

```text
한글 식별자   파라미터 · 지역변수 · 별칭 · 명명인수(@이름 = …) · 대괄호 없는 정규식별자
              메타데이터(sys.parameters) · 오류 메시지  전부 정상
식별자 예산   sysname = nvarchar(128) → **128 글자**. 한글이 손해 보지 않는다
              (126글자 = 252바이트여도 LEN 은 126)
진짜 함정     식별자가 아니라 **N 없는 리터럴**이다.
              N 없이 쓰면 한글은 CP949 에 있어 살아남고 —(U+2014) 만 조용히 ? 가 된다.
              exit code 0 · PASS 건수 그대로라 실행으로는 드러나지 않는다. V16 이 정적으로 잡는다
applock       자원명은 **바이트 비교**인데 DB 는 CI·폭무시다. 자원명에 UPPER 를 이미 넣었다
02 · 03       SP 이름 0건 → 재봉인 대상 아님
```

한글화가 왜 지금까지 안 됐는지도 기록에 있다 — `docs/phase4/plans/09-korean-column-rename.md` §2.1.
컬럼 한글화 때 *"SP·Result Set·ResultCode 계약이 통째로 언 채 물리 스키마만 바뀌도록"* 경계에서 멈춘 것이다.
이번에는 그 경계를 넘는 것이므로 그 문서가 경고한 **"이름이 세 갈래인데 글자가 같다"** 함정을 다시 읽어라.

---

## 6. 되돌림

착수 전에 현재 커밋에 태그를 박는다. 실패하면 여기로 돌아온다.

```bash
git tag phase4-db-final-english 4ae40f6
# 되돌릴 때
git reset --hard phase4-db-final-english
```

브랜치는 `phase4-database` 를 그대로 쓴다. 태그는 이미 4개 있고 관례와 맞는다.

---

## 7. 검증 방법

전체 회귀는 `./scripts/test.sh` 하나다. 두 번 돌려야 한다.

```text
창 안 (월~토 11:10~15:50)   Write SP 성공 경로 · CON-001~008 · CWR-006/009
창 밖                        OFF-308 · OFF-309-01·02
```

시각 의존 경로는 **머신 시각을 옮겨** 검증한다. 코드를 고치는 우회는 금지다 —
`rebuild` 가 매 시나리오마다 TVF 를 되돌리므로 성립하지 않고, 배포 원본을 고치면
**다른 제품을 시험한 `PASS`** 가 된다. 상대 이동(`Set-Date -Adjust`)으로 옮기고
같은 크기로 되돌리면 오차가 0 이다. 이 PC 는 NTP 에 붙어 있지 않아 자동 복구되지 않는다.
**시각 변경은 사람이 한다.** 되돌리지 못한 채 죽는 자동화를 만들지 마라.
시계를 옮긴 뒤에는 셸과 SQL Server 양쪽을 확인하라 — 옮긴 직후 되돌아간 적이 있다.

---

## 8. 완료 조건

```text
[ ] 매핑표 사용자 승인
[ ] SP 16개 · Parameter 99 · Result Set 전건 한글화
[ ] 04 · 05 재봉인 (파일 + verify-baseline.sh 해시 2개를 같은 커밋에)
[ ] verify-baseline 6/6 · verify-docs 전건 · contract 0 · clean-rebuild 0 · red-probe 0
[ ] 창 안 회귀 exit 0 · FAIL 0 · 창 밖 회귀 exit 0 · FAIL 0
[ ] C# 호출 사전 검수 결과 기록
[ ] docs/baseline/output/ 에 00~05 여섯 종
[ ] 06 · phase4-report.md 갱신 (SP 이름·Parameter·Result Set 전건)
```

## 9. 확인하지 않은 것

- **C# 실제 호출은 검증한 적이 없다.** WinForms 코드에 DB 식별자 참조가 0건이라
  (`plans/09` §1.2 실측) 지금까지 확인할 대상이 없었다. ③ 단계에서 처음 한다.
- **`build_04.js`·`build_05.js` 는 존재하지 않는다.** 신설이 맞는지 산출물 형식과 함께
  착수 전에 사용자에게 한 번 더 확인하라 — 분량이 크다.
- `docs/baseline/output/` 은 `.gitignore` 대상이라 **커밋되지 않는다.**
  재현 수단은 `tools/docgen` 뿐이므로 산출물을 고치려면 생성기를 고쳐 다시 돌린다.
