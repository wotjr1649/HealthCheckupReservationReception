using System;
using System.Windows.Forms;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// [R17] 목록 화면의 `[조회]` 한 번을 **정말 한 번으로** 만든다.
    ///
    /// **가드가 둘 다 필요하다** (2026-09-10 사용자 결정).
    /// <see cref="clsBusyScope"/> 는 사용자가 잠긴 것을 **보게** 하고, 여기의 재진입
    /// 가드는 그 사이 들어온 Enter·연타를 막는다. 하나만으로는 구멍이 닫히지 않는다 —
    /// 조회는 UI 스레드에서 동기로 SP 를 부르고, 그동안 쌓인 클릭은 끝난 뒤 발화한다.
    ///
    /// 킷 §2 — 같은 모양이 구체 화면 둘(WF-PAT-01 · WF-WRK-01)에 있었다. 2026-09-14
    /// 실측으로 두 벌의 본문이 한 글자도 다르지 않은 것을 확인하고 한 벌로 모았다.
    /// 규칙이 두 곳에 있으면 한 곳만 고쳐지는 날이 온다 (ROOT AGENTS.md §6).
    /// </summary>
    public sealed class clsSearchRunner
    {
        private readonly Control _cursorHost;
        private readonly Control[] _locked;
        private bool _running;

        /// <summary>
        /// 인자는 <see cref="clsBusyScope"/> 에 그대로 넘어간다 — 대기 커서를 세울 화면과,
        /// 도는 동안 잠글 컨트롤들이다.
        /// </summary>
        public clsSearchRunner(Control cursorHost, params Control[] locked)
        {
            _cursorHost = cursorHost;
            _locked = locked;
        }

        /// <summary>
        /// 조회를 한 번 돌린다. 이미 돌고 있으면 아무 일도 하지 않는다.
        ///
        /// <paramref name="handler"/> 는 **읽어 둔 델리게이트**를 받는다 — 화면이
        /// `SearchRequested` 를 그대로 넘기면 그 시점의 구독자에게 간다.
        /// </summary>
        public void Run(EventHandler handler, object sender)
        {
            if (handler == null || _running) { return; }

            _running = true;
            try
            {
                using (new clsBusyScope(_cursorHost, _locked))
                {
                    handler(sender, EventArgs.Empty);
                }
            }
            finally
            {
                _running = false;
            }
        }

        /// <summary>
        /// 조회조건 칸에서 Enter 를 치면 `[조회]` 와 같은 일이 난다 (2026-09-10 사용자 결정).
        ///
        /// [X] `Form.AcceptButton` 을 쓰지 않는다. 그 속성은 폼이 갖는데 목록 화면은
        ///     UserControl 이고, MainForm 에 걸면 어느 업무 화면이 떠 있든 그 버튼이
        ///     Enter 를 먹는다.
        ///
        /// [X] **Enter 를 여기서 끊는다.** 위로 새어 나가면 상위 폼이 한 번 더 받는다.
        /// </summary>
        public void RunOnEnter(KeyEventArgs e, EventHandler handler, object sender)
        {
            if (e == null || e.KeyCode != Keys.Enter) { return; }

            e.Handled = true;
            e.SuppressKeyPress = true;
            Run(handler, sender);
        }
    }
}
