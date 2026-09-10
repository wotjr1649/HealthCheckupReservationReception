// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;

namespace HealthCheckupReservationReception.Services
{
    /// <summary>
    /// DLG-HOL-01 의 업무 계층 (03 §24 · 05 §12.5~§12.8).
    ///
    /// **판정을 여기서 하지 않는다.** 날짜 중복(801)·법정공휴일 편집(802)·행버전 충돌(601)은
    /// 전부 SP 가 낸다 (03 §24.5 *"최종 판정은 DB"*). 이 계층이 하는 일은 RS0 를 읽어
    /// 계약 위반과 업무 실패를 가르는 것뿐이다.
    /// </summary>
    public sealed class HolidayService : IHolidayService
    {
        // 05 §12.6 의 Parameter 크기.
        private const int NameMax = 100;
        private const int MemoMax = 500;

        private readonly IHolidayRepository _repository;

        public HolidayService(IHolidayRepository repository)
        {
            _repository = repository;
        }

        public OperationResult<HolidayListReadDto> Search(HolidaySearchRequest request)
        {
            HolidayListReadDto read = _repository.Search(request);
            if (read == null || read.Result == null)
            {
                return OperationResult<HolidayListReadDto>.Failure("휴무일을 조회하지 못했습니다.");
            }

            if (!read.Result.Success)
            {
                // 분기는 숫자 결과코드로만 한다 (05 §3.6 · §4.3) — 메시지는 DB 것을 그대로 올린다.
                return OperationResult<HolidayListReadDto>.Failure(read.Result.Message);
            }

            // 05 §12.5 — RS1 0건은 성공이고, RS2 는 **항상 1행**이다. 없으면 계약 위반이다.
            if (read.Registry == null)
            {
                return OperationResult<HolidayListReadDto>.Failure("공휴일 등재현황을 받지 못했습니다.");
            }

            if (read.Rows == null)
            {
                read.Rows = new System.Collections.Generic.List<HolidayListItemDto>();
            }

            return OperationResult<HolidayListReadDto>.Success(read);
        }

        public OperationResult<HolidaySaveReadDto> Register(HolidaySaveRequest request)
        {
            OperationResult<HolidaySaveReadDto> tooLong = Fits(request);
            return tooLong ?? Saved(_repository.Register(request), "휴무일을 등록하지 못했습니다.");
        }

        public OperationResult<HolidaySaveReadDto> Update(HolidaySaveRequest request)
        {
            OperationResult<HolidaySaveReadDto> tooLong = Fits(request);
            return tooLong ?? Saved(_repository.Update(request), "휴무일을 수정하지 못했습니다.");
        }

        /// <summary>
        /// 05 §12.6 Parameter 크기. 길이는 Service 가 한 번만 본다 (킷 §6) — 화면의 MaxLength 는
        /// UI 제한이지 검증이 아니다. 넘치면 SP 가 자르는 대신 여기서 먼저 말해 준다.
        /// </summary>
        private static OperationResult<HolidaySaveReadDto> Fits(HolidaySaveRequest request)
        {
            if (Length(request.HolidayName) > NameMax)
            {
                return OperationResult<HolidaySaveReadDto>.Failure(
                    "휴무일명은 " + NameMax + "자를 넘을 수 없습니다.");
            }

            if (Length(request.Memo) > MemoMax)
            {
                return OperationResult<HolidaySaveReadDto>.Failure(
                    "비고는 " + MemoMax + "자를 넘을 수 없습니다.");
            }

            return null;
        }

        private static int Length(string value)
        {
            return value == null ? 0 : value.Trim().Length;
        }

        public OperationResult<HolidaySaveReadDto> Delete(DateTime holidayDate, byte[] rowVersion)
        {
            return Saved(_repository.Delete(holidayDate, rowVersion), "휴무일을 삭제하지 못했습니다.");
        }

        /// <summary>
        /// 업무 실패(801·802·601·800)는 **예외가 아니라 결과**다. 화면이 그 사유를 그대로 보여야
        /// 하므로 `결과코드` 를 실어 올린다 (킷 §2 · 03 §24.5).
        /// </summary>
        private static OperationResult<HolidaySaveReadDto> Saved(HolidaySaveReadDto read, string unknown)
        {
            if (read == null || read.Result == null)
            {
                return OperationResult<HolidaySaveReadDto>.Failure(unknown);
            }

            return OperationResult<HolidaySaveReadDto>.Success(read);
        }
    }
}
