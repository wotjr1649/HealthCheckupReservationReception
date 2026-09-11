// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System;
using System.Collections.Generic;
using System.Globalization;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception.Presenters
{
    /// <summary>
    /// DLG-HOL-01 휴무일 관리 (03 §24).
    ///
    /// **판정을 여기서 하지 않는다.** 날짜 중복(`801`)·법정공휴일 편집(`802`)·행버전 충돌(`601`)은
    /// 전부 SP 가 낸다 (03 §24.5 *"최종 판정은 DB"*). 이 계층이 하는 일은 셋뿐이다 —
    /// 언제 물을지 정하고, 받은 것을 화면 자리에 놓고, 어느 Action 을 여닫을지 고른다.
    ///
    /// [X] **만료 경고의 임계 숫자를 갖지 않는다** (03 §24.6). 그 값의 단일 출처는 `00` §7.4 이고
    ///     SP 가 `경고임계일수` 로 함께 내보내므로 `잔여일수 &lt; 경고임계일수` 만 비교한다.
    ///     여기에 `180` 을 적으면 같은 값이 세 곳에 생긴다 (ROOT AGENTS.md §6).
    /// </summary>
    public sealed class HolidayPresenter
    {
        private readonly IHolidayView _view;
        private readonly IHolidayService _service;
        private readonly ICommonStatusService _statusService;

        // 지금 잡혀 있는 행. 수정·삭제가 요구하는 `행버전` 이 여기서 나온다 (05 §12.7 · §12.8).
        private HolidayListItemDto _picked;

        public HolidayPresenter(IHolidayView view, IHolidayService service, ICommonStatusService statusService)
        {
            _view = view;
            _service = service;
            _statusService = statusService;

            _view.SearchRequested += OnSearchRequested;
            _view.SelectionChanged += OnSelectionChanged;

            Pick(null);
        }

        /// <summary>
        /// 화면을 열면 한 번 조회한다.
        ///
        /// [X] **실패를 모달로 알리지 않는다.** 사용자가 부탁한 호출이 아니므로 창을 열자마자
        ///     오류창이 뜨면 안 된다 — WF-PAT-01 에서 같은 자리가 UI 시험을 멈춰 세웠다
        ///     (실측 2026-09-10). 사유는 Inline 으로 남긴다.
        /// </summary>
        public void LoadInitial()
        {
            // 03 §24.4 — 조회기간 기본값은 오늘부터 두 해다.
            //
            // [X] **오늘을 PC 시계에서 얻지 않는다.** 창구 PC 가 하루 어긋나면 목록도 등재
            //     경고도 통째로 어긋난다. WF-WRK-01 이 가는 길과 같다 — 공통 업무상태가 준다.
            //
            // [X] **이 자리가 통째로 비어 있었다** (2026-09-11 실측). `_statusService` 는
            //     생성자가 받아 두기만 하고 한 번도 쓰이지 않았고, 조회기간을 채우는 길은
            //     아무도 부르지 않는 `UcHoliday.Begin(today)` 뿐이었다. 그래서 두 칸이 빈 채로
            //     남아 `[조회]` 가 `100 시작일자 필수` 로 막히고, 목록이 비어 행을 못 고르니
            //     `[수정]`·`[삭제]` 도 영영 열리지 않았다 — 화면의 CRUD 가 전부 죽어 있었다.
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
                _view.BlockMessage = "오늘 날짜를 확인하지 못해 조회기간을 세우지 못했습니다.";
                return;
            }

            DateTime today = status.Value.Today.Date;
            _view.FromDate = today;
            _view.ToDate = today.AddYears(2);

            Search();
        }

        private void OnSearchRequested(object sender, EventArgs e)
        {
            Search();
        }

        private void Search()
        {
            DateTime? from = _view.FromDate;
            DateTime? to = _view.ToDate;

            // 05 §12.5 — 두 날짜는 필수다. 비어 있음 판정은 Presenter 가 한다 (킷 §6).
            if (from == null || to == null)
            {
                _view.BlockMessage = "조회기간을 입력하십시오.";
                return;
            }

            if (from.Value.Date > to.Value.Date)
            {
                _view.BlockMessage = "시작일은 종료일보다 늦을 수 없습니다.";
                return;
            }

            OperationResult<HolidayListReadDto> result;
            try
            {
                result = _service.Search(new HolidaySearchRequest
                {
                    FromDate = from,
                    ToDate = to,
                    HolidayType = _view.HolidayTypeFilter,
                });
            }
            catch (Exception)
            {
                // 예외 본문을 화면에 싣지 않는다 (킷 §6).
                _view.BlockMessage = "휴무일을 조회하지 못했습니다.";
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                _view.BlockMessage = result == null ? "휴무일을 조회하지 못했습니다." : result.Message;
                return;
            }

            _view.BlockMessage = null;
            _view.Rows = result.Value.Rows;
            _view.RegistryWarning = WarningOf(result.Value.Registry);
            Pick(null);
        }

        /// <summary>
        /// 03 §24.6 — `잔여일수 &lt; 경고임계일수` 일 때만 적는다. 경고일 뿐 편집을 막지 않는다.
        /// 공휴일이 0건이면 최종일자도 잔여일수도 NULL 이다 (05 §12.5).
        /// </summary>
        private static string WarningOf(HolidayRegistryDto registry)
        {
            if (registry == null)
            {
                return string.Empty;
            }

            if (registry.LastHolidayDate == null || registry.RemainingDays == null)
            {
                return "공휴일이 하나도 등재되어 있지 않습니다.";
            }

            if (registry.RemainingDays.Value >= registry.WarningThresholdDays)
            {
                return string.Empty;
            }

            return "공휴일 등재가 " + clsWorkText.FormatDate(registry.LastHolidayDate.Value)
                + " 에 끝납니다. 남은 기간 "
                + registry.RemainingDays.Value.ToString(CultureInfo.InvariantCulture) + "일";
        }

        private void OnSelectionChanged(object sender, HolidayListItemDto row)
        {
            Pick(row);
        }

        /// <summary>
        /// 03 §24.4 — **법정·대체 행은 선택해도 입력행에 싣지 않는다.** 보여 주는 이유는 그 날짜가
        /// 왜 업무 불가인지 확인하는 것과, 같은 날짜에 자체휴무일을 넣으려는 시도를 미리 막는
        /// 것 둘이다 (HOL-04 날짜 중복 금지).
        /// </summary>
        private void Pick(HolidayListItemDto row)
        {
            bool own = row != null
                && DbHolidayType.Own.Equals(row.HolidayType, StringComparison.Ordinal);

            _picked = own ? row : null;
            _view.EditEnabled = true;
            _view.RowActionsEnabled = own;

            if (own)
            {
                _view.InputDate = row.HolidayDate;
                _view.InputName = row.HolidayName;
                _view.InputActive = row.IsActive;
                _view.InputMemo = row.Memo;
            }
            else
            {
                _view.InputName = string.Empty;
                _view.InputActive = true;
                _view.InputMemo = string.Empty;
            }
        }

        /// <summary>03 §24.5 [추가] — `휴무구분` 은 항상 `자체휴무일` 이고 화면이 고르지 않는다.</summary>
        public void Register()
        {
            HolidaySaveRequest request = Input(null);
            if (request == null)
            {
                return;
            }

            Apply(Call(_service.Register, request, "휴무일을 등록하지 못했습니다."), request.HolidayDate);
        }

        /// <summary>03 §24.5 [수정] — `휴무일명`·`사용여부`·`비고` 만 바꾼다. 날짜는 PK 다.</summary>
        public void Update()
        {
            if (_picked == null)
            {
                return;
            }

            HolidaySaveRequest request = Input(_picked.RowVersion);
            if (request == null)
            {
                return;
            }

            // 05 §12.7 — 날짜를 옮기려면 삭제 후 등록이다. 잡아 둔 행의 날짜로 보낸다.
            request.HolidayDate = _picked.HolidayDate;
            Apply(Call(_service.Update, request, "휴무일을 수정하지 못했습니다."), request.HolidayDate);
        }

        /// <summary>03 §24.5 [삭제] — **물리 삭제다.** 되돌릴 수 없으므로 한 번 묻는다.</summary>
        public void Delete()
        {
            if (_picked == null)
            {
                return;
            }

            if (!_view.Confirm(clsWorkText.FormatDate(_picked.HolidayDate)
                + " " + _picked.HolidayName + " 을(를) 삭제하시겠습니까?"))
            {
                return;
            }

            DateTime date = _picked.HolidayDate;
            byte[] rowVersion = _picked.RowVersion;

            OperationResult<HolidaySaveReadDto> result;
            try
            {
                result = _service.Delete(date, rowVersion);
            }
            catch (Exception)
            {
                _view.BlockMessage = "휴무일을 삭제하지 못했습니다.";
                return;
            }

            // 지운 자리로 돌아갈 수 없다 — 목록만 다시 읽는다.
            Apply(result, null);
        }

        private OperationResult<HolidaySaveReadDto> Call(
            Func<HolidaySaveRequest, OperationResult<HolidaySaveReadDto>> call,
            HolidaySaveRequest request,
            string unknown)
        {
            try
            {
                return call(request);
            }
            catch (Exception)
            {
                return OperationResult<HolidaySaveReadDto>.Failure(unknown);
            }
        }

        /// <summary>
        /// 저장 결과 하나를 받는 자리. **성공이든 실패든 목록을 다시 읽는다** — 다른 창구가
        /// 그 사이 무엇을 했는지 사용자가 보아야 하고, `801`(중복)·`802`(법정공휴일)·`601`(충돌)은
        /// 전부 목록이 낡았다는 뜻이기 때문이다.
        ///
        /// [X] 사유를 먼저 적고 재조회하면 재조회가 그 칸을 지운다 — WF-RSV-01 에서 실측한
        ///     자리다. **재조회를 먼저 하고 사유를 마지막에 적는다.**
        /// </summary>
        private void Apply(OperationResult<HolidaySaveReadDto> result, DateTime? keep)
        {
            if (result == null)
            {
                _view.BlockMessage = "휴무일을 저장하지 못했습니다.";
                return;
            }

            if (!result.IsSuccess)
            {
                _view.BlockMessage = result.Message;
                return;
            }

            DbResult db = result.Value.Result;
            Search();

            if (db != null && !db.Success)
            {
                _view.BlockMessage = db.Message;
                return;
            }

            if (keep != null)
            {
                _view.SelectDate(keep.Value);
            }
        }

        /// <summary>
        /// 입력행을 요청으로 옮긴다. 비어 있음은 Presenter 가 보고 길이는 Service 가 본다 (킷 §6).
        /// </summary>
        private HolidaySaveRequest Input(byte[] rowVersion)
        {
            DateTime? date = _view.InputDate;
            if (date == null)
            {
                _view.BlockMessage = "휴무일자를 입력하십시오.";
                return null;
            }

            string name = _view.InputName;
            if (string.IsNullOrWhiteSpace(name))
            {
                _view.BlockMessage = "휴무일명을 입력하십시오.";
                return null;
            }

            return new HolidaySaveRequest
            {
                HolidayDate = date.Value.Date,
                RowVersion = rowVersion,
                HolidayName = name.Trim(),
                IsActive = _view.InputActive,
                Memo = _view.InputMemo,
            };
        }
    }
}
