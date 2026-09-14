// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// DLG-HOL-01 휴무일 관리 (03 §24). DevExpress 타입이 나타나지 않는다 (킷 §2).
    ///
    /// 03 §24 는 Modal 로 적었지만 2026-09-11 사용자 결정으로 **업무 화면**이 되었다 —
    /// 목록 조회·추가·수정·삭제는 원래 목록 화면 성격이고, 리본 탭이 「가는 곳」만 갖게
    /// 되면서 자리가 생겼다 (ROOT AGENTS.md §1.1 이 배치·내비게이션을 풀었다).
    /// 화면 ID 는 그대로다 — 07 §3 의 ID 목록이 문서 쪽 단일 출처다.
    /// </summary>
    public interface IHolidayView
    {
        /// <summary>`[조회]` 를 눌렀다.</summary>
        event EventHandler SearchRequested;

        /// <summary>Grid 의 행 선택이 바뀌었다. 선택이 없으면 null 이다.</summary>
        event EventHandler<HolidayListItemDto> SelectionChanged;

        // ── 조회조건 (05 §12.5 — 두 날짜는 필수다)

        /// <summary>
        /// `[X]` **Presenter 가 쓴다.** 기본값(03 §24.4 오늘부터 두 해)의 `오늘` 은 DB 가
        /// 주므로 화면이 스스로 채울 수 없다 — `get` 만 두었더니 채우는 쪽이 없어 두 칸이
        /// 빈 채로 남았고, 그 하나로 이 화면의 CRUD 가 전부 죽었다 (2026-09-11 실측).
        /// </summary>
        DateTime? FromDate { get; set; }
        DateTime? ToDate { get; set; }

        /// <summary>null 이면 세 구분 전부다.</summary>
        string HolidayTypeFilter { get; }

        IList<HolidayListItemDto> Rows { set; }

        /// <summary>고른 행을 다시 잡아 준다. 저장 뒤 목록을 다시 읽어도 자리를 잃지 않는다.</summary>
        void SelectDate(DateTime holidayDate);

        // ── 입력행 (03 §24.5 — 자체휴무일만 편집한다)

        DateTime? InputDate { get; set; }
        string InputName { get; set; }
        bool InputActive { get; set; }
        string InputMemo { get; set; }

        /// <summary>
        /// 03 §24.4 — 법정·대체 행이 선택되면 입력행과 [수정]·[삭제] 를 닫는다. **숨기지 않는다.**
        /// </summary>
        bool EditEnabled { set; }

        /// <summary>[수정]·[삭제] 는 자체휴무일 행이 잡혀 있을 때만 열린다.</summary>
        bool RowActionsEnabled { set; }

        /// <summary>
        /// 03 §24.6 만료 경고 한 줄. 비면 지운다.
        /// **임계 숫자를 화면이 갖지 않는다** — Presenter 가 DB 가 준 둘을 비교한 결과만 온다.
        /// </summary>
        string RegistryWarning { set; }

        /// <summary>차단·실패 사유 한 줄. 모달이 아니라 Inline 이다.</summary>
        string BlockMessage { set; }

        void ShowMessage(string message);

        /// <summary>03 §24.5 삭제는 되돌릴 수 없다 — 물리 삭제다.</summary>
        bool Confirm(string message);
    }
}
