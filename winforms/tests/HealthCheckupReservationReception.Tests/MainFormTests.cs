using System;
using System.Threading;
using System.Windows.Forms;
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
                using (var form = new MainForm())
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
                using (var form = new MainForm())
                {
                    Assert.IsNotNull(form.Ribbon, "Ribbon 이 없다");
                    Assert.IsNotNull(form.StatusBar, "StatusBar 가 없다");
                    Assert.AreSame(form.StatusBar, form.Ribbon.StatusBar);
                    Assert.AreSame(form.Ribbon, form.StatusBar.Ribbon);
                }
            });
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
