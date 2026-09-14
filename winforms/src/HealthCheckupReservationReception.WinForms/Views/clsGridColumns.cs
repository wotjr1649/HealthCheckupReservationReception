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
        /// 가운데 정렬. 날짜·시간대·상태·성별처럼 폭이 정해진 칸이다.
        ///
        /// 한 줄에 여러 컬럼을 받는다 — 화면마다 같은 호출이 다섯·여섯 줄씩 이어지던
        /// 자리이고, 그 줄들은 「무엇이 가운데인가」를 읽는 데 방해만 됐다 (2026-09-14).
        /// </summary>
        public static void Center(params GridColumn[] columns)
        {
            Apply(columns, HorzAlignment.Center);
        }

        /// <summary>
        /// 왼쪽 정렬. 차트번호·이름·메모처럼 글이 이어지는 칸이다.
        /// DevExpress 의 `Near` 다 — RTL 을 쓰지 않으므로 읽는 이름으로 적는다.
        /// </summary>
        public static void Left(params GridColumn[] columns)
        {
            Apply(columns, HorzAlignment.Near);
        }

        /// <summary>
        /// 날짜 컬럼의 표기 `yyyy-MM-dd`.
        ///
        /// [X] **`FormatType` 을 함께 세우지 않으면 `FormatString` 이 무시된다.**
        ///     그 두 줄 짝이 화면 셋에 복사돼 있었다 — 한쪽만 적으면 조용히 안 듣는다.
        /// </summary>
        public static void Date(params GridColumn[] columns)
        {
            foreach (GridColumn column in columns)
            {
                if (column == null) { continue; }

                column.DisplayFormat.FormatType = FormatType.DateTime;
                column.DisplayFormat.FormatString = "yyyy-MM-dd";
            }
        }

        private static void Apply(GridColumn[] columns, HorzAlignment alignment)
        {
            foreach (GridColumn column in columns)
            {
                if (column != null) { Align(column, alignment); }
            }
        }

        /// <summary>
        /// 컬럼 정렬. DevExpress 는 Cell 과 Header 를 따로 받는다 — 그 둘을 함께 맞춘다.
        /// </summary>
        private static void Align(GridColumn column, HorzAlignment alignment)
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
                    // [X] `e.Value as string` 이었다. 컬럼이 문자열이 아니면 **조용히 null** 이
                    //     되어 어떤 포맷도 걸리지 않는다 — `사용여부` 가 BIT 라 전부 `미사용` 으로
                    //     그려졌다 (2026-09-11 실측). 값 쪽에서 끊는다.
                    e.DisplayText = format(e.Value == null ? null : e.Value.ToString());
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
