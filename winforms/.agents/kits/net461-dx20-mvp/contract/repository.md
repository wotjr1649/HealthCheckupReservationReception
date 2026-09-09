# Stored Procedures and Repositories

Read before writing a repository. `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 3 holds the invariants; this file holds the mechanics and one reference repository in the shape Section 2 requires. Copy the shape, not the names.

## Rules

- Open a new connection per call inside `using`, and dispose commands and readers the same way. Never keep one connection open for the application lifetime.
- The connection string comes from `app.config` `<connectionStrings>` (`<add name="AppDb" connectionString="..." providerName="System.Data.SqlClient" />`) through `ConfigurationManager` (reference `System.Configuration`), read once in `Program.cs` and passed to the repository constructor.
- A connection string, password, or account value never leaves the code and `app.config`: not in a report, a log, a console line, a commit message, or a test fixture. Treat a commit as leaving the machine, because SVN sends it to the server at once. Refer to such a value by its name only.
- Parameters: `Parameters.Add(name, SqlDbType)`, with `size` when the type has one; set `Precision` and `Scale` on the returned `SqlParameter` for decimals.
- Nulls are `DBNull.Value` in both directions: `(object)value ?? DBNull.Value` going in, `reader.IsDBNull(ordinal)` coming out.
- Map results to typed DTOs inside the repository. `DataTable` and `DataRow` appear only inside repository mapping code.
- Several procedures that must be atomic run on one `SqlConnection` and one `SqlTransaction`, assigned to each command's `Transaction`; the repository exposes one method per atomic unit and the service calls it once.
- Database and provider exceptions are technical failures: let them reach the presenter boundary (`.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 6). Decide an expected business outcome from the procedure's `RETURN` and `OUTPUT` values, never by parsing a localized exception message.

## Reference repository

Two files; the DTO and service that use them are in `service.md`, the presenter and test in the UI skill's `references/mvp-wiring.md`.

```csharp
// file: Repositories/IStudentRepository.cs
using System;
using System.Collections.Generic;
using Hospital.Models;

namespace Hospital.Repositories
{
    public interface IStudentRepository
    {
        IList<StudentDto> Search(string studentNo, string studentName);

        StudentSaveResultDto Insert(string studentName, DateTime? birthDate, IList<StudentCourseDto> courses);
    }
}

// file: Repositories/StudentRepository.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Hospital.Models;

namespace Hospital.Repositories
{
    public partial class StudentRepository : IStudentRepository
    {
        private readonly string _connectionString;

        public StudentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IList<StudentDto> Search(string studentNo, string studentName)
        {
            var rows = new List<StudentDto>();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_Student_Search", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@StudentNo", SqlDbType.NVarChar, 20).Value = (object)studentNo ?? DBNull.Value;
                command.Parameters.Add("@StudentName", SqlDbType.NVarChar, 50).Value = (object)studentName ?? DBNull.Value;
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    int ordStudentNo = reader.GetOrdinal("StudentNo");
                    int ordStudentName = reader.GetOrdinal("StudentName");
                    int ordBirthDate = reader.GetOrdinal("BirthDate");
                    while (reader.Read())
                    {
                        rows.Add(new StudentDto
                        {
                            StudentNo = reader.GetString(ordStudentNo),
                            StudentName = reader.GetString(ordStudentName),
                            BirthDate = reader.IsDBNull(ordBirthDate) ? (DateTime?)null : reader.GetDateTime(ordBirthDate)
                        });
                    }
                }
            }

            return rows;
        }
    }
}
```

## Reference atomic save

One use case, two procedures, one transaction. The contract for it, as a task message would give it:

```text
dbo.usp_Student_Insert        @StudentName nvarchar(50) NOT NULL, @BirthDate date NULL,
                              @StudentNo nvarchar(20) OUTPUT
                              return 0 success / 1 duplicate name; no result set
dbo.usp_StudentCourse_Insert  @StudentNo nvarchar(20) NOT NULL, @LineNo int NOT NULL,
                              @CourseCode nvarchar(20) NOT NULL, @Credit decimal(4,1) NOT NULL
                              return 0 success / 1 unknown course; no result set
Both: the caller opens the transaction; neither procedure does BEGIN or COMMIT.
```

