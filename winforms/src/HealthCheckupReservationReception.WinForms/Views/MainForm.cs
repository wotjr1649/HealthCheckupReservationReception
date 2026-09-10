// 화면 ID: WF-00 — MainForm Shell (03 §4)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
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
        private readonly IPatientService _patientService;
        private readonly string _operatorName;
        // 한 번 만든 업무 화면은 들고 있는다. Tab 스트립이 사라졌어도 화면을 오갈 때마다
        // 새로 세우면 사용자가 조회해 둔 목록이 매번 날아간다.
        private readonly Dictionary<BusinessTab, Control> _screens = new Dictionary<BusinessTab, Control>();

        // Presenter 가 시킨 변경이 다시 event 로 돌아와 무한 왕복하는 것을 막는다.
        private bool _suppressEvents;

        // 03 §5.2 의 Action 상태는 행 선택 여부 하나로 정해진다. 판정은
        // PatientManagementPresenter 가 하고 그리는 자리는 Ribbon 을 가진 여기다.
        private bool _patientRowSelected;

        private UcPatientManagement _patientView;

        /// <summary>
        /// [X] **VS 디자이너 전용이다.** 디자이너는 설계 대상 타입을 매개변수 없는 생성자로
        ///     만든다 — 그것이 없으면 「디자이너에 대한 문서를 로드하지 않았으므로 디자이너를
        ///     표시할 수 없습니다」로 화면이 아예 열리지 않는다(실측 2026-09-10).
        ///
        ///     서비스도 Presenter 도 만들지 않는다. 디자인 표면에서 DB 에 닿으면 안 된다
        ///     (`references/designer.md` 함정 2). 실행 경로는 아래 생성자다.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

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
        /// 화면 밖으로 나가 상태바를 볼 수 없다 (03 §1.4 최소 검증 1366x768).
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

        public string WorkStatusText
        {
            set { barStaticWorkStatus.Caption = value; }
        }

        public string OperatorText
        {
            set { barStaticOperator.Caption = value; }
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

        /// <summary>
        /// 03 §5.2 표 — 이 셋은 **행 선택 하나만** 본다.
        ///
        /// [R12] 공통 업무불가는 더 이상 Action 을 닫지 않는다 (03 §1.3 · `00` §1.1).
        ///       화면이 미리 닫으면 저장 시점의 DB 판정을 사용자가 받아볼 수 없고, 수검자
        ///       기준정보는 애초에 `308`/`309` 의 대상이 아니다. 업무 상태는 상태영역이 보인다.
        /// </summary>
        private void ApplyPatientRowActions()
        {
            barBtnPatientEdit.Enabled = _patientRowSelected;
            barBtnPatientReserve.Enabled = _patientRowSelected;
            barBtnPatientLog.Enabled = _patientRowSelected;
        }

        /// <summary>
        /// 2026-09-10 사용자 결정 — 업무 화면은 한 번에 하나만 뜬다. Tab 스트립을 걷었으므로
        /// 여기서 하는 일은 "그 화면을 앞에 세우는 것" 뿐이다. 닫기도 Caption 도 없다.
        /// </summary>
        public void ShowBusinessScreen(BusinessTab screen)
        {
            Control content;
            if (!_screens.TryGetValue(screen, out content))
            {
                content = CreateScreen(screen);
                _screens.Add(screen, content);
                if (content != null)
                {
                    content.Dock = DockStyle.Fill;
                    pnlBusiness.Controls.Add(content);
                }
            }

            foreach (Control child in pnlBusiness.Controls)
            {
                child.Visible = ReferenceEquals(child, content);
            }

            if (content != null)
            {
                content.BringToFront();
            }
        }

        /// <summary>
        /// 업무 화면 하나는 XtraUserControl 하나다. 아직 만들지 않은 화면은 빈 자리로 둔다.
        /// </summary>
        private Control CreateScreen(BusinessTab tab)
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

        // 03 §5.2 — [신규등록] 은 대상 행이 필요 없다. Modal 만 열면 된다.
        private void barBtnPatientNew_ItemClick(object sender, ItemClickEventArgs e)
        {
            OpenPatientEditor(null);
        }

        // 03 §5.2 — [정보수정] 은 **행 선택**일 때만 열려 있다. [R12] 곱하던 두 번째 판정
        // (공통 업무가능)은 사라졌다 — 화면은 업무 상태로 Action 을 막지 않는다. 그래도 대상을
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
    }
}
