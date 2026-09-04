using System;
using System.Windows.Forms;
using HealthCheckupReservationReception.WinForms.Views;

namespace HealthCheckupReservationReception.WinForms
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
