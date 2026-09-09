# Designer.cs Reference

`.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 5 defines what `InitializeComponent()` may contain and where `ConfigureUI()` lives. This file covers the mechanics.

## Single writer

`*.Designer.cs` and its `.resx` are one serialized state.

- Re-read both files immediately before every edit; never apply a change from an `InitializeComponent()` you remember.
- After the VS designer saves the form, the saved files are the truth; diff them before editing again.
- Review every diff for designer reserialization unrelated to the intended change.
- Never reformat, reorder, or regenerate a working `InitializeComponent()`; add and change only the lines the task needs.

## Initialization pattern priority

1. The working pattern already in this form.
2. A working form in the same project that uses the same control.
3. The snippet below, which follows DevExpress 20.2 designer output.
4. DevExpress v20.2 documentation.

Preserve existing pairs and relationships: `BeginInit`/`EndInit`, `SuspendLayout`/`ResumeLayout`, `GridControl.MainView` and `ViewCollection`, `RepositoryItems` and `ColumnEdit`, `SearchLookUpEdit` popup view, layout item and control pairs.

## Hand-edit pitfalls

1. **Parser.** The designer parses only what it serializes. A conditional, loop, lambda, string interpolation, or call to a method of yours inside `InitializeComponent()` produces "The designer cannot process the code at line N" and the form stops opening. Move such code to `ConfigureUI()`.
2. **Base class runs at design time.** The designer instantiates the base class of the form, so a base form class constructor or `OnLoad` that touches services or the database breaks design time. Guard with `LicenseManager.UsageMode == LicenseUsageMode.Designtime`; `DesignMode` is false inside constructors.
3. **Dock order.** Docking is applied from the last control in `Controls` to the first, so the `Dock = Fill` control must be added first: `Controls.Add(gcXxx)` before `Controls.Add(pnlTop)`.
4. **Editor `Properties`.** Every `BaseEdit`-derived control (`*Edit`, `RadioGroup`, `ProgressBarControl`) needs `((ISupportInitialize)(x.Properties)).BeginInit()` and `EndInit()`. `DateEdit` also needs the pair for `x.Properties.CalendarTimeProperties`; `SearchLookUpEdit` also on its popup `GridView`. DevExpress containers (`PanelControl`, `GroupControl`, `SplitContainerControl`, `XtraTabControl`), `BarManager`, and `ImageCollection` need the pair on the component itself, and `SplitContainerControl` also on its `Panel1` and `Panel2`. `TablePanel` and `StackPanel` derive from `PanelControl`, so they need it too; the 20.2 XML doc does not list it because an inherited interface implementation has no doc entry.
5. **Repository items.** Add the item to `gcXxx.RepositoryItems.AddRange(...)`, give it its own `BeginInit`/`EndInit` pair, assign it through `colXxx.ColumnEdit`, and set `gvXxx.OptionsView.ShowButtonMode = ShowAlways` when a button must show in every row. A button column needs `gvXxx.OptionsBehavior.Editable` left at its default `true` (the snippet shows the line; the designer serializes only `false`) with `OptionsColumn.AllowEdit = false` on the data columns.
6. **Unbound columns.** DevExpress 20.2 uses `GridColumn.UnboundType`; `UnboundDataType` does not exist in 20.2.
7. **Layout changes.** Widening the form or moving existing controls to fit new ones is reported as a deviation.
8. **Resources.** Check the `.resx` before removing a resource-backed property. Never hand-author base64 blobs such as `ImageCollection` streams; leave image resources to the VS designer.
9. **Scale.** New forms and UserControls use the `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 1 baseline: `AutoScaleMode = Font`, `AutoScaleDimensions = 7F, 12F`, and the form serializes its own `Font` (skeleton below). Do not copy `6F, 13F` or `7F, 15F` from templates or samples. The serialized `Font` is what holds the baseline: `Program.cs` does not run at design time, so without it the first designer save recomputes `AutoScaleDimensions` from whatever font the designer is using and rescales every control. Measured: a form written without it came back `7F, 14F` with `ClientSize` 561 -> 654, exactly 14/12; with it, `7F, 12F` survived the save.
10. **Registration.** A new `FrmXxx.cs` and `FrmXxx.Designer.cs`, and the `FrmXxx.resx` when the designer created one, compile only after the csproj lists them (`SubType Form`, `DependentUpon`). A hand-written empty `.resx` fails the build (MSB3103).
11. **License file.** `licenses.licx` is written by the VS designer; never hand-author entries. The only hand edit is emptying the file when a trial dialog or a design-time license error appears, then opening one form in the VS designer so it is regenerated. In a project that has had no designer session yet, create it empty and register it: an unregistered file embeds no license however it was produced, and the first designer session fills the entries.

For a `LabelControl` in a `TablePanel` cell, when `ConfigureUI()` sets `AutoSizeMode = None`, serialize `Dock = Fill` in the Designer. Otherwise a designer save can retain the smaller design-time text bounds, which clip the runtime font. After saving, check the live label's width and height against an automatically sized label with the same text and font.

## New form skeleton (written without a designer session)

