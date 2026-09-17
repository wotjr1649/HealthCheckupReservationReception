# Database

검진 예약·접수 관리 프로그램의 SQL Server 데이터 계층입니다. 스키마와 기준정보를 구성하고,
조회·등록·변경·취소에 필요한 업무 판정을 Stored Procedure로 제공합니다.

## 담당 영역

- 수검자 기본정보와 중복 확인
- 검사코드와 국가검진 검사 구성
- 휴무일과 운영기준
- 예약·접수 업무와 상태 전이
- 검진 완료이력과 변경이력
- 예약 가능일·시간대·정원 판정
- 행 버전과 트랜잭션을 이용한 동시성 제어

애플리케이션은 테이블을 직접 변경하지 않고 Stored Procedure를 통해서만 데이터를 읽고 씁니다.
필수값, 참조 무결성과 동시성의 최종 방어선은 데이터베이스입니다.

## 주요 데이터

| 영역 | 역할 |
|---|---|
| 수검자 | 차트번호, 주민번호, 이름과 연락처 관리 |
| 검사코드 | 국가검진 및 추가검사 항목 관리 |
| 휴무일 | 법정공휴일과 자체 휴무일 관리 |
| 운영기준 | 운영시간, 예약 정원과 업무 기준 관리 |
| 예약접수 | 예약·접수 상태와 선택 검사 관리 |
| 완료이력 | 과거 검진 완료 기록 관리 |

## 구성

```text
deploy/   스키마·Seed·함수·Stored Procedure와 배포 검증
tests/    스키마·업무 규칙·SP·롤백·동시성 검증 SQL
```

주요 함수는 일정 확인, 검진 대상 확인, 국가검사 구성과 추가검사 적합성을 판정합니다.
Stored Procedure는 수검자, 예약, 접수, 휴무일과 변경이력 단위로 나뉩니다.

## 요구사항

- SQL Server 또는 SQL Server Express
- Windows 통합 인증
- 배포와 시험을 위한 별도 로컬 데이터베이스

## 배포

`deploy/`의 SQL을 다음 순서로 실행합니다.

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

`tests/`에는 다음 검증이 포함됩니다.

- 테이블, 키, 제약조건과 인덱스
- Seed와 업무 판정 함수
- 조회·수검자·예약·접수 Stored Procedure
- 오류 시 트랜잭션 롤백
- 행 버전 충돌과 동시 실행
- 휴무일과 운영시간 판정

시험은 파일명 순서와 시나리오별 선행 조건을 사용하며 데이터를 변경합니다. 별도 로컬 시험 DB에서
실행하고, 동시성 시험은 세션별 SQL의 실행 순서를 유지하십시오.
