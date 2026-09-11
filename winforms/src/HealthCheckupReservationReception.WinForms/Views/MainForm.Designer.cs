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
            this.components = new System.ComponentModel.Container();
            this.barRibbonMain = new DevExpress.XtraBars.Ribbon.RibbonControl();
            this.barBtnPatientNew = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnPatientEdit = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnPatientReserve = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnPatientLog = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRsvEdit = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRsvCancel = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRcpStart = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRsvLog = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRcpExtra = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRcpCancel = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRcpLog = new DevExpress.XtraBars.BarButtonItem();
            this.barStaticWorkStatus = new DevExpress.XtraBars.BarStaticItem();
            this.barStaticOperator = new DevExpress.XtraBars.BarStaticItem();
            this.barPagePatient = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.barGroupPatientWork = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barGroupPatientView = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barPageRsvDesk = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.barGroupRsvWork = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barGroupRsvView = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barPageRcpDesk = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.barBtnHolidayManage = new DevExpress.XtraBars.BarButtonItem();
            this.barGroupPatientBase = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barGroupRsvBase = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barGroupRcpBase = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
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
            this.barBtnRsvEdit,
            this.barBtnRsvCancel,
            this.barBtnRcpStart,
            this.barBtnRsvLog,
            this.barBtnRcpExtra,
            this.barBtnRcpCancel,
            this.barBtnRcpLog,
            this.barBtnHolidayManage,
            this.barStaticWorkStatus,
            this.barStaticOperator});
            this.barRibbonMain.Location = new System.Drawing.Point(0, 0);
            this.barRibbonMain.MaxItemId = 28;
            this.barRibbonMain.Name = "barRibbonMain";
            // **Page 는 「가는 곳」만 갖는다** (2026-09-11 사용자 결정). 업무 화면 셋이 그
            // 전부이고, Page 를 누르면 반드시 그 화면이 선다 — Page ↔ 화면 1:1 이다.
            // 예전에는 `신규 예약` 과 `휴무일 관리` 도 Page 였는데 둘 다 눌러도 아무 데도
            // 가지 않고 Modal 만 띄운 뒤 탭이 제자리로 돌아왔다. 같은 띠에서 어떤 것은 가고
            // 어떤 것은 뜨니 예측이 되지 않았다.
            this.barRibbonMain.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] {
            this.barPagePatient,
            this.barPageRsvDesk,
            this.barPageRcpDesk});

            // 03 에 없는 리본 크롬을 끈다. RibbonControl 을 만들면 자동으로 켜지는 것들이라
            // 능동적으로 넣은 것이 아니라 끄지 않았던 것이다 (07 §14.3 A-07).
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
            this.barBtnPatientReserve.Caption = "예약";
            this.barBtnPatientReserve.Id = 5;
            this.barBtnPatientReserve.Name = "barBtnPatientReserve";
            this.barBtnPatientReserve.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnPatientReserve.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnPatientReserve_ItemClick);
            //
            // barBtnPatientLog
            //
            this.barBtnPatientLog.Caption = "변경이력";
            this.barBtnPatientLog.Id = 6;
            this.barBtnPatientLog.Name = "barBtnPatientLog";
            this.barBtnPatientLog.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnPatientLog.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnPatientLog_ItemClick);
            //
            // barBtnRsvEdit
            //
            this.barBtnRsvEdit.Caption = "예약변경";
            this.barBtnRsvEdit.Id = 10;
            this.barBtnRsvEdit.Name = "barBtnRsvEdit";
            this.barBtnRsvEdit.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnRsvEdit.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnWorkAction_ItemClick);
            //
            // barBtnRsvCancel
            //
            this.barBtnRsvCancel.Caption = "예약취소";
            this.barBtnRsvCancel.Id = 11;
            this.barBtnRsvCancel.Name = "barBtnRsvCancel";
            this.barBtnRsvCancel.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnRsvCancel.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnWorkAction_ItemClick);
            //
            // barBtnRcpStart
            //
            this.barBtnRcpStart.Caption = "접수";
            this.barBtnRcpStart.Id = 12;
            this.barBtnRcpStart.Name = "barBtnRcpStart";
            this.barBtnRcpStart.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnRcpStart.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnWorkAction_ItemClick);
            //
            // barBtnRsvLog
            //
            this.barBtnRsvLog.Caption = "변경이력";
            this.barBtnRsvLog.Id = 13;
            this.barBtnRsvLog.Name = "barBtnRsvLog";
            this.barBtnRsvLog.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnRsvLog.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnWorkLog_ItemClick);
            //
            // barBtnRcpExtra
            //
            this.barBtnRcpExtra.Caption = "추가검사변경";
            this.barBtnRcpExtra.Id = 19;
            this.barBtnRcpExtra.Name = "barBtnRcpExtra";
            this.barBtnRcpExtra.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnRcpExtra.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnWorkAction_ItemClick);
            //
            // barBtnRcpCancel
            //
            this.barBtnRcpCancel.Caption = "접수취소";
            this.barBtnRcpCancel.Id = 20;
            this.barBtnRcpCancel.Name = "barBtnRcpCancel";
            this.barBtnRcpCancel.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnRcpCancel.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnWorkAction_ItemClick);
            //
            // barBtnRcpLog
            //
            this.barBtnRcpLog.Caption = "변경이력";
            this.barBtnRcpLog.Id = 21;
            this.barBtnRcpLog.Name = "barBtnRcpLog";
            this.barBtnRcpLog.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barBtnRcpLog.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnWorkLog_ItemClick);
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
            this.barGroupPatientView,
            this.barGroupPatientBase});
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
            // barPageRsvDesk
            //
            // [검색] 그룹과 [보기] 의 [컬럼설정] 이 없다. 2026-09-10 사용자 결정으로 둘 다
            // 화면 안(WF-WRK-01 의 [조회] 버튼 · Grid 옆 [컬럼 설정] 드롭다운)으로 옮겼다 —
            // 같은 버튼이 두 곳에 있을 이유가 없다. 수검자 Page 가 먼저 같은 길을 갔다.
            this.barPageRsvDesk.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
            this.barGroupRsvWork,
            this.barGroupRsvView,
            this.barGroupRsvBase});
            this.barPageRsvDesk.Name = "barPageRsvDesk";
            this.barPageRsvDesk.Text = "예약 관리";
            //
            // barGroupRsvWork
            //
            this.barGroupRsvWork.ItemLinks.Add(this.barBtnRsvEdit);
            this.barGroupRsvWork.ItemLinks.Add(this.barBtnRsvCancel);
            this.barGroupRsvWork.Name = "barGroupRsvWork";
            this.barGroupRsvWork.Text = "예약 업무";
            //
            // barGroupRsvView
            //
            this.barGroupRsvView.ItemLinks.Add(this.barBtnRsvLog);
            this.barGroupRsvView.Name = "barGroupRsvView";
            this.barGroupRsvView.Text = "보기";
            //
            // barPageRcpDesk
            //
            this.barPageRcpDesk.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
            this.barGroupRcpWork,
            this.barGroupRcpView,
            this.barGroupRcpBase});
            this.barPageRcpDesk.Name = "barPageRcpDesk";
            this.barPageRcpDesk.Text = "접수 관리";
            //
            // barGroupRcpWork
            //
            // [현장 당일예약] 이 없다 — 2026-09-11 grilling. 00 RP-05 가 일반/현장을 **시각**으로
            // 가르므로 버튼을 둘로 두면 조작자가 시계를 대신 읽는다. 예약으로 들어가는 자리는
            // `수검자 관리 [예약]` 하나이고 예약구분은 DB 가 정한다.
            // [예약변경]·[접수] 가 없다 — 2026-09-11 사용자 지시. 둘 다 `RSV` 건을 다루는
            // 명령이라 예약 관리에 있고(05 §8.2 허용조건이 `RSV`), 여기 두면 같은 명령이 두
            // 탭에 생긴다. 탭이 상태로 갈린다: **예약 관리 = 예약 건 · 접수 관리 = 접수 건.**
            // [접수] 가 여기 있다 — 2026-09-11 사용자 지시. 접수 Page 의 목록이 오늘의
            // `RSV` 를 담으므로 (WorkbenchPresenter.StatusesOf) 접수할 대상이 이 탭에 있다.
            // 지난 회차에 이 버튼을 걷은 것은 판단이 틀렸다: 고쳤어야 하는 것은 목록이다.
            this.barGroupRcpWork.ItemLinks.Add(this.barBtnRcpStart);
            this.barGroupRcpWork.ItemLinks.Add(this.barBtnRcpExtra);
            this.barGroupRcpWork.ItemLinks.Add(this.barBtnRcpCancel);
            this.barGroupRcpWork.Name = "barGroupRcpWork";
            this.barGroupRcpWork.Text = "접수 업무";
            //
            // barGroupRcpView
            //
            this.barGroupRcpView.ItemLinks.Add(this.barBtnRcpLog);
            this.barGroupRcpView.Name = "barGroupRcpView";
            this.barGroupRcpView.Text = "보기";
            //
            // barBtnHolidayManage
            //
            // 03 §24.2 — 업무 Tab 을 열지 않고 DLG-HOL-01 Modal 을 직접 연다. 기준정보이므로
            // 업무 페이지의 동배가 아니고, 페이지 머리줄에 늘 보이는 버튼 하나로 둔다.
            this.barBtnHolidayManage.Caption = "휴무일 관리";
            this.barBtnHolidayManage.Id = 25;
            this.barBtnHolidayManage.Name = "barBtnHolidayManage";
            // [X] 페이지 머리줄의 기본 스타일은 **아이콘만**이다. 아이콘이 없으니 빈 네모
            //     하나가 되어 무엇인지 알 수 없었다 (2026-09-11 캡처 실측). 글자를 세운다.
            this.barBtnHolidayManage.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            // [X] 처음엔 PageHeaderItemLinks 에 달았다. 이 DevExpress 버전은 그 자리를
            //     **아이콘만**으로 그려서 RibbonStyle 도 PaintStyle 도 듣지 않았고, 글자
            //     없는 작은 네모 하나가 남았다 (2026-09-11 캡처 실측). 항목은 하나로 두고
            //     링크만 세 페이지에 건다 — 어느 탭에서든 글자로 보이고 탭 흉내도 안 낸다.
            //
            // barGroupPatientBase
            //
            this.barGroupPatientBase.ItemLinks.Add(this.barBtnHolidayManage);
            this.barGroupPatientBase.Name = "barGroupPatientBase";
            this.barGroupPatientBase.Text = "기준정보";
            //
            // barGroupRsvBase
            //
            this.barGroupRsvBase.ItemLinks.Add(this.barBtnHolidayManage);
            this.barGroupRsvBase.Name = "barGroupRsvBase";
            this.barGroupRsvBase.Text = "기준정보";
            //
            // barGroupRcpBase
            //
            this.barGroupRcpBase.ItemLinks.Add(this.barBtnHolidayManage);
            this.barGroupRcpBase.Name = "barGroupRcpBase";
            this.barGroupRcpBase.Text = "기준정보";
            this.barBtnHolidayManage.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnHolidayManage_ItemClick);
            //
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
        private DevExpress.XtraBars.Ribbon.RibbonPage barPageRsvDesk;
        private DevExpress.XtraBars.Ribbon.RibbonPage barPageRcpDesk;
        private DevExpress.XtraBars.BarButtonItem barBtnHolidayManage;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupPatientBase;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupRsvBase;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupRcpBase;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupPatientWork;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupPatientView;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupRsvWork;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupRsvView;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupRcpWork;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupRcpView;
        private DevExpress.XtraBars.BarButtonItem barBtnPatientNew;
        private DevExpress.XtraBars.BarButtonItem barBtnPatientEdit;
        private DevExpress.XtraBars.BarButtonItem barBtnPatientReserve;
        private DevExpress.XtraBars.BarButtonItem barBtnPatientLog;
        private DevExpress.XtraBars.BarButtonItem barBtnRsvEdit;
        private DevExpress.XtraBars.BarButtonItem barBtnRsvCancel;
        private DevExpress.XtraBars.BarButtonItem barBtnRcpStart;
        private DevExpress.XtraBars.BarButtonItem barBtnRsvLog;
        private DevExpress.XtraBars.BarButtonItem barBtnRcpExtra;
        private DevExpress.XtraBars.BarButtonItem barBtnRcpCancel;
        private DevExpress.XtraBars.BarButtonItem barBtnRcpLog;
        private DevExpress.XtraBars.BarStaticItem barStaticWorkStatus;
        private DevExpress.XtraBars.BarStaticItem barStaticOperator;
        private DevExpress.XtraEditors.PanelControl pnlBusiness;
    }
}
