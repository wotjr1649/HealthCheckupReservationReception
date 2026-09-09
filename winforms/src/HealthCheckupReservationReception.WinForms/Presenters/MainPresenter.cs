// 화면 ID: WF-00 — MainForm Shell (03 §4)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception.Presenters
{
    /// <summary>
    /// WF-00 MainForm Shell 의 Presenter (03 §1.3 · §4).
    /// 어느 Tab 이 열려 있는지와 Workbench 의 Context 는 여기가 들고 있는다 —
    /// 화면은 그리기만 한다 (킷 §2).
    /// </summary>
    public sealed class MainPresenter
    {
        private const string StatusPrefix = "업무 상태 : ";
        private const string OperatorPrefix = "조작자 : ";

        private readonly IMainView _view;
        private readonly ICommonStatusService _service;
        private readonly string _operatorName;
        private readonly HashSet<BusinessTab> _openTabs = new HashSet<BusinessTab>();

        // 03 §9.1 — Workbench 는 Tab 하나를 Reservation / Reception 두 Context 가 나눠 쓴다.
        private BusinessNavigation _workbenchContext = BusinessNavigation.ReservationDesk;

        // 휴무일 관리 Page 에서 돌아올 자리. Shell 은 첫 Page 가 선택된 채 열린다 (03 §1.1 순서).
        private BusinessNavigation _lastBusinessPage = BusinessNavigation.PatientManagement;

        public MainPresenter(IMainView view, ICommonStatusService service, string operatorName)
        {
            _view = view;
            _service = service;
            _operatorName = operatorName;

            _view.ShellLoaded += OnShellLoaded;
            _view.NavigationRequested += OnNavigationRequested;
            _view.TabCloseRequested += OnTabCloseRequested;
            _view.TabActivated += OnTabActivated;
        }

        private void OnShellLoaded(object sender, EventArgs e)
        {
            _view.OperatorText = OperatorPrefix +
                (string.IsNullOrWhiteSpace(_operatorName) ? "(미지정)" : _operatorName.Trim());
            RefreshWorkStatus();
        }

        /// <summary>
        /// 308·309 는 시각이 바뀌면 뒤집힌다. 한 번 읽고 세션 내내 쓰지 않는다 (07 §14.3 A-01).
        /// </summary>
        public void RefreshWorkStatus()
        {
            OperationResult<CommonWorkStatusDto> result;
            try
            {
                result = _service.GetCurrent();
            }
            catch (Exception)
            {
                // 예외 본문을 화면에 싣지 않는다 — provider 메시지가 DB·머신 정보를 드러낸다 (킷 §6).
                Block("확인 불가", "업무 상태를 확인하지 못했습니다.");
                return;
            }

            if (!result.IsSuccess)
            {
                Block("확인 불가", result.Message);
                return;
            }

            _view.WorkStatusText = FormatWorkStatus(result.Value);

            // 03 §1.3 · §5.2 · §9.6 · §9.7 — 공통 업무불가면 업무 수행 Action 을 비활성한다.
            _view.BusinessActionsEnabled = result.Value.IsWorkAllowed;
        }

        private void Block(string statusTail, string message)
        {
            _view.WorkStatusText = StatusPrefix + statusTail;
            _view.BusinessActionsEnabled = false;
            _view.ShowMessage(message);
        }

        /// <summary>
        /// 운영시간·휴무일 문구를 화면이 만들지 않는다. DB 가 돌려준 차단메시지와 운영시각을
        /// 그대로 쓴다 — 임계값·시각을 C# 에 심지 않는다 (07 §9).
        /// </summary>
        private static string FormatWorkStatus(CommonWorkStatusDto status)
        {
            if (status.IsWorkAllowed)
            {
                return StatusPrefix + "업무 가능";
            }

            string reason = string.IsNullOrWhiteSpace(status.BlockMessage)
                ? "업무 불가"
                : status.BlockMessage.Trim();

            if (status.BlockCode == (int)DbCode.CenterClosed && !string.IsNullOrWhiteSpace(status.HolidayName))
            {
                reason = reason + " (" + status.HolidayName.Trim() + ")";
            }
            else if (status.BlockCode == (int)DbCode.OutsideHours)
            {
                reason = reason + " (" + Hhmm(status.OpenTime) + "~" + Hhmm(status.CloseTime) + ")";
            }

            return StatusPrefix + "업무 불가 — " + reason;
        }

        private static string Hhmm(TimeSpan time)
        {
            return time.ToString(@"hh\:mm");
        }

        private void OnNavigationRequested(object sender, BusinessNavigation target)
        {
            // 03 §24.2 — 휴무일 관리는 업무 Tab 을 열지 않고 Modal 을 연다.
            // 공통 업무조건도 이 화면에는 걸리지 않으므로 여기서 막지 않는다.
            if (target == BusinessNavigation.HolidayManagement)
            {
                // 먼저 직전 Page 로 돌려놓고 연다. 반대로 하면 빈 Ribbon 이 Modal 뒤에 남는다.
                _view.SelectNavigationPage(_lastBusinessPage);
                _view.ShowHolidayManagement();
                return;
            }

            _lastBusinessPage = target;
            BusinessTab tab = TabOf(target);

            if (tab == BusinessTab.Workbench)
            {
                _workbenchContext = target;
            }

            if (_openTabs.Add(tab))
            {
                _view.OpenTab(tab, CaptionOf(tab));
            }
            else
            {
                // 03 §4.3 — 같은 Tab 을 다시 부르면 새로 만들지 않고 기존 것을 살린다.
                // Workbench 는 Context 가 바뀌었을 수 있으므로 Caption 을 다시 쓴다 (§9.1).
                _view.SetTabCaption(tab, CaptionOf(tab));
            }

            _view.ActivateTab(tab);
        }

        private void OnTabCloseRequested(object sender, BusinessTab tab)
        {
            if (_openTabs.Remove(tab))
            {
                _view.CloseTab(tab);
            }
        }

        private void OnTabActivated(object sender, BusinessTab tab)
        {
            // 03 §4.2 — Tab 전환 시 그 Tab 의 Ribbon Page 를 활성화한다.
            _view.SelectNavigationPage(NavigationOf(tab));
        }

        private static BusinessTab TabOf(BusinessNavigation target)
        {
            switch (target)
            {
                case BusinessNavigation.PatientManagement:
                    return BusinessTab.PatientManagement;
                case BusinessNavigation.NewReservation:
                    return BusinessTab.NewReservation;
                default:
                    return BusinessTab.Workbench;
            }
        }

        private BusinessNavigation NavigationOf(BusinessTab tab)
        {
            switch (tab)
            {
                case BusinessTab.PatientManagement:
                    return BusinessNavigation.PatientManagement;
                case BusinessTab.NewReservation:
                    return BusinessNavigation.NewReservation;
                default:
                    return _workbenchContext;
            }
        }

        private string CaptionOf(BusinessTab tab)
        {
            switch (tab)
            {
                case BusinessTab.PatientManagement:
                    return "수검자 관리";
                case BusinessTab.NewReservation:
                    return "신규 예약";
                default:
                    // 03 §9.1 — Context 에 따라 Caption 이 바뀐다.
                    return _workbenchContext == BusinessNavigation.ReceptionDesk ? "접수 관리" : "예약 관리";
            }
        }
    }
}
