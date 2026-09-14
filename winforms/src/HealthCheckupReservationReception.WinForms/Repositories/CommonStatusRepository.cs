// ── 공통 업무상태 리포지토리 ─────────────────────────────────────────────────
// 계약과 구현을 한 파일에 둔다. **SqlClient 는 이 폴더 안에서만 산다** (킷 §2).
//
//   USP_HC_공통업무상태_조회   SP-COM-01   RS0+RS1

using System.Data;
using System.Data.SqlClient;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    public interface ICommonStatusRepository
    {
        CommonWorkStatusReadDto Read();
    }

    /// <summary>
    /// SP-COM-01 `[dbo].[USP_HC_공통업무상태_조회]` (05 §7.1). 입력 Parameter 는 없다.
    /// </summary>
    public sealed class CommonStatusRepository : ICommonStatusRepository
    {
        private readonly string _connectionString;

        public CommonStatusRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public CommonWorkStatusReadDto Read()
        {
            var read = new CommonWorkStatusReadDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.USP_HC_공통업무상태_조회", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    read.Result = DbResultReader.Read(reader);

                    // RS0 이 실패면 후속 Result Set 을 쓰지 않는다 (05 §16.4).
                    if (read.Result != null && read.Result.Success && reader.NextResult())
                    {
                        read.Status = ReadStatus(reader);
                    }
                }
            }

            return read;
        }

        private static CommonWorkStatusDto ReadStatus(SqlDataReader reader)
        {
            if (!reader.Read())
            {
                return null;
            }

            int ordHolidayName = reader.GetOrdinal("휴무일명");
            return new CommonWorkStatusDto
            {
                Today = reader.GetDateTime(reader.GetOrdinal("오늘날짜")),
                DayName = reader.GetString(reader.GetOrdinal("요일명")),
                HolidayName = reader.IsDBNull(ordHolidayName) ? null : reader.GetString(ordHolidayName),
                OpenTime = reader.GetTimeSpan(reader.GetOrdinal("운영시작시각")),
                CloseTime = reader.GetTimeSpan(reader.GetOrdinal("운영종료시각")),
                IsBusinessDay = reader.GetBoolean(reader.GetOrdinal("업무일여부")),
                IsWithinHours = reader.GetBoolean(reader.GetOrdinal("운영시간내여부")),
                IsWorkAllowed = reader.GetBoolean(reader.GetOrdinal("현재업무가능")),
                BlockCode = reader.GetInt32(reader.GetOrdinal("차단코드")),
                BlockMessage = reader.GetString(reader.GetOrdinal("차단메시지"))
            };
        }
    }
}
