// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8.5 중복판단)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-PAT-05 `[dbo].[USP_HC_수검자유효업무_조회]` 의 RS1 (05 §7.4).
    ///
    /// 조회범위는 `수검자ID 일치 AND 예약일 >= DB 오늘날짜 AND 상태코드 IN ('RSV','RCP')` 이고
    /// 정상 Cardinality 는 **0행 또는 1행**이다 — RP-06 이 유효업무를 하나로 못박기 때문이다.
    /// 2행 이상이면 SP 가 `701` 을 낸다.
    /// </summary>
    public sealed class PatientValidWorkDto
    {
        public long WorkId { get; set; }           // [업무ID]     03 §8.5 가 이것으로 Workbench 를 연다
        public DateTime ReserveDate { get; set; }  // [예약일]
        public string SlotCode { get; set; }       // [시간대코드]
        public string StatusCode { get; set; }     // [상태코드]
        public string StatusName { get; set; }     // [상태명]
        public bool IsToday { get; set; }          // [오늘여부]
        public byte[] RowVersion { get; set; }     // [행버전]
    }
}
