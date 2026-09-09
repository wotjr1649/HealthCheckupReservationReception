# MVP Wiring and Fake-View Test

One screen end to end in the shape `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 2 requires. Copy the shape, not the names. Namespaces follow folders: `<App>.Views`, `<App>.Presenters`, `<App>.Services`, `<App>.Repositories`, `<App>.Models`. All code is C# 7.3.

## Files for one screen

```text
Views/IStudentSearchView.cs            DevExpress-free contract
Views/FrmStudentSearch.cs              XtraForm, implements the contract, creates the presenter
Views/FrmStudentSearch.Designer.cs     designer serialization only
Views/FrmStudentSearch.UI.cs           optional ConfigureUI()
Presenters/StudentSearchPresenter.cs
Services/IStudentService.cs, Services/StudentService.cs                 (.agents/kits/net461-dx20-mvp/contract/service.md)
Repositories/IStudentRepository.cs, Repositories/StudentRepository.cs   (persistence in scope only; .agents/kits/net461-dx20-mvp/contract/repository.md)
Models/StudentSearchRequest.cs, Models/StudentDto.cs, Common/OperationResult.cs   (.agents/kits/net461-dx20-mvp/contract/service.md)
<App>.Tests/Presenters/StudentSearchPresenterTests.cs
```

The models, result type, and service this screen uses are in `.agents/kits/net461-dx20-mvp/contract/service.md`; the repository is in `.agents/kits/net461-dx20-mvp/contract/repository.md`. This file holds the UI side: view contract, form, `UI.cs`, presenter, `Program.cs`, and the fake-view test. A `UcXxx` keeps a parameterless constructor (the designer instantiates it) and receives its service through a method the hosting form calls after `InitializeComponent()`; it shows messages with `FindForm()` as owner and caption.

## View contract

```csharp
// file: Views/IStudentSearchView.cs
using System;
using System.Collections.Generic;
using Hospital.Models;

namespace Hospital.Views
{
    public interface IStudentSearchView
    {
        event EventHandler SearchRequested;
        event EventHandler<string> DetailRequested;

        string StudentNo { get; }
        string StudentName { get; }

        IList<StudentDto> Rows { set; }
        bool DetailEnabled { set; }

        void ShowMessage(string message);
    }
}
```

## Form

```csharp
// file: Views/FrmStudentSearch.cs
using System;
using System.Collections.Generic;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using Hospital.Models;
using Hospital.Presenters;
using Hospital.Services;

namespace Hospital.Views
{
    public partial class FrmStudentSearch : XtraForm, IStudentSearchView
    {
        private readonly StudentSearchPresenter _presenter;

        partial void ConfigureUI();

        public FrmStudentSearch(IStudentService service)
        {
            InitializeComponent();
            ConfigureUI();
            _presenter = new StudentSearchPresenter(this, service);
        }

        public event EventHandler SearchRequested;
        public event EventHandler<string> DetailRequested;

        public string StudentNo { get { return txtStudentNo.Text; } }
        public string StudentName { get { return txtStudentName.Text; } }

        public IList<StudentDto> Rows { set { gcStudentList.DataSource = value; } }
        public bool DetailEnabled { set { btnDetail.Enabled = value; } }

        public void ShowMessage(string message)
        {
            XtraMessageBox.Show(this, message, Text);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            EventHandler handler = SearchRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void btnDetail_Click(object sender, EventArgs e)
        {
            RaiseDetailRequested();
        }

        private void repoBtnDetail_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            RaiseDetailRequested();
        }

        private void RaiseDetailRequested()
        {
            StudentDto row = gvStudentList.GetFocusedRow() as StudentDto;
            EventHandler<string> handler = DetailRequested;
            if (row != null && handler != null)
            {
                handler(this, row.StudentNo);
            }
        }
    }
}
```

`repoBtnDetail_ButtonClick` is the handler the grid snippet in `references/designer.md` wires; both button paths raise the same view event.

## UI.cs (optional)

`partial void ConfigureUI();` in the form makes the call disappear when this file is absent. Create the file only when there is something to configure (`.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 5).

```csharp
// file: Views/FrmStudentSearch.UI.cs
using System.Drawing;

namespace Hospital.Views
{
    public partial class FrmStudentSearch
    {
        partial void ConfigureUI()
        {
            lblStudentNo.Appearance.ForeColor = Color.Red;
            lblStudentNo.Appearance.Options.UseForeColor = true;
            repoBtnDetail.Buttons[0].Caption = "상세";
        }
    }
}
```

## Presenter

