// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System.Collections.Generic;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>SP-WRK-02 한 번의 호출이 낸 여섯 Result Set (05 §8.2).</summary>
    public sealed class WorkDetailReadDto
    {
        public DbResult Result { get; set; }                  // RS0
        public WorkDetailDto Detail { get; set; }             // RS1. RS0 실패면 null
        public IList<WorkExamItemDto> NexItems { get; set; }  // RS2
        public IList<WorkExamItemDto> AexItems { get; set; }  // RS3
        public IList<WorkActionDto> Actions { get; set; }     // RS4

        /// <summary>
        /// RS5 추가검사구성 — **정확히 7행**이다 (05 §8.2, R18). `AexItems`(RS3)는 저장된
        /// 것만 주므로 고치는 화면은 이쪽을 쓴다.
        ///
        /// `ReservationAexItemDto` 를 그대로 쓴다 — 「고르는 자리」의 DTO 이고 RS5 의 일곱
        /// 컬럼이 그 모양에 그대로 앉는다. 둘로 가르면 같은 Grid 정의가 두 벌이 된다.
        /// </summary>
        public IList<ReservationAexItemDto> AexOptions { get; set; }
    }
}
