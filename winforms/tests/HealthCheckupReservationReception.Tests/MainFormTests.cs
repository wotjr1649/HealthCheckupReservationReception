using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraBars.Ribbon;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Tests.Presenters;
using HealthCheckupReservationReception.Tests.Services;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests
{
    [TestClass]
    public class MainFormTests
    {
        // 대상: MainForm (WF-00) — 글꼴과 AutoScale 기준
        // 목적: 킷 §1 의 굴림 9pt · AutoScaleMode.Font 는 Program.cs 가 정하는데 그것이
        //       디자인타임에는 돌지 않는다. 폼이 자기 Font 를 직렬화해 두지 않으면 다음 디자이너
        //       저장에서 치수가 다시 계산되어 화면 전체가 통째로 재조정된다.
        // 확인: AutoScaleMode 가 Font 이고 Font 가 굴림 9pt 다.
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

        // 대상: MainForm (WF-00) — Ribbon 과 StatusBar 의 상호 참조
        // 목적: DevExpress 는 둘을 서로 연결해야 상태바가 Ribbon 스킨을 따르고 Page 전환에
        //       반응한다. 한쪽만 걸려 있으면 상태바가 딴 모양으로 뜨거나 갱신되지 않는다.
        //       화면 제목 문자열은 여기서 비교하지 않는다 — 05 §1.1 이 단일 출처이고
        //       verify-contract-names.sh 의 CFG-007 이 Designer 와 대조한다.
        // 확인: Ribbon 과 StatusBar 가 둘 다 있고, 서로를 같은 인스턴스로 가리킨다.
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

        // 대상: MainForm (WF-00) — Ribbon Page 중 그룹이 없는 Page 의 동작
        // 목적: 2026-09-14 사용자 지시로 Page 는 모두 「가는 곳」이다. 예전에는 그룹 없는 Page 가
        //       「눌러도 아무 데도 안 가는 Page」였고 그런 것이 둘(신규 예약·휴무일 관리) 있었다 —
        //       눌리면 탭이 바뀌었다 제자리로 돌아와 예측이 되지 않았고, 사용자가 「기능 배치가
        //       중구난방」이라 보고한 자리다. 지금 휴무일 관리는 전환 자체를 취소하고 Modal 만
        //       연다. 그래서 재는 것이 「그룹이 있는가」가 아니라 「그룹이 없으면 정말 안 열리는가」다.
        // 확인: Shell 핸들이 만들어지고, 그룹 없는 Page 를 누르면 선택된 Page 가 바뀌지 않으며,
        //       그렇게 열리지 않는 문은 「휴무일 관리」 하나뿐이다.
        [TestMethod]
        public void 그룹_없는_Page_는_열리지_않는_문_하나뿐이다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    // 문은 Modal 을 BeginInvoke 로 미루므로 핸들이 있어야 한다. 시험에는
                    // 메시지 펌프가 없어 그 지연 호출이 끝내 돌지 않는다 — 창은 뜨지 않는다.
                    IntPtr handle = form.Handle;
                    Assert.AreNotEqual(IntPtr.Zero, handle, "Shell 핸들이 만들어지지 않았다");

                    RibbonPage opened = form.Ribbon.SelectedPage;
                    var doors = new List<string>();

                    foreach (RibbonPage page in form.Ribbon.Pages)
                    {
                        if (page.Groups.Count != 0)
                        {
                            continue;
                        }

                        doors.Add(page.Text);
                        form.Ribbon.SelectedPage = page;
                        Assert.AreSame(opened, form.Ribbon.SelectedPage,
                            page.Text + " 를 누르니 탭이 옮겨갔다 — 그룹이 없으면 빈 탭이 열린다");
                    }

                    CollectionAssert.AreEqual(new List<string> { "휴무일 관리" }, doors,
                        "그룹 없는 Page 는 휴무일 관리 하나다 — 나머지는 장소이므로 그룹을 갖는다");
                }
            });
        }

        // 대상: MainForm (WF-00) — 업무 Page 세 곳의 Ribbon Group 구성과 순서
        // 목적: 03 §4.2 는 [검색] → [현재 업무 Action] → [보기] 순서를 적었는데, 2026-09-10
        //       사용자 결정으로 [검색] 그룹이 Ribbon 에서 사라졌다 — [조회] 는 화면 안에 있고
        //       같은 버튼이 두 곳에 있을 이유가 없다. 남은 규칙은 「[보기] 가 있으면 그것이
        //       마지막」이다. 휴무일 관리에는 [보기] 가 없으므로(03 §24.7) 「있으면」이고,
        //       건너뛰는 조건은 곧 아무것도 재지 않는 green 으로 자라므로 훑은 Page 이름을 함께 잰다.
        // 확인: 어느 Page 에도 [검색]·[기준정보] 그룹이 없고, [보기] 가 있으면 마지막 자리다.
        //       훑은 목록에 수검자 관리·예약 관리·접수 관리 셋이 들어 있다.
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

                        // 2026-09-14 — `기준정보` 그룹이 세 Page 에서 사라졌다. 휴무일 관리는
                        // 그룹이 아니라 `접수 관리` 오른쪽 탭 자리의 문이 되었다 (03 §24.2).
                        // 그래서 `[보기]` 가 다시 마지막이다.
                        var order = new List<string>();
                        foreach (RibbonPageGroup group in page.Groups)
                        {
                            order.Add(group.Text);
                        }

                        Assert.AreEqual(-1, order.IndexOf("기준정보"),
                            page.Text + " 에 [기준정보] 가 남아 있다 — 휴무일 관리는 탭 자리 하나다");
                        Assert.AreEqual(order.Count - 1, order.IndexOf("보기"),
                            page.Text + " 의 [보기] 는 마지막 그룹이다");
                    }

                    // 아무 Page 도 집지 못하면 위 단언이 한 번도 돌지 않는다.
                    CollectionAssert.Contains(inspected, "수검자 관리", "수검자 관리 Page 를 못 집었다");
                    CollectionAssert.Contains(inspected, "예약 관리", "예약 관리 Page 를 못 집었다");
                    CollectionAssert.Contains(inspected, "접수 관리", "접수 관리 Page 를 못 집었다");
                }
            });
        }

        // 대상: MainForm (WF-00) — 공통 업무상태가 업무불가일 때의 Ribbon Action
        // 목적: R12 가 되돌린 자리다. 00 §1.1 · 03 §1.3·§5.2·§9.6·§9.7 에 따라 공통 업무불가는
        //       Action 을 닫지 않는다 — 화면이 미리 닫으면 조작자가 저장 시점의 DB 판정(사유가
        //       담긴 결과코드)을 받아볼 길이 사라진다.
        // 확인: 업무불가 상태에서도 업무 Action 버튼들과 [변경이력] 이 열려 있다.
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

                using (var form = new MainForm(service, new FakePatientService(), new FakeWorkService(), new FakeReservationService(), new FakeHolidayService(), new FakeChangeLogService(), "접수1번창구", true))
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

        // 대상: MainForm (WF-00) — 수검자 관리 Page 의 Action 과 Grid 선택의 연동
        // 목적: 03 §5.2 표대로 대상이 없는 Action 은 닫혀 있어야 한다. 열려 있으면 직전에 고른
        //       사람에게 정보수정·예약이 나간다. [신규등록] 만은 행과 무관하다.
        // 확인: 행 미선택이면 [정보수정]·[예약]·[변경이력] 이 닫히고 [신규등록] 은 열려 있다.
        //       행을 고르면 셋이 함께 열린다.
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

        // 대상: MainForm (WF-00) — 업무 화면을 담는 PanelControl
        // 목적: 2026-09-10 사용자 결정으로 업무 화면은 한 번에 하나만 뜬다 (Tab 스트립을 걷었다).
        //       같은 화면을 다시 불렀을 때 판에 겹쳐 쌓이면 메모리에 화면이 쌓이고 이벤트가
        //       중복 발화한다.
        // 확인: 판의 자식이 정확히 1개이고 그것이 UcPatientManagement 이며 Dock=Fill 이다.
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

        // 대상: MainForm (WF-00) — 예약 관리·접수 관리 두 Page 가 같은 UcWorkbench 를 쓰는 구조
        // 목적: 03 §9.1 에서 두 창구는 화면 하나를 나눠 쓴다. Context 마다 화면을 새로 세우면
        //       같은 화면이 둘 쌓이고, 조회조건과 선택이 창구 사이에서 새어 나간다.
        // 확인: Context 를 바꿔 다시 열어도 판의 자식이 1개이고 UcWorkbench 이며 Dock=Fill 이다.
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

        // 대상: MainForm (WF-00) — 예약·접수 두 Page 의 업무 Action 과 Grid 선택의 연동
        // 목적: 03 §9.6·§9.7 미선택 행 규칙이다. 예전에는 [현장 당일예약] 만 선택행과 무관한
        //       독립 Action 이라 열린 채였는데, 2026-09-11 grilling 으로 그 버튼이 사라졌다 —
        //       예약으로 들어가는 자리는 수검자 관리의 [예약] 하나이고, 일반/현장은 조작자가
        //       아니라 시각이 가른다 (00 RP-05).
        // 확인: 행 미선택이면 두 Page 모두에서 다섯 업무 Action 과 [변경이력] 이 전부 닫혀 있다.
        [TestMethod]
        public void 행_미선택이면_Workbench_업무_Action_이_전부_닫힌다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    string[] rsvClosed = { "예약변경", "예약취소", "변경이력" };
                    foreach (string caption in rsvClosed)
                    {
                        Assert.IsFalse(PageItemEnabled(form, "예약 관리", caption),
                            "예약 관리 / " + caption);
                    }

                    // 2026-09-11 — 탭은 창구다. `[접수]` 는 접수 Page 에 있고 `[예약변경]` 은
                    // 예약 Page 에 있다. 접수 Page 의 목록이 오늘의 `RSV` 를 담으므로 접수할
                    // 대상이 그 탭에 있다.
                    string[] rcpClosed = { "접수", "추가검사변경", "접수취소", "변경이력" };
                    foreach (string caption in rcpClosed)
                    {
                        Assert.IsFalse(PageItemEnabled(form, "접수 관리", caption),
                            "접수 관리 / " + caption);
                    }
                }
            });
        }

        // 대상: 어셈블리의 Form·UserControl 전부 — 매개변수 없는 생성자
        // 목적: VS 디자이너는 설계 대상 타입을 매개변수 없는 생성자로 만든다. 그것이 없으면
        //       「디자이너를 표시할 수 없습니다」로 화면이 아예 열리지 않는데, 컴파일도 시험도
        //       통과하므로 디자이너를 열기 전까지 아무도 모른다 (2026-09-10 실측: MainForm ·
        //       FrmPatientEditor 등 셋이 그랬다). 서비스를 생성자로 받는 화면을 새로 만들 때마다
        //       되풀이되는 부류다.
        // 확인: 모든 화면 타입에 매개변수 없는 생성자가 있다. 훑은 목록에 Form 과 UserControl 이
        //       둘 다 들어 있어 검사가 헛돌지 않았음을 함께 잰다.
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

        // 대상: MainForm (WF-00) — RibbonControl 이 기본으로 켜 두는 부가 UI
        // 목적: 쓰지 않는 리본 크롬은 RibbonControl 을 만들면 자동으로 켜진다 — 넣은 것이 아니라
        //       끄지 않은 것이다. 배치 게이트(SCR-*)는 설계 소스에 있는 것만 보므로 「설계에
        //       없는데 켜진 것」은 잡지 못하고, 그 자리를 이 시험이 맡는다. 켜져 있으면 조작자가
        //       리본을 접거나 도구모음을 바꿔 놓고 되돌리는 길을 모른다.
        // 확인: Application Button · 표시옵션 메뉴 · 확장/축소 버튼이 모두 False 이고, 빠른 실행
        //       도구모음이 Hidden 이며, 도구모음 사용자 지정·리본 최소화 경로가 막혀 있고
        //       리본이 펼친 상태로 고정이다.
        [TestMethod]
        public void 쓰지_않는_리본_크롬은_꺼져_있다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    RibbonControl ribbon = form.Ribbon;
                    Assert.AreEqual(DefaultBoolean.False, ribbon.ShowApplicationButton,
                        "Application Button");
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

        // 대상: MainForm (WF-00) — Ribbon 버튼 전부의 아이콘
        // 목적: 2026-09-15 사용자 요청으로 버튼에 아이콘을 넣었다. 아이콘은 리소스 이름으로
        //       찾아 붙이므로 이름이 한 글자 틀리면 그 버튼만 조용히 글자만 남는다 —
        //       Designer 가 직렬화하는 값이 아니라 코드가 붙이는 값이라, 화면을 열어 보기
        //       전에는 아무도 모른다. 버튼을 새로 더하면서 아이콘을 빼먹는 것도 같은 모양으로
        //       지나간다.
        // 확인: Ribbon 의 모든 BarButtonItem 이 SvgImage 를 갖는다. 훑은 버튼이 0개이면
        //       아무것도 재지 않은 green 이므로 그것도 실패로 센다.
        [TestMethod]
        public void 리본_버튼은_전부_아이콘을_갖는다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    var bare = new List<string>();
                    int seen = 0;
                    foreach (RibbonPage page in form.Ribbon.Pages)
                    {
                        foreach (RibbonPageGroup group in page.Groups)
                        {
                            foreach (BarItemLink link in group.ItemLinks)
                            {
                                var button = link.Item as BarButtonItem;
                                if (button == null)
                                {
                                    continue;
                                }

                                seen++;
                                if (button.ImageOptions.SvgImage == null)
                                {
                                    bare.Add(page.Text + " / " + button.Caption);
                                }
                            }
                        }
                    }

                    Assert.AreNotEqual(0, seen, "Ribbon 에서 버튼을 하나도 찾지 못했다 — 아무것도 재지 않았다");
                    Assert.AreEqual(0, bare.Count, "아이콘 없는 버튼: " + string.Join(", ", bare));
                }
            });
        }

        // 대상: DLG-HOL-01 (FrmHoliday) — 대화상자 버튼의 아이콘
        // 목적: 2026-09-15 사용자가 물어서 드러난 자리다. 리본에만 아이콘을 넣어 두어
        //       대화상자는 글자만 남아 있었고, 창구에서는 절반만 바뀐 화면으로 보인다.
        //       휴무일 관리는 버튼이 다섯으로 가장 많아 하나를 빠뜨리기도 가장 쉽다.
        // 확인: 폼 안의 SimpleButton 전부가 SvgImage 를 갖는다. 훑은 버튼이 0개이면
        //       아무것도 재지 않은 green 이므로 그것도 실패로 센다.
        [TestMethod]
        public void 대화상자_버튼도_아이콘을_갖는다()
        {
            RunSta(() =>
            {
                using (var form = new FrmHoliday(new FakeHolidayService(), new FakeCommonStatusService()))
                {
                    var bare = new List<string>();
                    int seen = Walk(form, bare);

                    Assert.AreNotEqual(0, seen, "대화상자에서 버튼을 하나도 찾지 못했다 — 아무것도 재지 않았다");
                    Assert.AreEqual(0, bare.Count, "아이콘 없는 버튼: " + string.Join(", ", bare));
                }
            });
        }

        /// <summary>담은 컨트롤까지 내려가며 센다. 버튼은 LayoutControl 안에 있다.</summary>
        private static int Walk(Control parent, List<string> bare)
        {
            int seen = 0;
            foreach (Control child in parent.Controls)
            {
                var button = child as SimpleButton;
                if (button != null)
                {
                    seen++;
                    if (button.ImageOptions.SvgImage == null)
                    {
                        bare.Add(button.Name + " (" + button.Text + ")");
                    }
                }

                seen += Walk(child, bare);
            }

            return seen;
        }

        private static MainForm NewShell()
        {
            return new MainForm(
                new FakeCommonStatusService(), new FakePatientService(),
                new FakeWorkService(), new FakeReservationService(), new FakeHolidayService(), new FakeChangeLogService(), "접수1번창구", true);
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
