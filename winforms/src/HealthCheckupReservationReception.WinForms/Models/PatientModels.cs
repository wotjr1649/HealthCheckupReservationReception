// ── 수검자 모델 ──────────────────────────────────────────────────────────────
// 화면 ID: WF-PAT-01 (03 §5) · DLG-PAT-01 (03 §6) · DLG-PAT-03 (03 §6.5)
//
//   형식                       SP · Result Set          무엇
//   PatientSearchRequest       SP-PAT-01 입력           조회조건
//   PatientListItemDto         SP-PAT-01 RS1            수검자 목록 한 행
//   PatientListReadDto         SP-PAT-01 RS0+RS1        한 호출의 결과
//   PatientDetailDto           SP-PAT-02 RS1            수검자 상세
//   PatientDetailReadDto       SP-PAT-02 RS0+RS1        한 호출의 결과
//   PatientSaveRequest         SP-PAT-03·04 입력        등록·수정 한 벌
//   PatientSaveResultDto       SP-PAT-03·04 RS1         저장된 수검자
//   PatientSaveReadDto         RS0+RS1                  한 호출의 결과
//   PatientValidWorkDto        SP-PAT-05 RS1            기존 유효예약
//   PatientValidWorkReadDto    SP-PAT-05 RS0+RS1        한 호출의 결과
//   PatientActionState         (SP 아님)                화면 Action 의 열림·닫힘

using System;
using System.Collections.Generic;

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

    /// <summary>SP-PAT-01 한 번의 호출이 낸 두 Result Set (05 §7.2).</summary>
    public sealed class PatientListReadDto
    {
        public DbResult Result { get; set; }                  // RS0
        public IList<PatientListItemDto> Rows { get; set; }   // RS1. RS0 실패면 null
    }

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

    /// <summary>SP-PAT-02 한 번의 호출이 낸 두 Result Set (05 §7.3).</summary>
    public sealed class PatientDetailReadDto
    {
        public DbResult Result { get; set; }          // RS0
        public PatientDetailDto Detail { get; set; }  // RS1. RS0 실패면 null
    }

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

    /// <summary>
    /// 수검자 Write SP 한 번의 호출이 낸 Result Set (05 §10.1 · §10.2).
    ///
    /// `202`·`203` 은 **실패인데 RS1 을 읽는 유일한 예외다** (05 §3.5). 그래서
    /// <see cref="Rows"/> 가 있는지를 <see cref="DbResult.Success"/> 로 판단하지 않는다.
    /// </summary>
    public sealed class PatientSaveReadDto
    {
        public DbResult Result { get; set; }                    // RS0

        /// <summary>RS1. 없으면 null 이다. `203` 일 때만 2행 이상일 수 있다 (05 §10.1).</summary>
        public IList<PatientSaveResultDto> Rows { get; set; }
    }

    // 화면 ID: WF-RSV-01 — 신규 예약 (03 §8.5 중복판단)
    /// <summary>
    /// SP-PAT-05 `[dbo].[USP_HC_수검자유효업무_조회]` 의 RS1 (05 §7.4).
    ///
    /// 조회범위는 `수검자ID 일치 AND 예약일 >= DB 오늘날짜 AND 상태코드 IN ('RSV','RCP')` 이고
    /// 정상 Cardinality 는 **0행 또는 1행**이다 — RP-06 이 유효업무를 하나로 못박기 때문이다.
    /// 2행 이상이면 SP 가 `701` 을 낸다.
    /// </summary>
    public sealed class PatientValidWorkDto
    {
        public long WorkId { get; set; }           // [업무ID]     03 §8.5 가 이것으로 Workbench 를 연다
        public DateTime ReserveDate { get; set; }  // [예약일]
        public string SlotCode { get; set; }       // [시간대코드]
        public string StatusCode { get; set; }     // [상태코드]
        public bool IsToday { get; set; }          // [오늘여부]
        public byte[] RowVersion { get; set; }     // [행버전]
    }

    // 화면 ID: WF-RSV-01 — 신규 예약 (03 §8.5 중복판단)
    /// <summary>SP-PAT-05 한 번의 호출이 낸 두 Result Set (05 §7.4).</summary>
    public sealed class PatientValidWorkReadDto
    {
        public DbResult Result { get; set; }        // RS0
        public PatientValidWorkDto Work { get; set; } // RS1. 0행이면 null — 유효업무가 없다는 뜻이다
    }

    // 화면 ID: WF-PAT-01 — 수검자 관리 (03 §5)
    /// <summary>
    /// 03 §5.2 — 수검자 관리의 Ribbon Action 상태. Ribbon 은 MainForm 이 갖고 판정은
    /// Presenter 가 하므로, 그 판정을 실어 나르는 자리다 (<see cref="WorkActionState"/> 와 같은 꼴).
    ///
    /// <see cref="Reserve"/> 만 축이 하나 더 있다: **그 수검자가 예약 가능한가**
    /// (2026-09-11 사용자 지시). 목록이 `불가` 라고 적어 두고 버튼은 열어 두면 화면이 스스로
    /// 모순되고, 눌러 봐야 모달이 그 사실을 다시 말한다.
    ///
    /// [X] **모르는 것은 불가가 아니다.** 업무 조회가 실패해 `예약` 칸이 비었을 때 닫아 버리면
    ///     DB 한 번 끊긴 것으로 예약이 통째로 막힌다 — 그때는 열어 두고 DB 가 판정하게 한다
    ///     (R12 가 지키려던 것이 이것이다).
    /// </summary>
    public sealed class PatientActionState
    {
        public bool RowSelected { get; set; }
        public bool Reserve { get; set; }

        /// <summary>행 미선택 — 전부 닫힌 상태다 (03 §5.2).</summary>
        public static PatientActionState None()
        {
            return new PatientActionState();
        }
    }
}
