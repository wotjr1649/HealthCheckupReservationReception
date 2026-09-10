// 화면 ID: WF-PAT-01 — 수검자 관리 (03 §5)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// WF-PAT-01 수검자 관리 Tab (03 §5). DevExpress 타입이 나타나지 않는다 (킷 §2).
    /// </summary>
    public interface IPatientManagementView
    {
        /// <summary>Ribbon 의 `[조회]` 또는 조회조건의 `[조회]` 를 눌렀다.</summary>
        event EventHandler SearchRequested;

        /// <summary>Grid 의 행 선택이 바뀌었다. 선택이 없으면 null 이다 (03 §5.5).</summary>
        event EventHandler<long?> SelectionChanged;

        // 조회조건 다섯 (03 §5.3). 화면이 담은 그대로 넘긴다 — 정규화는 Service 가 한다.
        //
        // 2026-09-10 에 생년월일·휴대전화를 화면에서 걷었다가 grilling 2회차에서 되돌렸다.
        // 사라진 것이 아니라 **[조회 조건] 드롭다운으로 끌 수 있는 조건**이 되었고 기본은
        // 꺼져 있다 — DLG-PAT-02 와 같은 다섯이며 (03 §7.2 가 조회계약을 §5.3 에 위임한다)
        // `SP-PAT-01` 은 그 둘을 처음부터 받고 있었다 (05 §7.2).
        string ChartNo { get; }
        string Name { get; }
        string SocialNumber { get; }

        /// <summary>`yyyyMMdd`. 달력 칸의 값을 화면이 그 꼴로 바꿔 준다.</summary>
        string Birthday { get; }

        string MobilePhone { get; }

        /// <summary>
        /// 여섯째 조회조건 `예약 없는 수검자만` (2026-09-11 grilling).
        ///
        /// [X] **SP 조건이 아니다.** `SP-PAT-01` 은 예약을 모르므로 이것만 화면이 거른다 —
        ///     나머지 다섯과 성질이 달라 `PatientSearchRequest` 에 넣지 않는다.
        /// </summary>
        bool ReservableOnly { get; }

        IList<PatientListItemDto> Rows { set; }

        /// <summary>우측 상세. null 이면 비운다 (03 §5.5 재조회 시 상세 Clear).</summary>
        PatientDetailDto Detail { set; }

        /// <summary>
        /// 우측 상세의 `예약 상태` 한 줄. 목록 컬럼은 `가능`/`불가` 두 값뿐이라 훑기에는 좋지만
        /// 일정을 말해 주지 못한다 — 고른 사람에 대해서만 날짜까지 적는다.
        /// </summary>
        string ReserveStatusText { set; }

        /// <summary>행 선택 여부에 따르는 Ribbon Action (03 §5.2).</summary>
        bool RowSelected { set; }

        void ShowMessage(string message);
    }
}
