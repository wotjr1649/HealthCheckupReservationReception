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

        // [X] **`저장가능=0` 은 실패가 아니다.** 차단코드를 달고 오는 성공한 조회이고
        //     (05 §9.6) 화면은 그 사유를 보여야 한다. 실패로 접으면 사용자는 왜 저장이
        //     막혔는지 영영 모른다 — 정원 초과·마감 경과가 전부 이 길로 온다.
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

        // 05 §9.6 — RS1 은 정확히 1행이다. 비면 계약 위반이지 빈 결과가 아니다.
        [TestMethod]
        public void RS0_성공인데_예약요약이_비면_실패다()
        {
            ReservationAvailabilityReadDto read = Availability();
            read.Summary = null;

            var service = new ReservationService(new FakeReservationRepository { Availability = read });

            Assert.IsFalse(service.GetAvailability(Request()).IsSuccess);
        }

        [TestMethod]
        public void RS0_을_못_읽으면_실패다()
        {
            var service = new ReservationService(new FakeReservationRepository { Availability = new ReservationAvailabilityReadDto() });

            Assert.IsFalse(service.GetAvailability(Request()).IsSuccess);
        }

        // 05 §9.11 — 변경범위에 따라 뒤 넷은 0행이 정상이다. 화면이 null 을 따지지 않게 맞춘다.
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

        // [X] 03 §8.11 2단계 저장 — DB 가 낸 실패 결과코드도 화면이 이어서 처리해야 한다
        //     (사유 표시 · 일정/대상/검사구성 Refresh). 서비스가 여기서 접으면 그 길이 끊긴다.
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

        // 05 §11.1 의 Parameter 크기. 길이는 Service 가 본다 (킷 §6).
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

        public WorkSaveReadDto Register(ReservationSaveRequest request)
        {
            LastSave = request;
            return Save;
        }
    }
}