```csharp
// file: Presenters/StudentSearchPresenter.cs
using System;
using System.Collections.Generic;
using Hospital.Common;
using Hospital.Models;
using Hospital.Services;
using Hospital.Views;

namespace Hospital.Presenters
{
    public class StudentSearchPresenter
    {
        private readonly IStudentSearchView _view;
        private readonly IStudentService _service;

        public StudentSearchPresenter(IStudentSearchView view, IStudentService service)
        {
            _view = view;
            _service = service;
            _view.SearchRequested += OnSearchRequested;
            _view.DetailRequested += OnDetailRequested;
            _view.DetailEnabled = false;
        }

        private void OnSearchRequested(object sender, EventArgs e)
        {
            var request = new StudentSearchRequest
            {
                StudentNo = _view.StudentNo,
                StudentName = _view.StudentName
            };

            OperationResult<IList<StudentDto>> result;
            try
            {
                result = _service.Search(request);
            }
            catch (Exception)
            {
                // 킷 §6 — 원문 예외 문자열을 화면에 싣지 않는다. provider 메시지는
                // DB 이름·서버 이름을 노출한다. 이 자리는 복사되기 쉬우므로 그대로 둔다.
                _view.ShowMessage("조회 중 오류가 발생했습니다.");
                return;
            }

            if (!result.IsSuccess)
            {
                _view.ShowMessage(result.Message);
                return;
            }

            _view.Rows = result.Value;
            _view.DetailEnabled = result.Value.Count > 0;
        }

        private void OnDetailRequested(object sender, string studentNo)
        {
            // EXTENSION POINT: open the detail screen for studentNo.
        }
    }
}
```

## Program.cs

The connection string is read here once, as `.agents/kits/net461-dx20-mvp/contract/repository.md` specifies, and passed to the repository. When persistence is out of scope, `Main` builds `new StudentService()`, reads no connection string, and drops `using System.Configuration;` and `using Hospital.Repositories;`.

```csharp
// file: Program.cs
using System;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Hospital.Repositories;
using Hospital.Services;
using Hospital.Views;

namespace Hospital
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            WindowsFormsSettings.SetPerMonitorDpiAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            WindowsFormsSettings.DefaultFont = new Font("굴림", 9f);
            WindowsFormsSettings.DefaultMenuFont = new Font("굴림", 9f);

            string connectionString = ConfigurationManager.ConnectionStrings["AppDb"].ConnectionString;
            IStudentService service = new StudentService(new StudentRepository(connectionString));
            Application.Run(new FrmStudentSearch(service));
        }
    }
}
```

## Fake-view test (`<App>.Tests`, MSTest v2)

Project: Visual Studio 2019 template "Unit Test Project (.NET Framework)", target net461, the MSTest packages pinned in `.agents/kits/net461-dx20-mvp/contract/build.md`, a project reference to `<App>`. Tests never touch DevExpress types; the fakes are plain classes.

```csharp
// file: <App>.Tests/Presenters/StudentSearchPresenterTests.cs
using System;
using System.Collections.Generic;
using Hospital.Common;
using Hospital.Models;
using Hospital.Presenters;
using Hospital.Services;
using Hospital.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hospital.Tests.Presenters
{
    [TestClass]
    public class StudentSearchPresenterTests
    {
        [TestMethod]
        public void Search_WithRows_ShowsRowsAndEnablesDetail()
        {
            var view = new FakeStudentSearchView { StudentNo = "S001" };
            var service = new FakeStudentService
            {
                Result = OperationResult<IList<StudentDto>>.Success(new List<StudentDto> { new StudentDto { StudentNo = "S001" } })
            };
            new StudentSearchPresenter(view, service);

            view.RaiseSearchRequested();

            Assert.AreEqual("S001", service.LastRequest.StudentNo);
            Assert.AreEqual(1, view.Rows.Count);
            Assert.IsTrue(view.DetailEnabled);
        }

        [TestMethod]
        public void Search_Failure_ShowsMessage()
        {
            var view = new FakeStudentSearchView();
            var service = new FakeStudentService { Result = OperationResult<IList<StudentDto>>.Failure("실패") };
            new StudentSearchPresenter(view, service);

            view.RaiseSearchRequested();

            Assert.AreEqual("실패", view.LastMessage);
        }
    }

    internal sealed class FakeStudentSearchView : IStudentSearchView
    {
        public event EventHandler SearchRequested;
        public event EventHandler<string> DetailRequested;

        public string StudentNo { get; set; }
        public string StudentName { get; set; }
        public IList<StudentDto> Rows { get; set; }
        public bool DetailEnabled { get; set; }
        public string LastMessage { get; private set; }

        public void ShowMessage(string message)
        {
            LastMessage = message;
        }

        public void RaiseSearchRequested()
        {
            EventHandler handler = SearchRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void RaiseDetailRequested(string studentNo)
        {
            EventHandler<string> handler = DetailRequested;
            if (handler != null)
            {
                handler(this, studentNo);
            }
        }
    }

    internal sealed class FakeStudentService : IStudentService
    {
        public OperationResult<IList<StudentDto>> Result { get; set; }
        public StudentSearchRequest LastRequest { get; private set; }

        public OperationResult<IList<StudentDto>> Search(StudentSearchRequest request)
        {
            LastRequest = request;
            return Result;
        }

        // A fake implements every member of the interface, including the ones this screen
        // does not exercise; leaving one out is a compile error, not a smaller fake.
        public OperationResult<string> Save(StudentSaveRequest request)
        {
            return OperationResult<string>.Success(string.Empty);
        }
    }
}
```

`Flow Verified` is reported per `.agents/kits/net461-dx20-mvp/contract/report.md` after these tests ran through the test command in `.agents/kits/net461-dx20-mvp/contract/build.md`.
