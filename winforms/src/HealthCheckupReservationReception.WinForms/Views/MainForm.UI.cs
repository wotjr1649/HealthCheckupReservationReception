// ── MainForm 의 Designer 밖 구성 ─────────────────────────────────────────────
//
// Designer 가 직렬화하지 못하는 것만 여기 있다 (킷 §5) — **리소스에서 읽어 오는 값**이다.
// 리터럴 캡션·폭·Options 는 Designer 에 그대로 둔다.

namespace HealthCheckupReservationReception.Views
{
    public partial class MainForm
    {
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
            Icon = clsGlyph.ApplicationIcon();

            clsGlyph.Apply(barBtnPatientNew, "PatientNew");
            clsGlyph.Apply(barBtnPatientEdit, "PatientEdit");
            clsGlyph.Apply(barBtnPatientReserve, "Reserve");
            clsGlyph.Apply(barBtnPatientLog, "ChangeLog");

            clsGlyph.Apply(barBtnRsvEdit, "ReserveEdit");
            clsGlyph.Apply(barBtnRsvCancel, "ReserveCancel");
            clsGlyph.Apply(barBtnRsvLog, "ChangeLog");

            clsGlyph.Apply(barBtnRcpStart, "ReceptionStart");
            clsGlyph.Apply(barBtnRcpExtra, "ReceptionExtra");
            clsGlyph.Apply(barBtnRcpCancel, "ReceptionCancel");
            clsGlyph.Apply(barBtnRcpLog, "ChangeLog");
        }

    }
}
