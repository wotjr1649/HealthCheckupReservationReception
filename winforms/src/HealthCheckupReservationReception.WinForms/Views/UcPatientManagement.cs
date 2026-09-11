// 화면 ID: WF-PAT-01 — 수검자 관리 (03 §5)
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
    /// WF-PAT-01 수검자 관리 업무 화면 (03 §5). 판정은 하지 않는다: 그리고 이벤트만 올린다 (킷 §2).
    ///
    /// 배치는 `lcMain` LayoutControl 하나가 갖는다 — 조회조건은 `수검자 목록` 그룹 안의 한 줄이고,
    /// 좌 목록 · 우 상세는 `splitPatient` 로 나뉜다. 03 과 와이어프레임은 더 이상 배치를 구속하지
    /// 않는다 (ROOT AGENTS.md §1.1, 2026-09-10 사용자 결정) — 필드의 뜻만 거기서 온다.
    ///
    /// 조회조건 드롭다운 · 컬럼설정 드롭다운 · 행 선택 판정은 WF-WRK-01 과 같은 부품을 쓴다
    /// (`clsSearchConditions` · `clsColumnChooser` · `clsGridRowPicker`, 킷 §2).
    /// </summary>
    public partial class UcPatientManagement : XtraUserControl, IPatientManagementView
    {
        private PatientManagementPresenter _presenter;

        private clsGridRowPicker _picker;
        private clsSearchConditions _conditions;
        private clsColumnChooser _columns;

        // [R17] 조회 재진입 가드. 동기 SP 호출 동안 쌓인 클릭이 되돌아오는 것을 막는다.
        private bool _searching;

        partial void ConfigureUI();

        public UcPatientManagement()
        {
            InitializeComponent();
            ConfigureUI();
        }

        /// <summary>
        /// Presenter 를 붙인다. UserControl 은 디자이너가 만들어야 하므로 생성자로 받지 않는다
        /// (킷 `references/mvp-wiring.md`).
        /// </summary>
        public void Attach(IPatientService service, IWorkService workService, ICommonStatusService statusService)
        {
            _presenter = new PatientManagementPresenter(this, service, workService, statusService);
        }

        /// <summary>
        /// [R16] 03 §5.3 — 화면을 열면 조건 없이 한 번 조회해 목록을 채운다.
        ///
        /// `Attach` 가 Presenter 를 먼저 만들고 MainForm 이 그 뒤에 화면을 얹으므로
        /// `OnLoad` 시점에는 배선이 이미 끝나 있다.
        ///
        /// [X] **`SearchRequested` 를 올리지 않는다.** 그 경로는 실패를 모달로 알리는데,
        ///     사용자가 부탁하지 않은 호출이 창을 열자마자 오류창을 띄우면 안 된다.
        ///     `LoadInitial` 이 같은 조회를 조용히 한다 (Presenter 주석 참조).
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
        /// 03 §5.2 의 Context Ribbon 은 MainForm 이 갖는다. 행 선택 판정은 Presenter 가 하고
        /// 여기서는 그 판정을 Ribbon 을 가진 쪽으로 넘기기만 한다.
        /// </summary>
        public event EventHandler<PatientActionState> RowActionsChanged;

        public string ChartNo { get { return txtChartNo.Text; } }

        // `Name` 은 Control 이 이미 갖는 이름이다. 그대로 구현하면 Designer 의
        // `this.Name = "UcPatientManagement"` 가 조회조건을 가리키게 된다 — 명시적 구현으로 가른다.
        string IPatientManagementView.Name { get { return txtName.Text; } }

        public string SocialNumber { get { return txtSocialNumber.Text; } }

        // 03 §5.3 · §7.2 — 달력 칸의 값을 조회조건이 쓰는 yyyyMMdd 로 바꾼다.
        // 그 규칙은 DLG-PAT-02 와 한 벌이다 (clsSearchConditions).
        public string Birthday { get { return clsSearchConditions.BirthdayOf(deBirthday); } }

        public string MobilePhone { get { return txtMobilePhone.Text; } }

        public IList<PatientListItemDto> Rows
        {
            set { _picker.Rebind(gcPatientList, value); }
        }

        public PatientDetailDto Detail
        {
            set
            {
                txtDetailChartNo.Text = value == null ? string.Empty : value.ChartNo;
                txtDetailName.Text = value == null ? string.Empty : value.Name;
                txtDetailSocialNumber.Text = value == null ? string.Empty : clsPatientText.FormatSocialNumber(value.SocialNumber);
                txtDetailBirthGender.Text = value == null
                    ? string.Empty
                    : Pair(clsPatientText.FormatBirthday(value.Birthday), clsPatientText.FormatGender(value.Gender));
                txtDetailMobilePhone.Text = value == null ? string.Empty
                    : clsPatientText.FormatPhone(value.MobilePhone);
                txtDetailPhoneEmail.Text = value == null ? string.Empty
                    : Pair(clsPatientText.FormatPhone(value.Phone), value.Email);
                txtDetailZipAddress.Text = value == null ? string.Empty : Pair(value.Zipcode, value.Address);
                txtDetailAddressDetail.Text = value == null ? string.Empty : value.AddressDetail;
                memoDetailMemo.Text = value == null ? string.Empty : value.Memo;
            }
        }

        /// <summary>
        /// 여섯째 조회조건 `예약 없는 수검자만`. 앞 다섯과 달리 **SP 로 가지 않는다** —
        /// SP-PAT-01 은 예약을 모르므로 Presenter 가 이어 붙인 뒤 거른다.
        /// </summary>
        public bool ReservableOnly
        {
            get { return _conditions != null && _conditions.IsOn("예약 없는 수검자만"); }
        }

        public string ReserveStatusText
        {
            set { lblDetailReserve.Text = value ?? string.Empty; }
        }

        public string NoticeText
        {
            set { lblNotice.Text = value ?? string.Empty; }
        }

        public void SelectPatient(long patientId)
        {
            for (int i = 0; i < gvPatientList.RowCount; i++)
            {
                var row = gvPatientList.GetRow(i) as PatientListItemDto;
                if (row != null && row.PatientId == patientId)
                {
                    _picker.Select(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 예약이 하나 생겼다 — 목록의 `예약` 칸이 낡았으므로 되읽고 그 줄로 돌아간다.
        /// MainForm 이 저장 뒤에 부른다.
        /// </summary>
        public void ReloadAfterReservation(long patientId, string notice)
        {
            if (_presenter == null)
            {
                return;
            }

            using (new clsBusyScope(this))
            {
                _presenter.Reload(patientId);
            }

            NoticeText = notice;
        }

        public PatientActionState RowActions
        {
            set
            {
                EventHandler<PatientActionState> handler = RowActionsChanged;
                if (handler != null)
                {
                    handler(this, value ?? PatientActionState.None());
                }
            }
        }

        /// <summary>
        /// 03 §5.2 — `[정보수정]`·`[변경이력]` 이 대상으로 삼는 행. 판정은 Presenter 가 하고
        /// Ribbon 을 가진 MainForm 이 이 값을 읽는다. Grid 가 잡아 둔 행과 사용자가 고른 행은
        /// 다르다 (07 §14.3 A-10) — 가르는 것은 `clsGridRowPicker` 다.
        /// </summary>
        public long? SelectedPatientId
        {
            get
            {
                var row = _picker.Row as PatientListItemDto;
                return row == null ? (long?)null : row.PatientId;
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
        /// 조회조건 칸에서 Enter 를 치면 `[조회]` 와 같은 일이 난다 (2026-09-10 사용자 결정).
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

        // 2026-09-10 사용자 결정 — 두 드롭다운은 무엇이 골라졌는지를 적지 않고 늘 제 이름을 적는다.
        // 안에 든 것은 체크 목록이지 값이 아니므로, 값을 요약해 봐야 읽히지 않는다.
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
        /// [R17] 조회는 UI 스레드에서 동기로 SP 를 부르고, 그동안 쌓인 클릭은 끝난 뒤 발화한다.
        ///
        /// **가드가 둘 다 필요하다.** `clsBusyScope` 는 사용자가 잠긴 것을 **보게** 하고,
        /// `_searching` 은 그 사이 들어온 Enter·연타를 막는다 (2026-09-10 사용자 결정).
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

        private void gvPatientList_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            if (_picker != null) { _picker.FocusedRowChanged(e.FocusedRowHandle); }
        }

        private void gvPatientList_RowClick(object sender, DevExpress.XtraGrid.Views.Grid.RowClickEventArgs e)
        {
            if (_picker != null) { _picker.RowClick(e.RowHandle); }
        }

        /// <summary>03 §5.5 — 행 선택 즉시 우측 상세를 갱신한다. 판정은 Presenter 가 한다.</summary>
        private void Picker_PickChanged(object sender, EventArgs e)
        {
            var row = _picker.Row as PatientListItemDto;
            EventHandler<long?> handler = SelectionChanged;
            if (handler != null)
            {
                handler(this, row == null ? (long?)null : row.PatientId);
            }
        }

        public IList<WorkListItemDto> History
        {
            set { gcHistory.DataSource = value; }
        }

        private void gvPatientList_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            // 03 §5.6 — DB 가 주는 값과 화면 표기가 다른 네 컬럼. 값 자체는 바꾸지 않는다.
            if (e.Column == colBirthday)
            {
                e.DisplayText = clsPatientText.FormatBirthday(e.Value as string);
            }
            else if (e.Column == colMobilePhone)
            {
                // 저장 형식이 바뀌기 전 행은 숫자만 들어 있다. 표시에서 한 꼴로 맞춘다.
                e.DisplayText = clsPatientText.FormatPhone(e.Value as string);
            }
            else if (e.Column == colGender)
            {
                e.DisplayText = clsPatientText.FormatGender(e.Value as string);
            }
            else if (e.Column == colSocialNumber)
            {
                e.DisplayText = clsPatientText.FormatSocialNumber(e.Value as string);
            }
        }

        /// <summary>설계가 한 칸에 둘을 넣은 자리다 (`생년월일 / 성별` 등). 한쪽이 비면 남는 쪽만 적는다.</summary>
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
