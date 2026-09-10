// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    /// <summary>
    /// DLG-HOL-01 이 쓰는 네 SP (05 §12.5~§12.8).
    /// Parameter 는 이름·타입·크기를 계약 그대로 명시한다. AddWithValue 를 쓰지 않는다 (킷 §3).
    /// </summary>
    public sealed class HolidayRepository : IHolidayRepository
    {
        private readonly string _connectionString;

        public HolidayRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// SP-HOL-01. Result Set 이 셋이고 **순서가 계약**이다 —
        /// RS0 처리결과 · RS1 휴무일목록 · RS2 공휴일등재현황.
        /// </summary>
        public HolidayListReadDto Search(HolidaySearchRequest request)
        {
            var read = new HolidayListReadDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.USP_HC_휴무일목록_조회", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                AddDate(command, "@시작일자", request.FromDate);
                AddDate(command, "@종료일자", request.ToDate);

                // 05 §12.5 — NULL 이면 세 구분을 모두 반환한다.
                command.Parameters.Add("@휴무구분", SqlDbType.NVarChar, 10).Value =
                    string.IsNullOrWhiteSpace(request.HolidayType)
                        ? (object)DBNull.Value
                        : request.HolidayType;

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

                    if (reader.NextResult() && reader.Read())
                    {
                        read.Registry = ReadRegistry(reader);
                    }
                }
            }

            return read;
        }

        public HolidaySaveReadDto Register(HolidaySaveRequest request)
        {
            using (var command = new SqlCommand("dbo.USP_HC_자체휴무일_등록"))
            {
                command.Parameters.Add("@휴무일자", SqlDbType.Date).Value = request.HolidayDate.Date;
                AddName(command, request.HolidayName);
                command.Parameters.Add("@사용여부", SqlDbType.Bit).Value = request.IsActive;
                AddMemo(command, request.Memo);
                return Save(command, true);
            }
        }

        public HolidaySaveReadDto Update(HolidaySaveRequest request)
        {
            using (var command = new SqlCommand("dbo.USP_HC_자체휴무일_수정"))
            {
                command.Parameters.Add("@휴무일자", SqlDbType.Date).Value = request.HolidayDate.Date;
                AddRowVersion(command, request.RowVersion);
                AddName(command, request.HolidayName);
                command.Parameters.Add("@사용여부", SqlDbType.Bit).Value = request.IsActive;
                AddMemo(command, request.Memo);
                return Save(command, true);
            }
        }

        public HolidaySaveReadDto Delete(DateTime holidayDate, byte[] rowVersion)
        {
            using (var command = new SqlCommand("dbo.USP_HC_자체휴무일_삭제"))
            {
                command.Parameters.Add("@휴무일자", SqlDbType.Date).Value = holidayDate.Date;
                AddRowVersion(command, rowVersion);
                return Save(command, false);
            }
        }

        /// <summary>
        /// 셋이 RS0 를 같은 모양으로 내고 둘만 RS1 을 더 낸다 (05 §12.6~§12.8).
        /// <paramref name="hasRow"/> 가 그 차이 전부다.
        /// </summary>
        private HolidaySaveReadDto Save(SqlCommand command, bool hasRow)
        {
            var read = new HolidaySaveReadDto();
            using (var connection = new SqlConnection(_connectionString))
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    read.Result = DbResultReader.Read(reader);
                    if (read.Result == null || !read.Result.Success || !hasRow)
                    {
                        return read;
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        read.HolidayDate = reader.GetDateTime(reader.GetOrdinal("휴무일자"));
                        read.RowVersion = (byte[])reader.GetValue(reader.GetOrdinal("행버전"));
                    }
                }
            }

            return read;
        }

        private static IList<HolidayListItemDto> ReadRows(SqlDataReader reader)
        {
            var rows = new List<HolidayListItemDto>();
            int ordHolidayDate = reader.GetOrdinal("휴무일자");
            int ordHolidayName = reader.GetOrdinal("휴무일명");
            int ordHolidayType = reader.GetOrdinal("휴무구분");
            int ordIsActive = reader.GetOrdinal("사용여부");
            int ordMemo = reader.GetOrdinal("비고");
            int ordRowVersion = reader.GetOrdinal("행버전");

            while (reader.Read())
            {
                rows.Add(new HolidayListItemDto
                {
                    HolidayDate = reader.GetDateTime(ordHolidayDate),
                    HolidayName = reader.GetString(ordHolidayName),
                    HolidayType = reader.GetString(ordHolidayType),
                    IsActive = reader.GetBoolean(ordIsActive),
                    Memo = reader.IsDBNull(ordMemo) ? null : reader.GetString(ordMemo),
                    RowVersion = (byte[])reader.GetValue(ordRowVersion),
                });
            }

            return rows;
        }

        private static HolidayRegistryDto ReadRegistry(SqlDataReader reader)
        {
            int ordLast = reader.GetOrdinal("공휴일최종일자");
            int ordRemaining = reader.GetOrdinal("잔여일수");
            int ordThreshold = reader.GetOrdinal("경고임계일수");

            return new HolidayRegistryDto
            {
                // 05 §12.5 — 공휴일이 0건이면 둘 다 NULL 이다.
                LastHolidayDate = reader.IsDBNull(ordLast) ? (DateTime?)null : reader.GetDateTime(ordLast),
                RemainingDays = reader.IsDBNull(ordRemaining) ? (int?)null : reader.GetInt32(ordRemaining),
                WarningThresholdDays = reader.GetInt32(ordThreshold),
            };
        }

        private static void AddDate(SqlCommand command, string name, DateTime? value)
        {
            // DATE 는 시각을 갖지 않는다. DateEdit 이 실어 보낸 시분초를 여기서 떨군다.
            command.Parameters.Add(name, SqlDbType.Date).Value =
                value == null ? (object)DBNull.Value : value.Value.Date;
        }

        private static void AddName(SqlCommand command, string value)
        {
            // 05 §12.6 — 공백은 SP 의 NULLIF 가 NULL 로 만들어 100 으로 흡수한다.
            command.Parameters.Add("@휴무일명", SqlDbType.NVarChar, 100).Value =
                string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value;
        }

        private static void AddMemo(SqlCommand command, string value)
        {
            command.Parameters.Add("@비고", SqlDbType.NVarChar, 500).Value =
                string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value;
        }

        private static void AddRowVersion(SqlCommand command, byte[] value)
        {
            command.Parameters.Add("@행버전", SqlDbType.Binary, 8).Value =
                value == null ? (object)DBNull.Value : value;
        }
    }
}
