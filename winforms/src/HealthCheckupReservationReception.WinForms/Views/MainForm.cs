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
        private readonly IPatientService _patientService;
        private readonly string _operatorName;
        private readonly Dictionary<BusinessTab, XtraTabPage> _pages = new Dictionary<BusinessTab, XtraTabPage>();

        // Presenter 가 시킨 변경이 다시 event 로 돌아와 무한 왕복하는 것을 막는다.
        private bool _suppressEvents;

        // 03 §5.2 의 Action 상태는 두 판정의 곱이다 — 공통 업무가능 여부(MainPresenter)와
        // 행 선택 여부(PatientManagementPresenter). 곱하는 자리는 Ribbon 을 가진 여기뿐이다.
        private bool _businessAllowed = true;
        private bool _patientRowSelected;

        private UcPatientManagement _patientView;

        public MainForm(ICommonStatusService statusService, IPatientService patientService, string operatorName)
        {
            InitializeComponent();
            ClampToWorkingArea();
            _patientService = patientService;
            _operatorName = operatorName;

            // Designer 는 버튼을 켜진 채로 만든다. Presenter 가 붙기 전에 03 §5.2 의
            // 초기 상태(행 미선택)로 맞춘다.
            ApplyPatientRowActions();

            _presenter = new MainPresenter(this, statusService, operatorName);
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
                // [정보수정]·[신규예약] 은 여기 없다 — 행 선택도 함께 봐야 하므로
                // ApplyPatientRowActions 가 혼자 정한다 (03 §5.2).
                BarItem[] gated =
                {
                    barBtnPatientSearch, barBtnPatientNew,
                    barBtnReservationSave,
                    barBtnRsvSearch, barBtnRsvEdit, barBtnRsvCancel, barBtnRsvReception,
                    barBtnRcpSearch, barBtnRcpWalkIn, barBtnRcpRsvEdit, barBtnRcpStart,
                    barBtnRcpExtra, barBtnRcpCancel
                };

                foreach (BarItem item in gated)
                {
                    item.Enabled = value;
                }

                _businessAllowed = value;
                ApplyPatientRowActions();
            }
        }

        /// <summary>
        /// 03 §5.2 — 수검자 목록의 행 선택 여부. 판정은 PatientManagementPresenter 가 하고
        /// 어느 버튼이 열리는지는 Ribbon 을 가진 이 화면이 그린다.
        /// </summary>
        public bool PatientRowSelected
        {
            set
            {
                _patientRowSelected = value;
                ApplyPatientRowActions();
            }
        }

        private void ApplyPatientRowActions()
        {
            // 03 §5.2 표 — [정보수정]·[신규예약] 은 행 선택 + 공통 업무가능이 모두 필요하고,
            // [변경이력] 은 조회 Action 이라 공통 업무불가에도 행만 잡혀 있으면 열린다(§23.4).
            barBtnPatientEdit.Enabled = _patientRowSelected && _businessAllowed;
            barBtnPatientReserve.Enabled = _patientRowSelected && _businessAllowed;
            barBtnPatientLog.Enabled = _patientRowSelected;
        }

        public void OpenTab(BusinessTab tab, string caption)
        {
            if (_pages.ContainsKey(tab))
            {
                return;
            }

            var page = new XtraTabPage { Text = caption, Tag = tab };

            Control content = CreateTabContent(tab);
            if (content != null)
            {
                content.Dock = DockStyle.Fill;
                page.Controls.Add(content);
            }

            _pages.Add(tab, page);
            tabBusiness.TabPages.Add(page);
        }

        /// <summary>
        /// 03 §1.4 — 업무 Tab 의 내용은 화면마다 하나의 XtraUserControl 이다.
        /// 아직 만들지 않은 화면은 빈 Tab 으로 열린다.
        /// </summary>
        private Control CreateTabContent(BusinessTab tab)
        {
            if (tab != BusinessTab.PatientManagement)
            {
                // EXTENSION POINT: WF-RSV-01 · WF-WRK-01.
                return null;
            }

            var view = new UcPatientManagement();
            view.RowActionsChanged += PatientView_RowActionsChanged;
            view.Attach(_patientService);
            _patientView = view;
            return view;
        }

        private void PatientView_RowActionsChanged(object sender, bool rowSelected)
        {
            PatientRowSelected = rowSelected;
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
            if (tab == BusinessTab.PatientManagement)
            {
                _patientView = null;
                PatientRowSelected = false;
            }

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

        // [X] 마지막 업무 Tab 을 닫으면 남는 것이 Ribbon 뿐이고, 이미 선택돼 있는 Page 를
        //     다시 눌러도 SelectedPageChanged 가 나지 않아 돌아갈 길이 없었다.
        //     업무 Action 이 자기 Tab 을 먼저 보장하게 해서 막다른 골목을 없앤다 (07 §14.3 A-08).
        private void barBtnPatientSearch_ItemClick(object sender, ItemClickEventArgs e)
        {
            UcPatientManagement view = EnsurePatientTab();
            if (view != null)
            {
                view.RequestSearch();
            }
        }

        private void barBtnPatientColumns_ItemClick(object sender, ItemClickEventArgs e)
        {
            UcPatientManagement view = EnsurePatientTab();
            if (view != null)
            {
                view.ShowColumnChooser();
            }
        }

        // 03 §5.2 — [신규등록] 은 대상 행이 필요 없다. Modal 만 열면 되므로 Tab 을 열지 않는다.
        private void barBtnPatientNew_ItemClick(object sender, ItemClickEventArgs e)
        {
            OpenPatientEditor(null);
        }

        // 03 §5.2 — [정보수정] 은 행 선택 + 공통 업무가능일 때만 열려 있다. 그래도 대상을
        // 다시 확인한다: 버튼 상태와 Grid 상태가 어긋난 채로 0 을 저장 SP 에 보내지 않는다.
        private void barBtnPatientEdit_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_patientView == null || _patientView.SelectedPatientId == null)
            {
                return;
            }

            OpenPatientEditor(_patientView.SelectedPatientId);
        }

        /// <summary>
        /// DLG-PAT-01 을 연다 (03 §6). 03 §5 는 저장 뒤 수검자 관리 화면이 할 일을 정하지
        /// 않으므로 목록을 자동으로 다시 읽지 않는다 (07 §14.3 A-11).
        /// </summary>
        private void OpenPatientEditor(long? patientId)
        {
            using (var editor = new FrmPatientEditor(_patientService, _operatorName, patientId))
            {
                editor.ShowDialog(this);
            }
        }

        private void barBtnNotImplemented_ItemClick(object sender, ItemClickEventArgs e)
        {
            // EXTENSION POINT: WF-RSV-01 · DLG-LOG-01.
            XtraMessageBox.Show(this, e.Item.Caption + " 화면은 아직 만들지 않았습니다.", Text);
        }

        private UcPatientManagement EnsurePatientTab()
        {
            EventHandler<BusinessNavigation> handler = NavigationRequested;
            if (handler != null)
            {
                handler(this, BusinessNavigation.PatientManagement);
            }

            return _patientView;
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
