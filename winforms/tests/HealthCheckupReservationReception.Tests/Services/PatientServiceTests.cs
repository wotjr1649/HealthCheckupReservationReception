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
        // 대상: PatientService — 수검자 등록(SP-PAT-03) 요청 조립, 차트번호 자동발급 분기
        // 목적: 05 §10.1 조합 계약에서 차트번호자동발급여부=1 이면 차트번호는 NULL 이어야 한다.
        //       화면에 남아 있던 값을 그대로 실어 보내면 SP 가 조합 위반으로 막거나, 더 나쁘게는
        //       자동발급 대신 그 값으로 등록된다.
        // 확인: 자동발급으로 등록 요청을 만들면 Repository 가 받은 요청의 ChartNo 가 null 이다.
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

        // 대상: PatientService — 05 §2.2 문자열 정규화 (저장 경로)
        // 목적: 정규화 대상이 값마다 다르다. 주민번호는 하이픈을 떼고 13자리로 보내지만,
        //       휴대전화·전화번호는 VARCHAR(13) 이 하이픈까지 담으므로 표시 그대로 저장한다
        //       (떼는 것은 검색값일 때뿐이다). 한 규칙으로 뭉뚱그리면 한쪽이 반드시 깨진다.
        // 확인: 990707-2000018 은 9907072000018 로 떼어 보내고, 010-0000-0003 · 02-000-0003 은
        //       하이픈이 든 채로 그대로 간다. 이름은 앞뒤 공백만 걷고 그대로다.
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

        // 대상: PatientService — 결과코드 202(동일번호 이름불일치)·203(유사후보) 처리
        // 목적: 05 §3.5 에서 202·203 은 실패 결과코드지만 화면이 이어서 처리하는 값이다.
        //       Service 가 이것을 실패로 접으면 화면이 후보 목록을 받지 못하고, 조작자는 왜
        //       등록이 안 되는지 모른 채 같은 입력을 반복한다.
        // 확인: 203 이 온 결과도 IsSuccess=true 로 올라오고, result.Value.Result.Code 가 203 이다
        //       — 판정은 화면이 이어서 한다.
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

        // 대상: PatientService — 05 §10.1 Parameter 크기에 따른 길이 검증
        // 목적: 킷 §6 은 SP 계약의 길이 제한을 Service 에서 한 번만 검증하기로 정했다. 넘치는
        //       값을 그대로 보내면 DB 가 자르거나 101 을 돌려주고, 왕복 한 번이 헛돈다.
        // 확인: 길이를 넘긴 값으로 부르면 IsSuccess=false 이고 Repository 가 아예 호출되지 않는다
        //       (LastRequest 가 null).
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

        // 대상: PatientService — 수검자정보 수정(SP-PAT-04) 의 낙관적 동시성 값 전달
        // 목적: 05 §10.2·§16.3 에서 행버전은 byte[8] 원본 그대로 왕복해야 한다. 문자열로 바꿔
        //       재전송하거나 잃으면 SP 의 행버전 비교가 언제나 어긋나 601 만 돌아온다 (06 §26).
        // 확인: 수검자ID 7 과 byte[8] 행버전을 넘기면 Repository 가 받은 값이 바이트 단위로 같다.
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

        // 대상: PatientService — RS0 자체를 못 읽은 경우
        // 목적: 05 §3.1 에서 RS0 는 모든 SP 가 정확히 1행 낸다. 없다는 것은 계약이 깨졌다는
        //       뜻이므로 성공으로 넘기지 않는다 — 넘기면 등록이 안 됐는데 됐다고 보인다.
        // 확인: RS0 없는 결과를 받으면 IsSuccess=false 다.
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

        public PatientListReadDto Search(PatientSearchRequest request)
        {
            return new PatientListReadDto();
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
