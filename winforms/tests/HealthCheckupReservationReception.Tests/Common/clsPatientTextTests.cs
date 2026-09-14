using HealthCheckupReservationReception.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Common
{
    /// <summary>
    /// 수검자 값의 표기·정규화. 표기는 `03` §5.6 이, 정규화는 `05` §2.2 가 정한다.
    /// </summary>
    [TestClass]
    public class clsPatientTextTests
    {
        // [2026-09-10 사용자 요청] 전화 두 칸은 `-` 를 자동으로 넣는다.
        // 한국 지역번호는 자릿수가 아니라 **번호대**로 정해지므로 표로 못박는다 —
        // 0212345678 을 02-1234-5678 로 끊을지 021-234-5678 로 끊을지는 규칙이 정한다.
        [DataTestMethod]
        [DataRow("021234567", "02-123-4567", "서울 9자리")]
        [DataRow("0212345678", "02-1234-5678", "서울 10자리")]
        [DataRow("0311234567", "031-123-4567", "경기 10자리")]
        [DataRow("03112345678", "031-1234-5678", "경기 11자리")]
        [DataRow("07012345678", "070-1234-5678", "인터넷전화")]
        [DataRow("01012345678", "010-1234-5678", "휴대전화 11자리")]
        [DataRow("0111234567", "011-123-4567", "휴대전화 10자리 (구번호)")]
        [DataRow("15881234", "1588-1234", "대표번호")]
        [DataRow("010-1234-5678", "010-1234-5678", "이미 넣어 둔 값은 그대로")]
        [DataRow("010 1234 5678", "010-1234-5678", "공백도 숫자만 남긴다")]
        public void 전화번호는_번호대_규칙으로_끊는다(string input, string expected, string why)
        {
            Assert.AreEqual(expected, clsPatientText.FormatPhone(input), why);
        }

        // [!] `04` §8.1.2 — 휴대전화·전화번호는 VARCHAR(13) 이다. 하이픈을 넣으면 14자가
        //     되는 번호대(0504·0505·0507 …)가 있어서 그 자리에서는 하이픈을 포기한다.
        //     번호를 잃는 것보다 낫다 — Service 의 13자 검증이 거부하기 때문이다.
        [TestMethod]
        public void 열세자를_넘기면_하이픈을_포기한다()
        {
            string got = clsPatientText.FormatPhone("050412345678");   // 0504-1234-5678 = 14자
            Assert.AreEqual("050412345678", got);
            Assert.IsTrue(got.Length <= 13, "저장 폭을 넘겼다");
        }

        [TestMethod]
        public void 분류할_수_없으면_숫자를_그대로_돌려준다()
        {
            // 지어내지 않는다. 어느 규칙에도 안 맞으면 숫자만 남긴다.
            Assert.AreEqual("123", clsPatientText.FormatPhone("123"));
            Assert.IsNull(clsPatientText.FormatPhone("   "));
            Assert.IsNull(clsPatientText.FormatPhone(null));
        }

        [TestMethod]
        public void 주민번호는_여섯_일곱로_끊고_마스킹하지_않는다()
        {
            // 03 §5.6 No 7 — 과제는 임의 시험값만 쓰므로 가리지 않는다 (00 §2.1).
            Assert.AreEqual("990707-2000018", clsPatientText.FormatSocialNumber("9907072000018"));
        }
        // [2026-09-10 사용자 요청] 자릿수는 **하이픈을 빼고** 센다.
        // 휴대전화 10~11 은 CK_수검자_CEL_DIGIT 이 정한 값 그대로다 (04 §8.1.3).
        [DataTestMethod]
        [DataRow("010-1234-5678", true, true, "휴대전화 11자리")]
        [DataRow("011-123-4567", true, true, "휴대전화 10자리")]
        [DataRow("010-123-456", true, false, "9자리는 짧다")]
        [DataRow("010123456789", true, false, "12자리는 길다")]
        [DataRow(null, true, true, "미입력은 선택이라 통과")]
        [DataRow("   ", true, true, "공백도 미입력")]
        [DataRow("02-123-4567", false, true, "전화번호 9자리")]
        [DataRow("1588-1234", false, true, "대표번호 8자리")]
        [DataRow("1234567", false, false, "7자리는 짧다")]
        [DataRow("031-1234-56789", false, false, "12자리는 길다")]
        public void 전화_자릿수는_하이픈을_빼고_센다(string input, bool mobile, bool expected, string why)
        {
            Assert.AreEqual(expected, clsPatientText.IsPhoneDigitCountValid(input, mobile), why);
        }

        // [2026-09-14] 화면 셋에 흩어져 있던 `Pair` 를 여기로 모았다. 한쪽이 비면 `/` 를
        // 적지 않는 것이 이 함수의 전부이고, 그것이 틀리면 빈 값 옆에 남은 `/` 가
        // 사용자에게 깨진 값으로 읽힌다.
        [DataTestMethod]
        [DataRow("19990707", "F", "1999-07-07 / 여", "둘 다 있으면 잇는다")]
        [DataRow("19990707", null, "1999-07-07", "성별이 없으면 생년월일만")]
        [DataRow(null, "M", "남", "생년월일이 없으면 성별만")]
        [DataRow(null, null, "", "둘 다 없으면 빈 문자열")]
        public void 생년월일과_성별은_한쪽이_비면_남는_쪽만_적는다(string birthday, string gender, string expected, string why)
        {
            Assert.AreEqual(expected, clsPatientText.FormatBirthGender(birthday, gender), why);
        }

        [DataTestMethod]
        [DataRow("12345", "서울시", "12345 / 서울시", "둘 다 있으면 잇는다")]
        [DataRow("12345", "   ", "12345", "공백은 없는 것이다")]
        [DataRow("", "서울시", "서울시", "왼쪽이 비면 오른쪽만")]
        public void 한_칸에_둘을_넣을_때도_같은_규칙이다(string left, string right, string expected, string why)
        {
            Assert.AreEqual(expected, clsPatientText.Pair(left, right), why);
        }

    }
}
