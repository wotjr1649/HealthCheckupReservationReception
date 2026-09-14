using System;
using System.Windows.Forms;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// 화면 동작 한 번을 **정말 한 번으로** 만든다 — 조회·저장·접수·휴무일 CRUD·Ribbon
    /// 업무동작이 전부 이 길로 간다.
    ///
    /// **가드가 둘 다 필요하다** (2026-09-10 사용자 결정).
    /// <see cref="clsBusyScope"/> 는 사용자가 잠긴 것을 **보게** 하고, 여기의 재진입
    /// 가드는 그 사이 들어온 Enter·연타를 막는다. 하나만으로는 구멍이 닫히지 않는다 —
    /// 조회는 UI 스레드에서 동기로 SP 를 부르고, 그동안 쌓인 클릭은 끝난 뒤 발화한다.
    ///
    /// [X] **이 가드가 있던 곳은 두 곳뿐이었다** (2026-09-14 실측). 조회(WF-PAT-01 ·
    ///     WF-WRK-01)와 수검자 저장(DLG-PAT-01) — 둘 다 사용자가 겪은 뒤에 붙은 것이다
    ///     (2026-09-10 사용자 지적). 나머지 여섯 경로는 `clsBusyScope(this)` 만 들고 있었고,
    ///     그것은 **잠글 컨트롤을 받지 않으면 커서만 바꾼다.** 예약 저장 · 추가검사 저장 ·
    ///     접수처리 · 휴무일 추가/수정/삭제 · 휴무일 조회 · Ribbon 업무동작이 그랬다.
    ///     규칙을 두 곳에서 배우고 나머지에 퍼뜨리지 않은 것이다 (ROOT AGENTS.md §6).
    /// </summary>
    public sealed class clsActionRunner
    {
        private readonly Control _cursorHost;
        private readonly Control[] _locked;
        private bool _running;

        /// <summary>
        /// 인자는 <see cref="clsBusyScope"/> 에 그대로 넘어간다 — 대기 커서를 세울 화면과,
        /// 도는 동안 잠글 컨트롤들이다.
        ///
        /// [!] **쓰기는 `[닫기]` 까지 잠근다. 읽기는 그 버튼만 잠근다.**
        ///     호출이 동기라 도는 동안 창은 얼어 있고, 그때 누른 `[닫기]` 는 끝난 뒤에야
        ///     발화한다. 쓰기가 실패하면 창은 사유를 적은 채 남는데 — 그 사유는 모달이
        ///     아니라 인라인 한 줄이다 — 뒤늦게 발화한 클릭 하나가 **사유와 입력을 함께
        ///     지운다.** 조회는 다시 누르면 그만이므로 그 자리까지 잠그지 않는다.
        /// </summary>
        public clsActionRunner(Control cursorHost, params Control[] locked)
        {
            _cursorHost = cursorHost;
            _locked = locked;
        }

        /// <summary>
        /// 동작을 한 번 돌린다. 이미 돌고 있으면 아무 일도 하지 않는다.
        /// 이벤트가 아닌 것(휴무일 추가/수정/삭제처럼 Presenter 를 바로 부르는 길)은 이쪽이다.
        /// </summary>
        public void Run(Action body)
        {
            if (body == null || _running) { return; }

            _running = true;
            try
            {
                using (new clsBusyScope(_cursorHost, _locked))
                {
                    body();
                }
            }
            finally
            {
                _running = false;
            }
        }

        /// <summary>
        /// 이벤트를 한 번 올린다.
        ///
        /// <paramref name="handler"/> 는 **읽어 둔 델리게이트**를 받는다 — 화면이
        /// `SaveRequested` 를 그대로 넘기면 그 시점의 구독자에게 간다.
        /// </summary>
        public void Run(EventHandler handler, object sender)
        {
            if (handler == null) { return; }

            Run(delegate { handler(sender, EventArgs.Empty); });
        }

        /// <summary>
        /// 조회조건 칸에서 Enter 를 치면 `[조회]` 와 같은 일이 난다 (2026-09-10 사용자 결정).
        /// 조회 화면만 쓴다.
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
