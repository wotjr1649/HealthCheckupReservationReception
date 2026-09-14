// 화면 ID: DLG-RCP-01 — 접수 처리 (03 §11)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception.Presenters
{
    /// <summary>
    /// DLG-RCP-01 접수 처리 (03 §11).
    ///
    /// **화면이 접수 가능 여부를 계산하지 않는다.** 05 §8.2 RS4 의 `START_RECEPTION` 행이
    /// 허용여부와 사유를 함께 주고(차단 우선순위 `502 → 308/309 → 503 → 304`), 화면은 그것을
    /// 읽어 주기만 한다. 03 §11.3 의 다섯 조건을 여기 다시 적으면 판정이 두 곳이 된다.
    ///
    /// [X] **창을 열 때 상세를 다시 읽는다.** 목록 행에는 `행버전` 이 없고(05 §8.1 RS1),
    ///     Workbench 가 들고 있던 값은 그 사이에 낡을 수 있다 — 03 §11.3 이 *"최신 RowVersion
    ///     일치"* 를 가능조건에 넣은 이유다.
    /// </summary>
    public sealed class ReceptionPresenter
    {
        private readonly IReceptionView _view;
        private readonly IWorkService _service;
        private readonly string _operatorName;

        private WorkDetailDto _detail;

        public ReceptionPresenter(IReceptionView view, IWorkService service, string operatorName)
        {
            _view = view;
            _service = service;
            _operatorName = operatorName;

            _view.ReceiveRequested += OnReceiveRequested;
        }

        /// <summary>
        /// **부모가 읽어 둔 한 벌을 받아서 연다** (2026-09-14 사용자 지시). 이 창이 그리는 것은
        /// RS1·RS2·RS3·RS4 넷이고 넷 다 `WF-WRK-01` 이 행을 고를 때 이미 받은 것이다 —
        /// 같은 업무ID 로 `SP-WRK-02` 를 한 번 더 부르지 않는다.
        ///
        /// [!] **낡을 수 있다.** 그 사이 마감이 지났으면 `[접수]` 가 열린 채로 남고, 저장이
        ///     `304` 로 막는다. 화면이 미리 닫지 않는 것이 R12 다 — 판정은 SP 가 한다.
        /// </summary>
        public void Begin(WorkDetailReadDto read)
        {
            Load(read);
        }

        /// <summary>
        /// **저장이 막힌 뒤 최신값을 다시 세운다.** 진입은 부모가 준 한 벌로 열지만, `601`·`304`
        /// 로 막혔다는 것은 그 스냅샷이 낡았다는 뜻이므로 여기서만 DB 를 다시 본다
        /// (2026-09-14). 이것이 낡은 스냅샷을 받아들이는 대가를 갚는 자리다.
        /// </summary>
        private void Reload(long workId)
        {
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

            Load(result.Value);
        }

        private void Load(WorkDetailReadDto read)
        {
            _detail = null;
            _view.ReceiveEnabled = false;

            if (read == null || read.Detail == null)
            {
                Clear("업무 상세를 받지 못했습니다.");
                return;
            }

            _detail = read.Detail;
            _view.Detail = read.Detail;
            _view.NexItems = read.NexItems;
            _view.AexItems = read.AexItems;

            WorkActionDto start = Find(read.Actions, DbWorkAction.StartReception);
            if (start == null)
            {
                // RS4 는 정확히 5행이고 코드가 고정이다 (05 §8.2). 없으면 계약 위반이다.
                Clear("접수 가능 여부를 받지 못했습니다.");
                return;
            }

            _view.ReceiveEnabled = start.Allowed;
            _view.EligibilityText = start.Allowed
                ? "접수 가능"
                : "접수 불가 — " + Reason(start);
            _view.ValidationMessage = null;
        }

        private void OnReceiveRequested(object sender, EventArgs e)
        {
            if (_detail == null)
            {
                return;
            }

            long workId = _detail.WorkId;
            OperationResult<WorkSaveReadDto> result;
            try
            {
                result = _service.CompleteReception(new WorkActionRequest
                {
                    WorkId = workId,
                    RowVersion = _detail.RowVersion,
                    OperatorName = _operatorName,
                });
            }
            catch (Exception)
            {
                _view.ValidationMessage = "접수하지 못했습니다.";
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                _view.ValidationMessage = result == null ? "접수하지 못했습니다." : result.Message;
                return;
            }

            if (result.Value.Result != null && !result.Value.Result.Success)
            {
                // [X] **사유를 적기 전에 다시 읽는다.** Load 가 ValidationMessage 를 지우므로
                //     순서가 뒤집히면 사유가 사라진다 — WF-RSV-01 이 같은 함정을 밟았다.
                Reload(workId);
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
            _view.AexItems = new List<WorkExamItemDto>();
            _view.EligibilityText = string.Empty;
            _view.ReceiveEnabled = false;
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

        /// <summary>사유메시지가 비어 오면 코드라도 보인다 — 이유 없이 닫힌 버튼은 고장이다.</summary>
        private static string Reason(WorkActionDto action)
        {
            return string.IsNullOrWhiteSpace(action.ReasonMessage)
                ? "사유코드 " + action.ReasonCode
                : action.ReasonMessage;
        }
    }
}
