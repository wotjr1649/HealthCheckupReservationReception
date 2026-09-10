// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Services
{
    public interface IWorkService
    {
        /// <summary>SP-WRK-01 목록 조회 (05 §8.1 · 03 §9.3).</summary>
        OperationResult<IList<WorkListItemDto>> Search(WorkSearchRequest request);

        /// <summary>
        /// SP-WRK-02 상세 조회 (05 §8.2 · 03 §9.5).
        ///
        /// 네 Result Set 을 한 번에 돌려준다 — 상세·NEX·AEX·가능한업무는 같은 한 호출의
        /// 결과이고, 갈라서 부르면 서로 다른 시점의 값이 한 화면에 앉는다.
        /// </summary>
        OperationResult<WorkDetailReadDto> GetDetail(long workId);
    }
}
