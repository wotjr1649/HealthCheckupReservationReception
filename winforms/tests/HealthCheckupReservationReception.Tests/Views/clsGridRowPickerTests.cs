using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// **Grid 가 잡아 둔 행과 사용자가 고른 행을 가른다.**
    ///
    /// [X] GridView 는 행이 있으면 반드시 하나를 focus 한다 — `FocusedRowHandle =
    ///     InvalidRowHandle` 은 대입 직후 다시 0 이 된다 (2026-09-10 실측). 화면이 잡아 둔
    ///     행을 대상으로 넘기면 사용자가 고르지도 않은 행이 저장 SP 로 간다.
    ///
    /// [X] **화면 시험은 이 규칙의 겉만 본다** (2026-09-15 실측). `UcPatientManagementTests`
    ///     가 「목록을 실어도 행이 선택되지 않는다」를 재지만, `Select` 의 두 갈래(이미 focus
    ///     된 행 · 아닌 행)와 없는 행 처리는 어느 시험도 밟지 않았다.
    /// </summary>
    [TestClass]
    public class clsGridRowPickerTests
    {
        // 대상: clsGridRowPicker 생성 직후의 상태
        // 목적: Grid 는 행이 있으면 스스로 0행을 focus 한다. 그것을 「고른 행」으로 읽으면
        //       화면이 열리자마자 첫 사람의 상세가 서고 Action 이 열린다.
        // 확인: 행이 있어도 Row 가 null 이고 선택 표시가 꺼져 있다.
        [TestMethod]
        public void 행이_있어도_처음에는_고른_것이_없다()
        {
            RunSta(delegate
            {
                using (Fixture f = NewFixture(2))
                {
                    Assert.IsNull(f.Picker.Row, "Grid 가 잡아 둔 행이 고른 행으로 올라왔다");
                    Assert.IsFalse(f.View.OptionsSelection.EnableAppearanceFocusedRow,
                        "고르지도 않았는데 선택으로 보인다");
                }
            });
        }

        // 대상: clsGridRowPicker.FocusedRowChanged — 키보드·클릭으로 행이 바뀌는 길
        // 목적: 사용자가 실제로 고른 행은 상세와 Action 의 대상이 되어야 한다. 이벤트가 나지
        //       않으면 화면은 고른 것을 모른 채 앞 상태에 머문다.
        // 확인: Row 가 그 행이 되고 PickChanged 가 한 번 나며 선택 표시가 켜진다.
        [TestMethod]
        public void 고르면_그_행이_대상이_되고_한_번_알린다()
        {
            RunSta(delegate
            {
                using (Fixture f = NewFixture(2))
                {
                    int fired = 0;
                    f.Picker.PickChanged += delegate { fired++; };

                    f.Picker.FocusedRowChanged(1);

                    Assert.AreSame(f.Rows[1], f.Picker.Row);
                    Assert.AreEqual(1, fired, "알리지 않았거나 두 번 알렸다");
                    Assert.IsTrue(f.View.OptionsSelection.EnableAppearanceFocusedRow);
                }
            });
        }

        // 대상: clsGridRowPicker.Rebind — 목록을 다시 싣는 길
        // 목적: 목록을 실으면 Grid 가 0행을 자동으로 잡는다. 그 잠깐의 선택이 위로 올라가면
        //       방금 비운 상세를 곧바로 다시 조회하게 된다. 03 §5.3·§9.4 는 재조회 시
        //       선택·상세를 Clear 하라고 정했고, 그 둘을 한 번에 지키는 자리가 여기다.
        // 확인: 싣는 동안 PickChanged 가 한 번도 나지 않고, 끝난 뒤 Row 가 null 이며
        //       선택 표시가 꺼져 있다.
        [TestMethod]
        public void 다시_실으면_선택이_풀리고_그_사이_알리지_않는다()
        {
            RunSta(delegate
            {
                using (Fixture f = NewFixture(2))
                {
                    f.Picker.FocusedRowChanged(1);
                    Assert.IsNotNull(f.Picker.Row, "먼저 골라 두어야 이 시험이 뜻을 갖는다");

                    int fired = 0;
                    f.Picker.PickChanged += delegate { fired++; };

                    f.Picker.Rebind(f.Grid, NewRows(3));

                    Assert.AreEqual(0, fired, "다시 싣는 동안 선택 이벤트가 위로 샜다");
                    Assert.IsNull(f.Picker.Row, "재조회 뒤에도 앞 행이 대상으로 남았다");
                    Assert.IsFalse(f.View.OptionsSelection.EnableAppearanceFocusedRow);
                }
            });
        }

        // 대상: clsGridRowPicker.RowClick — 조회 직후 첫 행을 처음 누르는 길
        // 목적: 조회 직후 이미 focus 되어 있는 첫 행을 사용자가 처음 누르면 FocusedRowChanged
        //       가 나지 않는다 — 그 길로만 잡힌다. 없으면 첫 행은 두 번 눌러야 고를 수 있다.
        //       이미 고른 뒤에는 이 길이 다시 잡지 않아야 이벤트가 겹치지 않는다.
        // 확인: 안 고른 상태의 RowClick 은 대상을 세우고, 이미 고른 뒤의 RowClick 은
        //       대상을 바꾸지 않는다.
        [TestMethod]
        public void 처음_누른_것만_RowClick_이_잡는다()
        {
            RunSta(delegate
            {
                using (Fixture f = NewFixture(2))
                {
                    f.Picker.RowClick(0);
                    Assert.AreSame(f.Rows[0], f.Picker.Row, "첫 행을 처음 눌렀는데 안 잡혔다");

                    f.Picker.RowClick(1);
                    Assert.AreSame(f.Rows[0], f.Picker.Row,
                        "이미 고른 뒤에는 RowClick 이 대상을 바꾸지 않는다 — 이동은 FocusedRowChanged 가 맡는다");
                }
            });
        }

        // 대상: clsGridRowPicker.Select — 코드가 행을 겨누는 길 (03 §9.1 Targeted Navigation)
        // 목적: 저장 직후 그 건으로 돌아가는 길이다. 이미 그 행이 focus 되어 있으면
        //       FocusedRowHandle 대입이 이벤트를 내지 않으므로 그 경우만 직접 잡아야 한다 —
        //       아니면 「찾았다」고 해 놓고 상세와 Action 이 서지 않는다.
        // 확인: 이미 focus 된 0행을 겨눠도 true 이고 대상이 서며 PickChanged 가 난다.
        //       focus 아닌 1행을 겨눠도 대상이 그 행으로 바뀐다.
        [TestMethod]
        public void 겨눈_행은_이미_focus_되어_있어도_대상이_된다()
        {
            RunSta(delegate
            {
                using (Fixture f = NewFixture(2))
                {
                    int fired = 0;
                    f.Picker.PickChanged += delegate { fired++; };

                    Assert.IsTrue(f.Picker.Select(0), "0행을 겨눴는데 못 찾았다고 했다");
                    Assert.AreSame(f.Rows[0], f.Picker.Row, "겨눴는데 대상이 서지 않았다");
                    Assert.AreEqual(1, fired, "겨눈 뒤 상세·Action 이 따라오지 않는다");

                    Assert.IsTrue(f.Picker.Select(1));
                    Assert.AreSame(f.Rows[1], f.Picker.Row);
                }
            });
        }

        // 대상: clsGridRowPicker.Select — 목록에 없는 행을 겨눈 경우
        // 목적: 조회조건이 방금 저장한 건을 담지 못하면 못 찾는다 (07 §3.6.2). 그때 true 를
        //       돌려주면 화면은 「찾았다」고 믿고 아무 말도 하지 않아, 조작자는 저장이 안 된
        //       줄로 읽는다. 못 찾았다고 말해야 Inline 안내가 뜬다.
        // 확인: 없는 행 핸들이면 false 이고 대상이 서지 않는다.
        [TestMethod]
        public void 없는_행을_겨누면_못_찾았다고_말한다()
        {
            RunSta(delegate
            {
                using (Fixture f = NewFixture(2))
                {
                    Assert.IsFalse(f.Picker.Select(99), "없는 행을 찾았다고 했다");
                    Assert.IsNull(f.Picker.Row);
                }
            });
        }

        // ── helpers

        private sealed class Fixture : IDisposable
        {
            public Form Form;
            public GridControl Grid;
            public GridView View;
            public clsGridRowPicker Picker;
            public List<Row> Rows;

            public void Dispose()
            {
                if (Grid != null) { Grid.Dispose(); }
                if (Form != null) { Form.Dispose(); }
            }
        }

        private sealed class Row
        {
            public string Name { get; set; }
        }

        private static List<Row> NewRows(int n)
        {
            var rows = new List<Row>();
            for (int i = 0; i < n; i++) { rows.Add(new Row { Name = "행" + i }); }
            return rows;
        }

        private static Fixture NewFixture(int rows)
        {
            // [!] Form 에 얹지 않으면 GridView 가 행을 세우지 않는다 — GetRow 가 전부 null 이
            //     되어 시험이 아무것도 재지 못한 채 green 이 된다.
            var form = new Form();
            var grid = new GridControl();
            var view = new GridView(grid);
            grid.MainView = view;
            form.Controls.Add(grid);
            form.CreateControl();
            List<Row> data = NewRows(rows);
            grid.DataSource = data;
            grid.ForceInitialize();

            var picker = new clsGridRowPicker(view);

            // [!] 화면이 Designer 에서 잇는 배선을 여기서도 그대로 잇는다
            //     (`UcWorkbench.gvWorkList_FocusedRowChanged`). 이것이 없으면 `Select` 가
            //     FocusedRowHandle 만 옮기고 대상은 앞 행에 머문다 — 화면과 다른 것을 재게 된다.
            view.FocusedRowChanged += delegate (object sender, FocusedRowChangedEventArgs e)
            {
                picker.FocusedRowChanged(e.FocusedRowHandle);
            };

            return new Fixture
            {
                Form = form,
                Grid = grid,
                View = view,
                Picker = picker,
                Rows = data,
            };
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
