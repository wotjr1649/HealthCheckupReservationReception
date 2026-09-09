using System;
using System.Threading;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests
{
    [TestClass]
    public class MainFormTests
    {
        [TestMethod]
        public void MainForm_생성_시_제목이_설정된다()
        {
            RunSta(() =>
            {
                using (var form = new MainForm())
                {
                    Assert.AreEqual("건강검진 예약·접수", form.Text);
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
