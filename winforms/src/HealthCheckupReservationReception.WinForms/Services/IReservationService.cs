// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Services
{
    public interface IReservationService
    {
        /// <summary>
        /// SP-RSV-01 예약가능정보 (05 §9). 일정·TGT·NEX·AEX·저장가능을 한 번에 받는다.
        ///
        /// 달력 Cell Paint·Hover 에서는 부르지 않는다 — 날짜 선택이 확정된 시점이다 (05 §9.1).
        /// </summary>
        OperationResult<ReservationAvailabilityReadDto> GetAvailability(ReservationAvailabilityRequest request);

        /// <summary>
        /// SP-RSV-02 예약 등록 (05 §11.1).
        ///
        /// `IsSuccess` 는 **DB 판정을 받아 왔는가** 다. 03 §8.11 의 2단계 저장은 실패 결과코드도
        /// 화면이 이어서 처리해야 하므로(사유 표시 · 일정/대상/검사구성 Refresh) 여기서 실패로
        /// 접지 않는다 — 결과코드 분기는 Presenter 가 한다. DB 에 닿기 전에 접히는 것만
        /// `Failure` 다.
        /// </summary>
        OperationResult<WorkSaveReadDto> Register(ReservationSaveRequest request);

        /// <summary>
        /// SP-RSV-03 예약 변경 (05 §11.2). `Register` 와 같은 규약이다 — DB 판정을 받아 왔으면
        /// `IsSuccess` 이고 결과코드 분기는 Presenter 가 한다.
        /// </summary>
        OperationResult<WorkSaveReadDto> Change(ReservationChangeRequest request);

        /// <summary>SP-RSV-04 예약 취소 (05 §11.3). RSV → CNR.</summary>
        OperationResult<WorkSaveReadDto> Cancel(WorkActionRequest request);
    }
}
