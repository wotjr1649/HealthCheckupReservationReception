// 화면 ID: WF-WRK-01 — 예약/접수 공통 Workbench (03 §9)
using System;
using System.Drawing;
using DevExpress.Utils;
using DevExpress.XtraEditors.Controls;
using HealthCheckupReservationReception.Common;

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

            // 03 §9.3 은 기본 기간을 정하지 않는다. 시작일만 오늘로 세우고 종료일은 비운다 —
            // §9.3 의 `From만 있으면 이후` 라서 오늘과 앞으로의 예약이 한꺼번에 보이고,
            // `최소 하나의 실질 조건` 도 이미 만족해 화면이 빈 채로 뜨지 않는다.
            //
            // [X] 양끝을 오늘로 두면 오늘 예약이 없는 날 화면이 통째로 비어 고장처럼 보인다.
            //     당일 마감이 지난 뒤에는 오늘 예약을 새로 만들 수도 없다(304) — 창구가
            //     실제로 보는 것은 앞으로의 예약이다. 사용자는 종료일을 채워 좁힐 수 있다.
            deFrom.EditValue = DateTime.Today;
            deTo.EditValue = null;

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

            // [X] 성공한 0건과 실패가 사용자에게 같은 그림이면 안 된다 — 2026-09-10 실측.
            clsGridColumns.ShowEmptyText(gvWorkList, "조회 결과가 없습니다.");
            // 03 §8.8 — 구분은 기본/조건부로 적는다. WF-RSV-01 과 같은 규칙을 쓴다.
            clsGridColumns.Display(gvNexList, colNexType, clsWorkText.FormatNexType);

            clsGridColumns.ShowEmptyText(gvNexList, "선택한 업무가 없습니다.");
            clsGridColumns.ShowEmptyText(gvAexList, "선택한 업무가 없습니다.");

            // 03 §9.3 Inline 오류. 붉은 글씨가 이 자리가 오류라는 유일한 단서다.
            lblValidation.Appearance.ForeColor = Color.Firebrick;
            lblValidation.Appearance.Options.UseForeColor = true;

            // 부품 셋은 Grid·조회조건 칸이 다 선 **뒤에** 만든다 — 그 시점의 Grid 가
            // `[기본값 복원]` 이 되돌릴 기준이다.
            _picker = new clsGridRowPicker(gvWorkList);
            _picker.PickChanged += Picker_PickChanged;

            _conditions = new clsSearchConditions(clbConditions);
            _conditions.Add("예약/접수일", true, lciDateFrom, deFrom).Also(lciDateTo, deTo);
            _conditions.Add("상태", true, lciStatus, cboStatus);
            _conditions.Add("차트번호", true, lciChartNo, txtChartNo);
            _conditions.Add("이름", true, lciName, txtName);

            _columns = new clsColumnChooser(clbColumns, gvWorkList);
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
    }
}
