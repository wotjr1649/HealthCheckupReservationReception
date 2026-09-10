// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// WF-RSV-01 신규 예약 (03 §8). DevExpress 타입이 나타나지 않는다 (킷 §2).
    ///
    /// Normal 과 WalkIn 이 이 화면 하나를 나눠 쓴다 (03 §8.1 · §9.8). 정원·TGT·NEX·AEX·
    /// 저장가능은 전부 DB 가 낸 값이고 화면은 그리기만 한다 (05 §9.12).
    /// </summary>
    public interface IReservationView
    {
        /// <summary>수검자가 확정됐다 — 진입 전달키이거나 DLG-PAT-02 에서 고른 결과다.</summary>
        event EventHandler<long> PatientPicked;

        /// <summary>예약일 또는 시간대가 바뀌었다 (03 §8.5 일정 확정).</summary>
        event EventHandler ScheduleChanged;

        /// <summary>Ribbon `[예약저장]` 을 눌렀다 (03 §8.2).</summary>
        event EventHandler SaveRequested;

        // ── 수검자 (03 §8.3)

        /// <summary>null 이면 비운다.</summary>
        PatientDetailDto Patient { set; }

        // ── 일정 (03 §8.5 — 수검자 확정 전에는 Disabled)

        bool ScheduleEnabled { set; }
        DateTime ReserveDate { get; set; }

        /// <summary>WalkIn 은 예약일=DB 오늘날짜 ReadOnly 다 (03 §8.6).</summary>
        bool ReserveDateReadOnly { set; }

        /// <summary>
        /// AM/PM 두 줄을 다시 세운다. 정원이 찼거나 운영하지 않는 시간대는 고를 수 없다 —
        /// 그 판정(<see cref="SlotInfoDto.Selectable"/>)도 DB 가 낸 것이다 (05 §9.7).
        /// </summary>
        IList<SlotInfoDto> Slots { set; }

        /// <summary>고른 시간대. 없으면 null 이다.</summary>
        string SlotCode { get; set; }

        // ── 판정 (03 §8.7 · §8.8 · §8.9)

        /// <summary>`대상판정 : ...` 한 줄.</summary>
        string TargetText { set; }

        IList<WorkExamItemDto> NexItems { set; }
        IList<ReservationAexItemDto> AexItems { set; }

        /// <summary>TGT 비대상이면 AEX 를 통째로 닫는다 (03 §8.5).</summary>
        bool AexEnabled { set; }

        /// <summary>지금 켜져 있는 AEX. 자리 순서가 곧 `추가검사01`~`07` 이다.</summary>
        bool[] AexSelection { get; }

        // ── 저장

        /// <summary>`[예약저장]` 은 05 §9.12 의 `저장가능` 그대로다 — 화면이 다시 세지 않는다.</summary>
        bool SaveEnabled { set; }

        /// <summary>차단 사유 한 줄. null 이면 지운다 (05 §9.6 차단메시지).</summary>
        string BlockMessage { set; }

        /// <summary>
        /// 03 §8.5 기존 유효예약 · §8.11 저장 성공 — 둘 다 신규예약을 접고 Workbench 로 넘긴다.
        /// 화면은 MainForm 으로 올리기만 한다.
        /// </summary>
        void GoToWorkbench(WorkContext context, long workId);

        void ShowMessage(string message);
    }
}
