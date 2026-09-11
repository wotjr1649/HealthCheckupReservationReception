// 화면 ID: DLG-RCP-01 — 접수 처리 (03 §11)
namespace HealthCheckupReservationReception.Views
{
    partial class FrmReception
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
            this.lcMain = new DevExpress.XtraLayout.LayoutControl();
            this.txtChartNo = new DevExpress.XtraEditors.TextEdit();
            this.txtName = new DevExpress.XtraEditors.TextEdit();
            this.txtBirthGender = new DevExpress.XtraEditors.TextEdit();
            this.txtMobilePhone = new DevExpress.XtraEditors.TextEdit();
            this.txtSchedule = new DevExpress.XtraEditors.TextEdit();
            this.txtStatus = new DevExpress.XtraEditors.TextEdit();
            this.txtCapacity = new DevExpress.XtraEditors.TextEdit();
            this.gcNex = new DevExpress.XtraGrid.GridControl();
            this.gvNex = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colNexName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colNexType = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gcAex = new DevExpress.XtraGrid.GridControl();
            this.gvAex = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colAexName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.lblEligibility = new DevExpress.XtraEditors.LabelControl();
            this.lblValidation = new DevExpress.XtraEditors.LabelControl();
            this.btnReceive = new DevExpress.XtraEditors.SimpleButton();
            this.btnClose = new DevExpress.XtraEditors.SimpleButton();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgPatient = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciChartNo = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciName = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciBirthGender = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciMobilePhone = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgWork = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciSchedule = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciStatus = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciCapacity = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceWork = new DevExpress.XtraLayout.EmptySpaceItem();
            this.lcgNex = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciNex = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgAex = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciAex = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgActions = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciEligibility = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciValidation = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceActions = new DevExpress.XtraLayout.EmptySpaceItem();
            this.lciReceive = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciClose = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).BeginInit();
            this.lcMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtBirthGender.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSchedule.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtStatus.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtCapacity.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcNex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvNex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcAex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvAex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgPatient)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciChartNo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciBirthGender)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciMobilePhone)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgWork)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSchedule)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciStatus)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciCapacity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceWork)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgNex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciNex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgAex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciAex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgActions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciEligibility)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciValidation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceActions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciReceive)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciClose)).BeginInit();
            this.SuspendLayout();
            //
            // lcMain
            //
            this.lcMain.Controls.Add(this.txtChartNo);
            this.lcMain.Controls.Add(this.txtName);
            this.lcMain.Controls.Add(this.txtBirthGender);
            this.lcMain.Controls.Add(this.txtMobilePhone);
            this.lcMain.Controls.Add(this.txtSchedule);
            this.lcMain.Controls.Add(this.txtStatus);
            this.lcMain.Controls.Add(this.txtCapacity);
            this.lcMain.Controls.Add(this.gcNex);
            this.lcMain.Controls.Add(this.gcAex);
            this.lcMain.Controls.Add(this.lblEligibility);
            this.lcMain.Controls.Add(this.lblValidation);
            this.lcMain.Controls.Add(this.btnReceive);
            this.lcMain.Controls.Add(this.btnClose);
            this.lcMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lcMain.Location = new System.Drawing.Point(0, 0);
            this.lcMain.Name = "lcMain";
            this.lcMain.Root = this.Root;
            this.lcMain.Size = new System.Drawing.Size(760, 470);
            this.lcMain.TabIndex = 0;
            //
            // txtChartNo
            //
            this.txtChartNo.Location = new System.Drawing.Point(100, 36);
            this.txtChartNo.Name = "txtChartNo";
            this.txtChartNo.Properties.ReadOnly = true;
            this.txtChartNo.Size = new System.Drawing.Size(250, 20);
            this.txtChartNo.StyleController = this.lcMain;
            this.txtChartNo.TabIndex = 0;
            //
            // txtName
            //
            this.txtName.Location = new System.Drawing.Point(100, 60);
            this.txtName.Name = "txtName";
            this.txtName.Properties.ReadOnly = true;
            this.txtName.Size = new System.Drawing.Size(250, 20);
            this.txtName.StyleController = this.lcMain;
            this.txtName.TabIndex = 1;
            //
            // txtBirthGender
            //
            this.txtBirthGender.Location = new System.Drawing.Point(100, 84);
            this.txtBirthGender.Name = "txtBirthGender";
            this.txtBirthGender.Properties.ReadOnly = true;
            this.txtBirthGender.Size = new System.Drawing.Size(250, 20);
            this.txtBirthGender.StyleController = this.lcMain;
            this.txtBirthGender.TabIndex = 2;
            //
            // txtMobilePhone
            //
            this.txtMobilePhone.Location = new System.Drawing.Point(100, 108);
            this.txtMobilePhone.Name = "txtMobilePhone";
            this.txtMobilePhone.Properties.ReadOnly = true;
            this.txtMobilePhone.Size = new System.Drawing.Size(250, 20);
            this.txtMobilePhone.StyleController = this.lcMain;
            this.txtMobilePhone.TabIndex = 3;
            //
            // txtSchedule
            //
            this.txtSchedule.Location = new System.Drawing.Point(462, 36);
            this.txtSchedule.Name = "txtSchedule";
            this.txtSchedule.Properties.ReadOnly = true;
            this.txtSchedule.Size = new System.Drawing.Size(250, 20);
            this.txtSchedule.StyleController = this.lcMain;
            this.txtSchedule.TabIndex = 4;
            //
            // txtStatus
            //
            this.txtStatus.Location = new System.Drawing.Point(462, 60);
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.Properties.ReadOnly = true;
            this.txtStatus.Size = new System.Drawing.Size(250, 20);
            this.txtStatus.StyleController = this.lcMain;
            this.txtStatus.TabIndex = 5;
            //
            // txtCapacity
            //
            this.txtCapacity.Location = new System.Drawing.Point(462, 84);
            this.txtCapacity.Name = "txtCapacity";
            this.txtCapacity.Properties.ReadOnly = true;
            this.txtCapacity.Size = new System.Drawing.Size(250, 20);
            this.txtCapacity.StyleController = this.lcMain;
            this.txtCapacity.TabIndex = 6;
            //
            // gcNex
            //
            this.gcNex.Location = new System.Drawing.Point(24, 168);
            this.gcNex.MainView = this.gvNex;
            this.gcNex.Name = "gcNex";
            this.gcNex.Size = new System.Drawing.Size(326, 208);
            this.gcNex.TabIndex = 7;
            this.gcNex.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvNex});
            //
            // gvNex
            //
            this.gvNex.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colNexName,
            this.colNexType});
            this.gvNex.GridControl = this.gcNex;
            this.gvNex.Name = "gvNex";
            this.gvNex.OptionsBehavior.AutoPopulateColumns = false;
            this.gvNex.OptionsBehavior.Editable = false;
            this.gvNex.OptionsSelection.MultiSelect = false;
            this.gvNex.OptionsView.ShowGroupPanel = false;
            this.gvNex.OptionsView.ShowIndicator = false;
            //
            // colNexName
            //
            this.colNexName.Caption = "검사명";
            this.colNexName.FieldName = "ExamItemName";
            this.colNexName.Name = "colNexName";
            this.colNexName.MinWidth = 160;
            this.colNexName.Visible = true;
            this.colNexName.VisibleIndex = 0;
            this.colNexName.Width = 200;
            //
            // colNexType
            //
            this.colNexType.Caption = "구분";
            this.colNexType.FieldName = "NexType";
            this.colNexType.Name = "colNexType";
            this.colNexType.MinWidth = 70;
            this.colNexType.Visible = true;
            this.colNexType.VisibleIndex = 1;
            this.colNexType.Width = 70;
            //
            // gcAex
            //
            this.gcAex.Location = new System.Drawing.Point(386, 168);
            this.gcAex.MainView = this.gvAex;
            this.gcAex.Name = "gcAex";
            this.gcAex.Size = new System.Drawing.Size(326, 208);
            this.gcAex.TabIndex = 8;
            this.gcAex.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvAex});
            //
            // gvAex
            //
            this.gvAex.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colAexName});
            this.gvAex.GridControl = this.gcAex;
            this.gvAex.Name = "gvAex";
            this.gvAex.OptionsBehavior.AutoPopulateColumns = false;
            this.gvAex.OptionsBehavior.Editable = false;
            this.gvAex.OptionsSelection.MultiSelect = false;
            this.gvAex.OptionsView.ShowGroupPanel = false;
            this.gvAex.OptionsView.ShowIndicator = false;
            //
            // colAexName
            //
            this.colAexName.Caption = "검사명";
            this.colAexName.FieldName = "ExamItemName";
            this.colAexName.Name = "colAexName";
            this.colAexName.MinWidth = 200;
            this.colAexName.Visible = true;
            this.colAexName.VisibleIndex = 0;
            this.colAexName.Width = 280;
            //
            // lblEligibility
            //
            this.lblEligibility.Location = new System.Drawing.Point(24, 392);
            this.lblEligibility.Name = "lblEligibility";
            this.lblEligibility.Size = new System.Drawing.Size(0, 14);
            this.lblEligibility.StyleController = this.lcMain;
            this.lblEligibility.TabIndex = 9;
            //
            // lblValidation
            //
            this.lblValidation.Location = new System.Drawing.Point(24, 412);
            this.lblValidation.Name = "lblValidation";
            this.lblValidation.Size = new System.Drawing.Size(0, 14);
            this.lblValidation.StyleController = this.lcMain;
            this.lblValidation.TabIndex = 10;
            //
            // btnReceive
            //
            this.btnReceive.Location = new System.Drawing.Point(544, 432);
            this.btnReceive.Name = "btnReceive";
            this.btnReceive.Size = new System.Drawing.Size(96, 26);
            this.btnReceive.StyleController = this.lcMain;
            this.btnReceive.TabIndex = 11;
            this.btnReceive.Text = "접수처리";
            this.btnReceive.Click += new System.EventHandler(this.btnReceive_Click);
            //
            // btnClose
            //
            this.btnClose.Location = new System.Drawing.Point(644, 432);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(96, 26);
            this.btnClose.StyleController = this.lcMain;
            this.btnClose.TabIndex = 12;
            this.btnClose.Text = "닫기";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // Root
            //
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lcgPatient,
            this.lcgWork,
            this.lcgNex,
            this.lcgAex,
            this.lcgActions});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(760, 470);
            this.Root.TextVisible = false;
            //
            // lcgPatient
            //
            this.lcgPatient.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciChartNo,
            this.lciName,
            this.lciBirthGender,
            this.lciMobilePhone});
            this.lcgPatient.Location = new System.Drawing.Point(0, 0);
            this.lcgPatient.Name = "lcgPatient";
            this.lcgPatient.Size = new System.Drawing.Size(362, 144);
            this.lcgPatient.Text = "수검자 정보";
            //
            // lciChartNo
            //
            this.lciChartNo.Control = this.txtChartNo;
            this.lciChartNo.Location = new System.Drawing.Point(0, 0);
            this.lciChartNo.MaxSize = new System.Drawing.Size(0, 24);
            this.lciChartNo.MinSize = new System.Drawing.Size(150, 24);
            this.lciChartNo.Name = "lciChartNo";
            this.lciChartNo.Size = new System.Drawing.Size(338, 24);
            this.lciChartNo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciChartNo.Text = "차트번호";
            this.lciChartNo.TextSize = new System.Drawing.Size(72, 14);
            //
            // lciName
            //
            this.lciName.Control = this.txtName;
            this.lciName.Location = new System.Drawing.Point(0, 24);
            this.lciName.MaxSize = new System.Drawing.Size(0, 24);
            this.lciName.MinSize = new System.Drawing.Size(150, 24);
            this.lciName.Name = "lciName";
            this.lciName.Size = new System.Drawing.Size(338, 24);
            this.lciName.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciName.Text = "이름";
            this.lciName.TextSize = new System.Drawing.Size(72, 14);
            //
            // lciBirthGender
            //
            this.lciBirthGender.Control = this.txtBirthGender;
            this.lciBirthGender.Location = new System.Drawing.Point(0, 48);
            this.lciBirthGender.MaxSize = new System.Drawing.Size(0, 24);
            this.lciBirthGender.MinSize = new System.Drawing.Size(150, 24);
            this.lciBirthGender.Name = "lciBirthGender";
            this.lciBirthGender.Size = new System.Drawing.Size(338, 24);
            this.lciBirthGender.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciBirthGender.Text = "생년월일 / 성별";
            this.lciBirthGender.TextSize = new System.Drawing.Size(72, 14);
            //
            // lciMobilePhone
            //
            this.lciMobilePhone.Control = this.txtMobilePhone;
            this.lciMobilePhone.Location = new System.Drawing.Point(0, 72);
            this.lciMobilePhone.MaxSize = new System.Drawing.Size(0, 24);
            this.lciMobilePhone.MinSize = new System.Drawing.Size(150, 24);
            this.lciMobilePhone.Name = "lciMobilePhone";
            this.lciMobilePhone.Size = new System.Drawing.Size(338, 24);
            this.lciMobilePhone.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciMobilePhone.Text = "휴대전화";
            this.lciMobilePhone.TextSize = new System.Drawing.Size(72, 14);
            //
            // lcgWork
            //
            this.lcgWork.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciSchedule,
            this.lciStatus,
            this.lciCapacity,
            this.emptySpaceWork});
            this.lcgWork.Location = new System.Drawing.Point(362, 0);
            this.lcgWork.Name = "lcgWork";
            this.lcgWork.Size = new System.Drawing.Size(362, 144);
            this.lcgWork.Text = "예약 정보";
            //
            // lciSchedule
            //
            this.lciSchedule.Control = this.txtSchedule;
            this.lciSchedule.Location = new System.Drawing.Point(0, 0);
            this.lciSchedule.MaxSize = new System.Drawing.Size(0, 24);
            this.lciSchedule.MinSize = new System.Drawing.Size(150, 24);
            this.lciSchedule.Name = "lciSchedule";
            this.lciSchedule.Size = new System.Drawing.Size(338, 24);
            this.lciSchedule.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciSchedule.Text = "예약일 / 시간대";
            this.lciSchedule.TextSize = new System.Drawing.Size(72, 14);
            //
            // lciStatus
            //
            this.lciStatus.Control = this.txtStatus;
            this.lciStatus.Location = new System.Drawing.Point(0, 24);
            this.lciStatus.MaxSize = new System.Drawing.Size(0, 24);
            this.lciStatus.MinSize = new System.Drawing.Size(150, 24);
            this.lciStatus.Name = "lciStatus";
            this.lciStatus.Size = new System.Drawing.Size(338, 24);
            this.lciStatus.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciStatus.Text = "상태";
            this.lciStatus.TextSize = new System.Drawing.Size(72, 14);
            //
            // lciCapacity
            //
            this.lciCapacity.Control = this.txtCapacity;
            this.lciCapacity.Location = new System.Drawing.Point(0, 48);
            this.lciCapacity.MaxSize = new System.Drawing.Size(0, 24);
            this.lciCapacity.MinSize = new System.Drawing.Size(150, 24);
            this.lciCapacity.Name = "lciCapacity";
            this.lciCapacity.Size = new System.Drawing.Size(338, 24);
            this.lciCapacity.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciCapacity.Text = "정원현황";
            this.lciCapacity.TextSize = new System.Drawing.Size(72, 14);
            //
            // emptySpaceWork
            //
            this.emptySpaceWork.AllowHotTrack = false;
            this.emptySpaceWork.Location = new System.Drawing.Point(0, 72);
            this.emptySpaceWork.Name = "emptySpaceWork";
            this.emptySpaceWork.Size = new System.Drawing.Size(338, 24);
            this.emptySpaceWork.TextSize = new System.Drawing.Size(0, 0);
            //
            // lcgNex
            //
            this.lcgNex.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciNex});
            this.lcgNex.Location = new System.Drawing.Point(0, 144);
            this.lcgNex.Name = "lcgNex";
            this.lcgNex.Size = new System.Drawing.Size(362, 232);
            this.lcgNex.Text = "국가검사 (ReadOnly)";
            //
            // lciNex
            //
            this.lciNex.Control = this.gcNex;
            this.lciNex.Location = new System.Drawing.Point(0, 0);
            this.lciNex.Name = "lciNex";
            this.lciNex.Size = new System.Drawing.Size(338, 208);
            this.lciNex.TextSize = new System.Drawing.Size(0, 0);
            this.lciNex.TextVisible = false;
            //
            // lcgAex
            //
            this.lcgAex.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciAex});
            this.lcgAex.Location = new System.Drawing.Point(362, 144);
            this.lcgAex.Name = "lcgAex";
            this.lcgAex.Size = new System.Drawing.Size(362, 232);
            this.lcgAex.Text = "추가검사 (ReadOnly)";
            //
            // lciAex
            //
            this.lciAex.Control = this.gcAex;
            this.lciAex.Location = new System.Drawing.Point(0, 0);
            this.lciAex.Name = "lciAex";
            this.lciAex.Size = new System.Drawing.Size(338, 208);
            this.lciAex.TextSize = new System.Drawing.Size(0, 0);
            this.lciAex.TextVisible = false;
            //
            // lcgActions
            //
            this.lcgActions.GroupBordersVisible = false;
            this.lcgActions.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciEligibility,
            this.lciValidation,
            this.emptySpaceActions,
            this.lciReceive,
            this.lciClose});
            this.lcgActions.Location = new System.Drawing.Point(0, 376);
            this.lcgActions.Name = "lcgActions";
            this.lcgActions.Size = new System.Drawing.Size(724, 74);
            this.lcgActions.TextVisible = false;
            //
            // lciEligibility
            //
            this.lciEligibility.Control = this.lblEligibility;
            this.lciEligibility.Location = new System.Drawing.Point(0, 0);
            this.lciEligibility.MaxSize = new System.Drawing.Size(0, 20);
            this.lciEligibility.MinSize = new System.Drawing.Size(104, 20);
            this.lciEligibility.Name = "lciEligibility";
            this.lciEligibility.Size = new System.Drawing.Size(724, 20);
            this.lciEligibility.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciEligibility.TextVisible = false;
            //
            // lciValidation
            //
            this.lciValidation.Control = this.lblValidation;
            this.lciValidation.Location = new System.Drawing.Point(0, 20);
            this.lciValidation.MaxSize = new System.Drawing.Size(0, 20);
            this.lciValidation.MinSize = new System.Drawing.Size(104, 20);
            this.lciValidation.Name = "lciValidation";
            this.lciValidation.Size = new System.Drawing.Size(724, 20);
            this.lciValidation.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciValidation.TextVisible = false;
            //
            // emptySpaceActions
            //
            this.emptySpaceActions.AllowHotTrack = false;
            this.emptySpaceActions.Location = new System.Drawing.Point(0, 40);
            this.emptySpaceActions.Name = "emptySpaceActions";
            this.emptySpaceActions.Size = new System.Drawing.Size(524, 34);
            this.emptySpaceActions.TextSize = new System.Drawing.Size(0, 0);
            //
            // lciReceive
            //
            this.lciReceive.Control = this.btnReceive;
            this.lciReceive.Location = new System.Drawing.Point(524, 40);
            this.lciReceive.MaxSize = new System.Drawing.Size(100, 34);
            this.lciReceive.MinSize = new System.Drawing.Size(100, 34);
            this.lciReceive.Name = "lciReceive";
            this.lciReceive.Size = new System.Drawing.Size(100, 34);
            this.lciReceive.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciReceive.TextVisible = false;
            //
            // lciClose
            //
            this.lciClose.Control = this.btnClose;
            this.lciClose.Location = new System.Drawing.Point(624, 40);
            this.lciClose.MaxSize = new System.Drawing.Size(100, 34);
            this.lciClose.MinSize = new System.Drawing.Size(100, 34);
            this.lciClose.Name = "lciClose";
            this.lciClose.Size = new System.Drawing.Size(100, 34);
            this.lciClose.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciClose.TextVisible = false;
            //
            // FrmReception
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(760, 470);
            this.Controls.Add(this.lcMain);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(700, 460);
            this.Name = "FrmReception";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "접수 처리";
            ((System.ComponentModel.ISupportInitialize)(this.lciClose)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciReceive)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciValidation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciEligibility)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciAex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgAex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciNex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgNex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceWork)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciCapacity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciStatus)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSchedule)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgWork)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciMobilePhone)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciBirthGender)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciChartNo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgPatient)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvAex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcAex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvNex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcNex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtCapacity.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtStatus.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSchedule.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtBirthGender.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).EndInit();
            this.lcMain.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl lcMain;
        private DevExpress.XtraEditors.TextEdit txtChartNo;
        private DevExpress.XtraEditors.TextEdit txtName;
        private DevExpress.XtraEditors.TextEdit txtBirthGender;
        private DevExpress.XtraEditors.TextEdit txtMobilePhone;
        private DevExpress.XtraEditors.TextEdit txtSchedule;
        private DevExpress.XtraEditors.TextEdit txtStatus;
        private DevExpress.XtraEditors.TextEdit txtCapacity;
        private DevExpress.XtraGrid.GridControl gcNex;
        private DevExpress.XtraGrid.Views.Grid.GridView gvNex;
        private DevExpress.XtraGrid.Columns.GridColumn colNexName;
        private DevExpress.XtraGrid.Columns.GridColumn colNexType;
        private DevExpress.XtraGrid.GridControl gcAex;
        private DevExpress.XtraGrid.Views.Grid.GridView gvAex;
        private DevExpress.XtraGrid.Columns.GridColumn colAexName;
        private DevExpress.XtraEditors.LabelControl lblEligibility;
        private DevExpress.XtraEditors.LabelControl lblValidation;
        private DevExpress.XtraEditors.SimpleButton btnReceive;
        private DevExpress.XtraEditors.SimpleButton btnClose;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraLayout.LayoutControlGroup lcgPatient;
        private DevExpress.XtraLayout.LayoutControlItem lciChartNo;
        private DevExpress.XtraLayout.LayoutControlItem lciName;
        private DevExpress.XtraLayout.LayoutControlItem lciBirthGender;
        private DevExpress.XtraLayout.LayoutControlItem lciMobilePhone;
        private DevExpress.XtraLayout.LayoutControlGroup lcgWork;
        private DevExpress.XtraLayout.LayoutControlItem lciSchedule;
        private DevExpress.XtraLayout.LayoutControlItem lciStatus;
        private DevExpress.XtraLayout.LayoutControlItem lciCapacity;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceWork;
        private DevExpress.XtraLayout.LayoutControlGroup lcgNex;
        private DevExpress.XtraLayout.LayoutControlItem lciNex;
        private DevExpress.XtraLayout.LayoutControlGroup lcgAex;
        private DevExpress.XtraLayout.LayoutControlItem lciAex;
        private DevExpress.XtraLayout.LayoutControlGroup lcgActions;
        private DevExpress.XtraLayout.LayoutControlItem lciEligibility;
        private DevExpress.XtraLayout.LayoutControlItem lciValidation;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceActions;
        private DevExpress.XtraLayout.LayoutControlItem lciReceive;
        private DevExpress.XtraLayout.LayoutControlItem lciClose;
    }
}
