using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraLayout;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// **모든 화면이 LayoutControl 로 배치한다** (2026-09-11 사용자 지시).
    ///
    /// 이것을 문장으로만 두면 썩는다 — 새 화면 하나가 절대좌표로 들어와도 아무도 모른다.
    /// 그래서 어셈블리의 화면을 전부 만들어 **컨트롤 하나하나가 어떻게 자리를 얻는지** 본다.
    ///
    /// 자리를 얻는 길은 셋뿐이다:
    ///
    /// <list type="number">
    /// <item>LayoutControl 안에 있다 — 배치를 LayoutControl 이 한다</item>
    /// <item>Dock 이 None 이 아니다 — 도킹이 배치를 한다 (Shell 의 Ribbon·StatusBar·업무 판)</item>
    /// <item>PopupContainerControl 이다 — 뜰 때 팝업 창으로 옮겨 가므로 Location 에 뜻이 없다</item>
    /// </list>
    ///
    /// 그 밖은 절대좌표다. 화면마다 면제 목록을 두지 않는다 — 목록은 손으로 관리해야 하고
    /// 그 자체가 또 썩는다 (ROOT AGENTS.md §6).
    /// </summary>
    [TestClass]
    public class LayoutBaselineTests
    {
        [TestMethod]
        public void 모든_화면이_LayoutControl_이나_Dock_으로만_자리를_잡는다()
        {
            RunSta(() =>
            {
                var inspected = new List<string>();
                var offenders = new List<string>();

                foreach (Type type in Screens())
                {
                    inspected.Add(type.Name);

                    // 디자이너용 생성자다 — 서비스도 Presenter 도 만들지 않는다.
                    using (var screen = (Control)Activator.CreateInstance(type))
                    {
                        Walk(type.Name, screen, offenders);
                    }
                }

                // [X] 타입 필터가 틀리면 이 시험은 아무것도 안 보고 green 이 된다.
                //     Form 쪽과 UserControl 쪽을 하나씩 실제로 집었는지 확인한다.
                CollectionAssert.Contains(inspected, "MainForm", "Form 을 하나도 못 집었다");
                CollectionAssert.Contains(inspected, "UcPatientManagement", "UserControl 을 하나도 못 집었다");

                Assert.AreEqual(0, offenders.Count,
                    "LayoutControl 밖에서 절대좌표로 놓인 컨트롤이 있다:" + Environment.NewLine
                    + string.Join(Environment.NewLine, offenders.ToArray()));
            });
        }

        /// <summary>
        /// 배치가 화면 폭을 넘으면 LayoutControl 안에 가로 스크롤이 서고, 오른쪽 끝의 버튼들이
        /// 밖으로 밀린다.
        ///
        /// [X] **이 부류를 세 번 밟았다** — WF-WRK-01 의 종료일 잘림, WF-PAT-01 의 조회조건
        ///     다섯 넘침, DLG-PAT-03 의 가로 스크롤. 눈으로는 기본 상태에서만 보이고 캡처도
        ///     늘 뜨지 않는다. 그래서 실제로 띄워서 잰다.
        /// </summary>
        [TestMethod]
        public void 어느_화면도_배치가_가로로_넘치지_않는다()
        {
            RunSta(() =>
            {
                var offenders = new List<string>();

                foreach (Type type in Screens())
                {
                    var screen = (Control)Activator.CreateInstance(type);
                    LayoutControl layout = FindLayout(screen);
                    if (layout == null)
                    {
                        // Shell 은 Ribbon·StatusBar·업무 판을 Dock 으로만 놓는다.
                        screen.Dispose();
                        continue;
                    }

                    Show(screen, delegate
                    {
                        // [X] 넘쳤다는 것은 **가로 스크롤이 실제로 섰다**는 것이다. 자식의
                        //     Right 를 재는 것으로는 못 잡는다 — LayoutControl 이 컨트롤을
                        //     이미 좁혀 놓고 스크롤바만 세우기 때문이다.
                        foreach (Control child in layout.Controls)
                        {
                            var bar = child as DevExpress.XtraEditors.HScrollBar;
                            if (bar != null && bar.Visible)
                            {
                                offenders.Add("    " + type.Name + " · 가로 스크롤이 섰다 (자리 "
                                    + layout.ClientSize.Width + "px)");
                            }
                        }
                    });
                }

                Assert.AreEqual(0, offenders.Count,
                    "배치가 가로로 넘쳤다:" + Environment.NewLine
                    + string.Join(Environment.NewLine, offenders.ToArray()));
            });
        }

        /// <summary>Form 은 남의 자식이 될 수 없다 — 제 크기로 띄우고, UserControl 만 집에 담는다.</summary>
        private static void Show(Control screen, Action inspect)
        {
            var form = screen as Form;
            if (form != null)
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-32000, -32000);
                form.Show();
                Application.DoEvents();
                inspect();
                form.Close();
                form.Dispose();
                return;
            }

            using (var host = new Form())
            {
                host.StartPosition = FormStartPosition.Manual;
                host.Location = new Point(-32000, -32000);
                host.ClientSize = screen.Size;
                screen.Dock = DockStyle.Fill;
                host.Controls.Add(screen);
                host.Show();
                Application.DoEvents();
                inspect();
                host.Close();
            }
        }

        private static LayoutControl FindLayout(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                var layout = child as LayoutControl;
                if (layout != null)
                {
                    return layout;
                }
            }

            return null;
        }

        private static IEnumerable<Type> Screens()
        {
            foreach (Type type in typeof(MainForm).Assembly.GetTypes())
            {
                bool isScreen = typeof(Form).IsAssignableFrom(type) || typeof(UserControl).IsAssignableFrom(type);
                if (!type.IsAbstract && isScreen && type.GetConstructor(Type.EmptyTypes) != null)
                {
                    yield return type;
                }
            }
        }

        private static void Walk(string screen, Control parent, IList<string> offenders)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is LayoutControl || child is PopupContainerControl)
                {
                    // LayoutControl 안쪽은 그것이 배치하므로 더 들어가지 않는다.
                    // 팝업 컨테이너는 제 안을 Dock 으로 채우므로 그 안만 이어서 본다.
                    if (child is PopupContainerControl)
                    {
                        Walk(screen, child, offenders);
                    }

                    continue;
                }

                // [X] DevExpress 컨트롤은 제 스크롤바 같은 자식을 **실행 중에** 스스로 만든다.
                //     그것들은 이름이 없다 — Designer 가 만든 것은 반드시 Name 을 갖는다.
                //     내가 놓지 않은 컨트롤을 절대좌표라고 부르면 이 시험은 늘 red 가 된다.
                if (child.Dock == DockStyle.None && !string.IsNullOrEmpty(child.Name))
                {
                    offenders.Add("    " + screen + " · " + child.Name + " (" + child.GetType().Name + ")");
                }

                Walk(screen, child, offenders);
            }
        }

        private static void RunSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);
                    action();
                }
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
