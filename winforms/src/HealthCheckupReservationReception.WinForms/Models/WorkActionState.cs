// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// 03 §9.6 · §9.7 — Workbench 의 Ribbon Action 상태. Ribbon 은 MainForm 이 갖고
    /// 판정은 Presenter 가 하므로, 그 판정을 실어 나르는 자리다.
    ///
    /// 앞 다섯은 05 §8.2 RS4 `허용여부` 를 그대로 옮긴 것이다 — 화면이 다시 계산하지 않는다.
    /// <see cref="ChangeLog"/> 만 화면 규칙이다: 상태와 무관하게 행이 선택되면 열린다
    /// (03 §9.6 · §23.4 — 취소된 업무의 변경 내역을 보는 것이 열람의 목적이다).
    ///
    /// `[현장 당일예약]` 은 여기 없다. 선택행과 무관한 독립 Action 이라 (03 §9.7)
    /// Reception Ribbon 에서 늘 열려 있다.
    /// </summary>
    public sealed class WorkActionState
    {
        public bool EditReservation { get; set; }
        public bool CancelReservation { get; set; }
        public bool StartReception { get; set; }
        public bool EditExtra { get; set; }
        public bool CancelReception { get; set; }
        public bool ChangeLog { get; set; }

        /// <summary>행 미선택 · 상세 조회 실패 — 전부 닫힌 상태다 (03 §9.6 미선택 행).</summary>
        public static WorkActionState None()
        {
            return new WorkActionState();
        }
    }
}
