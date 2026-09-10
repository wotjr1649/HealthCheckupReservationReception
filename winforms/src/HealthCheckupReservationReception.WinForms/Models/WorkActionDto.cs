// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-WRK-02 의 RS4 가능한업무 (05 §8.2). 정확히 5행이며 `업무동작코드` 는 고정이다.
    ///
    /// **허용여부를 화면이 다시 계산하지 않는다** — 상태·공통 업무조건·마감의 조합과
    /// 차단 우선순위는 DB 가 갖는다 (05 §8.2 허용조건 표). 화면은 받은 값을 그리기만 한다.
    /// </summary>
    public sealed class WorkActionDto
    {
        public string ActionCode { get; set; }     // [업무동작코드]
        public bool Allowed { get; set; }          // [허용여부]
        public int ReasonCode { get; set; }        // [사유코드]
        public string ReasonMessage { get; set; }  // [사유메시지]
    }
}
