using System;
using System.Windows.Forms;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// **되돌리기가 남의 변경을 덮으면 안 된다.**
    ///
    /// `[X]` 실제 결함에서 나왔다 (2026-09-14 코드리뷰). `clsBusyScope` 는 생성 시점의
    ///      `Enabled` 를 떠 두었다가 `Dispose` 에서 그대로 되돌린다. 그런데 잠그는 버튼이
    ///      Presenter 가 구동하는 바로 그 버튼이다 — 스코프 **안에서** Presenter 가 값을
    ///      바꾸면 `Dispose` 가 그것을 옛 값으로 덮는다.
    ///
    ///      DLG-HOL-01 `[추가]` 가 그랬다. 등록이 끝나면 Presenter 가 새 행을 골라
    ///      `RowActionsEnabled = true` 를 주는데, `Dispose` 가 Designer 기본값 `false` 로
    ///      되돌려 `[수정]`·`[삭제]` 가 회색으로 남았다. 반대로 `[삭제]` 뒤에는 선택이
    ///      풀려 `false` 가 되어야 하는데 `true` 로 되살아나 **눌러도 아무 일도 없는 버튼**이
    ///      됐다.
    ///
    /// `[X]` **Presenter 시험은 이것을 못 본다.** fake view 가 `RowActionsEnabled` 를 받아
    ///      기억할 뿐 실제 `Control.Enabled` 를 거치지 않는다. 그 틈이 여기다.
    /// </summary>
    [TestClass]
    public class clsBusyScopeTests
    {
        [TestMethod]
        public void 도는_동안_잠기고_끝나면_원래대로_돌아온다()
        {
            using (var host = new Control())
            using (var button = new Control())
            {
                button.Enabled = true;

                using (new clsBusyScope(host, button))
                {
                    Assert.IsFalse(button.Enabled, "도는 동안 잠겨야 쌓인 클릭이 버려진다");
                }

                Assert.IsTrue(button.Enabled, "아무도 안 건드렸으면 원래대로 돌아온다");
            }
        }

        [TestMethod]
        public void 스코프_안에서_남이_켠_것을_덮지_않는다()
        {
            using (var host = new Control())
            using (var button = new Control())
            {
                // DLG-HOL-01 [추가]: 눌린 시점에는 꺼져 있고, 등록이 끝나면 Presenter 가 켠다.
                button.Enabled = false;

                using (new clsBusyScope(host, button))
                {
                    clsBusyScope.SetEnabled(button, true);
                }

                Assert.IsTrue(button.Enabled,
                    "Presenter 가 켠 것을 되돌리기가 껐다 — 방금 등록한 행인데 [수정]이 회색이다");
            }
        }

        [TestMethod]
        public void 스코프_안에서_남이_끈_것을_덮지_않는다()
        {
            using (var host = new Control())
            using (var button = new Control())
            {
                // DLG-HOL-01 [삭제]: 눌린 시점에는 켜져 있고, 삭제가 끝나면 선택이 풀려 꺼진다.
                button.Enabled = true;

                using (new clsBusyScope(host, button))
                {
                    clsBusyScope.SetEnabled(button, false);
                }

                Assert.IsFalse(button.Enabled,
                    "Presenter 가 끈 것을 되돌리기가 켰다 — 눌러도 아무 일도 없는 버튼이 된다");
            }
        }

        [TestMethod]
        public void 잠겨_있지_않으면_그대로_쓴다()
        {
            using (var button = new Control())
            {
                button.Enabled = true;
                clsBusyScope.SetEnabled(button, false);
                Assert.IsFalse(button.Enabled, "잠금 밖에서는 호출부가 잠금 여부를 따지지 않아도 된다");
            }
        }

        [TestMethod]
        public void null_은_건너뛴다()
        {
            using (var host = new Control())
            {
                using (new clsBusyScope(host, null, null)) { }
            }
        }
    }
}
