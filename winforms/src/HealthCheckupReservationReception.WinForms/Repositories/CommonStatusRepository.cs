using System.Data;
using System.Data.SqlClient;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
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
                    read.Result = ReadResult(reader);

                    // RS0 이 실패면 후속 Result Set 을 쓰지 않는다 (05 §16.4).
                    if (read.Result != null && read.Result.Success && reader.NextResult())
                    {
                        read.Status = ReadStatus(reader);
                    }
                }
            }

            return read;
        }

        // 컬럼은 이름으로 읽는다. 순서에 기대지 않는다 (05 §16.2).
        private static DbResult ReadResult(SqlDataReader reader)
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
                ServerTime = reader.GetDateTime(reader.GetOrdinal("서버시각"))
            };
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
