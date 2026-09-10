// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception.Presenters
{
    /// <summary>
    /// WF-WRK-01 예약/접수 공통 Workbench (03 §9).
    ///
    /// 어느 Context 인지는 여기가 들고 있는다 — 1구간에서 지웠던 `_workbenchContext` 가
    /// 돌아온 자리다. 화면은 하나이고 §9.6·§9.7 이 Context 마다 다른 Ribbon Action 을 건다.
    ///
    /// 비어 있음·From&gt;To 판정은 여기가 한다. 길이는 Service 다 (킷 §6).
    /// </summary>
    public sealed class WorkbenchPresenter
    {
        private readonly IWorkbenchView _view;
        private readonly IWorkService _service;

        // 03 §9.1 — 상단에서 [예약 관리] 로 들어오는 것이 기본이다.
        private WorkContext _context = WorkContext.Reservation;

        public WorkbenchPresenter(IWorkbenchView view, IWorkService service)
        {
            _view = view;
            _service = service;

            _view.SearchRequested += OnSearchRequested;
            _view.SelectionChanged += OnSelectionChanged;

            _view.ContextTitle = TitleOf(_context);
            ClearSelection();
        }

        /// <summary>
        /// 03 §9.1 Context 전환. 조회조건과 Grid 결과는 그대로 두고 **선택상태만 승계하지
        /// 않는다** — 같은 행이 다른 Context 에서 다른 Action 을 갖기 때문이다 (§9.6 · §9.7).
        /// </summary>
        public void OpenContext(WorkContext context, long? workId)
        {
            _context = context;
            _view.ContextTitle = TitleOf(context);
            ClearSelection();

            // EXTENSION POINT: WorkId Targeted Navigation (03 §9.1) — 조회조건과 무관하게
            //                  그 한 건을 직접 조회해 행을 자동선택한다. 신규예약 저장 성공과
            //                  접수 Shortcut 이 이 길로 돌아온다.
        }

        /// <summary>
        /// 화면을 열면 한 번 조회한다. 조회조건의 기간이 오늘로 서 있으므로 03 §9.3 의
        /// `최소 하나의 실질 조건` 을 이미 만족한다.
        ///
        /// </summary>
        public void LoadInitial()
        {
            Search();
        }

        private void OnSearchRequested(object sender, EventArgs e)
        {
            Search();
        }

        /// <summary>
        /// [X] **조회 실패를 모달로 알리지 않는다.** 셋 다 같은 자리에 적는다 — 조건 오류도,
        ///     조건 없음도, SP 실패도 사용자가 보는 것은 `왜 목록이 비었는가` 하나이고 그
        ///     답이 Grid 바로 위에 있어야 한다. 03 §9.3 이 Inline 을 규정한 것도 같은 이유다.
        ///
        ///     모달로 두면 창을 열자마자 뜨는 것을 막느라 조용히 삼켜야 하고, 그러면 실패가
        ///     아예 보이지 않는다 — 2026-09-10 에 실제로 "조회가 안 된다" 로 보고됐다.
        ///     그때는 성공한 0건이었는데 화면이 아무 말도 하지 않아 구별할 길이 없었다.
        /// </summary>
        private void Search()
        {
            _view.ValidationMessage = null;

            DateTime? from = _view.FromDate;
            DateTime? to = _view.ToDate;

            // 03 §9.3 — 날짜는 양끝 포함이고 From>To 는 Inline 오류다.
            if (from != null && to != null && from.Value.Date > to.Value.Date)
            {
                _view.ValidationMessage = "시작일이 종료일보다 늦습니다.";
                return;
            }

            var request = new WorkSearchRequest
            {
                FromDate = from,
                ToDate = to,
                StatusCode = _view.StatusCode,
                ChartNo = _view.ChartNo,
                Name = _view.PatientName,
            };

            // 03 §9.3 — 최소 하나의 실질 조건이 필요하다. 상태는 세지 않는다.
            // SP 도 같은 판정을 하고 103 을 내지만 (05 §8.1), 왕복하지 않고 여기서 막는다.
            if (!HasCondition(request))
            {
                _view.ValidationMessage = "기간·차트번호·이름 중 하나 이상을 입력하세요.";
                return;
            }

            OperationResult<IList<WorkListItemDto>> result;
            try
            {
                result = _service.Search(request);
            }
            catch (Exception)
            {
                // 예외 본문을 화면에 싣지 않는다 (킷 §6).
                _view.ValidationMessage = "예약·접수 목록을 조회하지 못했습니다.";
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                _view.ValidationMessage = result == null
                    ? "예약·접수 목록을 조회하지 못했습니다."
                    : result.Message;
                return;
            }

            // 03 §9.4 — 재조회 시 선택·상세·Transaction Action 을 Clear 한다.
            _view.Rows = result.Value;
            ClearSelection();
        }

        private static bool HasCondition(WorkSearchRequest request)
        {
            return request.FromDate != null
                || request.ToDate != null
                || !string.IsNullOrWhiteSpace(request.ChartNo)
                || !string.IsNullOrWhiteSpace(request.Name);
        }

        private void OnSelectionChanged(object sender, long? workId)
        {
            if (workId == null)
            {
                ClearSelection();
                return;
            }

            OperationResult<WorkDetailReadDto> result;
            try
            {
                result = _service.GetDetail(workId.Value);
            }
            catch (Exception)
            {
                ClearSelection();
                _view.ShowMessage("업무 상세를 조회하지 못했습니다.");
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                ClearSelection();
                _view.ShowMessage(result == null ? "업무 상세를 조회하지 못했습니다." : result.Message);
                return;
            }

            WorkDetailReadDto read = result.Value;
            _view.Detail = read.Detail;
            _view.NexItems = read.NexItems;
            _view.AexItems = read.AexItems;
            _view.Actions = StateOf(read.Actions);
        }

        private void ClearSelection()
        {
            _view.Detail = null;
            _view.NexItems = new List<WorkExamItemDto>();
            _view.AexItems = new List<WorkExamItemDto>();
            _view.Actions = WorkActionState.None();
        }

        /// <summary>
        /// 05 §8.2 RS4 를 Ribbon 상태로 옮긴다. **허용여부를 다시 계산하지 않는다** —
        /// 상태·공통 업무조건·마감의 조합과 차단 우선순위는 DB 가 갖는다.
        ///
        /// Context 로 갈리지 않는다: 두 Ribbon Page 가 같은 다섯 동작에서 각자 쓸 것만
        /// 골라 보인다 (03 §9.6 · §9.7). 여기서 한 번 더 거르면 판정이 두 곳이 된다.
        /// </summary>
        private static WorkActionState StateOf(IList<WorkActionDto> actions)
        {
            // 03 §9.6 · §23.4 — [변경이력] 은 상태와 무관하게 행이 선택되면 열린다.
            var state = new WorkActionState { ChangeLog = true };
            if (actions == null)
            {
                return state;
            }

            foreach (WorkActionDto action in actions)
            {
                switch (action.ActionCode)
                {
                    case DbWorkAction.EditReservation: state.EditReservation = action.Allowed; break;
                    case DbWorkAction.CancelReservation: state.CancelReservation = action.Allowed; break;
                    case DbWorkAction.StartReception: state.StartReception = action.Allowed; break;
                    case DbWorkAction.EditExtra: state.EditExtra = action.Allowed; break;
                    case DbWorkAction.CancelReception: state.CancelReception = action.Allowed; break;
                }
            }

            return state;
        }

        private static string TitleOf(WorkContext context)
        {
            return context == WorkContext.Reception ? "접수 관리" : "예약 관리";
        }
    }
}
