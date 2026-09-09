// 화면 ID: WF-PAT-01 — 수검자 관리 (03 §5)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception.Presenters
{
    /// <summary>
    /// WF-PAT-01 수검자 관리 (03 §5).
    /// 비어 있음 판정은 여기가 한다 — 길이·형식은 Service 다 (킷 §6).
    /// </summary>
    public sealed class PatientManagementPresenter
    {
        private readonly IPatientManagementView _view;
        private readonly IPatientService _service;

        public PatientManagementPresenter(IPatientManagementView view, IPatientService service)
        {
            _view = view;
            _service = service;

            _view.SearchRequested += OnSearchRequested;
            _view.SelectionChanged += OnSelectionChanged;

            _view.RowSelected = false;
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

            // 03 §5.3 — 최소 1개 조건이 있어야 조회한다. 빈 문자열은 미입력으로 본다.
            // DB 도 103 으로 막지만(05 §7.2) 화면에서 먼저 안내한다 (킷 §6).
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

            // 03 §5.3 · §5.5 — 재조회 시 선택행과 우측 상세를 초기화한다.
            _view.Rows = result.Value;
            _view.Detail = null;
            _view.RowSelected = false;
        }

        private void OnSelectionChanged(object sender, long? patientId)
        {
            if (patientId == null)
            {
                _view.Detail = null;
                _view.RowSelected = false;
                return;
            }

            _view.RowSelected = true;

            OperationResult<PatientDetailDto> result;
            try
            {
                result = _service.GetDetail(patientId.Value);
            }
            catch (Exception)
            {
                _view.Detail = null;
                _view.ShowMessage("수검자 상세를 조회하지 못했습니다.");
                return;
            }

            if (!result.IsSuccess)
            {
                _view.Detail = null;
                _view.ShowMessage(result.Message);
                return;
            }

            _view.Detail = result.Value;
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
