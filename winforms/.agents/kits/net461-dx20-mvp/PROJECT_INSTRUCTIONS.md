# Project Development Contract

**Contract revision: v1.8**

This is the NET461/DX20 WinForms development kit, loaded through the project's existing instructions. Existing applicable project instructions take precedence over this kit; use the kit for requirements the project has not specified. A technical conflict with the actual framework, dependencies, architecture, or DBA stored-procedure contract must be reported before changing the affected code. Do not retarget, replace dependencies, or change the DBA contract to make the project fit this kit. Independent compatible work can continue. Keep changes small, explicit, localized, and buildable.

Paths beginning with `.agents/` resolve from the project root. `references/...` resolves from `.agents/skills/winforms-devexpress-ui/` (the `.claude/skills/winforms-devexpress-ui/` copy is byte-identical). Section numbers refer to this document. Read the following contract documents only at their stated triggers.

Naming source: the company document 「개발 네이밍 및 코딩 규칙」 owns the rules it states, which `.agents/kits/net461-dx20-mvp/contract/naming.md` transcribes; naming.md owns everything else, marking each item as project-owned. When the task provides that document and a transcribed rule differs, the document wins; report that difference instead of following the transcription. Project-owned items are not differences.

## 1. Fixed Environment

| Item | Value |
|---|---|
| Language | C# 7.3, the project-type default. Do not add or change `<LangVersion>`. |
| Framework | .NET Framework 4.6.1. Do not retarget. |
| IDE | Visual Studio 2019 (16.11), 32-bit: keep `PlatformTarget` AnyCPU so its designer loads this project's controls. |
| UI | Windows Forms with DevExpress 20.2.x. The installed 20.2 assemblies are the source of truth; docs.devexpress.com pages are read with `?v=20.2` appended. |
| Architecture | MVP with a Passive View bias (Section 2). |
| Database | SQL Server through DBA-provided stored procedures (Section 3). |
| Solution layout | One WinForms project `<App>` and one test project `<App>.Tests` (MSTest v2, net461). Folders equal namespaces: `Views/`, `Presenters/`, `Services/`, `Repositories/` (only when persistence is in scope), `Models/` (DTO, Request, domain types), `Common/` (`OperationResult` and other types no single screen or entity owns). Layer folders are flat unless the task names a business subfolder; then create that subfolder under every layer alike (`Views/Student/`, `Presenters/Student/`, `Services/Student/`) and let namespaces follow. Its name must not match a type name: a file-top `using` then resolves that name to the namespace (CS0118). An existing solution keeps its layout. |
| UI baseline | 굴림 9pt, set once in `Program.cs` through `WindowsFormsSettings.DefaultFont` and `DefaultMenuFont`. DevExpress default skin unless the task says otherwise. Forms and UserControls use `AutoScaleMode = Font`, `AutoScaleDimensions = 7F, 12F`, and serialize their own `Font` in the Designer: `Program.cs` does not run at design time, so without it the first designer save recomputes the dimensions and rescales the form (`references/designer.md`). `WindowsFormsSettings.SetPerMonitorDpiAware()` is the first statement of `Main`; `app.manifest` carries no DPI setting. Captions and messages are Korean string literals in code; `.resx` localization only when the task asks for it. |
| Files | C# sources are UTF-8 with BOM and CRLF (see `.agents/kits/net461-dx20-mvp/editorconfig.example`; merge applicable settings into the project's existing `.editorconfig`). The csproj is the legacy format: every new C# file gets `<Compile Include>` (`<SubType>Form</SubType>` or `UserControl` on the main file, `<DependentUpon>` on `*.Designer.cs` and `*.UI.cs`, `<SubType>Code</SubType>` on `*.UI.cs`); `*.resx` gets `<EmbeddedResource>` with `<DependentUpon>`. `licenses.licx` and a designer-created `FrmXxx.resx` are toolchain-managed; never hand-author entries. Both need `<EmbeddedResource>` in the csproj; verify registration after the designer saves: `references/designer.md` has the procedure and when it applies. |

Toolchain pitfalls the compiler reports only after a wasted build:

