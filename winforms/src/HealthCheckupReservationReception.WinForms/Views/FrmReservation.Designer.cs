// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
namespace HealthCheckupReservationReception.Views
{
    partial class FrmReservation
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
            this.txtPatientChartNo = new DevExpress.XtraEditors.TextEdit();
            this.txtPatientName = new DevExpress.XtraEditors.TextEdit();
            this.txtPatientBirthGender = new DevExpress.XtraEditors.TextEdit();
            this.deReserveDate = new DevExpress.XtraEditors.DateEdit();
            this.rgSlot = new DevExpress.XtraEditors.RadioGroup();
            this.lblTarget = new DevExpress.XtraEditors.LabelControl();
            this.gcNexList = new DevExpress.XtraGrid.GridControl();
            this.gvNexList = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colNexName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colNexType = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gcAexList = new DevExpress.XtraGrid.GridControl();
            this.gvAexList = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colAexChecked = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colAexName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colAexReason = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repoChkAex = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.lblBlock = new DevExpress.XtraEditors.LabelControl();
            this.btnSave = new DevExpress.XtraEditors.SimpleButton();
            this.btnClose = new DevExpress.XtraEditors.SimpleButton();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgTop = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgPatient = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciPatientChartNo = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciPatientName = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciPatientBirthGender = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptyPatient = new DevExpress.XtraLayout.EmptySpaceItem();
            this.splitTop = new DevExpress.XtraLayout.SplitterItem();
            this.lcgSchedule = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciReserveDate = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciSlot = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciTarget = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgBottom = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgNex = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciNexList = new DevExpress.XtraLayout.LayoutControlItem();
            this.splitBottom = new DevExpress.XtraLayout.SplitterItem();
            this.lcgAex = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciAexList = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciBlock = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgActions = new DevExpress.XtraLayout.LayoutControlGroup();
            this.emptySpaceActions = new DevExpress.XtraLayout.EmptySpaceItem();
            this.lciSave = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciClose = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).BeginInit();
            this.lcMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtPatientChartNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPatientName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPatientBirthGender.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deReserveDate.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deReserveDate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rgSlot.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcNexList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvNexList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcAexList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvAexList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repoChkAex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgTop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgPatient)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciPatientChartNo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciPatientName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciPatientBirthGender)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptyPatient)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitTop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgSchedule)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciReserveDate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSlot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciTarget)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgBottom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgNex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciNexList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitBottom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgAex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciAexList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciBlock)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgActions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceActions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSave)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciClose)).BeginInit();
            this.SuspendLayout();
            //
            // lcMain
            //
            this.lcMain.Controls.Add(this.txtPatientChartNo);
            this.lcMain.Controls.Add(this.txtPatientName);
            this.lcMain.Controls.Add(this.txtPatientBirthGender);
            this.lcMain.Controls.Add(this.deReserveDate);
            this.lcMain.Controls.Add(this.rgSlot);
            this.lcMain.Controls.Add(this.lblTarget);
            this.lcMain.Controls.Add(this.gcNexList);
            this.lcMain.Controls.Add(this.gcAexList);
            this.lcMain.Controls.Add(this.lblBlock);
            this.lcMain.Controls.Add(this.btnSave);
            this.lcMain.Controls.Add(this.btnClose);
            this.lcMain.AllowCustomization = false;
            this.lcMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lcMain.Location = new System.Drawing.Point(0, 0);
            this.lcMain.Name = "lcMain";
            this.lcMain.Root = this.Root;
            this.lcMain.Size = new System.Drawing.Size(1100, 700);
            this.lcMain.TabIndex = 0;
            //
            // txtPatientChartNo
            //
            // 03 §8.3 수검자 정보. 모달은 대상을 받고 열리므로 여기서 바꾸지 않는다.
            this.txtPatientChartNo.Location = new System.Drawing.Point(108, 43);
            this.txtPatientChartNo.Name = "txtPatientChartNo";
            this.txtPatientChartNo.Properties.ReadOnly = true;
            this.txtPatientChartNo.Size = new System.Drawing.Size(370, 20);
            this.txtPatientChartNo.StyleController = this.lcMain;
            this.txtPatientChartNo.TabIndex = 0;
            this.txtPatientChartNo.TabStop = false;
            //
            // txtPatientName
            //
            this.txtPatientName.Location = new System.Drawing.Point(108, 67);
            this.txtPatientName.Name = "txtPatientName";
            this.txtPatientName.Properties.ReadOnly = true;
            this.txtPatientName.Size = new System.Drawing.Size(370, 20);
            this.txtPatientName.StyleController = this.lcMain;
            this.txtPatientName.TabIndex = 1;
            this.txtPatientName.TabStop = false;
            //
            // txtPatientBirthGender
            //
            this.txtPatientBirthGender.Location = new System.Drawing.Point(108, 91);
            this.txtPatientBirthGender.Name = "txtPatientBirthGender";
            this.txtPatientBirthGender.Properties.ReadOnly = true;
            this.txtPatientBirthGender.Size = new System.Drawing.Size(370, 20);
            this.txtPatientBirthGender.StyleController = this.lcMain;
            this.txtPatientBirthGender.TabIndex = 2;
            this.txtPatientBirthGender.TabStop = false;
            //
            // deReserveDate
            //
            this.deReserveDate.EditValue = null;
            this.deReserveDate.Location = new System.Drawing.Point(580, 43);
            this.deReserveDate.Name = "deReserveDate";
            this.deReserveDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deReserveDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deReserveDate.Size = new System.Drawing.Size(150, 20);
            this.deReserveDate.StyleController = this.lcMain;
            this.deReserveDate.TabIndex = 3;
            this.deReserveDate.EditValueChanged += new System.EventHandler(this.Schedule_Changed);
            //
            // rgSlot
            //
            // 항목은 Presenter 가 준 Slots 로 그때그때 만든다 — 정원·마감·선택가능이
            // 조회할 때마다 달라진다 (05 §9.7).
            this.rgSlot.Location = new System.Drawing.Point(508, 69);
            this.rgSlot.Name = "rgSlot";
            this.rgSlot.Properties.Columns = 1;
            this.rgSlot.Size = new System.Drawing.Size(568, 68);
            this.rgSlot.StyleController = this.lcMain;
            this.rgSlot.TabIndex = 4;
            this.rgSlot.SelectedIndexChanged += new System.EventHandler(this.Schedule_Changed);
            //
            // lblTarget
            //
            // 03 §8.7 — `대상판정 : 대상 — 최초검진` 한 줄. 굵게는 ConfigureUI 가 준다.
            this.lblTarget.Location = new System.Drawing.Point(12, 221);
            this.lblTarget.Name = "lblTarget";
            this.lblTarget.Size = new System.Drawing.Size(0, 14);
            this.lblTarget.StyleController = this.lcMain;
            this.lblTarget.TabIndex = 5;
            //
            // gcNexList
            //
            this.gcNexList.Location = new System.Drawing.Point(24, 271);
            this.gcNexList.MainView = this.gvNexList;
            this.gcNexList.Name = "gcNexList";
            this.gcNexList.Size = new System.Drawing.Size(552, 351);
            this.gcNexList.TabIndex = 6;
            this.gcNexList.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvNexList});
            //
            // gvNexList
            //
            this.gvNexList.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colNexName,
            this.colNexType});
            this.gvNexList.GridControl = this.gcNexList;
            this.gvNexList.Name = "gvNexList";
            this.gvNexList.OptionsBehavior.AutoPopulateColumns = false;
            // 03 §8.8 — 사용자 추가·삭제 금지. 보는 자리다.
            this.gvNexList.OptionsBehavior.Editable = false;
            this.gvNexList.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.gvNexList.OptionsSelection.EnableAppearanceFocusedRow = false;
            this.gvNexList.OptionsView.ShowGroupPanel = false;
            this.gvNexList.OptionsView.ShowIndicator = false;
            //
            // colNexName
            //
            this.colNexName.Caption = "검사명";
            this.colNexName.FieldName = "ExamItemName";
            this.colNexName.Name = "colNexName";
            this.colNexName.MinWidth = 160;
            this.colNexName.Visible = true;
            this.colNexName.VisibleIndex = 0;
            //
            // colNexType
            //
            this.colNexType.Caption = "구분";
            this.colNexType.FieldName = "NexType";
            this.colNexType.Name = "colNexType";
            this.colNexType.MinWidth = 80;
            this.colNexType.Visible = true;
            this.colNexType.VisibleIndex = 1;
            this.colNexType.Width = 100;
            //
            // gcAexList
            //
            this.gcAexList.Location = new System.Drawing.Point(622, 271);
            this.gcAexList.MainView = this.gvAexList;
            this.gcAexList.Name = "gcAexList";
            this.gcAexList.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repoChkAex});
            this.gcAexList.Size = new System.Drawing.Size(454, 351);
            this.gcAexList.TabIndex = 7;
            this.gcAexList.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvAexList});
            //
            // gvAexList
            //
            this.gvAexList.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colAexChecked,
            this.colAexName,
            this.colAexReason});
            this.gvAexList.GridControl = this.gcAexList;
            this.gvAexList.Name = "gvAexList";
            this.gvAexList.OptionsBehavior.AutoPopulateColumns = false;
            // 03 §8.9 — 여기만 고르는 자리다. 체크 컬럼 하나를 열기 위해 View 는 편집 가능해야 하고,
            // 나머지 두 컬럼은 각자 AllowEdit=false 로 닫는다 (references/designer.md 함정 5).
            this.gvAexList.OptionsBehavior.Editable = true;
            this.gvAexList.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.gvAexList.OptionsView.ShowGroupPanel = false;
            this.gvAexList.OptionsView.ShowIndicator = false;
            this.gvAexList.ShowingEditor += new System.ComponentModel.CancelEventHandler(this.gvAexList_ShowingEditor);
            this.gvAexList.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(this.gvAexList_RowStyle);
            //
            // colAexChecked
            //
            this.colAexChecked.Caption = "선택";
            this.colAexChecked.ColumnEdit = this.repoChkAex;
            this.colAexChecked.FieldName = "Requested";
            this.colAexChecked.Name = "colAexChecked";
            this.colAexChecked.MinWidth = 50;
            this.colAexChecked.Visible = true;
            this.colAexChecked.VisibleIndex = 0;
            this.colAexChecked.Width = 60;
            //
            // colAexName
            //
            this.colAexName.Caption = "검사명";
            this.colAexName.FieldName = "ExamItemName";
            this.colAexName.Name = "colAexName";
            this.colAexName.MinWidth = 140;
            this.colAexName.OptionsColumn.AllowEdit = false;
            this.colAexName.Visible = true;
            this.colAexName.VisibleIndex = 1;
            this.colAexName.Width = 160;
            //
            // colAexReason
            //
            // 03 §8.9 `선택불가 사유` 칸. 성별 불충족·국가검진 포함이 여기로 온다 (05 §9.10).
            this.colAexReason.Caption = "선택불가 사유";
            this.colAexReason.FieldName = "ReasonMessage";
            this.colAexReason.Name = "colAexReason";
            this.colAexReason.MinWidth = 180;
            this.colAexReason.OptionsColumn.AllowEdit = false;
            this.colAexReason.Visible = true;
            this.colAexReason.VisibleIndex = 2;
            //
            // repoChkAex
            //
            this.repoChkAex.AutoHeight = false;
            this.repoChkAex.Name = "repoChkAex";
            //
            // lblBlock
            //
            // 05 §9.6 차단메시지 · 03 §8.11 저장 실패 사유. 붉은 글씨는 ConfigureUI 가 준다.
            this.lblBlock.Location = new System.Drawing.Point(12, 634);
            this.lblBlock.Name = "lblBlock";
            this.lblBlock.Size = new System.Drawing.Size(0, 14);
            this.lblBlock.StyleController = this.lcMain;
            this.lblBlock.TabIndex = 8;
            //
            // btnSave
            //
            // 03 §8.2 의 `[예약저장]`. 2026-09-10 grilling 2회차에서 Ribbon Page 하나를
            // 차지하던 것을 모달 하단으로 내렸다 — DLG-RSV-01(03 §10.1)과 같은 자리다.
            this.btnSave.Location = new System.Drawing.Point(908, 660);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(84, 26);
            this.btnSave.StyleController = this.lcMain;
            this.btnSave.TabIndex = 9;
            this.btnSave.Text = "저장";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // btnClose
            //
            this.btnClose.Location = new System.Drawing.Point(1000, 660);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(84, 26);
            this.btnClose.StyleController = this.lcMain;
            this.btnClose.TabIndex = 10;
            this.btnClose.Text = "닫기";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // Root
            //
            this.Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lcgTop,
            this.lciTarget,
            this.lcgBottom,
            this.lciBlock,
            this.lcgActions});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(1100, 700);
            this.Root.TextVisible = false;
            //
            // lcgTop
            //
            // 03 §8.4 — 상단 수검자/일정 30~35%. 내부는 수검자 45 : 일정 55.
            this.lcgTop.GroupBordersVisible = false;
            this.lcgTop.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lcgPatient,
            this.splitTop,
            this.lcgSchedule});
            this.lcgTop.Location = new System.Drawing.Point(0, 0);
            this.lcgTop.Name = "lcgTop";
            this.lcgTop.Size = new System.Drawing.Size(1100, 160);
            this.lcgTop.TextVisible = false;
            //
            // lcgPatient
            //
            this.lcgPatient.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciPatientChartNo,
            this.lciPatientName,
            this.lciPatientBirthGender,
            this.emptyPatient});
            this.lcgPatient.Location = new System.Drawing.Point(0, 0);
            this.lcgPatient.Name = "lcgPatient";
            this.lcgPatient.OptionsItemText.TextAlignMode = DevExpress.XtraLayout.TextAlignModeGroup.AlignWithChildren;
            this.lcgPatient.Size = new System.Drawing.Size(490, 160);
            this.lcgPatient.Text = "수검자";
            //
            // lciPatientChartNo
            //
            // [X] 캡션·테두리가 있는 그룹 안에서는 폭을 못 박지 않는다 — 그룹의 안쪽 폭이
            //     그룹 폭보다 작아 넘친다(DLG-PAT-03 실측). 높이만 고정하고 폭은 맡긴다.
            this.lciPatientChartNo.Control = this.txtPatientChartNo;
            this.lciPatientChartNo.Location = new System.Drawing.Point(0, 0);
            this.lciPatientChartNo.MaxSize = new System.Drawing.Size(0, 24);
            this.lciPatientChartNo.MinSize = new System.Drawing.Size(150, 24);
            this.lciPatientChartNo.Name = "lciPatientChartNo";
            this.lciPatientChartNo.Size = new System.Drawing.Size(490, 24);
            this.lciPatientChartNo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciPatientChartNo.Text = "차트번호";
            this.lciPatientChartNo.TextSize = new System.Drawing.Size(84, 14);
            //
            // lciPatientName
            //
            this.lciPatientName.Control = this.txtPatientName;
            this.lciPatientName.Location = new System.Drawing.Point(0, 24);
            this.lciPatientName.MaxSize = new System.Drawing.Size(0, 24);
            this.lciPatientName.MinSize = new System.Drawing.Size(150, 24);
            this.lciPatientName.Name = "lciPatientName";
            this.lciPatientName.Size = new System.Drawing.Size(490, 24);
            this.lciPatientName.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciPatientName.Text = "이름";
            this.lciPatientName.TextSize = new System.Drawing.Size(84, 14);
            //
            // lciPatientBirthGender
            //
            this.lciPatientBirthGender.Control = this.txtPatientBirthGender;
            this.lciPatientBirthGender.Location = new System.Drawing.Point(0, 48);
            this.lciPatientBirthGender.MaxSize = new System.Drawing.Size(0, 24);
            this.lciPatientBirthGender.MinSize = new System.Drawing.Size(150, 24);
            this.lciPatientBirthGender.Name = "lciPatientBirthGender";
            this.lciPatientBirthGender.Size = new System.Drawing.Size(490, 24);
            this.lciPatientBirthGender.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciPatientBirthGender.Text = "생년월일 / 성별";
            this.lciPatientBirthGender.TextSize = new System.Drawing.Size(84, 14);
            //
            // emptyPatient
            //
            this.emptyPatient.AllowHotTrack = false;
            this.emptyPatient.Location = new System.Drawing.Point(0, 72);
            this.emptyPatient.Name = "emptyPatient";
            this.emptyPatient.Size = new System.Drawing.Size(490, 88);
            this.emptyPatient.TextSize = new System.Drawing.Size(0, 0);
            //
            // splitTop
            //
            this.splitTop.AllowHotTrack = true;
            this.splitTop.Location = new System.Drawing.Point(490, 0);
            this.splitTop.Name = "splitTop";
            this.splitTop.Size = new System.Drawing.Size(10, 160);
            //
            // lcgSchedule
            //
            this.lcgSchedule.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciReserveDate,
            this.lciSlot});
            this.lcgSchedule.Location = new System.Drawing.Point(500, 0);
            this.lcgSchedule.Name = "lcgSchedule";
            this.lcgSchedule.Size = new System.Drawing.Size(600, 160);
            this.lcgSchedule.Text = "예약 일정";
            //
            // lciReserveDate
            //
            this.lciReserveDate.Control = this.deReserveDate;
            this.lciReserveDate.Location = new System.Drawing.Point(0, 0);
            this.lciReserveDate.MaxSize = new System.Drawing.Size(0, 26);
            this.lciReserveDate.MinSize = new System.Drawing.Size(220, 26);
            this.lciReserveDate.Name = "lciReserveDate";
            this.lciReserveDate.Size = new System.Drawing.Size(600, 26);
            this.lciReserveDate.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciReserveDate.Text = "예약일";
            this.lciReserveDate.TextSize = new System.Drawing.Size(60, 14);
            //
            // lciSlot
            //
            this.lciSlot.Control = this.rgSlot;
            this.lciSlot.Location = new System.Drawing.Point(0, 26);
            this.lciSlot.MaxSize = new System.Drawing.Size(0, 68);
            this.lciSlot.MinSize = new System.Drawing.Size(150, 68);
            this.lciSlot.Name = "lciSlot";
            this.lciSlot.Size = new System.Drawing.Size(600, 68);
            this.lciSlot.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciSlot.TextSize = new System.Drawing.Size(0, 0);
            this.lciSlot.TextVisible = false;
            //
            // lciTarget
            //
            this.lciTarget.Control = this.lblTarget;
            this.lciTarget.Location = new System.Drawing.Point(0, 160);
            this.lciTarget.MaxSize = new System.Drawing.Size(0, 26);
            this.lciTarget.MinSize = new System.Drawing.Size(104, 26);
            this.lciTarget.Name = "lciTarget";
            this.lciTarget.Size = new System.Drawing.Size(1100, 26);
            this.lciTarget.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciTarget.TextSize = new System.Drawing.Size(0, 0);
            this.lciTarget.TextVisible = false;
            //
            // lcgBottom
            //
            // 03 §8.4 — 하단 검사구성 65~70%. 내부는 NEX 55 : AEX 45.
            this.lcgBottom.GroupBordersVisible = false;
            this.lcgBottom.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lcgNex,
            this.splitBottom,
            this.lcgAex});
            this.lcgBottom.Location = new System.Drawing.Point(0, 186);
            this.lcgBottom.Name = "lcgBottom";
            this.lcgBottom.Size = new System.Drawing.Size(1100, 434);
            this.lcgBottom.TextVisible = false;
            //
            // lcgNex
            //
            this.lcgNex.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciNexList});
            this.lcgNex.Location = new System.Drawing.Point(0, 0);
            this.lcgNex.Name = "lcgNex";
            this.lcgNex.Size = new System.Drawing.Size(600, 434);
            this.lcgNex.Text = "국가검사 (NEX) · ReadOnly";
            //
            // lciNexList
            //
            this.lciNexList.Control = this.gcNexList;
            this.lciNexList.Location = new System.Drawing.Point(0, 0);
            this.lciNexList.Name = "lciNexList";
            this.lciNexList.Size = new System.Drawing.Size(600, 434);
            this.lciNexList.TextSize = new System.Drawing.Size(0, 0);
            this.lciNexList.TextVisible = false;
            //
            // splitBottom
            //
            this.splitBottom.AllowHotTrack = true;
            this.splitBottom.Location = new System.Drawing.Point(600, 0);
            this.splitBottom.Name = "splitBottom";
            this.splitBottom.Size = new System.Drawing.Size(10, 434);
            //
            // lcgAex
            //
            this.lcgAex.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciAexList});
            this.lcgAex.Location = new System.Drawing.Point(610, 0);
            this.lcgAex.Name = "lcgAex";
            this.lcgAex.Size = new System.Drawing.Size(490, 434);
            this.lcgAex.Text = "추가검사 (AEX)";
            //
            // lciAexList
            //
            this.lciAexList.Control = this.gcAexList;
            this.lciAexList.Location = new System.Drawing.Point(0, 0);
            this.lciAexList.Name = "lciAexList";
            this.lciAexList.Size = new System.Drawing.Size(490, 434);
            this.lciAexList.TextSize = new System.Drawing.Size(0, 0);
            this.lciAexList.TextVisible = false;
            //
            // lciBlock
            //
            this.lciBlock.Control = this.lblBlock;
            this.lciBlock.Location = new System.Drawing.Point(0, 620);
            this.lciBlock.MaxSize = new System.Drawing.Size(0, 24);
            this.lciBlock.MinSize = new System.Drawing.Size(104, 24);
            this.lciBlock.Name = "lciBlock";
            this.lciBlock.Size = new System.Drawing.Size(1100, 24);
            this.lciBlock.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciBlock.TextSize = new System.Drawing.Size(0, 0);
            this.lciBlock.TextVisible = false;
            //
            // lcgActions
            //
            this.lcgActions.GroupBordersVisible = false;
            this.lcgActions.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.emptySpaceActions,
            this.lciSave,
            this.lciClose});
            this.lcgActions.Location = new System.Drawing.Point(0, 644);
            this.lcgActions.Name = "lcgActions";
            this.lcgActions.Size = new System.Drawing.Size(1100, 56);
            this.lcgActions.TextVisible = false;
            //
            // emptySpaceActions
            //
            this.emptySpaceActions.AllowHotTrack = false;
            this.emptySpaceActions.Location = new System.Drawing.Point(0, 0);
            this.emptySpaceActions.Name = "emptySpaceActions";
            this.emptySpaceActions.Size = new System.Drawing.Size(916, 56);
            this.emptySpaceActions.TextSize = new System.Drawing.Size(0, 0);
            //
            // lciSave
            //
            this.lciSave.Control = this.btnSave;
            this.lciSave.Location = new System.Drawing.Point(916, 0);
            this.lciSave.MaxSize = new System.Drawing.Size(92, 34);
            this.lciSave.MinSize = new System.Drawing.Size(92, 34);
            this.lciSave.Name = "lciSave";
            this.lciSave.Size = new System.Drawing.Size(92, 56);
            this.lciSave.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciSave.TextVisible = false;
            //
            // lciClose
            //
            this.lciClose.Control = this.btnClose;
            this.lciClose.Location = new System.Drawing.Point(1008, 0);
            this.lciClose.MaxSize = new System.Drawing.Size(92, 34);
            this.lciClose.MinSize = new System.Drawing.Size(92, 34);
            this.lciClose.Name = "lciClose";
            this.lciClose.Size = new System.Drawing.Size(92, 56);
            this.lciClose.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciClose.TextVisible = false;
            //
            // FrmReservation
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // Esc 도 [닫기] 와 같은 길로 보낸다 — 03 §8.10 의 폐기 확인이 OnFormClosing 하나에
            // 걸려 있으므로 닫는 길이 늘어도 묻는 자리는 그대로다.
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(1100, 700);
            // Dock=Fill 인 lcMain 을 먼저 넣는다 (references/designer.md 함정 3).
            this.Controls.Add(this.lcMain);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(940, 640);
            this.Name = "FrmReservation";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "신규 예약";
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).EndInit();
            this.lcMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.txtPatientChartNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPatientName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPatientBirthGender.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deReserveDate.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deReserveDate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rgSlot.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcNexList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvNexList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcAexList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvAexList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repoChkAex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgTop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgPatient)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciPatientChartNo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciPatientName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciPatientBirthGender)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptyPatient)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitTop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgSchedule)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciReserveDate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSlot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciTarget)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgNex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciNexList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgAex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciAexList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciBlock)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSave)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciClose)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl lcMain;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraLayout.LayoutControlGroup lcgTop;
        private DevExpress.XtraLayout.LayoutControlGroup lcgPatient;
        private DevExpress.XtraEditors.TextEdit txtPatientChartNo;
        private DevExpress.XtraEditors.TextEdit txtPatientName;
        private DevExpress.XtraEditors.TextEdit txtPatientBirthGender;
        private DevExpress.XtraLayout.LayoutControlItem lciPatientChartNo;
        private DevExpress.XtraLayout.LayoutControlItem lciPatientName;
        private DevExpress.XtraLayout.LayoutControlItem lciPatientBirthGender;
        private DevExpress.XtraLayout.EmptySpaceItem emptyPatient;
        private DevExpress.XtraLayout.SplitterItem splitTop;
        private DevExpress.XtraLayout.LayoutControlGroup lcgSchedule;
        private DevExpress.XtraEditors.DateEdit deReserveDate;
        private DevExpress.XtraEditors.RadioGroup rgSlot;
        private DevExpress.XtraLayout.LayoutControlItem lciReserveDate;
        private DevExpress.XtraLayout.LayoutControlItem lciSlot;
        private DevExpress.XtraEditors.LabelControl lblTarget;
        private DevExpress.XtraLayout.LayoutControlItem lciTarget;
        private DevExpress.XtraLayout.LayoutControlGroup lcgBottom;
        private DevExpress.XtraLayout.LayoutControlGroup lcgNex;
        private DevExpress.XtraGrid.GridControl gcNexList;
        private DevExpress.XtraGrid.Views.Grid.GridView gvNexList;
        private DevExpress.XtraGrid.Columns.GridColumn colNexName;
        private DevExpress.XtraGrid.Columns.GridColumn colNexType;
        private DevExpress.XtraLayout.LayoutControlItem lciNexList;
        private DevExpress.XtraLayout.SplitterItem splitBottom;
        private DevExpress.XtraLayout.LayoutControlGroup lcgAex;
        private DevExpress.XtraGrid.GridControl gcAexList;
        private DevExpress.XtraGrid.Views.Grid.GridView gvAexList;
        private DevExpress.XtraGrid.Columns.GridColumn colAexChecked;
        private DevExpress.XtraGrid.Columns.GridColumn colAexName;
        private DevExpress.XtraGrid.Columns.GridColumn colAexReason;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit repoChkAex;
        private DevExpress.XtraLayout.LayoutControlItem lciAexList;
        private DevExpress.XtraEditors.LabelControl lblBlock;
        private DevExpress.XtraLayout.LayoutControlItem lciBlock;
        private DevExpress.XtraLayout.LayoutControlGroup lcgActions;
        private DevExpress.XtraEditors.SimpleButton btnSave;
        private DevExpress.XtraEditors.SimpleButton btnClose;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceActions;
        private DevExpress.XtraLayout.LayoutControlItem lciSave;
        private DevExpress.XtraLayout.LayoutControlItem lciClose;
    }
}
