namespace HealthCheckupReservationReception.Models
{
    /// <summary>SP-PAT-02 한 번의 호출이 낸 두 Result Set (05 §7.3).</summary>
    public sealed class PatientDetailReadDto
    {
        public DbResult Result { get; set; }          // RS0
        public PatientDetailDto Detail { get; set; }  // RS1. RS0 실패면 null
    }
}
