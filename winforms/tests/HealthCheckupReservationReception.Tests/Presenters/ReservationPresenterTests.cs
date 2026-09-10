using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Presenters
{
    /// <summary>
    /// WF-RSV-01 신규 예약 (03 §8).
    ///
    /// 재는 것은 **판정이 아니라 진행 단계와 배선**이다 — 정원·마감·TGT·AEX 가용성·저장가능은
    /// DB 가 내고, 이 계층이 그것을 접거나 다시 세지 않는지를 본다.
    /// </summary>
    [TestClass]
    public class ReservationPresenterTests
    {
        private static readonly DateTime Day = new DateTime(2026, 9, 14);
        private const long PatientId = 1000;

        // ── 03 §8.5 진행 상태

        [TestMethod]
        public void 최초에는_일정과_AEX_와_저장이_닫혀_있다()
        {
            var view = new FakeReservationView();
            new ReservationPresenter(view, new FakeReservationService(), new FakePatientService(), "창구");

            Assert.IsFalse(view.ScheduleEnabled, "수검자 확정 전에는 일정영역이 닫혀 있다");
            Assert.IsFalse(view.AexEnabled);
            Assert.IsFalse(view.SaveEnabled);
            Assert.AreEqual("대상판정 : 미판정", view.TargetText);
        }

        [TestMethod]
        public void 전달키로_들어오면_수검자가_확정되고_일정이_열린다()
        {
            var view = new FakeReservationView();
            var patients = Patients();
            var service = new FakeReservationService { Availability = Availability() };
            var presenter = new ReservationPresenter(view, service, patients, "창구");

            presenter.Begin(ReservationContext.Normal, PatientId, NavigationSource.PatientManagement);

            Assert.AreEqual("C000001", view.Patient.ChartNo);
            Assert.IsTrue(view.ScheduleEnabled);
            Assert.AreEqual(1, service.AvailabilityCalls, "일정영역을 연 뒤 한 번은 물어야 정원이 보인다");
        }

        // 03 §8.5 — 기존 유효예약이 있으면 신규예약을 중단하고 그 WorkId 로 Workbench 로 간다.
        [TestMethod]
        public void 기존_유효예약이_있으면_신규예약을_접고_Workbench_로_간다()
        {
            var view = new FakeReservationView();
            var patients = Patients();
            patients.ValidWorkResult = OperationResult<PatientValidWorkDto>.Success(
                new PatientValidWorkDto { WorkId = 55, ReserveDate = Day, StatusCode = "RSV" });
            var service = new FakeReservationService { Availability = Availability() };
            var presenter = new ReservationPresenter(view, service, patients, "창구");

            presenter.Begin(ReservationContext.Normal, PatientId, NavigationSource.PatientManagement);

            Assert.AreEqual(55L, view.WorkbenchWorkId);
            Assert.AreEqual(WorkContext.Reservation, view.WorkbenchContext);
            Assert.IsFalse(view.ScheduleEnabled, "신규예약을 이어 가면 안 된다");
            Assert.AreEqual(0, service.AvailabilityCalls, "접을 것을 조회했다");
        }

        // 03 §8.5 — 조회 시점에 다른 창구가 예약을 넣었을 수도 있다 (05 §9.6 다른업무ID).
        [TestMethod]
        public void 조회가_다른업무ID_를_주면_그때도_Workbench_로_간다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Summary.OtherWorkId = 77;
            var service = new FakeReservationService { Availability = read };
            var presenter = new ReservationPresenter(view, service, Patients(), "창구");

            presenter.Begin(ReservationContext.Normal, PatientId, NavigationSource.Navigation);

            Assert.AreEqual(77L, view.WorkbenchWorkId);
        }

        // ── 05 §9.12 — `저장가능` 은 DB 것이다

        [TestMethod]
        public void 저장_버튼은_DB_의_저장가능_그대로다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Summary.CanSave = true;
            var presenter = Loaded(view, read);

            Assert.IsTrue(view.SaveEnabled);

            read.Summary.CanSave = false;
            read.Summary.BlockCode = (int)DbCode.SlotFull;
            read.Summary.BlockMessage = "해당 시간대의 정원이 찼습니다.";
            view.ReserveDate = Day.AddDays(1);
            view.RaiseScheduleChanged();

            Assert.IsFalse(view.SaveEnabled);
            Assert.AreEqual("해당 시간대의 정원이 찼습니다.", view.BlockMessage, "차단 사유가 보이지 않는다");
        }

        // ── 03 §8.7 대상판정 문구

        [TestMethod]
        public void 최초검진이면_대상_최초검진이다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Target = new ExamTargetDto { IsTarget = true, Age = 27, LastCompletedDate = null };
            Loaded(view, read);

            Assert.AreEqual("대상판정 : 대상 — 최초검진", view.TargetText);
        }

        [TestMethod]
        public void 완료이력이_있으면_최근_완료연도를_적는다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Target = new ExamTargetDto
            {
                IsTarget = true,
                Age = 46,
                LastCompletedDate = new DateTime(2024, 5, 11),
            };
            Loaded(view, read);

            Assert.AreEqual("대상판정 : 대상 — 최근 완료연도 2024", view.TargetText);
        }

        // 비대상 사유는 화면이 짓지 않는다 — DB 가 준 `사유메시지` 를 그대로 붙인다.
        [TestMethod]
        public void 비대상이면_DB_가_준_사유를_그대로_붙인다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Target = new ExamTargetDto
            {
                IsTarget = false,
                Age = 19,
                ReasonCode = (int)DbCode.UnderAge,
                ReasonMessage = "예약일 기준 만 20세 미만입니다.",
            };
            Loaded(view, read);

            Assert.AreEqual("대상판정 : 비대상 — 예약일 기준 만 20세 미만입니다.", view.TargetText);
        }

        // 03 §8.5 — 비대상이면 AEX Disabled.
        [TestMethod]
        public void 비대상이면_AEX_를_닫는다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Target = new ExamTargetDto { IsTarget = false, ReasonMessage = "2년 주기가 도래하지 않았습니다." };
            Loaded(view, read);

            Assert.IsFalse(view.AexEnabled);
        }

        [TestMethod]
        public void 일정을_아직_못_잡으면_미판정이다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Target = null;   // 05 §9.8 — 일정 평가가 불가능하면 0행이다
            Loaded(view, read);

            Assert.AreEqual("대상판정 : 미판정", view.TargetText);
            Assert.IsFalse(view.AexEnabled);
        }

        // ── 03 §8.10 AEX 유지 규칙

        // 예약일이 바뀌면 **유효 선택만 유지**한다. 무엇이 살아남았는지는 DB 의 `유효선택여부` 다.
        [TestMethod]
        public void 예약일이_바뀌면_유효_선택만_남는다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.AexItems = new List<ReservationAexItemDto>
            {
                // 요청은 했지만 DB 가 인정하지 않았다 — 새 예약일에서 성별·중복으로 막힌 것.
                new ReservationAexItemDto { AexCode = "OPT01", Requested = true, EffectiveSelected = false, Selectable = false },
                new ReservationAexItemDto { AexCode = "OPT02", Requested = true, EffectiveSelected = true, Selectable = true },
            };
            Loaded(view, read);

            Assert.IsFalse(view.AexItems[0].Requested, "인정받지 못한 선택이 남았다");
            Assert.IsTrue(view.AexItems[1].Requested);
        }

        // ── 조회 횟수

        // [X] DateEdit 은 글자를 칠 때마다 값이 바뀐다. 같은 일정으로 동기 SP 를 되풀이해
        //     부르면 그때마다 화면이 얼어붙는다.
        [TestMethod]
        public void 같은_일정으로는_다시_묻지_않는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService { Availability = Availability() };
            var presenter = new ReservationPresenter(view, service, Patients(), "창구");
            presenter.Begin(ReservationContext.Normal, PatientId, NavigationSource.Navigation);
            Assert.AreEqual(1, service.AvailabilityCalls);

            view.RaiseScheduleChanged();
            view.RaiseScheduleChanged();

            Assert.AreEqual(1, service.AvailabilityCalls, "같은 예약일·시간대로 SP 를 다시 불렀다");
        }

        [TestMethod]
        public void 시간대를_고르면_다시_묻는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService { Availability = Availability() };
            var presenter = new ReservationPresenter(view, service, Patients(), "창구");
            presenter.Begin(ReservationContext.Normal, PatientId, NavigationSource.Navigation);

            view.SlotCode = "PM";
            view.RaiseScheduleChanged();

            Assert.AreEqual(2, service.AvailabilityCalls);
            Assert.AreEqual("PM", service.LastAvailability.SlotCode);
        }

        // ── 03 §8.11 2단계 저장

        [TestMethod]
        public void 저장에_성공하면_전체를_비우고_예약_Workbench_로_간다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = Availability(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = Ok(),
                    Row = new WorkSaveResultDto { WorkId = 91, StatusCode = "RSV", RowVersion = new byte[8] },
                }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), "창구");
            presenter.Begin(ReservationContext.Normal, PatientId, NavigationSource.Navigation);

            view.RaiseSaveRequested();

            Assert.AreEqual(91L, view.WorkbenchWorkId);
            Assert.AreEqual(WorkContext.Reservation, view.WorkbenchContext);
            Assert.IsNull(view.Patient, "03 §8.10 저장 성공 — 전체 Clear");
            Assert.IsFalse(view.SaveEnabled);
            Assert.AreEqual("창구", service.LastSave.OperatorName);
        }

        // 03 §9.8 — WalkIn 은 Reservation 이 아니라 Reception 으로 돌아간다.
        [TestMethod]
        public void WalkIn_저장은_접수_Workbench_로_간다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = Availability(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = Ok(),
                    Row = new WorkSaveResultDto { WorkId = 92, StatusCode = "RSV", RowVersion = new byte[8] },
                }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), "창구");
            presenter.Begin(ReservationContext.WalkIn, PatientId, NavigationSource.ReceptionDesk);

            view.RaiseSaveRequested();

            Assert.AreEqual(WorkContext.Reception, view.WorkbenchContext);
            Assert.AreEqual(DbReserveType.WalkIn, service.LastSave.ReserveType);
        }

        // 03 §8.6 — WalkIn 은 예약일이 ReadOnly 다.
        [TestMethod]
        public void WalkIn_은_예약일을_고치지_못한다()
        {
            var view = new FakeReservationView();
            var presenter = new ReservationPresenter(
                view, new FakeReservationService { Availability = Availability() }, Patients(), "창구");

            presenter.Begin(ReservationContext.WalkIn, PatientId, NavigationSource.ReceptionDesk);

            Assert.IsTrue(view.ReserveDateReadOnly);
        }

        // 03 §8.10 — 모달을 닫을 때 폐기를 묻는 근거. **수검자가 확정된 순간부터** 화면에는
        // 사용자가 들인 것이 있고(일정·AEX), 저장이 끝나면 Reset 이 그것을 내린다.
        [TestMethod]
        public void 수검자가_확정되기_전에는_폐기를_묻지_않는다()
        {
            var view = new FakeReservationView();
            var presenter = new ReservationPresenter(
                view, new FakeReservationService(), new FakePatientService(), "창구");

            Assert.IsFalse(presenter.HasUnsavedInput);
        }

        [TestMethod]
        public void 수검자가_확정되면_폐기를_묻는다()
        {
            var view = new FakeReservationView();
            var presenter = new ReservationPresenter(
                view, new FakeReservationService { Availability = Availability() }, Patients(), "창구");

            presenter.Begin(ReservationContext.Normal, PatientId, NavigationSource.Navigation);

            Assert.IsTrue(presenter.HasUnsavedInput);
        }

        // 03 §8.11 실패 — Commit 없음. 사유를 보이고 일정·대상·검사구성을 최신값으로 다시 읽는다.
        [TestMethod]
        public void 저장이_DB_에서_막히면_사유를_보이고_최신값을_다시_읽는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = Availability(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = new DbResult
                    {
                        Success = false,
                        Code = (int)DbCode.SlotFull,
                        Message = "해당 시간대의 정원이 찼습니다.",
                    },
                }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), "창구");
            presenter.Begin(ReservationContext.Normal, PatientId, NavigationSource.Navigation);
            int before = service.AvailabilityCalls;

            view.RaiseSaveRequested();

            Assert.AreEqual("해당 시간대의 정원이 찼습니다.", view.BlockMessage);
            Assert.IsNull(view.WorkbenchWorkId, "Commit 이 없는데 Workbench 로 갔다");
            Assert.AreEqual(before + 1, service.AvailabilityCalls, "최신값을 다시 읽지 않았다");
        }

        // 05 §11 — 성공이면 RS1 이 온다. 없으면 계약 위반이므로 업무ID 를 지어내지 않는다.
        [TestMethod]
        public void 성공인데_업무ID_가_없으면_Workbench_로_넘기지_않는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = Availability(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto { Result = Ok() }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), "창구");
            presenter.Begin(ReservationContext.Normal, PatientId, NavigationSource.Navigation);

            view.RaiseSaveRequested();

            Assert.IsNull(view.WorkbenchWorkId);
        }

        // 킷 §6 — provider 메시지는 DB·머신 정보를 드러낸다. 화면에 원문을 싣지 않는다.
        [TestMethod]
        public void 예외가_나도_예외_본문을_화면에_싣지_않는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                AvailabilityFailure = new InvalidOperationException("서버 DESKTOP-XYZ 의 로그인에 실패했습니다"),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), "창구");

            presenter.Begin(ReservationContext.Normal, PatientId, NavigationSource.Navigation);

            Assert.AreEqual("예약 가능정보를 조회하지 못했습니다.", view.BlockMessage);
            StringAssert.DoesNotMatch(view.BlockMessage, new System.Text.RegularExpressions.Regex("DESKTOP"));
        }

        // ── helpers

        private static ReservationPresenter Loaded(FakeReservationView view, ReservationAvailabilityReadDto read)
        {
            var presenter = new ReservationPresenter(
                view, new FakeReservationService { Availability = read }, Patients(), "창구");
            presenter.Begin(ReservationContext.Normal, PatientId, NavigationSource.Navigation);
            return presenter;
        }

        private static FakePatientService Patients()
        {
            return new FakePatientService
            {
                DetailResult = OperationResult<PatientDetailDto>.Success(new PatientDetailDto
                {
                    PatientId = PatientId,
                    ChartNo = "C000001",
                    Name = "홍길동",
                    Birthday = "19800101",
                    Gender = "M",
                }),
            };
        }

        private static DbResult Ok()
        {
            return new DbResult
            {
                Success = true,
                Code = (int)DbCode.Ok,
                Message = "정상 처리되었습니다.",
                ServerTime = new DateTime(2026, 9, 10, 9, 0, 0),
            };
        }

        private static ReservationAvailabilityReadDto Availability()
        {
            return new ReservationAvailabilityReadDto
            {
                Result = Ok(),
                Summary = new ReservationSummaryDto
                {
                    ChangeScope = "ALL",
                    PatientId = PatientId,
                    ReserveType = DbReserveType.Normal,
                    ReserveDate = Day,
                    SlotCode = "AM",
                    WorkAllowed = true,
                    CanSave = true,
                    BlockCode = (int)DbCode.Ok,
                    BlockMessage = string.Empty,
                },
                Slots = new List<SlotInfoDto>
                {
                    new SlotInfoDto { SlotCode = "AM", SlotName = "오전", Capacity = 20, CurrentCount = 12, AppliedCount = 13, RemainingSeats = 7, IsOperating = true, Selectable = true, BlockMessage = string.Empty },
                    new SlotInfoDto { SlotCode = "PM", SlotName = "오후", Capacity = 20, CurrentCount = 20, AppliedCount = 21, RemainingSeats = 0, IsOperating = true, Selectable = false, BlockCode = (int)DbCode.SlotFull, BlockMessage = "해당 시간대의 정원이 찼습니다." },
                },
                Target = new ExamTargetDto { IsTarget = true, Age = 46, ReasonMessage = string.Empty },
                NexItems = new List<WorkExamItemDto>
                {
                    new WorkExamItemDto { ExamItemCode = "EX001", ExamItemName = "문진/진찰", NexType = "BASIC" },
                },
                AexItems = new List<ReservationAexItemDto>
                {
                    new ReservationAexItemDto { AexCode = "OPT01", ExamItemName = "복부초음파", Selectable = true, ReasonMessage = string.Empty },
                },
            };
        }
    }

    internal sealed class FakeReservationView : IReservationView
    {
        public event EventHandler ScheduleChanged;
        public event EventHandler SaveRequested;

        public FakeReservationView()
        {
            AexSelection = new bool[ReservationAvailabilityRequest.AexParameterCount];
            ReserveDate = DateTime.Today;
        }

        public PatientDetailDto Patient { get; set; }
        public bool ScheduleEnabled { get; set; }
        public DateTime ReserveDate { get; set; }
        public bool ReserveDateReadOnly { get; set; }
        public IList<SlotInfoDto> Slots { get; set; }
        public string SlotCode { get; set; }
        public string TargetText { get; set; }
        public IList<WorkExamItemDto> NexItems { get; set; }
        public IList<ReservationAexItemDto> AexItems { get; set; }
        public bool AexEnabled { get; set; }
        public bool[] AexSelection { get; set; }
        public bool SaveEnabled { get; set; }
        public string BlockMessage { get; set; }
        public string LastMessage { get; private set; }

        public WorkContext? WorkbenchContext { get; private set; }
        public long? WorkbenchWorkId { get; private set; }

        public void GoToWorkbench(WorkContext context, long workId)
        {
            WorkbenchContext = context;
            WorkbenchWorkId = workId;
        }

        public void ShowMessage(string message) { LastMessage = message; }

        public void RaiseScheduleChanged()
        {
            EventHandler handler = ScheduleChanged;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }

        public void RaiseSaveRequested()
        {
            EventHandler handler = SaveRequested;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }
    }

    internal sealed class FakeReservationService : IReservationService
    {
        public ReservationAvailabilityReadDto Availability { get; set; }
        public OperationResult<WorkSaveReadDto> Save { get; set; }
        public Exception AvailabilityFailure { get; set; }

        public int AvailabilityCalls { get; private set; }
        public ReservationAvailabilityRequest LastAvailability { get; private set; }
        public ReservationSaveRequest LastSave { get; private set; }

        public OperationResult<ReservationAvailabilityReadDto> GetAvailability(ReservationAvailabilityRequest request)
        {
            if (AvailabilityFailure != null)
            {
                throw AvailabilityFailure;
            }

            AvailabilityCalls++;
            LastAvailability = request;
            return Availability == null
                ? OperationResult<ReservationAvailabilityReadDto>.Failure("없다")
                : OperationResult<ReservationAvailabilityReadDto>.Success(Availability);
        }

        public OperationResult<WorkSaveReadDto> Register(ReservationSaveRequest request)
        {
            LastSave = request;
            return Save ?? OperationResult<WorkSaveReadDto>.Failure("없다");
        }
    }
}
