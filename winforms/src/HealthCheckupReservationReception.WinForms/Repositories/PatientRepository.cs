using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    /// <summary>
    /// WF-PAT-01 이 쓰는 두 조회 SP (05 §7.2 · §7.3).
    /// Parameter 는 이름·타입·크기를 계약 그대로 명시한다. AddWithValue 를 쓰지 않는다 (킷 §3).
    /// </summary>
    public sealed class PatientRepository : IPatientRepository
    {
        private readonly string _connectionString;

        public PatientRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public PatientListReadDto Search(PatientSearchRequest request)
        {
            var read = new PatientListReadDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.USP_HC_수검자목록_조회", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                Add(command, "@차트번호", SqlDbType.NVarChar, 100, request.ChartNo);
                Add(command, "@성명", SqlDbType.NVarChar, 100, request.Name);
                Add(command, "@주민번호", SqlDbType.VarChar, 13, request.SocialNumber);
                Add(command, "@생년월일", SqlDbType.VarChar, 8, request.Birthday);
                Add(command, "@휴대전화", SqlDbType.VarChar, 13, request.MobilePhone);

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    read.Result = DbResultReader.Read(reader);
                    if (read.Result != null && read.Result.Success && reader.NextResult())
                    {
                        read.Rows = ReadRows(reader);
                    }
                }
            }

            return read;
        }

        public PatientDetailReadDto ReadDetail(long patientId)
        {
            var read = new PatientDetailReadDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.USP_HC_수검자상세_조회", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@수검자ID", SqlDbType.BigInt).Value = patientId;

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    read.Result = DbResultReader.Read(reader);
                    if (read.Result != null && read.Result.Success && reader.NextResult() && reader.Read())
                    {
                        read.Detail = ReadDetail(reader);
                    }
                }
            }

            return read;
        }

        private static void Add(SqlCommand command, string name, SqlDbType type, int size, string value)
        {
            command.Parameters.Add(name, type, size).Value =
                string.IsNullOrWhiteSpace(value) ? (object)System.DBNull.Value : value;
        }

        private static IList<PatientListItemDto> ReadRows(SqlDataReader reader)
        {
            var rows = new List<PatientListItemDto>();
            int ordPatientId = reader.GetOrdinal("수검자ID");
            int ordChartNo = reader.GetOrdinal("차트번호");
            int ordName = reader.GetOrdinal("성명");
            int ordSocialNumber = reader.GetOrdinal("주민번호");
            int ordBirthday = reader.GetOrdinal("생년월일");
            int ordGender = reader.GetOrdinal("성별");
            int ordMobilePhone = reader.GetOrdinal("휴대전화");
            int ordPhone = reader.GetOrdinal("전화번호");
            int ordEmail = reader.GetOrdinal("이메일");
            int ordZipcode = reader.GetOrdinal("우편번호");
            int ordAddress = reader.GetOrdinal("주소");

            while (reader.Read())
            {
                rows.Add(new PatientListItemDto
                {
                    PatientId = reader.GetInt64(ordPatientId),
                    ChartNo = reader.GetString(ordChartNo),
                    Name = reader.GetString(ordName),
                    SocialNumber = reader.GetString(ordSocialNumber),
                    Birthday = reader.GetString(ordBirthday),
                    Gender = reader.GetString(ordGender),
                    MobilePhone = Text(reader, ordMobilePhone),
                    Phone = Text(reader, ordPhone),
                    Email = Text(reader, ordEmail),
                    Zipcode = Text(reader, ordZipcode),
                    Address = Text(reader, ordAddress),
                });
            }

            return rows;
        }

        private static PatientDetailDto ReadDetail(SqlDataReader reader)
        {
            int ordRowVersion = reader.GetOrdinal("행버전");
            var rowVersion = new byte[8];
            reader.GetBytes(ordRowVersion, 0, rowVersion, 0, rowVersion.Length);

            return new PatientDetailDto
            {
                PatientId = reader.GetInt64(reader.GetOrdinal("수검자ID")),
                ChartNo = reader.GetString(reader.GetOrdinal("차트번호")),
                Name = reader.GetString(reader.GetOrdinal("성명")),
                SocialNumber = reader.GetString(reader.GetOrdinal("주민번호")),
                Birthday = reader.GetString(reader.GetOrdinal("생년월일")),
                Gender = reader.GetString(reader.GetOrdinal("성별")),
                MobilePhone = Text(reader, reader.GetOrdinal("휴대전화")),
                Phone = Text(reader, reader.GetOrdinal("전화번호")),
                Email = Text(reader, reader.GetOrdinal("이메일")),
                Zipcode = Text(reader, reader.GetOrdinal("우편번호")),
                Address = Text(reader, reader.GetOrdinal("주소")),
                AddressDetail = Text(reader, reader.GetOrdinal("상세주소")),
                Memo = Text(reader, reader.GetOrdinal("비고")),
                HepatitisBExcluded = reader.GetBoolean(reader.GetOrdinal("B형간염제외여부")),
                RowVersion = rowVersion,
            };
        }

        private static string Text(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
    }
}
