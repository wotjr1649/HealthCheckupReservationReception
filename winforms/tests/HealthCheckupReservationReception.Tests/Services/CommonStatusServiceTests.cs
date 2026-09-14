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
        // 대상: CommonStatusService.GetCurrent — 공통업무상태(SP-CMN-01) 성공 경로
        // 목적: 05 §3.1 에서 RS0 가 성공이면 뒤따르는 Result Set 이 답이다. Service 가 그 값을
        //       다듬으면 화면이 받는 그림과 DB 가 낸 그림이 달라져, 셸 상태줄이 DB 와 다른 말을 한다.
        // 확인: IsSuccess=true 이고 result.Value 가 Repository 가 준 DTO 와 같은 인스턴스다
        //       (복사·가공 없이 그대로 통과한다).
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

        // 대상: CommonStatusService.GetCurrent — RS0 가 실패로 온 경우
        // 목적: 05 §3.1·§4.3 에서 실패 사유의 문장은 DB 가 갖는다. Service 가 자기 문장으로 바꿔
        //       쓰면 같은 결과코드에 두 가지 안내가 생기고 조작자는 어느 쪽이 진짜인지 모른다.
        // 확인: IsSuccess=false 이고 메시지가 DB 가 준 「함께 사용할 수 없는 입력값 조합입니다.」
        //       그대로다.
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

        // 대상: CommonStatusService.GetCurrent — RS0 성공인데 RS1 이 비어 온 경우
        // 목적: 07 §6.2 — RS0 가 성공이면 RS1 이 반드시 있다. 없는데 성공으로 읽으면 셸이 빈
        //       상태값으로 「업무 가능」을 그리게 되고, 그것은 조작자를 막히는 길로 들여보낸다.
        // 확인: IsSuccess=false 이고 메시지가 빈 문자열이 아니다 — 조용히 넘기지 않고 말한다.
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

        // 대상: CommonStatusService.GetCurrent — RS0 자체를 못 읽은 경우
        // 목적: 05 §3.1 에서 RS0 는 모든 SP 가 정확히 1행 낸다. 없다는 것은 계약이 깨졌다는
        //       뜻이고, 성공으로 넘기면 빈 값이 정상값처럼 화면에 올라간다.
        // 확인: Result=null 인 결과를 받으면 IsSuccess=false 다.
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
