namespace HealthCheckupReservationReception.Common
{
    /// <summary>
    /// 05 §9.2 · §11.1 의 `@예약구분` 두 값. 화면의 `ReservationContext` 가 이것으로 번역된다.
    ///
    /// `DbWorkAction` 과 달리 게이트를 두지 않는다 — 틀린 값은 조용하지 않기 때문이다.
    /// SP 가 곧바로 `102` 로 막고 사용자가 그 자리에서 본다. 업무동작코드는 반대로 틀려도
    /// 아무 소리가 나지 않아(버튼 하나가 영영 닫힌다) 게이트가 필요했다.
    /// </summary>
    public static class DbReserveType
    {
        public const string Normal = "NORMAL";
        public const string WalkIn = "WALKIN";
    }
}
