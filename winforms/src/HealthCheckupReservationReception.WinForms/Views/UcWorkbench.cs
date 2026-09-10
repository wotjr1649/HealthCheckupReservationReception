// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Columns;
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
    /// 03 과 와이어프레임은 배치를 구속하지 않는다 (ROOT AGENTS.md §1.1, 2026-09-10 사용자 결정) —
    /// 필드의 뜻과 업무 규칙만 거기서 온다.
    /// </summary>
    public partial class UcWorkbench : XtraUserControl, IWorkbenchView
    {
        private WorkbenchPresenter _presenter;

        // 목록을 다시 실으면 Grid 가 0행을 자동으로 잡는다. 그 잠깐의 선택이 Presenter 까지
        // 올라가면 곧바로 지울 상세를 한 번 조회하게 된다 (03 §9.4 재조회 시 선택 Clear).
        private bool _suppressSelection;

        // 지금 화면이 "행이 골라진" 꼴로 보이는가. ShowSelection 이 유일한 쓰기 지점이다.
        private bool _rowPicked;

        // 조회 재진입 가드. 동기 SP 호출 동안 쌓인 클릭이 되돌아오는 것을 막는다.
        private bool _searching;

        // 체크 목록을 코드가 고쳐 쓰는 동안 ItemCheck 이 되돌아오는 것을 막는다.
        private bool _syncingChecks;

        // 03 §18 `기본값 복원` 이 되돌릴 자리. Designer 가 직렬화한 그 상태다.
        private readonly MemoryStream _defaultLayout = new MemoryStream();

        partial void ConfigureUI();

        public UcWorkbench()
        {
            InitializeComponent();
            ConfigureUI();
            gvWorkList.SaveLayoutToStream(_defaultLayout);
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
        /// 화면을 열면 한 번 조회한다. `[조회]` 이벤트를 올리지 않는다 — 그 경로는 실패를
        /// 모달로 알리는데, 사용자가 부탁하지 않은 호출이 창을 열자마자 오류창을 띄우면 안 된다.
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
            set
            {
                _suppressSelection = true;
                try
                {
                    gcWorkList.DataSource = value;
                }
                finally
                {
                    _suppressSelection = false;
                }

                ShowSelection(false);
            }
        }

        /// <summary>
        /// 03 §9.4 — 재조회하면 선택행이 없다.
        ///
        /// [X] GridView 는 행이 있으면 반드시 하나를 focus 한다.
        ///     `FocusedRowHandle = InvalidRowHandle` 은 대입 직후 다시 0 이 된다(WF-PAT-01 실측).
        ///     그래서 focus 를 없애는 대신 **선택으로 보이는 것**을 끈다.
        /// </summary>
        private void ShowSelection(bool on)
        {
            _rowPicked = on;
            gvWorkList.OptionsSelection.EnableAppearanceFocusedRow = on;
            gvWorkList.OptionsSelection.EnableAppearanceFocusedCell = on;
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
        /// 03 §9.1 · §23.2 — Ribbon 의 업무 Action 이 대상으로 삼는 행. **선택으로 보이지
        /// 않는 동안은 대상이 없다** — Grid 가 잡아 둔 행과 사용자가 고른 행은 다르다.
        /// </summary>
        public long? SelectedWorkId
        {
            get
            {
                if (!_rowPicked)
                {
                    return null;
                }

                var row = gvWorkList.GetFocusedRow() as WorkListItemDto;
                return row == null ? (long?)null : row.WorkId;
            }
        }

        public void ShowMessage(string message)
        {
            Form owner = FindForm();
            XtraMessageBox.Show(owner, message, owner == null ? string.Empty : owner.Text);
        }

        /// <summary>
        /// 03 §18 `기본값 복원`. 무엇이 기본인지는 Designer 가 직렬화한 그 상태이므로
        /// 화면이 서는 순간 한 벌 떠 둔다 — 어느 컬럼이 기본인지를 코드에 다시 적으면
        /// Designer 와 두 곳이 된다 (ROOT AGENTS.md §6).
        /// </summary>
        private void RestoreDefaultColumns()
        {
            _defaultLayout.Position = 0;
            gvWorkList.RestoreLayoutFromStream(_defaultLayout);
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

        // 무엇이 골라졌는지를 적지 않고 늘 제 이름을 적는다 (WF-PAT-01 과 같다) —
        // 안에 든 것은 체크 목록이지 값이 아니다.
        private void cboColumns_QueryDisplayText(object sender, QueryDisplayTextEventArgs e)
        {
            e.DisplayText = "컬럼 설정";
        }

        /// <summary>
        /// 03 §18 컬럼설정. 확인/취소를 두지 않는다 — 되돌리는 길이 `[기본값 복원]` 하나로 충분하다.
        /// </summary>
        private void clbColumns_ItemCheck(object sender, DevExpress.XtraEditors.Controls.ItemCheckEventArgs e)
        {
            if (_syncingChecks) { return; }

            _syncingChecks = true;
            try
            {
                clbColumns.Items[e.Index].CheckState = e.State;
                var column = clbColumns.Items[e.Index].Value as GridColumn;
                if (column != null)
                {
                    column.Visible = e.State == CheckState.Checked;
                }
            }
            finally { _syncingChecks = false; }
        }

        private void btnColumnsDefault_Click(object sender, EventArgs e)
        {
            RestoreDefaultColumns();
            SyncColumnChecks();
        }

        /// <summary>
        /// 체크 상태를 Grid 에서 받아 적는다. **Grid 가 단일 출처다** (ROOT AGENTS.md §6) —
        /// `기본값 복원` 은 Grid 를 되돌리므로 목록도 거기서 다시 읽어야 어긋나지 않는다.
        /// </summary>
        private void SyncColumnChecks()
        {
            _syncingChecks = true;
            try
            {
                for (int i = 0; i < clbColumns.Items.Count; i++)
                {
                    var column = clbColumns.Items[i].Value as GridColumn;
                    clbColumns.Items[i].CheckState = column != null && column.Visible
                        ? CheckState.Checked
                        : CheckState.Unchecked;
                }
            }
            finally { _syncingChecks = false; }
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

        // 키보드 이동. 이미 focus 된 행을 다시 눌렀을 때는 나지 않으므로 RowClick 이 짝을 이룬다.
        private void gvWorkList_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            Pick(e.FocusedRowHandle);
        }

        // 조회 직후 이미 focus 되어 있는 첫 행을 사용자가 처음 누르는 경우는
        // FocusedRowChanged 가 나지 않아 여기서만 잡힌다.
        private void gvWorkList_RowClick(object sender, DevExpress.XtraGrid.Views.Grid.RowClickEventArgs e)
        {
            if (!_rowPicked)
            {
                Pick(e.RowHandle);
            }
        }

        private void Pick(int rowHandle)
        {
            if (_suppressSelection)
            {
                return;
            }

            var row = gvWorkList.GetRow(rowHandle) as WorkListItemDto;
            ShowSelection(row != null);

            EventHandler<long?> handler = SelectionChanged;
            if (handler != null)
            {
                handler(this, row == null ? (long?)null : row.WorkId);
            }
        }

        private void gvWorkList_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            // DB 가 주는 값과 화면 표기가 다른 셋. 값 자체는 바꾸지 않는다.
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
