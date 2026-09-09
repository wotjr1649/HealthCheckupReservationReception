namespace HealthCheckupReservationReception.Views
{
    partial class FrmColumnChooser
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
            this.clbColumns = new DevExpress.XtraEditors.CheckedListBoxControl();
            this.btnRestoreDefault = new DevExpress.XtraEditors.SimpleButton();
            this.btnClose = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.clbColumns)).BeginInit();
            this.SuspendLayout();
            //
            // clbColumns
            //
            this.clbColumns.CheckOnClick = true;
            this.clbColumns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clbColumns.Location = new System.Drawing.Point(10, 10);
            this.clbColumns.Name = "clbColumns";
            this.clbColumns.Size = new System.Drawing.Size(244, 210);
            this.clbColumns.TabIndex = 0;
            this.clbColumns.ItemCheck += new DevExpress.XtraEditors.Controls.ItemCheckEventHandler(this.clbColumns_ItemCheck);
            //
            // btnRestoreDefault
            //
            this.btnRestoreDefault.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRestoreDefault.Location = new System.Drawing.Point(10, 226);
            this.btnRestoreDefault.Name = "btnRestoreDefault";
            this.btnRestoreDefault.Size = new System.Drawing.Size(100, 26);
            this.btnRestoreDefault.TabIndex = 1;
            this.btnRestoreDefault.Text = "기본값 복원";
            this.btnRestoreDefault.Click += new System.EventHandler(this.btnRestoreDefault_Click);
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(174, 226);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(80, 26);
            this.btnClose.TabIndex = 2;
            this.btnClose.Text = "닫기";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            //
            // FrmColumnChooser
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(264, 262);
            this.Controls.Add(this.clbColumns);
            this.Controls.Add(this.btnRestoreDefault);
            this.Controls.Add(this.btnClose);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmColumnChooser";
            this.Padding = new System.Windows.Forms.Padding(10, 10, 10, 52);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "컬럼설정";
            ((System.ComponentModel.ISupportInitialize)(this.clbColumns)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.CheckedListBoxControl clbColumns;
        private DevExpress.XtraEditors.SimpleButton btnRestoreDefault;
        private DevExpress.XtraEditors.SimpleButton btnClose;
    }
}
