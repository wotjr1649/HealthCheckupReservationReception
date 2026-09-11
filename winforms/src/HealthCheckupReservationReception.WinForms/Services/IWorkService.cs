// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Services
{
    public interface IWorkService
    {
        /// <summary>SP-WRK-01 목록 조회 (05 §8.1 · 03 §9.3).</summary>
        OperationResult<IList<WorkListItemDto>> Search(WorkSearchRequest request);

        /// <summary>
        /// SP-WRK-02 상세 조회 (05 §8.2 · 03 §9.5).
        ///
        /// 네 Result Set 을 한 번에 돌려준다 — 상세·NEX·AEX·가능한업무는 같은 한 호출의
        /// 결과이고, 갈라서 부르면 서로 다른 시점의 값이 한 화면에 앉는다.
        /// </summary>
        OperationResult<WorkDetailReadDto> GetDetail(long workId);

        /// <summary>
        /// SP-RCP-01 접수 완료 (05 §12.1). RSV → RCP.
        ///
        /// `IsSuccess` 는 **DB 판정을 받아 왔는가** 다 — 예약 저장과 같은 규약이다.
        /// 마감 지남·정원·행버전 충돌은 결과코드로 오고 그 분기는 화면이 한다.
        /// </summary>
        OperationResult<WorkSaveReadDto> CompleteReception(WorkActionRequest request);

        /// <summary>SP-RCP-03 접수 취소 (05 §12.3). RCP → CNC.</summary>
        OperationResult<WorkSaveReadDto> CancelReception(WorkActionRequest request);

        /// <summary>
        /// SP-RCP-02 접수완료 추가검사 변경 (05 §12.2). 상태는 `RCP` 를 유지하고 행버전만 바뀐다.
        /// 동일 집합이면 `결과코드=1` No-op 이다 — 화면이 미리 견주지 않는다.
        /// </summary>
        OperationResult<WorkSaveReadDto> ChangeExtraExam(ExtraExamChangeRequest request);
    }
}
