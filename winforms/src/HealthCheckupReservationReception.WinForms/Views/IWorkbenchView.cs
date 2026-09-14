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

        /// <summary>
        /// Ribbon 의 업무 Action 을 눌렀다 (03 §9.6 · §9.7). **어느 것인지는 인자가 말한다** —
        /// 05 §8.2 의 업무동작코드 다섯이 이미 그 이름을 갖고 있으므로 이벤트를 다섯으로
        /// 늘리지 않는다 (`DbWorkAction`).
        /// </summary>
        event EventHandler<string> ActionRequested;

        /// <summary>
        /// 03 §13 취소 확인. `[확인]` 이면 true 다.
        ///
        /// [X] **화면이 묻는 길을 하나로 모은다.** 시험이 이것만 덮어쓰면 회귀가 사람 손을
        ///     기다리지 않는다 (2026-09-11 사용자 지적 · `FrmReservation.Confirm` 과 같은 규약).
        /// </summary>
        bool Confirm(string message);

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

        /// <summary>
        /// 이 Context 가 다루는 상태코드 (2026-09-11 사용자 지시). 드롭다운이 이것으로 서고
        /// `전체` 는 **이 집합 전부**를 뜻한다 — 탭이 이미 상태로 갈렸기 때문이다.
        /// </summary>
        IList<string> StatusChoices { set; }

        /// <summary>
        /// 조회 기간의 기본값을 세운다 (2026-09-11). Context 를 열 때마다 부른다 —
        /// 접수 창구는 오늘 하루, 예약 창구는 오늘부터 앞이 기본이다.
        /// </summary>
        void ResetSearchRange(DateTime from, DateTime? to);

        /// <summary>03 §9.3 — From&gt;To 와 조건 없음은 Inline 오류다. null 이면 지운다.</summary>
        string ValidationMessage { set; }

        IList<WorkListItemDto> Rows { set; }

        /// <summary>우측 Detail (03 §9.5). null 이면 비운다.</summary>
        WorkDetailDto Detail { set; }

        /// <summary>
        /// 마지막 선택행의 `SP-WRK-02` 한 벌(RS0~RS5) 그대로. **모달이 이것을 받아 열린다**
        /// (2026-09-14 사용자 지시) — 같은 업무ID 로 같은 여섯을 다시 읽지 않는다.
        ///
        /// [!] **낡을 수 있다.** 다른 창구가 그 사이 바꾸면 모달은 옛 값을 보인다. 저장은
        ///     `행버전` 으로 `601` 이, 마감은 `304` 가 막으므로 **틀린 저장은 통과하지 못한다**
        ///     — 화면이 미리 닫지 않는 R12 와 같은 줄이다.
        ///
        /// 선택이 풀리면 null 이다. <see cref="Detail"/> 은 그리기용이고 이것은 넘겨주기용이다.
        /// </summary>
        WorkDetailReadDto Read { get; set; }

        IList<WorkExamItemDto> NexItems { set; }
        IList<WorkExamItemDto> AexItems { set; }

        /// <summary>03 §9.6 · §9.7 의 Ribbon Action 상태. Ribbon 은 MainForm 이 갖는다.</summary>
        WorkActionState Actions { set; }

        /// <summary>
        /// 03 §9.1 WorkId Targeted Navigation — 그 한 건이 보이도록 조회조건을 그 날 하루로
        /// 좁히고 나머지 조건을 비운다. 조건을 그대로 두면 방금 저장한 업무가 목록에 없을 수 있다.
        /// </summary>
        void FocusSearchOn(DateTime day);

        /// <summary>목록에서 그 업무를 골라 세운다. 없으면 false 다.</summary>
        bool SelectWork(long workId);

        void ShowMessage(string message);
    }
}
