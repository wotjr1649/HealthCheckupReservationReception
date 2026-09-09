// 화면 ID: DLG-PAT-02 — 수검자 선택 (03 §7)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// DLG-PAT-02 수검자 선택 Modal (03 §7). 조회계약은 WF-PAT-01 과 같다 (03 §7.2 · §5.3).
    /// DevExpress 타입이 나타나지 않는다 (킷 §2).
    /// </summary>
    public interface IPatientSelectView
    {
        event EventHandler SearchRequested;

        /// <summary>Grid 의 행 선택이 바뀌었다. 선택이 없으면 null 이다.</summary>
        event EventHandler<long?> SelectionChanged;

        string ChartNo { get; }
        string Name { get; }
        string SocialNumber { get; }
        string Birthday { get; }
        string MobilePhone { get; }

        IList<PatientListItemDto> Rows { set; }

        /// <summary>03 §7.2 — 행이 잡혀 있어야 `[선택]` 으로 PatientId 를 돌려줄 수 있다.</summary>
        bool SelectEnabled { set; }

        void ShowMessage(string message);
    }
}
