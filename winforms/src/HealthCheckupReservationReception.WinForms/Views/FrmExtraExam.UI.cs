// 화면 ID: DLG-RCP-02 — 접수완료 추가검사 변경 (03 §12)
using HealthCheckupReservationReception.Common;

namespace HealthCheckupReservationReception.Views
{
    public partial class FrmExtraExam
    {
        /// <summary>Designer 가 직렬화하지 않는 것만 여기 있다 (킷 §5).</summary>
        partial void ConfigureUI()
        {
            // 버튼 아이콘 (2026-09-15 사용자 요청). 뜻이 같으면 Ribbon 과 같은 그림을 쓴다.
            clsGlyph.Apply(btnSave, "Save");
            clsGlyph.Apply(btnClose, "Close");

            clsGridColumns.Display(gvNex, colNexType, clsWorkText.FormatNexType);
            clsGridColumns.Center(colNexType, colAexChecked);

            clsGridColumns.ShowEmptyText(gvNex, "국가검사 구성이 없습니다.");
            clsGridColumns.ShowEmptyText(gvAex, "추가검사 목록을 받지 못했습니다.");

            clsNotice.Error(lblValidation);

            _save = new clsActionRunner(this, btnSave, btnClose);
        }
    }
}
