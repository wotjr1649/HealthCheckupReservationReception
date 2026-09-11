// 화면 ID: DLG-RCP-01 — 접수 처리 (03 §11)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// DLG-RCP-01 (03 §11). DevExpress 타입이 나타나지 않는다 (킷 §2).
    ///
    /// **읽기 전용 화면에 버튼 하나다** (03 §11.2) — 수검자·예약일·시간대·NEX·AEX 가 전부
    /// ReadOnly 이고 접수 단계에서 AEX 를 고치지 않는다. 접수 전 변경은 예약변경으로 한다.
    /// </summary>
    public interface IReceptionView
    {
        /// <summary>`[접수처리]` 를 눌렀다.</summary>
        event EventHandler ReceiveRequested;

        /// <summary>03 §11.1 좌우 두 칸. null 이면 비운다.</summary>
        WorkDetailDto Detail { set; }

        IList<WorkExamItemDto> NexItems { set; }
        IList<WorkExamItemDto> AexItems { set; }

        /// <summary>
        /// 03 §11.1 `접수 가능 여부 : 가능 / 불가 + 사유`. 값은 05 §8.2 RS4 의
        /// `START_RECEPTION` 행 그대로다 — 화면이 다시 계산하지 않는다.
        /// </summary>
        string EligibilityText { set; }

        /// <summary>`[접수처리]` 의 여닫음. RS4 `허용여부` 그대로다.</summary>
        bool ReceiveEnabled { set; }

        /// <summary>실패 사유 한 줄. null 이면 지운다 — 모달을 겹치지 않는다.</summary>
        string ValidationMessage { set; }

        /// <summary>접수가 끝났다. 창을 닫고 부른 쪽이 목록을 다시 읽는다.</summary>
        void Done();
    }
}
