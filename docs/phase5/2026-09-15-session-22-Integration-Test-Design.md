# 인계 — 통합테스트 설계·구현 (session 22)

이 문서는 **다음 세션이 그대로 이어받아 실행하는 지시서**다. 읽고 §9 부터 시작하면 된다.

저장소 규칙(ROOT `AGENTS.md`·`winforms/AGENTS.md`·킷)은 세션이 스스로 읽으므로 여기 베끼지
않는다. 여기 적는 것은 **지금 참인 상태와 이번에 결정된 것**뿐이다.

앞 세션 전문: `claude --resume bc6aa750-2ccb-43b6-8004-6d8feb6236bf`
(결정에 이른 논거가 필요할 때만 열어라 — 결론은 아래 §3~§6 에 전부 있다)

---

## 1. 지금 무엇이 참인가

```text
브랜치   test/integration-design   main(6b1e5ad) 에서 딴 것. 커밋 0개. 작업트리 깨끗
직전     PR #6 머지됨 — 단위시험 정리 (4 커밋, +3582/-596, 42 파일)
DB       .\SQLEXPRESS / HealthCheckupReservationReceptionDb (통합인증)
```

단위시험은 **끝났다.** 305행 · 339케이스 전건 PASS · FAIL 0 · 미실행 0.
산출물 `docs/phase5/output/P5_단위시험_목록.xlsx` 는 `winforms/tools/build-test-inventory.js`
가 찍는다 — 손으로 열지 않는다. 대조 기록은 `docs/phase5/2026-09-15-Unit-Test-Inventory.md`.

## 2. `[!]` 되돌려야 할 것 — 가장 누락되기 쉬운 자리

**실물 `운영기준` 의 운영시간이 24시간으로 열려 있다** (2026-09-15 사용자 결정, 통합테스트까지
유지). 마감 넷은 `00` 값 그대로다.

```text
지금   운영 00:00:00 ~ 23:59:00   마감 10:00/15:00/11:00/16:00
```

그래서 `database/scripts/verify-operating-baseline.sh` 의 `OPR-G4` 가 **의도된 red** 다.
`G1`·`G2`·`G3` 는 PASS 이고 `verify-live-sync.sh`(G17)도 PASS 다 — 바뀐 것은 **데이터뿐**이고
`docs/baseline/`·`database/deploy/` 는 한 바이트도 열지 않았다.

`[!]` **통합테스트가 끝나면 `00` 값으로 되돌린다.** 되돌릴 값을 여기 적지 않는다 —
`verify-operating-baseline.sh` 가 `OPR-G1` 줄에 기대값을 찍는다(ROOT `AGENTS.md` §6).
그 값으로 UPDATE 하고 같은 게이트로 확인해라. **`OPR-G4` 가 PASS 로 돌아오는 것이 완료 조건의
하나다** (§8).

## 3. 이번에 결정된 수용기준

```text
A  SP 16 전부가 C# Repository 경로로 실물 DB 에서 실행된다
   깊이 = 성공 경로 1회 + Result Set 형상이 갈리는 분기
B  각 SP 의 Result Set 형상(개수·컬럼 이름·타입)을 C# DTO 가 손실 없이 받는다
C  업무 흐름이 이어질 때 값이 실제로 흐른다          ← S01~S07 이 이미 끝냄. 더할 것 없다
```

**왜 이것이 기준인가.** 단위시험이 fake 로 확인한 것은 「약속된 답을 받았을 때 내 코드가 옳게
행동하는가」이고, **그 fake 는 우리가 썼다.** 계약을 잘못 읽었으면 fake 도 같이 잘못 읽었고
단위시험 339건이 전부 green 인 채 화면이 깨진다. 통합시험은 단위시험의 반복이 아니라
**fake 가 진실인지 확인하는 것**이다. `SelectRepositoryDbTests` 머리말이 같은 말을 한다 —
「컬럼 이름 오타는 컴파일도 되고 fake 를 쓰는 단위시험도 통과하며 실행할 때만 터진다」.

따라서 **틀릴 수 있는 것 = 사람이 손으로 두 곳에 적은 값**만 실물로 본다.
컬럼 이름 · 파라미터 이름/타입/크기 · 결과코드 숫자 · Result Set 개수와 순서 · 동작코드 문자열.
`verify-rs-columns.sh`·`verify-param-size.sh` 는 **`05` 문서를 보지 DB 를 보지 않는다** —
문서와 코드가 사이좋게 함께 틀리면 둘 다 green 이다. 그 틈이 이 작업의 대상이다.

## 4. 대상 — 다섯

