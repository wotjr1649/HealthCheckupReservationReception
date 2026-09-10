// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-RSV-02 `[dbo].[USP_HC_예약_등록]` 의 입력 (05 §11.1).
    ///
    /// 조회 SP 가 낸 `저장가능=1` 은 저장을 보장하지 않는다 — Write SP 는 조회 결과를
    /// 신뢰하지 않고 모든 조건을 다시 검증한다 (05 §11.1). 03 §8.11 의 2단계 저장이 그것이다.
    /// </summary>
    public sealed class ReservationSaveRequest
    {
        public ReservationSaveRequest()
        {
            AexSelected = new bool[ReservationAvailabilityRequest.AexParameterCount];
        }

        public long PatientId { get; set; }         // [@수검자ID]
        public string ReserveType { get; set; }     // [@예약구분] WalkIn 은 예약일=DB 오늘날짜여야 한다
        public DateTime ReserveDate { get; set; }   // [@예약일]
        public string SlotCode { get; set; }        // [@시간대코드]
        public bool[] AexSelected { get; set; }     // [@추가검사01~07선택여부]
        public string OperatorName { get; set; }    // [@조작자명]
    }
}
