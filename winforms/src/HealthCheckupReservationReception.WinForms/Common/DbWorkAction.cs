namespace HealthCheckupReservationReception.Common
{
    /// <summary>
    /// 05 §8.2 RS4 가 고정으로 돌려주는 `업무동작코드` 다섯이다.
    ///
    /// 사본이므로 원본이 바뀌면 뒤처진다 (ROOT AGENTS.md §6) — `DbCode` 와 같은 처지이고
    /// 같은 방식으로 지킨다: `scripts/verify-work-actions.sh` 가 이 파일과 05 §8.2 를
    /// 양방향으로 대조한다. 값을 여기서 정하지 않는다.
    ///
    /// 문자열로 두는 이유는 RS4 컬럼이 `VARCHAR(30)` 이기 때문이다. Enum 으로 두면
    /// 읽는 자리에서 다시 문자열 대응표를 만들게 된다.
    /// </summary>
    public static class DbWorkAction
    {
        public const string EditReservation = "EDIT_RESERVATION";
        public const string CancelReservation = "CANCEL_RESERVATION";
        public const string StartReception = "START_RECEPTION";
        public const string EditExtra = "EDIT_EXTRA";
        public const string CancelReception = "CANCEL_RECEPTION";
    }
}
