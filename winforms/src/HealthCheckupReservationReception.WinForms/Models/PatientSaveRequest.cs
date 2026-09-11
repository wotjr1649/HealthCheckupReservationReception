namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// DLG-PAT-01 의 저장 요청 (03 §6). New 는 SP-PAT-03 (05 §10.1), Edit 는 SP-PAT-04
    /// (05 §10.2) 로 간다. 두 SP 는 Parameter 가 다르지만 **화면이 하나**이므로 요청도
    /// 한 벌로 둔다 — 어느 값이 어느 SP 의 Parameter 인지는 PatientRepository 가
    /// SqlParameter 를 다는 자리에서 갈린다.
    ///
    /// 값은 화면이 담은 그대로다. 정규화(`-` 제거 · 공백 → 미입력)는 Service 가 한다.
    /// **생년월일·성별은 여기 없다** — 계산열이 유일한 기준이고 Parameter 로 받지 않는다
    /// (05 §10.1).
    /// </summary>
    public sealed class PatientSaveRequest
    {
        // 등록·수정 공통
        public string ChartNo { get; set; }                // [차트번호]
        public string Name { get; set; }                   // [성명]
        public string SocialNumber { get; set; }           // [주민번호]
        public string MobilePhone { get; set; }            // [휴대전화]
        public string Phone { get; set; }                  // [전화번호]
        public string Email { get; set; }                  // [이메일]
        public string Zipcode { get; set; }                // [우편번호]
        public string Address { get; set; }                // [주소]
        public string AddressDetail { get; set; }          // [상세주소]
        public string Memo { get; set; }                   // [비고]
        public bool HepatitisBExcluded { get; set; }       // [B형간염제외여부]
        public string OperatorName { get; set; }           // [조작자명]

        // 등록 전용 (05 §10.1)

        /// <summary>[차트번호자동발급여부] 참이면 차트번호를 싣지 않는다 (03 §6.3).</summary>
        public bool AutoChartNo { get; set; }

        /// <summary>
        /// [유사수검자확인여부] DLG-PAT-03 에서 `별도 수검자로 계속` 을 고른 상태다.
        /// 성명 + 산출 생년월일 + 주민번호 조합 하나에만 유효하다 (05 §10.1 · 03 §6.5).
        /// </summary>
        public bool SimilarConfirmed { get; set; }

        // 수정 전용 (05 §10.2)
        public long PatientId { get; set; }                // [수검자ID]

        /// <summary>[행버전] Modal 진입 때 받은 원본 동시성값이다 (03 §16 · 05 §16.3).</summary>
        public byte[] RowVersion { get; set; }
    }
}
