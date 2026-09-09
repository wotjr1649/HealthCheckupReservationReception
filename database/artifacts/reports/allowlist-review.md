# G13 (c) — 스펙 §9.2 허용목록 준수 검토

`verify-tsql-allowlist.sh` 는 **금지 블랙리스트**만 본다. §9.2 는 허용목록이므로 "목록에 없는 것을
쓰지 않았다" 는 그 스캔으로 증명되지 않는다 — 그래서 G13 `(c)` 는 자동 판정 불가이고 `REVIEWED` 다.
이 파일은 그 `REVIEWED` 가 무증거 자기선언이 되지 않게 하는 근거이며, `node tools/allowlist-review.js`
로 다시 만든다. 허용목록 자체는 06 §9.2 가 단일 출처이고 여기에 베끼지 않는다.

## 1. 대상 파일

| 파일 | 줄 |
|---|---:|
| `Deploy.sql` | 18 |
| `Rebuild.sql` | 62 |
| `deploy/00_Preflight.sql` | 50 |
| `deploy/01_Schema.sql` | 375 |
| `deploy/02_Seed.sql` | 101 |
| `deploy/03_Functions.sql` | 258 |
| `deploy/04_Procedures_Select.sql` | 1025 |
| `deploy/05_Procedures_Patient_Write.sql` | 719 |
| `deploy/06_Procedures_Reservation_Write.sql` | 1029 |
| `deploy/07_Procedures_Reception_Write.sql` | 759 |
| `deploy/07a_Procedures_Holiday.sql` | 404 |
| `deploy/08_Verify.sql` | 66 |
| `tests/00_Test_Harness.sql` | 194 |
| `tests/00b_Test_Harness_RCP.sql` | 66 |
| `tests/01_Schema_Tests.sql` | 419 |
| `tests/02_Seed_Tests.sql` | 167 |
| `tests/03_Rule_Tests.sql` | 351 |
| `tests/04_Select_SP_Tests.sql` | 70 |
| `tests/05_Patient_Write_Tests.sql` | 323 |
| `tests/06_Reservation_Write_Tests.sql` | 452 |
| `tests/07_Reception_Write_Tests.sql` | 437 |
| `tests/08_Rollback_Tests.sql` | 270 |
| `tests/09_Concurrency_Setup.sql` | 114 |
| `tests/10_Concurrency_Session_A.sql` | 113 |
| `tests/11_Concurrency_Session_B.sql` | 72 |
| `tests/12_Concurrency_Verify.sql` | 116 |
| `tests/14_Clean_Rebuild_Verify.sql` | 136 |
| `tests/15_Holiday_Tests.sql` | 177 |
| `tests/contract/01_공통업무상태.sql` | 4 |
| `tests/contract/02_수검자목록_조건없음.sql` | 4 |
| `tests/contract/03_수검자목록_ChartNo.sql` | 4 |
| `tests/contract/04_수검자목록_0건.sql` | 4 |
| `tests/contract/05_수검자목록_주민번호형식.sql` | 4 |
| `tests/contract/06_수검자상세_PatientId없음.sql` | 4 |
| `tests/contract/07_수검자상세_미존재.sql` | 4 |
| `tests/contract/08_수검자상세_정상.sql` | 6 |
| `tests/contract/09_수검자유효업무_0건.sql` | 6 |
| `tests/contract/10_수검자유효업무_2건701.sql` | 6 |
| `tests/contract/11_예약접수목록_날짜역전.sql` | 4 |
| `tests/contract/12_예약접수목록_조건없음.sql` | 4 |
| `tests/contract/13_예약접수목록_날짜범위.sql` | 4 |
| `tests/contract/14_예약접수상세_미존재.sql` | 4 |
| `tests/contract/15_예약접수상세_NEX0행701.sql` | 8 |
| `tests/contract/16_예약접수상세_정상.sql` | 8 |
| `tests/contract/17_예약가능정보_RowVersion단독.sql` | 6 |
| `tests/contract/18_예약가능정보_AEX_NULL.sql` | 6 |
| `tests/contract/19_예약가능정보_WALKIN_날짜불일치.sql` | 6 |
| `tests/contract/20_예약가능정보_휴무일.sql` | 6 |
| `tests/contract/21_예약가능정보_Scope_ALL.sql` | 6 |
| `tests/contract/22_예약가능정보_Scope_SLOT.sql` | 10 |
| `tests/contract/23_예약가능정보_Scope_EXTRA.sql` | 10 |
| `tests/contract/24_예약가능정보_Scope_SLOT_EXTRA.sql` | 10 |
| `tests/contract/25_예약가능정보_Scope_NONE.sql` | 10 |
| `tests/contract/CWR-000_fixture.sql` | 11 |
| `tests/contract/CWR-001_접수완료_미존재_WorkId.sql` | 4 |
| `tests/contract/CWR-002_접수완료_미래예약일.sql` | 9 |
| `tests/contract/CWR-003_접수완료_stale_RowVersion.sql` | 17 |
| `tests/contract/CWR-004_접수완료_CNR_Work.sql` | 9 |
| `tests/contract/CWR-005_접수완료_NEX0행.sql` | 9 |
| `tests/contract/CWR-006_접수완료_성공.sql` | 25 |
| `tests/contract/CWR-007_접수완료_이미_RCP.sql` | 7 |
| `tests/contract/CWR-008_접수완료_과거예약일.sql` | 17 |
| `tests/contract/CWR-009_접수완료_마감경과.sql` | 22 |
| `tests/contract/CWR-011_접수완료_Master역할불일치.sql` | 17 |
| `tests/contract/CWR-020_접수추가검사_동일집합.sql` | 7 |
| `tests/contract/CWR-021_접수추가검사_실제변경.sql` | 6 |
| `tests/contract/CWR-022_접수추가검사_전체해제.sql` | 7 |
| `tests/contract/CWR-023_접수추가검사_성별불충족.sql` | 7 |
| `tests/contract/CWR-024_접수추가검사_NEX중복.sql` | 10 |
| `tests/contract/CWR-025_접수추가검사_RSV상태.sql` | 6 |
| `tests/contract/CWR-026_접수추가검사_stale_RowVersion.sql` | 6 |
| `tests/contract/CWR-040_접수취소_RSV상태.sql` | 6 |
| `tests/contract/CWR-041_접수취소_stale_RowVersion.sql` | 6 |
| `tests/contract/CWR-042_접수취소_성공.sql` | 6 |
| `tests/contract/CWR-043_접수취소_이미_CNC.sql` | 7 |
| `tests/contract/CWR-044_CNC_예약변경_불가.sql` | 7 |
| `tests/contract/CWR-099_cleanup.sql` | 11 |
| `tests/contract/HOL-001_휴무일목록_정상.sql` | 6 |
| `tests/contract/HOL-002_휴무일목록_시작일_NULL.sql` | 5 |
| `tests/contract/HOL-003_휴무일목록_기간역전.sql` | 5 |
| `tests/contract/HOL-004_휴무일목록_구분_허용밖.sql` | 5 |
| `tests/contract/HOL-005_자체휴무일_등록_날짜중복.sql` | 6 |
| `tests/contract/HOL-006_자체휴무일_수정_법정공휴일.sql` | 7 |
| `tests/contract/HOL-007_자체휴무일_삭제_미존재.sql` | 5 |
| `tests/contract/OFF-308-01_업무일아님_예약등록.sql` | 38 |
| `tests/contract/OFF-309-02_업무시간밖_예약등록.sql` | 24 |
| `tests/contract/PWR-000_fixture.sql` | 10 |
| `tests/contract/PWR-001_신규등록_자동차트.sql` | 5 |
| `tests/contract/PWR-002_동일주민_동일이름.sql` | 6 |
| `tests/contract/PWR-003_동일주민_다른이름.sql` | 6 |
| `tests/contract/PWR-004_주민번호_12자리.sql` | 5 |
| `tests/contract/PWR-005_주민번호_없는날짜.sql` | 6 |
| `tests/contract/PWR-007_수동차트_NULL.sql` | 5 |
| `tests/contract/PWR-008_수동차트_중복.sql` | 6 |
| `tests/contract/PWR-009_유사후보_미확인.sql` | 7 |
| `tests/contract/PWR-010_유사후보_확인후등록.sql` | 6 |
| `tests/contract/PWR-013_주민번호_14자리_경계절단.sql` | 11 |
| `tests/contract/PWR-014_주민번호_비숫자.sql` | 6 |
| `tests/contract/PWR-020_수정_Noop.sql` | 12 |
| `tests/contract/PWR-021_수정_stale_RowVersion.sql` | 13 |
| `tests/contract/PWR-022_수정_이름변경.sql` | 12 |
| `tests/contract/PWR-023_주민번호변경_RSV보유.sql` | 13 |
| `tests/contract/PWR-024_차트번호변경_RSV보유.sql` | 12 |
| `tests/contract/PWR-025_주민번호_타수검자사용중.sql` | 12 |
| `tests/contract/PWR-026_수정_미존재_PatientId.sql` | 5 |
| `tests/contract/PWR-027_주민번호변경_RCP보유.sql` | 12 |
| `tests/contract/PWR-028_차트번호변경_RCP보유.sql` | 11 |
| `tests/contract/PWR-099_cleanup.sql` | 11 |
| `tests/contract/RWR-000_fixture.sql` | 19 |
| `tests/contract/RWR-001_신규예약_과거일.sql` | 5 |
| `tests/contract/RWR-002_신규예약_일요일.sql` | 6 |
| `tests/contract/RWR-003_신규예약_휴무일.sql` | 6 |
| `tests/contract/RWR-004_신규예약_토요일PM.sql` | 6 |
| `tests/contract/RWR-005_신규예약_WALKIN_날짜불일치.sql` | 6 |
| `tests/contract/RWR-006_신규예약_TGT_미달.sql` | 6 |
| `tests/contract/RWR-007_신규예약_AEX_성별불충족.sql` | 6 |
| `tests/contract/RWR-008_신규예약_성공.sql` | 5 |
| `tests/contract/RWR-010_신규예약_타유효업무1건.sql` | 6 |
| `tests/contract/RWR-011_신규예약_타유효업무2건.sql` | 6 |
| `tests/contract/RWR-012_신규예약_정원마감.sql` | 10 |
| `tests/contract/RWR-020_예약변경_변경없음.sql` | 7 |
| `tests/contract/RWR-021_예약변경_시간대만.sql` | 7 |
| `tests/contract/RWR-022_예약변경_AEX만.sql` | 6 |
| `tests/contract/RWR-023_예약변경_예약일만.sql` | 7 |
| `tests/contract/RWR-024_예약변경_시간대_AEX.sql` | 6 |
| `tests/contract/RWR-025_예약변경_stale_RowVersion.sql` | 6 |
| `tests/contract/RWR-026_예약변경_CNR_Work.sql` | 7 |
| `tests/contract/RWR-027_예약변경_미존재_WorkId.sql` | 4 |
| `tests/contract/RWR-028_예약변경_자기Work_오탐없음.sql` | 8 |
| `tests/contract/RWR-029_예약변경_TGT_비대상.sql` | 22 |
| `tests/contract/RWR-030_예약변경_정원유지.sql` | 12 |
| `tests/contract/RWR-031_예약변경_나이경계_AEX중복.sql` | 11 |
| `tests/contract/RWR-032_신규예약_타유효업무1건_재확인.sql` | 6 |
| `tests/contract/RWR-033_신규예약_타유효업무2건_재확인.sql` | 5 |
| `tests/contract/RWR-034_예약변경_NEX상한초과.sql` | 22 |
| `tests/contract/RWR-040_예약취소_미존재_WorkId.sql` | 4 |
| `tests/contract/RWR-041_예약취소_stale_RowVersion.sql` | 8 |
| `tests/contract/RWR-042_예약취소_이미_CNR.sql` | 9 |
| `tests/contract/RWR-043_예약취소_성공.sql` | 10 |
| `tests/contract/RWR-099_cleanup.sql` | 19 |
| `tests/contract/SEL-021_변경이력_정상.sql` | 17 |
| `tests/contract/SEL-022_변경이력_기록0건.sql` | 6 |
| `tests/contract/SEL-023_변경이력_TargetTable_허용밖.sql` | 5 |
| `tests/contract/SEL-024_변경이력_TargetTable_NULL.sql` | 4 |
| **합계 144개** | **9328** |

