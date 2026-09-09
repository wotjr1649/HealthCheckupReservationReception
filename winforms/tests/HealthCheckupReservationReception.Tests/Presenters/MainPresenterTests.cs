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
        public void 업무가능이면_업무_가능을_표시하고_업무_Action_을_연다()
        {
            var view = new FakeMainView();
            var service = new FakeCommonStatusService { Result = Ok(Allowed()) };
            new MainPresenter(view, service, "접수1번창구");

            view.RaiseShellLoaded();

            Assert.AreEqual("업무 상태 : 업무 가능", view.WorkStatusText);
            Assert.AreEqual("조작자 : 접수1번창구", view.OperatorText);
            Assert.IsTrue(view.BusinessActionsEnabled);
            Assert.IsNull(view.LastMessage);
        }

        [TestMethod]
        public void 운영시간_밖이면_DB_가_준_운영시각을_붙이고_업무_Action_을_닫는다()
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
            Assert.IsFalse(view.BusinessActionsEnabled);
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
            Assert.IsFalse(view.BusinessActionsEnabled);
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
            Assert.IsFalse(view.BusinessActionsEnabled);
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

        // ── 03 §4.3 Single Instance 업무 Tab

        // 기동이 이미 수검자 관리 Tab 을 열어 두므로 아직 열리지 않은 화면으로 시험한다.
        [TestMethod]
        public void Navigation_은_업무_Tab_을_열고_활성화한다()
        {
            FakeMainView view = LoadedShell();

            view.RaiseNavigationRequested(BusinessNavigation.NewReservation);

            CollectionAssert.AreEqual(
                new List<string> { "OpenTab:NewReservation:신규 예약", "ActivateTab:NewReservation" },
                view.Calls);
        }

        [TestMethod]
        public void 같은_Tab_을_다시_부르면_새로_만들지_않고_살린다()
        {
            FakeMainView view = LoadedShell();
            view.RaiseNavigationRequested(BusinessNavigation.NewReservation);
            view.Calls.Clear();

            view.RaiseNavigationRequested(BusinessNavigation.NewReservation);

            CollectionAssert.DoesNotContain(view.Calls, "OpenTab:NewReservation:신규 예약");
            CollectionAssert.Contains(view.Calls, "ActivateTab:NewReservation");
        }

        // 03 §1.1 · §9.1 — 예약 관리와 접수 관리는 Tab 하나를 나눠 쓰고 Caption 만 바뀐다.
        [TestMethod]
        public void 예약관리와_접수관리는_같은_Workbench_Tab_을_Caption_만_바꿔_쓴다()
        {
            FakeMainView view = LoadedShell();

            view.RaiseNavigationRequested(BusinessNavigation.ReservationDesk);
            view.Calls.Clear();
            view.RaiseNavigationRequested(BusinessNavigation.ReceptionDesk);

            CollectionAssert.AreEqual(
                new List<string> { "SetTabCaption:Workbench:접수 관리", "ActivateTab:Workbench" },
                view.Calls);
        }

        [TestMethod]
        public void Tab_을_닫으면_다음_호출에서_다시_연다()
        {
            FakeMainView view = LoadedShell();
            view.RaiseNavigationRequested(BusinessNavigation.PatientManagement);
            view.RaiseTabCloseRequested(BusinessTab.PatientManagement);
            view.Calls.Clear();

            view.RaiseNavigationRequested(BusinessNavigation.PatientManagement);

            CollectionAssert.Contains(view.Calls, "OpenTab:PatientManagement:수검자 관리");
        }

        [TestMethod]
        public void 열리지_않은_Tab_의_닫기는_아무것도_하지_않는다()
        {
            FakeMainView view = LoadedShell();
            view.Calls.Clear();

            view.RaiseTabCloseRequested(BusinessTab.Workbench);

            Assert.AreEqual(0, view.Calls.Count);
        }

        // 03 §4.2 — Tab 전환 시 그 Tab 의 Ribbon Page 를 활성화한다.
        [TestMethod]
        public void Tab_을_고르면_그_Tab_의_Ribbon_Page_가_선택된다()
        {
            FakeMainView view = LoadedShell();
            view.RaiseNavigationRequested(BusinessNavigation.NewReservation);
            view.Calls.Clear();

            view.RaiseTabActivated(BusinessTab.NewReservation);

            CollectionAssert.AreEqual(
                new List<string> { "SelectNavigationPage:NewReservation" }, view.Calls);
        }

        [TestMethod]
        public void Workbench_Tab_을_고르면_마지막_Context_의_Page_로_돌아간다()
        {
            FakeMainView view = LoadedShell();
            view.RaiseNavigationRequested(BusinessNavigation.ReceptionDesk);
            view.Calls.Clear();

            view.RaiseTabActivated(BusinessTab.Workbench);

            CollectionAssert.AreEqual(
                new List<string> { "SelectNavigationPage:ReceptionDesk" }, view.Calls);
        }

        // 03 §24.2 — 휴무일 관리는 Tab 을 열지 않고, 고른 뒤에는 직전 Page 로 돌아간다.
        [TestMethod]
        public void 휴무일_관리는_Tab_을_열지_않고_직전_Page_로_돌아간다()
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
            Assert.IsFalse(view.BusinessActionsEnabled);
            view.Calls.Clear();

            view.RaiseNavigationRequested(BusinessNavigation.HolidayManagement);

            CollectionAssert.Contains(view.Calls, "ShowHolidayManagement");
        }

        // [X] 기동 직후 Ribbon 은 첫 Page 가 선택된 채 뜨지만 그것은 "변경"이 아니라 이벤트가
        //     나지 않는다. Shell 이 첫 Tab 을 직접 열지 않으면, 이미 선택된 [수검자 관리] 를
        //     눌러도 영원히 열리지 않는다. 실행 화면에서 실측한 결함이다.
        [TestMethod]
        public void 기동하면_첫_업무_Tab_이_열린다()
        {
            var view = new FakeMainView();
            new MainPresenter(view, new FakeCommonStatusService { Result = Ok(Allowed()) }, "창구");

            view.RaiseShellLoaded();

            CollectionAssert.Contains(view.Calls, "OpenTab:PatientManagement:수검자 관리");
            CollectionAssert.Contains(view.Calls, "ActivateTab:PatientManagement");
        }

        [TestMethod]
        public void 기동으로_열린_Tab_은_다시_열리지_않는다()
        {
            FakeMainView view = LoadedShell();

            view.RaiseNavigationRequested(BusinessNavigation.PatientManagement);

            CollectionAssert.DoesNotContain(view.Calls, "OpenTab:PatientManagement:수검자 관리");
            CollectionAssert.Contains(view.Calls, "ActivateTab:PatientManagement");
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
        public event EventHandler<BusinessTab> TabCloseRequested;
        public event EventHandler<BusinessTab> TabActivated;

        public List<string> Calls { get; private set; }

        public FakeMainView()
        {
            Calls = new List<string>();
        }

        public string WorkStatusText { get; set; }
        public string OperatorText { get; set; }
        public bool BusinessActionsEnabled { get; set; }
        public string LastMessage { get; private set; }

        public void OpenTab(BusinessTab tab, string caption) { Calls.Add("OpenTab:" + tab + ":" + caption); }
        public void ActivateTab(BusinessTab tab) { Calls.Add("ActivateTab:" + tab); }
        public void SetTabCaption(BusinessTab tab, string caption) { Calls.Add("SetTabCaption:" + tab + ":" + caption); }
        public void CloseTab(BusinessTab tab) { Calls.Add("CloseTab:" + tab); }
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

        public void RaiseTabCloseRequested(BusinessTab tab)
        {
            EventHandler<BusinessTab> handler = TabCloseRequested;
            if (handler != null) { handler(this, tab); }
        }

        public void RaiseTabActivated(BusinessTab tab)
        {
            EventHandler<BusinessTab> handler = TabActivated;
            if (handler != null) { handler(this, tab); }
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
