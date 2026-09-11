// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
namespace HealthCheckupReservationReception.Views
{
    partial class UcHoliday
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

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.lcMain = new DevExpress.XtraLayout.LayoutControl();
            this.deFrom = new DevExpress.XtraEditors.DateEdit();
            this.deTo = new DevExpress.XtraEditors.DateEdit();
            this.cboType = new DevExpress.XtraEditors.ImageComboBoxEdit();
            this.btnSearch = new DevExpress.XtraEditors.SimpleButton();
            this.lblRegistry = new DevExpress.XtraEditors.LabelControl();
            this.gcHolidayList = new DevExpress.XtraGrid.GridControl();
            this.gvHolidayList = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colHolidayDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colHolidayName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colHolidayType = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colIsActive = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMemo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.deInputDate = new DevExpress.XtraEditors.DateEdit();
            this.txtInputName = new DevExpress.XtraEditors.TextEdit();
            this.chkInputActive = new DevExpress.XtraEditors.CheckEdit();
            this.txtInputMemo = new DevExpress.XtraEditors.TextEdit();
            this.lblBlock = new DevExpress.XtraEditors.LabelControl();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgSearch = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciFrom = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciTo = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciType = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySearch = new DevExpress.XtraLayout.EmptySpaceItem();
            this.lciSearch = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciRegistry = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgList = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciHolidayList = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgInput = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciInputDate = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciInputName = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciInputActive = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciInputMemo = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciBlock = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).BeginInit();
            this.lcMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.deFrom.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deFrom.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deTo.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deTo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboType.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcHolidayList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvHolidayList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deInputDate.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deInputDate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtInputName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkInputActive.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtInputMemo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciType)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciRegistry)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciHolidayList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgInput)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciInputDate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciInputName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciInputActive)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciInputMemo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciBlock)).BeginInit();
            this.SuspendLayout();
            //
            // lcMain
            //
            this.lcMain.Controls.Add(this.deFrom);
            this.lcMain.Controls.Add(this.deTo);
            this.lcMain.Controls.Add(this.cboType);
            this.lcMain.Controls.Add(this.btnSearch);
            this.lcMain.Controls.Add(this.lblRegistry);
            this.lcMain.Controls.Add(this.gcHolidayList);
            this.lcMain.Controls.Add(this.deInputDate);
            this.lcMain.Controls.Add(this.txtInputName);
            this.lcMain.Controls.Add(this.chkInputActive);
            this.lcMain.Controls.Add(this.txtInputMemo);
            this.lcMain.Controls.Add(this.lblBlock);
            this.lcMain.AllowCustomization = false;
            this.lcMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lcMain.Location = new System.Drawing.Point(0, 0);
            this.lcMain.Name = "lcMain";
            this.lcMain.Root = this.Root;
            this.lcMain.Size = new System.Drawing.Size(1916, 887);
            this.lcMain.TabIndex = 0;
            //
            // deFrom
            //
            this.deFrom.EditValue = null;
            this.deFrom.Location = new System.Drawing.Point(89, 36);
            this.deFrom.Name = "deFrom";
            this.deFrom.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deFrom.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deFrom.Size = new System.Drawing.Size(120, 20);
            this.deFrom.StyleController = this.lcMain;
            this.deFrom.TabIndex = 0;
            //
            // deTo
            //
            this.deTo.EditValue = null;
            this.deTo.Location = new System.Drawing.Point(238, 36);
            this.deTo.Name = "deTo";
            this.deTo.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deTo.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deTo.Size = new System.Drawing.Size(120, 20);
            this.deTo.StyleController = this.lcMain;
            this.deTo.TabIndex = 1;
            //
            // cboType
            //
            // 항목은 ConfigureUI 가 만든다 — 값이 DbHolidayType 이라 캡션과 값이 한 줄에 같이
            // 있어야 어긋나지 않는다 (킷 §5).
            this.cboType.Location = new System.Drawing.Point(410, 36);
            this.cboType.Name = "cboType";
            this.cboType.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cboType.Size = new System.Drawing.Size(120, 20);
            this.cboType.StyleController = this.lcMain;
            this.cboType.TabIndex = 2;
            //
            // btnSearch
            //
            this.btnSearch.Location = new System.Drawing.Point(546, 34);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(84, 24);
            this.btnSearch.StyleController = this.lcMain;
            this.btnSearch.TabIndex = 3;
            this.btnSearch.Text = "조회";
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            //
            // lblRegistry
            //
            // 03 §24.6 만료 경고. 임계 숫자는 화면에 없다 — DB 가 둘을 주고 Presenter 가 비교한다.
            this.lblRegistry.Location = new System.Drawing.Point(12, 66);
            this.lblRegistry.Name = "lblRegistry";
            this.lblRegistry.Size = new System.Drawing.Size(0, 14);
            this.lblRegistry.StyleController = this.lcMain;
            this.lblRegistry.TabIndex = 4;
            //
            // gcHolidayList
            //
            this.gcHolidayList.Location = new System.Drawing.Point(24, 116);
            this.gcHolidayList.MainView = this.gvHolidayList;
            this.gcHolidayList.Name = "gcHolidayList";
            this.gcHolidayList.Size = new System.Drawing.Size(1868, 631);
            this.gcHolidayList.TabIndex = 5;
            this.gcHolidayList.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvHolidayList});
            //
            // gvHolidayList
            //
            this.gvHolidayList.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colHolidayDate,
            this.colHolidayName,
            this.colHolidayType,
            this.colIsActive,
            this.colMemo});
            this.gvHolidayList.GridControl = this.gcHolidayList;
            this.gvHolidayList.Name = "gvHolidayList";
            this.gvHolidayList.OptionsBehavior.AutoPopulateColumns = false;
            this.gvHolidayList.OptionsBehavior.Editable = false;
            this.gvHolidayList.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.gvHolidayList.OptionsView.ShowGroupPanel = false;
            this.gvHolidayList.OptionsView.ShowIndicator = false;
            this.gvHolidayList.FocusedRowChanged += new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(this.gvHolidayList_FocusedRowChanged);
            this.gvHolidayList.RowClick += new DevExpress.XtraGrid.Views.Grid.RowClickEventHandler(this.gvHolidayList_RowClick);
            this.gvHolidayList.RowStyle += new DevExpress.XtraGrid.Views.Grid.RowStyleEventHandler(this.gvHolidayList_RowStyle);
            //
            // colHolidayDate
            //
            this.colHolidayDate.Caption = "휴무일자";
            this.colHolidayDate.FieldName = "HolidayDate";
            this.colHolidayDate.Name = "colHolidayDate";
            this.colHolidayDate.MinWidth = 100;
            this.colHolidayDate.Visible = true;
            this.colHolidayDate.VisibleIndex = 0;
            this.colHolidayDate.Width = 140;
            //
            // colHolidayName
            //
            this.colHolidayName.Caption = "휴무일명";
            this.colHolidayName.FieldName = "HolidayName";
            this.colHolidayName.Name = "colHolidayName";
            this.colHolidayName.MinWidth = 140;
            this.colHolidayName.Visible = true;
            this.colHolidayName.VisibleIndex = 1;
            this.colHolidayName.Width = 260;
            //
            // colHolidayType
            //
            this.colHolidayType.Caption = "휴무구분";
            this.colHolidayType.FieldName = "HolidayType";
            this.colHolidayType.Name = "colHolidayType";
            this.colHolidayType.MinWidth = 100;
            this.colHolidayType.Visible = true;
            this.colHolidayType.VisibleIndex = 2;
            this.colHolidayType.Width = 140;
            //
            // colIsActive
            //
            this.colIsActive.Caption = "사용여부";
            this.colIsActive.FieldName = "IsActive";
            this.colIsActive.Name = "colIsActive";
            this.colIsActive.MinWidth = 80;
            this.colIsActive.Visible = true;
            this.colIsActive.VisibleIndex = 3;
            this.colIsActive.Width = 100;
            //
            // colMemo
            //
            this.colMemo.Caption = "비고";
            this.colMemo.FieldName = "Memo";
            this.colMemo.Name = "colMemo";
            this.colMemo.MinWidth = 200;
            this.colMemo.Visible = true;
            this.colMemo.VisibleIndex = 4;
            //
            // deInputDate
            //
            this.deInputDate.EditValue = null;
            this.deInputDate.Location = new System.Drawing.Point(89, 787);
            this.deInputDate.Name = "deInputDate";
            this.deInputDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deInputDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deInputDate.Size = new System.Drawing.Size(120, 20);
            this.deInputDate.StyleController = this.lcMain;
            this.deInputDate.TabIndex = 6;
            //
            // txtInputName
            //
            this.txtInputName.Location = new System.Drawing.Point(298, 787);
            this.txtInputName.Name = "txtInputName";
            this.txtInputName.Properties.MaxLength = 100;
            this.txtInputName.Size = new System.Drawing.Size(260, 20);
            this.txtInputName.StyleController = this.lcMain;
            this.txtInputName.TabIndex = 7;
            //
            // chkInputActive
            //
            // 05 §12.6 `@사용여부` 는 NOT NULL 이다. 03 §24.5 — 0 은 삭제가 아니라 일시 무효화다.
            this.chkInputActive.Location = new System.Drawing.Point(647, 787);
            this.chkInputActive.Name = "chkInputActive";
            this.chkInputActive.Properties.Caption = "사용";
            this.chkInputActive.Size = new System.Drawing.Size(80, 19);
            this.chkInputActive.StyleController = this.lcMain;
            this.chkInputActive.TabIndex = 8;
            //
            // txtInputMemo
            //
            this.txtInputMemo.Location = new System.Drawing.Point(816, 787);
            this.txtInputMemo.Name = "txtInputMemo";
            this.txtInputMemo.Properties.MaxLength = 500;
            this.txtInputMemo.Size = new System.Drawing.Size(1076, 20);
            this.txtInputMemo.StyleController = this.lcMain;
            this.txtInputMemo.TabIndex = 9;
            //
            // lblBlock
            //
            // 실패는 모달이 아니라 Inline 이다 (문서 §2.1 — 창을 열자마자 뜨는 것을 막느라
            // 조용히 삼키면 실패가 아예 보이지 않는다).
            this.lblBlock.Location = new System.Drawing.Point(12, 851);
            this.lblBlock.Name = "lblBlock";
            this.lblBlock.Size = new System.Drawing.Size(0, 14);
            this.lblBlock.StyleController = this.lcMain;
            this.lblBlock.TabIndex = 10;
            //
            // Root
            //
            this.Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lcgSearch,
            this.lciRegistry,
            this.lcgList,
            this.lcgInput,
            this.lciBlock});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(1916, 887);
            this.Root.TextVisible = false;
            //
            // lcgSearch
            //
            this.lcgSearch.GroupBordersVisible = false;
            this.lcgSearch.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciFrom,
            this.lciTo,
            this.lciType,
            this.lciSearch,
            this.emptySearch});
            this.lcgSearch.Location = new System.Drawing.Point(0, 0);
            this.lcgSearch.Name = "lcgSearch";
            this.lcgSearch.Size = new System.Drawing.Size(1916, 50);
            this.lcgSearch.TextVisible = false;
            //
            // lciFrom
            //
            // [X] 라벨 길이가 제각각인 줄이다. 항목마다 AutoSize 를 주지 않으면 LayoutControl 이
            //     가장 긴 라벨에 맞춰 폭을 통일하고 그만큼 입력칸이 깎인다 (문서 §2.1).
            this.lciFrom.Control = this.deFrom;
            this.lciFrom.Location = new System.Drawing.Point(0, 0);
            this.lciFrom.MaxSize = new System.Drawing.Size(209, 26);
            this.lciFrom.MinSize = new System.Drawing.Size(209, 26);
            this.lciFrom.Name = "lciFrom";
            this.lciFrom.Size = new System.Drawing.Size(209, 50);
            this.lciFrom.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciFrom.Text = "조회기간";
            this.lciFrom.TextSize = new System.Drawing.Size(60, 14);
            this.lciFrom.OptionsTableLayoutItem.ColumnIndex = 0;
            this.lciFrom.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            //
            // lciTo
            //
            this.lciTo.Control = this.deTo;
            this.lciTo.Location = new System.Drawing.Point(209, 0);
            this.lciTo.MaxSize = new System.Drawing.Size(149, 26);
            this.lciTo.MinSize = new System.Drawing.Size(149, 26);
            this.lciTo.Name = "lciTo";
            this.lciTo.Size = new System.Drawing.Size(149, 50);
            this.lciTo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciTo.Text = "~";
            this.lciTo.TextSize = new System.Drawing.Size(12, 14);
            this.lciTo.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            //
            // lciType
            //
            this.lciType.Control = this.cboType;
            this.lciType.Location = new System.Drawing.Point(358, 0);
            this.lciType.MaxSize = new System.Drawing.Size(176, 26);
            this.lciType.MinSize = new System.Drawing.Size(176, 26);
            this.lciType.Name = "lciType";
            this.lciType.Size = new System.Drawing.Size(176, 50);
            this.lciType.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciType.Text = "구분";
            this.lciType.TextSize = new System.Drawing.Size(36, 14);
            this.lciType.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            //
            // lciSearch
            //
            this.lciSearch.Control = this.btnSearch;
            this.lciSearch.Location = new System.Drawing.Point(534, 0);
            this.lciSearch.MaxSize = new System.Drawing.Size(92, 32);
            this.lciSearch.MinSize = new System.Drawing.Size(92, 32);
            this.lciSearch.Name = "lciSearch";
            this.lciSearch.Size = new System.Drawing.Size(92, 50);
            this.lciSearch.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciSearch.TextVisible = false;
            //
            // emptySearch
            //
            this.emptySearch.AllowHotTrack = false;
            this.emptySearch.Location = new System.Drawing.Point(626, 0);
            this.emptySearch.Name = "emptySearch";
            this.emptySearch.Size = new System.Drawing.Size(1290, 50);
            this.emptySearch.TextSize = new System.Drawing.Size(0, 0);
            //
            // lciRegistry
            //
            this.lciRegistry.Control = this.lblRegistry;
            this.lciRegistry.Location = new System.Drawing.Point(0, 50);
            this.lciRegistry.MaxSize = new System.Drawing.Size(0, 24);
            this.lciRegistry.MinSize = new System.Drawing.Size(104, 24);
            this.lciRegistry.Name = "lciRegistry";
            this.lciRegistry.Size = new System.Drawing.Size(1916, 24);
            this.lciRegistry.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciRegistry.TextSize = new System.Drawing.Size(0, 0);
            this.lciRegistry.TextVisible = false;
            //
            // lcgList
            //
            this.lcgList.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciHolidayList});
            this.lcgList.Location = new System.Drawing.Point(0, 74);
            this.lcgList.Name = "lcgList";
            this.lcgList.Size = new System.Drawing.Size(1916, 681);
            this.lcgList.Text = "휴무일 목록";
            //
            // lciHolidayList
            //
            this.lciHolidayList.Control = this.gcHolidayList;
            this.lciHolidayList.Location = new System.Drawing.Point(0, 0);
            this.lciHolidayList.Name = "lciHolidayList";
            this.lciHolidayList.Size = new System.Drawing.Size(1916, 681);
            this.lciHolidayList.TextSize = new System.Drawing.Size(0, 0);
            this.lciHolidayList.TextVisible = false;
            //
            // lcgInput
            //
            // 03 §24.5 — 자체휴무일만 편집한다. [추가]·[수정]·[삭제] 는 Ribbon 에 있다.
            this.lcgInput.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciInputDate,
            this.lciInputName,
            this.lciInputActive,
            this.lciInputMemo});
            this.lcgInput.Location = new System.Drawing.Point(0, 755);
            this.lcgInput.Name = "lcgInput";
            this.lcgInput.Size = new System.Drawing.Size(1916, 74);
            this.lcgInput.Text = "자체휴무일 입력";
            //
            // lciInputDate
            //
            this.lciInputDate.Control = this.deInputDate;
            this.lciInputDate.Location = new System.Drawing.Point(0, 0);
            this.lciInputDate.MaxSize = new System.Drawing.Size(209, 26);
            this.lciInputDate.MinSize = new System.Drawing.Size(209, 26);
            this.lciInputDate.Name = "lciInputDate";
            this.lciInputDate.Size = new System.Drawing.Size(209, 26);
            this.lciInputDate.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciInputDate.Text = "휴무일자";
            this.lciInputDate.TextSize = new System.Drawing.Size(60, 14);
            this.lciInputDate.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            //
            // lciInputName
            //
            this.lciInputName.Control = this.txtInputName;
            this.lciInputName.Location = new System.Drawing.Point(209, 0);
            this.lciInputName.MaxSize = new System.Drawing.Size(349, 26);
            this.lciInputName.MinSize = new System.Drawing.Size(349, 26);
            this.lciInputName.Name = "lciInputName";
            this.lciInputName.Size = new System.Drawing.Size(349, 26);
            this.lciInputName.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciInputName.Text = "휴무일명";
            this.lciInputName.TextSize = new System.Drawing.Size(60, 14);
            this.lciInputName.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            //
            // lciInputActive
            //
            this.lciInputActive.Control = this.chkInputActive;
            this.lciInputActive.Location = new System.Drawing.Point(558, 0);
            this.lciInputActive.MaxSize = new System.Drawing.Size(148, 26);
            this.lciInputActive.MinSize = new System.Drawing.Size(148, 26);
            this.lciInputActive.Name = "lciInputActive";
            this.lciInputActive.Size = new System.Drawing.Size(148, 26);
            this.lciInputActive.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciInputActive.TextSize = new System.Drawing.Size(0, 0);
            this.lciInputActive.TextVisible = false;
            //
            // lciInputMemo
            //
            this.lciInputMemo.Control = this.txtInputMemo;
            this.lciInputMemo.Location = new System.Drawing.Point(706, 0);
            this.lciInputMemo.MaxSize = new System.Drawing.Size(0, 26);
            this.lciInputMemo.MinSize = new System.Drawing.Size(200, 26);
            this.lciInputMemo.Name = "lciInputMemo";
            this.lciInputMemo.Size = new System.Drawing.Size(1210, 26);
            this.lciInputMemo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciInputMemo.Text = "비고";
            this.lciInputMemo.TextSize = new System.Drawing.Size(36, 14);
            this.lciInputMemo.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            //
            // lciBlock
            //
            this.lciBlock.Control = this.lblBlock;
            this.lciBlock.Location = new System.Drawing.Point(0, 829);
            this.lciBlock.MaxSize = new System.Drawing.Size(0, 24);
            this.lciBlock.MinSize = new System.Drawing.Size(104, 24);
            this.lciBlock.Name = "lciBlock";
            this.lciBlock.Size = new System.Drawing.Size(1916, 24);
            this.lciBlock.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciBlock.TextSize = new System.Drawing.Size(0, 0);
            this.lciBlock.TextVisible = false;
            //
            // UcHoliday
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lcMain);
            this.Name = "UcHoliday";
            this.Size = new System.Drawing.Size(1916, 887);
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).EndInit();
            this.lcMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.deFrom.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deFrom.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deTo.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deTo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboType.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcHolidayList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvHolidayList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deInputDate.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deInputDate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtInputName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkInputActive.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtInputMemo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciType)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciRegistry)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciHolidayList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgInput)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciInputDate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciInputName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciInputActive)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciInputMemo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciBlock)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl lcMain;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraLayout.LayoutControlGroup lcgSearch;
        private DevExpress.XtraEditors.DateEdit deFrom;
        private DevExpress.XtraEditors.DateEdit deTo;
        private DevExpress.XtraEditors.ImageComboBoxEdit cboType;
        private DevExpress.XtraEditors.SimpleButton btnSearch;
        private DevExpress.XtraLayout.LayoutControlItem lciFrom;
        private DevExpress.XtraLayout.LayoutControlItem lciTo;
        private DevExpress.XtraLayout.LayoutControlItem lciType;
        private DevExpress.XtraLayout.LayoutControlItem lciSearch;
        private DevExpress.XtraLayout.EmptySpaceItem emptySearch;
        private DevExpress.XtraEditors.LabelControl lblRegistry;
        private DevExpress.XtraLayout.LayoutControlItem lciRegistry;
        private DevExpress.XtraLayout.LayoutControlGroup lcgList;
        private DevExpress.XtraGrid.GridControl gcHolidayList;
        private DevExpress.XtraGrid.Views.Grid.GridView gvHolidayList;
        private DevExpress.XtraGrid.Columns.GridColumn colHolidayDate;
        private DevExpress.XtraGrid.Columns.GridColumn colHolidayName;
        private DevExpress.XtraGrid.Columns.GridColumn colHolidayType;
        private DevExpress.XtraGrid.Columns.GridColumn colIsActive;
        private DevExpress.XtraGrid.Columns.GridColumn colMemo;
        private DevExpress.XtraLayout.LayoutControlItem lciHolidayList;
        private DevExpress.XtraLayout.LayoutControlGroup lcgInput;
        private DevExpress.XtraEditors.DateEdit deInputDate;
        private DevExpress.XtraEditors.TextEdit txtInputName;
        private DevExpress.XtraEditors.CheckEdit chkInputActive;
        private DevExpress.XtraEditors.TextEdit txtInputMemo;
        private DevExpress.XtraLayout.LayoutControlItem lciInputDate;
        private DevExpress.XtraLayout.LayoutControlItem lciInputName;
        private DevExpress.XtraLayout.LayoutControlItem lciInputActive;
        private DevExpress.XtraLayout.LayoutControlItem lciInputMemo;
        private DevExpress.XtraEditors.LabelControl lblBlock;
        private DevExpress.XtraLayout.LayoutControlItem lciBlock;
    }
}
