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
        /// Tab 스트립도 닫기도 없다.
        /// </summary>
        void ShowBusinessScreen(BusinessTab screen);

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
}
