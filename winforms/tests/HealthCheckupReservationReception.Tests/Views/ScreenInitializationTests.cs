using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Tests.Presenters;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// **화면이 열리면 스스로 조회하는가.**
    ///
    /// `[X]` 이 검사는 실제 결함에서 나왔다 (2026-09-11). 목록 화면 셋 중 `UcHoliday` 만
    ///      `OnLoad` 가 없었고, 초기화는 아무도 부르지 않는 `Begin(today)` 안에만 있었다.
    ///      그래서 조회기간 두 칸이 빈 채로 남아 `[조회]` 가 `100 시작일자 필수` 로 막히고,
    ///      목록이 비어 행을 못 고르니 `[수정]`·`[삭제]` 도 영영 열리지 않았다 —
    ///      **화면의 CRUD 가 전부 죽어 있었다.**
    ///
    /// `[X]` **아무 검사도 그것을 보지 않았다.** Presenter 시험은 `LoadInitial()` 을 직접
    ///      부르므로 배선을 재지 않고, 캡처 시험은 `view.Rows = ...` 로 행을 물려 넣어
    ///      죽은 화면을 건강해 보이게 만들었다. 그 둘 사이의 틈이 여기다.
    ///
    /// `[!]` **새 화면을 만들면 여기에 한 건을 더한다.** 「조회가 불렸다」만 재고 결과는 보지
    ///      않는다 — 결과를 어떻게 그리는지는 각 화면의 시험이 이미 맡는다.
    /// </summary>
    [TestClass]
    public class ScreenInitializationTests
    {
        [TestMethod]
        public void 수검자_관리는_열리면_스스로_조회한다()
        {
            var service = new FakePatientService();

            RunSta(delegate
            {
                var screen = new UcPatientManagement();
                screen.Attach(service, new FakeWorkService(), Status());
                Host(screen);
            });

            Assert.IsNotNull(service.LastRequest,
                "화면이 섰는데 SP-PAT-01 을 한 번도 부르지 않았다 — 목록이 영원히 빈다");
        }

        [TestMethod]
        public void 예약접수_관리는_열리면_스스로_조회한다()
        {
            var service = new FakeWorkService();

            RunSta(delegate
            {
                var screen = new UcWorkbench();
                screen.Attach(service, new FakeReservationService(), Status(), "접수1번창구");
                Host(screen);
            });

            Assert.IsNotNull(service.LastSearch,
                "화면이 섰는데 SP-WRK-01 을 한 번도 부르지 않았다");
        }

        [TestMethod]
        public void 휴무일_관리는_열리면_스스로_조회한다()
        {
            var service = new FakeHolidayService();

            RunSta(delegate
            {
                using (var screen = new FrmHoliday(service, Status()))
                {
                    Show(screen);
                }
            });

            Assert.AreEqual(1, service.SearchCalls,
                "모달이 떴는데 SP-HOL-01 을 부르지 않았다 — 2026-09-11 에 실제로 이랬다");
        }

        /// <summary>
        /// 03 §24.4 — 조회기간 기본값은 **DB 오늘**부터 두 해다. PC 시계를 쓰면 창구 PC 가
        /// 하루 어긋났을 때 목록도 등재 경고도 같이 어긋난다.
        /// </summary>
        [TestMethod]
        public void 휴무일_관리의_조회기간은_DB_오늘부터_두_해다()
        {
            var service = new FakeHolidayService();

            RunSta(delegate
            {
                using (var screen = new FrmHoliday(service, Status()))
                {
                    Show(screen);
                }
            });

            Assert.IsNotNull(service.LastSearch, "조회를 부르지 않았다");
            Assert.AreEqual(Today, service.LastSearch.FromDate);
            Assert.AreEqual(Today.AddYears(2), service.LastSearch.ToDate);
        }

        private static readonly DateTime Today = new DateTime(2026, 9, 11);

        private static FakeCommonStatusService Status()
        {
            return new FakeCommonStatusService
            {
                Result = OperationResult<CommonWorkStatusDto>.Success(new CommonWorkStatusDto
                {
                    Today = Today,
                    DayName = "금요일",
                    OpenTime = new TimeSpan(9, 0, 0),
                    CloseTime = new TimeSpan(18, 0, 0),
                    IsBusinessDay = true,
                    IsWithinHours = true,
                    IsWorkAllowed = true,
                    BlockCode = (int)DbCode.Ok,
                    BlockMessage = string.Empty,
                }),
            };
        }

        /// <summary>UserControl 은 Form 에 얹어 보여야 `OnLoad` 가 돈다.</summary>
        private static void Host(Control screen)
        {
            using (var host = new Form())
            {
                screen.Dock = DockStyle.Fill;
                host.Controls.Add(screen);
                Show(host);
            }
        }

        private static void Show(Form form)
        {
            // 화면 밖에 띄운다 — 시험이 사용자의 화면을 가리지 않는다.
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-32000, -32000);
            form.Show();
            Application.DoEvents();
            form.Close();
        }

        private static void RunSta(Action action)
        {
            WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);

            Exception failure = null;
            var thread = new Thread(delegate()
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                throw new AssertFailedException("화면을 세우다 터졌다: " + failure.Message, failure);
            }
        }
    }
}
