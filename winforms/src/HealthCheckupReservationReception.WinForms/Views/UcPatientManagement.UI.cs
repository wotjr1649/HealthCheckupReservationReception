// 화면 ID: WF-PAT-01 — 수검자 관리 (03 §5)
using DevExpress.Utils;
using DevExpress.XtraGrid.Columns;

namespace HealthCheckupReservationReception.Views
{
    public partial class UcPatientManagement
    {
        /// <summary>
        /// Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance 와 DisplayFormat (킷 §5).
        /// </summary>
        partial void ConfigureUI()
        {
            // 설계 wf_pat_01.js 의 K.cols — 차트번호만 좌측 정렬이고 나머지 넷은 가운데다.
            Align(colChartNo, HorzAlignment.Near);
            Align(colName, HorzAlignment.Center);
            Align(colBirthday, HorzAlignment.Center);
            Align(colGender, HorzAlignment.Center);
            Align(colMobilePhone, HorzAlignment.Center);

            // 03 §5.3 은 생년월일을 달력 입력으로 그린다. 조회는 yyyyMMdd 로 넘어간다.
            deBirthday.Properties.DisplayFormat.FormatType = FormatType.DateTime;
            deBirthday.Properties.DisplayFormat.FormatString = "yyyy-MM-dd";
            deBirthday.Properties.EditFormat.FormatType = FormatType.DateTime;
            deBirthday.Properties.EditFormat.FormatString = "yyyy-MM-dd";
            deBirthday.Properties.Mask.UseMaskAsDisplayFormat = true;
        }

        private static void Align(GridColumn column, HorzAlignment alignment)
        {
            column.AppearanceCell.TextOptions.HAlignment = alignment;
            column.AppearanceCell.Options.UseTextOptions = true;
            column.AppearanceHeader.TextOptions.HAlignment = alignment;
            column.AppearanceHeader.Options.UseTextOptions = true;
        }
    }
}
