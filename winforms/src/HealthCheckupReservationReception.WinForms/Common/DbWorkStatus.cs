namespace HealthCheckupReservationReception.Common
{
    /// <summary>
    /// 05 §2.2 허용 코드의 `상태코드` 넷이다.
    ///
    /// 사본이므로 원본이 바뀌면 뒤처진다 (ROOT AGENTS.md §6) — `DbWorkAction` 과 같은 처지이고
    /// 같은 방식으로 지킨다: `scripts/verify-work-status.sh` 가 이 파일과 05 §2.2 를 양방향으로
    /// 대조한다. 값을 여기서 정하지 않는다.
    ///
    /// [X] 넷은 이미 `UcWorkbench.UI.cs` 의 상태 드롭다운에 문자열로 박혀 있었다. 거기서는
    ///     틀려도 조용하다 — 드롭다운 한 칸이 아무것도 못 찾을 뿐이다. 수검자 목록의
    ///     `예약 가능/불가` 가 같은 값을 쓰기 시작하면서 틀린 값이 **잘못된 판정**이 되므로
    ///     한 곳으로 모으고 게이트를 붙였다.
    ///
    /// 표시명은 여기 없다. DB 가 `상태명` 을 함께 돌려주므로 (05 §8.1 RS1) 화면은 그것을 쓴다.
    /// </summary>
    public static class DbWorkStatus
    {
        public const string Reserved = "RSV";
        public const string Received = "RCP";
        public const string CancelledReservation = "CNR";
        public const string CancelledReception = "CNC";
    }
}
