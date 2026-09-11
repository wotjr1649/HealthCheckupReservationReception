// 화면 ID: WF-00 — MainForm Shell (03 §4)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;

namespace HealthCheckupReservationReception.Views
{
    public partial class MainForm : RibbonForm, IMainView
    {
        private readonly MainPresenter _presenter;
        // App.config `MoveToReceptionAfterSave` (2026-09-11 사용자 지시).
        private readonly bool _moveToReceptionAfterSave = true;

        private readonly ICommonStatusService _statusService;
        private readonly IHolidayService _holidayService;
        private readonly IPatientService _patientService;
        private readonly IWorkService _workService;
        private readonly IReservationService _reservationService;
        private readonly IChangeLogService _changeLogService;
        private readonly string _operatorName;
        // 한 번 만든 업무 화면은 들고 있는다. Tab 스트립이 사라졌어도 화면을 오갈 때마다
        // 새로 세우면 사용자가 조회해 둔 목록이 매번 날아간다.
        private readonly Dictionary<BusinessTab, Control> _screens = new Dictionary<BusinessTab, Control>();

        // Presenter 가 시킨 변경이 다시 event 로 돌아와 무한 왕복하는 것을 막는다.
        private bool _suppressEvents;

        // 03 §5.2 의 Action 상태. 판정은 PatientManagementPresenter 가 하고 그리는 자리는
        // Ribbon 을 가진 여기다.
        private PatientActionState _patientActions = PatientActionState.None();

        private UcPatientManagement _patientView;
        private UcWorkbench _workView;
        private UcHoliday _holidayView;

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

