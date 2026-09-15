// 화면 ID: DLG-RCP-01 — 접수 처리 (03 §11)
using HealthCheckupReservationReception.Common;

namespace HealthCheckupReservationReception.Views
{
    public partial class FrmReception
    {
        /// <summary>Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance 와 빈 목록 안내 (킷 §5).</summary>
        partial void ConfigureUI()
        {
            // 버튼 아이콘 (2026-09-15 사용자 요청). 뜻이 같으면 Ribbon 과 같은 그림을 쓴다.
            clsGlyph.Apply(btnReceive, "ReceptionStart");
            clsGlyph.Apply(btnClose, "Close");

            // 03 §8.8 과 같은 규칙이다 — NEX 구분은 기본/조건부로 적는다.
            clsGridColumns.Display(gvNex, colNexType, clsWorkText.FormatNexType);
            clsGridColumns.Center(colNexType);

            clsGridColumns.ShowEmptyText(gvNex, "국가검사 구성이 없습니다.");
            clsGridColumns.ShowEmptyText(gvAex, "선택한 추가검사가 없습니다.");

            clsNotice.Ok(lblEligibility);
            clsNotice.Error(lblValidation);

            _receive = new clsActionRunner(this, btnReceive, btnClose);
        }
    }
}
