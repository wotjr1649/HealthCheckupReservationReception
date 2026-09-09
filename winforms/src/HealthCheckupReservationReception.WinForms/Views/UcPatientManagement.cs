// 화면 ID: WF-PAT-01 — 수검자 관리 (03 §5)
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// WF-PAT-01 수검자 관리 업무 Tab (03 §5). 배치의 출처는
    /// `tools/docgen/wireframe/screens/wf_pat_01.js` 다 — 조회조건 밴드 위, 좌 목록 0.62 ·
    /// 우 상세 0.38 이다. 판정은 하지 않는다: 그리고 이벤트만 올린다 (킷 §2).
    /// </summary>
    public partial class UcPatientManagement : XtraUserControl, IPatientManagementView
    {
        private PatientManagementPresenter _presenter;

        // 목록을 다시 실으면 Grid 가 0행을 자동으로 잡는다. 그 잠깐의 선택이 Presenter 까지
        // 올라가면 곧바로 지울 상세를 한 번 조회하게 된다 (03 §5.3 재조회 시 선택행 초기화).
        private bool _suppressSelection;

        // 지금 화면이 "행이 골라진" 꼴로 보이는가. ShowSelection 이 유일한 쓰기 지점이다.
        private bool _rowPicked;

        // [R17] 조회 재진입 가드. Ribbon 과 화면 버튼이 같은 Action 을 부른다.
        private bool _searching;

        // 03 §18 `기본값 복원` 이 되돌릴 자리. Designer 가 직렬화한 그 상태다.
        private readonly MemoryStream _defaultLayout = new MemoryStream();

        partial void ConfigureUI();

        public UcPatientManagement()
        {
            InitializeComponent();
            ConfigureUI();
            gvPatientList.SaveLayoutToStream(_defaultLayout);
        }

        /// <summary>
        /// Presenter 를 붙인다. UserControl 은 디자이너가 만들어야 하므로 생성자로 받지 않는다
        /// (킷 `references/mvp-wiring.md`).
        /// </summary>
        public void Attach(IPatientService service)
        {
            _presenter = new PatientManagementPresenter(this, service);
        }

        /// <summary>
        /// [R16] 03 §5.3 — 화면을 열면 조건 없이 한 번 조회해 목록을 채운다.
        ///
        /// `Attach` 가 Presenter 를 먼저 만들고 MainForm 이 그 뒤에 Tab 에 얹으므로
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
        public event EventHandler<bool> RowActionsChanged;

        public string ChartNo { get { return txtChartNo.Text; } }

        // `Name` 은 Control 이 이미 갖는 이름이다. 그대로 구현하면 Designer 의
        // `this.Name = "UcPatientManagement"` 가 조회조건을 가리키게 된다 — 명시적 구현으로 가른다.
        string IPatientManagementView.Name { get { return txtName.Text; } }

        public string SocialNumber { get { return txtSocialNumber.Text; } }

        public string Birthday
        {
            get
            {
                object value = deBirthday.EditValue;
                if (value == null || value == DBNull.Value)
                {
                    return null;
                }

                DateTime picked = deBirthday.DateTime;
                return picked == DateTime.MinValue ? null : picked.ToString("yyyyMMdd");
            }
        }

        public string MobilePhone { get { return txtMobilePhone.Text; } }

        public IList<PatientListItemDto> Rows
        {
            set
            {
                _suppressSelection = true;
                try
                {
                    gcPatientList.DataSource = value;
                }
                finally
                {
                    _suppressSelection = false;
                }

                ShowSelection(false);
            }
        }

        /// <summary>
        /// 03 §5.3 · §5.5 — 재조회하면 선택행이 없다.
        ///
        /// [X] GridView 는 행이 있으면 반드시 하나를 focus 한다.
        ///     `FocusedRowHandle = InvalidRowHandle` 은 대입 직후 다시 0 이 된다(측정값).
        ///     그래서 focus 를 없애는 대신 **선택으로 보이는 것**을 끈다 — 사용자가 행을
        ///     고르는 순간(<see cref="gvPatientList_RowClick"/> · FocusedRowChanged) 켠다.
        /// </summary>
        private void ShowSelection(bool on)
        {
            _rowPicked = on;
            gvPatientList.OptionsSelection.EnableAppearanceFocusedRow = on;
            gvPatientList.OptionsSelection.EnableAppearanceFocusedCell = on;
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

        public bool RowSelected
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

        /// <summary>
        /// 03 §5.2 — `[정보수정]`·`[변경이력]` 이 대상으로 삼는 행. 판정은 Presenter 가 하고
        /// Ribbon 을 가진 MainForm 이 이 값을 읽는다. **선택으로 보이지 않는 동안은 대상이
        /// 없다** — Grid 가 잡아 둔 행과 사용자가 고른 행은 다르다 (07 §14.3 A-10).
        /// </summary>
        public long? SelectedPatientId
        {
            get
            {
                if (!_rowPicked)
                {
                    return null;
                }

                var row = gvPatientList.GetFocusedRow() as PatientListItemDto;
                return row == null ? (long?)null : row.PatientId;
            }
        }

        public void ShowMessage(string message)
        {
            Form owner = FindForm();
            XtraMessageBox.Show(owner, message, owner == null ? string.Empty : owner.Text);
        }

        /// <summary>
        /// Ribbon 의 `[조회]` 도 조회조건의 `[조회]` 와 같은 Action 이다 (03 §5.2 · §5.3).
        /// </summary>
        public void RequestSearch()
        {
            RaiseSearchRequested();
        }

        /// <summary>
        /// 03 §5.5 · §18 — `[컬럼설정]`. 표시/숨김과 기본값 복원, 그 둘뿐이다.
        /// </summary>
        public void ShowColumnChooser()
        {
            using (var chooser = new FrmColumnChooser())
            {
                chooser.Bind(gvPatientList, RestoreDefaultColumns);
                chooser.ShowDialog(FindForm());
            }
        }

        /// <summary>
        /// 03 §18 `기본값 복원`. 무엇이 기본인지는 Designer 가 직렬화한 그 상태이므로
        /// 화면이 서는 순간 한 벌 떠 둔다 — 어느 컬럼이 기본인지를 코드에 다시 적으면
        /// Designer 와 두 곳이 된다 (ROOT AGENTS.md §6).
        /// </summary>
        private void RestoreDefaultColumns()
        {
            _defaultLayout.Position = 0;
            gvPatientList.RestoreLayoutFromStream(_defaultLayout);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            RaiseSearchRequested();
        }

        /// <summary>
        /// [R17] 조회는 UI 스레드에서 동기로 SP 를 부르고, 그동안 쌓인 클릭은 끝난 뒤 발화한다.
        ///
        /// **가드가 둘 다 필요하다.** `[조회]` 는 화면 버튼과 Ribbon 버튼 둘이 같은 Action 을
        /// 부르므로 버튼 하나만 잠그면 다른 쪽이 열려 있다 — `_searching` 이 그 구멍을 닫고,
        /// `clsBusyScope` 는 사용자가 잠긴 것을 **보게** 한다 (2026-09-10 사용자 결정).
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
        private void gvPatientList_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            Pick(e.FocusedRowHandle);
        }

        // 03 §5.5 — 행 선택 즉시 우측 상세를 갱신한다. 조회 직후 이미 focus 되어 있는
        // 첫 행을 사용자가 처음 누르는 경우는 FocusedRowChanged 가 나지 않아 여기서만 잡힌다.
        private void gvPatientList_RowClick(object sender, DevExpress.XtraGrid.Views.Grid.RowClickEventArgs e)
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

            var row = gvPatientList.GetRow(rowHandle) as PatientListItemDto;
            ShowSelection(row != null);

            EventHandler<long?> handler = SelectionChanged;
            if (handler != null)
            {
                handler(this, row == null ? (long?)null : row.PatientId);
            }
        }

        private void gvPatientList_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            // 03 §5.6 — DB 가 주는 값과 화면 표기가 다른 세 컬럼. 값 자체는 바꾸지 않는다.
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
