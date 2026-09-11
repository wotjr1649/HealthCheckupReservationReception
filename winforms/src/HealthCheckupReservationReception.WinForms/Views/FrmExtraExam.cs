// 화면 ID: DLG-RCP-02 — 접수완료 추가검사 변경 (03 §12)
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// DLG-RCP-02 접수완료 추가검사 변경 Modal (03 §12).
    ///
    /// **고칠 수 있는 것은 AEX 하나다.** 예약일·시간대·NEX 는 ReadOnly 이고 상태는 `RCP` 를
    /// 유지한다. 배치는 DLG-RCP-01 과 같은 꼴이다 — 같은 정보를 보여 주고 한 칸만 열린다.
    /// </summary>
    public partial class FrmExtraExam : XtraForm, IExtraExamView
    {
        private readonly ExtraExamPresenter _presenter;
        private readonly long _workId;

        partial void ConfigureUI();

        /// <summary>[X] **VS 디자이너 전용이다** (`references/designer.md` 함정 2).</summary>
        public FrmExtraExam()
        {
            InitializeComponent();
            ConfigureUI();
        }

        public FrmExtraExam(IWorkService service, string operatorName, long workId) : this()
        {
            _presenter = new ExtraExamPresenter(this, service, operatorName);
            _workId = workId;
        }

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

        public event EventHandler SaveRequested;

        public string Title
        {
            set { Text = string.IsNullOrWhiteSpace(value) ? "추가검사 변경" : value; }
        }

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

        public IList<ReservationAexItemDto> AexOptions
        {
            set { gcAex.DataSource = value; }
        }

        /// <summary>
        /// 05 §12.2 `@추가검사01~07선택여부`. **순서가 곧 그 번호다** — RS5 가 추가검사코드
        /// ASC 로 오므로 받은 순서를 그대로 되돌려 주면 자리가 맞는다 (WF-RSV-01 과 같은 규약).
        /// </summary>
        public bool[] AexSelection
        {
            get
            {
                // 셀 편집 중이면 아직 DataSource 에 안 내려갔다.
                gvAex.PostEditor();

                var selected = new bool[ReservationAvailabilityRequest.AexParameterCount];
                var list = gcAex.DataSource as IList<ReservationAexItemDto>;
                if (list == null)
                {
                    return selected;
                }

                for (int i = 0; i < list.Count && i < selected.Length; i++)
                {
                    selected[i] = list[i].Requested;
                }

                return selected;
            }
        }

        public bool SaveEnabled
        {
            set
            {
                btnSave.Enabled = value;
                gcAex.Enabled = value;
            }
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

        private void btnSave_Click(object sender, EventArgs e)
        {
            EventHandler handler = SaveRequested;
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

        /// <summary>선택 불가인 행은 체크 편집기를 열지 않는다 (03 §12 · WF-RSV-01 과 같다).</summary>
        private void gvAex_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var row = gvAex.GetFocusedRow() as ReservationAexItemDto;
            if (row != null && !row.Selectable)
            {
                e.Cancel = true;
            }
        }

        /// <summary>선택 불가인 행은 회색으로 — 왜 안 눌리는지 사유 칸이 옆에서 말한다.</summary>
        private void gvAex_RowStyle(object sender, RowStyleEventArgs e)
        {
            if (e.RowHandle < 0)
            {
                return;
            }

            var row = gvAex.GetRow(e.RowHandle) as ReservationAexItemDto;
            if (row != null && !row.Selectable)
            {
                e.Appearance.ForeColor = System.Drawing.Color.FromArgb(150, 150, 150);
            }
        }
    }
}
