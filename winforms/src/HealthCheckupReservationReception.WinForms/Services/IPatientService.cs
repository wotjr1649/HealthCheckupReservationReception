using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Services
{
    public interface IPatientService
    {
        OperationResult<IList<PatientListItemDto>> Search(PatientSearchRequest request);

        OperationResult<PatientDetailDto> GetDetail(long patientId);
    }
}
