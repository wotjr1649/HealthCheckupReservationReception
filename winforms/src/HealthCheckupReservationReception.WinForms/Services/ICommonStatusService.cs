using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Services
{
    public interface ICommonStatusService
    {
        OperationResult<CommonWorkStatusDto> GetCurrent();
    }
}
