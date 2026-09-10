// 화면 ID: WF-00 — MainForm Shell (03 §4)
namespace HealthCheckupReservationReception.Views
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.barRibbonMain = new DevExpress.XtraBars.Ribbon.RibbonControl();
            this.barBtnPatientNew = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnPatientEdit = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnPatientReserve = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnPatientLog = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnReservationSave = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRsvSearch = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRsvEdit = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRsvCancel = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRsvReception = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRsvLog = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRsvColumns = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRcpSearch = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRcpWalkIn = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRcpRsvEdit = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRcpStart = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRcpExtra = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRcpCancel = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRcpLog = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRcpColumns = new DevExpress.XtraBars.BarButtonItem();
            this.barStaticWorkStatus = new DevExpress.XtraBars.BarStaticItem();
            this.barStaticOperator = new DevExpress.XtraBars.BarStaticItem();
            this.barPagePatient = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.barGroupPatientWork = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barGroupPatientView = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barPageNewReservation = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.barGroupReservationWork = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barPageRsvDesk = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.barGroupRsvSearch = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barGroupRsvWork = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barGroupRsvView = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barPageRcpDesk = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.barPageHoliday = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.barGroupRcpSearch = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barGroupRcpWork = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barGroupRcpView = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barStatusMain = new DevExpress.XtraBars.Ribbon.RibbonStatusBar();
            this.pnlBusiness = new DevExpress.XtraEditors.PanelControl();
            ((System.ComponentModel.ISupportInitialize)(this.barRibbonMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pnlBusiness)).BeginInit();
            this.SuspendLayout();
            //
            // barRibbonMain
            //
            this.barRibbonMain.ExpandCollapseItem.Id = 0;
            this.barRibbonMain.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.barRibbonMain.ExpandCollapseItem,
            this.barBtnPatientNew,
            this.barBtnPatientEdit,
            this.barBtnPatientReserve,
            this.barBtnPatientLog,
            this.barBtnReservationSave,
            this.barBtnRsvSearch,
            this.barBtnRsvEdit,
            this.barBtnRsvCancel,
            this.barBtnRsvReception,
            this.barBtnRsvLog,
            this.barBtnRsvColumns,
            this.barBtnRcpSearch,
            this.barBtnRcpWalkIn,
            this.barBtnRcpRsvEdit,
            this.barBtnRcpStart,
            this.barBtnRcpExtra,
            this.barBtnRcpCancel,
            this.barBtnRcpLog,
            this.barBtnRcpColumns,
            this.barStaticWorkStatus,
            this.barStaticOperator});
            this.barRibbonMain.Location = new System.Drawing.Point(0, 0);
            this.barRibbonMain.MaxItemId = 25;
            this.barRibbonMain.Name = "barRibbonMain";
            // 03 §1.1 상단 업무 Navigation 다섯. 화면설계서 slide4 가 y=101 한 줄에
            // 같은 크기(w=122)로 다섯을 그린다 — 휴무일 관리도 동등한 Page 다.
            // 열리는 것이 Tab 이 아니라 Modal 이라는 것은 선택 시 동작이 정한다 (03 §24.2).
            this.barRibbonMain.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] {
            this.barPagePatient,
            this.barPageNewReservation,
            this.barPageRsvDesk,
            this.barPageRcpDesk,
            this.barPageHoliday});
            // 03 에 없는 리본 크롬을 끈다. RibbonControl 을 만들면 자동으로 켜지는 것들이라
            // 능동적으로 넣은 것이 아니라 끄지 않았던 것이다 (07 §14.3 A-07).
            //   ShowApplicationButton         Office 의 [파일] 탭 자리. 03 에 없다
            //   ShowDisplayOptionsMenuButton  제목표시줄의 리본 표시 옵션 드롭다운
            //   ShowExpandCollapseButton      리본 우측 하단의 접기 버튼
            //   AllowMinimizeRibbon           [X] 버튼만 숨기면 페이지 헤더 더블클릭으로 여전히 접힌다
            //   Minimized                     펼친 상태로 고정
            this.barRibbonMain.AllowMinimizeRibbon = false;
            this.barRibbonMain.Minimized = false;
            this.barRibbonMain.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            this.barRibbonMain.ShowDisplayOptionsMenuButton = DevExpress.Utils.DefaultBoolean.False;
            this.barRibbonMain.ShowExpandCollapseButton = DevExpress.Utils.DefaultBoolean.False;
            this.barRibbonMain.ShowToolbarCustomizeItem = false;
            this.barRibbonMain.Size = new System.Drawing.Size(1920, 143);
            this.barRibbonMain.StatusBar = this.barStatusMain;
            this.barRibbonMain.ToolbarLocation = DevExpress.XtraBars.Ribbon.RibbonQuickAccessToolbarLocation.Hidden;
            this.barRibbonMain.SelectedPageChanged += new System.EventHandler(this.barRibbonMain_SelectedPageChanged);
            //
            // barBtnPatientNew
            //
            this.barBtnPatientNew.Caption = "신규등록";
            this.barBtnPatientNew.Id = 3;
            this.barBtnPatientNew.Name = "barBtnPatientNew";
            this.barBtnPatientNew.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnPatientNew.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnPatientNew_ItemClick);
            //
            // barBtnPatientEdit
            //
            this.barBtnPatientEdit.Caption = "정보수정";
            this.barBtnPatientEdit.Id = 4;
            this.barBtnPatientEdit.Name = "barBtnPatientEdit";
            this.barBtnPatientEdit.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnPatientEdit.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnPatientEdit_ItemClick);
            //
            // barBtnPatientReserve
            //
            this.barBtnPatientReserve.Caption = "신규예약";
            this.barBtnPatientReserve.Id = 5;
            this.barBtnPatientReserve.Name = "barBtnPatientReserve";
            this.barBtnPatientReserve.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnPatientReserve.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnNotImplemented_ItemClick);
            //
            // barBtnPatientLog
            //
            this.barBtnPatientLog.Caption = "변경이력";
            this.barBtnPatientLog.Id = 6;
            this.barBtnPatientLog.Name = "barBtnPatientLog";
            this.barBtnPatientLog.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnPatientLog.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnNotImplemented_ItemClick);
            //
            // barBtnReservationSave
            //
            this.barBtnReservationSave.Caption = "예약저장";
            this.barBtnReservationSave.Id = 8;
            this.barBtnReservationSave.Name = "barBtnReservationSave";
            this.barBtnReservationSave.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRsvSearch
            //
            this.barBtnRsvSearch.Caption = "조회";
            this.barBtnRsvSearch.Id = 9;
            this.barBtnRsvSearch.Name = "barBtnRsvSearch";
            this.barBtnRsvSearch.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRsvEdit
            //
            this.barBtnRsvEdit.Caption = "예약변경";
            this.barBtnRsvEdit.Id = 10;
            this.barBtnRsvEdit.Name = "barBtnRsvEdit";
            this.barBtnRsvEdit.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRsvCancel
            //
            this.barBtnRsvCancel.Caption = "예약취소";
            this.barBtnRsvCancel.Id = 11;
            this.barBtnRsvCancel.Name = "barBtnRsvCancel";
            this.barBtnRsvCancel.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRsvReception
            //
            this.barBtnRsvReception.Caption = "접수";
            this.barBtnRsvReception.Id = 12;
            this.barBtnRsvReception.Name = "barBtnRsvReception";
            this.barBtnRsvReception.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRsvLog
            //
            this.barBtnRsvLog.Caption = "변경이력";
            this.barBtnRsvLog.Id = 13;
            this.barBtnRsvLog.Name = "barBtnRsvLog";
            this.barBtnRsvLog.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRsvColumns
            //
            this.barBtnRsvColumns.Caption = "컬럼설정";
            this.barBtnRsvColumns.Id = 14;
            this.barBtnRsvColumns.Name = "barBtnRsvColumns";
            this.barBtnRsvColumns.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRcpSearch
            //
            this.barBtnRcpSearch.Caption = "조회";
            this.barBtnRcpSearch.Id = 15;
            this.barBtnRcpSearch.Name = "barBtnRcpSearch";
            this.barBtnRcpSearch.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRcpWalkIn
            //
            this.barBtnRcpWalkIn.Caption = "현장 당일예약";
            this.barBtnRcpWalkIn.Id = 16;
            this.barBtnRcpWalkIn.Name = "barBtnRcpWalkIn";
            this.barBtnRcpWalkIn.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRcpRsvEdit
            //
            this.barBtnRcpRsvEdit.Caption = "예약변경";
            this.barBtnRcpRsvEdit.Id = 17;
            this.barBtnRcpRsvEdit.Name = "barBtnRcpRsvEdit";
            this.barBtnRcpRsvEdit.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRcpStart
            //
            this.barBtnRcpStart.Caption = "접수";
            this.barBtnRcpStart.Id = 18;
            this.barBtnRcpStart.Name = "barBtnRcpStart";
            this.barBtnRcpStart.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRcpExtra
            //
            this.barBtnRcpExtra.Caption = "추가검사변경";
            this.barBtnRcpExtra.Id = 19;
            this.barBtnRcpExtra.Name = "barBtnRcpExtra";
            this.barBtnRcpExtra.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRcpCancel
            //
            this.barBtnRcpCancel.Caption = "접수취소";
            this.barBtnRcpCancel.Id = 20;
            this.barBtnRcpCancel.Name = "barBtnRcpCancel";
            this.barBtnRcpCancel.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRcpLog
            //
            this.barBtnRcpLog.Caption = "변경이력";
            this.barBtnRcpLog.Id = 21;
            this.barBtnRcpLog.Name = "barBtnRcpLog";
            this.barBtnRcpLog.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barBtnRcpColumns
            //
            this.barBtnRcpColumns.Caption = "컬럼설정";
            this.barBtnRcpColumns.Id = 22;
            this.barBtnRcpColumns.Name = "barBtnRcpColumns";
            this.barBtnRcpColumns.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            //
            // barStaticWorkStatus
            //
            this.barStaticWorkStatus.Id = 23;
            this.barStaticWorkStatus.Name = "barStaticWorkStatus";
            //
            // barStaticOperator
            //
            this.barStaticOperator.Alignment = DevExpress.XtraBars.BarItemLinkAlignment.Right;
            this.barStaticOperator.Id = 24;
            this.barStaticOperator.Name = "barStaticOperator";
            //
            // barPagePatient
            //
            this.barPagePatient.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
            this.barGroupPatientWork,
            this.barGroupPatientView});
            this.barPagePatient.Name = "barPagePatient";
            this.barPagePatient.Text = "수검자 관리";
            //
            // barGroupPatientWork
            //
            this.barGroupPatientWork.ItemLinks.Add(this.barBtnPatientNew);
            this.barGroupPatientWork.ItemLinks.Add(this.barBtnPatientEdit);
            this.barGroupPatientWork.ItemLinks.Add(this.barBtnPatientReserve);
            this.barGroupPatientWork.Name = "barGroupPatientWork";
            this.barGroupPatientWork.Text = "수검자";
            //
            // barGroupPatientView
            //
            this.barGroupPatientView.ItemLinks.Add(this.barBtnPatientLog);
            this.barGroupPatientView.Name = "barGroupPatientView";
            this.barGroupPatientView.Text = "보기";
            //
            // barPageNewReservation
            //
            this.barPageNewReservation.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
            this.barGroupReservationWork});
            this.barPageNewReservation.Name = "barPageNewReservation";
            this.barPageNewReservation.Text = "신규 예약";
            //
            // barGroupReservationWork
            //
            this.barGroupReservationWork.ItemLinks.Add(this.barBtnReservationSave);
            this.barGroupReservationWork.Name = "barGroupReservationWork";
            this.barGroupReservationWork.Text = "예약";
            //
            // barPageRsvDesk
            //
            this.barPageRsvDesk.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
            this.barGroupRsvSearch,
            this.barGroupRsvWork,
            this.barGroupRsvView});
            this.barPageRsvDesk.Name = "barPageRsvDesk";
            this.barPageRsvDesk.Text = "예약 관리";
            //
            // barGroupRsvSearch
            //
            this.barGroupRsvSearch.ItemLinks.Add(this.barBtnRsvSearch);
            this.barGroupRsvSearch.Name = "barGroupRsvSearch";
            this.barGroupRsvSearch.Text = "검색";
            //
            // barGroupRsvWork
            //
            this.barGroupRsvWork.ItemLinks.Add(this.barBtnRsvEdit);
            this.barGroupRsvWork.ItemLinks.Add(this.barBtnRsvCancel);
            this.barGroupRsvWork.ItemLinks.Add(this.barBtnRsvReception);
            this.barGroupRsvWork.Name = "barGroupRsvWork";
            this.barGroupRsvWork.Text = "예약 업무";
            //
            // barGroupRsvView
            //
            this.barGroupRsvView.ItemLinks.Add(this.barBtnRsvLog);
            this.barGroupRsvView.ItemLinks.Add(this.barBtnRsvColumns);
            this.barGroupRsvView.Name = "barGroupRsvView";
            this.barGroupRsvView.Text = "보기";
            //
            // barPageRcpDesk
            //
            this.barPageRcpDesk.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
            this.barGroupRcpSearch,
            this.barGroupRcpWork,
            this.barGroupRcpView});
            this.barPageRcpDesk.Name = "barPageRcpDesk";
            this.barPageRcpDesk.Text = "접수 관리";
            //
            // barGroupRcpSearch
            //
            this.barGroupRcpSearch.ItemLinks.Add(this.barBtnRcpSearch);
            this.barGroupRcpSearch.Name = "barGroupRcpSearch";
            this.barGroupRcpSearch.Text = "검색";
            //
            // barGroupRcpWork
            //
            this.barGroupRcpWork.ItemLinks.Add(this.barBtnRcpWalkIn);
            this.barGroupRcpWork.ItemLinks.Add(this.barBtnRcpRsvEdit);
            this.barGroupRcpWork.ItemLinks.Add(this.barBtnRcpStart);
            this.barGroupRcpWork.ItemLinks.Add(this.barBtnRcpExtra);
            this.barGroupRcpWork.ItemLinks.Add(this.barBtnRcpCancel);
            this.barGroupRcpWork.Name = "barGroupRcpWork";
            this.barGroupRcpWork.Text = "접수 업무";
            //
            // barGroupRcpView
            //
            this.barGroupRcpView.ItemLinks.Add(this.barBtnRcpLog);
            this.barGroupRcpView.ItemLinks.Add(this.barBtnRcpColumns);
            this.barGroupRcpView.Name = "barGroupRcpView";
            this.barGroupRcpView.Text = "보기";
            //
            // barPageHoliday
            //
            // 그룹이 없다. 이 Page 는 선택되는 즉시 DLG-HOL-01 을 열고 직전 Page 로 돌아간다
            // — 03 §24.2 가 업무 Tab 을 열지 않는다고 확정했다.
            this.barPageHoliday.Name = "barPageHoliday";
            this.barPageHoliday.Text = "휴무일 관리";
            //
            // barStatusMain
            //
            this.barStatusMain.ItemLinks.Add(this.barStaticWorkStatus);
            this.barStatusMain.ItemLinks.Add(this.barStaticOperator);
            this.barStatusMain.Location = new System.Drawing.Point(0, 1058);
            this.barStatusMain.Name = "barStatusMain";
            this.barStatusMain.Ribbon = this.barRibbonMain;
            this.barStatusMain.Size = new System.Drawing.Size(1920, 22);
            //
            // pnlBusiness
            //
            // 2026-09-10 사용자 결정 — 업무 Tab 스트립(XtraTabControl)을 걷었다. 화면은 한 번에
            // 하나만 뜨고 전환은 Ribbon Page 가 한다. 여러 폼을 겹쳐 두지 않는다.
            this.pnlBusiness.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.pnlBusiness.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlBusiness.Location = new System.Drawing.Point(0, 143);
            this.pnlBusiness.Name = "pnlBusiness";
            this.pnlBusiness.Size = new System.Drawing.Size(1920, 915);
            this.pnlBusiness.TabIndex = 2;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1920, 1080);
            this.MinimumSize = new System.Drawing.Size(1366, 768);
            this.Controls.Add(this.pnlBusiness);
            this.Controls.Add(this.barStatusMain);
            this.Controls.Add(this.barRibbonMain);
            this.Name = "MainForm";
            this.Ribbon = this.barRibbonMain;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.StatusBar = this.barStatusMain;
            this.Text = "검진 예약·접수 관리 프로그램";
            ((System.ComponentModel.ISupportInitialize)(this.barRibbonMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pnlBusiness)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraBars.Ribbon.RibbonControl barRibbonMain;
        private DevExpress.XtraBars.Ribbon.RibbonStatusBar barStatusMain;
        private DevExpress.XtraBars.Ribbon.RibbonPage barPagePatient;
        private DevExpress.XtraBars.Ribbon.RibbonPage barPageNewReservation;
        private DevExpress.XtraBars.Ribbon.RibbonPage barPageRsvDesk;
        private DevExpress.XtraBars.Ribbon.RibbonPage barPageRcpDesk;
        private DevExpress.XtraBars.Ribbon.RibbonPage barPageHoliday;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupPatientWork;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupPatientView;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupReservationWork;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupRsvSearch;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupRsvWork;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupRsvView;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupRcpSearch;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupRcpWork;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupRcpView;
        private DevExpress.XtraBars.BarButtonItem barBtnPatientNew;
        private DevExpress.XtraBars.BarButtonItem barBtnPatientEdit;
        private DevExpress.XtraBars.BarButtonItem barBtnPatientReserve;
        private DevExpress.XtraBars.BarButtonItem barBtnPatientLog;
        private DevExpress.XtraBars.BarButtonItem barBtnReservationSave;
        private DevExpress.XtraBars.BarButtonItem barBtnRsvSearch;
        private DevExpress.XtraBars.BarButtonItem barBtnRsvEdit;
        private DevExpress.XtraBars.BarButtonItem barBtnRsvCancel;
        private DevExpress.XtraBars.BarButtonItem barBtnRsvReception;
        private DevExpress.XtraBars.BarButtonItem barBtnRsvLog;
        private DevExpress.XtraBars.BarButtonItem barBtnRsvColumns;
        private DevExpress.XtraBars.BarButtonItem barBtnRcpSearch;
        private DevExpress.XtraBars.BarButtonItem barBtnRcpWalkIn;
        private DevExpress.XtraBars.BarButtonItem barBtnRcpRsvEdit;
        private DevExpress.XtraBars.BarButtonItem barBtnRcpStart;
        private DevExpress.XtraBars.BarButtonItem barBtnRcpExtra;
        private DevExpress.XtraBars.BarButtonItem barBtnRcpCancel;
        private DevExpress.XtraBars.BarButtonItem barBtnRcpLog;
        private DevExpress.XtraBars.BarButtonItem barBtnRcpColumns;
        private DevExpress.XtraBars.BarStaticItem barStaticWorkStatus;
        private DevExpress.XtraBars.BarStaticItem barStaticOperator;
        private DevExpress.XtraEditors.PanelControl pnlBusiness;
    }
}
