namespace HealthCheckupReservationReception.Common
{
    /// <summary>
    /// `04` §8.4 `휴무일.휴무구분` 의 세 값 (`00` HOL-03).
    ///
    /// 사본이므로 원본이 바뀌면 뒤처진다 (ROOT AGENTS.md §6) — `DbWorkAction` · `DbWorkStatus` 와
    /// 같은 처지이고 같은 방식으로 지킨다: `scripts/verify-holiday-type.sh` 가 배포된
    /// `01_Schema.sql` 의 `CK_휴무일_TYPE` 제약과 양방향으로 대조한다.
    ///
    /// [X] 화면이 이 값을 **판정에 쓴다.** 05 §12.7·§12.8 이 `휴무구분 = 자체휴무일` 이 아닌 행에
    ///     `802` 를 돌려주므로 (03 §24.5) 화면은 그 전에 입력행과 [수정]·[삭제] 를 닫는다.
    ///     오타가 나면 **모든 행이 편집 불가**가 되거나 **법정공휴일이 편집 가능**으로 열린다.
    /// </summary>
    public static class DbHolidayType
    {
        public const string Statutory = "법정공휴일";
        public const string Substitute = "대체공휴일";
        public const string Own = "자체휴무일";
    }
}
