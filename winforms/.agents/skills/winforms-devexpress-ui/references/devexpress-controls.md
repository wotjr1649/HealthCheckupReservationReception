# DevExpress 20.2 Control Reference

Version rule: the installed 20.2 assembly set decides what exists (`.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 1; confirmation procedure in `SKILL.md` Preflight).

## Control selection

Existing project usage wins. Without one, start here:

| Purpose | Control |
|---|---|
| Form | `XtraForm` |
| UserControl | `XtraUserControl` |
| Single-line text | `TextEdit` |
| Multi-line text | `MemoEdit` |
| Date, time | `DateEdit`, `TimeEdit` |
| Number | `SpinEdit`, `CalcEdit` |
| Simple lookup | `LookUpEdit` |
| Searchable lookup | `SearchLookUpEdit` |
| Fixed choice list | `ComboBoxEdit`, `ImageComboBoxEdit` |
| Command | `SimpleButton` |
| Boolean | `CheckEdit` |
| Option group | `RadioGroup` |
| Data list | `GridControl` + `GridView` |
| Section | `PanelControl`, `GroupControl` |
| Split layout | `SplitContainerControl` |
| Tabs | `XtraTabControl` |
| Label and editor grid | `TablePanel` |
| Button row | `StackPanel` |
| Designer-maintained flexible layout | `LayoutControl` |
| Message | `XtraMessageBox` |

## GridControl / GridView

- Designer versus `ConfigureUI()`: the split is `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 5, applied to `MainView`, `ViewCollection`, `RepositoryItems`, `Columns.AddRange`, `ColumnEdit`, captions, widths, `DisplayFormat`, and repository-item settings alike.
- Binding: the presenter supplies the full `List<T>` or `BindingList<T>`; the view assigns it once to `gcXxx.DataSource` (the `Rows` setter in `references/mvp-wiring.md`).
- Row buttons: `RepositoryItemButtonEdit` in an unbound column (snippet in `references/designer.md`); the `ButtonClick` handler raises a view event carrying the focused row key.
- Large data and repeated rebinding: `references/performance.md`.

## Appearance

- Global appearance (Skin, `DefaultFont`, AutoScale, panel and grid colors) follows `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 1 and the existing screens; a wireframe never changes it.
- A cue drawn on one control in the wireframe, such as a red label caption, is reproduced on that control in `ConfigureUI()`: `lblPatientNo.Appearance.ForeColor = Color.Red; lblPatientNo.Appearance.Options.UseForeColor = true;`. Reproducing the cue does not make the field required.
