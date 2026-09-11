// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
using System;

namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-LOG-01 `[dbo].[USP_HC_변경이력_조회]` 의 RS1 (05 §8.3).
    /// 영문 이름은 05 §16.5 대응표가 정한다.
    ///
    /// `대상테이블` 은 실리지 않는다 — 호출자가 이미 알고 넘긴 값이다 (05 §8.3 계약 경계).
    /// </summary>
    public sealed class ChangeLogItemDto
    {
        public long LogId { get; set; }            // [이력ID]     내부키. 화면에 내지 않는다 (03 §23.3)
        public DateTime RecordedAt { get; set; }   // [기록일시]
        public string OperatorName { get; set; }   // [조작자명]   **자기신고 값**이다 (04 §14.2 L4)
        public string ColumnName { get; set; }     // [컬럼명]
        public string BeforeValue { get; set; }    // [변경전]     NVARCHAR(4000) 에서 잘렸을 수 있다
        public string AfterValue { get; set; }     // [변경후]     같다 (00 CP-06)
    }
}
