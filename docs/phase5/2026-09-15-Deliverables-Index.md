# 산출물 안내 — 무엇이 어디에 있고, 무엇이 그것을 지키는가

이 문서는 **길잡이**다. **값을 갖지 않는다** — 건수도 해시도 날짜도 적지 않는다. 적는 순간
사본이 되고, 사본은 한쪽만 고쳐지는 날이 온다(ROOT `AGENTS.md` §6). 이 저장소는 그 함정에
두 번 물렸다.

그래서 각 줄이 말하는 것은 셋뿐이다 — **어디에 있는가 · 무엇이 만드는가 · 무엇이 판정하는가.**

`[!]` **과제 브리프 §9 「최종 산출물」과의 대조는 이 문서가 하지 못한다.** 그 원문이 저장소에
없어서, 항목별 대응은 받는 쪽에서 확인해야 한다. 여기 있는 것은 *저장소가 실제로 내놓는 것*의
전부다.

---

## 1. 사내 공개본 — 저장소를 열지 않는 사람이 받는 것

| 무엇 | 어디에 | 무엇이 만드는가 | 무엇이 판정하는가 |
|---|---|---|---|
| 업무정책·업무프로세스·기능정의·화면설계·DB설계·SP계약 | `docs/baseline/output/` | `node tools/docgen/build_all.js` | `tools/docgen/verify_output.js` (금지 어휘·`03` 의 지위 문장) |

`[!]` **내용이 바뀌지 않았으면 다시 돌리지 않는다.** 생성기가 결정적이지 않아 `docProps` 에
생성 시각이 들어가므로, 다시 돌리면 **여섯 종이 전부 바뀐다**(ROOT `AGENTS.md` §3). 바뀐 것이
없는데 커밋하면 그 diff 는 전부 소음이다.

`[X]` **`06` 은 공개본이 없다.** 내부 어휘 비중이 크고 값이 표가 아니라 산문에 있어
`verify_output.js` 를 지날 수 없다. 원본보다 공개본이 하나 적은 비대칭은 의도한 것이다.

## 2. 시험 산출물 — 「무엇을 어떻게 시험했는가」

| 무엇 | 어디에 | 무엇이 만드는가 | 무엇이 판정하는가 |
|---|---|---|---|
| 단위시험 목록 | `docs/phase5/output/P5_단위시험_목록.xlsx` | `winforms/tools/build-test-inventory.js` | 같은 생성기 (`TI-000`~`003`) |
| 통합시험 시나리오 | `docs/phase5/output/P5_통합시험_시나리오.xlsx` | `winforms/tools/build-scenario-inventory.js` | 같은 생성기 |
| 화면 증빙 | `docs/phase5/output/screens/` | `Visual/ScenarioCaptureTests` 가 뜨고 `winforms/tools/build-scenario-evidence.js` 가 옮긴다 | 같은 생성기 |

세 산출물 모두 **손으로 쓰는 칸이 없다.** 이유·시나리오·설명은 각각 코드 주석과 시나리오
문서에서 뽑는다 — 엑셀에 따로 적으면 그 순간 값이 두 곳이 된다.

## 3. 시험을 설명하는 문서

| 무엇 | 어디에 |
|---|---|
| 테스트 시나리오와 결과 (브리프 §7) | `docs/phase5/2026-09-14-Test-Scenarios.md` |
| 통합시험 설계·실측·리뷰 잔여 | `docs/phase5/2026-09-15-Integration-Test-Design.md` |
| 단위시험 ↔ 기준선 시험계약 대조 | `docs/phase5/2026-09-15-Unit-Test-Inventory.md` |
| 마감시각 실측 | `docs/phase5/2026-09-14-Cutoff-Field-Measurement.md` |
| 잔여 대장 — 무엇이 열려 있고 무엇이 열 조건인가 | `docs/phase5/2026-09-15-Leftover-Ledger.md` |
| 정책 55개 ↔ DB 검증 대조 | `docs/phase5/2026-09-15-Policy-DB-Coverage.md` |

`[I]` 시나리오 문서의 **결과 칸은 손으로 적지 않는다** — 증거 칸이 시험 이름이고, 그 시험이
실물 DB 에 붙어 돈다.

## 4. 원본 문서 — 봉인되어 있다

| 무엇 | 어디에 | 무엇이 판정하는가 |
|---|---|---|
| `00`~`06` | `docs/baseline/` | `database/scripts/verify-baseline.sh` (한 바이트라도 다르면 `exit 1`) |
| `07` 화면↔DB 최종 대조 | `docs/phase5/07_UI_DB_Matrix_Final_Validation.md` | 없다 — `CANDIDATE` 이고 갱신 의무가 면제됐다(ROOT `AGENTS.md` §4) |

`07` 이 `PLANNED`·`CANDIDATE` 로 남긴 칸은 **그 시점의 계획**이라는 뜻이지 지금의 참이 아니다.
Phase 5 후반부의 판단과 실측은 `2026-09-10-session-17-Phase5-UI-Overhaul.md` 가 갖는다.

## 5. 프로그램과 DB

| 무엇 | 어디에 | 무엇이 판정하는가 |
|---|---|---|
| C# WinForms 소스 | `winforms/src/` | `winforms/scripts/test.sh` · MSBuild · vstest |
| 시험 | `winforms/tests/` | 같은 것 |
| DB 배포 스크립트 | `database/deploy/` | `winforms/scripts/verify-db-frozen.sh` (동결) |
| DB 계약시험 | `database/tests/` | `database/scripts/test.sh` — **돌리지 않는다.** `DROP DATABASE` 를 한다 |

## 6. 다시 만드는 법

```bash
cd winforms
MSB="C:/Program Files (x86)/Microsoft Visual Studio/2019/Professional/MSBuild/Current/Bin/MSBuild.exe"
VST="C:/Program Files (x86)/Microsoft Visual Studio/2019/Professional/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe"

"$MSB" HealthCheckupReservationReception.sln -p:Configuration=Debug -v:m
"$VST" tests/HealthCheckupReservationReception.Tests/bin/Debug/HealthCheckupReservationReception.Tests.dll /Logger:trx

node tools/build-scenario-evidence.js    # 화면 증빙을 산출물로 옮긴다
node tools/build-test-inventory.js       # 단위시험 엑셀
node tools/build-scenario-inventory.js   # 통합시험 시나리오 엑셀
./scripts/test.sh                        # winforms 회귀

cd ../database
./scripts/verify-operating-baseline.sh   # 운영기준이 00 값으로 돌아왔는가
./scripts/verify-live-sync.sh            # 라이브 DB == deploy/
```

`[!]` **판정은 콘솔이 아니라 `TestResults/*.trx` 의 `<Counters .../>` 로 읽는다.** 콘솔 한글이
깨져서 「실패: 1」을 「건너뜀: 1」로 잘못 읽은 적이 있다.

`[!]` **공개본은 위 목록에 없다.** §1 이 적은 이유로, 내용이 바뀌었을 때만 돌린다.

## 7. 이 문서가 세지 않는 것

시험 건수 · 그림 장수 · 산출물 종수 · 해시. 전부 세는 곳이 따로 있고, 여기 적으면 그것이
두 번째 사본이 된다. 숫자가 필요하면 §6 의 명령을 돌린다 — 그때 찍히는 것이 그 순간의 참이다.
