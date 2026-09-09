using System;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;
using HealthCheckupReservationReception.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Services
{
    [TestClass]
    public class CommonStatusServiceTests
    {
        [TestMethod]
        public void RS0_성공이면_RS1_을_그대로_돌려준다()
        {
            var status = new CommonWorkStatusDto { IsWorkAllowed = true };
            var service = new CommonStatusService(new FakeCommonStatusRepository
            {
                Read = new CommonWorkStatusReadDto { Result = Ok(), Status = status }
            });

            OperationResult<CommonWorkStatusDto> result = service.GetCurrent();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreSame(status, result.Value);
        }

        [TestMethod]
        public void RS0_실패면_그_메시지로_실패한다()
        {
            var service = new CommonStatusService(new FakeCommonStatusRepository
            {
                Read = new CommonWorkStatusReadDto
                {
                    Result = new DbResult
                    {
                        Success = false,
                        Code = (int)DbCode.BadRequest,
                        Message = "함께 사용할 수 없는 입력값 조합입니다."
                    }
                }
            });

            OperationResult<CommonWorkStatusDto> result = service.GetCurrent();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("함께 사용할 수 없는 입력값 조합입니다.", result.Message);
        }

        // RS0 이 성공인데 RS1 이 비면 계약 위반이다. 조용히 성공으로 읽지 않는다 (07 §6.2).
        [TestMethod]
        public void RS0_성공인데_RS1_이_비면_실패다()
        {
            var service = new CommonStatusService(new FakeCommonStatusRepository
            {
                Read = new CommonWorkStatusReadDto { Result = Ok(), Status = null }
            });

            OperationResult<CommonWorkStatusDto> result = service.GetCurrent();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreNotEqual(string.Empty, result.Message);
        }

        [TestMethod]
        public void RS0_자체가_없으면_실패다()
        {
            var service = new CommonStatusService(new FakeCommonStatusRepository
            {
                Read = new CommonWorkStatusReadDto { Result = null }
            });

            Assert.IsFalse(service.GetCurrent().IsSuccess);
        }

        private static DbResult Ok()
        {
            return new DbResult
            {
                Success = true,
                Code = (int)DbCode.Ok,
                Message = "정상 처리되었습니다.",
                ServerTime = new DateTime(2026, 9, 9, 12, 0, 0)
            };
        }
    }

    internal sealed class FakeCommonStatusRepository : ICommonStatusRepository
    {
        public CommonWorkStatusReadDto Read { get; set; }

        CommonWorkStatusReadDto ICommonStatusRepository.Read()
        {
            return Read;
        }
    }
}
