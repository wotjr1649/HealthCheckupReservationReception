// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    /// <summary>
    /// DLG-LOG-01 이 쓰는 유일한 SP (05 §8.3).
    /// Parameter 는 이름·타입·크기를 계약 그대로 명시한다. AddWithValue 를 쓰지 않는다 (킷 §3).
    /// </summary>
    public sealed class ChangeLogRepository : IChangeLogRepository
    {
        private readonly string _connectionString;

        public ChangeLogRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ChangeLogReadDto Read(string targetTable, long targetKey)
        {
            var read = new ChangeLogReadDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.USP_HC_변경이력_조회", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@대상테이블", SqlDbType.NVarChar, 10).Value = targetTable;
                command.Parameters.Add("@대상키", SqlDbType.BigInt).Value = targetKey;

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    read.Result = DbResultReader.Read(reader);
                    if (read.Result == null || !read.Result.Success)
                    {
                        return read;
                    }

                    if (reader.NextResult())
                    {
                        read.Rows = ReadRows(reader);
                    }
                }
            }

            return read;
        }

        /// <summary>
        /// 05 §8.3 RS1 여섯 컬럼. ordinal 을 `Read` 앞에서 잡는다 — 0행이어도 컬럼 이름
        /// 계약이 전건 검증된다.
        /// </summary>
        private static IList<ChangeLogItemDto> ReadRows(SqlDataReader reader)
        {
            var rows = new List<ChangeLogItemDto>();
            int ordLogId = reader.GetOrdinal("이력ID");
            int ordRecordedAt = reader.GetOrdinal("기록일시");
            int ordOperatorName = reader.GetOrdinal("조작자명");
            int ordColumnName = reader.GetOrdinal("컬럼명");
            int ordBeforeValue = reader.GetOrdinal("변경전");
            int ordAfterValue = reader.GetOrdinal("변경후");

            while (reader.Read())
            {
                rows.Add(new ChangeLogItemDto
                {
                    LogId = reader.GetInt64(ordLogId),
                    RecordedAt = reader.GetDateTime(ordRecordedAt),
                    OperatorName = Text(reader, ordOperatorName),
                    ColumnName = reader.GetString(ordColumnName),
                    BeforeValue = Text(reader, ordBeforeValue),
                    AfterValue = Text(reader, ordAfterValue),
                });
            }

            return rows;
        }

        private static string Text(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
    }
}
