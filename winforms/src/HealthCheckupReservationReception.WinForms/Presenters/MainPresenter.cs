// 화면 ID: WF-00 — MainForm Shell (03 §4)
using System;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception.Presenters
{
    /// <summary>
    /// WF-00 MainForm Shell 의 Presenter (03 §1.3 · §4).
    /// 어느 업무 화면을 세울지는 여기가 정한다 — 화면은 그리기만 한다 (킷 §2).
    /// </summary>
    public sealed class MainPresenter
    {
        private const string StatusPrefix = "업무 상태 : ";
        private const string OperatorPrefix = "조작자 : ";

        private readonly IMainView _view;
        private readonly ICommonStatusService _service;
        private readonly string _operatorName;

        public MainPresenter(IMainView view, ICommonStatusService service, string operatorName)
        {
            _view = view;
            _service = service;
            _operatorName = operatorName;

            _view.ShellLoaded += OnShellLoaded;
            _view.NavigationRequested += OnNavigationRequested;
            _view.WorkbenchRequested += OnWorkbenchRequested;
        }

        private void OnShellLoaded(object sender, EventArgs e)
        {
            _view.OperatorText = OperatorPrefix +
                (string.IsNullOrWhiteSpace(_operatorName) ? "(미지정)" : _operatorName.Trim());
            RefreshWorkStatus();

            // [X] 기동 직후 Ribbon 은 첫 Page 가 선택된 채로 뜨는데 그것은 "변경"이 아니라
            //     초기값이라 SelectedPageChanged 가 나지 않는다. 그래서 업무 Tab 이 하나도
            //     서지 않고, 이미 선택된 [수검자 관리] 를 눌러도 이벤트가 없어 영원히
            //     서지 않았다. 첫 Page 의 화면은 Shell 이 직접 세운다. §14.3 A-06.
            OnNavigationRequested(this, BusinessNavigation.PatientManagement);
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

            // 03 §1.3 — 공통 업무 상태는 **표시만** 한다. R12 부터 화면은 그것으로 Action 을
            // 막지 않으며 판정은 저장 시점에 DB 가 한다 (`00` §1.1 · §9 9항).
            _view.WorkStatusText = FormatWorkStatus(result.Value);
        }

        /// <summary>
        /// 업무 상태를 못 읽었을 때다. 상태를 `확인 불가` 로 적고 알리기만 한다 —
        /// [R12] 화면이 Action 을 닫지 않는다. 상태를 못 읽은 것이 업무 불가를 뜻하지도 않고,
        /// 닫아 버리면 사용자가 저장을 눌러 DB 판정을 받아볼 길이 사라진다.
        /// </summary>
        private void Block(string statusTail, string message)
        {
            _view.WorkStatusText = StatusPrefix + statusTail;
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

        /// <summary>
        /// 03 §4.3 — 같은 화면을 다시 부르면 새로 만들지 않고 세워 둔 것을 앞에 낸다.
        /// 그 판정은 화면을 들고 있는 Shell 이 한다.
        ///
        /// **Page 는 전부 장소다** (2026-09-11 사용자 결정). 예전에는 `신규 예약` 과
        /// `휴무일 관리` 가 눌러도 아무 데도 가지 않는 Page 였다 — 같은 띠에서 어떤 것은
        /// 가고 어떤 것은 떠서 어디를 눌러야 무엇이 나오는지 예측할 수 없었다. 둘 다
        /// 명령이므로 명령 자리로 내렸고, 그래서 이 갈래가 특례 없이 셋으로 남는다.
        ///
        /// 상단 Navigation 에 전달키는 없다 — PatientId·WorkId 를 들고 오는 진입점은
        /// 화면 쪽 Action 이다 (03 §3 Navigation 전달키).
        /// </summary>
        private void OnNavigationRequested(object sender, BusinessNavigation target)
        {
            switch (target)
            {
                case BusinessNavigation.ReservationDesk:
                    _view.OpenWorkbench(WorkContext.Reservation, null);
                    break;
                case BusinessNavigation.ReceptionDesk:
                    // 03 §9.1 — 화면은 예약 관리와 같은 하나다. 가르는 것은 이 Context 뿐이다.
                    _view.OpenWorkbench(WorkContext.Reception, null);
                    break;
                default:
                    _view.ShowBusinessScreen(BusinessTab.PatientManagement);
                    break;
            }
        }

        /// <summary>
        /// 03 §8.5 기존 유효예약 · §8.11 저장 성공 — 업무 화면이 Workbench 로 넘겨 달라고 한다.
        ///
        /// Ribbon Page 도 함께 옮긴다. 화면만 바꾸고 Page 를 두면 열려 있는 Action 이 그 화면의
        /// 것이 아니게 된다 — Navigation 상태를 한 곳에 두는 이유가 이것이다.
        /// </summary>
        private void OnWorkbenchRequested(object sender, WorkbenchTarget target)
        {
            BusinessNavigation page = target.Context == WorkContext.Reception
                ? BusinessNavigation.ReceptionDesk
                : BusinessNavigation.ReservationDesk;

            _view.SelectNavigationPage(page);
            _view.OpenWorkbench(target.Context, target.WorkId);
        }
    }
}
