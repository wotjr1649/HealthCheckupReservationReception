// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System.Collections.Generic;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>SP-WRK-01 한 번의 호출이 낸 두 Result Set (05 §8.1).</summary>
    public sealed class WorkListReadDto
    {
        public DbResult Result { get; set; }               // RS0
        public IList<WorkListItemDto> Rows { get; set; }   // RS1. RS0 실패면 null
    }
}
