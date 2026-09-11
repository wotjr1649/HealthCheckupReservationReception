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
    /// 수검자 Write 경로의 업무 계층 (05 §10.1 · §10.2). 정규화와 길이가 여기 것이다 (07 §7).
    /// </summary>
    [TestClass]
    public class PatientServiceTests
    {
        // 05 §10.1 조합 — 차트번호자동발급여부=1 이면 차트번호=NULL 이다.
        [TestMethod]
        public void 자동발급이면_차트번호를_싣지_않는다()
        {
            var repository = new FakePatientRepository();
            IPatientService service = new PatientService(repository);

            service.Register(new PatientSaveRequest
            {
                AutoChartNo = true,
                ChartNo = "사용자가 남겨 둔 값",
                Name = "홍길동",
                SocialNumber = "990707-2000018",
            });

            Assert.IsNull(repository.LastRequest.ChartNo);
        }

        // 05 §2.2 — 주민번호만 하이픈을 뗀다. **휴대전화·전화번호는 검색값일 때만** 떼며
        // 저장값은 표시 그대로 들어간다 (VARCHAR(13) 이 하이픈까지 담는다).
        [TestMethod]
        public void 주민번호는_하이픈을_떼고_전화번호는_그대로_저장한다()
        {
            var repository = new FakePatientRepository();
            IPatientService service = new PatientService(repository);

            service.Register(new PatientSaveRequest
            {
                AutoChartNo = true,
                Name = " 홍길동 ",
                SocialNumber = "990707-2000018",
                MobilePhone = "010-0000-0003",
                Phone = "02-000-0003",
            });

            Assert.AreEqual("9907072000018", repository.LastRequest.SocialNumber);
            Assert.AreEqual("010-0000-0003", repository.LastRequest.MobilePhone);
            Assert.AreEqual("02-000-0003", repository.LastRequest.Phone);
            Assert.AreEqual("홍길동", repository.LastRequest.Name);
        }

        // 05 §3.5 — 202·203 은 실패 결과코드지만 화면이 이어서 처리한다. 여기서 접지 않는다.
        [TestMethod]
        public void 중복후보_결과도_그대로_올린다()
        {
            var repository = new FakePatientRepository
            {
                RegisterResult = Read(DbCode.SimilarPatient, false),
            };
            IPatientService service = new PatientService(repository);

            OperationResult<PatientSaveReadDto> result = service.Register(new PatientSaveRequest
            {
                AutoChartNo = true,
                Name = "홍길동",
                SocialNumber = "990707-2000018",
            });

            Assert.IsTrue(result.IsSuccess, "203 을 Service 가 접어 버렸다 — 화면이 후보를 볼 수 없다");
            Assert.AreEqual((int)DbCode.SimilarPatient, result.Value.Result.Code);
        }

        // 킷 §6 — 길이는 Service 가 판정한다. 넘치면 SP 를 부르지 않는다.
        [TestMethod]
        public void 길이를_넘기면_SP_를_부르지_않는다()
        {
            var repository = new FakePatientRepository();
            IPatientService service = new PatientService(repository);

            OperationResult<PatientSaveReadDto> result = service.Register(new PatientSaveRequest
            {
                AutoChartNo = true,
                Name = new string('가', 101),
                SocialNumber = "990707-2000018",
            });

            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(repository.LastRequest, "길이가 넘쳤는데 SP 를 불렀다");
        }

        // 05 §10.2 — 수정은 행버전을 그대로 실어 보낸다 (05 §16.3 byte[8]).
        [TestMethod]
        public void 수정은_행버전을_그대로_싣는다()
        {
            var repository = new FakePatientRepository { UpdateResult = Read(DbCode.Ok, true) };
            IPatientService service = new PatientService(repository);
            var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            service.Update(new PatientSaveRequest
            {
                PatientId = 7,
                RowVersion = rowVersion,
                ChartNo = "2026-000007",
                Name = "홍길동",
                SocialNumber = "990707-2000018",
            });

            Assert.AreEqual(7L, repository.LastRequest.PatientId);
            CollectionAssert.AreEqual(rowVersion, repository.LastRequest.RowVersion);
        }

        [TestMethod]
        public void RS0_을_못_읽으면_실패다()
        {
            var repository = new FakePatientRepository { RegisterResult = new PatientSaveReadDto() };
            IPatientService service = new PatientService(repository);

            OperationResult<PatientSaveReadDto> result = service.Register(new PatientSaveRequest
            {
                AutoChartNo = true,
                Name = "홍길동",
                SocialNumber = "990707-2000018",
            });

            Assert.IsFalse(result.IsSuccess);
        }

        private static PatientSaveReadDto Read(DbCode code, bool success)
        {
            return new PatientSaveReadDto
            {
                Result = new DbResult
                {
                    Success = success,
                    Code = (int)code,
                    Message = "결과 메시지",
                    ServerTime = new DateTime(2026, 9, 9),
                },
                Rows = new List<PatientSaveResultDto>(),
            };
        }
    }

    internal sealed class FakePatientRepository : IPatientRepository
    {
        public PatientSaveRequest LastRequest { get; private set; }
        public PatientSaveReadDto RegisterResult { get; set; }
        public PatientSaveReadDto UpdateResult { get; set; }

        public PatientValidWorkReadDto ValidWorkResult { get; set; }

        public PatientListReadDto Search(PatientSearchRequest request)
        {
            return new PatientListReadDto();
        }

        public PatientValidWorkReadDto ReadValidWork(long patientId)
        {
            return ValidWorkResult;
        }

        public PatientDetailReadDto ReadDetail(long patientId)
        {
            return new PatientDetailReadDto();
        }

        public PatientSaveReadDto Register(PatientSaveRequest request)
        {
            LastRequest = request;
            return RegisterResult ?? Ok();
        }

        public PatientSaveReadDto Update(PatientSaveRequest request)
        {
            LastRequest = request;
            return UpdateResult ?? Ok();
        }

        private static PatientSaveReadDto Ok()
        {
            return new PatientSaveReadDto
            {
                Result = new DbResult
                {
                    Success = true,
                    Code = (int)DbCode.Ok,
                    Message = string.Empty,
                    ServerTime = new DateTime(2026, 9, 9),
                },
                Rows = new List<PatientSaveResultDto>(),
            };
        }
    }
}
