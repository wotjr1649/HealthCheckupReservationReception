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
    /// DLG-RCP-01 접수 처리 (03 §11).
    ///
    /// [X] **접수 가능 여부를 화면이 계산하지 않는다.** 05 §8.2 RS4 의 `START_RECEPTION` 이
    ///     허용여부와 사유를 함께 주고 차단 우선순위(`502 → 308/309 → 503 → 304`)도 DB 가
    ///     갖는다. 03 §11.3 의 다섯 조건을 화면에 다시 적으면 판정이 두 곳이 된다 —
    ///     시험도 **DB 가 준 값을 그대로 그리는가**만 본다.
    /// </summary>
    [TestClass]
    public class ReceptionPresenterTests
    {
        [TestMethod]
        public void 열면_상세를_읽고_RS4_그대로_버튼을_연다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService { DetailResult = Detail(Allowed(true, 0, string.Empty)) };

            new ReceptionPresenter(view, service, "접수1번창구").Begin(77);

            Assert.AreEqual(77L, service.LastDetailWorkId);
            Assert.AreEqual("C000001", view.Detail.ChartNo);
            Assert.IsTrue(view.ReceiveEnabled);
            Assert.AreEqual("접수 가능", view.EligibilityText);
        }

        [TestMethod]
        public void 불가면_DB_가_준_사유를_그대로_붙이고_버튼을_닫는다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService
            {
                DetailResult = Detail(Allowed(false, 304, "접수 마감시각이 지났습니다.")),
            };

            new ReceptionPresenter(view, service, "접수1번창구").Begin(77);

            Assert.IsFalse(view.ReceiveEnabled);
            Assert.AreEqual("접수 불가 — 접수 마감시각이 지났습니다.", view.EligibilityText);
        }

        // 이유 없이 닫힌 버튼은 고장으로 읽힌다 — 메시지가 비면 코드라도 보인다.
        [TestMethod]
        public void 사유메시지가_비면_사유코드라도_보인다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService { DetailResult = Detail(Allowed(false, 503, string.Empty)) };

            new ReceptionPresenter(view, service, "접수1번창구").Begin(77);

            StringAssert.Contains(view.EligibilityText, "503");
        }

        [TestMethod]
        public void 접수에_성공하면_창을_닫는다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService
            {
                DetailResult = Detail(Allowed(true, 0, string.Empty)),
                SaveResult = Saved(true, 0, "정상 처리되었습니다."),
            };
            new ReceptionPresenter(view, service, "접수1번창구").Begin(77);

            view.RaiseReceive();

            Assert.AreEqual(77L, service.LastReception.WorkId);
            Assert.AreEqual("접수1번창구", service.LastReception.OperatorName);
            Assert.IsNotNull(service.LastReception.RowVersion, "행버전을 안 실었다");
            Assert.IsTrue(view.Closed, "성공했는데 창이 안 닫혔다");
        }

        /// <summary>
        /// [X] **사유를 적기 전에 다시 읽는다.** 다시 읽기가 ValidationMessage 를 지우므로
        ///     순서가 뒤집히면 사유가 사라진다 — WF-RSV-01 이 밟은 함정이다.
        /// </summary>
        [TestMethod]
        public void DB_가_막으면_사유가_남고_창은_열려_있다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService
            {
                DetailResult = Detail(Allowed(true, 0, string.Empty)),
                SaveResult = Saved(false, 601, "다른 사용자가 예약·접수 업무를 변경했습니다."),
            };
            new ReceptionPresenter(view, service, "접수1번창구").Begin(77);
            int before = service.DetailCalls;

            view.RaiseReceive();

            Assert.IsFalse(view.Closed);
            StringAssert.Contains(view.ValidationMessage, "다른 사용자가");
            Assert.IsTrue(service.DetailCalls > before, "막힌 뒤 최신값을 다시 읽지 않았다");
        }

        [TestMethod]
        public void 상세를_못_읽으면_비우고_버튼을_닫는다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService { DetailFailure = new InvalidOperationException("서버 SQLDEV01") };

            new ReceptionPresenter(view, service, "접수1번창구").Begin(77);

            Assert.IsNull(view.Detail);
            Assert.IsFalse(view.ReceiveEnabled);
            Assert.IsFalse(view.ValidationMessage.Contains("SQLDEV01"), view.ValidationMessage);
        }

        // 05 §8.2 RS4 는 정확히 5행이고 코드가 고정이다. 없으면 계약 위반이다.
        [TestMethod]
        public void RS4_에_접수_행이_없으면_계약_위반이다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService { DetailResult = Detail(new List<WorkActionDto>()) };

            new ReceptionPresenter(view, service, "접수1번창구").Begin(77);

            Assert.IsFalse(view.ReceiveEnabled);
            Assert.IsNotNull(view.ValidationMessage);
        }

        private static IList<WorkActionDto> Allowed(bool allowed, int reasonCode, string reasonMessage)
        {
            return new List<WorkActionDto>
            {
                new WorkActionDto
                {
                    ActionCode = DbWorkAction.StartReception,
                    Allowed = allowed,
                    ReasonCode = reasonCode,
                    ReasonMessage = reasonMessage,
                },
            };
        }

        private static OperationResult<WorkDetailReadDto> Detail(IList<WorkActionDto> actions)
        {
            return OperationResult<WorkDetailReadDto>.Success(new WorkDetailReadDto
            {
                Result = new DbResult { Success = true, Code = 0, Message = "정상 처리되었습니다." },
                Detail = new WorkDetailDto
                {
                    WorkId = 77,
                    PatientId = 1000,
                    ChartNo = "C000001",
                    Name = "홍길동",
                    Birthday = "19800101",
                    Gender = "M",
                    ReserveDate = new DateTime(2026, 9, 11),
                    SlotCode = "AM",
                    StatusCode = DbWorkStatus.Reserved,
                    Capacity = 20,
                    CurrentCount = 3,
                    RemainingSeats = 17,
                    RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                },
                NexItems = new List<WorkExamItemDto>(),
                AexItems = new List<WorkExamItemDto>(),
                Actions = actions,
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

    internal sealed class FakeReceptionView : IReceptionView
    {
        public event EventHandler ReceiveRequested;

        public WorkDetailDto Detail { get; set; }
        public IList<WorkExamItemDto> NexItems { get; set; }
        public IList<WorkExamItemDto> AexItems { get; set; }
        public string EligibilityText { get; set; }
        public bool ReceiveEnabled { get; set; }
        public string ValidationMessage { get; set; }
        public bool Closed { get; private set; }

        public void Done() { Closed = true; }

        public void RaiseReceive()
        {
            EventHandler handler = ReceiveRequested;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }
    }
}
