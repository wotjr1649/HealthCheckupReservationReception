// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System;
using HealthCheckupReservationReception.Common;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-HOL-02 · SP-HOL-03 의 RS1 휴무일결과 (05 §12.6 · §12.7).
    /// SP-HOL-04 삭제는 RS1 이 없다 — 지운 행의 행버전을 돌려줄 이유가 없다 (05 §12.8).
    /// </summary>
    public sealed class HolidaySaveReadDto
    {
        public DbResult Result { get; set; }
        public DateTime? HolidayDate { get; set; }   // [휴무일자]
        public byte[] RowVersion { get; set; }       // [행버전]
    }
}
