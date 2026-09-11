// 화면 ID: DLG-LOG-01 — 변경이력 열람 (03 §23)
using System;
using System.Collections.Generic;
using DevExpress.XtraEditors;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;

namespace HealthCheckupReservationReception.Views
{
    /// <summary>
    /// DLG-LOG-01 변경이력 열람 Modal (03 §23).
    ///
    /// **진입점이 둘인데 화면은 하나다** (§23.2) — 수검자 관리의 행과 예약/접수 관리의 행이
    /// 같은 창을 연다. 다른 것은 `대상테이블`·`대상키`·제목줄뿐이고, 그 셋을
    /// <see cref="ChangeLogTarget"/> 하나로 받는다.
    ///
    /// **읽기 전용이다** (§23.4). 편집·삭제·재적용·정렬·페이징이 없다 — 있는 Action 은
    /// `[닫기]` 하나다. 그래서 폐기 확인도 없다: 사용자가 들인 것이 없다.
    /// </summary>
    public partial class FrmChangeLog : XtraForm, IChangeLogView
    {
        private readonly ChangeLogPresenter _presenter;
        private readonly ChangeLogTarget _target;

        partial void ConfigureUI();

        /// <summary>
        /// [X] **VS 디자이너 전용이다.** 서비스도 Presenter 도 만들지 않는다 — 디자인 표면에서
        ///     DB 에 닿으면 안 된다 (`references/designer.md` 함정 2).
        /// </summary>
        public FrmChangeLog()
        {
            InitializeComponent();
            ConfigureUI();
        }

        public FrmChangeLog(IChangeLogService service, ChangeLogTarget target) : this()
        {
            _presenter = new ChangeLogPresenter(this, service);
            _target = target;
        }

        /// <summary>
        /// [X] **진입 조회를 생성자에서 돌리지 않는다.** `FrmReservation` 이 그렇게 해서
        ///     아직 뜨지도 않은 폼을 닫아 `ObjectDisposedException` 이 났다 (문서 §4.7).
        ///     여기는 닫을 일이 없지만 같은 규약을 지킨다 — 조회는 창이 뜬 뒤다.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (_presenter == null)
            {
                return;
            }

            using (new clsBusyScope(this))
            {
                _presenter.Begin(_target);
            }
        }

        /// <summary>03 §23.3 제목줄 — 어느 행의 이력인지 창 이름이 말한다.</summary>
        public string Subject
        {
            set { Text = string.IsNullOrWhiteSpace(value) ? "변경이력" : "변경이력 — " + value; }
        }

        public IList<ChangeLogItemDto> Rows
        {
            set { gcLog.DataSource = value; }
        }

        public string ValidationMessage
        {
            set { lblValidation.Text = value ?? string.Empty; }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