## 2. §9.2 허용목록 대조

행은 06 §9.2 허용 표에서 그대로 읽었다. `사용` 은 그 행의 백틱 토큰이 대상 파일에 나타난 파일 수다.

| §9.2 허용 항목 | 판정 | 근거 |
|---|---|---|
| `SEQUENCE`, `NEXT VALUE FOR` | 사용 | `SEQUENCE` 4개 파일 · `NEXT VALUE FOR` 1개 파일 |
| `TRY_CONVERT` | 사용 | `TRY_CONVERT` 4개 파일 |
| `THROW` | 사용 | `THROW` 25개 파일 |
| `ROWVERSION` | 사용 | `ROWVERSION` 5개 파일 |
| Filtered Index | 사용 | `UX_검사코드_AEX_CODE` 1개 파일 |
| `PERSISTED` 계산열 | 사용 | `PERSISTED` 1개 파일 |
| `sp_getapplock` / `sp_releaseapplock` | 사용 | `sp_getapplock` 5개 파일 |
| `HASHBYTES('SHA2_256', …)` | 사용 | `HASHBYTES('SHA2_256', )` 2개 파일 |
| `EXCEPT` / `INTERSECT` | 사용 | `EXCEPT` 5개 파일 · `INTERSECT` 1개 파일 |
| `VALUES` 행 생성자 | 사용 | `VALUES` 23개 파일 |
| Table Variable, CTE, `OUTER APPLY`, `CROSS APPLY` | 사용 | `OUTER APPLY` 1개 파일 · `CROSS APPLY` 5개 파일 |
| `TOP (n)` + `ORDER BY` | 사용 | `TOP (n)` 57개 파일 · `ORDER BY` 38개 파일 |
| `WHILE` 루프 | 사용 | `WHILE` 17개 파일 |
| 쉼표 구분 문자열 + 양끝 패딩 `LIKE` | 사용 | `LIKE` 23개 파일 |
| `OFFSET … FETCH`, `IIF`, `CONCAT`, `FORMAT` | 사용 | `OFFSET  FETCH` 3개 파일 · `FORMAT` 2개 파일 |
| `DBCC CHECKIDENT(…, RESEED, n)` | 사용 | `DBCC CHECKIDENT` 1개 파일 |
| `EXECUTE AS USER` / `REVERT`, `ALTER ROLE … ADD MEMBER` | 사용 | `ALTER ROLE  ADD MEMBER` 7개 파일 |
| `sys.dm_exec_describe_first_result_set(_for_object)` | 미사용 | `sys.dm_exec_describe_first_result_set(_for_object)` — 0건 |
| `WAITFOR TIME` | 사용 | `WAITFOR TIME` 2개 파일 |
| **예외적 허용 (SQL 2016 DDL 배관)** `CREATE OR ALTER`, `DROP … IF EXISTS` | 사용 | `CREATE OR ALTER` 8개 파일 · `DROP  IF EXISTS` 2개 파일 |
| `STRING_SPLIT`, XML 파싱, JSON 함수/타입, `OPENJSON`, `FOR JSON` — `04` §3.8·§15.5 | 사용 | `FOR JSON` 11개 파일 · `04` 26개 파일 |
| 비트마스크 — `04` §15.5 | 사용 | `04` 26개 파일 |
| `SESSION_CONTEXT`, `AT TIME ZONE`, `STRING_AGG`, `TRIM()`, `CONCAT_WS`, `TRANSLATE`, `DATEDIFF_BIG`, `COMPRESS`, `GREATEST`/`LEAST`, `GENERATE_SERIES`, 정규식 함수, 벡터 타입 — 2012 이후 기능 | 사용 | `AT TIME ZONE` 42개 파일 · `TRIM()` 6개 파일 |

