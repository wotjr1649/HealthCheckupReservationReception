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
            Search(false);
        }

        /// <summary>
        /// [R16] 03 §5.3 — 화면을 열 때 조건 없이 한 번 조회해 목록을 채운다.
        ///
        /// [X] **실패를 알리지 않는다.** 사용자가 부탁한 호출이 아니므로 창을 열자마자
        ///     오류창이 뜨면 안 된다. 실제로 초판이 그렇게 해서 UI 시험이 모달에 걸려
        ///     멈췄다(실측 2026-09-10). 빈 목록으로 두면 사용자가 `[조회]` 를 눌러
        ///     같은 경로를 다시 타고, 그때는 이유를 본다.
        /// </summary>
        public void LoadInitial()
        {
            Search(true);
        }

        private void Search(bool silent)
        {
            var request = new PatientSearchRequest
            {
                ChartNo = _view.ChartNo,
                Name = _view.Name,
                SocialNumber = _view.SocialNumber,
                Birthday = _view.Birthday,
                MobilePhone = _view.MobilePhone,
            };

            // 다섯 다 보낸다. 꺼 둔 조건은 화면이 값을 비우므로 (clsSearchConditions) 여기서는
            // 무엇이 켜졌는지 따지지 않는다 — 끈 칸에 남은 글자가 조회에 섞이지 않는 이유다.

            // [R16] 03 §5.3 — 조건이 하나도 없으면 **전체 목록**이다. 막지 않는다.
            //       SP 도 103 을 내지 않는다 (05 §7.2 · §13).

            OperationResult<IList<PatientListItemDto>> result;
            try
            {
                result = _service.Search(request);
            }
            catch (Exception)
            {
                // 예외 본문을 화면에 싣지 않는다 (킷 §6).
                if (!silent) { _view.ShowMessage("수검자를 조회하지 못했습니다."); }
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                if (!silent) { _view.ShowMessage(result == null ? "수검자를 조회하지 못했습니다." : result.Message); }
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
    }
}
