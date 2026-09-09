using System;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// 03 §18 Column Chooser. **표시/숨김과 기본값 복원, 그 둘뿐이다** — 사용자별 Layout
    /// 저장도 개인화도 하지 않는다(§18 제외).
    ///
    /// [D] 2026-09-09 사용자 결정 — DevExpress 기본 Customization Form 의 더블클릭·드래그
    ///     대신 체크박스로 고른다. 기본 폼에는 `기본값 복원` 을 넣을 자리가 없고 제목·검색
    ///     문구가 영문이라 이 폼을 따로 둔다.
    ///
    /// Grid 하나를 받아 그 컬럼을 다룬다. 화면에 매이지 않으므로 다른 Grid 도 같은 정책을
    /// 그대로 쓴다 — `03` §18 이 애초에 Grid 전체를 다스리는 정책이다.
    /// </summary>
    public partial class FrmColumnChooser : XtraForm
    {
        private GridView _view;
        private Action _restoreDefaults;
        private bool _loading;

        public FrmColumnChooser()
        {
            InitializeComponent();
        }

        /// <summary>
        /// <paramref name="restoreDefaults"/> 는 Grid 를 처음 상태로 되돌린다. 무엇이
        /// 처음인지는 Grid 를 가진 화면만 알고 있으므로 여기서 정하지 않는다.
        /// </summary>
        public void Bind(GridView view, Action restoreDefaults)
        {
            _view = view;
            _restoreDefaults = restoreDefaults;
            Reload();
        }

        private void Reload()
        {
            _loading = true;
            try
            {
                clbColumns.Items.Clear();
                foreach (GridColumn column in _view.Columns)
                {
                    // 03 §18 — 허용 컬럼 후보만 제공한다. 내부키·정규화 컬럼·동시성값은
                    // 애초에 Grid 의 컬럼이 아니므로 여기에도 나타나지 않는다.
                    if (column.OptionsColumn.ShowInCustomizationForm)
                    {
                        clbColumns.Items.Add(new Entry(column), column.Visible);
                    }
                }
            }
            finally
            {
                _loading = false;
            }
        }

        private void clbColumns_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_loading)
            {
                return;
            }

            var entry = clbColumns.Items[e.Index].Value as Entry;
            if (entry == null)
            {
                return;
            }

            // 체크하는 즉시 반영한다. 확인/취소를 두지 않는 것은 되돌리는 값이 `기본값 복원`
            // 하나로 충분하기 때문이다 (03 §18).
            entry.Column.Visible = e.State == System.Windows.Forms.CheckState.Checked;
        }

        private void btnRestoreDefault_Click(object sender, EventArgs e)
        {
            _restoreDefaults();
            Reload();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>목록에 실을 한 줄. `Caption` 을 그대로 보여준다.</summary>
        private sealed class Entry
        {
            public Entry(GridColumn column)
            {
                Column = column;
            }

            public GridColumn Column { get; private set; }

            public override string ToString()
            {
                return Column.Caption;
            }
        }
    }
}
