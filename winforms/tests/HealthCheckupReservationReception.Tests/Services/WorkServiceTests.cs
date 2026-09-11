using System;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;
using HealthCheckupReservationReception.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Services
{
    /// <summary>
    /// SP-RCP-01 접수완료 · SP-RCP-03 접수취소 (05 §12.1 · §12.3).
    ///
    /// [X] **판정을 이 계층에서 하지 않는다.** 예약일=오늘 · 접수마감 전 · 행버전 · 상태코드는
    ///     전부 SP 가 다시 검증한다. 여기서 미리 막으면 DB 판정을 받아 볼 길이 사라지고,
    ///     그것이 R12 가 되돌린 바로 그 형태다. 그래서 시험도 **막지 않는지**를 본다.
    /// </summary>
    [TestClass]
    public class WorkServiceTests
    {
        [TestMethod]
        public void 접수는_요청을_그대로_넘기고_결과코드를_올려보낸다()
        {
            var repository = new FakeWorkRepository { Save = Blocked(310, "접수 마감시각이 지났습니다.") };
            var service = new WorkService(repository);

            OperationResult<WorkSaveReadDto> result = service.CompleteReception(Request());

            Assert.AreEqual(77L, repository.LastReception.WorkId);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, repository.LastReception.RowVersion);
            Assert.AreEqual("접수1번창구", repository.LastReception.OperatorName);

            // 실패 결과코드도 화면이 이어서 처리한다 — 여기서 접지 않는다.
            Assert.IsTrue(result.IsSuccess, "DB 판정을 받아 왔는데 실패로 접었다");
            Assert.AreEqual(310, result.Value.Result.Code);
        }

        [TestMethod]
        public void 접수취소도_같은_길로_간다()
        {
            var repository = new FakeWorkRepository { Save = Blocked(502, "현재 상태에서는 요청한 업무를 처리할 수 없습니다.") };
            var service = new WorkService(repository);

            OperationResult<WorkSaveReadDto> result = service.CancelReception(Request());

            Assert.AreEqual(77L, repository.LastCancel.WorkId);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(502, result.Value.Result.Code);
        }

        // 05 §12.1 `@조작자명 NVARCHAR(50)`. 길이는 Service 가 본다 (킷 §6).
        [TestMethod]
        public void 조작자명이_50자를_넘으면_부르지_않는다()
        {
            var repository = new FakeWorkRepository();
            var service = new WorkService(repository);

            WorkActionRequest request = Request();
            request.OperatorName = new string('가', 51);

            OperationResult<WorkSaveReadDto> result = service.CompleteReception(request);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(repository.LastReception, "길이를 넘겼는데 SP 를 불렀다");
        }

        [TestMethod]
        public void RS0_을_못_읽으면_실패다()
        {
            var repository = new FakeWorkRepository { Save = new WorkSaveReadDto() };
            var service = new WorkService(repository);

            OperationResult<WorkSaveReadDto> result = service.CompleteReception(Request());

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("접수하지 못했습니다.", result.Message);
        }

        private static WorkActionRequest Request()
        {
            return new WorkActionRequest
            {
                WorkId = 77,
                RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                OperatorName = "  접수1번창구  ",
            };
        }

        private static WorkSaveReadDto Blocked(int code, string message)
        {
            return new WorkSaveReadDto
            {
                Result = new DbResult { Success = false, Code = code, Message = message },
            };
        }
    }

    internal sealed class FakeWorkRepository : IWorkRepository
    {
        public WorkSaveReadDto Save { get; set; }
        public WorkActionRequest LastReception { get; private set; }
        public WorkActionRequest LastCancel { get; private set; }

        public WorkListReadDto Search(WorkSearchRequest request) { return new WorkListReadDto(); }

        public WorkDetailReadDto ReadDetail(long workId) { return new WorkDetailReadDto(); }

        public WorkSaveReadDto CompleteReception(WorkActionRequest request)
        {
            LastReception = request;
            return Save;
        }

        public WorkSaveReadDto CancelReception(WorkActionRequest request)
        {
            LastCancel = request;
            return Save;
        }
    }
}
