// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System.Collections.Generic;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>SP-WRK-02 한 번의 호출이 낸 다섯 Result Set (05 §8.2).</summary>
    public sealed class WorkDetailReadDto
    {
        public DbResult Result { get; set; }                  // RS0
        public WorkDetailDto Detail { get; set; }             // RS1. RS0 실패면 null
        public IList<WorkExamItemDto> NexItems { get; set; }  // RS2
        public IList<WorkExamItemDto> AexItems { get; set; }  // RS3
        public IList<WorkActionDto> Actions { get; set; }     // RS4
    }
}
