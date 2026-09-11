// 화면 ID: DLG-RCP-01 — 접수 처리 (03 §11)
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// DLG-RCP-01 접수 처리 Modal (03 §11).
    ///
    /// **읽기 전용 화면에 버튼 하나다.** 수검자·예약일·시간대·NEX·AEX 가 전부 ReadOnly 이고
    /// 접수 단계에서 AEX 를 고치지 않는다 (§11.2) — 접수 전 변경은 예약변경으로 한다.
    /// 그래서 폐기 확인이 없다: 사용자가 들인 것이 없다.
    /// </summary>
    public partial class FrmReception : XtraForm, IReceptionView
    {
        private readonly ReceptionPresenter _presenter;
        private readonly long _workId;

        partial void ConfigureUI();

        /// <summary>[X] **VS 디자이너 전용이다** (`references/designer.md` 함정 2).</summary>
        public FrmReception()
        {
            InitializeComponent();
            ConfigureUI();
        }

        public FrmReception(IWorkService service, string operatorName, long workId) : this()
        {
            _presenter = new ReceptionPresenter(this, service, operatorName);
            _workId = workId;
        }

        /// <summary>진입 조회는 창이 뜬 뒤다 — `FrmReservation` 이 밟은 함정을 되풀이하지 않는다.</summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (_presenter == null)
            {
                return;
            }

            using (new clsBusyScope(this))
            {
                _presenter.Begin(_workId);
            }
        }

        public event EventHandler ReceiveRequested;

        public WorkDetailDto Detail
        {
            set
            {
                txtChartNo.Text = value == null ? string.Empty : value.ChartNo;
                txtName.Text = value == null ? string.Empty : value.Name;
                txtBirthGender.Text = value == null
                    ? string.Empty
                    : clsPatientText.FormatBirthday(value.Birthday) + " / " + clsPatientText.FormatGender(value.Gender);
                txtMobilePhone.Text = value == null
                    ? string.Empty
                    : clsPatientText.FormatPhone(value.MobilePhone);
                txtSchedule.Text = value == null
                    ? string.Empty
                    : clsWorkText.FormatDate(value.ReserveDate) + " " + clsWorkText.FormatSlot(value.SlotCode);
                txtStatus.Text = value == null ? string.Empty : clsWorkText.FormatStatus(value.StatusCode);
                txtCapacity.Text = value == null
                    ? string.Empty
                    : clsWorkText.FormatCapacity(value.CurrentCount, value.Capacity, value.RemainingSeats);
            }
        }

        public IList<WorkExamItemDto> NexItems
        {
            set { gcNex.DataSource = value; }
        }

        public IList<WorkExamItemDto> AexItems
        {
            set { gcAex.DataSource = value; }
        }

        public string EligibilityText
        {
            set { lblEligibility.Text = value ?? string.Empty; }
        }

        public bool ReceiveEnabled
        {
            set { btnReceive.Enabled = value; }
        }

        public string ValidationMessage
        {
            set { lblValidation.Text = value ?? string.Empty; }
        }

        public void Done()
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnReceive_Click(object sender, EventArgs e)
        {
            EventHandler handler = ReceiveRequested;
            if (handler == null)
            {
                return;
            }

            using (new clsBusyScope(this))
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
