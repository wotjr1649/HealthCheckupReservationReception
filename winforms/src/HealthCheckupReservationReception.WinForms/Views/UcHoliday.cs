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
    /// 03 §24 는 Modal 로 적었지만 2026-09-11 사용자 결정으로 **업무 화면**이 되었다.
    /// 리본 탭이 「가는 곳」만 갖게 되면서 `접수 관리` 오른쪽에 자리가 생겼고, 목록 조회·추가·
    /// 수정·삭제는 원래 목록 화면 성격이다 (ROOT AGENTS.md §1.1 이 배치·내비게이션을 풀었다).
    /// </summary>
    public partial class UcHoliday : XtraUserControl, IHolidayView
    {
        private HolidayPresenter _presenter;
        private clsGridRowPicker _picker;

        partial void ConfigureUI();

        public UcHoliday()
        {
            InitializeComponent();
            ConfigureUI();
        }

        /// <summary>
        /// Presenter 를 붙인다. UserControl 은 디자이너가 만들어야 하므로 생성자로 받지 않는다
        /// (킷 `references/mvp-wiring.md`).
        /// </summary>
        public void Attach(IHolidayService service, ICommonStatusService statusService)
        {
            _presenter = new HolidayPresenter(this, service, statusService);
        }

        /// <summary>
        /// 03 §24.4 조회기간 기본값은 오늘부터 두 해다.
        ///
        /// [X] `00` §7.4 가 정한 등재 범위(달력 날짜 둘)를 여기 옮겨 적지 않는다 — 같은 값이 두
        ///     곳에 생기고 이 자리에는 그것을 지킬 게이트가 없다 (ROOT AGENTS.md §6). 화면
        ///     기본값은 「오늘부터 두 해」라는 상대 범위이고, 등재가 언제 끝나는지는 SP 가
        ///     RS2 로 말해 준다(§24.6).
        /// </summary>
        public void Begin(DateTime today)
        {
            deFrom.EditValue = today.Date;
            deTo.EditValue = today.Date.AddYears(2);

            if (_presenter != null)
            {
                using (new clsBusyScope(this))
                {
                    _presenter.LoadInitial();
                }
            }
        }

        /// <summary>Ribbon 의 `[휴무일추가]`·`[휴무일수정]`·`[휴무일삭제]` 가 부른다.</summary>
        public void RequestRegister() { Run(Register); }

        public void RequestUpdate() { Run(Update); }

        public void RequestDelete() { Run(Delete); }

        private void Register() { _presenter.Register(); }

        private void Update() { _presenter.Update(); }

        private void Delete() { _presenter.Delete(); }

        /// <summary>03 §24.5 — 자체휴무일 행이 잡혀 있는지. Ribbon 이 그것으로 Action 을 여닫는다.</summary>
        public event EventHandler<bool> RowActionsChanged;

        public event EventHandler SearchRequested;
        public event EventHandler<HolidayListItemDto> SelectionChanged;

        public DateTime? FromDate
        {
            get { return DateOf(deFrom); }
        }

        public DateTime? ToDate
        {
            get { return DateOf(deTo); }
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

        public bool RowActionsEnabled
        {
            set
            {
                EventHandler<bool> handler = RowActionsChanged;
                if (handler != null)
                {
                    handler(this, value);
                }
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
