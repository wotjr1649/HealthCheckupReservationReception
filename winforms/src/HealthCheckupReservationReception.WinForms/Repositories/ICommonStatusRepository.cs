using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    public interface ICommonStatusRepository
    {
        CommonWorkStatusReadDto Read();
    }
}
