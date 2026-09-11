// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Services
{
    public interface IChangeLogService
    {
        OperationResult<ChangeLogReadDto> Read(string targetTable, long targetKey);
    }
}
