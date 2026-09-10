using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraLayout;
using DevExpress.XtraLayout.Utils;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// WF-WRK-01 화면 자체의 규칙. Presenter 시험이 못 보는 것만 여기서 본다 —
    /// 조회 한 줄의 구성, 조회조건 드롭다운, 컬럼 드롭다운, Grid 컬럼.
    /// </summary>
    [TestClass]
    public class UcWorkbenchTests
    {
        // [X] **이 시험이 잘림을 막는다.** LayoutControl 은 기본값에서 한 줄 안의 라벨 폭을
        //     **가장 긴 것에 맞춰 통일**한다. 그러면 `~` 한 글자짜리 라벨이 `예약/접수일` 만큼의
        //     자리를 떠안고, 그 폭이 그대로 입력칸에서 깎여 종료일 DateEdit 이 잘린다
        //     (2026-09-10 실측·사용자 보고). 항목마다 AutoSize 를 주는 것이 유일한 해법이고,
        //     라벨 길이가 제각각인 줄에서는 하나라도 빠지면 다시 밟는다.
        [TestMethod]
        public void 조회조건_라벨은_저마다_제_폭을_쓴다()
        {
            RunSta(() =>
            {
                var screen = new UcWorkbench();
                string[] labelled = { "lciDateFrom", "lciDateTo", "lciStatus", "lciChartNo", "lciName" };
                foreach (string name in labelled)
                {
                    Assert.AreEqual(TextAlignModeItem.AutoSize, Item(screen, name).TextAlignMode,
                        name + " 이 라벨 폭을 옆 항목과 나눠 쓴다 — 입력칸이 그만큼 눌린다");
                }
            });
        }

        // 2026-09-10 사용자 지적 — 조회 한 줄은 WF-PAT-01 과 같은 꼴이어야 한다.
        // 오른쪽 끝 셋의 순서가 [조회] [조회 조건] [컬럼 설정] 이다.
        [TestMethod]
        public void 조회_한_줄의_끝은_조회_조회조건_컬럼설정_순서다()
        {
            RunSta(() =>
            {
                var screen = new UcWorkbench();
                var group = Field<LayoutControlGroup>(screen, "lcgSearch");

                var names = new List<string>();
                foreach (BaseLayoutItem item in group.Items)
                {
                    names.Add(item.Name);
                }

                int end = names.Count;
                CollectionAssert.AreEqual(
                    new List<string> { "lciSearch", "lciConditions", "lciColumns" },
                    names.GetRange(end - 3, 3),
                    "끝 셋의 순서가 WF-PAT-01 과 다르다: " + string.Join(" ", names.ToArray()));
            });
        }

        // [X] **넷을 다 켜면 한 줄이 넘칠 수 있다.** 항목마다 Min=Max 로 못 박혀 있어 좁아지지
        //     않고, 넘치면 LayoutControl 안에 가로 스크롤이 서서 [조회] 무리가 화면 밖으로
        //     밀린다. WF-PAT-01 에서 실제로 넘친 자리다 (조건 다섯 · 실측 1176 > 1144).
        [TestMethod]
        public void 조회조건을_다_켜도_한_줄에_들어간다()
        {
            RunSta(() =>
            {
                var screen = new UcWorkbench();
                var group = Field<LayoutControlGroup>(screen, "lcgSearch");

                int need = 0;
                foreach (BaseLayoutItem item in group.Items)
                {
                    // EmptySpaceItem 은 남는 자리를 빨아들이는 쪽이라 세지 않는다.
                    if (item is EmptySpaceItem)
                    {
                        continue;
                    }

                    need += item.MaxSize.Width > 0 ? item.MaxSize.Width : item.Size.Width;
                }

                Assert.IsTrue(need <= group.Size.Width,
                    "조회 한 줄이 " + need + "px 인데 자리는 " + group.Size.Width + "px 다");
            });
        }

        // 03 §9.3 — 조회조건 넷. 무엇을 낼지는 [조회 조건] 드롭다운이 정한다 (WF-PAT-01 과 같다).
        [TestMethod]
        public void 조회조건은_넷이고_기본은_전부_켜져_있다()
        {
            RunSta(() =>
            {
                var screen = new UcWorkbench();
                CheckedListBoxControl list = Field<CheckedListBoxControl>(screen, "clbConditions");

                Assert.AreEqual(4, list.Items.Count, "조회조건이 넷이 아니다");
                foreach (string name in new[] { "lciDateFrom", "lciDateTo", "lciStatus", "lciChartNo", "lciName" })
                {
                    Assert.AreEqual(LayoutVisibility.Always, Item(screen, name).Visibility, name);
                }
            });
        }

        // [X] 기간은 조건 하나가 칸 **둘**을 여닫는다. 하나만 사라지면 반쪽짜리 조건이 남는다.
        [TestMethod]
        public void 기간_조건을_끄면_두_칸이_함께_사라지고_값도_비워진다()
        {
            RunSta(() =>
            {
                var screen = new UcWorkbench();
                var view = (IWorkbenchView)screen;
                Assert.IsNotNull(view.FromDate, "시작일은 오늘로 서 있어야 한다");

                // 종료일은 기본이 비어 있다(§9.3 `From만 있으면 이후`). 끄기가 두 칸을 다
                // 비우는지 보려면 먼저 채워 둬야 이 시험이 뜻을 갖는다.
                Field<DateEdit>(screen, "deTo").EditValue = DateTime.Today.AddDays(3);
                Assert.IsNotNull(view.ToDate);

                // 사용자가 목록에서 체크를 끄는 것과 같은 경로다. 기간이 첫 항목이다.
                Field<CheckedListBoxControl>(screen, "clbConditions").ToggleItem(0);

                Assert.AreEqual(LayoutVisibility.Never, Item(screen, "lciDateFrom").Visibility);
                Assert.AreEqual(LayoutVisibility.Never, Item(screen, "lciDateTo").Visibility);
                Assert.AreEqual(LayoutVisibility.Always, Item(screen, "lciChartNo").Visibility, "켜 둔 조건까지 사라졌다");
                Assert.IsNull(view.FromDate, "숨긴 칸에 값이 남아 조회에 섞인다");
                Assert.IsNull(view.ToDate, "숨긴 칸에 값이 남아 조회에 섞인다");
            });
        }

        // 03 §9.3 — 상태 드롭다운은 `전체` 를 포함해 다섯이고 `전체` 만 값이 없다.
        [TestMethod]
        public void 상태_드롭다운은_전체를_포함해_다섯이고_전체는_값이_없다()
        {
            RunSta(() =>
            {
                var screen = new UcWorkbench();
                var combo = Field<ImageComboBoxEdit>(screen, "cboStatus");

                Assert.AreEqual(5, combo.Properties.Items.Count);
                Assert.IsNull(((IWorkbenchView)screen).StatusCode, "기본이 `전체` 가 아니다");
            });
        }

        // 03 §18 — 컬럼 목록은 Grid 가 가진 것에서 만들고, 되돌리는 길은 [기본값 복원] 하나다.
        [TestMethod]
        public void 컬럼_체크를_끄면_그_컬럼이_사라지고_기본값_복원이_되살린다()
        {
            RunSta(() =>
            {
                var screen = new UcWorkbench();
                GridView grid = Grid(screen);
                CheckedListBoxControl list = Field<CheckedListBoxControl>(screen, "clbColumns");

                Assert.AreEqual(grid.Columns.Count, list.Items.Count, "컬럼 목록이 Grid 와 다르다");

                int chartNo = IndexOfColumn(list, "차트번호");
                Assert.IsTrue(list.GetItemChecked(chartNo), "차트번호는 기본 컬럼이다");

                list.ToggleItem(chartNo);
                Assert.IsFalse(Column(grid, "차트번호").Visible, "체크를 껐는데 컬럼이 남았다");

                // [X] `SimpleButton.PerformClick()` 은 안 뜬 폼의 버튼에서 아무 일도 하지 않는다.
                //     Designer 가 이름으로 잇는 그 핸들러를 직접 부른다.
                Invoke(screen, "btnColumnsDefault_Click");

                Assert.IsTrue(Column(grid, "차트번호").Visible, "기본값 복원이 컬럼을 되살리지 못했다");
                Assert.IsTrue(list.GetItemChecked(chartNo), "Grid 는 돌아왔는데 체크가 어긋났다");
            });
        }

        // 03 §9.4 — 기본 다섯 · 선택 셋. 선택 컬럼은 처음에 숨어 있다.
        [TestMethod]
        public void Grid_는_기본_다섯에_선택_컬럼_셋을_숨겨_둔다()
        {
            RunSta(() =>
            {
                var screen = new UcWorkbench();
                GridView grid = Grid(screen);

                foreach (string caption in new[] { "예약/접수일", "시간대", "상태", "이름", "차트번호" })
                {
                    Assert.IsTrue(Column(grid, caption).Visible, caption + " 이 기본 컬럼이 아니다");
                }

                foreach (string caption in new[] { "성별", "생년월일", "휴대전화번호" })
                {
                    Assert.IsFalse(Column(grid, caption).Visible, caption + " 은 선택 컬럼이다");
                }
            });
        }

        // [X] `ColumnAutoWidth` 는 기본이 true 라 Grid 가 컬럼을 뷰 폭에 욱여넣는다. 가로로
        //     미는 지렛대는 `MinWidth` 하나뿐이다 — 컬럼을 새로 더하면서 빼먹으면 찌그러진다.
        [TestMethod]
        public void 모든_컬럼이_찌그러짐을_막을_MinWidth_를_갖는다()
        {
            RunSta(() =>
            {
                var screen = new UcWorkbench();
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

        private static void Invoke(UcWorkbench screen, string handler)
        {
            MethodInfo method = typeof(UcWorkbench).GetMethod(
                handler, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, handler + " 가 사라졌다");
            method.Invoke(screen, new object[] { null, EventArgs.Empty });
        }

        private static LayoutControlItem Item(UcWorkbench screen, string name)
        {
            return Field<LayoutControlItem>(screen, name);
        }

        private static GridView Grid(UcWorkbench screen)
        {
            return Field<GridView>(screen, "gvWorkList");
        }

        // Grid 와 배치 항목은 화면 내부 부품이라 View 계약에 나오지 않는다. 화면이 실제로
        // 어떻게 보이는지는 이것 말고 물어볼 데가 없어 Designer 필드를 직접 본다.
        private static T Field<T>(UcWorkbench screen, string name)
        {
            FieldInfo field = typeof(UcWorkbench).GetField(
                name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, name + " 필드가 사라졌다");
            return (T)field.GetValue(screen);
        }

        private static void RunSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);
                    action();
                }
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
