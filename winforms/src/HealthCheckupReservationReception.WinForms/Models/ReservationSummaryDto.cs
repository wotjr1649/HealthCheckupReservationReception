// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-RSV-01 의 RS1 예약요약 (05 §9.6). 정확히 1행이다.
    ///
    /// **`저장가능` 을 화면이 다시 계산하지 않는다** (05 §9.12). 공통 업무가능·다른 유효예약·
    /// 시간대 선택가능·TGT·NEX 8건·AEX 유효성의 곱을 DB 가 이미 냈다. 화면이 한 번 더 세면
    /// 같은 규칙이 두 곳에 생긴다 (ROOT AGENTS.md §6).
    /// </summary>
    public sealed class ReservationSummaryDto
    {
        public string ChangeScope { get; set; }    // [변경범위] ALL/SLOT/EXTRA/SLOT_EXTRA/NONE
        public long PatientId { get; set; }        // [수검자ID]
        public long? WorkId { get; set; }          // [업무ID]   신규예약은 NULL
        public string ReserveType { get; set; }    // [예약구분]
        public DateTime ReserveDate { get; set; }  // [예약일]
        public string SlotCode { get; set; }       // [시간대코드] 미선택이면 NULL

        // 셋 다 신규예약에서는 NULL 이다 (05 §9.6).
        public bool? DateChanged { get; set; }     // [예약일변경여부]
        public bool? SlotChanged { get; set; }     // [시간대변경여부]
        public bool? AexChanged { get; set; }      // [추가검사변경여부]

        public bool WorkAllowed { get; set; }      // [현재업무가능]
        public long? OtherWorkId { get; set; }     // [다른업무ID] 있으면 03 §8.5 가 Workbench 로 보낸다
        public bool CanSave { get; set; }          // [저장가능]
        public int BlockCode { get; set; }         // [차단코드]   대표 하나. 우선순위는 05 §9.6
        public string BlockMessage { get; set; }   // [차단메시지]
    }
}
