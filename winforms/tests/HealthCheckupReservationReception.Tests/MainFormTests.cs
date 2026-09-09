using System;
using System.Threading;
using System.Windows.Forms;
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

        // 03 §4.1 상단 업무 Navigation 다섯. 하나라도 빠지면 Shell 이 아니다.
        [TestMethod]
        public void MainForm_은_업무_Navigation_다섯을_갖는다()
        {
            RunSta(() =>
            {
                using (MainForm form = NewShell())
                {
                    Assert.AreEqual(1, form.Ribbon.Pages.Count);
                    Assert.AreEqual("업무", form.Ribbon.Pages[0].Text);
                    Assert.AreEqual(1, form.Ribbon.Pages[0].Groups.Count);
                    Assert.AreEqual(5, form.Ribbon.Pages[0].Groups[0].ItemLinks.Count);
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
