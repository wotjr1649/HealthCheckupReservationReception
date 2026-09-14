using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// **Grid 표시 공통** — 정렬·날짜 표기·표시글·빈 목록 안내 (킷 §2).
    ///
    /// [X] **생산코드 마흔 곳이 쓰는데 아무 시험도 없었다** (2026-09-15 실측). 화면 시험은
    ///     컬럼의 `Visible` 과 `MinWidth` 만 보고 정렬도 포맷도 보지 않는다.
    ///
    /// 여기서 재는 것은 **DevExpress 가 조용히 무시하는 짝**이다 — `FormatType` 없는
    /// `FormatString`, `UseTextOptions` 없는 `HAlignment`. 한쪽만 적으면 컴파일도 되고
    /// 화면도 뜨는데 그 설정만 안 듣는다.
    /// </summary>
    [TestClass]
    public class clsGridColumnsTests
    {
        // 대상: clsGridColumns.Center · Left — 컬럼 정렬
        // 목적: DevExpress 는 Cell 과 Header 를 따로 받고, 둘 다 UseTextOptions 를 켜야
        //       HAlignment 가 실제로 쓰인다. 한쪽만 맞추면 머리글과 값이 어긋나 보이고,
        //       UseTextOptions 를 빠뜨리면 아무 일도 일어나지 않는다.
        // 확인: Center 는 Cell·Header 둘 다 Center + UseTextOptions=true, Left 는 Near 다.
        [TestMethod]
        public void 정렬은_Cell_과_Header_를_함께_맞추고_UseTextOptions_를_켠다()
        {
            RunSta(delegate
            {
                GridColumn center = NewColumn();
                GridColumn left = NewColumn();

                clsGridColumns.Center(center);
                clsGridColumns.Left(left);

                Assert.AreEqual(HorzAlignment.Center, center.AppearanceCell.TextOptions.HAlignment);
                Assert.AreEqual(HorzAlignment.Center, center.AppearanceHeader.TextOptions.HAlignment,
                    "머리글이 값과 다른 쪽으로 붙는다");
                Assert.IsTrue(center.AppearanceCell.Options.UseTextOptions, "켜지 않으면 무시된다");
                Assert.IsTrue(center.AppearanceHeader.Options.UseTextOptions);

                Assert.AreEqual(HorzAlignment.Near, left.AppearanceCell.TextOptions.HAlignment);
                Assert.AreEqual(HorzAlignment.Near, left.AppearanceHeader.TextOptions.HAlignment);
            });
        }

        // 대상: clsGridColumns.Date — 날짜 컬럼 표기 yyyy-MM-dd
        // 목적: FormatType 을 함께 세우지 않으면 FormatString 이 무시된다. 그 두 줄 짝이
        //       화면 셋에 복사돼 있던 자리이고, 한쪽만 적으면 날짜가 기본 표기(시각까지)로
        //       나와 컬럼이 넘친다.
        // 확인: FormatType=DateTime 과 FormatString=yyyy-MM-dd 가 함께 선다.
        [TestMethod]
        public void 날짜는_FormatType_과_FormatString_을_짝으로_세운다()
        {
            RunSta(delegate
            {
                GridColumn column = NewColumn();

                clsGridColumns.Date(column);

                Assert.AreEqual(FormatType.DateTime, column.DisplayFormat.FormatType,
                    "FormatType 이 없으면 FormatString 이 무시된다");
                Assert.AreEqual("yyyy-MM-dd", column.DisplayFormat.FormatString);
            });
        }

        // 대상: clsGridColumns.Center · Left · Date — null 컬럼이 섞인 호출
        // 목적: 한 줄에 여러 컬럼을 받는 자리라 화면이 아직 만들지 않은 컬럼이 섞일 수 있다.
        //       거기서 터지면 화면이 열리지도 않는다 — 정렬 하나 때문에 화면 전체를 잃는다.
        // 확인: null 이 섞여도 예외가 나지 않고 나머지 컬럼은 정상으로 맞춰진다.
        [TestMethod]
        public void null_컬럼이_섞여도_나머지는_맞춰진다()
        {
            RunSta(delegate
            {
                GridColumn real = NewColumn();

                // [!] `Left(null)` 처럼 쓰지 않는다 — C# 이 그것을 **null 배열**로 읽어
                //     params 순회에서 터진다. 실제로 오는 모양은 「배열 안에 null 이 섞인 것」이다.
                clsGridColumns.Center(null, real, null);
                clsGridColumns.Left(new GridColumn[] { null });
                clsGridColumns.Date(new GridColumn[] { null });

                Assert.AreEqual(HorzAlignment.Center, real.AppearanceCell.TextOptions.HAlignment);
            });
        }

        // 대상: clsGridColumns.Display — 컬럼 하나의 표시글만 바꾸는 배선
        // 목적: 예전에는 `e.Value as string` 이었다. 컬럼이 문자열이 아니면 **조용히 null** 이
        //       되어 어떤 포맷도 걸리지 않는다 — `사용여부` 가 BIT 라 전부 `미사용` 으로
        //       그려졌다 (2026-09-11 실측). 값 쪽에서 끊어야 BIT·숫자·날짜도 포맷이 걸린다.
        // 확인: bool 값 true 가 포맷 함수에 "True" 로 들어와 「사용」으로 그려진다
        //       (문자열이 아닌 값도 포맷을 탄다).
        [TestMethod]
        public void 표시글은_문자열이_아닌_값도_포맷을_탄다()
        {
            RunSta(delegate
            {
                using (var form = new Form())
                using (var grid = new GridControl())
                {
                    var view = new GridView(grid);
                    grid.MainView = view;
                    form.Controls.Add(grid);
                    form.CreateControl();
                    grid.DataSource = new List<Row> { new Row { IsActive = true } };
                    grid.ForceInitialize();

                    GridColumn column = view.Columns["IsActive"];
                    Assert.IsNotNull(column, "컬럼을 잡지 못해 시험이 헛돈다");

                    clsGridColumns.Display(view, column, delegate (string raw)
                    {
                        return raw == "True" ? "사용" : "미사용";
                    });

                    Assert.AreEqual("사용", view.GetRowCellDisplayText(0, column),
                        "BIT 컬럼이 포맷을 타지 못하면 전부 미사용으로 그려진다");
                }
            });
        }

        // 대상: clsGridColumns.ShowEmptyText — 0행일 때의 안내
        // 목적: 빈 Grid 는 고장과 구별되지 않는다. 2026-09-10 실측: 예약 데이터가 0건인 DB 에서
        //       조회가 정상 성공했는데 화면이 아무 말도 하지 않아 「조회가 안 된다」로 보고됐다.
        //       성공한 0건과 실패가 사용자에게 같은 그림이면 안 된다.
        // 확인: 배선한 뒤 행이 0건이면 안내를 그리는 경로가 돌고, 행이 있으면 돌지 않는다.
        [TestMethod]
        public void 빈_목록_안내는_0행일_때만_그린다()
        {
            RunSta(delegate
            {
                using (var form = new Form())
                using (var grid = new GridControl())
                {
                    var view = new GridView(grid);
                    grid.MainView = view;
                    form.Controls.Add(grid);
                    form.CreateControl();
                    grid.DataSource = new List<Row>();
                    grid.ForceInitialize();

                    clsGridColumns.ShowEmptyText(view, "조회 결과가 없습니다.");
                    Assert.AreEqual(0, view.RowCount, "0행이어야 안내가 뜰 자리다");

                    grid.DataSource = new List<Row> { new Row { IsActive = true } };
                    grid.RefreshDataSource();
                    Assert.AreEqual(1, view.RowCount, "행이 있으면 안내를 그리지 않는다");
                }
            });
        }

        private sealed class Row
        {
            public bool IsActive { get; set; }
        }

        private static GridColumn NewColumn()
        {
            return new GridColumn { FieldName = "값", Caption = "값", Visible = true };
        }

        private static void RunSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(delegate ()
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) { throw new AssertFailedException(failure.Message, failure); }
        }
    }
}