| Area | Rule |
|---|---|
| C# syntax | Not available: switch expressions, `using var`, `??=`, nullable annotations such as `string?`, records, `init`, `required`, ranges and indices, target-typed `new()`, default interface members, file-scoped namespaces, `is not`/`and`/`or` and property patterns, static local functions, `global using`, raw strings, primary constructors, collection expressions `[...]`. |
| Tuples | `(int, string)` needs the System.ValueTuple package on 4.6.1. Use a small DTO instead. |
| BCL | Not available: `DateOnly`, `TimeOnly`, `Math.Clamp`, `HashCode`, `string.Contains(char)`, `string.Contains(string, StringComparison)`, `Dictionary.TryAdd`, `IReadOnlyDictionary` `GetValueOrDefault`, `KeyValuePair` deconstruction, `string.Split(char, options)`, `string.Join(char, ...)`, `StartsWith(char)`, LINQ `DistinctBy`, `Chunk`, `MaxBy`, `TakeLast`, `SkipLast`, `ToHashSet`, `Append`, `Prepend`. |
| Packages | Add a package only when it ships a net45 to net461 build; a netstandard2.0-only package pulls shim assemblies and binding redirects into 4.6.1. Microsoft.Data.SqlClient is out (Section 3 uses System.Data.SqlClient). Newtonsoft.Json is acceptable when JSON is required. |

## 2. Architecture

```text
View (FrmXxx : XtraForm, IXxxView) -> Presenter -> Service (IXxxService) -> Repository (IXxxRepository) -> Stored procedure
```

Create only the layers the task needs. When persistence is out of scope there is no Repository, connection, SQL, or stored-procedure code; the service shape for that case is in `.agents/kits/net461-dx20-mvp/contract/service.md`.

- **View** owns controls, data binding, focus, dialogs, and DevExpress behavior. It raises events, exposes input through properties, and renders the state the presenter sets. It contains no business rules and no SqlClient types. DevExpress types stay inside `Views/` and `Program.cs`; `IXxxView` is DevExpress-free.
- **Presenter** subscribes to view events, builds requests, calls the service, and sets view state: rows, enabled and visible flags, validation messages, selection. UI-state decisions such as "export is enabled only when rows exist" are made here and rendered by the view.
- **Service** owns business validation, state transitions, and use-case orchestration. Expected business failures return `OperationResult` or `OperationResult<T>`; exceptions are for unexpected failures. Before writing a service, DTO, request, or result type, read `.agents/kits/net461-dx20-mvp/contract/service.md`.
- **Repository** owns stored-procedure calls and mapping to typed DTOs, nothing else. `SqlConnection`, `SqlCommand`, and `SqlDataReader` appear only inside `Repositories/`; a Form event handler never calls a repository or the database.
- `IXxxView`, `IXxxService`, and `IXxxRepository` exist because `<App>.Tests` substitutes them. Add no other abstraction (no `GenericRepository<T>`, generic CRUD presenters or forms, universal grid builders) until the same shape exists in two concrete screens.

A new View/Presenter pair and its fake-view test follow `references/mvp-wiring.md`.

## 3. Stored Procedures

Applies only when persistence is in scope.

- The DBA team provides every stored procedure; do not create or alter one. Its contract (name, parameters with `SqlDbType`, size, precision, scale, direction and null handling, result columns, output and return values, error and transaction contract) lives in the task message or in `docs/sp/<schema>.<name>.sql`; match it exactly. A missing or unfit contract makes the affected requirement `BLOCKED`.
- Use `System.Data.SqlClient` with `CommandType.StoredProcedure` and explicitly typed `Parameters.Add`. No `AddWithValue`, no inline `SELECT`/`INSERT`/`UPDATE`/`DELETE` in C#.
- Calls are synchronous by default. Use `async`/`await` only when the task asks for it or a measured call exceeds one second.
- Before writing a repository, read `.agents/kits/net461-dx20-mvp/contract/repository.md`: connection, parameter, null, transaction, and mapping rules with the reference repository.

## 4. Naming

Before creating or renaming any identifier, read `.agents/kits/net461-dx20-mvp/contract/naming.md`: the company table (variables, fields, methods, forms, `cls` utilities, `dt`/`ds`/`bs` data objects), the .NET-default role suffixes, the control prefix tables, the project-owned readings, and the precedence rule.

## 5. Designer Files

- AI may edit `*.Designer.cs`. It holds only what the VS designer serializes: the `components` field, the `Dispose(bool)` override, control field declarations and, inside `InitializeComponent()`, construction, property assignments, `Controls.Add` and `AddRange`, `Columns.AddRange`, `RepositoryItems`, `ColumnEdit`, `+=` with named handlers, `SuspendLayout`/`ResumeLayout`/`PerformLayout`, `BeginInit`/`EndInit`. It contains no business rules and no data access. Everything else belongs in `ConfigureUI()` or the form class; `references/designer.md` lists what breaks the designer.
- Designer vs `ConfigureUI()`: column and repository-item construction, literal captions and widths, `Properties.MaxLength`, and `Options*` values stay in the Designer. Non-literal values known at construction, `DisplayFormat`, appearance, and settings the designer serializes through multi-argument constructors (repository-item button captions, `LookUpColumnInfo`) go in `ConfigureUI()`, implemented in the optional partial `FrmXxx.UI.cs`, declared `partial void ConfigureUI();` in `FrmXxx.cs`, and called right after `InitializeComponent()`. Values derived from loaded data (lookup sources, data-based widths) are set inside the view setter the presenter drives. Each property is set in exactly one place. Omit `UI.cs` when there is nothing to configure.
- Pitfalls, new-form skeleton, canonical grid snippet: `references/designer.md`.

