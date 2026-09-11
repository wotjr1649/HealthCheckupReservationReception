// 화면 ID: WF-RSV-01 · WF-WRK-01 — 예약·접수 Write 공통 (05 §11)
namespace HealthCheckupReservationReception.Models
{
    /// <summary>예약·접수 Write SP 한 번의 호출이 낸 두 Result Set (05 §11).</summary>
    public sealed class WorkSaveReadDto
    {
        public DbResult Result { get; set; }       // RS0
        public WorkSaveResultDto Row { get; set; } // RS1. 실패면 null
    }
}
