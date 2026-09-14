namespace HealthCheckupReservationReception.Common
{
    /// <summary>
    /// [R22] 05 §12.6 `USP_HC_자체휴무일_저장` 의 `@휴무동작코드` 둘이다.
    ///
    /// 등록과 수정이 SP 하나가 되면서 **의도를 값으로 보낸다.** 「행이 있으면 수정」으로
    /// 유도하지 않는 이유는 05 §12.6 이 적는다 — `[추가]` 가 남의 행을 조용히 덮어쓰는 것을
    /// `801` 이 막고, 그 방어선이 서려면 의도가 Parameter 로 와야 한다.
    ///
    /// 사본이므로 원본이 바뀌면 뒤처진다 (ROOT AGENTS.md §6) — <see cref="DbWorkAction"/> 과
    /// 같은 처지이고 같은 게이트가 지킨다 (`scripts/verify-work-actions.sh` WKA-002).
    /// </summary>
    public static class DbHolidayAction
    {
        public const string Create = "CREATE_HOLIDAY";
        public const string Update = "UPDATE_HOLIDAY";
    }
}
