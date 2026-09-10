// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
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
    /// WF-RSV-01 신규 예약 (03 §8).
    ///
    /// **업무 판정을 여기서 하지 않는다.** 정원·마감·TGT·NEX·AEX 가용성·저장가능은 전부
    /// SP-RSV-01 이 낸 값이다 (05 §9.12). 이 계층이 하는 일은 셋뿐이다 —
    /// 언제 물을지 정하고, 받은 것을 화면 자리에 놓고, 03 §8.5 의 진행 단계를 여닫는다.
    ///
    /// 03 §8.7 의 판정문구만 화면 계산결과다. 비대상 사유는 DB 가 주고 대상 쪽 문구
    /// (`최초검진` · `최근 완료연도 2024`)는 최근완료일자에서 만든다.
    /// </summary>
    public sealed class ReservationPresenter
    {
        private const string TargetPrefix = "대상판정 : ";

        private readonly IReservationView _view;
        private readonly IReservationService _service;
        private readonly IPatientService _patientService;
        private readonly string _operatorName;

        private ReservationContext _context = ReservationContext.Normal;
        private long? _patientId;

        // 조회 한 번을 아끼는 자리. DateEdit 은 글자를 칠 때마다 값이 바뀌므로 같은
        // 일정으로 SP 를 되풀이해 부르게 된다 — 같은 (예약일, 시간대) 면 건너뛴다.
        private DateTime? _askedDate;
        private string _askedSlot;

        public ReservationPresenter(
            IReservationView view,
            IReservationService service,
            IPatientService patientService,
            string operatorName)
        {
            _view = view;
            _service = service;
            _patientService = patientService;
            _operatorName = operatorName;

            _view.ScheduleChanged += OnScheduleChanged;
            _view.SaveRequested += OnSaveRequested;

            Reset();
        }

        /// <summary>
        /// 03 §3 `BeginNewReservation(Context, PatientId?, Source)`.
        ///
        /// **PatientId 는 늘 있다** — 모달을 여는 쪽이 DLG-PAT-02 를 먼저 거치거나 목록에서
        /// 고른 값을 들고 온다 (2026-09-10 grilling 2회차). 화면 안에서 수검자를 바꾸지 않으므로
        /// 03 §8.10 의 「다른 PatientId 로 재호출」 트리거는 `모달을 닫고 다시 여는 것` 이 된다.
        /// </summary>
        public void Begin(ReservationContext context, long patientId, NavigationSource source)
        {
            _context = context;
            Reset();

            // 03 §8.6 — WalkIn 은 예약일이 DB 오늘날짜이고 ReadOnly 다. 화면 시계와 DB 시계가
            // 어긋나면 SP 가 `102` 로 막는다 (05 §9.3) — 여기서 우기지 않는다.
            _view.ReserveDateReadOnly = context == ReservationContext.WalkIn;

            ConfirmPatient(patientId);
        }

        /// <summary>
        /// 03 §8.10 폐기 확인 — 닫을 때 물어야 하는가.
        ///
        /// 수검자가 확정된 순간부터 화면에는 사용자가 들인 것이 있다(일정·AEX 선택). 저장이
        /// 끝나면 <see cref="Reset"/> 이 이것을 내리므로 성공 뒤에는 묻지 않는다.
        /// </summary>
        public bool HasUnsavedInput
        {
            get { return _patientId != null; }
        }

        /// <summary>03 §8.5 최초 상태. 수검자 선택만 열려 있다.</summary>
        private void Reset()
        {
            _patientId = null;
            _askedDate = null;
            _askedSlot = null;

            _view.Patient = null;
            _view.ScheduleEnabled = false;
            _view.ReserveDate = DateTime.Today;
            _view.Slots = new List<SlotInfoDto>();
            _view.SlotCode = null;
            _view.TargetText = TargetPrefix + "미판정";
            _view.NexItems = new List<WorkExamItemDto>();
            _view.AexItems = new List<ReservationAexItemDto>();
            _view.AexEnabled = false;
            _view.SaveEnabled = false;
            _view.BlockMessage = null;
        }

        /// <summary>
        /// 03 §8.5 — PatientId 확정 후 **중복판단 유효예약 확인**이 먼저다.
        /// 기존 유효예약이 있으면 신규예약을 중단하고 그 WorkId 로 Workbench 로 간다.
        /// </summary>
        private void ConfirmPatient(long patientId)
        {
            OperationResult<PatientValidWorkDto> valid;
            try
            {
                valid = _patientService.GetValidWork(patientId);
            }
            catch (Exception)
            {
                // 예외 본문을 화면에 싣지 않는다 (킷 §6).
                _view.BlockMessage = "수검자의 기존 예약을 확인하지 못했습니다.";
                return;
            }

            if (valid == null || !valid.IsSuccess)
            {
                _view.BlockMessage = valid == null ? "수검자의 기존 예약을 확인하지 못했습니다." : valid.Message;
                return;
            }

            if (valid.Value != null)
            {
                _view.ShowMessage("이미 예약이 있는 수검자입니다. 예약 관리에서 확인하세요.");
                _view.GoToWorkbench(WorkContext.Reservation, valid.Value.WorkId);
                return;
            }

            OperationResult<PatientDetailDto> detail;
            try
            {
                detail = _patientService.GetDetail(patientId);
            }
            catch (Exception)
            {
                _view.BlockMessage = "수검자 상세를 조회하지 못했습니다.";
                return;
            }

            if (detail == null || !detail.IsSuccess)
            {
                _view.BlockMessage = detail == null ? "수검자 상세를 조회하지 못했습니다." : detail.Message;
                return;
            }

            _patientId = patientId;
            _view.Patient = detail.Value;
            _view.ScheduleEnabled = true;

            // 03 §8.5 — 여기서 일정영역이 열린다. 예약일 칸에 값이 이미 서 있으므로 그 일정으로
            // 한 번 묻는다: 사용자가 날짜를 건드리기 전에도 정원과 대상판정을 볼 수 있다.
            Ask(true);
        }

        private void OnScheduleChanged(object sender, EventArgs e)
        {
            Ask(false);
        }

        /// <summary>
        /// SP-RSV-01 한 번 (05 §9). 달력 Hover 에서는 부르지 않는다 — 값이 확정된 시점이다.
        ///
        /// [X] AEX 체크가 바뀔 때는 부르지 않는다. 가용성(`선택가능`)은 예약일이 정하는 것이고
        ///     선택 자체로는 바뀌지 않는다 — 클릭마다 동기 SP 를 부르면 화면이 그때마다 얼어붙는다.
        ///     저장할 때 SP-RSV-02 가 어차피 전부 다시 검증한다 (05 §11.1).
        /// </summary>
        private void Ask(bool force)
        {
            if (_patientId == null)
            {
                return;
            }

            DateTime date = _view.ReserveDate.Date;
            string slot = _view.SlotCode;
            if (!force && _askedDate == date && _askedSlot == slot)
            {
                return;
            }

            var request = new ReservationAvailabilityRequest
            {
                PatientId = _patientId.Value,
                ReserveType = _context == ReservationContext.WalkIn
                    ? DbReserveType.WalkIn
                    : DbReserveType.Normal,
                ReserveDate = date,
                SlotCode = slot,
                AexSelected = _view.AexSelection,
            };

            OperationResult<ReservationAvailabilityReadDto> result;
            try
            {
                result = _service.GetAvailability(request);
            }
            catch (Exception)
            {
                _view.BlockMessage = "예약 가능정보를 조회하지 못했습니다.";
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                _view.BlockMessage = result == null ? "예약 가능정보를 조회하지 못했습니다." : result.Message;
                _view.SaveEnabled = false;
                return;
            }

            Render(result.Value);

            // [X] 가드의 열쇠는 **조회 뒤 화면이 실제로 든 값**이다. 시간대를 아직 고르지 않고
            //     물으면 DB 가 고를 수 있는 하나를 정해 돌려주고 Render 가 그것을 화면에
            //     세우는데(05 §9.6), 보낸 값(NULL)으로 열쇠를 잡아 두면 다음 번에 "바뀌었다"
            //     로 보여 같은 일정을 한 번 더 묻는다.
            _askedDate = date;
            _askedSlot = _view.SlotCode;
        }

        private void Render(ReservationAvailabilityReadDto read)
        {
            ReservationSummaryDto summary = read.Summary;


            // 03 §8.5 — 다른 창구가 그 사이 예약을 넣었을 수 있다. 조회가 그것을 잡으면
            // 여기서도 신규예약을 접는다 (05 §9.6 `다른업무ID`).
            if (summary.OtherWorkId != null)
            {
                _view.ShowMessage("이미 예약이 있는 수검자입니다. 예약 관리에서 확인하세요.");
                _view.GoToWorkbench(WorkContext.Reservation, summary.OtherWorkId.Value);
                return;
            }

            _view.Slots = read.Slots;
            _view.SlotCode = summary.SlotCode;
            _view.TargetText = TargetTextOf(read.Target);
            _view.NexItems = read.NexItems;

            // 03 §8.10 — 예약일이 바뀌면 **유효 선택만 유지**한다. 무엇이 살아남았는지는
            // DB 가 `유효선택여부` 로 알려 준다.
            foreach (ReservationAexItemDto aex in read.AexItems)
            {
                aex.Requested = aex.EffectiveSelected;
            }

            _view.AexItems = read.AexItems;

            // 03 §8.5 비대상 — AEX Disabled.
            _view.AexEnabled = read.Target != null && read.Target.IsTarget;

            _view.SaveEnabled = summary.CanSave;
            _view.BlockMessage = summary.BlockMessage;
        }

        /// <summary>
        /// 03 §8.7 — 판정문구는 화면 계산결과이며 별도 DB 컬럼으로 저장하지 않는다.
        /// 비대상 사유는 DB 가 주고(`사유메시지`), 대상 쪽 문구만 최근완료일자에서 만든다.
        /// </summary>
        private static string TargetTextOf(ExamTargetDto target)
        {
            if (target == null)
            {
                return TargetPrefix + "미판정";
            }

            if (!target.IsTarget)
            {
                return string.IsNullOrWhiteSpace(target.ReasonMessage)
                    ? TargetPrefix + "비대상"
                    : TargetPrefix + "비대상 — " + target.ReasonMessage.Trim();
            }

            if (target.LastCompletedDate == null)
            {
                return TargetPrefix + "대상 — 최초검진";
            }

            return TargetPrefix + "대상 — 최근 완료연도 "
                + target.LastCompletedDate.Value.Year.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 03 §8.11 2단계 저장. 화면 준비상태는 이미 `저장가능` 이 판정했고, 저장 클릭 뒤에는
        /// DB 가 상태·마감·정원·중복·TGT·NEX·AEX 를 **처음부터 다시** 검증한다 (05 §11.1).
        /// </summary>
        private void OnSaveRequested(object sender, EventArgs e)
        {
            if (_patientId == null)
            {
                return;
            }

            var request = new ReservationSaveRequest
            {
                PatientId = _patientId.Value,
                ReserveType = _context == ReservationContext.WalkIn
                    ? DbReserveType.WalkIn
                    : DbReserveType.Normal,
                ReserveDate = _view.ReserveDate.Date,
                SlotCode = _view.SlotCode,
                AexSelected = _view.AexSelection,
                OperatorName = _operatorName,
            };

            OperationResult<WorkSaveReadDto> result;
            try
            {
                result = _service.Register(request);
            }
            catch (Exception)
            {
                _view.BlockMessage = "예약을 저장하지 못했습니다.";
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                _view.BlockMessage = result == null ? "예약을 저장하지 못했습니다." : result.Message;
                return;
            }

            DbResult db = result.Value.Result;
            if (!db.Success)
            {
                // 03 §8.11 실패 — Commit 없음. 사유를 보이고 일정·대상·검사구성을 최신값으로 다시 읽는다.
                //
                // [X] **순서가 뒤집히면 사유가 사라진다.** Refresh 가 `차단메시지` 로 같은 칸을
                //     다시 쓰기 때문이다 — 그 값은 보통 비어 있다(저장 전에는 막힌 곳이 없었다).
                //     사용자가 봐야 하는 것은 방금 저장이 막힌 이유이므로 그것이 마지막이다.
                Ask(true);
                _view.BlockMessage = db.Message;
                _view.SaveEnabled = false;
                return;
            }

            if (result.Value.Row == null)
            {
                // 성공인데 RS1 이 없다 — 계약 위반이므로 Workbench 로 넘기지 않는다 (05 §11).
                _view.BlockMessage = "예약은 저장됐지만 업무ID 를 받지 못했습니다.";
                return;
            }

            // 03 §8.11 성공 — 생성건을 Workbench 에서 자동선택한다.
            // WalkIn 은 Reservation 이 아니라 Reception 이다 (03 §9.8).
            long workId = result.Value.Row.WorkId;
            WorkContext target = _context == ReservationContext.WalkIn
                ? WorkContext.Reception
                : WorkContext.Reservation;

            // 03 §8.10 저장 성공 행 — 전체 Clear.
            Reset();
            _view.GoToWorkbench(target, workId);
        }
    }
}
