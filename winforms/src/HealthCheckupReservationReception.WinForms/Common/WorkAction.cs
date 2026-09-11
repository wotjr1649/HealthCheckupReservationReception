// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Common
{
    /// <summary>
    /// `@업무ID`·`@행버전`·`@조작자명` 셋만 보내는 SP 셋의 공통 정규화
    /// (SP-RSV-04 · SP-RCP-01 · SP-RCP-03). 두 Service 가 같은 것을 하므로 한 벌만 둔다.
    /// </summary>
    public static class WorkAction
    {
        public static WorkActionRequest Normalize(WorkActionRequest request)
        {
            return new WorkActionRequest
            {
                WorkId = request.WorkId,
                RowVersion = request.RowVersion,
                OperatorName = string.IsNullOrWhiteSpace(request.OperatorName)
                    ? null
                    : request.OperatorName.Trim(),
            };
        }
    }
}
