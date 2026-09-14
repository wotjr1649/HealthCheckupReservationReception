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

        // 대상: MainPresenter (WF-00) — 셸 상태줄의 업무 상태·조작자 표시
        // 목적: 05 §7.1 에서 업무 가능 여부는 USP_HC_공통업무상태_조회 가 낸다. 화면이 시계를
        //       읽어 다시 판정하면 판정이 두 곳이 되고, 창구 PC 시계가 어긋난 날 DB 와 다른
        //       말을 한다.
        // 확인: 상태줄이 「업무 상태 : 업무 가능」이고 조작자가 「조작자 : 접수1번창구」이며
        //       오류 메시지가 없다.
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

        // 대상: MainPresenter (WF-00) — 운영시간 밖일 때의 상태줄 문구
        // 목적: 00 CP-02 · 05 §7.1 — 운영시각 수치를 화면이 갖지 않는다. 여기에 09:00~18:00 을
        //       적으면 같은 값이 00 · 운영기준 테이블 · 화면 세 곳이 되고, 운영시간을 바꾸는 날
        //       화면만 옛 값을 말한다.
        // 확인: 상태줄이 DB 가 준 차단메시지를 그대로 붙여 표시하고, 화면 안에 시각 리터럴이
        //       들어가지 않는다.
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

        // 대상: MainPresenter (WF-00) — 오늘이 휴무일일 때의 상태줄 문구
        // 목적: 00 HOL Rule 에서 휴무일명은 휴무일 테이블의 값이다. 조작자가 「왜 오늘 업무가
        //       안 되는가」를 상태줄 한 줄로 알 수 있어야 문의가 줄어든다.
        // 확인: 상태줄이 「업무 상태 : 업무 불가 — 오늘은 업무일이 아닙니다. (성탄절)」이다.
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

        // 대상: MainPresenter (WF-00) — 휴무일명이 없는 업무불가(일요일 등)의 상태줄 문구
        // 목적: 이름이 없는데 괄호만 남으면 「업무 불가 ()」가 되어 값이 빠진 것처럼 읽힌다.
        //       빈 괄호는 조작자에게 프로그램 결함으로 보인다.
        // 확인: 상태줄이 「업무 상태 : 업무 불가 — 오늘은 업무일이 아닙니다.」로 괄호 없이 끝난다.
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

        // 대상: MainPresenter (WF-00) — 공통 업무상태 조회가 실패한 경우
        // 목적: 상태를 못 읽었는데 「업무 가능」으로 기본값을 삼으면 조작자가 막힐 일을 모르고
        //       들어간다. 모르는 것은 모른다고 말해야 한다.
        // 확인: 상태줄이 「업무 상태 : 확인 불가」이고 실패 사유가 메시지로 전해진다.
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

        // 대상: MainPresenter (WF-00) — 조회에서 예외가 올라온 경우
        // 목적: 킷 §6 — provider 메시지는 DB 이름·서버명 같은 내부 정보를 드러낸다. 셸 상태줄은
        //       늘 보이는 자리라 그대로 실으면 화면 캡처마다 서버명이 따라 나간다.
        // 확인: 상태줄이 「확인 불가」이고 메시지가 화면용 문장이며, 예외에 든 서버명(DESKTOP…)이
        //       문구에 없다.
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

        // 대상: MainPresenter (WF-00) — 조작자명이 비었을 때의 표시
        // 목적: 변경이력의 조작자 는 감사 기록이다 (04 §14). 빈칸을 그대로 보여 주면 값이 안
        //       들어온 것인지 화면이 안 그린 것인지 구분되지 않는다.
        // 확인: 공백만 있는 조작자명으로도 상태줄이 「조작자 : (미지정)」으로 선다.
        [TestMethod]
        public void 조작자가_비어_있으면_미지정으로_표시한다()
        {
            var view = new FakeMainView();
            new MainPresenter(view, new FakeCommonStatusService { Result = Ok(Allowed()) }, "   ");

            view.RaiseShellLoaded();

            Assert.AreEqual("조작자 : (미지정)", view.OperatorText);
        }

        // ── 2026-09-10 사용자 결정: 업무 화면은 한 번에 하나만 뜬다 (Tab 스트립 제거)

        // 대상: MainPresenter (WF-00) — BusinessNavigation 열거형 전체의 Page 전환 동작
        // 목적: 2026-09-11 사용자 결정으로 상단 Navigation 은 전부 「가는 곳」이다. 예전에는
        //       신규 예약과 휴무일 관리가 눌러도 아무 데도 가지 않고 Modal 만 띄운 뒤 탭이
        //       제자리로 돌아오는 Page 였고, 같은 띠에서 어떤 것은 가고 어떤 것은 떠 예측이
        //       되지 않았다 — 사용자가 「기능 배치가 중구난방」이라 보고한 자리다. 문장으로 두면
        //       다시 그런 Page 가 끼어드므로 열거형 전체를 돌며 잰다.
        // 확인: 어느 Page 를 눌러도 화면 호출이 정확히 1회이고, 그 호출이 업무 화면을 세우는
        //       ShowBusinessScreen 이거나 Workbench 를 여는 OpenWorkbench 다.
        [TestMethod]
        public void 상단_Navigation_은_하나도_빠짐없이_업무_화면을_세운다()
        {
            foreach (BusinessNavigation page in Enum.GetValues(typeof(BusinessNavigation)))
            {
                FakeMainView view = LoadedShell();

                view.RaiseNavigationRequested(page);

                Assert.AreEqual(1, view.Calls.Count,
                    page + " 를 골랐는데 한 일이 하나가 아니다: " + string.Join(" · ", view.Calls.ToArray()));

                string call = view.Calls[0];
                Assert.IsTrue(call.StartsWith("ShowBusinessScreen:") || call.StartsWith("OpenWorkbench:"),
                    page + " 는 업무 화면을 세우지 않는다 (" + call + ")");
            }
        }

        // 대상: MainPresenter (WF-00) — 예약 관리·접수 관리 두 Page 가 넘기는 WorkContext
        // 목적: 03 §1.1·§9.1 에서 두 창구는 화면 하나를 나눠 쓰고 Context 만 갈린다. §9.6·§9.7 이
        //       Context 마다 다른 Ribbon Action 을 요구하므로 그 값이 화면까지 가야 한다 —
        //       안 가면 접수 창구에서 예약 Action 이 열린다.
        // 확인: 두 Page 가 같은 Workbench 를 열되 넘긴 Context 값이 각각 예약·접수로 다르다.
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

        // 대상: MainPresenter (WF-00) — 프로그램 기동 직후의 첫 화면
        // 목적: 기동 직후 Ribbon 은 첫 Page 가 선택된 채 뜨지만 그것은 「변경」이 아니라 이벤트가
        //       나지 않는다. Shell 이 첫 화면을 직접 세우지 않으면, 이미 선택된 [수검자 관리] 를
        //       눌러도 영원히 서지 않는다 — 실행 화면에서 실제로 밟은 결함이다.
        // 확인: 기동 호출 목록에 수검자 관리 화면을 세우는 호출이 들어 있다.
        [TestMethod]
        public void 기동하면_첫_업무_화면이_선다()
        {
            var view = new FakeMainView();
            new MainPresenter(view, new FakeCommonStatusService { Result = Ok(Allowed()) }, "창구");

            view.RaiseShellLoaded();

            CollectionAssert.Contains(view.Calls, "ShowBusinessScreen:PatientManagement");
        }

        // 대상: MainPresenter (WF-00) — 업무 화면이 Workbench 이동을 요청했을 때의 Page 전환
        // 목적: 03 §8.5 기존 유효예약 · §8.11 저장 성공에서 업무 화면이 Workbench 로 넘겨 달라고
        //       한다. 화면만 바꾸고 Ribbon Page 를 두면 열려 있는 Action 이 그 화면의 것이 아니게
        //       되고, 휴무일 관리에서 돌아올 자리도 어긋난다.
        // 확인: Workbench 이동 요청에 화면 전환과 Page 전환이 함께 일어나고 순서도 고정이다.
        [TestMethod]
        public void 화면이_Workbench_를_부르면_Page_도_함께_옮긴다()
        {
            FakeMainView view = LoadedShell();

            view.RaiseWorkbenchRequested(WorkContext.Reservation, 91);

            CollectionAssert.AreEqual(
                new List<string> { "SelectNavigationPage:ReservationDesk", "OpenWorkbench:Reservation,91" },
                view.Calls);
        }

        // 대상: MainPresenter (WF-00) — 현장 당일예약 저장 뒤의 이동 위치
        // 목적: 00 RP-05 현장 당일예약은 저장 즉시 접수로 이어진다. 저장 뒤에 갈 곳이 예약
        //       Workbench 면 조작자가 방금 만든 건을 접수하러 한 번 더 옮겨야 하고, 상단 Page 를
        //       함께 옮기지 않으면 탭과 내용이 어긋난 채 남는다.
        // 확인: 접수 Context 로 Workbench 를 열면서 Ribbon Page 도 접수 관리로 함께 옮긴다.
        [TestMethod]
        public void WalkIn_저장은_접수_관리_Page_로_옮긴다()
        {
            FakeMainView view = LoadedShell();

            view.RaiseWorkbenchRequested(WorkContext.Reception, 92);

            CollectionAssert.AreEqual(
                new List<string> { "SelectNavigationPage:ReceptionDesk", "OpenWorkbench:Reception,92" },
                view.Calls);
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
        public event EventHandler<WorkbenchTarget> WorkbenchRequested;

        public List<string> Calls { get; private set; }

        public FakeMainView()
        {
            Calls = new List<string>();
        }

        public string WorkStatusText { get; set; }
        public string OperatorText { get; set; }
        public string LastMessage { get; private set; }

        public void ShowBusinessScreen(BusinessTab screen) { Calls.Add("ShowBusinessScreen:" + screen); }

        public void BeginNewReservation(long patientId)
        {
            Calls.Add("BeginNewReservation:" + patientId);
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

        public void RaiseWorkbenchRequested(WorkContext context, long workId)
        {
            EventHandler<WorkbenchTarget> handler = WorkbenchRequested;
            if (handler != null)
            {
                handler(this, new WorkbenchTarget { Context = context, WorkId = workId });
            }
        }

    }

    internal sealed class FakeCommonStatusService : ICommonStatusService
    {
        public OperationResult<CommonWorkStatusDto> Result { get; set; }
        public Exception Failure { get; set; }

        /// <summary>몇 번 물었는가. 「조회마다 오늘날짜를 다시 묻지 않는다」를 재는 자리다.</summary>
        public int Calls { get; private set; }

        public OperationResult<CommonWorkStatusDto> GetCurrent()
        {
            Calls++;
            if (Failure != null)
            {
                throw Failure;
            }

            return Result;
        }
    }
}
