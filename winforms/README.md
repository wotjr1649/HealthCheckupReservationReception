# WinForms

검진 예약·접수 관리 프로그램의 Windows Forms 애플리케이션과 MSTest 프로젝트입니다.

## 요구사항

- Visual Studio 2019
- .NET Framework 4.6.1 Developer Pack
- DevExpress 20.2
- SQL Server 또는 SQL Server Express

## 빌드

`winforms/`에서 실행합니다.

```text
MSBuild HealthCheckupReservationReception.sln -t:restore
MSBuild HealthCheckupReservationReception.sln -p:Configuration=Debug
```

## 테스트

DB를 사용하지 않는 기본 테스트:

```text
vstest.console tests/HealthCheckupReservationReception.Tests/bin/Debug/HealthCheckupReservationReception.Tests.dll /TestCaseFilter:"TestCategory!=Db"
```

`Db` 카테고리는 데이터를 변경하므로 폐기 가능한 로컬 시험 DB에서만 실행합니다.

```text
vstest.console tests/HealthCheckupReservationReception.Tests/bin/Debug/HealthCheckupReservationReception.Tests.dll /TestCaseFilter:"TestCategory=Db"
```

데이터베이스 연결은 Windows 통합 인증을 사용합니다. 실행 전
`src/HealthCheckupReservationReception.WinForms/App.config`의 SQL Server 인스턴스를 로컬
환경에 맞게 설정하십시오.
