// ── MainForm 의 Designer 밖 구성 ─────────────────────────────────────────────
//
// Designer 가 직렬화하지 못하는 것만 여기 있다 (킷 §5) — **리소스에서 읽어 오는 값**이다.
// 리터럴 캡션·폭·Options 는 Designer 에 그대로 둔다.

using System;
using System.Drawing;
using System.IO;
using DevExpress.Utils.Svg;
using DevExpress.XtraBars;

namespace HealthCheckupReservationReception.Views
{
    public partial class MainForm
    {
        /// <summary>
        /// 리소스 이름의 앞머리. `RootNamespace` + 폴더 경로다 (`05` §1.1 이 정한
        /// `HealthCheckupReservationReception` 이고 `CFG-004` 가 그것을 지킨다).
        /// </summary>
        private const string IconRoot = "HealthCheckupReservationReception.Resources.";

        /// <summary>
        /// 아이콘을 붙인다.
        ///
        /// [X] **버튼 열하나에 그림 아홉이다.** 「변경이력」 셋이 같은 것을 쓴다 — 같은 일을
        ///     하는 버튼이 Page 마다 하나씩 있기 때문이다. 셋에 다른 그림을 주면 같은 기능이
        ///     화면마다 달라 보인다.
        ///
        /// [I] SVG 를 쓰는 이유는 이 프로그램이 Per-Monitor DPI 를 켜기 때문이다
        ///     (`Program.cs` 의 첫 문장). 비트맵은 125%·150% 에서 뭉갠다.
        /// </summary>
        partial void ConfigureUI()
        {
            Icon = LoadApplicationIcon();

            SetGlyph(barBtnPatientNew, "PatientNew");
            SetGlyph(barBtnPatientEdit, "PatientEdit");
            SetGlyph(barBtnPatientReserve, "Reserve");
            SetGlyph(barBtnPatientLog, "ChangeLog");

            SetGlyph(barBtnRsvEdit, "ReserveEdit");
            SetGlyph(barBtnRsvCancel, "ReserveCancel");
            SetGlyph(barBtnRsvLog, "ChangeLog");

            SetGlyph(barBtnRcpStart, "ReceptionStart");
            SetGlyph(barBtnRcpExtra, "ReceptionExtra");
            SetGlyph(barBtnRcpCancel, "ReceptionCancel");
            SetGlyph(barBtnRcpLog, "ChangeLog");
        }

        /// <summary>
        /// `05` 계약이 아니라 자산 이름이라 게이트가 없다 — 대신 시험이 **버튼 전부가 그림을
        /// 갖는지** 잰다. 이름이 틀리면 `FromResources` 가 그 자리에서 터진다.
        /// </summary>
        private static void SetGlyph(BarItem item, string name)
        {
            item.ImageOptions.SvgImage = SvgImage.FromResources(
                IconRoot + "Icons." + name + ".svg", typeof(MainForm).Assembly);
        }

        /// <summary>
        /// 제목표시줄과 작업표시줄의 아이콘. `csproj` 의 `ApplicationIcon` 은 **exe 파일의**
        /// 아이콘이고, 폼이 띄우는 것은 이것이다 — 둘은 같은 `App.ico` 를 본다.
        /// </summary>
        private static Icon LoadApplicationIcon()
        {
            using (Stream stream = typeof(MainForm).Assembly.GetManifestResourceStream(IconRoot + "App.ico"))
            {
                // 못 읽으면 기본 아이콘으로 둔다 — 아이콘 하나 때문에 프로그램이 서지 않는다.
                return stream == null ? null : new Icon(stream);
            }
        }
    }
}
