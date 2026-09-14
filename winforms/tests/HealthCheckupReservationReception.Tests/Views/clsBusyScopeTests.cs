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
        // 대상: clsBusyScope — 동작 중 컨트롤 잠금과 해제 시 복원
        // 목적: 잠금의 기본 계약이다. 도는 동안 꺼져야 쌓인 클릭이 버려지고, 끝나면 들어올 때
        //       값으로 돌아와야 조작자가 이어서 쓸 수 있다.
        // 확인: 스코프 안에서 버튼의 Enabled 가 false 이고, 스코프를 벗어나면 다시 true 다.
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

        // 대상: clsBusyScope — 스코프 안에서 Presenter 가 Enabled 를 바꾼 경우의 복원 규칙
        // 목적: 실제 결함에서 나왔다 (2026-09-14 코드리뷰). 되돌리기가 스코프 안의 변경을 덮으면
        //       Presenter 가 정한 Enabled 가 사라진다 — 화면도 Presenter 시험도 초록인데 버튼만
        //       죽는 형태라 이 시험이 아니면 드러나지 않는다. DLG-HOL-01 의 [추가] 가 그 경우다:
        //       눌린 시점에는 꺼져 있고 등록이 끝나면 Presenter 가 켠다.
        // 확인: 꺼진 채로 들어가 스코프 안에서 켠 버튼이, 스코프를 벗어난 뒤에도 켜져 있다.
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

        // 대상: clsBusyScope — 위와 반대 방향의 복원 규칙
        // 목적: DLG-HOL-01 의 [삭제] 가 그 경우다 — 눌린 시점에는 켜져 있고 삭제가 끝나면 선택이
        //       풀려 꺼진다. 되돌리기가 켜 버리면 이미 지워진 행에 대고 삭제 버튼이 열린다.
        // 확인: 켜진 채로 들어가 스코프 안에서 끈 버튼이, 스코프를 벗어난 뒤에도 꺼져 있다.
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

        // 대상: clsBusyScope.SetEnabled — 잠금 스코프 밖에서의 호출
        // 목적: 호출부가 「지금 잠겨 있나」를 따져야 하면 그 판단이 화면마다 복사된다.
        //       잠금 밖에서는 그냥 통하게 두는 것이 이 함수를 한 곳에 두는 이유다.
        // 확인: 스코프 없이 SetEnabled(button, false) 를 부르면 버튼이 그대로 꺼진다.
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

        // 대상: clsBusyScope — 잠글 컨트롤이 주어지지 않은 경로
        // 목적: 잠글 컨트롤이 없는 호출 경로가 있다. 거기서 null 참조로 터지면 잠금을 쓰는
        //       화면 전부가 못 연다.
        // 확인: 컨트롤 자리에 null 을 넘겨 스코프를 열고 닫아도 예외가 나지 않는다.
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
