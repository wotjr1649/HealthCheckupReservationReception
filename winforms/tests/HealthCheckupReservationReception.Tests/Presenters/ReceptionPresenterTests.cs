using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Presenters
{
    /// <summary>
    /// DLG-RCP-01 접수 처리 (03 §11).
    ///
    /// [X] **접수 가능 여부를 화면이 계산하지 않는다.** 05 §8.2 RS4 의 `START_RECEPTION` 이
    ///     허용여부와 사유를 함께 주고 차단 우선순위(`502 → 308/309 → 503 → 304`)도 DB 가
    ///     갖는다. 03 §11.3 의 다섯 조건을 화면에 다시 적으면 판정이 두 곳이 된다 —
    ///     시험도 **DB 가 준 값을 그대로 그리는가**만 본다.
    /// </summary>
    [TestClass]
    public class ReceptionPresenterTests
    {
        // 대상: ReceptionPresenter (DLG-RCP-01) — 진입 시 부모가 넘긴 상세 한 벌의 사용
        // 목적: 2026-09-14 사용자 지시로 진입에서 SP 를 부르지 않는다. 부모(WF-WRK-01)가 행을
        //       고를 때 이미 SP-WRK-02 로 여섯을 받았고, 같은 업무ID 로 같은 값을 다시 읽으면
        //       창이 열리는 동안 화면이 한 번 더 멈춘다.
        // 확인: 상세 조회가 0회이고, 받아 온 값으로 차트번호가 서며, 접수 버튼이 열리고
        //       판정 문구가 「접수 가능」이다.
        [TestMethod]
        public void 열면_받아온_한_벌로_RS4_그대로_버튼을_연다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService { DetailResult = Detail(Allowed(true, 0, string.Empty)) };

            new ReceptionPresenter(view, service, "접수1번창구").Begin(service.DetailResult.Value);

            Assert.AreEqual(0, service.DetailCalls, "진입에서 상세를 다시 읽었다");
            Assert.AreEqual("C000001", view.Detail.ChartNo);
            Assert.IsTrue(view.ReceiveEnabled);
            Assert.AreEqual("접수 가능", view.EligibilityText);
        }

        // 대상: ReceptionPresenter (DLG-RCP-01) — RS4 가 접수 불가를 준 경우
        // 목적: 05 §8.2 RS4 가 허용여부와 사유를 함께 준다. 03 §11.3 의 다섯 조건을 화면에 다시
        //       적으면 판정이 두 곳이 되고 차단 우선순위(502 → 308/309 → 503 → 304)도 둘이 된다.
        // 확인: 접수 버튼이 닫히고 문구가 「접수 불가 — 접수 마감시각이 지났습니다.」다 —
        //       사유 문장이 DB 것 그대로다.
        [TestMethod]
        public void 불가면_DB_가_준_사유를_그대로_붙이고_버튼을_닫는다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService
            {
                DetailResult = Detail(Allowed(false, 304, "접수 마감시각이 지났습니다.")),
            };

            new ReceptionPresenter(view, service, "접수1번창구").Begin(service.DetailResult.Value);

            Assert.IsFalse(view.ReceiveEnabled);
            Assert.AreEqual("접수 불가 — 접수 마감시각이 지났습니다.", view.EligibilityText);
        }

        // 대상: ReceptionPresenter (DLG-RCP-01) — 사유코드는 있는데 사유메시지가 빈 경우
        // 목적: 이유 없이 닫힌 버튼은 조작자에게 고장으로 읽힌다. 메시지가 비어 있어도 코드라도
        //       보여야 무엇을 물어볼지 알 수 있다.
        // 확인: 판정 문구에 사유코드 503 이 들어 있다.
        [TestMethod]
        public void 사유메시지가_비면_사유코드라도_보인다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService { DetailResult = Detail(Allowed(false, 503, string.Empty)) };

            new ReceptionPresenter(view, service, "접수1번창구").Begin(service.DetailResult.Value);

            StringAssert.Contains(view.EligibilityText, "503");
        }

        // 대상: ReceptionPresenter (DLG-RCP-01) — 접수 성공 시의 전달값과 창 닫기
        // 목적: 창이 남아 있으면 조작자가 같은 건을 한 번 더 접수하려 든다 — 그때 돌아오는 것은
        //       502 이고, 성공한 일을 실패로 기억하게 된다. 행버전을 실어야 남이 그 사이 바꾼
        //       경우를 DB 가 잡는다.
        // 확인: 업무ID 77 · 조작자명 · 행버전이 모두 전달되고, 성공하면 창이 닫힌다.
        [TestMethod]
        public void 접수에_성공하면_창을_닫는다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService
            {
                DetailResult = Detail(Allowed(true, 0, string.Empty)),
                SaveResult = Saved(true, 0, "정상 처리되었습니다."),
            };
            new ReceptionPresenter(view, service, "접수1번창구").Begin(service.DetailResult.Value);

            view.RaiseReceive();

            Assert.AreEqual(77L, service.LastReception.WorkId);
            Assert.AreEqual("접수1번창구", service.LastReception.OperatorName);
            Assert.IsNotNull(service.LastReception.RowVersion, "행버전을 안 실었다");
            Assert.IsTrue(view.Closed, "성공했는데 창이 안 닫혔다");
        }

        // 대상: ReceptionPresenter (DLG-RCP-01) — 접수가 601 행버전 충돌로 막힌 경우
        // 목적: 막힌 뒤에는 최신값을 다시 읽어야 조작자가 남이 바꾼 상태를 보고 판단할 수 있다.
        //       그런데 다시 읽기가 ValidationMessage 를 지우므로 순서가 뒤집히면 사유가 사라진다 —
        //       WF-RSV-01 이 실제로 밟은 함정이라 순서를 시험으로 고정한다.
        // 확인: 창이 열린 채 남고, 사유에 「다른 사용자가」가 들어 있으며, 상세 조회가 한 번 더
        //       불린다 (사유가 지워지지 않은 채로).
        [TestMethod]
        public void DB_가_막으면_사유가_남고_창은_열려_있다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService
            {
                DetailResult = Detail(Allowed(true, 0, string.Empty)),
                SaveResult = Saved(false, 601, "다른 사용자가 예약·접수 업무를 변경했습니다."),
            };
            new ReceptionPresenter(view, service, "접수1번창구").Begin(service.DetailResult.Value);
            int before = service.DetailCalls;

            view.RaiseReceive();

            Assert.IsFalse(view.Closed);
            StringAssert.Contains(view.ValidationMessage, "다른 사용자가");
            Assert.IsTrue(service.DetailCalls > before, "막힌 뒤 최신값을 다시 읽지 않았다");
        }

        // 대상: ReceptionPresenter (DLG-RCP-01) — 부모가 빈손으로 창을 연 경우
        // 목적: 진입이 조회를 하지 않게 된 뒤로는 「받은 것이 없을 때」가 곧 열 수 없는 때다.
        //       그때 스스로 SP 를 불러 메우면 진입 계약이 깨지고, 버튼을 열어 두면 대상 없는
        //       접수가 나간다.
        // 확인: 상세가 null 이고 접수 버튼이 닫히며, SP 를 부르지 않았다 (DetailCalls=0).
        [TestMethod]
        public void 받은_것이_없으면_비우고_버튼을_닫는다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService();

            new ReceptionPresenter(view, service, "접수1번창구").Begin(null);

            Assert.IsNull(view.Detail);
            Assert.IsFalse(view.ReceiveEnabled);
            Assert.AreEqual(0, service.DetailCalls, "빈손인데 SP 를 불렀다");
        }

        // 대상: ReceptionPresenter (DLG-RCP-01) — RS4 에 START_RECEPTION 행이 없는 경우
        // 목적: 05 §8.2 RS4 는 정확히 5행이고 코드가 고정이다. 없다는 것은 계약 위반이며,
        //       화면이 그것을 「허용」으로 기본값 삼으면 DB 가 판단하지 않은 접수가 나간다.
        // 확인: 접수 버튼이 닫히고 안내 메시지가 선다 — 조용히 넘기지 않는다.
        [TestMethod]
        public void RS4_에_접수_행이_없으면_계약_위반이다()
        {
            var view = new FakeReceptionView();
            var service = new FakeWorkService { DetailResult = Detail(new List<WorkActionDto>()) };

            new ReceptionPresenter(view, service, "접수1번창구").Begin(service.DetailResult.Value);

            Assert.IsFalse(view.ReceiveEnabled);
            Assert.IsNotNull(view.ValidationMessage);
        }

        private static IList<WorkActionDto> Allowed(bool allowed, int reasonCode, string reasonMessage)
        {
            return new List<WorkActionDto>
            {
                new WorkActionDto
                {
                    ActionCode = DbWorkAction.StartReception,
                    Allowed = allowed,
                    ReasonCode = reasonCode,
                    ReasonMessage = reasonMessage,
                },
            };
        }

        private static OperationResult<WorkDetailReadDto> Detail(IList<WorkActionDto> actions)
        {
            return OperationResult<WorkDetailReadDto>.Success(new WorkDetailReadDto
            {
                Result = new DbResult { Success = true, Code = 0, Message = "정상 처리되었습니다." },
                Detail = new WorkDetailDto
                {
                    WorkId = 77,
                    PatientId = 1000,
                    ChartNo = "C000001",
                    Name = "홍길동",
                    Birthday = "19800101",
                    Gender = "M",
                    ReserveDate = new DateTime(2026, 9, 11),
                    SlotCode = "AM",
                    StatusCode = DbWorkStatus.Reserved,
                    Capacity = 20,
                    CurrentCount = 3,
                    RemainingSeats = 17,
                    RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                },
                NexItems = new List<WorkExamItemDto>(),
                AexItems = new List<WorkExamItemDto>(),
                Actions = actions,
            });
        }

        private static OperationResult<WorkSaveReadDto> Saved(bool success, int code, string message)
        {
            return OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
            {
                Result = new DbResult { Success = success, Code = code, Message = message },
                Row = success
                    ? new WorkSaveResultDto { WorkId = 77, StatusCode = DbWorkStatus.Received, RowVersion = new byte[8] }
                    : null,
            });
        }
    }

    internal sealed class FakeReceptionView : IReceptionView
    {
        public event EventHandler ReceiveRequested;

        public WorkDetailDto Detail { get; set; }
        public IList<WorkExamItemDto> NexItems { get; set; }
        public IList<WorkExamItemDto> AexItems { get; set; }
        public string EligibilityText { get; set; }
        public bool ReceiveEnabled { get; set; }
        public string ValidationMessage { get; set; }
        public bool Closed { get; private set; }

        public void Done() { Closed = true; }

        public void RaiseReceive()
        {
            EventHandler handler = ReceiveRequested;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }
    }
}
