// 화면 ID: WF-PAT-01 — 수검자 관리 (03 §5)
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Columns;

namespace HealthCheckupReservationReception.Views
{
    public partial class UcPatientManagement
    {
        /// <summary>
        /// Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance 와, 데이터에서 만드는 목록 (킷 §5).
        /// </summary>
        partial void ConfigureUI()
        {
            // 차트번호만 좌측 정렬이고 나머지 넷은 가운데다.
            clsGridColumns.Align(colChartNo, HorzAlignment.Near);
            clsGridColumns.Align(colName, HorzAlignment.Center);
            clsGridColumns.Align(colBirthday, HorzAlignment.Center);
            clsGridColumns.Align(colGender, HorzAlignment.Center);
            clsGridColumns.Align(colMobilePhone, HorzAlignment.Center);

            // 03 §18 — 컬럼을 숨기는 길은 [컬럼 설정] 드롭다운 하나뿐이다. 헤더를 밖으로 끌어
            // 숨기는 경로를 열어 두면 실수로 사라진 컬럼을 되돌릴 방법을 사용자가 모른다.
            gvPatientList.OptionsCustomization.AllowQuickHideColumns = false;

            LoadColumnChecks();
            ApplyConditionVisibility();
        }

        /// <summary>
        /// 03 §18 — 컬럼 목록은 **Grid 가 가진 것**에서 만든다. 컬럼 이름을 여기 적으면
        /// Designer 와 두 곳이 된다 (ROOT AGENTS.md §6).
        ///
        /// 내부키·정규화 컬럼·동시성값은 애초에 Grid 의 컬럼이 아니므로 여기에도 없다.
        /// </summary>
        private void LoadColumnChecks()
        {
            foreach (GridColumn column in gvPatientList.Columns)
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
