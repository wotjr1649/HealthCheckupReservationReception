// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8.5 중복판단)
namespace HealthCheckupReservationReception.Models
{
    /// <summary>SP-PAT-05 한 번의 호출이 낸 두 Result Set (05 §7.4).</summary>
    public sealed class PatientValidWorkReadDto
    {
        public DbResult Result { get; set; }        // RS0
        public PatientValidWorkDto Work { get; set; } // RS1. 0행이면 null — 유효업무가 없다는 뜻이다
    }
}
