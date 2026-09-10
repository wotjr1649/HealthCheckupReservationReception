// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using System.Collections.Generic;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-RSV-01 한 번의 호출이 낸 여섯 Result Set (05 §9.5).
    ///
    /// **한 번에 받는 것이 계약이다.** 갈라 부르면 일정·TGT·NEX·AEX 가 서로 다른 시점의
    /// 값이 되어 한 화면에 앉는다 — 정원은 초 단위로 바뀐다.
    ///
    /// 채워지는 것은 <see cref="ReservationSummaryDto.ChangeScope"/> 가 정한다 (05 §9.11
    /// Cardinality 표). 신규예약은 늘 `ALL` 이라 전부 온다.
    /// </summary>
    public sealed class ReservationAvailabilityReadDto
    {
        public DbResult Result { get; set; }                    // RS0
        public ReservationSummaryDto Summary { get; set; }      // RS1. 정확히 1행
        public IList<SlotInfoDto> Slots { get; set; }           // RS2. AM/PM 2행 또는 0행
        public ExamTargetDto Target { get; set; }               // RS3. 1행 또는 0행
        public IList<WorkExamItemDto> NexItems { get; set; }    // RS4. 8~11행 또는 0행
        public IList<ReservationAexItemDto> AexItems { get; set; } // RS5. 7행 또는 0행
    }
}
