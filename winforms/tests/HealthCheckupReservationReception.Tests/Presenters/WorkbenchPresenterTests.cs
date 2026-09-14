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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 진입 시 기본 기간으로 하는 초기 조회
        // 목적: 05 §8.1 에서 조회조건은 화면이 담아 SP 로 넘긴다. 화면이 여는 순간 스스로 한 번
        //       조회하는 것이 이 화면의 기본 동작이다 — 빈 Grid 는 조작자에게 「무엇을 검색해야
        //       하는지 모르겠다」로 읽힌다.
        // 확인: 기본 기간(오늘~오늘)으로 SP 가 불리고 목록 1행이 실리며 안내가 없다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 뒤집힌 조회기간
        // 목적: 03 §9.3 은 From>To 를 Inline 오류로 정했다. 왕복하면 DB 가 0건을 성공으로 주고
        //       조작자는 「예약이 없다」로 읽는다. 모달로 내면 조회할 때마다 창을 닫아야 한다.
        // 확인: 안내가 「시작일이 종료일보다 늦습니다.」로 Inline 에 서고 SP 가 불리지 않으며,
        //       모달 메시지는 뜨지 않는다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 상태 「전체」만 고른 상태의 조회
        // 목적: 03 §9.3 은 최소 하나의 실질 조건을 요구한다. 상태 「전체」는 조건이 아니므로 그
        //       상태로 조회하면 전 기간 전건을 긁는다.
        // 확인: 안내가 「기간·차트번호·이름 중 하나 이상을 입력하세요.」이고 SP 가 불리지 않는다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 기간 없이 차트번호만 넣은 조회
        // 목적: 05 §8.1 은 조건 조합에 최소 개수를 두지 않는다. 화면이 「기간도 넣으십시오」로
        //       막으면 계약이 허락한 조회를 화면이 좁히는 것이 되고, 그 사람의 전 이력을 볼 길이
        //       사라진다.
        // 확인: 차트번호만으로 SP 가 불리고 그 값이 그대로 전달되며 안내가 없다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 재조회 시 선택·상세·Action 초기화
        // 목적: 03 §9.4 — 목록이 바뀌었는데 앞의 상세와 Action 이 남으면, 새 목록에 없는 업무에
        //       대고 취소·접수가 나간다.
        // 확인: 상세가 서 있던 상태에서 재조회하면 상세가 null 이 되고 검사구성 두 목록이 0건이
        //       되며 업무 Action 과 [변경이력] 이 닫힌다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 행 선택 시 상세·검사구성·Ribbon Action 구성
        // 목적: 05 §8.2 RS4 · 07 §3.6.1 에서 Ribbon 활성화의 출처는 RS4 가능한업무 다. 화면이
        //       상태코드를 보고 다시 판정하면 DB 가 막은 것을 화면이 열어 버리고, 조작자는 눌러
        //       봐야 막힌 것을 안다.
        // 확인: 그 업무ID 로 상세를 읽고 차트번호·국가검사 1건·추가검사 1건이 서며, RS4 가 허용한
        //       예약변경·예약취소만 열리고 접수·추가검사·접수취소는 닫힌다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 취소된 업무를 고른 경우의 [변경이력]
        // 목적: 03 §9.6·§23.4 에서 [변경이력] 은 상태와 무관하게 행이 선택되면 열린다. 취소된
        //       업무는 다섯 동작이 전부 닫히지만 변경 내역은 남아 있고, 그것을 보는 것이 바로
        //       이 상황에서 조작자가 하려는 일이다.
        // 확인: 다섯 업무 Action 이 모두 닫혀도 [변경이력] 은 열린다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — RS4 허용여부의 무가공 반영
        // 목적: 05 §8.2 에서 상태·마감·공통 업무조건의 조합은 이미 DB 에서 판정된 것이다. 화면이
        //       다시 계산하면 판정이 두 곳이 되고, 계약이 바뀔 때마다 둘을 함께 고쳐야 한다.
        // 확인: DB 가 접수·추가검사·접수취소를 허용으로 주면 셋이 열리고, 불허인 예약변경은
        //       닫힌다 — 화면이 상태코드로 되재지 않는다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 선택 해제 시의 상세·Action
        // 목적: 고른 행이 없는데 Action 이 열려 있으면 직전 행에 대고 SP 가 나간다. 선택 해제는
        //       조회할 일이 아니므로 상세를 다시 읽지도 않아야 한다.
        // 확인: 상세가 null 이고 [변경이력] 이 닫히며, 상세 조회 횟수가 늘지 않는다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 상세 조회가 실패한 경우
        // 목적: RS4 를 못 읽었는데 Action 을 남겨 두면 무엇이 허용되는지 모르는 채 버튼이 열린다.
        //       모르는 것을 「가능」으로 기본값 삼지 않는다.
        // 확인: 상세가 null 이고 Action 이 닫히며 DB 가 준 사유가 전해진다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 목록 조회에서 예외가 올라온 경우
        // 목적: 킷 §6 — provider 메시지는 DB·머신 정보를 드러낸다. 목록 화면은 늘 열려 있는
        //       자리라 그대로 실으면 화면 캡처마다 서버명이 따라 나간다.
        // 확인: 안내가 화면용 문장이고 예외에 든 서버명(DESKTOP…)이 문구에 없다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 조회 실패의 표시 방식
        // 목적: 모달이면 창을 열자마자 뜨는 것을 막느라 조용히 삼켜야 하고, 그러면 실패가 아예
        //       보이지 않는다 — 2026-09-10 에 성공한 0건과 구별이 안 돼 「조회가 안 된다」로
        //       보고된 자리다.
        // 확인: 실패 사유가 Inline 안내에 서고 모달 메시지는 뜨지 않는다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 기본 Context 와 제목
        // 목적: 03 §9.1 에서 두 창구가 한 화면을 쓰므로 어느 쪽으로 열렸는지가 제목에 남아야
        //       한다. 안 남으면 조작자가 지금 어느 창구에서 일하는지 모른다.
        // 확인: 제목이 「예약 관리」다.
        [TestMethod]
        public void 기본_Context_는_예약_관리다()
        {
            var view = new FakeWorkbenchView();
            new WorkbenchPresenter(view, new FakeWorkService(), new FakeReservationService(), Status(Today), "접수1번창구");

            Assert.AreEqual("예약 관리", view.ContextTitle);
        }

        // 대상: WorkbenchPresenter (WF-WRK-01) — Context 전환 시 선택·상세 초기화
        // 목적: 03 §9.1 — 같은 행이 다른 Context 에서 다른 Action 을 갖는다 (§9.6·§9.7).
        //       선택을 승계하면 예약 창구에서 고른 행에 접수 Action 이 붙는 순간이 생긴다.
        // 확인: 제목이 「접수 관리」로 바뀌고 상세가 null 이 되며 Action 이 모두 닫힌다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 저장 직후 한 건을 겨누는 진입
        // 목적: 신규예약 저장 성공(03 §8.11)과 접수 Shortcut 이 이 길로 돌아온다. 조회조건을
        //       그대로 두면 방금 저장한 건이 목록에 없을 수 있고, 상태 조건이 남으면 더 그렇다 —
        //       그 날 하루로 좁히고 상태를 푼다.
        // 확인: 그 업무의 예약일로 기간이 오늘~오늘로 좁혀지고 상태 조건이 null 이며,
        //       업무ID 77 이 선택되고 목록이 채워진다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 겨눈 업무가 목록에 없는 경우
        // 목적: 07 §3.6.2 의 저장 직후 겨누기다. 조회조건이 그 건을 담지 못하면 못 찾는데,
        //       조용히 넘기면 조작자는 저장이 안 된 줄로 읽는다. 오류창이 아니라 Inline 인 것도
        //       규칙이다 — 저장은 성공했으므로 오류가 아니다.
        // 확인: 안내가 「방금 저장한 업무를 목록에서 찾지 못했습니다.」로 Inline 에 선다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 겨눌 업무 없이 탭만 연 경우
        // 목적: 2026-09-11 에 뒤집힌 규칙이다. 예전에는 상단 전환이 조회조건을 건드리지 않아
        //       두 탭이 같은 목록을 보고 있었다. 이제 탭을 열면 그 창구의 기간으로 세우고 다시
        //       조회한다 — 겨누기(FocusedDay)는 쓰지 않는다. 그쪽은 저장 뒤 한 건을 겨누는 길이다.
        // 확인: 겨눈 날짜가 없고 기간이 그 창구 기본값으로 서며, 조회가 실제로 나간다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 예약취소(SP-WRK-03) 실행 경로
        // 목적: 03 §13.1 — 묻고, 부르고, 다시 읽는다. 문구의 핵심은 되돌릴 수 없다는 것이다.
        //       행버전을 안 실으면 남이 그 사이 바꾼 건을 덮어쓰며 취소한다.
        // 확인: 확인창이 1회 뜨고 문구에 「복원할 수 없습니다」가 있으며, 예약취소 동작으로
        //       업무ID 77 · 행버전 · 조작자명이 전달된다.
        [TestMethod]
        public void 예약취소는_묻고_행버전을_실어_보낸다()
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

            view.RaiseAction(DbWorkAction.CancelReservation);

            Assert.AreEqual(1, view.Questions.Count, "묻지 않았거나 두 번 물었다");
            StringAssert.Contains(view.Questions[0], "복원할 수 없습니다");

            // [R20] 취소가 SP 하나로 합쳐졌다 — **무엇을 취소하는지는 동작코드가 말한다.**
            Assert.AreEqual(DbWorkAction.CancelReservation, service.LastCancelAction);
            Assert.AreEqual(77L, service.LastCancel.WorkId);
            Assert.IsNotNull(service.LastCancel.RowVersion, "행버전을 안 실었다");
            Assert.AreEqual("접수1번창구", service.LastCancel.OperatorName);
        }

        // 대상: WorkbenchPresenter (WF-WRK-01) — 취소 확인에서 「아니오」를 고른 경우
        // 목적: 03 §13 CNF-RSV-01 에서 취소는 되돌릴 수 없다. 물어보기만 하고 부르는 구현에서도
        //       화면은 똑같아 보이므로, 「아니오」가 실제로 SP 를 막는지는 시험이 아니면 드러나지
        //       않는다 — 그 결함은 취소된 뒤에 알게 된다.
        // 확인: 확인창이 1회 뜨고 취소 SP 가 불리지 않는다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 접수취소(SP-RCP-03) 실행 경로
        // 목적: 05 §11.3·§12.3 에서 예약취소와 접수취소는 상태전이가 다르다 (CNR vs CNC).
        //       같은 버튼처럼 보이지만 부르는 SP 가 다르고, 잘못 부르면 상태가 한 칸 어긋난 채
        //       저장된다. 확인 문구도 그 차이를 말해야 한다.
        // 확인: 확인 문구에 「되돌아가지 않으며」가 들어 있고 접수 계열 SP 로 업무ID 77 이 간다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — Action 이 601 등으로 막힌 경우
        // 목적: 행버전 충돌이면 그 다시 읽기가 곧 복구다 — 사유만 적고 옛 값을 두면 다음 Action
        //       도 같은 이유로 막혀 조작자가 빠져나올 길이 없다.
        // 확인: 안내에 「최신 정보를 다시 조회」가 들어 있고 상세 조회가 한 번 더 불린다.
        [TestMethod]
        public void DB_가_막으면_사유를_적고_최신값을_다시_읽는다()
        {
            var view = new FakeWorkbenchView { ConfirmAnswer = true, SelectFound = true };
            // [R20] 취소가 WorkService 하나로 합쳐졌다 — 예전에는 예약취소만 ReservationService 였다.
            var service = new FakeWorkService
            {
                SearchResult = Ok(OneRow()),
                DetailResult = OkDetail(Allowed()),
                SaveResult = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = new DbResult
                    {
                        Success = false,
                        Code = 601,
                        Message = "다른 사용자가 예약·접수 업무를 변경했습니다. 최신 정보를 다시 조회하십시오.",
                    },
                }),
            };
            var presenter = new WorkbenchPresenter(view, service, new FakeReservationService(), Status(Today), "접수1번창구");
            presenter.LoadInitial();
            view.RaiseSelectionChanged(77);
            int before = service.DetailCalls;

            view.RaiseAction(DbWorkAction.CancelReservation);

            StringAssert.Contains(view.ValidationMessage, "최신 정보를 다시 조회");
            Assert.IsTrue(service.DetailCalls > before, "막힌 뒤 최신값을 다시 읽지 않았다");
        }

        // 대상: WorkbenchPresenter (WF-WRK-01) — 선택 행 없이 Action 을 실행한 경우
        // 목적: 대상 없이 확인창을 띄우면 조작자가 「예」를 눌러도 아무 일이 없다 — 무엇이
        //       취소됐는지 모르는 채로 남는다. 물어보기 전에 대상이 있어야 한다.
        // 확인: 확인창이 0회이고 SP 도 불리지 않는다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 접수 Context 의 목록 상태 필터
        // 목적: 접수 창구의 목록에 오늘의 RSV 가 있어야 한다 — 접수의 입력이 예약 건이므로 그것이
        //       안 보이면 창구는 자기 탭에서 할 일이 없다. CNR 은 예약 창구의 것이라 여기 섞이면
        //       안 된다.
        // 확인: 접수 창구 목록의 상태가 예약완료·접수완료·접수취소 셋이다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — 예약 Context 의 목록 상태 필터
        // 목적: 00 RP-01 · 03 §9.6 에서 창구가 다루는 상태가 다르다. 접수 창구의 건이 예약 창구
        //       목록에 섞이면 조작자가 남의 창구 건을 고르게 된다.
        // 확인: 예약 창구 목록의 상태가 예약완료·예약취소 둘이다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — Context 별 기본 조회기간
        // 목적: 접수는 당일 업무다 (05 §8.2 START_RECEPTION 이 예약일=오늘) — 오늘 하루로 선다.
        //       예약은 앞으로의 일정이라 종료일을 비워 둔다. 둘 다 기본값일 뿐 조작자가 바꾸지만,
        //       기본값이 창구와 안 맞으면 탭을 열 때마다 조건부터 고쳐야 한다.
        // 확인: 접수 창구는 오늘~오늘이고, 예약 창구는 오늘부터 종료일이 열려 있다.
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

        // 대상: WorkbenchPresenter (WF-WRK-01) — DB 오늘날짜 조회가 실패한 경우
        // 목적: 오늘을 PC 시계에서 얻지 않는다. 창구 PC 가 하루 어긋나면 목록도 접수 가능 판정도
        //       같이 어긋나므로, 못 읽었으면 잘못된 날로 세우느니 조작자가 고르게 둔다.
        // 확인: 기간이 세워지지 않고 안내가 「오늘 날짜를 확인하지 못해 기간을 세우지 못했습니다.」다.
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
        public WorkDetailReadDto Read { get; set; }
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

        public string LastCancelAction { get; private set; }

        public OperationResult<WorkSaveReadDto> CancelWork(string actionCode, WorkActionRequest request)
        {
            LastCancelAction = actionCode;
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