`Integration/` 경로가 한 번도 실물로 밟지 않는 것들이다.

```text
읽기  USP_HC_예약가능정보_조회     SP-RSV-01. Result Set 여섯. 변경범위 다섯이 형상을 가른다
                                  (05 §9.11 ALL / SLOT / EXTRA / SLOT_EXTRA / NONE)
쓰기  USP_HC_예약_변경
      USP_HC_접수추가검사_변경     RCP 상태 업무가 필요하다
      USP_HC_자체휴무일_저장(성공) 지금 도는 것은 101 실패 경로뿐이고 그것은 DB 에 안 쓴다
      USP_HC_자체휴무일_삭제       물리 삭제
```

`[!]` **`자체휴무일_저장` 을 「닿는다」로 세지 마라.** 이름 매칭 스캔은 O 로 세지만 실제로
도는 것은 `SelectRepositoryDbTests` 의 `101` 경로이고 그 시험 자신이 「아무것도 쓰지 않는다」고
적는다 — 코드 검증이 잠금·Transaction 앞에서 끝난다.

## 5. 방법 — 이번에 확정된 것

| | |
|---|---|
| **쓰기 뒷정리** | 자기가 만든 것만 심고 지운다. 자체휴무일은 **저장 → 삭제를 한 쌍**으로 묶어 심는 것이 곧 지우는 준비가 되게 한다 |
| **휴무일 날짜** | **아무 시험도 쓰지 않는 먼 미래**로 고정한다. `UFN_HC_일정확인` 이 휴무일을 업무일 판정에 쓰므로, 날짜를 잘못 고르면 **다른 시험이 전부 `308` 로 막힌다** |
| **실행 시각** | 시험이 `운영기준` 의 창을 **잠깐 옮겼다 되돌린다**. `06` §33.2a 와 `winforms/scripts/measure-cutoff.sh` 가 쓰는 같은 패턴이다 — 시각 무관하게 **항상 판정**된다 |
| **복원 안전장치** | 새로 만들지 않는다. `OPR-G4` 가 복원 판정을 겸하고 2026-09-15 에 실제로 red 를 내는 것을 확인했다 |
| **병렬** | 오늘 MSTest 는 순차다(`Parallelize` 선언 없음 — 확인함). 선언은 썩으므로 **`[assembly: Parallelize]` 가 들어오면 잡는 검사를 함께 둔다** — 병렬이면 두 시험이 서로의 창을 덮어쓴다 |
| **B 의 강도** | 계약이 정한 수치까지 단언한다 — RS4 는 정확히 5행, RS5 는 7행, 변경범위별 cardinality (`05` §9.11) |

## 6. 하지 않을 것 — 명시한다

범위가 번지는 것을 막는 것이 이 목록의 목적이다.

```text
Presenter 분기를 실물로 재실행    fake 로 156건이 이미 돈다. 실물이 더 잡는 것이 없다
길이·정규화 재확인                Service 안의 순수 로직이다
화면 배치·색·정렬                 DB 와 무관하다
동시성(CON-001~008)               DB 계층 8건이 있다. C# 이 더할 것은 601 분기뿐이고 이미 있다
결과코드 분기(305·411·601 …)      DB 계층 계약시험 411건이 재고 있다
                                  — 같은 판정을 두 계층에 두지 않는다
```

## 7. `[X]` 이 세션이 실제로 밟은 함정 — 다시 밟지 마라

