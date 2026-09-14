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
        // 대상: UcPatientManagement (WF-PAT-01) — Grid 에 DataSource 를 실었을 때의 선택 상태
        // 목적: 03 §5.3·§5.5 는 재조회 시 선택을 해제하라고 정했다. 그런데 DevExpress Grid 는
        //       DataSource 를 받으면 스스로 0행을 잡고, 그 선택이 이벤트로 올라오면 방금 비운
        //       상세를 곧바로 다시 채운다 — 조작자가 고르지도 않은 사람의 상세가 선다.
        // 확인: 목록만 실었을 때 선택 이벤트로 올라온 수검자ID 가 null 이고, 화면에도 선택으로
        //       보이지 않는다.
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

                    ((IPatientManagementView)screen).Rows = new List<PatientDto>
                    {
                        new PatientDto { PatientId = 11, ChartNo = "2026-000123", Name = "홍길동" },
                        new PatientDto { PatientId = 12, ChartNo = "2026-000124", Name = "수검자4" },
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

        // 대상: UcPatientManagement (WF-PAT-01) — 조회조건 영역의 가로 폭
        // 목적: 항목마다 Min=Max 로 못 박혀 있어 좁아지지 않는다. 다섯을 다 켜서 한 줄이 넘치면
        //       LayoutControl 안에 가로 스크롤이 서고 [조회] 무리가 화면 밖으로 밀린다. 조건을
        //       더하거나 폭을 늘릴 때 이 시험에서 먼저 걸린다.
        // 확인: 조회 영역에 항목이 있고(검사가 헛돌지 않았다), 각 줄의 오른쪽 끝이 그룹 폭 안에
        //       들어간다.
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

        // 대상: UcPatientManagement (WF-PAT-01) — [컬럼설정] 의 기본값 복원
        // 목적: 03 §18 이 Column Chooser 에 표시/숨김과 기본값 복원 둘만 주기로 했다. 복원이
        //       실제로 Grid 를 움직이지 않으면 조작자는 컬럼을 잘못 만진 뒤 되돌릴 길이 없다.
        // 확인: 복원 뒤 기본 컬럼(차트번호)이 보이고, 기본이 아닌 후보 컬럼(주민등록번호)은
        //       다시 숨겨진다 — 양방향 모두 제자리로 간다.
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

        // 대상: UcPatientManagement (WF-PAT-01) — Grid 의 AllowQuickHideColumns 옵션
        // 목적: 03 §18 은 컬럼을 숨기는 길을 [컬럼설정] 하나로 정했다. 헤더를 끌어내 숨기는
        //       경로가 열려 있으면 실수로 사라진 컬럼을 조작자가 되찾는 길을 모른다.
        // 확인: Grid 의 OptionsCustomization.AllowQuickHideColumns 가 false 다.
        [TestMethod]
        public void 헤더를_끌어내_컬럼을_숨길_수_없다()
        {
            RunSta(() =>
            {
                var screen = new UcPatientManagement();
                Assert.IsFalse(Grid(screen).OptionsCustomization.AllowQuickHideColumns);
            });
        }

        // 대상: UcPatientManagement (WF-PAT-01) — 조회조건의 기본 구성
        // 목적: 03 §5.3 의 조회조건 다섯 중 기본으로 켜 두는 것은 셋이다. 2026-09-10 에
        //       생년월일·휴대전화를 걷었다가 grilling 2회차에서 「끌 수 있는 조건」으로 되돌렸다 —
        //       사라진 것이 아니라 기본이 꺼진 것이고, 그래서 01 P01-01 · 02 F-PAT-001 과의
        //       이탈이 닫힌다.
        // 확인: 조회조건 항목이 셋이고 차트번호·이름·주민등록번호가 모두 Always 로 보이며,
        //       여섯째인 「예약 없는 수검자만」 체크박스는 기본이 꺼짐이다.
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

        // 대상: UcPatientManagement (WF-PAT-01) — 걷어 낸 두 조회조건이 SP 로 가지 않는지
        // 목적: SP-PAT-01 은 @생년월일·@휴대전화를 여전히 받으므로 (05 §7.2) 계약만 봐서는 화면이
        //       그 둘을 보내는지 알 수 없다. 화면이 null 을 내주는 것이 유일한 고리다. 이 시험이
        //       red 가 되면 둘이 되살아난 것이고, 그때는 01 P01-01 · 02 F-PAT-001 과의 이탈도
        //       함께 사라지므로 session-17 §5 를 같이 고쳐야 한다.
        // 확인: 조회조건 드롭다운에 생년월일·휴대전화가 없고, 화면이 내주는 Birthday·MobilePhone
        //       이 둘 다 null 이다.
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

        // 대상: UcPatientManagement (WF-PAT-01) — 조회조건을 껐을 때의 칸과 값
        // 목적: 끈 조건은 값도 버려야 한다. Presenter 는 세 칸을 그대로 읽으므로, 안 보이는 칸에
        //       남은 글자가 조회에 섞이면 조작자는 왜 그 결과가 나왔는지 알 길이 없다.
        // 확인: 차트번호에 값을 넣은 뒤 그 조건을 끄면 칸이 Never 로 사라지고, 켜 둔 이름 조건은
        //       그대로 Always 이며, 화면이 내주는 차트번호 값이 비어 있다.
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

        // 대상: UcPatientManagement (WF-PAT-01) — Grid 옆 [컬럼설정] 드롭다운과 Grid 의 연동
        // 목적: 03 §18 에서 컬럼설정은 Ribbon 이 아니라 Grid 옆 드롭다운이 갖는다 (2026-09-10
        //       사용자 결정). 목록과 Grid 가 어긋나면 체크는 켜져 있는데 컬럼은 없는 상태가 되어
        //       조작자가 되돌릴 방법을 잃는다.
        // 확인: 목록 항목 수가 Grid 컬럼 수와 같고, 차트번호 체크를 끄면 그 컬럼이 사라지며,
        //       기본값 복원 뒤에는 컬럼과 체크가 함께 되살아난다.
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

        // 대상: UcPatientManagement (WF-PAT-01) — Grid 컬럼의 MinWidth
        // 목적: ColumnAutoWidth 는 기본이 true 라 Grid 가 컬럼을 뷰 폭에 욱여넣는다. 가로로 미는
        //       지렛대는 MinWidth 하나뿐이고, 컬럼을 새로 더하면서 빼먹으면 그 컬럼이 찌그러져
        //       값이 «...» 로만 보인다. WF-WRK-01 과 같은 규칙이라 두 화면을 함께 지킨다.
        // 확인: 모든 컬럼의 MinWidth 가 20 보다 크다.
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