        public MainForm(
            ICommonStatusService statusService,
            IPatientService patientService,
            IWorkService workService,
            IReservationService reservationService,
            IHolidayService holidayService,
            IChangeLogService changeLogService,
            string operatorName,
            bool moveToReceptionAfterSave)
        {
            InitializeComponent();
            ClampToWorkingArea();
            _statusService = statusService;
            _holidayService = holidayService;
            _moveToReceptionAfterSave = moveToReceptionAfterSave;
            _patientService = patientService;
            _workService = workService;
            _reservationService = reservationService;
            _changeLogService = changeLogService;
            _operatorName = operatorName;

            // Designer 는 버튼을 켜진 채로 만든다. Presenter 가 붙기 전에 03 §5.2 · §9.6 · §9.7 의
            // 초기 상태(행 미선택)로 맞춘다.
            ApplyPatientRowActions();
            ApplyWorkActions(WorkActionState.None());
            ApplyHolidayRowActions(false);

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
        public event EventHandler<WorkbenchTarget> WorkbenchRequested;

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
        public PatientActionState PatientActions
        {
            set
            {
                _patientActions = value ?? PatientActionState.None();
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
            barBtnPatientEdit.Enabled = _patientActions.RowSelected;
            barBtnPatientLog.Enabled = _patientActions.RowSelected;

            // 03 §5.2 에 축이 하나 더 있다 — 이미 예약이 있는 수검자는 [예약] 이 닫힌다
            // (2026-09-11 사용자 지시). 목록이 `불가` 라고 적어 두고 버튼을 열어 두면
            // 화면이 스스로 모순된다.
            barBtnPatientReserve.Enabled = _patientActions.Reserve;
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
        /// 03 §3 호출계약. WF-RSV-01 은 업무 판이 아니라 **모달**이다 (2026-09-10 grilling 2회차).
        ///
        /// **대상 없이 부르지 않는다** (2026-09-11 grilling). 예전에는 대상이 없으면 DLG-PAT-02
        /// 로 먼저 골랐는데, 그 모달은 수검자 관리 화면을 그대로 복제한 것이었다. 진입점이
        /// `수검자 관리 [예약]` 하나가 되면서 대상은 늘 정해져 있고, 그래서 그 화면이 사라졌다.
        /// </summary>
        public void BeginNewReservation(long patientId)
        {
            using (var reservation = new FrmReservation(
                _reservationService, _patientService, _operatorName, patientId))
            {
                if (reservation.ShowDialog(this) != DialogResult.OK || reservation.Result == null)
                {
                    return;
                }

                Land(reservation.Result, patientId);
            }
        }

        /// <summary>
        /// 예약 창이 닫힌 뒤 어디로 갈 것인가 (2026-09-11 사용자 지시).
        ///
        /// 예전에는 무조건 옮겼다. 명단을 연달아 예약하는 창구에서는 사람마다 화면이 튀어
        /// 흐름이 끊긴다 — 저장 뒤 「다음에 할 일」이 두 가지이기 때문이다:
        ///
        /// <list type="bullet">
        /// <item>오늘 예약 — 그 사람이 창구에 서 있다. 다음은 접수다</item>
        /// <item>미래 예약 — 다음은 다음 사람 예약이다. 옮기면 방해다</item>
        /// </list>
        ///
        /// 답이 날짜로 정해지므로 묻지 않는다. 창구마다 다를 수 있는 것은 `App.config` 가 갖는다.
        ///
        /// **보러 가는 것은 막지 않는다** — 사용자가 「예약 관리에서 확인하시겠습니까」에 이미
        /// `예` 라고 답한 것이라 여기서 한 번 더 판정하면 그 답을 무시하는 셈이다.
        /// </summary>
        private void Land(WorkbenchTarget target, long patientId)
        {
            bool move = !target.FromSave
                || (_moveToReceptionAfterSave && target.Context == WorkContext.Reception);

            if (move)
            {
                // 여는 것은 Navigation 을 가진 MainPresenter 의 일이라 그대로 올린다.
                Screen_WorkbenchRequested(this, target);
                return;
            }

            // 남는다 — 목록의 `예약` 칸이 방금 낡았으므로 되읽고 그 줄로 돌아간다.
            if (_patientView != null)
            {
                _patientView.ReloadAfterReservation(patientId, "예약이 등록되었습니다.");
            }
        }

        /// <summary>
        /// 03 §3 호출계약. 예약 관리와 접수 관리는 화면 하나를 나눠 쓰고 (§9.1),
        /// 어느 쪽인지는 <paramref name="context"/> 만이 나른다 — 탭 Caption 이 나르던
        /// 그 값이다. §9.6·§9.7 이 같은 화면에 다른 Ribbon Action 을 요구한다.
        /// </summary>
        public void OpenWorkbench(WorkContext context, long? workId)
        {
            // 세우는 것이 먼저다 — 화면이 붙어야 Presenter 가 서고, 그 뒤에 Context 를 준다.
            ShowBusinessScreen(BusinessTab.Workbench);
            if (_workView != null)
            {
                _workView.OpenContext(context, workId);
            }
        }

        /// <summary>
        /// 03 §9.6 · §9.7 — 같은 화면 하나에 Context 마다 다른 Ribbon Action 이 걸린다.
        /// 다섯 동작의 허용여부는 DB 가 준 것이고 (05 §8.2 RS4) 두 Page 는 그중 각자 쓸 것만
        /// 보인다. 그래서 상태 하나를 두 Page 에 그대로 바른다 — Page 마다 다시 판정하면
        /// 같은 규칙이 두 곳에 생긴다 (ROOT AGENTS.md §6).
        ///
        /// 접수 Page 에는 `RSV` 건을 다루는 Action 이 없다 (2026-09-11 사용자 지시) —
        /// `[예약변경]`·`[접수]` 는 예약 Page 에만 있다. 탭이 상태로 갈린다.
        /// </summary>
        private void ApplyWorkActions(WorkActionState state)
        {
            barBtnRsvEdit.Enabled = state.EditReservation;
            barBtnRsvCancel.Enabled = state.CancelReservation;
            barBtnRsvLog.Enabled = state.ChangeLog;

            // [접수] 는 접수 Page 에 있다 (2026-09-11) — 접수 Page 의 목록이 오늘의 `RSV` 를
            // 담으므로 접수할 대상이 그 탭에 있다.
            barBtnRcpStart.Enabled = state.StartReception;

            barBtnRcpExtra.Enabled = state.EditExtra;
            barBtnRcpCancel.Enabled = state.CancelReception;
            barBtnRcpLog.Enabled = state.ChangeLog;
        }

        /// <summary>
        /// 업무 화면 하나는 XtraUserControl 하나다. 아직 만들지 않은 화면은 빈 자리로 둔다.
        /// </summary>
        private Control CreateScreen(BusinessTab tab)
        {
            if (tab == BusinessTab.PatientManagement)
            {
                var patient = new UcPatientManagement();
                patient.RowActionsChanged += PatientView_RowActionsChanged;
                patient.Attach(_patientService, _workService, _statusService);
                _patientView = patient;
                return patient;
            }

            if (tab == BusinessTab.Holiday)
            {
                var holiday = new UcHoliday();
                holiday.RowActionsChanged += HolidayView_RowActionsChanged;
                holiday.Attach(_holidayService, _statusService);
                _holidayView = holiday;
                return holiday;
            }

            if (tab == BusinessTab.Workbench)
            {
                var workbench = new UcWorkbench();
                workbench.WorkActionsChanged += WorkView_WorkActionsChanged;
                workbench.Attach(_workService, _reservationService, _statusService, _operatorName);
                _workView = workbench;
                return workbench;
            }

            return null;
        }

        /// <summary>
        /// 03 §8.5 기존 유효예약 · §8.11 저장 성공. Navigation 상태는 MainPresenter 가 갖고
        /// 있으므로 여기서 직접 열지 않고 그대로 올린다.
        /// </summary>
        private void Screen_WorkbenchRequested(object sender, WorkbenchTarget target)
        {
            EventHandler<WorkbenchTarget> handler = WorkbenchRequested;
            if (handler != null)
            {
                handler(this, target);
            }
        }

        private void PatientView_RowActionsChanged(object sender, PatientActionState state)
        {
            PatientActions = state;
        }

        private void WorkView_WorkActionsChanged(object sender, WorkActionState state)
        {
            ApplyWorkActions(state);
        }

        private void HolidayView_RowActionsChanged(object sender, bool ownRowPicked)
        {
            ApplyHolidayRowActions(ownRowPicked);
        }

        /// <summary>
        /// 03 §24.5 — `[휴무일수정]`·`[휴무일삭제]` 는 **자체휴무일 행이 잡혔을 때만** 열린다.
        /// `[휴무일추가]` 는 선택행과 무관한 독립 Action 이라 늘 열려 있다.
        /// </summary>
        private void ApplyHolidayRowActions(bool ownRowPicked)
        {
            barBtnHolidayEdit.Enabled = ownRowPicked;
            barBtnHolidayDelete.Enabled = ownRowPicked;
        }

        private void barBtnHolidayNew_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_holidayView != null) { _holidayView.RequestRegister(); }
        }

        private void barBtnHolidayEdit_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_holidayView != null) { _holidayView.RequestUpdate(); }
        }

