using System;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// Grid 가 잡아 둔 행과 **사용자가 고른 행**을 가른다.
    ///
    /// [X] GridView 는 행이 있으면 반드시 하나를 focus 한다. `FocusedRowHandle =
    ///     InvalidRowHandle` 은 대입 직후 다시 0 이 된다(실측 2026-09-10). 그래서 focus 를
    ///     없애는 대신 **선택으로 보이는 것**을 끄고, 그 상태를 대상 판정의 기준으로 삼는다.
    ///     화면이 잡아 둔 행을 대상으로 넘기면 사용자가 고르지도 않은 행이 저장 SP 로 간다.
    ///
    /// [X] 목록을 다시 실으면 Grid 가 0행을 자동으로 잡는다. 그 잠깐의 선택이 위로 올라가면
    ///     방금 비운 상세를 곧바로 다시 조회하게 된다 — <see cref="Rebind"/> 가 막는다.
    ///
    /// 킷 §2 — WF-PAT-01 과 WF-WRK-01 두 구체 화면에 같은 모양이 생겨서 한 벌로 뽑았다.
    /// 행 타입은 화면마다 다르므로 여기서는 <see cref="Row"/> 를 object 로 낸다.
    /// </summary>
    public sealed class clsGridRowPicker
    {
        private readonly GridView _view;

        private bool _suppress;
        private bool _picked;
        private object _row;

        public clsGridRowPicker(GridView view)
        {
            _view = view;
            Show(false);
        }

        /// <summary>고른 행이 바뀌었다. 선택이 풀린 것도 여기로 온다.</summary>
        public event EventHandler PickChanged;

        /// <summary>사용자가 고른 행. 고르지 않았으면 null 이다.</summary>
        public object Row
        {
            get { return _picked ? _row : null; }
        }

        /// <summary>
        /// 목록을 새로 싣는다. 싣는 동안 선택 이벤트를 막고, 끝나면 선택 표시를 끈다 —
        /// 재조회 시 선택·상세를 Clear 하는 것은 03 §5.3 · §9.4 의 계약이다.
        /// </summary>
        public void Rebind(GridControl grid, object dataSource)
        {
            _suppress = true;
            try
            {
                grid.DataSource = dataSource;
            }
            finally
            {
                _suppress = false;
            }

            _row = null;
            Show(false);
        }

        /// <summary>
        /// 키보드 이동. Designer 가 이름으로 잇는 `FocusedRowChanged` 가 그대로 넘겨준다.
        /// </summary>
        public void FocusedRowChanged(int rowHandle)
        {
            Pick(rowHandle);
        }

        /// <summary>
        /// 조회 직후 이미 focus 되어 있는 첫 행을 사용자가 처음 누르는 경우는
        /// `FocusedRowChanged` 가 나지 않아 이 길로만 잡힌다.
        /// </summary>
        public void RowClick(int rowHandle)
        {
            if (!_picked)
            {
                Pick(rowHandle);
            }
        }

        private void Pick(int rowHandle)
        {
            if (_suppress)
            {
                return;
            }

            _row = _view.GetRow(rowHandle);
            Show(_row != null);

            EventHandler handler = PickChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void Show(bool on)
        {
            _picked = on;
            _view.OptionsSelection.EnableAppearanceFocusedRow = on;
            _view.OptionsSelection.EnableAppearanceFocusedCell = on;
        }
    }
}