```csharp
// file: Views/FrmStudentSearch.Designer.cs
namespace Hospital.Views
{
    partial class FrmStudentSearch
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            // construction, BeginInit, SuspendLayout, property assignments, Controls.Add, EndInit, ResumeLayout

            // the form's own block, which must carry Font (pitfall 9):
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 561);
            this.Name = "FrmStudentSearch";
            this.Text = "학생 조회";
        }

        #endregion

        // control and component field declarations
        private DevExpress.XtraGrid.GridControl gcStudentList;
        private DevExpress.XtraGrid.Views.Grid.GridView gvStudentList;
        private DevExpress.XtraGrid.Columns.GridColumn colStudentNo;
        private DevExpress.XtraGrid.Columns.GridColumn colDetail;
        private DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit repoBtnDetail;
        private DevExpress.XtraEditors.TextEdit txtStudentNo;
        private DevExpress.XtraEditors.TextEdit txtStudentName;
        private DevExpress.XtraEditors.SimpleButton btnDetail;
        private DevExpress.XtraEditors.LabelControl lblStudentNo;
    }
}
```

## The first designer session

A `Designer.cs` written without a designer session is re-serialized in full the first time the VS designer opens and saves it: declaration order, `BeginInit` order and property order all change. Measured on one form: 269 of 393 lines. This is mechanical and expected, and no ordering you can write by hand avoids it. Open the form once, save, and commit that re-serialization on its own so later diffs stay readable.

That save is also when files appear that nothing has registered yet. After it, check and add to the csproj what the designer created:

- `FrmXxx.resx` — `<EmbeddedResource>` with `<DependentUpon>` (pitfall 10). The designer may register it automatically; inspect the csproj and add the registration if missing. An unregistered resource can go unnoticed by the build until a resource-backed property needs it.
- `Properties/licenses.licx` — `<EmbeddedResource>` (pitfall 11), if it was not registered already.

Then confirm the form still reads `AutoScaleDimensions = 7F, 12F` and that `obj\Debug\<app>.exe.licenses` exists.

## Canonical grid snippet (designer form)

```csharp
// file: Views/FrmStudentSearch.Designer.cs (the statements below go inside InitializeComponent of the file above)
this.gcStudentList = new DevExpress.XtraGrid.GridControl();
this.gvStudentList = new DevExpress.XtraGrid.Views.Grid.GridView();
this.colStudentNo = new DevExpress.XtraGrid.Columns.GridColumn();
this.colDetail = new DevExpress.XtraGrid.Columns.GridColumn();
this.repoBtnDetail = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
((System.ComponentModel.ISupportInitialize)(this.gcStudentList)).BeginInit();
((System.ComponentModel.ISupportInitialize)(this.gvStudentList)).BeginInit();
((System.ComponentModel.ISupportInitialize)(this.repoBtnDetail)).BeginInit();
this.SuspendLayout();
//
// gcStudentList
//
this.gcStudentList.Dock = System.Windows.Forms.DockStyle.Fill;
this.gcStudentList.Location = new System.Drawing.Point(0, 48);
this.gcStudentList.MainView = this.gvStudentList;
this.gcStudentList.Name = "gcStudentList";
this.gcStudentList.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
this.repoBtnDetail});
this.gcStudentList.Size = new System.Drawing.Size(884, 473);
this.gcStudentList.TabIndex = 1;
this.gcStudentList.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
this.gvStudentList});
//
// gvStudentList
//
this.gvStudentList.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
this.colStudentNo,
this.colDetail});
this.gvStudentList.GridControl = this.gcStudentList;
this.gvStudentList.Name = "gvStudentList";
this.gvStudentList.OptionsBehavior.Editable = true;
this.gvStudentList.OptionsView.ShowButtonMode = DevExpress.XtraGrid.Views.Base.ShowButtonModeEnum.ShowAlways;
this.gvStudentList.OptionsView.ShowGroupPanel = false;
//
// colStudentNo
//
this.colStudentNo.Caption = "학번";
this.colStudentNo.FieldName = "StudentNo";
this.colStudentNo.Name = "colStudentNo";
this.colStudentNo.OptionsColumn.AllowEdit = false;
this.colStudentNo.Visible = true;
this.colStudentNo.VisibleIndex = 0;
//
// colDetail
//
this.colDetail.Caption = "상세";
this.colDetail.ColumnEdit = this.repoBtnDetail;
this.colDetail.FieldName = "Detail";
this.colDetail.Name = "colDetail";
this.colDetail.UnboundType = DevExpress.Data.UnboundColumnType.String;
this.colDetail.Visible = true;
this.colDetail.VisibleIndex = 1;
this.colDetail.Width = 60;
//
// repoBtnDetail
//
this.repoBtnDetail.AutoHeight = false;
this.repoBtnDetail.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph)});
this.repoBtnDetail.Name = "repoBtnDetail";
this.repoBtnDetail.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.HideTextEditor;
this.repoBtnDetail.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(this.repoBtnDetail_ButtonClick);
// ... other controls (for example this.txtStudentNo.Properties.MaxLength = 20;), then:
((System.ComponentModel.ISupportInitialize)(this.gcStudentList)).EndInit();
((System.ComponentModel.ISupportInitialize)(this.gvStudentList)).EndInit();
((System.ComponentModel.ISupportInitialize)(this.repoBtnDetail)).EndInit();
this.ResumeLayout(false);
```

The button caption (`repoBtnDetail.Buttons[0].Caption = "상세"`) is set in `ConfigureUI()` because the designer serializes it only through a multi-argument `EditorButton` constructor; widths derived from loaded data are set in the view setter the presenter drives (`.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 5).

## After a Designer change

1. Build.
2. Open the form in the VS2019 designer when a designer session is available: this proves the file parses (pitfall 1), nothing more, because `ConfigureUI()` and `WindowsFormsSettings.DefaultFont` do not run on the design surface. Then run the application or use a screenshot of the build under review.
3. Review the diff for unintended designer churn.
4. Report `Visual Verified` per `.agents/kits/net461-dx20-mvp/PROJECT_INSTRUCTIONS.md` Section 10.
