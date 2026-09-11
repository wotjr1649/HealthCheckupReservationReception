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
        // 목록 컬럼은 두 값뿐이다 — 훑는 자리라 좁고 색으로 읽힌다.
        private const string Open = "가능";
        private const string Blocked = "불가";

        private DateTime _today;

        private readonly IPatientManagementView _view;
        private readonly IPatientService _service;
        private readonly IWorkService _workService;
        private readonly ICommonStatusService _statusService;

        // 마지막 조회의 예약 상태. 행을 고를 때 상세 한 줄과 `[예약]` 의 여닫음이 여기서 나온다.
        private readonly Dictionary<long, string> _reserveDetail = new Dictionary<long, string>();
        private readonly Dictionary<long, string> _reserveBlocked = new Dictionary<long, string>();

        public PatientManagementPresenter(
            IPatientManagementView view,
            IPatientService service,
            IWorkService workService,
            ICommonStatusService statusService)
        {
            _view = view;
            _service = service;
            _workService = workService;
            _statusService = statusService;

            _view.SearchRequested += OnSearchRequested;
            _view.SelectionChanged += OnSelectionChanged;

            _view.RowActions = PatientActionState.None();
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

        /// <summary>
        /// 목록을 되읽고 그 수검자로 돌아간다. 예약이 하나 생기면 `예약` 칸이 낡는데,
        /// 화면이 방금 예약한 사람을 계속 `가능` 으로 보이면 그것은 거짓말이다.
        /// </summary>
        public void Reload(long patientId)
        {
            Search(true);
            _view.SelectPatient(patientId);
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

            IList<PatientListItemDto> rows = Annotate(result.Value);

            // 여섯째 조회조건은 SP 가 모르므로 화면이 거른다 (IPatientManagementView.ReservableOnly).
            if (_view.ReservableOnly)
            {
                var open = new List<PatientListItemDto>();
                foreach (PatientListItemDto row in rows)
                {
                    if (!Blocked.Equals(row.ReserveStatus, StringComparison.Ordinal))
                    {
                        open.Add(row);
                    }
                }

                rows = open;
            }

            // 03 §5.3 · §5.5 — 재조회 시 선택행과 우측 상세를 초기화한다.
            _view.Rows = rows;
            _view.Detail = null;
            _view.ReserveStatusText = string.Empty;
            _view.NoticeText = string.Empty;
            _view.RowActions = PatientActionState.None();
        }

        /// <summary>
        /// 목록에 `예약 가능/불가` 를 이어 붙인다 (2026-09-11 grilling).
        ///
        /// `SP-PAT-01` 은 예약을 모르고 계약이 동결이라 컬럼을 붙일 수 없다. 대신 `SP-WRK-01`
        /// 의 RS1 이 `수검자ID` 를 싣고 있으므로 화면에서 이어 붙인다.
        ///
        /// [X] **가르는 규칙은 `00` RP-06 하나다** — *"중복판단 유효예약은 `예약일 >= DB 현재일`
        ///     이고 상태가 `RSV` 또는 `RCP` 인 업무다. 과거 업무는 중복판단에서 제외한다."*
        ///     그 문장이 화면에 한 번 더 적히므로 상태코드는 `DbWorkStatus` 로 모으고
        ///     `scripts/verify-work-status.sh` 가 05 §2.2 와 대조한다 (ROOT AGENTS.md §6).
        ///
        /// [X] 오늘날짜를 PC 시계에서 얻지 않는다. 그러면 창구 PC 가 하루 어긋났을 때 예약
        ///     가능한 사람이 불가로 보인다 — `SP-CMN-01` 이 DB 오늘날짜를 준다.
        ///
        /// 이어 붙이지 못하면 **칸을 비운다.** 모르는 것을 `가능` 이라 적으면 거짓이 된다.
        /// </summary>
        private IList<PatientListItemDto> Annotate(IList<PatientListItemDto> rows)
        {
            _reserveDetail.Clear();
            _reserveBlocked.Clear();
            if (rows == null)
            {
                return new List<PatientListItemDto>();
            }

            IList<WorkListItemDto> works = LoadWorks();
            if (works == null)
            {
                return rows;
            }

            DateTime today = _today;
            var latestValid = new Dictionary<long, WorkListItemDto>();
            var latestMissed = new Dictionary<long, WorkListItemDto>();

            foreach (WorkListItemDto work in works)
            {
                if (work.ReserveDate.Date >= today)
                {
                    latestValid[work.PatientId] = work;
                }
                else if (DbWorkStatus.Reserved.Equals(work.StatusCode, StringComparison.Ordinal))
                {
                    // 지난 예약인데 접수가 없다 — 새 상태코드 없이 두 값의 조합으로 읽는다.
                    latestMissed[work.PatientId] = work;
                }
            }

            foreach (PatientListItemDto row in rows)
            {
                WorkListItemDto valid;
                if (latestValid.TryGetValue(row.PatientId, out valid))
                {
                    row.ReserveStatus = Blocked;
                    row.ReserveStatusDetail = "예약 불가 — " + Schedule(valid) + " " + valid.StatusName;
                }
                else
                {
                    row.ReserveStatus = Open;
                    WorkListItemDto missed;
                    row.ReserveStatusDetail = latestMissed.TryGetValue(row.PatientId, out missed)
                        ? "예약 가능 (지난 예약 " + Schedule(missed) + " 미접수)"
                        : "예약 가능";
                }

                _reserveDetail[row.PatientId] = row.ReserveStatusDetail;
                _reserveBlocked[row.PatientId] = row.ReserveStatus;
            }

            return rows;
        }

        /// <summary>
        /// RP-06 이 보는 두 상태를 한 번씩 읽는다.
        ///
        /// `RSV` 는 날짜를 걸지 않는다 — 과거의 미접수 건까지 봐야 상세에 그것을 적을 수 있고,
        /// 남는 양도 작다(예약 대기 + 노쇼). `RCP` 는 오늘 것만 본다: 접수는 당일 업무이므로
        /// (05 §8.2 `START_RECEPTION` 이 `예약일=오늘`) 유효한 `RCP` 는 오늘뿐이고, 날짜를
        /// 걸지 않으면 완료된 접수가 세월과 함께 쌓인다.
        /// </summary>
        private IList<WorkListItemDto> LoadWorks()
        {
            OperationResult<CommonWorkStatusDto> status;
            try
            {
                status = _statusService.GetCurrent();
            }
            catch (Exception)
            {
                return null;
            }

            if (status == null || !status.IsSuccess || status.Value == null)
            {
                return null;
            }

            _today = status.Value.Today.Date;

            IList<WorkListItemDto> reserved = Works(DbWorkStatus.Reserved, null);
            if (reserved == null)
            {
                return null;
            }

            IList<WorkListItemDto> received = Works(DbWorkStatus.Received, _today);
            if (received == null)
            {
                return null;
            }

            var all = new List<WorkListItemDto>(reserved);
            all.AddRange(received);
            return all;
        }

        private IList<WorkListItemDto> Works(string statusCode, DateTime? from)
        {
            OperationResult<IList<WorkListItemDto>> result;
            try
            {
                result = _workService.Search(new WorkSearchRequest
                {
                    FromDate = from,
                    StatusCode = statusCode,
                });
            }
            catch (Exception)
            {
                return null;
            }

            return result != null && result.IsSuccess ? result.Value : null;
        }

        private static string Schedule(WorkListItemDto work)
        {
            return clsWorkText.FormatDate(work.ReserveDate) + " " + clsWorkText.FormatSlot(work.SlotCode);
        }

        private void OnSelectionChanged(object sender, long? patientId)
        {
            if (patientId == null)
            {
                _view.Detail = null;
                _view.ReserveStatusText = string.Empty;
                _view.RowActions = PatientActionState.None();
                return;
            }

            // 03 §5.2 — 행이 잡혔다. `[예약]` 만 축이 하나 더 있다: 그 사람이 예약 가능한가.
            //
            // [X] **모르는 것은 불가가 아니다** — 업무 조회가 실패해 칸이 비었으면 열어 둔다.
            string status;
            _view.RowActions = new PatientActionState
            {
                RowSelected = true,
                Reserve = !_reserveBlocked.TryGetValue(patientId.Value, out status)
                    || !Blocked.Equals(status, StringComparison.Ordinal),
            };

            string detail;
            _view.ReserveStatusText = _reserveDetail.TryGetValue(patientId.Value, out detail)
                ? detail
                : string.Empty;

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
