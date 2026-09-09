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
            this.grpSearch = new DevExpress.XtraEditors.GroupControl();
            this.lblChartNo = new DevExpress.XtraEditors.LabelControl();
            this.txtChartNo = new DevExpress.XtraEditors.TextEdit();
            this.lblName = new DevExpress.XtraEditors.LabelControl();
            this.txtName = new DevExpress.XtraEditors.TextEdit();
            this.lblSocialNumber = new DevExpress.XtraEditors.LabelControl();
            this.txtSocialNumber = new DevExpress.XtraEditors.TextEdit();
            this.lblBirthday = new DevExpress.XtraEditors.LabelControl();
            this.deBirthday = new DevExpress.XtraEditors.DateEdit();
            this.lblMobilePhone = new DevExpress.XtraEditors.LabelControl();
            this.txtMobilePhone = new DevExpress.XtraEditors.TextEdit();
            this.btnSearch = new DevExpress.XtraEditors.SimpleButton();
            this.splitPatient = new DevExpress.XtraEditors.SplitContainerControl();
            this.grpList = new DevExpress.XtraEditors.GroupControl();
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
            this.grpDetail = new DevExpress.XtraEditors.GroupControl();
            this.lblSectionBasic = new DevExpress.XtraEditors.LabelControl();
            this.lblDetailChartNo = new DevExpress.XtraEditors.LabelControl();
            this.txtDetailChartNo = new DevExpress.XtraEditors.TextEdit();
            this.lblDetailName = new DevExpress.XtraEditors.LabelControl();
            this.txtDetailName = new DevExpress.XtraEditors.TextEdit();
            this.lblDetailSocialNumber = new DevExpress.XtraEditors.LabelControl();
            this.txtDetailSocialNumber = new DevExpress.XtraEditors.TextEdit();
            this.lblDetailBirthGender = new DevExpress.XtraEditors.LabelControl();
            this.txtDetailBirthGender = new DevExpress.XtraEditors.TextEdit();
            this.lblSectionContact = new DevExpress.XtraEditors.LabelControl();
            this.lblDetailMobilePhone = new DevExpress.XtraEditors.LabelControl();
            this.txtDetailMobilePhone = new DevExpress.XtraEditors.TextEdit();
            this.lblDetailPhoneEmail = new DevExpress.XtraEditors.LabelControl();
            this.txtDetailPhoneEmail = new DevExpress.XtraEditors.TextEdit();
            this.lblSectionAddress = new DevExpress.XtraEditors.LabelControl();
            this.lblDetailZipAddress = new DevExpress.XtraEditors.LabelControl();
            this.txtDetailZipAddress = new DevExpress.XtraEditors.TextEdit();
            this.lblDetailAddressDetail = new DevExpress.XtraEditors.LabelControl();
            this.txtDetailAddressDetail = new DevExpress.XtraEditors.TextEdit();
            this.lblSectionMemo = new DevExpress.XtraEditors.LabelControl();
            this.memoDetailMemo = new DevExpress.XtraEditors.MemoEdit();
            ((System.ComponentModel.ISupportInitialize)(this.grpSearch)).BeginInit();
            this.grpSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deBirthday.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deBirthday.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitPatient)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitPatient.Panel1)).BeginInit();
            this.splitPatient.Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitPatient.Panel2)).BeginInit();
            this.splitPatient.Panel2.SuspendLayout();
            this.splitPatient.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grpList)).BeginInit();
            this.grpList.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gcPatientList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvPatientList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpDetail)).BeginInit();
            this.grpDetail.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailChartNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailSocialNumber.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailBirthGender.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailMobilePhone.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailPhoneEmail.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailZipAddress.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailAddressDetail.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.memoDetailMemo.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // grpSearch
            //
            // [X] GroupControl 은 캡션 높이를 ClientRectangle 에서 빼지 않는다. Dock 은
            //     DisplayRectangle 을 쓰므로 캡션 아래에 붙지만, 절대좌표 자식은 컨트롤
            //     테두리부터 세어 캡션 글자 위에 얹힌다. 안쪽 y 는 캡션 높이만큼 내려 잡았다.
            this.grpSearch.Controls.Add(this.btnSearch);
            this.grpSearch.Controls.Add(this.txtMobilePhone);
            this.grpSearch.Controls.Add(this.lblMobilePhone);
            this.grpSearch.Controls.Add(this.deBirthday);
            this.grpSearch.Controls.Add(this.lblBirthday);
            this.grpSearch.Controls.Add(this.txtSocialNumber);
            this.grpSearch.Controls.Add(this.lblSocialNumber);
            this.grpSearch.Controls.Add(this.txtName);
            this.grpSearch.Controls.Add(this.lblName);
            this.grpSearch.Controls.Add(this.txtChartNo);
            this.grpSearch.Controls.Add(this.lblChartNo);
            this.grpSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpSearch.Location = new System.Drawing.Point(0, 0);
            this.grpSearch.Name = "grpSearch";
            this.grpSearch.Size = new System.Drawing.Size(1916, 96);
            this.grpSearch.TabIndex = 0;
            this.grpSearch.Text = "조회조건";
            //
            // lblChartNo
            //
            this.lblChartNo.Location = new System.Drawing.Point(12, 35);
            this.lblChartNo.Name = "lblChartNo";
            this.lblChartNo.Size = new System.Drawing.Size(48, 14);
            this.lblChartNo.TabIndex = 0;
            this.lblChartNo.Text = "차트번호";
            //
            // txtChartNo
            //
            this.txtChartNo.Location = new System.Drawing.Point(82, 32);
            this.txtChartNo.Name = "txtChartNo";
            this.txtChartNo.Properties.MaxLength = 100;
            this.txtChartNo.Size = new System.Drawing.Size(150, 20);
            this.txtChartNo.TabIndex = 1;
            //
            // lblName
            //
            this.lblName.Location = new System.Drawing.Point(248, 35);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(24, 14);
            this.lblName.TabIndex = 2;
            this.lblName.Text = "이름";
            //
            // txtName
            //
            this.txtName.Location = new System.Drawing.Point(318, 32);
            this.txtName.Name = "txtName";
            this.txtName.Properties.MaxLength = 100;
            this.txtName.Size = new System.Drawing.Size(126, 20);
            this.txtName.TabIndex = 3;
            //
            // lblSocialNumber
            //
            this.lblSocialNumber.Location = new System.Drawing.Point(460, 35);
            this.lblSocialNumber.Name = "lblSocialNumber";
            this.lblSocialNumber.Size = new System.Drawing.Size(48, 14);
            this.lblSocialNumber.TabIndex = 4;
            this.lblSocialNumber.Text = "주민번호";
            //
            // txtSocialNumber
            //
            this.txtSocialNumber.Location = new System.Drawing.Point(530, 32);
            this.txtSocialNumber.Name = "txtSocialNumber";
            this.txtSocialNumber.Properties.MaxLength = 14;
            this.txtSocialNumber.Size = new System.Drawing.Size(192, 20);
            this.txtSocialNumber.TabIndex = 5;
            //
            // lblBirthday
            //
            this.lblBirthday.Location = new System.Drawing.Point(12, 65);
            this.lblBirthday.Name = "lblBirthday";
            this.lblBirthday.Size = new System.Drawing.Size(48, 14);
            this.lblBirthday.TabIndex = 6;
            this.lblBirthday.Text = "생년월일";
            //
            // deBirthday
            //
            this.deBirthday.EditValue = null;
            this.deBirthday.Location = new System.Drawing.Point(82, 62);
            this.deBirthday.Name = "deBirthday";
            this.deBirthday.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deBirthday.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deBirthday.Size = new System.Drawing.Size(150, 20);
            this.deBirthday.TabIndex = 7;
            //
            // lblMobilePhone
            //
            this.lblMobilePhone.Location = new System.Drawing.Point(248, 65);
            this.lblMobilePhone.Name = "lblMobilePhone";
            this.lblMobilePhone.Size = new System.Drawing.Size(48, 14);
            this.lblMobilePhone.TabIndex = 8;
            this.lblMobilePhone.Text = "휴대전화";
            //
            // txtMobilePhone
            //
            this.txtMobilePhone.Location = new System.Drawing.Point(318, 62);
            this.txtMobilePhone.Name = "txtMobilePhone";
            this.txtMobilePhone.Properties.MaxLength = 13;
            this.txtMobilePhone.Size = new System.Drawing.Size(186, 20);
            this.txtMobilePhone.TabIndex = 9;
            //
            // btnSearch
            //
            this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSearch.Location = new System.Drawing.Point(1802, 32);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(96, 26);
            this.btnSearch.TabIndex = 10;
            this.btnSearch.Text = "조회";
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            //
            // splitPatient
            //
            this.splitPatient.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitPatient.FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.None;
            this.splitPatient.Horizontal = true;
            this.splitPatient.Location = new System.Drawing.Point(0, 96);
            this.splitPatient.Name = "splitPatient";
            this.splitPatient.Panel1.Controls.Add(this.grpList);
            this.splitPatient.Panel1.Text = "Panel1";
            this.splitPatient.Panel2.Controls.Add(this.grpDetail);
            this.splitPatient.Panel2.Text = "Panel2";
            this.splitPatient.Size = new System.Drawing.Size(1916, 791);
            // 설계 wf_pat_01.js — 본문을 좌 0.62 · 우 0.38 로 나눈다. FixedPanel 이 None 이라
            // 창을 늘리면 그 비율이 그대로 간다.
            this.splitPatient.SplitterPosition = 1180;
            this.splitPatient.TabIndex = 1;
            //
            // grpList
            //
            this.grpList.Controls.Add(this.gcPatientList);
            this.grpList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpList.Location = new System.Drawing.Point(0, 0);
            this.grpList.Name = "grpList";
            this.grpList.Size = new System.Drawing.Size(1180, 791);
            this.grpList.TabIndex = 0;
            this.grpList.Text = "수검자 목록";
            //
            // gcPatientList
            //
            this.gcPatientList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gcPatientList.Location = new System.Drawing.Point(2, 23);
            this.gcPatientList.MainView = this.gvPatientList;
            this.gcPatientList.Name = "gcPatientList";
            this.gcPatientList.Size = new System.Drawing.Size(1176, 766);
            this.gcPatientList.TabIndex = 0;
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
            this.gvPatientList.FocusedRowChanged += new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(this.gvPatientList_FocusedRowChanged);
            this.gvPatientList.RowClick += new DevExpress.XtraGrid.Views.Grid.RowClickEventHandler(this.gvPatientList_RowClick);
            this.gvPatientList.CustomColumnDisplayText += new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(this.gvPatientList_CustomColumnDisplayText);
            //
            // colChartNo
            //
            this.colChartNo.Caption = "차트번호";
            this.colChartNo.FieldName = "ChartNo";
            this.colChartNo.Name = "colChartNo";
            this.colChartNo.Visible = true;
            this.colChartNo.VisibleIndex = 0;
            this.colChartNo.Width = 115;
            //
            // colName
            //
            this.colName.Caption = "이름";
            this.colName.FieldName = "Name";
            this.colName.Name = "colName";
            this.colName.Visible = true;
            this.colName.VisibleIndex = 1;
            this.colName.Width = 80;
            //
            // colBirthday
            //
            this.colBirthday.Caption = "생년월일";
            this.colBirthday.FieldName = "Birthday";
            this.colBirthday.Name = "colBirthday";
            this.colBirthday.Visible = true;
            this.colBirthday.VisibleIndex = 2;
            this.colBirthday.Width = 100;
            //
            // colGender
            //
            this.colGender.Caption = "성별";
            this.colGender.FieldName = "Gender";
            this.colGender.Name = "colGender";
            this.colGender.Visible = true;
            this.colGender.VisibleIndex = 3;
            this.colGender.Width = 55;
            //
            // colMobilePhone
            //
            this.colMobilePhone.Caption = "휴대전화번호";
            this.colMobilePhone.FieldName = "MobilePhone";
            this.colMobilePhone.Name = "colMobilePhone";
            this.colMobilePhone.Visible = true;
            this.colMobilePhone.VisibleIndex = 4;
            this.colMobilePhone.Width = 181;
            //
            // colSocialNumber
            //
            this.colSocialNumber.Caption = "주민등록번호";
            this.colSocialNumber.FieldName = "SocialNumber";
            this.colSocialNumber.Name = "colSocialNumber";
            //
            // colPhone
            //
            this.colPhone.Caption = "전화번호";
            this.colPhone.FieldName = "Phone";
            this.colPhone.Name = "colPhone";
            //
            // colEmail
            //
            this.colEmail.Caption = "E-mail";
            this.colEmail.FieldName = "Email";
            this.colEmail.Name = "colEmail";
            //
            // colZipcode
            //
            this.colZipcode.Caption = "우편번호";
            this.colZipcode.FieldName = "Zipcode";
            this.colZipcode.Name = "colZipcode";
            //
            // colAddress
            //
            this.colAddress.Caption = "주소";
            this.colAddress.FieldName = "Address";
            this.colAddress.Name = "colAddress";
            //
            // grpDetail
            //
            this.grpDetail.Controls.Add(this.memoDetailMemo);
            this.grpDetail.Controls.Add(this.lblSectionMemo);
            this.grpDetail.Controls.Add(this.txtDetailAddressDetail);
            this.grpDetail.Controls.Add(this.lblDetailAddressDetail);
            this.grpDetail.Controls.Add(this.txtDetailZipAddress);
            this.grpDetail.Controls.Add(this.lblDetailZipAddress);
            this.grpDetail.Controls.Add(this.lblSectionAddress);
            this.grpDetail.Controls.Add(this.txtDetailPhoneEmail);
            this.grpDetail.Controls.Add(this.lblDetailPhoneEmail);
            this.grpDetail.Controls.Add(this.txtDetailMobilePhone);
            this.grpDetail.Controls.Add(this.lblDetailMobilePhone);
            this.grpDetail.Controls.Add(this.lblSectionContact);
            this.grpDetail.Controls.Add(this.txtDetailBirthGender);
            this.grpDetail.Controls.Add(this.lblDetailBirthGender);
            this.grpDetail.Controls.Add(this.txtDetailSocialNumber);
            this.grpDetail.Controls.Add(this.lblDetailSocialNumber);
            this.grpDetail.Controls.Add(this.txtDetailName);
            this.grpDetail.Controls.Add(this.lblDetailName);
            this.grpDetail.Controls.Add(this.txtDetailChartNo);
            this.grpDetail.Controls.Add(this.lblDetailChartNo);
            this.grpDetail.Controls.Add(this.lblSectionBasic);
            this.grpDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDetail.Location = new System.Drawing.Point(0, 0);
            this.grpDetail.Name = "grpDetail";
            this.grpDetail.Size = new System.Drawing.Size(726, 791);
            this.grpDetail.TabIndex = 0;
            this.grpDetail.Text = "수검자 상세 · ReadOnly";
            //
            // lblSectionBasic
            //
            this.lblSectionBasic.Location = new System.Drawing.Point(10, 30);
            this.lblSectionBasic.Name = "lblSectionBasic";
            this.lblSectionBasic.Size = new System.Drawing.Size(66, 14);
            this.lblSectionBasic.TabIndex = 0;
            this.lblSectionBasic.Text = "[ 기본정보 ]";
            //
            // lblDetailChartNo
            //
            this.lblDetailChartNo.Location = new System.Drawing.Point(10, 53);
            this.lblDetailChartNo.Name = "lblDetailChartNo";
            this.lblDetailChartNo.Size = new System.Drawing.Size(48, 14);
            this.lblDetailChartNo.TabIndex = 1;
            this.lblDetailChartNo.Text = "차트번호";
            //
            // txtDetailChartNo
            //
            this.txtDetailChartNo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDetailChartNo.Location = new System.Drawing.Point(130, 50);
            this.txtDetailChartNo.Name = "txtDetailChartNo";
            this.txtDetailChartNo.Properties.ReadOnly = true;
            this.txtDetailChartNo.Size = new System.Drawing.Size(582, 20);
            this.txtDetailChartNo.TabIndex = 2;
            //
            // lblDetailName
            //
            this.lblDetailName.Location = new System.Drawing.Point(10, 79);
            this.lblDetailName.Name = "lblDetailName";
            this.lblDetailName.Size = new System.Drawing.Size(24, 14);
            this.lblDetailName.TabIndex = 3;
            this.lblDetailName.Text = "이름";
            //
            // txtDetailName
            //
            this.txtDetailName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDetailName.Location = new System.Drawing.Point(130, 76);
            this.txtDetailName.Name = "txtDetailName";
            this.txtDetailName.Properties.ReadOnly = true;
            this.txtDetailName.Size = new System.Drawing.Size(582, 20);
            this.txtDetailName.TabIndex = 4;
            //
            // lblDetailSocialNumber
            //
            this.lblDetailSocialNumber.Location = new System.Drawing.Point(10, 105);
            this.lblDetailSocialNumber.Name = "lblDetailSocialNumber";
            this.lblDetailSocialNumber.Size = new System.Drawing.Size(72, 14);
            this.lblDetailSocialNumber.TabIndex = 5;
            this.lblDetailSocialNumber.Text = "주민등록번호";
            //
            // txtDetailSocialNumber
            //
            this.txtDetailSocialNumber.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDetailSocialNumber.Location = new System.Drawing.Point(130, 102);
            this.txtDetailSocialNumber.Name = "txtDetailSocialNumber";
            this.txtDetailSocialNumber.Properties.ReadOnly = true;
            this.txtDetailSocialNumber.Size = new System.Drawing.Size(582, 20);
            this.txtDetailSocialNumber.TabIndex = 6;
            //
            // lblDetailBirthGender
            //
            this.lblDetailBirthGender.Location = new System.Drawing.Point(10, 131);
            this.lblDetailBirthGender.Name = "lblDetailBirthGender";
            this.lblDetailBirthGender.Size = new System.Drawing.Size(78, 14);
            this.lblDetailBirthGender.TabIndex = 7;
            this.lblDetailBirthGender.Text = "생년월일 / 성별";
            //
            // txtDetailBirthGender
            //
            this.txtDetailBirthGender.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDetailBirthGender.Location = new System.Drawing.Point(130, 128);
            this.txtDetailBirthGender.Name = "txtDetailBirthGender";
            this.txtDetailBirthGender.Properties.ReadOnly = true;
            this.txtDetailBirthGender.Size = new System.Drawing.Size(582, 20);
            this.txtDetailBirthGender.TabIndex = 8;
            //
            // lblSectionContact
            //
            this.lblSectionContact.Location = new System.Drawing.Point(10, 159);
            this.lblSectionContact.Name = "lblSectionContact";
            this.lblSectionContact.Size = new System.Drawing.Size(54, 14);
            this.lblSectionContact.TabIndex = 9;
            this.lblSectionContact.Text = "[ 연락처 ]";
            //
            // lblDetailMobilePhone
            //
            this.lblDetailMobilePhone.Location = new System.Drawing.Point(10, 182);
            this.lblDetailMobilePhone.Name = "lblDetailMobilePhone";
            this.lblDetailMobilePhone.Size = new System.Drawing.Size(48, 14);
            this.lblDetailMobilePhone.TabIndex = 10;
            this.lblDetailMobilePhone.Text = "휴대전화";
            //
            // txtDetailMobilePhone
            //
            this.txtDetailMobilePhone.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDetailMobilePhone.Location = new System.Drawing.Point(130, 179);
            this.txtDetailMobilePhone.Name = "txtDetailMobilePhone";
            this.txtDetailMobilePhone.Properties.ReadOnly = true;
            this.txtDetailMobilePhone.Size = new System.Drawing.Size(582, 20);
            this.txtDetailMobilePhone.TabIndex = 11;
            //
            // lblDetailPhoneEmail
            //
            this.lblDetailPhoneEmail.Location = new System.Drawing.Point(10, 208);
            this.lblDetailPhoneEmail.Name = "lblDetailPhoneEmail";
            this.lblDetailPhoneEmail.Size = new System.Drawing.Size(96, 14);
            this.lblDetailPhoneEmail.TabIndex = 12;
            this.lblDetailPhoneEmail.Text = "전화번호 / E-mail";
            //
            // txtDetailPhoneEmail
            //
            this.txtDetailPhoneEmail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDetailPhoneEmail.Location = new System.Drawing.Point(130, 205);
            this.txtDetailPhoneEmail.Name = "txtDetailPhoneEmail";
            this.txtDetailPhoneEmail.Properties.ReadOnly = true;
            this.txtDetailPhoneEmail.Size = new System.Drawing.Size(582, 20);
            this.txtDetailPhoneEmail.TabIndex = 13;
            //
            // lblSectionAddress
            //
            this.lblSectionAddress.Location = new System.Drawing.Point(10, 236);
            this.lblSectionAddress.Name = "lblSectionAddress";
            this.lblSectionAddress.Size = new System.Drawing.Size(42, 14);
            this.lblSectionAddress.TabIndex = 14;
            this.lblSectionAddress.Text = "[ 주소 ]";
            //
            // lblDetailZipAddress
            //
            this.lblDetailZipAddress.Location = new System.Drawing.Point(10, 259);
            this.lblDetailZipAddress.Name = "lblDetailZipAddress";
            this.lblDetailZipAddress.Size = new System.Drawing.Size(84, 14);
            this.lblDetailZipAddress.TabIndex = 15;
            this.lblDetailZipAddress.Text = "우편번호 / 주소";
            //
            // txtDetailZipAddress
            //
            this.txtDetailZipAddress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDetailZipAddress.Location = new System.Drawing.Point(130, 256);
            this.txtDetailZipAddress.Name = "txtDetailZipAddress";
            this.txtDetailZipAddress.Properties.ReadOnly = true;
            this.txtDetailZipAddress.Size = new System.Drawing.Size(582, 20);
            this.txtDetailZipAddress.TabIndex = 16;
            //
            // lblDetailAddressDetail
            //
            this.lblDetailAddressDetail.Location = new System.Drawing.Point(10, 285);
            this.lblDetailAddressDetail.Name = "lblDetailAddressDetail";
            this.lblDetailAddressDetail.Size = new System.Drawing.Size(48, 14);
            this.lblDetailAddressDetail.TabIndex = 17;
            this.lblDetailAddressDetail.Text = "상세주소";
            //
            // txtDetailAddressDetail
            //
            this.txtDetailAddressDetail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDetailAddressDetail.Location = new System.Drawing.Point(130, 282);
            this.txtDetailAddressDetail.Name = "txtDetailAddressDetail";
            this.txtDetailAddressDetail.Properties.ReadOnly = true;
            this.txtDetailAddressDetail.Size = new System.Drawing.Size(582, 20);
            this.txtDetailAddressDetail.TabIndex = 18;
            //
            // lblSectionMemo
            //
            this.lblSectionMemo.Location = new System.Drawing.Point(10, 313);
            this.lblSectionMemo.Name = "lblSectionMemo";
            this.lblSectionMemo.Size = new System.Drawing.Size(42, 14);
            this.lblSectionMemo.TabIndex = 19;
            this.lblSectionMemo.Text = "[ 메모 ]";
            //
            // memoDetailMemo
            //
            this.memoDetailMemo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.memoDetailMemo.Location = new System.Drawing.Point(10, 333);
            this.memoDetailMemo.Name = "memoDetailMemo";
            this.memoDetailMemo.Properties.ReadOnly = true;
            this.memoDetailMemo.Size = new System.Drawing.Size(702, 425);
            this.memoDetailMemo.TabIndex = 20;
            //
            // UcPatientManagement
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitPatient);
            this.Controls.Add(this.grpSearch);
            this.Name = "UcPatientManagement";
            this.Size = new System.Drawing.Size(1916, 887);
            ((System.ComponentModel.ISupportInitialize)(this.grpSearch)).EndInit();
            this.grpSearch.ResumeLayout(false);
            this.grpSearch.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deBirthday.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deBirthday.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitPatient.Panel1)).EndInit();
            this.splitPatient.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitPatient.Panel2)).EndInit();
            this.splitPatient.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitPatient)).EndInit();
            this.splitPatient.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grpList)).EndInit();
            this.grpList.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gcPatientList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvPatientList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grpDetail)).EndInit();
            this.grpDetail.ResumeLayout(false);
            this.grpDetail.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailChartNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailSocialNumber.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailBirthGender.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailMobilePhone.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailPhoneEmail.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailZipAddress.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDetailAddressDetail.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.memoDetailMemo.Properties)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.GroupControl grpSearch;
        private DevExpress.XtraEditors.LabelControl lblChartNo;
        private DevExpress.XtraEditors.TextEdit txtChartNo;
        private DevExpress.XtraEditors.LabelControl lblName;
        private DevExpress.XtraEditors.TextEdit txtName;
        private DevExpress.XtraEditors.LabelControl lblSocialNumber;
        private DevExpress.XtraEditors.TextEdit txtSocialNumber;
        private DevExpress.XtraEditors.LabelControl lblBirthday;
        private DevExpress.XtraEditors.DateEdit deBirthday;
        private DevExpress.XtraEditors.LabelControl lblMobilePhone;
        private DevExpress.XtraEditors.TextEdit txtMobilePhone;
        private DevExpress.XtraEditors.SimpleButton btnSearch;
        private DevExpress.XtraEditors.SplitContainerControl splitPatient;
        private DevExpress.XtraEditors.GroupControl grpList;
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
        private DevExpress.XtraEditors.GroupControl grpDetail;
        private DevExpress.XtraEditors.LabelControl lblSectionBasic;
        private DevExpress.XtraEditors.LabelControl lblDetailChartNo;
        private DevExpress.XtraEditors.TextEdit txtDetailChartNo;
        private DevExpress.XtraEditors.LabelControl lblDetailName;
        private DevExpress.XtraEditors.TextEdit txtDetailName;
        private DevExpress.XtraEditors.LabelControl lblDetailSocialNumber;
        private DevExpress.XtraEditors.TextEdit txtDetailSocialNumber;
        private DevExpress.XtraEditors.LabelControl lblDetailBirthGender;
        private DevExpress.XtraEditors.TextEdit txtDetailBirthGender;
        private DevExpress.XtraEditors.LabelControl lblSectionContact;
        private DevExpress.XtraEditors.LabelControl lblDetailMobilePhone;
        private DevExpress.XtraEditors.TextEdit txtDetailMobilePhone;
        private DevExpress.XtraEditors.LabelControl lblDetailPhoneEmail;
        private DevExpress.XtraEditors.TextEdit txtDetailPhoneEmail;
        private DevExpress.XtraEditors.LabelControl lblSectionAddress;
        private DevExpress.XtraEditors.LabelControl lblDetailZipAddress;
        private DevExpress.XtraEditors.TextEdit txtDetailZipAddress;
        private DevExpress.XtraEditors.LabelControl lblDetailAddressDetail;
        private DevExpress.XtraEditors.TextEdit txtDetailAddressDetail;
        private DevExpress.XtraEditors.LabelControl lblSectionMemo;
        private DevExpress.XtraEditors.MemoEdit memoDetailMemo;
    }
}
