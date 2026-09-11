// 화면 ID: WF-PAT-01 — 수검자 관리 (03 §5)
namespace HealthCheckupReservationReception.Models
{
    /// <summary>
    /// 03 §5.2 — 수검자 관리의 Ribbon Action 상태. Ribbon 은 MainForm 이 갖고 판정은
    /// Presenter 가 하므로, 그 판정을 실어 나르는 자리다 (<see cref="WorkActionState"/> 와 같은 꼴).
    ///
    /// <see cref="Reserve"/> 만 축이 하나 더 있다: **그 수검자가 예약 가능한가**
    /// (2026-09-11 사용자 지시). 목록이 `불가` 라고 적어 두고 버튼은 열어 두면 화면이 스스로
    /// 모순되고, 눌러 봐야 모달이 그 사실을 다시 말한다.
    ///
    /// [X] **모르는 것은 불가가 아니다.** 업무 조회가 실패해 `예약` 칸이 비었을 때 닫아 버리면
    ///     DB 한 번 끊긴 것으로 예약이 통째로 막힌다 — 그때는 열어 두고 DB 가 판정하게 한다
    ///     (R12 가 지키려던 것이 이것이다).
    /// </summary>
    public sealed class PatientActionState
    {
        public bool RowSelected { get; set; }
        public bool Reserve { get; set; }

        /// <summary>행 미선택 — 전부 닫힌 상태다 (03 §5.2).</summary>
        public static PatientActionState None()
        {
            return new PatientActionState();
        }
    }
}
