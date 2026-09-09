---
name: winforms-devexpress-ui
description: Use when creating or changing WinForms UI on .NET Framework 4.6.1 with DevExpress 20.2 - Forms, UserControls, *.Designer.cs, .resx, wireframes or mockups, GridControl/GridView/RepositoryItem, layout containers, DPI/AutoScale, resize, control naming, visual checks, UI performance. Korean triggers - 화면, 폼, 그리드, 디자이너, 와이어프레임, 레이아웃, 컨트롤 명명. Not for service, domain, DTO, or repository-only work.
---

# WinForms + DevExpress UI Workflow

**Skill revision: v1.8**

Read `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` before applying this skill. Its project-precedence and compatibility rules apply here too. Paths beginning with `.agents/` resolve from the project root; `references/` resolves from this skill directory.

## 1. Preflight

1. Separate confirmed requirement text from wireframe structure.
2. Inspect the target Form or UserControl and one similar working screen when one exists. For a new screen, read `references/mvp-wiring.md` first.
3. A DevExpress member that neither the references nor an existing screen already uses is confirmed before use: grep its name in the XML doc beside the assembly (`C:\Program Files (x86)\DevExpress 20.2\Components\Bin\Framework\DevExpress.<Assembly>.v20.2.xml`) or read the docs page with `?v=20.2`. That XML lists only members declared on the type itself, so an inherited one greps as absent: before concluding a member does not exist, grep the base type too or check the docs page.
4. List the minimal file set: `FrmXxx.cs`, `FrmXxx.Designer.cs`, optional `FrmXxx.UI.cs`, `IXxxView.cs`, `XxxPresenter.cs`, the `IXxxService`/`XxxService` pair and models when none exist (`.agents/kits/net461-dx20-mvp/contract/service.md`), the csproj registration, and the presenter test. `FrmXxx.resx` exists only when the VS designer created it; never hand-write one.

## 2. References

Load only the reference the current subtask needs:

- `.agents/kits/net461-dx20-mvp/contract/naming.md` (path from the repository root) when naming or renaming any control or component.
- `references/designer.md` when editing `*.Designer.cs` or `.resx`: single-writer rules, initialization pattern priority, hand-edit pitfalls, canonical grid snippet.
- `references/wireframe-layout.md` when reading a wireframe or deciding layout, resize, DPI, or the visual checklist.
- `references/devexpress-controls.md` when choosing a DevExpress control or splitting grid configuration between Designer and `UI.cs`.
- `references/mvp-wiring.md` when creating a View/Presenter pair or the fake-view test; `.agents/kits/net461-dx20-mvp/contract/repository.md` for a repository.
- `references/performance.md` when a trigger applies. Triggers: a grid may exceed about 1,000 rows; a measured action exceeds one second or the task says it is slow; forms open and close repeatedly; the task mentions 성능 or 메모리. A trigger applies only when the task message, `docs/`, or a measurement states it; an unknown data volume is not a trigger.

## 3. Implementation Order

1. Resolve behavior from confirmed requirements before interpreting visual details.
2. Reuse the pattern of an existing screen when it fits the task.
3. Make the smallest additive `Designer.cs` change; split Designer and `ConfigureUI()` items per `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 5.
4. Let the presenter decide state and the view render it.
5. Register new files in the csproj, normalize them, build, and run the tests per `.agents/kits/net461-dx20-mvp/contract/build.md`, then inspect the UI when a runtime or a current screenshot is available.

## 4. Completion

Write the `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 10 report.

The `.agents/skills/winforms-devexpress-ui/` copy is canonical; `.claude/skills/winforms-devexpress-ui/` is its generated mirror. Keep project-specific exceptions in the existing project instructions.
