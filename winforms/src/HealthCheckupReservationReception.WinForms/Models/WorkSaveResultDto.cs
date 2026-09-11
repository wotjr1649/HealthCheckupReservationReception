// 화면 ID: WF-RSV-01 · WF-WRK-01 — 예약·접수 Write 공통 (05 §11)
namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// 05 §11 머리말 — **예약·접수 Write SP 의 성공 RS1 은 공통으로 이 Schema 다.**
    /// 예약등록·예약변경·예약취소·접수완료·추가검사변경·접수취소 여섯이 같은 것을 낸다.
    /// 그래서 DTO 도 한 벌이다 (ROOT AGENTS.md §6).
    /// </summary>
    public sealed class WorkSaveResultDto
    {
        public long WorkId { get; set; }        // [업무ID]   03 §8.11 이 이것으로 Workbench 를 연다
        public string StatusCode { get; set; }  // [상태코드]
        public byte[] RowVersion { get; set; }  // [행버전]   05 §16.3 — 문자열로 바꾸지 않는다
    }
}
