// 화면 ID: WF-PAT-01 — 수검자 관리 (03 §5)
using DevExpress.Utils;

namespace HealthCheckupReservationReception.Views
{
    public partial class UcPatientManagement
    {
        /// <summary>
        /// Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance 와, 데이터에서 만드는 목록 (킷 §5).
        /// </summary>
        partial void ConfigureUI()
        {
            // 차트번호만 좌측 정렬이고 나머지 넷은 가운데다.
            clsGridColumns.Align(colChartNo, HorzAlignment.Near);
            clsGridColumns.Align(colName, HorzAlignment.Center);
            clsGridColumns.Align(colBirthday, HorzAlignment.Center);
            clsGridColumns.Align(colGender, HorzAlignment.Center);
            clsGridColumns.Align(colMobilePhone, HorzAlignment.Center);

            // 03 §18 — 컬럼을 숨기는 길은 [컬럼 설정] 드롭다운 하나뿐이다. 헤더를 밖으로 끌어
            // 숨기는 경로를 열어 두면 실수로 사라진 컬럼을 되돌릴 방법을 사용자가 모른다.
            gvPatientList.OptionsCustomization.AllowQuickHideColumns = false;

            clsGridColumns.ShowEmptyText(gvPatientList, "조회 결과가 없습니다.");

            // 부품 셋은 Grid·조회조건 칸이 다 선 **뒤에** 만든다 — 그 시점의 Grid 가
            // `[기본값 복원]` 이 되돌릴 기준이다.
            _picker = new clsGridRowPicker(gvPatientList);
            _picker.PickChanged += Picker_PickChanged;

            clsSearchConditions.SetupBirthday(deBirthday);

            // 03 §5.3 조회조건 다섯. 생년월일·휴대전화는 2026-09-10 에 화면에서 걷었다가
            // grilling 2회차에서 **끌 수 있는 조건**으로 되돌린 것이라 기본은 꺼져 있다 —
            // DLG-PAT-02 와 같은 다섯이고 (03 §7.2) SP-PAT-01 은 처음부터 받고 있었다.
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
