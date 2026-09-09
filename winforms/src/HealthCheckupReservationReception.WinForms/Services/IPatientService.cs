using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Services
{
    public interface IPatientService
    {
        OperationResult<IList<PatientListItemDto>> Search(PatientSearchRequest request);

        OperationResult<PatientDetailDto> GetDetail(long patientId);

        /// <summary>
        /// DLG-PAT-01 New 저장 (SP-PAT-03 · 05 §10.1).
        ///
        /// `IsSuccess` 는 **DB 판정을 받아 왔는가** 다. `202`·`203` 처럼 실패 결과코드도
        /// 화면이 이어서 처리해야 하므로 (03 §6.3 · §6.5) 여기서 실패로 접지 않는다 —
        /// 결과코드 분기는 Presenter 가 한다 (05 §3.6 · 07 §6.2). DB 에 닿기 전에
        /// 접히는 것(길이 위반 · RS0 없음)만 `Failure` 다.
        /// </summary>
        OperationResult<PatientSaveReadDto> Register(PatientSaveRequest request);

        /// <summary>DLG-PAT-01 Edit 저장 (SP-PAT-04 · 05 §10.2). 성패 규약은 Register 와 같다.</summary>
        OperationResult<PatientSaveReadDto> Update(PatientSaveRequest request);
    }
}
