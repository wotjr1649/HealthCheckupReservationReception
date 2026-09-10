// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>SP-HOL-01 의 세 Result Set 을 한 덩어리로 나른다 (05 §12.5).</summary>
    public sealed class HolidayListReadDto
    {
        public DbResult Result { get; set; }
        public IList<HolidayListItemDto> Rows { get; set; }
        public HolidayRegistryDto Registry { get; set; }
    }
}
