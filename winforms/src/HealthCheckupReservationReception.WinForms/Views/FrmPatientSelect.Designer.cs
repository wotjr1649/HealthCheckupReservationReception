// 화면 ID: DLG-PAT-02 — 수검자 선택 (03 §7)
namespace HealthCheckupReservationReception.Views
{
    partial class FrmPatientSelect
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
            this.txtSocialNumber = new DevExpress.XtraEditors.TextEdit();
            this.deBirthday = new DevExpress.XtraEditors.DateEdit();
            this.txtMobilePhone = new DevExpress.XtraEditors.TextEdit();
            this.btnSearch = new DevExpress.XtraEditors.SimpleButton();
            this.cboConditions = new DevExpress.XtraEditors.PopupContainerEdit();
            this.cboColumns = new DevExpress.XtraEditors.PopupContainerEdit();
            this.gcPatientList = new DevExpress.XtraGrid.GridControl();
            this.gvPatientList = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colChartNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colSocialNumber = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colBirthday = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colGender = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMobilePhone = new DevExpress.XtraGrid.Columns.GridColumn();
            this.btnNew = new DevExpress.XtraEditors.SimpleButton();
            this.btnSelect = new DevExpress.XtraEditors.SimpleButton();
            this.btnClose = new DevExpress.XtraEditors.SimpleButton();
            this.pccConditions = new DevExpress.XtraEditors.PopupContainerControl();
            this.clbConditions = new DevExpress.XtraEditors.CheckedListBoxControl();
            this.pccColumns = new DevExpress.XtraEditors.PopupContainerControl();
            this.clbColumns = new DevExpress.XtraEditors.CheckedListBoxControl();
            this.btnColumnsDefault = new DevExpress.XtraEditors.SimpleButton();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgSearch = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciChartNo = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciName = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciSocialNumber = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciBirthday = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciMobilePhone = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceSearch = new DevExpress.XtraLayout.EmptySpaceItem();
            this.lciSearch = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciConditions = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciColumns = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciPatientList = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgActions = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciNew = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceActions = new DevExpress.XtraLayout.EmptySpaceItem();
            this.lciSelect = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciClose = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).BeginInit();
            this.lcMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deBirthday.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deBirthday.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboConditions.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboColumns.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcPatientList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvPatientList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pccConditions)).BeginInit();
            this.pccConditions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.clbConditions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pccColumns)).BeginInit();
            this.pccColumns.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.clbColumns)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciChartNo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSocialNumber)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciBirthday)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciMobilePhone)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciConditions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciColumns)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciPatientList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgActions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciNew)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceActions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSelect)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciClose)).BeginInit();
            this.SuspendLayout();
            //
            // lcMain
            //
            this.lcMain.Controls.Add(this.txtChartNo);
            this.lcMain.Controls.Add(this.txtName);
            this.lcMain.Controls.Add(this.txtSocialNumber);
            this.lcMain.Controls.Add(this.deBirthday);
            this.lcMain.Controls.Add(this.txtMobilePhone);
            this.lcMain.Controls.Add(this.btnSearch);
            this.lcMain.Controls.Add(this.cboConditions);
            this.lcMain.Controls.Add(this.cboColumns);
            this.lcMain.Controls.Add(this.gcPatientList);
            this.lcMain.Controls.Add(this.btnNew);
            this.lcMain.Controls.Add(this.btnSelect);
            this.lcMain.Controls.Add(this.btnClose);
            this.lcMain.AllowCustomization = false;
            this.lcMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lcMain.Location = new System.Drawing.Point(0, 0);
            this.lcMain.Name = "lcMain";
            this.lcMain.Root = this.Root;
            this.lcMain.Size = new System.Drawing.Size(1180, 640);
            this.lcMain.TabIndex = 0;
            //
            // txtChartNo
            //
            this.txtChartNo.Location = new System.Drawing.Point(72, 31);
            this.txtChartNo.Name = "txtChartNo";
            this.txtChartNo.Properties.MaxLength = 100;
            this.txtChartNo.Size = new System.Drawing.Size(100, 20);
            this.txtChartNo.StyleController = this.lcMain;
            this.txtChartNo.TabIndex = 0;
            this.txtChartNo.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchInput_KeyDown);
            //
            // txtName
            //
            this.txtName.Location = new System.Drawing.Point(237, 31);
            this.txtName.Name = "txtName";
            this.txtName.Properties.MaxLength = 100;
            this.txtName.Size = new System.Drawing.Size(80, 20);
            this.txtName.StyleController = this.lcMain;
            this.txtName.TabIndex = 1;
            this.txtName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchInput_KeyDown);
            //
            // txtSocialNumber
            //
            this.txtSocialNumber.Location = new System.Drawing.Point(382, 31);
            this.txtSocialNumber.Name = "txtSocialNumber";
            this.txtSocialNumber.Properties.MaxLength = 14;
            this.txtSocialNumber.Properties.NullValuePrompt = "000000-0000000";
            this.txtSocialNumber.Properties.NullValuePromptShowForEmptyValue = true;
            this.txtSocialNumber.Size = new System.Drawing.Size(125, 20);
            this.txtSocialNumber.StyleController = this.lcMain;
            this.txtSocialNumber.TabIndex = 2;
            this.txtSocialNumber.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchInput_KeyDown);
            //
            // deBirthday
            //
            // 03 §7.2 는 조회계약을 §5.3 에 위임한다 — 달력으로 고르고 조회에는 yyyyMMdd 로 간다.
            // 그 규칙은 ConfigureUI 의 clsSearchConditions.SetupBirthday 가 WF-PAT-01 과 한 벌로 갖는다.
            this.deBirthday.EditValue = null;
            this.deBirthday.Location = new System.Drawing.Point(572, 31);
            this.deBirthday.Name = "deBirthday";
            this.deBirthday.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deBirthday.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deBirthday.Size = new System.Drawing.Size(110, 20);
            this.deBirthday.StyleController = this.lcMain;
            this.deBirthday.TabIndex = 3;
            this.deBirthday.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchInput_KeyDown);
            //
            // txtMobilePhone
            //
            this.txtMobilePhone.Location = new System.Drawing.Point(747, 31);
            this.txtMobilePhone.Name = "txtMobilePhone";
            this.txtMobilePhone.Properties.MaxLength = 13;
            this.txtMobilePhone.Size = new System.Drawing.Size(110, 20);
            this.txtMobilePhone.StyleController = this.lcMain;
            this.txtMobilePhone.TabIndex = 4;
            this.txtMobilePhone.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchInput_KeyDown);
            //
            // btnSearch
            //
            this.btnSearch.Location = new System.Drawing.Point(866, 29);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(88, 22);
            this.btnSearch.StyleController = this.lcMain;
            this.btnSearch.TabIndex = 5;
            this.btnSearch.Text = "조회";
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            //
            // cboConditions
            //
            this.cboConditions.Location = new System.Drawing.Point(962, 31);
            this.cboConditions.Name = "cboConditions";
            this.cboConditions.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cboConditions.Properties.PopupControl = this.pccConditions;
            this.cboConditions.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.cboConditions.Size = new System.Drawing.Size(110, 20);
            this.cboConditions.StyleController = this.lcMain;
            this.cboConditions.TabIndex = 6;
            this.cboConditions.QueryDisplayText += new DevExpress.XtraEditors.Controls.QueryDisplayTextEventHandler(this.cboConditions_QueryDisplayText);
            //
            // cboColumns
            //
            this.cboColumns.Location = new System.Drawing.Point(1077, 31);
            this.cboColumns.Name = "cboColumns";
            this.cboColumns.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cboColumns.Properties.PopupControl = this.pccColumns;
            this.cboColumns.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.cboColumns.Size = new System.Drawing.Size(110, 20);
            this.cboColumns.StyleController = this.lcMain;
            this.cboColumns.TabIndex = 7;
            this.cboColumns.QueryDisplayText += new DevExpress.XtraEditors.Controls.QueryDisplayTextEventHandler(this.cboColumns_QueryDisplayText);
            //
            // gcPatientList
            //
            this.gcPatientList.Location = new System.Drawing.Point(12, 57);
            this.gcPatientList.MainView = this.gvPatientList;
            this.gcPatientList.Name = "gcPatientList";
            this.gcPatientList.Size = new System.Drawing.Size(1156, 554);
            this.gcPatientList.TabIndex = 8;
            this.gcPatientList.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvPatientList});
            //
            // gvPatientList
            //
            this.gvPatientList.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colChartNo,
            this.colName,
            this.colSocialNumber,
            this.colBirthday,
            this.colGender,
            this.colMobilePhone});
            this.gvPatientList.GridControl = this.gcPatientList;
            this.gvPatientList.Name = "gvPatientList";
            this.gvPatientList.OptionsBehavior.AutoPopulateColumns = false;
            this.gvPatientList.OptionsBehavior.Editable = false;
            // 03 §7.2 — Single Row Selection.
            this.gvPatientList.OptionsSelection.MultiSelect = false;
            this.gvPatientList.OptionsView.ShowGroupPanel = false;
            this.gvPatientList.FocusedRowChanged += new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(this.gvPatientList_FocusedRowChanged);
            this.gvPatientList.RowClick += new DevExpress.XtraGrid.Views.Grid.RowClickEventHandler(this.gvPatientList_RowClick);
            this.gvPatientList.CustomColumnDisplayText += new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(this.gvPatientList_CustomColumnDisplayText);
            //
            // colChartNo
            //
            this.colChartNo.Caption = "차트번호";
            this.colChartNo.FieldName = "ChartNo";
            this.colChartNo.Name = "colChartNo";
            this.colChartNo.MinWidth = 110;
            this.colChartNo.Visible = true;
            this.colChartNo.VisibleIndex = 0;
            this.colChartNo.Width = 130;
            //
            // colName
            //
            this.colName.Caption = "이름";
            this.colName.FieldName = "Name";
            this.colName.Name = "colName";
            this.colName.MinWidth = 80;
            this.colName.Visible = true;
            this.colName.VisibleIndex = 1;
            this.colName.Width = 100;
            //
            // colSocialNumber
            //
            // 03 §7.2 — 주민번호는 전체값이다. 마스킹하지 않는다.
            this.colSocialNumber.Caption = "주민등록번호";
            this.colSocialNumber.FieldName = "SocialNumber";
            this.colSocialNumber.Name = "colSocialNumber";
            this.colSocialNumber.MinWidth = 130;
            this.colSocialNumber.Visible = true;
            this.colSocialNumber.VisibleIndex = 2;
            this.colSocialNumber.Width = 150;
            //
            // colBirthday
            //
            this.colBirthday.Caption = "생년월일";
            this.colBirthday.FieldName = "Birthday";
            this.colBirthday.Name = "colBirthday";
            this.colBirthday.MinWidth = 90;
            this.colBirthday.Visible = true;
            this.colBirthday.VisibleIndex = 3;
            this.colBirthday.Width = 110;
            //
            // colGender
            //
            this.colGender.Caption = "성별";
            this.colGender.FieldName = "Gender";
            this.colGender.Name = "colGender";
            this.colGender.MinWidth = 50;
            this.colGender.Visible = true;
            this.colGender.VisibleIndex = 4;
            this.colGender.Width = 60;
            //
            // colMobilePhone
            //
            this.colMobilePhone.Caption = "휴대전화번호";
            this.colMobilePhone.FieldName = "MobilePhone";
            this.colMobilePhone.Name = "colMobilePhone";
            this.colMobilePhone.MinWidth = 120;
            this.colMobilePhone.Visible = true;
            this.colMobilePhone.VisibleIndex = 5;
            this.colMobilePhone.Width = 140;
            //
            // btnNew
            //
            // 03 §7.2 — 결과가 없으면 여기서 DLG-PAT-01 New 로 들어간다. 조회 줄이 아니라
            // 결과 아래에 둔다: 빈 목록을 보고 나서 누르는 버튼이고, 조회 줄은 세 화면이
            // 같은 꼴이어야 한다 (2026-09-10 사용자 지적).
            this.btnNew.Location = new System.Drawing.Point(12, 615);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(112, 22);
            this.btnNew.StyleController = this.lcMain;
            this.btnNew.TabIndex = 9;
            this.btnNew.Text = "신규등록";
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            //
            // btnSelect
            //
            this.btnSelect.Location = new System.Drawing.Point(1000, 615);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(80, 22);
            this.btnSelect.StyleController = this.lcMain;
            this.btnSelect.TabIndex = 10;
            this.btnSelect.Text = "선택";
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            //
            // btnClose
            //
            this.btnClose.Location = new System.Drawing.Point(1088, 615);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(80, 22);
            this.btnClose.StyleController = this.lcMain;
            this.btnClose.TabIndex = 11;
            this.btnClose.Text = "닫기";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // pccConditions
            //
            this.pccConditions.Controls.Add(this.clbConditions);
            this.pccConditions.Location = new System.Drawing.Point(0, 0);
            this.pccConditions.Name = "pccConditions";
            this.pccConditions.Size = new System.Drawing.Size(150, 116);
            this.pccConditions.TabIndex = 1;
            //
            // clbConditions
            //
            // 항목은 ConfigureUI 의 clsSearchConditions 가 만든다 (ROOT AGENTS.md §6).
            this.clbConditions.CheckOnClick = true;
            this.clbConditions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clbConditions.Location = new System.Drawing.Point(2, 2);
            this.clbConditions.Name = "clbConditions";
            this.clbConditions.Size = new System.Drawing.Size(146, 112);
            this.clbConditions.TabIndex = 0;
            this.clbConditions.ItemCheck += new DevExpress.XtraEditors.Controls.ItemCheckEventHandler(this.clbConditions_ItemCheck);
            //
            // pccColumns
            //
            this.pccColumns.Controls.Add(this.clbColumns);
            this.pccColumns.Controls.Add(this.btnColumnsDefault);
            this.pccColumns.Location = new System.Drawing.Point(160, 0);
            this.pccColumns.Name = "pccColumns";
            this.pccColumns.Size = new System.Drawing.Size(180, 200);
            this.pccColumns.TabIndex = 2;
            //
            // clbColumns
            //
            this.clbColumns.CheckOnClick = true;
            this.clbColumns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clbColumns.Location = new System.Drawing.Point(2, 2);
            this.clbColumns.Name = "clbColumns";
            this.clbColumns.Size = new System.Drawing.Size(176, 168);
            this.clbColumns.TabIndex = 0;
            this.clbColumns.ItemCheck += new DevExpress.XtraEditors.Controls.ItemCheckEventHandler(this.clbColumns_ItemCheck);
            //
            // btnColumnsDefault
            //
            this.btnColumnsDefault.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnColumnsDefault.Location = new System.Drawing.Point(2, 170);
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
            this.lcgSearch,
            this.lciPatientList,
            this.lcgActions});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(1180, 640);
            this.Root.TextVisible = false;
            //
            // lcgSearch
            //
            // 조회 한 줄은 WF-PAT-01 · WF-WRK-01 과 같은 꼴이다 — 입력칸들 · [조회] ·
            // [조회 조건] · [컬럼 설정] (2026-09-10 사용자 지적).
            this.lcgSearch.GroupBordersVisible = false;
            this.lcgSearch.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciChartNo,
            this.lciName,
            this.lciSocialNumber,
            this.lciBirthday,
            this.lciMobilePhone,
            this.emptySpaceSearch,
            this.lciSearch,
            this.lciConditions,
            this.lciColumns});
            this.lcgSearch.Location = new System.Drawing.Point(0, 0);
            this.lcgSearch.Name = "lcgSearch";
            this.lcgSearch.Size = new System.Drawing.Size(1180, 26);
            this.lcgSearch.TextVisible = false;
            //
            // lciChartNo
            //
            this.lciChartNo.Control = this.txtChartNo;
            this.lciChartNo.Location = new System.Drawing.Point(0, 0);
            this.lciChartNo.MaxSize = new System.Drawing.Size(157, 26);
            this.lciChartNo.MinSize = new System.Drawing.Size(157, 26);
            this.lciChartNo.Name = "lciChartNo";
            this.lciChartNo.Size = new System.Drawing.Size(157, 26);
            this.lciChartNo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciChartNo.Text = "차트번호";
            this.lciChartNo.TextSize = new System.Drawing.Size(48, 14);
            //
            // lciName
            //
            this.lciName.Control = this.txtName;
            this.lciName.Location = new System.Drawing.Point(157, 0);
            this.lciName.MaxSize = new System.Drawing.Size(137, 26);
            this.lciName.MinSize = new System.Drawing.Size(137, 26);
            this.lciName.Name = "lciName";
            this.lciName.Size = new System.Drawing.Size(137, 26);
            this.lciName.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciName.Text = "이름";
            this.lciName.TextSize = new System.Drawing.Size(48, 14);
            //
            // lciSocialNumber
            //
            this.lciSocialNumber.Control = this.txtSocialNumber;
            this.lciSocialNumber.Location = new System.Drawing.Point(294, 0);
            this.lciSocialNumber.MaxSize = new System.Drawing.Size(182, 26);
            this.lciSocialNumber.MinSize = new System.Drawing.Size(182, 26);
            this.lciSocialNumber.Name = "lciSocialNumber";
            this.lciSocialNumber.Size = new System.Drawing.Size(182, 26);
            this.lciSocialNumber.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciSocialNumber.Text = "주민번호";
            this.lciSocialNumber.TextSize = new System.Drawing.Size(48, 14);
            //
            // lciBirthday
            //
            this.lciBirthday.Control = this.deBirthday;
            this.lciBirthday.Location = new System.Drawing.Point(476, 0);
            this.lciBirthday.MaxSize = new System.Drawing.Size(167, 26);
            this.lciBirthday.MinSize = new System.Drawing.Size(167, 26);
            this.lciBirthday.Name = "lciBirthday";
            this.lciBirthday.Size = new System.Drawing.Size(167, 26);
            this.lciBirthday.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciBirthday.Text = "생년월일";
            this.lciBirthday.TextSize = new System.Drawing.Size(48, 14);
            //
            // lciMobilePhone
            //
            this.lciMobilePhone.Control = this.txtMobilePhone;
            this.lciMobilePhone.Location = new System.Drawing.Point(643, 0);
            this.lciMobilePhone.MaxSize = new System.Drawing.Size(167, 26);
            this.lciMobilePhone.MinSize = new System.Drawing.Size(167, 26);
            this.lciMobilePhone.Name = "lciMobilePhone";
            this.lciMobilePhone.Size = new System.Drawing.Size(167, 26);
            this.lciMobilePhone.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciMobilePhone.Text = "휴대전화";
            this.lciMobilePhone.TextSize = new System.Drawing.Size(48, 14);
            //
            // emptySpaceSearch
            //
            this.emptySpaceSearch.AllowHotTrack = false;
            this.emptySpaceSearch.Location = new System.Drawing.Point(810, 0);
            this.emptySpaceSearch.Name = "emptySpaceSearch";
            this.emptySpaceSearch.Size = new System.Drawing.Size(44, 26);
            this.emptySpaceSearch.TextSize = new System.Drawing.Size(0, 0);
            //
            // lciSearch
            //
            this.lciSearch.Control = this.btnSearch;
            this.lciSearch.Location = new System.Drawing.Point(854, 0);
            this.lciSearch.MaxSize = new System.Drawing.Size(96, 26);
            this.lciSearch.MinSize = new System.Drawing.Size(96, 26);
            this.lciSearch.Name = "lciSearch";
            this.lciSearch.Size = new System.Drawing.Size(96, 26);
            this.lciSearch.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciSearch.TextVisible = false;
            //
            // lciConditions
            //
            this.lciConditions.Control = this.cboConditions;
            this.lciConditions.Location = new System.Drawing.Point(950, 0);
            this.lciConditions.MaxSize = new System.Drawing.Size(115, 26);
            this.lciConditions.MinSize = new System.Drawing.Size(115, 26);
            this.lciConditions.Name = "lciConditions";
            this.lciConditions.Size = new System.Drawing.Size(115, 26);
            this.lciConditions.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciConditions.TextVisible = false;
            //
            // lciColumns
            //
            this.lciColumns.Control = this.cboColumns;
            this.lciColumns.Location = new System.Drawing.Point(1065, 0);
            this.lciColumns.MaxSize = new System.Drawing.Size(115, 26);
            this.lciColumns.MinSize = new System.Drawing.Size(115, 26);
            this.lciColumns.Name = "lciColumns";
            this.lciColumns.Size = new System.Drawing.Size(115, 26);
            this.lciColumns.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciColumns.TextVisible = false;
            //
            // lciPatientList
            //
            this.lciPatientList.Control = this.gcPatientList;
            this.lciPatientList.Location = new System.Drawing.Point(0, 26);
            this.lciPatientList.Name = "lciPatientList";
            this.lciPatientList.Size = new System.Drawing.Size(1180, 578);
            this.lciPatientList.TextSize = new System.Drawing.Size(0, 0);
            this.lciPatientList.TextVisible = false;
            //
            // lcgActions
            //
            this.lcgActions.GroupBordersVisible = false;
            this.lcgActions.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciNew,
            this.emptySpaceActions,
            this.lciSelect,
            this.lciClose});
            this.lcgActions.Location = new System.Drawing.Point(0, 604);
            this.lcgActions.Name = "lcgActions";
            this.lcgActions.Size = new System.Drawing.Size(1180, 36);
            this.lcgActions.TextVisible = false;
            //
            // lciNew
            //
            this.lciNew.Control = this.btnNew;
            this.lciNew.Location = new System.Drawing.Point(0, 0);
            this.lciNew.MaxSize = new System.Drawing.Size(120, 36);
            this.lciNew.MinSize = new System.Drawing.Size(120, 36);
            this.lciNew.Name = "lciNew";
            this.lciNew.Size = new System.Drawing.Size(120, 36);
            this.lciNew.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciNew.TextVisible = false;
            //
            // emptySpaceActions
            //
            this.emptySpaceActions.AllowHotTrack = false;
            this.emptySpaceActions.Location = new System.Drawing.Point(120, 0);
            this.emptySpaceActions.Name = "emptySpaceActions";
            this.emptySpaceActions.Size = new System.Drawing.Size(884, 36);
            this.emptySpaceActions.TextSize = new System.Drawing.Size(0, 0);
            //
            // lciSelect
            //
            this.lciSelect.Control = this.btnSelect;
            this.lciSelect.Location = new System.Drawing.Point(1004, 0);
            this.lciSelect.MaxSize = new System.Drawing.Size(88, 36);
            this.lciSelect.MinSize = new System.Drawing.Size(88, 36);
            this.lciSelect.Name = "lciSelect";
            this.lciSelect.Size = new System.Drawing.Size(88, 36);
            this.lciSelect.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciSelect.TextVisible = false;
            //
            // lciClose
            //
            this.lciClose.Control = this.btnClose;
            this.lciClose.Location = new System.Drawing.Point(1092, 0);
            this.lciClose.MaxSize = new System.Drawing.Size(88, 36);
            this.lciClose.MinSize = new System.Drawing.Size(88, 36);
            this.lciClose.Name = "lciClose";
            this.lciClose.Size = new System.Drawing.Size(88, 36);
            this.lciClose.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciClose.TextVisible = false;
            //
            // FrmPatientSelect
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(1180, 640);
            // Dock=Fill 인 lcMain 을 먼저 넣는다 (references/designer.md 함정 3).
            this.Controls.Add(this.lcMain);
            this.Controls.Add(this.pccConditions);
            this.Controls.Add(this.pccColumns);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(1000, 500);
            this.Name = "FrmPatientSelect";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "수검자 선택";
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).EndInit();
            this.lcMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deBirthday.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deBirthday.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboConditions.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboColumns.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcPatientList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvPatientList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.clbConditions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pccConditions)).EndInit();
            this.pccConditions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.clbColumns)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pccColumns)).EndInit();
            this.pccColumns.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciChartNo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSocialNumber)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciBirthday)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciMobilePhone)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciConditions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciColumns)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciPatientList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciNew)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSelect)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciClose)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl lcMain;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraLayout.LayoutControlGroup lcgSearch;
        private DevExpress.XtraEditors.TextEdit txtChartNo;
        private DevExpress.XtraEditors.TextEdit txtName;
        private DevExpress.XtraEditors.TextEdit txtSocialNumber;
        private DevExpress.XtraEditors.DateEdit deBirthday;
        private DevExpress.XtraEditors.TextEdit txtMobilePhone;
        private DevExpress.XtraEditors.SimpleButton btnSearch;
        private DevExpress.XtraEditors.PopupContainerEdit cboConditions;
        private DevExpress.XtraEditors.PopupContainerControl pccConditions;
        private DevExpress.XtraEditors.CheckedListBoxControl clbConditions;
        private DevExpress.XtraEditors.PopupContainerEdit cboColumns;
        private DevExpress.XtraEditors.PopupContainerControl pccColumns;
        private DevExpress.XtraEditors.CheckedListBoxControl clbColumns;
        private DevExpress.XtraEditors.SimpleButton btnColumnsDefault;
        private DevExpress.XtraLayout.LayoutControlItem lciChartNo;
        private DevExpress.XtraLayout.LayoutControlItem lciName;
        private DevExpress.XtraLayout.LayoutControlItem lciSocialNumber;
        private DevExpress.XtraLayout.LayoutControlItem lciBirthday;
        private DevExpress.XtraLayout.LayoutControlItem lciMobilePhone;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceSearch;
        private DevExpress.XtraLayout.LayoutControlItem lciSearch;
        private DevExpress.XtraLayout.LayoutControlItem lciConditions;
        private DevExpress.XtraLayout.LayoutControlItem lciColumns;
        private DevExpress.XtraGrid.GridControl gcPatientList;
        private DevExpress.XtraGrid.Views.Grid.GridView gvPatientList;
        private DevExpress.XtraGrid.Columns.GridColumn colChartNo;
        private DevExpress.XtraGrid.Columns.GridColumn colName;
        private DevExpress.XtraGrid.Columns.GridColumn colSocialNumber;
        private DevExpress.XtraGrid.Columns.GridColumn colBirthday;
        private DevExpress.XtraGrid.Columns.GridColumn colGender;
        private DevExpress.XtraGrid.Columns.GridColumn colMobilePhone;
        private DevExpress.XtraLayout.LayoutControlItem lciPatientList;
        private DevExpress.XtraLayout.LayoutControlGroup lcgActions;
        private DevExpress.XtraEditors.SimpleButton btnNew;
        private DevExpress.XtraEditors.SimpleButton btnSelect;
        private DevExpress.XtraEditors.SimpleButton btnClose;
        private DevExpress.XtraLayout.LayoutControlItem lciNew;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceActions;
        private DevExpress.XtraLayout.LayoutControlItem lciSelect;
        private DevExpress.XtraLayout.LayoutControlItem lciClose;
    }
}
