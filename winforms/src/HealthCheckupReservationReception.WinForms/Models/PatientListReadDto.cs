using System.Collections.Generic;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>SP-PAT-01 한 번의 호출이 낸 두 Result Set (05 §7.2).</summary>
    public sealed class PatientListReadDto
    {
        public DbResult Result { get; set; }                  // RS0
        public IList<PatientListItemDto> Rows { get; set; }   // RS1. RS0 실패면 null
    }
}
