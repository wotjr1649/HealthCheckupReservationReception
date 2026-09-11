using System;
using System.Globalization;

namespace HealthCheckupReservationReception.Common
{
    /// <summary>
    /// 예약·접수 값의 화면 표기. 값 자체는 바꾸지 않는다 — `clsPatientText` 와 같은 자리다.
    ///
    /// 예약 계열 화면 여럿이 같은 것을 쓰므로 한 벌만 둔다 (킷 §2).
    /// </summary>
    public static class clsWorkText
    {
        public const string SlotMorning = "AM";
        public const string SlotAfternoon = "PM";

        // 05 §7.x 의 `국가검사구분` 도메인. 03 §8.8 은 화면에 기본/조건부로 적으라고 한다.
        public const string NexBasic = "BASIC";
        public const string NexConditional = "CONDITIONAL";

        /// <summary>
        /// 04 §8.x 의 시간대코드 `AM`/`PM` 을 화면 글로 바꾼다.
        /// 모르는 값은 그대로 낸다 — 화면이 값을 숨기면 사용자가 무엇을 본 것인지 알 수 없다.
        /// </summary>
        public static string FormatSlot(string value)
        {
            if (value == SlotMorning) { return "오전"; }
            if (value == SlotAfternoon) { return "오후"; }
            return value ?? string.Empty;
        }

        /// <summary>
        /// 03 §8.8 — 국가검사 구분은 화면에서 기본/조건부로 적는다. DB 는 `BASIC`/`CONDITIONAL`
        /// 로 준다 (05 §7.x). 모르는 값은 그대로 낸다 — 틀려도 원문이 보이므로 조용하지 않다.
        /// </summary>
        public static string FormatNexType(string value)
        {
            if (value == NexBasic) { return "기본"; }
            if (value == NexConditional) { return "조건부"; }
            return value ?? string.Empty;
        }

        /// <summary>
        /// 03 §9.5 — 정원현황은 **현재 조회값**이며 Work 저장 Snapshot 이 아니다.
        /// 수치는 전부 DB 가 준 것을 그대로 쓴다 (05 §8.2 RS1).
        ///
        /// [X] **`잔여자리` 는 SP 마다 뜻이 다르다.** 여기(SP-WRK-02 RS1)는
        ///     `MAX(0, 정원 - 현재인원)` 이라 `2 / 20 (잔여 18)` 이 그대로 맞는다.
        ///     예약 화면의 것은 `MAX(0, 정원 - **적용후인원**)` 이고 신규는 적용후인원이
        ///     현재인원+1 이라 `2 / 20 (잔여 17)` 이 된다 — 같은 문구를 쓰면 숫자가 안 맞는
        ///     것처럼 읽힌다(실측 2026-09-11 사용자 보고). 그래서 함수를 둘로 갈랐다.
        /// </summary>
        public static string FormatCapacity(int current, int capacity, int remaining)
        {
            return Counts(current, capacity) + " (잔여 " + Number(remaining) + ")";
        }

        /// <summary>
        /// 03 §8.3 예약 화면의 시간대 줄. `잔여자리` 가 **이 예약을 넣은 뒤**의 수다
        /// (05 §9.7 `잔여자리 = MAX(0, 정원 - 적용후인원)`, 신규는 `적용후인원 = 현재인원 + 1`).
        ///
        /// 앞의 `현재인원 / 정원` 과 기준이 다르므로 그 사실을 글자로 적는다 — 숫자를 화면이
        /// 고쳐 계산하지 않는다.
        /// </summary>
        public static string FormatCapacityAfterBooking(int current, int capacity, int remainingAfter)
        {
            return Counts(current, capacity) + " (예약 후 잔여 " + Number(remainingAfter) + ")";
        }

        private static string Counts(int current, int capacity)
        {
            return Number(current) + " / " + Number(capacity);
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 05 §2.2 `상태코드` 넷의 화면 표기 (2026-09-11 사용자 지시).
        ///
        /// [X] **DB 의 `상태명` 컬럼을 화면에 쓰지 않는다.** 그쪽은 `RSV` 를 `예약` 이라 적는데
        ///     창구가 읽어야 하는 것은 `예약완료` 다 — `접수완료` 와 짝이 맞아야 두 값이 같은
        ///     축의 두 눈금으로 읽힌다. 계약은 동결이라 DB 를 고칠 수 없으므로 화면이 표시명을
        ///     갖는다. 그래서 `StatusName` 을 DTO 에서 걷었다: 남겨 두면 같은 뜻의 문자열이
        ///     두 곳에 있게 된다 (ROOT AGENTS.md §6).
        ///
        /// [X] 이 표에는 게이트가 없다. `verify-work-status.sh` 가 지키는 것은 왼쪽의 **코드**
        ///     넷이고, 오른쪽 표시명은 DB 와 **일부러 다르므로** 대조할 대상이 없다. 그래서
        ///     한 곳에만 둔다 — 베끼는 순간 지킬 방법이 사라진다.
        ///
        /// 모르는 값은 그대로 낸다 — 화면이 값을 숨기면 사용자가 무엇을 본 것인지 알 수 없다.
        /// </summary>
        public static string FormatStatus(string value)
        {
            if (DbWorkStatus.Reserved.Equals(value, StringComparison.Ordinal)) { return "예약완료"; }
            if (DbWorkStatus.Received.Equals(value, StringComparison.Ordinal)) { return "접수완료"; }
            if (DbWorkStatus.CancelledReservation.Equals(value, StringComparison.Ordinal)) { return "예약취소"; }
            if (DbWorkStatus.CancelledReception.Equals(value, StringComparison.Ordinal)) { return "접수취소"; }
            return value ?? string.Empty;
        }

        /// <summary>
        /// 05 §9.6 RS1 `예약구분`. 조작자가 고른 값이 아니라 DB 가 시각으로 가른 값이므로
        /// (00 RP-05) 화면은 그것을 읽어 주기만 한다.
        /// </summary>
        public static string FormatReserveType(string value)
        {
            if (DbReserveType.WalkIn.Equals(value, StringComparison.Ordinal)) { return "현장 당일예약"; }
            if (DbReserveType.Normal.Equals(value, StringComparison.Ordinal)) { return "일반 예약"; }
            return value ?? string.Empty;
        }

        public static string FormatDate(DateTime value)
        {
            return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
