// 화면 ID: DLG-PAT-01 — 수검자 등록·정보수정 (03 §6)
using DevExpress.XtraEditors.Controls;

namespace HealthCheckupReservationReception.Views
{
    public partial class FrmPatientEditor
    {
        /// <summary>
        /// Designer 가 직렬화하지 않는 것만 여기 있다 — `RadioGroupItem` 은 다인자 생성자로만
        /// 만들어지므로 Designer 가 아니라 여기서 담는다 (킷 §5).
        /// </summary>
        partial void ConfigureUI()
        {
            rgChartMode.Properties.Items.AddRange(new[]
            {
                new RadioGroupItem(true, "자동발급"),
                new RadioGroupItem(false, "수동입력"),
            });

            // 03 §6.3 — New 의 기본값은 자동발급이다. 항목을 담은 뒤에 골라야 반영된다.
            rgChartMode.EditValue = true;
        }
    }
}
