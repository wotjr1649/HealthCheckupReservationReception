# Database

SQL Server에서 사용하는 스키마, Seed, 함수, Stored Procedure와 검증 SQL입니다.

## 요구사항

- SQL Server 또는 SQL Server Express
- Windows 통합 인증
- 배포와 시험을 위한 별도 로컬 데이터베이스

## 배포

`deploy/`의 SQL을 파일명 순서대로 실행합니다.

```text
00_Preflight.sql
01_Schema.sql
02_Seed.sql
03_Functions.sql
04_Procedures_Select.sql
05_Procedures_Patient_Write.sql
06_Procedures_Reservation_Write.sql
07_Procedures_Reception_Write.sql
07a_Procedures_Holiday.sql
08_Verify.sql
```

배포 SQL은 스키마와 데이터를 변경합니다. 운영 또는 공유 데이터베이스가 아닌 폐기 가능한
로컬 데이터베이스에서 먼저 검증하십시오.

## 시험

`tests/`에는 스키마, 업무 규칙, Stored Procedure, 롤백 및 동시성 검증 SQL이 있습니다.
파일명 순서와 시나리오별 선행 조건을 유지해야 하며, 시험 중 데이터가 변경됩니다.