```text
INSERT … EXEC 금지        암시적 트랜잭션을 열어 Write SP 진입 가드가 50003 으로 막는다
                          (06 §33.1a). 맨 EXEC 로 부르고 결과는 뒤에서 다시 센다
C# 은 UTF-8 BOM + CRLF    `sed -i` 가 CRLF 를 LF 로 날린다. Write 도구는 BOM 없이 쓴다
                          — UIB-005 가 잡지만, 고칠 때 파일 전체가 diff 가 된다
`///` 는 [TestMethod] 위   아래에 놓으면 C# 이 문서 주석으로 읽지 않는다. TI-000 이 잡는다
GridView 는 Form 에 얹어야  안 그러면 GetRow 가 전부 null 이라 **아무것도 못 재는 채 green**
FocusedRowChanged 배선     Designer 가 잇는다. 픽스처가 안 이으면 화면과 다른 것을 잰다
콘솔 한글이 깨진다         판정은 TestResults/*.trx 의 <Counters .../> 로 읽어라
                          — 깨진 요약에서 「실패: 1」을 「건너뜀: 1」로 잘못 읽은 적이 있다
sqlcmd -S '.\SQLEXPRESS'   작은따옴표로 감싸라. 안 그러면 error 53 이 난다
winforms 를 고치면         `cd database && ./scripts/verify-winforms-unchanged.sh init` 매니페스트를
                          **같은 커밋에** 넣는다
```

## 8. 완료 정의 — 이것이 전부 참이면 끝이다

```text
1  SP 16 전부가 실물 DB 에서 C# Repository 경로로 실행된다     (스캔으로 재측정)
2  예약가능정보의 변경범위 다섯이 각각 한 번씩 돈다
3  밤에 돌려도 Inconclusive 0 이다                             (창을 옮겼다 되돌리므로)
4  쓰기 시험이 남긴 것이 없다                                   (휴무일 증가 0 · 업무는 취소로 복원)
5  OPR-G4 가 PASS 로 돌아온다                                   (운영시간 복원 · §2)
6  winforms/scripts/test.sh PASS · vstest 전건 PASS FAIL 0
7  엑셀을 다시 찍었고 실물 DB 층 건수가 늘었다
8  PR 을 올려 문제 없음을 확인하고 머지한다
```

## 9. 다음 행동 — 여기서 시작해라

1. `docs/phase5/2026-09-15-Integration-Test-Design.md` 를 쓴다 — §3~§6 을 설계 문서로 옮기고
   시험 다섯의 시나리오(전제·호출·기대 형상·뒷정리)를 표로 편다.
2. 씨앗을 손본다. `winforms/scripts/seed-probe-works.sh` 가 오늘자 AM·PM RSV 를 세운다 —
   변경·접수추가검사가 쓸 **RCP 상태 업무**와 예약변경이 쓸 **행버전**이 더 필요하다.
3. 시험 다섯을 `winforms/tests/.../Integration/` 에 구현한다. 새 파일은 킷 §1 대로
   **UTF-8 BOM + CRLF** 로 쓰고 csproj `<Compile Include>` 에 등록한다.
4. 대상·목적·확인 세 머리말을 주석에 단다 — `TI-001` 이 하나라도 비면 red 다.
5. 돌리고, 운영시간을 복원하고, 엑셀을 다시 찍고, 커밋·PR.

## 10. 검증 명령

```bash
cd D:/AIDEV/HealthCheckupReservationReception/winforms
MSB="C:/Program Files (x86)/Microsoft Visual Studio/2019/Professional/MSBuild/Current/Bin/MSBuild.exe"
VST="C:/Program Files (x86)/Microsoft Visual Studio/2019/Professional/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe"

"$MSB" HealthCheckupReservationReception.sln -p:Configuration=Debug -v:m
"$VST" tests/HealthCheckupReservationReception.Tests/bin/Debug/HealthCheckupReservationReception.Tests.dll /Logger:trx
node tools/build-test-inventory.js        # TI-000~003 + 엑셀
./scripts/test.sh                          # winforms 회귀

cd ../database
./scripts/verify-operating-baseline.sh     # OPR-G4 — 복원 판정
./scripts/verify-live-sync.sh              # 라이브 DB == deploy/
./scripts/verify-winforms-unchanged.sh     # 매니페스트
```

`database/scripts/test.sh` 는 **돌리지 않는다** — 계약 동결이고 `DROP DATABASE` 를 한다.
마지막 전체 회귀는 회차 R19 `PASS 411 · FAIL 0` 이고 기록은 `docs/phase4/reseal-history.md`.

## 11. 확인하지 않은 것 — 그대로 두지 말고 재라

```text
창 옮기기를 C# 에서 하는 것   SQL 스크립트(measure-cutoff.sh)로만 증명됐다. C# 시험이
                              [TestCleanup] 로 되돌리는 경로는 이번에 안 해 봤다 —
                              프로세스가 죽으면 그것도 안 돈다. OPR-G4 가 최후 방어선이다
변경범위 다섯의 씨앗 난이도    SLOT·EXTRA·NONE 은 @업무ID + @행버전을 요구한다.
                              씨앗이 틀리면 시험이 헛돈다 — 형상을 단언하기 전에
                              결과코드가 0 인지 먼저 확인해라
```

## 12. 별건 — 범위 밖이지만 적어 둔다

`USP_HC_수검자유효업무_조회` 가 죽었다. R21 이 지워 `05` 에 0회 등장인데,
`winforms/src/.../Repositories/PatientRepository.cs` 의 인터페이스에 **멤버 없는 `///` 주석**만
남아 있다(그 다음 줄이 닫는 중괄호다). 고치는 김에 걷어도 되고, 별도 커밋으로 빼도 된다.
