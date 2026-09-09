using System;
using System.Windows.Forms;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception
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
