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
