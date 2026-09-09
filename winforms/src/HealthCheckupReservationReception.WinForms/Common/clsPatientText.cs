using System;
using System.Linq;

namespace HealthCheckupReservationReception.Common
{
    /// <summary>
    /// 수검자 값의 표기와 정규화. 표기는 03 §5.6 No 7~9 가, 정규화는 05 §2.2 가 정한다.
    ///
    /// 수검자 계열 화면 넷(WF-PAT-01 · DLG-PAT-01 · DLG-PAT-02 · DLG-PAT-03)이 같은 것을
    /// 쓰므로 한 벌만 둔다 — 킷 §2 가 *"같은 모양이 구체 화면 둘에 생기기 전에는"* 이라고
    /// 한 그 조건이 채워졌다.
    /// </summary>
    public static class clsPatientText
    {
        /// <summary>03 §5.6 No 9 — DB 는 `M`/`F` 로 주고 화면은 `남`/`여` 로 적는다 (05 §16.5).</summary>
        public static string FormatGender(string value)
        {
            if (value == "M") { return "남"; }
            if (value == "F") { return "여"; }
            return value;
        }

        /// <summary>03 §5.6 No 8 — `yyyyMMdd` 계산열을 사람이 읽는 꼴로만 끊는다.</summary>
        public static string FormatBirthday(string value)
        {
            return value != null && value.Length == 8
                ? value.Substring(0, 4) + "-" + value.Substring(4, 2) + "-" + value.Substring(6, 2)
                : value;
        }

        /// <summary>03 §5.6 No 7 — 마스킹하지 않는다. 13자리를 6-7 로 끊기만 한다.</summary>
        public static string FormatSocialNumber(string value)
        {
            return value != null && value.Length == 13
                ? value.Substring(0, 6) + "-" + value.Substring(6)
                : value;
        }


        /// <summary>
        /// 전화번호 표기. 숫자만 뽑아 한국 번호대 규칙으로 `-` 를 넣는다.
        ///
        /// [X] **라이브 마스크로는 못 한다.** `0212345678` 이 `02-1234-5678`(서울)인지
        ///     `021-234-5678` 인지는 입력 중에 알 수 없다 — 한국 지역번호는 자릿수가 아니라
        ///     **번호대**로 정해지기 때문이다. 그래서 다 친 뒤에 한 번 정렬한다
        ///     (2026-09-10 사용자 결정).
        ///
        /// [!] **13자를 넘기면 숫자만 돌려준다.** `휴대전화`·`전화번호` 는 `VARCHAR(13)` 이고
        ///     (`04` §8.1.2) `0504-1234-5678` 은 14자다. 하이픈을 고집하면 저장이 거부되므로
        ///     그 자리에서는 하이픈을 포기한다 — 번호를 잃는 것보다 낫다.
        ///
        /// 분류할 수 없는 값은 **숫자 그대로** 돌려준다. 지어내지 않는다.
        /// </summary>
        public static string FormatPhone(string value)
        {
            string d = Digits(value);
            if (d == null)
            {
                return null;
            }

            string formatted = Split(d);
            return formatted != null && formatted.Length <= PhoneStoreMax ? formatted : d;
        }

        /// <summary>`04` §8.1.2 — `휴대전화`·`전화번호` 의 저장 폭.</summary>
        private const int PhoneStoreMax = 13;

        /// <summary>
        /// 전화 자릿수가 쓸 수 있는 범위인가. **하이픈은 세지 않는다** (2026-09-10 사용자 요청).
        ///
        /// 휴대전화 10~11 은 `CK_수검자_CEL_DIGIT` 이 정한 값 그대로다 (`04` §8.1.3) —
        /// 화면이 먼저 잡지 않으면 DB 가 제약 위반으로 튕기고 사용자는 이유를 못 본다.
        /// 전화번호에는 CHECK 이 없으므로(`04` §8.1.3) 실제로 쓰이는 8~11 을 쓴다 —
        /// 대표번호 `1588-1234` 가 8, 지역번호가 9~11 이다.
        ///
        /// 미입력(`null`)은 선택 항목이라 참이다 (`03` §5.6 No 11·12).
        /// </summary>
        public static bool IsPhoneDigitCountValid(string value, bool mobile)
        {
            string d = Digits(value);
            if (d == null) { return true; }
            return mobile ? (d.Length >= 10 && d.Length <= 11)
                          : (d.Length >= 8 && d.Length <= 11);
        }

        /// <summary>
        /// 번호대별 끊는 자리. 앞의 것이 먼저 걸린다.
        ///
        ///   02        서울           02-XXX-XXXX      / 02-XXXX-XXXX
        ///   050X      안심·인터넷    0504-XXXX-XXXX   (12자리)
        ///   0XX       그 밖의 지역·이동통신·070
        ///                            0XX-XXX-XXXX     / 0XX-XXXX-XXXX
        ///   1XXX      대표번호       1588-XXXX        (8자리)
        /// </summary>
        private static string Split(string d)
        {
            if (d.StartsWith("02", StringComparison.Ordinal) && (d.Length == 9 || d.Length == 10))
            {
                return "02-" + d.Substring(2, d.Length - 6) + "-" + d.Substring(d.Length - 4);
            }

            if (d.Length == 12 && d.StartsWith("050", StringComparison.Ordinal))
            {
                return d.Substring(0, 4) + "-" + d.Substring(4, 4) + "-" + d.Substring(8);
            }

            if (d.StartsWith("0", StringComparison.Ordinal) && (d.Length == 10 || d.Length == 11))
            {
                return d.Substring(0, 3) + "-" + d.Substring(3, d.Length - 7) + "-" + d.Substring(d.Length - 4);
            }

            if (d.Length == 8 && d.StartsWith("1", StringComparison.Ordinal))
            {
                return d.Substring(0, 4) + "-" + d.Substring(4);
            }

            return null;
        }

        /// <summary>05 §2.2 — 숫자만 남긴다. 하나도 없으면 미입력이다.</summary>
        public static string Digits(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string digits = new string(value.Where(char.IsDigit).ToArray());
            return digits.Length == 0 ? null : digits;
        }
    }
}
