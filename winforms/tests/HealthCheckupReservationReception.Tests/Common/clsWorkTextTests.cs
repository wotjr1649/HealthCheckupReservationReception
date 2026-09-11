using HealthCheckupReservationReception.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Common
{
    /// <summary>
    /// 예약·접수 값의 화면 표기 (`clsWorkText`).
    ///
    /// [X] 상태 표시명에는 게이트가 없다 — DB 의 `상태명` 과 **일부러 다르므로** 대조할
    ///     대상이 없다(`verify-work-status.sh` 는 왼쪽의 코드 넷만 지킨다). 그래서 시험이
    ///     그 자리를 대신 지킨다.
    /// </summary>
    [TestClass]
    public class clsWorkTextTests
    {
        // 2026-09-11 사용자 지시 — `예약` 이 아니라 `예약완료` 다. `접수완료` 와 짝이 맞아야
        // 두 값이 같은 축의 두 눈금으로 읽힌다.
        [DataTestMethod]
        [DataRow("RSV", "예약완료")]
        [DataRow("RCP", "접수완료")]
        [DataRow("CNR", "예약취소")]
        [DataRow("CNC", "접수취소")]
        public void 상태코드_넷은_화면_표시명을_갖는다(string code, string expected)
        {
            Assert.AreEqual(expected, clsWorkText.FormatStatus(code));
        }

        // 모르는 값은 그대로 낸다 — 화면이 값을 숨기면 사용자가 무엇을 본 것인지 알 수 없다.
        [DataTestMethod]
        [DataRow("XXX", "XXX")]
        [DataRow(null, "")]
        public void 모르는_상태는_원문을_그대로_낸다(string code, string expected)
        {
            Assert.AreEqual(expected, clsWorkText.FormatStatus(code));
        }

        // 05 §8.2 RS1 은 `정원 - 현재인원`, 05 §9.7 RS2 는 `정원 - 적용후인원` 이다.
        // 같은 `잔여자리` 라는 이름으로 다른 것을 주므로 문구가 기준을 밝힌다 (2026-09-11).
        [TestMethod]
        public void 정원_문구는_잔여의_기준을_밝힌다()
        {
            Assert.AreEqual("2 / 20 (잔여 18)", clsWorkText.FormatCapacity(2, 20, 18));
            Assert.AreEqual("2 / 20 (예약 후 잔여 17)", clsWorkText.FormatCapacityAfterBooking(2, 20, 17));
        }
    }
}
