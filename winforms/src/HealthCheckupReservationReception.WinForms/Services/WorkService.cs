// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;

namespace HealthCheckupReservationReception.Services
{
    /// <summary>
    /// WF-WRK-01 의 업무 계층 (03 §9.3 · 05 §8.1 · §8.2).
    /// 정규화와 길이 검증이 여기 있다 — 비어 있음 판정과 From&gt;To 는 Presenter 가 한다 (킷 §6).
    /// </summary>
    public sealed class WorkService : IWorkService
    {
        // 05 §11.3 · §12.1 · §12.3 의 `@조작자명` 크기.
        private const int OperatorNameMax = 50;

        // 05 §8.1 의 Parameter 크기. 화면의 MaxLength 는 UI 제한이지 검증이 아니다 (킷 §6).
        private const int ChartNoMax = 100;
        private const int NameMax = 100;

        private readonly IWorkRepository _repository;

        public WorkService(IWorkRepository repository)
        {
            _repository = repository;
        }

        public OperationResult<IList<WorkListItemDto>> Search(WorkSearchRequest request)
        {
            var normalized = new WorkSearchRequest
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                StatusCode = Trim(request.StatusCode),
                ChartNo = Trim(request.ChartNo),
                Name = Trim(request.Name),
            };

            if (Length(normalized.ChartNo) > ChartNoMax)
            {
                return OperationResult<IList<WorkListItemDto>>.Failure(
                    "차트번호는 " + ChartNoMax + "자를 넘을 수 없습니다.");
            }

            if (Length(normalized.Name) > NameMax)
            {
                return OperationResult<IList<WorkListItemDto>>.Failure(
                    "이름은 " + NameMax + "자를 넘을 수 없습니다.");
            }

            WorkListReadDto read = _repository.Search(normalized);
            if (read == null || read.Result == null)
            {
                return OperationResult<IList<WorkListItemDto>>.Failure("예약·접수 목록을 읽지 못했습니다.");
            }

            // 분기는 숫자 결과코드로만 한다 (05 §3.6 · §4.3).
            if (!read.Result.Success)
            {
                return OperationResult<IList<WorkListItemDto>>.Failure(read.Result.Message);
            }

            // 조회 0건은 성공이다 (05 §3.4).
            return OperationResult<IList<WorkListItemDto>>.Success(
                read.Rows ?? new List<WorkListItemDto>());
        }

        public OperationResult<WorkDetailReadDto> GetDetail(long workId)
        {
            WorkDetailReadDto read = _repository.ReadDetail(workId);
            if (read == null || read.Result == null)
            {
                return OperationResult<WorkDetailReadDto>.Failure("업무 상세를 읽지 못했습니다.");
            }

            if (!read.Result.Success)
            {
                return OperationResult<WorkDetailReadDto>.Failure(read.Result.Message);
            }

            if (read.Detail == null)
            {
                // RS1 은 정확히 1행이다 (05 §8.2). 비었으면 계약 위반이므로 성공으로 읽지 않는다.
                return OperationResult<WorkDetailReadDto>.Failure("업무 상세 결과가 비어 있습니다.");
            }

            // RS2·RS3 은 0행일 수 있다. 화면이 null 을 따지지 않도록 여기서 빈 목록으로 맞춘다.
            if (read.NexItems == null) { read.NexItems = new List<WorkExamItemDto>(); }
            if (read.AexItems == null) { read.AexItems = new List<WorkExamItemDto>(); }
            if (read.Actions == null) { read.Actions = new List<WorkActionDto>(); }
            if (read.AexOptions == null) { read.AexOptions = new List<ReservationAexItemDto>(); }

            return OperationResult<WorkDetailReadDto>.Success(read);
        }

        /// <summary>
        /// SP-RCP-01 (05 §12.1). RSV → RCP.
        ///
        /// **판정을 여기서 하지 않는다.** 예약일=오늘 · 접수마감 전 · 행버전 · 검사구성
        /// 무결성은 전부 SP 가 다시 검증한다 (03 §11.3 *"최신 RowVersion 일치"*). 이 계층이
        /// 하는 일은 조작자명 길이 하나와 RS0 를 읽어 올리는 것뿐이다.
        /// </summary>
        public OperationResult<WorkSaveReadDto> CompleteReception(WorkActionRequest request)
        {
            return Run(request, _repository.CompleteReception, "접수하지 못했습니다.");
        }

        public OperationResult<WorkSaveReadDto> CancelReception(WorkActionRequest request)
        {
            return Run(request, _repository.CancelReception, "접수를 취소하지 못했습니다.");
        }

        /// <summary>
        /// SP-RCP-02 (05 §12.2). 성별·저장 NEX 중복 판정은 SP 가 한다 — 화면이 고를 수 있는
        /// 것만 보여 주지만 최종 판정은 거기가 아니다 (03 §24.5 와 같은 규칙).
        /// </summary>
        public OperationResult<WorkSaveReadDto> ChangeExtraExam(ExtraExamChangeRequest request)
        {
            var normalized = new ExtraExamChangeRequest
            {
                WorkId = request.WorkId,
                RowVersion = request.RowVersion,
                AexSelected = request.AexSelected,
                OperatorName = Trim(request.OperatorName),
            };

            if (Length(normalized.OperatorName) > OperatorNameMax)
            {
                return OperationResult<WorkSaveReadDto>.Failure(
                    "조작자명은 " + OperatorNameMax + "자를 넘을 수 없습니다.");
            }

            WorkSaveReadDto read = _repository.ChangeExtraExam(normalized);
            if (read == null || read.Result == null)
            {
                return OperationResult<WorkSaveReadDto>.Failure("추가검사를 변경하지 못했습니다.");
            }

            return OperationResult<WorkSaveReadDto>.Success(read);
        }

        private static OperationResult<WorkSaveReadDto> Run(
            WorkActionRequest request,
            System.Func<WorkActionRequest, WorkSaveReadDto> call,
            string failure)
        {
            WorkActionRequest normalized = WorkAction.Normalize(request);
            if (Length(normalized.OperatorName) > OperatorNameMax)
            {
                return OperationResult<WorkSaveReadDto>.Failure(
                    "조작자명은 " + OperatorNameMax + "자를 넘을 수 없습니다.");
            }

            WorkSaveReadDto read = call(normalized);
            if (read == null || read.Result == null)
            {
                return OperationResult<WorkSaveReadDto>.Failure(failure);
            }

            // 실패 결과코드도 화면이 이어서 처리한다 — 예약 저장과 같은 규약이다.
            return OperationResult<WorkSaveReadDto>.Success(read);
        }

        private static string Trim(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static int Length(string value)
        {
            return value == null ? 0 : value.Length;
        }
    }
}
