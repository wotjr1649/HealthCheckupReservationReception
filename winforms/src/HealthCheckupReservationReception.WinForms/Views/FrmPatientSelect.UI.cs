// 화면 ID: DLG-PAT-02 — 수검자 선택 (03 §7)
using DevExpress.Utils;

namespace HealthCheckupReservationReception.Views
{
    public partial class FrmPatientSelect
    {
        /// <summary>
        /// Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance 와 DisplayFormat (킷 §5).
        /// </summary>
        partial void ConfigureUI()
        {
            // 설계 dlg_pat_02.js 의 K.cols — 차트번호만 좌측 정렬이고 나머지 다섯은 가운데다.
            clsGridColumns.Align(colChartNo, HorzAlignment.Near);
            clsGridColumns.Align(colName, HorzAlignment.Center);
            clsGridColumns.Align(colSocialNumber, HorzAlignment.Center);
            clsGridColumns.Align(colBirthday, HorzAlignment.Center);
            clsGridColumns.Align(colGender, HorzAlignment.Center);
            clsGridColumns.Align(colMobilePhone, HorzAlignment.Center);

            // 03 §7.2 는 조회계약을 §5.3 에 위임한다 — 생년월일은 달력 입력이고 yyyyMMdd 로 넘어간다.
            deBirthday.Properties.DisplayFormat.FormatType = FormatType.DateTime;
            deBirthday.Properties.DisplayFormat.FormatString = "yyyy-MM-dd";
            deBirthday.Properties.EditFormat.FormatType = FormatType.DateTime;
            deBirthday.Properties.EditFormat.FormatString = "yyyy-MM-dd";
            deBirthday.Properties.Mask.UseMaskAsDisplayFormat = true;
        }
    }
}
