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
            var presenter = new WorkbenchPresenter(view, service);

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
            new WorkbenchPresenter(view, service);

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
            new WorkbenchPresenter(view, service);

            view.RaiseSearchRequested();

            Assert.AreEqual("기간·차트번호·이름 중 하나 이상을 입력하세요.", view.ValidationMessage);
            Assert.IsNull(service.LastSearch, "SP 를 불렀다");
        }

        [TestMethod]
        public void 차트번호만_있어도_조건이_된다()
        {
            var view = new FakeWorkbenchView { ChartNo = "C-0001" };
            var service = new FakeWorkService { SearchResult = Ok(new List<WorkListItemDto>()) };
            new WorkbenchPresenter(view, service);

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
            new WorkbenchPresenter(view, service);
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
            new WorkbenchPresenter(view, service);

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
            new WorkbenchPresenter(view, service);

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
            new WorkbenchPresenter(view, service);

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
            new WorkbenchPresenter(view, service);
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
            new WorkbenchPresenter(view, service);

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
            new WorkbenchPresenter(view, service);

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
            var presenter = new WorkbenchPresenter(view, service);

            presenter.LoadInitial();

            Assert.AreEqual("읽지 못했습니다.", view.ValidationMessage, "실패가 보이지 않는다");
            Assert.IsNull(view.LastMessage, "창을 열자마자 모달이 떴다");
        }

        // ── 03 §9.1 Context

        [TestMethod]
        public void 기본_Context_는_예약_관리다()
        {
            var view = new FakeWorkbenchView();
            new WorkbenchPresenter(view, new FakeWorkService());

            Assert.AreEqual("예약 관리", view.ContextTitle);
        }

        // 03 §9.1 — Context 를 바꾸면 기존 SelectedRow 를 해제하고 우측 Detail 을 Clear 한다.
        // 같은 행이 다른 Context 에서 다른 Action 을 갖기 때문이다 (§9.6 · §9.7).
        [TestMethod]
        public void Context_를_바꾸면_선택을_승계하지_않는다()
        {
            var view = new FakeWorkbenchView();
            var service = new FakeWorkService { DetailResult = OkDetail(Allowed(DbWorkAction.EditReservation)) };
            var presenter = new WorkbenchPresenter(view, service);
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
            var presenter = new WorkbenchPresenter(view, service);

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
            var presenter = new WorkbenchPresenter(view, service);

            presenter.OpenContext(WorkContext.Reception, 77);

            Assert.AreEqual("방금 저장한 업무를 목록에서 찾지 못했습니다.", view.ValidationMessage);
        }

        [TestMethod]
        public void WorkId_가_없으면_조회조건을_건드리지_않는다()
        {
            var view = new FakeWorkbenchView { FromDate = Today.AddDays(-30) };
            var service = new FakeWorkService { DetailResult = OkDetail(Allowed()) };
            var presenter = new WorkbenchPresenter(view, service);

            presenter.OpenContext(WorkContext.Reception, null);

            Assert.IsNull(view.FocusedDay);
            Assert.AreEqual(Today.AddDays(-30), view.FromDate, "상단 전환이 조회조건을 갈아치웠다");
        }

        // ── helpers

        private static OperationResult<IList<WorkListItemDto>> Ok(IList<WorkListItemDto> rows)
        {
            return OperationResult<IList<WorkListItemDto>>.Success(rows);
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
        public long? LastDetailWorkId { get; private set; }
        public int DetailCalls { get; private set; }

        public OperationResult<IList<WorkListItemDto>> Search(WorkSearchRequest request)
        {
            if (SearchFailure != null)
            {
                throw SearchFailure;
            }

            LastSearch = request;
            return SearchResult ?? OperationResult<IList<WorkListItemDto>>.Success(new List<WorkListItemDto>());
        }

        public OperationResult<WorkDetailReadDto> GetDetail(long workId)
        {
            LastDetailWorkId = workId;
            DetailCalls++;
            return DetailResult;
        }
    }
}
