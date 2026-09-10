using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraLayout;
using DevExpress.XtraLayout.Utils;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// WF-PAT-01 화면 자체의 규칙. Presenter 시험이 못 보는 것 —
    /// Grid 바인딩이 자기 마음대로 하는 일 — 만 여기서 본다.
    /// </summary>
    [TestClass]
    public class UcPatientManagementTests
    {
        // 03 §5.3 · §5.5 — 재조회 시 SelectedRow 를 해제한다.
        // [X] Grid 는 DataSource 를 받으면 0행을 잡는다. 그 선택이 그대로 올라오면
        //     방금 비운 상세를 곧바로 다시 채우게 된다.
        [TestMethod]
        public void 목록을_실어도_행이_선택되지_않는다()
        {
            RunSta(() =>
            {
                WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);

                var screen = new UcPatientManagement();
                var raised = new List<long?>();
                ((IPatientManagementView)screen).SelectionChanged += (s, id) => raised.Add(id);

                using (var host = new Form())
                {
                    host.StartPosition = FormStartPosition.Manual;
                    host.Location = new Point(-32000, -32000);
                    host.ClientSize = new Size(1200, 700);
                    screen.Dock = DockStyle.Fill;
                    host.Controls.Add(screen);
                    host.Show();
                    Application.DoEvents();

                    ((IPatientManagementView)screen).Rows = new List<PatientListItemDto>
                    {
                        new PatientListItemDto { PatientId = 11, ChartNo = "2026-000123", Name = "홍길동" },
                        new PatientListItemDto { PatientId = 12, ChartNo = "2026-000124", Name = "수검자4" },
                    };
                    Application.DoEvents();

                    host.Close();
                }

                foreach (long? id in raised)
                {
                    Assert.IsNull(id, "목록만 실었는데 행 선택이 올라왔다: " + id);
                }

                // 화면에도 선택이 없어야 한다. Presenter 는 비어 있는데 Grid 만 첫 행을
                // 칠하고 있으면 사용자는 골라 놓은 줄 안다.
                // GridView 는 행이 있으면 focus 를 반드시 하나 잡으므로(측정: InvalidRowHandle
                // 대입이 대입 직후 0 으로 돌아온다) 화면이 판정할 수 있는 것은 선택 표시뿐이다.
                Assert.IsFalse(SelectionShown(screen), "고르지도 않았는데 선택으로 보인다");
            });
        }

        // 03 §18 — Column Chooser 가 제공하는 것은 표시/숨김과 기본값 복원 둘뿐이다.
        // 여기서는 그 둘이 실제로 Grid 를 움직이는지만 본다. 폼을 띄우지 않고 화면이
        // 노출한 복원 경로를 직접 부른다.
        [TestMethod]
        public void 기본값_복원은_숨긴_컬럼을_되살리고_꺼낸_컬럼을_되돌린다()
        {
            RunSta(() =>
            {
                WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);
                var screen = new UcPatientManagement();
                GridView view = Grid(screen);

                GridColumn shown = Column(view, "차트번호");
                GridColumn hidden = Column(view, "주민등록번호");
                Assert.IsTrue(shown.Visible, "차트번호는 기본 컬럼이다 (03 §5.5)");
                Assert.IsFalse(hidden.Visible, "주민등록번호는 Column Chooser 후보다 (03 §5.5)");

                shown.Visible = false;
                hidden.Visible = true;

                Restore(screen);

                Assert.IsTrue(Column(view, "차트번호").Visible, "기본 컬럼이 돌아오지 않았다");
                Assert.IsFalse(Column(view, "주민등록번호").Visible, "후보 컬럼이 남았다");
            });
        }

        // 03 §18 — 컬럼을 숨기는 길은 [컬럼설정] 하나다. 헤더를 끌어내 숨기는 경로를 막는다.
        [TestMethod]
        public void 헤더를_끌어내_컬럼을_숨길_수_없다()
        {
            RunSta(() =>
            {
                var screen = new UcPatientManagement();
                Assert.IsFalse(Grid(screen).OptionsCustomization.AllowQuickHideColumns);
            });
        }

        // 2026-09-10 사용자 결정 — 조회조건은 차트번호·이름·주민번호 셋이고, 무엇을 낼지는
        // [조회 조건] 드롭다운의 체크 목록이 정한다. 생년월일·휴대전화는 걷었다.
        [TestMethod]
        public void 조회조건은_세_칸이고_기본은_전부_켜져_있다()
        {
            RunSta(() =>
            {
                var screen = new UcPatientManagement();
                CheckedListBoxControl list = Field<CheckedListBoxControl>(screen, "clbConditions");

                Assert.AreEqual(3, list.Items.Count, "조회조건이 셋이 아니다");
                Assert.AreEqual(LayoutVisibility.Always, Item(screen, "lciChartNo").Visibility);
                Assert.AreEqual(LayoutVisibility.Always, Item(screen, "lciName").Visibility);
                Assert.AreEqual(LayoutVisibility.Always, Item(screen, "lciSocialNumber").Visibility);
            });
        }

        // [X] 끈 조건은 값도 버린다. 안 보이는 칸에 남은 글자가 조회에 섞이면
        //     사용자는 왜 그 결과가 나왔는지 알 길이 없다 — Presenter 는 세 칸을 그대로 읽는다.
        [TestMethod]
        public void 조회조건을_끄면_칸이_사라지고_값도_비워진다()
        {
            RunSta(() =>
            {
                var screen = new UcPatientManagement();
                var view = (IPatientManagementView)screen;
                Field<TextEdit>(screen, "txtChartNo").Text = "C000001";
                Assert.AreEqual("C000001", view.ChartNo, "칸에 값이 들어가지 않았다");

                // 사용자가 목록에서 체크를 끄는 것과 같은 경로다.
                Field<CheckedListBoxControl>(screen, "clbConditions").ToggleItem(0);

                Assert.AreEqual(LayoutVisibility.Never, Item(screen, "lciChartNo").Visibility, "끈 조건이 아직 보인다");
                Assert.AreEqual(LayoutVisibility.Always, Item(screen, "lciName").Visibility, "켜 둔 조건까지 사라졌다");
                Assert.IsTrue(string.IsNullOrEmpty(view.ChartNo), "숨긴 칸에 값이 남아 조회에 섞인다: " + view.ChartNo);
            });
        }

        // 03 §18 — 컬럼설정은 Ribbon 이 아니라 Grid 옆 드롭다운이 갖는다 (2026-09-10 사용자 결정).
        // 목록은 Grid 가 가진 컬럼에서 만들고, 체크를 끄면 그 컬럼이 사라진다.
        [TestMethod]
        public void 컬럼_체크를_끄면_그_컬럼이_사라지고_기본값_복원이_되살린다()
        {
            RunSta(() =>
            {
                var screen = new UcPatientManagement();
                GridView grid = Grid(screen);
                CheckedListBoxControl list = Field<CheckedListBoxControl>(screen, "clbColumns");

                Assert.AreEqual(grid.Columns.Count, list.Items.Count, "컬럼 목록이 Grid 와 다르다");

                int chartNo = IndexOfColumn(list, "차트번호");
                Assert.IsTrue(list.GetItemChecked(chartNo), "차트번호는 기본 컬럼이다");

                list.ToggleItem(chartNo);
                Assert.IsFalse(Column(grid, "차트번호").Visible, "체크를 껐는데 컬럼이 남았다");

                // [X] `SimpleButton.PerformClick()` 은 폼에 올라가 보이지 않는 버튼에서는
                //     아무 일도 하지 않는다(실측 2026-09-10 — 컬럼이 그대로 숨은 채였다).
                //     Designer 가 이름으로 잇는 그 핸들러를 직접 부른다.
                Invoke(screen, "btnColumnsDefault_Click");

                Assert.IsTrue(Column(grid, "차트번호").Visible, "기본값 복원이 컬럼을 되살리지 못했다");
                Assert.IsTrue(list.GetItemChecked(chartNo), "Grid 는 돌아왔는데 체크가 어긋났다");
            });
        }

        /// <summary>Designer 가 이름으로 잇는 이벤트 핸들러를 그대로 부른다.</summary>
        private static void Invoke(UcPatientManagement screen, string handler)
        {
            MethodInfo method = typeof(UcPatientManagement).GetMethod(
                handler, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, handler + " 가 사라졌다");
            method.Invoke(screen, new object[] { null, EventArgs.Empty });
        }

        // [X] `OptionsView.ColumnAutoWidth` 는 기본이 true 라 Grid 가 컬럼을 뷰 폭에 욱여넣는다.
        //     그래서 컬럼 폭을 아무리 넓혀도 가로 스크롤바가 서지 않는다(실측 2026-09-10:
        //     컬럼폭합 3000 · 뷰폭 1140 인데 가로 스크롤 숨김). 가로로 미는 지렛대는 `MinWidth`
        //     하나뿐이다 — 컬럼을 새로 더하면서 이것을 빼먹으면 그 컬럼은 20px 까지 찌그러진다.
        [TestMethod]
        public void 모든_컬럼이_찌그러짐을_막을_MinWidth_를_갖는다()
        {
            RunSta(() =>
            {
                var screen = new UcPatientManagement();
                foreach (GridColumn column in Grid(screen).Columns)
                {
                    Assert.IsTrue(column.MinWidth > 20,
                        column.Caption + " 에 MinWidth 가 없다 — 가로 스크롤이 서지 않는다");
                }
            });
        }

        private static int IndexOfColumn(CheckedListBoxControl list, string caption)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                var column = list.Items[i].Value as GridColumn;
                if (column != null && column.Caption == caption)
                {
                    return i;
                }
            }

            throw new AssertFailedException("컬럼 목록에 " + caption + " 이 없다");
        }

        private static BaseLayoutItem Item(UcPatientManagement screen, string name)
        {
            return Field<BaseLayoutItem>(screen, name);
        }

        private static T Field<T>(UcPatientManagement screen, string name)
        {
            FieldInfo field = typeof(UcPatientManagement).GetField(
                name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, name + " 필드가 사라졌다");
            return (T)field.GetValue(screen);
        }

        private static GridColumn Column(GridView view, string caption)
        {
            foreach (GridColumn column in view.Columns)
            {
                if (column.Caption == caption)
                {
                    return column;
                }
            }

            throw new AssertFailedException("Grid 에 " + caption + " 컬럼이 없다");
        }

        private static void Restore(UcPatientManagement screen)
        {
            MethodInfo method = typeof(UcPatientManagement).GetMethod(
                "RestoreDefaultColumns", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "RestoreDefaultColumns 가 사라졌다");
            method.Invoke(screen, null);
        }

        // Grid 는 화면 내부 부품이라 View 계약에 나오지 않는다. 화면이 실제로 어떻게
        // 보이는지는 이것 말고 물어볼 데가 없어 Designer 필드를 직접 본다.
        private static GridView Grid(UcPatientManagement screen)
        {
            FieldInfo field = typeof(UcPatientManagement).GetField(
                "gvPatientList", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "gvPatientList 필드가 사라졌다");
            return (GridView)field.GetValue(screen);
        }

        private static bool SelectionShown(UcPatientManagement screen)
        {
            return Grid(screen).OptionsSelection.EnableAppearanceFocusedRow;
        }

        private static void RunSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                throw new AssertFailedException(failure.Message, failure);
            }
        }
    }
}
