// ── 변경이력 모델 ────────────────────────────────────────────────────────────
// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
//
//   형식                SP · Result Set      무엇
//   ChangeLogTarget     (SP 아님)            어느 행의 이력인가 — 화면이 넘기는 것
//   ChangeLogItemDto    SP-LOG-01 RS1        이력 한 행
//   ChangeLogReadDto    SP-LOG-01 RS0+RS1    한 호출의 결과

using System;
using System.Collections.Generic;

namespace HealthCheckupReservationReception.Models
{
    // 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
    /// <summary>
    /// 03 §23.2 의 진입점 둘이 모달에 넘기는 것. 값 셋이 늘 함께 다니므로 한 덩어리다.
    ///
    /// <see cref="Caption"/> 은 §23.3 의 제목줄 — *"변경이력 — 수검자 홍길동 (2026-000123)"* —
    /// 에 들어간다. **어느 행의 이력인지 화면이 말하지 않으면 사용자가 확인할 길이 없다.**
    /// SP 는 `대상테이블` 조차 돌려주지 않는다 (05 §8.3 계약 경계).
    /// </summary>
    public sealed class ChangeLogTarget
    {
        public string TargetTable { get; set; }
        public long TargetKey { get; set; }
        public string Caption { get; set; }
    }

    // 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
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

    // 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
    /// <summary>SP-LOG-01 의 두 Result Set 을 한 덩어리로 나른다 (05 §8.3).</summary>
    public sealed class ChangeLogReadDto
    {
        public DbResult Result { get; set; }
        public IList<ChangeLogItemDto> Rows { get; set; }
    }
}
