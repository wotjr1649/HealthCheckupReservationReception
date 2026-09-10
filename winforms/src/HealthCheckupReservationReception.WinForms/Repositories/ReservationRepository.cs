// 화면 ID: WF-RSV-01 — 신규 예약 (03 §8)
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using HealthCheckupReservationReception.Models;

namespace HealthCheckupReservationReception.Repositories
{
    /// <summary>
    /// WF-RSV-01 이 쓰는 두 SP (05 §9 · §11.1).
    /// Parameter 는 이름·타입·크기를 계약 그대로 명시한다. AddWithValue 를 쓰지 않는다 (킷 §3).
    /// </summary>
    public sealed class ReservationRepository : IReservationRepository
    {
        private readonly string _connectionString;

        public ReservationRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// SP-RSV-01 (05 §9). Result Set 이 여섯이고 **순서가 계약**이다 —
        /// RS0 처리결과 · RS1 예약요약 · RS2 시간대정보 · RS3 검진대상 ·
        /// RS4 국가검사항목 · RS5 추가검사항목.
        ///
        /// 뒤 넷은 변경범위에 따라 0행일 수 있다 (05 §9.11). 0행과 "그 Result Set 이 없다" 는
        /// 다르다 — SP 는 늘 여섯을 낸다.
        /// </summary>
        public ReservationAvailabilityReadDto ReadAvailability(ReservationAvailabilityRequest request)
        {
            var read = new ReservationAvailabilityReadDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.USP_HC_예약가능정보_조회", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@수검자ID", SqlDbType.BigInt).Value = request.PatientId;
                command.Parameters.Add("@업무ID", SqlDbType.BigInt).Value =
                    request.WorkId == null ? (object)DBNull.Value : request.WorkId.Value;

                // 05 §16.3 — DB 에서 읽은 byte[8] 을 문자열로 바꿔 재전송하지 않는다.
                command.Parameters.Add("@행버전", SqlDbType.Binary, 8).Value =
                    (object)request.RowVersion ?? DBNull.Value;
                AddText(command, "@예약구분", SqlDbType.VarChar, 10, request.ReserveType);
                command.Parameters.Add("@예약일", SqlDbType.Date).Value = request.ReserveDate.Date;

                // 날짜만 고른 상태에서는 NULL 로 간다 (05 §9.3) — 그때 SP 가 AM/PM 둘 다 낸다.
                AddText(command, "@시간대코드", SqlDbType.Char, 2, request.SlotCode);
                AddAex(command, request.AexSelected);

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
                        read.Summary = ReadSummary(reader);
                    }

                    if (reader.NextResult())
                    {
                        read.Slots = ReadSlots(reader);
                    }

                    if (reader.NextResult())
                    {
                        read.Target = ReadTarget(reader);
                    }

                    if (reader.NextResult())
                    {
                        read.NexItems = ReadNexRows(reader);
                    }

