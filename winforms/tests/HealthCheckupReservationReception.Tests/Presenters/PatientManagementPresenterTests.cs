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
            service.SearchResult = OperationResult<IList<PatientDto>>.Success(
                new List<PatientDto>());
            Presenter(view, service);

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
            service.SearchResult = OperationResult<IList<PatientDto>>.Success(
                new List<PatientDto>());
            var presenter = Presenter(view, service);

            presenter.LoadInitial();

            Assert.IsNotNull(service.LastRequest);
            Assert.IsNull(view.LastMessage);
        }

        [TestMethod]
        public void 초기_조회가_실패해도_오류창을_띄우지_않는다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { Failure = new InvalidOperationException("DB") };
            var presenter = Presenter(view, service);

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
            service.SearchResult = OperationResult<IList<PatientDto>>.Success(
                new List<PatientDto>());
            Presenter(view, service);

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
            Presenter(view, service);

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
            var service = new FakePatientService { SearchResult = Rows() };
            Presenter(view, service);

            view.RaiseSearchRequested();
            view.RaiseSelectionChanged(11);
            Assert.IsNotNull(view.Detail, "행을 골랐는데 상세가 비어 있다");

            view.RaiseSearchRequested();

            Assert.AreEqual(1, view.Rows.Count);
            Assert.IsNull(view.Detail);
            Assert.IsFalse(view.RowActions.RowSelected);
        }

        // 조회 0건은 성공이다 (05 §3.4). 안내창을 띄우지 않는다.
        [TestMethod]
        public void 조회_0건은_빈_목록이지_실패가_아니다()
        {
            var view = new FakePatientManagementView { Name = "없는이름" };
            var service = new FakePatientService
            {
                SearchResult = OperationResult<IList<PatientDto>>.Success(new List<PatientDto>()),
            };
            Presenter(view, service);

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
                SearchResult = OperationResult<IList<PatientDto>>.Failure("차트번호는 100자를 넘을 수 없습니다."),
            };
            Presenter(view, service);

            view.RaiseSearchRequested();

            Assert.AreEqual("차트번호는 100자를 넘을 수 없습니다.", view.LastMessage);
            Assert.IsNull(view.Rows);
        }

        // 킷 §6 — provider 메시지는 DB·머신 정보를 드러낸다. 본문을 화면에 싣지 않는다.
        [TestMethod]
        public void 예외가_나도_예외_본문을_화면에_싣지_않는다()
        {
            var view = new FakePatientManagementView { ChartNo = "2026-000123" };
            var service = new FakePatientService { Failure = new InvalidOperationException("서버 SQLDEV01 에 붙지 못했습니다") };
            Presenter(view, service);

            view.RaiseSearchRequested();

            Assert.IsFalse(view.LastMessage.Contains("SQLDEV01"), "예외 본문이 화면에 실렸다: " + view.LastMessage);
        }

        /// <summary>
        /// 03 §5.5 — 행을 고르면 우측 상세가 선다. §5.2 의 Action 도 그때 열린다.
        ///
        /// **[R21] 그 상세는 목록이 이미 받아 온 행이다** (2026-09-14 사용자 지시).
        /// `SELECT_수검자상세`(SP-PAT-02)가 사라졌고 그 칸들이 목록 RS1 안으로 들어왔다.
        /// </summary>
        [TestMethod]
        public void 행을_고르면_받아_둔_행이_그대로_상세가_된다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows() };
            Presenter(view, service);
            view.RaiseSearchRequested();

            view.RaiseSelectionChanged(11);

            Assert.AreEqual(0, service.DetailCalls, "받아 둔 행이 있는데 수검자를 다시 읽었다");
            Assert.AreEqual("홍길동", view.Detail.Name);
            Assert.AreEqual("6603122000019", view.Detail.SocialNumber, "목록 RS1 이 주민번호를 싣지 않았다");
            Assert.IsTrue(view.RowActions.RowSelected);
        }
        [TestMethod]
        public void 선택이_풀리면_상세와_Action_을_닫는다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows() };
            Presenter(view, service);
            view.RaiseSearchRequested();

            view.RaiseSelectionChanged(11);
            view.RaiseSelectionChanged(null);

            Assert.IsNull(view.Detail);
            Assert.IsFalse(view.RowActions.RowSelected);
        }
        /// <summary>
        /// [X] **모르는 키는 상세를 비운다.** [R21] 이후 상세를 메워 줄 SP 가 없으므로
        ///     받아 둔 행에 없는 키가 오면 지어내지 않는다.
        /// </summary>
        [TestMethod]
        public void 목록에_없는_행을_고르면_상세를_비운다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows() };
            Presenter(view, service);
            view.RaiseSearchRequested();

            view.RaiseSelectionChanged(99);

            Assert.IsNull(view.Detail);
            Assert.IsNull(view.History);
            Assert.IsFalse(view.RowActions.RowSelected);
        }
        /// <summary>
        /// [R23] **쓰기가 끝나면 다시 읽고 그 줄로 돌아간다** (2026-09-14 사용자 지시).
        /// `MainForm` 이 수검자 저장·예약 저장 뒤에 이 길로 들어온다.
        ///
        /// [X] 되읽지 않으면 방금 고친 이름이 목록에 옛 값으로 남고, 그 행으로 `[정보수정]` 을
        ///     다시 누르면 낡은 `행버전` 이 올라가 `601` 이 난다.
        /// </summary>
        [TestMethod]
        public void 저장_뒤_되읽으면_목록을_다시_읽고_그_줄로_돌아간다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows() };
            PatientManagementPresenter presenter = Presenter(view, service);

            presenter.Reload(11);

            Assert.AreEqual(1, view.Rows.Count, "목록을 다시 읽지 않았다");
            Assert.AreEqual(11L, view.SelectedPatientId, "고쳤던 줄로 돌아가지 않았다");
            Assert.IsNull(view.LastMessage, "되읽기는 조용해야 한다 — 사용자가 부탁한 조회가 아니다");
        }

        private static OperationResult<IList<PatientDto>> Rows()
        {
            return Rows(null);
        }

        /// <summary>
        /// [R21] 목록 SP(`SP-PAT-01`) RS1 한 행. 유효업무 네 칸이 여기 실려 온다 —
        /// `valid` 가 `null` 이면 **예약 가능**이고 그 판정은 SP 가 냈다 (00 RP-06).
        /// 상세 칸(주민번호·생년월일·성별)도 같은 행에 있다. 예전에는 `SP-PAT-02` 몫이었다.
        /// </summary>
        private static OperationResult<IList<PatientDto>> Rows(PatientValidWorkDto valid)
        {
            IList<PatientDto> rows = new List<PatientDto>
            {
                new PatientDto
                {
                    PatientId = 11,
                    ChartNo = "2026-000123",
                    Name = "홍길동",
                    SocialNumber = "6603122000019",
                    Birthday = "19660312",
                    Gender = "F",
                    ValidWork = valid,
                },
            };
            return OperationResult<IList<PatientDto>>.Success(rows);
        }

        /// <summary>유효업무 한 건 — SP 가 `예약일 >= DB 현재일` 인 것만 이 칸에 싣는다.</summary>
        private static PatientValidWorkDto Booked(DateTime day, string slot)
        {
            return new PatientValidWorkDto
            {
                WorkId = 91,
                ReserveDate = day,
                SlotCode = slot,
                StatusCode = DbWorkStatus.Reserved,
            };
        }
        /// <summary>
        /// 예약 조인을 재지 않는 시험용. 업무 목록이 비어 있으면 전원 `가능` 이고, 그것이
        /// 예약접수 0행인 DB 에서 실제로 참인 값이다.
        /// </summary>
        private static PatientManagementPresenter Presenter(
            FakePatientManagementView view, FakePatientService service)
        {
            return new PatientManagementPresenter(view, service, new FakeWorkService());
        }

        private static readonly DateTime Today = new DateTime(2026, 9, 11);

        /// <summary>
        /// **[R21] `[조회]` 한 번이 SP 하나다** (2026-09-14 사용자 지시). 예전에는 넷이 나갔다 —
        /// 목록(`SP-PAT-01`) · 오늘날짜(`SP-COM-01`) · 유효업무(`SP-WRK-01`) 두 번(오늘 RSV ·
        /// 오늘 RCP). 이제 목록 RS1 이 `유효업무*` 네 칸을 싣고 오므로 나머지가 필요 없다.
        ///
        /// [!] RP-06 판정이 화면에서 사라진 것이 핵심이다 — 예전에는 화면이 `예약일 >= 오늘`
        ///     을 **다시 재서** 이어 붙였다.
        /// </summary>
        [TestMethod]
        public void 조회는_수검자_목록_SP_하나만_부른다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows() };
            var works = new FakeWorkService();
            var presenter = new PatientManagementPresenter(view, service, works);

            presenter.LoadInitial();
            view.RaiseSearchRequested();
            view.RaiseSearchRequested();

            Assert.AreEqual(0, works.Searches.Count, "목록을 채우는 데 업무 SP 를 불렀다");
            Assert.AreEqual(0, service.DetailCalls, "목록을 채우는 데 상세 SP 를 불렀다");
        }
        /// <summary>
        /// 노쇼 — 지난 예약을 접수도 검사도 하지 않아 `RSV` 인 채로 남았다. 그 사람이 오늘
        /// 다시 예약할 수 있어야 한다 (2026-09-11 사용자 지시).
        ///
        /// **[R21] 그 판정은 SP 가 낸다.** 목록 RS1 의 유효업무 조인이 `예약일 >= DB 현재일`
        /// 이라 (00 RP-06 *"과거 업무는 중복판단에서 제외한다"*) 지난 노쇼 건은 애초에 실리지
        /// 않는다. 화면이 받는 그림은 `유효업무 없음` 이고, 재는 것은 **화면이 그것을
        /// 따르는가**다 — 목록은 `가능` 이라 적으면서 버튼을 닫아 두면 예약할 길이 없다.
        /// </summary>
        [TestMethod]
        public void 유효업무가_없으면_예약_버튼을_닫지_않는다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows() };
            new PatientManagementPresenter(view, service, new FakeWorkService());
            view.RaiseSearchRequested();

            view.RaiseSelectionChanged(11);

            Assert.AreEqual("가능", view.Rows[0].ReserveStatus);
            Assert.IsTrue(view.RowActions.Reserve, "지난 노쇼 하나로 예약이 막혔다");
        }
        // ── 2026-09-11: 상세의 `예약·접수 이력`

        /// <summary>
        /// `SP-WRK-01` 에 `@수검자ID` 가 없다 (05 §8.1 Parameter 다섯). 차트번호가 고유하므로
        /// (04 §8.1.5 `UQ_수검자_CHART_NO`) **정확검색**으로 그 사람의 전 업무를 받는다 —
        /// 날짜도 상태도 걸지 않는다.
        /// </summary>
        [TestMethod]
        public void 행을_고르면_차트번호로_전_업무를_읽어_이력에_싣는다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows() };
            var works = new FakeWorkService
            {
                SearchResult = OperationResult<IList<WorkListItemDto>>.Success(new List<WorkListItemDto>
                {
                    new WorkListItemDto
                    {
                        WorkId = 91, PatientId = 11, ReserveDate = Today.AddDays(-200),
                        SlotCode = "PM", StatusCode = DbWorkStatus.CancelledReservation,
                    },
                }),
            };
            new PatientManagementPresenter(view, service, works);
            view.RaiseSearchRequested();

            view.RaiseSelectionChanged(11);

            WorkSearchRequest history = works.Searches[works.Searches.Count - 1];
            Assert.AreEqual("2026-000123", history.ChartNo, "차트번호로 묻지 않았다");
            Assert.IsNull(history.FromDate, "기간을 걸면 지난 이력이 잘린다");
            Assert.IsNull(history.ToDate);
            Assert.IsNull(history.StatusCode, "상태를 걸면 취소 이력이 빠진다");
            Assert.AreEqual(1, view.History.Count);
        }

        [TestMethod]
        public void 선택이_풀리면_이력도_비운다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows() };
            var works = new FakeWorkService();
            new PatientManagementPresenter(view, service, works);
            view.RaiseSearchRequested();

            view.RaiseSelectionChanged(11);
            view.RaiseSelectionChanged(null);

            Assert.IsNull(view.History);
        }

        /// <summary>
        /// 이력을 못 읽으면 비운다 — 상세 조회는 이미 성공했으므로 모달을 띄우면 행을 고를
        /// 때마다 창이 뜬다.
        /// </summary>
        [TestMethod]
        public void 이력_조회가_실패해도_상세는_선다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows() };
            var works = new FakeWorkService { SearchFailure = new InvalidOperationException("끊겼다") };
            new PatientManagementPresenter(view, service, works);
            view.RaiseSearchRequested();

            view.RaiseSelectionChanged(11);

            Assert.IsNotNull(view.Detail, "상세까지 같이 죽었다");
            Assert.IsNull(view.History);
            Assert.IsNull(view.LastMessage, "행을 고를 때마다 창이 뜬다");
        }

        // ── 2026-09-11 grilling: 목록에 「예약 가능/불가」 를 이어 붙인다

        /// <summary>
        /// `00` RP-06 — 유효업무가 있으면 `불가`, 없으면 `가능` 이다.
        /// [R21] 그 판정은 SP 가 냈고 화면은 **문구만 만든다**.
        /// </summary>
        [TestMethod]
        public void 유효업무가_실려_오면_불가로_적는다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows(Booked(Today.AddDays(3), "AM")) };
            new PatientManagementPresenter(view, service, new FakeWorkService());

            view.RaiseSearchRequested();

            Assert.AreEqual("불가", view.Rows[0].ReserveStatus);
            StringAssert.Contains(view.Rows[0].ReserveStatusDetail, "2026-09-14");
        }
        /// <summary>유효업무 칸이 비면 `가능` 이다. 문구까지 그렇게 적는다.</summary>
        [TestMethod]
        public void 유효업무가_없으면_가능으로_적는다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows() };
            new PatientManagementPresenter(view, service, new FakeWorkService());

            view.RaiseSearchRequested();

            Assert.AreEqual("가능", view.Rows[0].ReserveStatus);
            Assert.AreEqual("예약 가능", view.Rows[0].ReserveStatusDetail);
        }
        // 여섯째 조회조건 — SP 가 모르므로 화면이 거른다.
        [TestMethod]
        public void 예약_없는_수검자만_을_켜면_불가인_행이_빠진다()
        {
            var view = new FakePatientManagementView { ReservableOnly = true };
            var service = new FakePatientService { SearchResult = Rows(Booked(Today, "AM")) };
            new PatientManagementPresenter(view, service, new FakeWorkService());

            view.RaiseSearchRequested();

            Assert.AreEqual(0, view.Rows.Count, "불가인 행이 남았다");
        }
        /// <summary>
        /// 03 §5.2 에 축이 하나 더 있다 — **이미 예약이 있는 수검자는 `[예약]` 이 닫힌다**
        /// (2026-09-11 사용자 지시). 목록이 `불가` 라고 적어 두고 버튼을 열어 두면 화면이
        /// 스스로 모순되고, 눌러 봐야 모달이 그 사실을 다시 말한다.
        /// </summary>
        [TestMethod]
        public void 예약이_있는_행을_고르면_예약_버튼이_닫힌다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows(Booked(Today.AddDays(3), "AM")) };
            new PatientManagementPresenter(view, service, new FakeWorkService());
            view.RaiseSearchRequested();

            view.RaiseSelectionChanged(11);

            Assert.IsTrue(view.RowActions.RowSelected, "행은 잡혔다");
            Assert.IsFalse(view.RowActions.Reserve, "예약 불가인데 [예약] 이 열려 있다");
        }
        // 행을 고르면 상세가 날짜까지 말해 준다 — 목록의 두 값이 못 하는 일이다.
        [TestMethod]
        public void 행을_고르면_상세에_예약_일정이_선다()
        {
            var view = new FakePatientManagementView();
            var service = new FakePatientService { SearchResult = Rows(Booked(Today.AddDays(3), "PM")) };
            new PatientManagementPresenter(view, service, new FakeWorkService());
            view.RaiseSearchRequested();

            view.RaiseSelectionChanged(11);

            StringAssert.Contains(view.ReserveStatusText, "오후");
            StringAssert.StartsWith(view.ReserveStatusText, "예약 불가");
        }
    }

    internal sealed class FakePatientManagementView : IPatientManagementView
    {
        public event EventHandler SearchRequested;
        public event EventHandler<long?> SelectionChanged;

        public string ChartNo { get; set; }
        public string Name { get; set; }
        public string SocialNumber { get; set; }
        public string Birthday { get; set; }
        public string MobilePhone { get; set; }

        public bool ReservableOnly { get; set; }

        public IList<PatientDto> Rows { get; set; }
        public PatientDto Detail { get; set; }
        public string ReserveStatusText { get; set; }
        public string NoticeText { get; set; }
        public long? SelectedPatientId { get; private set; }

        public void SelectPatient(long patientId) { SelectedPatientId = patientId; }
        public PatientActionState RowActions { get; set; }
        public IList<WorkListItemDto> History { get; set; }
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
        public OperationResult<IList<PatientDto>> SearchResult { get; set; }
        public OperationResult<PatientDto> DetailResult { get; set; }
        public Exception Failure { get; set; }

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

        public OperationResult<IList<PatientDto>> Search(PatientSearchRequest request)
        {
            if (Failure != null) { throw Failure; }
            LastRequest = request;
            return SearchResult;
        }

        public string LastChartNo { get; private set; }

        public OperationResult<PatientDto> GetByChartNo(string chartNo)
        {
            if (Failure != null) { throw Failure; }
            LastChartNo = chartNo;
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

        private void Record(PatientSaveRequest request)
        {
            LastSaveRequest = request;
            SaveRequests.Add(request);
        }
    }
}
