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
        /// <summary>
        /// 예약 창구가 다루는 상태 — 살아 있는 예약과 취소된 예약이다 (2026-09-11 사용자 지시).
        /// `RCP` 는 없다: 접수된 건은 접수 창구의 것이다.
        /// </summary>
        private static readonly string[] ReservationStatuses =
        {
            DbWorkStatus.Reserved, DbWorkStatus.CancelledReservation,
        };

        /// <summary>
        /// 접수 창구가 다루는 상태 — **받을 사람(`RSV`)과 받은 사람(`RCP`)과 무른 건(`CNC`)** 이다.
        ///
        /// [X] `RSV` 가 여기 있는 것이 이 회차의 핵심이다. 접수의 입력은 예약 건이므로 그것이
        ///     보이지 않으면 접수 창구는 자기 탭에서 할 일이 없다 — 지난 회차에 `[접수]` 버튼을
        ///     걷은 것이 그 증상이었고, 고쳤어야 하는 것은 버튼이 아니라 이 목록이다.
        ///
        /// 그래서 오늘의 `RSV` 한 행은 두 탭에 다 보인다. 중복이 아니라 두 창구가 같은 건을
        /// 다른 이유로 보는 것이고, **명령은 여전히 한 탭에만 있다** (Ribbon UX Guide).
        /// </summary>
        private static readonly string[] ReceptionStatuses =
        {
            DbWorkStatus.Reserved, DbWorkStatus.Received, DbWorkStatus.CancelledReception,
        };

        private readonly IWorkbenchView _view;
        private readonly IWorkService _service;
        private readonly ICommonStatusService _statusService;

        // 03 §9.1 — 상단에서 [예약 관리] 로 들어오는 것이 기본이다.
        private WorkContext _context = WorkContext.Reservation;
        private string[] _statuses = ReservationStatuses;

        public WorkbenchPresenter(IWorkbenchView view, IWorkService service, ICommonStatusService statusService)
        {
            _view = view;
            _service = service;
            _statusService = statusService;

            _view.SearchRequested += OnSearchRequested;
            _view.SelectionChanged += OnSelectionChanged;

            _view.ContextTitle = TitleOf(_context);
            _view.StatusChoices = _statuses;
            ClearSelection();
        }

        /// <summary>
        /// 03 §9.1 Context 전환. 조회조건과 Grid 결과는 그대로 두고 **선택상태만 승계하지
        /// 않는다** — 같은 행이 다른 Context 에서 다른 Action 을 갖기 때문이다 (§9.6 · §9.7).
        /// </summary>
        public void OpenContext(WorkContext context, long? workId)
        {
            _context = context;
            _statuses = StatusesOf(context);
            _view.ContextTitle = TitleOf(context);
            _view.StatusChoices = _statuses;
            ClearSelection();

            if (workId != null)
            {
                Target(workId.Value);
                return;
            }

            // 2026-09-11 — 탭을 열 때마다 그 창구의 기간으로 세우고 다시 조회한다.
            // 예전에는 목록을 그대로 두어 두 탭이 **같은 목록**이었다: 조회조건도 부르는 SP 도
            // 같았고 다른 것은 제목과 Ribbon 뿐이었다.
            // [X] 순서가 중요하다. `Search` 가 첫 줄에서 ValidationMessage 를 비우므로
            //     기간 경고를 먼저 적으면 그대로 사라진다 — 조회 뒤에 다시 적는다.
            string warning = ResetRange();
            Search();
            if (warning != null)
            {
                _view.ValidationMessage = warning;
            }
        }

        /// <summary>
        /// 그 창구의 기본 기간을 세운다.
        ///
        /// 접수는 당일 업무다 (05 §8.2 `START_RECEPTION` 이 `예약일=오늘`) — 오늘 하루로
        /// 세운다. 예약은 앞으로의 일정이므로 오늘부터 열어 둔다. 둘 다 **기본값일 뿐**이고
        /// 사용자가 바꿀 수 있다 (2026-09-11 사용자 결정): 어제 무른 접수를 되짚을 길이 남는다.
        ///
        /// [X] **오늘을 PC 시계에서 얻지 않는다.** 창구 PC 가 하루 어긋나면 접수 창구의 기본
        ///     목록이 통째로 빈다. `SP-CMN-01` 이 DB 오늘날짜를 준다 — 읽지 못하면 기간을
        ///     건드리지 않고 사유만 적는다. 잘못된 날로 세우느니 사용자가 고르게 둔다.
        /// </summary>
        private string ResetRange()
        {
            OperationResult<CommonWorkStatusDto> status;
            try
            {
                status = _statusService.GetCurrent();
            }
            catch (Exception)
            {
                status = null;
            }

            if (status == null || !status.IsSuccess || status.Value == null)
            {
                return "오늘 날짜를 확인하지 못해 기간을 세우지 못했습니다.";
            }

            DateTime today = status.Value.Today.Date;
            _view.ResetSearchRange(today, _context == WorkContext.Reception ? today : (DateTime?)null);
            return null;
        }

        private static string[] StatusesOf(WorkContext context)
        {
            return context == WorkContext.Reception ? ReceptionStatuses : ReservationStatuses;
        }

        /// <summary>
        /// 03 §9.1 WorkId Targeted Navigation — 조회조건과 무관하게 그 한 건을 직접 조회하고
        /// 행을 자동선택한다. 신규예약 저장 성공(§8.11)과 접수 Shortcut 이 이 길로 돌아온다.
        ///
        /// [X] 상세를 **먼저** 부른다. 그 업무가 며칠인지 알아야 목록을 그 날로 좁힐 수 있기
        ///     때문이다 — 조회조건만으로는 방금 저장한 건이 목록에 없을 수 있다. 행을 고르면
        ///     선택 이벤트가 상세를 한 번 더 읽지만, 그 값이 화면에 서는 최신값이다.
        /// </summary>
        private void Target(long workId)
        {
            OperationResult<WorkDetailReadDto> detail;
            try
            {
                detail = _service.GetDetail(workId);
            }
            catch (Exception)
            {
                _view.ValidationMessage = "업무 상세를 조회하지 못했습니다.";
                return;
            }

            if (detail == null || !detail.IsSuccess)
            {
                _view.ValidationMessage = detail == null ? "업무 상세를 조회하지 못했습니다." : detail.Message;
                return;
            }

            _view.FocusSearchOn(detail.Value.Detail.ReserveDate.Date);
            Search();

            if (!_view.SelectWork(workId))
            {
                _view.ValidationMessage = "방금 저장한 업무를 목록에서 찾지 못했습니다.";
            }
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
            _view.Rows = Narrow(result.Value);
            ClearSelection();
        }

        /// <summary>
        /// 그 창구가 다루는 상태만 남긴다 (2026-09-11).
        ///
        /// [X] **SP 로 거르지 못한다.** `@상태코드` 는 한 번에 한 값이거나 NULL 이다
        ///     (05 §8.1) — 「`RSV` 와 `RCP` 둘」을 물을 방법이 없다. 드롭다운에서 한 값을
        ///     고르면 SP 가 이미 거른 뒤라 이 걸음은 통과만 하고, `전체` 일 때만 실제로 좁힌다.
        ///     행 수가 하루치라 화면에서 거르는 비용은 작다.
        /// </summary>
        private IList<WorkListItemDto> Narrow(IList<WorkListItemDto> rows)
        {
            if (rows == null)
            {
                return new List<WorkListItemDto>();
            }

            var kept = new List<WorkListItemDto>();
            foreach (WorkListItemDto row in rows)
            {
                if (Array.IndexOf(_statuses, row.StatusCode) >= 0)
                {
                    kept.Add(row);
                }
            }

            return kept;
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
