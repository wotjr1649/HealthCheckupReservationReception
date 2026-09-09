using System;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// WF-00 MainForm Shell (03 §4). DevExpress 타입이 나타나지 않는다 (킷 §2).
    /// </summary>
    public interface IMainView
    {
        event EventHandler ShellLoaded;
        event EventHandler<BusinessNavigation> NavigationRequested;

        string WorkStatusText { set; }
        string OperatorText { set; }

        void ShowMessage(string message);
    }

    /// <summary>
    /// 03 §4.1 상단 업무 Navigation 다섯. 03 §2 의 화면 ID 로 가는 입구다.
    /// </summary>
    public enum BusinessNavigation
    {
        PatientManagement,   // WF-PAT-01
        NewReservation,      // WF-RSV-01
        ReservationDesk,     // WF-WRK-01  Reservation Context
        ReceptionDesk,       // WF-WRK-01  Reception Context
        HolidayManagement    // DLG-HOL-01
    }
}
