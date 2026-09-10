using System;
using DevExpress.Utils;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// Grid 표시 공통 — 컬럼 정렬과 빈 목록 안내. 업무 화면 여럿이 같은 것을 쓴다 (킷 §2).
    /// </summary>
    public static class clsGridColumns
    {
        /// <summary>
        /// 컬럼 정렬. DevExpress 는 Cell 과 Header 를 따로 받는다 — 그 둘을 함께 맞춘다.
        /// </summary>
        public static void Align(GridColumn column, HorzAlignment alignment)
        {
            column.AppearanceCell.TextOptions.HAlignment = alignment;
            column.AppearanceCell.Options.UseTextOptions = true;
            column.AppearanceHeader.TextOptions.HAlignment = alignment;
            column.AppearanceHeader.Options.UseTextOptions = true;
        }

        /// <summary>
        /// 컬럼 하나의 **표시글**만 바꾼다. 값은 건드리지 않는다.
        ///
        /// 같은 세 줄이 화면마다 되풀이되던 자리다 — NEX 의 `구분` 은 WF-RSV-01 과 WF-WRK-01
        /// 둘 다 같은 규칙으로 적어야 한다 (킷 §2 · ROOT AGENTS.md §6).
        /// </summary>
        public static void Display(GridView view, GridColumn column, Func<string, string> format)
        {
            view.CustomColumnDisplayText += delegate(object sender, CustomColumnDisplayTextEventArgs e)
            {
                if (e.Column == column)
                {
                    e.DisplayText = format(e.Value as string);
                }
            };
        }

        /// <summary>
        /// 0행일 때 빈 Grid 한가운데에 이유를 적는다.
        ///
        /// [X] **빈 Grid 는 고장과 구별되지 않는다.** 2026-09-10 실측: 예약 데이터가 0건인
        ///     DB 에서 조회가 정상 성공했는데 화면이 아무 말도 하지 않아 "조회가 안 된다" 로
        ///     보고됐다. 성공한 0건과 실패는 사용자에게 같은 그림이면 안 된다.
        /// </summary>
        public static void ShowEmptyText(GridView view, string text)
        {
            view.CustomDrawEmptyForeground += delegate(object sender, CustomDrawEventArgs e)
            {
                var grid = sender as GridView;
                if (grid == null || grid.RowCount > 0)
                {
                    return;
                }

                var appearance = new AppearanceObject(e.Appearance);
                appearance.TextOptions.HAlignment = HorzAlignment.Center;
                appearance.TextOptions.VAlignment = VertAlignment.Center;
                appearance.Options.UseTextOptions = true;
                appearance.DrawString(e.Cache, text, e.Bounds);
            };
        }
    }
}
