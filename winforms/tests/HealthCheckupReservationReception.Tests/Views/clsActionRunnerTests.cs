using System;
using System.Windows.Forms;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// [R17] 조회 재진입 가드. **2026-09-14 이전에는 이 규칙에 시험이 없었다** —
    /// 두 화면(WF-PAT-01 · WF-WRK-01)에 같은 18줄이 복사돼 있었고, 화면 시험 18건은
    /// 전부 배치·컬럼·조회조건만 본다. 규칙을 <see cref="clsActionRunner"/> 한 곳으로
    /// 모으면서 검사도 함께 붙인다 (ROOT AGENTS.md §6 — 검사를 만들 수 없으면 적지 않는다).
    ///
    /// 화면이 없어도 돌아간다 — `clsActionRunner` 는 DevExpress 를 쓰지 않고 `Control`
    /// 하나면 선다.
    /// </summary>
    [TestClass]
    public class clsActionRunnerTests
    {
        // 대상: clsActionRunner — 조회 재진입 가드 (R17 로 두 화면에서 이리 모았다)
        // 목적: 조회는 UI 스레드에서 동기로 SP 를 부른다. 그동안 쌓인 클릭·Enter 는 핸들러가
        //       끝난 뒤 그대로 발화하므로, 가드가 없으면 두 번 누른 만큼 SP 가 두 번 나간다.
        // 확인: 핸들러가 도는 중에 같은 Run 을 다시 불러도 안쪽 동작이 정확히 1회만 실행된다.
        [TestMethod]
        public void 도는_동안_다시_불러도_한_번만_돈다()
        {
            using (var host = new Control())
            {
                var runner = new clsActionRunner(host);
                int calls = 0;
                EventHandler handler = null;

                // 조회 중에 Enter·연타가 되돌아오는 상황을 그대로 만든다.
                //
                // [X] **되부르는 횟수를 묶어 둔다.** 가드가 없으면 여기는 무한재귀가 되어
                //     StackOverflow 로 시험 호스트째 죽는다 — 그러면 "가드가 없다" 와
                //     "시험이 깨졌다" 가 구별되지 않는다. 다섯에서 끊으면 가드가 없을 때
                //     calls 가 5 로 올라오고, 있으면 1 이다.
                handler = delegate
                {
                    calls++;
                    if (calls < 5) { runner.Run(handler, host); }
                };

                runner.Run(handler, host);

                Assert.AreEqual(1, calls, "재진입은 막힌다 — 이것이 없으면 SP 가 두 번 돈다");
            }
        }

        // 대상: clsActionRunner — 가드 해제
        // 목적: 재진입을 막는 것과 한 번만 도는 것은 다르다. 가드가 풀리지 않으면 화면이 한 번
        //       조회하고 영영 굳어, 조작자는 프로그램이 죽은 것으로 본다.
        // 확인: Run 을 끝낸 뒤 다시 부르면 동작이 2회 실행된다 — 가드는 도는 동안만이다.
        [TestMethod]
        public void 끝나고_나면_다시_돌_수_있다()
        {
            using (var host = new Control())
            {
                var runner = new clsActionRunner(host);
                int calls = 0;
                EventHandler handler = delegate { calls++; };

                runner.Run(handler, host);
                runner.Run(handler, host);

                Assert.AreEqual(2, calls, "가드는 도는 동안만이다 — 영영 잠기면 안 된다");
            }
        }

        // 대상: clsActionRunner — 안쪽 동작이 예외를 던졌을 때의 가드 복구
        // 목적: 예외가 가드를 켜 둔 채로 빠져나가면 실패 한 번에 화면이 죽는다. 되돌리기는
        //       finally 의 몫이고, 그것이 실제로 도는지는 시험이 아니면 드러나지 않는다.
        // 확인: 안쪽에서 던진 예외가 호출자까지 그대로 올라오고, 그 뒤 다시 부르면 동작이
        //       실행된다 (가드가 풀려 있다).
        [TestMethod]
        public void 안에서_터져도_다음_조회가_열린다()
        {
            using (var host = new Control())
            {
                var runner = new clsActionRunner(host);
                int calls = 0;

                try
                {
                    runner.Run(delegate { throw new InvalidOperationException("SP 실패"); }, host);
                    Assert.Fail("예외가 호출자까지 올라와야 한다");
                }
                catch (InvalidOperationException)
                {
                }

                runner.Run(delegate { calls++; }, host);

                Assert.AreEqual(1, calls, "finally 가 가드를 풀지 않으면 화면이 영영 조회를 못 한다");
            }
        }

        // 대상: clsActionRunner — 이벤트 핸들러가 아닌 직접 호출 경로
        // 목적: 조회는 [조회] 버튼·Enter·화면 진입 세 길로 들어온다. 한 길만 가드를 받으면
        //       나머지 길로 들어온 중복 호출이 그대로 SP 로 나간다.
        // 확인: Action 경로로 재진입해도 안쪽 동작이 1회만 실행된다.
        [TestMethod]
        public void 이벤트가_아닌_동작도_같은_가드를_받는다()
        {
            using (var host = new Control())
            {
                var runner = new clsActionRunner(host);
                int calls = 0;
                Action body = null;

                body = delegate
                {
                    calls++;
                    if (calls < 5) { runner.Run(body); }
                };

                runner.Run(body);

                Assert.AreEqual(1, calls, "Action 경로도 재진입을 막는다");
            }
        }

        // 대상: clsActionRunner — 아직 아무도 구독하지 않은 상태에서의 호출
        // 목적: 화면을 만드는 중에는 이벤트에 구독자가 붙기 전인 순간이 있다. 그때 null 참조로
        //       터지면 화면이 열리지도 않는다.
        // 확인: 구독자 없이 Run(null, host) 을 불러도 예외 없이 지나간다.
        [TestMethod]
        public void 구독자가_없으면_아무_일도_하지_않는다()
        {
            using (var host = new Control())
            {
                new clsActionRunner(host).Run(null, host);
            }
        }

        // 대상: clsActionRunner — 조회조건 입력칸의 KeyDown 처리
        // 목적: 03 §5.2 는 조회를 Enter 로 정했다. 처리한 키를 위로 흘려보내면 폼의 AcceptButton
        //       이 한 번 더 반응해 같은 조회가 두 번 나가고, 처리 표시를 안 하면 경고음이 난다.
        // 확인: Tab 은 조회를 부르지 않고 Handled=false 로 그대로 흘려보낸다. Enter 는 조회를
        //       1회 부르고 Handled·SuppressKeyPress 가 모두 true 다 (위로 새지 않고 경고음도 없다).
        [TestMethod]
        public void Enter_만_조회이고_그_키는_위로_새지_않는다()
        {
            using (var host = new Control())
            {
                var runner = new clsActionRunner(host);
                int calls = 0;
                EventHandler handler = delegate { calls++; };

                var tab = new KeyEventArgs(Keys.Tab);
                runner.RunOnEnter(tab, handler, host);
                Assert.AreEqual(0, calls, "Enter 가 아니면 조회하지 않는다");
                Assert.IsFalse(tab.Handled, "다른 키는 그대로 흘려보낸다");

                var enter = new KeyEventArgs(Keys.Enter);
                runner.RunOnEnter(enter, handler, host);
                Assert.AreEqual(1, calls, "Enter 는 [조회] 와 같은 일이다");
                Assert.IsTrue(enter.Handled, "Enter 가 위로 새면 상위 폼이 한 번 더 받는다");
                Assert.IsTrue(enter.SuppressKeyPress, "경고음도 나지 않아야 한다");
            }
        }
    }
}
