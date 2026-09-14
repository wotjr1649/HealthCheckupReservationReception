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
        // 대상: ChangeLogService — 변경이력 조회(SP-LOG-01) 가 0건을 돌려준 경우
        // 목적: 05 §8.3 이 0건을 결과코드=0 으로 정했다. 감사 기록은 대상 행보다 오래 살기
        //       때문에 200 PatientNotFound 를 쓰지 않기로 계약이 결정한 것이고, Service 가 0건을
        //       실패로 바꾸면 화면이 그 설계를 뒤집는다.
        // 확인: IsSuccess=true 이고 Rows.Count=0 이다 — 「기록이 없다」와 「못 읽었다」를 가른다.
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

        // 대상: ChangeLogService — RS1 자체가 오지 않은 경우의 목록 초기화
        // 목적: 화면이 null 목록을 Grid 에 바인딩하면 그 자리에서 터진다. 0건과 null 을 같은
        //       모양(빈 목록)으로 내보내 화면이 한 가지만 다루게 한다.
        // 확인: IsSuccess=true 이고 Rows 가 null 이 아니라 0건짜리 목록이다.
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

        // 대상: ChangeLogService — RS0 가 실패로 온 경우
        // 목적: 05 §3.1·§4.3 에서 실패 사유의 문장은 DB 가 갖는다. Service 가 바꿔 쓰면 같은
        //       결과코드에 두 가지 안내가 생긴다.
        // 확인: IsSuccess=false 이고 메시지가 「입력값이 올바르지 않습니다.」 그대로다.
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

        // 대상: ChangeLogService — RS0 자체를 못 읽은 경우
        // 목적: 05 §3.1 에서 RS0 는 모든 SP 가 정확히 1행 낸다. 없다는 것은 계약이 깨졌다는
        //       뜻이므로 성공으로 넘기지 않는다.
        // 확인: IsSuccess=false 이고 메시지가 예외 본문이 아니라 「변경이력을 조회하지 못했습니다.」
        //       라는 화면용 문장이다.
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

        // 대상: ChangeLogService — 조회 대상(대상테이블·대상키) 전달
        // 목적: 05 §8.3 에서 대상은 둘뿐이고 그 밖의 값은 SP 가 101 로 막는다. Service 가 대상을
        //       바꿔 넘기면 다른 행의 이력이 이 창에 뜨는데, 감사 기록에서는 그것이 가장 나쁜
        //       실패다 — 틀린 줄도 모르고 남의 기록을 읽는다.
        // 확인: 예약접수·4242 를 넘기면 Repository 가 받은 값도 예약접수·4242 다.
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

    /// <summary>화면 시험이 쓰는 빈 서비스 — DLG-LOG-01 은 열릴 때만 부른다.</summary>
    internal sealed class FakeChangeLogService : IChangeLogService
    {
        public OperationResult<ChangeLogReadDto> Result { get; set; }
        public Exception Failure { get; set; }
        public string LastTargetTable { get; private set; }
        public long LastTargetKey { get; private set; }

        public OperationResult<ChangeLogReadDto> Read(string targetTable, long targetKey)
        {
            if (Failure != null) { throw Failure; }

            LastTargetTable = targetTable;
            LastTargetKey = targetKey;
            return Result ?? OperationResult<ChangeLogReadDto>.Success(
                new ChangeLogReadDto { Rows = new List<ChangeLogItemDto>() });
        }
    }
}
