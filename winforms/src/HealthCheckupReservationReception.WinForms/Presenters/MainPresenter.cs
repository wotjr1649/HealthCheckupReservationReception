using System;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception.Presenters
{
    /// <summary>
    /// WF-00 MainForm Shell 의 Presenter (03 §1.3 · §4).
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
                _view.WorkStatusText = StatusPrefix + "확인 불가";
                _view.ShowMessage("업무 상태를 확인하지 못했습니다.");
                return;
            }

            if (!result.IsSuccess)
            {
                _view.WorkStatusText = StatusPrefix + "확인 불가";
                _view.ShowMessage(result.Message);
                return;
            }

            _view.WorkStatusText = FormatWorkStatus(result.Value);
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
            // EXTENSION POINT: 업무 Tab 을 연다 (03 §4.3 Single Instance).
            // 대상 화면이 아직 하나도 없어 여기서 열 것이 없다. 첫 Tab 화면과 함께 붙인다.
        }
    }
}
