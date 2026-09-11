using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;
using HealthCheckupReservationReception.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Services
{
    /// <summary>
    /// DLG-LOG-01 의 업무 계층 (05 §8.3).
    ///
    /// [X] **여기서 가장 중요한 시험은 「0건이 성공이다」 하나다.** 계약이 그것을 명시적으로
    ///     정했고(`200 PatientNotFound` 를 쓰지 않는다 — 감사 기록은 대상 행보다 오래 산다),
    ///     서비스가 0건을 실패로 바꾸면 화면이 그 설계를 뒤집는다.
    /// </summary>
    [TestClass]
    public class ChangeLogServiceTests
    {
        [TestMethod]
        public void 기록이_0건이어도_성공이고_빈_목록이다()
        {
            var service = new ChangeLogService(new FakeChangeLogRepository
            {
                Read = new ChangeLogReadDto { Result = Ok(), Rows = new List<ChangeLogItemDto>() },
            });

            OperationResult<ChangeLogReadDto> result = service.Read(DbLogTarget.Patient, 11);

            Assert.IsTrue(result.IsSuccess, "0건을 실패로 바꿨다: " + result.Message);
            Assert.AreEqual(0, result.Value.Rows.Count);
        }

        // RS1 자체가 없어도 화면이 null 을 만나지 않는다.
        [TestMethod]
        public void RS1_이_없으면_빈_목록으로_채운다()
        {
            var service = new ChangeLogService(new FakeChangeLogRepository
            {
                Read = new ChangeLogReadDto { Result = Ok(), Rows = null },
            });

            OperationResult<ChangeLogReadDto> result = service.Read(DbLogTarget.Work, 77);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Value.Rows);
            Assert.AreEqual(0, result.Value.Rows.Count);
        }

        [TestMethod]
        public void RS0_실패면_그_메시지로_실패한다()
        {
            var service = new ChangeLogService(new FakeChangeLogRepository
            {
                Read = new ChangeLogReadDto
                {
                    Result = new DbResult
                    {
                        Success = false,
                        Code = (int)DbCode.BadRequest,
                        Message = "입력값이 올바르지 않습니다.",
                    },
                },
            });

            OperationResult<ChangeLogReadDto> result = service.Read("완료이력", 1);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("입력값이 올바르지 않습니다.", result.Message);
        }

        [TestMethod]
        public void RS0_을_못_읽으면_실패다()
        {
            var service = new ChangeLogService(new FakeChangeLogRepository
            {
                Read = new ChangeLogReadDto { Result = null },
            });

            OperationResult<ChangeLogReadDto> result = service.Read(DbLogTarget.Patient, 11);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("변경이력을 조회하지 못했습니다.", result.Message);
        }

        // 05 §8.3 — 대상은 둘뿐이고 그 밖의 값은 SP 가 101 이다. 화면이 무엇을 보내는지는
        // 값이 아니라 **그대로 전달되는가**를 본다.
        [TestMethod]
        public void 대상테이블과_대상키를_그대로_넘긴다()
        {
            var repository = new FakeChangeLogRepository
            {
                Read = new ChangeLogReadDto { Result = Ok(), Rows = new List<ChangeLogItemDto>() },
            };
            var service = new ChangeLogService(repository);

            service.Read(DbLogTarget.Work, 4242);

            Assert.AreEqual("예약접수", repository.LastTargetTable);
            Assert.AreEqual(4242L, repository.LastTargetKey);
        }

        private static DbResult Ok()
        {
            return new DbResult
            {
                Success = true,
                Code = (int)DbCode.Ok,
                Message = "정상 처리되었습니다.",
                ServerTime = new DateTime(2026, 9, 11, 9, 0, 0),
            };
        }
    }

    internal sealed class FakeChangeLogRepository : IChangeLogRepository
    {
        public ChangeLogReadDto Read { get; set; }
        public string LastTargetTable { get; private set; }
        public long LastTargetKey { get; private set; }

        ChangeLogReadDto IChangeLogRepository.Read(string targetTable, long targetKey)
        {
            LastTargetTable = targetTable;
            LastTargetKey = targetKey;
            return Read;
        }
    }
}