        private void barBtnHolidayDelete_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_holidayView != null) { _holidayView.RequestDelete(); }
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

        /// <summary>
        /// 03 §5.2 `[예약]` — 수검자 관리에서 고른 행 그대로 예약 모달을 연다.
        /// **예약으로 들어가는 유일한 자리다** (2026-09-11 grilling).
        /// </summary>
        private void barBtnPatientReserve_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_patientView == null || _patientView.SelectedPatientId == null)
            {
                return;
            }

            BeginNewReservation(_patientView.SelectedPatientId.Value);
        }

        /// <summary>
        /// 03 §23.2 — 진입점 둘이 같은 Modal 을 연다. 다른 것은 대상뿐이고 그것은 고른 행이
        /// 안다. **버튼이 셋인데 화면은 하나다** — 여기서 갈라지지 않는다.
        /// </summary>
        private void OpenChangeLog(ChangeLogTarget target)
        {
            if (target == null)
            {
                // Ribbon 이 이미 선택행 없이는 닫혀 있다 (03 §5.2 · §9.6). 여기 오면 그 판정이
                // 어긋난 것이므로 조용히 무시하지 않고 말한다.
                ShowMessage("먼저 목록에서 행을 선택하십시오.");
                return;
            }

            using (var log = new FrmChangeLog(_changeLogService, target))
            {
                log.ShowDialog(this);
            }
        }

        private void barBtnPatientLog_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_patientView != null) { OpenChangeLog(_patientView.CurrentLogTarget()); }
        }

        private void barBtnWorkLog_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_workView != null) { OpenChangeLog(_workView.CurrentLogTarget()); }
        }

        /// <summary>
        /// 03 §9.6 · §9.7 — 업무 Action 을 Workbench 로 넘긴다. **어느 것인지는 업무동작코드가
        /// 말한다** (05 §8.2). Ribbon 버튼마다 핸들러를 두면 같은 이름이 다섯 쌍 생긴다.
        /// </summary>
        private void barBtnWorkAction_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_workView == null)
            {
                return;
            }

            // 모달을 여는 Action 은 Form 이 맡는다 — UserControl 이 창을 띄우면 그 창의 부모가
            // 화면마다 달라진다. 창을 열지 않는 것은 Presenter 로 내려간다.
            if (e.Item == barBtnRcpStart)
            {
                BeginReception();
                return;
            }

            if (e.Item == barBtnRsvEdit)
            {
                BeginReservationChange();
                return;
            }

            _workView.RequestAction(ActionOf(e.Item));
        }

        /// <summary>
        /// DLG-RCP-01 접수 처리 (03 §11). 모달이 스스로 상세를 다시 읽으므로 여기서 넘기는
        /// 것은 업무ID 하나다 — 목록이 들고 있던 `행버전` 은 그 사이 낡을 수 있다 (§11.3).
        /// </summary>
        /// <summary>
        /// DLG-RSV-01 예약 변경 (03 §10). `FrmReservation` 을 변경 모드로 연다 — 새 화면이
        /// 아니라 같은 모달의 분기다.
        /// </summary>
        private void BeginReservationChange()
        {
            WorkDetailDto detail = _workView.CurrentDetail;
            if (detail == null)
            {
                ShowMessage("먼저 목록에서 행을 선택하십시오.");
                return;
            }

            using (var reservation = new FrmReservation(
                _reservationService, _patientService, _operatorName, detail))
            {
                if (reservation.ShowDialog(this) == DialogResult.OK && reservation.Result != null)
                {
                    _workView.OpenContext(reservation.Result.Context, reservation.Result.WorkId);
                }
            }
        }

        private void BeginReception()
        {
            WorkDetailDto detail = _workView.CurrentDetail;
            if (detail == null)
            {
                ShowMessage("먼저 목록에서 행을 선택하십시오.");
                return;
            }

            using (var reception = new FrmReception(_workService, _operatorName, detail.WorkId))
            {
                if (reception.ShowDialog(this) == DialogResult.OK)
                {
                    _workView.OpenContext(WorkContext.Reception, detail.WorkId);
                }
            }
        }

        /// <summary>Ribbon 버튼 ↔ 05 §8.2 업무동작코드. 여기 한 곳에서만 잇는다.</summary>
        private string ActionOf(BarItem item)
        {
            if (item == barBtnRsvEdit) { return DbWorkAction.EditReservation; }
            if (item == barBtnRsvCancel) { return DbWorkAction.CancelReservation; }
            if (item == barBtnRcpStart) { return DbWorkAction.StartReception; }
            if (item == barBtnRcpExtra) { return DbWorkAction.EditExtra; }
            if (item == barBtnRcpCancel) { return DbWorkAction.CancelReception; }
            return null;
        }

        private void barBtnNotImplemented_ItemClick(object sender, ItemClickEventArgs e)
        {
            // EXTENSION POINT: WF-RSV-01 · DLG-LOG-01.
            XtraMessageBox.Show(this, e.Item.Caption + " 화면은 아직 만들지 않았습니다.", Text);
        }
    }
}
