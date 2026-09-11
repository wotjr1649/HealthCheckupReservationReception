// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    public interface IReservationRepository
    {
        /// <summary>SP-RSV-01 `[dbo].[USP_HC_예약가능정보_조회]` (05 §9).</summary>
        ReservationAvailabilityReadDto ReadAvailability(ReservationAvailabilityRequest request);

        /// <summary>SP-RSV-02 `[dbo].[USP_HC_예약_등록]` (05 §11.1).</summary>
        WorkSaveReadDto Register(ReservationSaveRequest request);

        /// <summary>SP-RSV-03 `[dbo].[USP_HC_예약_변경]` (05 §11.2).</summary>
        WorkSaveReadDto Change(ReservationChangeRequest request);

        /// <summary>SP-RSV-04 `[dbo].[USP_HC_예약_취소]` (05 §11.3).</summary>
        WorkSaveReadDto Cancel(WorkActionRequest request);
    }
}
