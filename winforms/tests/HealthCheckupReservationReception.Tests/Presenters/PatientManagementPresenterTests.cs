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
        // 대상: PatientManagementPresenter (WF-PAT-01) — 조건 없는 조회
        // 목적: R16 이 「최소 1개 조건」을 걷었다 (2026-09-10 사용자 결정). 빈 Grid 가 「무엇을
        //       검색해야 하는지 모르겠다」로 읽혔기 때문이다. 화면이 조건 없음을 막으면 그 결정이
        //       되돌아간다.
        // 확인: 조건 없이 조회해도 SP 가 불리고, 넘어간 차트번호·이름·주민번호가 모두 null 이며,
        //       안내창이 뜨지 않는다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — 진입 시 자동 조회
        // 목적: R16 — 화면을 열면 조건 없이 한 번 조회한다. 실패는 알리지 않는다: 조작자가
        //       부탁하지 않은 호출이 창을 열자마자 오류창을 띄우면 안 된다.
        // 확인: 진입만으로 SP 가 불리고 안내 메시지가 없다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — 진입 조회가 실패한 경우
        // 목적: 화면이 열리자마자 모달이 뜨면 조작자는 아무것도 하기 전에 창부터 닫아야 한다.
        //       초기 조회는 조작자가 시킨 일이 아니므로 Inline 으로 알린다 — 조작자가 직접 누른
        //       [조회] 의 실패와는 다루는 방식이 다르다.
        // 확인: 진입 실패에는 모달 메시지가 없고, 조작자가 [조회] 를 눌러 실패하면 메시지가 선다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — 공백만 든 조회조건
        // 목적: 03 §5.3 은 빈 문자열을 미입력으로 처리한다. R16 이후로는 미입력이어도 막지 않고,
        //       공백을 NULL 로 만드는 정규화는 Service 가 한다 (05 §2.2) — Presenter 는 화면 값을
        //       그대로 넘긴다. 여기서 보는 것은 「막지 않는다」 하나다.
        // 확인: 공백뿐인 조건으로도 SP 가 불리고 안내가 없다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — 조회조건 전달
        // 목적: 05 §7.2 에서 조회 계약은 SP 가 갖는다. 화면이 조건을 고르거나 지우면 DB 가 받는
        //       그림이 화면과 달라지고 결과가 왜 그런지 설명할 수 없게 된다. 걷어 낸 생년월일·
        //       휴대전화가 null 로 가는 것도 같은 규칙이다.
        // 확인: 차트번호·이름·주민번호가 화면 값 그대로 가고, 생년월일·휴대전화는 null 이다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — 재조회 시 선택·상세 초기화
        // 목적: 03 §5.3·§5.5 — 목록이 바뀌었는데 앞의 선택과 상세가 남으면, 새 목록에 없는
        //       사람의 상세를 보며 [정보수정]·[예약] 을 누르게 된다.
        // 확인: 행을 골라 상세가 선 상태에서 다시 조회하면 상세가 null 이 되고 행 선택이 풀린다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — 조회 결과가 0건인 경우
        // 목적: 05 §3.4 에서 조회 0건은 성공이다. 안내창을 띄우면 조작자는 조회가 실패한 줄 알고
        //       같은 조건으로 다시 누른다.
        // 확인: 목록이 0건이고 안내 메시지가 없다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — 조회가 실패로 온 경우
        // 목적: 실패에 목록을 비우면 조작자는 방금 보던 결과를 잃는다. 실패는 「새 결과가 없다」
        //       이지 「앞 결과가 틀렸다」가 아니다.
        // 확인: DB 가 준 사유가 메시지로 서고 목록은 건드리지 않는다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — Service 에서 예외가 올라온 경우
        // 목적: 킷 §6 — provider 메시지는 DB 이름·서버명을 드러낸다. 목록 화면은 늘 열려 있는
        //       자리라 그대로 실으면 화면 캡처마다 서버명이 따라 나간다.
        // 확인: 예외에 든 서버명(SQLDEV01)이 화면 문구에 없다.
        [TestMethod]
        public void 예외가_나도_예외_본문을_화면에_싣지_않는다()
        {
            var view = new FakePatientManagementView { ChartNo = "2026-000123" };
            var service = new FakePatientService { Failure = new InvalidOperationException("서버 SQLDEV01 에 붙지 못했습니다") };
            Presenter(view, service);

            view.RaiseSearchRequested();

            Assert.IsFalse(view.LastMessage.Contains("SQLDEV01"), "예외 본문이 화면에 실렸다: " + view.LastMessage);
        }

        // 대상: PatientManagementPresenter (WF-PAT-01) — 행 선택 시 우측 상세 구성
        // 목적: R21 로 SELECT_수검자상세(SP-PAT-02)가 사라지고 그 칸들이 목록 RS1 안으로 들어왔다
        //       (2026-09-14 사용자 지시). 행을 고를 때마다 SP 를 다시 부르면 이미 손에 있는 값을
        //       한 번 더 읽는 것이고, 그동안 화면이 멈춘다.
        // 확인: 상세 SP 가 0회 불리고, 목록 RS1 이 실어 온 이름·주민번호가 상세에 그대로 서며,
        //       행 Action 이 열린다.
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
        // 대상: PatientManagementPresenter (WF-PAT-01) — 선택 해제 시의 상세·Action
        // 목적: 03 §5.5 — 상세와 Action 은 고른 행의 것이다. 선택이 풀렸는데 남아 있으면 직전
        //       행에 대고 수정·예약이 나간다.
        // 확인: 선택을 풀면 상세가 null 이고 행 Action 이 닫힌다.
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
        // 대상: PatientManagementPresenter (WF-PAT-01) — 받아 둔 목록에 없는 키가 올라온 경우
        // 목적: R21 이후 상세를 메워 줄 SP 가 없으므로, 받아 둔 행에 없는 키가 오면 지어내지
        //       않는다. 빈 값으로 상세를 세우면 조작자는 그것을 그 사람의 정보로 읽는다.
        // 확인: 상세와 이력이 모두 null 이고 행 Action 이 닫힌다.
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
        // 대상: PatientManagementPresenter (WF-PAT-01) — 쓰기 완료 뒤의 목록 갱신 경로 (R23)
        // 목적: 2026-09-14 사용자 지시로 쓰기가 끝나면 다시 읽고 그 줄로 돌아간다. 되읽지 않으면
        //       방금 고친 이름이 목록에 옛 값으로 남고, 그 행으로 [정보수정] 을 다시 누르면 낡은
        //       행버전이 올라가 601 이 난다. 되읽기는 조작자가 부탁한 조회가 아니므로 조용해야 한다.
        // 확인: 목록을 다시 읽고 선택이 고쳤던 수검자ID 11 로 돌아가며, 안내 메시지가 없다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — [조회] 한 번에 나가는 SP 개수 (R21)
        // 목적: 2026-09-14 사용자 지시 — [조회] 한 번이 SP 하나다. 예전에는 넷이 나갔다: 목록 ·
        //       오늘날짜 · 유효업무 두 번. 이제 목록 RS1 이 유효업무 네 칸을 싣고 오므로 나머지가
        //       필요 없다. 핵심은 RP-06 판정이 화면에서 사라진 것이다 — 예전에는 화면이
        //       「예약일 >= 오늘」을 다시 재서 이어 붙였다.
        // 확인: 목록을 채우는 동안 업무 SP 도 상세 SP 도 0회다.
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
        // 대상: PatientManagementPresenter (WF-PAT-01) — 지난 노쇼 예약만 있는 수검자
        // 목적: 지난 예약을 접수도 검사도 하지 않아 RSV 로 남은 노쇼 건이 있다. 그 사람이 오늘
        //       다시 예약할 수 있어야 한다 (2026-09-11 사용자 지시). 판정은 SP 가 낸다 — 목록 RS1
        //       의 유효업무 조인이 「예약일 >= DB 현재일」이라 (00 RP-06) 지난 건은 애초에 실리지
        //       않는다. 재는 것은 화면이 그것을 따르는가다.
        // 확인: 목록의 예약 상태가 「가능」이고 [예약] 버튼이 열려 있다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — 선택 행의 예약·접수 이력 조회
        // 목적: SP-WRK-01 에는 @수검자ID 가 없다 (05 §8.1 Parameter 다섯). 차트번호가 고유하므로
        //       (04 §8.1.5 UQ_수검자_CHART_NO) 정확검색으로 그 사람의 전 업무를 받는다. 기간을
        //       걸면 지난 이력이 잘리고, 상태를 걸면 취소 이력이 빠진다.
        // 확인: 차트번호로 묻고 시작일·종료일·상태코드가 모두 null 이며, 이력 1건이 실린다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — 선택 해제 시의 이력
        // 목적: 이력은 그 수검자의 것이다. 선택이 풀린 뒤에도 남으면 다음에 고른 사람의 이력으로
        //       읽힌다.
        // 확인: 선택을 풀면 이력이 null 이다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — 이력 조회만 실패한 경우
        // 목적: 이력을 못 읽었다고 상세까지 죽이면 행 하나 고를 때마다 화면이 비고, 모달을
        //       띄우면 행을 고를 때마다 창이 뜬다. 이력은 부가 정보이고 상세는 이미 손에 있다.
        // 확인: 상세는 서 있고 이력만 null 이며 안내 메시지가 없다.
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

        // 대상: PatientManagementPresenter (WF-PAT-01) — 목록의 예약 가능 여부 문구
        // 목적: 00 RP-06 에서 유효업무가 있으면 불가, 없으면 가능이다. R21 로 그 판정은 SP 가
        //       냈고 화면은 문구만 만든다 — 화면이 다시 판정하면 두 곳이 된다.
        // 확인: 유효업무가 실려 온 행의 상태가 「불가」이고, 상세 문구에 그 예약일(2026-09-14)이
        //       들어 있다.
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
        // 대상: PatientManagementPresenter (WF-PAT-01) — 유효업무 칸이 빈 행의 문구
        // 목적: 위와 한 쌍이다. 빈 칸을 「불가」로 읽거나 문구를 비워 두면 조작자가 예약할 수
        //       있는 사람을 못 찾는다.
        // 확인: 상태가 「가능」이고 상세 문구가 「예약 가능」이다.
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
        // 대상: PatientManagementPresenter (WF-PAT-01) — 여섯째 조회조건의 화면 거르기
        // 목적: 이 조건은 SP 가 모르는 값이라 화면이 거른다 (2026-09-11 grilling 이 더한 조건).
        //       거르지 않으면 조작자가 켜 놓고도 예약 불가인 사람을 계속 본다.
        // 확인: 조건을 켜면 예약 불가인 행이 목록에서 빠져 0건이 된다.
        [TestMethod]
        public void 예약_없는_수검자만_을_켜면_불가인_행이_빠진다()
        {
            var view = new FakePatientManagementView { ReservableOnly = true };
            var service = new FakePatientService { SearchResult = Rows(Booked(Today, "AM")) };
            new PatientManagementPresenter(view, service, new FakeWorkService());

            view.RaiseSearchRequested();

            Assert.AreEqual(0, view.Rows.Count, "불가인 행이 남았다");
        }
        // 대상: PatientManagementPresenter (WF-PAT-01) — 예약 불가 행의 [예약] Action
        // 목적: 03 §5.2 에 축이 하나 더 있다 — 이미 예약이 있는 수검자는 [예약] 이 닫힌다
        //       (2026-09-11 사용자 지시). 목록이 「불가」라고 적어 두고 버튼을 열어 두면 화면이
        //       스스로 모순되고, 눌러 봐야 모달이 그 사실을 다시 말한다.
        // 확인: 행은 선택되어 있고 [예약] 만 닫혀 있다.
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
        // 대상: PatientManagementPresenter (WF-PAT-01) — 상세의 예약 상태 문구
        // 목적: 목록의 두 값(가능/불가)은 날짜와 시간대를 말하지 못한다. 조작자가 「언제 예약이
        //       있는가」를 알아야 그 건을 찾아갈지 새로 잡을지 판단한다.
        // 확인: 상세 문구가 「예약 불가」로 시작하고 시간대(오후)가 들어 있다.
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
