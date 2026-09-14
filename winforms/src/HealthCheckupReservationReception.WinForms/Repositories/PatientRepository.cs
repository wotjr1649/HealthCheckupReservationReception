// ── 수검자 리포지토리 ────────────────────────────────────────────────────────
// 계약과 구현을 한 파일에 둔다. **SqlClient 는 이 폴더 안에서만 산다** (킷 §2).
//
//   USP_HC_수검자목록_조회       SP-PAT-01   RS0+RS1  목록·상세·유효업무를 한 번에 (R21)
//   USP_HC_수검자_등록           SP-PAT-03   RS0+RS1
//   USP_HC_수검자정보_수정       SP-PAT-04   RS0+RS1

using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    public interface IPatientRepository
    {
        /// <summary>SP-PAT-01 `[dbo].[USP_HC_수검자목록_조회]` (05 §7.2).</summary>
        PatientListReadDto Search(PatientSearchRequest request);

        /// <summary>SP-PAT-03 `[dbo].[USP_HC_수검자_등록]` (05 §10.1).</summary>
        PatientSaveReadDto Register(PatientSaveRequest request);

        /// <summary>SP-PAT-04 `[dbo].[USP_HC_수검자정보_수정]` (05 §10.2).</summary>
        PatientSaveReadDto Update(PatientSaveRequest request);
    }

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

        private static IList<PatientDto> ReadRows(SqlDataReader reader)
        {
            var rows = new List<PatientDto>();
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
            // [R21] 예전 SELECT_수검자상세 가 주던 넷.
            int ordAddressDetail = reader.GetOrdinal("상세주소");
            int ordMemo = reader.GetOrdinal("비고");
            int ordHepatitisB = reader.GetOrdinal("B형간염제외여부");
            int ordRowVersion = reader.GetOrdinal("행버전");
            // [R21] 예전 SELECT_수검자유효업무 가 주던 것. 없으면 NULL 이다.
            int ordValidWorkId = reader.GetOrdinal("유효업무ID");
            int ordValidDate = reader.GetOrdinal("유효예약일");
            int ordValidSlot = reader.GetOrdinal("유효시간대코드");
            int ordValidStatus = reader.GetOrdinal("유효상태코드");

            while (reader.Read())
            {
                rows.Add(new PatientDto
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
                    AddressDetail = Text(reader, ordAddressDetail),
                    Memo = Text(reader, ordMemo),
                    HepatitisBExcluded = reader.GetBoolean(ordHepatitisB),
                    RowVersion = Bytes(reader, ordRowVersion),

                    // 유효업무가 없으면 null 이다 — 그것이 「예약 가능」의 근거다 (00 RP-06).
                    ValidWork = reader.IsDBNull(ordValidWorkId) ? null : new PatientValidWorkDto
                    {
                        WorkId = reader.GetInt64(ordValidWorkId),
                        ReserveDate = reader.GetDateTime(ordValidDate),
                        SlotCode = reader.GetString(ordValidSlot),
                        StatusCode = reader.GetString(ordValidStatus),
                    },
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
