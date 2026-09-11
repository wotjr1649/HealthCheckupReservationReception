namespace HealthCheckupReservationReception.Common
{
    /// <summary>
    /// 05 §8.3 `@대상테이블` 의 허용값 둘. `04` §8.6.3 의 `CK_변경이력_TARGET_TABLE` 이
    /// 실제로 강제하는 값이고, 그 밖의 값은 SP 가 `101` 로 돌려보낸다.
    ///
    /// 사본이므로 원본이 바뀌면 뒤처진다 (ROOT AGENTS.md §6) — `DbHolidayType` 과 같은
    /// 처지이고 같은 게이트가 지킨다: `scripts/verify-check-values.sh` 가 이 파일과 배포
    /// 스키마의 CHECK 제약을 양방향으로 대조한다. 값을 여기서 정하지 않는다.
    ///
    /// [X] **완료이력은 여기 없다.** `변경이력` 의 대상은 둘뿐이다 — 완료이력은 스크립트로
    ///     넣고 복합키라 `대상키 BIGINT` 하나로 행을 특정하지 못한다 (04 §8.6.3 · 03 §23.5).
    /// </summary>
    public static class DbLogTarget
    {
        public const string Patient = "수검자";
        public const string Work = "예약접수";
    }
}
