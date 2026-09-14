namespace HealthCheckupReservationReception.Common
{
    /// <summary>
    /// 05 가 정한 Parameter 크기 중 **Service 가 길이 검증에 쓰는 것**이다. 여기서 값을
    /// 정하지 않는다 — 05 의 Parameter 표가 단일 출처이고 `scripts/verify-param-size.sh` 가
    /// 대조한다.
    ///
    /// [!] **같은 계약값이 `Repositories/` 에도 있다.** `SqlParameter` 의 크기가 그것이고,
    ///     **조용히 자르는 것은 그쪽이다** — 이 파일이 통과시킨 값을 `SqlDbType.NVarChar, 100`
    ///     이 100자로 잘라 넣는다. 그래서 이 파일 하나로 「한자리에 모았다」가 되지 않는다.
    ///     게이트가 `PSZ-005` 로 그쪽도 함께 본다.
    ///
    /// [X] **크기가 틀리면 조용하다.** 컴파일도 되고 fake 를 쓰는 단위시험도 같은 상수를
    ///     쓰므로 함께 틀린다. 코드가 05 보다 크게 잡으면 화면이 통과시킨 값을 DB 가
    ///     자르거나 튕기고, 작게 잡으면 계약이 허락한 입력을 화면이 막는다. 어느 쪽이든
    ///     사용자는 이유를 못 본다.
    ///
    /// [X] **같은 숫자라고 한 상수로 묶지 않는다.** `@주소` 와 `@상세주소` 는 둘 다 200 이고
    ///     `@성명` 과 `@휴무일명` 은 둘 다 100 이지만 서로 다른 계약값이다. 한쪽만 바뀌는
    ///     날 묶어 둔 상수는 조용히 틀린다 — 예전에 `AddressMax` 하나가 그 둘을 함께 재고
    ///     있었다.
    ///
    /// [X] **정확 자릿수는 여기 없다.** `주민번호 13자리`·`생년월일 8자리` 는 도메인 규칙이고
    ///     Parameter 크기와 우연히 같을 뿐이다. 여기 두면 05 가 폭을 넓히는 날 「정확히
    ///     20자리」가 되어 조용히 틀린다 — 그 둘은 PatientService 가 제 자리에 갖는다.
    ///
    /// 줄 끝의 `// @파라미터` 는 장식이 아니라 게이트가 읽는 대응이다. 상수를 더하면
    /// 그 표시를 함께 단다 — 표시가 없는 상수는 게이트가 FAIL 로 센다.
    /// </summary>
    public static class DbSize
    {
        public const int ChartNo = 100;         // @차트번호
        public const int PatientName = 100;     // @성명
        public const int MobilePhone = 13;      // @휴대전화
        public const int Phone = 13;            // @전화번호
        public const int Email = 200;           // @이메일
        public const int Zipcode = 10;          // @우편번호
        public const int Address = 200;         // @주소
        public const int AddressDetail = 200;   // @상세주소
        public const int OperatorName = 50;     // @조작자명
        public const int HolidayName = 100;     // @휴무일명
        public const int HolidayMemo = 500;     // @비고
    }
}
