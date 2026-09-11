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

        /// <summary>
        /// **Page 는 「가는 곳」만 갖는다** (2026-09-11 사용자 결정 · 문서 §4.7).
        ///
        /// [X] 그룹이 없는 Page 가 곧 「눌러도 아무 데도 안 가는 Page」다. 예전에는 그런 것이
        ///     둘(`신규 예약` · `휴무일 관리`) 있었고, 같은 띠에서 어떤 것은 가고 어떤 것은
        ///     떠서 예측이 되지 않았다 — 사용자가 「기능 배치가 중구난방」이라 보고한 자리다.
        ///     여기서는 **그 모양 자체**를 막는다: Page 는 반드시 명령 그룹을 갖는다.
        ///
        /// Page 의 개수·이름은 적지 않는다 — `MainPresenterTests` 가 `BusinessNavigation`
        /// 전체를 돌며 「가지 않는 Page」를 잡고, 그것이 단일 출처다 (ROOT AGENTS.md §6).
        /// </summary>
        [TestMethod]
        public void Ribbon_Page_는_전부_명령_그룹을_갖는다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    foreach (RibbonPage page in form.Ribbon.Pages)
                    {
                        Assert.AreNotEqual(0, page.Groups.Count,
                            page.Text + " 는 그룹이 없다 — 장소가 아니라 명령이라는 뜻이므로 Page 가 아니어야 한다");
                    }

                    // 휴무일 관리도 Page 다 (2026-09-11 사용자 지시로 업무 화면이 되었다).
                    // Application 버튼은 그래서 다시 꺼졌다 — 거기 둘 것이 없다.
                }
            });
        }

        // 03 §4.2 는 Ribbon Group 순서를 [검색] → [현재 업무 Action] → [보기] 로 적었다.
        // 2026-09-10 사용자 결정으로 **[검색] 그룹이 Ribbon 에서 사라졌다** — [조회] 는 화면
        // 안에 있고 같은 버튼이 두 곳에 있을 이유가 없다. [컬럼설정] 도 같은 이유로 Grid 옆
        // 드롭다운이 되었다. 남은 규칙은 "[보기] 가 있으면 그것이 마지막" 이다.
        //
        // 휴무일 관리에는 [보기] 가 없다 — 03 §24.7 이 그 화면에 변경이력을 두지 않는다.
        // 그래서 "있으면" 이고, **있어야 하는 셋은 아래에서 이름으로 확인한다** — 건너뛰는
        // 조건은 곧 아무것도 재지 않는 green 으로 자란다.
        [TestMethod]
        public void 보기_그룹이_있으면_마지막이고_검색_그룹은_없다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    var inspected = new List<string>();
                    foreach (RibbonPage page in form.Ribbon.Pages)
                    {
                        foreach (RibbonPageGroup group in page.Groups)
                        {
                            Assert.AreNotEqual("검색", group.Text,
                                page.Text + " 에 [검색] 그룹이 남아 있다 — 조회는 화면 안이다");
                        }

                        bool hasView = false;
                        foreach (RibbonPageGroup group in page.Groups)
                        {
                            if (group.Text == "보기") { hasView = true; }
                        }

                        if (!hasView)
                        {
                            continue;
                        }

                        inspected.Add(page.Text);
                        Assert.AreEqual("보기", page.Groups[page.Groups.Count - 1].Text,
                            page.Text + " 마지막 그룹");
                    }

                    // 아무 Page 도 집지 못하면 위 단언이 한 번도 돌지 않는다.
                    CollectionAssert.Contains(inspected, "수검자 관리", "수검자 관리 Page 를 못 집었다");
                    CollectionAssert.Contains(inspected, "예약 관리", "예약 관리 Page 를 못 집었다");
                    CollectionAssert.Contains(inspected, "접수 관리", "접수 관리 Page 를 못 집었다");
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

                using (var form = new MainForm(service, new FakePatientService(), new FakeWorkService(), new FakeReservationService(), new FakeHolidayService(), "접수1번창구", true))
                {
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-32000, -32000);
                    form.Show();
                    Application.DoEvents();
                    form.PatientActions = new PatientActionState { RowSelected = true, Reserve = true };

                    // [X] "모든 버튼이 열려 있다" 로 재지 않는다. R12 가 요구하는 것은
                    //     "공통 업무불가가 닫지 않는다" 뿐이고, 03 §9.6·§9.7 은 여전히
                    //     Work 상태로 닫는 칸을 갖는다 — WF-WRK-01 이 그 표대로 구현되는
                    //     순간 과잉 단언이 red 가 된다. 03 §5.2 가 실제로 다루는 Action 만 센다.
                    // [조회]·[컬럼설정] 은 2026-09-10 결정으로 수검자 Page 에서 화면 안으로
                    // 옮겨 갔다. 여기 남기면 예약·접수 Page 의 동명 버튼을 재게 된다.
                    string[] shouldStayOpen = { "신규등록", "정보수정", "예약" };
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

        // 03 §5.2 표 — 행 미선택이면 [정보수정]·[예약]·[변경이력]이 닫히고
        // 행을 고르면 셋이 함께 열린다. [R12] 이제 이 표에 다른 축은 없다.
        [TestMethod]
        public void 행_미선택이면_정보수정_예약_변경이력이_닫힌다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    form.PatientActions = PatientActionState.None();
                    Assert.IsFalse(Enabled(form, "정보수정"));
                    Assert.IsFalse(Enabled(form, "예약"));
                    Assert.IsFalse(PatientLogEnabled(form));
                    Assert.IsTrue(Enabled(form, "신규등록"), "신규등록은 행과 무관하다");

                    form.PatientActions = new PatientActionState { RowSelected = true, Reserve = true };
                    Assert.IsTrue(Enabled(form, "정보수정"));
                    Assert.IsTrue(Enabled(form, "예약"));
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

        // 03 §9.1 — 예약 관리와 접수 관리는 화면 하나를 나눠 쓴다. Context 를 바꿔 다시 열어도
        // 판에는 하나뿐이고, 갈리는 것은 넘겨준 WorkContext 뿐이다.
        [TestMethod]
        public void Workbench_는_두_Context_가_화면_하나를_나눠_쓴다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    IMainView view = form;
                    view.OpenWorkbench(WorkContext.Reservation, null);
                    view.OpenWorkbench(WorkContext.Reception, null);

                    Control panel = BusinessPanel(form);
                    Assert.AreEqual(1, panel.Controls.Count, "Context 마다 화면이 새로 섰다");
                    Assert.IsInstanceOfType(panel.Controls[0], typeof(UcWorkbench));
                    Assert.AreEqual(DockStyle.Fill, panel.Controls[0].Dock);
                }
            });
        }

        // 03 §9.6 · §9.7 미선택 행 — 다섯 업무 Action 과 [변경이력] 이 전부 닫혀 있다.
        //
        // 예전에는 `[현장 당일예약]` 만 선택행과 무관한 독립 Action 이라 열린 채였다. 2026-09-11
        // grilling 으로 그 버튼이 사라졌다 — 예약으로 들어가는 자리는 수검자 관리의 [예약]
        // 하나이고, 일반/현장은 조작자가 아니라 시각이 가른다 (00 RP-05).
        [TestMethod]
        public void 행_미선택이면_Workbench_업무_Action_이_전부_닫힌다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    string[] rsvClosed = { "예약변경", "예약취소", "접수", "변경이력" };
                    foreach (string caption in rsvClosed)
                    {
                        Assert.IsFalse(PageItemEnabled(form, "예약 관리", caption),
                            "예약 관리 / " + caption);
                    }

                    // [예약변경]·[접수] 는 2026-09-11 에 접수 Page 에서 사라졌다 — 둘 다 `RSV`
                    // 건을 다루는 명령이라 예약 Page 에만 있다.
                    string[] rcpClosed = { "추가검사변경", "접수취소", "변경이력" };
                    foreach (string caption in rcpClosed)
                    {
                        Assert.IsFalse(PageItemEnabled(form, "접수 관리", caption),
                            "접수 관리 / " + caption);
                    }
                }
            });
        }

        // [X] VS 디자이너는 설계 대상 타입을 **매개변수 없는 생성자**로 만든다. 그것이 없으면
        //     「디자이너에 대한 문서를 로드하지 않았으므로 디자이너를 표시할 수 없습니다」로
        //     화면이 아예 열리지 않는다 — 컴파일도 시험도 통과하므로 디자이너를 열기 전까지
        //     아무도 모른다(실측 2026-09-10: MainForm · FrmPatientEditor · FrmPatientSelect(당시)
        //     셋이 그랬다). 서비스를 생성자로 받는 화면을 새로 만들 때마다 되풀이된다.
        [TestMethod]
        public void 모든_화면이_디자이너용_생성자를_갖는다()
        {
            var inspected = new List<string>();
            foreach (Type type in typeof(MainForm).Assembly.GetTypes())
            {
                bool isScreen = typeof(Form).IsAssignableFrom(type) || typeof(UserControl).IsAssignableFrom(type);
                if (type.IsAbstract || !isScreen)
                {
                    continue;
                }

                inspected.Add(type.Name);
                Assert.IsNotNull(type.GetConstructor(Type.EmptyTypes),
                    type.Name + " 에 매개변수 없는 생성자가 없다 — VS 디자이너가 이 화면을 못 연다");
            }

            // [X] 타입 필터가 틀리면 이 시험은 아무것도 안 보고 green 이 된다. Form 쪽과
            //     UserControl 쪽을 하나씩 실제로 집었는지 확인해 공허한 통과를 막는다.
            //     개수로 재지 않는다 — 화면이 늘 때마다 거짓이 되는 수치다 (ROOT AGENTS.md §6).
            CollectionAssert.Contains(inspected, "MainForm", "Form 을 하나도 못 집었다");
            CollectionAssert.Contains(inspected, "UcPatientManagement", "UserControl 을 하나도 못 집었다");
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

        /// <summary>
        /// 같은 Caption 이 Page 마다 있다 (`예약변경`·`접수`·`변경이력`). Ribbon 전체를 훑는
        /// <see cref="Enabled"/> 는 첫 번째만 집으므로, Page 를 짚어야 하는 자리는 이것을 쓴다.
        /// </summary>
        private static bool PageItemEnabled(MainForm form, string pageText, string caption)
        {
            foreach (RibbonPage page in form.Ribbon.Pages)
            {
                if (page.Text != pageText)
                {
                    continue;
                }

                foreach (RibbonPageGroup group in page.Groups)
                {
                    foreach (DevExpress.XtraBars.BarItemLink link in group.ItemLinks)
                    {
                        if (link.Item.Caption == caption)
                        {
                            return link.Item.Enabled;
                        }
                    }
                }
            }

            throw new AssertFailedException(pageText + " Page 에 " + caption + " 이 없다");
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

        // [X] 쓰지 않는 리본 크롬은 RibbonControl 을 만들면 자동으로 켜진다 — 내가 넣은 것이
        //     아니라 끄지 않았던 것이다. 배치 게이트(SCR-*)는 설계 소스에 **있는** 것만 보므로
        //     "설계에 없는데 켜진 것" 은 잡지 못한다. 그 자리를 이 시험이 맡는다.
        //
        // Application Button 은 2026-09-11 에 이 목록에서 빠졌다 — 휴무일 관리가 거기 산다.
        // 「끄지 않았던 것」이 아니라 **쓰려고 켠 것**이라 성질이 다르다 (§4.7).
        [TestMethod]
        public void 쓰지_않는_리본_크롬은_꺼져_있다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    RibbonControl ribbon = form.Ribbon;
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
            return new MainForm(
                new FakeCommonStatusService(), new FakePatientService(),
                new FakeWorkService(), new FakeReservationService(), new FakeHolidayService(), "접수1번창구", true);
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
