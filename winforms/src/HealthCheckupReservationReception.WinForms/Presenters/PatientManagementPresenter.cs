// 화면 ID: WF-PAT-01 — 수검자 관리 (03 §5)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;

namespace HealthCheckupReservationReception.Presenters
{
    /// <summary>
    /// WF-PAT-01 수검자 관리 (03 §5).
    /// 비어 있음 판정은 여기가 한다 — 길이·형식은 Service 다 (킷 §6).
    /// </summary>
    public sealed class PatientManagementPresenter
    {
        // 목록 컬럼은 두 값뿐이다 — 훑는 자리라 좁고 색으로 읽힌다.
        private const string Open = "가능";
        private const string Blocked = "불가";

        private readonly IPatientManagementView _view;
        private readonly IPatientService _service;
        private readonly IWorkService _workService;

        /// <summary>
        /// 마지막 조회가 받은 행. **[R21] 행을 고를 때 SP 를 부르지 않는 근거다** — 목록 한 행이
        /// 곧 상세이고(SP-PAT-01 RS1), 예약 가능 여부도 그 안의 `ValidWork` 가 말한다.
        /// 예전에는 상태 문자열 두 벌을 따로 들고 있었는데 같은 값의 사본이었다.
        /// </summary>
        private readonly Dictionary<long, PatientDto> _rows = new Dictionary<long, PatientDto>();

        public PatientManagementPresenter(
            IPatientManagementView view,
            IPatientService service,
            IWorkService workService)
        {
            _view = view;
            _service = service;
            _workService = workService;

            _view.SearchRequested += OnSearchRequested;
            _view.SelectionChanged += OnSelectionChanged;

            _view.RowActions = PatientActionState.None();
        }

        private void OnSearchRequested(object sender, EventArgs e)
        {
            Search(false);
        }

        /// <summary>
        /// [R16] 03 §5.3 — 화면을 열 때 조건 없이 한 번 조회해 목록을 채운다.
        ///
        /// [X] **실패를 알리지 않는다.** 사용자가 부탁한 호출이 아니므로 창을 열자마자
        ///     오류창이 뜨면 안 된다. 실제로 초판이 그렇게 해서 UI 시험이 모달에 걸려
        ///     멈췄다(실측 2026-09-10). 빈 목록으로 두면 사용자가 `[조회]` 를 눌러
        ///     같은 경로를 다시 타고, 그때는 이유를 본다.
        /// </summary>
        public void LoadInitial()
        {
            Search(true);
        }

        /// <summary>
        /// 목록을 되읽고 그 수검자로 돌아간다. 예약이 하나 생기면 `예약` 칸이 낡는데,
        /// 화면이 방금 예약한 사람을 계속 `가능` 으로 보이면 그것은 거짓말이다.
        ///
        /// [R23] **수검자 등록·수정 뒤에도 부른다** (2026-09-14 사용자 지시). 낡는 칸이
        /// 예약이든 이름이든 답은 같다 — 쓰기가 끝나면 한 번 다시 읽고 그 줄로 돌아간다.
        /// 그러지 않으면 방금 고친 이름이 목록에 옛 값으로 남고, 그 행으로 `[정보수정]` 을
        /// 다시 누르면 낡은 `행버전` 이 올라가 `601` 이 난다.
        /// </summary>
        public void Reload(long patientId)
        {
            Search(true);
            _view.SelectPatient(patientId);
        }

        private void Search(bool silent)
        {
            var request = new PatientSearchRequest
            {
                ChartNo = _view.ChartNo,
                Name = _view.Name,
                SocialNumber = _view.SocialNumber,
                Birthday = _view.Birthday,
                MobilePhone = _view.MobilePhone,
            };

            // 다섯 다 보낸다. 꺼 둔 조건은 화면이 값을 비우므로 (clsSearchConditions) 여기서는
            // 무엇이 켜졌는지 따지지 않는다 — 끈 칸에 남은 글자가 조회에 섞이지 않는 이유다.

            // [R16] 03 §5.3 — 조건이 하나도 없으면 **전체 목록**이다. 막지 않는다.
            //       SP 도 103 을 내지 않는다 (05 §7.2 · §13).

            OperationResult<IList<PatientDto>> result;
            try
            {
                result = _service.Search(request);
            }
            catch (Exception)
            {
                // 예외 본문을 화면에 싣지 않는다 (킷 §6).
                if (!silent) { _view.ShowMessage("수검자를 조회하지 못했습니다."); }
                return;
            }

            if (result == null || !result.IsSuccess)
            {
                if (!silent) { _view.ShowMessage(result == null ? "수검자를 조회하지 못했습니다." : result.Message); }
                return;
            }

            IList<PatientDto> rows = Annotate(result.Value);

            // 여섯째 조회조건은 SP 가 모르므로 화면이 거른다 (IPatientManagementView.ReservableOnly).
            if (_view.ReservableOnly)
            {
                var open = new List<PatientDto>();
                foreach (PatientDto row in rows)
                {
                    if (!Blocked.Equals(row.ReserveStatus, StringComparison.Ordinal))
                    {
                        open.Add(row);
                    }
                }

                rows = open;
            }

            // 03 §5.3 · §5.5 — 재조회 시 선택행과 우측 상세를 초기화한다.
            _view.Rows = rows;
            _view.Detail = null;
            _view.ReserveStatusText = string.Empty;
            _view.NoticeText = string.Empty;
            _view.RowActions = PatientActionState.None();
        }

