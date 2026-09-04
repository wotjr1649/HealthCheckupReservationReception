# 검진 예약·접수 관리 — Phase 4 Database

`HealthCheckupReservationReceptionDb` 의 스키마·Seed·Inline TVF·Stored Procedure·보안·테스트 일체.
설계 계약은 `../docs/baseline/04_DB_Design.md` 와 `../docs/baseline/05_DB_Rule_SP_Contract.md`,
구현 스펙은 `../docs/phase4/06_DB_Transaction_Security_Seed_CANDIDATE.md` 다.

## 대상 환경

| 항목 | 값 |
|---|---|
| Instance | `.\SQLEXPRESS` (SQL Server 2025 Express) |
| Database | `HealthCheckupReservationReceptionDb` |
| Collation | `Korean_Wansung_CI_AS` (`CREATE DATABASE` 에 명시 고정) |
| 시각 | KST `+09:00` (`DATEPART(TZOFFSET, SYSDATETIMEOFFSET()) = 540`) |
| 도구 | `sqlcmd` / Git Bash / `node` (표준 라이브러리만) |

## 빠른 시작

```bash
./scripts/rebuild.sh     # DB Drop → Create → 전체 배포
./scripts/test.sh        # rebuild 후 전체 회귀
```

객체만 다시 배포할 때는 `./scripts/deploy.sh` 를 쓴다. DB 자체는 유지된다.

## 파일 구조

스펙 §7 이 전체 트리의 출처다. 요약하면 다음과 같다.

```text
Deploy.sql / Rebuild.sql   진입점 2개
deploy/                    00_Preflight → 01_Schema → 02_Seed → 03_Functions
                           → 04~07_Procedures → 08_Security → 09_Verify
tests/                     00 Harness · 01~14 단계별 시험 · contract/ SP 호출 시나리오
scripts/                   deploy · rebuild · test · concurrency-test
                           verify-baseline · verify-winforms-unchanged
                           verify-contract-all · verify-no-secret
tools/                     verify-contract.js · verify-docs.js · *.json
artifacts/logs/            sqlcmd -u 원본 출력 (.gitignore)
artifacts/reports/         커밋 대상 증거
```

## 로그 읽는 법

`sqlcmd -u` 출력은 UTF-16 (BOM 포함) 이다.

```bash
iconv -f UTF-16 -t UTF-8 artifacts/logs/*.log | grep -E '^(PASS|FAIL|SKIP|INFO|Msg )'
```

`-f UTF-16LE` 를 쓰지 않는다. BOM 이 `EF BB BF` 로 남아 첫 줄의 `^PASS` 가 매치되지 않는다(실측 확인).

## Gate

G00~G16 의 정의와 증거 파일 대응은 스펙 §42 에 있다.
`PASS` 는 실제 실행 증거가 있을 때만 기록한다. 자세한 금지사항은 `CLAUDE.md` 를 본다.
