// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-WRK-01 `[dbo].[USP_HC_예약접수목록_조회]` 의 RS1 (05 §8.1).
    /// 영문 이름은 05 §16.5 대응표가 정한다 — 표에 없는 넷(예약일·시간대코드·상태코드·상태명)은
    /// DB 계층에만 있는 컬럼이라 대응표의 대상이 아니다.
    /// </summary>
    public sealed class WorkListItemDto
    {
        public long WorkId { get; set; }         // [업무ID]     내부키. Grid 에 노출하지 않는다 (03 §9.4)
        public long PatientId { get; set; }      // [수검자ID]   내부키. 같은 이유로 노출하지 않는다
        public DateTime ReserveDate { get; set; }// [예약일]
        public string SlotCode { get; set; }     // [시간대코드] AM/PM
        public string StatusCode { get; set; }   // [상태코드]   RSV/RCP/CNR/CNC
        public string StatusName { get; set; }   // [상태명]     DB 가 준 표시명을 그대로 쓴다
        public string Name { get; set; }         // [성명]
        public string ChartNo { get; set; }      // [차트번호]
        public string Gender { get; set; }       // [성별]       M/F 계산열
        public string Birthday { get; set; }     // [생년월일]   yyyyMMdd 계산열
        public string MobilePhone { get; set; }  // [휴대전화]
    }
}
