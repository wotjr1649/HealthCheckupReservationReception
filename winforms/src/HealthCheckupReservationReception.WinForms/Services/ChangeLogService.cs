// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;

namespace HealthCheckupReservationReception.Services
{
    /// <summary>
    /// DLG-LOG-01 의 업무 계층 (03 §23 · 05 §8.3).
    ///
    /// [X] **0건은 성공이다.** 대상 행이 없어도, 기록이 하나도 없어도 SP 는 `결과코드=0` 에
    ///     RS1 0행을 준다 — `200 PatientNotFound` 를 쓰지 않는다. 감사 기록은 대상 행보다
    ///     오래 살기 때문이다 (05 §8.3 계약 경계 · 04 §8.6.3). 여기서 0건을 실패로 바꾸면
    ///     그 설계를 화면이 뒤집는다.
    /// </summary>
    public sealed class ChangeLogService : IChangeLogService
    {
        private readonly IChangeLogRepository _repository;

        public ChangeLogService(IChangeLogRepository repository)
        {
            _repository = repository;
        }

        public OperationResult<ChangeLogReadDto> Read(string targetTable, long targetKey)
        {
            ChangeLogReadDto read = _repository.Read(targetTable, targetKey);
            if (read == null || read.Result == null)
            {
                return OperationResult<ChangeLogReadDto>.Failure("변경이력을 조회하지 못했습니다.");
            }

            if (!read.Result.Success)
            {
                // 분기는 숫자 결과코드로만 한다 (05 §3.6 · §4.3) — 메시지는 DB 것을 그대로 올린다.
                return OperationResult<ChangeLogReadDto>.Failure(read.Result.Message);
            }

            if (read.Rows == null)
            {
                read.Rows = new List<ChangeLogItemDto>();
            }

            return OperationResult<ChangeLogReadDto>.Success(read);
        }
    }
}
