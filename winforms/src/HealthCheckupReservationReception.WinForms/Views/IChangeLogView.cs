// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
using System.Collections.Generic;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// DLG-LOG-01 (03 §23). DevExpress 타입이 나타나지 않는다 (킷 §2).
    ///
    /// **Action 이 없다.** 03 §23.4 가 읽기 전용으로 못박았으므로 이벤트도 편집도 없다 —
    /// 화면이 하는 일은 열릴 때 한 번 그리는 것뿐이다.
    /// </summary>
    public interface IChangeLogView
    {
        /// <summary>§23.3 제목줄. 어느 행의 이력인지 여기서 말한다.</summary>
        string Subject { set; }

        IList<ChangeLogItemDto> Rows { set; }

        /// <summary>Grid 위 한 줄. 조회 실패 사유가 여기 선다 — 모달을 겹치지 않는다.</summary>
        string ValidationMessage { set; }
    }
}
