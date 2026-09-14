# 검진 예약·접수 관리 프로그램

성인 일반건강검진의 **예약**과 **접수**를 창구에서 처리하는 원내 프로그램이다.

```text
C# WinForms · .NET Framework 4.6.1 · DevExpress 20.2 · SQL Server (Stored Procedure 전용)
```

화면에서 DB 로 직접 SQL 을 보내지 않는다. 모든 읽기·쓰기는 Stored Procedure 를 지나고,
저장·상태전이의 **최종 판정은 DB 가 한다.** 화면은 그 판정을 받아 사용자에게 옮긴다.

---

## 코드 주석의 `03 §24.6` 은 무슨 뜻인가

**앞 숫자가 문서 번호, `§` 뒤가 그 문서의 절**이다. `03 §24.6` 은 「화면설계서 24.6절」이다.

| | 문서 | 무엇을 정하는가 |
|---|---|---|
| `00` | 업무정책 | CP·EP·RP·RCP 정책과 TGT·NEX·AEX·HOL 규칙 |
| `01` | 업무프로세스 | P01~P03 의 단계·분기·상태전이 |
| `02` | 기능정의 | Function ID 와 기능 계약 |
| `03` | 화면설계 | 화면 구성·필드·검증 |
| `04` | DB 설계 | 테이블·컬럼·키·제약·인덱스 |
| `05` | Rule·SP 계약 | SP·Parameter·Result Set·ResultCode |
| `06` | DB 구현 계약 | Transaction·잠금·Seed·시험 |
| `07` | 화면↔DB 최종 대조 | 검증 기록 (이것만 `docs/phase5/` 에 있다) |

**번호가 작은 문서가 이긴다.** 충돌하면 `00` 이 `05` 를 이긴다.

원본은 `docs/baseline/`, 사내 공개본(xlsx·pptx)은 `docs/baseline/output/` 에 있다.

`[X]` · `[!]` 로 시작하는 주석은 **「무엇이 조용히 틀리는가」**를 적은 것이다.
대부분 실제로 한 번 겪고 나서 쓴 것이다.

`[!]` **공개본 `03` 화면설계서는 설계 시점의 기록이다.** 개발 중 사용성 판단으로 실제 화면과
달라진 부분이 있고, **동작의 기준은 실행 프로그램**이다. 업무 규칙과 항목의 의미는 `03` 이 기준이다.

---

## 구조

```text
docs/baseline/     계약 문서 00~06 과 공개본 output/
database/          SQL · 배포 스크립트 · DB 시험 · 증거
winforms/          C# 구현
tools/docgen/      공개본 생성기
```

C# 은 **MVP** 다. 각 계층이 하는 일이 정해져 있고, 그 경계를 게이트가 지킨다.

```text
View (FrmXxx : XtraForm)   컨트롤·바인딩·포커스. 업무 규칙이 없다
  → Presenter              화면 이벤트를 받아 요청을 만들고 결과를 화면에 쓴다
  → Service                업무 검증·상태 전이. 실패는 OperationResult 로 돌려준다
  → Repository             Stored Procedure 호출과 DTO 매핑. SqlClient 는 여기에만 있다
  → Stored Procedure       최종 판정
```

```text
winforms/src/.../Views/         화면. cls* 는 화면들이 함께 쓰는 부품이다
              /Presenters/      화면당 하나
              /Services/        업무 규칙
              /Repositories/    SP 호출
              /Models/          DTO · Request
              /Common/          계약 코드값(DbCodes·DbSize) 과 표기(clsPatientText 등)
```

---

## 돌려 보기

```bash
# 빌드와 단위시험 (VS2019 / MSBuild 16.11)
MSBuild winforms/HealthCheckupReservationReception.sln -p:Configuration=Debug
vstest.console winforms/tests/.../bin/Debug/HealthCheckupReservationReception.Tests.dll

# 화면을 PNG 로 떠 본다 (DB 없이 돈다)
vstest.console ... /TestCaseFilter:"ClassName~ShellCaptureTests"   # → winforms/artifacts/logs/

# 게이트 전수
cd winforms  && ./scripts/test.sh
cd database  && ./scripts/test.sh          # SQL Server 가 필요하다
```

---

## 이 저장소의 게이트

계약값을 코드에 옮겨 적으면 **사본이 둘**이 된다. 사본은 원본이 바뀌면 뒤처지는데,
그 어긋남은 대개 **조용하다** — 컴파일도 되고 단위시험도 같은 상수를 쓰므로 함께 틀린다.

그래서 이 저장소는 **값을 두 곳에 두면 그것을 재는 검사를 함께 둔다.**

```bash
./scripts/test.sh              # 무엇이 도는지와 왜 있는지는 각 스크립트 머리에 있다
```

예를 들어 `verify-param-size.sh` 는 `05` 가 정한 Parameter 크기를 C# 의 길이 검증과
`SqlParameter` 크기 양쪽과 대조한다. 코드가 크게 잡으면 DB 가 값을 자르고, 작게 잡으면
계약이 허락한 입력을 화면이 막는다 — **어느 쪽이든 사용자는 이유를 못 본다.**

게이트마다 `selftest` 가 있다. 「검사를 만들었다」가 아니라 **「검사가 실제로 잡는 것을 봤다」**
가 기준이다.
