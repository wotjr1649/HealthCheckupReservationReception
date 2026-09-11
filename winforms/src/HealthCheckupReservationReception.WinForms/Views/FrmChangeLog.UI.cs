// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
using System.Drawing;
using DevExpress.Utils;

namespace HealthCheckupReservationReception.Views
{
    public partial class FrmChangeLog
    {
        /// <summary>
        /// Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance 와 빈 목록 안내 (킷 §5).
        /// </summary>
        partial void ConfigureUI()
        {
            colRecordedAt.DisplayFormat.FormatType = FormatType.DateTime;
            colRecordedAt.DisplayFormat.FormatString = "yyyy-MM-dd HH:mm:ss";

            clsGridColumns.Align(colRecordedAt, HorzAlignment.Center);
            clsGridColumns.Align(colOperatorName, HorzAlignment.Center);
            clsGridColumns.Align(colColumnName, HorzAlignment.Center);
            clsGridColumns.Align(colBeforeValue, HorzAlignment.Near);
            clsGridColumns.Align(colAfterValue, HorzAlignment.Near);

            // 03 §23.4 — 0건은 오류가 아니다. 빈 Grid 를 고장과 구별되게 적는다.
            clsGridColumns.ShowEmptyText(gvLog, "변경 기록이 없습니다.");

            // 03 §23.4 — 값은 NVARCHAR(4000) 에서 잘려 저장됐을 수 있고 이 화면은 복원 근거가
            // 아니다 (00 CP-06). 화면이 그 사실을 말한다.
            lblNotice.Text = "변경 기록은 열람용입니다. 값이 저장 한도에서 잘렸을 수 있습니다.";
            lblNotice.Appearance.ForeColor = Color.FromArgb(112, 112, 112);
            lblNotice.Appearance.Options.UseForeColor = true;

            lblValidation.Appearance.ForeColor = Color.FromArgb(192, 0, 0);
            lblValidation.Appearance.Options.UseForeColor = true;
        }
    }
}
