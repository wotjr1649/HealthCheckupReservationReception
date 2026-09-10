// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-HOL-01 의 조회조건 (05 §12.5). 두 날짜는 **필수**다 — 없으면 SP 가 `100` 이다.
    /// </summary>
    public sealed class HolidaySearchRequest
    {
        public DateTime? FromDate { get; set; }      // [@시작일자]
        public DateTime? ToDate { get; set; }        // [@종료일자]
        public string HolidayType { get; set; }      // [@휴무구분] null = 세 구분 전부
    }
}
