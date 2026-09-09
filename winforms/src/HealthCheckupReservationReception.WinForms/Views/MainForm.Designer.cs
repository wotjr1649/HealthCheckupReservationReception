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
            this.barBtnPatient = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnNewReservation = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnReservationDesk = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnReceptionDesk = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnHoliday = new DevExpress.XtraBars.BarButtonItem();
            this.barStaticWorkStatus = new DevExpress.XtraBars.BarStaticItem();
            this.barStaticOperator = new DevExpress.XtraBars.BarStaticItem();
            this.barPageBusiness = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.barGroupNavigation = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.barStatusMain = new DevExpress.XtraBars.Ribbon.RibbonStatusBar();
            this.tabBusiness = new DevExpress.XtraTab.XtraTabControl();
            ((System.ComponentModel.ISupportInitialize)(this.barRibbonMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tabBusiness)).BeginInit();
            this.SuspendLayout();
            //
            // barRibbonMain
            //
            this.barRibbonMain.ExpandCollapseItem.Id = 0;
            this.barRibbonMain.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.barRibbonMain.ExpandCollapseItem,
            this.barBtnPatient,
            this.barBtnNewReservation,
            this.barBtnReservationDesk,
            this.barBtnReceptionDesk,
            this.barBtnHoliday,
            this.barStaticWorkStatus,
            this.barStaticOperator});
            this.barRibbonMain.Location = new System.Drawing.Point(0, 0);
            this.barRibbonMain.MaxItemId = 8;
            this.barRibbonMain.Name = "barRibbonMain";
            this.barRibbonMain.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] {
            this.barPageBusiness});
            this.barRibbonMain.Size = new System.Drawing.Size(1024, 130);
            this.barRibbonMain.StatusBar = this.barStatusMain;
            //
            // barBtnPatient
            //
            this.barBtnPatient.Caption = "수검자 관리";
            this.barBtnPatient.Id = 1;
            this.barBtnPatient.Name = "barBtnPatient";
            this.barBtnPatient.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnPatient_ItemClick);
            //
            // barBtnNewReservation
            //
            this.barBtnNewReservation.Caption = "신규 예약";
            this.barBtnNewReservation.Id = 2;
            this.barBtnNewReservation.Name = "barBtnNewReservation";
            this.barBtnNewReservation.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnNewReservation_ItemClick);
            //
            // barBtnReservationDesk
            //
            this.barBtnReservationDesk.Caption = "예약 관리";
            this.barBtnReservationDesk.Id = 3;
            this.barBtnReservationDesk.Name = "barBtnReservationDesk";
            this.barBtnReservationDesk.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnReservationDesk_ItemClick);
            //
            // barBtnReceptionDesk
            //
            this.barBtnReceptionDesk.Caption = "접수 관리";
            this.barBtnReceptionDesk.Id = 4;
            this.barBtnReceptionDesk.Name = "barBtnReceptionDesk";
            this.barBtnReceptionDesk.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnReceptionDesk_ItemClick);
            //
            // barBtnHoliday
            //
            this.barBtnHoliday.Caption = "휴무일 관리";
            this.barBtnHoliday.Id = 5;
            this.barBtnHoliday.Name = "barBtnHoliday";
            this.barBtnHoliday.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barBtnHoliday_ItemClick);
            //
            // barStaticWorkStatus
            //
            this.barStaticWorkStatus.Id = 6;
            this.barStaticWorkStatus.Name = "barStaticWorkStatus";
            //
            // barStaticOperator
            //
            this.barStaticOperator.Id = 7;
            this.barStaticOperator.Name = "barStaticOperator";
            //
            // barPageBusiness
            //
            this.barPageBusiness.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
            this.barGroupNavigation});
            this.barPageBusiness.Name = "barPageBusiness";
            this.barPageBusiness.Text = "업무";
            //
            // barGroupNavigation
            //
            this.barGroupNavigation.ItemLinks.Add(this.barBtnPatient);
            this.barGroupNavigation.ItemLinks.Add(this.barBtnNewReservation);
            this.barGroupNavigation.ItemLinks.Add(this.barBtnReservationDesk);
            this.barGroupNavigation.ItemLinks.Add(this.barBtnReceptionDesk);
            this.barGroupNavigation.ItemLinks.Add(this.barBtnHoliday);
            this.barGroupNavigation.Name = "barGroupNavigation";
            this.barGroupNavigation.Text = "업무 이동";
            //
            // barStatusMain
            //
            this.barStatusMain.ItemLinks.Add(this.barStaticWorkStatus);
            this.barStatusMain.ItemLinks.Add(this.barStaticOperator);
            this.barStatusMain.Location = new System.Drawing.Point(0, 678);
            this.barStatusMain.Name = "barStatusMain";
            this.barStatusMain.Ribbon = this.barRibbonMain;
            this.barStatusMain.Size = new System.Drawing.Size(1024, 22);
            //
            // tabBusiness
            //
            this.tabBusiness.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabBusiness.Location = new System.Drawing.Point(0, 130);
            this.tabBusiness.Name = "tabBusiness";
            this.tabBusiness.Size = new System.Drawing.Size(1024, 548);
            this.tabBusiness.TabIndex = 2;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 700);
            this.Controls.Add(this.tabBusiness);
            this.Controls.Add(this.barStatusMain);
            this.Controls.Add(this.barRibbonMain);
            this.Name = "MainForm";
            this.Ribbon = this.barRibbonMain;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.StatusBar = this.barStatusMain;
            this.Text = "검진 예약·접수 관리 프로그램";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.barRibbonMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tabBusiness)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private DevExpress.XtraBars.Ribbon.RibbonControl barRibbonMain;
        private DevExpress.XtraBars.Ribbon.RibbonStatusBar barStatusMain;
        private DevExpress.XtraBars.Ribbon.RibbonPage barPageBusiness;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup barGroupNavigation;
        private DevExpress.XtraBars.BarButtonItem barBtnPatient;
        private DevExpress.XtraBars.BarButtonItem barBtnNewReservation;
        private DevExpress.XtraBars.BarButtonItem barBtnReservationDesk;
        private DevExpress.XtraBars.BarButtonItem barBtnReceptionDesk;
        private DevExpress.XtraBars.BarButtonItem barBtnHoliday;
        private DevExpress.XtraBars.BarStaticItem barStaticWorkStatus;
        private DevExpress.XtraBars.BarStaticItem barStaticOperator;
        private DevExpress.XtraTab.XtraTabControl tabBusiness;
    }
}
