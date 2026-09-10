// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// WF-WRK-01 예약/접수 공통 Workbench (03 §9). DevExpress 타입이 나타나지 않는다 (킷 §2).
    ///
    /// 화면은 하나이고 Reservation·Reception 두 Context 가 나눠 쓴다 (03 §9.1).
    /// </summary>
    public interface IWorkbenchView
    {
        /// <summary>조회조건의 `[조회]` 를 눌렀거나 조건 칸에서 Enter 를 쳤다.</summary>
        event EventHandler SearchRequested;

        /// <summary>Grid 의 행 선택이 바뀌었다. 선택이 없으면 null 이다 (03 §9.4).</summary>
        event EventHandler<long?> SelectionChanged;

        // 조회조건 (03 §9.3). 화면이 담은 그대로 넘긴다 — 정규화는 Service 가 한다.
        DateTime? FromDate { get; }
        DateTime? ToDate { get; }

        /// <summary>상태 드롭다운. `전체` 는 null 이며 조회조건으로 세지 않는다 (03 §9.3).</summary>
        string StatusCode { get; }

        string ChartNo { get; }

        // `Name` 은 Control 이 이미 갖는 이름이라 쓰지 않는다 — 명시적 구현으로 가르는 것보다
        // 처음부터 다른 이름을 두는 편이 읽기 쉽다.
        string PatientName { get; }

        /// <summary>지금 어느 Context 인가 (03 §9.1). Ribbon Page 와 별개로 화면에도 적는다.</summary>
        string ContextTitle { set; }

        /// <summary>03 §9.3 — From&gt;To 와 조건 없음은 Inline 오류다. null 이면 지운다.</summary>
        string ValidationMessage { set; }

        IList<WorkListItemDto> Rows { set; }

        /// <summary>우측 Detail (03 §9.5). null 이면 비운다.</summary>
        WorkDetailDto Detail { set; }

        IList<WorkExamItemDto> NexItems { set; }
        IList<WorkExamItemDto> AexItems { set; }

        /// <summary>03 §9.6 · §9.7 의 Ribbon Action 상태. Ribbon 은 MainForm 이 갖는다.</summary>
        WorkActionState Actions { set; }

        void ShowMessage(string message);
    }
}
