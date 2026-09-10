// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    public interface IHolidayRepository
    {
        /// <summary>SP-HOL-01 목록 + 공휴일 등재현황 (05 §12.5).</summary>
        HolidayListReadDto Search(HolidaySearchRequest request);

        /// <summary>SP-HOL-02 자체휴무일 등록 (05 §12.6).</summary>
        HolidaySaveReadDto Register(HolidaySaveRequest request);

        /// <summary>SP-HOL-03 자체휴무일 수정 (05 §12.7).</summary>
        HolidaySaveReadDto Update(HolidaySaveRequest request);

        /// <summary>SP-HOL-04 자체휴무일 물리 삭제 (05 §12.8). RS1 이 없다.</summary>
        HolidaySaveReadDto Delete(DateTime holidayDate, byte[] rowVersion);
    }
}
