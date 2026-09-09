// 화면 ID: WF-00 — MainForm Shell (03 §4)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using DevExpress.XtraTab;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;

namespace HealthCheckupReservationReception.Views
{
    public partial class MainForm : RibbonForm, IMainView
    {
        private readonly MainPresenter _presenter;
        private readonly Dictionary<BusinessTab, XtraTabPage> _pages = new Dictionary<BusinessTab, XtraTabPage>();

        // Presenter 가 시킨 변경이 다시 event 로 돌아와 무한 왕복하는 것을 막는다.
        private bool _suppressEvents;

        public MainForm(ICommonStatusService service, string operatorName)
        {
            InitializeComponent();
            ClampToWorkingArea();
            _presenter = new MainPresenter(this, service, operatorName);
        }

        /// <summary>
        /// 기본 크기는 03 §1.4 의 권장 기준 1920x1080 이고 최대화도 그대로 쓴다.
        /// 화면이 그보다 작으면 작업 영역에 맞춘다 — 그러지 않으면 창의 아래·오른쪽이
        /// 화면 밖으로 나가 상태바와 Tab 스트립을 볼 수 없다 (03 §1.4 최소 검증 1366x768).
        /// </summary>
        private void ClampToWorkingArea()
        {
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            if (Width <= work.Width && Height <= work.Height)
            {
                return;
            }

            Size = new Size(Math.Min(Width, work.Width), Math.Min(Height, work.Height));
        }

        public event EventHandler ShellLoaded;
        public event EventHandler<BusinessNavigation> NavigationRequested;
        public event EventHandler<BusinessTab> TabCloseRequested;
        public event EventHandler<BusinessTab> TabActivated;

        public string WorkStatusText
        {
            set { barStaticWorkStatus.Caption = value; }
        }

        public string OperatorText
        {
            set { barStaticOperator.Caption = value; }
        }

        /// <summary>
        /// 03 §5.2 · §9.6 · §9.7 의 `공통 업무불가` 행 그대로다 —
        /// `변경이력`·`컬럼설정`만 남고 나머지 업무 Action 은 전부 비활성이다.
        /// `휴무일 관리`는 이 규칙 밖이다 (03 §24.2).
        /// </summary>
        public bool BusinessActionsEnabled
        {
            set
            {
                BarItem[] gated =
                {
                    barBtnPatientSearch, barBtnPatientNew, barBtnPatientEdit, barBtnPatientReserve,
                    barBtnReservationSave,
                    barBtnRsvSearch, barBtnRsvEdit, barBtnRsvCancel, barBtnRsvReception,
                    barBtnRcpSearch, barBtnRcpWalkIn, barBtnRcpRsvEdit, barBtnRcpStart,
                    barBtnRcpExtra, barBtnRcpCancel
                };

                foreach (BarItem item in gated)
                {
                    item.Enabled = value;
                }
            }
        }

        public void OpenTab(BusinessTab tab, string caption)
        {
            if (_pages.ContainsKey(tab))
            {
                return;
            }

            var page = new XtraTabPage { Text = caption, Tag = tab };
            _pages.Add(tab, page);
            tabBusiness.TabPages.Add(page);
        }

        public void ActivateTab(BusinessTab tab)
        {
            XtraTabPage page;
            if (!_pages.TryGetValue(tab, out page))
            {
                return;
            }

            _suppressEvents = true;
            try
            {
                tabBusiness.SelectedTabPage = page;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        public void SetTabCaption(BusinessTab tab, string caption)
        {
            XtraTabPage page;
            if (_pages.TryGetValue(tab, out page))
            {
                page.Text = caption;
            }
        }

        public void CloseTab(BusinessTab tab)
        {
            XtraTabPage page;
            if (!_pages.TryGetValue(tab, out page))
            {
                return;
            }

            _pages.Remove(tab);
            _suppressEvents = true;
            try
            {
                tabBusiness.TabPages.Remove(page);
            }
            finally
            {
                _suppressEvents = false;
            }

            page.Dispose();
        }

        public void SelectNavigationPage(BusinessNavigation page)
        {
            RibbonPage target = RibbonPageOf(page);
            if (target == null)
            {
                return;
            }

            _suppressEvents = true;
            try
            {
                barRibbonMain.SelectedPage = target;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        public void ShowMessage(string message)
        {
            XtraMessageBox.Show(this, message, Text);
        }

        public void ShowHolidayManagement()
        {
            // EXTENSION POINT: DLG-HOL-01 을 연다 (03 §24).
            XtraMessageBox.Show(this, "휴무일 관리 화면은 아직 만들지 않았습니다.", Text);
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

        private RibbonPage RibbonPageOf(BusinessNavigation page)
        {
            switch (page)
            {
                case BusinessNavigation.PatientManagement: return barPagePatient;
                case BusinessNavigation.NewReservation: return barPageNewReservation;
                case BusinessNavigation.ReservationDesk: return barPageRsvDesk;
                case BusinessNavigation.ReceptionDesk: return barPageRcpDesk;
                case BusinessNavigation.HolidayManagement: return barPageHoliday;
                default: return null;
            }
        }

        private void barRibbonMain_SelectedPageChanged(object sender, EventArgs e)
        {
            if (_suppressEvents)
            {
                return;
            }

            RibbonPage selected = barRibbonMain.SelectedPage;
            EventHandler<BusinessNavigation> handler = NavigationRequested;
            if (selected == null || handler == null)
            {
                return;
            }

            if (selected == barPagePatient)
            {
                handler(this, BusinessNavigation.PatientManagement);
            }
            else if (selected == barPageNewReservation)
            {
                handler(this, BusinessNavigation.NewReservation);
            }
            else if (selected == barPageRsvDesk)
            {
                handler(this, BusinessNavigation.ReservationDesk);
            }
            else if (selected == barPageRcpDesk)
            {
                handler(this, BusinessNavigation.ReceptionDesk);
            }
            else if (selected == barPageHoliday)
            {
                handler(this, BusinessNavigation.HolidayManagement);
            }
        }

        private void tabBusiness_CloseButtonClick(object sender, EventArgs e)
        {
            var args = e as DevExpress.XtraTab.ViewInfo.ClosePageButtonEventArgs;
            if (args == null)
            {
                return;
            }

            var page = args.Page as XtraTabPage;
            if (page == null || !(page.Tag is BusinessTab))
            {
                return;
            }

            EventHandler<BusinessTab> handler = TabCloseRequested;
            if (handler != null)
            {
                handler(this, (BusinessTab)page.Tag);
            }
        }

        private void tabBusiness_SelectedPageChanged(object sender, TabPageChangedEventArgs e)
        {
            if (_suppressEvents || e.Page == null || !(e.Page.Tag is BusinessTab))
            {
                return;
            }

            EventHandler<BusinessTab> handler = TabActivated;
            if (handler != null)
            {
                handler(this, (BusinessTab)e.Page.Tag);
            }
        }
    }
}
