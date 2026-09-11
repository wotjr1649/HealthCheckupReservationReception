// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System.Drawing;
using DevExpress.Utils;
using DevExpress.XtraEditors.Controls;
using HealthCheckupReservationReception.Common;

namespace HealthCheckupReservationReception.Views
{
    public partial class UcHoliday
    {
        /// <summary>
        /// Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance 와, 값이 상수에서 오는 목록 (킷 §5).
        /// </summary>
        partial void ConfigureUI()
        {
            clsGridColumns.Align(colHolidayDate, HorzAlignment.Center);
            clsGridColumns.Align(colHolidayName, HorzAlignment.Near);
            clsGridColumns.Align(colHolidayType, HorzAlignment.Center);
            clsGridColumns.Align(colIsActive, HorzAlignment.Center);
            clsGridColumns.Align(colMemo, HorzAlignment.Near);

            colHolidayDate.DisplayFormat.FormatType = FormatType.DateTime;
            colHolidayDate.DisplayFormat.FormatString = "yyyy-MM-dd";

            // 05 §12.5 `사용여부` 는 BIT 다. `True/False` 대신 03 §24.3 의 표기를 쓴다.
            clsGridColumns.Display(gvHolidayList, colIsActive, IsActiveText);

            // [X] 성공한 0건과 실패가 같은 그림이면 안 된다 (문서 §2.1).
            clsGridColumns.ShowEmptyText(gvHolidayList, "조회 결과가 없습니다. 조회기간을 넓혀 보십시오.");

            clsSearchConditions.SetupBirthday(deFrom);
            clsSearchConditions.SetupBirthday(deTo);
            clsSearchConditions.SetupBirthday(deInputDate);

            _picker = new clsGridRowPicker(gvHolidayList);
            _picker.PickChanged += Picker_PickChanged;

            // 05 §12.5 — NULL 이 `전체` 다. 값은 DbHolidayType 이 갖고 여기서 짓지 않는다.
            cboType.Properties.Items.AddRange(new[]
            {
                new ImageComboBoxItem("전체", null),
                new ImageComboBoxItem(DbHolidayType.Statutory, DbHolidayType.Statutory),
                new ImageComboBoxItem(DbHolidayType.Substitute, DbHolidayType.Substitute),
                new ImageComboBoxItem(DbHolidayType.Own, DbHolidayType.Own),
            });
            cboType.EditValue = null;

            // 03 §24.6 만료 경고는 경고일 뿐 편집을 막지 않는다 — 눈에는 띄어야 한다.
            lblRegistry.Appearance.ForeColor = Color.DarkOrange;
            lblRegistry.Appearance.Options.UseForeColor = true;

            lblBlock.Appearance.ForeColor = Color.Firebrick;
            lblBlock.Appearance.Options.UseForeColor = true;
        }

        /// <summary>03 §24.3 — `Y` / `N`.</summary>
        private static string IsActiveText(string value)
        {
            return "True".Equals(value, System.StringComparison.OrdinalIgnoreCase) ? "Y" : "N";
        }
    }
}
