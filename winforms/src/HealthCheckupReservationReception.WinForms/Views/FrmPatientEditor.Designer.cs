// 화면 ID: DLG-PAT-01 — 수검자 등록·정보수정 (03 §6)
namespace HealthCheckupReservationReception.Views
{
    partial class FrmPatientEditor
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
            this.lblSectionBasic = new DevExpress.XtraEditors.LabelControl();
            this.lblChartMode = new DevExpress.XtraEditors.LabelControl();
            this.rgChartMode = new DevExpress.XtraEditors.RadioGroup();
            this.lblChartNo = new DevExpress.XtraEditors.LabelControl();
            this.txtChartNo = new DevExpress.XtraEditors.TextEdit();
            this.lblName = new DevExpress.XtraEditors.LabelControl();
            this.txtName = new DevExpress.XtraEditors.TextEdit();
            this.lblSocialNumber = new DevExpress.XtraEditors.LabelControl();
            this.txtSocialNumber = new DevExpress.XtraEditors.TextEdit();
            this.lblBirthday = new DevExpress.XtraEditors.LabelControl();
            this.txtBirthday = new DevExpress.XtraEditors.TextEdit();
            this.lblGender = new DevExpress.XtraEditors.LabelControl();
            this.txtGender = new DevExpress.XtraEditors.TextEdit();
            this.lblSectionContact = new DevExpress.XtraEditors.LabelControl();
            this.lblMobilePhone = new DevExpress.XtraEditors.LabelControl();
            this.txtMobilePhone = new DevExpress.XtraEditors.TextEdit();
            this.lblPhone = new DevExpress.XtraEditors.LabelControl();
            this.txtPhone = new DevExpress.XtraEditors.TextEdit();
            this.lblEmail = new DevExpress.XtraEditors.LabelControl();
            this.txtEmail = new DevExpress.XtraEditors.TextEdit();
            this.lblSectionAddress = new DevExpress.XtraEditors.LabelControl();
            this.lblZipAddress = new DevExpress.XtraEditors.LabelControl();
            this.txtZipcode = new DevExpress.XtraEditors.TextEdit();
            this.txtAddress = new DevExpress.XtraEditors.TextEdit();
            this.lblAddressDetail = new DevExpress.XtraEditors.LabelControl();
            this.txtAddressDetail = new DevExpress.XtraEditors.TextEdit();
            this.lblSectionExclude = new DevExpress.XtraEditors.LabelControl();
            this.chkHepatitisB = new DevExpress.XtraEditors.CheckEdit();
            this.lblSectionMemo = new DevExpress.XtraEditors.LabelControl();
            this.memoMemo = new DevExpress.XtraEditors.MemoEdit();
            this.btnSave = new DevExpress.XtraEditors.SimpleButton();
            this.btnClose = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.rgChartMode.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtBirthday.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtGender.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPhone.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtEmail.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtZipcode.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtAddress.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtAddressDetail.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkHepatitisB.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.memoMemo.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // lblSectionBasic
            //
            this.lblSectionBasic.Location = new System.Drawing.Point(12, 10);
            this.lblSectionBasic.Name = "lblSectionBasic";
            this.lblSectionBasic.Size = new System.Drawing.Size(60, 14);
            this.lblSectionBasic.TabIndex = 0;
            this.lblSectionBasic.Text = "[ 기본정보 ]";
            //
            // lblChartMode
            //
            this.lblChartMode.Location = new System.Drawing.Point(12, 33);
            this.lblChartMode.Name = "lblChartMode";
            this.lblChartMode.Size = new System.Drawing.Size(70, 14);
            this.lblChartMode.TabIndex = 1;
            this.lblChartMode.Text = "차트번호 방식";
            //
            // rgChartMode
            //
            this.rgChartMode.Location = new System.Drawing.Point(137, 28);
            this.rgChartMode.Name = "rgChartMode";
            this.rgChartMode.Properties.Columns = 2;
            this.rgChartMode.Size = new System.Drawing.Size(250, 24);
            this.rgChartMode.TabIndex = 2;
            this.rgChartMode.SelectedIndexChanged += new System.EventHandler(this.rgChartMode_SelectedIndexChanged);
            //
            // lblChartNo
            //
            this.lblChartNo.Location = new System.Drawing.Point(12, 63);
            this.lblChartNo.Name = "lblChartNo";
            this.lblChartNo.Size = new System.Drawing.Size(48, 14);
            this.lblChartNo.TabIndex = 3;
            this.lblChartNo.Text = "차트번호";
            //
            // txtChartNo
            //
            this.txtChartNo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtChartNo.Location = new System.Drawing.Point(137, 60);
            this.txtChartNo.Name = "txtChartNo";
            this.txtChartNo.Properties.MaxLength = 100;
            this.txtChartNo.Properties.NullValuePrompt = "저장 시 자동발급";
            this.txtChartNo.Size = new System.Drawing.Size(440, 20);
            this.txtChartNo.TabIndex = 4;
            //
            // lblName
            //
            this.lblName.Location = new System.Drawing.Point(12, 89);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(36, 14);
            this.lblName.TabIndex = 5;
            this.lblName.Text = "이름 *";
            //
            // txtName
            //
            this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtName.Location = new System.Drawing.Point(137, 86);
            this.txtName.Name = "txtName";
            this.txtName.Properties.MaxLength = 100;
            this.txtName.Size = new System.Drawing.Size(440, 20);
            this.txtName.TabIndex = 6;
            this.txtName.EditValueChanged += new System.EventHandler(this.Input_EditValueChanged);
            //
            // lblSocialNumber
            //
            this.lblSocialNumber.Location = new System.Drawing.Point(12, 115);
            this.lblSocialNumber.Name = "lblSocialNumber";
            this.lblSocialNumber.Size = new System.Drawing.Size(84, 14);
            this.lblSocialNumber.TabIndex = 7;
            this.lblSocialNumber.Text = "주민등록번호 *";
            //
            // txtSocialNumber
            //
            this.txtSocialNumber.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSocialNumber.Location = new System.Drawing.Point(137, 112);
            this.txtSocialNumber.Name = "txtSocialNumber";
            this.txtSocialNumber.Properties.Mask.EditMask = "000000-0000000";
            this.txtSocialNumber.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Simple;
            this.txtSocialNumber.Properties.MaxLength = 14;
            this.txtSocialNumber.Size = new System.Drawing.Size(440, 20);
            this.txtSocialNumber.TabIndex = 8;
            this.txtSocialNumber.EditValueChanged += new System.EventHandler(this.Input_EditValueChanged);
            //
            // lblBirthday
            //
            this.lblBirthday.Location = new System.Drawing.Point(12, 141);
            this.lblBirthday.Name = "lblBirthday";
            this.lblBirthday.Size = new System.Drawing.Size(48, 14);
            this.lblBirthday.TabIndex = 9;
            this.lblBirthday.Text = "생년월일";
            //
            // txtBirthday
            //
            this.txtBirthday.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBirthday.Location = new System.Drawing.Point(137, 138);
            this.txtBirthday.Name = "txtBirthday";
            this.txtBirthday.Properties.ReadOnly = true;
            this.txtBirthday.Size = new System.Drawing.Size(440, 20);
            this.txtBirthday.TabIndex = 10;
            this.txtBirthday.TabStop = false;
            //
            // lblGender
            //
            this.lblGender.Location = new System.Drawing.Point(12, 167);
            this.lblGender.Name = "lblGender";
            this.lblGender.Size = new System.Drawing.Size(24, 14);
            this.lblGender.TabIndex = 11;
            this.lblGender.Text = "성별";
            //
            // txtGender
            //
            this.txtGender.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtGender.Location = new System.Drawing.Point(137, 164);
            this.txtGender.Name = "txtGender";
            this.txtGender.Properties.ReadOnly = true;
            this.txtGender.Size = new System.Drawing.Size(440, 20);
            this.txtGender.TabIndex = 12;
            this.txtGender.TabStop = false;
            //
            // lblSectionContact
            //
            this.lblSectionContact.Location = new System.Drawing.Point(12, 195);
            this.lblSectionContact.Name = "lblSectionContact";
            this.lblSectionContact.Size = new System.Drawing.Size(48, 14);
            this.lblSectionContact.TabIndex = 13;
            this.lblSectionContact.Text = "[ 연락처 ]";
            //
            // lblMobilePhone
            //
            this.lblMobilePhone.Location = new System.Drawing.Point(12, 218);
            this.lblMobilePhone.Name = "lblMobilePhone";
            this.lblMobilePhone.Size = new System.Drawing.Size(48, 14);
            this.lblMobilePhone.TabIndex = 14;
            this.lblMobilePhone.Text = "휴대전화";
            //
            // txtMobilePhone
            //
            this.txtMobilePhone.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMobilePhone.Location = new System.Drawing.Point(137, 215);
            this.txtMobilePhone.Name = "txtMobilePhone";
            this.txtMobilePhone.Properties.MaxLength = 13;
            this.txtMobilePhone.Size = new System.Drawing.Size(440, 20);
            this.txtMobilePhone.TabIndex = 15;
            //
            // lblPhone
            //
            this.lblPhone.Location = new System.Drawing.Point(12, 244);
            this.lblPhone.Name = "lblPhone";
            this.lblPhone.Size = new System.Drawing.Size(48, 14);
            this.lblPhone.TabIndex = 16;
            this.lblPhone.Text = "전화번호";
            //
            // txtPhone
            //
            this.txtPhone.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPhone.Location = new System.Drawing.Point(137, 241);
            this.txtPhone.Name = "txtPhone";
            this.txtPhone.Properties.MaxLength = 13;
            this.txtPhone.Size = new System.Drawing.Size(440, 20);
            this.txtPhone.TabIndex = 17;
            //
            // lblEmail
            //
            this.lblEmail.Location = new System.Drawing.Point(12, 270);
            this.lblEmail.Name = "lblEmail";
            this.lblEmail.Size = new System.Drawing.Size(36, 14);
            this.lblEmail.TabIndex = 18;
            this.lblEmail.Text = "E-mail";
            //
            // txtEmail
            //
            this.txtEmail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtEmail.Location = new System.Drawing.Point(137, 267);
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.Properties.MaxLength = 200;
            this.txtEmail.Size = new System.Drawing.Size(440, 20);
            this.txtEmail.TabIndex = 19;
            //
            // lblSectionAddress
            //
            this.lblSectionAddress.Location = new System.Drawing.Point(12, 298);
            this.lblSectionAddress.Name = "lblSectionAddress";
            this.lblSectionAddress.Size = new System.Drawing.Size(36, 14);
            this.lblSectionAddress.TabIndex = 20;
            this.lblSectionAddress.Text = "[ 주소 ]";
            //
            // lblZipAddress
            //
            this.lblZipAddress.Location = new System.Drawing.Point(12, 321);
            this.lblZipAddress.Name = "lblZipAddress";
            this.lblZipAddress.Size = new System.Drawing.Size(88, 14);
            this.lblZipAddress.TabIndex = 21;
            this.lblZipAddress.Text = "우편번호 / 주소";
            //
            // txtZipcode
            //
            this.txtZipcode.Location = new System.Drawing.Point(137, 318);
            this.txtZipcode.Name = "txtZipcode";
            this.txtZipcode.Properties.MaxLength = 10;
            this.txtZipcode.Size = new System.Drawing.Size(110, 20);
            this.txtZipcode.TabIndex = 22;
            //
            // txtAddress
            //
            this.txtAddress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAddress.Location = new System.Drawing.Point(253, 318);
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.Properties.MaxLength = 200;
            this.txtAddress.Size = new System.Drawing.Size(324, 20);
            this.txtAddress.TabIndex = 23;
            //
            // lblAddressDetail
            //
            this.lblAddressDetail.Location = new System.Drawing.Point(12, 347);
            this.lblAddressDetail.Name = "lblAddressDetail";
            this.lblAddressDetail.Size = new System.Drawing.Size(48, 14);
            this.lblAddressDetail.TabIndex = 24;
            this.lblAddressDetail.Text = "상세주소";
            //
            // txtAddressDetail
            //
            this.txtAddressDetail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAddressDetail.Location = new System.Drawing.Point(137, 344);
            this.txtAddressDetail.Name = "txtAddressDetail";
            this.txtAddressDetail.Properties.MaxLength = 200;
            this.txtAddressDetail.Size = new System.Drawing.Size(440, 20);
            this.txtAddressDetail.TabIndex = 25;
            //
            // lblSectionExclude
            //
            this.lblSectionExclude.Location = new System.Drawing.Point(12, 375);
            this.lblSectionExclude.Name = "lblSectionExclude";
            this.lblSectionExclude.Size = new System.Drawing.Size(60, 14);
            this.lblSectionExclude.TabIndex = 26;
            this.lblSectionExclude.Text = "[ 검사 제외 ]";
            //
            // chkHepatitisB
            //
            this.chkHepatitisB.Location = new System.Drawing.Point(137, 372);
            this.chkHepatitisB.Name = "chkHepatitisB";
            this.chkHepatitisB.Properties.Caption = "B형간염 검사 제외";
            this.chkHepatitisB.Size = new System.Drawing.Size(200, 20);
            this.chkHepatitisB.TabIndex = 27;
            //
            // lblSectionMemo
            //
            this.lblSectionMemo.Location = new System.Drawing.Point(12, 400);
            this.lblSectionMemo.Name = "lblSectionMemo";
            this.lblSectionMemo.Size = new System.Drawing.Size(36, 14);
            this.lblSectionMemo.TabIndex = 28;
            this.lblSectionMemo.Text = "[ 메모 ]";
            //
            // memoMemo
            //
            this.memoMemo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.memoMemo.Location = new System.Drawing.Point(12, 418);
            this.memoMemo.Name = "memoMemo";
            this.memoMemo.Size = new System.Drawing.Size(565, 78);
            this.memoMemo.TabIndex = 29;
            //
            // btnSave
            //
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(413, 508);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(80, 26);
            this.btnSave.TabIndex = 30;
            this.btnSave.Text = "저장";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(497, 508);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(80, 26);
            this.btnClose.TabIndex = 31;
            this.btnClose.Text = "닫기";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // FrmPatientEditor
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(590, 544);
            this.Controls.Add(this.lblSectionBasic);
            this.Controls.Add(this.lblChartMode);
            this.Controls.Add(this.rgChartMode);
            this.Controls.Add(this.lblChartNo);
            this.Controls.Add(this.txtChartNo);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblSocialNumber);
            this.Controls.Add(this.txtSocialNumber);
            this.Controls.Add(this.lblBirthday);
            this.Controls.Add(this.txtBirthday);
            this.Controls.Add(this.lblGender);
            this.Controls.Add(this.txtGender);
            this.Controls.Add(this.lblSectionContact);
            this.Controls.Add(this.lblMobilePhone);
            this.Controls.Add(this.txtMobilePhone);
            this.Controls.Add(this.lblPhone);
            this.Controls.Add(this.txtPhone);
            this.Controls.Add(this.lblEmail);
            this.Controls.Add(this.txtEmail);
            this.Controls.Add(this.lblSectionAddress);
            this.Controls.Add(this.lblZipAddress);
            this.Controls.Add(this.txtZipcode);
            this.Controls.Add(this.txtAddress);
            this.Controls.Add(this.lblAddressDetail);
            this.Controls.Add(this.txtAddressDetail);
            this.Controls.Add(this.lblSectionExclude);
            this.Controls.Add(this.chkHepatitisB);
            this.Controls.Add(this.lblSectionMemo);
            this.Controls.Add(this.memoMemo);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnClose);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(606, 583);
            this.Name = "FrmPatientEditor";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "수검자 신규등록 / 정보수정";
            ((System.ComponentModel.ISupportInitialize)(this.rgChartMode.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtChartNo.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtBirthday.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtGender.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPhone.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtEmail.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtZipcode.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtAddress.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtAddressDetail.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkHepatitisB.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.memoMemo.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl lblSectionBasic;
        private DevExpress.XtraEditors.LabelControl lblChartMode;
        private DevExpress.XtraEditors.RadioGroup rgChartMode;
        private DevExpress.XtraEditors.LabelControl lblChartNo;
        private DevExpress.XtraEditors.TextEdit txtChartNo;
        private DevExpress.XtraEditors.LabelControl lblName;
        private DevExpress.XtraEditors.TextEdit txtName;
        private DevExpress.XtraEditors.LabelControl lblSocialNumber;
        private DevExpress.XtraEditors.TextEdit txtSocialNumber;
        private DevExpress.XtraEditors.LabelControl lblBirthday;
        private DevExpress.XtraEditors.TextEdit txtBirthday;
        private DevExpress.XtraEditors.LabelControl lblGender;
        private DevExpress.XtraEditors.TextEdit txtGender;
        private DevExpress.XtraEditors.LabelControl lblSectionContact;
        private DevExpress.XtraEditors.LabelControl lblMobilePhone;
        private DevExpress.XtraEditors.TextEdit txtMobilePhone;
        private DevExpress.XtraEditors.LabelControl lblPhone;
        private DevExpress.XtraEditors.TextEdit txtPhone;
        private DevExpress.XtraEditors.LabelControl lblEmail;
        private DevExpress.XtraEditors.TextEdit txtEmail;
        private DevExpress.XtraEditors.LabelControl lblSectionAddress;
        private DevExpress.XtraEditors.LabelControl lblZipAddress;
        private DevExpress.XtraEditors.TextEdit txtZipcode;
        private DevExpress.XtraEditors.TextEdit txtAddress;
        private DevExpress.XtraEditors.LabelControl lblAddressDetail;
        private DevExpress.XtraEditors.TextEdit txtAddressDetail;
        private DevExpress.XtraEditors.LabelControl lblSectionExclude;
        private DevExpress.XtraEditors.CheckEdit chkHepatitisB;
        private DevExpress.XtraEditors.LabelControl lblSectionMemo;
        private DevExpress.XtraEditors.MemoEdit memoMemo;
        private DevExpress.XtraEditors.SimpleButton btnSave;
        private DevExpress.XtraEditors.SimpleButton btnClose;
    }
}
