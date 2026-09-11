// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// 선택행 하나에 거는 Write SP 셋의 입력 — `SP-RSV-04`(예약취소) · `SP-RCP-01`(접수완료) ·
    /// `SP-RCP-03`(접수취소). **셋의 Parameter 가 글자까지 같다** (05 §11.3 · §12.1 · §12.3):
    /// `@업무ID` · `@행버전` · `@조작자명`.
    ///
    /// 같은 모양을 셋으로 베끼지 않는다 (킷 §2 — 같은 shape 가 둘 이상이면 한 벌이다).
    /// 어느 SP 를 부를지는 Service 의 메서드 이름이 가른다.
    /// </summary>
    public sealed class WorkActionRequest
    {
        public long WorkId { get; set; }          // [@업무ID]
        public byte[] RowVersion { get; set; }    // [@행버전]
        public string OperatorName { get; set; }  // [@조작자명]
    }
}