The repository exposes one method for the whole unit and reports the outcome as a DTO. It is one class in two files: `partial` keeps the atomic save beside the search without either block reprinting the other. Turning a return value into a message is business judgement, so it belongs to the service (`.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 2); the repository only carries the numbers out.

```csharp
// file: Models/StudentCourseDto.cs
namespace Hospital.Models
{
    public class StudentCourseDto
    {
        public string CourseCode { get; set; }
        public decimal Credit { get; set; }
    }
}

// file: Models/StudentSaveResultDto.cs
namespace Hospital.Models
{
    public class StudentSaveResultDto
    {
        public int ReturnValue { get; set; }
        public int LineReturnValue { get; set; }
        public int FailedLineNo { get; set; }
        public string StudentNo { get; set; }
    }
}

// file: Repositories/StudentRepository.Save.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Hospital.Models;

namespace Hospital.Repositories
{
    public partial class StudentRepository
    {
        public StudentSaveResultDto Insert(string studentName, DateTime? birthDate, IList<StudentCourseDto> courses)
        {
            var result = new StudentSaveResultDto();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    if (!InsertStudent(connection, transaction, studentName, birthDate, result) ||
                        !InsertCourses(connection, transaction, result.StudentNo, courses, result))
                    {
                        transaction.Rollback();
                        return result;
                    }

                    transaction.Commit();
                }
            }

            return result;
        }

        private static bool InsertStudent(SqlConnection connection, SqlTransaction transaction, string studentName, DateTime? birthDate, StudentSaveResultDto result)
        {
            using (var command = new SqlCommand("dbo.usp_Student_Insert", connection, transaction))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@StudentName", SqlDbType.NVarChar, 50).Value = (object)studentName ?? DBNull.Value;
                command.Parameters.Add("@BirthDate", SqlDbType.Date).Value = (object)birthDate ?? DBNull.Value;
                SqlParameter studentNoParameter = command.Parameters.Add("@StudentNo", SqlDbType.NVarChar, 20);
                studentNoParameter.Direction = ParameterDirection.Output;
                SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                returnParameter.Direction = ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();

                result.ReturnValue = (int)returnParameter.Value;
                if (result.ReturnValue != 0)
                {
                    return false;
                }

                result.StudentNo = studentNoParameter.Value as string;
                return true;
            }
        }

        private static bool InsertCourses(SqlConnection connection, SqlTransaction transaction, string studentNo, IList<StudentCourseDto> courses, StudentSaveResultDto result)
        {
            for (int i = 0; i < courses.Count; i++)
            {
                using (var command = new SqlCommand("dbo.usp_StudentCourse_Insert", connection, transaction))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add("@StudentNo", SqlDbType.NVarChar, 20).Value = studentNo;
                    command.Parameters.Add("@LineNo", SqlDbType.Int).Value = i + 1;
                    command.Parameters.Add("@CourseCode", SqlDbType.NVarChar, 20).Value = (object)courses[i].CourseCode ?? DBNull.Value;
                    SqlParameter creditParameter = command.Parameters.Add("@Credit", SqlDbType.Decimal);
                    creditParameter.Precision = 4;
                    creditParameter.Scale = 1;
                    creditParameter.Value = courses[i].Credit;
                    SqlParameter returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                    returnParameter.Direction = ParameterDirection.ReturnValue;

                    command.ExecuteNonQuery();

                    result.LineReturnValue = (int)returnParameter.Value;
                    if (result.LineReturnValue != 0)
                    {
                        result.FailedLineNo = i + 1;
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
```

The service maps the numbers to `OperationResult`: `ReturnValue == 1` is the duplicate-name message, `LineReturnValue == 1` names `FailedLineNo`, and success carries `StudentNo` in `OperationResult<string>` when the screen shows it.
