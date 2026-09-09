// 화면 ID: DLG-PAT-03 — 중복 후보 확인 (03 §6.5)
namespace HealthCheckupReservationReception.Views
{
    partial class FrmPatientDuplicate
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
            this.grpCandidates = new DevExpress.XtraEditors.GroupControl();
            this.gcCandidates = new DevExpress.XtraGrid.GridControl();
            this.gvCandidates = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colChartNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colBirthday = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colGender = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colSocialNumber = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMobilePhone = new DevExpress.XtraGrid.Columns.GridColumn();
            this.pnlBottom = new DevExpress.XtraEditors.PanelControl();
            this.lblNotice = new DevExpress.XtraEditors.LabelControl();
            this.btnModify = new DevExpress.XtraEditors.SimpleButton();
            this.btnContinue = new DevExpress.XtraEditors.SimpleButton();
            this.btnClose = new DevExpress.XtraEditors.SimpleButton();
            this.grpInput = new DevExpress.XtraEditors.GroupControl();
            this.lblName = new DevExpress.XtraEditors.LabelControl();
            this.txtName = new DevExpress.XtraEditors.TextEdit();
            this.lblBirthday = new DevExpress.XtraEditors.LabelControl();
            this.txtBirthday = new DevExpress.XtraEditors.TextEdit();
            this.lblSocialNumber = new DevExpress.XtraEditors.LabelControl();
            this.txtSocialNumber = new DevExpress.XtraEditors.TextEdit();
            this.lblMobilePhone = new DevExpress.XtraEditors.LabelControl();
            this.txtMobilePhone = new DevExpress.XtraEditors.TextEdit();
            ((System.ComponentModel.ISupportInitialize)(this.grpCandidates)).BeginInit();
            this.grpCandidates.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gcCandidates)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvCandidates)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pnlBottom)).BeginInit();
            this.pnlBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grpInput)).BeginInit();
            this.grpInput.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtBirthday.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // grpCandidates
            //
            this.grpCandidates.Controls.Add(this.gcCandidates);
            this.grpCandidates.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCandidates.Location = new System.Drawing.Point(0, 62);
            this.grpCandidates.Name = "grpCandidates";
            this.grpCandidates.Size = new System.Drawing.Size(787, 222);
            this.grpCandidates.TabIndex = 1;
            this.grpCandidates.Text = "중복 후보 · 이름 + 생년월일 동일 / 주민등록번호 상이";
            //
            // gcCandidates
            //
            this.gcCandidates.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gcCandidates.Location = new System.Drawing.Point(2, 23);
            this.gcCandidates.MainView = this.gvCandidates;
            this.gcCandidates.Name = "gcCandidates";
            this.gcCandidates.Size = new System.Drawing.Size(783, 197);
            this.gcCandidates.TabIndex = 0;
            this.gcCandidates.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvCandidates});
            //
            // gvCandidates
            //
            this.gvCandidates.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colChartNo,
            this.colName,
            this.colBirthday,
            this.colGender,
            this.colSocialNumber,
            this.colMobilePhone});
            this.gvCandidates.GridControl = this.gcCandidates;
            this.gvCandidates.Name = "gvCandidates";
            this.gvCandidates.OptionsBehavior.Editable = false;
            this.gvCandidates.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.gvCandidates.OptionsSelection.EnableAppearanceFocusedRow = false;
            this.gvCandidates.OptionsCustomization.AllowQuickHideColumns = false;
            this.gvCandidates.OptionsView.ShowGroupPanel = false;
            this.gvCandidates.CustomColumnDisplayText += new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(this.gvCandidates_CustomColumnDisplayText);
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
            this.colName.Width = 77;
            //
            // colBirthday
            //
            this.colBirthday.Caption = "생년월일";
            this.colBirthday.FieldName = "Birthday";
            this.colBirthday.Name = "colBirthday";
            this.colBirthday.Visible = true;
            this.colBirthday.VisibleIndex = 2;
            this.colBirthday.Width = 101;
            //
            // colGender
            //
            this.colGender.Caption = "성별";
            this.colGender.FieldName = "Gender";
            this.colGender.Name = "colGender";
            this.colGender.Visible = true;
            this.colGender.VisibleIndex = 3;
            this.colGender.Width = 53;
            //
            // colSocialNumber
            //
            this.colSocialNumber.Caption = "주민번호";
            this.colSocialNumber.FieldName = "SocialNumber";
            this.colSocialNumber.Name = "colSocialNumber";
            this.colSocialNumber.Visible = true;
            this.colSocialNumber.VisibleIndex = 4;
            this.colSocialNumber.Width = 134;
            //
            // colMobilePhone
            //
            this.colMobilePhone.Caption = "휴대전화";
            this.colMobilePhone.FieldName = "MobilePhone";
            this.colMobilePhone.Name = "colMobilePhone";
            this.colMobilePhone.Visible = true;
            this.colMobilePhone.VisibleIndex = 5;
            this.colMobilePhone.Width = 295;
            //
            // pnlBottom
            //
            this.pnlBottom.Controls.Add(this.lblNotice);
            this.pnlBottom.Controls.Add(this.btnModify);
            this.pnlBottom.Controls.Add(this.btnContinue);
            this.pnlBottom.Controls.Add(this.btnClose);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 284);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(787, 62);
            this.pnlBottom.TabIndex = 2;
            //
            // lblNotice
            //
            this.lblNotice.Location = new System.Drawing.Point(12, 10);
            this.lblNotice.Name = "lblNotice";
            this.lblNotice.Size = new System.Drawing.Size(230, 14);
            this.lblNotice.TabIndex = 0;
            this.lblNotice.Text = "주민등록번호 오입력 여부를 확인하십시오.";
            //
            // btnModify
            //
            this.btnModify.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnModify.Location = new System.Drawing.Point(447, 30);
            this.btnModify.Name = "btnModify";
            this.btnModify.Size = new System.Drawing.Size(100, 26);
            this.btnModify.TabIndex = 1;
            this.btnModify.Text = "입력값 수정";
            this.btnModify.Click += new System.EventHandler(this.btnModify_Click);
            //
            // btnContinue
            //
            this.btnContinue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnContinue.Location = new System.Drawing.Point(555, 30);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(130, 26);
            this.btnContinue.TabIndex = 2;
            this.btnContinue.Text = "별도 수검자로 계속";
            this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(693, 30);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(80, 26);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "닫기";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // grpInput
            //
            // [X] GroupControl 은 캡션 높이를 ClientRectangle 에서 빼지 않는다. 절대좌표 자식은
            //     테두리부터 세므로 안쪽 y 를 캡션 높이만큼 내려 잡았다 (WF-PAT-01 과 같다).
            this.grpInput.Controls.Add(this.lblName);
            this.grpInput.Controls.Add(this.txtName);
            this.grpInput.Controls.Add(this.lblBirthday);
            this.grpInput.Controls.Add(this.txtBirthday);
            this.grpInput.Controls.Add(this.lblSocialNumber);
            this.grpInput.Controls.Add(this.txtSocialNumber);
            this.grpInput.Controls.Add(this.lblMobilePhone);
            this.grpInput.Controls.Add(this.txtMobilePhone);
            this.grpInput.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpInput.Location = new System.Drawing.Point(0, 0);
            this.grpInput.Name = "grpInput";
            this.grpInput.Size = new System.Drawing.Size(787, 62);
            this.grpInput.TabIndex = 0;
            this.grpInput.Text = "입력값";
            //
            // lblName
            //
            this.lblName.Location = new System.Drawing.Point(12, 35);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(24, 14);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "이름";
            //
            // txtName
            //
            this.txtName.Location = new System.Drawing.Point(64, 32);
            this.txtName.Name = "txtName";
            this.txtName.Properties.ReadOnly = true;
            this.txtName.Size = new System.Drawing.Size(86, 20);
            this.txtName.TabIndex = 1;
            this.txtName.TabStop = false;
            //
            // lblBirthday
            //
            this.lblBirthday.Location = new System.Drawing.Point(162, 35);
            this.lblBirthday.Name = "lblBirthday";
            this.lblBirthday.Size = new System.Drawing.Size(48, 14);
            this.lblBirthday.TabIndex = 2;
            this.lblBirthday.Text = "생년월일";
            //
            // txtBirthday
            //
            this.txtBirthday.Location = new System.Drawing.Point(214, 32);
            this.txtBirthday.Name = "txtBirthday";
            this.txtBirthday.Properties.ReadOnly = true;
            this.txtBirthday.Size = new System.Drawing.Size(96, 20);
            this.txtBirthday.TabIndex = 3;
            this.txtBirthday.TabStop = false;
            //
            // lblSocialNumber
            //
            this.lblSocialNumber.Location = new System.Drawing.Point(322, 35);
            this.lblSocialNumber.Name = "lblSocialNumber";
            this.lblSocialNumber.Size = new System.Drawing.Size(48, 14);
            this.lblSocialNumber.TabIndex = 4;
            this.lblSocialNumber.Text = "주민번호";
            //
            // txtSocialNumber
            //
            this.txtSocialNumber.Location = new System.Drawing.Point(374, 32);
            this.txtSocialNumber.Name = "txtSocialNumber";
            this.txtSocialNumber.Properties.ReadOnly = true;
            this.txtSocialNumber.Size = new System.Drawing.Size(130, 20);
            this.txtSocialNumber.TabIndex = 5;
            this.txtSocialNumber.TabStop = false;
            //
            // lblMobilePhone
            //
            this.lblMobilePhone.Location = new System.Drawing.Point(516, 35);
            this.lblMobilePhone.Name = "lblMobilePhone";
            this.lblMobilePhone.Size = new System.Drawing.Size(48, 14);
            this.lblMobilePhone.TabIndex = 6;
            this.lblMobilePhone.Text = "휴대전화";
            //
            // txtMobilePhone
            //
            this.txtMobilePhone.Location = new System.Drawing.Point(568, 32);
            this.txtMobilePhone.Name = "txtMobilePhone";
            this.txtMobilePhone.Properties.ReadOnly = true;
            this.txtMobilePhone.Size = new System.Drawing.Size(115, 20);
            this.txtMobilePhone.TabIndex = 7;
            this.txtMobilePhone.TabStop = false;
            //
            // FrmPatientDuplicate
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(787, 346);
            this.Controls.Add(this.grpCandidates);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.grpInput);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(803, 385);
            this.Name = "FrmPatientDuplicate";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "중복 후보 확인";
            ((System.ComponentModel.ISupportInitialize)(this.grpCandidates)).EndInit();
            this.grpCandidates.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gcCandidates)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvCandidates)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pnlBottom)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.pnlBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grpInput)).EndInit();
            this.grpInput.ResumeLayout(false);
            this.grpInput.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtBirthday.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.GroupControl grpCandidates;
        private DevExpress.XtraGrid.GridControl gcCandidates;
        private DevExpress.XtraGrid.Views.Grid.GridView gvCandidates;
        private DevExpress.XtraGrid.Columns.GridColumn colChartNo;
        private DevExpress.XtraGrid.Columns.GridColumn colName;
        private DevExpress.XtraGrid.Columns.GridColumn colBirthday;
        private DevExpress.XtraGrid.Columns.GridColumn colGender;
        private DevExpress.XtraGrid.Columns.GridColumn colSocialNumber;
        private DevExpress.XtraGrid.Columns.GridColumn colMobilePhone;
        private DevExpress.XtraEditors.PanelControl pnlBottom;
        private DevExpress.XtraEditors.LabelControl lblNotice;
        private DevExpress.XtraEditors.SimpleButton btnModify;
        private DevExpress.XtraEditors.SimpleButton btnContinue;
        private DevExpress.XtraEditors.SimpleButton btnClose;
        private DevExpress.XtraEditors.GroupControl grpInput;
        private DevExpress.XtraEditors.LabelControl lblName;
        private DevExpress.XtraEditors.TextEdit txtName;
        private DevExpress.XtraEditors.LabelControl lblBirthday;
        private DevExpress.XtraEditors.TextEdit txtBirthday;
        private DevExpress.XtraEditors.LabelControl lblSocialNumber;
        private DevExpress.XtraEditors.TextEdit txtSocialNumber;
        private DevExpress.XtraEditors.LabelControl lblMobilePhone;
        private DevExpress.XtraEditors.TextEdit txtMobilePhone;
    }
}