사용 22 · 미사용 1 · 수동판단 0 (허용 행 23)

`[I]` **미사용은 위반이 아니다.** 허용목록은 "써도 되는 것" 이지 "써야 하는 것" 이 아니다.

## 3. 허용목록 밖 발견

블랙리스트 게이트가 보지 않는 항목을 여기서 직접 훑는다 — `verify-tsql-allowlist.sh` 의 BAN 에
**TVP 와 Trigger 가 없다**(실측). 그 구멍을 이 검토가 메운다.

| 항목 | 결과 |
|---|---|
| TVP (CREATE TYPE … AS TABLE) | **0건** |
| 업무 Trigger (CREATE TRIGGER) | **0건** |
| FK Cascade (ON DELETE/UPDATE CASCADE) | **0건** |
| CURSOR 선언 | **0건** |
| FOR XML | **0건** |
| 동적 SQL (EXEC( / sp_executesql) | **0건** |

## 4. 수동 판단이 필요한 행

백틱 토큰이 없어 기계가 판정하지 못한 행이다. 사람이 읽고 아래에 판정을 적는다.


## 5. 금지 목록

06 §9.2 금지 표는 0행이다. 자동 스캔은 `verify-tsql-allowlist.sh` 가,
그 BAN 에 없는 TVP·Trigger·FK Cascade 는 위 §3 이 본다.

## 6. 검토자·시각

| 항목 | 값 |
|---|---|
| 검토자 | Claude Opus 5 (세션 실행) |
| 시각 | 2026-09-10 00:39 KST |
| 생성 | `node tools/allowlist-review.js` |
| 대상 커밋 | (커밋 직전 트리) |
