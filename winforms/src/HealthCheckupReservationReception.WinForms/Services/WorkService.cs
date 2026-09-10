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

            return OperationResult<WorkDetailReadDto>.Success(read);
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
