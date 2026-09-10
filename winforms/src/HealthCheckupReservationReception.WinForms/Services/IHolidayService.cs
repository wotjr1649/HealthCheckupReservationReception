// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Services
{
    public interface IHolidayService
    {
        OperationResult<HolidayListReadDto> Search(HolidaySearchRequest request);
        OperationResult<HolidaySaveReadDto> Register(HolidaySaveRequest request);
        OperationResult<HolidaySaveReadDto> Update(HolidaySaveRequest request);
        OperationResult<HolidaySaveReadDto> Delete(DateTime holidayDate, byte[] rowVersion);
    }
}
