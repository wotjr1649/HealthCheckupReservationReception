// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-WRK-01 의 조회조건 (05 §8.1 · 03 §9.3).
    /// 값은 화면이 넣은 그대로다 — 정규화는 Service 가 한다.
    /// </summary>
    public sealed class WorkSearchRequest
    {
        public DateTime? FromDate { get; set; }    // [@시작일] 포함
        public DateTime? ToDate { get; set; }      // [@종료일] 포함
        public string StatusCode { get; set; }     // [@상태코드] null = 전체. 조회조건으로 보지 않는다
        public string ChartNo { get; set; }        // [@차트번호] 정확검색
        public string Name { get; set; }           // [@성명] 접두검색
    }
}
