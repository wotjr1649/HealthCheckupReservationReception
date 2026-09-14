using System.Drawing;
using DevExpress.Utils;
using DevExpress.XtraEditors;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// 화면 한 줄 안내의 색. 역할은 넷뿐이고 **색을 고르는 자리는 여기 하나다.**
    /// 화면은 색이 아니라 역할을 말한다.
    ///
    /// [X] **모으기 전에 이미 어긋나 있었다** (2026-09-14 실측). 같은 `lblValidation` 이
    ///     FrmChangeLog·FrmExtraExam·FrmReception 에서는 `FromArgb(192, 0, 0)` 인데
    ///     UcWorkbench 에서만 `Firebrick` 이었다. 양쪽 주석은 똑같이 「붉은 글씨가 오류라는
    ///     유일한 단서다」라고 적혀 있었다 — 역할이 같은데 색이 갈린 것이다.
    ///     값이 아홉 곳에 흩어져 있으면 이렇게 된다 (ROOT AGENTS.md §6).
    ///
    /// 차단메시지(`lblBlock`)도 같은 `Error` 다. 05 §9.6 차단과 03 §8.11 저장 실패는
    /// 사용자에게 둘 다 「이래서 안 된다」이고, 둘을 색으로 가르는 규칙은 어디에도 없다.
    /// </summary>
    public static class clsNotice
    {
        /// <summary>오류·차단. 붉은 글씨가 그 자리가 오류라는 유일한 단서다.</summary>
        public static void Error(LabelControl label)
        {
            Tint(label, Color.FromArgb(192, 0, 0));
        }

        /// <summary>경고. 막지는 않지만 눈에는 띄어야 한다 (03 §24.6 만료 경고).</summary>
        public static void Warn(LabelControl label)
        {
            Tint(label, Color.DarkOrange);
        }

        /// <summary>통과. `접수 가능` 처럼 판정이 열렸다는 말이다 (03 §11.1).</summary>
        public static void Ok(LabelControl label)
        {
            Tint(label, Color.FromArgb(0, 96, 0));
        }

        /// <summary>안내. 읽어 두라는 말일 뿐 판정이 아니다.</summary>
        public static void Hint(LabelControl label)
        {
            Tint(label, Color.FromArgb(112, 112, 112));
        }

        /// <summary>
        /// 고를 수 없는 행. Grid 의 `RowStyle` 이 부른다 — 라벨과 달리 `UseForeColor` 를
        /// 켜지 않는다. RowStyle 이 넘겨주는 Appearance 는 그대로 쓰인다.
        ///
        /// [X] **여기도 갈려 있었다** (2026-09-14 실측). FrmExtraExam 은
        ///     `FromArgb(150, 150, 150)`, FrmReservation 은 `SystemColors.GrayText` 인데
        ///     **둘은 같은 AEX 목록을 같은 조건(`Selectable`)으로 칠하고 있었다.**
        ///     테마를 따라가는 쪽으로 맞췄다 — 고대비 테마에서 하드코딩한 회색은 안 보인다.
        /// </summary>
        public static void Disabled(AppearanceObject appearance)
        {
            appearance.ForeColor = SystemColors.GrayText;
        }

        /// <summary>
        /// [X] **`ForeColor` 만 넣으면 DevExpress 가 무시한다.** `Options.UseForeColor` 를
        ///     함께 켜야 Appearance 가 실제로 쓰인다. 그 두 줄이 아홉 곳에 있던 자리다.
        /// </summary>
        private static void Tint(LabelControl label, Color color)
        {
            label.Appearance.ForeColor = color;
            label.Appearance.Options.UseForeColor = true;
        }
    }
}
