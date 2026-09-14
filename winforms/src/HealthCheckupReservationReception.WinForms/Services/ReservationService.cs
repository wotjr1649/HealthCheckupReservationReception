// ── 예약 서비스 ──────────────────────────────────────────────────────────────
// 계약(IReservationService) 과 구현(ReservationService) 을 한 파일에 둔다.
// 부르는 SP: SP-RSV-01 예약가능정보 · 02 등록 · 03 변경 · 04 취소.

using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;

namespace HealthCheckupReservationReception.Services
{
    // 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
    public interface IReservationService
    {
        /// <summary>
        /// SP-RSV-01 예약가능정보 (05 §9). 일정·TGT·NEX·AEX·저장가능을 한 번에 받는다.
        ///
        /// 달력 Cell Paint·Hover 에서는 부르지 않는다 — 날짜 선택이 확정된 시점이다 (05 §9.1).
        /// </summary>
        OperationResult<ReservationAvailabilityReadDto> GetAvailability(ReservationAvailabilityRequest request);

        /// <summary>
        /// SP-RSV-02 예약 등록 (05 §11.1).
        ///
        /// `IsSuccess` 는 **DB 판정을 받아 왔는가** 다. 03 §8.11 의 2단계 저장은 실패 결과코드도
        /// 화면이 이어서 처리해야 하므로(사유 표시 · 일정/대상/검사구성 Refresh) 여기서 실패로
        /// 접지 않는다 — 결과코드 분기는 Presenter 가 한다. DB 에 닿기 전에 접히는 것만
        /// `Failure` 다.
        /// </summary>
        OperationResult<WorkSaveReadDto> Register(ReservationSaveRequest request);

        /// <summary>
        /// SP-RSV-03 예약 변경 (05 §11.2). `Register` 와 같은 규약이다 — DB 판정을 받아 왔으면
        /// `IsSuccess` 이고 결과코드 분기는 Presenter 가 한다.
        /// </summary>
        OperationResult<WorkSaveReadDto> Change(ReservationChangeRequest request);

        /// <summary>SP-RSV-04 예약 취소 (05 §11.3). RSV → CNR.</summary>
        OperationResult<WorkSaveReadDto> Cancel(WorkActionRequest request);
    }

    // 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
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

        /// <summary>
        /// SP-RSV-03 (05 §11.2). 변경범위를 여기서 재지 않는다 — 원하는 최종 상태를 보내고
        /// DB 가 현재 행과 견주어 잰다. 변경이 없으면 `결과코드=1` No-op 이 온다.
        /// </summary>
        public OperationResult<WorkSaveReadDto> Change(ReservationChangeRequest request)
        {
            var normalized = new ReservationChangeRequest
            {
                WorkId = request.WorkId,
                RowVersion = request.RowVersion,
                ReserveDate = request.ReserveDate,
                SlotCode = request.SlotCode,
                AexSelected = request.AexSelected,
                OperatorName = Trim(request.OperatorName),
            };

            OperationResult<WorkSaveReadDto> tooLong = OperatorFits(normalized.OperatorName);
            if (tooLong != null) { return tooLong; }

            return Saved(_repository.Change(normalized), "예약을 변경하지 못했습니다.");
        }

        public OperationResult<WorkSaveReadDto> Cancel(WorkActionRequest request)
        {
            WorkActionRequest normalized = WorkAction.Normalize(request);
            OperationResult<WorkSaveReadDto> tooLong = OperatorFits(normalized.OperatorName);
            if (tooLong != null) { return tooLong; }

            return Saved(_repository.Cancel(normalized), "예약을 취소하지 못했습니다.");
        }

        private OperationResult<WorkSaveReadDto> OperatorFits(string operatorName)
        {
            return Length(operatorName) > OperatorNameMax
                ? OperationResult<WorkSaveReadDto>.Failure(
                    "조작자명은 " + OperatorNameMax + "자를 넘을 수 없습니다.")
                : null;
        }

        private static OperationResult<WorkSaveReadDto> Saved(WorkSaveReadDto read, string failure)
        {
            if (read == null || read.Result == null)
            {
                return OperationResult<WorkSaveReadDto>.Failure(failure);
            }

            // 실패 결과코드도 화면이 이어서 처리한다 — `Register` 와 같은 규약이다.
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
