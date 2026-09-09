namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-PAT-01 의 조회조건 (05 §7.2 · 03 §5.3).
    /// 값은 화면이 넣은 그대로다 — 정규화(`-` 제거 · 빈 문자열 → 미입력)는 Service 가 한다.
    /// </summary>
    public sealed class PatientSearchRequest
    {
        public string ChartNo { get; set; }        // 정확검색
        public string Name { get; set; }           // 접두검색
        public string SocialNumber { get; set; }   // `-` 제거 후 정확검색
        public string Birthday { get; set; }       // yyyyMMdd 정확검색
        public string MobilePhone { get; set; }    // `-` 제거 후 정확검색
    }
}
