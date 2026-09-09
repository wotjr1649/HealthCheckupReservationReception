using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    public interface IPatientRepository
    {
        /// <summary>SP-PAT-01 `[dbo].[USP_HC_수검자목록_조회]` (05 §7.2).</summary>
        PatientListReadDto Search(PatientSearchRequest request);

        /// <summary>SP-PAT-02 `[dbo].[USP_HC_수검자상세_조회]` (05 §7.3).</summary>
        PatientDetailReadDto ReadDetail(long patientId);
    }
}
