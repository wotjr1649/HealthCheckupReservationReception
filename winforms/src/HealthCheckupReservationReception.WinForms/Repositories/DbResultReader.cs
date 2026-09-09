using System.Data.SqlClient;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    /// <summary>
    /// RS0 처리결과는 **모든 외부 SP 가 같다** (05 §3.1) — 정확히 1행, 같은 순서·타입이다.
    /// 그래서 컬럼 이름이 리포지토리마다 되풀이되지 않도록 여기 한 벌만 둔다
    /// (ROOT AGENTS.md §6). 컬럼은 이름으로 읽고 순서에 기대지 않는다 (05 §16.2).
    /// </summary>
    internal static class DbResultReader
    {
        public static DbResult Read(SqlDataReader reader)
        {
            if (!reader.Read())
            {
                return null;
            }

            int ordField = reader.GetOrdinal("오류항목");
            return new DbResult
            {
                Success = reader.GetBoolean(reader.GetOrdinal("성공여부")),
                Code = reader.GetInt32(reader.GetOrdinal("결과코드")),
                Message = reader.GetString(reader.GetOrdinal("결과메시지")),
                Field = reader.IsDBNull(ordField) ? null : reader.GetString(ordField),
                ServerTime = reader.GetDateTime(reader.GetOrdinal("서버시각")),
            };
        }
    }
}
