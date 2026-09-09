using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Tests.Presenters;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Visual
{
    /// <summary>
    /// 화면을 PNG 로 떠서 배치를 눈으로 볼 수 있게 한다.
    ///
    /// [X] VS 디자인 표면은 실행 화면이 아니다 (references/designer.md). Program.cs 와
    ///     Presenter 가 디자인타임에 돌지 않아 글꼴·업무 Tab·상태바가 나타나지 않는다.
    ///     그래서 배치를 디자이너로 판정할 수 없고, 실제로 그 때문에 기동 결함을 놓쳤다
    ///     (07 §12.3).
    ///
    /// [I] 여기서 뜨는 것은 **결정적**이다 — DB 에 붙지 않고 fake 로 상태를 고정한다.
    ///     실물 DB 를 거친 화면이 필요하면 tools/capture-shell.ps1 이 그것을 한다.
    ///
    /// 산출 위치는 winforms/artifacts/logs/ 다. .gitignore 대상이라 manifest 를 흔들지 않는다.
    /// </summary>
    [TestClass]
    public class ShellCaptureTests
    {
        [TestMethod]
        [TestCategory("Visual")]
        public void WF00_실행_화면을_PNG_로_뜬다()
        {
            string path = null;
            RunSta(() =>
            {
                // Program.cs 는 시험에서 돌지 않는다. 글꼴 기준을 여기서 같게 맞춘다 (킷 §1).
                WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);
                WindowsFormsSettings.DefaultMenuFont = new Font("굴림", 9F);

                var service = new FakeCommonStatusService
                {
                    Result = OperationResult<CommonWorkStatusDto>.Success(new CommonWorkStatusDto
                    {
                        Today = new DateTime(2026, 9, 9),
                        DayName = "수요일",
                        OpenTime = new TimeSpan(9, 0, 0),
                        CloseTime = new TimeSpan(18, 0, 0),
                        IsBusinessDay = true,
                        IsWithinHours = true,
                        IsWorkAllowed = true,
                        BlockCode = (int)DbCode.Ok,
                        BlockMessage = string.Empty,
                    }),
                };

                using (var form = new MainForm(service, "접수1번창구"))
                {
                    // 화면 밖에 띄운다. 보이지 않으면 DevExpress 가 스킨을 그리지 않는다.
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-32000, -32000);
                    form.Show();
                    Application.DoEvents();

                    using (var bmp = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
                    {
                        form.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size));
                        path = Save(bmp, "wf00_shell.png");
                    }

                    form.Close();
                }
            });

            Assert.IsNotNull(path, "PNG 를 쓰지 못했다");
            var info = new FileInfo(path);
            Assert.IsTrue(info.Exists && info.Length > 10 * 1024,
                "PNG 가 비었거나 너무 작다 — 폼이 그려지지 않았다: " + path + " (" + (info.Exists ? info.Length : 0) + " bytes)");
            Console.WriteLine("캡처: " + path);
        }

        private static string Save(Bitmap bmp, string name)
        {
            // bin/Debug 에서 winforms/ 까지 올라간다.
            string dir = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "artifacts", "logs"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name);
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            return path;
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
