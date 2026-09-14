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
        // 대상: clsWorkText.FormatStatus — 업무 상태코드 넷의 화면 표시명 (DataRow 4건)
        // 목적: 2026-09-11 사용자 지시로 RSV 를 예약 이 아니라 예약완료 로 적는다 — 접수완료 와
        //       짝이 맞아야 두 값이 같은 축의 두 눈금으로 읽힌다. 이 표시명은 DB 의 상태명 과
        //       일부러 다르므로 대조할 게이트가 없고, 그 자리를 이 시험이 대신 지킨다.
        // 확인: RSV→예약완료 · RCP→접수완료 · CNR→예약취소 · CNC→접수취소 네 쌍이 그대로 나온다.
        [DataTestMethod]
        [DataRow("RSV", "예약완료")]
        [DataRow("RCP", "접수완료")]
        [DataRow("CNR", "예약취소")]
        [DataRow("CNC", "접수취소")]
        public void 상태코드_넷은_화면_표시명을_갖는다(string code, string expected)
        {
            Assert.AreEqual(expected, clsWorkText.FormatStatus(code));
        }

        // 대상: clsWorkText.FormatStatus — 표에 없는 상태코드가 들어온 경우 (DataRow 2건)
        // 목적: 모르는 코드를 빈칸이나 「알 수 없음」으로 바꾸면 조작자는 무엇을 본 것인지
        //       알 수 없고, DB 에 예상 못한 값이 들어와도 화면이 그것을 숨긴다.
        // 확인: XXX 는 XXX 그대로 나오고, null 은 빈 문자열이 된다.
        [DataTestMethod]
        [DataRow("XXX", "XXX")]
        [DataRow(null, "")]
        public void 모르는_상태는_원문을_그대로_낸다(string code, string expected)
        {
            Assert.AreEqual(expected, clsWorkText.FormatStatus(code));
        }

        // 대상: clsWorkText.FormatCapacity · FormatCapacityBeforeBooking — 정원 표기 두 종
        // 목적: 05 §8.2 RS1 의 잔여자리는 정원-현재인원 이고 05 §9.7 RS2 의 잔여자리는
        //       정원-적용후인원 이다. 같은 이름으로 다른 것을 주므로 문구가 기준을 밝히지 않으면
        //       조작자는 두 화면에서 다른 수를 보고 어느 쪽이 맞는지 알 수 없다 (2026-09-11).
        // 확인: 접수 쪽은 「2 / 20 (잔여 18)」, 예약 쪽은 「2 / 20 (예약 전 잔여 18)」로
        //       괄호 안 문구가 서로 다르다.
        [TestMethod]
        public void 정원_문구는_잔여의_기준을_밝힌다()
        {
            Assert.AreEqual("2 / 20 (잔여 18)", clsWorkText.FormatCapacity(2, 20, 18));
            Assert.AreEqual("2 / 20 (예약 전 잔여 18)", clsWorkText.FormatCapacityBeforeBooking(2, 20));
        }

        // 대상: clsWorkText.FormatCapacityBeforeBooking — 예약 화면 정원 문구 (DataRow 4건)
        // 목적: 예약 화면은 DB 의 잔여자리 를 쓰지 않고 정원-현재인원 으로 적는다 (2026-09-12
        //       사용자 지시). 앞의 두 수와 뒤의 잔여가 어긋나 보이면 조작자가 계산을 의심한다.
        // 확인: 1/20 은 잔여 19, 0/20 은 20, 20/20 은 0 이고, 현재인원이 정원을 넘은 21/20 도
        //       음수가 아니라 0 이다 (05 §9.7 의 MAX(0, …) 와 같은 바닥).
        [TestMethod]
        [DataRow(1, 20, "1 / 20 (예약 전 잔여 19)")]
        [DataRow(0, 20, "0 / 20 (예약 전 잔여 20)")]
        [DataRow(20, 20, "20 / 20 (예약 전 잔여 0)")]
        [DataRow(21, 20, "21 / 20 (예약 전 잔여 0)")]   // 05 §9.7 의 MAX(0, ...)
        public void 예약_전_잔여는_정원에서_현재인원을_뺀다(int current, int capacity, string expected)
        {
            Assert.AreEqual(expected, clsWorkText.FormatCapacityBeforeBooking(current, capacity));
        }
    }
}
