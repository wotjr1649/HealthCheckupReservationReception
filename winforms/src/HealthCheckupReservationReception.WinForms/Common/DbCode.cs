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
}
