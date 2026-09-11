// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    public interface IChangeLogRepository
    {
        ChangeLogReadDto Read(string targetTable, long targetKey);
    }
}
