// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-RSV-01 `[dbo].[USP_HC_예약가능정보_조회]` 의 입력 (05 §9.2 · §9.3).
    ///
    /// **신규예약과 예약변경이 같은 입력을 쓴다.** 신규는 <see cref="WorkId"/>·
    /// <see cref="RowVersion"/> 이 null 이고, 예약변경은 둘 다 필수다 (§9.3 조합표) —
    /// 지금 쓰는 곳은 WF-RSV-01 뿐이지만 갈라 두면 §9.4 변경범위 규칙이 두 곳에 생긴다.
    ///
    /// [X] 05 §9.3 — AEX BIT 중 하나라도 NULL 이면 `100` 이다. 그래서 <see cref="AexSelected"/>
    ///     는 늘 계약 개수만큼 차 있어야 하고, 생성자가 그 길이로 만들어 둔다.
    /// </summary>
    public sealed class ReservationAvailabilityRequest
    {
        public ReservationAvailabilityRequest()
        {
            AexSelected = new bool[AexParameterCount];
        }

        /// <summary>05 §9.2 의 `@추가검사01~07선택여부` 개수. Parameter 목록 그 자체다.</summary>
        public const int AexParameterCount = 7;

        public long PatientId { get; set; }         // [@수검자ID]
        public long? WorkId { get; set; }           // [@업무ID]   신규예약은 NULL
        public byte[] RowVersion { get; set; }      // [@행버전]   신규예약은 NULL
        public string ReserveType { get; set; }     // [@예약구분] NORMAL / WALKIN
        public DateTime ReserveDate { get; set; }   // [@예약일]
        public string SlotCode { get; set; }        // [@시간대코드] 날짜 고르는 중에는 NULL

        /// <summary>
        /// 순서가 곧 `추가검사01`~`07` 이다. RS5 는 추가검사코드 ASC 로 오므로 (05 §9.6)
        /// 화면이 받은 순서를 그대로 되돌려 주면 자리가 맞는다 — 코드 문자열을 C# 에 적지 않는다.
        /// </summary>
        public bool[] AexSelected { get; set; }
    }
}
