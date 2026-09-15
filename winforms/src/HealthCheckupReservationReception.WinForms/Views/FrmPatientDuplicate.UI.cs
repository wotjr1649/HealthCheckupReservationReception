// 화면 ID: DLG-PAT-03 — 중복 후보 확인 (03 §6.5)

namespace HealthCheckupReservationReception.Views
{
    public partial class FrmPatientDuplicate
    {
        /// <summary>
        /// Designer 가 직렬화하지 않는 것만 여기 있다 — Appearance (킷 §5).
        /// </summary>
        partial void ConfigureUI()
        {
            // 버튼 아이콘 (2026-09-15 사용자 요청). 뜻이 같으면 Ribbon 과 같은 그림을 쓴다.
            clsGlyph.Apply(btnModify, "Edit");
            clsGlyph.Apply(btnContinue, "PatientNew");
            clsGlyph.Apply(btnClose, "Close");

            // 설계 dlg_pat_03.js 의 K.cols — 차트번호만 좌측 정렬이고 나머지 다섯은 가운데다.
            clsGridColumns.Left(colChartNo);
            clsGridColumns.Center(colName, colBirthday, colGender, colSocialNumber, colMobilePhone);
        }
    }
}
