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
        // 대상: clsPatientText.FormatPhone — 지역·번호대별 하이픈 삽입 (DataRow 10건)
        // 목적: 2026-09-10 사용자 요청으로 전화 두 칸에 하이픈을 자동으로 넣는다. 한국 번호는
        //       자릿수가 아니라 번호대가 끊는 자리를 정한다 — 0212345678 을 자릿수로만 보면
        //       021-234-5678 로도 끊을 수 있고, 그러면 저장된 번호를 사람이 잘못 읽는다.
        // 확인: 서울 02 는 9·10자리를 각각 02-123-4567 · 02-1234-5678 로, 경기 031 은 10·11자리를
        //       031-123-4567 · 031-1234-5678 로, 070·010·011·1588 도 각 번호대 규칙대로 끊는다.
        //       이미 하이픈이 든 값과 공백이 섞인 값은 숫자만 남겨 같은 결과를 낸다.
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

        // 대상: clsPatientText.FormatPhone — 전화번호 표기 정규화
        // 목적: 04 §8.1.2 가 휴대전화·전화번호를 VARCHAR(13) 으로 정했다. 0504·0505·0507 번호대는
        //       하이픈을 넣으면 14자가 되어 저장 폭을 넘고, Service 의 13자 검증이 그 번호를
        //       통째로 거부한다 — 보기 좋으라고 넣은 하이픈 때문에 번호를 잃는다.
        // 확인: 050412345678 (하이픈 시 14자) 을 넣으면 하이픈 없이 050412345678 이 그대로 나오고,
        //       반환값 길이가 13 이하다.
        [TestMethod]
        public void 열세자를_넘기면_하이픈을_포기한다()
        {
            string got = clsPatientText.FormatPhone("050412345678");   // 0504-1234-5678 = 14자
            Assert.AreEqual("050412345678", got);
            Assert.IsTrue(got.Length <= 13, "저장 폭을 넘겼다");
        }

        // 대상: clsPatientText.FormatPhone — 어느 번호대 규칙에도 맞지 않는 입력
        // 목적: 규칙에 없는 값에 하이픈을 지어 넣으면 조작자가 입력한 것과 다른 번호가 화면에
        //       남는다. 모르는 것은 꾸미지 않고 그대로 두는 것이 이 함수의 안전한 기본값이다.
        // 확인: 123 처럼 어느 규칙에도 안 맞는 값은 123 그대로 나오고, 공백만 있거나 null 이면
        //       빈 문자열이 아니라 null 을 돌려준다 (미입력과 빈 값을 구분한다).
        [TestMethod]
        public void 분류할_수_없으면_숫자를_그대로_돌려준다()
        {
            Assert.AreEqual("123", clsPatientText.FormatPhone("123"));
            Assert.IsNull(clsPatientText.FormatPhone("   "));
            Assert.IsNull(clsPatientText.FormatPhone(null));
        }

        // 대상: clsPatientText.FormatSocialNumber — 주민등록번호 화면 표기
        // 목적: 03 §5.6 No 7 이 6-7 로 끊어 보여 주기로 정했고, 00 §2.1 에 따라 이 과제는 임의
        //       시험값만 쓰므로 뒷자리를 가리지 않는다. 조작자가 입력한 값과 화면에 보이는 값이
        //       달라지면 오타를 눈으로 잡을 수 없다.
        // 확인: 9907072000018 이 990707-2000018 로 나온다 — 하이픈 하나만 들어가고 마스킹은 없다.
        [TestMethod]
        public void 주민번호는_여섯_일곱로_끊고_마스킹하지_않는다()
        {
            Assert.AreEqual("990707-2000018", clsPatientText.FormatSocialNumber("9907072000018"));
        }
        // 대상: clsPatientText.IsPhoneDigitCountValid — 전화·휴대전화 자릿수 검증 (DataRow 10건)
        // 목적: 2026-09-10 사용자 요청으로 자릿수는 하이픈을 빼고 센다. 하이픈까지 세면 같은
        //       번호가 표기 방식에 따라 통과했다 막혔다 하고, 휴대전화 10~11 은 DB 제약
        //       CK_수검자_CEL_DIGIT (04 §8.1.3) 이 정한 값이라 화면이 그것과 어긋나면 안 된다.
        // 확인: 휴대전화는 하이픈을 뺀 10~11자리만 통과하고 9자리·12자리는 막힌다. 전화번호는
        //       8~11자리를 통과시키고 7자리·12자리는 막는다. null·공백은 선택 입력이라 통과다.
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

        // 대상: clsPatientText.FormatBirthGender — 생년월일과 성별을 한 칸에 잇는 표기
        // 목적: 2026-09-14 에 화면 셋에 흩어져 있던 같은 로직을 여기로 모았다. 한쪽이 비었을 때
        //       구분자 / 가 그대로 남으면 조작자에게는 값이 깨진 것으로 읽힌다.
        // 확인: 둘 다 있으면 1999-07-07 / 여, 성별만 없으면 1999-07-07, 생년월일만 없으면 남,
        //       둘 다 없으면 빈 문자열이다 — 어느 경우에도 남는 / 가 없다.
        [DataTestMethod]
        [DataRow("19990707", "F", "1999-07-07 / 여", "둘 다 있으면 잇는다")]
        [DataRow("19990707", null, "1999-07-07", "성별이 없으면 생년월일만")]
        [DataRow(null, "M", "남", "생년월일이 없으면 성별만")]
        [DataRow(null, null, "", "둘 다 없으면 빈 문자열")]
        public void 생년월일과_성별은_한쪽이_비면_남는_쪽만_적는다(string birthday, string gender, string expected, string why)
        {
            Assert.AreEqual(expected, clsPatientText.FormatBirthGender(birthday, gender), why);
        }

        // 대상: clsPatientText.Pair — 우편번호+주소처럼 두 값을 한 칸에 잇는 공통 함수
        // 목적: 같은 모양이 화면마다 복사되면 한쪽만 고쳐지는 날이 온다. 규칙을 한 곳에 두고,
        //       그 규칙이 생년월일·성별과 같다는 것을 시험이 지킨다.
        // 확인: 둘 다 있으면 12345 / 서울시, 오른쪽이 공백뿐이면 12345, 왼쪽이 빈 문자열이면
        //       서울시 — 공백만 있는 값을 없는 값과 같게 다룬다.
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
