namespace HealthCheckupReservationReception.Views
{
    partial class MainForm
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
            this.barRibbonMain = new DevExpress.XtraBars.Ribbon.RibbonControl();
            this.barStatusMain = new DevExpress.XtraBars.Ribbon.RibbonStatusBar();
            ((System.ComponentModel.ISupportInitialize)(this.barRibbonMain)).BeginInit();
            this.SuspendLayout();
            //
            // barRibbonMain
            //
            this.barRibbonMain.Location = new System.Drawing.Point(0, 0);
            this.barRibbonMain.Name = "barRibbonMain";
            this.barRibbonMain.Size = new System.Drawing.Size(1024, 32);
            this.barRibbonMain.StatusBar = this.barStatusMain;
            //
            // barStatusMain
            //
            this.barStatusMain.Location = new System.Drawing.Point(0, 678);
            this.barStatusMain.Name = "barStatusMain";
            this.barStatusMain.Ribbon = this.barRibbonMain;
            this.barStatusMain.Size = new System.Drawing.Size(1024, 22);
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 700);
            this.Controls.Add(this.barStatusMain);
            this.Controls.Add(this.barRibbonMain);
            this.Name = "MainForm";
            this.Ribbon = this.barRibbonMain;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.StatusBar = this.barStatusMain;
            this.Text = "검진 예약·접수 관리 프로그램";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.barRibbonMain)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraBars.Ribbon.RibbonControl barRibbonMain;
        private DevExpress.XtraBars.Ribbon.RibbonStatusBar barStatusMain;
    }
}
