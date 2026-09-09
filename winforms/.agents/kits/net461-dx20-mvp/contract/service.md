# Service Layer Reference

Read before writing a service, a DTO, a request type, or a result type. `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 2 holds the rules; this file holds one screen's models and service in the shape Section 2 requires. Copy the shape, not the names. The view, presenter, form, and fake-view test that use these types are in the UI skill's `references/mvp-wiring.md`; the repository is in `repository.md`.

## Models

```csharp
// file: Models/StudentDto.cs
using System;

namespace Hospital.Models
{
    public class StudentDto
    {
        public string StudentNo { get; set; }
        public string StudentName { get; set; }
        public DateTime? BirthDate { get; set; }
    }
}

// file: Models/StudentSearchRequest.cs
namespace Hospital.Models
{
    public class StudentSearchRequest
    {
        public string StudentNo { get; set; }
        public string StudentName { get; set; }
    }
}

// file: Models/StudentSaveRequest.cs
using System;
using System.Collections.Generic;

namespace Hospital.Models
{
    public class StudentSaveRequest
    {
        public string StudentName { get; set; }
        public DateTime? BirthDate { get; set; }
        public IList<StudentCourseDto> Courses { get; set; }
    }
}
```

## Result type

```csharp
// file: Common/OperationResult.cs
namespace Hospital.Common
{
    public class OperationResult
    {
        protected OperationResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message ?? string.Empty;
        }

        public bool IsSuccess { get; private set; }
        public string Message { get; private set; }

        public static OperationResult Success()
        {
            return new OperationResult(true, string.Empty);
        }

        public static OperationResult Failure(string message)
        {
            return new OperationResult(false, message);
        }
    }

    public sealed class OperationResult<T> : OperationResult
    {
        private OperationResult(bool isSuccess, string message, T value)
            : base(isSuccess, message)
        {
            Value = value;
        }

        public T Value { get; private set; }

        public static OperationResult<T> Success(T value)
        {
            return new OperationResult<T>(true, string.Empty, value);
        }

        public new static OperationResult<T> Failure(string message)
        {
            return new OperationResult<T>(false, message, default(T));
        }
    }
}
```

## Service

```csharp
// file: Services/IStudentService.cs
using System.Collections.Generic;
using Hospital.Common;
using Hospital.Models;

namespace Hospital.Services
{
    public interface IStudentService
    {
        OperationResult<IList<StudentDto>> Search(StudentSearchRequest request);

        OperationResult<string> Save(StudentSaveRequest request);
    }
}

// file: Services/StudentService.cs
using System.Collections.Generic;
using Hospital.Common;
using Hospital.Models;
using Hospital.Repositories;

namespace Hospital.Services
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _repository;

        public StudentService(IStudentRepository repository)
        {
            _repository = repository;
        }

        public OperationResult<IList<StudentDto>> Search(StudentSearchRequest request)
        {
            string studentNo = Normalize(request.StudentNo);
            string studentName = Normalize(request.StudentName);
            if (studentNo != null && studentNo.Length > 20)
            {
                return OperationResult<IList<StudentDto>>.Failure("학번은 20자 이하로 입력하세요.");
            }
            if (studentName != null && studentName.Length > 50)
            {
                return OperationResult<IList<StudentDto>>.Failure("학생명은 50자 이하로 입력하세요.");
            }

            return OperationResult<IList<StudentDto>>.Success(_repository.Search(studentNo, studentName));
        }

        public OperationResult<string> Save(StudentSaveRequest request)
        {
            string studentName = Normalize(request.StudentName);
            if (studentName != null && studentName.Length > 50)
            {
                return OperationResult<string>.Failure("학생명은 50자 이하로 입력하세요.");
            }

            StudentSaveResultDto saved = _repository.Insert(studentName, request.BirthDate, request.Courses);
            if (saved.ReturnValue == 1)
            {
                return OperationResult<string>.Failure("같은 이름의 학생이 이미 있습니다.");
            }
            if (saved.LineReturnValue == 1)
            {
                return OperationResult<string>.Failure(saved.FailedLineNo + "번째 과목을 찾을 수 없습니다.");
            }

            return OperationResult<string>.Success(saved.StudentNo);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
```

`Save` shows the shape `repository.md`'s atomic save expects on this side: the repository hands back the numbers, the service turns them into messages, and emptiness is not re-checked here because the presenter owns it (`.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 6).

When persistence is out of scope (`.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 2), the service has no repository constructor parameter and no `using Hospital.Repositories;`, no repository interface exists yet, and `Search` returns `OperationResult<IList<StudentDto>>.Success(new List<StudentDto>())` under an `// EXTENSION POINT` comment. Without a stored-procedure contract there is no length check and no `MaxLength`; a limit the task states goes under `Assumptions`. The interface carries only the members the task needs, so a search-only screen declares no `Save` and nothing has to stub it; a save use case has no out-of-scope form at all, because with no repository there is nothing to save and Section 3 makes that requirement `BLOCKED`.
