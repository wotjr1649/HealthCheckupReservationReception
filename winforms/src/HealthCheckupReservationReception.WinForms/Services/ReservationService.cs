// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;

namespace HealthCheckupReservationReception.Services
{
    /// <summary>
    /// WF-RSV-01 의 업무 계층 (03 §8 · 05 §9 · §11.1).
    ///
    /// **여기서 업무 판정을 하지 않는다.** 정원·마감·TGT·NEX·AEX·저장가능은 전부 DB 가
    /// 낸 값이고 (05 §9.12) 이 계층은 그것을 화면이 쓰기 좋은 꼴로 넘길 뿐이다. 한 줄이라도
    /// 다시 계산하면 같은 규칙이 두 곳에 생긴다 (ROOT AGENTS.md §6).
    ///
    /// 길이 검증만 여기 있다 — 비어 있음 판정은 Presenter 다 (킷 §6).
    /// </summary>
    public sealed class ReservationService : IReservationService
    {
        // 05 §11.1 의 Parameter 크기.
        private const int OperatorNameMax = 50;

        private readonly IReservationRepository _repository;

        public ReservationService(IReservationRepository repository)
        {
            _repository = repository;
        }

        public OperationResult<ReservationAvailabilityReadDto> GetAvailability(ReservationAvailabilityRequest request)
        {
            ReservationAvailabilityReadDto read = _repository.ReadAvailability(request);
            if (read == null || read.Result == null)
            {
                return OperationResult<ReservationAvailabilityReadDto>.Failure("예약 가능정보를 읽지 못했습니다.");
            }

            // 분기는 숫자 결과코드로만 한다 (05 §3.6 · §4.3).
            //
            // [X] `저장가능=0` 은 실패가 아니다. 차단코드를 달고 오는 **성공한 조회**이고
            //     (05 §9.6) 화면은 그 사유를 보여야 한다. 실패로 접으면 사용자는 왜 저장이
            //     막혔는지 영영 모른다.
            if (!read.Result.Success)
            {
                return OperationResult<ReservationAvailabilityReadDto>.Failure(read.Result.Message);
            }

            if (read.Summary == null)
            {
                // RS1 은 정확히 1행이다 (05 §9.6). 비었으면 계약 위반이므로 성공으로 읽지 않는다.
                return OperationResult<ReservationAvailabilityReadDto>.Failure("예약 요약 결과가 비어 있습니다.");
            }

            // 변경범위에 따라 0행인 것이 정상이다 (05 §9.11). 화면이 null 을 따지지 않도록 맞춘다.
            if (read.Slots == null) { read.Slots = new List<SlotInfoDto>(); }
            if (read.NexItems == null) { read.NexItems = new List<WorkExamItemDto>(); }
            if (read.AexItems == null) { read.AexItems = new List<ReservationAexItemDto>(); }

            return OperationResult<ReservationAvailabilityReadDto>.Success(read);
        }

        public OperationResult<WorkSaveReadDto> Register(ReservationSaveRequest request)
        {
            var normalized = new ReservationSaveRequest
            {
                PatientId = request.PatientId,
                ReserveType = request.ReserveType,
                ReserveDate = request.ReserveDate,
                SlotCode = request.SlotCode,
                AexSelected = request.AexSelected,
                OperatorName = Trim(request.OperatorName),
            };

            if (Length(normalized.OperatorName) > OperatorNameMax)
            {
                return OperationResult<WorkSaveReadDto>.Failure(
                    "조작자명은 " + OperatorNameMax + "자를 넘을 수 없습니다.");
            }

            WorkSaveReadDto read = _repository.Register(normalized);
            if (read == null || read.Result == null)
            {
                return OperationResult<WorkSaveReadDto>.Failure("예약을 저장하지 못했습니다.");
            }

            // 실패 결과코드도 화면이 이어서 처리한다 (03 §8.11) — 여기서 접지 않는다.
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
