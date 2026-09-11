using System.IO;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// 03 §18 `[컬럼 설정]` 드롭다운 — Grid 옆에서 컬럼을 켜고 끈다 (2026-09-10 사용자 결정으로
    /// Ribbon `[보기]` 의 Modal 에서 여기로 옮겼다. 목록을 보면서 켜고 끄는 편이 낫다).
    ///
    /// 킷 §2 — WF-PAT-01 과 WF-WRK-01 두 구체 화면에 같은 모양이 생겨서 한 벌로 뽑았다.
    ///
    /// **목록은 Grid 가 가진 컬럼에서 만든다** (ROOT AGENTS.md §6). 컬럼 이름을 여기에도
    /// Designer 에도 적으면 두 곳이 된다. 무엇이 `기본` 인지도 마찬가지라, Designer 가
    /// 직렬화한 그 상태를 화면이 서는 순간 한 벌 떠 두었다가 그대로 되돌린다.
    ///
    /// 확인/취소를 두지 않는다 — 되돌리는 길이 `[기본값 복원]` 하나로 충분하다.
    /// </summary>
    public sealed class clsColumnChooser
    {
        /// <summary>드롭다운은 무엇이 골라졌는지를 적지 않고 늘 제 이름을 적는다.</summary>
        public const string Caption = "컬럼 설정";

        private readonly GridView _view;
        private readonly CheckedListBoxControl _list;
        private readonly MemoryStream _defaultLayout = new MemoryStream();

        // 목록을 코드가 고쳐 쓰는 동안 ItemCheck 이 되돌아오는 것을 막는다.
        private bool _syncing;

        /// <summary>
        /// Designer 가 컬럼을 다 세운 뒤에 만든다 — 그 시점의 Grid 가 `기본값` 의 정의다.
        /// 내부키·정규화 컬럼은 애초에 Grid 의 컬럼이 아니므로 목록에도 없다.
        /// </summary>
        public clsColumnChooser(CheckedListBoxControl list, GridView view)
        {
            _list = list;
            _view = view;
            _view.SaveLayoutToStream(_defaultLayout);

            foreach (GridColumn column in _view.Columns)
            {
                if (!column.OptionsColumn.ShowInCustomizationForm)
                {
                    continue;
                }

                _list.Items.Add(new CheckedListBoxItem(
                    column,
                    column.Caption,
                    column.Visible ? CheckState.Checked : CheckState.Unchecked,
                    true));
            }
        }

        /// <summary>화면의 `clbColumns_ItemCheck` 이 그대로 넘겨준다.</summary>
        public void Toggle(DevExpress.XtraEditors.Controls.ItemCheckEventArgs e)
        {
            if (_syncing) { return; }

            _syncing = true;
            try
            {
                _list.Items[e.Index].CheckState = e.State;
                var column = _list.Items[e.Index].Value as GridColumn;
                if (column != null)
                {
                    column.Visible = e.State == CheckState.Checked;
                }
            }
            finally { _syncing = false; }
        }

        /// <summary>화면의 `btnColumnsDefault_Click` 이 부른다.</summary>
        public void RestoreDefault()
        {
            _defaultLayout.Position = 0;
            _view.RestoreLayoutFromStream(_defaultLayout);
            SyncChecks();
        }

        /// <summary>
        /// 체크 상태를 Grid 에서 받아 적는다. **Grid 가 단일 출처다** —
        /// 복원은 Grid 를 되돌리므로 목록도 거기서 다시 읽어야 어긋나지 않는다.
        /// </summary>
        private void SyncChecks()
        {
            _syncing = true;
            try
            {
                for (int i = 0; i < _list.Items.Count; i++)
                {
                    var column = _list.Items[i].Value as GridColumn;
                    _list.Items[i].CheckState = column != null && column.Visible
                        ? CheckState.Checked
                        : CheckState.Unchecked;
                }
            }
            finally { _syncing = false; }
        }
    }
}
