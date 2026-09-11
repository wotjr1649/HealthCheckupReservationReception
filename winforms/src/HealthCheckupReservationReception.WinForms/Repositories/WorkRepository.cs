// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    /// <summary>
    /// WF-WRK-01 이 쓰는 두 조회 SP (05 §8.1 · §8.2).
    /// Parameter 는 이름·타입·크기를 계약 그대로 명시한다. AddWithValue 를 쓰지 않는다 (킷 §3).
    /// </summary>
    public sealed class WorkRepository : IWorkRepository
    {
        private readonly string _connectionString;

        public WorkRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public WorkListReadDto Search(WorkSearchRequest request)
        {
            var read = new WorkListReadDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.USP_HC_예약접수목록_조회", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                AddDate(command, "@시작일", request.FromDate);
                AddDate(command, "@종료일", request.ToDate);

                // 05 §8.1 — 상태코드는 CHAR(3) 이고 NULL 이 `전체` 다. 조회조건으로 세지 않는다.
                AddText(command, "@상태코드", SqlDbType.Char, 3, request.StatusCode);
                AddText(command, "@차트번호", SqlDbType.NVarChar, 100, request.ChartNo);
                AddText(command, "@성명", SqlDbType.NVarChar, 100, request.Name);

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    read.Result = DbResultReader.Read(reader);
                    if (read.Result != null && read.Result.Success && reader.NextResult())
                    {
                        read.Rows = ReadListRows(reader);
                    }
                }
            }

            return read;
        }

        /// <summary>
        /// SP-WRK-02 (05 §8.2). Result Set 이 다섯이고 **순서가 계약**이다 —
        /// RS0 처리결과 · RS1 업무상세 · RS2 국가검사항목 · RS3 추가검사항목 · RS4 가능한업무.
        /// </summary>
        public WorkDetailReadDto ReadDetail(long workId)
        {
            var read = new WorkDetailReadDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.USP_HC_예약접수상세_조회", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@업무ID", SqlDbType.BigInt).Value = workId;

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    read.Result = DbResultReader.Read(reader);
                    if (read.Result == null || !read.Result.Success)
                    {
                        return read;
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        read.Detail = ReadDetailRow(reader);
                    }

                    if (reader.NextResult())
                    {
                        read.NexItems = ReadNexRows(reader);
                    }

                    if (reader.NextResult())
                    {
                        read.AexItems = ReadAexRows(reader);
                    }

                    if (reader.NextResult())
                    {
                        read.Actions = ReadActionRows(reader);
                    }
                }
            }

            return read;
        }

        // ordinal 을 루프 밖에서 잡는다 — 0행이어도 컬럼 이름 계약이 전건 검증된다.
        private static IList<WorkListItemDto> ReadListRows(SqlDataReader reader)
        {
            var rows = new List<WorkListItemDto>();
            int ordWorkId = reader.GetOrdinal("업무ID");
            int ordPatientId = reader.GetOrdinal("수검자ID");
            int ordReserveDate = reader.GetOrdinal("예약일");
            int ordSlotCode = reader.GetOrdinal("시간대코드");
            int ordStatusCode = reader.GetOrdinal("상태코드");
            int ordName = reader.GetOrdinal("성명");
            int ordChartNo = reader.GetOrdinal("차트번호");
            int ordGender = reader.GetOrdinal("성별");
            int ordBirthday = reader.GetOrdinal("생년월일");
            int ordMobilePhone = reader.GetOrdinal("휴대전화");

            while (reader.Read())
            {
                rows.Add(new WorkListItemDto
                {
                    WorkId = reader.GetInt64(ordWorkId),
                    PatientId = reader.GetInt64(ordPatientId),
                    ReserveDate = reader.GetDateTime(ordReserveDate),
                    SlotCode = reader.GetString(ordSlotCode),
                    StatusCode = reader.GetString(ordStatusCode),
                    Name = reader.GetString(ordName),
                    ChartNo = reader.GetString(ordChartNo),
                    Gender = reader.GetString(ordGender),
                    Birthday = reader.GetString(ordBirthday),
                    MobilePhone = Text(reader, ordMobilePhone),
                });
            }

            return rows;
        }

        private static WorkDetailDto ReadDetailRow(SqlDataReader reader)
        {
            int ordRowVersion = reader.GetOrdinal("행버전");
            var rowVersion = new byte[8];
            reader.GetBytes(ordRowVersion, 0, rowVersion, 0, rowVersion.Length);

            return new WorkDetailDto
            {
                WorkId = reader.GetInt64(reader.GetOrdinal("업무ID")),
                PatientId = reader.GetInt64(reader.GetOrdinal("수검자ID")),
                ChartNo = reader.GetString(reader.GetOrdinal("차트번호")),
                Name = reader.GetString(reader.GetOrdinal("성명")),
                Birthday = reader.GetString(reader.GetOrdinal("생년월일")),
                Gender = reader.GetString(reader.GetOrdinal("성별")),
                MobilePhone = Text(reader, reader.GetOrdinal("휴대전화")),
                ReserveDate = reader.GetDateTime(reader.GetOrdinal("예약일")),
                SlotCode = reader.GetString(reader.GetOrdinal("시간대코드")),
                StatusCode = reader.GetString(reader.GetOrdinal("상태코드")),
                Capacity = reader.GetInt32(reader.GetOrdinal("정원")),
                CurrentCount = reader.GetInt32(reader.GetOrdinal("현재인원")),
                RemainingSeats = reader.GetInt32(reader.GetOrdinal("잔여자리")),
                RowVersion = rowVersion,
            };
        }

        private static IList<WorkExamItemDto> ReadNexRows(SqlDataReader reader)
        {
            var rows = new List<WorkExamItemDto>();
            int ordExamItemCode = reader.GetOrdinal("검사항목코드");
            int ordExamItemName = reader.GetOrdinal("검사항목명");
            int ordNexType = reader.GetOrdinal("국가검사구분");
            int ordNexRuleCode = reader.GetOrdinal("국가검사규칙코드");

            while (reader.Read())
            {
                rows.Add(new WorkExamItemDto
                {
                    ExamItemCode = reader.GetString(ordExamItemCode),
                    ExamItemName = reader.GetString(ordExamItemName),
                    NexType = reader.GetString(ordNexType),
                    NexRuleCode = reader.GetString(ordNexRuleCode),
                });
            }

            return rows;
        }

        private static IList<WorkExamItemDto> ReadAexRows(SqlDataReader reader)
        {
            var rows = new List<WorkExamItemDto>();
            int ordAexCode = reader.GetOrdinal("추가검사코드");
            int ordExamItemCode = reader.GetOrdinal("검사항목코드");
            int ordExamItemName = reader.GetOrdinal("검사항목명");

            while (reader.Read())
            {
                rows.Add(new WorkExamItemDto
                {
                    AexCode = reader.GetString(ordAexCode),
                    ExamItemCode = reader.GetString(ordExamItemCode),
                    ExamItemName = reader.GetString(ordExamItemName),
                });
            }

            return rows;
        }

        private static IList<WorkActionDto> ReadActionRows(SqlDataReader reader)
        {
            var rows = new List<WorkActionDto>();
            int ordActionCode = reader.GetOrdinal("업무동작코드");
            int ordAllowed = reader.GetOrdinal("허용여부");
            int ordReasonCode = reader.GetOrdinal("사유코드");
            int ordReasonMessage = reader.GetOrdinal("사유메시지");

            while (reader.Read())
            {
                rows.Add(new WorkActionDto
                {
                    ActionCode = reader.GetString(ordActionCode),
                    Allowed = reader.GetBoolean(ordAllowed),
                    ReasonCode = reader.GetInt32(ordReasonCode),
                    ReasonMessage = reader.GetString(ordReasonMessage),
                });
            }

            return rows;
        }

        private static void AddDate(SqlCommand command, string name, DateTime? value)
        {
            // DATE 는 시각을 갖지 않는다. DateEdit 이 실어 보낸 시분초를 여기서 떨군다.
            command.Parameters.Add(name, SqlDbType.Date).Value =
                value == null ? (object)DBNull.Value : value.Value.Date;
        }

        private static void AddText(SqlCommand command, string name, SqlDbType type, int size, string value)
        {
            command.Parameters.Add(name, type, size).Value =
                string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value;
        }

        private static string Text(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
    }
}
