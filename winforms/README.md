# WinForms

검진 예약·접수 관리 프로그램의 Windows Forms 클라이언트입니다. DevExpress 컨트롤과 MVP 구조를
사용하며, 모든 데이터 접근은 Repository를 통해 SQL Server Stored Procedure로 전달됩니다.

## 주요 화면과 기능

| 화면 | 기능 |
|---|---|
| MainForm | 업무 화면 이동, 공통 업무 상태와 조작자 표시 |
| 수검자 관리 | 조건 검색, 상세정보, 예약·접수 이력 조회 |
| 수검자 편집 | 신규 등록, 중복 후보 확인, 기존 정보 수정 |
| 예약 관리 | 예약 조회, 신규 예약, 일정·검사 변경, 예약 취소 |
| 접수 관리 | 접수 대상 조회, 접수 완료·취소, 추가검사 변경 |
| 휴무일 관리 | 휴무일 조회와 자체 휴무일 등록·수정·삭제 |
| 변경이력 | 수검자와 예약·접수 업무의 변경 기록 조회 |

## MVP 구조

```text
View → Presenter → Service → Repository → Stored Procedure
```

- `Views/`: WinForms·DevExpress 컨트롤, 바인딩과 사용자 상호작용
- `Presenters/`: 화면 이벤트 처리, 요청 생성과 화면 상태 결정
- `Services/`: 업무 입력 검증과 사용 사례 조정
- `Repositories/`: Stored Procedure 호출과 DTO 매핑
- `Models/`: 요청, 결과와 화면 데이터 형식
- `Common/`: 결과 형식, DB 코드·크기와 공통 표시 로직

View는 SQL을 직접 실행하지 않으며, Presenter와 View 인터페이스는 테스트에서 대체할 수 있도록
분리되어 있습니다.

## 프로젝트 구조

```text
HealthCheckupReservationReception.sln
src/HealthCheckupReservationReception.WinForms/     애플리케이션
tests/HealthCheckupReservationReception.Tests/       MSTest 프로젝트
```

## 요구사항

- Visual Studio 2019
- .NET Framework 4.6.1 Developer Pack
- DevExpress 20.2
- SQL Server 또는 SQL Server Express

## 연결 설정

`src/HealthCheckupReservationReception.WinForms/App.config`에서 SQL Server 인스턴스를 로컬 환경에
맞게 설정합니다. 연결은 Windows 통합 인증만 사용합니다.

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

`Db` 카테고리는 Repository와 실제 Stored Procedure 흐름을 검증하며 데이터를 변경합니다.
폐기 가능한 로컬 시험 DB에서만 실행하십시오.

```text
vstest.console tests/HealthCheckupReservationReception.Tests/bin/Debug/HealthCheckupReservationReception.Tests.dll /TestCaseFilter:"TestCategory=Db"
```

`Visual` 카테고리는 WinForms 화면을 렌더링해 레이아웃과 초기 상태를 검증합니다.

## Designer 리소스

`*.Designer.cs`와 같은 이름의 `*.resx`는 Visual Studio Designer가 관리하는 한 쌍입니다.
리소스가 생성된 화면은 `.csproj`에 `EmbeddedResource`와 `DependentUpon`으로 등록합니다. Designer가
생성한 XML이나 직렬화 리소스를 손으로 재작성하지 마십시오.
