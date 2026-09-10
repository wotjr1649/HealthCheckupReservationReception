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
            this.lcMain = new DevExpress.XtraLayout.LayoutControl();
            this.txtName = new DevExpress.XtraEditors.TextEdit();
            this.txtBirthday = new DevExpress.XtraEditors.TextEdit();
            this.txtSocialNumber = new DevExpress.XtraEditors.TextEdit();
            this.txtMobilePhone = new DevExpress.XtraEditors.TextEdit();
            this.gcCandidates = new DevExpress.XtraGrid.GridControl();
            this.gvCandidates = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colChartNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colBirthday = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colGender = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colSocialNumber = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMobilePhone = new DevExpress.XtraGrid.Columns.GridColumn();
            this.lblNotice = new DevExpress.XtraEditors.LabelControl();
            this.btnModify = new DevExpress.XtraEditors.SimpleButton();
            this.btnContinue = new DevExpress.XtraEditors.SimpleButton();
            this.btnClose = new DevExpress.XtraEditors.SimpleButton();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lcgInput = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciName = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciBirthday = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciSocialNumber = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciMobilePhone = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceInput = new DevExpress.XtraLayout.EmptySpaceItem();
            this.lcgCandidates = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciCandidates = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgActions = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciNotice = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceActions = new DevExpress.XtraLayout.EmptySpaceItem();
            this.lciModify = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciContinue = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciClose = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).BeginInit();
            this.lcMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtBirthday.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcCandidates)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvCandidates)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgInput)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciBirthday)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSocialNumber)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciMobilePhone)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceInput)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgCandidates)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciCandidates)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgActions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciNotice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceActions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciModify)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciContinue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciClose)).BeginInit();
            this.SuspendLayout();
            //
            // lcMain
            //
            this.lcMain.Controls.Add(this.txtName);
            this.lcMain.Controls.Add(this.txtBirthday);
            this.lcMain.Controls.Add(this.txtSocialNumber);
            this.lcMain.Controls.Add(this.txtMobilePhone);
            this.lcMain.Controls.Add(this.gcCandidates);
            this.lcMain.Controls.Add(this.lblNotice);
            this.lcMain.Controls.Add(this.btnModify);
            this.lcMain.Controls.Add(this.btnContinue);
            this.lcMain.Controls.Add(this.btnClose);
            this.lcMain.AllowCustomization = false;
            this.lcMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lcMain.Location = new System.Drawing.Point(0, 0);
            this.lcMain.Name = "lcMain";
            this.lcMain.Root = this.Root;
            this.lcMain.Size = new System.Drawing.Size(800, 400);
            this.lcMain.TabIndex = 0;
            //
            // txtName
            //
            // 입력값 넷은 방금 사용자가 친 값을 되비추는 자리다 — 여기서 고치지 않는다 (03 §6.5).
            this.txtName.Location = new System.Drawing.Point(84, 43);
            this.txtName.Name = "txtName";
            this.txtName.Properties.ReadOnly = true;
            this.txtName.Size = new System.Drawing.Size(88, 20);
            this.txtName.StyleController = this.lcMain;
            this.txtName.TabIndex = 0;
            this.txtName.TabStop = false;
            //
            // txtBirthday
            //
            this.txtBirthday.Location = new System.Drawing.Point(244, 43);
            this.txtBirthday.Name = "txtBirthday";
            this.txtBirthday.Properties.ReadOnly = true;
            this.txtBirthday.Size = new System.Drawing.Size(108, 20);
            this.txtBirthday.StyleController = this.lcMain;
            this.txtBirthday.TabIndex = 1;
            this.txtBirthday.TabStop = false;
            //
            // txtSocialNumber
            //
            this.txtSocialNumber.Location = new System.Drawing.Point(424, 43);
            this.txtSocialNumber.Name = "txtSocialNumber";
            this.txtSocialNumber.Properties.ReadOnly = true;
            this.txtSocialNumber.Size = new System.Drawing.Size(143, 20);
            this.txtSocialNumber.StyleController = this.lcMain;
            this.txtSocialNumber.TabIndex = 2;
            this.txtSocialNumber.TabStop = false;
            //
            // txtMobilePhone
            //
            this.txtMobilePhone.Location = new System.Drawing.Point(639, 43);
            this.txtMobilePhone.Name = "txtMobilePhone";
            this.txtMobilePhone.Properties.ReadOnly = true;
            this.txtMobilePhone.Size = new System.Drawing.Size(123, 20);
            this.txtMobilePhone.StyleController = this.lcMain;
            this.txtMobilePhone.TabIndex = 3;
            this.txtMobilePhone.TabStop = false;
            //
            // gcCandidates
            //
            this.gcCandidates.Location = new System.Drawing.Point(24, 93);
            this.gcCandidates.MainView = this.gvCandidates;
            this.gcCandidates.Name = "gcCandidates";
            this.gcCandidates.Size = new System.Drawing.Size(752, 227);
            this.gcCandidates.TabIndex = 4;
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
            this.gvCandidates.OptionsBehavior.AutoPopulateColumns = false;
            // 03 §6.5 — 고르는 자리가 아니라 확인하는 자리다.
            this.gvCandidates.OptionsBehavior.Editable = false;
            this.gvCandidates.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.gvCandidates.OptionsSelection.EnableAppearanceFocusedRow = false;
            this.gvCandidates.OptionsView.ShowGroupPanel = false;
            this.gvCandidates.OptionsView.ShowIndicator = false;
            this.gvCandidates.CustomColumnDisplayText += new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(this.gvCandidates_CustomColumnDisplayText);
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
            // colBirthday
            //
            this.colBirthday.Caption = "생년월일";
            this.colBirthday.FieldName = "Birthday";
            this.colBirthday.Name = "colBirthday";
            this.colBirthday.MinWidth = 90;
            this.colBirthday.Visible = true;
            this.colBirthday.VisibleIndex = 2;
            this.colBirthday.Width = 110;
            //
            // colGender
            //
            this.colGender.Caption = "성별";
            this.colGender.FieldName = "Gender";
            this.colGender.Name = "colGender";
            this.colGender.MinWidth = 50;
            this.colGender.Visible = true;
            this.colGender.VisibleIndex = 3;
            this.colGender.Width = 60;
            //
            // colSocialNumber
            //
            // 03 §6.5 — 이 화면의 존재 이유가 `이름+생년월일 동일 / 주민번호 상이` 라서
            // 주민번호는 전체값이다. 마스킹하지 않는다.
            this.colSocialNumber.Caption = "주민등록번호";
            this.colSocialNumber.FieldName = "SocialNumber";
            this.colSocialNumber.Name = "colSocialNumber";
            this.colSocialNumber.MinWidth = 130;
            this.colSocialNumber.Visible = true;
            this.colSocialNumber.VisibleIndex = 4;
            this.colSocialNumber.Width = 150;
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
            // lblNotice
            //
            this.lblNotice.Location = new System.Drawing.Point(24, 349);
            this.lblNotice.Name = "lblNotice";
            this.lblNotice.Size = new System.Drawing.Size(230, 14);
            this.lblNotice.StyleController = this.lcMain;
            this.lblNotice.TabIndex = 5;
            this.lblNotice.Text = "주민등록번호 오입력 여부를 확인하십시오.";
            //
            // btnModify
            //
            this.btnModify.Location = new System.Drawing.Point(470, 345);
            this.btnModify.Name = "btnModify";
            this.btnModify.Size = new System.Drawing.Size(100, 22);
            this.btnModify.StyleController = this.lcMain;
            this.btnModify.TabIndex = 6;
            this.btnModify.Text = "입력값 수정";
            this.btnModify.Click += new System.EventHandler(this.btnModify_Click);
            //
            // btnContinue
            //
            this.btnContinue.Location = new System.Drawing.Point(578, 345);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(130, 22);
            this.btnContinue.StyleController = this.lcMain;
            this.btnContinue.TabIndex = 7;
            this.btnContinue.Text = "별도 수검자로 계속";
            this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);
            //
            // btnClose
            //
            this.btnClose.Location = new System.Drawing.Point(716, 345);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(60, 22);
            this.btnClose.StyleController = this.lcMain;
            this.btnClose.TabIndex = 8;
            this.btnClose.Text = "닫기";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // Root
            //
            this.Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lcgInput,
            this.lcgCandidates,
            this.lcgActions});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(800, 400);
            this.Root.TextVisible = false;
            //
            // lcgInput
            //
            this.lcgInput.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciName,
            this.lciBirthday,
            this.lciSocialNumber,
            this.lciMobilePhone,
            this.emptySpaceInput});
            this.lcgInput.Location = new System.Drawing.Point(0, 0);
            this.lcgInput.Name = "lcgInput";
            this.lcgInput.Size = new System.Drawing.Size(800, 50);
            this.lcgInput.Text = "입력값";
            //
            // lciName
            //
            this.lciName.Control = this.txtName;
            this.lciName.Location = new System.Drawing.Point(0, 0);
            this.lciName.MaxSize = new System.Drawing.Size(140, 24);
            this.lciName.MinSize = new System.Drawing.Size(140, 24);
            this.lciName.Name = "lciName";
            this.lciName.Size = new System.Drawing.Size(140, 24);
            this.lciName.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciName.Text = "이름";
            this.lciName.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            //
            // lciBirthday
            //
            this.lciBirthday.Control = this.txtBirthday;
            this.lciBirthday.Location = new System.Drawing.Point(140, 0);
            this.lciBirthday.MaxSize = new System.Drawing.Size(160, 24);
            this.lciBirthday.MinSize = new System.Drawing.Size(160, 24);
            this.lciBirthday.Name = "lciBirthday";
            this.lciBirthday.Size = new System.Drawing.Size(160, 24);
            this.lciBirthday.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciBirthday.Text = "생년월일";
            this.lciBirthday.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            //
            // lciSocialNumber
            //
            this.lciSocialNumber.Control = this.txtSocialNumber;
            this.lciSocialNumber.Location = new System.Drawing.Point(300, 0);
            this.lciSocialNumber.MaxSize = new System.Drawing.Size(210, 24);
            this.lciSocialNumber.MinSize = new System.Drawing.Size(210, 24);
            this.lciSocialNumber.Name = "lciSocialNumber";
            this.lciSocialNumber.Size = new System.Drawing.Size(210, 24);
            this.lciSocialNumber.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciSocialNumber.Text = "주민등록번호";
            this.lciSocialNumber.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            //
            // lciMobilePhone
            //
            this.lciMobilePhone.Control = this.txtMobilePhone;
            this.lciMobilePhone.Location = new System.Drawing.Point(510, 0);
            this.lciMobilePhone.MaxSize = new System.Drawing.Size(175, 24);
            this.lciMobilePhone.MinSize = new System.Drawing.Size(175, 24);
            this.lciMobilePhone.Name = "lciMobilePhone";
            this.lciMobilePhone.Size = new System.Drawing.Size(175, 24);
            this.lciMobilePhone.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciMobilePhone.Text = "휴대전화";
            this.lciMobilePhone.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            //
            // emptySpaceInput
            //
            this.emptySpaceInput.AllowHotTrack = false;
            this.emptySpaceInput.Location = new System.Drawing.Point(685, 0);
            this.emptySpaceInput.Name = "emptySpaceInput";
            this.emptySpaceInput.Size = new System.Drawing.Size(115, 24);
            this.emptySpaceInput.TextSize = new System.Drawing.Size(0, 0);
            //
            // lcgCandidates
            //
            this.lcgCandidates.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciCandidates});
            this.lcgCandidates.Location = new System.Drawing.Point(0, 50);
            this.lcgCandidates.Name = "lcgCandidates";
            this.lcgCandidates.Size = new System.Drawing.Size(800, 270);
            this.lcgCandidates.Text = "중복 후보 · 이름 + 생년월일 동일 / 주민등록번호 상이";
            //
            // lciCandidates
            //
            this.lciCandidates.Control = this.gcCandidates;
            this.lciCandidates.Location = new System.Drawing.Point(0, 0);
            this.lciCandidates.Name = "lciCandidates";
            this.lciCandidates.Size = new System.Drawing.Size(800, 270);
            this.lciCandidates.TextSize = new System.Drawing.Size(0, 0);
            this.lciCandidates.TextVisible = false;
            //
            // lcgActions
            //
            this.lcgActions.GroupBordersVisible = false;
            this.lcgActions.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciNotice,
            this.emptySpaceActions,
            this.lciModify,
            this.lciContinue,
            this.lciClose});
            this.lcgActions.Location = new System.Drawing.Point(0, 320);
            this.lcgActions.Name = "lcgActions";
            this.lcgActions.Size = new System.Drawing.Size(800, 80);
            this.lcgActions.TextVisible = false;
            //
            // lciNotice
            //
            this.lciNotice.Control = this.lblNotice;
            this.lciNotice.Location = new System.Drawing.Point(0, 0);
            this.lciNotice.MaxSize = new System.Drawing.Size(400, 30);
            this.lciNotice.MinSize = new System.Drawing.Size(104, 30);
            this.lciNotice.Name = "lciNotice";
            this.lciNotice.Size = new System.Drawing.Size(400, 80);
            this.lciNotice.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciNotice.TextSize = new System.Drawing.Size(0, 0);
            this.lciNotice.TextVisible = false;
            //
            // emptySpaceActions
            //
            this.emptySpaceActions.AllowHotTrack = false;
            this.emptySpaceActions.Location = new System.Drawing.Point(400, 0);
            this.emptySpaceActions.Name = "emptySpaceActions";
            this.emptySpaceActions.Size = new System.Drawing.Size(58, 80);
            this.emptySpaceActions.TextSize = new System.Drawing.Size(0, 0);
            //
            // lciModify
            //
            this.lciModify.Control = this.btnModify;
            this.lciModify.Location = new System.Drawing.Point(458, 0);
            this.lciModify.MaxSize = new System.Drawing.Size(108, 30);
            this.lciModify.MinSize = new System.Drawing.Size(108, 30);
            this.lciModify.Name = "lciModify";
            this.lciModify.Size = new System.Drawing.Size(108, 80);
            this.lciModify.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciModify.TextVisible = false;
            //
            // lciContinue
            //
            this.lciContinue.Control = this.btnContinue;
            this.lciContinue.Location = new System.Drawing.Point(566, 0);
            this.lciContinue.MaxSize = new System.Drawing.Size(138, 30);
            this.lciContinue.MinSize = new System.Drawing.Size(138, 30);
            this.lciContinue.Name = "lciContinue";
            this.lciContinue.Size = new System.Drawing.Size(138, 80);
            this.lciContinue.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciContinue.TextVisible = false;
            //
            // lciClose
            //
            this.lciClose.Control = this.btnClose;
            this.lciClose.Location = new System.Drawing.Point(704, 0);
            this.lciClose.MaxSize = new System.Drawing.Size(96, 30);
            this.lciClose.MinSize = new System.Drawing.Size(96, 30);
            this.lciClose.Name = "lciClose";
            this.lciClose.Size = new System.Drawing.Size(96, 80);
            this.lciClose.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciClose.TextVisible = false;
            //
            // FrmPatientDuplicate
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 400);
            this.Controls.Add(this.lcMain);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(816, 439);
            this.Name = "FrmPatientDuplicate";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "중복 후보 확인";
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).EndInit();
            this.lcMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.txtName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtBirthday.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSocialNumber.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMobilePhone.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcCandidates)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvCandidates)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgInput)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciBirthday)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciSocialNumber)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciMobilePhone)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceInput)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgCandidates)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciCandidates)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciNotice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciModify)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciContinue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciClose)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl lcMain;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraLayout.LayoutControlGroup lcgInput;
        private DevExpress.XtraEditors.TextEdit txtName;
        private DevExpress.XtraEditors.TextEdit txtBirthday;
        private DevExpress.XtraEditors.TextEdit txtSocialNumber;
        private DevExpress.XtraEditors.TextEdit txtMobilePhone;
        private DevExpress.XtraLayout.LayoutControlItem lciName;
        private DevExpress.XtraLayout.LayoutControlItem lciBirthday;
        private DevExpress.XtraLayout.LayoutControlItem lciSocialNumber;
        private DevExpress.XtraLayout.LayoutControlItem lciMobilePhone;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceInput;
        private DevExpress.XtraLayout.LayoutControlGroup lcgCandidates;
        private DevExpress.XtraGrid.GridControl gcCandidates;
        private DevExpress.XtraGrid.Views.Grid.GridView gvCandidates;
        private DevExpress.XtraGrid.Columns.GridColumn colChartNo;
        private DevExpress.XtraGrid.Columns.GridColumn colName;
        private DevExpress.XtraGrid.Columns.GridColumn colBirthday;
        private DevExpress.XtraGrid.Columns.GridColumn colGender;
        private DevExpress.XtraGrid.Columns.GridColumn colSocialNumber;
        private DevExpress.XtraGrid.Columns.GridColumn colMobilePhone;
        private DevExpress.XtraLayout.LayoutControlItem lciCandidates;
        private DevExpress.XtraLayout.LayoutControlGroup lcgActions;
        private DevExpress.XtraEditors.LabelControl lblNotice;
        private DevExpress.XtraEditors.SimpleButton btnModify;
        private DevExpress.XtraEditors.SimpleButton btnContinue;
        private DevExpress.XtraEditors.SimpleButton btnClose;
        private DevExpress.XtraLayout.LayoutControlItem lciNotice;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceActions;
        private DevExpress.XtraLayout.LayoutControlItem lciModify;
        private DevExpress.XtraLayout.LayoutControlItem lciContinue;
        private DevExpress.XtraLayout.LayoutControlItem lciClose;
    }
}
