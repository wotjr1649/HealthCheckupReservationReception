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
        /// 03 §9.5 — 정원현황은 **현재 조회값**이며 Work 저장 Snapshot 이 아니다.
        /// 수치는 전부 DB 가 준 것을 그대로 쓴다 (05 §8.2 RS1).
        /// </summary>
        public static string FormatCapacity(int current, int capacity, int remaining)
        {
            return current.ToString(CultureInfo.InvariantCulture)
                + " / " + capacity.ToString(CultureInfo.InvariantCulture)
                + " (잔여 " + remaining.ToString(CultureInfo.InvariantCulture) + ")";
        }

        public static string FormatDate(DateTime value)
        {
            return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
