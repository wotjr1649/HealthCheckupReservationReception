namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-PAT-01 `[dbo].[USP_HC_수검자목록_조회]` 의 RS1 (05 §7.2).
    /// 영문 이름은 05 §16.5 대응표가 정한다 — 여기서 새로 짓지 않는다.
    /// </summary>
    public sealed class PatientListItemDto
    {
        public long PatientId { get; set; }        // [수검자ID]   내부키. Grid 에 노출하지 않는다 (03 §5.5)
        public string ChartNo { get; set; }        // [차트번호]
        public string Name { get; set; }           // [성명]
        public string SocialNumber { get; set; }   // [주민번호]   테스트 전체값 (03 §5.6)
        public string Birthday { get; set; }       // [생년월일]   yyyyMMdd 계산열
        public string Gender { get; set; }         // [성별]       M/F 계산열
        public string MobilePhone { get; set; }    // [휴대전화]
        public string Phone { get; set; }          // [전화번호]
        public string Email { get; set; }          // [이메일]
        public string Zipcode { get; set; }        // [우편번호]
        public string Address { get; set; }        // [주소]
    }
}
