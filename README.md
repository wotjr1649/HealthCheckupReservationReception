# 검진 예약·접수 관리 프로그램

성인 일반건강검진의 수검자 관리부터 예약, 접수, 취소와 변경이력 조회까지 지원하는
원내 Windows 데스크톱 애플리케이션입니다. 화면은 업무 요청과 결과 표시를 담당하고,
업무 가능 여부와 상태 변경의 최종 판정은 SQL Server Stored Procedure가 수행합니다.

## 주요 기능

- 수검자 조회·등록·수정과 중복 후보 확인
- 검진 예약 등록·변경·취소
- 예약자 접수 완료·접수 취소
- 국가검진 대상 여부와 기본 검사 구성 조회
- 성별·나이 조건을 반영한 추가검사 선택
- 법정공휴일 조회와 자체 휴무일 관리
- 예약·접수 상태, 업무 가능 사유와 변경이력 조회
- 행 버전을 이용한 동시 수정 충돌 방지

## 기술 구성

- C# 7.3, .NET Framework 4.6.1
- Windows Forms, DevExpress 20.2
- SQL Server, Windows 통합 인증
- Stored Procedure 전용 데이터 접근
- MVP 구조
- MSTest v2

## 아키텍처

```text
WinForms View
    → Presenter
        → Service
            → Repository
                → SQL Server Stored Procedure
```

- View: 컨트롤, 데이터 바인딩, 포커스와 대화상자
- Presenter: 화면 이벤트 처리와 화면 상태 결정
- Service: 입력 검증과 업무 흐름 조정
- Repository: Stored Procedure 실행과 결과 매핑
- Database: 트랜잭션, 상태 전이, 무결성과 동시성의 최종 판정

## 저장소 구조

```text
database/deploy/   SQL Server 스키마·Seed·함수·Stored Procedure
database/tests/    스키마·업무 규칙·SP·동시성 검증 SQL
winforms/src/      WinForms 애플리케이션
winforms/tests/    단위·통합·화면 검증 테스트
```

세부 안내:

- [Database](database/README.md)
- [WinForms](winforms/README.md)

## 빠른 시작

요구 환경:

- Visual Studio 2019
- .NET Framework 4.6.1 Developer Pack
- DevExpress 20.2
- SQL Server 또는 SQL Server Express

데이터베이스는 `database/deploy/`의 SQL을 파일명 순서대로 실행해 준비합니다. 배포 SQL은
스키마와 데이터를 변경하므로 먼저 폐기 가능한 로컬 데이터베이스에서 검증하십시오.

애플리케이션 복원과 빌드:

```text
MSBuild winforms/HealthCheckupReservationReception.sln -t:restore
MSBuild winforms/HealthCheckupReservationReception.sln -p:Configuration=Debug
```

실행 전 `winforms/src/HealthCheckupReservationReception.WinForms/App.config`의 SQL Server
인스턴스를 로컬 환경에 맞게 설정합니다. 연결은 Windows 통합 인증만 사용합니다.

## 테스트

DB를 사용하지 않는 기본 테스트:

```text
vstest.console winforms/tests/HealthCheckupReservationReception.Tests/bin/Debug/HealthCheckupReservationReception.Tests.dll /TestCaseFilter:"TestCategory!=Db"
```

`Db` 카테고리는 데이터를 변경합니다. 운영 또는 공유 DB가 아닌 폐기 가능한 로컬 시험 DB에서만
실행하십시오.

```text
vstest.console winforms/tests/HealthCheckupReservationReception.Tests/bin/Debug/HealthCheckupReservationReception.Tests.dll /TestCaseFilter:"TestCategory=Db"
```

## 공개 범위

이 저장소는 애플리케이션과 데이터베이스 실행에 필요한 소스 및 테스트를 공개합니다. 사내 업무
계약 원문, 작업 세션 기록, 생성 산출물과 내부 개발 도구는 공개 트리에 포함하지 않습니다.
