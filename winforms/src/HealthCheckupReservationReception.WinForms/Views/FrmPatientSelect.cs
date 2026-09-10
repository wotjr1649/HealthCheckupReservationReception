// 화면 ID: DLG-PAT-02 — 수검자 선택 (03 §7)
using System;
using System.Collections.Generic;
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
    /// DLG-PAT-02 수검자 선택 Modal (03 §7). 배치의 출처는
    /// `tools/docgen/wireframe/screens/dlg_pat_02.js` 다 — 조회조건 2행과 우측 `[조회] [신규등록]`,
    /// 그 아래 `조회 결과` 목록, 하단 `[선택] [닫기]` 다.
    ///
    /// 이 창은 상세를 읽지 않는다 — `PatientId` 하나만 호출 화면에 돌려준다 (03 §7.2).
    /// </summary>
    public partial class FrmPatientSelect : XtraForm, IPatientSelectView
    {
        private readonly PatientSelectPresenter _presenter;
        private readonly IPatientService _service;
        private readonly string _operatorName;

        // 목록을 다시 실으면 Grid 가 0행을 자동으로 잡는다. 그 선택이 그대로 올라오면
        // 고르지도 않은 행으로 [선택] 이 열린다 (07 §14.3 A-10 과 같은 사정이다).
        private bool _suppressSelection;
        private bool _rowPicked;

        /// <summary>
        /// VS 디자이너 전용. 매개변수 없는 생성자가 없으면 디자이너가 화면을 못 연다
        /// (<see cref="MainForm"/> 의 같은 생성자 주석 참조). Presenter 를 만들지 않는다.
        /// </summary>
        public FrmPatientSelect()
        {
            InitializeComponent();
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

        public bool SelectEnabled { set { btnSelect.Enabled = value; } }

        public void ShowMessage(string message)
        {
            XtraMessageBox.Show(this, message, Text);
        }

        /// <summary>
        /// 07 §14.3 A-10 과 같은 처리다 — `GridView` 는 행이 있으면 반드시 하나를 focus 하므로
        /// focus 를 없애는 대신 **선택으로 보이는 것**을 끈다.
        /// </summary>
        private void ShowSelection(bool on)
        {
            _rowPicked = on;
            gvPatientList.OptionsSelection.EnableAppearanceFocusedRow = on;
            gvPatientList.OptionsSelection.EnableAppearanceFocusedCell = on;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            EventHandler handler = SearchRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
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
            var row = gvPatientList.GetFocusedRow() as PatientListItemDto;
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

        // 키보드 이동. 이미 focus 된 행을 다시 눌렀을 때는 나지 않으므로 RowClick 이 짝을 이룬다.
        private void gvPatientList_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            Pick(e.FocusedRowHandle);
        }

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
        }
    }
}
