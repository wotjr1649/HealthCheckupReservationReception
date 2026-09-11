// 화면 ID: DLG-RCP-02 — 접수완료 추가검사 변경 (03 §12)
using System.Drawing;
using DevExpress.Utils;
using HealthCheckupReservationReception.Common;

namespace HealthCheckupReservationReception.Views
{
    public partial class FrmExtraExam
    {
        /// <summary>Designer 가 직렬화하지 않는 것만 여기 있다 (킷 §5).</summary>
        partial void ConfigureUI()
        {
            clsGridColumns.Display(gvNex, colNexType, clsWorkText.FormatNexType);
            clsGridColumns.Align(colNexType, HorzAlignment.Center);
            clsGridColumns.Align(colAexChecked, HorzAlignment.Center);

            clsGridColumns.ShowEmptyText(gvNex, "국가검사 구성이 없습니다.");
            clsGridColumns.ShowEmptyText(gvAex, "추가검사 목록을 받지 못했습니다.");

            lblValidation.Appearance.ForeColor = Color.FromArgb(192, 0, 0);
            lblValidation.Appearance.Options.UseForeColor = true;
        }
    }
}
