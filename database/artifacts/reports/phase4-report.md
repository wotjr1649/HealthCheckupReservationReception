# Phase 4 DB 구현 — 실행 보고서

- **작성:** 2026-09-07
- **대상:** `HealthCheckupReservationReceptionDb` · `.\SQLEXPRESS` · SQL Server 2025 Express `17.0.1125.2`
- **기준선:** `HC-RSV-RCP-20260904-R3`
- **계약서:** `docs/phase4/06_DB_Transaction_Security_Seed.md` v1.0 `FINAL / GO`
- **브랜치:** `phase4-database`

이 보고서는 **실행한 것만** 적는다. 실행하지 않은 것은 `NOT RUN` 으로 남기고 `SKIP` 을 `PASS` 로 세지 않는다.

---

## 1. 실행 요약

| 항목 | 값 |
|---|---|
| 물리 Table | 6 |
| Inline TVF | 4 |
| Stored Procedure | 16 (SELECT 8 / INSERT 2 / UPDATE 6 · DELETE 0) |
| Sequence | 1 |
| 제약 | PK 6 / FK 2 / UQ 2 / UX 1 / NCI 5 / CHECK 24 / Default 8 |
| SP Parameter | 99 (`EXCEPT` 양방향 일치) |
| Test ID 카탈로그 | 249 (`06` §45.2 단일 출처) |
| Result Set 계약 시나리오 | 110 파일 (창 안 회차 108건 판정 · 창 밖 회차 32건 — 나머지는 시각 배타성으로 SKIP) |

### 회귀 회차 — 시각 의존 경로가 배타적이라 둘 다 필요하다

| 회차 | 시각 | 업무시간 | exit | PASS | FAIL | SKIP | NOT RUN | 로그 |
|---|---|---|---:|---:|---:|---:|---:|---|
| `A` 창 안 | 2026-09-07 12:49 | 안 | 0 | **353** | 0 | 2 | **1** | `artifacts/logs/full_test_run.log` |
| `B` 창 밖 | 2026-09-08 01:11 | 밖 | 0 | **181** | 0 | 78 | 9 | `artifacts/logs/full_test_run_off.log` |

- 회차 `A` 의 `SKIP` 2건은 `OFF-309-01`·`02` 뿐이다 — 업무시간 안에서 `309` 는 **정의상** 나올 수 없고, 회차 `B` 에서 `PASS` 로 판정된다.
- 회차 `B` 의 `SKIP` 78건은 업무시간 밖이라 성립하지 않는 Write SP 성공 경로다. 회차 `A` 가 판정한다.
- 회차 `A` 는 사용자가 머신 시각을 6시간 뒤로 옮겨 만든 창이다. **코드는 한 글자도 바꾸지 않았다** — `SYSDATETIME()` 이 시각의 유일한 입구다. 끝난 뒤 같은 크기로 되돌렸다.
- 두 회차 모두 최종 코드로 돌렸다. 회차 `A` 의 `NOT RUN` 은 `RBD-001` 하나뿐이며 구조적으로 불가능하다.

---

## 2. Gate 판정 — `06` §42 요약

| Gate | 결과 | 근거 |
|---|---|---|
| G00 Baseline Hash | **PASS** | `verify-baseline.sh` 6/6 |
| G01 WinForms 보호 | **PASS** | 변경 0건 · R3 태그 diff 0줄 |
| G02 Preflight | **PASS** | `PRE-001`~`006` |
| G03 Clean Deploy | **PASS** | `RBD-003` exit 0 |
| G04 Object Inventory | **PASS** | `VER-001`~`004` · `RBD-004` 지문 |
| G05 Schema | **PASS** | `SCH-001`~`019` · `DOC-001`~`005` 양방향 |
| G06 금지 객체 | **PASS** | Trigger 0 / TVP 0 / DELETE SP 0 |
| G07 Seed | **PASS** | `VER-007` · `RBD-006` 역할 분포 |
| G08 Rule | **PASS** | `tests/03` 51건 |
| G09 SP Contract | **PASS** | 창 안 회차 계약 108건 전건 일치 · `SCH-019` Parameter 99 `EXCEPT` 양방향 · `V17` · `V18` |
| G10 Rollback | **PASS** | `RBK-001`~`007` · `RBK-008` 은 `V18` 정적 판정 |
| G11 Concurrency | **PASS** | `CON-001`~`008` 8/8 |
| G12 Security | **PASS** | 계정·권한은 **폐기** (2026-09-07 사용자 결정). 남은 `SEC-010` 은 매 회귀 PASS |
| G13 호환성 | **(a) PASS · (b) PASS · (c) REVIEWED** | 배포·시험 exit 0 · `.sql` 137개 0건 · (c) 자동 판정 불가 |
| G14 Repeatability | **PASS** | `RBD-005` diff 0줄 |
| G15 Evidence | **PASS** | run ID·시각·업무시간 기록 |
| G16 06 문서 | **PASS** | v1.0 `FINAL / GO` |

