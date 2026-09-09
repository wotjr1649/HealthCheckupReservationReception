# Completion Report Definitions

Read before writing the `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 10 report. Section 10 holds the template; this file holds the pass condition of each line.

## Requirement statuses

- `PASS` = implemented and evidenced by a file, a test, or a screenshot named in the Evidence column.
- `PARTIAL` = implemented with a stated gap.
- `FAIL` = attempted and not working.
- `BLOCKED` = a missing stored-procedure contract, or one that does not fit the requirement (Section 3), or an unspecified policy (Section 8) stopped it; the report names what is missing.
- `NOT APPLICABLE` = the accepted scope makes the requirement inapplicable.

## Summary lines

- `Code Complete: Yes` = every explicit requirement is `PASS` or validly `NOT APPLICABLE`, and nothing was substituted without approval.
- `Build Verified: Yes` = the build command in `build.md` finished with 0 errors; `Failed` = it ran and failed; `Not Performed` = not run, with the reason.
- `Flow Verified: Yes` = a test in `<App>.Tests` drove event -> Presenter -> Service -> View state through fake `IXxxView`/`IXxxService` and passed under the test command in `build.md`; `Failed` = such a test ran and failed; `Not Performed` = not run, with the reason; `Not Applicable` = the task has no interaction flow. It never implies OS input or visual verification.
- `Visual Verified: Yes` = the running UI, or a screenshot taken from the build under review (same sources, same commit), was inspected, naming the scales inspected (for example 100%, 125%, 150%); `Defects Found` = inspected and problems remain; `Not Performed` = not inspected, with the reason; `Not Applicable` = the change touches no UI file. Build, tests, and opening the form in the VS designer never imply it.
- `Performance Verified: Yes` = the measurements below exist for this change and meet their limits; `Failed (<numbers>)` = they exist and a limit is missed; `Not Applicable` = none of the triggers listed in the UI skill's `references/performance.md` applies (non-UI work: `Not Applicable` unless a trigger other than grid size applies); otherwise `Not Performed`. Every `Not Performed` carries its reason, also where the template omits the placeholder.

## Performance measurements

1. The data size used for the measurement and whether it is representative.
2. A Release build (`build.md`).
3. `Stopwatch` numbers for load, search, and bind of the changed screen.
4. When the change touches form lifecycle: `Process.GetCurrentProcess().PrivateMemorySize64`, read after `GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();`, at 10 and at 20 open-and-close cycles; the second value is less than 5% above the first.
5. The numbers appear in the report next to the requirement they support.
