using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HealthCheckupReservationReception.Common;
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

        /// <summary>
        /// SP-PAT-03 (05 §10.1). 생년월일·성별은 Parameter 가 아니다 — 계산열이 유도한다.
        /// </summary>
        public PatientSaveReadDto Register(PatientSaveRequest request)
        {
            var read = new PatientSaveReadDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.USP_HC_수검자_등록", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@차트번호자동발급여부", SqlDbType.Bit).Value = request.AutoChartNo;
                Add(command, "@차트번호", SqlDbType.NVarChar, 100, request.ChartNo);
                Add(command, "@성명", SqlDbType.NVarChar, 100, request.Name);
                Add(command, "@주민번호", SqlDbType.VarChar, 13, request.SocialNumber);
                Add(command, "@휴대전화", SqlDbType.VarChar, 13, request.MobilePhone);
                Add(command, "@전화번호", SqlDbType.VarChar, 13, request.Phone);
                Add(command, "@이메일", SqlDbType.VarChar, 200, request.Email);
                Add(command, "@우편번호", SqlDbType.VarChar, 10, request.Zipcode);
                Add(command, "@주소", SqlDbType.NVarChar, 200, request.Address);
                Add(command, "@상세주소", SqlDbType.NVarChar, 200, request.AddressDetail);

                // NVARCHAR(MAX) 는 size -1 이다. 100 같은 임의값을 적으면 거기서 잘린다.
                Add(command, "@비고", SqlDbType.NVarChar, -1, request.Memo);
                command.Parameters.Add("@B형간염제외여부", SqlDbType.Bit).Value = request.HepatitisBExcluded;
                command.Parameters.Add("@유사수검자확인여부", SqlDbType.Bit).Value = request.SimilarConfirmed;
                Add(command, "@조작자명", SqlDbType.NVarChar, 50, request.OperatorName);

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    read.Result = DbResultReader.Read(reader);
                    if (HasRegisterRows(read.Result) && reader.NextResult())
                    {
                        read.Rows = ReadRegisterRows(reader);
                    }
                }
            }

            return read;
        }

        /// <summary>SP-PAT-04 (05 §10.2). RS1 은 세 컬럼뿐이다.</summary>
        public PatientSaveReadDto Update(PatientSaveRequest request)
        {
            var read = new PatientSaveReadDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.USP_HC_수검자정보_수정", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@수검자ID", SqlDbType.BigInt).Value = request.PatientId;

                // 05 §16.3 — DB 에서 읽은 byte[8] 을 문자열로 바꿔 재전송하지 않는다.
                command.Parameters.Add("@행버전", SqlDbType.Binary, 8).Value =
                    (object)request.RowVersion ?? System.DBNull.Value;
                Add(command, "@차트번호", SqlDbType.NVarChar, 100, request.ChartNo);
                Add(command, "@성명", SqlDbType.NVarChar, 100, request.Name);
                Add(command, "@주민번호", SqlDbType.VarChar, 13, request.SocialNumber);
                Add(command, "@휴대전화", SqlDbType.VarChar, 13, request.MobilePhone);
                Add(command, "@전화번호", SqlDbType.VarChar, 13, request.Phone);
                Add(command, "@이메일", SqlDbType.VarChar, 200, request.Email);
                Add(command, "@우편번호", SqlDbType.VarChar, 10, request.Zipcode);
                Add(command, "@주소", SqlDbType.NVarChar, 200, request.Address);
                Add(command, "@상세주소", SqlDbType.NVarChar, 200, request.AddressDetail);
                Add(command, "@비고", SqlDbType.NVarChar, -1, request.Memo);
                command.Parameters.Add("@B형간염제외여부", SqlDbType.Bit).Value = request.HepatitisBExcluded;
                Add(command, "@조작자명", SqlDbType.NVarChar, 50, request.OperatorName);

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    read.Result = DbResultReader.Read(reader);
                    if (read.Result != null && read.Result.Success && reader.NextResult())
                    {
                        read.Rows = ReadUpdateRows(reader);
                    }
                }
            }

            return read;
        }

        /// <summary>
        /// 05 §3.5 — 기본은 실패면 RS0 뿐이고, 수검자 등록의 `202`·`203` 만 실패와 함께
        /// RS1 을 준다. 성공여부만 보면 중복후보 목록을 통째로 놓친다.
        /// </summary>
        private static bool HasRegisterRows(DbResult result)
        {
            if (result == null)
            {
                return false;
            }

            return result.Success
                || result.Code == (int)DbCode.SameNumberDifferentName
                || result.Code == (int)DbCode.SimilarPatient;
        }

        // ordinal 을 루프 밖에서 잡는다 — 0행이어도 컬럼 이름 계약이 전건 검증된다.
        private static IList<PatientSaveResultDto> ReadRegisterRows(SqlDataReader reader)
        {
            var rows = new List<PatientSaveResultDto>();
            int ordPatientId = reader.GetOrdinal("수검자ID");
            int ordChartNo = reader.GetOrdinal("차트번호");
            int ordName = reader.GetOrdinal("성명");
            int ordSocialNumber = reader.GetOrdinal("주민번호");
            int ordBirthday = reader.GetOrdinal("생년월일");
            int ordGender = reader.GetOrdinal("성별");
            int ordMobilePhone = reader.GetOrdinal("휴대전화");
            int ordRowVersion = reader.GetOrdinal("행버전");

            while (reader.Read())
            {
                rows.Add(new PatientSaveResultDto
                {
                    PatientId = reader.GetInt64(ordPatientId),
                    ChartNo = reader.GetString(ordChartNo),
                    Name = reader.GetString(ordName),
                    SocialNumber = reader.GetString(ordSocialNumber),
                    Birthday = reader.GetString(ordBirthday),
                    Gender = reader.GetString(ordGender),
                    MobilePhone = Text(reader, ordMobilePhone),
                    RowVersion = Bytes(reader, ordRowVersion),
                });
            }

            return rows;
        }

        private static IList<PatientSaveResultDto> ReadUpdateRows(SqlDataReader reader)
        {
            var rows = new List<PatientSaveResultDto>();
            int ordPatientId = reader.GetOrdinal("수검자ID");
            int ordChartNo = reader.GetOrdinal("차트번호");
            int ordRowVersion = reader.GetOrdinal("행버전");

            while (reader.Read())
            {
                rows.Add(new PatientSaveResultDto
                {
                    PatientId = reader.GetInt64(ordPatientId),
                    ChartNo = reader.GetString(ordChartNo),
                    RowVersion = Bytes(reader, ordRowVersion),
                });
            }

            return rows;
        }

        /// <summary>
        /// SP-PAT-05 (05 §7.4). RS1 은 0행 또는 1행이다 — 0행이 "유효업무 없음" 이고 정상이다.
        /// </summary>
        public PatientValidWorkReadDto ReadValidWork(long patientId)
        {
            var read = new PatientValidWorkReadDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.USP_HC_수검자유효업무_조회", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@수검자ID", SqlDbType.BigInt).Value = patientId;

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    read.Result = DbResultReader.Read(reader);
                    if (read.Result != null && read.Result.Success && reader.NextResult())
                    {
                        read.Work = ReadValidWorkRow(reader);
                    }
                }
            }

            return read;
        }

        private static PatientValidWorkDto ReadValidWorkRow(SqlDataReader reader)
        {
            // ordinal 을 Read 앞에서 잡는다 — 0행이어도 컬럼 이름 계약이 전건 검증된다.
            int ordWorkId = reader.GetOrdinal("업무ID");
            int ordReserveDate = reader.GetOrdinal("예약일");
            int ordSlotCode = reader.GetOrdinal("시간대코드");
            int ordStatusCode = reader.GetOrdinal("상태코드");
            int ordIsToday = reader.GetOrdinal("오늘여부");
            int ordRowVersion = reader.GetOrdinal("행버전");

            if (!reader.Read())
            {
                return null;
            }

            var rowVersion = new byte[8];
            reader.GetBytes(ordRowVersion, 0, rowVersion, 0, rowVersion.Length);

            return new PatientValidWorkDto
            {
                WorkId = reader.GetInt64(ordWorkId),
                ReserveDate = reader.GetDateTime(ordReserveDate),
                SlotCode = reader.GetString(ordSlotCode),
                StatusCode = reader.GetString(ordStatusCode),
                IsToday = reader.GetBoolean(ordIsToday),
                RowVersion = rowVersion,
            };
        }

        private static byte[] Bytes(SqlDataReader reader, int ordinal)
        {
            var value = new byte[8];
            reader.GetBytes(ordinal, 0, value, 0, value.Length);
            return value;
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