---

## 3. 실행 명령과 exit code

```text
./scripts/rebuild.sh                     0    DROP DATABASE + Deploy 전체
./scripts/deploy.sh                      0    Deploy 단독 (변경이력 보존 경로)
./scripts/test.sh                        0    전체 회귀 (아래를 전부 포함)
  tests/00·00b·01·02·03·04·05·06·07·08·14
  scripts/verify-contract-all.sh         0    계약 110종 (창 안 108 판정 / 창 밖 32 판정)
  scripts/concurrency-test.sh 1..8       0    창 안에서 8/8, 창 밖에서 exit 3 = NOT RUN
  scripts/clean-rebuild-verify.sh        0    RBD-002·003·005·007·008·009
  scripts/verify-no-secret.sh            0    SEC-010
  scripts/verify-tsql-allowlist.sh       0    G13-b
  scripts/verify-baseline.sh             0    G00
  scripts/verify-winforms-unchanged.sh   0    G01
  scripts/verify-schema-doc.sh           0    G05
  node tools/verify-docs.js              0    V01~V18 (20 판정)
./scripts/run-con-window.sh              0    CON 8종을 한 창에서
./scripts/make-summary.sh                0    artifacts/reports/test-summary.txt
```

`exit 3` 은 **NOT RUN** 이다 — 업무시간·접수마감 창 밖이라 실행하지 않았다는 뜻이며 `test.sh` 가 따로 센다.

---

## 4. Deviation

`06` §44 에 전문이 있다. 요약:

- `D4-001` `03` baseline hash 불일치 — Phase 4 가 소비하는 `04`·`05` 는 byte 일치라 DB 객체계약에 영향 없음
- `D4-002` Git repository 부재 → 해소
- `D4-004` G13 을 세 갈래로 분리 (실행 / 블랙리스트 / 허용목록 REVIEWED)
- `44.6` `05` §17.6 "허용되지 않은 7번째 자리" 는 구성 불가 — 기준선을 고치지 않고 Deviation 으로 기록
- `44.6a` `PWR-013` 14자리 주민번호는 SP 경계에서 구성 불가
- **`44.8` 실행검증 단계에서 새로 발견한 8건** — 전부 시험·게이트 결함이며 계약은 하나도 바뀌지 않았다

`44.8` 의 공통점은 **"게이트가 green 인데 아무것도 검증하지 않고 있었다"** 이다.
`OFF-308` 은 한 번도 실행된 적이 없었고, `CON-004` 는 필수값 오류로 끝나면서 PASS 였고,
`G13-b` grep 은 항상 FAIL 이라 무시되고 있었고, `G00`·`G01`·`G05` 는 회귀 밖에 있었다.

---

## 5. 알려진 한계

`06` §43 에 14건이 있다. 중요한 것:

