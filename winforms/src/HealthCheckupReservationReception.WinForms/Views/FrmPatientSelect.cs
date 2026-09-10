// 화면 ID: DLG-PAT-02 — 수검자 선택 (03 §7)
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
    /// DLG-PAT-02 수검자 선택 Modal (03 §7).
    ///
    /// **조회부는 WF-PAT-01 과 같은 것이다.** `03` §7.2 가 조회계약을 §5.3 에 위임했는데
    /// 코드는 오래 각자 가지고 있었다 — 2026-09-10 grilling 2회차에서 같은 부품 위로 옮겼다
    /// (`clsSearchConditions` · `clsColumnChooser` · `clsGridRowPicker` · `clsGridColumns`).
    /// 조회 한 줄의 꼴과 순서도 같다: 입력칸 다섯 · `[조회]` · `[조회 조건]` · `[컬럼 설정]`.
    ///
    /// 이 창은 상세를 읽지 않는다 — `PatientId` 하나만 호출 화면에 돌려준다 (03 §7.2).
    /// </summary>
    public partial class FrmPatientSelect : XtraForm, IPatientSelectView
    {
        private readonly PatientSelectPresenter _presenter;
        private readonly IPatientService _service;
        private readonly string _operatorName;

        private clsGridRowPicker _picker;
        private clsSearchConditions _conditions;
        private clsColumnChooser _columns;

        // 조회 재진입 가드. 동기 SP 호출 동안 쌓인 클릭이 되돌아오는 것을 막는다.
        private bool _searching;

        /// <summary>
        /// VS 디자이너 전용. 매개변수 없는 생성자가 없으면 디자이너가 화면을 못 연다
        /// (<see cref="MainForm"/> 의 같은 생성자 주석 참조). Presenter 를 만들지 않는다.
        /// </summary>
        public FrmPatientSelect()
        {
            InitializeComponent();
            ConfigureUI();
        }

        public FrmPatientSelect(IPatientService service, string operatorName)
        {
            InitializeComponent();
            ConfigureUI();

            _service = service;
            _operatorName = operatorName;
            _presenter = new PatientSelectPresenter(this, service);
        }

        partial void ConfigureUI();

        /// <summary>03 §7.2 — 호출 화면이 받아 가는 값. 취소면 null 이다.</summary>
        public long? SelectedPatientId { get; private set; }

        public event EventHandler SearchRequested;
        public event EventHandler<long?> SelectionChanged;

        public string ChartNo { get { return txtChartNo.Text; } }

        // `Name` 은 Control 이 이미 갖는 이름이다 — 명시적 구현으로 가른다.
        string IPatientSelectView.Name { get { return txtName.Text; } }

        public string SocialNumber { get { return txtSocialNumber.Text; } }

        // 달력 칸 → yyyyMMdd. 그 규칙은 WF-PAT-01 과 한 벌이다 (clsSearchConditions).
        public string Birthday { get { return clsSearchConditions.BirthdayOf(deBirthday); } }

        public string MobilePhone { get { return txtMobilePhone.Text; } }

        public IList<PatientListItemDto> Rows
        {
            set { _picker.Rebind(gcPatientList, value); }
        }

        public bool SelectEnabled { set { btnSelect.Enabled = value; } }

        public void ShowMessage(string message)
        {
            XtraMessageBox.Show(this, message, Text);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            RaiseSearchRequested();
        }

        /// <summary>
        /// 조회조건 칸에서 Enter 를 치면 `[조회]` 와 같은 일이 난다 — 세 화면이 같다.
        ///
        /// [X] `Form.AcceptButton` 을 쓰지 않는다. 그것을 걸면 Enter 가 `[선택]` 으로 가서
        ///     조회조건을 치다 말고 창이 닫힌다.
        /// </summary>
        private void SearchInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            RaiseSearchRequested();
        }

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

        // 두 드롭다운은 무엇이 골라졌는지를 적지 않고 늘 제 이름을 적는다 — 세 화면이 같다.
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
        /// 03 §7.2 — 결과가 없으면 `[신규등록]` 으로 DLG-PAT-01 New 에 들어간다. 저장되면
        /// **재검색을 요구하지 않고** 곧바로 PatientId 를 돌려주고, 취소하면 이 창이 유지된다.
        /// </summary>
        private void btnNew_Click(object sender, EventArgs e)
        {
            using (var editor = new FrmPatientEditor(_service, _operatorName, null))
            {
                if (editor.ShowDialog(this) != DialogResult.OK || editor.SavedPatientId == null)
                {
                    return;
                }

                Finish(editor.SavedPatientId);
            }
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            var row = _picker.Row as PatientListItemDto;
            if (row == null)
            {
                return;
            }

            Finish(row.PatientId);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            SelectedPatientId = null;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void Finish(long? patientId)
        {
            SelectedPatientId = patientId;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void gvPatientList_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            if (_picker != null) { _picker.FocusedRowChanged(e.FocusedRowHandle); }
        }

        private void gvPatientList_RowClick(object sender, DevExpress.XtraGrid.Views.Grid.RowClickEventArgs e)
        {
            if (_picker != null) { _picker.RowClick(e.RowHandle); }
        }

        /// <summary>03 §7.2 — 행이 잡혀 있어야 `[선택]` 이 열린다. 판정은 Presenter 가 한다.</summary>
        private void Picker_PickChanged(object sender, EventArgs e)
        {
            var row = _picker.Row as PatientListItemDto;
            EventHandler<long?> handler = SelectionChanged;
            if (handler != null)
            {
                handler(this, row == null ? (long?)null : row.PatientId);
            }
        }

        private void gvPatientList_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            // 03 §5.6 No 7~9 — 값 자체는 바꾸지 않고 표기만 바꾼다. 주민번호는 마스킹하지 않는다.
            if (e.Column == colBirthday)
            {
                e.DisplayText = clsPatientText.FormatBirthday(e.Value as string);
            }
            else if (e.Column == colGender)
            {
                e.DisplayText = clsPatientText.FormatGender(e.Value as string);
            }
            else if (e.Column == colSocialNumber)
            {
                e.DisplayText = clsPatientText.FormatSocialNumber(e.Value as string);
            }
            else if (e.Column == colMobilePhone)
            {
                e.DisplayText = clsPatientText.FormatPhone(e.Value as string);
            }
        }
    }
}
