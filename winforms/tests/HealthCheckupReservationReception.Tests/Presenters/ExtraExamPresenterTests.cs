using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Presenters
{
    /// <summary>
    /// DLG-RCP-02 접수완료 추가검사 변경 (03 §12).
    ///
    /// 이 화면이 서게 된 것이 R18 재봉인의 이유다 — `SP-WRK-02` RS5 가 AEX 일곱을 주기
    /// 전에는 안 고른 다섯의 이름도 가용성도 읽을 길이 없었다 (05 §8.2).
    /// </summary>
    [TestClass]
    public class ExtraExamPresenterTests
    {
        [TestMethod]
        public void 열면_RS5_일곱을_그대로_싣는다()
        {
            var view = new FakeExtraExamView();
            var service = new FakeWorkService { DetailResult = Detail(true) };

            new ExtraExamPresenter(view, service, "접수1번창구").Begin(77);

            Assert.AreEqual(7, view.AexOptions.Count);
            Assert.IsTrue(view.SaveEnabled);
            Assert.AreEqual("추가검사 변경", view.Title);
            Assert.IsNull(view.ValidationMessage);
        }

        // 05 §8.2 RS4 `EDIT_EXTRA` 가 허용여부를 준다 — 화면이 상태로 다시 재지 않는다.
        [TestMethod]
        public void 허용되지_않으면_사유를_적고_닫는다()
        {
            var view = new FakeExtraExamView();
            var service = new FakeWorkService { DetailResult = Detail(false) };

            new ExtraExamPresenter(view, service, "접수1번창구").Begin(77);

            Assert.IsFalse(view.SaveEnabled);
            StringAssert.Contains(view.ValidationMessage, "현재 상태에서는");
        }

        /// <summary>
        /// 05 §12.2 — 화면이 고른 일곱을 **순서 그대로** 보낸다. 동일 집합인지는 SP 가 잰다.
        /// </summary>
        [TestMethod]
        public void 저장은_일곱_선택을_순서대로_보낸다()
        {
            var view = new FakeExtraExamView { Selection = new[] { true, false, false, true, false, false, false } };
            var service = new FakeWorkService
            {
                DetailResult = Detail(true),
                SaveResult = Saved(true, 0, "정상 처리되었습니다."),
            };
            new ExtraExamPresenter(view, service, "접수1번창구").Begin(77);

            view.RaiseSave();

            CollectionAssert.AreEqual(
                new[] { true, false, false, true, false, false, false }, service.LastExtra.AexSelected);
            Assert.AreEqual(77L, service.LastExtra.WorkId);
            Assert.IsNotNull(service.LastExtra.RowVersion, "행버전을 안 실었다");
            Assert.IsTrue(view.Closed);
        }

        [TestMethod]
        public void DB_가_막으면_사유가_남고_창은_열려_있다()
        {
            var view = new FakeExtraExamView();
            var service = new FakeWorkService
            {
                DetailResult = Detail(true),
                SaveResult = Saved(false, 411, "성별 조건을 충족하지 않는 추가검사입니다."),
            };
            new ExtraExamPresenter(view, service, "접수1번창구").Begin(77);
            int before = service.DetailCalls;

            view.RaiseSave();

            Assert.IsFalse(view.Closed);
            StringAssert.Contains(view.ValidationMessage, "성별 조건");
            Assert.IsTrue(service.DetailCalls > before, "막힌 뒤 최신값을 다시 읽지 않았다");
        }

        private static OperationResult<WorkDetailReadDto> Detail(bool allowed)
        {
            var options = new List<ReservationAexItemDto>();
            for (int i = 1; i <= 7; i++)
            {
                options.Add(new ReservationAexItemDto
                {
                    AexCode = "OPT0" + i,
                    ExamItemCode = "EX0" + (13 + i),
                    ExamItemName = "추가검사" + i,
                    Requested = i == 4,
                    Selectable = i != 3,
                    ReasonCode = i == 3 ? 411 : 0,
                    ReasonMessage = i == 3 ? "성별 조건을 충족하지 않는 추가검사입니다." : string.Empty,
                });
            }

            return OperationResult<WorkDetailReadDto>.Success(new WorkDetailReadDto
            {
                Result = new DbResult { Success = true, Code = 0, Message = "정상 처리되었습니다." },
                Detail = new WorkDetailDto
                {
                    WorkId = 77, PatientId = 1000, ChartNo = "C000001", Name = "홍길동",
                    Birthday = "19800101", Gender = "M",
                    ReserveDate = new DateTime(2026, 9, 11), SlotCode = "AM",
                    StatusCode = DbWorkStatus.Received, Capacity = 20, CurrentCount = 3, RemainingSeats = 17,
                    RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                },
                NexItems = new List<WorkExamItemDto>(),
                AexItems = new List<WorkExamItemDto>(),
                AexOptions = options,
                Actions = new List<WorkActionDto>
                {
                    new WorkActionDto
                    {
                        ActionCode = DbWorkAction.EditExtra,
                        Allowed = allowed,
                        ReasonCode = allowed ? 0 : 502,
                        ReasonMessage = allowed ? string.Empty : "현재 상태에서는 요청한 업무를 처리할 수 없습니다.",
                    },
                },
            });
        }

        private static OperationResult<WorkSaveReadDto> Saved(bool success, int code, string message)
        {
            return OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
            {
                Result = new DbResult { Success = success, Code = code, Message = message },
                Row = success
                    ? new WorkSaveResultDto { WorkId = 77, StatusCode = DbWorkStatus.Received, RowVersion = new byte[8] }
                    : null,
            });
        }
    }

    internal sealed class FakeExtraExamView : IExtraExamView
    {
        public event EventHandler SaveRequested;

        public string Title { get; set; }
        public WorkDetailDto Detail { get; set; }
        public IList<WorkExamItemDto> NexItems { get; set; }
        public IList<ReservationAexItemDto> AexOptions { get; set; }
        public bool SaveEnabled { get; set; }
        public string ValidationMessage { get; set; }
        public bool Closed { get; private set; }

        public bool[] Selection { get; set; }

        public bool[] AexSelection
        {
            get { return Selection ?? new bool[ReservationAvailabilityRequest.AexParameterCount]; }
        }

        public void Done() { Closed = true; }

        public void RaiseSave()
        {
            EventHandler handler = SaveRequested;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }
    }
}
