// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// WF-RSV-01 신규 예약 (03 §8). 판정은 하지 않는다: 그리고 이벤트만 올린다 (킷 §2).
    ///
    /// 배치는 `lcMain` LayoutControl 하나가 갖는다 — 상단 수검자|일정, 가운데 대상판정 한 줄,
    /// 하단 NEX|AEX (03 §8.4 비율). 03 과 와이어프레임은 배치를 구속하지 않는다
    /// (ROOT AGENTS.md §1.1) — 필드의 뜻과 업무 규칙만 거기서 온다.
    /// </summary>
    public partial class UcReservation : XtraUserControl, IReservationView
    {
        private ReservationPresenter _presenter;
        private IPatientService _patientService;
        private string _operatorName;

        // Presenter 가 시킨 값 쓰기가 다시 ScheduleChanged 로 돌아와 무한 왕복하는 것을 막는다.
        private bool _suppress;

        partial void ConfigureUI();

        public UcReservation()
        {
            InitializeComponent();
            ConfigureUI();
        }

        /// <summary>
        /// Presenter 를 붙인다. UserControl 은 디자이너가 만들어야 하므로 생성자로 받지 않는다
        /// (킷 `references/mvp-wiring.md`).
        ///
        /// `IPatientService` 는 DLG-PAT-02 를 여는 데 쓴다 — Modal 을 여는 것은 View 의 일이다.
        /// </summary>
        public void Attach(IReservationService service, IPatientService patientService, string operatorName)
        {
            _patientService = patientService;
            _operatorName = operatorName;
            _presenter = new ReservationPresenter(this, service, patientService, operatorName);
        }

        /// <summary>03 §3 `BeginNewReservation(Context, PatientId?, Source)`. MainForm 이 부른다.</summary>
        public void Begin(ReservationContext context, long? patientId, NavigationSource source)
        {
            if (_presenter != null)
            {
                _presenter.Begin(context, patientId, source);
            }
        }

        /// <summary>Ribbon `[예약저장]` 이 눌렸다. Ribbon 은 MainForm 이 갖는다 (03 §8.2).</summary>
        public void RequestSave()
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

        public event EventHandler<long> PatientPicked;
        public event EventHandler ScheduleChanged;
        public event EventHandler SaveRequested;

        /// <summary>`[예약저장]` 의 Enabled 를 Ribbon 을 가진 쪽으로 넘긴다 (03 §8.2).</summary>
        public event EventHandler<bool> SaveEnabledChanged;

        /// <summary>03 §8.5 기존 유효예약 · §8.11 저장 성공 — 둘 다 Workbench 로 넘긴다.</summary>
        public event EventHandler<WorkbenchTarget> WorkbenchRequested;

        public PatientDetailDto Patient
        {
            set
            {
                txtPatientChartNo.Text = value == null ? string.Empty : value.ChartNo;
                txtPatientName.Text = value == null ? string.Empty : value.Name;
                txtPatientBirthGender.Text = value == null
                    ? string.Empty
                    : Pair(clsPatientText.FormatBirthday(value.Birthday), clsPatientText.FormatGender(value.Gender));
            }
        }

        /// <summary>03 §8.5 — 수검자가 확정되기 전에는 일정영역이 닫혀 있다.</summary>
        public bool ScheduleEnabled
        {
            set
            {
                deReserveDate.Enabled = value;
                rgSlot.Enabled = value;
            }
        }

        public DateTime ReserveDate
        {
            get
            {
                return deReserveDate.EditValue is DateTime ? deReserveDate.DateTime.Date : DateTime.Today;
            }

            set
            {
                _suppress = true;
                try { deReserveDate.EditValue = value; }
                finally { _suppress = false; }
            }
        }

        public bool ReserveDateReadOnly
        {
            set { deReserveDate.Properties.ReadOnly = value; }
        }

        /// <summary>
        /// 03 §8.3 — `○ 오전  12 / 20`. 정원이 찼거나 운영하지 않거나 마감이 지난 시간대는
        /// 아예 고를 수 없다 (03 §8.6). 그 판정은 DB 의 `선택가능` 이다 (05 §9.7).
        /// </summary>
        public IList<SlotInfoDto> Slots
        {
            set
            {
                _suppress = true;
                try
                {
                    rgSlot.Properties.Items.Clear();
                    if (value != null)
                    {
                        foreach (SlotInfoDto slot in value)
                        {
                            var item = new RadioGroupItem(slot.SlotCode, DescriptionOf(slot));
                            item.Enabled = slot.Selectable;
                            rgSlot.Properties.Items.Add(item);
                        }
                    }

                    rgSlot.EditValue = null;
                }
                finally { _suppress = false; }
            }
        }

        public string SlotCode
        {
            get { return rgSlot.EditValue as string; }

            set
            {
                _suppress = true;
                try { rgSlot.EditValue = value; }
                finally { _suppress = false; }
            }
        }

        public string TargetText
        {
            set { lblTarget.Text = value ?? string.Empty; }
        }

        public IList<WorkExamItemDto> NexItems
        {
            set { gcNexList.DataSource = value; }
        }

        public IList<ReservationAexItemDto> AexItems
        {
            set { gcAexList.DataSource = value; }
        }

        /// <summary>03 §8.5 — TGT 비대상이면 AEX 를 통째로 닫는다.</summary>
        public bool AexEnabled
        {
            set { gcAexList.Enabled = value; }
        }

        /// <summary>
        /// 자리 순서가 곧 `추가검사01`~`07` 이다 — RS5 가 추가검사코드 ASC 로 오므로 (05 §9.6)
        /// 받은 순서를 그대로 되돌리면 맞는다.
        ///
        /// [X] 체크를 켠 직후에는 편집기가 아직 열려 있어 값이 DTO 로 내려가지 않았을 수 있다.
        ///     Ribbon 의 `[예약저장]` 은 Grid 밖이라 그대로 읽으면 방금 켠 것을 놓친다.
        /// </summary>
        public bool[] AexSelection
        {
            get
            {
                gvAexList.PostEditor();

                var selected = new bool[ReservationAvailabilityRequest.AexParameterCount];
                var list = gcAexList.DataSource as IList<ReservationAexItemDto>;
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
                EventHandler<bool> handler = SaveEnabledChanged;
                if (handler != null)
                {
                    handler(this, value);
                }
            }
        }

        public string BlockMessage
        {
            set { lblBlock.Text = value ?? string.Empty; }
        }

        public void GoToWorkbench(WorkContext context, long workId)
        {
            EventHandler<WorkbenchTarget> handler = WorkbenchRequested;
            if (handler != null)
            {
                handler(this, new WorkbenchTarget { Context = context, WorkId = workId });
            }
        }

        public void ShowMessage(string message)
        {
            Form owner = FindForm();
            XtraMessageBox.Show(owner, message, owner == null ? string.Empty : owner.Text);
        }

        /// <summary>03 §8.5 — DLG-PAT-02 로 수검자를 확정한다. Modal 을 여는 것은 View 의 일이다.</summary>
        private void btnPickPatient_Click(object sender, EventArgs e)
        {
            if (_patientService == null)
            {
                return;
            }

            using (var select = new FrmPatientSelect(_patientService, _operatorName))
            {
                if (select.ShowDialog(FindForm()) != DialogResult.OK || select.SelectedPatientId == null)
                {
                    return;
                }

                EventHandler<long> handler = PatientPicked;
                if (handler != null)
                {
                    handler(this, select.SelectedPatientId.Value);
                }
            }
        }

        /// <summary>
        /// 예약일과 시간대가 같은 자리로 들어온다 — 03 §8.5 는 둘을 묶어 `일정 확정` 으로 본다.
        /// 조회는 동기 SP 호출이라 그동안 대기 커서를 세운다.
        /// </summary>
        private void Schedule_Changed(object sender, EventArgs e)
        {
            if (_suppress)
            {
                return;
            }

            EventHandler handler = ScheduleChanged;
            if (handler == null)
            {
                return;
            }

            using (new clsBusyScope(this))
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 03 §8.9 — 성별 불충족·국가검진 포함인 검사는 고를 수 없다. 판정은 DB 의 `선택가능` 이고
        /// 사유는 옆 칸에 이미 적혀 있으므로, 여기서는 편집기를 열지 않기만 한다.
        /// </summary>
        private void gvAexList_ShowingEditor(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var row = gvAexList.GetFocusedRow() as ReservationAexItemDto;
            e.Cancel = row == null || !row.Selectable;
        }

        /// <summary>
        /// 03 §8.9 `Disabled+사유` — 사유 칸만으로는 그 줄이 닫혔다는 것이 눈에 들어오지 않는다.
        /// 고를 수 없는 줄은 흐리게 적어 체크박스가 열려 있는 줄과 구별한다.
        /// </summary>
        private void gvAexList_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0)
            {
                return;
            }

            var row = gvAexList.GetRow(e.RowHandle) as ReservationAexItemDto;
            if (row != null && !row.Selectable)
            {
                e.Appearance.ForeColor = System.Drawing.SystemColors.GrayText;
            }
        }

        /// <summary>03 §8.3 — `오전  12 / 20`. 고를 수 없는 시간대는 사유를 함께 적는다.</summary>
        private static string DescriptionOf(SlotInfoDto slot)
        {
            string text = slot.SlotName + "    "
                + clsWorkText.FormatCapacity(slot.CurrentCount, slot.Capacity, slot.RemainingSeats);

            if (!slot.Selectable && !string.IsNullOrWhiteSpace(slot.BlockMessage))
            {
                text = text + "   —   " + slot.BlockMessage.Trim();
            }

            return text;
        }

        /// <summary>한 칸에 둘을 넣은 자리다 (`생년월일 / 성별`). 한쪽이 비면 남는 쪽만 적는다.</summary>
        private static string Pair(string left, string right)
        {
            bool hasLeft = !string.IsNullOrWhiteSpace(left);
            bool hasRight = !string.IsNullOrWhiteSpace(right);
            if (hasLeft && hasRight) { return left + " / " + right; }
            if (hasLeft) { return left; }
            return hasRight ? right : string.Empty;
        }
    }
}
