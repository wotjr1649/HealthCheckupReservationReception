// 화면 ID: DLG-RCP-02 — 접수완료 추가검사 변경 (03 §12)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// DLG-RCP-02 (03 §12). DevExpress 타입이 나타나지 않는다 (킷 §2).
    ///
    /// **고칠 수 있는 것은 AEX 하나다.** 수검자·예약일·시간대·NEX 는 ReadOnly 이고
    /// 상태는 `RCP` 를 유지한다.
    /// </summary>
    public interface IExtraExamView
    {
        event EventHandler SaveRequested;

        string Title { set; }
        WorkDetailDto Detail { set; }
        IList<WorkExamItemDto> NexItems { set; }

        /// <summary>05 §8.2 RS5 — 정확히 7행. 고르는 자리다.</summary>
        IList<ReservationAexItemDto> AexOptions { set; }

        /// <summary>화면이 지금 들고 있는 선택. 순서가 곧 `추가검사01`~`07` 이다.</summary>
        bool[] AexSelection { get; }

        bool SaveEnabled { set; }
        string ValidationMessage { set; }

        void Done();
    }
}