                    if (reader.NextResult())
                    {
                        read.AexItems = ReadAexRows(reader);
                    }
                }
            }

            return read;
        }

        /// <summary>SP-RSV-02 (05 §11.1). 성공 RS1 은 예약·접수 Write 공통 Schema 다 (05 §11).</summary>
        public WorkSaveReadDto Register(ReservationSaveRequest request)
        {
            var read = new WorkSaveReadDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.USP_HC_예약_등록", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@수검자ID", SqlDbType.BigInt).Value = request.PatientId;
                AddText(command, "@예약구분", SqlDbType.VarChar, 10, request.ReserveType);
                command.Parameters.Add("@예약일", SqlDbType.Date).Value = request.ReserveDate.Date;
                AddText(command, "@시간대코드", SqlDbType.Char, 2, request.SlotCode);
                AddAex(command, request.AexSelected);
                AddText(command, "@조작자명", SqlDbType.NVarChar, 50, request.OperatorName);

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    read.Result = DbResultReader.Read(reader);
                    if (read.Result != null && read.Result.Success && reader.NextResult() && reader.Read())
                    {
                        read.Row = ReadSaveRow(reader);
                    }
                }
            }

            return read;
        }

        private static ReservationSummaryDto ReadSummary(SqlDataReader reader)
        {
            return new ReservationSummaryDto
            {
                ChangeScope = reader.GetString(reader.GetOrdinal("변경범위")),
                PatientId = reader.GetInt64(reader.GetOrdinal("수검자ID")),
                WorkId = Int64OrNull(reader, reader.GetOrdinal("업무ID")),
                ReserveType = reader.GetString(reader.GetOrdinal("예약구분")),
                ReserveDate = reader.GetDateTime(reader.GetOrdinal("예약일")),
                SlotCode = Text(reader, reader.GetOrdinal("시간대코드")),
                DateChanged = BoolOrNull(reader, reader.GetOrdinal("예약일변경여부")),
                SlotChanged = BoolOrNull(reader, reader.GetOrdinal("시간대변경여부")),
                AexChanged = BoolOrNull(reader, reader.GetOrdinal("추가검사변경여부")),
                WorkAllowed = reader.GetBoolean(reader.GetOrdinal("현재업무가능")),
                OtherWorkId = Int64OrNull(reader, reader.GetOrdinal("다른업무ID")),
                CanSave = reader.GetBoolean(reader.GetOrdinal("저장가능")),
                BlockCode = reader.GetInt32(reader.GetOrdinal("차단코드")),
                BlockMessage = reader.GetString(reader.GetOrdinal("차단메시지")),
            };
        }

        // ordinal 을 루프 밖에서 잡는다 — 0행이어도 컬럼 이름 계약이 전건 검증된다.
        private static IList<SlotInfoDto> ReadSlots(SqlDataReader reader)
        {
            var rows = new List<SlotInfoDto>();
            int ordSlotCode = reader.GetOrdinal("시간대코드");
            int ordSlotName = reader.GetOrdinal("시간대명");
            int ordCapacity = reader.GetOrdinal("정원");
            int ordCurrentCount = reader.GetOrdinal("현재인원");
            int ordAppliedCount = reader.GetOrdinal("적용후인원");
            int ordRemainingSeats = reader.GetOrdinal("잔여자리");
            int ordIsOperating = reader.GetOrdinal("운영여부");
            int ordCutoffTime = reader.GetOrdinal("마감시각");
            int ordCutoffPassed = reader.GetOrdinal("마감경과여부");
            int ordSelectable = reader.GetOrdinal("선택가능");
            int ordBlockCode = reader.GetOrdinal("차단코드");
            int ordBlockMessage = reader.GetOrdinal("차단메시지");

            while (reader.Read())
            {
                rows.Add(new SlotInfoDto
                {
                    SlotCode = reader.GetString(ordSlotCode),
                    SlotName = reader.GetString(ordSlotName),
                    Capacity = reader.GetInt32(ordCapacity),
                    CurrentCount = reader.GetInt32(ordCurrentCount),
                    AppliedCount = reader.GetInt32(ordAppliedCount),
                    RemainingSeats = reader.GetInt32(ordRemainingSeats),
                    IsOperating = reader.GetBoolean(ordIsOperating),
                    CutoffTime = reader.IsDBNull(ordCutoffTime)
                        ? (TimeSpan?)null
                        : reader.GetTimeSpan(ordCutoffTime),
                    CutoffPassed = reader.GetBoolean(ordCutoffPassed),
                    Selectable = reader.GetBoolean(ordSelectable),
                    BlockCode = reader.GetInt32(ordBlockCode),
                    BlockMessage = reader.GetString(ordBlockMessage),
                });
            }

            return rows;
        }

        /// <summary>05 §9.8 — 1행 또는 0행이다. 0행이면 아직 판정할 일정이 아니다.</summary>
        private static ExamTargetDto ReadTarget(SqlDataReader reader)
        {
            int ordIsTarget = reader.GetOrdinal("검진대상여부");
            int ordAge = reader.GetOrdinal("나이");
            int ordLastCompletedDate = reader.GetOrdinal("최근완료일자");
            int ordReasonCode = reader.GetOrdinal("사유코드");
            int ordReasonMessage = reader.GetOrdinal("사유메시지");

            if (!reader.Read())
            {
                return null;
            }

            return new ExamTargetDto
            {
                IsTarget = reader.GetBoolean(ordIsTarget),
                Age = reader.GetInt32(ordAge),
                LastCompletedDate = reader.IsDBNull(ordLastCompletedDate)
                    ? (DateTime?)null
                    : reader.GetDateTime(ordLastCompletedDate),
                ReasonCode = reader.GetInt32(ordReasonCode),
                ReasonMessage = reader.GetString(ordReasonMessage),
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

        private static IList<ReservationAexItemDto> ReadAexRows(SqlDataReader reader)
        {
            var rows = new List<ReservationAexItemDto>();
            int ordAexCode = reader.GetOrdinal("추가검사코드");
            int ordExamItemCode = reader.GetOrdinal("검사항목코드");
            int ordExamItemName = reader.GetOrdinal("검사항목명");
            int ordRequested = reader.GetOrdinal("요청선택여부");
            int ordEffective = reader.GetOrdinal("유효선택여부");
            int ordSelectable = reader.GetOrdinal("선택가능");
            int ordReasonCode = reader.GetOrdinal("사유코드");
            int ordReasonMessage = reader.GetOrdinal("사유메시지");

            while (reader.Read())
            {
                rows.Add(new ReservationAexItemDto
                {
                    AexCode = reader.GetString(ordAexCode),
                    ExamItemCode = reader.GetString(ordExamItemCode),
                    ExamItemName = reader.GetString(ordExamItemName),
                    Requested = reader.GetBoolean(ordRequested),
                    EffectiveSelected = reader.GetBoolean(ordEffective),
                    Selectable = reader.GetBoolean(ordSelectable),
                    ReasonCode = reader.GetInt32(ordReasonCode),
                    ReasonMessage = reader.GetString(ordReasonMessage),
                });
            }

            return rows;
        }

        private static WorkSaveResultDto ReadSaveRow(SqlDataReader reader)
        {
            int ordRowVersion = reader.GetOrdinal("행버전");
            var rowVersion = new byte[8];
            reader.GetBytes(ordRowVersion, 0, rowVersion, 0, rowVersion.Length);

            return new WorkSaveResultDto
            {
                WorkId = reader.GetInt64(reader.GetOrdinal("업무ID")),
                StatusCode = reader.GetString(reader.GetOrdinal("상태코드")),
                RowVersion = rowVersion,
            };
        }

        /// <summary>
        /// 05 §9.2 의 `@추가검사01`~`@추가검사07`. 자리 순서가 곧 `OPT01`~`OPT07` 이다.
        ///
        /// [X] 하나라도 NULL 이면 `100` 이다 (05 §9.3). 그래서 짧은 배열이 와도 계약 개수를
        ///     반드시 채운다 — 모자란 자리는 미선택(false)이다.
        /// </summary>
        private static void AddAex(SqlCommand command, bool[] selected)
        {
            for (int i = 0; i < ReservationAvailabilityRequest.AexParameterCount; i++)
            {
                string name = "@추가검사" + (i + 1).ToString("00", CultureInfo.InvariantCulture) + "선택여부";
                bool on = selected != null && i < selected.Length && selected[i];
                command.Parameters.Add(name, SqlDbType.Bit).Value = on;
            }
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

        private static long? Int64OrNull(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (long?)null : reader.GetInt64(ordinal);
        }

        private static bool? BoolOrNull(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? (bool?)null : reader.GetBoolean(ordinal);
        }
    }
}
