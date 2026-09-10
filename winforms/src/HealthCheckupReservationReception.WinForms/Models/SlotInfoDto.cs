// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-RSV-01 의 RS2 시간대정보 (05 §9.7). 일정을 평가하는 변경범위에서 AM/PM 2행이다.
    ///
    /// [X] **마감시각을 C# 에 심지 않는다.** 03 §8.6 이 표로 적은 네 값(Normal/WalkIn ×
    ///     AM/PM)은 R13 에서 DB `운영기준` 테이블로 옮겨졌고 (`04` §8.7) 여기 <see cref="CutoffTime"/>·
    ///     <see cref="CutoffPassed"/> 로 내려온다. 화면은 받은 것을 그리기만 한다.
    /// </summary>
    public sealed class SlotInfoDto
    {
        public string SlotCode { get; set; }        // [시간대코드] AM/PM
        public string SlotName { get; set; }        // [시간대명]   DB 가 준 표시명을 그대로 쓴다
        public int Capacity { get; set; }           // [정원]
        public int CurrentCount { get; set; }       // [현재인원]
        public int AppliedCount { get; set; }       // [적용후인원] 신규는 현재인원+1 (05 §9.7)
        public int RemainingSeats { get; set; }     // [잔여자리]
        public bool IsOperating { get; set; }       // [운영여부]   토요일 PM 등
        public TimeSpan? CutoffTime { get; set; }   // [마감시각]
        public bool CutoffPassed { get; set; }      // [마감경과여부]
        public bool Selectable { get; set; }        // [선택가능]   일정·운영·마감·정원만 본다
        public int BlockCode { get; set; }          // [차단코드]
        public string BlockMessage { get; set; }    // [차단메시지]
    }
}
