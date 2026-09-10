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

        // 조회조건 (03 §5.3). 화면이 담은 그대로 넘긴다 — 정규화는 Service 가 한다.
        // 2026-09-10 사용자 결정으로 생년월일·휴대전화는 조회조건에서 걷었다. SP-PAT-01 은
        // 그 둘을 여전히 받지만 (05 §7.2) 화면이 채우지 않으므로 미입력으로 간다.
        string ChartNo { get; }
        string Name { get; }
        string SocialNumber { get; }

        IList<PatientListItemDto> Rows { set; }

        /// <summary>우측 상세. null 이면 비운다 (03 §5.5 재조회 시 상세 Clear).</summary>
        PatientDetailDto Detail { set; }

        /// <summary>행 선택 여부에 따르는 Ribbon Action (03 §5.2).</summary>
        bool RowSelected { set; }

        void ShowMessage(string message);
    }
}
