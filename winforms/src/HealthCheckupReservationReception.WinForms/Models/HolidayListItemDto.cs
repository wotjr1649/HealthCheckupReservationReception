// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-HOL-01 `[dbo].[USP_HC_휴무일목록_조회]` 의 RS1 (05 §12.5).
    /// `휴무일자` 오름차순 고정이고 0건은 성공이다.
    /// </summary>
    public sealed class HolidayListItemDto
    {
        public DateTime HolidayDate { get; set; }   // [휴무일자]  PK. 바꾸지 않는다 (05 §12.7)
        public string HolidayName { get; set; }     // [휴무일명]
        public string HolidayType { get; set; }     // [휴무구분]  DbHolidayType 셋 중 하나
        public bool IsActive { get; set; }          // [사용여부]  0 은 삭제가 아니라 일시 무효화다
        public string Memo { get; set; }            // [비고]
        public byte[] RowVersion { get; set; }      // [행버전]    표시하지 않는다 (03 §24.3)
    }
}
