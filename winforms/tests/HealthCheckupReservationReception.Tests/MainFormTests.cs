using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraBars.Ribbon;
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

        // 03 §1.3 · §5.2 · §9.6 · §9.7 — 공통 업무불가면 변경이력·컬럼설정만 남는다.
        [TestMethod]
        public void 공통_업무불가면_변경이력과_컬럼설정만_남는다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    IMainView view = form;
                    view.BusinessActionsEnabled = false;

                    var stillOpen = new List<string>();
                    var closed = new List<string>();
                    foreach (DevExpress.XtraBars.BarItem item in form.Ribbon.Items)
                    {
                        var button = item as DevExpress.XtraBars.BarButtonItem;
                        if (button == null)
                        {
                            continue;
                        }

                        (button.Enabled ? stillOpen : closed).Add(button.Caption);
                    }

                    CollectionAssert.Contains(stillOpen, "변경이력");
                    CollectionAssert.Contains(stillOpen, "컬럼설정");
                    CollectionAssert.Contains(closed, "조회");
                    CollectionAssert.Contains(closed, "신규등록");
                    CollectionAssert.Contains(closed, "예약저장");
                    CollectionAssert.Contains(closed, "현장 당일예약");
                    CollectionAssert.DoesNotContain(closed, "변경이력");
                    CollectionAssert.DoesNotContain(closed, "컬럼설정");
                }
            });
        }

        private static MainForm NewShell()
        {
            return new MainForm(new FakeCommonStatusService(), "접수1번창구");
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
