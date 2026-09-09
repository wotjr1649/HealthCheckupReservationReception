using System;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;

namespace HealthCheckupReservationReception.Views
{
    public partial class MainForm : RibbonForm, IMainView
    {
        private readonly MainPresenter _presenter;

        public MainForm(ICommonStatusService service, string operatorName)
        {
            InitializeComponent();
            _presenter = new MainPresenter(this, service, operatorName);
        }

        public event EventHandler ShellLoaded;
        public event EventHandler<BusinessNavigation> NavigationRequested;

        public string WorkStatusText
        {
            set { barStaticWorkStatus.Caption = value; }
        }

        public string OperatorText
        {
            set { barStaticOperator.Caption = value; }
        }

        public void ShowMessage(string message)
        {
            XtraMessageBox.Show(this, message, Text);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            EventHandler handler = ShellLoaded;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void barBtnPatient_ItemClick(object sender, ItemClickEventArgs e)
        {
            RaiseNavigation(BusinessNavigation.PatientManagement);
        }

        private void barBtnNewReservation_ItemClick(object sender, ItemClickEventArgs e)
        {
            RaiseNavigation(BusinessNavigation.NewReservation);
        }

        private void barBtnReservationDesk_ItemClick(object sender, ItemClickEventArgs e)
        {
            RaiseNavigation(BusinessNavigation.ReservationDesk);
        }

        private void barBtnReceptionDesk_ItemClick(object sender, ItemClickEventArgs e)
        {
            RaiseNavigation(BusinessNavigation.ReceptionDesk);
        }

        private void barBtnHoliday_ItemClick(object sender, ItemClickEventArgs e)
        {
            RaiseNavigation(BusinessNavigation.HolidayManagement);
        }

        private void RaiseNavigation(BusinessNavigation target)
        {
            EventHandler<BusinessNavigation> handler = NavigationRequested;
            if (handler != null)
            {
                handler(this, target);
            }
        }
    }
}
