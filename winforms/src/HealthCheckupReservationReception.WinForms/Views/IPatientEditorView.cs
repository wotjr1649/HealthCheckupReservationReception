// 화면 ID: DLG-PAT-01 — 수검자 등록·정보수정 (03 §6)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// DLG-PAT-01 수검자 등록·정보수정 Modal (03 §6). DevExpress 타입이 나타나지 않는다 (킷 §2).
    /// </summary>
    public interface IPatientEditorView
    {
        /// <summary>Modal 이 떴다. Edit 는 여기서 진입값을 읽는다 (07 §3.3).</summary>
        event EventHandler ViewLoaded;

        /// <summary>이름·주민번호가 바뀌었다 (03 §6.2 파생 · §6.3 EP-04 · §6.5 확인값).</summary>
        event EventHandler InputChanged;

        event EventHandler SaveRequested;

        /// <summary>03 §6.3 — 차트번호 방식. Edit 에는 이 선택이 없다 (§6.4).</summary>
        bool AutoChartNo { get; }

        string ChartNo { get; }
        string Name { get; }
        string SocialNumber { get; }
        string MobilePhone { get; }
        string Phone { get; }
        string Email { get; }
        string Zipcode { get; }
        string Address { get; }
        string AddressDetail { get; }
        string Memo { get; }
        bool HepatitisBExcluded { get; }

        /// <summary>03 §6.2 — 주민번호에서 산출한다. 실패면 빈 값으로 Clear 한다.</summary>
        string Birthday { set; }

        /// <summary>03 §6.2 — 산출값. 화면 표기는 `남`/`여` 다 (03 §5.6 No 9).</summary>
        string Gender { set; }

        /// <summary>03 §6.3 EP-04 — 이름·주민번호가 없거나 파생이 실패하면 저장할 수 없다.</summary>
        bool SaveEnabled { set; }

        /// <summary>03 §6.4 — Edit 는 자동발급 전환 없이 기존 차트번호 수동수정만 허용한다.</summary>
        void ShowEditMode();

        /// <summary>
        /// Edit 진입값 (SP-PAT-02 · 07 §3.3). `HepatitisBExcluded` 현재값도 여기서 온다 (03 §6.2a).
        /// </summary>
        void LoadDetail(PatientDetailDto detail);

        void ClearFieldErrors();

        /// <summary>
        /// 03 §15.1 Inline 오류. <paramref name="parameterName"/> 은 RS0 `오류항목` 이 담는
        /// Parameter 이름이고, 화면이 입력항목을 찾는 키다 (05 §16.2).
        /// </summary>
        void ShowFieldError(string parameterName, string message);

        /// <summary>
        /// 입력 중 알림. `ShowFieldError` 와 달리 **Focus 를 옮기지 않는다** — 타이핑 도중
        /// 매 글자마다 커서를 끌어오면 다른 칸으로 넘어갈 수가 없다.
        /// 빈 문자열이면 그 칸의 표시를 지운다.
        /// </summary>
        void ShowFieldHint(string parameterName, string message);

        void ShowMessage(string message);

        /// <summary>
        /// 03 §6.3 · §15.2 — 동일 주민번호이고 이름이 다르다(`202`). 기존 전체값을 확인받는다.
        /// </summary>
        bool ConfirmExistingPatient(PatientSaveResultDto existing);

        /// <summary>03 §6.5 DLG-PAT-03 중복 후보 확인 (`203`).</summary>
        DuplicateChoice ShowDuplicateCandidates(
            PatientSaveResultDto input, IList<PatientSaveResultDto> candidates);

        /// <summary>
        /// 03 §6.3 — 저장 성공 후 PatientId 와 ChartNo 를 호출 화면으로 돌려준다.
        /// 돌려줄 것이 없으면(진입 실패·취소) 둘 다 null 이다.
        /// </summary>
        void CloseWith(long? patientId, string chartNo);
    }

    /// <summary>DLG-PAT-03 의 세 버튼 (03 §6.5).</summary>
    public enum DuplicateChoice
    {
        Modify,     // [입력값 수정] — Editor 로 복귀해 다시 검증한다
        Continue,   // [별도 수검자로 계속] — 확인값을 실어 다시 저장한다
        Close       // [닫기]
    }

    /// <summary>
    /// RS0 `오류항목` 이 담는 Parameter 이름 (05 §10.1 · §16.2).
    /// **Presenter 의 자체 검증도 같은 키를 쓴다** — 두 벌이 되면 한쪽만 고쳐도 화면이
    /// 조용히 엉뚱한 Control 을 가리킨다 (ROOT AGENTS.md §6).
    /// </summary>
    public static class PatientErrorField
    {
        public const string ChartNo = "차트번호";
        public const string Name = "성명";
        public const string SocialNumber = "주민번호";
    }
}
