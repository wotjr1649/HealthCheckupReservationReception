using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    public interface IPatientRepository
    {
        /// <summary>SP-PAT-01 `[dbo].[USP_HC_수검자목록_조회]` (05 §7.2).</summary>
        PatientListReadDto Search(PatientSearchRequest request);

        /// <summary>SP-PAT-02 `[dbo].[USP_HC_수검자상세_조회]` (05 §7.3).</summary>
        PatientDetailReadDto ReadDetail(long patientId);

        /// <summary>SP-PAT-03 `[dbo].[USP_HC_수검자_등록]` (05 §10.1).</summary>
        PatientSaveReadDto Register(PatientSaveRequest request);

        /// <summary>SP-PAT-04 `[dbo].[USP_HC_수검자정보_수정]` (05 §10.2).</summary>
        PatientSaveReadDto Update(PatientSaveRequest request);

        /// <summary>SP-PAT-05 `[dbo].[USP_HC_수검자유효업무_조회]` (05 §7.4).</summary>
        PatientValidWorkReadDto ReadValidWork(long patientId);
    }
}
