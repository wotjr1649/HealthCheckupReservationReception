// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    public interface IWorkRepository
    {
        /// <summary>SP-WRK-01 `[dbo].[USP_HC_예약접수목록_조회]` (05 §8.1).</summary>
        WorkListReadDto Search(WorkSearchRequest request);

        /// <summary>SP-WRK-02 `[dbo].[USP_HC_예약접수상세_조회]` (05 §8.2).</summary>
        WorkDetailReadDto ReadDetail(long workId);
    }
}
