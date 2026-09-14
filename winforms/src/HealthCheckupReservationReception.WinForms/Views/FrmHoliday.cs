// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
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
    /// DLG-HOL-01 휴무일 관리 (03 §24). 판정은 하지 않는다: 그리고 이벤트만 올린다 (킷 §2).
    ///
    /// `[X]` **업무 Tab 이었다가 Modal 로 되돌렸다** — 2026-09-11 사용자 결정, 같은 날 두 번째다.
    /// `03` §2 와 §24.2 는 처음부터 Modal 이라고 적고 있었다. 탭으로 두는 동안 **초기화 경로가
    /// 통째로 끊겨 이 화면의 CRUD 가 전부 죽어 있었다** — 경위는 `HolidayPresenter.LoadInitial`
    /// 의 `[X]` 가 갖는다. 모달이 되면서 `OnLoad` 하나로 그 길이 이어진다.
    ///
    /// `[!]` **공통 업무 가능조건으로 이 화면을 닫지 않는다** (`03` §24.2). 정비를 운영시간
    /// 안으로 묶으면 *내일이 휴무가 되었다* 를 오늘 18:00 이후에 넣을 수 없다 — 정비가 필요한
    /// 바로 그 시각에 막히는 셈이다.
    /// </summary>
    public partial class FrmHoliday : XtraForm, IHolidayView
    {
        private HolidayPresenter _presenter;
        private clsGridRowPicker _picker;

        partial void ConfigureUI();

        public FrmHoliday()
        {
            InitializeComponent();
            ConfigureUI();
        }

        /// <summary>모달이므로 생성자가 곧 진입점이다 (킷 `references/mvp-wiring.md`).</summary>
        public FrmHoliday(IHolidayService service, ICommonStatusService statusService)
            : this()
        {
            _presenter = new HolidayPresenter(this, service, statusService);
        }

        /// <summary>
        /// `[X]` **이 자리가 없어서 화면이 죽어 있었다.** 형제 둘(`UcPatientManagement`·
        ///      `UcWorkbench`)은 `OnLoad` 에서 최초 조회를 걸고 있었는데 휴무일 화면만 빠져
        ///      있었고, 어떤 검사도 그것을 보지 않았다 (2026-09-11 실측). 그래서 이제
        ///      `ScreenInitializationTests` 가 화면마다 이 한 줄을 재는다.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (_presenter != null)
            {
                using (new clsBusyScope(this))
                {
                    _presenter.LoadInitial();
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e) { Run(Register); }

        private void btnEdit_Click(object sender, EventArgs e) { Run(Update); }

        private void btnDelete_Click(object sender, EventArgs e) { Run(Delete); }

        /// <summary>
        /// Action 마다 즉시 저장하므로 지킬 Dirty 가 없다 — 폐기 확인을 묻지 않는다
        /// (2026-09-11 사용자 결정). 예약 모달과 다른 점이 이것이다.
        /// </summary>
        private void btnClose_Click(object sender, EventArgs e) { Close(); }

        private void Register() { _presenter.Register(); }

        private void Update() { _presenter.Update(); }

        private void Delete() { _presenter.Delete(); }

        public event EventHandler SearchRequested;
        public event EventHandler<HolidayListItemDto> SelectionChanged;

        /// <summary>Presenter 가 03 §24.4 기본값(DB 오늘부터 두 해)으로 채운다.</summary>
        public DateTime? FromDate
        {
            get { return DateOf(deFrom); }
            set { deFrom.EditValue = value; }
        }

        public DateTime? ToDate
        {
            get { return DateOf(deTo); }
            set { deTo.EditValue = value; }
        }

        public string HolidayTypeFilter
        {
            get { return cboType.EditValue as string; }
        }

        public IList<HolidayListItemDto> Rows
        {
            set { _picker.Rebind(gcHolidayList, value); }
        }

        /// <summary>
        /// 저장 뒤 그 날짜로 돌아간다. 목록을 다시 읽으면 자리를 잃는데, 방금 고친 줄을
        /// 사용자가 다시 찾게 하지 않는다.
        /// </summary>
        public void SelectDate(DateTime holidayDate)
        {
            for (int i = 0; i < gvHolidayList.RowCount; i++)
            {
                var row = gvHolidayList.GetRow(i) as HolidayListItemDto;
                if (row != null && row.HolidayDate.Date == holidayDate.Date)
                {
                    _picker.Select(i);
                    return;
                }
            }
        }

        public DateTime? InputDate
        {
            get { return DateOf(deInputDate); }
            set { deInputDate.EditValue = value; }
        }

        public string InputName
        {
            get { return txtInputName.Text; }
            set { txtInputName.Text = value ?? string.Empty; }
        }

        public bool InputActive
        {
            get { return chkInputActive.Checked; }
            set { chkInputActive.Checked = value; }
        }

        public string InputMemo
        {
            get { return txtInputMemo.Text; }
            set { txtInputMemo.Text = value ?? string.Empty; }
        }

        /// <summary>03 §24.4 — 법정·대체 행이 선택되면 닫는다. **숨기지 않는다** (§4.2).</summary>
        public bool EditEnabled
        {
            set
            {
                deInputDate.Enabled = value;
                txtInputName.Enabled = value;
                chkInputActive.Enabled = value;
                txtInputMemo.Enabled = value;
            }
        }

        /// <summary>03 §24.5 — `[수정]`·`[삭제]` 는 자체휴무일 행이 잡혔을 때만 열린다.</summary>
        public bool RowActionsEnabled
        {
            set
            {
                btnEdit.Enabled = value;
                btnDelete.Enabled = value;
            }
        }

        public string RegistryWarning
        {
            set { lblRegistry.Text = value ?? string.Empty; }
        }

        public string BlockMessage
        {
            set { lblBlock.Text = value ?? string.Empty; }
        }

        public void ShowMessage(string message)
        {
            Form owner = FindForm();
            XtraMessageBox.Show(owner, message, owner == null ? string.Empty : owner.Text);
        }

        /// <summary>03 §24.5 — 물리 삭제라 되돌릴 수 없다. 이것만 모달로 묻는다.</summary>
        public bool Confirm(string message)
        {
            Form owner = FindForm();
            return XtraMessageBox.Show(owner, message, owner == null ? string.Empty : owner.Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void Run(Action action)
        {
            if (_presenter == null)
            {
                return;
            }

            using (new clsBusyScope(this))
            {
                action();
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            EventHandler handler = SearchRequested;
            if (handler == null)
            {
                return;
            }

            using (new clsBusyScope(this))
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void gvHolidayList_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            _picker.FocusedRowChanged(e.FocusedRowHandle);
        }

        private void gvHolidayList_RowClick(object sender, DevExpress.XtraGrid.Views.Grid.RowClickEventArgs e)
        {
            _picker.RowClick(e.RowHandle);
        }

        /// <summary>
        /// 03 §24.4 — **법정공휴일·대체공휴일 행은 회색으로 표시한다.** 보여 주는 이유는 그 날짜가
        /// 왜 업무 불가인지 확인하는 것과, 같은 날짜에 자체휴무일을 넣으려는 시도를 미리 막는
        /// 것 둘이다.
        /// </summary>
        private void gvHolidayList_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0)
            {
                return;
            }

            var row = gvHolidayList.GetRow(e.RowHandle) as HolidayListItemDto;
            if (row != null && !DbHolidayType.Own.Equals(row.HolidayType, StringComparison.Ordinal))
            {
                e.Appearance.ForeColor = System.Drawing.SystemColors.GrayText;
            }
        }

        private void Picker_PickChanged(object sender, EventArgs e)
        {
            EventHandler<HolidayListItemDto> handler = SelectionChanged;
            if (handler != null)
            {
                handler(this, _picker.Row as HolidayListItemDto);
            }
        }

        /// <summary>비었으면 null 이고 그것이 미입력이다.</summary>
        private static DateTime? DateOf(DateEdit editor)
        {
            object value = editor.EditValue;
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            DateTime picked = editor.DateTime;
            return picked == DateTime.MinValue ? (DateTime?)null : picked.Date;
        }
    }
}
