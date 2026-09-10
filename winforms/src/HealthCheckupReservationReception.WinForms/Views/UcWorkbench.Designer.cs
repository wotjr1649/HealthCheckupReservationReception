// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
namespace HealthCheckupReservationReception.Views
{
    partial class UcWorkbench
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
            this.cboStatus = new DevExpress.XtraEditors.ImageComboBoxEdit();
            this.txtChartNo = new DevExpress.XtraEditors.TextEdit();
            this.txtName = new DevExpress.XtraEditors.TextEdit();
            this.cboColumns = new DevExpress.XtraEditors.PopupContainerEdit();
            this.btnSearch = new DevExpress.XtraEditors.SimpleButton();
            this.lblValidation = new DevExpress.XtraEditors.LabelControl();
            this.gcWorkList = new DevExpress.XtraGrid.GridControl();
            this.gvWorkList = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colReserveDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colSlot = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colStatusName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colChartNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colGender = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colBirthday = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMobilePhone = new DevExpress.XtraGrid.Columns.GridColumn();
            this.txtDetailChartNo = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailName = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailBirthGender = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailReserveDate = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailSlot = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailCapacity = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailStatus = new DevExpress.XtraEditors.TextEdit();
            this.gcNexList = new DevExpress.XtraGrid.GridControl();
            this.gvNexList = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colNexName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colNexType = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gcAexList = new DevExpress.XtraGrid.GridControl();
            this.gvAexList = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colAexName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.pccColumns = new DevExpress.XtraEditors.PopupContainerControl();
            this.clbColumns = new DevExpress.XtraEditors.CheckedListBoxControl();
            this.btnColumnsDefault = new DevExpress.XtraEditors.SimpleButton();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgList = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgSearch = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciDateFrom = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciDateTo = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciStatus = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciChartNo = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciName = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceSearch = new DevExpress.XtraLayout.EmptySpaceItem();
            this.lciColumns = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciSearch = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciValidation = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciWorkList = new DevExpress.XtraLayout.LayoutControlItem();
            this.splitWork = new DevExpress.XtraLayout.SplitterItem();
            this.lcgDetail = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgDetailPatient = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciDetailChartNo = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciDetailName = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciDetailBirthGender = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgDetailWork = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciDetailReserveDate = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciDetailSlot = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciDetailCapacity = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciDetailStatus = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgDetailNex = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciNexList = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgDetailAex = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciAexList = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).BeginInit();
            this.lcMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.deFrom.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deFrom.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deTo.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deTo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboStatus.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboColumns.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcWorkList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvWorkList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailChartNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailBirthGender.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailReserveDate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailSlot.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailCapacity.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailStatus.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcNexList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvNexList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcAexList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvAexList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pccColumns)).BeginInit();
            this.pccColumns.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.clbColumns)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDateFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDateTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciStatus)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciChartNo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciColumns)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciValidation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciWorkList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitWork)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetail)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailPatient)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailChartNo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailBirthGender)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailWork)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailReserveDate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailSlot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailCapacity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailStatus)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailNex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciNexList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailAex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciAexList)).BeginInit();
            this.SuspendLayout();
            //
            // lcMain
            //
            this.lcMain.Controls.Add(this.deFrom);
            this.lcMain.Controls.Add(this.deTo);
            this.lcMain.Controls.Add(this.cboStatus);
            this.lcMain.Controls.Add(this.txtChartNo);
            this.lcMain.Controls.Add(this.txtName);
            this.lcMain.Controls.Add(this.cboColumns);
            this.lcMain.Controls.Add(this.btnSearch);
            this.lcMain.Controls.Add(this.lblValidation);
            this.lcMain.Controls.Add(this.gcWorkList);
            this.lcMain.Controls.Add(this.txtDetailChartNo);
            this.lcMain.Controls.Add(this.txtDetailName);
            this.lcMain.Controls.Add(this.txtDetailBirthGender);
            this.lcMain.Controls.Add(this.txtDetailReserveDate);
            this.lcMain.Controls.Add(this.txtDetailSlot);
            this.lcMain.Controls.Add(this.txtDetailCapacity);
            this.lcMain.Controls.Add(this.txtDetailStatus);
            this.lcMain.Controls.Add(this.gcNexList);
            this.lcMain.Controls.Add(this.gcAexList);
            // 항목을 숨기는 길이 하나여야 사용자가 되돌릴 줄 안다 (03 §18) —
            // LayoutControl 의 기본 우클릭 메뉴는 항목을 끌어내 숨길 수 있으므로 닫는다.
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
            this.deFrom.Location = new System.Drawing.Point(84, 31);
            this.deFrom.Name = "deFrom";
            this.deFrom.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            this.deFrom.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deFrom.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deFrom.Size = new System.Drawing.Size(114, 20);
            this.deFrom.StyleController = this.lcMain;
            this.deFrom.TabIndex = 0;
            this.deFrom.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchInput_KeyDown);
            //
            // deTo
            //
            this.deTo.EditValue = null;
            this.deTo.Location = new System.Drawing.Point(222, 31);
            this.deTo.Name = "deTo";
            this.deTo.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            this.deTo.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deTo.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deTo.Size = new System.Drawing.Size(114, 20);
            this.deTo.StyleController = this.lcMain;
            this.deTo.TabIndex = 1;
            this.deTo.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchInput_KeyDown);
            //
            // cboStatus
            //
            // 항목은 ConfigureUI 가 만든다 — ImageComboBoxItem 은 인자 둘짜리 생성자로만
            // 직렬화되는 자리다 (킷 §5).
            this.cboStatus.Location = new System.Drawing.Point(388, 31);
            this.cboStatus.Name = "cboStatus";
            this.cboStatus.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cboStatus.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.cboStatus.Size = new System.Drawing.Size(112, 20);
            this.cboStatus.StyleController = this.lcMain;
            this.cboStatus.TabIndex = 2;
            //
            // txtChartNo
            //
            this.txtChartNo.Location = new System.Drawing.Point(572, 31);
            this.txtChartNo.Name = "txtChartNo";
            this.txtChartNo.Properties.MaxLength = 100;
            this.txtChartNo.Size = new System.Drawing.Size(108, 20);
            this.txtChartNo.StyleController = this.lcMain;
            this.txtChartNo.TabIndex = 3;
            this.txtChartNo.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchInput_KeyDown);
            //
            // txtName
            //
            this.txtName.Location = new System.Drawing.Point(740, 31);
            this.txtName.Name = "txtName";
            this.txtName.Properties.MaxLength = 100;
            this.txtName.Size = new System.Drawing.Size(110, 20);
            this.txtName.StyleController = this.lcMain;
            this.txtName.TabIndex = 4;
            this.txtName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchInput_KeyDown);
            //
            // cboColumns
            //
            // 03 §18 컬럼설정. Ribbon [보기] 가 아니라 Grid 옆이다 — 목록을 보면서 켜고 끄는
            // 편이 낫다 (2026-09-10 사용자 결정, WF-PAT-01 과 같은 자리).
            this.cboColumns.Location = new System.Drawing.Point(981, 31);
            this.cboColumns.Name = "cboColumns";
            this.cboColumns.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cboColumns.Properties.PopupControl = this.pccColumns;
            // 글자를 고르지도 캐럿을 두지도 않는다 — 값이 아니라 이름을 적는 자리다.
            this.cboColumns.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.cboColumns.Size = new System.Drawing.Size(110, 20);
            this.cboColumns.StyleController = this.lcMain;
            this.cboColumns.TabIndex = 5;
            this.cboColumns.QueryDisplayText += new DevExpress.XtraEditors.Controls.QueryDisplayTextEventHandler(this.cboColumns_QueryDisplayText);
            //
            // btnSearch
            //
            this.btnSearch.Location = new System.Drawing.Point(1096, 29);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(88, 22);
            this.btnSearch.StyleController = this.lcMain;
            this.btnSearch.TabIndex = 6;
            this.btnSearch.Text = "조회";
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            //
            // lblValidation
            //
            // 03 §9.3 — From>To 와 조건 없음은 Inline 오류다. 붉은 글씨는 ConfigureUI 가 준다.
            this.lblValidation.Location = new System.Drawing.Point(12, 57);
            this.lblValidation.Name = "lblValidation";
            this.lblValidation.Size = new System.Drawing.Size(0, 14);
            this.lblValidation.StyleController = this.lcMain;
            this.lblValidation.TabIndex = 7;
            //
            // gcWorkList
            //
            this.gcWorkList.Location = new System.Drawing.Point(12, 79);
            this.gcWorkList.MainView = this.gvWorkList;
            this.gcWorkList.Name = "gcWorkList";
            this.gcWorkList.Size = new System.Drawing.Size(1176, 796);
            this.gcWorkList.TabIndex = 8;
            this.gcWorkList.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvWorkList});
            //
            // gvWorkList
            //
            this.gvWorkList.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colReserveDate,
            this.colSlot,
            this.colStatusName,
            this.colName,
            this.colChartNo,
            this.colGender,
            this.colBirthday,
            this.colMobilePhone});
            this.gvWorkList.GridControl = this.gcWorkList;
            this.gvWorkList.Name = "gvWorkList";
            this.gvWorkList.OptionsBehavior.AutoPopulateColumns = false;
            this.gvWorkList.OptionsBehavior.Editable = false;
            // 03 §9.4 — Single Row Selection.
            this.gvWorkList.OptionsSelection.MultiSelect = false;
            this.gvWorkList.OptionsView.ShowGroupPanel = false;
            // 가로 스크롤은 컬럼 폭이 아니라 MinWidth 합이 세운다 — WF-PAT-01 과 같은 이유다
            // (ColumnAutoWidth 기본값 true 가 컬럼을 뷰 폭에 욱여넣는다. 실측 2026-09-10).
            this.gvWorkList.FocusedRowChanged += new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(this.gvWorkList_FocusedRowChanged);
            this.gvWorkList.RowClick += new DevExpress.XtraGrid.Views.Grid.RowClickEventHandler(this.gvWorkList_RowClick);
            this.gvWorkList.CustomColumnDisplayText += new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(this.gvWorkList_CustomColumnDisplayText);
            //
            // colReserveDate
            //
            this.colReserveDate.Caption = "예약/접수일";
            this.colReserveDate.FieldName = "ReserveDate";
            this.colReserveDate.Name = "colReserveDate";
            this.colReserveDate.MinWidth = 110;
            this.colReserveDate.Visible = true;
            this.colReserveDate.VisibleIndex = 0;
            this.colReserveDate.Width = 130;
            //
            // colSlot
            //
            this.colSlot.Caption = "시간대";
            this.colSlot.FieldName = "SlotCode";
            this.colSlot.Name = "colSlot";
            this.colSlot.MinWidth = 70;
            this.colSlot.Visible = true;
            this.colSlot.VisibleIndex = 1;
            this.colSlot.Width = 80;
            //
            // colStatusName
            //
            // DB 가 준 상태명을 그대로 쓴다 (05 §8.1 RS1) — 코드를 화면에서 이름으로 바꾸지 않는다.
            this.colStatusName.Caption = "상태";
            this.colStatusName.FieldName = "StatusName";
            this.colStatusName.Name = "colStatusName";
            this.colStatusName.MinWidth = 80;
            this.colStatusName.Visible = true;
            this.colStatusName.VisibleIndex = 2;
            this.colStatusName.Width = 90;
            //
            // colName
            //
            this.colName.Caption = "이름";
            this.colName.FieldName = "Name";
            this.colName.Name = "colName";
            this.colName.MinWidth = 80;
            this.colName.Visible = true;
            this.colName.VisibleIndex = 3;
            this.colName.Width = 100;
            //
            // colChartNo
            //
            this.colChartNo.Caption = "차트번호";
            this.colChartNo.FieldName = "ChartNo";
            this.colChartNo.Name = "colChartNo";
            this.colChartNo.MinWidth = 90;
            this.colChartNo.Visible = true;
            this.colChartNo.VisibleIndex = 4;
            this.colChartNo.Width = 120;
            //
            // colGender
            //
            // 03 §9.4 선택 컬럼 셋. 기본은 숨김이고 [컬럼 설정] 이 켠다.
            this.colGender.Caption = "성별";
            this.colGender.FieldName = "Gender";
            this.colGender.Name = "colGender";
            this.colGender.MinWidth = 50;
            //
            // colBirthday
            //
            this.colBirthday.Caption = "생년월일";
            this.colBirthday.FieldName = "Birthday";
            this.colBirthday.Name = "colBirthday";
            this.colBirthday.MinWidth = 90;
            //
            // colMobilePhone
            //
            this.colMobilePhone.Caption = "휴대전화번호";
            this.colMobilePhone.FieldName = "MobilePhone";
            this.colMobilePhone.Name = "colMobilePhone";
            this.colMobilePhone.MinWidth = 120;
            //
            // txtDetailChartNo
            //
            this.txtDetailChartNo.Location = new System.Drawing.Point(1310, 55);
            this.txtDetailChartNo.Name = "txtDetailChartNo";
            this.txtDetailChartNo.Properties.ReadOnly = true;
            this.txtDetailChartNo.Size = new System.Drawing.Size(582, 20);
            this.txtDetailChartNo.StyleController = this.lcMain;
            this.txtDetailChartNo.TabIndex = 9;
            //
            // txtDetailName
            //
            this.txtDetailName.Location = new System.Drawing.Point(1310, 79);
            this.txtDetailName.Name = "txtDetailName";
            this.txtDetailName.Properties.ReadOnly = true;
            this.txtDetailName.Size = new System.Drawing.Size(582, 20);
            this.txtDetailName.StyleController = this.lcMain;
            this.txtDetailName.TabIndex = 10;
            //
            // txtDetailBirthGender
            //
            this.txtDetailBirthGender.Location = new System.Drawing.Point(1310, 103);
            this.txtDetailBirthGender.Name = "txtDetailBirthGender";
            this.txtDetailBirthGender.Properties.ReadOnly = true;
            this.txtDetailBirthGender.Size = new System.Drawing.Size(582, 20);
            this.txtDetailBirthGender.StyleController = this.lcMain;
            this.txtDetailBirthGender.TabIndex = 11;
            //
            // txtDetailReserveDate
            //
            this.txtDetailReserveDate.Location = new System.Drawing.Point(1310, 151);
            this.txtDetailReserveDate.Name = "txtDetailReserveDate";
            this.txtDetailReserveDate.Properties.ReadOnly = true;
            this.txtDetailReserveDate.Size = new System.Drawing.Size(582, 20);
            this.txtDetailReserveDate.StyleController = this.lcMain;
            this.txtDetailReserveDate.TabIndex = 12;
            //
            // txtDetailSlot
            //
            this.txtDetailSlot.Location = new System.Drawing.Point(1310, 175);
            this.txtDetailSlot.Name = "txtDetailSlot";
            this.txtDetailSlot.Properties.ReadOnly = true;
            this.txtDetailSlot.Size = new System.Drawing.Size(582, 20);
            this.txtDetailSlot.StyleController = this.lcMain;
            this.txtDetailSlot.TabIndex = 13;
            //
            // txtDetailCapacity
            //
            this.txtDetailCapacity.Location = new System.Drawing.Point(1310, 199);
            this.txtDetailCapacity.Name = "txtDetailCapacity";
            this.txtDetailCapacity.Properties.ReadOnly = true;
            this.txtDetailCapacity.Size = new System.Drawing.Size(582, 20);
            this.txtDetailCapacity.StyleController = this.lcMain;
            this.txtDetailCapacity.TabIndex = 14;
            //
            // txtDetailStatus
            //
            this.txtDetailStatus.Location = new System.Drawing.Point(1310, 223);
            this.txtDetailStatus.Name = "txtDetailStatus";
            this.txtDetailStatus.Properties.ReadOnly = true;
            this.txtDetailStatus.Size = new System.Drawing.Size(582, 20);
            this.txtDetailStatus.StyleController = this.lcMain;
            this.txtDetailStatus.TabIndex = 15;
            //
            // gcNexList
            //
            this.gcNexList.Location = new System.Drawing.Point(1214, 271);
            this.gcNexList.MainView = this.gvNexList;
            this.gcNexList.Name = "gcNexList";
            this.gcNexList.Size = new System.Drawing.Size(678, 302);
            this.gcNexList.TabIndex = 16;
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
            this.gvNexList.OptionsBehavior.Editable = false;
            // 03 §9.5 — 실제 저장 구성 ReadOnly. 고르는 자리가 아니라 보는 자리다.
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
            // 03 §8.8 — 구분은 기본/조건부로 표시 가능하다. DB 가 준 값을 그대로 낸다.
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
            this.gcAexList.Location = new System.Drawing.Point(1214, 597);
            this.gcAexList.MainView = this.gvAexList;
            this.gcAexList.Name = "gcAexList";
            this.gcAexList.Size = new System.Drawing.Size(678, 278);
            this.gcAexList.TabIndex = 17;
            this.gcAexList.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvAexList});
            //
            // gvAexList
            //
            this.gvAexList.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colAexName});
            this.gvAexList.GridControl = this.gcAexList;
            this.gvAexList.Name = "gvAexList";
            this.gvAexList.OptionsBehavior.AutoPopulateColumns = false;
            this.gvAexList.OptionsBehavior.Editable = false;
            this.gvAexList.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.gvAexList.OptionsSelection.EnableAppearanceFocusedRow = false;
            this.gvAexList.OptionsView.ShowGroupPanel = false;
            this.gvAexList.OptionsView.ShowIndicator = false;
            //
            // colAexName
            //
            this.colAexName.Caption = "검사명";
            this.colAexName.FieldName = "ExamItemName";
            this.colAexName.Name = "colAexName";
            this.colAexName.MinWidth = 160;
            this.colAexName.Visible = true;
            this.colAexName.VisibleIndex = 0;
            //
            // pccColumns
            //
            this.pccColumns.Controls.Add(this.clbColumns);
            this.pccColumns.Controls.Add(this.btnColumnsDefault);
            this.pccColumns.Location = new System.Drawing.Point(0, 0);
            this.pccColumns.Name = "pccColumns";
            this.pccColumns.Size = new System.Drawing.Size(180, 230);
            this.pccColumns.TabIndex = 1;
            //
            // clbColumns
            //
            // 목록은 Grid 가 가진 컬럼에서 만든다 (ConfigureUI). 여기에 컬럼 이름을 적으면
            // Designer 와 두 곳이 된다 (ROOT AGENTS.md §6).
            this.clbColumns.CheckOnClick = true;
            this.clbColumns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clbColumns.Location = new System.Drawing.Point(2, 2);
            this.clbColumns.Name = "clbColumns";
            this.clbColumns.Size = new System.Drawing.Size(176, 198);
            this.clbColumns.TabIndex = 0;
            this.clbColumns.ItemCheck += new DevExpress.XtraEditors.Controls.ItemCheckEventHandler(this.clbColumns_ItemCheck);
            //
            // btnColumnsDefault
            //
            this.btnColumnsDefault.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnColumnsDefault.Location = new System.Drawing.Point(2, 200);
            this.btnColumnsDefault.Name = "btnColumnsDefault";
            this.btnColumnsDefault.Size = new System.Drawing.Size(176, 28);
            this.btnColumnsDefault.TabIndex = 1;
            this.btnColumnsDefault.Text = "기본값 복원";
            this.btnColumnsDefault.Click += new System.EventHandler(this.btnColumnsDefault_Click);
            //
            // Root
            //
            this.Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lcgList,
            this.splitWork,
            this.lcgDetail});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(1916, 887);
            this.Root.TextVisible = false;
            //
            // lcgList
            //
            // 그룹 Caption 은 Context 를 적는다 — 예약 관리 / 접수 관리 (03 §9.1).
            // 초기값은 Presenter 가 생성자에서 다시 쓰므로 여기 값은 디자인 표면용이다.
            this.lcgList.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lcgSearch,
            this.lciValidation,
            this.lciWorkList});
            this.lcgList.Location = new System.Drawing.Point(0, 0);
            this.lcgList.Name = "lcgList";
            this.lcgList.Size = new System.Drawing.Size(1180, 887);
            this.lcgList.Text = "예약 관리";
            //
            // lcgSearch
            //
            this.lcgSearch.GroupBordersVisible = false;
            this.lcgSearch.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciDateFrom,
            this.lciDateTo,
            this.lciStatus,
            this.lciChartNo,
            this.lciName,
            this.emptySpaceSearch,
            this.lciColumns,
            this.lciSearch});
            this.lcgSearch.Location = new System.Drawing.Point(0, 0);
            this.lcgSearch.Name = "lcgSearch";
            this.lcgSearch.Size = new System.Drawing.Size(1180, 26);
            this.lcgSearch.TextVisible = false;
            //
            // lciDateFrom
            //
            this.lciDateFrom.Control = this.deFrom;
            this.lciDateFrom.Location = new System.Drawing.Point(0, 0);
            this.lciDateFrom.MaxSize = new System.Drawing.Size(210, 26);
            this.lciDateFrom.MinSize = new System.Drawing.Size(210, 26);
            this.lciDateFrom.Name = "lciDateFrom";
            this.lciDateFrom.Size = new System.Drawing.Size(210, 26);
            this.lciDateFrom.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDateFrom.Text = "예약/접수일";
            this.lciDateFrom.TextSize = new System.Drawing.Size(72, 14);
            //
            // lciDateTo
            //
            this.lciDateTo.Control = this.deTo;
            this.lciDateTo.Location = new System.Drawing.Point(210, 0);
            this.lciDateTo.MaxSize = new System.Drawing.Size(138, 26);
            this.lciDateTo.MinSize = new System.Drawing.Size(138, 26);
            this.lciDateTo.Name = "lciDateTo";
            this.lciDateTo.Size = new System.Drawing.Size(138, 26);
            this.lciDateTo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDateTo.Text = "~";
            this.lciDateTo.TextSize = new System.Drawing.Size(12, 14);
            //
            // lciStatus
            //
            this.lciStatus.Control = this.cboStatus;
            this.lciStatus.Location = new System.Drawing.Point(348, 0);
            this.lciStatus.MaxSize = new System.Drawing.Size(160, 26);
            this.lciStatus.MinSize = new System.Drawing.Size(160, 26);
            this.lciStatus.Name = "lciStatus";
            this.lciStatus.Size = new System.Drawing.Size(160, 26);
            this.lciStatus.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciStatus.Text = "상태";
            this.lciStatus.TextSize = new System.Drawing.Size(36, 14);
            //
            // lciChartNo
            //
            this.lciChartNo.Control = this.txtChartNo;
            this.lciChartNo.Location = new System.Drawing.Point(508, 0);
            this.lciChartNo.MaxSize = new System.Drawing.Size(180, 26);
            this.lciChartNo.MinSize = new System.Drawing.Size(180, 26);
            this.lciChartNo.Name = "lciChartNo";
            this.lciChartNo.Size = new System.Drawing.Size(180, 26);
            this.lciChartNo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciChartNo.Text = "차트번호";
            this.lciChartNo.TextSize = new System.Drawing.Size(60, 14);
            //
            // lciName
            //
            this.lciName.Control = this.txtName;
            this.lciName.Location = new System.Drawing.Point(688, 0);
            this.lciName.MaxSize = new System.Drawing.Size(170, 26);
            this.lciName.MinSize = new System.Drawing.Size(170, 26);
            this.lciName.Name = "lciName";
            this.lciName.Size = new System.Drawing.Size(170, 26);
            this.lciName.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciName.Text = "이름";
            this.lciName.TextSize = new System.Drawing.Size(48, 14);
            //
            // emptySpaceSearch
            //
            this.emptySpaceSearch.AllowHotTrack = false;
            this.emptySpaceSearch.Location = new System.Drawing.Point(858, 0);
            this.emptySpaceSearch.Name = "emptySpaceSearch";
            this.emptySpaceSearch.Size = new System.Drawing.Size(111, 26);
            this.emptySpaceSearch.TextSize = new System.Drawing.Size(0, 0);
            //
            // lciColumns
            //
            this.lciColumns.Control = this.cboColumns;
            this.lciColumns.Location = new System.Drawing.Point(969, 0);
            this.lciColumns.MaxSize = new System.Drawing.Size(115, 26);
            this.lciColumns.MinSize = new System.Drawing.Size(115, 26);
            this.lciColumns.Name = "lciColumns";
            this.lciColumns.Size = new System.Drawing.Size(115, 26);
            this.lciColumns.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciColumns.TextVisible = false;
            //
            // lciSearch
            //
            this.lciSearch.Control = this.btnSearch;
            this.lciSearch.Location = new System.Drawing.Point(1084, 0);
            this.lciSearch.MaxSize = new System.Drawing.Size(96, 26);
            this.lciSearch.MinSize = new System.Drawing.Size(96, 26);
            this.lciSearch.Name = "lciSearch";
            this.lciSearch.Size = new System.Drawing.Size(96, 26);
            this.lciSearch.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciSearch.TextVisible = false;
            //
            // lciValidation
            //
            this.lciValidation.Control = this.lblValidation;
            this.lciValidation.Location = new System.Drawing.Point(0, 26);
            this.lciValidation.MaxSize = new System.Drawing.Size(0, 22);
            this.lciValidation.MinSize = new System.Drawing.Size(104, 22);
            this.lciValidation.Name = "lciValidation";
            this.lciValidation.Size = new System.Drawing.Size(1180, 22);
            this.lciValidation.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciValidation.TextSize = new System.Drawing.Size(0, 0);
            this.lciValidation.TextVisible = false;
            //
            // lciWorkList
            //
            this.lciWorkList.Control = this.gcWorkList;
            this.lciWorkList.Location = new System.Drawing.Point(0, 48);
            this.lciWorkList.Name = "lciWorkList";
            this.lciWorkList.Size = new System.Drawing.Size(1180, 839);
            this.lciWorkList.TextSize = new System.Drawing.Size(0, 0);
            this.lciWorkList.TextVisible = false;
            //
            // splitWork
            //
            // 03 §9.2 — 좌 조회조건+Grid 60 : 우 Detail 40. 사용자 조절을 허용한다.
            this.splitWork.AllowHotTrack = true;
            this.splitWork.Location = new System.Drawing.Point(1180, 0);
            this.splitWork.Name = "splitWork";
            this.splitWork.Size = new System.Drawing.Size(10, 887);
            //
            // lcgDetail
            //
            this.lcgDetail.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lcgDetailPatient,
            this.lcgDetailWork,
            this.lcgDetailNex,
            this.lcgDetailAex});
            this.lcgDetail.Location = new System.Drawing.Point(1190, 0);
            this.lcgDetail.Name = "lcgDetail";
            // 두 소그룹의 라벨 폭을 한 벌로 맞춘다 — 손으로 x 좌표를 맞추던 자리다.
            this.lcgDetail.OptionsItemText.TextAlignMode = DevExpress.XtraLayout.TextAlignModeGroup.AlignWithChildren;
            this.lcgDetail.Size = new System.Drawing.Size(726, 887);
            this.lcgDetail.Text = "업무 상세 · ReadOnly";
            //
            // lcgDetailPatient
            //
            this.lcgDetailPatient.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciDetailChartNo,
            this.lciDetailName,
            this.lciDetailBirthGender});
            this.lcgDetailPatient.Location = new System.Drawing.Point(0, 0);
            this.lcgDetailPatient.Name = "lcgDetailPatient";
            this.lcgDetailPatient.Size = new System.Drawing.Size(726, 96);
            this.lcgDetailPatient.Text = "수검자";
            //
            // lciDetailChartNo
            //
            this.lciDetailChartNo.Control = this.txtDetailChartNo;
            this.lciDetailChartNo.Location = new System.Drawing.Point(0, 0);
            this.lciDetailChartNo.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailChartNo.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailChartNo.Name = "lciDetailChartNo";
            this.lciDetailChartNo.Size = new System.Drawing.Size(726, 24);
            this.lciDetailChartNo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailChartNo.Text = "차트번호";
            this.lciDetailChartNo.TextSize = new System.Drawing.Size(84, 14);
            //
            // lciDetailName
            //
            this.lciDetailName.Control = this.txtDetailName;
            this.lciDetailName.Location = new System.Drawing.Point(0, 24);
            this.lciDetailName.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailName.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailName.Name = "lciDetailName";
            this.lciDetailName.Size = new System.Drawing.Size(726, 24);
            this.lciDetailName.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailName.Text = "이름";
            this.lciDetailName.TextSize = new System.Drawing.Size(84, 14);
            //
            // lciDetailBirthGender
            //
            this.lciDetailBirthGender.Control = this.txtDetailBirthGender;
            this.lciDetailBirthGender.Location = new System.Drawing.Point(0, 48);
            this.lciDetailBirthGender.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailBirthGender.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailBirthGender.Name = "lciDetailBirthGender";
            this.lciDetailBirthGender.Size = new System.Drawing.Size(726, 24);
            this.lciDetailBirthGender.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailBirthGender.Text = "생년월일 / 성별";
            this.lciDetailBirthGender.TextSize = new System.Drawing.Size(84, 14);
            //
            // lcgDetailWork
            //
            this.lcgDetailWork.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciDetailReserveDate,
            this.lciDetailSlot,
            this.lciDetailCapacity,
            this.lciDetailStatus});
            this.lcgDetailWork.Location = new System.Drawing.Point(0, 96);
            this.lcgDetailWork.Name = "lcgDetailWork";
            this.lcgDetailWork.Size = new System.Drawing.Size(726, 120);
            this.lcgDetailWork.Text = "예약";
            //
            // lciDetailReserveDate
            //
            this.lciDetailReserveDate.Control = this.txtDetailReserveDate;
            this.lciDetailReserveDate.Location = new System.Drawing.Point(0, 0);
            this.lciDetailReserveDate.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailReserveDate.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailReserveDate.Name = "lciDetailReserveDate";
            this.lciDetailReserveDate.Size = new System.Drawing.Size(726, 24);
            this.lciDetailReserveDate.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailReserveDate.Text = "예약일";
            this.lciDetailReserveDate.TextSize = new System.Drawing.Size(84, 14);
            //
            // lciDetailSlot
            //
            this.lciDetailSlot.Control = this.txtDetailSlot;
            this.lciDetailSlot.Location = new System.Drawing.Point(0, 24);
            this.lciDetailSlot.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailSlot.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailSlot.Name = "lciDetailSlot";
            this.lciDetailSlot.Size = new System.Drawing.Size(726, 24);
            this.lciDetailSlot.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailSlot.Text = "시간대";
            this.lciDetailSlot.TextSize = new System.Drawing.Size(84, 14);
            //
            // lciDetailCapacity
            //
            this.lciDetailCapacity.Control = this.txtDetailCapacity;
            this.lciDetailCapacity.Location = new System.Drawing.Point(0, 48);
            this.lciDetailCapacity.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailCapacity.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailCapacity.Name = "lciDetailCapacity";
            this.lciDetailCapacity.Size = new System.Drawing.Size(726, 24);
            this.lciDetailCapacity.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailCapacity.Text = "현재 정원";
            this.lciDetailCapacity.TextSize = new System.Drawing.Size(84, 14);
            //
            // lciDetailStatus
            //
            this.lciDetailStatus.Control = this.txtDetailStatus;
            this.lciDetailStatus.Location = new System.Drawing.Point(0, 72);
            this.lciDetailStatus.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailStatus.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailStatus.Name = "lciDetailStatus";
            this.lciDetailStatus.Size = new System.Drawing.Size(726, 24);
            this.lciDetailStatus.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailStatus.Text = "상태";
            this.lciDetailStatus.TextSize = new System.Drawing.Size(84, 14);
            //
            // lcgDetailNex
            //
            this.lcgDetailNex.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciNexList});
            this.lcgDetailNex.Location = new System.Drawing.Point(0, 216);
            this.lcgDetailNex.Name = "lcgDetailNex";
            this.lcgDetailNex.Size = new System.Drawing.Size(726, 336);
            this.lcgDetailNex.Text = "국가검사 (NEX)";
            //
            // lciNexList
            //
            this.lciNexList.Control = this.gcNexList;
            this.lciNexList.Location = new System.Drawing.Point(0, 0);
            this.lciNexList.Name = "lciNexList";
            this.lciNexList.Size = new System.Drawing.Size(726, 336);
            this.lciNexList.TextSize = new System.Drawing.Size(0, 0);
            this.lciNexList.TextVisible = false;
            //
            // lcgDetailAex
            //
            this.lcgDetailAex.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciAexList});
            this.lcgDetailAex.Location = new System.Drawing.Point(0, 552);
            this.lcgDetailAex.Name = "lcgDetailAex";
            this.lcgDetailAex.Size = new System.Drawing.Size(726, 335);
            this.lcgDetailAex.Text = "추가검사 (AEX)";
            //
            // lciAexList
            //
            this.lciAexList.Control = this.gcAexList;
            this.lciAexList.Location = new System.Drawing.Point(0, 0);
            this.lciAexList.Name = "lciAexList";
            this.lciAexList.Size = new System.Drawing.Size(726, 335);
            this.lciAexList.TextSize = new System.Drawing.Size(0, 0);
            this.lciAexList.TextVisible = false;
            //
            // UcWorkbench
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // Dock=Fill 인 lcMain 을 먼저 넣는다 (references/designer.md 함정 3).
            // 팝업은 PopupContainerEdit 이 뜰 때 팝업 창으로 옮겨 간다.
            this.Controls.Add(this.lcMain);
            this.Controls.Add(this.pccColumns);
            this.Name = "UcWorkbench";
            this.Size = new System.Drawing.Size(1916, 887);
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).EndInit();
            this.lcMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.deFrom.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deFrom.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deTo.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deTo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboStatus.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboColumns.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcWorkList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvWorkList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailChartNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailBirthGender.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailReserveDate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailSlot.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailCapacity.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailStatus.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcNexList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvNexList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcAexList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvAexList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.clbColumns)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pccColumns)).EndInit();
            this.pccColumns.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDateFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDateTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciStatus)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciChartNo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciColumns)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciValidation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciWorkList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitWork)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetail)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailPatient)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailChartNo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailBirthGender)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailWork)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailReserveDate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailSlot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailCapacity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailStatus)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailNex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciNexList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailAex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciAexList)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl lcMain;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraLayout.LayoutControlGroup lcgList;
        private DevExpress.XtraLayout.LayoutControlGroup lcgSearch;
        private DevExpress.XtraEditors.DateEdit deFrom;
        private DevExpress.XtraEditors.DateEdit deTo;
        private DevExpress.XtraEditors.ImageComboBoxEdit cboStatus;
        private DevExpress.XtraEditors.TextEdit txtChartNo;
        private DevExpress.XtraEditors.TextEdit txtName;
        private DevExpress.XtraEditors.PopupContainerEdit cboColumns;
        private DevExpress.XtraEditors.PopupContainerControl pccColumns;
        private DevExpress.XtraEditors.CheckedListBoxControl clbColumns;
        private DevExpress.XtraEditors.SimpleButton btnColumnsDefault;
        private DevExpress.XtraEditors.SimpleButton btnSearch;
        private DevExpress.XtraEditors.LabelControl lblValidation;
        private DevExpress.XtraLayout.LayoutControlItem lciDateFrom;
        private DevExpress.XtraLayout.LayoutControlItem lciDateTo;
        private DevExpress.XtraLayout.LayoutControlItem lciStatus;
        private DevExpress.XtraLayout.LayoutControlItem lciChartNo;
        private DevExpress.XtraLayout.LayoutControlItem lciName;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceSearch;
        private DevExpress.XtraLayout.LayoutControlItem lciColumns;
        private DevExpress.XtraLayout.LayoutControlItem lciSearch;
        private DevExpress.XtraLayout.LayoutControlItem lciValidation;
        private DevExpress.XtraGrid.GridControl gcWorkList;
        private DevExpress.XtraGrid.Views.Grid.GridView gvWorkList;
        private DevExpress.XtraGrid.Columns.GridColumn colReserveDate;
        private DevExpress.XtraGrid.Columns.GridColumn colSlot;
        private DevExpress.XtraGrid.Columns.GridColumn colStatusName;
        private DevExpress.XtraGrid.Columns.GridColumn colName;
        private DevExpress.XtraGrid.Columns.GridColumn colChartNo;
        private DevExpress.XtraGrid.Columns.GridColumn colGender;
        private DevExpress.XtraGrid.Columns.GridColumn colBirthday;
        private DevExpress.XtraGrid.Columns.GridColumn colMobilePhone;
        private DevExpress.XtraLayout.LayoutControlItem lciWorkList;
        private DevExpress.XtraLayout.SplitterItem splitWork;
        private DevExpress.XtraLayout.LayoutControlGroup lcgDetail;
        private DevExpress.XtraLayout.LayoutControlGroup lcgDetailPatient;
        private DevExpress.XtraEditors.TextEdit txtDetailChartNo;
        private DevExpress.XtraEditors.TextEdit txtDetailName;
        private DevExpress.XtraEditors.TextEdit txtDetailBirthGender;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailChartNo;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailName;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailBirthGender;
        private DevExpress.XtraLayout.LayoutControlGroup lcgDetailWork;
        private DevExpress.XtraEditors.TextEdit txtDetailReserveDate;
        private DevExpress.XtraEditors.TextEdit txtDetailSlot;
        private DevExpress.XtraEditors.TextEdit txtDetailCapacity;
        private DevExpress.XtraEditors.TextEdit txtDetailStatus;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailReserveDate;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailSlot;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailCapacity;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailStatus;
        private DevExpress.XtraLayout.LayoutControlGroup lcgDetailNex;
        private DevExpress.XtraGrid.GridControl gcNexList;
        private DevExpress.XtraGrid.Views.Grid.GridView gvNexList;
        private DevExpress.XtraGrid.Columns.GridColumn colNexName;
        private DevExpress.XtraGrid.Columns.GridColumn colNexType;
        private DevExpress.XtraLayout.LayoutControlItem lciNexList;
        private DevExpress.XtraLayout.LayoutControlGroup lcgDetailAex;
        private DevExpress.XtraGrid.GridControl gcAexList;
        private DevExpress.XtraGrid.Views.Grid.GridView gvAexList;
        private DevExpress.XtraGrid.Columns.GridColumn colAexName;
        private DevExpress.XtraLayout.LayoutControlItem lciAexList;
    }
}
