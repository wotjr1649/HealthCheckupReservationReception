using System;
using System.Drawing;
using System.Threading;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// **화면 한 줄 안내의 색.** 역할은 넷뿐이고 색을 고르는 자리는 `clsNotice` 하나다.
    ///
    /// [X] **모으기 전에 이미 어긋나 있었다** (2026-09-14 실측). 같은 `lblValidation` 이
    ///     세 화면에서는 `FromArgb(192, 0, 0)` 인데 `UcWorkbench` 에서만 `Firebrick` 이었다.
    ///     값이 아홉 곳에 흩어져 있으면 이렇게 된다 (ROOT AGENTS.md §6).
    ///
    /// [X] **모은 뒤에도 아무 시험이 없었다** (2026-09-15 실측). 화면 시험은 안내 **문구**만
    ///     보고 색은 보지 않는다 — 네 역할이 같은 색이 되어도 전부 green 이다.
    /// </summary>
    [TestClass]
    public class clsNoticeTests
    {
        // 대상: clsNotice.Error · Warn · Ok · Hint — 안내 역할 넷의 색
        // 목적: 역할이 다른데 색이 같으면 조작자는 오류와 안내를 구분할 수 없다. 색을 고치다
        //       둘이 같아지는 것은 화면을 띄워 보지 않으면 모르고, 화면 시험은 문구만 본다.
        // 확인: 네 역할이 서로 다른 네 가지 색을 낸다 (같은 색이 하나도 없다).
        [TestMethod]
        public void 역할_넷은_서로_다른_색이다()
        {
            RunSta(delegate
            {
                Color error = ColorOf(clsNotice.Error);
                Color warn = ColorOf(clsNotice.Warn);
                Color ok = ColorOf(clsNotice.Ok);
                Color hint = ColorOf(clsNotice.Hint);

                var all = new[] { error, warn, ok, hint };
                for (int i = 0; i < all.Length; i++)
                {
                    for (int j = i + 1; j < all.Length; j++)
                    {
                        Assert.AreNotEqual(all[i].ToArgb(), all[j].ToArgb(),
                            "역할 " + i + " 과 " + j + " 의 색이 같다 — 조작자가 구분하지 못한다");
                    }
                }
            });
        }

        // 대상: clsNotice.Error · Warn · Ok · Hint — Appearance.Options.UseForeColor
        // 목적: DevExpress 는 ForeColor 만 넣으면 **무시한다**. Options.UseForeColor 를 함께
        //       켜야 실제로 칠해지고, 그 두 줄 짝이 아홉 곳에 흩어져 있던 자리다. 한쪽만
        //       남으면 색이 지정돼 있는데 화면은 검은 글씨로 뜬다 — 조용한 실패다.
        // 확인: 네 역할 모두 ForeColor 를 넣고 UseForeColor 를 켠다.
        [TestMethod]
        public void 색을_넣을_때_UseForeColor_를_함께_켠다()
        {
            RunSta(delegate
            {
                Action<LabelControl>[] roles =
                {
                    clsNotice.Error, clsNotice.Warn, clsNotice.Ok, clsNotice.Hint,
                };

                foreach (Action<LabelControl> role in roles)
                {
                    using (var label = new LabelControl())
                    {
                        role(label);
                        Assert.IsTrue(label.Appearance.Options.UseForeColor,
                            "UseForeColor 를 안 켜면 DevExpress 가 색을 무시한다");
                    }
                }
            });
        }

        // 대상: clsNotice.Disabled — Grid RowStyle 이 쓰는 「고를 수 없는 행」 색
        // 목적: 하드코딩한 회색은 고대비 테마에서 보이지 않는다. 이 자리도 두 화면에서
        //       `FromArgb(150,150,150)` 과 `SystemColors.GrayText` 로 갈려 있었는데
        //       둘은 같은 추가검사 목록을 같은 조건으로 칠하고 있었다 (2026-09-14 실측).
        //       테마를 따라가는 쪽으로 맞췄고, 그것이 되돌아가는 것을 여기서 잡는다.
        // 확인: 시스템 테마색 GrayText 이고, 라벨과 달리 UseForeColor 를 켜지 않는다
        //       (RowStyle 이 넘겨주는 Appearance 는 그대로 쓰이므로 켤 필요가 없다).
        [TestMethod]
        public void 고를_수_없는_행은_테마의_회색을_쓴다()
        {
            RunSta(delegate
            {
                var appearance = new AppearanceObject();

                clsNotice.Disabled(appearance);

                Assert.AreEqual(SystemColors.GrayText.ToArgb(), appearance.ForeColor.ToArgb(),
                    "하드코딩한 회색은 고대비 테마에서 안 보인다");
            });
        }

        private static Color ColorOf(Action<LabelControl> role)
        {
            using (var label = new LabelControl())
            {
                role(label);
                return label.Appearance.ForeColor;
            }
        }

        private static void RunSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(delegate ()
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) { throw new AssertFailedException(failure.Message, failure); }
        }
    }
}
