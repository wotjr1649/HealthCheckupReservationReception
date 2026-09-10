using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraLayout;
using DevExpress.XtraLayout.Utils;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// `[조회 조건]` 드롭다운 — 어느 조회조건 칸을 낼지 정하는 체크 목록 (2026-09-10 사용자 결정).
    ///
    /// 킷 §2 는 같은 모양이 **구체 화면 둘**에 생긴 뒤에 공통으로 뽑으라고 한다. WF-PAT-01 과
    /// WF-WRK-01 이 그 둘이다. 목록 항목·표시글·숨김 규칙이 한 벌만 있게 된다.
    ///
    /// [X] **끈 조건은 값도 버린다.** 안 보이는 칸에 남은 글자가 조회에 섞이면 사용자는 왜 그
    ///     결과가 나왔는지 알 길이 없다 — Presenter 는 칸을 그대로 읽는다.
    ///
    /// 항목을 Designer 에 적지 않는다. 캡션과 그 캡션이 여닫는 칸이 한 줄에 함께 있어야
    /// 둘이 어긋나지 않는다 (ROOT AGENTS.md §6).
    /// </summary>
    public sealed class clsSearchConditions
    {
        /// <summary>드롭다운은 무엇이 골라졌는지를 적지 않고 늘 제 이름을 적는다.</summary>
        public const string Caption = "조회 조건";

        private readonly CheckedListBoxControl _list;

        // 목록을 코드가 고쳐 쓰는 동안 ItemCheck 이 되돌아오는 것을 막는다.
        private bool _syncing;

        public clsSearchConditions(CheckedListBoxControl list)
        {
            _list = list;
        }

        /// <summary>
        /// 조회조건 하나를 목록에 더하고 처음 상태를 화면에 바른다.
        /// 칸이 둘 이상인 조건(기간 From~To)은 돌려받은 것에 <see cref="clsSearchCondition.Also"/> 로 잇는다.
        /// </summary>
        public clsSearchCondition Add(string caption, bool on, LayoutControlItem item, BaseEdit editor)
        {
            var condition = new clsSearchCondition();
            condition.Also(item, editor);
            _list.Items.Add(new CheckedListBoxItem(
                condition, caption, on ? CheckState.Checked : CheckState.Unchecked, true));
            condition.Show(on);
            return condition;
        }

        /// <summary>
        /// 화면의 `clbConditions_ItemCheck` 이 그대로 넘겨준다.
        ///
        /// [X] `ItemCheck` 이 항목에 반영되기 **전**에 오는지 뒤에 오는지는 재지 않는다 —
        ///     어느 쪽이든 `e.State` 를 한 번 더 써 넣으면 같은 값이 된다. 재진입만 막으면 된다.
        /// </summary>
        public void Toggle(DevExpress.XtraEditors.Controls.ItemCheckEventArgs e)
        {
            if (_syncing) { return; }

            _syncing = true;
            try
            {
                _list.Items[e.Index].CheckState = e.State;
                var condition = _list.Items[e.Index].Value as clsSearchCondition;
                if (condition != null)
                {
                    condition.Show(e.State == CheckState.Checked);
                }
            }
            finally { _syncing = false; }
        }
    }

    /// <summary>조회조건 하나. 칸 하나일 수도, 기간처럼 둘일 수도 있다.</summary>
    public sealed class clsSearchCondition
    {
        private readonly List<LayoutControlItem> _items = new List<LayoutControlItem>();
        private readonly List<BaseEdit> _editors = new List<BaseEdit>();

        /// <summary>같은 조건이 여닫는 칸을 하나 더 잇는다 (`예약/접수일 [From] ~ [To]`).</summary>
        public clsSearchCondition Also(LayoutControlItem item, BaseEdit editor)
        {
            _items.Add(item);
            _editors.Add(editor);
            return this;
        }

        internal void Show(bool on)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Visibility = on ? LayoutVisibility.Always : LayoutVisibility.Never;
                if (!on && _editors[i] != null)
                {
                    _editors[i].EditValue = null;
                }
            }
        }
    }
}
