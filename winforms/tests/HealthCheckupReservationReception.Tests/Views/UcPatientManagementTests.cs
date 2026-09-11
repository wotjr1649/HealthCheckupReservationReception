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

        // [X] **다섯을 다 켜면 한 줄이 넘칠 수 있다.** 항목마다 Min=Max 로 못 박혀 있어
        //     좁아지지 않고, 넘치면 LayoutControl 안에 가로 스크롤이 서서 [조회] 무리가
        //     화면 밖으로 밀린다. 조건을 더하거나 폭을 늘릴 때 여기서 걸린다.
        [TestMethod]
        public void 조회조건_다섯을_다_켜도_한_줄에_들어간다()
        {
            RunSta(() =>
            {
                var screen = new UcPatientManagement();
                var group = Field<LayoutControlGroup>(screen, "lcgSearch");

                // [X] 숨어 있어도 켜면 자리를 차지하므로 보이는 것만 세면 아무것도 못 잡는다.
                //     반대로 EmptySpaceItem 은 남는 자리를 빨아들이는 쪽이라 세면 안 된다 —
                //     조건 둘이 꺼져 있는 동안 그만큼 부풀어 있어 합이 늘 넘치게 나온다.
                //
                // [X] **줄마다 따로 센다** (2026-09-11). 조회 영역이 두 줄이 되었다 —
                //     `예약 없는 수검자만` 체크박스가 둘째 줄이다. 전부 한 줄로 더하면
                //     넘치지 않는 배치도 넘친 것으로 잡힌다.
                var rows = new Dictionary<int, int>();
                foreach (BaseLayoutItem item in group.Items)
                {
                    if (item is EmptySpaceItem)
                    {
                        continue;
                    }

                    int y = item.Location.Y;
                    int width = item.MaxSize.Width > 0 ? item.MaxSize.Width : item.Size.Width;
                    rows[y] = (rows.ContainsKey(y) ? rows[y] : 0) + width;
                }

                Assert.AreNotEqual(0, rows.Count, "조회 영역에 항목이 없다");
                foreach (KeyValuePair<int, int> row in rows)
                {
                    Assert.IsTrue(row.Value <= group.Size.Width,
                        "y=" + row.Key + " 줄이 " + row.Value + "px 인데 자리는 " + group.Size.Width + "px 다");
                }
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

        // 03 §5.3 조회조건 다섯. 2026-09-10 에 생년월일·휴대전화를 걷었다가 grilling 2회차에서
        // **끌 수 있는 조건**으로 되돌렸다 — 사라진 것이 아니라 기본이 꺼진 것이고, 그래서
        // 01 P01-01 · 02 F-PAT-001 과의 이탈이 닫힌다.
        //
        // 여섯째 `예약 없는 수검자만` 은 2026-09-11 grilling 이 더한 것이다. SP 로 가지 않고
        // 화면이 거르므로 여닫을 칸이 없다 — 앞 다섯과 성질이 다르다.
        [TestMethod]
        public void 조회조건은_셋이고_모두_켜져_있다()
        {
            RunSta(() =>
            {
                var screen = new UcPatientManagement();
                CheckedListBoxControl list = Field<CheckedListBoxControl>(screen, "clbConditions");

                // 2026-09-11 — 생년월일·휴대전화를 걷었다 (사용자 지시). 남은 셋은 전부 켜져
                // 있고, 끌 수 있다는 것이 [조회 조건] 드롭다운이 남아 있는 이유다.
                Assert.AreEqual(3, list.Items.Count, "조회조건이 셋이 아니다");
                Assert.AreEqual(LayoutVisibility.Always, Item(screen, "lciChartNo").Visibility);
                Assert.AreEqual(LayoutVisibility.Always, Item(screen, "lciName").Visibility);
                Assert.AreEqual(LayoutVisibility.Always, Item(screen, "lciSocialNumber").Visibility);
                Assert.IsFalse(((IPatientManagementView)screen).ReservableOnly, "체크박스는 기본이 꺼짐이다");
            });
        }

        /// <summary>
        /// `[X]` **걷었다는 것을 화면 밖에서도 고정한다.** `SP-PAT-01` 은 `@생년월일`·
        ///      `@휴대전화` 를 여전히 받으므로(`05` §7.2) 계약만 봐서는 화면이 그 둘을
        ///      보내는지 알 수 없다. 화면이 `null` 을 내주는 것이 유일한 고리다.
        ///
        /// `[!]` 이 시험이 red 가 되면 둘이 되살아난 것이다. 그때는 `01` P01-01 ·
        ///      `02` F-PAT-001 과의 이탈이 함께 사라지므로 session-17 §5 도 같이 고쳐라.
        /// </summary>
        [TestMethod]
        public void 생년월일_휴대전화는_조회조건으로_가지_않는다()
        {
            RunSta(() =>
            {
                var screen = new UcPatientManagement();
                var view = (IPatientManagementView)screen;
                CheckedListBoxControl list = Field<CheckedListBoxControl>(screen, "clbConditions");

                for (int i = 0; i < list.Items.Count; i++)
                {
                    string caption = list.GetItemValue(i) as string;
                    Assert.AreNotEqual("생년월일", caption, "드롭다운에 생년월일이 남아 있다");
                    Assert.AreNotEqual("휴대전화", caption, "드롭다운에 휴대전화가 남아 있다");
                }

                Assert.IsNull(view.Birthday, "화면이 생년월일을 조회조건으로 내주고 있다");
                Assert.IsNull(view.MobilePhone, "화면이 휴대전화를 조회조건으로 내주고 있다");
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

        /// <summary>
        /// `[기본값 복원]` 이 실제로 도는 경로. 되돌리는 일은 `clsColumnChooser` 가 하고
        /// 화면은 Designer 가 이름으로 잇는 이 핸들러 한 줄만 갖는다.
        /// </summary>
        private static void Restore(UcPatientManagement screen)
        {
            Invoke(screen, "btnColumnsDefault_Click");
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
