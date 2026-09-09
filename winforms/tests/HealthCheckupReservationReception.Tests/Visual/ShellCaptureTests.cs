using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
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

                using (var form = new MainForm(service, new FakePatientService(), "접수1번창구"))
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

        /// <summary>
        /// WF-PAT-01 을 목록·상세가 찬 상태로 뜬다. 빈 화면으로는 컬럼 폭과 좌우 비율을
        /// 볼 수 없다 — 설계(wf_pat_01.js)와 견주려면 값이 들어 있어야 한다.
        /// </summary>
        [TestMethod]
        [TestCategory("Visual")]
        public void WFPAT01_실행_화면을_PNG_로_뜬다()
        {
            string path = null;
            RunSta(() =>
            {
                WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);
                WindowsFormsSettings.DefaultMenuFont = new Font("굴림", 9F);

                var screen = new UcPatientManagement();
                screen.Attach(new FakePatientService());

                using (var host = new Form())
                {
                    host.StartPosition = FormStartPosition.Manual;
                    host.Location = new Point(-32000, -32000);
                    host.ClientSize = new Size(1916, 887);
                    screen.Dock = DockStyle.Fill;
                    host.Controls.Add(screen);
                    host.Show();
                    Application.DoEvents();

                    // 실행 순서 그대로다 — 화면이 먼저 서고 그 다음 조회 결과가 들어온다.
                    // 순서를 뒤집으면 Grid 가 핸들을 만들 때 자기 마음대로 0행을 잡는다.
                    IPatientManagementView view = screen;
                    view.Rows = SampleRows();
                    view.Detail = SampleDetail();
                    Application.DoEvents();

                    using (var bmp = new Bitmap(host.ClientSize.Width, host.ClientSize.Height))
                    {
                        host.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size));
                        path = Save(bmp, "wf_pat_01.png");
                    }

                    host.Close();
                }
            });

            var info = new FileInfo(path);
            Assert.IsTrue(info.Exists && info.Length > 10 * 1024,
                "PNG 가 비었거나 너무 작다: " + path + " (" + (info.Exists ? info.Length : 0) + " bytes)");
            Console.WriteLine("캡처: " + path);
        }

        /// <summary>
        /// 03 §18 Column Chooser. 체크박스 목록과 `기본값 복원` 이 실제로 어떻게 보이는지는
        /// 시험이 아니라 이것으로만 알 수 있다.
        /// </summary>
        [TestMethod]
        [TestCategory("Visual")]
        public void 컬럼설정_창을_PNG_로_뜬다()
        {
            string path = null;
            RunSta(() =>
            {
                WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);

                var screen = new UcPatientManagement();
                using (var chooser = new FrmColumnChooser())
                {
                    chooser.Bind(GridOf(screen), delegate { });
                    chooser.StartPosition = FormStartPosition.Manual;
                    chooser.Location = new Point(-32000, -32000);
                    chooser.Show();
                    Application.DoEvents();

                    using (var bmp = new Bitmap(chooser.Width, chooser.Height))
                    {
                        chooser.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size));
                        path = Save(bmp, "wf_pat_01_columns.png");
                    }

                    chooser.Close();
                }
            });

            var info = new FileInfo(path);
            Assert.IsTrue(info.Exists && info.Length > 2 * 1024, "PNG 가 비었다: " + path);
            Console.WriteLine("캡처: " + path);
        }

        private static DevExpress.XtraGrid.Views.Grid.GridView GridOf(UcPatientManagement screen)
        {
            return (DevExpress.XtraGrid.Views.Grid.GridView)typeof(UcPatientManagement)
                .GetField("gvPatientList", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(screen);
        }

        // 설계 wf_pat_01.js 의 예시 행 그대로다. 계약이 아니라 눈으로 견주기 위한 값이다.
        private static IList<PatientListItemDto> SampleRows()
        {
            return new List<PatientListItemDto>
            {
                Row(1, "2026-000121", "수검자1", "19800511", "M", "010-0000-0001"),
                Row(2, "2026-000122", "수검자2", "19721103", "F", "010-0000-0002"),
                Row(3, "2026-000123", "홍길동", "19660312", "F", "010-0000-0003"),
                Row(4, "2026-000124", "수검자4", "19950827", "M", "010-0000-0004"),
            };
        }

        private static PatientListItemDto Row(long id, string chartNo, string name, string birthday, string gender, string mobile)
        {
            return new PatientListItemDto
            {
                PatientId = id,
                ChartNo = chartNo,
                Name = name,
                Birthday = birthday,
                Gender = gender,
                MobilePhone = mobile,
                SocialNumber = birthday.Substring(2) + (gender == "M" ? "1000019" : "2000019"),
            };
        }

        private static PatientDetailDto SampleDetail()
        {
            return new PatientDetailDto
            {
                PatientId = 3,
                ChartNo = "2026-000123",
                Name = "홍길동",
                SocialNumber = "6603122000019",
                Birthday = "19660312",
                Gender = "F",
                MobilePhone = "010-0000-0003",
            };
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
