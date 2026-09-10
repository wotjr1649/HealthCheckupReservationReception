// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-HOL-01 의 RS2 공휴일등재현황 (05 §12.5). **항상 1행**이다.
    ///
    /// [X] **임계 숫자를 화면이 갖지 않는다.** 그 값의 단일 출처는 `00` §7.4 이고 SP 가 그것을
    ///     <see cref="WarningThresholdDays"/> 로 함께 내보낸다 — 화면은
    ///     `잔여일수 &lt; 경고임계일수` 만 비교한다 (03 §24.6).
    ///
    /// RS2 는 `@휴무구분` 필터의 영향을 받지 않는다. 화면이 자체휴무일만 보고 있어도 만료
    /// 경고는 같아야 한다.
    /// </summary>
    public sealed class HolidayRegistryDto
    {
        public DateTime? LastHolidayDate { get; set; }   // [공휴일최종일자] 0건이면 NULL
        public int? RemainingDays { get; set; }          // [잔여일수]       이미 지났으면 음수
        public int WarningThresholdDays { get; set; }    // [경고임계일수]
    }
}
