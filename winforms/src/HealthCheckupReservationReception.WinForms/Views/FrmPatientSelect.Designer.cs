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
            this.grpResult = new DevExpress.XtraEditors.GroupControl();
            this.gcPatientList = new DevExpress.XtraGrid.GridControl();
            this.gvPatientList = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colChartNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colSocialNumber = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colBirthday = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colGender = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMobilePhone = new DevExpress.XtraGrid.Columns.GridColumn();
            this.pnlBottom = new DevExpress.XtraEditors.PanelControl();
            this.btnSelect = new DevExpress.XtraEditors.SimpleButton();
            this.btnClose = new DevExpress.XtraEditors.SimpleButton();
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
            this.btnNew = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.grpResult)).BeginInit();
            this.grpResult.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gcPatientList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvPatientList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pnlBottom)).BeginInit();
            this.pnlBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grpSearch)).BeginInit();
            this.grpSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deBirthday.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deBirthday.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // grpResult
            //
            this.grpResult.Controls.Add(this.gcPatientList);
            this.grpResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpResult.Location = new System.Drawing.Point(0, 96);
            this.grpResult.Name = "grpResult";
            this.grpResult.Size = new System.Drawing.Size(730, 275);
            this.grpResult.TabIndex = 1;
            this.grpResult.Text = "조회 결과";
            //
            // gcPatientList
            //
            this.gcPatientList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gcPatientList.Location = new System.Drawing.Point(2, 23);
            this.gcPatientList.MainView = this.gvPatientList;
            this.gcPatientList.Name = "gcPatientList";
            this.gcPatientList.Size = new System.Drawing.Size(726, 250);
            this.gcPatientList.TabIndex = 0;
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
            this.gvPatientList.OptionsBehavior.Editable = false;
            this.gvPatientList.OptionsCustomization.AllowQuickHideColumns = false;
            this.gvPatientList.OptionsSelection.MultiSelect = false;
            this.gvPatientList.OptionsView.ShowGroupPanel = false;
            this.gvPatientList.CustomColumnDisplayText += new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(this.gvPatientList_CustomColumnDisplayText);
            this.gvPatientList.FocusedRowChanged += new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(this.gvPatientList_FocusedRowChanged);
            this.gvPatientList.RowClick += new DevExpress.XtraGrid.Views.Grid.RowClickEventHandler(this.gvPatientList_RowClick);
            //
            // colChartNo
            //
            this.colChartNo.Caption = "차트번호";
            this.colChartNo.FieldName = "ChartNo";
            this.colChartNo.Name = "colChartNo";
            this.colChartNo.Visible = true;
            this.colChartNo.VisibleIndex = 0;
            this.colChartNo.Width = 110;
            //
            // colName
            //
            this.colName.Caption = "이름";
            this.colName.FieldName = "Name";
            this.colName.Name = "colName";
            this.colName.Visible = true;
            this.colName.VisibleIndex = 1;
            this.colName.Width = 77;
            //
            // colSocialNumber
            //
            this.colSocialNumber.Caption = "주민번호";
            this.colSocialNumber.FieldName = "SocialNumber";
            this.colSocialNumber.Name = "colSocialNumber";
            this.colSocialNumber.Visible = true;
            this.colSocialNumber.VisibleIndex = 2;
            this.colSocialNumber.Width = 130;
            //
            // colBirthday
            //
            this.colBirthday.Caption = "생년월일";
            this.colBirthday.FieldName = "Birthday";
            this.colBirthday.Name = "colBirthday";
            this.colBirthday.Visible = true;
            this.colBirthday.VisibleIndex = 3;
            this.colBirthday.Width = 96;
            //
            // colGender
            //
            this.colGender.Caption = "성별";
            this.colGender.FieldName = "Gender";
            this.colGender.Name = "colGender";
            this.colGender.Visible = true;
            this.colGender.VisibleIndex = 4;
            this.colGender.Width = 48;
            //
            // colMobilePhone
            //
            this.colMobilePhone.Caption = "휴대전화";
            this.colMobilePhone.FieldName = "MobilePhone";
            this.colMobilePhone.Name = "colMobilePhone";
            this.colMobilePhone.Visible = true;
            this.colMobilePhone.VisibleIndex = 5;
            this.colMobilePhone.Width = 247;
            //
            // pnlBottom
            //
            this.pnlBottom.Controls.Add(this.btnSelect);
            this.pnlBottom.Controls.Add(this.btnClose);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 371);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(730, 42);
            this.pnlBottom.TabIndex = 2;
            //
            // btnSelect
            //
            this.btnSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelect.Location = new System.Drawing.Point(548, 8);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(80, 26);
            this.btnSelect.TabIndex = 0;
            this.btnSelect.Text = "선택";
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(636, 8);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(80, 26);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "닫기";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // grpSearch
            //
            // [X] GroupControl 은 캡션 높이를 ClientRectangle 에서 빼지 않는다. 절대좌표 자식은
            //     테두리부터 세므로 안쪽 y 를 캡션 높이만큼 내려 잡았다 (WF-PAT-01 과 같다).
            this.grpSearch.Controls.Add(this.lblChartNo);
            this.grpSearch.Controls.Add(this.txtChartNo);
            this.grpSearch.Controls.Add(this.lblName);
            this.grpSearch.Controls.Add(this.txtName);
            this.grpSearch.Controls.Add(this.lblSocialNumber);
            this.grpSearch.Controls.Add(this.txtSocialNumber);
            this.grpSearch.Controls.Add(this.lblBirthday);
            this.grpSearch.Controls.Add(this.deBirthday);
            this.grpSearch.Controls.Add(this.lblMobilePhone);
            this.grpSearch.Controls.Add(this.txtMobilePhone);
            this.grpSearch.Controls.Add(this.btnSearch);
            this.grpSearch.Controls.Add(this.btnNew);
            this.grpSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpSearch.Location = new System.Drawing.Point(0, 0);
            this.grpSearch.Name = "grpSearch";
            this.grpSearch.Size = new System.Drawing.Size(730, 96);
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
            this.txtChartNo.Location = new System.Drawing.Point(66, 32);
            this.txtChartNo.Name = "txtChartNo";
            this.txtChartNo.Properties.MaxLength = 100;
            this.txtChartNo.Size = new System.Drawing.Size(96, 20);
            this.txtChartNo.TabIndex = 1;
            //
            // lblName
            //
            this.lblName.Location = new System.Drawing.Point(176, 35);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(24, 14);
            this.lblName.TabIndex = 2;
            this.lblName.Text = "이름";
            //
            // txtName
            //
            this.txtName.Location = new System.Drawing.Point(206, 32);
            this.txtName.Name = "txtName";
            this.txtName.Properties.MaxLength = 100;
            this.txtName.Size = new System.Drawing.Size(79, 20);
            this.txtName.TabIndex = 3;
            //
            // lblSocialNumber
            //
            this.lblSocialNumber.Location = new System.Drawing.Point(299, 35);
            this.lblSocialNumber.Name = "lblSocialNumber";
            this.lblSocialNumber.Size = new System.Drawing.Size(48, 14);
            this.lblSocialNumber.TabIndex = 4;
            this.lblSocialNumber.Text = "주민번호";
            //
            // txtSocialNumber
            //
            this.txtSocialNumber.Location = new System.Drawing.Point(353, 32);
            this.txtSocialNumber.Name = "txtSocialNumber";
            this.txtSocialNumber.Properties.MaxLength = 14;
            this.txtSocialNumber.Size = new System.Drawing.Size(108, 20);
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
            this.deBirthday.Location = new System.Drawing.Point(66, 62);
            this.deBirthday.Name = "deBirthday";
            this.deBirthday.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deBirthday.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.deBirthday.Size = new System.Drawing.Size(110, 20);
            this.deBirthday.TabIndex = 7;
            //
            // lblMobilePhone
            //
            this.lblMobilePhone.Location = new System.Drawing.Point(190, 65);
            this.lblMobilePhone.Name = "lblMobilePhone";
            this.lblMobilePhone.Size = new System.Drawing.Size(48, 14);
            this.lblMobilePhone.TabIndex = 8;
            this.lblMobilePhone.Text = "휴대전화";
            //
            // txtMobilePhone
            //
            this.txtMobilePhone.Location = new System.Drawing.Point(244, 62);
            this.txtMobilePhone.Name = "txtMobilePhone";
            this.txtMobilePhone.Properties.MaxLength = 13;
            this.txtMobilePhone.Size = new System.Drawing.Size(130, 20);
            this.txtMobilePhone.TabIndex = 9;
            //
            // btnSearch
            //
            this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSearch.Location = new System.Drawing.Point(560, 32);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(60, 24);
            this.btnSearch.TabIndex = 10;
            this.btnSearch.Text = "조회";
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            //
            // btnNew
            //
            this.btnNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNew.Location = new System.Drawing.Point(628, 32);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(88, 24);
            this.btnNew.TabIndex = 11;
            this.btnNew.Text = "신규등록";
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            //
            // FrmPatientSelect
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(730, 413);
            this.Controls.Add(this.grpResult);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.grpSearch);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(746, 452);
            this.Name = "FrmPatientSelect";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "수검자 선택";
            ((System.ComponentModel.ISupportInitialize)(this.grpResult)).EndInit();
            this.grpResult.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gcPatientList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvPatientList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pnlBottom)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grpSearch)).EndInit();
            this.grpSearch.ResumeLayout(false);
            this.grpSearch.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deBirthday.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deBirthday.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.GroupControl grpResult;
        private DevExpress.XtraGrid.GridControl gcPatientList;
        private DevExpress.XtraGrid.Views.Grid.GridView gvPatientList;
        private DevExpress.XtraGrid.Columns.GridColumn colChartNo;
        private DevExpress.XtraGrid.Columns.GridColumn colName;
        private DevExpress.XtraGrid.Columns.GridColumn colSocialNumber;
        private DevExpress.XtraGrid.Columns.GridColumn colBirthday;
        private DevExpress.XtraGrid.Columns.GridColumn colGender;
        private DevExpress.XtraGrid.Columns.GridColumn colMobilePhone;
        private DevExpress.XtraEditors.PanelControl pnlBottom;
        private DevExpress.XtraEditors.SimpleButton btnSelect;
        private DevExpress.XtraEditors.SimpleButton btnClose;
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
        private DevExpress.XtraEditors.SimpleButton btnNew;
    }
}
