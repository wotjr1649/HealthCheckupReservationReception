# Performance and Memory

Triggers: a grid may exceed about 1,000 rows; a measured action exceeds one second or the task says it is slow; forms open and close repeatedly; the task mentions 성능 or 메모리. A trigger applies only when the task message, `docs/`, or a measurement states it; an unknown data volume is not a trigger.

## Rules

- Measure before changing anything: `Stopwatch` around the service call and around the bind, on a Release build (`.agents/kits/net461-dx20-mvp/contract/build.md`), with a stated representative data size. An optimization without a number is not accepted.
- One stored-procedure call per user action. Never call the database per grid row or per cell.
- Do not append rows one by one to a bound list while the grid is visible; build the full list first and assign it once (binding rule: `references/devexpress-controls.md`).
- Wrap batched column changes made in a view setter (widths derived from loaded data) in `gvXxx.BeginUpdate()` / `EndUpdate()`, and batched data changes in `BeginDataUpdate()` / `EndDataUpdate()`.
- `BestFitColumns()` at most once after binding; skip it above roughly 10,000 rows.
- `ServerMode` and `InstantFeedback` sources only when a confirmed requirement names data volumes that require them.
- Modal forms are created in `using` or disposed after `ShowDialog`. A presenter that subscribes to a publisher that outlives the form (a service event, a static event, a shared timer) unsubscribes when the view raises its closed event (add one to `IXxxView`; the form raises it from `FormClosed`). Timers owned by the form are stopped and disposed with it.
- Images come from one shared `SvgImageCollection` or `ImageCollection`; never create a `Bitmap` per row.

## Reporting

The measurements `Performance Verified: Yes` requires, and the `Failed` condition, are in `.agents/kits/net461-dx20-mvp/contract/report.md`.
