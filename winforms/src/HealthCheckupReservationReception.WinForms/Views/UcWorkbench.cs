// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Views.Base;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// WF-WRK-01 예약/접수 공통 Workbench (03 §9). 판정은 하지 않는다: 그리고 이벤트만 올린다 (킷 §2).
    ///
    /// 배치는 `lcMain` LayoutControl 하나가 갖고, 좌 목록 · 우 상세는 `splitWork` 로 나뉜다.
    /// 조회 한 줄은 WF-PAT-01 과 같은 꼴이다 — 입력칸들 · `[조회]` · `[조회 조건]` · `[컬럼 설정]`.
    /// 그 셋의 동작은 같은 부품을 쓴다 (`clsSearchConditions` · `clsColumnChooser` ·
    /// `clsGridRowPicker`, 킷 §2 — 같은 모양이 구체 화면 둘에 생겼다).
    ///
    /// 03 과 와이어프레임은 배치를 구속하지 않는다 (ROOT AGENTS.md §1.1, 2026-09-10 사용자 결정) —
    /// 필드의 뜻과 업무 규칙만 거기서 온다.
    /// </summary>
    public partial class UcWorkbench : XtraUserControl, IWorkbenchView
    {
        private WorkbenchPresenter _presenter;

        private clsGridRowPicker _picker;
        private clsSearchConditions _conditions;
        private clsColumnChooser _columns;

        // 조회 재진입 가드. 동기 SP 호출 동안 쌓인 클릭이 되돌아오는 것을 막는다.
        private bool _searching;

        partial void ConfigureUI();

        public UcWorkbench()
        {
            InitializeComponent();
            ConfigureUI();
        }

        /// <summary>
        /// Presenter 를 붙인다. UserControl 은 디자이너가 만들어야 하므로 생성자로 받지 않는다
        /// (킷 `references/mvp-wiring.md`).
        /// </summary>
        public void Attach(IWorkService service)
        {
            _presenter = new WorkbenchPresenter(this, service);
        }

        /// <summary>
        /// 03 §9.1 — 상단 Navigation 이든 WorkId Targeted Navigation 이든 이 길로 들어온다.
        /// MainForm 의 <see cref="IMainView.OpenWorkbench"/> 가 부른다.
        /// </summary>
        public void OpenContext(WorkContext context, long? workId)
        {
            if (_presenter != null)
            {
                _presenter.OpenContext(context, workId);
            }
        }

        /// <summary>
        /// 화면을 열면 한 번 조회한다. `[조회]` 이벤트를 올리지 않는다 — 그 경로는 눌린 버튼을
        /// 잠그는 자리라 사용자가 부탁하지 않은 호출에는 맞지 않는다. 실패는 Inline 으로 보인다.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (_presenter != null)
            {
                _presenter.LoadInitial();
            }
        }

        public event EventHandler SearchRequested;
        public event EventHandler<long?> SelectionChanged;

        /// <summary>
        /// 03 §9.6 · §9.7 의 Context Ribbon 은 MainForm 이 갖는다. 판정은 Presenter 가 하고
        /// 여기서는 그 판정을 Ribbon 을 가진 쪽으로 넘기기만 한다.
        /// </summary>
        public event EventHandler<WorkActionState> WorkActionsChanged;

        public DateTime? FromDate { get { return DateOf(deFrom); } }
        public DateTime? ToDate { get { return DateOf(deTo); } }

        /// <summary>03 §9.3 — `전체` 는 EditValue 가 null 이다.</summary>
        public string StatusCode { get { return cboStatus.EditValue as string; } }

        public string ChartNo { get { return txtChartNo.Text; } }
        public string PatientName { get { return txtName.Text; } }

        public string ContextTitle
        {
            set { lcgList.Text = value; }
        }

        public string ValidationMessage
        {
            set { lblValidation.Text = value ?? string.Empty; }
        }

        public IList<WorkListItemDto> Rows
        {
            set { _picker.Rebind(gcWorkList, value); }
        }

        public WorkDetailDto Detail
        {
            set
            {
                txtDetailChartNo.Text = value == null ? string.Empty : value.ChartNo;
                txtDetailName.Text = value == null ? string.Empty : value.Name;
                txtDetailBirthGender.Text = value == null
                    ? string.Empty
                    : Pair(clsPatientText.FormatBirthday(value.Birthday), clsPatientText.FormatGender(value.Gender));
                txtDetailReserveDate.Text = value == null
                    ? string.Empty
                    : clsWorkText.FormatDate(value.ReserveDate);
                txtDetailSlot.Text = value == null ? string.Empty : clsWorkText.FormatSlot(value.SlotCode);
                txtDetailCapacity.Text = value == null
                    ? string.Empty
                    : clsWorkText.FormatCapacity(value.CurrentCount, value.Capacity, value.RemainingSeats);
                txtDetailStatus.Text = value == null ? string.Empty : value.StatusName;
            }
        }

        public IList<WorkExamItemDto> NexItems
        {
            set { gcNexList.DataSource = value; }
        }

        public IList<WorkExamItemDto> AexItems
        {
            set { gcAexList.DataSource = value; }
        }

        public WorkActionState Actions
        {
            set
            {
                EventHandler<WorkActionState> handler = WorkActionsChanged;
                if (handler != null)
                {
                    handler(this, value ?? WorkActionState.None());
                }
            }
        }

        /// <summary>
        /// 03 §9.1 · §23.2 — Ribbon 의 업무 Action 이 대상으로 삼는 행. Grid 가 잡아 둔 행과
        /// 사용자가 고른 행은 다르다 — 가르는 것은 `clsGridRowPicker` 다.
        /// </summary>
        public long? SelectedWorkId
        {
            get
            {
                var row = _picker.Row as WorkListItemDto;
                return row == null ? (long?)null : row.WorkId;
            }
        }

        public void ShowMessage(string message)
        {
            Form owner = FindForm();
            XtraMessageBox.Show(owner, message, owner == null ? string.Empty : owner.Text);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            RaiseSearchRequested();
        }

        /// <summary>
        /// 조회조건 칸에서 Enter 를 치면 `[조회]` 와 같은 일이 난다 (WF-PAT-01 과 같다).
        ///
        /// [X] `Form.AcceptButton` 을 쓰지 않는다. 그 속성은 폼이 갖는데 이 화면은 UserControl 이고,
        ///     MainForm 에 걸면 어느 업무 화면이 떠 있든 이 버튼이 Enter 를 먹는다.
        /// </summary>
        private void SearchInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            // Enter 가 위로 새어 나가면 상위 폼이 한 번 더 받는다.
            e.Handled = true;
            e.SuppressKeyPress = true;
            RaiseSearchRequested();
        }

        // 두 드롭다운은 무엇이 골라졌는지를 적지 않고 늘 제 이름을 적는다 (WF-PAT-01 과 같다) —
        // 안에 든 것은 체크 목록이지 값이 아니다.
        private void cboConditions_QueryDisplayText(object sender, QueryDisplayTextEventArgs e)
        {
            e.DisplayText = clsSearchConditions.Caption;
        }

        private void cboColumns_QueryDisplayText(object sender, QueryDisplayTextEventArgs e)
        {
            e.DisplayText = clsColumnChooser.Caption;
        }

        private void clbConditions_ItemCheck(object sender, DevExpress.XtraEditors.Controls.ItemCheckEventArgs e)
        {
            if (_conditions != null) { _conditions.Toggle(e); }
        }

        private void clbColumns_ItemCheck(object sender, DevExpress.XtraEditors.Controls.ItemCheckEventArgs e)
        {
            if (_columns != null) { _columns.Toggle(e); }
        }

        private void btnColumnsDefault_Click(object sender, EventArgs e)
        {
            if (_columns != null) { _columns.RestoreDefault(); }
        }

        /// <summary>
        /// 조회는 UI 스레드에서 동기로 SP 를 부르고, 그동안 쌓인 클릭은 끝난 뒤 발화한다.
        /// `clsBusyScope` 는 잠긴 것을 **보이게** 하고 `_searching` 은 그 사이 들어온
        /// Enter·연타를 막는다 — 둘 다 필요하다 (WF-PAT-01 과 같은 이유).
        /// </summary>
        private void RaiseSearchRequested()
        {
            EventHandler handler = SearchRequested;
            if (handler == null || _searching) { return; }

            _searching = true;
            try
            {
                using (new clsBusyScope(this, btnSearch))
                {
                    handler(this, EventArgs.Empty);
                }
            }
            finally
            {
                _searching = false;
            }
        }

        private void gvWorkList_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            if (_picker != null) { _picker.FocusedRowChanged(e.FocusedRowHandle); }
        }

        private void gvWorkList_RowClick(object sender, DevExpress.XtraGrid.Views.Grid.RowClickEventArgs e)
        {
            if (_picker != null) { _picker.RowClick(e.RowHandle); }
        }

        /// <summary>03 §9.5 — 행 선택 즉시 우측 상세를 갱신한다. 판정은 Presenter 가 한다.</summary>
        private void Picker_PickChanged(object sender, EventArgs e)
        {
            var row = _picker.Row as WorkListItemDto;
            EventHandler<long?> handler = SelectionChanged;
            if (handler != null)
            {
                handler(this, row == null ? (long?)null : row.WorkId);
            }
        }

        private void gvWorkList_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            // DB 가 주는 값과 화면 표기가 다른 넷. 값 자체는 바꾸지 않는다.
            if (e.Column == colSlot)
            {
                e.DisplayText = clsWorkText.FormatSlot(e.Value as string);
            }
            else if (e.Column == colBirthday)
            {
                e.DisplayText = clsPatientText.FormatBirthday(e.Value as string);
            }
            else if (e.Column == colGender)
            {
                e.DisplayText = clsPatientText.FormatGender(e.Value as string);
            }
            else if (e.Column == colMobilePhone)
            {
                e.DisplayText = clsPatientText.FormatPhone(e.Value as string);
            }
        }

        /// <summary>
        /// 03 §9.3 — 비어 있는 날짜 칸은 조건이 아니다. `EditValue` 가 날짜가 아니면 미입력이다.
        /// </summary>
        private static DateTime? DateOf(DateEdit editor)
        {
            return editor.EditValue is DateTime ? (DateTime?)editor.DateTime.Date : null;
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
