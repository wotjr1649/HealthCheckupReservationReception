# Wireframe, Layout, DPI, and Visual Reference

## Source priority

```text
Confirmed requirement text (task message, text files under docs/)
> Explicit wireframe annotations
> Existing project screens
> Minimal general UI convention
```

A wireframe defines structure only; policy comes from confirmed text, and a missing policy is handled as `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 8 states.

Read a wireframe as a 96 DPI, 100% design reference. Unannotated pixel distances are not requirements.

## Layout

- Search rows and label/editor pairs: `TablePanel`, or absolute positions inside a `PanelControl` docked `Top` when the row holds a single line of editors.
- Button rows: `StackPanel`, or right-anchored buttons in a `PanelControl` docked `Bottom`.
- Main content: `GridControl` docked `Fill` (add order: `references/designer.md`, pitfall 3).
- Two resizable regions: `SplitContainerControl`.
- `LayoutControl` only when the VS designer will maintain it; its serialization is not hand-editable in practice.
- `MinimumSize` defaults to the wireframe size; change it only from an inspected screenshot, and list it under `Assumptions`.

## Resize and DPI checks

Check when applicable: overlap or clipping, caption clipping, editor height consistency, grid header readability, popup size, Dock and Anchor behavior at the minimum size, long text.

Verify at the scale the runtime offers, usually 100%; the report names the scales inspected (`.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 10).

## Visual checklist

- Region proportions match the wireframe intent.
- No overlap or clipping at the initial size and at the minimum size.
- Grid columns, order, widths, and formats match the specification.
- Read-only, enabled, and visible states match the confirmed requirements.
- Initial focus and tab order are usable.
- Empty result state is usable.
- Validation messages appear where specified.
- Skin, font, and scale follow `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 1.

Report the result with the `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 10 template.
