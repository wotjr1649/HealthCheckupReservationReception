// 화면 ID: DLG-RCP-01 — 접수 처리 (03 §11)
using System.Drawing;
using DevExpress.Utils;
using HealthCheckupReservationReception.Common;

namespace HealthCheckupReservationReception.Views
{
    public partial class FrmReception
    {
        /// <summary>Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance 와 빈 목록 안내 (킷 §5).</summary>
        partial void ConfigureUI()
        {
            // 03 §8.8 과 같은 규칙이다 — NEX 구분은 기본/조건부로 적는다.
            clsGridColumns.Display(gvNex, colNexType, clsWorkText.FormatNexType);
            clsGridColumns.Align(colNexType, HorzAlignment.Center);

            clsGridColumns.ShowEmptyText(gvNex, "국가검사 구성이 없습니다.");
            clsGridColumns.ShowEmptyText(gvAex, "선택한 추가검사가 없습니다.");

            lblEligibility.Appearance.ForeColor = Color.FromArgb(0, 96, 0);
            lblEligibility.Appearance.Options.UseForeColor = true;
            lblValidation.Appearance.ForeColor = Color.FromArgb(192, 0, 0);
            lblValidation.Appearance.Options.UseForeColor = true;
        }
    }
}
