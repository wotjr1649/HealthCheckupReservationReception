// ── 아이콘을 붙이는 한 자리 ──────────────────────────────────────────────────
//
// 리본 버튼과 대화상자 버튼이 같은 그림첩을 쓴다. 붙이는 방법을 두 곳에 두지 않는다
// (ROOT AGENTS.md §6) — 다른 것은 무엇에 붙이는가뿐이다.

using System;
using System.Drawing;
using System.IO;
using DevExpress.Utils.Svg;
using DevExpress.XtraBars;
using DevExpress.XtraEditors;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// `Resources/Icons/*.svg` 를 화면 요소에 붙인다.
    ///
    /// [I] SVG 를 쓰는 이유는 이 프로그램이 Per-Monitor DPI 를 켜기 때문이다
    ///     (`Program.cs` 의 첫 문장). 비트맵은 125%·150% 에서 뭉갠다.
    ///
    /// [!] 이름이 틀리면 `FromResources` 가 그 자리에서 터진다. 계약이 아니라 자산이라
    ///     게이트가 없으므로, **버튼 전부가 그림을 갖는지**를 시험이 대신 잰다.
    /// </summary>
    public static class clsGlyph
    {
        /// <summary>
        /// 리소스 이름의 앞머리. `RootNamespace` + 폴더 경로다 (05 §1.1 이 정한
        /// `HealthCheckupReservationReception` 이고 CFG-004 가 그것을 지킨다).
        /// </summary>
        private const string Root = "HealthCheckupReservationReception.Resources.";

        /// <summary>Ribbon 버튼에 붙인다.</summary>
        public static void Apply(BarItem item, string name)
        {
            item.ImageOptions.SvgImage = Load(name);
        }

        /// <summary>대화상자 버튼에 붙인다.</summary>
        public static void Apply(SimpleButton button, string name)
        {
            button.ImageOptions.SvgImage = Load(name);
        }

        /// <summary>
        /// 제목표시줄·작업표시줄의 아이콘. `csproj` 의 `ApplicationIcon` 은 **exe 파일의**
        /// 아이콘이고 폼이 띄우는 것은 이것이다 — 둘은 같은 `App.ico` 를 본다.
        /// </summary>
        public static Icon ApplicationIcon()
        {
            using (Stream stream = typeof(clsGlyph).Assembly.GetManifestResourceStream(Root + "App.ico"))
            {
                // 못 읽으면 기본 아이콘으로 둔다 — 아이콘 하나 때문에 프로그램이 서지 않는다.
                return stream == null ? null : new Icon(stream);
            }
        }

        private static SvgImage Load(string name)
        {
            return SvgImage.FromResources(Root + "Icons." + name + ".svg", typeof(clsGlyph).Assembly);
        }
    }
}
