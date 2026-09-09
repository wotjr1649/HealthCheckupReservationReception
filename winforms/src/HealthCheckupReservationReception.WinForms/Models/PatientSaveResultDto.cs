namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// 수검자 Write SP 의 RS1 한 행.
    ///
    /// SP-PAT-03 (05 §10.1) 은 여덟 값을 전부 준다 — 성공 1행이거나, `202` 의 기존 1행이거나,
    /// `203` 의 후보 1행 이상이다. SP-PAT-04 (05 §10.2) 의 RS1 은 그중 셋
    /// (<see cref="PatientId"/> · <see cref="ChartNo"/> · <see cref="RowVersion"/>) 뿐이므로
    /// 나머지는 채워지지 않는다 — 계약이 좁은 것이지 읽다 만 것이 아니다.
    /// </summary>
    public sealed class PatientSaveResultDto
    {
        public long PatientId { get; set; }        // [수검자ID]
        public string ChartNo { get; set; }        // [차트번호]
        public string Name { get; set; }           // [성명]
        public string SocialNumber { get; set; }   // [주민번호]
        public string Birthday { get; set; }       // [생년월일]
        public string Gender { get; set; }         // [성별]
        public string MobilePhone { get; set; }    // [휴대전화]
        public byte[] RowVersion { get; set; }     // [행버전]
    }
}
