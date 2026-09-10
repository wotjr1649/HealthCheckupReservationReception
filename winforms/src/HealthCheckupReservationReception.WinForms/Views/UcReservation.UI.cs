// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using System.Drawing;
using DevExpress.Utils;
using HealthCheckupReservationReception.Common;

namespace HealthCheckupReservationReception.Views
{
    public partial class UcReservation
    {
        /// <summary>
        /// Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance 뿐이다 (킷 §5).
        /// 시간대 항목과 검사 목록은 조회할 때마다 달라지므로 Presenter 가 준 값으로 만든다.
        /// </summary>
        partial void ConfigureUI()
        {
            clsGridColumns.Align(colNexName, HorzAlignment.Near);
            clsGridColumns.Align(colNexType, HorzAlignment.Center);
            clsGridColumns.Align(colAexChecked, HorzAlignment.Center);
            clsGridColumns.Align(colAexName, HorzAlignment.Near);
            clsGridColumns.Align(colAexReason, HorzAlignment.Near);

            // [X] 성공한 0건과 실패가 사용자에게 같은 그림이면 안 된다 — WF-WRK-01 에서 실측한 자리다.
            //     여기서는 03 §8.5 의 진행 단계가 그 이유이므로 그대로 적는다.
            // 03 §8.8 — 구분은 기본/조건부로 적는다. WF-WRK-01 과 같은 규칙을 쓴다.
            clsGridColumns.Display(gvNexList, colNexType, clsWorkText.FormatNexType);

            clsGridColumns.ShowEmptyText(gvNexList, "수검자와 예약일을 고르면 검사 구성이 나옵니다.");
            clsGridColumns.ShowEmptyText(gvAexList, "수검자와 예약일을 고르면 추가검사를 고를 수 있습니다.");

            // 03 §8.7 대상판정은 화면에서 가장 먼저 읽혀야 하는 한 줄이다.
            lblTarget.Appearance.Font = new Font(lblTarget.Appearance.GetFont(), FontStyle.Bold);
            lblTarget.Appearance.Options.UseFont = true;

            // 05 §9.6 차단메시지 · 03 §8.11 저장 실패 사유. 붉은 글씨가 오류라는 유일한 단서다.
            lblBlock.Appearance.ForeColor = Color.Firebrick;
            lblBlock.Appearance.Options.UseForeColor = true;
        }
    }
}
