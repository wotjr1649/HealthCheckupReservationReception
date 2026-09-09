using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;

namespace HealthCheckupReservationReception.Services
{
    public sealed class CommonStatusService : ICommonStatusService
    {
        private readonly ICommonStatusRepository _repository;

        public CommonStatusService(ICommonStatusRepository repository)
        {
            _repository = repository;
        }

        public OperationResult<CommonWorkStatusDto> GetCurrent()
        {
            CommonWorkStatusReadDto read = _repository.Read();

            // 분기는 숫자 결과코드로만 한다 — RETURN·OUTPUT·메시지 비교를 쓰지 않는다 (05 §3.6).
            // 킷 contract/repository.md 는 RETURN·OUTPUT 으로 판정하라고 적지만 05 가 이긴다
            // (winforms/AGENTS.md "05 outranks the kit" · 07 §6.3).
            if (read == null || read.Result == null)
            {
                return OperationResult<CommonWorkStatusDto>.Failure("공통 업무상태를 읽지 못했습니다.");
            }

            if (!read.Result.Success)
            {
                return OperationResult<CommonWorkStatusDto>.Failure(read.Result.Message);
            }

            if (read.Status == null)
            {
                // RS0 이 성공인데 RS1 이 비었다 — 계약 위반이므로 조용히 성공으로 읽지 않는다 (07 §6.2).
                return OperationResult<CommonWorkStatusDto>.Failure("공통 업무상태 결과가 비어 있습니다.");
            }

            return OperationResult<CommonWorkStatusDto>.Success(read.Status);
        }
    }
}
