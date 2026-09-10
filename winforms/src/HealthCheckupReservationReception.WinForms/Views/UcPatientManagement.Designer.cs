// 화면 ID: WF-PAT-01 — 수검자 관리 (03 §5)
namespace HealthCheckupReservationReception.Views
{
    partial class UcPatientManagement
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
            this.cboConditions = new DevExpress.XtraEditors.PopupContainerEdit();
            this.txtChartNo = new DevExpress.XtraEditors.TextEdit();
            this.txtName = new DevExpress.XtraEditors.TextEdit();
            this.txtSocialNumber = new DevExpress.XtraEditors.TextEdit();
            this.cboColumns = new DevExpress.XtraEditors.PopupContainerEdit();
            this.btnSearch = new DevExpress.XtraEditors.SimpleButton();
            this.gcPatientList = new DevExpress.XtraGrid.GridControl();
            this.gvPatientList = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colChartNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colBirthday = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colGender = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMobilePhone = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colSocialNumber = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colPhone = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colEmail = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colZipcode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colAddress = new DevExpress.XtraGrid.Columns.GridColumn();
            this.txtDetailChartNo = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailName = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailSocialNumber = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailBirthGender = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailMobilePhone = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailPhoneEmail = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailZipAddress = new DevExpress.XtraEditors.TextEdit();
            this.txtDetailAddressDetail = new DevExpress.XtraEditors.TextEdit();
            this.memoDetailMemo = new DevExpress.XtraEditors.MemoEdit();
            this.pccConditions = new DevExpress.XtraEditors.PopupContainerControl();
            this.clbConditions = new DevExpress.XtraEditors.CheckedListBoxControl();
            this.pccColumns = new DevExpress.XtraEditors.PopupContainerControl();
            this.clbColumns = new DevExpress.XtraEditors.CheckedListBoxControl();
            this.btnColumnsDefault = new DevExpress.XtraEditors.SimpleButton();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgList = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgSearch = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciConditions = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciChartNo = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciName = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciSocialNumber = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceSearch = new DevExpress.XtraLayout.EmptySpaceItem();
            this.lciColumns = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciSearch = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciPatientList = new DevExpress.XtraLayout.LayoutControlItem();
            this.splitPatient = new DevExpress.XtraLayout.SplitterItem();
            this.lcgDetail = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgDetailBasic = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciDetailChartNo = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciDetailName = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciDetailSocialNumber = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciDetailBirthGender = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgDetailContact = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciDetailMobilePhone = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciDetailPhoneEmail = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgDetailAddress = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciDetailZipAddress = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciDetailAddressDetail = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgDetailMemo = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciDetailMemo = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).BeginInit();
            this.lcMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cboConditions.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboColumns.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcPatientList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvPatientList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailChartNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailSocialNumber.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailBirthGender.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailMobilePhone.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailPhoneEmail.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailZipAddress.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailAddressDetail.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.memoDetailMemo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pccConditions)).BeginInit();
            this.pccConditions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.clbConditions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pccColumns)).BeginInit();
            this.pccColumns.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.clbColumns)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciConditions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciChartNo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSocialNumber)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciColumns)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSearch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciPatientList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitPatient)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetail)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailBasic)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailChartNo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailSocialNumber)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailBirthGender)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailContact)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailMobilePhone)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailPhoneEmail)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailAddress)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailZipAddress)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailAddressDetail)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailMemo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailMemo)).BeginInit();
            this.SuspendLayout();
            //
            // lcMain
            //
            this.lcMain.Controls.Add(this.cboConditions);
            this.lcMain.Controls.Add(this.txtChartNo);
            this.lcMain.Controls.Add(this.txtName);
            this.lcMain.Controls.Add(this.txtSocialNumber);
            this.lcMain.Controls.Add(this.cboColumns);
            this.lcMain.Controls.Add(this.btnSearch);
            this.lcMain.Controls.Add(this.gcPatientList);
            this.lcMain.Controls.Add(this.txtDetailChartNo);
            this.lcMain.Controls.Add(this.txtDetailName);
            this.lcMain.Controls.Add(this.txtDetailSocialNumber);
            this.lcMain.Controls.Add(this.txtDetailBirthGender);
            this.lcMain.Controls.Add(this.txtDetailMobilePhone);
            this.lcMain.Controls.Add(this.txtDetailPhoneEmail);
            this.lcMain.Controls.Add(this.txtDetailZipAddress);
            this.lcMain.Controls.Add(this.txtDetailAddressDetail);
            this.lcMain.Controls.Add(this.memoDetailMemo);
            // 03 §18 과 같은 이유다 — 항목을 숨기는 길이 하나여야 사용자가 되돌릴 줄 안다.
            // LayoutControl 의 기본 우클릭 메뉴는 항목을 끌어내 숨길 수 있으므로 닫는다.
            this.lcMain.AllowCustomization = false;
            this.lcMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lcMain.Location = new System.Drawing.Point(0, 0);
            this.lcMain.Name = "lcMain";
            this.lcMain.Root = this.Root;
            this.lcMain.Size = new System.Drawing.Size(1916, 887);
            this.lcMain.TabIndex = 0;
            //
            // cboConditions
            //
            // 표시글은 무엇이 켜졌는지가 아니라 늘 `조회 조건` 이다 — QueryDisplayText 가 정한다
            // (2026-09-10 사용자 결정). CheckedComboBoxEdit 은 그 이벤트를 지원하지 않아 못 쓴다.
            this.cboConditions.Location = new System.Drawing.Point(12, 31);
            this.cboConditions.Name = "cboConditions";
            this.cboConditions.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cboConditions.Properties.PopupControl = this.pccConditions;
            // 글자를 고르지도 캐럿을 두지도 않는다 — 값이 아니라 이름을 적는 자리다.
            this.cboConditions.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.cboConditions.Size = new System.Drawing.Size(110, 20);
            this.cboConditions.StyleController = this.lcMain;
            this.cboConditions.TabIndex = 3;
            this.cboConditions.QueryDisplayText += new DevExpress.XtraEditors.Controls.QueryDisplayTextEventHandler(this.cboConditions_QueryDisplayText);
            //
            // txtChartNo
            //
            this.txtChartNo.Location = new System.Drawing.Point(179, 31);
            this.txtChartNo.Name = "txtChartNo";
            this.txtChartNo.Properties.MaxLength = 100;
            this.txtChartNo.Size = new System.Drawing.Size(128, 20);
            this.txtChartNo.StyleController = this.lcMain;
            this.txtChartNo.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchInput_KeyDown);
            this.txtChartNo.TabIndex = 0;
            //
            // txtName
            //
            this.txtName.Location = new System.Drawing.Point(344, 31);
            this.txtName.Name = "txtName";
            this.txtName.Properties.MaxLength = 100;
            this.txtName.Size = new System.Drawing.Size(131, 20);
            this.txtName.StyleController = this.lcMain;
            this.txtName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchInput_KeyDown);
            this.txtName.TabIndex = 1;
            //
            // txtSocialNumber
            //
            this.txtSocialNumber.Location = new System.Drawing.Point(539, 31);
            this.txtSocialNumber.Name = "txtSocialNumber";
            this.txtSocialNumber.Properties.MaxLength = 14;
            this.txtSocialNumber.Properties.NullValuePrompt = "000000-0000000";
            this.txtSocialNumber.Properties.NullValuePromptShowForEmptyValue = true;
            this.txtSocialNumber.Size = new System.Drawing.Size(146, 20);
            this.txtSocialNumber.StyleController = this.lcMain;
            this.txtSocialNumber.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchInput_KeyDown);
            this.txtSocialNumber.TabIndex = 2;
            //
            // cboColumns
            //
            // 03 §18 컬럼설정. Ribbon [보기] 에 있던 것을 Grid 옆으로 옮겼다 — 목록을 보면서
            // 켜고 끄는 편이 낫다 (2026-09-10 사용자 결정). Modal 이던 FrmColumnChooser 를 걷었다.
            this.cboColumns.Location = new System.Drawing.Point(986, 31);
            this.cboColumns.Name = "cboColumns";
            this.cboColumns.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cboColumns.Properties.PopupControl = this.pccColumns;
            // 글자를 고르지도 캐럿을 두지도 않는다 — 값이 아니라 이름을 적는 자리다.
            this.cboColumns.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.cboColumns.Size = new System.Drawing.Size(110, 20);
            this.cboColumns.StyleController = this.lcMain;
            this.cboColumns.TabIndex = 4;
            this.cboColumns.QueryDisplayText += new DevExpress.XtraEditors.Controls.QueryDisplayTextEventHandler(this.cboColumns_QueryDisplayText);
            //
            // btnSearch
            //
            this.btnSearch.Location = new System.Drawing.Point(1100, 29);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(88, 22);
            this.btnSearch.StyleController = this.lcMain;
            this.btnSearch.TabIndex = 5;
            this.btnSearch.Text = "조회";
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            //
            // gcPatientList
            //
            this.gcPatientList.Location = new System.Drawing.Point(12, 57);
            this.gcPatientList.MainView = this.gvPatientList;
            this.gcPatientList.Name = "gcPatientList";
            this.gcPatientList.Size = new System.Drawing.Size(1176, 818);
            this.gcPatientList.TabIndex = 6;
            this.gcPatientList.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvPatientList});
            //
            // gvPatientList
            //
            this.gvPatientList.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colChartNo,
            this.colName,
            this.colBirthday,
            this.colGender,
            this.colMobilePhone,
            this.colSocialNumber,
            this.colPhone,
            this.colEmail,
            this.colZipcode,
            this.colAddress});
            this.gvPatientList.GridControl = this.gcPatientList;
            this.gvPatientList.Name = "gvPatientList";
            this.gvPatientList.OptionsBehavior.AutoPopulateColumns = false;
            this.gvPatientList.OptionsBehavior.Editable = false;
            this.gvPatientList.OptionsSelection.MultiSelect = false;
            this.gvPatientList.OptionsView.ShowGroupPanel = false;
            // [X] 가로 스크롤은 `ColumnAutoWidth` 가 **기본값 true** 이므로 컬럼 폭만으로는
            //     영영 서지 않는다 — Grid 가 컬럼을 뷰 폭에 욱여넣는다(실측 2026-09-10).
            //     가로로 미는 유일한 지렛대가 컬럼별 `MinWidth` 다: 합이 뷰 폭을 넘는 순간
            //     가로 스크롤바가 서고, 그 전까지는 빈틈 없이 폭을 채운다. 그래서 auto 를
            //     끄지 않고 컬럼마다 MinWidth 를 준다. 세로는 행 수만으로 알아서 선다.
            this.gvPatientList.FocusedRowChanged += new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(this.gvPatientList_FocusedRowChanged);
            this.gvPatientList.RowClick += new DevExpress.XtraGrid.Views.Grid.RowClickEventHandler(this.gvPatientList_RowClick);
            this.gvPatientList.CustomColumnDisplayText += new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(this.gvPatientList_CustomColumnDisplayText);
            //
            // colChartNo
            //
            this.colChartNo.Caption = "차트번호";
            this.colChartNo.FieldName = "ChartNo";
            this.colChartNo.Name = "colChartNo";
            this.colChartNo.MinWidth = 90;
            this.colChartNo.Visible = true;
            this.colChartNo.VisibleIndex = 0;
            this.colChartNo.Width = 115;
            //
            // colName
            //
            this.colName.Caption = "이름";
            this.colName.FieldName = "Name";
            this.colName.Name = "colName";
            this.colName.MinWidth = 80;
            this.colName.Visible = true;
            this.colName.VisibleIndex = 1;
            this.colName.Width = 80;
            //
            // colBirthday
            //
            this.colBirthday.Caption = "생년월일";
            this.colBirthday.FieldName = "Birthday";
            this.colBirthday.Name = "colBirthday";
            this.colBirthday.MinWidth = 90;
            this.colBirthday.Visible = true;
            this.colBirthday.VisibleIndex = 2;
            this.colBirthday.Width = 100;
            //
            // colGender
            //
            this.colGender.Caption = "성별";
            this.colGender.FieldName = "Gender";
            this.colGender.Name = "colGender";
            this.colGender.MinWidth = 50;
            this.colGender.Visible = true;
            this.colGender.VisibleIndex = 3;
            this.colGender.Width = 55;
            //
            // colMobilePhone
            //
            this.colMobilePhone.Caption = "휴대전화번호";
            this.colMobilePhone.FieldName = "MobilePhone";
            this.colMobilePhone.Name = "colMobilePhone";
            this.colMobilePhone.MinWidth = 120;
            this.colMobilePhone.Visible = true;
            this.colMobilePhone.VisibleIndex = 4;
            this.colMobilePhone.Width = 181;
            //
            // colSocialNumber
            //
            this.colSocialNumber.Caption = "주민등록번호";
            this.colSocialNumber.FieldName = "SocialNumber";
            this.colSocialNumber.Name = "colSocialNumber";
            this.colSocialNumber.MinWidth = 120;
            //
            // colPhone
            //
            this.colPhone.Caption = "전화번호";
            this.colPhone.FieldName = "Phone";
            this.colPhone.Name = "colPhone";
            this.colPhone.MinWidth = 110;
            //
            // colEmail
            //
            this.colEmail.Caption = "E-mail";
            this.colEmail.FieldName = "Email";
            this.colEmail.Name = "colEmail";
            this.colEmail.MinWidth = 160;
            //
            // colZipcode
            //
            this.colZipcode.Caption = "우편번호";
            this.colZipcode.FieldName = "Zipcode";
            this.colZipcode.Name = "colZipcode";
            this.colZipcode.MinWidth = 70;
            //
            // colAddress
            //
            this.colAddress.Caption = "주소";
            this.colAddress.FieldName = "Address";
            this.colAddress.Name = "colAddress";
            this.colAddress.MinWidth = 200;
            //
            // txtDetailChartNo
            //
            this.txtDetailChartNo.Location = new System.Drawing.Point(1310, 55);
            this.txtDetailChartNo.Name = "txtDetailChartNo";
            this.txtDetailChartNo.Properties.ReadOnly = true;
            this.txtDetailChartNo.Size = new System.Drawing.Size(582, 20);
            this.txtDetailChartNo.StyleController = this.lcMain;
            this.txtDetailChartNo.TabIndex = 7;
            //
            // txtDetailName
            //
            this.txtDetailName.Location = new System.Drawing.Point(1310, 79);
            this.txtDetailName.Name = "txtDetailName";
            this.txtDetailName.Properties.ReadOnly = true;
            this.txtDetailName.Size = new System.Drawing.Size(582, 20);
            this.txtDetailName.StyleController = this.lcMain;
            this.txtDetailName.TabIndex = 8;
            //
            // txtDetailSocialNumber
            //
            this.txtDetailSocialNumber.Location = new System.Drawing.Point(1310, 103);
            this.txtDetailSocialNumber.Name = "txtDetailSocialNumber";
            this.txtDetailSocialNumber.Properties.ReadOnly = true;
            this.txtDetailSocialNumber.Size = new System.Drawing.Size(582, 20);
            this.txtDetailSocialNumber.StyleController = this.lcMain;
            this.txtDetailSocialNumber.TabIndex = 9;
            //
            // txtDetailBirthGender
            //
            this.txtDetailBirthGender.Location = new System.Drawing.Point(1310, 127);
            this.txtDetailBirthGender.Name = "txtDetailBirthGender";
            this.txtDetailBirthGender.Properties.ReadOnly = true;
            this.txtDetailBirthGender.Size = new System.Drawing.Size(582, 20);
            this.txtDetailBirthGender.StyleController = this.lcMain;
            this.txtDetailBirthGender.TabIndex = 10;
            //
            // txtDetailMobilePhone
            //
            this.txtDetailMobilePhone.Location = new System.Drawing.Point(1310, 199);
            this.txtDetailMobilePhone.Name = "txtDetailMobilePhone";
            this.txtDetailMobilePhone.Properties.ReadOnly = true;
            this.txtDetailMobilePhone.Size = new System.Drawing.Size(582, 20);
            this.txtDetailMobilePhone.StyleController = this.lcMain;
            this.txtDetailMobilePhone.TabIndex = 11;
            //
            // txtDetailPhoneEmail
            //
            this.txtDetailPhoneEmail.Location = new System.Drawing.Point(1310, 223);
            this.txtDetailPhoneEmail.Name = "txtDetailPhoneEmail";
            this.txtDetailPhoneEmail.Properties.ReadOnly = true;
            this.txtDetailPhoneEmail.Size = new System.Drawing.Size(582, 20);
            this.txtDetailPhoneEmail.StyleController = this.lcMain;
            this.txtDetailPhoneEmail.TabIndex = 12;
            //
            // txtDetailZipAddress
            //
            this.txtDetailZipAddress.Location = new System.Drawing.Point(1310, 295);
            this.txtDetailZipAddress.Name = "txtDetailZipAddress";
            this.txtDetailZipAddress.Properties.ReadOnly = true;
            this.txtDetailZipAddress.Size = new System.Drawing.Size(582, 20);
            this.txtDetailZipAddress.StyleController = this.lcMain;
            this.txtDetailZipAddress.TabIndex = 13;
            //
            // txtDetailAddressDetail
            //
            this.txtDetailAddressDetail.Location = new System.Drawing.Point(1310, 319);
            this.txtDetailAddressDetail.Name = "txtDetailAddressDetail";
            this.txtDetailAddressDetail.Properties.ReadOnly = true;
            this.txtDetailAddressDetail.Size = new System.Drawing.Size(582, 20);
            this.txtDetailAddressDetail.StyleController = this.lcMain;
            this.txtDetailAddressDetail.TabIndex = 14;
            //
            // memoDetailMemo
            //
            this.memoDetailMemo.Location = new System.Drawing.Point(1214, 391);
            this.memoDetailMemo.Name = "memoDetailMemo";
            this.memoDetailMemo.Properties.ReadOnly = true;
            this.memoDetailMemo.Size = new System.Drawing.Size(678, 484);
            this.memoDetailMemo.StyleController = this.lcMain;
            this.memoDetailMemo.TabIndex = 15;
            //
            // pccConditions
            //
            this.pccConditions.Controls.Add(this.clbConditions);
            this.pccConditions.Location = new System.Drawing.Point(0, 0);
            this.pccConditions.Name = "pccConditions";
            this.pccConditions.Size = new System.Drawing.Size(150, 76);
            this.pccConditions.TabIndex = 1;
            //
            // clbConditions
            //
            this.clbConditions.CheckOnClick = true;
            this.clbConditions.Dock = System.Windows.Forms.DockStyle.Fill;
            // Value 는 조회조건 이름이다. 화면 코드가 이 값으로 입력칸을 찾는다.
            this.clbConditions.Items.AddRange(new DevExpress.XtraEditors.Controls.CheckedListBoxItem[] {
            new DevExpress.XtraEditors.Controls.CheckedListBoxItem("ChartNo", "차트번호", System.Windows.Forms.CheckState.Checked, true),
            new DevExpress.XtraEditors.Controls.CheckedListBoxItem("Name", "이름", System.Windows.Forms.CheckState.Checked, true),
            new DevExpress.XtraEditors.Controls.CheckedListBoxItem("SocialNumber", "주민번호", System.Windows.Forms.CheckState.Checked, true)});
            this.clbConditions.Location = new System.Drawing.Point(2, 2);
            this.clbConditions.Name = "clbConditions";
            this.clbConditions.Size = new System.Drawing.Size(146, 72);
            this.clbConditions.TabIndex = 0;
            this.clbConditions.ItemCheck += new DevExpress.XtraEditors.Controls.ItemCheckEventHandler(this.clbConditions_ItemCheck);
            //
            // pccColumns
            //
            this.pccColumns.Controls.Add(this.clbColumns);
            this.pccColumns.Controls.Add(this.btnColumnsDefault);
            this.pccColumns.Location = new System.Drawing.Point(160, 0);
            this.pccColumns.Name = "pccColumns";
            this.pccColumns.Size = new System.Drawing.Size(180, 260);
            this.pccColumns.TabIndex = 2;
            //
            // clbColumns
            //
            // 목록은 Grid 가 가진 컬럼에서 만든다 (ConfigureUI). 여기에 컬럼 이름을 적으면
            // Designer 와 두 곳이 된다 (ROOT AGENTS.md §6).
            this.clbColumns.CheckOnClick = true;
            this.clbColumns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clbColumns.Location = new System.Drawing.Point(2, 2);
            this.clbColumns.Name = "clbColumns";
            this.clbColumns.Size = new System.Drawing.Size(176, 228);
            this.clbColumns.TabIndex = 0;
            this.clbColumns.ItemCheck += new DevExpress.XtraEditors.Controls.ItemCheckEventHandler(this.clbColumns_ItemCheck);
            //
            // btnColumnsDefault
            //
            this.btnColumnsDefault.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnColumnsDefault.Location = new System.Drawing.Point(2, 230);
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
            this.splitPatient,
            this.lcgDetail});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(1916, 887);
            this.Root.TextVisible = false;
            //
            // lcgList
            //
            this.lcgList.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lcgSearch,
            this.lciPatientList});
            this.lcgList.Location = new System.Drawing.Point(0, 0);
            this.lcgList.Name = "lcgList";
            this.lcgList.Size = new System.Drawing.Size(1180, 887);
            this.lcgList.Text = "수검자 목록";
            //
            // lcgSearch
            //
            this.lcgSearch.GroupBordersVisible = false;
            this.lcgSearch.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciChartNo,
            this.lciName,
            this.lciSocialNumber,
            this.emptySpaceSearch,
            this.lciSearch,
            this.lciConditions,
            this.lciColumns});
            this.lcgSearch.Location = new System.Drawing.Point(0, 0);
            this.lcgSearch.Name = "lcgSearch";
            this.lcgSearch.Size = new System.Drawing.Size(1180, 26);
            this.lcgSearch.TextVisible = false;
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
            // lciChartNo
            //
            this.lciChartNo.Control = this.txtChartNo;
            this.lciChartNo.Location = new System.Drawing.Point(0, 0);
            this.lciChartNo.MaxSize = new System.Drawing.Size(203, 26);
            this.lciChartNo.MinSize = new System.Drawing.Size(203, 26);
            this.lciChartNo.Name = "lciChartNo";
            this.lciChartNo.Size = new System.Drawing.Size(203, 26);
            this.lciChartNo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciChartNo.Text = "차트번호";
            this.lciChartNo.TextSize = new System.Drawing.Size(48, 14);
            //
            // lciName
            //
            this.lciName.Control = this.txtName;
            this.lciName.Location = new System.Drawing.Point(203, 0);
            this.lciName.MaxSize = new System.Drawing.Size(203, 26);
            this.lciName.MinSize = new System.Drawing.Size(203, 26);
            this.lciName.Name = "lciName";
            this.lciName.Size = new System.Drawing.Size(203, 26);
            this.lciName.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciName.Text = "이름";
            this.lciName.TextSize = new System.Drawing.Size(48, 14);
            //
            // lciSocialNumber
            //
            this.lciSocialNumber.Control = this.txtSocialNumber;
            this.lciSocialNumber.Location = new System.Drawing.Point(406, 0);
            this.lciSocialNumber.MaxSize = new System.Drawing.Size(203, 26);
            this.lciSocialNumber.MinSize = new System.Drawing.Size(203, 26);
            this.lciSocialNumber.Name = "lciSocialNumber";
            this.lciSocialNumber.Size = new System.Drawing.Size(203, 26);
            this.lciSocialNumber.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciSocialNumber.Text = "주민번호";
            this.lciSocialNumber.TextSize = new System.Drawing.Size(48, 14);
            //
            // emptySpaceSearch
            //
            this.emptySpaceSearch.AllowHotTrack = false;
            this.emptySpaceSearch.Location = new System.Drawing.Point(609, 0);
            this.emptySpaceSearch.Name = "emptySpaceSearch";
            this.emptySpaceSearch.Size = new System.Drawing.Size(245, 26);
            this.emptySpaceSearch.TextSize = new System.Drawing.Size(0, 0);
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
            // lciPatientList
            //
            this.lciPatientList.Control = this.gcPatientList;
            this.lciPatientList.Location = new System.Drawing.Point(0, 26);
            this.lciPatientList.Name = "lciPatientList";
            this.lciPatientList.Size = new System.Drawing.Size(1180, 861);
            this.lciPatientList.TextSize = new System.Drawing.Size(0, 0);
            this.lciPatientList.TextVisible = false;
            //
            // splitPatient
            //
            // 설계 wf_pat_01.js — 본문을 좌 0.62 · 우 0.38 로 나눈다. SplitterItem 은
            // 창을 늘려도 그 비율을 유지한다.
            this.splitPatient.AllowHotTrack = true;
            this.splitPatient.Location = new System.Drawing.Point(1180, 0);
            this.splitPatient.Name = "splitPatient";
            this.splitPatient.Size = new System.Drawing.Size(10, 887);
            //
            // lcgDetail
            //
            this.lcgDetail.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lcgDetailBasic,
            this.lcgDetailContact,
            this.lcgDetailAddress,
            this.lcgDetailMemo});
            this.lcgDetail.Location = new System.Drawing.Point(1190, 0);
            this.lcgDetail.Name = "lcgDetail";
            // 세 소그룹의 라벨 폭을 한 벌로 맞춘다 — 손으로 x 좌표를 맞추던 자리다.
            this.lcgDetail.OptionsItemText.TextAlignMode = DevExpress.XtraLayout.TextAlignModeGroup.AlignWithChildren;
            this.lcgDetail.Size = new System.Drawing.Size(726, 887);
            this.lcgDetail.Text = "수검자 상세 · ReadOnly";
            //
            // lcgDetailBasic
            //
            this.lcgDetailBasic.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciDetailChartNo,
            this.lciDetailName,
            this.lciDetailSocialNumber,
            this.lciDetailBirthGender});
            this.lcgDetailBasic.Location = new System.Drawing.Point(0, 0);
            this.lcgDetailBasic.Name = "lcgDetailBasic";
            this.lcgDetailBasic.Size = new System.Drawing.Size(726, 120);
            this.lcgDetailBasic.Text = "기본정보";
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
            // lciDetailSocialNumber
            //
            this.lciDetailSocialNumber.Control = this.txtDetailSocialNumber;
            this.lciDetailSocialNumber.Location = new System.Drawing.Point(0, 48);
            this.lciDetailSocialNumber.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailSocialNumber.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailSocialNumber.Name = "lciDetailSocialNumber";
            this.lciDetailSocialNumber.Size = new System.Drawing.Size(726, 24);
            this.lciDetailSocialNumber.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailSocialNumber.Text = "주민등록번호";
            this.lciDetailSocialNumber.TextSize = new System.Drawing.Size(84, 14);
            //
            // lciDetailBirthGender
            //
            this.lciDetailBirthGender.Control = this.txtDetailBirthGender;
            this.lciDetailBirthGender.Location = new System.Drawing.Point(0, 72);
            this.lciDetailBirthGender.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailBirthGender.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailBirthGender.Name = "lciDetailBirthGender";
            this.lciDetailBirthGender.Size = new System.Drawing.Size(726, 24);
            this.lciDetailBirthGender.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailBirthGender.Text = "생년월일 / 성별";
            this.lciDetailBirthGender.TextSize = new System.Drawing.Size(84, 14);
            //
            // lcgDetailContact
            //
            this.lcgDetailContact.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciDetailMobilePhone,
            this.lciDetailPhoneEmail});
            this.lcgDetailContact.Location = new System.Drawing.Point(0, 120);
            this.lcgDetailContact.Name = "lcgDetailContact";
            this.lcgDetailContact.Size = new System.Drawing.Size(726, 72);
            this.lcgDetailContact.Text = "연락처";
            //
            // lciDetailMobilePhone
            //
            this.lciDetailMobilePhone.Control = this.txtDetailMobilePhone;
            this.lciDetailMobilePhone.Location = new System.Drawing.Point(0, 0);
            this.lciDetailMobilePhone.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailMobilePhone.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailMobilePhone.Name = "lciDetailMobilePhone";
            this.lciDetailMobilePhone.Size = new System.Drawing.Size(726, 24);
            this.lciDetailMobilePhone.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailMobilePhone.Text = "휴대전화";
            this.lciDetailMobilePhone.TextSize = new System.Drawing.Size(84, 14);
            //
            // lciDetailPhoneEmail
            //
            this.lciDetailPhoneEmail.Control = this.txtDetailPhoneEmail;
            this.lciDetailPhoneEmail.Location = new System.Drawing.Point(0, 24);
            this.lciDetailPhoneEmail.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailPhoneEmail.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailPhoneEmail.Name = "lciDetailPhoneEmail";
            this.lciDetailPhoneEmail.Size = new System.Drawing.Size(726, 24);
            this.lciDetailPhoneEmail.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailPhoneEmail.Text = "전화번호 / E-mail";
            this.lciDetailPhoneEmail.TextSize = new System.Drawing.Size(84, 14);
            //
            // lcgDetailAddress
            //
            this.lcgDetailAddress.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciDetailZipAddress,
            this.lciDetailAddressDetail});
            this.lcgDetailAddress.Location = new System.Drawing.Point(0, 192);
            this.lcgDetailAddress.Name = "lcgDetailAddress";
            this.lcgDetailAddress.Size = new System.Drawing.Size(726, 72);
            this.lcgDetailAddress.Text = "주소";
            //
            // lciDetailZipAddress
            //
            this.lciDetailZipAddress.Control = this.txtDetailZipAddress;
            this.lciDetailZipAddress.Location = new System.Drawing.Point(0, 0);
            this.lciDetailZipAddress.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailZipAddress.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailZipAddress.Name = "lciDetailZipAddress";
            this.lciDetailZipAddress.Size = new System.Drawing.Size(726, 24);
            this.lciDetailZipAddress.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailZipAddress.Text = "우편번호 / 주소";
            this.lciDetailZipAddress.TextSize = new System.Drawing.Size(84, 14);
            //
            // lciDetailAddressDetail
            //
            this.lciDetailAddressDetail.Control = this.txtDetailAddressDetail;
            this.lciDetailAddressDetail.Location = new System.Drawing.Point(0, 24);
            this.lciDetailAddressDetail.MaxSize = new System.Drawing.Size(0, 24);
            this.lciDetailAddressDetail.MinSize = new System.Drawing.Size(150, 24);
            this.lciDetailAddressDetail.Name = "lciDetailAddressDetail";
            this.lciDetailAddressDetail.Size = new System.Drawing.Size(726, 24);
            this.lciDetailAddressDetail.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciDetailAddressDetail.Text = "상세주소";
            this.lciDetailAddressDetail.TextSize = new System.Drawing.Size(84, 14);
            //
            // lcgDetailMemo
            //
            this.lcgDetailMemo.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciDetailMemo});
            this.lcgDetailMemo.Location = new System.Drawing.Point(0, 264);
            this.lcgDetailMemo.Name = "lcgDetailMemo";
            this.lcgDetailMemo.Size = new System.Drawing.Size(726, 623);
            this.lcgDetailMemo.Text = "메모";
            //
            // lciDetailMemo
            //
            this.lciDetailMemo.Control = this.memoDetailMemo;
            this.lciDetailMemo.Location = new System.Drawing.Point(0, 0);
            this.lciDetailMemo.Name = "lciDetailMemo";
            this.lciDetailMemo.Size = new System.Drawing.Size(726, 623);
            this.lciDetailMemo.TextSize = new System.Drawing.Size(0, 0);
            this.lciDetailMemo.TextVisible = false;
            //
            // UcPatientManagement
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // Dock=Fill 인 lcMain 을 먼저 넣는다 (references/designer.md 함정 3).
            // 팝업 둘은 PopupContainerEdit 이 뜰 때 팝업 창으로 옮겨 간다.
            this.Controls.Add(this.lcMain);
            this.Controls.Add(this.pccConditions);
            this.Controls.Add(this.pccColumns);
            this.Name = "UcPatientManagement";
            this.Size = new System.Drawing.Size(1916, 887);
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).EndInit();
            this.lcMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.cboConditions.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cboColumns.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcPatientList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvPatientList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailChartNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailSocialNumber.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailBirthGender.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailMobilePhone.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailPhoneEmail.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailZipAddress.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailAddressDetail.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.memoDetailMemo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.clbConditions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pccConditions)).EndInit();
            this.pccConditions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.clbColumns)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pccColumns)).EndInit();
            this.pccColumns.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciConditions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciChartNo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSocialNumber)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciColumns)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSearch)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciPatientList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitPatient)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetail)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailBasic)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailChartNo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailSocialNumber)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailBirthGender)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailContact)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailMobilePhone)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailPhoneEmail)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailAddress)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailZipAddress)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailAddressDetail)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgDetailMemo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciDetailMemo)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl lcMain;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraLayout.LayoutControlGroup lcgList;
        private DevExpress.XtraLayout.LayoutControlGroup lcgSearch;
        private DevExpress.XtraEditors.PopupContainerEdit cboConditions;
        private DevExpress.XtraEditors.PopupContainerControl pccConditions;
        private DevExpress.XtraEditors.CheckedListBoxControl clbConditions;
        private DevExpress.XtraEditors.TextEdit txtChartNo;
        private DevExpress.XtraEditors.TextEdit txtName;
        private DevExpress.XtraEditors.TextEdit txtSocialNumber;
        private DevExpress.XtraEditors.PopupContainerEdit cboColumns;
        private DevExpress.XtraEditors.PopupContainerControl pccColumns;
        private DevExpress.XtraEditors.CheckedListBoxControl clbColumns;
        private DevExpress.XtraEditors.SimpleButton btnColumnsDefault;
        private DevExpress.XtraEditors.SimpleButton btnSearch;
        private DevExpress.XtraLayout.LayoutControlItem lciConditions;
        private DevExpress.XtraLayout.LayoutControlItem lciChartNo;
        private DevExpress.XtraLayout.LayoutControlItem lciName;
        private DevExpress.XtraLayout.LayoutControlItem lciSocialNumber;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceSearch;
        private DevExpress.XtraLayout.LayoutControlItem lciColumns;
        private DevExpress.XtraLayout.LayoutControlItem lciSearch;
        private DevExpress.XtraGrid.GridControl gcPatientList;
        private DevExpress.XtraGrid.Views.Grid.GridView gvPatientList;
        private DevExpress.XtraGrid.Columns.GridColumn colChartNo;
        private DevExpress.XtraGrid.Columns.GridColumn colName;
        private DevExpress.XtraGrid.Columns.GridColumn colBirthday;
        private DevExpress.XtraGrid.Columns.GridColumn colGender;
        private DevExpress.XtraGrid.Columns.GridColumn colMobilePhone;
        private DevExpress.XtraGrid.Columns.GridColumn colSocialNumber;
        private DevExpress.XtraGrid.Columns.GridColumn colPhone;
        private DevExpress.XtraGrid.Columns.GridColumn colEmail;
        private DevExpress.XtraGrid.Columns.GridColumn colZipcode;
        private DevExpress.XtraGrid.Columns.GridColumn colAddress;
        private DevExpress.XtraLayout.LayoutControlItem lciPatientList;
        private DevExpress.XtraLayout.SplitterItem splitPatient;
        private DevExpress.XtraLayout.LayoutControlGroup lcgDetail;
        private DevExpress.XtraLayout.LayoutControlGroup lcgDetailBasic;
        private DevExpress.XtraEditors.TextEdit txtDetailChartNo;
        private DevExpress.XtraEditors.TextEdit txtDetailName;
        private DevExpress.XtraEditors.TextEdit txtDetailSocialNumber;
        private DevExpress.XtraEditors.TextEdit txtDetailBirthGender;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailChartNo;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailName;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailSocialNumber;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailBirthGender;
        private DevExpress.XtraLayout.LayoutControlGroup lcgDetailContact;
        private DevExpress.XtraEditors.TextEdit txtDetailMobilePhone;
        private DevExpress.XtraEditors.TextEdit txtDetailPhoneEmail;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailMobilePhone;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailPhoneEmail;
        private DevExpress.XtraLayout.LayoutControlGroup lcgDetailAddress;
        private DevExpress.XtraEditors.TextEdit txtDetailZipAddress;
        private DevExpress.XtraEditors.TextEdit txtDetailAddressDetail;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailZipAddress;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailAddressDetail;
        private DevExpress.XtraLayout.LayoutControlGroup lcgDetailMemo;
        private DevExpress.XtraEditors.MemoEdit memoDetailMemo;
        private DevExpress.XtraLayout.LayoutControlItem lciDetailMemo;
    }
}
