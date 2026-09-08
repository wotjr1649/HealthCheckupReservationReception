# 검진 예약·접수 관리 프로그램

건강검진센터의 **예약 → 접수 → 완료** 업무를 다루는 사내 프로그램이다.
DB 는 SQL Server, 화면은 C# WinForms(.NET Framework 4.6.1 · DevExpress) 다.

현재 **Phase 4(Database) 종료**, Phase 5(WinForms) 착수 전이다.

---

## 이 저장소가 실제로 주장하는 것

> **실행하지 않은 검증을 `PASS` 로 적지 않는다.**

문서에 적힌 판정은 전부 기계가 낸 것이고, 그렇지 않은 것은 `NOT RUN` 으로 남아 있다.
그래서 **읽고 믿는 것이 아니라 돌려서 확인하는** 구조로 만들었다.

```bash
cd database
./scripts/test.sh              # 전체 회귀. SQL Server 접속이 필요하다
node tools/verify-docs.js      # 문서 정합성 게이트 (DB 없이 돈다)
./scripts/verify-baseline.sh   # 봉인 문서의 SHA-256 전건 대조
```

건수·판정은 여기 적지 않는다 — **`docs/baseline/06_DB_Transaction_Security_Seed.md` §42** 가
단일 출처이고, 이 문단이 그 값을 베끼면 다음 회차에 낡는다.

## 구성

```text
docs/baseline/      봉인된 계약 00~06. 한 바이트라도 바뀌면 게이트가 exit 1 이다
  00 업무정책 · 01 업무프로세스 · 02 기능정의 · 03 화면설계
  04 DB설계  · 05 Rule·SP계약   · 06 DB구현계약 + 실행검증 기록
docs/phase4/        Phase 4 기록 — 단계별 계획서와 재봉인 이력
docs/phase5/        Phase 5 작업 공간. 07(화면↔DB 최종 대조)을 여기서 쓴다
database/           Phase 4 산출물 — 스키마 · Seed · TVF · SP · 시험 · 게이트
winforms/           Phase 5 구현 대상. 지금은 골격뿐이다
tools/docgen/       사내 공개용 xlsx·pptx 생성기
```

작업 규칙은 `AGENTS.md`(저장소 전체)와 `database/AGENTS.md`(DB 작업 경계)에 있다.
`CLAUDE.md` 는 그것을 가져오는 한 줄짜리 입구다.

## 게이트

문서와 구현이 어긋나는 것을 사람이 눈으로 막지 않는다. `verify-docs.js` 의 `V01`~`V23` 이
계약↔계획↔SQL 을 양방향으로 대조하고, 셸 게이트가 봉인 해시·스키마·동시성·비밀값을 본다.

이 저장소에서 **게이트 자신이 썩은 사건이 반복해서 나왔다** — 초록인 채 아무것도 검증하지
않고 있던 검사들이다. `06` §44.8 과 커밋 메시지가 그 사건들을 하나씩 적어 두었다.
그래서 규칙이 하나 더 있다.

> 수치나 해시를 문서에 적고 싶으면 **그 값이 게이트와 일치하는지 보는 검사를 함께 만든다.**
> 검사를 만들 수 없으면 적지 않는다.

## 기준선

봉인 문서를 고치는 것을 **재봉인**이라 하고, 파일과 해시를 같은 커밋에 묶는다.
회차는 `baseline-*` 태그가 표시한다.

```bash
git tag -l "baseline-*"
```
