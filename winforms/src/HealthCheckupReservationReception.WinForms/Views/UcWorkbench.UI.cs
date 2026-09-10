// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Columns;

namespace HealthCheckupReservationReception.Views
{
    public partial class UcWorkbench
    {
        /// <summary>
        /// Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance 와, 인자 둘짜리 생성자로만
        /// 만들어지는 항목과, 데이터에서 만드는 목록 (킷 §5).
        /// </summary>
        partial void ConfigureUI()
        {
            LoadStatusItems();

            // 03 §9.3 은 기본 기간을 정하지 않는다. 접수 창구가 여는 순간 보고 싶은 것은
            // 오늘이므로 양끝을 오늘로 세운다 — 그러면 §9.3 의 `최소 하나의 실질 조건` 도
            // 이미 만족해 화면이 빈 채로 뜨지 않는다. 사용자는 지우거나 늘릴 수 있다.
            deFrom.EditValue = DateTime.Today;
            deTo.EditValue = DateTime.Today;

            colReserveDate.DisplayFormat.FormatType = FormatType.DateTime;
            colReserveDate.DisplayFormat.FormatString = "yyyy-MM-dd";

            clsGridColumns.Align(colReserveDate, HorzAlignment.Center);
            clsGridColumns.Align(colSlot, HorzAlignment.Center);
            clsGridColumns.Align(colStatusName, HorzAlignment.Center);
            clsGridColumns.Align(colName, HorzAlignment.Center);
            clsGridColumns.Align(colChartNo, HorzAlignment.Near);
            clsGridColumns.Align(colGender, HorzAlignment.Center);
            clsGridColumns.Align(colBirthday, HorzAlignment.Center);
            clsGridColumns.Align(colMobilePhone, HorzAlignment.Center);

            clsGridColumns.Align(colNexName, HorzAlignment.Near);
            clsGridColumns.Align(colNexType, HorzAlignment.Center);
            clsGridColumns.Align(colAexName, HorzAlignment.Near);

            // 03 §18 — 컬럼을 숨기는 길은 [컬럼 설정] 드롭다운 하나뿐이다. 헤더를 밖으로 끌어
            // 숨기는 경로를 열어 두면 실수로 사라진 컬럼을 되돌릴 방법을 사용자가 모른다.
            gvWorkList.OptionsCustomization.AllowQuickHideColumns = false;

            // 03 §9.3 Inline 오류. 붉은 글씨는 이 자리가 오류라는 유일한 단서다.
            lblValidation.Appearance.ForeColor = Color.Firebrick;
            lblValidation.Appearance.Options.UseForeColor = true;

            LoadColumnChecks();
        }

        /// <summary>
        /// 03 §9.3 상태 드롭다운. `전체` 만 값이 없고 (null) 나머지 넷은 04 §1.1 의 상태코드다.
        ///
        /// 값과 표시글이 여기 한 곳에만 있다. 상태명을 Grid 에도 적지 않는다 — 목록의 `상태`
        /// 컬럼은 DB 가 준 `상태명` 을 그대로 쓴다 (05 §8.1 RS1 · ROOT AGENTS.md §6).
        /// </summary>
        private void LoadStatusItems()
        {
            cboStatus.Properties.Items.AddRange(new[]
            {
                new ImageComboBoxItem("전체", null),
                new ImageComboBoxItem("예약", "RSV"),
                new ImageComboBoxItem("접수완료", "RCP"),
                new ImageComboBoxItem("예약취소", "CNR"),
                new ImageComboBoxItem("접수취소", "CNC"),
            });

            cboStatus.EditValue = null;
        }

        /// <summary>
        /// 03 §18 — 컬럼 목록은 **Grid 가 가진 것**에서 만든다. 컬럼 이름을 여기 적으면
        /// Designer 와 두 곳이 된다 (ROOT AGENTS.md §6).
        ///
        /// 내부키(업무ID·수검자ID)는 애초에 Grid 의 컬럼이 아니므로 여기에도 없다 (03 §9.4).
        /// </summary>
        private void LoadColumnChecks()
        {
            foreach (GridColumn column in gvWorkList.Columns)
            {
                if (!column.OptionsColumn.ShowInCustomizationForm)
                {
                    continue;
                }

                clbColumns.Items.Add(new CheckedListBoxItem(
                    column,
                    column.Caption,
                    column.Visible ? CheckState.Checked : CheckState.Unchecked,
                    true));
            }
        }
    }
}
