// 화면 ID: DLG-RSV-01 — 예약 변경 (03 §10)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-RSV-03 `[dbo].[USP_HC_예약_변경]` 의 입력 (05 §11.2).
    ///
    /// `ReservationSaveRequest`(신규)와 갈라 둔다 — 신규는 `수검자ID`·`예약구분` 을 보내고
    /// 변경은 `업무ID`·`행버전` 을 보낸다. 한 DTO 로 합치면 어느 칸이 어느 쪽 것인지
    /// 호출부가 알아서 비워야 하고, 그 규칙은 어디에도 적히지 않는다.
    ///
    /// [X] **무엇이 바뀌었는지 화면이 정하지 않는다.** 05 §11.2 가 *"현재 예약일·시간대와
    ///     AEX 코드 집합으로 … 변경여부 계산"* 이라고 못박았다 — 화면은 **원하는 최종 상태**를
    ///     통째로 보내고 변경범위는 DB 가 잰다. 변경이 하나도 없으면 `결과코드=1` No-op 이다.
    /// </summary>
    public sealed class ReservationChangeRequest
    {
        public ReservationChangeRequest()
        {
            AexSelected = new bool[ReservationAvailabilityRequest.AexParameterCount];
        }

        public long WorkId { get; set; }            // [@업무ID]
        public byte[] RowVersion { get; set; }      // [@행버전]   조회 시점의 것을 그대로 되돌려준다
        public DateTime ReserveDate { get; set; }   // [@예약일]
        public string SlotCode { get; set; }        // [@시간대코드]
        public bool[] AexSelected { get; set; }     // [@추가검사01~07선택여부]
        public string OperatorName { get; set; }    // [@조작자명]
    }
}
