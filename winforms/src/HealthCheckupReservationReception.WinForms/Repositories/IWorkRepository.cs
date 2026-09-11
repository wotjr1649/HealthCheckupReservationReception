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

        /// <summary>SP-RCP-01 `[dbo].[USP_HC_접수_완료]` (05 §12.1). RSV → RCP.</summary>
        WorkSaveReadDto CompleteReception(WorkActionRequest request);

        /// <summary>SP-RCP-03 `[dbo].[USP_HC_접수_취소]` (05 §12.3). RCP → CNC.</summary>
        WorkSaveReadDto CancelReception(WorkActionRequest request);

        /// <summary>SP-RCP-02 `[dbo].[USP_HC_접수추가검사_변경]` (05 §12.2). RCP 유지.</summary>
        WorkSaveReadDto ChangeExtraExam(ExtraExamChangeRequest request);
    }
}
