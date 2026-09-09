// 화면 ID: DLG-PAT-02 — 수검자 선택 (03 §7)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception.Presenters
{
    /// <summary>
    /// DLG-PAT-02 수검자 선택 (03 §7). **조회계약은 WF-PAT-01 과 같다** (03 §7.2).
    ///
    /// 같은 것을 두 화면이 쓰지만 묶지 않는다 — 실제로 겹치는 것은 이미 `PatientService.Search`
    /// 안에 있고, 여기 남는 것은 화면마다 다른 조회조건 읽기와 다른 결과 처리뿐이다.
    /// 이 둘을 억지로 한 기반 Presenter 로 묶으면 View 계약 둘을 다시 추상으로 덮어야 한다 (킷 §2).
    /// </summary>
    public sealed class PatientSelectPresenter
    {
        private readonly IPatientSelectView _view;
        private readonly IPatientService _service;

        public PatientSelectPresenter(IPatientSelectView view, IPatientService service)
        {
            _view = view;
            _service = service;

            _view.SearchRequested += OnSearchRequested;
            _view.SelectionChanged += OnSelectionChanged;

            _view.SelectEnabled = false;
        }

        private void OnSearchRequested(object sender, EventArgs e)
        {
            var request = new PatientSearchRequest
            {
                ChartNo = _view.ChartNo,
                Name = _view.Name,
                SocialNumber = _view.SocialNumber,
                Birthday = _view.Birthday,
                MobilePhone = _view.MobilePhone,
            };

            // 03 §7.2 는 조회계약을 §5.3 에 위임한다 — 최소 1개 조건이 있어야 부른다.
            if (AllEmpty(request))
            {
                _view.ShowMessage("조회조건을 하나 이상 입력하십시오.");
                return;
            }

            OperationResult<IList<PatientListItemDto>> result;
            try
            {
                result = _service.Search(request);
            }
            catch (Exception)
            {
                // 예외 본문을 화면에 싣지 않는다 (킷 §6).
                _view.ShowMessage("수검자를 조회하지 못했습니다.");
                return;
            }

            if (!result.IsSuccess)
            {
                _view.ShowMessage(result.Message);
                return;
            }

            _view.Rows = result.Value;
            _view.SelectEnabled = false;
        }

        private void OnSelectionChanged(object sender, long? patientId)
        {
            // 03 §7.2 — 상세를 읽지 않는다. 이 창은 PatientId 하나만 돌려준다.
            _view.SelectEnabled = patientId != null;
        }

        private static bool AllEmpty(PatientSearchRequest r)
        {
            return string.IsNullOrWhiteSpace(r.ChartNo)
                && string.IsNullOrWhiteSpace(r.Name)
                && string.IsNullOrWhiteSpace(r.SocialNumber)
                && string.IsNullOrWhiteSpace(r.Birthday)
                && string.IsNullOrWhiteSpace(r.MobilePhone);
        }
    }
}
