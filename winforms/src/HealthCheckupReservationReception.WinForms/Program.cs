using System;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using HealthCheckupReservationReception.Repositories;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception
{
    internal static class Program
    {
        // 05 §1.1 이 확정한 연결문자열 키. App.config 와 이 상수가 같은지는
        // scripts/verify-contract-names.sh 가 05 §1.1 을 파싱해 대조한다.
        private const string ConnectionName = "HealthCheckupDb";

        // 03 §1.3 — 조작자는 설정 파일에서 읽어 읽기 전용으로 표시하고,
        // Write SP 호출 때 @조작자명 으로 전달한다. 입력 Control 이 아니다.
        private const string OperatorSettingName = "OperatorName";
        private const string MoveAfterSaveSettingName = "MoveToReceptionAfterSave";

        /// <summary>
        /// 2026-09-11 사용자 지시 — 저장 뒤 화면을 옮길지는 창구마다 다르다.
        ///
        /// [X] **못 읽으면 켬이다.** 설정을 지우거나 오타를 냈다고 동작이 조용히 바뀌면,
        ///     현장 내원자를 접수하려는 창구가 이유 없이 탭을 옮기게 된다.
        /// </summary>
        private static bool MoveAfterSave()
        {
            string value = ConfigurationManager.AppSettings[MoveAfterSaveSettingName];
            bool parsed;
            return !bool.TryParse(value, out parsed) || parsed;
        }

        [STAThread]
        private static void Main()
        {
            // SetPerMonitorDpiAware 는 Main 의 첫 문장이어야 한다 (킷 §1).
            // app.manifest 에는 DPI 설정을 두지 않는다 — 두 곳이 서로 다른 말을 하게 된다.
            WindowsFormsSettings.SetPerMonitorDpiAware();

            // UI 기준 글꼴. 여기 한 번만 정하고 폼은 Designer 에 자기 Font 를 직렬화한다
            // (킷 §1 · references/designer.md 함정 9 — Program.cs 는 디자인타임에 돌지 않는다).
            WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);
            WindowsFormsSettings.DefaultMenuFont = new Font("굴림", 9F);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ConnectionStringSettings connection = ConfigurationManager.ConnectionStrings[ConnectionName];
            if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionString))
            {
                // 화면 표시명을 여기 적지 않는다 — 05 §1.1 의 값이 Designer 와 두 곳에 있게 된다.
                XtraMessageBox.Show("App.config 에 연결문자열 '" + ConnectionName + "' 이 없습니다.");
                return;
            }

            ICommonStatusService statusService = new CommonStatusService(
                new CommonStatusRepository(connection.ConnectionString));
            IPatientService patientService = new PatientService(
                new PatientRepository(connection.ConnectionString));
            IWorkService workService = new WorkService(
                new WorkRepository(connection.ConnectionString));
            IReservationService reservationService = new ReservationService(
                new ReservationRepository(connection.ConnectionString));
            IHolidayService holidayService = new HolidayService(
                new HolidayRepository(connection.ConnectionString));

            Application.Run(new MainForm(
                statusService,
                patientService,
                workService,
                reservationService,
                holidayService,
                ConfigurationManager.AppSettings[OperatorSettingName],
                MoveAfterSave()));
        }
    }
}
