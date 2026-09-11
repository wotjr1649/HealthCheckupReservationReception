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

        // 05 §9.2 `@예약구분`. **조작자가 고르지 않는다** — 00 RP-05 가 시각으로 가르고,
        // 그 판정은 Ask 가 DB 의 답을 보고 내린다.
        private string _reserveType = DbReserveType.Normal;
        private long? _patientId;

        // DLG-RSV-01 예약변경 (03 §10). 신규예약이면 둘 다 null 이고, 그 둘이 있는 것이 곧
        // 「변경 모드」다 — 05 §9.3 조합표가 `업무ID`·`행버전` 을 그렇게 짝지어 놓았다.
        private long? _workId;
        private byte[] _rowVersion;

        // 마지막 조회의 시간대정보. 저장 뒤 어디로 갈지가 여기서 나온다 (IsToday).
        private IList<SlotInfoDto> _slots;

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
        public void Begin(long patientId)
        {
            Reset();
            ConfirmPatient(patientId);
        }

        /// <summary>
        /// DLG-RSV-01 예약 변경 (03 §10). **같은 화면이 모드만 바꾼다** — 수검자는 ReadOnly,
        /// 예약일·시간대·AEX 는 Editable 이라는 것이 §10.2 이고 그것은 신규예약과 같다.
        ///
        /// 진입값은 Workbench 가 방금 읽은 상세다. 수검자 조회(`SP-PAT-02`)를 다시 하지
        /// 않는다 — 05 §8.2 RS1 이 차트번호·성명·생년월일·성별·휴대전화를 이미 싣고 있다.
        ///
        /// [X] **기존 유효예약 확인(`SP-PAT-05`)을 하지 않는다.** 그 판정은 *"이 수검자로
        ///     새 예약을 만들 수 있는가"* 이고, 변경은 이미 있는 그 예약을 고치는 일이다.
        ///     중복은 `SP-RSV-03` 이 **현재 Work 를 제외하고** 다시 본다 (05 §11.2).
        /// </summary>
        public void BeginChange(WorkDetailDto detail)
        {
            Reset();
            if (detail == null)
            {
                _view.BlockMessage = "변경할 업무를 알 수 없습니다.";
                return;
            }

            _workId = detail.WorkId;
            _rowVersion = detail.RowVersion;
            _patientId = detail.PatientId;

            _view.Title = "예약 변경";
            _view.Patient = new PatientDetailDto
            {
                PatientId = detail.PatientId,
                ChartNo = detail.ChartNo,
                Name = detail.Name,
                Birthday = detail.Birthday,
                Gender = detail.Gender,
                MobilePhone = detail.MobilePhone,
            };

            // 03 §10.3 — 지금 일정으로 한 번 묻는다. 사용자가 아무것도 건드리지 않아도
            // 정원·대상판정·검사구성이 서 있어야 무엇이 바뀌는지 견줄 수 있다.
            _view.ReserveDate = detail.ReserveDate;
            _view.SlotCode = detail.SlotCode;
            _view.ScheduleEnabled = true;
            Ask(true);
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
            _reserveType = DbReserveType.Normal;
            _workId = null;
            _rowVersion = null;
            _slots = null;

            _view.Patient = null;
            _view.ScheduleEnabled = false;
            _view.ReserveDate = DateTime.Today;
            _view.Slots = new List<SlotInfoDto>();
            _view.SlotCode = null;
            _view.TargetText = TargetPrefix + "미판정";
            _view.ReserveTypeText = string.Empty;
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
                // 이 수검자로는 더 진행할 수 없다 (RP-06). 남은 선택은 「그 예약을 보러 갈까」뿐이다.
                //
                // [X] 예전에는 알리고 곧바로 데려갔다. 명단을 연달아 예약하는 중이면 잘못 누른
                //     한 번이 흐름을 끊는다 — 2026-09-11 사용자 지시로 묻고 간다.
                Leave(WorkContext.Reservation, valid.Value.WorkId,
                    "이미 예약이 있는 수검자입니다 — " + Schedule(valid.Value)
                    + ". 예약 관리에서 확인하시겠습니까?");
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

            // 00 RP-05 — 현장 내원자는 **당일예약 마감 전이면 일반, 그 뒤 접수 마감 전까지는
            // 현장 당일예약**이다. 같은 사람·같은 행동이고 시각만 다르다. 조작자에게 시계를
            // 읽히지 않는다: 일반으로 묻고, 마감이 지나 막혔으면 현장으로 한 번 더 묻는다.
            _reserveType = DbReserveType.Normal;
            ReservationAvailabilityReadDto read = Query(date, slot, DbReserveType.Normal);
            if (read == null)
            {
                return;
            }

            if (CutoffBlocked(read.Slots))
            {
                ReservationAvailabilityReadDto walkIn = Query(date, slot, DbReserveType.WalkIn);
                if (walkIn != null)
                {
                    _reserveType = DbReserveType.WalkIn;
                    read = walkIn;
                }
            }

            Render(read);

            // [X] 가드의 열쇠는 **조회 뒤 화면이 실제로 든 값**이다. 시간대를 아직 고르지 않고
            //     물으면 DB 가 고를 수 있는 하나를 정해 돌려주고 Render 가 그것을 화면에
            //     세우는데(05 §9.6), 보낸 값(NULL)으로 열쇠를 잡아 두면 다음 번에 "바뀌었다"
            //     로 보여 같은 일정을 한 번 더 묻는다.
            _askedDate = date;
            _askedSlot = _view.SlotCode;
        }

        /// <summary>
        /// SP-RSV-01 한 번. 실패는 화면에 적고 null 을 돌려준다 — 부른 쪽이 이어 가지 않는다.
        /// </summary>
        private ReservationAvailabilityReadDto Query(DateTime date, string slot, string reserveType)
        {
            var request = new ReservationAvailabilityRequest
            {
                PatientId = _patientId.Value,
                WorkId = _workId,
                RowVersion = _rowVersion,
                ReserveType = reserveType,
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
                return null;
            }

            if (result == null || !result.IsSuccess)
            {
                _view.BlockMessage = result == null ? "예약 가능정보를 조회하지 못했습니다." : result.Message;
                _view.SaveEnabled = false;
                return null;
            }

            return result.Value;
        }

        /// <summary>
        /// 막힌 이유가 **마감** 인 시간대가 있는가 (05 §4.2 `304 CutoffPassed`).
        ///
        /// [X] 여기서 "오늘인가" 를 화면이 재지 않는다. `03_Functions.sql` 이
        ///     `적용마감시각 = CASE WHEN @예약일 = 오늘날짜 THEN 기본마감시각 ELSE NULL END`
        ///     이므로 **마감이 걸렸다는 것 자체가 그 날이 DB 오늘날짜라는 뜻**이다.
        ///     PC 시계도, 오늘날짜를 얻으려는 추가 조회도 필요 없다.
        /// </summary>
        private static bool CutoffBlocked(IList<SlotInfoDto> slots)
        {
            if (slots == null)
            {
                return false;
            }

            foreach (SlotInfoDto slot in slots)
            {
                if (!slot.Selectable && slot.BlockCode == (int)DbCode.CutoffPassed)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 이 수검자로는 더 진행할 수 없다. 그 예약을 보러 갈지만 묻고 어느 쪽이든 창을 닫는다.
        /// </summary>
        private void Leave(WorkContext context, long workId, string question)
        {
            if (_view.Confirm(question))
            {
                _view.GoToWorkbench(context, workId, false);
            }
            else
            {
                _view.Dismiss();
            }
        }

        private static string Schedule(PatientValidWorkDto work)
        {
            return clsWorkText.FormatDate(work.ReserveDate) + " " + clsWorkText.FormatSlot(work.SlotCode);
        }

        /// <summary>
        /// 저장한 시간대의 예약일이 DB 오늘날짜인가.
        ///
        /// [X] `DateTime.Today` 와 비교하지 않는다 — PC 시계는 DB 시계가 아니다. 마감시각은
        ///     `@예약일 = 오늘날짜` 일 때만 채워지므로(위와 같은 자리), **마감시각이 있다는
        ///     것이 곧 오늘이라는 뜻**이다. 운영하지 않는 시간대는 오늘이어도 NULL 인데,
        ///     그런 시간대는 애초에 저장되지 않으므로 이 판정에 닿지 않는다.
        /// </summary>
        private bool IsToday(string slotCode)
        {
            if (_slots == null || slotCode == null)
            {
                return false;
            }

            foreach (SlotInfoDto slot in _slots)
            {
                if (slotCode.Equals(slot.SlotCode, StringComparison.Ordinal))
                {
                    return slot.CutoffTime != null;
                }
            }

            return false;
        }

        private void Render(ReservationAvailabilityReadDto read)
        {
            ReservationSummaryDto summary = read.Summary;


            // 03 §8.5 — 다른 창구가 그 사이 예약을 넣었을 수 있다. 조회가 그것을 잡으면
            // 여기서도 신규예약을 접는다 (05 §9.6 `다른업무ID`).
            if (summary.OtherWorkId != null)
            {
                // 여기서는 일정을 모른다 — RS1 은 `다른업무ID` 만 준다 (05 §9.6).
                Leave(WorkContext.Reservation, summary.OtherWorkId.Value,
                    "그 사이 다른 창구가 이 수검자를 예약했습니다. 예약 관리에서 확인하시겠습니까?");
                return;
            }

            _slots = read.Slots;
            _view.Slots = read.Slots;
            _view.SlotCode = summary.SlotCode;

            // 05 §9.6 RS1 `예약구분` — DB 가 되돌려 준 값을 그대로 적는다. 화면이 든 값이 아니라
            // DB 의 답을 적는 이유는, 둘이 갈리면 사용자가 보는 쪽이 참이어야 해서다.
            _view.ReserveTypeText = clsWorkText.FormatReserveType(summary.ReserveType);
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
        private ReservationSaveRequest SaveRequest()
        {
            return new ReservationSaveRequest
            {
                PatientId = _patientId.Value,
                ReserveType = _reserveType,
                ReserveDate = _view.ReserveDate.Date,
                SlotCode = _view.SlotCode,
                AexSelected = _view.AexSelection,
                OperatorName = _operatorName,
            };
        }

        /// <summary>
        /// 05 §11.2 — **원하는 최종 상태를 통째로 보낸다.** 무엇이 바뀌었는지는 DB 가 현재
        /// 행과 견주어 잰다 (§9.4 변경범위). 화면이 미리 가르면 그 규칙이 두 곳에 생긴다.
        /// </summary>
        private ReservationChangeRequest ChangeRequest()
        {
            return new ReservationChangeRequest
            {
                WorkId = _workId.Value,
                RowVersion = _rowVersion,
                ReserveDate = _view.ReserveDate.Date,
                SlotCode = _view.SlotCode,
                AexSelected = _view.AexSelection,
                OperatorName = _operatorName,
            };
        }

        private void OnSaveRequested(object sender, EventArgs e)
        {
            if (_patientId == null)
            {
                return;
            }

            bool changing = _workId != null;
            string failure = changing ? "예약을 변경하지 못했습니다." : "예약을 저장하지 못했습니다.";

            OperationResult<WorkSaveReadDto> result;
            try
            {
                result = changing ? _service.Change(ChangeRequest()) : _service.Register(SaveRequest());
            }
            catch (Exception)
            {
                _view.BlockMessage = failure;
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                _view.BlockMessage = result == null ? failure : result.Message;
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
            //
            // **가르는 것은 예약구분이 아니라 날짜다** (2026-09-11 grilling). 오늘이면 그 사람은
            // 지금 창구에 서 있고 다음에 할 일이 접수다 — 09:30 에 온 현장 내원자는 예약구분이
            // 일반이라 예전 규칙으로는 예약 관리로 떨어졌다.
            long workId = result.Value.Row.WorkId;
            string savedSlot = _view.SlotCode;
            WorkContext target = IsToday(savedSlot)
                ? WorkContext.Reception
                : WorkContext.Reservation;

            // 03 §8.10 저장 성공 행 — 전체 Clear.
            Reset();
            _view.GoToWorkbench(target, workId, true);
        }
    }
}
