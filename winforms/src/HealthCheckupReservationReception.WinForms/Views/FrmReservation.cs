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
    /// **모달이다** (2026-09-10 grilling 2회차). 예전에는 업무 탭 하나를 차지했는데,
    /// 탭 띠를 걷은 뒤로는 "떠나 있다가 돌아온다" 는 탭의 유일한 이점이 사라졌고 —
    /// 미저장 신규예약이 아무 데서도 보이지 않는 자리가 되었다. 05 §9.3 은 신규·WalkIn·
    /// 예약변경을 `변경범위` 하나로 가르는 **한 SP** 로 설계했으므로, 화면도 하나여야 한다.
    ///
    /// 배치는 `lcMain` LayoutControl 하나가 갖는다 — 상단 수검자|일정, 가운데 대상판정 한 줄,
    /// 하단 NEX|AEX, 맨 아래 `[저장] [닫기]`. 03 과 와이어프레임은 배치를 구속하지 않는다
    /// (ROOT AGENTS.md §1.1) — 필드의 뜻과 업무 규칙만 거기서 온다.
    /// </summary>
    public partial class FrmReservation : XtraForm, IReservationView
    {
        private readonly ReservationPresenter _presenter;

        // Presenter 가 시킨 값 쓰기가 다시 ScheduleChanged 로 돌아와 무한 왕복하는 것을 막는다.
        private bool _suppress;

        // 저장이 끝난 뒤에는 폐기 확인을 묻지 않는다 (03 §8.10).
        private bool _saved;

        // OnLoad 까지 들고 있는 진입 인자다 (03 §3 BeginNewReservation).
        private long _patientId;

        partial void ConfigureUI();

        /// <summary>
        /// 디자이너용 생성자다. VS 디자이너와 배치 게이트(LayoutBaselineTests)가 이것으로 만든다 —
        /// 서비스도 Presenter 도 없이 껍데기만 선다.
        /// </summary>
        public FrmReservation()
        {
            InitializeComponent();
            ConfigureUI();
        }

        /// <summary>
        /// 03 §3 `BeginNewReservation(PatientId)`. **모달은 대상을 받고 열린다** —
        /// 수검자가 정해지지 않은 채로는 이 화면이 아예 서지 않는다.
        ///
        /// 일반/현장을 가르는 인자가 없다. 그것은 조작자가 아니라 시각이 정한다 (00 RP-05).
        /// </summary>
        public FrmReservation(IReservationService service, IPatientService patientService,
            string operatorName, long patientId)
            : this()
        {
            _presenter = new ReservationPresenter(this, service, patientService, operatorName);
            _patientId = patientId;
        }

        /// <summary>
        /// [X] **진입 조회를 생성자에서 돌리면 안 된다.** 03 §8.5 는 기존 유효예약이 있으면
        ///     신규예약을 중단하라고 한다 — 그 길은 <see cref="GoToWorkbench"/> 이고 창을
        ///     닫는다. 아직 뜨지도 않은 폼을 닫으면 그대로 Dispose 되어, 부른 쪽의
        ///     `ShowDialog` 가 `ObjectDisposedException` 으로 터진다(실측 2026-09-11).
        ///
        ///     OnLoad 는 창이 그려지기 **전**이면서 `ShowDialog` 안이다. 여기서 닫으면
        ///     창이 깜빡이지도 않고 `DialogResult` 가 부른 쪽으로 그대로 돌아간다.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (_presenter == null)
            {
                return;
            }

            using (new clsBusyScope(this))
            {
                _presenter.Begin(_patientId);
            }
        }

        /// <summary>
        /// 03 §8.11 저장 성공·§8.5 기존 유효예약 — 둘 다 여기로 넘어간다. 모달이 `OK` 로 닫히면
        /// 부른 쪽(MainForm)이 이 값을 읽어 Workbench 를 연다.
        /// </summary>
        public WorkbenchTarget Result { get; private set; }

        public event EventHandler ScheduleChanged;
        public event EventHandler SaveRequested;

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

        public string ReserveTypeText
        {
            set { lblReserveType.Text = value ?? string.Empty; }
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
        ///     `[저장]` 은 Grid 밖이라 그대로 읽으면 방금 켠 것을 놓친다.
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

        /// <summary>
        /// 03 §8.2 — 저장 단추는 조건이 갖춰졌을 때만 열린다. Ribbon 으로 넘기던 것을 모달
        /// 하단으로 내렸으므로 이제 자기 단추를 자기가 연다.
        /// </summary>
        public bool SaveEnabled
        {
            set { btnSave.Enabled = value; }
        }

        public string BlockMessage
        {
            set { lblBlock.Text = value ?? string.Empty; }
        }

        /// <summary>
        /// 03 §8.11 — 저장이 끝나면 이 화면은 할 일이 없다. 결과만 남기고 닫는다.
        /// Workbench 를 여는 것은 Shell 의 일이다 (03 §3 `OpenWorkbench`).
        /// </summary>
        public void GoToWorkbench(WorkContext context, long workId)
        {
            Result = new WorkbenchTarget { Context = context, WorkId = workId };
            _saved = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        public void ShowMessage(string message)
        {
            XtraMessageBox.Show(this, message, Text);
        }

        /// <summary>
        /// 03 §8.10 폐기 확인. **닫는 길이 하나뿐이라 트리거도 하나다** — `[닫기]`·`Esc`·창 X 가
        /// 전부 여기로 온다.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_saved && _presenter != null && _presenter.HasUnsavedInput)
            {
                DialogResult answer = XtraMessageBox.Show(this,
                    "입력한 내용이 저장되지 않았습니다. 닫으시겠습니까?", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
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
