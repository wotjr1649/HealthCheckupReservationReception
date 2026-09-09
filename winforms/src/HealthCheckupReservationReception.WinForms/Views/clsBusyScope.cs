using System;
using System.Windows.Forms;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// 처리하는 동안 버튼을 잠그고 대기 커서를 세운다. `Dispose` 에서 되돌린다.
    ///
    /// [X] **조회·저장은 UI 스레드에서 동기로 SP 를 부른다.** 그동안 창은 얼어 있지만 클릭은
    ///     메시지 큐에 **쌓였다가** 핸들러가 끝나면 그대로 발화한다 — 두 번 누르면 두 번 돈다
    ///     (2026-09-10 사용자 지적). 눌린 순간 버튼을 잠그면 큐에 있던 클릭이 비활성 컨트롤로
    ///     배달돼 버려진다.
    ///
    /// [!] **`Application.DoEvents()` 를 쓰지 않는다.** 그것이 바로 쌓인 클릭을 통과시키는
    ///     길이라 문제를 키운다. 잠근 모습을 보이려면 그 컨트롤만 `Update()` 로 칠한다 —
    ///     메시지 펌프를 돌리지 않는 그리기다.
    ///
    /// [!] **이것만으로는 부족하다.** `[조회]` 는 화면 버튼과 Ribbon 버튼 **둘**이 같은 Action 을
    ///     부르므로, 실행 경로 쪽 재진입 가드와 **함께** 써야 구멍이 닫힌다.
    ///
    /// 킷 §2 — 같은 모양이 구체 화면 둘(`WF-PAT-01` 조회 · `DLG-PAT-01` 저장)에 생겨서 한 벌로 둔다.
    /// </summary>
    public sealed class clsBusyScope : IDisposable
    {
        private readonly Control[] _locked;
        private readonly bool[] _wasEnabled;
        private readonly Control _cursorHost;
        private readonly Cursor _oldCursor;
        private bool _done;

        /// <summary>
        /// <paramref name="cursorHost"/> 는 대기 커서를 세울 화면이다. `null` 이면 커서를 건드리지 않는다.
        /// <paramref name="locked"/> 중 `null` 은 건너뛴다 — 호출부가 존재 여부를 따지지 않게 한다.
        /// </summary>
        public clsBusyScope(Control cursorHost, params Control[] locked)
        {
            _locked = locked ?? new Control[0];
            _wasEnabled = new bool[_locked.Length];

            for (int i = 0; i < _locked.Length; i++)
            {
                Control c = _locked[i];
                if (c == null) { continue; }
                _wasEnabled[i] = c.Enabled;
                c.Enabled = false;
                // 동기 호출이라 여기서 칠하지 않으면 잠긴 모습이 보이지 않는다.
                c.Update();
            }

            _cursorHost = cursorHost;
            if (_cursorHost != null)
            {
                _oldCursor = _cursorHost.Cursor;
                _cursorHost.Cursor = Cursors.WaitCursor;
                _cursorHost.Update();
            }
        }

        public void Dispose()
        {
            if (_done) { return; }
            _done = true;

            if (_cursorHost != null && !_cursorHost.IsDisposed)
            {
                _cursorHost.Cursor = _oldCursor;
            }

            for (int i = 0; i < _locked.Length; i++)
            {
                Control c = _locked[i];
                // 처리 중에 창이 닫혔을 수 있다. 되돌릴 대상이 사라졌으면 넘어간다.
                if (c == null || c.IsDisposed) { continue; }
                c.Enabled = _wasEnabled[i];
            }
        }
    }
}
