// 화면 ID: DLG-PAT-03 — 중복 후보 확인 (03 §6.5)
using DevExpress.Utils;

namespace HealthCheckupReservationReception.Views
{
    public partial class FrmPatientDuplicate
    {
        /// <summary>
        /// Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance (킷 §5).
        /// </summary>
        partial void ConfigureUI()
        {
            // 설계 dlg_pat_03.js 의 K.cols — 차트번호만 좌측 정렬이고 나머지 다섯은 가운데다.
            clsGridColumns.Align(colChartNo, HorzAlignment.Near);
            clsGridColumns.Align(colName, HorzAlignment.Center);
            clsGridColumns.Align(colBirthday, HorzAlignment.Center);
            clsGridColumns.Align(colGender, HorzAlignment.Center);
            clsGridColumns.Align(colSocialNumber, HorzAlignment.Center);
            clsGridColumns.Align(colMobilePhone, HorzAlignment.Center);
        }
    }
}
