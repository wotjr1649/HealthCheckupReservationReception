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
        // 대상: WorkService.CompleteReception — 요청 전달과 업무 판정 결과의 성패 규약
        // 목적: 05 §12.1 에서 310(마감경과)처럼 막히는 것은 SP 가 낸 업무 판정이지 호출 실패가
        //       아니다 (05 §3.3). Service 가 이것을 실패로 접으면 화면이 사유를 읽을 길이 사라져
        //       「접수가 안 됩니다」만 뜨고 왜 안 되는지는 어디에도 남지 않는다.
        // 확인: 업무ID·행버전·조작자명이 Repository 에 그대로 전달되고, 310 이 온 경우에도
        //       IsSuccess=true 이며 result.Value.Result.Code 가 310 이다.
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

        // 대상: WorkService.CancelWork — 접수취소(SP-RCP-03) 의 성패 규약
        // 목적: 05 §12.3 에서 취소도 완료와 같은 규약이다. 두 경로가 성패를 다르게 다루면 같은
        //       502 가 한쪽에서는 사유로, 다른 쪽에서는 오류창으로 보여 조작자가 규칙을 못 배운다.
        // 확인: 업무ID 가 그대로 전달되고, 502 가 온 경우에도 IsSuccess=true 이며 결과코드가 502 다.
        [TestMethod]
        public void 접수취소도_같은_길로_간다()
        {
            var repository = new FakeWorkRepository { Save = Blocked(502, "현재 상태에서는 요청한 업무를 처리할 수 없습니다.") };
            var service = new WorkService(repository);

            OperationResult<WorkSaveReadDto> result = service.CancelWork(DbWorkAction.CancelReception, Request());

            Assert.AreEqual(77L, repository.LastCancel.WorkId);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(502, result.Value.Result.Code);
        }

        // 대상: WorkService — 05 §12.1 의 @조작자명 NVARCHAR(50) 길이 검증
        // 목적: 킷 §6 은 SP 계약의 길이를 Service 에서 한 번만 본다. 넘치는 값을 보내면 DB 가
        //       자르고, 잘린 이름이 변경이력에 감사 기록으로 남는다 (04 §14).
        // 확인: 51자 조작자명으로 부르면 IsSuccess=false 이고 Repository 가 호출되지 않는다.
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

        // 대상: WorkService.CompleteReception — 접수완료(SP-RCP-01) 호출 결과 해석
        // 목적: 05 §3.1 은 모든 SP 가 RS0 를 정확히 1행 낸다고 정했다. 그것이 없다는 것은 계약이
        //       깨졌다는 뜻인데, Service 가 성공으로 넘기면 빈 DTO 가 정상값처럼 화면에 올라가고
        //       조작자는 접수가 된 줄로 읽는다.
        // 확인: Repository 가 RS0 없는 결과를 돌려주면 IsSuccess=false 이고, 메시지는 예외 본문이
        //       아니라 「접수하지 못했습니다.」 라는 화면용 문장이다.
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

        public string LastCancelAction { get; private set; }

        public WorkSaveReadDto CancelWork(string actionCode, WorkActionRequest request)
        {
            LastCancelAction = actionCode;
            LastCancel = request;
            return Save;
        }

        public ExtraExamChangeRequest LastExtra { get; private set; }

        public WorkSaveReadDto ChangeExtraExam(ExtraExamChangeRequest request)
        {
            LastExtra = request;
            return Save;
        }
    }
}
