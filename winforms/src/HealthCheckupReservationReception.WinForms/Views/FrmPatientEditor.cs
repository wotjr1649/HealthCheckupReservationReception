// 화면 ID: DLG-PAT-01 — 수검자 등록·정보수정 (03 §6)
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
    /// DLG-PAT-01 수검자 등록·정보수정 Modal (03 §6). 배치의 출처는
    /// `tools/docgen/wireframe/screens/dlg_pat_01.js` 다 — 라벨 폭 1.30 / 입력 나머지의
    /// 한 줄 구성이고 구획은 기본정보·연락처·주소·검사 제외·메모 다섯이다.
    ///
    /// 판정은 하지 않는다: 값을 내주고 Presenter 가 그린 것을 받는다 (킷 §2).
    /// </summary>
    public partial class FrmPatientEditor : XtraForm, IPatientEditorView
    {
        private readonly PatientEditorPresenter _presenter;

        partial void ConfigureUI();

        /// <summary>
        /// <paramref name="patientId"/> 가 null 이면 New, 값이 있으면 Edit 다 (03 §6.3 · §6.4).
        /// </summary>
        public FrmPatientEditor(IPatientService service, string operatorName, long? patientId)
        {
            InitializeComponent();
            ConfigureUI();
            _presenter = new PatientEditorPresenter(this, service, operatorName, patientId);
        }

        /// <summary>03 §6.3 — 저장 성공 후 호출 화면이 받아 가는 값. 취소면 null 이다.</summary>
        public long? SavedPatientId { get; private set; }

        public string SavedChartNo { get; private set; }

        public event EventHandler ViewLoaded;
        public event EventHandler InputChanged;
        public event EventHandler SaveRequested;

        public bool AutoChartNo
        {
            get { return rgChartMode.EditValue is bool && (bool)rgChartMode.EditValue; }
        }

        public string ChartNo { get { return txtChartNo.Text; } }

        // `Name` 은 Control 이 이미 갖는 이름이다. 명시적 구현으로 가른다 — 그러지 않으면
        // Designer 의 `this.Name = "FrmPatientEditor"` 가 이름 입력칸을 가리키게 된다.
        string IPatientEditorView.Name { get { return txtName.Text; } }

        public string SocialNumber { get { return txtSocialNumber.Text; } }

        public string MobilePhone { get { return txtMobilePhone.Text; } }

        public string Phone { get { return txtPhone.Text; } }

        public string Email { get { return txtEmail.Text; } }

        public string Zipcode { get { return txtZipcode.Text; } }

        public string Address { get { return txtAddress.Text; } }

        public string AddressDetail { get { return txtAddressDetail.Text; } }

        public string Memo { get { return memoMemo.Text; } }

        public bool HepatitisBExcluded { get { return chkHepatitisB.Checked; } }

        public string Birthday { set { txtBirthday.Text = value; } }

        public string Gender { set { txtGender.Text = value; } }

        public bool SaveEnabled { set { btnSave.Enabled = value; } }

        /// <summary>
        /// 03 §6.4 — Edit 에는 차트번호 방식 선택이 없다. 기존 차트번호 수동수정만 허용하므로
        /// 자동발급을 끈 채로 입력칸을 연다.
        /// </summary>
        public void ShowEditMode()
        {
            lblChartMode.Visible = false;
            rgChartMode.Visible = false;
            rgChartMode.EditValue = false;
            ApplyChartMode();
        }

        public void LoadDetail(PatientDetailDto detail)
        {
            txtChartNo.Text = detail.ChartNo;
            txtName.Text = detail.Name;

            // 03 §6.4 — 기존 주민번호는 전체값으로 Load 한다. 마스킹하지 않는다 (§5.6 No 7).
            txtSocialNumber.Text = clsPatientText.FormatSocialNumber(detail.SocialNumber);
            txtMobilePhone.Text = detail.MobilePhone;
            txtPhone.Text = detail.Phone;
            txtEmail.Text = detail.Email;
            txtZipcode.Text = detail.Zipcode;
            txtAddress.Text = detail.Address;
            txtAddressDetail.Text = detail.AddressDetail;
            memoMemo.Text = detail.Memo;

            // 03 §6.2a — Edit 는 SP-PAT-02 가 준 현재값으로 초기화한다.
            chkHepatitisB.Checked = detail.HepatitisBExcluded;
        }

        public void ClearFieldErrors()
        {
            txtChartNo.ErrorText = string.Empty;
            txtName.ErrorText = string.Empty;
            txtSocialNumber.ErrorText = string.Empty;
        }

        public void ShowFieldError(string parameterName, string message)
        {
            BaseEdit editor = EditorOf(parameterName);
            if (editor == null)
            {
                // 05 §16.2 가 정한 입력항목이 아니면 가리킬 Control 이 없다.
                // 삼키지 않고 Blocking 으로 드러낸다 (07 §6.2).
                ShowMessage(message);
                return;
            }

            editor.ErrorText = message;
            editor.Focus();
        }

        public void ShowMessage(string message)
        {
            XtraMessageBox.Show(this, message, Text);
        }

        public bool ConfirmExistingPatient(PatientSaveResultDto existing)
        {
            string body = "동일한 주민등록번호의 수검자가 이미 있습니다."
                + Environment.NewLine + Environment.NewLine
                + "차트번호 : " + existing.ChartNo + Environment.NewLine
                + "이름 : " + existing.Name + Environment.NewLine
                + "주민등록번호 : " + clsPatientText.FormatSocialNumber(existing.SocialNumber) + Environment.NewLine
                + "생년월일 : " + clsPatientText.FormatBirthday(existing.Birthday) + Environment.NewLine
                + "성별 : " + clsPatientText.FormatGender(existing.Gender)
                + Environment.NewLine + Environment.NewLine
                + "이 수검자로 계속하시겠습니까?";

            return XtraMessageBox.Show(this, body, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                == DialogResult.Yes;
        }

        public DuplicateChoice ShowDuplicateCandidates(
            PatientSaveResultDto input, IList<PatientSaveResultDto> candidates)
        {
            using (var dialog = new FrmPatientDuplicate())
            {
                dialog.Bind(input, candidates);
                dialog.ShowDialog(this);
                return dialog.Choice;
            }
        }

        public void CloseWith(long? patientId, string chartNo)
        {
            SavedPatientId = patientId;
            SavedChartNo = chartNo;
            DialogResult = patientId == null ? DialogResult.Cancel : DialogResult.OK;
            Close();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            ApplyChartMode();

            EventHandler handler = ViewLoaded;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 03 §6.3 — 자동발급이면 차트번호를 입력하지 않는다. 번호는 저장 Transaction 이
        /// 확정하므로 화면이 미리 점유하지 않는다.
        /// </summary>
        private void ApplyChartMode()
        {
            // Enabled 가 아니라 ReadOnly 다 — 꺼진 Editor 는 NullValuePrompt 를 그리지 않아
            // `저장 시 자동발급` 안내가 사라진다 (03 §6.1 이 그 문구를 칸 안에 둔다).
            txtChartNo.Properties.ReadOnly = AutoChartNo;
            if (AutoChartNo)
            {
                txtChartNo.EditValue = null;
            }
        }

        private void rgChartMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyChartMode();
        }

        private void Input_EditValueChanged(object sender, EventArgs e)
        {
            EventHandler handler = InputChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            EventHandler handler = SaveRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            CloseWith(null, null);
        }

        private BaseEdit EditorOf(string parameterName)
        {
            if (parameterName == PatientErrorField.ChartNo) { return txtChartNo; }
            if (parameterName == PatientErrorField.Name) { return txtName; }
            if (parameterName == PatientErrorField.SocialNumber) { return txtSocialNumber; }
            return null;
        }
    }
}
