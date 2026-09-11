// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception.Presenters
{
    /// <summary>
    /// DLG-LOG-01 변경이력 열람 (03 §23).
    ///
    /// 화면이 하는 일이 하나뿐이라 Presenter 도 하나다 — 열릴 때 `SP-LOG-01` 을 한 번 부른다.
    ///
    /// [X] **0건을 오류로 말하지 않는다.** 계약이 0건을 `결과코드=0` 으로 정했고 (05 §8.3)
    ///     03 §23.4 가 *"오류가 아니다"* 라고 못박았다. 빈 Grid 안내는 화면이 갖는다.
    /// </summary>
    public sealed class ChangeLogPresenter
    {
        private readonly IChangeLogView _view;
        private readonly IChangeLogService _service;

        public ChangeLogPresenter(IChangeLogView view, IChangeLogService service)
        {
            _view = view;
            _service = service;
        }

        public void Begin(ChangeLogTarget target)
        {
            if (target == null)
            {
                _view.ValidationMessage = "대상을 알 수 없습니다.";
                return;
            }

            _view.Subject = target.Caption;
            _view.ValidationMessage = null;

            OperationResult<ChangeLogReadDto> result;
            try
            {
                result = _service.Read(target.TargetTable, target.TargetKey);
            }
            catch (Exception)
            {
                // 예외 본문을 화면에 싣지 않는다 (킷 §6).
                _view.ValidationMessage = "변경이력을 조회하지 못했습니다.";
                _view.Rows = new List<ChangeLogItemDto>();
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                _view.ValidationMessage = result == null
                    ? "변경이력을 조회하지 못했습니다."
                    : result.Message;
                _view.Rows = new List<ChangeLogItemDto>();
                return;
            }

            _view.Rows = result.Value.Rows;
        }
    }
}
