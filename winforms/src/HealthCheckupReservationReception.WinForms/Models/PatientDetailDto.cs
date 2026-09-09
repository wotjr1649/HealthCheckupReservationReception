namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-PAT-02 `[dbo].[USP_HC_수검자상세_조회]` 의 RS1 (05 §7.3). 정확히 1행이다.
    /// 목록(<see cref="PatientListItemDto"/>)보다 네 값이 많다 — 상세주소·비고·B형간염제외여부·행버전.
    /// </summary>
    public sealed class PatientDetailDto
    {
        public long PatientId { get; set; }            // [수검자ID]
        public string ChartNo { get; set; }            // [차트번호]
        public string Name { get; set; }               // [성명]
        public string SocialNumber { get; set; }       // [주민번호]
        public string Birthday { get; set; }           // [생년월일]
        public string Gender { get; set; }             // [성별]
        public string MobilePhone { get; set; }        // [휴대전화]
        public string Phone { get; set; }              // [전화번호]
        public string Email { get; set; }              // [이메일]
        public string Zipcode { get; set; }            // [우편번호]
        public string Address { get; set; }            // [주소]
        public string AddressDetail { get; set; }      // [상세주소]
        public string Memo { get; set; }               // [비고]

        /// <summary>[B형간염제외여부] NEX-03 의 제외 판정 입력. 1 이 제외다 (05 §7.3).</summary>
        public bool HepatitisBExcluded { get; set; }

        /// <summary>[행버전] 동시성값. 화면이 들고 있되 표시하지 않는다 (03 §18 · 05 §16.3).</summary>
        public byte[] RowVersion { get; set; }
    }
}
