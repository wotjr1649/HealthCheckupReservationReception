// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-RSV-01 의 RS3 검진대상 (05 §9.8). `ALL` 에서 일정 평가가 가능하면 1행, 아니면 0행이다.
    ///
    /// 03 §8.7 의 판정문구는 화면 계산결과이고 별도 DB 컬럼이 아니다 — 사유는 DB 가 주고
    /// 화면은 `대상 —` / `비대상 —` 앞머리만 붙인다.
    /// </summary>
    public sealed class ExamTargetDto
    {
        public bool IsTarget { get; set; }              // [검진대상여부]
        public int Age { get; set; }                    // [나이]       예약일 기준
        public DateTime? LastCompletedDate { get; set; } // [최근완료일자] 최초검진이면 NULL
        public int ReasonCode { get; set; }             // [사유코드]   400 UnderAge · 401 NotDue
        public string ReasonMessage { get; set; }       // [사유메시지]
    }
}
