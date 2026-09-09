using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception
{
    internal static class Program
    {
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
            Application.Run(new MainForm());
        }
    }
}
