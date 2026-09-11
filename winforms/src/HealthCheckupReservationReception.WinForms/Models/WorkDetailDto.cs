// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-WRK-02 `[dbo].[USP_HC_예약접수상세_조회]` 의 RS1 업무상세 (05 §8.2).
    ///
    /// 정원·현재인원·잔여자리는 **현재 조회값**이며 Work 저장 Snapshot 이 아니다 (03 §9.5).
    /// </summary>
    public sealed class WorkDetailDto
    {
        public long WorkId { get; set; }           // [업무ID]
        public long PatientId { get; set; }        // [수검자ID]
        public string ChartNo { get; set; }        // [차트번호]
        public string Name { get; set; }           // [성명]
        public string Birthday { get; set; }       // [생년월일]
        public string Gender { get; set; }         // [성별]
        public string MobilePhone { get; set; }    // [휴대전화]
        public DateTime ReserveDate { get; set; }  // [예약일]
        public string SlotCode { get; set; }       // [시간대코드]
        public string StatusCode { get; set; }     // [상태코드]
        public int Capacity { get; set; }          // [정원]
        public int CurrentCount { get; set; }      // [현재인원]
        public int RemainingSeats { get; set; }    // [잔여자리]
        public byte[] RowVersion { get; set; }     // [행버전] 05 §16.3 — 문자열로 바꾸지 않는다
    }
}
