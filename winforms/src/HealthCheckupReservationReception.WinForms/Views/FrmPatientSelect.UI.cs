// 화면 ID: DLG-PAT-02 — 수검자 선택 (03 §7)
using DevExpress.Utils;

namespace HealthCheckupReservationReception.Views
{
    public partial class FrmPatientSelect
    {
        /// <summary>
        /// Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance 와, 데이터에서 만드는 목록 (킷 §5).
        ///
        /// **WF-PAT-01 의 ConfigureUI 와 같은 것을 같은 순서로 한다.** `03` §7.2 가 조회계약을
        /// §5.3 에 위임하므로 둘이 갈리면 그 위임이 거짓이 된다.
        /// </summary>
        partial void ConfigureUI()
        {
            // 차트번호만 좌측 정렬이고 나머지 다섯은 가운데다.
            clsGridColumns.Align(colChartNo, HorzAlignment.Near);
            clsGridColumns.Align(colName, HorzAlignment.Center);
            clsGridColumns.Align(colSocialNumber, HorzAlignment.Center);
            clsGridColumns.Align(colBirthday, HorzAlignment.Center);
            clsGridColumns.Align(colGender, HorzAlignment.Center);
            clsGridColumns.Align(colMobilePhone, HorzAlignment.Center);

            // 03 §18 — 컬럼을 숨기는 길은 [컬럼 설정] 드롭다운 하나뿐이다.
            gvPatientList.OptionsCustomization.AllowQuickHideColumns = false;

            clsGridColumns.ShowEmptyText(gvPatientList, "조회 결과가 없습니다. [신규등록] 으로 새 수검자를 만들 수 있습니다.");

            clsSearchConditions.SetupBirthday(deBirthday);

            _picker = new clsGridRowPicker(gvPatientList);
            _picker.PickChanged += Picker_PickChanged;

            // 03 §5.3 조회조건 다섯. WF-PAT-01 과 같은 목록·같은 기본값이다.
            _conditions = new clsSearchConditions(clbConditions);
            _conditions.Add("차트번호", true, lciChartNo, txtChartNo);
            _conditions.Add("이름", true, lciName, txtName);
            _conditions.Add("주민번호", true, lciSocialNumber, txtSocialNumber);
            _conditions.Add("생년월일", false, lciBirthday, deBirthday);
            _conditions.Add("휴대전화", false, lciMobilePhone, txtMobilePhone);

            _columns = new clsColumnChooser(clbColumns, gvPatientList);
        }
    }
}