## 6. Validation, Errors, Threads

- Checks answerable from the view's inputs alone (empty, pattern, numeric) are decided by the presenter and rendered by the view; a check that needs data, another field's meaning, or the stored-procedure contract belongs to the service. Length limits from the stored-procedure contract are validated once, in the service; emptiness is validated once, in the presenter, even when a `NOT NULL` parameter is what makes the value required; the editor's `Properties.MaxLength` (Designer) is a UI limit, not validation. Integrity and concurrency live in the database.
- Catch exceptions at the presenter boundary and show a Korean message without raw exception text or the stack trace; provider messages can expose database and machine details. No empty `catch`. No logging framework exists or is added; where a logging path already exists, pass the original exception to it. `async void` is limited to event handlers and carries its own `try`/`catch`: an exception thrown inside one bypasses the caller's `catch` and surfaces as `Application.ThreadException`, so the presenter boundary never sees it.
- Controls are touched only on the UI thread. When a background thread exists, marshal with `Invoke((MethodInvoker)(() => ...))` or `BeginInvoke` (the cast-free `Control.Invoke(Action)` overload and `Control.InvokeAsync` do not exist on 4.6.1), and never call `.Result` or `.Wait()` on the UI thread.

## 7. Reuse and Inheritance

Do not derive forms from a shared visual base form: DevExpress advises against visual inheritance in per-monitor DPI modes. Share cross-screen behavior through helper classes or, once the same shape exists in two concrete screens (Section 2), a base class that declares no controls.

## 8. Requirement Fidelity

- Explicit requirements are mandatory unless the user approves a change; a cleaner or simpler alternative is still a deviation.
- Confirmed requirements include the current task, existing applicable project instructions, and the task-selected text specifications under `docs/`. This kit adds defaults only where those sources do not specify a requirement. Images and wireframes define structure, not policy: required fields, read-only state, permissions, and transactions come from confirmed text, never from the image.
- Implement an ambiguous requirement in its smallest reversible reading and list it under `Assumptions` in the report. When save or delete eligibility, permissions, or persistence behavior is unspecified, report that requirement as `BLOCKED` instead of guessing. `BLOCKED` is for the requirement that is itself unspecified; an unspecified detail that would otherwise leave a confirmed requirement inoperable takes the smallest reversible reading and an `Assumptions` line.
- Baseline robustness (exception handling, `Dispose`, null guards, and the length and required-value checks that follow from the stored-procedure contract's sizes and `NOT NULL` parameters) is always in scope. New screen elements, actions, and validation rules are not; they need approval.

## 9. Workflow

Before editing: read the task, `docs/`, and `.agents/kits/net461-dx20-mvp/contract/build.md`; inspect the screen, presenter, service, and tests the change touches; identify the owning layer and the smallest change set; inspect the stored-procedure contract when persistence is in scope. Use the `winforms-devexpress-ui` skill for any Form, UserControl, Designer, DevExpress, layout, or visual work, and not for service- or repository-only work.

After editing: normalize the files you created, build, and run the tests per `build.md`, then write the Section 10 report. After three attempts that fail the same way without new evidence, stop retrying: report the error, what was tried, and what would unblock it.

## 10. Report

Every completion report uses this template. Read `.agents/kits/net461-dx20-mvp/contract/report.md` first for the definition of each line.

```text
Requirements
| ID | Requirement | Status (PASS / PARTIAL / FAIL / BLOCKED / NOT APPLICABLE) | Evidence |

Assumptions: <each ambiguity and the reading chosen, or "none">

Code Complete       : Yes | No
Build Verified      : Yes | Failed | Not Performed (<reason>)
Flow Verified       : Yes | Failed | Not Performed | Not Applicable
Visual Verified     : Yes (<scales inspected>) | Defects Found (<scales, defects>) | Not Performed | Not Applicable
Performance Verified: Yes | Failed (<numbers>) | Not Performed | Not Applicable
```
