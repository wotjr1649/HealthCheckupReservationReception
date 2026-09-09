# Naming

Read before creating or renaming any identifier. The company document 「개발 네이밍 및 코딩 규칙」 owns the first two tables (`.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md`, top note) except the rows marked as defaults. Everything else here — the third table, the readings below, and the rules — is project-owned and never reported as a difference against that document.

## Identifiers (company document)

| Target | Rule | Example |
|---|---|---|
| Local variable, parameter | camelCase | `studentName`, `void Save(int money)` |
| Member field | `_` + camelCase | `_studentName` |
| `DataTable`, `DataRow`, `DataSet`, `BindingSource`, `DataRow[]` | `dt`, `dr`, `ds`, `bs`, `drs` + PascalCase; member fields add `_`, including a designer-placed `BindingSource` (existing code; new code maps to DTOs, `repository.md`) | `dtStudent`, `_dtStudent`, `drsStudent`, `bsStudent`, `_bsStudent` |
| Method | PascalCase | `SearchStudent()` |
| Windows Form, UserControl, Report | `Frm`, `Uc`, `Rpt` + PascalCase | `FrmStudent`, `UcStudent`, `RptStudent` |
| Utility or helper class with no architectural role | `cls` + PascalCase | `clsDateUtil` |
| Presenter, Service, Repository, DTO, Request, Result, domain type, enum, interface | .NET defaults: PascalCase with the role as suffix, `I` prefix for interfaces. These are defaults, not company rules; do not declare them as rules in code or docs. | `StudentSearchPresenter`, `IStudentSearchView`, `StudentService`, `StudentRepository`, `StudentSearchRequest`, `StudentDto` |
| Designer-wired event handler (default, not a company rule) | Visual Studio default | `btnSearch_Click` |
| Test method (default, not a company rule) | `Scenario_Condition_Expected` | `Search_WithRows_ShowsRows` |
| UI control or component | control-kind prefix + PascalCase semantic name (tables below) | `btnSearch`, `gcPatientList`, `repoBtnDetail` |

`dt` is `DataTable` only, never a date editor.

## Control prefixes (company document)

| Control | Prefix | Pattern | Example |
|---|---|---|---|
| Button (`SimpleButton`, `Button`) | `btn` | `btn` + name | `btnSearch` |
| `BarManager`, `Bar`, bar items | `bar` | `bar` + item kind + name | `barBtnCompany`, `barSubFile`, `barStaticStatus` |
| `RepositoryItem*` in-place editors | `repo` | `repo` + editor kind + name | `repoBtnCompany`, `repoLueDept`, `repoChkUse` |
| `GridControl` | `gc` | `gc` + name | `gcPatientList` |
| `GridView` | `gv` | `gv` + name | `gvPatientList` |

`RepositoryItem*` is a DevExpress in-place editor component. It has nothing to do with the Repository layer in `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 2.

## Control prefixes (project defaults)

| Control | Prefix | Example |
|---|---|---|
| `TextEdit` | `txt` | `txtPatientNo` |
| `MemoEdit` | `memo` | `memoRemark` |
| `LabelControl` | `lbl` | `lblPatientNo` |
| `CheckEdit` | `chk` | `chkUseYn` |
| `RadioGroup` | `rg` | `rgGender` |
| `ComboBoxEdit`, `ImageComboBoxEdit` | `cbo` | `cboStatus` |
| `LookUpEdit` | `lue` | `lueDept` |
| `SearchLookUpEdit` | `slue` | `slueDoctor` |
| `DateEdit` | `de` | `deVisitFrom` |
| `TimeEdit` | `te` | `teVisitTime` |
| `SpinEdit`, `CalcEdit` | `spin` | `spinQty` |
| `PictureEdit` | `pic` | `picPhoto` |
| `PanelControl` | `pnl` | `pnlSearch` |
| `GroupControl` | `grp` | `grpPatient` |
| `SplitContainerControl` | `split` | `splitMain` |
| `LayoutControl` | `lc` | `lcMain` |
| `TablePanel` | `tp` | `tpSearch` |
| `StackPanel` | `sp` | `spButtons` |
| `XtraTabControl` | `tab` | `tabDetail` |
| `GridColumn` | `col` | `colPatientNo` |
| `ImageCollection`, `SvgImageCollection` | `img` | `imgToolbar` |
| `Timer` | `tmr` | `tmrRefresh` |

## Project-owned readings

These narrow a company rule where it would otherwise collide with a project default. The project owner has approved them; they are decisions, not transcription errors, so they are never reported as a difference. Anything that differs and is not listed here is.

- "일반 Class → cls" covers utility and helper classes with no architectural role (`clsDateUtil`). A class with an architectural role — presenter, service, repository, DTO, request, result, domain type, enum, interface — takes the .NET-default name from the first table instead, without the `cls` prefix.

## Rules

- Precedence: a tool-generated identifier (`components`, `InitializeComponent`, `Dispose`; never a control or component `Name`) stays as generated; then the tables above; then existing project usage for anything the tables do not cover. Do not rename existing identifiers solely for compliance.
- View and presenter names carry the screen name (`IStudentSearchView`, `StudentSearchPresenter`); service and repository names carry the entity or use case (`IStudentService`, `StudentRepository`).
- A control type missing from every table: follow the nearest existing screen; otherwise choose a task-local descriptive name (for example `hyperlinkHelp`) and do not add it to this file or announce it as a convention.
