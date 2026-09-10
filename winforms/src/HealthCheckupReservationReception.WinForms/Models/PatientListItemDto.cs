namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// SP-PAT-01 `[dbo].[USP_HC_수검자목록_조회]` 의 RS1 (05 §7.2).
    /// 영문 이름은 05 §16.5 대응표가 정한다 — 여기서 새로 짓지 않는다.
    ///
    /// [X] **끝 둘은 SP 컬럼이 아니다.** `SP-PAT-01` 은 예약을 모른다(RS1 아홉 칸이 전부
    ///     수검자 기준정보다). 계약이 동결이라 컬럼을 붙일 수 없으므로, 화면이 `SP-WRK-01`
    ///     결과를 `수검자ID` 로 이어 붙여 채운다 (2026-09-11 grilling). 그래서 Repository 는
    ///     이 둘을 읽지 않는다 — `verify-rs-columns.sh` 가 보는 `GetOrdinal` 목록에 없다.
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

        // ── 아래 둘은 화면이 이어 붙인 것이다 (SP 컬럼이 아니다)

        /// <summary>목록 컬럼 — `가능` / `불가`.</summary>
        public string ReserveStatus { get; set; }

        /// <summary>상세 한 줄 — `예약 불가 — 2026-09-14 오전 예약` 처럼 이유까지 적는다.</summary>
        public string ReserveStatusDetail { get; set; }
    }
}
