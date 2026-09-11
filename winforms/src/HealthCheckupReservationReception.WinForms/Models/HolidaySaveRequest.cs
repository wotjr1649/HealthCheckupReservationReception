// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-HOL-02 등록 · SP-HOL-03 수정 · SP-HOL-04 삭제의 입력 (05 §12.6~§12.8).
    ///
    /// `휴무구분` 이 없다 — 등록 SP 는 `자체휴무일` 만 만들고 화면이 고르지 않는다 (03 §24.5).
    /// `@조작자명` 도 없다: `변경이력` 의 대상 테이블이 `수검자`·`예약접수` 둘뿐이라 기록할 곳이
    /// 없고, 받아 두고 버리는 Parameter 를 계약에 남기지 않는다 (05 §12 머리말).
    /// </summary>
    public sealed class HolidaySaveRequest
    {
        public DateTime HolidayDate { get; set; }   // [@휴무일자] PK
        public byte[] RowVersion { get; set; }      // [@행버전]   수정·삭제만. 등록은 null
        public string HolidayName { get; set; }     // [@휴무일명]
        public bool IsActive { get; set; }          // [@사용여부]
        public string Memo { get; set; }            // [@비고]
    }
}
