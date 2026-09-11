// 화면 ID: DLG-RCP-02 — 접수완료 추가검사 변경 (03 §12)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception.Presenters
{
    /// <summary>
    /// DLG-RCP-02 접수완료 추가검사 변경 (03 §12).
    ///
    /// **진입 가능 여부를 화면이 재지 않는다** — 05 §8.2 RS4 의 `EDIT_EXTRA` 가 허용여부와
    /// 사유를 함께 준다 (`RCP` + 현재 공통 업무 가능).
    ///
    /// [X] **동일 집합인지 화면이 견주지 않는다.** 05 §12.2 가 No-op 을 `결과코드=1` 로
    ///     정해 두었다 — 화면이 먼저 가르면 그 판정이 두 곳에 생긴다.
    /// </summary>
    public sealed class ExtraExamPresenter
    {
        private readonly IExtraExamView _view;
        private readonly IWorkService _service;
        private readonly string _operatorName;

        private WorkDetailDto _detail;

        public ExtraExamPresenter(IExtraExamView view, IWorkService service, string operatorName)
        {
            _view = view;
            _service = service;
            _operatorName = operatorName;

            _view.SaveRequested += OnSaveRequested;
        }

        public void Begin(long workId)
        {
            Load(workId);
        }

        private void Load(long workId)
        {
            _detail = null;
            _view.SaveEnabled = false;

            OperationResult<WorkDetailReadDto> result;
            try
            {
                result = _service.GetDetail(workId);
            }
            catch (Exception)
            {
                // 예외 본문을 화면에 싣지 않는다 (킷 §6).
                Clear("업무 상세를 조회하지 못했습니다.");
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                Clear(result == null ? "업무 상세를 조회하지 못했습니다." : result.Message);
                return;
            }

            WorkDetailReadDto read = result.Value;
            _detail = read.Detail;
            _view.Detail = read.Detail;
            _view.NexItems = read.NexItems;
            _view.AexOptions = read.AexOptions;
            _view.Title = "추가검사 변경";

            WorkActionDto edit = Find(read.Actions, DbWorkAction.EditExtra);
            if (edit == null)
            {
                // RS4 는 정확히 5행이고 코드가 고정이다 (05 §8.2). 없으면 계약 위반이다.
                Clear("추가검사 변경 가능 여부를 받지 못했습니다.");
                return;
            }

            _view.SaveEnabled = edit.Allowed;
            _view.ValidationMessage = edit.Allowed
                ? null
                : "변경 불가 — " + Reason(edit);
        }

        private void OnSaveRequested(object sender, EventArgs e)
        {
            if (_detail == null)
            {
                return;
            }

            long workId = _detail.WorkId;
            OperationResult<WorkSaveReadDto> result;
            try
            {
                result = _service.ChangeExtraExam(new ExtraExamChangeRequest
                {
                    WorkId = workId,
                    RowVersion = _detail.RowVersion,
                    AexSelected = _view.AexSelection,
                    OperatorName = _operatorName,
                });
            }
            catch (Exception)
            {
                _view.ValidationMessage = "추가검사를 변경하지 못했습니다.";
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                _view.ValidationMessage = result == null ? "추가검사를 변경하지 못했습니다." : result.Message;
                return;
            }

            if (result.Value.Result != null && !result.Value.Result.Success)
            {
                // [X] **사유를 적기 전에 다시 읽는다** — Load 가 메시지 칸을 지운다.
                Load(workId);
                _view.ValidationMessage = result.Value.Result.Message;
                return;
            }

            _view.Done();
        }

        private void Clear(string message)
        {
            _detail = null;
            _view.Detail = null;
            _view.NexItems = new List<WorkExamItemDto>();
            _view.AexOptions = new List<ReservationAexItemDto>();
            _view.SaveEnabled = false;
            _view.ValidationMessage = message;
        }

        private static WorkActionDto Find(IList<WorkActionDto> actions, string code)
        {
            if (actions == null)
            {
                return null;
            }

            foreach (WorkActionDto action in actions)
            {
                if (code.Equals(action.ActionCode, StringComparison.Ordinal))
                {
                    return action;
                }
            }

            return null;
        }

        private static string Reason(WorkActionDto action)
        {
            return string.IsNullOrWhiteSpace(action.ReasonMessage)
                ? "사유코드 " + action.ReasonCode
                : action.ReasonMessage;
        }
    }
}
