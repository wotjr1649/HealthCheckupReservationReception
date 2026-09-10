using System;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// WF-00 MainForm Shell (03 §4). DevExpress 타입이 나타나지 않는다 (킷 §2).
    /// </summary>
    public interface IMainView
    {
        event EventHandler ShellLoaded;

        /// <summary>Ribbon Page 를 고르거나 [휴무일 관리] 를 눌렀다 (03 §1.1 상단 업무 Navigation).</summary>
        event EventHandler<BusinessNavigation> NavigationRequested;

        string WorkStatusText { set; }
        string OperatorText { set; }

        /// <summary>
        /// 그 업무 화면을 앞에 세운다. 2026-09-10 사용자 결정 — 한 번에 하나만 뜨고
        /// Tab 스트립도 닫기도 없다. 전달키가 없는 화면만 이것으로 세운다.
        /// </summary>
        void ShowBusinessScreen(BusinessTab screen);

        /// <summary>
        /// 03 §3 호출계약 `BeginNewReservation(Context, PatientId?, Source)` — 신규예약 화면을
        /// 그 Context 와 전달키로 세운다. 화면 하나를 Normal 과 WalkIn 이 나눠 쓴다 (§8.10 · §9.8).
        /// </summary>
        void BeginNewReservation(ReservationContext context, long? patientId, NavigationSource source);

        /// <summary>
        /// 03 §3 호출계약 `OpenWorkbench(WorkContext, WorkId?)` — 예약/접수 공통 Workbench 를
        /// 그 Context 로 세운다. <paramref name="workId"/> 가 있으면 조회조건과 무관하게 그
        /// 한 건을 직접 조회해 자동 선택한다 (§9.1 WorkId Targeted Navigation).
        /// </summary>
        void OpenWorkbench(WorkContext context, long? workId);

        void SelectNavigationPage(BusinessNavigation page);

        void ShowMessage(string message);
        void ShowHolidayManagement();
    }

    /// <summary>
    /// 03 §1.1 상단 업무 Navigation 다섯.
    /// 앞 넷은 `RibbonPage` 이고 <see cref="HolidayManagement"/> 만 Page 가 아니다 —
    /// 03 §24.2 가 Tab 을 열지 않고 Modal 을 여는 버튼으로 규정한다.
    /// </summary>
    public enum BusinessNavigation
    {
        PatientManagement,   // 수검자 관리   → WF-PAT-01
        NewReservation,      // 신규 예약     → WF-RSV-01
        ReservationDesk,     // 예약 관리     → WF-WRK-01 Reservation Context
        ReceptionDesk,       // 접수 관리     → WF-WRK-01 Reception Context
        HolidayManagement    // 휴무일 관리   → DLG-HOL-01 Modal
    }

    /// <summary>
    /// 업무 화면 셋. 예약 관리와 접수 관리는 같은 화면 하나를 나눠 쓴다 (03 §1.1 · §4.3).
    /// </summary>
    public enum BusinessTab
    {
        PatientManagement,
        NewReservation,
        Workbench
    }

    /// <summary>
    /// 03 §8.10 — 신규예약 화면의 Context. WalkIn 은 접수 관리의 [현장 당일예약] 이 같은
    /// 화면을 예약일=오늘 ReadOnly 로 빌려 쓰는 것이다 (§9.8). 정원·TGT·NEX·AEX 규칙은
    /// Normal 과 같아서 화면을 두 벌 만들지 않는다.
    /// </summary>
    public enum ReservationContext
    {
        Normal,
        WalkIn
    }

    /// <summary>
    /// 03 §9.1 — Workbench 화면 하나를 나눠 쓰는 두 Context. 같은 화면에 서로 다른 Ribbon
    /// Action 이 걸리므로 (§9.6 · §9.7) 화면을 세울 때 이 값이 함께 간다.
    /// </summary>
    public enum WorkContext
    {
        Reservation,
        Reception
    }

    /// <summary>
    /// 03 §3 호출계약의 `Source` — 어느 진입점에서 왔는가. §8.10 의 폐기 트리거
    /// 「다른 Flow가 같은 화면을 재사용」이 같은 Flow 재진입과 갈리려면 이 값이 필요하다.
    /// </summary>
    public enum NavigationSource
    {
        Navigation,          // 상단 [신규 예약] Page
        PatientManagement,   // WF-PAT-01 [신규예약]
        ReceptionDesk        // WF-WRK-01 Reception [현장 당일예약]
    }
}