- 시각 의존 경로(`CON-005`·`008`·`CWR-006`·`009`)는 특정 창에서만 성립한다. **머신 시각을 옮겨** 검증하며, TVF 상수를 고치는 우회는 금지다 — `rebuild` 가 되돌리고, 배포 원본을 고치면 다른 제품을 시험한 `PASS` 가 된다.
- `RBD-001`(잘못된 서버명 `50020`)은 인스턴스가 1개뿐이라 음성 시험이 구조적으로 불가하다.
- `G13(c)` 허용목록 준수는 자동 판정이 불가하다. 블랙리스트 0건을 준수 `PASS` 로 승격하지 않는다.
- 취소 계열 SP 는 `SLOT` 잠금을 잡지 않는다. 오차 방향이 보수적(과대)이라 무결성 위반이 아니다. **접수완료만** 과소집계가 가능해 잠금을 확장했고, `CON-008` 이 그 필요성을 실증했다.

---

## 6. 잔여 결함

**0건.** 회차 `A`·`B` 모두 `FAIL 0`. 계획서 체크박스 미체크도 **0**이다.

`NOT RUN` 은 셋이며 전부 이유가 기록돼 있다.

```text
RBD-001   잘못된 서버명(50020) 음성시험 — 인스턴스가 1개다. 두 번째 인스턴스 설치가 필요하다
G13 (c)   §9.2 허용목록 준수 — 자동 판정 불가. REVIEWED 로만 기록한다
          (블랙리스트 0건은 verify-tsql-allowlist.sh 가 별개로 매 회귀에서 PASS)
```

Security(계정·권한)는 `NOT RUN` 이 아니라 **폐기**다 — 미룬 것이 아니라 산출물이 아니므로
게이트에 잔여를 남기지 않는다. 남은 `SEC-010` 은 매 회귀에서 PASS 다.

---

## 7. Phase 5 인계

1. **App.config** 에 `HealthCheckupDb` 연결문자열 키를 추가한다. 통합인증(`Integrated Security=true`)을 쓴다 — 이 저장소는 자격증명을 파일에 두지 않으며 `SEC-010` 이 매 회귀에서 그것을 확인한다.
2. **계정·권한은 폐기됐다**(2026-09-07 사용자 결정). 그래서 화면은 배포 계정으로 붙고, 개발 환경에서 그것은 sysadmin 이다 — **"화면은 SP 만 호출한다" 는 강제되지 않는 관례**이며 6개 테이블 직접 DML 이 열려 있다. 실무 이관 시 `06` §32 의 설계를 되살려 반드시 닫아야 한다.
3. **화면은 SP 만 호출한다.** 테이블 직접 접근·동적 SQL 을 쓰지 않는다. RS0 `Code` 는 `05` §13 의 허용집합 안에서만 나온다(`tools/allowed-codes.json`).
4. **업무시간·접수마감은 DB 가 판정한다.** 화면이 자체 판정하지 않는다 — `308`/`309`/`304` 를 그대로 표시한다.
5. **동시성 오류 네 개를 화면이 구분해야 한다.** RS0 로 오는 것은 `601` 하나뿐이고 나머지 셋은 SQL 예외다.

   ```text
   601     다른 사용자가 먼저 변경했다   → 다시 읽고 재시도
   50001   applock 대기 5초 초과         → 그대로 재시도. **실제로 가장 흔하다**
   50002   applock 교착 victim           → 그대로 재시도. 전역 잠금순서로 구조적으로 드물다
   1205    엔진 교착 victim              → 그대로 재시도
   ```

   `50001`·`50002`·`1205` 는 기준선 `04`·`05` 에 없어 `DbCode` enum 에 대응 항목이 없다.
   Phase 5 가 `SqlException.Number` 로 직접 분기해야 한다 (`06` §20 · §25.1).
6. `변경이력` 은 배포로 지워지지 않는다(`Deploy.sql` 경로). `Rebuild.sql` 은 DB 를 통째로 지우는 **개발 전용** 진입점이다.
7. **Write SP 호출을 `SqlTransaction` 으로 감싸지 않는다.** SP 하나가 곧 한 업무단위다.
   감싸면 `Msg 50003` 으로 즉시 거절된다(`06` §21.1a). 바깥 트랜잭션은 doomed 가 되지 않으므로
   호출자가 자기 작업을 되돌리거나 커밋할 수 있다.
8. **Result Set 을 끝까지 읽는다.** 감사 기록(`변경이력`) INSERT 가 RS1 **뒤**에 있다(`06` §21.1 ③).
   `ExecuteNonQuery` 나 조기 `Dispose` 로 attention 이 나가면 업무는 커밋됐는데 감사만 없는 상태가 된다.