        /// <summary>
        /// 목록에 `예약 가능/불가` 문구를 붙인다.
        ///
        /// **[R21] 판정은 SP 가 한다** (2026-09-14 사용자 지시). 예전에는 이 화면이
        /// `SP-WRK-01` 을 두 번(오늘 RSV · 오늘 RCP) 불러 `예약일 >= 오늘` 을 **다시 재서**
        /// 이어 붙였다 — RP-06 판정이 화면으로 새어 나온 자리였다. 이제 목록 SP 의
        /// `유효업무*` 네 칸이 그 답을 싣고 오므로, 여기 남은 일은 **문구 만들기**뿐이다.
        ///
        /// 문구가 화면 몫인 이유는 그것이 업무 규칙이 아니라 표시이기 때문이다 (03 §5.5).
        /// </summary>
        private IList<PatientDto> Annotate(IList<PatientDto> rows)
        {
            _rows.Clear();
            if (rows == null)
            {
                return new List<PatientDto>();
            }

            foreach (PatientDto row in rows)
            {
                if (row.ValidWork == null)
                {
                    row.ReserveStatus = Open;
                    row.ReserveStatusDetail = "예약 가능";
                }
                else
                {
                    row.ReserveStatus = Blocked;
                    row.ReserveStatusDetail = "예약 불가 — " + Schedule(row.ValidWork)
                        + " " + clsWorkText.FormatStatus(row.ValidWork.StatusCode);
                }

                _rows[row.PatientId] = row;
            }

            return rows;
        }
        private static string Schedule(PatientValidWorkDto work)
        {
            return clsWorkText.FormatDate(work.ReserveDate) + " " + clsWorkText.FormatSlot(work.SlotCode);
        }

        /// <summary>
        /// 03 §5.5 — 행을 고르면 오른쪽 상세가 선다.
        ///
        /// **[R21] 여기서 SP 를 부르지 않는다** (2026-09-14 사용자 지시). 예전에는
        /// `SP-PAT-02` 로 상세를 다시 읽었는데, 목록 SP 가 이미 그 칸들을 실어 주므로
        /// 고른 행이 곧 상세다. 남은 조회는 `예약·접수 이력` 하나뿐이고 그것은 범위가
        /// 다르다 — 그 사람의 전 기간이다 (SP-WRK-01).
        ///
        /// [X] **모르는 것은 불가가 아니다** — 행을 못 찾으면 `[예약]` 을 닫지 않고 비운다.
        /// </summary>
        private void OnSelectionChanged(object sender, long? patientId)
        {
            PatientDto row;
            if (patientId == null || !_rows.TryGetValue(patientId.Value, out row))
            {
                _view.Detail = null;
                _view.ReserveStatusText = string.Empty;
                _view.History = null;
                _view.RowActions = PatientActionState.None();
                return;
            }

            // 03 §5.2 — 행이 잡혔다. `[예약]` 만 축이 하나 더 있다: 그 사람이 예약 가능한가.
            // 그 판정은 SP 가 준 `ValidWork` 하나로 끝난다 — 있으면 불가다 (00 RP-06).
            _view.RowActions = new PatientActionState
            {
                RowSelected = true,
                Reserve = row.ValidWork == null,
            };

            _view.ReserveStatusText = row.ReserveStatusDetail ?? string.Empty;
            _view.Detail = row;
            _view.History = LoadHistory(row.ChartNo);
        }
        /// <summary>
        /// 03 §5.5 상세의 `예약·접수 이력` (2026-09-11 사용자 지시).
        ///
        /// `SP-WRK-01` 을 **차트번호 정확검색**으로 부른다 (05 §8.1 `@차트번호 정확검색`,
        /// `04` §8.1.5 `UQ_수검자_CHART_NO`) — 날짜도 상태도 걸지 않으므로 그 사람의 전 업무가
        /// 온다. 한 사람치라 양이 갇힌다.
        ///
        /// [X] **`@수검자ID` 로 부를 수 없다.** `SP-WRK-01` 의 Parameter 다섯에 그것이 없고
        ///     (05 §8.1) 계약은 동결이다. 차트번호가 고유하므로 결과는 같다.
        ///
        /// [X] 목록 조회에서 이미 읽어 둔 `_reserveDetail` 로는 못 만든다. 그쪽은 RP-06 이
        ///     보는 둘(`RSV` 전체 · 오늘 `RCP`)만 담고 있어 지난 접수도 취소도 빠져 있다.
        ///
        /// 실패하면 **비운다.** 이력이 없는 것과 못 읽은 것을 같은 그림으로 보이지 않으려면
        /// 빈 Grid 가 말하는 「이력이 없습니다」가 거짓이면 안 되는데, 여기서 구별해 적을 자리가
        /// 없다 — 상세 조회는 이미 성공했으므로 모달을 띄우면 행을 고를 때마다 창이 뜬다.
        /// 그래서 비우고, 사용자가 목록을 다시 조회하면 같은 길을 다시 탄다.
        /// </summary>
        private IList<WorkListItemDto> LoadHistory(string chartNo)
        {
            if (string.IsNullOrWhiteSpace(chartNo))
            {
                return null;
            }

            OperationResult<IList<WorkListItemDto>> result;
            try
            {
                result = _workService.Search(new WorkSearchRequest { ChartNo = chartNo });
            }
            catch (Exception)
            {
                return null;
            }

            return result != null && result.IsSuccess ? result.Value : null;
        }
    }
}
