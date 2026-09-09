using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraEditors;
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