9. **`502`·`306` 은 RS0 만으로 화면을 만들 수 없다.**

   ```text
   502   현재 상태값(RSV/RCP/CNR/CNC)을 주지 않는다 → "이미 접수됨" 과 "취소됨" 을 구분하려면 재조회
   306   충돌한 WorkId 를 주지 않는다 → 그 예약으로 이동시키려면 SELECT_수검자유효업무 를 한 번 더
   ```

   나머지 Code 도 다음 행동을 문장에 담은 것은 일부뿐이다 — 코드→후속행동 매핑 표를 화면 쪽에 둔다.
10. **`@OperatorName` 을 무엇으로 채울지 Phase 5 가 정한다.** Write SP 8개가 전부 필수로 요구하는데
    `01_Process_Definition.md` 에 "조작자" 가 한 번도 안 나온다. `04` §14.2 L4 가 "인증되지 않은
    위조 가능 자기신고 문자열" 이라고 성질만 인정했다. 로그인은 `00` §8.2 범위 밖이다.
11. **차트번호는 대소문자를 구분하지 않는다.** `UQ_수검자_CHART_NO` 가 CI·폭무시라 `c1` 과 `C1` 은
    같은 번호다. applock 자원명도 `UPPER` 로 맞췄다(`06` §22). 화면에서 입력을 대문자로 정규화하면
    사용자가 혼동할 여지가 줄어든다.
12. **접수취소 확인 전에 "오늘 다시 접수할 수 있는가" 를 물어라.** 접수취소는 마감을 보지 않아
    09:00~18:00 언제든 되는데, 복구 경로인 WalkIn 신규예약은 PM 16:00 에 닫힌다. 16:00~18:00 취소분과
    토요일 AM 11:00 이후 취소분은 그날 복구할 수 없다 (`06` §43-15).
    **DB 로는 못 막는다** — 취소에 마감을 걸면 `05` §12.3 위반, `CNC → RSV` 되돌림은 `00` CP-05 위반이다.
    대신 `SELECT_예약가능정보` 가 그 답을 이미 갖고 있다.

    ```text
    EXEC USP_HC_SELECT_예약가능정보 @PatientId, @WorkId, @RowVersion, 'WALKIN', 오늘, 그 시간대, AEX…
      RS1  CanSave · BlockCode · BlockMessage      "지금 이 조건으로 저장 가능한가"
      RS2  CutoffTime · CutoffPassed · CanSelect    시간대별로
    CanSave = 0 이면 "취소하면 오늘 다시 접수할 수 없습니다" 를 띄운다.
    ```

    **`@WorkId` 에 취소할 그 업무를 반드시 넘겨라.** 안 넘기면 그 수검자가 아직 유효업무를 갖고 있어
    `306` 이 먼저 나온다 — 그 인자가 정확히 "현재 Work 제외" 용도다 (`F-COM-005`).
    RS1 이 `CanSave`·`BlockCode` 를 싣는 것은 실측했고, 취소 시나리오 자체는 Phase 5 에서 확인해야 한다.

---

## 8. 증거 파일

```text
artifacts/logs/full_test_run.log            회차 A (창 안)
artifacts/logs/full_test_run_off.log        회차 B (창 밖)
artifacts/logs/conc_<RUN>_<SCEN>_*.log      동시성 회차별 (setup·A·B·verify)
artifacts/reports/test-summary.txt          회귀 요약 (회차 시각 포함)
artifacts/reports/contract-verify.txt       계약 판정
artifacts/reports/object-inventory.txt      정렬 객체 덤프
artifacts/reports/clean-rebuild.txt         RBD 판정
artifacts/reports/no-secret.txt             SEC-010
artifacts/reports/tsql-allowlist.txt        G13-b
artifacts/reports/schema-actual.txt         DOC 대조
artifacts/reports/baseline-hash.txt         G00
```

`artifacts/logs/` 는 `.gitignore` 대상이다. 보고서는 커밋한다.
