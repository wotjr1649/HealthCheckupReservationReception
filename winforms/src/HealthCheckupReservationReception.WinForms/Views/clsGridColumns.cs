using DevExpress.Utils;
using DevExpress.XtraGrid.Columns;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// Grid 컬럼의 정렬. 설계(`tools/docgen/wireframe/screens/*.js`)의 `K.cols` 가 컬럼마다
    /// `align` 을 정하는데 DevExpress 는 Cell 과 Header 를 따로 받는다 — 그 둘을 함께 맞춘다.
    ///
    /// 수검자 계열 화면 셋이 같은 것을 쓰므로 한 벌만 둔다 (킷 §2).
    /// </summary>
    public static class clsGridColumns
    {
        public static void Align(GridColumn column, HorzAlignment alignment)
        {
            column.AppearanceCell.TextOptions.HAlignment = alignment;
            column.AppearanceCell.Options.UseTextOptions = true;
            column.AppearanceHeader.TextOptions.HAlignment = alignment;
            column.AppearanceHeader.Options.UseTextOptions = true;
        }
    }
}
