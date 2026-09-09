namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-COM-01 한 번의 호출이 낸 두 Result Set 을 그대로 나른다.
    /// 리포지토리는 숫자와 값만 내보내고 그것을 업무 문구로 바꾸는 것은 Service 다
    /// (킷 §2 · contract/repository.md).
    /// </summary>
    public sealed class CommonWorkStatusReadDto
    {
        public DbResult Result { get; set; }            // RS0
        public CommonWorkStatusDto Status { get; set; }  // RS1. RS0 실패면 null
    }
}
