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
    public class MainPresenterTests
    {
        // ── 03 §1.3 공통 업무조건 표시

        [TestMethod]
        public void 업무가능이면_업무_가능을_표시한다()
        {
            var view = new FakeMainView();
            var service = new FakeCommonStatusService { Result = Ok(Allowed()) };
            new MainPresenter(view, service, "접수1번창구");

            view.RaiseShellLoaded();

            Assert.AreEqual("업무 상태 : 업무 가능", view.WorkStatusText);
            Assert.AreEqual("조작자 : 접수1번창구", view.OperatorText);
            Assert.IsNull(view.LastMessage);
        }

        [TestMethod]
        public void 운영시간_밖이면_DB_가_준_운영시각을_붙여_표시만_한다()
        {
            CommonWorkStatusDto status = Allowed();
            status.IsWorkAllowed = false;
            status.IsWithinHours = false;
            status.BlockCode = (int)DbCode.OutsideHours;
            status.BlockMessage = "현재는 업무 운영시간이 아닙니다.";

            var view = new FakeMainView();
            new MainPresenter(view, new FakeCommonStatusService { Result = Ok(status) }, "창구");
            view.RaiseShellLoaded();

            Assert.AreEqual(
                "업무 상태 : 업무 불가 — 현재는 업무 운영시간이 아닙니다. (09:00~18:00)",
                view.WorkStatusText);

            // [R12] 상태는 표시만 한다. 화면이 Action 을 닫는 경로는 IMainView 에 아예 없다
            // (`00` §1.1 · 03 §1.3) — 없다는 것은 컴파일이 지킨다.
        }

        [TestMethod]
        public void 휴무일이면_휴무일명을_붙인다()
        {
            CommonWorkStatusDto status = Allowed();
            status.IsWorkAllowed = false;
            status.IsBusinessDay = false;
            status.HolidayName = "성탄절";
            status.BlockCode = (int)DbCode.CenterClosed;
            status.BlockMessage = "오늘은 업무일이 아닙니다.";

            var view = new FakeMainView();
            new MainPresenter(view, new FakeCommonStatusService { Result = Ok(status) }, "창구");
            view.RaiseShellLoaded();

            Assert.AreEqual("업무 상태 : 업무 불가 — 오늘은 업무일이 아닙니다. (성탄절)", view.WorkStatusText);
        }

        [TestMethod]
        public void 업무불가인데_휴무일명이_없으면_괄호를_붙이지_않는다()
        {
            CommonWorkStatusDto status = Allowed();
            status.IsWorkAllowed = false;
            status.IsBusinessDay = false;
            status.HolidayName = null;
            status.BlockCode = (int)DbCode.CenterClosed;
            status.BlockMessage = "오늘은 업무일이 아닙니다.";

            var view = new FakeMainView();
            new MainPresenter(view, new FakeCommonStatusService { Result = Ok(status) }, "창구");
            view.RaiseShellLoaded();

            Assert.AreEqual("업무 상태 : 업무 불가 — 오늘은 업무일이 아닙니다.", view.WorkStatusText);
        }

        [TestMethod]
        public void 서비스_실패면_확인_불가로_두고_업무_Action_을_닫는다()
        {
            var view = new FakeMainView();
            var service = new FakeCommonStatusService
            {
                Result = OperationResult<CommonWorkStatusDto>.Failure("공통 업무상태를 읽지 못했습니다.")
            };
            new MainPresenter(view, service, "창구");

            view.RaiseShellLoaded();

            Assert.AreEqual("업무 상태 : 확인 불가", view.WorkStatusText);
            Assert.AreEqual("공통 업무상태를 읽지 못했습니다.", view.LastMessage);
        }

        // 킷 §6 — provider 메시지는 DB·머신 정보를 드러낸다. 화면에 원문을 싣지 않는다.
        [TestMethod]
        public void 예외가_나도_예외_본문을_화면에_싣지_않는다()
        {
            var view = new FakeMainView();
            var service = new FakeCommonStatusService
            {
                Failure = new InvalidOperationException("서버 DESKTOP-XYZ 의 로그인에 실패했습니다")
            };
            new MainPresenter(view, service, "창구");

            view.RaiseShellLoaded();

            Assert.AreEqual("업무 상태 : 확인 불가", view.WorkStatusText);
            Assert.AreEqual("업무 상태를 확인하지 못했습니다.", view.LastMessage);
            StringAssert.DoesNotMatch(view.LastMessage, new System.Text.RegularExpressions.Regex("DESKTOP"));
        }

        [TestMethod]
        public void 조작자가_비어_있으면_미지정으로_표시한다()
        {
            var view = new FakeMainView();
            new MainPresenter(view, new FakeCommonStatusService { Result = Ok(Allowed()) }, "   ");

            view.RaiseShellLoaded();

            Assert.AreEqual("조작자 : (미지정)", view.OperatorText);
        }

        // ── 2026-09-10 사용자 결정: 업무 화면은 한 번에 하나만 뜬다 (Tab 스트립 제거)

        // 03 §3 — 상단 Navigation 에는 전달키가 없다. Context 는 Normal 이고, 어디서 왔는지는
        // Source 로 남는다 — §8.10 의 폐기 확인 트리거가 그것으로 갈린다.
        [TestMethod]
        public void 신규예약_Navigation_은_전달키_없이_Normal_로_연다()
        {
            FakeMainView view = LoadedShell();

            view.RaiseNavigationRequested(BusinessNavigation.NewReservation);

            CollectionAssert.AreEqual(
                new List<string> { "BeginNewReservation:Normal,-,Navigation" }, view.Calls);
        }

        // 03 §1.1 · §9.1 — 예약 관리와 접수 관리는 화면 하나를 나눠 쓰고 Context 만 갈린다.
        // §9.6·§9.7 이 그 Context 마다 다른 Ribbon Action 을 요구하므로 값이 화면까지 가야 한다.
        [TestMethod]
        public void 예약관리와_접수관리는_같은_Workbench_를_Context_만_달리해_연다()
        {
            FakeMainView view = LoadedShell();

            view.RaiseNavigationRequested(BusinessNavigation.ReservationDesk);
            view.RaiseNavigationRequested(BusinessNavigation.ReceptionDesk);

            CollectionAssert.AreEqual(
                new List<string> { "OpenWorkbench:Reservation,-", "OpenWorkbench:Reception,-" },
                view.Calls);
        }

        // 03 §24.2 — 휴무일 관리는 업무 화면을 세우지 않고, 고른 뒤에는 직전 Page 로 돌아간다.
        [TestMethod]
        public void 휴무일_관리는_업무_화면을_세우지_않고_직전_Page_로_돌아간다()
        {
            FakeMainView view = LoadedShell();
            view.RaiseNavigationRequested(BusinessNavigation.ReceptionDesk);
            view.Calls.Clear();

            view.RaiseNavigationRequested(BusinessNavigation.HolidayManagement);

            // 되돌린 뒤에 연다 — 반대면 빈 Ribbon 이 Modal 뒤에 남는다.
            CollectionAssert.AreEqual(
                new List<string> { "SelectNavigationPage:ReceptionDesk", "ShowHolidayManagement" },
                view.Calls);
        }

        [TestMethod]
        public void 아무_업무_Page_도_고르기_전에_휴무일을_열면_첫_Page_로_돌아간다()
        {
            FakeMainView view = LoadedShell();

            view.RaiseNavigationRequested(BusinessNavigation.HolidayManagement);

            CollectionAssert.AreEqual(
                new List<string> { "SelectNavigationPage:PatientManagement", "ShowHolidayManagement" },
                view.Calls);
        }

        // 03 §24.2 — 공통 업무조건이 이 화면에는 적용되지 않는다.
        [TestMethod]
        public void 공통_업무불가여도_휴무일_관리는_열린다()
        {
            CommonWorkStatusDto status = Allowed();
            status.IsWorkAllowed = false;
            status.BlockCode = (int)DbCode.CenterClosed;
            status.BlockMessage = "오늘은 업무일이 아닙니다.";

            var view = new FakeMainView();
            new MainPresenter(view, new FakeCommonStatusService { Result = Ok(status) }, "창구");
            view.RaiseShellLoaded();
            view.Calls.Clear();

            view.RaiseNavigationRequested(BusinessNavigation.HolidayManagement);

            CollectionAssert.Contains(view.Calls, "ShowHolidayManagement");
        }

        // [X] 기동 직후 Ribbon 은 첫 Page 가 선택된 채 뜨지만 그것은 "변경"이 아니라 이벤트가
        //     나지 않는다. Shell 이 첫 화면을 직접 세우지 않으면, 이미 선택된 [수검자 관리] 를
        //     눌러도 영원히 서지 않는다. 실행 화면에서 실측한 결함이다.
        [TestMethod]
        public void 기동하면_첫_업무_화면이_선다()
        {
            var view = new FakeMainView();
            new MainPresenter(view, new FakeCommonStatusService { Result = Ok(Allowed()) }, "창구");

            view.RaiseShellLoaded();

            CollectionAssert.Contains(view.Calls, "ShowBusinessScreen:PatientManagement");
        }

        private static FakeMainView LoadedShell()
        {
            var view = new FakeMainView();
            new MainPresenter(view, new FakeCommonStatusService { Result = Ok(Allowed()) }, "창구");
            view.RaiseShellLoaded();
            view.Calls.Clear();
            return view;
        }

        private static OperationResult<CommonWorkStatusDto> Ok(CommonWorkStatusDto status)
        {
            return OperationResult<CommonWorkStatusDto>.Success(status);
        }

        private static CommonWorkStatusDto Allowed()
        {
            return new CommonWorkStatusDto
            {
                Today = new DateTime(2026, 9, 9),
                DayName = "수요일",
                HolidayName = null,
                OpenTime = new TimeSpan(9, 0, 0),
                CloseTime = new TimeSpan(18, 0, 0),
                IsBusinessDay = true,
                IsWithinHours = true,
                IsWorkAllowed = true,
                BlockCode = (int)DbCode.Ok,
                BlockMessage = string.Empty
            };
        }
    }

    internal sealed class FakeMainView : IMainView
    {
        public event EventHandler ShellLoaded;
        public event EventHandler<BusinessNavigation> NavigationRequested;

        public List<string> Calls { get; private set; }

        public FakeMainView()
        {
            Calls = new List<string>();
        }

        public string WorkStatusText { get; set; }
        public string OperatorText { get; set; }
        public string LastMessage { get; private set; }

        public void ShowBusinessScreen(BusinessTab screen) { Calls.Add("ShowBusinessScreen:" + screen); }

        public void BeginNewReservation(ReservationContext context, long? patientId, NavigationSource source)
        {
            Calls.Add("BeginNewReservation:" + context + "," + Key(patientId) + "," + source);
        }

        public void OpenWorkbench(WorkContext context, long? workId)
        {
            Calls.Add("OpenWorkbench:" + context + "," + Key(workId));
        }

        // 03 §3 의 전달키는 없을 수 있다. 없는 것과 0 이 같은 글자가 되지 않게 한다.
        private static string Key(long? value)
        {
            return value == null ? "-" : value.Value.ToString();
        }

        public void SelectNavigationPage(BusinessNavigation page) { Calls.Add("SelectNavigationPage:" + page); }
        public void ShowHolidayManagement() { Calls.Add("ShowHolidayManagement"); }

        public void ShowMessage(string message) { LastMessage = message; }

        public void RaiseShellLoaded()
        {
            EventHandler handler = ShellLoaded;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }

        public void RaiseNavigationRequested(BusinessNavigation target)
        {
            EventHandler<BusinessNavigation> handler = NavigationRequested;
            if (handler != null) { handler(this, target); }
        }

    }

    internal sealed class FakeCommonStatusService : ICommonStatusService
    {
        public OperationResult<CommonWorkStatusDto> Result { get; set; }
        public Exception Failure { get; set; }

        public OperationResult<CommonWorkStatusDto> GetCurrent()
        {
            if (Failure != null)
            {
                throw Failure;
            }

            return Result;
        }
    }
}
