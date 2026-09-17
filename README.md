# 검진 예약·접수 관리 프로그램

성인 일반건강검진의 예약과 접수를 처리하는 원내 Windows 프로그램입니다.

## 기술 구성

- C# 7.3, .NET Framework 4.6.1
- Windows Forms, DevExpress 20.2
- SQL Server, Stored Procedure 전용 데이터 접근
- MVP 구조
- MSTest v2

## 디렉터리

```text
database/deploy/   SQL Server 배포 스크립트
database/tests/    데이터베이스 검증 SQL
winforms/src/      WinForms 애플리케이션
winforms/tests/    단위·통합 테스트
```

## 빌드

Visual Studio 2019와 DevExpress 20.2가 설치된 환경에서 실행합니다.

```text
MSBuild winforms/HealthCheckupReservationReception.sln -p:Configuration=Debug
vstest.console winforms/tests/HealthCheckupReservationReception.Tests/bin/Debug/HealthCheckupReservationReception.Tests.dll
```

데이터베이스 연결은 Windows 통합 인증을 사용합니다. 실제 실행 전
`winforms/src/HealthCheckupReservationReception.WinForms/App.config`의 SQL Server 인스턴스를
로컬 환경에 맞게 설정하십시오.
