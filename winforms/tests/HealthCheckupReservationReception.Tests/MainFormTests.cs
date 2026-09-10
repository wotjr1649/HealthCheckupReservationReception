using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraBars.Ribbon;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Tests.Presenters;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests
{
    [TestClass]
    public class MainFormTests
    {
        [TestMethod]
        public void MainForm_은_킷_UI_베이스라인을_따른다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    Assert.AreEqual(AutoScaleMode.Font, form.AutoScaleMode);
                    Assert.AreEqual("굴림", form.Font.Name);
                    Assert.AreEqual(9F, form.Font.SizeInPoints);
                }
            });
        }

        // 화면 제목은 여기서 문자열로 비교하지 않는다. 05 §1.1 이 단일 출처이고
        // scripts/verify-contract-names.sh 의 CFG-007 이 Designer 와 대조한다 —
        // 여기 적으면 같은 값이 세 곳에 있게 된다 (ROOT AGENTS.md §6).
        [TestMethod]
        public void MainForm_의_Ribbon_과_StatusBar_는_서로_연결된다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    Assert.IsNotNull(form.Ribbon, "Ribbon 이 없다");
                    Assert.IsNotNull(form.StatusBar, "StatusBar 가 없다");
                    Assert.AreSame(form.StatusBar, form.Ribbon.StatusBar);
                    Assert.AreSame(form.Ribbon, form.StatusBar.Ribbon);
                }
            });
        }

        // 03 §1.1 · §4.1 — 상단 업무 Navigation 다섯이 같은 밴드에 동등하게 선다.
        // 화면설계서 slide4 는 y=101 한 줄에 같은 크기(w=122)로 다섯을 그린다.
        // 목록·순서의 단일 출처는 kit.js 의 NAV 이고 verify-screen-design.js SCR-002 가 지킨다 —
        // 여기서는 다섯이라는 것과 휴무일이 마지막이라는 것만 본다.
        [TestMethod]
        public void 상단_Navigation_은_동등한_Page_다섯이다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    Assert.AreEqual(5, form.Ribbon.Pages.Count);
                    Assert.AreEqual("휴무일 관리", form.Ribbon.Pages[4].Text);
                    Assert.AreEqual(0, form.Ribbon.PageHeaderItemLinks.Count,
                        "다섯째만 Page 헤더 버튼으로 빼지 않는다");
                    Assert.AreEqual(0, form.Ribbon.Pages[4].Groups.Count,
                        "휴무일 Page 는 선택 즉시 Modal 을 열고 돌아가므로 그룹이 없다");
                }
            });
        }

        // 03 §4.2 — Ribbon Group 순서는 [검색] → [현재 업무 Action] → [보기] 다.
        [TestMethod]
        public void Ribbon_Group_순서는_검색_업무_보기_다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    foreach (RibbonPage page in form.Ribbon.Pages)
                    {
                        if (page.Groups.Count < 3)
                        {
                            continue;   // 신규 예약은 [예약] 그룹 하나뿐이다 (03 §8.2)
                        }

                        Assert.AreEqual("검색", page.Groups[0].Text, page.Text + " 첫 그룹");
                        Assert.AreEqual("보기", page.Groups[page.Groups.Count - 1].Text, page.Text + " 마지막 그룹");
                    }
                }
            });
        }

        // 03 §1.3 · §5.2 · §9.6 · §9.7 — 공통 업무불가여도 업무 Action 은 닫히지 않는다.
        // [R12] 공통 업무불가는 Action 을 닫지 않는다 (`00` §1.1 · 03 §1.3 · §5.2 · §9.6 · §9.7).
        //       화면이 미리 닫으면 저장 시점의 DB 판정을 사용자가 받아볼 수 없다.
        //       R12 이전에는 이 자리가 "변경이력·컬럼설정만 남는다" 를 재는 시험이었다.
        [TestMethod]
        public void 공통_업무불가여도_업무_Action_이_닫히지_않는다()
        {
            RunSta(() =>
            {
                var blocked = new CommonWorkStatusDto
                {
                    Today = new DateTime(2026, 9, 9),
                    DayName = "수요일",
                    OpenTime = new TimeSpan(9, 0, 0),
                    CloseTime = new TimeSpan(18, 0, 0),
                    IsBusinessDay = true,
                    IsWithinHours = false,
                    IsWorkAllowed = false,
                    BlockCode = (int)DbCode.OutsideHours,
                    BlockMessage = "현재는 업무 운영시간이 아닙니다.",
                };

                var service = new FakeCommonStatusService
                {
                    Result = OperationResult<CommonWorkStatusDto>.Success(blocked),
                };

                using (var form = new MainForm(service, new FakePatientService(), "접수1번창구"))
                {
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-32000, -32000);
                    form.Show();
                    Application.DoEvents();
                    form.PatientRowSelected = true;

                    // [X] "모든 버튼이 열려 있다" 로 재지 않는다. R12 가 요구하는 것은
                    //     "공통 업무불가가 닫지 않는다" 뿐이고, 03 §9.6·§9.7 은 여전히
                    //     Work 상태로 닫는 칸을 갖는다 — WF-WRK-01 이 그 표대로 구현되는
                    //     순간 과잉 단언이 red 가 된다. 03 §5.2 가 실제로 다루는 Action 만 센다.
                    // [조회]·[컬럼설정] 은 2026-09-10 결정으로 수검자 Page 에서 화면 안으로
                    // 옮겨 갔다. 여기 남기면 예약·접수 Page 의 동명 버튼을 재게 된다.
                    string[] shouldStayOpen = { "신규등록", "정보수정", "신규예약" };
                    foreach (string caption in shouldStayOpen)
                    {
                        Assert.IsTrue(Enabled(form, caption),
                            "업무불가인데 닫혔다: " + caption);
                    }

                    Assert.IsTrue(PatientLogEnabled(form), "업무불가인데 [변경이력]이 닫혔다");
                    form.Close();
                }
            });
        }

        // 03 §5.2 표 — 행 미선택이면 [정보수정]·[신규예약]·[변경이력]이 닫히고
        // 행을 고르면 셋이 함께 열린다. [R12] 이제 이 표에 다른 축은 없다.
        [TestMethod]
        public void 행_미선택이면_정보수정_신규예약_변경이력이_닫힌다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    form.PatientRowSelected = false;
                    Assert.IsFalse(Enabled(form, "정보수정"));
                    Assert.IsFalse(Enabled(form, "신규예약"));
                    Assert.IsFalse(PatientLogEnabled(form));
                    Assert.IsTrue(Enabled(form, "신규등록"), "신규등록은 행과 무관하다");

                    form.PatientRowSelected = true;
                    Assert.IsTrue(Enabled(form, "정보수정"));
                    Assert.IsTrue(Enabled(form, "신규예약"));
                    Assert.IsTrue(PatientLogEnabled(form));
                }
            });
        }

        // 2026-09-10 사용자 결정 — 업무 화면은 한 번에 하나만 뜬다. Tab 스트립을 걷었으므로
        // 담는 자리는 PanelControl 하나이고, 같은 화면을 다시 불러도 하나를 넘지 않는다.
        [TestMethod]
        public void 수검자_관리_화면은_업무_판_하나에_선다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    IMainView view = form;
                    view.ShowBusinessScreen(BusinessTab.PatientManagement);
                    view.ShowBusinessScreen(BusinessTab.PatientManagement);

                    Control panel = BusinessPanel(form);
                    Assert.AreEqual(1, panel.Controls.Count, "화면이 겹쳐 쌓였다");
                    Assert.IsInstanceOfType(panel.Controls[0], typeof(UcPatientManagement));
                    Assert.AreEqual(DockStyle.Fill, panel.Controls[0].Dock);
                }
            });
        }

        private static bool Enabled(MainForm form, string caption)
        {
            foreach (DevExpress.XtraBars.BarItem item in form.Ribbon.Items)
            {
                if (item.Caption == caption)
                {
                    return item.Enabled;
                }
            }

            throw new AssertFailedException("Ribbon 에 " + caption + " 이 없다");
        }

        // `변경이력`은 세 Page 에 하나씩 있다. 수검자 Page 의 것만 본다.
        private static bool PatientLogEnabled(MainForm form)
        {
            foreach (DevExpress.XtraBars.Ribbon.RibbonPageGroup group in form.Ribbon.Pages[0].Groups)
            {
                foreach (DevExpress.XtraBars.BarItemLink link in group.ItemLinks)
                {
                    if (link.Item.Caption == "변경이력")
                    {
                        return link.Item.Enabled;
                    }
                }
            }

            throw new AssertFailedException("수검자 Page 에 변경이력이 없다");
        }

        // [X] 03 에 없는 리본 크롬은 RibbonControl 을 만들면 자동으로 켜진다 — 내가 넣은 것이
        //     아니라 끄지 않았던 것이다. 배치 게이트(SCR-*)는 설계 소스에 **있는** 것만 보므로
        //     "설계에 없는데 켜진 것" 은 잡지 못한다. 그 자리를 이 시험이 맡는다.
        [TestMethod]
        public void 설계에_없는_리본_크롬은_꺼져_있다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    RibbonControl ribbon = form.Ribbon;
                    Assert.AreEqual(DefaultBoolean.False, ribbon.ShowApplicationButton,
                        "Application Button — Office 의 [파일] 탭 자리");
                    Assert.AreEqual(DefaultBoolean.False, ribbon.ShowDisplayOptionsMenuButton,
                        "제목표시줄의 리본 표시 옵션");
                    Assert.AreEqual(DefaultBoolean.False, ribbon.ShowExpandCollapseButton,
                        "리본 접기 버튼");
                    Assert.AreEqual(RibbonQuickAccessToolbarLocation.Hidden, ribbon.ToolbarLocation,
                        "Quick Access Toolbar");
                    Assert.IsFalse(ribbon.ShowToolbarCustomizeItem, "도구모음 사용자 지정");

                    // 버튼만 숨기면 페이지 헤더 더블클릭으로 여전히 접힌다. 경로를 막고 상태를 고정한다.
                    Assert.IsFalse(ribbon.AllowMinimizeRibbon, "리본 최소화 경로");
                    Assert.IsFalse(ribbon.Minimized, "리본은 펼친 상태로 고정한다");
                }
            });
        }

        private static MainForm NewShell()
        {
            return new MainForm(new FakeCommonStatusService(), new FakePatientService(), "접수1번창구");
        }

        private static Control BusinessPanel(MainForm form)
        {
            Control[] found = form.Controls.Find("pnlBusiness", true);
            Assert.AreEqual(1, found.Length, "업무 화면 판을 찾지 못했다");
            return found[0];
        }

        private static void RunSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                throw new AssertFailedException(failure.Message, failure);
            }
        }
    }
}
