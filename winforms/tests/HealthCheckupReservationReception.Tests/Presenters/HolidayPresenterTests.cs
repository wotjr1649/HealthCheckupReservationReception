// 화면 ID: DLG-HOL-01 — 휴무일 관리 (03 §24)
using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Presenters
{
    [TestClass]
    public class HolidayPresenterTests
    {
        private static readonly DateTime Today = new DateTime(2026, 9, 11);

        // ── 조회 (05 §12.5)

        // 05 §12.5 — 두 날짜는 필수다. 비어 있음 판정은 Presenter 가 한다 (킷 §6).
        [TestMethod]
        public void 조회기간이_비면_SP_를_부르지_않는다()
        {
            var view = new FakeHolidayView();
            var service = new FakeHolidayService { SearchResult = Ok(Rows()) };
            var presenter = new HolidayPresenter(view, service, Status());

            presenter.LoadInitial();

            Assert.IsNull(service.LastSearch, "조건이 없는데 SP 를 불렀다");
            StringAssert.Contains(view.BlockMessage, "조회기간");
        }

        [TestMethod]
        public void 시작일이_종료일보다_늦으면_SP_를_부르지_않는다()
        {
            var view = Ready();
            view.FromDate = Today.AddDays(3);
            view.ToDate = Today;
            var service = new FakeHolidayService { SearchResult = Ok(Rows()) };
            var presenter = new HolidayPresenter(view, service, Status());

            presenter.LoadInitial();

            Assert.IsNull(service.LastSearch);
        }

        // 05 §12.5 — RS2 는 항상 1행이다. 없으면 계약 위반이므로 성공으로 보지 않는다.
        [TestMethod]
        public void 목록과_등재현황이_함께_온다()
        {
            var view = Ready();
            var service = new FakeHolidayService { SearchResult = Ok(Rows()) };
            var presenter = new HolidayPresenter(view, service, Status());

            presenter.LoadInitial();

            Assert.AreEqual(2, view.Rows.Count);
            Assert.IsNull(service.LastSearch.HolidayType, "구분 미선택은 세 구분 전부다");
        }

        /// <summary>
        /// 03 §24.6 — **임계 숫자를 화면이 갖지 않는다.** DB 가 `잔여일수` 와 `경고임계일수` 를
        /// 함께 주고 화면은 둘을 비교만 한다. 여기에 `180` 을 적으면 같은 값이 세 곳에 생긴다.
        /// </summary>
        [TestMethod]
        public void 잔여일수가_임계보다_적을_때만_경고한다()
        {
            var view = Ready();
            var service = new FakeHolidayService
            {
                SearchResult = Ok(Rows(), new HolidayRegistryDto
                {
                    LastHolidayDate = new DateTime(2027, 12, 27),
                    RemainingDays = 200,
                    WarningThresholdDays = 180,
                }),
            };
            new HolidayPresenter(view, service, Status()).LoadInitial();
            Assert.AreEqual(string.Empty, view.RegistryWarning, "임계 이상인데 경고했다");

            service.SearchResult = Ok(Rows(), new HolidayRegistryDto
            {
                LastHolidayDate = new DateTime(2027, 12, 27),
                RemainingDays = 120,
                WarningThresholdDays = 180,
            });
            view.RaiseSearchRequested();

            StringAssert.Contains(view.RegistryWarning, "120");
        }

        [TestMethod]
        public void 공휴일이_하나도_없으면_그렇다고_적는다()
        {
            var view = Ready();
            var service = new FakeHolidayService
            {
                SearchResult = Ok(Rows(), new HolidayRegistryDto { WarningThresholdDays = 180 }),
            };

            new HolidayPresenter(view, service, Status()).LoadInitial();

            StringAssert.Contains(view.RegistryWarning, "등재되어 있지 않");
        }

        // ── 선택 (03 §24.4)

        /// <summary>
        /// 03 §24.4 — **법정·대체 행은 선택해도 입력행에 싣지 않는다.** 보여 주는 이유는 그 날짜가
        /// 왜 업무 불가인지 확인하는 것과, 같은 날짜에 자체휴무일을 넣으려는 시도를 막는 것이다.
        /// </summary>
        [TestMethod]
        public void 법정공휴일_행을_고르면_수정_삭제가_닫힌다()
        {
            var view = Ready();
            var presenter = new HolidayPresenter(view, new FakeHolidayService(), Status());

            view.RaiseSelectionChanged(Statutory());

            Assert.IsFalse(view.RowActionsEnabled);
            Assert.AreEqual(string.Empty, view.InputName, "법정공휴일이 입력행에 실렸다");
        }

        [TestMethod]
        public void 자체휴무일_행을_고르면_입력행에_실린다()
        {
            var view = Ready();
            new HolidayPresenter(view, new FakeHolidayService(), Status());

            view.RaiseSelectionChanged(Own());

            Assert.IsTrue(view.RowActionsEnabled);
            Assert.AreEqual("센터 휴진일", view.InputName);
            Assert.AreEqual(new DateTime(2026, 12, 26), view.InputDate);
        }

        // ── 저장 (05 §12.6~§12.8)

        [TestMethod]
        public void 휴무일명이_비면_SP_를_부르지_않는다()
        {
            var view = Ready();
            var service = new FakeHolidayService();
            var presenter = new HolidayPresenter(view, service, Status());
            view.InputDate = new DateTime(2026, 12, 26);
            view.InputName = "   ";

            presenter.Register();

            Assert.IsNull(service.LastSave);
            StringAssert.Contains(view.BlockMessage, "휴무일명");
        }

        /// <summary>
        /// 05 §12.7 — `휴무일자` 는 PK 이고 바꾸지 않는다. 입력칸의 날짜가 무엇이든 **잡아 둔 행의
        /// 날짜**로 보낸다 — 날짜를 옮기려면 삭제 후 등록이다 (03 §24.5).
        /// </summary>
        [TestMethod]
        public void 수정은_잡아_둔_행의_날짜와_행버전으로_보낸다()
        {
            var view = Ready();
            var service = new FakeHolidayService { SearchResult = Ok(Rows()), SaveResult = Saved() };
            var presenter = new HolidayPresenter(view, service, Status());
            view.RaiseSelectionChanged(Own());

            view.InputDate = new DateTime(2030, 1, 1);   // 사용자가 칸을 건드렸다
            view.InputName = "고친 이름";
            presenter.Update();

            Assert.AreEqual(new DateTime(2026, 12, 26), service.LastSave.HolidayDate);
            CollectionAssert.AreEqual(new byte[] { 9 }, service.LastSave.RowVersion);
        }

        [TestMethod]
        public void 자체휴무일_행이_없으면_수정도_삭제도_아무_일도_하지_않는다()
        {
            var view = Ready();
            var service = new FakeHolidayService();
            var presenter = new HolidayPresenter(view, service, Status());
            view.RaiseSelectionChanged(Statutory());

            presenter.Update();
            presenter.Delete();

            Assert.IsNull(service.LastSave);
            Assert.IsFalse(view.Confirmed, "법정공휴일인데 삭제를 물었다");
        }

        // 03 §24.5 — 물리 삭제라 되돌릴 수 없다. 한 번 묻고, 아니라면 부르지 않는다.
        [TestMethod]
        public void 삭제는_한_번_묻고_거절하면_부르지_않는다()
        {
            var view = Ready();
            view.ConfirmAnswer = false;
            var service = new FakeHolidayService();
            var presenter = new HolidayPresenter(view, service, Status());
            view.RaiseSelectionChanged(Own());

            presenter.Delete();

            Assert.IsTrue(view.Confirmed);
            Assert.IsNull(service.LastDeleteDate);
        }

        /// <summary>
        /// [X] 사유를 먼저 적고 재조회하면 재조회가 그 칸을 지운다 — WF-RSV-01 에서 실측한
        ///     자리다. **재조회를 먼저 하고 사유를 마지막에 적는다.**
        ///
        /// `801`(중복)·`802`(법정공휴일)·`601`(충돌)은 전부 목록이 낡았다는 뜻이므로 실패해도
        /// 다시 읽는다 — 다른 창구가 그 사이 무엇을 했는지 사용자가 보아야 한다.
        /// </summary>
        [TestMethod]
        public void 저장이_DB_에서_막히면_사유를_보이고_목록을_다시_읽는다()
        {
            var view = Ready();
            var service = new FakeHolidayService
            {
                SearchResult = Ok(Rows()),
                SaveResult = OperationResult<HolidaySaveReadDto>.Success(new HolidaySaveReadDto
                {
                    Result = new DbResult
                    {
                        Success = false,
                        Code = (int)DbCode.HolidayDuplicate,
                        Message = "이미 등록된 휴무일입니다.",
                    },
                }),
            };
            var presenter = new HolidayPresenter(view, service, Status());

            // [X] 생성자가 `Pick(null)` 로 입력행을 비운다 — 값은 그 뒤에 넣어야 한다.
            view.InputDate = new DateTime(2026, 12, 25);
            view.InputName = "겹치는 날";
            int before = service.SearchCalls;

            presenter.Register();

            Assert.AreEqual(before + 1, service.SearchCalls, "실패인데 목록을 다시 읽지 않았다");
            Assert.AreEqual("이미 등록된 휴무일입니다.", view.BlockMessage);
        }

        [TestMethod]
        public void 저장에_성공하면_목록을_다시_읽고_그_줄로_돌아간다()
        {
            var view = Ready();
            var service = new FakeHolidayService { SearchResult = Ok(Rows()), SaveResult = Saved() };
            var presenter = new HolidayPresenter(view, service, Status());
            view.InputDate = new DateTime(2026, 12, 26);
            view.InputName = "센터 휴진일";

            presenter.Register();

            Assert.AreEqual(new DateTime(2026, 12, 26), view.SelectedDate);
            Assert.AreEqual(string.Empty, view.BlockMessage ?? string.Empty);
        }

        // 예외 본문을 화면에 싣지 않는다 (킷 §6).
        [TestMethod]
        public void 예외가_나도_예외_본문을_화면에_싣지_않는다()
        {
            var view = Ready();
            var service = new FakeHolidayService { SearchFailure = new InvalidOperationException("서버가 끊겼습니다") };
            var presenter = new HolidayPresenter(view, service, Status());

            presenter.LoadInitial();

            Assert.IsFalse(view.BlockMessage.Contains("끊겼"));
        }

        // ── 도우미

        private static FakeHolidayView Ready()
        {
            return new FakeHolidayView { FromDate = Today, ToDate = Today.AddYears(2) };
        }

        private static FakeCommonStatusService Status()
        {
            return new FakeCommonStatusService
            {
                Result = OperationResult<CommonWorkStatusDto>.Success(new CommonWorkStatusDto
                {
                    Today = Today,
                    DayName = "금요일",
                    IsBusinessDay = true,
                    IsWithinHours = true,
                    IsWorkAllowed = true,
                    BlockMessage = string.Empty,
                }),
            };
        }

        private static HolidayListItemDto Statutory()
        {
            return new HolidayListItemDto
            {
                HolidayDate = new DateTime(2026, 12, 25),
                HolidayName = "성탄절",
                HolidayType = DbHolidayType.Statutory,
                IsActive = true,
                RowVersion = new byte[] { 1 },
            };
        }

        private static HolidayListItemDto Own()
        {
            return new HolidayListItemDto
            {
                HolidayDate = new DateTime(2026, 12, 26),
                HolidayName = "센터 휴진일",
                HolidayType = DbHolidayType.Own,
                IsActive = true,
                Memo = "정기 휴진",
                RowVersion = new byte[] { 9 },
            };
        }

        private static IList<HolidayListItemDto> Rows()
        {
            return new List<HolidayListItemDto> { Statutory(), Own() };
        }

        private static OperationResult<HolidayListReadDto> Ok(IList<HolidayListItemDto> rows)
        {
            return Ok(rows, new HolidayRegistryDto
            {
                LastHolidayDate = new DateTime(2027, 12, 27),
                RemainingDays = 472,
                WarningThresholdDays = 180,
            });
        }

        private static OperationResult<HolidayListReadDto> Ok(
            IList<HolidayListItemDto> rows, HolidayRegistryDto registry)
        {
            return OperationResult<HolidayListReadDto>.Success(new HolidayListReadDto
            {
                Result = new DbResult { Success = true, Code = 0, Message = "정상 처리되었습니다." },
                Rows = rows,
                Registry = registry,
            });
        }

        private static OperationResult<HolidaySaveReadDto> Saved()
        {
            return OperationResult<HolidaySaveReadDto>.Success(new HolidaySaveReadDto
            {
                Result = new DbResult { Success = true, Code = 0, Message = "정상 처리되었습니다." },
                HolidayDate = new DateTime(2026, 12, 26),
                RowVersion = new byte[] { 9 },
            });
        }
    }

    internal sealed class FakeHolidayView : IHolidayView
    {
        public event EventHandler SearchRequested;
        public event EventHandler<HolidayListItemDto> SelectionChanged;

        public FakeHolidayView()
        {
            InputName = string.Empty;
            InputMemo = string.Empty;
            ConfirmAnswer = true;
        }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string HolidayTypeFilter { get; set; }

        public IList<HolidayListItemDto> Rows { get; set; }
        public DateTime? SelectedDate { get; private set; }

        public DateTime? InputDate { get; set; }
        public string InputName { get; set; }
        public bool InputActive { get; set; }
        public string InputMemo { get; set; }

        public bool EditEnabled { get; set; }
        public bool RowActionsEnabled { get; set; }
        public string RegistryWarning { get; set; }
        public string BlockMessage { get; set; }
        public string LastMessage { get; private set; }

        public bool ConfirmAnswer { get; set; }
        public bool Confirmed { get; private set; }

        public void SelectDate(DateTime holidayDate) { SelectedDate = holidayDate; }

        public void ShowMessage(string message) { LastMessage = message; }

        public bool Confirm(string message)
        {
            Confirmed = true;
            return ConfirmAnswer;
        }

        public void RaiseSearchRequested()
        {
            EventHandler handler = SearchRequested;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }

        public void RaiseSelectionChanged(HolidayListItemDto row)
        {
            EventHandler<HolidayListItemDto> handler = SelectionChanged;
            if (handler != null) { handler(this, row); }
        }
    }

    internal sealed class FakeHolidayService : IHolidayService
    {
        public OperationResult<HolidayListReadDto> SearchResult { get; set; }
        public OperationResult<HolidaySaveReadDto> SaveResult { get; set; }
        public Exception SearchFailure { get; set; }

        public int SearchCalls { get; private set; }
        public HolidaySearchRequest LastSearch { get; private set; }
        public HolidaySaveRequest LastSave { get; private set; }
        public DateTime? LastDeleteDate { get; private set; }

        public OperationResult<HolidayListReadDto> Search(HolidaySearchRequest request)
        {
            if (SearchFailure != null) { throw SearchFailure; }

            SearchCalls++;
            LastSearch = request;
            return SearchResult ?? OperationResult<HolidayListReadDto>.Failure("없다");
        }

        public OperationResult<HolidaySaveReadDto> Register(HolidaySaveRequest request)
        {
            LastSave = request;
            return SaveResult ?? OperationResult<HolidaySaveReadDto>.Failure("없다");
        }

        public OperationResult<HolidaySaveReadDto> Update(HolidaySaveRequest request)
        {
            LastSave = request;
            return SaveResult ?? OperationResult<HolidaySaveReadDto>.Failure("없다");
        }

        public OperationResult<HolidaySaveReadDto> Delete(DateTime holidayDate, byte[] rowVersion)
        {
            LastDeleteDate = holidayDate;
            return SaveResult ?? OperationResult<HolidaySaveReadDto>.Failure("없다");
        }
    }
}
