// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
namespace HealthCheckupReservationReception.Views
{
    partial class FrmChangeLog
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
            this.lblValidation = new DevExpress.XtraEditors.LabelControl();
            this.gcLog = new DevExpress.XtraGrid.GridControl();
            this.gvLog = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colRecordedAt = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colOperatorName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colColumnName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colBeforeValue = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colAfterValue = new DevExpress.XtraGrid.Columns.GridColumn();
            this.lblNotice = new DevExpress.XtraEditors.LabelControl();
            this.btnClose = new DevExpress.XtraEditors.SimpleButton();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciValidation = new DevExpress.XtraLayout.LayoutControlItem();
            this.lciLog = new DevExpress.XtraLayout.LayoutControlItem();
            this.lcgActions = new DevExpress.XtraLayout.LayoutControlGroup();
            this.lciNotice = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceActions = new DevExpress.XtraLayout.EmptySpaceItem();
            this.lciClose = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).BeginInit();
            this.lcMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gcLog)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvLog)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciValidation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciLog)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgActions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciNotice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceActions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciClose)).BeginInit();
            this.SuspendLayout();
            //
            // lcMain
            //
            this.lcMain.Controls.Add(this.lblValidation);
            this.lcMain.Controls.Add(this.gcLog);
            this.lcMain.Controls.Add(this.lblNotice);
            this.lcMain.Controls.Add(this.btnClose);
            this.lcMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lcMain.Location = new System.Drawing.Point(0, 0);
            this.lcMain.Name = "lcMain";
            this.lcMain.Root = this.Root;
            this.lcMain.Size = new System.Drawing.Size(900, 480);
            this.lcMain.TabIndex = 0;
            //
            // lblValidation
            //
            this.lblValidation.Location = new System.Drawing.Point(12, 12);
            this.lblValidation.Name = "lblValidation";
            this.lblValidation.Size = new System.Drawing.Size(0, 14);
            this.lblValidation.StyleController = this.lcMain;
            this.lblValidation.TabIndex = 0;
            //
            // gcLog
            //
            this.gcLog.Location = new System.Drawing.Point(12, 32);
            this.gcLog.MainView = this.gvLog;
            this.gcLog.Name = "gcLog";
            this.gcLog.Size = new System.Drawing.Size(876, 400);
            this.gcLog.TabIndex = 1;
            this.gcLog.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvLog});
            //
            // gvLog
            //
            // 03 §23.4 — 읽기 전용이고 정렬은 기록일시 최신순 고정이다. 사용자 정렬을 주지
            // 않는다: SP 가 이미 `기록일시 DESC, 이력ID DESC` 로 낸다 (05 §8.3).
            this.gvLog.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colRecordedAt,
            this.colOperatorName,
            this.colColumnName,
            this.colBeforeValue,
            this.colAfterValue});
            this.gvLog.GridControl = this.gcLog;
            this.gvLog.Name = "gvLog";
            this.gvLog.OptionsBehavior.AutoPopulateColumns = false;
            this.gvLog.OptionsBehavior.Editable = false;
            this.gvLog.OptionsCustomization.AllowSort = false;
            this.gvLog.OptionsSelection.MultiSelect = false;
            this.gvLog.OptionsView.ShowGroupPanel = false;
            this.gvLog.OptionsView.ShowIndicator = false;
            //
            // colRecordedAt
            //
            this.colRecordedAt.Caption = "기록일시";
            this.colRecordedAt.FieldName = "RecordedAt";
            this.colRecordedAt.Name = "colRecordedAt";
            this.colRecordedAt.MinWidth = 140;
            this.colRecordedAt.Visible = true;
            this.colRecordedAt.VisibleIndex = 0;
            this.colRecordedAt.Width = 140;
            //
            // colOperatorName
            //
            // 03 §23.4 — `조작자` 는 인증된 사용자가 아니라 **자기신고 값**이다 (04 §14.2 L4).
            // 그 사실을 컬럼 머리에 적는다.
            this.colOperatorName.Caption = "조작자(자기신고)";
            this.colOperatorName.FieldName = "OperatorName";
            this.colOperatorName.Name = "colOperatorName";
            this.colOperatorName.MinWidth = 130;
            this.colOperatorName.Visible = true;
            this.colOperatorName.VisibleIndex = 1;
            this.colOperatorName.Width = 130;
            //
            // colColumnName
            //
            this.colColumnName.Caption = "항목";
            this.colColumnName.FieldName = "ColumnName";
            this.colColumnName.Name = "colColumnName";
            this.colColumnName.MinWidth = 110;
            this.colColumnName.Visible = true;
            this.colColumnName.VisibleIndex = 2;
            this.colColumnName.Width = 110;
            //
            // colBeforeValue
            //
            this.colBeforeValue.Caption = "변경전";
            this.colBeforeValue.FieldName = "BeforeValue";
            this.colBeforeValue.Name = "colBeforeValue";
            this.colBeforeValue.MinWidth = 180;
            this.colBeforeValue.Visible = true;
            this.colBeforeValue.VisibleIndex = 3;
            this.colBeforeValue.Width = 240;
            //
            // colAfterValue
            //
            this.colAfterValue.Caption = "변경후";
            this.colAfterValue.FieldName = "AfterValue";
            this.colAfterValue.Name = "colAfterValue";
            this.colAfterValue.MinWidth = 180;
            this.colAfterValue.Visible = true;
            this.colAfterValue.VisibleIndex = 4;
            this.colAfterValue.Width = 240;
            //
            // lblNotice
            //
            this.lblNotice.Location = new System.Drawing.Point(12, 444);
            this.lblNotice.Name = "lblNotice";
            this.lblNotice.Size = new System.Drawing.Size(0, 14);
            this.lblNotice.StyleController = this.lcMain;
            this.lblNotice.TabIndex = 2;
            //
            // btnClose
            //
            this.btnClose.Location = new System.Drawing.Point(792, 440);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(96, 26);
            this.btnClose.StyleController = this.lcMain;
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "닫기";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // Root
            //
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciValidation,
            this.lciLog,
            this.lcgActions});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(900, 480);
            this.Root.TextVisible = false;
            //
            // lciValidation
            //
            this.lciValidation.Control = this.lblValidation;
            this.lciValidation.Location = new System.Drawing.Point(0, 0);
            this.lciValidation.MaxSize = new System.Drawing.Size(0, 20);
            this.lciValidation.MinSize = new System.Drawing.Size(104, 20);
            this.lciValidation.Name = "lciValidation";
            this.lciValidation.Size = new System.Drawing.Size(880, 20);
            this.lciValidation.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciValidation.TextVisible = false;
            //
            // lciLog
            //
            this.lciLog.Control = this.gcLog;
            this.lciLog.Location = new System.Drawing.Point(0, 20);
            this.lciLog.Name = "lciLog";
            this.lciLog.Size = new System.Drawing.Size(880, 404);
            this.lciLog.TextVisible = false;
            //
            // lcgActions
            //
            this.lcgActions.GroupBordersVisible = false;
            this.lcgActions.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.lciNotice,
            this.emptySpaceActions,
            this.lciClose});
            this.lcgActions.Location = new System.Drawing.Point(0, 424);
            this.lcgActions.Name = "lcgActions";
            this.lcgActions.Size = new System.Drawing.Size(880, 36);
            this.lcgActions.TextVisible = false;
            //
            // lciNotice
            //
            this.lciNotice.Control = this.lblNotice;
            this.lciNotice.Location = new System.Drawing.Point(0, 0);
            this.lciNotice.MaxSize = new System.Drawing.Size(0, 26);
            this.lciNotice.MinSize = new System.Drawing.Size(104, 26);
            this.lciNotice.Name = "lciNotice";
            this.lciNotice.Size = new System.Drawing.Size(600, 36);
            this.lciNotice.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciNotice.TextVisible = false;
            //
            // emptySpaceActions
            //
            this.emptySpaceActions.AllowHotTrack = false;
            this.emptySpaceActions.Location = new System.Drawing.Point(600, 0);
            this.emptySpaceActions.Name = "emptySpaceActions";
            this.emptySpaceActions.Size = new System.Drawing.Size(184, 36);
            this.emptySpaceActions.TextSize = new System.Drawing.Size(0, 0);
            //
            // lciClose
            //
            this.lciClose.Control = this.btnClose;
            this.lciClose.Location = new System.Drawing.Point(784, 0);
            this.lciClose.MaxSize = new System.Drawing.Size(96, 36);
            this.lciClose.MinSize = new System.Drawing.Size(96, 36);
            this.lciClose.Name = "lciClose";
            this.lciClose.Size = new System.Drawing.Size(96, 36);
            this.lciClose.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.lciClose.TextVisible = false;
            //
            // FrmChangeLog
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 480);
            this.Controls.Add(this.lcMain);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(720, 400);
            this.Name = "FrmChangeLog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "변경이력";
            ((System.ComponentModel.ISupportInitialize)(this.lciClose)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciNotice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcgActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciLog)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lciValidation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvLog)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcLog)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lcMain)).EndInit();
            this.lcMain.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl lcMain;
        private DevExpress.XtraEditors.LabelControl lblValidation;
        private DevExpress.XtraGrid.GridControl gcLog;
        private DevExpress.XtraGrid.Views.Grid.GridView gvLog;
        private DevExpress.XtraGrid.Columns.GridColumn colRecordedAt;
        private DevExpress.XtraGrid.Columns.GridColumn colOperatorName;
        private DevExpress.XtraGrid.Columns.GridColumn colColumnName;
        private DevExpress.XtraGrid.Columns.GridColumn colBeforeValue;
        private DevExpress.XtraGrid.Columns.GridColumn colAfterValue;
        private DevExpress.XtraEditors.LabelControl lblNotice;
        private DevExpress.XtraEditors.SimpleButton btnClose;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraLayout.LayoutControlItem lciValidation;
        private DevExpress.XtraLayout.LayoutControlItem lciLog;
        private DevExpress.XtraLayout.LayoutControlGroup lcgActions;
        private DevExpress.XtraLayout.LayoutControlItem lciNotice;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceActions;
        private DevExpress.XtraLayout.LayoutControlItem lciClose;
    }
}
