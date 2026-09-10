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

        /// <summary>
        /// 업무 화면이 Workbench 로 넘겨 달라고 한다 — 03 §8.5 기존 유효예약과
        /// §8.11 저장 성공이 그 길이다. Navigation 상태를 Presenter 가 갖고 있으므로
        /// 화면이 직접 <see cref="OpenWorkbench"/> 를 부르지 않고 이 이벤트로 올린다.
        /// </summary>
        event EventHandler<WorkbenchTarget> WorkbenchRequested;

        string WorkStatusText { set; }
        string OperatorText { set; }

        /// <summary>
        /// 그 업무 화면을 앞에 세운다. 2026-09-10 사용자 결정 — 한 번에 하나만 뜨고
        /// Tab 스트립도 닫기도 없다. 전달키가 없는 화면만 이것으로 세운다.
        /// </summary>
        void ShowBusinessScreen(BusinessTab screen);

        /// <summary>
        /// 03 §3 호출계약 — 예약 **모달**을 그 수검자로 연다.
        ///
        /// **전달키 하나로 줄었다** (2026-09-11 grilling). 예전에는 `Context` 와 `Source` 를
        /// 함께 날랐는데, 진입점이 `수검자 관리 [예약]` 하나가 되면서 `Source` 는 늘 같은
        /// 값이 되었고 `Context`(일반/현장)는 조작자가 아니라 **시각**이 가르는 것이 되었다
        /// (00 RP-05). 둘 다 변하지 않는 값을 나르는 매개변수였다.
        /// </summary>
        void BeginNewReservation(long patientId);

        /// <summary>
        /// 03 §3 호출계약 `OpenWorkbench(WorkContext, WorkId?)` — 예약/접수 공통 Workbench 를
        /// 그 Context 로 세운다. <paramref name="workId"/> 가 있으면 조회조건과 무관하게 그
        /// 한 건을 직접 조회해 자동 선택한다 (§9.1 WorkId Targeted Navigation).
        /// </summary>
        void OpenWorkbench(WorkContext context, long? workId);

        void SelectNavigationPage(BusinessNavigation page);

        void ShowMessage(string message);
    }

    /// <summary>
    /// 상단 업무 Navigation. **여기 있는 것은 전부 「가는 곳」이다** (2026-09-11 사용자 결정).
    ///
    /// 예전에는 `신규 예약` 과 `휴무일 관리` 도 Page 였는데, 둘 다 눌러도 아무 데도 가지 않고
    /// Modal 만 띄운 뒤 탭이 제자리로 돌아왔다. 같은 띠에서 어떤 것은 가고 어떤 것은 뜨니
    /// 어디를 눌러야 무엇이 나오는지 예측할 수 없었다 — 리본을 명령이 아니라 내비게이션으로
    /// 쓴 결과다. 둘은 명령이므로 명령 자리(화면 Action · 리본 우측 버튼)로 내렸다.
    /// </summary>
    public enum BusinessNavigation
    {
        PatientManagement,   // 수검자 관리   → WF-PAT-01
        ReservationDesk,     // 예약 관리     → WF-WRK-01 Reservation Context
        ReceptionDesk        // 접수 관리     → WF-WRK-01 Reception Context
    }

    /// <summary>
    /// 업무 판에 세워 두는 화면 둘. 예약 관리와 접수 관리는 같은 화면 하나를 나눠 쓴다
    /// (03 §1.1 · §4.3). 신규 예약은 여기 없다 — 모달이라 세워 두지 않는다.
    /// </summary>
    public enum BusinessTab
    {
        PatientManagement,
        Workbench
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
    /// 03 §9.1 WorkId Targeted Navigation 의 대상. 어느 Context 로 갈지와 어느 업무를 고를지가
    /// 늘 함께 다닌다 — 같은 WorkId 라도 예약 관리와 접수 관리에서 열리는 Action 이 다르다
    /// (§9.6 · §9.7). 그래서 둘을 갈라 나르지 않는다.
    /// </summary>
    public sealed class WorkbenchTarget
    {
        public WorkContext Context { get; set; }
        public long WorkId { get; set; }
    }
}
