// 화면 ID: DLG-PAT-03 — 중복 후보 확인 (03 §6.5)
using System;
using System.Collections.Generic;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// DLG-PAT-03 중복 후보 확인 Modal (03 §6.5). 배치의 출처는
    /// `tools/docgen/wireframe/screens/dlg_pat_03.js` 다 — 위 `입력값` 한 줄, 가운데 후보
    /// 목록, 아래 안내문과 버튼 셋이다.
    ///
    /// **SP 를 부르지 않는다.** `SP-PAT-03` 이 `203` 과 함께 준 RS1 을 그리고 사용자가 고른
    /// 갈래만 돌려준다 (07 §3.3). 그래서 Presenter 도 Service 도 없다 (킷 §2 — 필요한 계층만).
    /// </summary>
    public partial class FrmPatientDuplicate : XtraForm
    {
        partial void ConfigureUI();

        public FrmPatientDuplicate()
        {
            InitializeComponent();
            ConfigureUI();
        }

        /// <summary>기본값은 `닫기` 다 — 창을 그냥 닫아도 Editor 로 돌아간다 (03 §6.5).</summary>
        public DuplicateChoice Choice { get; private set; }

        public void Bind(PatientSaveResultDto input, IList<PatientSaveResultDto> candidates)
        {
            txtName.Text = input.Name;
            txtBirthday.Text = clsPatientText.FormatBirthday(input.Birthday);
            txtSocialNumber.Text = clsPatientText.FormatSocialNumber(input.SocialNumber);
            txtMobilePhone.Text = input.MobilePhone;

            gcCandidates.DataSource = candidates;
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            Finish(DuplicateChoice.Modify);
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            Finish(DuplicateChoice.Continue);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Finish(DuplicateChoice.Close);
        }

        private void Finish(DuplicateChoice choice)
        {
            Choice = choice;
            Close();
        }

        private void gvCandidates_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
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
