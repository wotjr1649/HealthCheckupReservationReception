using System;
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
        public void 운영시간_밖이면_DB_가_준_운영시각을_붙인다()
        {
            CommonWorkStatusDto status = Allowed();
            status.IsWorkAllowed = false;
            status.IsWithinHours = false;
            status.BlockCode = (int)DbCode.OutsideHours;
            status.BlockMessage = "현재는 업무 운영시간이 아닙니다.";
            status.OpenTime = new TimeSpan(9, 0, 0);
            status.CloseTime = new TimeSpan(18, 0, 0);

            var view = new FakeMainView();
            new MainPresenter(view, new FakeCommonStatusService { Result = Ok(status) }, "창구");
            view.RaiseShellLoaded();

            Assert.AreEqual(
                "업무 상태 : 업무 불가 — 현재는 업무 운영시간이 아닙니다. (09:00~18:00)",
                view.WorkStatusText);
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
        public void 서비스_실패면_확인_불가로_두고_사유를_보여준다()
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

        public string WorkStatusText { get; set; }
        public string OperatorText { get; set; }
        public string LastMessage { get; private set; }

        public void ShowMessage(string message)
        {
            LastMessage = message;
        }

        public void RaiseShellLoaded()
        {
            EventHandler handler = ShellLoaded;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void RaiseNavigationRequested(BusinessNavigation target)
        {
            EventHandler<BusinessNavigation> handler = NavigationRequested;
            if (handler != null)
            {
                handler(this, target);
            }
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
