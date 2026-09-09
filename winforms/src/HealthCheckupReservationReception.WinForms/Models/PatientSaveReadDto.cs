using System.Collections.Generic;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// 수검자 Write SP 한 번의 호출이 낸 Result Set (05 §10.1 · §10.2).
    ///
    /// `202`·`203` 은 **실패인데 RS1 을 읽는 유일한 예외다** (05 §3.5). 그래서
    /// <see cref="Rows"/> 가 있는지를 <see cref="DbResult.Success"/> 로 판단하지 않는다.
    /// </summary>
    public sealed class PatientSaveReadDto
    {
        public DbResult Result { get; set; }                    // RS0

        /// <summary>RS1. 없으면 null 이다. `203` 일 때만 2행 이상일 수 있다 (05 §10.1).</summary>
        public IList<PatientSaveResultDto> Rows { get; set; }
    }
}
