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
    /// DLG-HOL-01 의 업무 계층 (05 §12.5~§12.8).
    ///
    /// [X] **Service 여섯 중 이것만 전용 시험이 없었다** (2026-09-15 실측). Presenter 는
    ///     `FakeHolidayService` 로 돌고 `HolidayRepository` 는 실물 DB 시험이 있는데,
    ///     그 사이 계층만 한 번도 실행되지 않았다 — RS2 계약 위반 판정도, 길이 검증도,
    ///     등록/수정이 서로 다른 동작코드로 가는지도 아무것도 보지 않았다.
    ///
    /// 나머지 다섯 Service 와 같은 것을 잰다: 판정이 아니라 **성패 규약**이다.
    /// 801·802·601 은 DB 가 내고 이 계층은 그것을 접지 않는다.
    /// </summary>
    [TestClass]
    public class HolidayServiceTests
    {
        private static readonly DateTime Day = new DateTime(2026, 12, 26);

        // 대상: HolidayService.Search — 휴무일목록 조회(SP-HOL-01) 성공 경로
        // 목적: 05 §3.1 에서 RS0 가 성공이면 뒤따르는 Result Set 이 답이다. Service 가 그 값을
        //       다듬으면 화면이 받는 그림과 DB 가 낸 그림이 달라진다.
        // 확인: IsSuccess=true 이고 목록 2행과 등재현황이 Repository 가 준 인스턴스 그대로다.
        [TestMethod]
        public void RS0_성공이면_목록과_등재현황을_그대로_돌려준다()
        {
            var registry = new HolidayRegistryDto { RemainingDays = 120, WarningThresholdDays = 180 };
            var repository = new FakeHolidayRepository
            {
                SearchResult = new HolidayListReadDto { Result = Ok(), Rows = Rows(), Registry = registry },
            };

            OperationResult<HolidayListReadDto> result = new HolidayService(repository).Search(Request());

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, result.Value.Rows.Count);
            Assert.AreSame(registry, result.Value.Registry, "등재현황을 다시 만들었다");
        }

        // 대상: HolidayService.Search — RS1 이 0건인 경우
        // 목적: 05 §12.5 에서 RS1 0건은 성공이다. 실패로 바꾸면 조작자는 조회가 안 된 줄 알고
        //       다시 누르고, 그 기간에 휴무일이 없다는 사실을 못 배운다.
        // 확인: IsSuccess=true 이고 목록이 0건이다.
        [TestMethod]
        public void 목록이_0건이어도_성공이다()
        {
            var repository = new FakeHolidayRepository
            {
                SearchResult = new HolidayListReadDto
                {
                    Result = Ok(),
                    Rows = new List<HolidayListItemDto>(),
                    Registry = new HolidayRegistryDto(),
                },
            };

            OperationResult<HolidayListReadDto> result = new HolidayService(repository).Search(Request());

            Assert.IsTrue(result.IsSuccess, "0건을 실패로 바꿨다: " + result.Message);
            Assert.AreEqual(0, result.Value.Rows.Count);
        }

        // 대상: HolidayService.Search — RS1 자체가 오지 않은 경우
        // 목적: 화면이 null 목록을 Grid 에 바인딩하면 그 자리에서 터진다. 0건과 null 을 같은
        //       모양(빈 목록)으로 내보내 화면이 한 가지만 다루게 한다.
        // 확인: IsSuccess=true 이고 Rows 가 null 이 아니라 0건짜리 목록이다.
        [TestMethod]
        public void RS1_이_없으면_빈_목록으로_채운다()
        {
            var repository = new FakeHolidayRepository
            {
                SearchResult = new HolidayListReadDto
                {
                    Result = Ok(),
                    Rows = null,
                    Registry = new HolidayRegistryDto(),
                },
            };

            OperationResult<HolidayListReadDto> result = new HolidayService(repository).Search(Request());

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Value.Rows, "화면이 null 을 Grid 에 바인딩하게 된다");
            Assert.AreEqual(0, result.Value.Rows.Count);
        }

        // 대상: HolidayService.Search — RS0 성공인데 RS2 등재현황이 오지 않은 경우
        // 목적: 05 §12.5 에서 RS2 는 **항상 1행**이다. 없다는 것은 계약 위반이며, 성공으로
        //       넘기면 화면이 잔여일수 없이 경고를 판단하게 된다 — 등재가 곧 끊긴다는 신호를
        //       조작자가 못 받는다.
        // 확인: IsSuccess=false 이고 사유가 빈 문자열이 아니다.
        [TestMethod]
        public void RS0_성공인데_등재현황이_없으면_실패다()
        {
            var repository = new FakeHolidayRepository
            {
                SearchResult = new HolidayListReadDto { Result = Ok(), Rows = Rows(), Registry = null },
            };

            OperationResult<HolidayListReadDto> result = new HolidayService(repository).Search(Request());

            Assert.IsFalse(result.IsSuccess, "RS2 가 없는데 성공으로 읽었다");
            Assert.AreNotEqual(string.Empty, result.Message);
        }

        // 대상: HolidayService.Search — RS0 가 실패로 온 경우
        // 목적: 05 §3.1 · §4.3 에서 실패 사유의 문장은 DB 가 갖는다. Service 가 자기 문장으로
        //       바꿔 쓰면 같은 결과코드에 두 가지 안내가 생긴다.
        // 확인: IsSuccess=false 이고 메시지가 DB 가 준 문장 그대로다.
        [TestMethod]
        public void RS0_실패면_그_메시지로_실패한다()
        {
            var repository = new FakeHolidayRepository
            {
                SearchResult = new HolidayListReadDto
                {
                    Result = new DbResult
                    {
                        Success = false,
                        Code = (int)DbCode.BadValue,
                        Message = "입력값이 올바르지 않습니다.",
                    },
                },
            };

            OperationResult<HolidayListReadDto> result = new HolidayService(repository).Search(Request());

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("입력값이 올바르지 않습니다.", result.Message);
        }

        // 대상: HolidayService.Search — RS0 자체를 못 읽은 경우
        // 목적: 05 §3.1 에서 RS0 는 모든 SP 가 정확히 1행 낸다. 없다는 것은 계약이 깨졌다는
        //       뜻이므로 성공으로 넘기지 않는다.
        // 확인: IsSuccess=false 이고 메시지가 화면용 문장이다.
        [TestMethod]
        public void RS0_을_못_읽으면_실패다()
        {
            var repository = new FakeHolidayRepository { SearchResult = new HolidayListReadDto() };

            OperationResult<HolidayListReadDto> result = new HolidayService(repository).Search(Request());

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("휴무일을 조회하지 못했습니다.", result.Message);
        }

        // 대상: HolidayService.Register · Update — R22 가 합친 SP-HOL-02 의 동작코드 분기
        // 목적: 05 §12.6 에서 등록과 수정이 SP 하나가 되며 `@휴무동작코드` 가 생겼다. 어느
        //       쪽인지를 Service 가 정해 넘기는데, 둘이 같은 코드로 가면 등록이 수정으로
        //       처리되어 없는 행을 고치려 들거나 그 반대가 된다. 실물에서 오타를 잡아 주는
        //       것은 `SelectRepositoryDbTests` 의 101 시험뿐이고, 이 분기 자체는 여기서만 본다.
        // 확인: Register 는 CREATE, Update 는 UPDATE 로 Repository 에 간다.
        [TestMethod]
        public void 등록과_수정은_서로_다른_동작코드로_간다()
        {
            var repository = new FakeHolidayRepository { SaveResult = Saved() };
            var service = new HolidayService(repository);

            service.Register(Save("센터 휴진일"));
            Assert.AreEqual(DbHolidayAction.Create, repository.LastAction, "등록이 CREATE 로 가지 않았다");

            service.Update(Save("센터 휴진일"));
            Assert.AreEqual(DbHolidayAction.Update, repository.LastAction, "수정이 UPDATE 로 가지 않았다");
        }

        // 대상: HolidayService.Register — 05 §12.6 의 @휴무일명 NVARCHAR(100) 길이 검증
        // 목적: 킷 §6 은 SP 계약의 길이를 Service 에서 한 번만 본다. 화면의 MaxLength 는 UI
        //       제한이지 검증이 아니므로, 붙여넣기로 들어온 긴 값은 여기서만 걸린다. 그대로
        //       보내면 DB 가 자르고 잘린 이름이 휴무일 목록에 남는다.
        // 확인: 101자면 IsSuccess=false 이고 Repository 가 아예 호출되지 않는다.
        [TestMethod]
        public void 휴무일명이_계약_길이를_넘으면_SP_에_가지_않는다()
        {
            var repository = new FakeHolidayRepository { SaveResult = Saved() };

            OperationResult<HolidaySaveReadDto> result =
                new HolidayService(repository).Register(Save(new string('가', DbSize.HolidayName + 1)));

            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(repository.LastRequest, "길이가 넘쳤는데 SP 를 불렀다");
        }

        // 대상: HolidayService.Register — @비고 NVARCHAR(500) 길이 검증
        // 목적: 휴무일명과 같은 이유다. 검증 대상이 둘인데 하나만 보면 나머지가 조용히 잘린다.
        // 확인: 501자 비고면 IsSuccess=false 이고 Repository 가 호출되지 않는다.
        [TestMethod]
        public void 비고가_계약_길이를_넘으면_SP_에_가지_않는다()
        {
            var repository = new FakeHolidayRepository { SaveResult = Saved() };
            HolidaySaveRequest request = Save("센터 휴진일");
            request.Memo = new string('나', DbSize.HolidayMemo + 1);

            OperationResult<HolidaySaveReadDto> result = new HolidayService(repository).Register(request);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(repository.LastRequest, "길이가 넘쳤는데 SP 를 불렀다");
        }

        // 대상: HolidayService.Register — 길이 검증이 세는 단위
        // 목적: 앞뒤 공백까지 세면 계약이 허락한 이름이 화면에서 막힌다. 정규화는 C# 이 먼저
        //       하므로(05 §2.2) 세는 것도 걷어 낸 뒤의 길이여야 한다.
        // 확인: 공백을 뺀 본문이 딱 100자면 통과해 SP 까지 간다.
        [TestMethod]
        public void 길이는_앞뒤_공백을_걷어_낸_뒤로_센다()
        {
            var repository = new FakeHolidayRepository { SaveResult = Saved() };

            OperationResult<HolidaySaveReadDto> result = new HolidayService(repository)
                .Register(Save("  " + new string('다', DbSize.HolidayName) + "  "));

            Assert.IsTrue(result.IsSuccess, "공백까지 세어 막았다: " + result.Message);
            Assert.IsNotNull(repository.LastRequest);
        }

        // 대상: HolidayService.Register — 801·802 처럼 DB 가 낸 업무 실패
        // 목적: 03 §24.5 「최종 판정은 DB」 — 날짜 중복(801)과 법정공휴일 편집(802)은 SP 가 낸
        //       업무 판정이지 호출 실패가 아니다. Service 가 실패로 접으면 화면이 사유를 읽을
        //       길이 사라지고, 목록을 다시 읽어 남의 변경을 보여 주는 경로도 함께 끊긴다.
        // 확인: 801 이 온 결과도 IsSuccess=true 이고 결과코드 801 이 화면까지 올라간다.
        [TestMethod]
        public void 업무_실패_결과코드도_화면까지_올려보낸다()
        {
            var repository = new FakeHolidayRepository
            {
                SaveResult = new HolidaySaveReadDto
                {
                    Result = new DbResult
                    {
                        Success = false,
                        Code = (int)DbCode.HolidayDuplicate,
                        Message = "이미 등록된 휴무일입니다.",
                    },
                },
            };

            OperationResult<HolidaySaveReadDto> result =
                new HolidayService(repository).Register(Save("센터 휴진일"));

            Assert.IsTrue(result.IsSuccess, "DB 판정을 받아 왔는데 실패로 접었다");
            Assert.AreEqual((int)DbCode.HolidayDuplicate, result.Value.Result.Code);
        }

        // 대상: HolidayService.Delete — 자체휴무일 삭제(SP-HOL-04) 전달값
        // 목적: 05 §12.8 에서 삭제는 물리 삭제이고 행버전으로 남의 변경을 가른다. 행버전을
        //       잃으면 그 사이 남이 고친 행을 그대로 지운다 (06 §26).
        // 확인: 날짜와 행버전이 바이트 단위로 Repository 까지 간다.
        [TestMethod]
        public void 삭제는_날짜와_행버전을_그대로_싣는다()
        {
            var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var repository = new FakeHolidayRepository { DeleteResult = Saved() };

            new HolidayService(repository).Delete(Day, rowVersion);

            Assert.AreEqual(Day, repository.LastDeleteDate);
            CollectionAssert.AreEqual(rowVersion, repository.LastDeleteRowVersion, "행버전을 안 실었다");
        }

        // 대상: HolidayService.Register · Update · Delete — RS0 자체를 못 읽은 경우
        // 목적: 05 §3.1 에서 RS0 는 모든 SP 가 정확히 1행 낸다. 없는데 성공으로 넘기면 저장이
        //       안 됐는데 된 것처럼 보이고, 화면은 목록을 다시 읽어 옛 값을 정상으로 그린다.
        //       세 경로가 각자 자기 문장을 갖는 것도 함께 잰다 — 무엇이 실패했는지 구분된다.
        // 확인: 셋 다 IsSuccess=false 이고 메시지가 등록·수정·삭제로 서로 다르다.
        [TestMethod]
        public void 쓰기_세_경로_모두_RS0_을_못_읽으면_실패다()
        {
            var repository = new FakeHolidayRepository
            {
                SaveResult = new HolidaySaveReadDto(),
                DeleteResult = new HolidaySaveReadDto(),
            };
            var service = new HolidayService(repository);

            Assert.IsFalse(service.Register(Save("센터 휴진일")).IsSuccess);
            Assert.AreEqual("휴무일을 등록하지 못했습니다.", service.Register(Save("센터 휴진일")).Message);
            Assert.AreEqual("휴무일을 수정하지 못했습니다.", service.Update(Save("센터 휴진일")).Message);
            Assert.AreEqual("휴무일을 삭제하지 못했습니다.", service.Delete(Day, new byte[8]).Message);
        }

        // ── helpers

        private static DbResult Ok()
        {
            return new DbResult { Success = true, Code = (int)DbCode.Ok, Message = "정상 처리되었습니다." };
        }

        private static HolidaySaveReadDto Saved()
        {
            return new HolidaySaveReadDto { Result = Ok(), HolidayDate = Day, RowVersion = new byte[8] };
        }

        private static HolidaySearchRequest Request()
        {
            return new HolidaySearchRequest { FromDate = Day, ToDate = Day.AddYears(2) };
        }

        private static HolidaySaveRequest Save(string name)
        {
            return new HolidaySaveRequest
            {
                HolidayDate = Day,
                HolidayName = name,
                IsActive = true,
                Memo = null,
                RowVersion = new byte[8],
            };
        }

        private static IList<HolidayListItemDto> Rows()
        {
            return new List<HolidayListItemDto>
            {
                new HolidayListItemDto { HolidayDate = Day, HolidayName = "센터 휴진일" },
                new HolidayListItemDto { HolidayDate = Day.AddDays(1), HolidayName = "성탄절" },
            };
        }

        /// <summary>실물 SP 대신 답을 미리 쥐고 있는 Repository. 받은 값을 그대로 붙들어 둔다.</summary>
        private sealed class FakeHolidayRepository : IHolidayRepository
        {
            public HolidayListReadDto SearchResult { get; set; }
            public HolidaySaveReadDto SaveResult { get; set; }
            public HolidaySaveReadDto DeleteResult { get; set; }

            public HolidaySaveRequest LastRequest { get; private set; }
            public string LastAction { get; private set; }
            public DateTime? LastDeleteDate { get; private set; }
            public byte[] LastDeleteRowVersion { get; private set; }

            public HolidayListReadDto Search(HolidaySearchRequest request)
            {
                return SearchResult;
            }

            public HolidaySaveReadDto Save(HolidaySaveRequest request, string action)
            {
                LastRequest = request;
                LastAction = action;
                return SaveResult;
            }

            public HolidaySaveReadDto Delete(DateTime holidayDate, byte[] rowVersion)
            {
                LastDeleteDate = holidayDate;
                LastDeleteRowVersion = rowVersion;
                return DeleteResult;
            }
        }
    }
}
