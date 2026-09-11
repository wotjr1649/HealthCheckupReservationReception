// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
using System.Collections.Generic;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>SP-LOG-01 의 두 Result Set 을 한 덩어리로 나른다 (05 §8.3).</summary>
    public sealed class ChangeLogReadDto
    {
        public DbResult Result { get; set; }
        public IList<ChangeLogItemDto> Rows { get; set; }
    }
}
