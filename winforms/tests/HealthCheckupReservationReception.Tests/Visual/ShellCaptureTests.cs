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

                using (var form = new MainForm(service, new FakePatientService(), new FakeWorkService(), "접수1번창구"))
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
        /// WF-WRK-01 을 목록·상세가 찬 상태로 뜬다. 빈 화면으로는 조회 한 줄의 칸 폭도
        /// 좌우 비율도 볼 수 없다 — 종료일 DateEdit 이 잘린 것이 실제로 그렇게 드러났다.
        /// </summary>
        [TestMethod]
        [TestCategory("Visual")]
        public void WFWRK01_실행_화면을_PNG_로_뜬다()
        {
            string path = null;
            RunSta(() =>
            {
                WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);
                WindowsFormsSettings.DefaultMenuFont = new Font("굴림", 9F);

                var screen = new UcWorkbench();
                screen.Attach(new FakeWorkService());

                using (var host = new Form())
                {
                    host.StartPosition = FormStartPosition.Manual;
                    host.Location = new Point(-32000, -32000);
                    host.ClientSize = new Size(1916, 887);
                    screen.Dock = DockStyle.Fill;
                    host.Controls.Add(screen);
                    host.Show();
                    Application.DoEvents();

                    IWorkbenchView view = screen;
                    view.Rows = SampleWorkRows();
                    view.Detail = SampleWorkDetail();
                    view.NexItems = SampleNex();
                    view.AexItems = SampleAex();
                    Application.DoEvents();

                    using (var bmp = new Bitmap(host.ClientSize.Width, host.ClientSize.Height))
                    {
                        host.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size));
                        path = Save(bmp, "wf_wrk_01.png");
                    }

                    host.Close();
                }
            });

            var info = new FileInfo(path);
            Assert.IsTrue(info.Exists && info.Length > 10 * 1024,
                "PNG 가 비었거나 너무 작다: " + path + " (" + (info.Exists ? info.Length : 0) + " bytes)");
            Console.WriteLine("캡처: " + path);
        }

        private static IList<WorkListItemDto> SampleWorkRows()
        {
            return new List<WorkListItemDto>
            {
                new WorkListItemDto { WorkId = 1, PatientId = 1000, ReserveDate = new DateTime(2026, 9, 11), SlotCode = "AM", StatusCode = "RSV", StatusName = "예약", Name = "홍길동", ChartNo = "C000001", Gender = "M", Birthday = "19800101", MobilePhone = "01012345678" },
                new WorkListItemDto { WorkId = 2, PatientId = 1003, ReserveDate = new DateTime(2026, 9, 11), SlotCode = "PM", StatusCode = "RCP", StatusName = "접수완료", Name = "테스트01", ChartNo = "C000006", Gender = "F", Birthday = "19560819", MobilePhone = "01098765432" },
                new WorkListItemDto { WorkId = 3, PatientId = 1004, ReserveDate = new DateTime(2026, 9, 14), SlotCode = "PM", StatusCode = "CNR", StatusName = "예약취소", Name = "시험수검자960553", ChartNo = "C000008", Gender = "F", Birthday = "19990707", MobilePhone = null },
            };
        }

        private static WorkDetailDto SampleWorkDetail()
        {
            return new WorkDetailDto
            {
                WorkId = 1,
                PatientId = 1000,
                ChartNo = "C000001",
                Name = "홍길동",
                Birthday = "19800101",
                Gender = "M",
                MobilePhone = "01012345678",
                ReserveDate = new DateTime(2026, 9, 11),
                SlotCode = "AM",
                StatusCode = "RSV",
                StatusName = "예약",
                Capacity = 20,
                CurrentCount = 12,
                RemainingSeats = 8,
                RowVersion = new byte[8],
            };
        }

        private static IList<WorkExamItemDto> SampleNex()
        {
            return new List<WorkExamItemDto>
            {
                new WorkExamItemDto { ExamItemCode = "E01", ExamItemName = "문진/진찰", NexType = "기본" },
                new WorkExamItemDto { ExamItemCode = "E02", ExamItemName = "신체계측", NexType = "기본" },
                new WorkExamItemDto { ExamItemCode = "E03", ExamItemName = "혈압측정", NexType = "기본" },
                new WorkExamItemDto { ExamItemCode = "E09", ExamItemName = "골밀도검사", NexType = "조건부" },
            };
        }

        private static IList<WorkExamItemDto> SampleAex()
        {
            return new List<WorkExamItemDto>
            {
                new WorkExamItemDto { AexCode = "OPT01", ExamItemCode = "E11", ExamItemName = "복부초음파" },
            };
        }

        /// <summary>
        /// DLG-PAT-01 New Mode. 설계 `dlg_pat_01.js` 와 견주려면 구획 다섯이 한 화면에
        /// 들어가는지가 먼저다 — 값이 비어 있어도 배치는 보인다.
        /// </summary>
        [TestMethod]
        [TestCategory("Visual")]
        public void DLGPAT01_실행_화면을_PNG_로_뜬다()
        {
            string path = Capture("dlg_pat_01.png", () =>
                new FrmPatientEditor(new FakePatientService(), "접수1번창구", null));

            Assert.IsTrue(new FileInfo(path).Length > 2 * 1024, "PNG 가 비었다: " + path);
            Console.WriteLine("캡처: " + path);
        }

        /// <summary>DLG-PAT-02. 조회 결과가 들어 있어야 컬럼 폭을 볼 수 있다.</summary>
        [TestMethod]
        [TestCategory("Visual")]
        public void DLGPAT02_실행_화면을_PNG_로_뜬다()
        {
            string path = Capture("dlg_pat_02.png", () =>
            {
                var form = new FrmPatientSelect(new FakePatientService(), "접수1번창구");
                ((IPatientSelectView)form).Rows = SampleRows();
                return form;
            });

            Assert.IsTrue(new FileInfo(path).Length > 2 * 1024, "PNG 가 비었다: " + path);
            Console.WriteLine("캡처: " + path);
        }

        /// <summary>
        /// DLG-PAT-03. 설계 `dlg_pat_03.js` 는 후보 둘의 주민번호가 서로 달라야 한다고 적는다 —
        /// 이 화면의 존재 이유가 `이름 + 생년월일 동일 / 주민번호 상이` 이기 때문이다.
        /// </summary>
        [TestMethod]
        [TestCategory("Visual")]
        public void DLGPAT03_실행_화면을_PNG_로_뜬다()
        {
            string path = Capture("dlg_pat_03.png", () =>
            {
                var form = new FrmPatientDuplicate();
                form.Bind(
                    Candidate(0, "", "홍길동", "6603122000019", "010-0000-0003"),
                    new List<PatientSaveResultDto>
                    {
                        Candidate(21, "2026-000098", "홍길동", "6603122000035", "010-0000-0098"),
                        Candidate(22, "2026-000114", "홍길동", "6603122000043", "010-0000-0114"),
                    });
                return form;
            });

            Assert.IsTrue(new FileInfo(path).Length > 2 * 1024, "PNG 가 비었다: " + path);
            Console.WriteLine("캡처: " + path);
        }

        private static PatientSaveResultDto Candidate(long id, string chartNo, string name, string social, string mobile)
        {
            return new PatientSaveResultDto
            {
                PatientId = id,
                ChartNo = chartNo,
                Name = name,
                SocialNumber = social,
                Birthday = "19660312",
                Gender = "F",
                MobilePhone = mobile,
            };
        }

        /// <summary>
        /// Modal 하나를 화면 밖에 띄워 PNG 로 뜬다. 보이지 않으면 DevExpress 가 스킨을 그리지
        /// 않으므로 닫기 전에 그린다.
        /// </summary>
        private static string Capture(string name, Func<Form> create)
        {
            string path = null;
            RunSta(() =>
            {
                WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);
                WindowsFormsSettings.DefaultMenuFont = new Font("굴림", 9F);

                using (Form form = create())
                {
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-32000, -32000);
                    form.Show();
                    Application.DoEvents();

                    using (var bmp = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size));
                        path = Save(bmp, name);
                    }

                    form.Close();
                }
            });

            return path;
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
