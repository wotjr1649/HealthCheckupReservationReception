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
    public class PatientManagementPresenterTests
    {
        // 03 §5.3 — 최소 1개 조건이 있어야 조회한다. DB 도 103 으로 막지만(05 §7.2)
        // 화면에서 먼저 안내하고 SP 를 부르지 않는다.
        [TestMethod]
        public void 조회조건이_하나도_없으면_전체를_조회한다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService();
            service.SearchResult = OperationResult<IList<PatientListItemDto>>.Success(
                new List<PatientListItemDto>());
            new PatientManagementPresenter(view, service);

            view.RaiseSearchRequested();

            // [R16] 03 §5.3 — 막지 않는다. 조건이 전부 null 인 채로 SP 가 불린다.
            Assert.IsNotNull(service.LastRequest, "조건이 없다고 SP 를 안 불렀다");
            Assert.IsNull(service.LastRequest.ChartNo);
            Assert.IsNull(service.LastRequest.Name);
            Assert.IsNull(service.LastRequest.SocialNumber);
            Assert.IsNull(view.LastMessage, "안내창이 떴다");
        }

        // [R16] 화면을 열면 조건 없이 한 번 조회한다. 실패는 알리지 않는다 —
        // 사용자가 부탁하지 않은 호출이 창을 열자마자 오류창을 띄우면 안 된다.
        [TestMethod]
        public void 화면이_열리면_조건_없이_한_번_조회한다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService();
            service.SearchResult = OperationResult<IList<PatientListItemDto>>.Success(
                new List<PatientListItemDto>());
            var presenter = new PatientManagementPresenter(view, service);

            presenter.LoadInitial();

            Assert.IsNotNull(service.LastRequest);
            Assert.IsNull(view.LastMessage);
        }

        [TestMethod]
        public void 초기_조회가_실패해도_오류창을_띄우지_않는다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { Failure = new InvalidOperationException("DB") };
            var presenter = new PatientManagementPresenter(view, service);

            presenter.LoadInitial();

            Assert.IsNull(view.LastMessage, "초기 조회 실패가 모달을 띄웠다");

            // 사용자가 [조회] 를 누르면 그때는 이유를 본다.
            view.RaiseSearchRequested();
            Assert.IsNotNull(view.LastMessage);
        }

        // 공백만 넣은 것도 미입력이다 (03 §5.3 "빈 문자열은 미입력으로 처리한다").
        // [R16] 미입력이어도 이제 **막지 않는다**. 공백을 NULL 로 만드는 정규화는 Service 가
        //       한다 (05 §2.2 · PatientService.Search 의 Trim) — Presenter 는 화면 값을 그대로
        //       넘긴다. 여기서 보는 것은 "막지 않는다" 하나다.
        [TestMethod]
        public void 공백만_넣은_조건도_막지_않는다()
        {
            var view = new FakePatientManagementView { Name = "   " };
            var service = new FakePatientService();
            service.SearchResult = OperationResult<IList<PatientListItemDto>>.Success(
                new List<PatientListItemDto>());
            new PatientManagementPresenter(view, service);

            view.RaiseSearchRequested();

            Assert.IsNotNull(service.LastRequest, "공백뿐인 조건이라고 SP 를 안 불렀다");
            Assert.IsNull(view.LastMessage);
        }

        [TestMethod]
        public void 조회조건은_화면이_담은_그대로_넘어간다()
        {
            var view = new FakePatientManagementView
            {
                ChartNo = "2026-000123",
                Name = "홍",
                SocialNumber = "660312-2000019",
            };
            var service = new FakePatientService { SearchResult = Rows() };
            new PatientManagementPresenter(view, service);

            view.RaiseSearchRequested();

            // 정규화(`-` 제거)는 Service 의 일이다 — Presenter 가 미리 손대지 않는다.
            Assert.AreEqual("2026-000123", service.LastRequest.ChartNo);
            Assert.AreEqual("홍", service.LastRequest.Name);
            Assert.AreEqual("660312-2000019", service.LastRequest.SocialNumber);
            // 2026-09-10 사용자 결정 — 화면 조회조건이 셋으로 줄었다. SP 는 그 둘도 받지만
            // 채우는 곳이 없다 (05 §7.2).
            Assert.IsNull(service.LastRequest.Birthday);
            Assert.IsNull(service.LastRequest.MobilePhone);
        }

        // 03 §5.3 · §5.5 — 재조회 시 선택행과 우측 상세를 초기화한다.
        [TestMethod]
        public void 재조회하면_선택행과_상세를_초기화한다()
        {
            var view = new FakePatientManagementView { ChartNo = "2026-000123" };
            var service = new FakePatientService { SearchResult = Rows(), DetailResult = Detail() };
            new PatientManagementPresenter(view, service);

            view.RaiseSelectionChanged(11);
            Assert.IsNotNull(view.Detail, "행을 골랐는데 상세가 비어 있다");

            view.RaiseSearchRequested();

            Assert.AreEqual(1, view.Rows.Count);
            Assert.IsNull(view.Detail);
            Assert.IsFalse(view.RowSelected);
        }

        // 조회 0건은 성공이다 (05 §3.4). 안내창을 띄우지 않는다.
        [TestMethod]
        public void 조회_0건은_빈_목록이지_실패가_아니다()
        {
            var view = new FakePatientManagementView { Name = "없는이름" };
            var service = new FakePatientService
            {
                SearchResult = OperationResult<IList<PatientListItemDto>>.Success(new List<PatientListItemDto>()),
            };
            new PatientManagementPresenter(view, service);

            view.RaiseSearchRequested();

            Assert.AreEqual(0, view.Rows.Count);
            Assert.IsNull(view.LastMessage);
        }

        [TestMethod]
        public void 조회_실패는_메시지만_보이고_목록을_건드리지_않는다()
        {
            var view = new FakePatientManagementView { ChartNo = "2026-000123" };
            var service = new FakePatientService
            {
                SearchResult = OperationResult<IList<PatientListItemDto>>.Failure("차트번호는 100자 이하로 입력하십시오."),
            };
            new PatientManagementPresenter(view, service);

            view.RaiseSearchRequested();

            Assert.AreEqual("차트번호는 100자 이하로 입력하십시오.", view.LastMessage);
            Assert.IsNull(view.Rows);
        }

        // 킷 §6 — provider 메시지는 DB·머신 정보를 드러낸다. 본문을 화면에 싣지 않는다.
        [TestMethod]
        public void 예외가_나도_예외_본문을_화면에_싣지_않는다()
        {
            var view = new FakePatientManagementView { ChartNo = "2026-000123" };
            var service = new FakePatientService { Failure = new InvalidOperationException("서버 SQLDEV01 에 붙지 못했습니다") };
            new PatientManagementPresenter(view, service);

            view.RaiseSearchRequested();

            Assert.IsFalse(view.LastMessage.Contains("SQLDEV01"), "예외 본문이 화면에 실렸다: " + view.LastMessage);
        }

        // 03 §5.5 — 행 선택 즉시 우측 상세를 갱신한다. §5.2 의 Action 도 그 때 열린다.
        [TestMethod]
        public void 행을_고르면_상세를_읽고_Action_을_연다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { DetailResult = Detail() };
            new PatientManagementPresenter(view, service);

            view.RaiseSelectionChanged(11);

            Assert.AreEqual(11L, service.LastPatientId);
            Assert.AreEqual("홍길동", view.Detail.Name);
            Assert.IsTrue(view.RowSelected);
        }

        [TestMethod]
        public void 선택이_풀리면_상세와_Action_을_닫는다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { DetailResult = Detail() };
            new PatientManagementPresenter(view, service);

            view.RaiseSelectionChanged(11);
            view.RaiseSelectionChanged(null);

            Assert.IsNull(view.Detail);
            Assert.IsFalse(view.RowSelected);
        }

        [TestMethod]
        public void 상세_조회가_실패하면_상세를_비운다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService
            {
                DetailResult = OperationResult<PatientDetailDto>.Failure("수검자 상세 결과가 비어 있습니다."),
            };
            new PatientManagementPresenter(view, service);

            view.RaiseSelectionChanged(11);

            Assert.IsNull(view.Detail);
            Assert.AreEqual("수검자 상세 결과가 비어 있습니다.", view.LastMessage);
        }

        private static OperationResult<IList<PatientListItemDto>> Rows()
        {
            IList<PatientListItemDto> rows = new List<PatientListItemDto>
            {
                new PatientListItemDto { PatientId = 11, ChartNo = "2026-000123", Name = "홍길동" },
            };
            return OperationResult<IList<PatientListItemDto>>.Success(rows);
        }

        private static OperationResult<PatientDetailDto> Detail()
        {
            return OperationResult<PatientDetailDto>.Success(new PatientDetailDto
            {
                PatientId = 11,
                ChartNo = "2026-000123",
                Name = "홍길동",
                SocialNumber = "6603122000019",
                Birthday = "19660312",
                Gender = "F",
            });
        }
    }

    internal sealed class FakePatientManagementView : IPatientManagementView
    {
        public event EventHandler SearchRequested;
        public event EventHandler<long?> SelectionChanged;

        public string ChartNo { get; set; }
        public string Name { get; set; }
        public string SocialNumber { get; set; }

        public IList<PatientListItemDto> Rows { get; set; }
        public PatientDetailDto Detail { get; set; }
        public bool RowSelected { get; set; }
        public string LastMessage { get; private set; }

        public void ShowMessage(string message) { LastMessage = message; }

        public void RaiseSearchRequested()
        {
            EventHandler handler = SearchRequested;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }

        public void RaiseSelectionChanged(long? patientId)
        {
            EventHandler<long?> handler = SelectionChanged;
            if (handler != null) { handler(this, patientId); }
        }
    }

    internal sealed class FakePatientService : IPatientService
    {
        public OperationResult<IList<PatientListItemDto>> SearchResult { get; set; }
        public OperationResult<PatientDetailDto> DetailResult { get; set; }
        public Exception Failure { get; set; }

        // SP-PAT-05 (05 §7.4). 기본은 "유효업무 없음" 이고 그것이 성공한 0행이다.
        public OperationResult<PatientValidWorkDto> ValidWorkResult { get; set; }
        public long? LastValidWorkPatientId { get; private set; }

        // DLG-PAT-01 은 한 번의 저장이 두 번 부를 수 있다 — 203 을 받고 확인값을 실어 다시
        // 부르는 길이다 (03 §6.5). 그래서 결과를 하나가 아니라 줄로 세워 둔다.
        public Queue<OperationResult<PatientSaveReadDto>> RegisterResults { get; private set; }

        public OperationResult<PatientSaveReadDto> UpdateResult { get; set; }

        public PatientSearchRequest LastRequest { get; private set; }
        public long? LastPatientId { get; private set; }
        public PatientSaveRequest LastSaveRequest { get; private set; }
        public IList<PatientSaveRequest> SaveRequests { get; private set; }
        public int DetailCalls { get; private set; }

        public FakePatientService()
        {
            RegisterResults = new Queue<OperationResult<PatientSaveReadDto>>();
            SaveRequests = new List<PatientSaveRequest>();
        }

        public OperationResult<IList<PatientListItemDto>> Search(PatientSearchRequest request)
        {
            if (Failure != null) { throw Failure; }
            LastRequest = request;
            return SearchResult;
        }

        public OperationResult<PatientDetailDto> GetDetail(long patientId)
        {
            if (Failure != null) { throw Failure; }
            LastPatientId = patientId;
            DetailCalls++;
            return DetailResult;
        }

        public OperationResult<PatientSaveReadDto> Register(PatientSaveRequest request)
        {
            if (Failure != null) { throw Failure; }
            Record(request);
            return RegisterResults.Count > 0 ? RegisterResults.Dequeue() : null;
        }

        public OperationResult<PatientSaveReadDto> Update(PatientSaveRequest request)
        {
            if (Failure != null) { throw Failure; }
            Record(request);
            return UpdateResult;
        }

        public OperationResult<PatientValidWorkDto> GetValidWork(long patientId)
        {
            if (Failure != null) { throw Failure; }
            LastValidWorkPatientId = patientId;
            return ValidWorkResult ?? OperationResult<PatientValidWorkDto>.Success(null);
        }

        private void Record(PatientSaveRequest request)
        {
            LastSaveRequest = request;
            SaveRequests.Add(request);
        }
    }
}
