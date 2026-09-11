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
    [TestClass]
    public class WorkbenchPresenterTests
    {
        private static readonly DateTime Today = new DateTime(2026, 9, 10);

        // ── 03 §9.3 조회조건

        [TestMethod]
        public void 기간이_서_있으면_조회하고_목록을_채운다()
        {
            var view = new FakeWorkbenchView { FromDate = Today, ToDate = Today };
            var service = new FakeWorkService { SearchResult = Ok(OneRow()) };
            var presenter = new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            presenter.LoadInitial();

            Assert.AreEqual(Today, service.LastSearch.FromDate);
            Assert.AreEqual(Today, service.LastSearch.ToDate);
            Assert.AreEqual(1, view.Rows.Count);
            Assert.IsNull(view.ValidationMessage);
        }

        // 03 §9.3 — From>To 는 Inline 오류다. 왕복하지 않는다.
        [TestMethod]
        public void 시작일이_종료일보다_늦으면_Inline_오류를_내고_조회하지_않는다()
        {
            var view = new FakeWorkbenchView { FromDate = Today.AddDays(1), ToDate = Today };
            var service = new FakeWorkService { SearchResult = Ok(OneRow()) };
            new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            view.RaiseSearchRequested();

            Assert.AreEqual("시작일이 종료일보다 늦습니다.", view.ValidationMessage);
            Assert.IsNull(service.LastSearch, "SP 를 불렀다");
            Assert.IsNull(view.LastMessage, "Inline 오류를 모달로 내지 않는다");
        }

        // 03 §9.3 — 최소 하나의 실질 조건이 필요하고 상태 `전체` 만 고른 것은 조건이 아니다.
        [TestMethod]
        public void 상태만_골라서는_조건이_되지_않는다()
        {
            var view = new FakeWorkbenchView { StatusCode = "RSV" };
            var service = new FakeWorkService { SearchResult = Ok(OneRow()) };
            new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            view.RaiseSearchRequested();

            Assert.AreEqual("기간·차트번호·이름 중 하나 이상을 입력하세요.", view.ValidationMessage);
            Assert.IsNull(service.LastSearch, "SP 를 불렀다");
        }

        [TestMethod]
        public void 차트번호만_있어도_조건이_된다()
        {
            var view = new FakeWorkbenchView { ChartNo = "C-0001" };
            var service = new FakeWorkService { SearchResult = Ok(new List<WorkListItemDto>()) };
            new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            view.RaiseSearchRequested();

            Assert.IsNotNull(service.LastSearch);
            Assert.AreEqual("C-0001", service.LastSearch.ChartNo);
            Assert.IsNull(view.ValidationMessage);
        }

        // 03 §9.4 — 재조회 시 선택·상세·Transaction Action 을 Clear 한다.
        [TestMethod]
        public void 재조회하면_상세와_Action_이_함께_닫힌다()
        {
            var view = new FakeWorkbenchView { FromDate = Today, ToDate = Today };
            var service = new FakeWorkService
            {
                SearchResult = Ok(OneRow()),
                DetailResult = OkDetail(Allowed(DbWorkAction.EditReservation)),
            };
            new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");
            view.RaiseSelectionChanged(77);
            Assert.IsNotNull(view.Detail, "먼저 상세가 서 있어야 이 시험이 뜻을 갖는다");

            view.RaiseSearchRequested();

            Assert.IsNull(view.Detail);
            Assert.AreEqual(0, view.NexItems.Count);
            Assert.AreEqual(0, view.AexItems.Count);
            Assert.IsFalse(view.Actions.EditReservation);
            Assert.IsFalse(view.Actions.ChangeLog);
        }

        // ── 03 §9.5 · §9.6 · §9.7 선택과 Action

        [TestMethod]
        public void 행을_고르면_상세와_검사구성이_서고_RS4_가_Ribbon_상태가_된다()
        {
            var view = new FakeWorkbenchView();
            var service = new FakeWorkService
            {
                DetailResult = OkDetail(Allowed(DbWorkAction.EditReservation, DbWorkAction.CancelReservation)),
            };
            new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            view.RaiseSelectionChanged(77);

            Assert.AreEqual(77L, service.LastDetailWorkId);
            Assert.AreEqual("C-0001", view.Detail.ChartNo);
            Assert.AreEqual(1, view.NexItems.Count);
            Assert.AreEqual(1, view.AexItems.Count);
            Assert.IsTrue(view.Actions.EditReservation);
            Assert.IsTrue(view.Actions.CancelReservation);
            Assert.IsFalse(view.Actions.StartReception);
            Assert.IsFalse(view.Actions.EditExtra);
            Assert.IsFalse(view.Actions.CancelReception);
        }

        // 03 §9.6 · §23.4 — [변경이력] 은 상태와 무관하게 행이 선택되면 열린다.
        // 취소된 업무는 다섯 동작이 전부 닫히지만 변경 내역은 남아 있고 그것을 보는 것이 목적이다.
        [TestMethod]
        public void 모든_업무동작이_닫혀도_변경이력은_열린다()
        {
            var view = new FakeWorkbenchView();
            var service = new FakeWorkService { DetailResult = OkDetail(Allowed()) };
            new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            view.RaiseSelectionChanged(77);

            Assert.IsFalse(view.Actions.EditReservation);
            Assert.IsFalse(view.Actions.CancelReservation);
            Assert.IsFalse(view.Actions.StartReception);
            Assert.IsFalse(view.Actions.EditExtra);
            Assert.IsFalse(view.Actions.CancelReception);
            Assert.IsTrue(view.Actions.ChangeLog, "행이 선택되면 열려야 한다");
        }

        // [X] 화면이 허용여부를 다시 계산하지 않는다 (05 §8.2). DB 가 1 을 주면 1 이다 —
        //     상태·마감·공통 업무조건의 조합은 이미 거기서 판정된 것이다.
        [TestMethod]
        public void 허용여부는_DB_가_준_값_그대로다()
        {
            var view = new FakeWorkbenchView();
            var service = new FakeWorkService
            {
                DetailResult = OkDetail(Allowed(
                    DbWorkAction.StartReception, DbWorkAction.EditExtra, DbWorkAction.CancelReception)),
            };
            new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            view.RaiseSelectionChanged(77);

            Assert.IsTrue(view.Actions.StartReception);
            Assert.IsTrue(view.Actions.EditExtra);
            Assert.IsTrue(view.Actions.CancelReception);
            Assert.IsFalse(view.Actions.EditReservation);
        }

        [TestMethod]
        public void 선택이_풀리면_상세와_Action_이_닫힌다()
        {
            var view = new FakeWorkbenchView();
            var service = new FakeWorkService { DetailResult = OkDetail(Allowed(DbWorkAction.EditReservation)) };
            new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");
            view.RaiseSelectionChanged(77);

            view.RaiseSelectionChanged(null);

            Assert.IsNull(view.Detail);
            Assert.IsFalse(view.Actions.ChangeLog);
            Assert.AreEqual(1, service.DetailCalls, "선택이 없는데 상세를 다시 조회했다");
        }

        [TestMethod]
        public void 상세_조회가_실패하면_Action_을_닫고_사유를_알린다()
        {
            var view = new FakeWorkbenchView();
            var service = new FakeWorkService
            {
                DetailResult = OperationResult<WorkDetailReadDto>.Failure("대상 업무를 찾을 수 없습니다."),
            };
            new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            view.RaiseSelectionChanged(77);

            Assert.IsNull(view.Detail);
            Assert.IsFalse(view.Actions.ChangeLog);
            Assert.AreEqual("대상 업무를 찾을 수 없습니다.", view.LastMessage);
        }

        // 킷 §6 — provider 메시지는 DB·머신 정보를 드러낸다. 화면에 원문을 싣지 않는다.
        [TestMethod]
        public void 예외가_나도_예외_본문을_화면에_싣지_않는다()
        {
            var view = new FakeWorkbenchView { FromDate = Today };
            var service = new FakeWorkService
            {
                SearchFailure = new InvalidOperationException("서버 DESKTOP-XYZ 의 로그인에 실패했습니다"),
            };
            new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            view.RaiseSearchRequested();

            Assert.AreEqual("예약·접수 목록을 조회하지 못했습니다.", view.ValidationMessage);
            StringAssert.DoesNotMatch(view.ValidationMessage, new System.Text.RegularExpressions.Regex("DESKTOP"));
        }

        // [X] **조회 실패는 모달이 아니라 Inline 이다.** 모달이면 창을 열자마자 뜨는 것을
        //     막느라 조용히 삼켜야 하고, 그러면 실패가 아예 보이지 않는다 — 2026-09-10 에
        //     성공한 0건과 구별이 안 돼 "조회가 안 된다" 로 보고됐다.
        [TestMethod]
        public void 조회_실패는_모달이_아니라_Inline_이다()
        {
            var view = new FakeWorkbenchView { FromDate = Today };
            var service = new FakeWorkService
            {
                SearchResult = OperationResult<IList<WorkListItemDto>>.Failure("읽지 못했습니다."),
            };
            var presenter = new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            presenter.LoadInitial();

            Assert.AreEqual("읽지 못했습니다.", view.ValidationMessage, "실패가 보이지 않는다");
            Assert.IsNull(view.LastMessage, "창을 열자마자 모달이 떴다");
        }

        // ── 03 §9.1 Context

        [TestMethod]
        public void 기본_Context_는_예약_관리다()
        {
            var view = new FakeWorkbenchView();
            new WorkbenchPresenter(view, new FakeWorkService(), new FakeReservationService(), Status(Today), "접수1번창구");

            Assert.AreEqual("예약 관리", view.ContextTitle);
        }

        // 03 §9.1 — Context 를 바꾸면 기존 SelectedRow 를 해제하고 우측 Detail 을 Clear 한다.
        // 같은 행이 다른 Context 에서 다른 Action 을 갖기 때문이다 (§9.6 · §9.7).
        [TestMethod]
        public void Context_를_바꾸면_선택을_승계하지_않는다()
        {
            var view = new FakeWorkbenchView();
            var service = new FakeWorkService { DetailResult = OkDetail(Allowed(DbWorkAction.EditReservation)) };
            var presenter = new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");
            view.RaiseSelectionChanged(77);

            presenter.OpenContext(WorkContext.Reception, null);

            Assert.AreEqual("접수 관리", view.ContextTitle);
            Assert.IsNull(view.Detail);
            Assert.IsFalse(view.Actions.EditReservation);
            Assert.IsFalse(view.Actions.ChangeLog);
        }

        // ── 03 §9.1 WorkId Targeted Navigation

        // 신규예약 저장 성공(03 §8.11)과 접수 Shortcut 이 이 길로 돌아온다.
        // [X] 조회조건을 그대로 두면 방금 저장한 건이 목록에 없을 수 있다 — 그 날 하루로 좁힌다.
        [TestMethod]
        public void WorkId_로_열면_그_날짜로_좁혀_조회하고_그_행을_고른다()
        {
            var view = new FakeWorkbenchView { FromDate = Today.AddDays(-30), SelectFound = true };
            var service = new FakeWorkService
            {
                SearchResult = Ok(OneRow()),
                DetailResult = OkDetail(Allowed(DbWorkAction.EditReservation)),
            };
            var presenter = new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            presenter.OpenContext(WorkContext.Reservation, 77);

            Assert.AreEqual(Today, view.FocusedDay, "그 업무의 예약일로 좁히지 않았다");
            Assert.AreEqual(Today, view.FromDate);
            Assert.AreEqual(Today, view.ToDate);
            Assert.IsNull(view.StatusCode, "상태 조건이 남으면 방금 저장한 건이 안 보일 수 있다");
            Assert.AreEqual(77L, view.SelectedWorkId);
            Assert.IsNotNull(view.Rows);
        }

        [TestMethod]
        public void WorkId_를_목록에서_못_찾으면_Inline_으로_알린다()
        {
            var view = new FakeWorkbenchView { SelectFound = false };
            var service = new FakeWorkService
            {
                SearchResult = Ok(new List<WorkListItemDto>()),
                DetailResult = OkDetail(Allowed()),
            };
            var presenter = new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            presenter.OpenContext(WorkContext.Reception, 77);

            Assert.AreEqual("방금 저장한 업무를 목록에서 찾지 못했습니다.", view.ValidationMessage);
        }

        /// <summary>
        /// 2026-09-11 — **뒤집힌 규칙이다.** 예전에는 상단 전환이 조회조건을 건드리지 않았고,
        /// 그래서 두 탭이 같은 목록을 보고 있었다. 이제 탭을 열면 그 창구의 기간으로 세우고
        /// 다시 조회한다. `FocusSearchOn` 은 여전히 안 쓴다 — 그쪽은 저장 뒤 한 건을 겨누는 길이다.
        /// </summary>
        [TestMethod]
        public void WorkId_가_없으면_그_창구의_기간으로_세우고_다시_조회한다()
        {
            var view = new FakeWorkbenchView { FromDate = Today.AddDays(-30) };
            var service = new FakeWorkService { SearchResult = Ok(OneRow()) };
            var presenter = new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            presenter.OpenContext(WorkContext.Reception, null);

            Assert.IsNull(view.FocusedDay);
            Assert.AreEqual(Today, view.FromDate);
            Assert.AreEqual(Today, view.ToDate);
            Assert.IsNotNull(service.LastSearch, "탭을 열었는데 조회하지 않았다");
        }

        // ── helpers

        private static OperationResult<IList<WorkListItemDto>> Ok(IList<WorkListItemDto> rows)
        {
            return OperationResult<IList<WorkListItemDto>>.Success(rows);
        }

        // ── 2026-09-11: 03 §13 취소 둘

        /// <summary>
        /// 03 §13.1 — 묻고, 부르고, 다시 읽는다. 문구의 핵심은 **되돌릴 수 없다**는 것이다.
        /// </summary>
        [TestMethod]
        public void 예약취소는_묻고_행버전을_실어_보낸다()
        {
            var view = new FakeWorkbenchView { ConfirmAnswer = true, SelectFound = true };
            var service = new FakeWorkService { SearchResult = Ok(OneRow()), DetailResult = OkDetail(Allowed()) };
            var reservation = new FakeReservationService { Save = SavedOk() };
            var presenter = new WorkbenchPresenter(view, service, reservation, Status(Today), "접수1번창구");
            presenter.LoadInitial();
            view.RaiseSelectionChanged(77);

            view.RaiseAction(DbWorkAction.CancelReservation);

            Assert.AreEqual(1, view.Questions.Count, "묻지 않았거나 두 번 물었다");
            StringAssert.Contains(view.Questions[0], "복원할 수 없습니다");
            Assert.AreEqual(77L, reservation.LastCancel.WorkId);
            Assert.IsNotNull(reservation.LastCancel.RowVersion, "행버전을 안 실었다");
            Assert.AreEqual("접수1번창구", reservation.LastCancel.OperatorName);
        }

        [TestMethod]
        public void 아니오라고_하면_부르지_않는다()
        {
            var view = new FakeWorkbenchView { ConfirmAnswer = false, SelectFound = true };
            var service = new FakeWorkService { SearchResult = Ok(OneRow()), DetailResult = OkDetail(Allowed()) };
            var reservation = new FakeReservationService();
            var presenter = new WorkbenchPresenter(view, service, reservation, Status(Today), "접수1번창구");
            presenter.LoadInitial();
            view.RaiseSelectionChanged(77);

            view.RaiseAction(DbWorkAction.CancelReservation);

            Assert.AreEqual(1, view.Questions.Count);
            Assert.IsNull(reservation.LastCancel, "아니오라고 했는데 SP 를 불렀다");
        }

        [TestMethod]
        public void 접수취소는_접수_계열_SP_로_간다()
        {
            var view = new FakeWorkbenchView { ConfirmAnswer = true, SelectFound = true };
            var service = new FakeWorkService
            {
                SearchResult = Ok(OneRow()),
                DetailResult = OkDetail(Allowed()),
                SaveResult = SavedOk(),
            };
            var presenter = new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");
            presenter.LoadInitial();
            view.RaiseSelectionChanged(77);

            view.RaiseAction(DbWorkAction.CancelReception);

            StringAssert.Contains(view.Questions[0], "되돌아가지 않으며");
            Assert.AreEqual(77L, service.LastCancel.WorkId);
        }

        /// <summary>
        /// [X] **DB 가 막아도 최신값을 다시 읽는다.** 행버전 충돌(601)이면 그 다시 읽기가 곧
        ///     복구다 — 사유만 적고 옛 값을 두면 다음 Action 도 같은 이유로 막힌다.
        /// </summary>
        [TestMethod]
        public void DB_가_막으면_사유를_적고_최신값을_다시_읽는다()
        {
            var view = new FakeWorkbenchView { ConfirmAnswer = true, SelectFound = true };
            var service = new FakeWorkService { SearchResult = Ok(OneRow()), DetailResult = OkDetail(Allowed()) };
            var reservation = new FakeReservationService
            {
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = new DbResult
                    {
                        Success = false,
                        Code = 601,
                        Message = "다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.",
                    },
                }),
            };
            var presenter = new WorkbenchPresenter(view, service, reservation, Status(Today), "접수1번창구");
            presenter.LoadInitial();
            view.RaiseSelectionChanged(77);
            int before = service.DetailCalls;

            view.RaiseAction(DbWorkAction.CancelReservation);

            StringAssert.Contains(view.ValidationMessage, "최신 정보를 다시 조회");
            Assert.IsTrue(service.DetailCalls > before, "막힌 뒤 최신값을 다시 읽지 않았다");
        }

        [TestMethod]
        public void 행이_없으면_묻지도_않는다()
        {
            var view = new FakeWorkbenchView { ConfirmAnswer = true };
            var reservation = new FakeReservationService();
            new WorkbenchPresenter(view, new FakeWorkService(), reservation, Status(Today), "접수1번창구");

            view.RaiseAction(DbWorkAction.CancelReservation);

            Assert.AreEqual(0, view.Questions.Count);
            Assert.IsNull(reservation.LastCancel);
        }

        private static OperationResult<WorkSaveReadDto> SavedOk()
        {
            return OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
            {
                Result = new DbResult { Success = true, Code = 0, Message = "정상 처리되었습니다." },
                Row = new WorkSaveResultDto { WorkId = 77, StatusCode = "CNR", RowVersion = new byte[8] },
            });
        }

        // ── 2026-09-11: 탭은 창구다

        /// <summary>
        /// 접수 창구의 목록에 **오늘의 `RSV`** 가 있어야 한다 — 접수의 입력이 예약 건이므로
        /// 그것이 안 보이면 창구는 자기 탭에서 할 일이 없다. `CNR` 은 예약 창구의 것이다.
        /// </summary>
        [TestMethod]
        public void 접수_창구는_예약완료와_접수완료와_접수취소만_본다()
        {
            var view = new FakeWorkbenchView();
            var service = new FakeWorkService { SearchResult = Ok(AllStatuses()) };
            var presenter = new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            presenter.OpenContext(WorkContext.Reception, null);

            CollectionAssert.AreEquivalent(
                new[] { DbWorkStatus.Reserved, DbWorkStatus.Received, DbWorkStatus.CancelledReception },
                Codes(view.Rows));
            CollectionAssert.AreEquivalent(
                new[] { DbWorkStatus.Reserved, DbWorkStatus.Received, DbWorkStatus.CancelledReception },
                new List<string>(view.StatusChoices));
        }

        [TestMethod]
        public void 예약_창구는_예약완료와_예약취소만_본다()
        {
            var view = new FakeWorkbenchView();
            var service = new FakeWorkService { SearchResult = Ok(AllStatuses()) };
            var presenter = new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            presenter.OpenContext(WorkContext.Reservation, null);

            CollectionAssert.AreEquivalent(
                new[] { DbWorkStatus.Reserved, DbWorkStatus.CancelledReservation },
                Codes(view.Rows));
        }

        /// <summary>
        /// 접수는 당일 업무다 (05 §8.2 `START_RECEPTION` 이 `예약일=오늘`) — 오늘 하루로 선다.
        /// 예약은 앞으로의 일정이라 종료일을 비워 둔다. 둘 다 기본값일 뿐 사용자가 바꾼다.
        /// </summary>
        [TestMethod]
        public void 창구마다_기간_기본값이_다르다()
        {
            var view = new FakeWorkbenchView();
            var service = new FakeWorkService { SearchResult = Ok(OneRow()) };
            var presenter = new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");

            presenter.OpenContext(WorkContext.Reception, null);
            Assert.AreEqual(Today, view.RangeFrom);
            Assert.AreEqual(Today, view.RangeTo, "접수는 오늘 하루다");

            presenter.OpenContext(WorkContext.Reservation, null);
            Assert.AreEqual(Today, view.RangeFrom);
            Assert.IsNull(view.RangeTo, "예약은 앞이 열려 있어야 한다");
        }

        /// <summary>
        /// [X] **오늘을 PC 시계에서 얻지 않는다.** DB 오늘날짜를 못 읽으면 기간을 건드리지 않고
        ///     사유만 적는다 — 잘못된 날로 세우느니 사용자가 고르게 둔다.
        /// </summary>
        [TestMethod]
        public void 오늘_날짜를_못_읽으면_기간을_건드리지_않는다()
        {
            var view = new FakeWorkbenchView { FromDate = Today.AddDays(-3) };
            var service = new FakeWorkService { SearchResult = Ok(OneRow()) };
            var status = new FakeCommonStatusService { Failure = new InvalidOperationException("끊겼다") };
            var presenter = new WorkbenchPresenter(view, service, new FakeReservationService(), status, "접수1번창구");

            presenter.OpenContext(WorkContext.Reception, null);

            Assert.IsNull(view.RangeFrom, "기간을 세웠다");
            Assert.AreEqual("오늘 날짜를 확인하지 못해 기간을 세우지 못했습니다.", view.ValidationMessage);
        }

        private static FakeCommonStatusService Status(DateTime today)
        {
            return new FakeCommonStatusService
            {
                Result = OperationResult<CommonWorkStatusDto>.Success(new CommonWorkStatusDto
                {
                    Today = today,
                    DayName = "목요일",
                    IsBusinessDay = true,
                    IsWithinHours = true,
                    IsWorkAllowed = true,
                    BlockMessage = string.Empty,
                }),
            };
        }

        private static string[] Codes(IList<WorkListItemDto> rows)
        {
            var codes = new List<string>();
            foreach (WorkListItemDto row in rows) { codes.Add(row.StatusCode); }
            return codes.ToArray();
        }

        private static IList<WorkListItemDto> AllStatuses()
        {
            var rows = new List<WorkListItemDto>();
            long id = 1;
            foreach (string code in new[]
            {
                DbWorkStatus.Reserved, DbWorkStatus.Received,
                DbWorkStatus.CancelledReservation, DbWorkStatus.CancelledReception,
            })
            {
                rows.Add(new WorkListItemDto
                {
                    WorkId = id, PatientId = id, ReserveDate = Today, SlotCode = "AM",
                    StatusCode = code, Name = "홍길동", ChartNo = "C-000" + id,
                    Gender = "M", Birthday = "19800101", MobilePhone = "01012345678",
                });
                id++;
            }

            return rows;
        }

        private static IList<WorkListItemDto> OneRow()
        {
            return new List<WorkListItemDto>
            {
                new WorkListItemDto
                {
                    WorkId = 77,
                    PatientId = 5,
                    ReserveDate = Today,
                    SlotCode = "AM",
                    StatusCode = "RSV",
                    Name = "홍길동",
                    ChartNo = "C-0001",
                    Gender = "M",
                    Birthday = "19800101",
                    MobilePhone = "010-1234-5678",
                },
            };
        }

        /// <summary>05 §8.2 RS4 는 정확히 다섯 행이다. 여기 적은 것만 허용여부가 1 이다.</summary>
        private static IList<WorkActionDto> Allowed(params string[] codes)
        {
            string[] all =
            {
                DbWorkAction.EditReservation,
                DbWorkAction.CancelReservation,
                DbWorkAction.StartReception,
                DbWorkAction.EditExtra,
                DbWorkAction.CancelReception,
            };

            var rows = new List<WorkActionDto>();
            foreach (string code in all)
            {
                bool on = Array.IndexOf(codes, code) >= 0;
                rows.Add(new WorkActionDto
                {
                    ActionCode = code,
                    Allowed = on,
                    ReasonCode = on ? 0 : (int)DbCode.WrongStatus,
                    ReasonMessage = on ? string.Empty : "현재 상태에서는 수행할 수 없습니다.",
                });
            }

            return rows;
        }

        private static OperationResult<WorkDetailReadDto> OkDetail(IList<WorkActionDto> actions)
        {
            return OperationResult<WorkDetailReadDto>.Success(new WorkDetailReadDto
            {
                Detail = new WorkDetailDto
                {
                    WorkId = 77,
                    PatientId = 5,
                    ChartNo = "C-0001",
                    Name = "홍길동",
                    Birthday = "19800101",
                    Gender = "M",
                    MobilePhone = "010-1234-5678",
                    ReserveDate = Today,
                    SlotCode = "AM",
                    StatusCode = "RSV",
                    Capacity = 20,
                    CurrentCount = 12,
                    RemainingSeats = 8,
                    RowVersion = new byte[8],
                },
                NexItems = new List<WorkExamItemDto>
                {
                    new WorkExamItemDto { ExamItemCode = "E01", ExamItemName = "신장", NexType = "기본" },
                },
                AexItems = new List<WorkExamItemDto>
                {
                    new WorkExamItemDto { AexCode = "OPT01", ExamItemCode = "E11", ExamItemName = "간염" },
                },
                Actions = actions,
            });
        }
    }

    internal sealed class FakeWorkbenchView : IWorkbenchView
    {
        public event EventHandler SearchRequested;
        public event EventHandler<long?> SelectionChanged;

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string StatusCode { get; set; }
        public string ChartNo { get; set; }
        public string PatientName { get; set; }

        public string ContextTitle { get; set; }
        public IList<string> StatusChoices { get; set; }
        public DateTime? RangeFrom { get; private set; }
        public DateTime? RangeTo { get; private set; }
        public string ValidationMessage { get; set; }
        public IList<WorkListItemDto> Rows { get; set; }
        public WorkDetailDto Detail { get; set; }
        public IList<WorkExamItemDto> NexItems { get; set; }
        public IList<WorkExamItemDto> AexItems { get; set; }
        public WorkActionState Actions { get; set; }
        public string LastMessage { get; private set; }

        // 03 §9.1 WorkId Targeted Navigation
        public DateTime? FocusedDay { get; private set; }
        public long? SelectedWorkId { get; private set; }
        public bool SelectFound { get; set; }

        public void ResetSearchRange(DateTime from, DateTime? to)
        {
            RangeFrom = from;
            RangeTo = to;
            FromDate = from;
            ToDate = to;
        }

        public void FocusSearchOn(DateTime day)
        {
            FocusedDay = day;
            FromDate = day;
            ToDate = day;
            StatusCode = null;
            ChartNo = null;
            PatientName = null;
        }

        public bool SelectWork(long workId)
        {
            SelectedWorkId = workId;
            return SelectFound;
        }

        public void ShowMessage(string message) { LastMessage = message; }

        // 2026-09-11 — 03 §13 취소 확인. 시험은 답을 미리 정해 둔다.
        public bool ConfirmAnswer { get; set; }
        public IList<string> Questions { get { return _questions; } }
        private readonly IList<string> _questions = new List<string>();

        public bool Confirm(string message)
        {
            _questions.Add(message);
            return ConfirmAnswer;
        }

        public event EventHandler<string> ActionRequested;

        public void RaiseAction(string actionCode)
        {
            EventHandler<string> handler = ActionRequested;
            if (handler != null) { handler(this, actionCode); }
        }

        public void RaiseSearchRequested()
        {
            EventHandler handler = SearchRequested;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }

        public void RaiseSelectionChanged(long? workId)
        {
            EventHandler<long?> handler = SelectionChanged;
            if (handler != null) { handler(this, workId); }
        }
    }

    internal sealed class FakeWorkService : IWorkService
    {
        public OperationResult<IList<WorkListItemDto>> SearchResult { get; set; }
        public OperationResult<WorkDetailReadDto> DetailResult { get; set; }
        public Exception SearchFailure { get; set; }
        public WorkSearchRequest LastSearch { get; private set; }
        public IList<WorkSearchRequest> Searches { get { return _searches; } }
        private readonly IList<WorkSearchRequest> _searches = new List<WorkSearchRequest>();
        public long? LastDetailWorkId { get; private set; }
        public int DetailCalls { get; private set; }

        public OperationResult<IList<WorkListItemDto>> Search(WorkSearchRequest request)
        {
            if (SearchFailure != null)
            {
                throw SearchFailure;
            }

            LastSearch = request;
            _searches.Add(request);
            return SearchResult ?? OperationResult<IList<WorkListItemDto>>.Success(new List<WorkListItemDto>());
        }

        public OperationResult<WorkSaveReadDto> SaveResult { get; set; }
        public WorkActionRequest LastReception { get; private set; }
        public WorkActionRequest LastCancel { get; private set; }

        public OperationResult<WorkSaveReadDto> CompleteReception(WorkActionRequest request)
        {
            LastReception = request;
            return SaveResult;
        }

        public OperationResult<WorkSaveReadDto> CancelReception(WorkActionRequest request)
        {
            LastCancel = request;
            return SaveResult;
        }

        public ExtraExamChangeRequest LastExtra { get; private set; }

        public OperationResult<WorkSaveReadDto> ChangeExtraExam(ExtraExamChangeRequest request)
        {
            LastExtra = request;
            return SaveResult;
        }

        public Exception DetailFailure { get; set; }

        public OperationResult<WorkDetailReadDto> GetDetail(long workId)
        {
            if (DetailFailure != null) { throw DetailFailure; }

            LastDetailWorkId = workId;
            DetailCalls++;
            return DetailResult;
        }
    }
}
