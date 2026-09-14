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
    /// WF-RSV-01 의 업무 계층 (05 §9 · §11.1).
    ///
    /// 여기서 재는 것은 **판정이 아니라 성패 규약**이다 — 정원·마감·TGT·AEX 는 DB 가 내고
    /// 이 계층은 그것을 접지 않고 넘기는지만 본다.
    /// </summary>
    [TestClass]
    public class ReservationServiceTests
    {
        private static readonly DateTime Day = new DateTime(2026, 9, 14);

        // 대상: ReservationService.GetAvailability — 예약 가능정보(SP-RSV-01) 의 차단 결과 처리
        // 목적: 05 §9.6 에서 저장가능=0 은 차단코드를 달고 오는 **성공한 조회**다. 실패로 접으면
        //       화면이 사유를 받지 못해 조작자는 왜 저장이 막혔는지 영영 모른다 — 정원 초과와
        //       마감 경과가 전부 이 길로 온다.
        // 확인: 저장가능=0 인 결과도 IsSuccess=true 이고, Summary.CanSave=false · BlockCode=305
        //       (정원초과) 가 화면까지 올라간다.
        [TestMethod]
        public void 저장가능이_0_이어도_성공한_조회다()
        {
            ReservationAvailabilityReadDto read = Availability();
            read.Summary.CanSave = false;
            read.Summary.BlockCode = (int)DbCode.SlotFull;
            read.Summary.BlockMessage = "해당 시간대의 정원이 찼습니다.";

            var service = new ReservationService(new FakeReservationRepository { Availability = read });

            OperationResult<ReservationAvailabilityReadDto> result = service.GetAvailability(Request());

            Assert.IsTrue(result.IsSuccess, "차단 사유를 실패로 접었다");
            Assert.IsFalse(result.Value.Summary.CanSave);
            Assert.AreEqual((int)DbCode.SlotFull, result.Value.Summary.BlockCode);
        }

        // 대상: ReservationService.GetAvailability — RS0 가 실패로 온 경우
        // 목적: 05 §3.1·§4.3 에서 실패 사유의 문장은 DB 가 갖는다. Service 가 바꿔 쓰면 같은
        //       결과코드에 두 가지 안내가 생긴다.
        // 확인: IsSuccess=false 이고 메시지가 「수검자를 찾을 수 없습니다.」 그대로다.
        [TestMethod]
        public void RS0_실패면_그_메시지로_실패한다()
        {
            ReservationAvailabilityReadDto read = Availability();
            read.Result.Success = false;
            read.Result.Code = (int)DbCode.PatientNotFound;
            read.Result.Message = "수검자를 찾을 수 없습니다.";

            var service = new ReservationService(new FakeReservationRepository { Availability = read });

            OperationResult<ReservationAvailabilityReadDto> result = service.GetAvailability(Request());

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("수검자를 찾을 수 없습니다.", result.Message);
        }

        // 대상: ReservationService.GetAvailability — RS0 성공인데 RS1 예약요약이 비어 온 경우
        // 목적: 05 §9.6 에서 RS1 은 정확히 1행이다. 비었다는 것은 계약 위반이지 「조회 결과가
        //       없다」가 아니다 — 빈 요약으로 화면을 세우면 정원·마감·저장가능이 전부 0 이 된다.
        // 확인: RS1 이 빈 결과를 받으면 IsSuccess=false 다.
        [TestMethod]
        public void RS0_성공인데_예약요약이_비면_실패다()
        {
            ReservationAvailabilityReadDto read = Availability();
            read.Summary = null;

            var service = new ReservationService(new FakeReservationRepository { Availability = read });

            Assert.IsFalse(service.GetAvailability(Request()).IsSuccess);
        }

        // 대상: ReservationService.GetAvailability — RS0 자체를 못 읽은 경우
        // 목적: 05 §3.1 에서 RS0 는 모든 SP 가 정확히 1행 낸다. 없다는 것은 계약이 깨졌다는
        //       뜻이므로 성공으로 넘기지 않는다.
        // 확인: RS0 없는 결과를 받으면 IsSuccess=false 다.
        [TestMethod]
        public void RS0_을_못_읽으면_실패다()
        {
            var service = new ReservationService(new FakeReservationRepository { Availability = new ReservationAvailabilityReadDto() });

            Assert.IsFalse(service.GetAvailability(Request()).IsSuccess);
        }

        // 대상: ReservationService — 05 §9.11 변경범위별 Cardinality 에 따른 빈 Result Set 처리
        // 목적: 변경범위에 따라 뒤따르는 넷은 0행이 정상이다. 그것을 null 로 흘려보내면 화면이
        //       Grid 바인딩에서 터지고, 화면마다 null 검사를 복사하게 된다.
        // 확인: Slots·NexItems·AexItems 가 null 이 아니라 0건짜리 목록이다. 단 Target 은 목록이
        //       아니라 1행이므로 없으면 null 그대로다 — 빈 목록으로 꾸미지 않는다.
        [TestMethod]
        public void 변경범위가_비운_Result_Set_은_빈_목록이_된다()
        {
            ReservationAvailabilityReadDto read = Availability();
            read.Slots = null;
            read.NexItems = null;
            read.AexItems = null;
            read.Target = null;

            var service = new ReservationService(new FakeReservationRepository { Availability = read });

            ReservationAvailabilityReadDto value = service.GetAvailability(Request()).Value;

            Assert.AreEqual(0, value.Slots.Count);
            Assert.AreEqual(0, value.NexItems.Count);
            Assert.AreEqual(0, value.AexItems.Count);
            Assert.IsNull(value.Target, "TGT 는 목록이 아니라 1행이다 — 없으면 없는 것이다");
        }

        // ── 05 §11.1 예약 등록

        // 대상: ReservationService — 예약 저장(SP-RSV-02) 이 업무 판정으로 막은 경우
        // 목적: 03 §8.11 의 2단계 저장은 DB 가 낸 실패 결과코드를 화면이 이어서 처리한다 —
        //       사유를 보이고 일정·대상·검사구성을 다시 읽는다. Service 가 여기서 접으면 그 길이
        //       끊겨 조작자는 사유도 최신값도 못 받는다.
        // 확인: 305(정원초과) 로 막힌 저장도 IsSuccess=true 이고 result.Value.Result.Code 가 305 다.
        [TestMethod]
        public void 저장_실패_결과코드도_화면까지_올려보낸다()
        {
            var read = new WorkSaveReadDto
            {
                Result = new DbResult
                {
                    Success = false,
                    Code = (int)DbCode.SlotFull,
                    Message = "해당 시간대의 정원이 찼습니다.",
                },
            };
            var service = new ReservationService(new FakeReservationRepository { Save = read });

            OperationResult<WorkSaveReadDto> result = service.Register(SaveRequest());

            Assert.IsTrue(result.IsSuccess, "DB 판정을 받아 왔으므로 성공이다");
            Assert.AreEqual((int)DbCode.SlotFull, result.Value.Result.Code);
        }

        // 대상: ReservationService — 저장 성공 시 업무ID·상태코드·행버전 회수
        // 목적: 05 §16.3 에서 저장 뒤의 행버전은 다음 변경·취소가 쥐고 가야 하는 값이다. 여기서
        //       잃으면 바로 이어지는 동작이 낙관적 동시성 검사에 걸려 601 만 돌아온다 (06 §26).
        // 확인: IsSuccess=true 이고 업무ID=77 · 상태코드=RSV 가 화면까지 올라온다.
        [TestMethod]
        public void 저장이_성공하면_업무ID_와_행버전을_돌려준다()
        {
            var repository = new FakeReservationRepository
            {
                Save = new WorkSaveReadDto
                {
                    Result = Ok(),
                    Row = new WorkSaveResultDto { WorkId = 77, StatusCode = "RSV", RowVersion = new byte[8] },
                },
            };
            var service = new ReservationService(repository);

            OperationResult<WorkSaveReadDto> result = service.Register(SaveRequest());

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(77L, result.Value.Row.WorkId);
            Assert.AreEqual("RSV", result.Value.Row.StatusCode);
        }

        // 대상: ReservationService — 05 §11.1 Parameter 크기에 따른 길이 검증
        // 목적: 킷 §6 은 SP 계약의 길이를 Service 에서 한 번만 본다. 넘치는 값을 보내면 DB 가
        //       자르고, 잘린 이름이 변경이력에 남는다.
        // 확인: 길이를 넘긴 조작자명으로 저장하면 IsSuccess=false 이고 Repository 가 호출되지 않는다.
        [TestMethod]
        public void 조작자명이_계약_길이를_넘으면_DB_에_가지_않는다()
        {
            var repository = new FakeReservationRepository();
            var service = new ReservationService(repository);

            ReservationSaveRequest request = SaveRequest();
            request.OperatorName = new string('창', 51);

            OperationResult<WorkSaveReadDto> result = service.Register(request);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(repository.LastSave, "길이 위반인데 SP 를 불렀다");
        }

        // 대상: ReservationService — 05 §2.2 문자열 정규화 (조작자명)
        // 목적: 정규화는 C# 이 먼저 한다. 공백이 섞인 채 저장되면 변경이력의 조작자 가 같은
        //       사람인데 다른 값으로 남아 감사 기록에서 한 사람이 둘로 세어진다 (04 §14).
        // 확인: 앞뒤 공백이 붙은 조작자명을 넘기면 Repository 가 받는 값은 「접수1번창구」다.
        [TestMethod]
        public void 조작자명의_앞뒤_공백은_걷어서_보낸다()
        {
            var repository = new FakeReservationRepository
            {
                Save = new WorkSaveReadDto { Result = Ok() },
            };
            var service = new ReservationService(repository);

            ReservationSaveRequest request = SaveRequest();
            request.OperatorName = "  접수1번창구  ";

            service.Register(request);

            Assert.AreEqual("접수1번창구", repository.LastSave.OperatorName);
        }

        // ── helpers

        private static ReservationAvailabilityRequest Request()
        {
            return new ReservationAvailabilityRequest
            {
                PatientId = 1000,
                ReserveType = DbReserveType.Normal,
                ReserveDate = Day,
                SlotCode = "AM",
            };
        }

        private static ReservationSaveRequest SaveRequest()
        {
            return new ReservationSaveRequest
            {
                PatientId = 1000,
                ReserveType = DbReserveType.Normal,
                ReserveDate = Day,
                SlotCode = "AM",
                OperatorName = "접수1번창구",
            };
        }

        private static DbResult Ok()
        {
            return new DbResult
            {
                Success = true,
                Code = (int)DbCode.Ok,
                Message = "정상 처리되었습니다.",
                ServerTime = new DateTime(2026, 9, 10, 9, 0, 0),
            };
        }

        private static ReservationAvailabilityReadDto Availability()
        {
            return new ReservationAvailabilityReadDto
            {
                Result = Ok(),
                Summary = new ReservationSummaryDto
                {
                    ChangeScope = "ALL",
                    PatientId = 1000,
                    ReserveType = DbReserveType.Normal,
                    ReserveDate = Day,
                    SlotCode = "AM",
                    WorkAllowed = true,
                    CanSave = true,
                    BlockCode = (int)DbCode.Ok,
                    BlockMessage = string.Empty,
                },
                Slots = new List<SlotInfoDto>
                {
                    new SlotInfoDto { SlotCode = "AM", SlotName = "오전", Capacity = 20, CurrentCount = 12, AppliedCount = 13, RemainingSeats = 7, IsOperating = true, Selectable = true, BlockMessage = string.Empty },
                    new SlotInfoDto { SlotCode = "PM", SlotName = "오후", Capacity = 20, CurrentCount = 20, AppliedCount = 21, RemainingSeats = 0, IsOperating = true, Selectable = false, BlockCode = (int)DbCode.SlotFull, BlockMessage = "해당 시간대의 정원이 찼습니다." },
                },
                Target = new ExamTargetDto { IsTarget = true, Age = 46, ReasonMessage = string.Empty },
                NexItems = new List<WorkExamItemDto>
                {
                    new WorkExamItemDto { ExamItemCode = "E01", ExamItemName = "문진/진찰", NexType = "기본" },
                },
                AexItems = new List<ReservationAexItemDto>
                {
                    new ReservationAexItemDto { AexCode = "OPT01", ExamItemCode = "E11", ExamItemName = "복부초음파", Selectable = true, ReasonMessage = string.Empty },
                },
            };
        }
    }

    internal sealed class FakeReservationRepository : IReservationRepository
    {
        public ReservationAvailabilityReadDto Availability { get; set; }
        public WorkSaveReadDto Save { get; set; }
        public ReservationAvailabilityRequest LastAvailability { get; private set; }
        public ReservationSaveRequest LastSave { get; private set; }

        public ReservationAvailabilityReadDto ReadAvailability(ReservationAvailabilityRequest request)
        {
            LastAvailability = request;
            return Availability;
        }

        public ReservationChangeRequest LastChange { get; private set; }
        public WorkActionRequest LastCancel { get; private set; }

        public WorkSaveReadDto Change(ReservationChangeRequest request)
        {
            LastChange = request;
            return Save;
        }

        public WorkSaveReadDto Cancel(WorkActionRequest request)
        {
            LastCancel = request;
            return Save;
        }

        public WorkSaveReadDto Register(ReservationSaveRequest request)
        {
            LastSave = request;
            return Save;
        }
    }
}
