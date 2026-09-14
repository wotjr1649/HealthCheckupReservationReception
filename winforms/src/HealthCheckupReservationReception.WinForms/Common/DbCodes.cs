// ── DB 계약이 정한 코드값 ─────────────────────────────────────────────────────
//
// 전부 **04·05 의 사본**이다. 여기서 값을 정하지 않는다 (ROOT AGENTS.md §6).
// 무엇이 어디를 베낀 것이고 무엇이 그것을 지키는지는 각 형식의 주석이 적는다.
//
//   DbCode           결과코드 Enum       05 §16.1    verify-dbcode.sh
//   DbWorkStatus     상태코드 넷         05 §2.2     verify-work-status.sh
//   DbWorkAction     업무동작코드 다섯   05 §8.2     verify-work-actions.sh  WKA-001
//   DbHolidayAction  휴무동작코드 둘     05 §12.6    verify-work-actions.sh  WKA-002
//   DbHolidayType    휴무구분 셋         04 §8.4     verify-check-values.sh
//   DbLogTarget      대상테이블 둘       04 §8.6.3   verify-check-values.sh
//   DbReserveType    예약구분 둘         05 §9.2     게이트 없다 — 이유는 그 자리에 적는다
//
// [!] **게이트는 `public static class <이름>` 줄부터 그 class 의 닫는 `}` 까지만 읽는다.**
//     한 파일에 상수 class 가 여럿이므로 좁혀 읽는다. class 이름을 바꾸거나 파일을 나누면
//     해당 스크립트의 `CLASS` 기본값을 같이 고친다 — 못 찾으면 출력이 비고 그 게이트는
//     FAIL 이다 (조용히 통과하지 않는다).

namespace HealthCheckupReservationReception.Common
{
    /// <summary>
    /// 05_DB_Rule_SP_Contract.md §16.1 이 확정한 ResultCode Enum 이다.
    /// 여기서 값을 정하지 않는다 — 05 §16.1 이 단일 출처이고
    /// scripts/verify-dbcode.sh 가 이 파일과 그 절을 양방향으로 대조한다.
    /// 분기는 숫자 결과코드로만 한다. 결과메시지 문자열을 비교하지 않는다 (05 §4.3).
    /// </summary>
    public enum DbCode
    {
        Ok = 0,
        NoChange = 1,
        ExistingPatient = 2,

        MissingValue = 100,
        BadValue = 101,
        BadRequest = 102,
        NeedSearchCondition = 103,
        BadDateRange = 104,

        PatientNotFound = 200,
        ChartNoUsed = 201,
        SameNumberDifferentName = 202,
        SimilarPatient = 203,
        SocialNumberUsed = 204,
        SocialChangeBlocked = 205,
        ChartNoLimit = 206,

        PastDate = 300,
        Sunday = 301,
        Holiday = 302,
        SlotClosed = 303,
        CutoffPassed = 304,
        SlotFull = 305,
        OtherReservation = 306,
        NoOpenSlot = 307,
        CenterClosed = 308,
        OutsideHours = 309,

        UnderAge = 400,
        NotDue = 401,
        ExamOff = 410,
        WrongGender = 411,
        ExamDuplicate = 412,

        WorkNotFound = 500,
        WrongPatient = 501,
        WrongStatus = 502,
        NotToday = 503,

        // 600 PatientChanged 는 R7 에서 601 에 흡수되었다. 재사용하지 않는다 (05 §4.4).
        RowChanged = 601,

        ExamSetupError = 700,
        WorkDataError = 701,

        HolidayNotFound = 800,
        HolidayDuplicate = 801,
        HolidayReadOnly = 802
    }

    /// <summary>
    /// 05 §2.2 허용 코드의 `상태코드` 넷이다.
    ///
    /// 사본이므로 원본이 바뀌면 뒤처진다 (ROOT AGENTS.md §6) — `DbWorkAction` 과 같은 처지이고
    /// 같은 방식으로 지킨다: `scripts/verify-work-status.sh` 가 이 class 와 05 §2.2 를 양방향으로
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

    /// <summary>
    /// 05 §8.2 RS4 가 고정으로 돌려주는 `업무동작코드` 다섯이다.
    ///
    /// 사본이므로 원본이 바뀌면 뒤처진다 (ROOT AGENTS.md §6) — `DbCode` 와 같은 처지이고
    /// 같은 방식으로 지킨다: `scripts/verify-work-actions.sh` 가 이 class 와 05 §8.2 를
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

    /// <summary>
    /// `04` §8.4 `휴무일.휴무구분` 의 세 값 (`00` HOL-03).
    ///
    /// 사본이므로 원본이 바뀌면 뒤처진다 (ROOT AGENTS.md §6) — `DbWorkAction` · `DbWorkStatus` 와
    /// 같은 처지이고 같은 방식으로 지킨다: `scripts/verify-holiday-type.sh` 가 배포된
    /// `01_Schema.sql` 의 `CK_휴무일_TYPE` 제약과 양방향으로 대조한다.
    ///
    /// [X] 화면이 이 값을 **판정에 쓴다.** 05 §12.6·§12.8 이 `휴무구분 = 자체휴무일` 이 아닌 행에
    ///     `802` 를 돌려주므로 (03 §24.5) 화면은 그 전에 입력행과 [수정]·[삭제] 를 닫는다.
    ///     오타가 나면 **모든 행이 편집 불가**가 되거나 **법정공휴일이 편집 가능**으로 열린다.
    /// </summary>
    public static class DbHolidayType
    {
        public const string Statutory = "법정공휴일";
        public const string Substitute = "대체공휴일";
        public const string Own = "자체휴무일";
    }

    /// <summary>
    /// 05 §8.3 `@대상테이블` 의 허용값 둘. `04` §8.6.3 의 `CK_변경이력_TARGET_TABLE` 이
    /// 실제로 강제하는 값이고, 그 밖의 값은 SP 가 `101` 로 돌려보낸다.
    ///
    /// 사본이므로 원본이 바뀌면 뒤처진다 (ROOT AGENTS.md §6) — `DbHolidayType` 과 같은
    /// 처지이고 같은 게이트가 지킨다: `scripts/verify-check-values.sh` 가 이 class 와 배포
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
