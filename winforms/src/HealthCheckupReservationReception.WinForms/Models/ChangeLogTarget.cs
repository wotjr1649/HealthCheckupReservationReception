// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
namespace HealthCheckupReservationReception.Models
{
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
}
