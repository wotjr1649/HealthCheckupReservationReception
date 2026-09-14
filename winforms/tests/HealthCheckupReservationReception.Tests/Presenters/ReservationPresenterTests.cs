using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Presenters
{
    /// <summary>
    /// WF-RSV-01 신규 예약 (03 §8).
    ///
    /// 재는 것은 **판정이 아니라 진행 단계와 배선**이다 — 정원·마감·TGT·AEX 가용성·저장가능은
    /// DB 가 내고, 이 계층이 그것을 접거나 다시 세지 않는지를 본다.
    /// </summary>
    [TestClass]
    public class ReservationPresenterTests
    {
        private static readonly DateTime Day = new DateTime(2026, 9, 14);
        private const long PatientId = 1000;

        // ── 03 §8.5 진행 상태

        // 대상: ReservationPresenter (WF-RSV-01) — 수검자 확정 전의 화면 상태
        // 목적: 03 §8.5 — 수검자가 확정되기 전에는 물어볼 대상이 없다. 일정이 열려 있으면
        //       수검자 없이 SP-RSV-01 이 나가고, 저장이 열려 있으면 빈 예약이 저장된다.
        // 확인: 일정영역·추가검사·저장이 모두 닫혀 있고 대상판정이 「미판정」이다.
        [TestMethod]
        public void 최초에는_일정과_AEX_와_저장이_닫혀_있다()
        {
            var view = new FakeReservationView();
            new ReservationPresenter(view, new FakeReservationService(), new FakePatientService(), Works(), "창구");

            Assert.IsFalse(view.ScheduleEnabled, "수검자 확정 전에는 일정영역이 닫혀 있다");
            Assert.IsFalse(view.AexEnabled);
            Assert.IsFalse(view.SaveEnabled);
            Assert.AreEqual("대상판정 : 미판정", view.TargetText);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 부모가 넘긴 수검자 행으로 여는 진입
        // 목적: R21 로 부모(WF-PAT-01)가 목록 RS1 에서 받아 둔 행을 그대로 넘긴다. 여기서 다시
        //       조회하면 같은 값을 두 번 읽고 그동안 화면이 멈춘다. 다만 일정영역을 연 뒤에는
        //       정원을 보여야 하므로 가용성은 한 번 물어야 한다.
        // 확인: 차트번호가 서고 일정영역이 열리며 가용성 조회가 정확히 1회다.
        [TestMethod]
        public void 전달키로_들어오면_수검자가_확정되고_일정이_열린다()
        {
            var view = new FakeReservationView();
            var patients = Patients();
            var service = new FakeReservationService { Availability = Availability() };
            var presenter = new ReservationPresenter(view, service, patients, Works(), "창구");

            presenter.Begin(PatientId, Patient());

            Assert.AreEqual("C000001", view.Patient.ChartNo);
            Assert.IsTrue(view.ScheduleEnabled);
            Assert.AreEqual(1, service.AvailabilityCalls, "일정영역을 연 뒤 한 번은 물어야 정원이 보인다");
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 지난 노쇼(RSV) 만 있는 수검자의 진입
        // 목적: 지난 예약이 RSV 인 채로 남아 있어도 오늘·미래 예약을 막지 않는다 (00 RP-06
        //       「과거 업무는 중복판단에서 제외한다」, 2026-09-11 사용자 지시). 조회범위가
        //       「예약일 >= 오늘」이라 지난 건은 RS1 에 아예 없고, 화면이 받는 그림은 「유효업무
        //       없음」이다. 그때 일정영역이 열려야 그 사람이 다시 예약할 수 있다.
        // 확인: 일정영역이 열리고 Workbench 로 보내지 않으며 가용성 조회가 1회다.
        [TestMethod]
        public void 지난_미접수_예약만_있으면_일정이_열린다()
        {
            var view = new FakeReservationView();
            var patients = Patients();
            var service = new FakeReservationService { Availability = Availability() };
            var presenter = new ReservationPresenter(view, service, patients, Works(), "창구");

            presenter.Begin(PatientId, Patient());

            Assert.IsTrue(view.ScheduleEnabled, "지난 노쇼 하나로 신규예약이 막혔다");
            Assert.IsNull(view.WorkbenchWorkId, "Workbench 로 보냈다");
            Assert.AreEqual(1, service.AvailabilityCalls);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 진입에서 수검자 관련 SP 를 부르지 않음 (R21)
        // 목적: 2026-09-14 사용자 지시 — 진입이 수검자 SP 를 한 번도 부르지 않는다. 예전에는
        //       상세와 유효예약 둘을 물었는데, 지금은 목록 SP RS1 이 그 두 답을 한 행에 싣고 온다.
        //       RP-06 판정이 화면으로 온 것이 아니다 — 유효업무가 채워지는 기준은 SP 안에 있다.
        // 확인: 수검자 상세 조회가 0회이고, 받아 온 차트번호로 화면이 서며 일정이 열린다.
        [TestMethod]
        public void 받아_온_수검자로_열면_SP_를_부르지_않는다()
        {
            var view = new FakeReservationView();
            var patients = Patients();
            var service = new FakeReservationService { Availability = Availability() };
            var presenter = new ReservationPresenter(view, service, patients, Works(), "창구");

            presenter.Begin(PatientId, Patient());

            Assert.AreEqual(0, patients.DetailCalls, "받아 온 행이 있는데 수검자를 읽었다");
            Assert.AreEqual("C000001", view.Patient.ChartNo);
            Assert.IsTrue(view.ScheduleEnabled);
        }
        // 대상: ReservationPresenter (WF-RSV-01) — 넘어온 행의 수검자ID 가 대상과 어긋난 경우
        // 목적: 넘어온 값과 대상이 어긋나면 엉뚱한 사람의 예약이 된다. 예전에는 그때 상세 SP 로
        //       메웠지만 R21 로 그 SP 가 사라졌고, 목록을 거치지 않고 이 창을 여는 길도 없다
        //       (03 §3 진입점은 하나다). 그러니 메우는 대신 멈추고 말한다.
        // 확인: 화면에 수검자가 서지 않고 일정이 닫힌 채이며 가용성을 조회하지 않고,
        //       안내에 「목록에서 다시」가 들어 있다.
        [TestMethod]
        public void 다른_수검자의_상세가_오면_열지_않는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService { Availability = Availability() };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");

            presenter.Begin(PatientId, new PatientDto { PatientId = PatientId + 1, ChartNo = "C000999" });

            Assert.IsNull(view.Patient, "화면에 다른 사람이 섰다");
            Assert.IsFalse(view.ScheduleEnabled, "어긋난 채로 일정이 열렸다");
            Assert.AreEqual(0, service.AvailabilityCalls, "열지 않을 것을 조회했다");
            StringAssert.Contains(view.BlockMessage, "목록에서 다시", "왜 안 열리는지 말하지 않았다");
        }
        // ── 2026-09-11: DLG-RSV-01 예약 변경 (03 §10)

        // 대상: ReservationPresenter (DLG-RSV-01 예약 변경) — 변경 모드 진입
        // 목적: 같은 화면이 모드만 바꾼다. 진입값이 업무 상세면 변경이고, 기존 유효예약 확인을
        //       거치지 않는다 — 그 판정은 「새 예약을 만들 수 있는가」이고 변경은 이미 있는 그
        //       예약을 고치는 일이다 (중복은 SP 가 현재 Work 를 빼고 본다). 행버전을 안 실으면
        //       남의 변경을 덮어쓴다.
        // 확인: 제목이 「예약 변경」이고 수검자가 상세에서 채워지며 일정이 열리고,
        //       가용성 조회에 업무ID 55 와 행버전이 실리고 수검자 SP 는 0회다.
        [TestMethod]
        public void 변경으로_열면_업무ID_와_행버전을_실어_묻는다()
        {
            var view = new FakeReservationView();
            var patients = Patients();
            var service = new FakeReservationService { Availability = Availability() };
            var presenter = new ReservationPresenter(view, service, patients, Works(), "창구");

            presenter.BeginChange(Works().DetailResult.Value);

            Assert.AreEqual("예약 변경", view.Title);
            Assert.AreEqual("C000001", view.Patient.ChartNo, "수검자를 상세에서 채우지 않았다");
            Assert.IsTrue(view.ScheduleEnabled);
            Assert.AreEqual(55L, service.LastAvailability.WorkId);
            Assert.IsNotNull(service.LastAvailability.RowVersion, "행버전을 안 실었다");
            Assert.AreEqual(0, patients.DetailCalls, "변경인데 수검자를 다시 읽었다");
        }

        // 대상: ReservationPresenter (DLG-RSV-01) — 변경 모드의 저장 경로
        // 목적: 05 §11.2 는 원하는 최종 상태를 통째로 보내고 변경범위는 DB 가 재도록 정했다.
        //       신규등록 SP 로 가면 같은 사람에게 예약이 하나 더 생긴다.
        // 확인: 신규등록 SP 가 불리지 않고 예약변경 SP 로 업무ID 55 와 조작자명이 간다.
        [TestMethod]
        public void 변경_저장은_예약변경_SP_로_간다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = Availability(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = new DbResult { Success = true, Code = 0, Message = "정상 처리되었습니다." },
                    Row = new WorkSaveResultDto { WorkId = 55, StatusCode = "RSV", RowVersion = new byte[8] },
                }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");
            presenter.BeginChange(Works().DetailResult.Value);

            view.RaiseSaveRequested();

            Assert.IsNull(service.LastSave, "변경인데 신규등록 SP 를 불렀다");
            Assert.IsNotNull(service.LastChange);
            Assert.AreEqual(55L, service.LastChange.WorkId);
            Assert.AreEqual("창구", service.LastChange.OperatorName);
        }

        // 대상: ReservationPresenter (DLG-RSV-01) — 변경 진입 시 검사구성 초기 표시
        // 목적: 2026-09-12 사용자 보고. 변경 진입은 변경범위=NONE 이라 SP-RSV-01 이 RS2~RS5 를
        //       전부 0행으로 준다 (05 §9.11). 그래서 시간대·국가검사·추가검사가 통째로 비었고,
        //       추가검사 목록이 없으니 「일정은 그대로 두고 추가검사만 변경」에 닿을 길이 없었다.
        //       이제 부모가 넘긴 상세가 저장된 검사구성을 먼저 세운다.
        // 확인: 상세를 다시 읽지 않고도 국가검사 1건·추가검사 2건이 서고 추가검사 영역이 열린다.
        [TestMethod]
        public void 변경으로_열면_저장된_검사구성이_먼저_선다()
        {
            var view = new FakeReservationView();
            var works = Works();
            var presenter = new ReservationPresenter(
                view, new FakeReservationService { Availability = NoChange() }, Patients(), works, "창구");

            presenter.BeginChange(Works().DetailResult.Value);

            // 2026-09-14 — 진입은 부모가 준 한 벌로 연다. 예전에는 여기서 SP-WRK-02 를
            // 스스로 불렀고, 부모가 같은 것을 방금 받아 둔 뒤였다.
            Assert.AreEqual(0, works.DetailCalls, "진입에서 상세를 다시 읽었다");
            Assert.IsNotNull(view.NexItems);
            Assert.AreEqual(1, view.NexItems.Count, "NEX 가 비었다");
            Assert.IsNotNull(view.AexItems);
            Assert.AreEqual(2, view.AexItems.Count, "AEX 목록이 비어 고를 것이 없다");
            Assert.IsTrue(view.AexEnabled, "AEX 가 닫혀 있어 추가검사만 변경할 수 없다");
        }

        // 대상: ReservationPresenter (DLG-RSV-01) — 예약일을 바꾸지 않은 응답의 해석
        // 목적: 03 §10.3 「TGT/NEX/AEX 유지」 — 0행은 「없음」이 아니라 「이번엔 재평가하지
        //       않았다」다. 예약일을 바꾸지 않은 응답은 RS2·RS3·RS4 가 0행으로 오는데 (05 §9.11)
        //       그것으로 시간대와 국가검사를 지우면 화면이 다시 빈다.
        // 확인: 국가검사 건수가 진입 때 그대로 남고, 시간대는 새 응답으로 2건으로 갱신되며,
        //       저장이 열려 있다.
        [TestMethod]
        public void 시간대만_바꾼_응답이_NEX_를_지우지_않는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService { Availability = NoChange() };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");
            presenter.BeginChange(Works().DetailResult.Value);

            int nexBefore = view.NexItems.Count;
            Assert.AreEqual(1, nexBefore, "진입에서 NEX 가 서지 않았다");

            // SLOT — RS2 만 차고 RS3·RS4·RS5 는 0행이다 (05 §9.11).
            ReservationAvailabilityReadDto slotOnly = NoChange();
            slotOnly.Summary.SlotChanged = true;
            slotOnly.Summary.SlotCode = "PM";
            slotOnly.Summary.CanSave = true;
            slotOnly.Slots = Availability().Slots;
            service.Availability = slotOnly;

            view.SlotCode = "PM";
            view.RaiseScheduleChanged();

            Assert.AreEqual(nexBefore, view.NexItems.Count, "NEX 가 지워졌다");
            Assert.AreEqual(2, view.Slots.Count, "시간대는 새 응답으로 갱신돼야 한다");
            Assert.IsTrue(view.SaveEnabled);
        }

        // 대상: ReservationPresenter (DLG-RSV-01) — 일정은 그대로 두고 추가검사만 바꾸는 길
        // 목적: 2026-09-12 사용자 보고. 추가검사 체크는 SP 를 다시 부르지 않으므로 저장가능 이
        //       진입 때 값 0 에 머문다. 버튼을 그 값에만 매어 두면 골라도 누를 수가 없어
        //       「일정 그대로, 추가검사만 변경」이 영영 막힌다. 03 §10.3 이 준 두 길 중 「No-op
        //       안내」를 골라, 막을 이유가 없으면 열고 SP 가 저장 시점에 판정한다.
        // 확인: 저장이 열려 있고, 저장하면 예약변경 SP 로 고른 추가검사가 실리며 예약일·시간대는
        //       진입 값 그대로 간다.
        [TestMethod]
        public void 바꾼_것이_없어도_저장은_열려_있고_AEX_선택이_그대로_실린다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = NoChange(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = Ok(),
                    Row = new WorkSaveResultDto { WorkId = 55, StatusCode = "RSV", RowVersion = new byte[8] },
                }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");
            presenter.BeginChange(Works().DetailResult.Value);

            Assert.IsTrue(view.SaveEnabled, "AEX 를 고를 수 있는데 저장이 닫혀 있다");

            // 조작자가 첫 추가검사를 켠다 — 화면은 SP 를 다시 부르지 않는다.
            view.AexSelection = new[] { true, false, false, false, false, false, false };
            view.RaiseSaveRequested();

            Assert.IsNotNull(service.LastChange, "예약변경 SP 를 부르지 않았다");
            Assert.IsTrue(service.LastChange.AexSelected[0], "고른 AEX 가 실리지 않았다");
            Assert.AreEqual(Day, service.LastChange.ReserveDate, "일정을 건드리지 않았어야 한다");
            Assert.AreEqual("AM", service.LastChange.SlotCode);
        }

        // 대상: ReservationPresenter (DLG-RSV-01) — 결과코드 1(No-op) 처리
        // 목적: 05 §11.2 에서 정말로 바꾼 것이 없으면 SP 가 결과코드 1 로 돌려준다. 성공이지만
        //       저장이 아니므로 Workbench 로 넘기면 조작자는 바뀐 줄 안다.
        // 확인: Workbench 로 데려가지 않고 안내에 「변경된 내용이 없습니다」가 선다.
        [TestMethod]
        public void 저장했는데_No_op_이면_데려가지_않고_사유를_적는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = NoChange(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = new DbResult { Success = true, Code = 1, Message = "변경된 내용이 없습니다." },
                    Row = new WorkSaveResultDto { WorkId = 55, StatusCode = "RSV", RowVersion = new byte[8] },
                }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");
            presenter.BeginChange(Works().DetailResult.Value);

            view.RaiseSaveRequested();

            Assert.IsNull(view.WorkbenchWorkId, "No-op 인데 Workbench 로 데려갔다");
            StringAssert.Contains(view.BlockMessage, "변경된 내용이 없습니다");
        }

        // 대상: ReservationPresenter (DLG-RSV-01) — 변경 진입 시 시간대 두 줄 구성
        // 목적: 05 §9.7 은 시간대를 AM/PM 정확히 2행으로 못박는다. 변경 진입에서는 SP 가 0행을
        //       주므로 화면이 두 줄을 세운다 — 지금 시간대의 정원은 상세 RS1 이 주고 반대쪽은
        //       아직 모르므로 정원 0(모름)으로 둔다. 반대쪽을 고를 수 없으면 시간대를 못 바꾼다.
        // 확인: 시간대가 2건이고 AM·PM 순서이며, 지금 시간대의 정원은 20 이고 반대쪽은 0 이되
        //       반대쪽도 고를 수 있다.
        [TestMethod]
        public void 변경으로_열면_시간대가_두_줄_선다()
        {
            var view = new FakeReservationView();
            var presenter = new ReservationPresenter(
                view, new FakeReservationService { Availability = NoChange() }, Patients(), Works(), "창구");

            presenter.BeginChange(Works().DetailResult.Value);

            Assert.IsNotNull(view.Slots);
            Assert.AreEqual(2, view.Slots.Count, "시간대를 고를 자리가 없다");
            Assert.AreEqual("AM", view.Slots[0].SlotCode);
            Assert.AreEqual("PM", view.Slots[1].SlotCode);
            Assert.AreEqual(20, view.Slots[0].Capacity, "지금 시간대의 정원은 상세가 준다");
            Assert.AreEqual(0, view.Slots[1].Capacity, "아직 모르는 정원은 0 이다");
            Assert.IsTrue(view.Slots[1].Selectable, "반대쪽을 고를 수 없으면 시간대를 못 바꾼다");
        }

        // 대상: ReservationPresenter (DLG-RSV-01) — 변경 진입 직후의 안내 문구
        // 목적: 03 §10.3 「없음 → 저장 Disabled 또는 No-op 안내」에서 05 §9.6 은 그 상태의
        //       차단메시지를 빈 문자열로 정했다. 비어 있으면 화면이 메운다 — 캡처에서 저장이 왜
        //       꺼져 있는지 알 수 없던 자리다 (2026-09-12 사용자 보고).
        // 확인: 안내에 「아직 바꾼 것이 없습니다」가 들어 있다.
        [TestMethod]
        public void 아직_바꾼_것이_없으면_그_사실을_적는다()
        {
            var view = new FakeReservationView();
            var presenter = new ReservationPresenter(
                view, new FakeReservationService { Availability = NoChange() }, Patients(), Works(), "창구");

            presenter.BeginChange(Works().DetailResult.Value);

            StringAssert.Contains(view.BlockMessage, "아직 바꾼 것이 없습니다");
        }

        // 대상: ReservationPresenter (DLG-RSV-01) — 예약일 미변경 시 대상판정 문구
        // 목적: 예약일을 건드리지 않으면 대상판정(RS3)이 0행으로 온다. 「미판정」으로 적으면
        //       비대상처럼 읽히므로 언제 판정되는지를 적어 조작자가 기다릴 줄 알게 한다.
        // 확인: 대상판정 문구에 「예약일을 바꾸면」이 들어 있다.
        [TestMethod]
        public void 예약일을_안_바꾸면_대상판정_대신_언제_판정하는지_적는다()
        {
            var view = new FakeReservationView();
            var presenter = new ReservationPresenter(
                view, new FakeReservationService { Availability = NoChange() }, Patients(), Works(), "창구");

            presenter.BeginChange(Works().DetailResult.Value);

            StringAssert.Contains(view.TargetText, "예약일을 바꾸면");
        }

        // 대상: ReservationPresenter (DLG-RSV-01) — 부모가 빈손으로 연 경우
        // 목적: 진입이 조회를 하지 않게 된 뒤로는 「받은 것이 없을 때」가 곧 열 수 없는 때다
        //       (2026-09-14). 스스로 SP 를 불러 메우면 진입 계약이 깨지고, 그대로 열면 대상 없는
        //       변경이 나간다.
        // 확인: 가용성도 상세도 부르지 않고 일정이 닫힌 채이며 안내에 「업무 상세」가 들어 있다.
        [TestMethod]
        public void 받은_것이_없으면_변경_진입을_접는다()
        {
            var view = new FakeReservationView();
            var works = new FakeWorkService();
            var service = new FakeReservationService { Availability = NoChange() };
            var presenter = new ReservationPresenter(view, service, Patients(), works, "창구");

            presenter.BeginChange(null);

            Assert.AreEqual(0, service.AvailabilityCalls, "상세도 없이 가용성을 물었다");
            Assert.AreEqual(0, works.DetailCalls, "빈손인데 SP 를 불렀다");
            Assert.IsFalse(view.ScheduleEnabled);
            StringAssert.Contains(view.BlockMessage, "업무 상세");
        }

        /// <summary>변경 진입의 실제 응답 — `변경범위=NONE`, RS2~RS5 전부 0행 (05 §9.11).</summary>
        private static ReservationAvailabilityReadDto NoChange()
        {
            ReservationAvailabilityReadDto read = Availability();
            read.Summary.WorkId = 55;
            read.Summary.DateChanged = false;
            read.Summary.SlotChanged = false;
            read.Summary.AexChanged = false;
            read.Summary.CanSave = false;
            read.Summary.BlockMessage = string.Empty;
            read.Slots = new List<SlotInfoDto>();
            read.Target = null;
            read.NexItems = new List<WorkExamItemDto>();
            read.AexItems = new List<ReservationAexItemDto>();
            return read;
        }

        /// <summary>
        /// `SP-WRK-02` 를 대신하는 fake. **예약 변경 진입은 이 조회로 화면을 채운다** —
        /// 05 §8.2 RS2(NEX)·RS5(추가검사구성 7행)·RS1(정원)을 한 번에 준다.
        /// </summary>
        private static FakeWorkService Works()
        {
            return Works(new List<WorkActionDto>());
        }

        /// <summary>
        /// RS4 의 `START_RECEPTION` 한 행만 골라 세운다. 그 사유코드가 「이 예약일이 오늘인가」를
        /// 말하므로(`503` 이면 오늘이 아니다), 착지를 재는 시험이 이것으로 갈린다.
        /// </summary>
        private static FakeWorkService WorksWithReception(int reasonCode)
        {
            return Works(new List<WorkActionDto>
            {
                new WorkActionDto
                {
                    ActionCode = DbWorkAction.StartReception,
                    Allowed = reasonCode == 0,
                    ReasonCode = reasonCode,
                    ReasonMessage = string.Empty,
                },
            });
        }

        private static FakeWorkService Works(IList<WorkActionDto> actions)
        {
            return new FakeWorkService
            {
                DetailResult = OperationResult<WorkDetailReadDto>.Success(new WorkDetailReadDto
                {
                    Result = Ok(),
                    Detail = Work(),
                    NexItems = new List<WorkExamItemDto>
                    {
                        new WorkExamItemDto { ExamItemCode = "EX001", ExamItemName = "문진/진찰", NexType = "BASIC" },
                    },
                    AexItems = new List<WorkExamItemDto>(),
                    Actions = actions,
                    AexOptions = new List<ReservationAexItemDto>
                    {
                        new ReservationAexItemDto { AexCode = "OPT01", ExamItemName = "복부초음파", Selectable = true, Requested = false, ReasonMessage = string.Empty },
                        new ReservationAexItemDto { AexCode = "OPT02", ExamItemName = "갑상선초음파", Selectable = true, Requested = false, ReasonMessage = string.Empty },
                    },
                }),
            };
        }

        private static WorkDetailDto Work()
        {
            return new WorkDetailDto
            {
                WorkId = 55,
                PatientId = PatientId,
                ChartNo = "C000001",
                Name = "홍길동",
                Birthday = "19800101",
                Gender = "M",
                ReserveDate = Day,
                SlotCode = "AM",
                StatusCode = "RSV",
                Capacity = 20,
                CurrentCount = 12,
                RemainingSeats = 8,
                RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            };
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 유효예약이 있는 수검자의 신규예약 진입
        // 목적: 03 §8.5 — 기존 유효예약이 있으면 신규예약을 중단하고 그 건으로 Workbench 로
        //       간다 (00 RP-06). 이어 가면 정원이 한 사람에게 두 자리 나가고, 접을 것을 조회하면
        //       쓸모없는 SP 왕복이 한 번 생긴다.
        // 확인: 그 업무ID 55 로 예약 Workbench 를 가리키고 일정영역이 열리지 않으며 가용성을
        //       조회하지 않는다.
        [TestMethod]
        public void 기존_유효예약이_있으면_신규예약을_접고_Workbench_로_간다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService { Availability = Availability() };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");

            presenter.Begin(PatientId, Booked(
                new PatientValidWorkDto { WorkId = 55, ReserveDate = Day, StatusCode = "RSV" }));

            Assert.AreEqual(55L, view.WorkbenchWorkId);
            Assert.AreEqual(WorkContext.Reservation, view.WorkbenchContext);
            Assert.IsFalse(view.ScheduleEnabled, "신규예약을 이어 가면 안 된다");
            Assert.AreEqual(0, service.AvailabilityCalls, "접을 것을 조회했다");
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 조회 응답의 다른업무ID (05 §9.6)
        // 목적: 03 §8.5 — 진입 시점에는 없었는데 조회 시점에 다른 창구가 예약을 넣었을 수 있다.
        //       그 경우에도 같은 길로 접어야 중복예약이 생기지 않는다.
        // 확인: 조회가 준 업무ID 77 로 Workbench 를 가리킨다.
        [TestMethod]
        public void 조회가_다른업무ID_를_주면_그때도_Workbench_로_간다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Summary.OtherWorkId = 77;
            var service = new FakeReservationService { Availability = read };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");

            presenter.Begin(PatientId, Patient());

            Assert.AreEqual(77L, view.WorkbenchWorkId);
        }

        // ── 05 §9.12 — `저장가능` 은 DB 것이다

        // 대상: ReservationPresenter (WF-RSV-01) — 저장 버튼 활성화와 차단 사유 표시
        // 목적: 05 §9.12 에서 저장가능 은 SP-RSV-01 이 정원·마감·TGT·AEX 를 모두 본 뒤 내는 답이다.
        //       화면이 그 조건을 다시 계산하면 판정이 두 곳이 되고, 둘이 어긋나는 날 조작자는
        //       버튼이 왜 닫혔는지 알 수 없다.
        // 확인: 저장가능=1 이면 저장 버튼이 열린다. 일정을 바꿔 저장가능=0 · 차단코드=305(정원초과)
        //       가 오면 버튼이 닫히고 DB 가 준 차단메시지가 화면에 그대로 뜬다.
        [TestMethod]
        public void 저장_버튼은_DB_의_저장가능_그대로다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Summary.CanSave = true;
            var presenter = Loaded(view, read);

            Assert.IsTrue(view.SaveEnabled);

            read.Summary.CanSave = false;
            read.Summary.BlockCode = (int)DbCode.SlotFull;
            read.Summary.BlockMessage = "해당 시간대의 정원이 찼습니다.";
            // [X] 여기는 **물어본 날과 다른 날**이면 된다 — 특정 날짜가 아니다 (2026-09-15 실측).
            //     `Ask` 는 `_askedDate` 와 같으면 되돌아가므로, 고정 상수를 쓰면 화면 기본값
            //     (`DateTime.Today`) 이 그 상수와 같아지는 하루에만 시험이 조용히 빨강이 된다.
            //     실제로 `Day.AddDays(1)` 이 오늘이 된 날 터졌다.
            view.ReserveDate = view.ReserveDate.AddDays(1);
            view.RaiseScheduleChanged();

            Assert.IsFalse(view.SaveEnabled);
            Assert.AreEqual("해당 시간대의 정원이 찼습니다.", view.BlockMessage, "차단 사유가 보이지 않는다");
        }

        // ── 03 §8.7 대상판정 문구

        // 대상: ReservationPresenter (WF-RSV-01) — 완료이력이 없는 대상자의 판정 문구
        // 목적: 03 §8.7 — 판정문구는 화면 계산결과이며 DB 컬럼으로 저장하지 않는다. 완료이력이
        //       없는 것과 판정을 못 한 것은 다르므로 그 둘을 같은 문구로 적지 않는다.
        // 확인: 대상판정이 「대상 — 최초검진」이다.
        [TestMethod]
        public void 최초검진이면_대상_최초검진이다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Target = new ExamTargetDto { IsTarget = true, Age = 27, LastCompletedDate = null };
            Loaded(view, read);

            Assert.AreEqual("대상판정 : 대상 — 최초검진", view.TargetText);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 완료이력이 있는 대상자의 판정 문구
        // 목적: 00 TGT-04 의 2년 주기가 업무 규칙이므로, 조작자가 보는 문구에 최근 완료연도가
        //       있어야 「왜 지금 대상인가」를 화면에서 바로 읽는다.
        // 확인: 대상판정이 「대상 — 최근 완료연도 2024」다.
        [TestMethod]
        public void 완료이력이_있으면_최근_완료연도를_적는다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Target = new ExamTargetDto
            {
                IsTarget = true,
                Age = 46,
                LastCompletedDate = new DateTime(2024, 5, 11),
            };
            Loaded(view, read);

            Assert.AreEqual("대상판정 : 대상 — 최근 완료연도 2024", view.TargetText);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 비대상 사유 문구
        // 목적: 비대상 사유는 화면이 짓지 않는다 — DB 가 준 사유메시지를 그대로 붙인다. 화면이
        //       지으면 400·401 같은 사유코드가 늘 때마다 문구가 두 곳이 된다.
        // 확인: 대상판정이 「비대상 — 예약일 기준 만 20세 미만입니다.」로 DB 문장 그대로다.
        [TestMethod]
        public void 비대상이면_DB_가_준_사유를_그대로_붙인다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Target = new ExamTargetDto
            {
                IsTarget = false,
                Age = 19,
                ReasonCode = (int)DbCode.UnderAge,
                ReasonMessage = "예약일 기준 만 20세 미만입니다.",
            };
            Loaded(view, read);

            Assert.AreEqual("대상판정 : 비대상 — 예약일 기준 만 20세 미만입니다.", view.TargetText);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 비대상일 때의 추가검사 영역
        // 목적: 03 §8.5 — 비대상이면 추가검사를 고를 수 없다. 열어 두면 조작자가 고른 뒤 저장
        //       시점에 막혀 고른 것을 잃는다.
        // 확인: 추가검사 영역이 닫힌다.
        [TestMethod]
        public void 비대상이면_AEX_를_닫는다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Target = new ExamTargetDto { IsTarget = false, ReasonMessage = "2년 주기가 도래하지 않았습니다." };
            Loaded(view, read);

            Assert.IsFalse(view.AexEnabled);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 일정 평가가 불가능할 때의 대상판정
        // 목적: 05 §9.8 에서 일정 평가가 불가능하면 RS3 이 0행이다. 그 0행을 「비대상」으로
        //       읽으면 아직 묻지 않은 것을 답이 나온 것처럼 보여 주게 된다.
        // 확인: 대상판정이 「미판정」이고 추가검사가 닫혀 있다.
        [TestMethod]
        public void 일정을_아직_못_잡으면_미판정이다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.Target = null;   // 05 §9.8 — 일정 평가가 불가능하면 0행이다
            Loaded(view, read);

            Assert.AreEqual("대상판정 : 미판정", view.TargetText);
            Assert.IsFalse(view.AexEnabled);
        }

        // ── 03 §8.10 AEX 유지 규칙

        // 대상: ReservationPresenter (WF-RSV-01) — 예약일 변경 시 추가검사 선택의 유지
        // 목적: 03 §8.10 — 예약일이 바뀌면 만나이가 바뀌어 국가검사 구성이 달라지고, 그에 따라
        //       고를 수 있는 추가검사도 달라진다. 무엇이 살아남았는지는 DB 의 유효선택여부가
        //       알려 주며, 화면이 임의로 유지하면 SP 가 411·412 로 막는 조합이 화면에 남는다.
        // 확인: 인정받지 못한 선택은 해제되고 인정된 선택만 체크된 채 남는다.
        [TestMethod]
        public void 예약일이_바뀌면_유효_선택만_남는다()
        {
            var view = new FakeReservationView();
            ReservationAvailabilityReadDto read = Availability();
            read.AexItems = new List<ReservationAexItemDto>
            {
                // 요청은 했지만 DB 가 인정하지 않았다 — 새 예약일에서 성별·중복으로 막힌 것.
                new ReservationAexItemDto { AexCode = "OPT01", Requested = true, EffectiveSelected = false, Selectable = false },
                new ReservationAexItemDto { AexCode = "OPT02", Requested = true, EffectiveSelected = true, Selectable = true },
            };
            Loaded(view, read);

            Assert.IsFalse(view.AexItems[0].Requested, "인정받지 못한 선택이 남았다");
            Assert.IsTrue(view.AexItems[1].Requested);
        }

        // ── 조회 횟수

        // 대상: ReservationPresenter (WF-RSV-01) — 같은 예약일·시간대 재조회 억제
        // 목적: DateEdit 은 글자를 칠 때마다 값이 바뀐다. 같은 일정으로 동기 SP 를 되풀이해
        //       부르면 타이핑하는 내내 화면이 얼어붙는다.
        // 확인: 같은 예약일·시간대로 이벤트가 여러 번 올라와도 가용성 조회가 1회에 머문다.
        [TestMethod]
        public void 같은_일정으로는_다시_묻지_않는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService { Availability = Availability() };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");
            presenter.Begin(PatientId, Patient());
            Assert.AreEqual(1, service.AvailabilityCalls);

            view.RaiseScheduleChanged();
            view.RaiseScheduleChanged();

            Assert.AreEqual(1, service.AvailabilityCalls, "같은 예약일·시간대로 SP 를 다시 불렀다");
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 시간대 변경 시 재조회
        // 목적: 05 §9.7 에서 정원과 마감은 시간대마다 다르다. 시간대를 바꾸고 묻지 않으면
        //       AM 값으로 PM 을 저장하러 가고, 정원이 찬 시간대에 예약이 나간다.
        // 확인: 시간대를 PM 으로 바꾸면 가용성 조회가 2회가 되고 마지막 조회의 시간대가 PM 이다.
        [TestMethod]
        public void 시간대를_고르면_다시_묻는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService { Availability = Availability() };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");
            presenter.Begin(PatientId, Patient());

            view.SlotCode = "PM";
            view.RaiseScheduleChanged();

            Assert.AreEqual(2, service.AvailabilityCalls);
            Assert.AreEqual("PM", service.LastAvailability.SlotCode);
        }

        // ── 03 §8.11 2단계 저장

        // 대상: ReservationPresenter (WF-RSV-01) — 저장 성공 뒤의 화면 초기화와 착지
        // 목적: 03 §8.10 — 저장한 값이 화면에 남아 있으면 다음 수검자에게 앞 사람 값이 섞인다.
        //       저장 뒤에 갈 곳이 예약 Workbench 인 것은 방금 만든 건을 확인하는 자리이기 때문이다.
        // 확인: 새 업무ID 91 로 예약 Workbench 를 가리키고, 수검자·저장 버튼이 초기화되며,
        //       저장 요청에 조작자명이 실렸다.
        [TestMethod]
        public void 저장에_성공하면_전체를_비우고_예약_Workbench_로_간다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = Availability(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = Ok(),
                    Row = new WorkSaveResultDto { WorkId = 91, StatusCode = "RSV", RowVersion = new byte[8] },
                }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");
            presenter.Begin(PatientId, Patient());

            view.RaiseSaveRequested();

            Assert.AreEqual(91L, view.WorkbenchWorkId);
            Assert.AreEqual(WorkContext.Reservation, view.WorkbenchContext);
            Assert.IsNull(view.Patient, "03 §8.10 저장 성공 — 전체 Clear");
            Assert.IsFalse(view.SaveEnabled);
            Assert.AreEqual("창구", service.LastSave.OperatorName);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 일반예약 마감 후의 예약구분 판정
        // 목적: 00 RP-05 — 현장 내원자는 당일예약 마감 전이면 일반, 그 뒤 접수 마감 전까지는
        //       현장 당일예약이다. 같은 사람·같은 행동이고 시각만 다르다. 조작자에게 시계를
        //       읽히면 10:00 직전·직후에 틀린 구분이 기록되므로, 화면이 일반으로 묻고 304
        //       마감경과로 막혔을 때만 현장으로 한 번 더 묻는다.
        // 확인: 가용성 조회가 2회(일반 한 번, 현장 한 번)이고, 마지막 조회와 저장의 예약구분이
        //       모두 현장 당일예약이다.
        [TestMethod]
        public void 마감이_지나면_현장_당일예약으로_한_번_더_묻는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = CutoffAvailability(),
                WalkInAvailability = TodayAvailability(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = Ok(),
                    Row = new WorkSaveResultDto { WorkId = 92, StatusCode = "RSV", RowVersion = new byte[8] },
                }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");

            presenter.Begin(PatientId, Patient());

            Assert.AreEqual(2, service.AvailabilityCalls, "일반으로 한 번, 현장으로 한 번이다");
            Assert.AreEqual(DbReserveType.WalkIn, service.LastAvailability.ReserveType);

            view.RaiseSaveRequested();

            Assert.AreEqual(DbReserveType.WalkIn, service.LastSave.ReserveType);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 마감 전의 예약구분 판정
        // 목적: 되묻는 것은 마감으로 막혔을 때뿐이다. 늘 두 번 물으면 SP 왕복이 배로 늘고
        //       마감 전인데도 현장 당일예약으로 기록될 길이 생긴다.
        // 확인: 가용성 조회가 1회이고 예약구분이 일반 예약이다.
        [TestMethod]
        public void 마감_전이면_일반으로_한_번만_묻는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService { Availability = TodayAvailability() };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");

            presenter.Begin(PatientId, Patient());

            Assert.AreEqual(1, service.AvailabilityCalls);
            Assert.AreEqual(DbReserveType.Normal, service.LastAvailability.ReserveType);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 저장 뒤 착지 창구 판정
        // 목적: 2026-09-11 grilling — 저장 뒤 착지는 예약구분이 아니라 날짜로 가른다. 오늘 건은
        //       바로 접수로 이어지기 때문이다. 오늘인지를 화면이 PC 시계로 재지 않는 것도 규칙이다:
        //       마감시각은 예약일이 오늘일 때만 채워지므로 마감시각이 있다는 것이 곧 오늘이라는
        //       뜻이다.
        // 확인: 일반 예약으로 저장해도 오늘 건이면 접수 Workbench 로 간다.
        [TestMethod]
        public void 오늘_예약은_예약구분과_무관하게_접수_Workbench_로_간다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = TodayAvailability(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = Ok(),
                    Row = new WorkSaveResultDto { WorkId = 92, StatusCode = "RSV", RowVersion = new byte[8] },
                }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");
            presenter.Begin(PatientId, Patient());

            view.RaiseSaveRequested();

            Assert.AreEqual(WorkContext.Reception, view.WorkbenchContext);
            Assert.AreEqual(DbReserveType.Normal, service.LastSave.ReserveType,
                "일반 예약이어도 오늘이면 다음 할 일은 접수다");
        }

        // 대상: ReservationPresenter (DLG-RSV-01) — 추가검사만 바꾼 오늘 건의 착지
        // 목적: 2026-09-14 수정. 예약일을 안 바꾸면 시간대정보가 끝까지 비어 있어 마감시각으로는
        //       「오늘인가」를 판정할 수 없었고, 그래서 오늘 건이 예약 Workbench 로 떨어졌다
        //       (session-20 §6). DB 오늘날짜를 따로 묻지 않는다 — 진입에서 이미 받는 RS4 의
        //       START_RECEPTION 사유코드가 그 답이라 SP 왕복이 늘지 않는다.
        // 확인: 일정을 건드리지 않고 추가검사만 바꿔 저장해도 접수 Workbench 로 간다.
        [TestMethod]
        public void 오늘_건은_일정을_안_바꿔도_접수_Workbench_로_간다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = NoChange(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = Ok(),
                    Row = new WorkSaveResultDto { WorkId = 55, StatusCode = "RSV", RowVersion = new byte[8] },
                }),
            };
            var presenter = new ReservationPresenter(
                view, service, Patients(), WorksWithReception((int)DbCode.Ok), "창구");
            presenter.BeginChange(WorksWithReception((int)DbCode.Ok).DetailResult.Value);

            view.AexSelection = new[] { true, false, false, false, false, false, false };
            view.RaiseSaveRequested();

            Assert.AreEqual(WorkContext.Reception, view.WorkbenchContext,
                "오늘 건인데 예약 Workbench 로 떨어졌다 — 다음에 할 일은 접수다");
        }

        // 대상: ReservationPresenter — 접수 마감이 지난 오늘 건의 착지
        // 목적: 마감이 지났다는 것은 그 날이 오늘이라는 뜻이다 — 503(오늘이 아님)을 이미
        //       통과했으므로 착지는 접수다. 마감을 「오늘이 아니다」로 읽으면 오늘 건이 예약
        //       창구로 떨어져 조작자가 한 번 더 옮겨야 한다.
        // 확인: 접수 Workbench 로 간다.
        [TestMethod]
        public void 마감이_지난_오늘_건도_접수_Workbench_로_간다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = NoChange(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = Ok(),
                    Row = new WorkSaveResultDto { WorkId = 55, StatusCode = "RSV", RowVersion = new byte[8] },
                }),
            };
            var presenter = new ReservationPresenter(
                view, service, Patients(), WorksWithReception((int)DbCode.CutoffPassed), "창구");
            presenter.BeginChange(WorksWithReception((int)DbCode.CutoffPassed).DetailResult.Value);

            view.AexSelection = new[] { true, false, false, false, false, false, false };
            view.RaiseSaveRequested();

            Assert.AreEqual(WorkContext.Reception, view.WorkbenchContext);
        }

        // 대상: ReservationPresenter — 미래 예약의 착지
        // 목적: 503 은 「오늘이 아니다」이고, 그때만 예약 Workbench 가 맞다. 위 두 갈래와 한
        //       묶음이라 셋을 함께 재야 판정이 뒤집히지 않는다.
        // 확인: 예약 Workbench 로 간다.
        [TestMethod]
        public void 오늘이_아닌_건은_예약_Workbench_로_간다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = NoChange(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = Ok(),
                    Row = new WorkSaveResultDto { WorkId = 55, StatusCode = "RSV", RowVersion = new byte[8] },
                }),
            };
            var presenter = new ReservationPresenter(
                view, service, Patients(), WorksWithReception((int)DbCode.NotToday), "창구");
            presenter.BeginChange(WorksWithReception((int)DbCode.NotToday).DetailResult.Value);

            view.AexSelection = new[] { true, false, false, false, false, false, false };
            view.RaiseSaveRequested();

            Assert.AreEqual(WorkContext.Reservation, view.WorkbenchContext);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 예약구분 문구
        // 목적: 05 §9.6 — 예약구분은 DB 가 돌려준 값을 그대로 읽어 준다. 화면이 시각으로 다시
        //       판정하면 저장된 값과 보이는 값이 달라진다.
        // 확인: 문구가 「현장 당일예약」이다.
        [TestMethod]
        public void 예약구분은_DB_가_돌려준_값을_그대로_적는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = CutoffAvailability(),
                WalkInAvailability = TodayAvailability(DbReserveType.WalkIn),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");

            presenter.Begin(PatientId, Patient());

            Assert.AreEqual("현장 당일예약", view.ReserveTypeText);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 시간대별 마감 판정 (H1 결함 회귀)
        // 목적: 2026-09-11 Deadline-Field-Check §4 의 결함이다. 예전에는 시간대 중 하나라도 304 면
        //       현장으로 넘어갔는데, 마감은 시간대마다 다르므로(AM 10:00 · PM 15:00) AM 이 마감이고
        //       PM 은 아직 마감 전인 구간에서 PM 예약이 현장으로 저장되었다. 막히지 않아 조용히
        //       틀린 구분이 기록됐다. 지금은 고른 시간대가 판정한다.
        // 확인: PM 을 고르면 가용성 조회가 1회이고 예약구분이 일반 예약으로 남는다.
        [TestMethod]
        public void PM_을_고르면_AM_이_마감이어도_일반으로_남는다()
        {
            ReservationAvailabilityReadDto normal = AmCutoffPmOpen();
            normal.Summary.SlotCode = "PM";

            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = normal,
                WalkInAvailability = TodayAvailability(DbReserveType.WalkIn),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");

            presenter.Begin(PatientId, Patient());

            Assert.AreEqual(1, service.AvailabilityCalls, "PM 은 아직 마감 전이라 되묻지 않는다");
            Assert.AreEqual(DbReserveType.Normal, service.LastAvailability.ReserveType);
            Assert.AreEqual("일반 예약", view.ReserveTypeText);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 시간대 미선택 상태의 최초 조회
        // 목적: 시간대를 아직 고르지 않은 최초 조회의 기준이다 — 하나라도 고를 수 있으면 일반이다.
        //       RS1 의 시간대코드는 보낸 값 그대로이고 미선택이면 NULL 이라 (05 §9.6) DB 가 대신
        //       골라 주지 않는다. 여기서 현장으로 넘기면 아직 고를 수 있는 시간대가 있는데도
        //       현장 당일예약으로 기록된다.
        // 확인: 가용성 조회가 1회이고 예약구분이 일반 예약이다.
        [TestMethod]
        public void 시간대_미선택이면_하나라도_열려_있는_동안은_일반이다()
        {
            ReservationAvailabilityReadDto normal = AmCutoffPmOpen();
            normal.Summary.SlotCode = null;

            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = normal,
                WalkInAvailability = TodayAvailability(DbReserveType.WalkIn),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");

            presenter.Begin(PatientId, Patient());

            Assert.AreEqual(1, service.AvailabilityCalls);
            Assert.AreEqual(DbReserveType.Normal, service.LastAvailability.ReserveType);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 시간대 미선택이고 AM·PM 둘 다 마감인 경우
        // 목적: 미선택이고 둘 다 마감이면 남은 길은 현장뿐이다. 되묻지 않으면 늦게 온 현장
        //       내원자가 예약할 자리를 잃는다 (00 RP-05).
        // 확인: 가용성 조회가 2회가 되고 예약구분이 현장 당일예약으로 바뀌며 문구도 따라간다.
        [TestMethod]
        public void 시간대_미선택이고_둘_다_마감이면_현장으로_묻는다()
        {
            ReservationAvailabilityReadDto normal = AmCutoffPmOpen();
            normal.Summary.SlotCode = null;
            normal.Slots[1].Selectable = false;
            normal.Slots[1].CutoffPassed = true;
            normal.Slots[1].BlockCode = (int)DbCode.CutoffPassed;
            normal.Slots[1].BlockMessage = "해당 시간대의 마감시간이 지났습니다.";

            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = normal,
                WalkInAvailability = TodayAvailability(DbReserveType.WalkIn),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");

            presenter.Begin(PatientId, Patient());

            Assert.AreEqual(2, service.AvailabilityCalls);
            Assert.AreEqual(DbReserveType.WalkIn, service.LastAvailability.ReserveType);
            Assert.AreEqual("현장 당일예약", view.ReserveTypeText);
        }

        /// <summary>AM 은 마감(304), PM 은 고를 수 있고 정원도 남은 상태.</summary>
        private static ReservationAvailabilityReadDto AmCutoffPmOpen()
        {
            ReservationAvailabilityReadDto read = CutoffAvailability();
            read.Slots[1].Selectable = true;
            read.Slots[1].CutoffPassed = false;
            read.Slots[1].BlockCode = (int)DbCode.Ok;
            read.Slots[1].BlockMessage = string.Empty;
            read.Slots[1].CurrentCount = 3;
            read.Slots[1].AppliedCount = 4;
            read.Slots[1].RemainingSeats = 16;
            return read;
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 유효예약 안내 후의 이동 확인
        // 목적: 03 §8.5 — 이 수검자로는 더 진행할 수 없다 (RP-06). 남은 선택은 「그 예약을 보러
        //       갈까」 하나이고, 묻고 간다 (2026-09-11 사용자 지시). 예전에는 알리고 곧바로
        //       데려갔는데, 명단을 연달아 예약하는 중이면 잘못 누른 한 번이 흐름을 끊는다.
        // 확인: 「아니오」면 창이 닫히고 Workbench 로 데려가지 않으며, 물음에 그 예약의
        //       일정(2026-09-14)이 들어 있다.
        [TestMethod]
        public void 기존_유효예약은_묻고_아니오면_그냥_닫는다()
        {
            var view = new FakeReservationView { ConfirmAnswer = false };
            var presenter = new ReservationPresenter(
                view, new FakeReservationService { Availability = Availability() }, Patients(), Works(), "창구");

            presenter.Begin(PatientId, Booked(new PatientValidWorkDto
            {
                WorkId = 77,
                ReserveDate = Day,
                SlotCode = "AM",
                StatusCode = "RSV",
            }));

            Assert.IsTrue(view.Dismissed, "아니오인데 창을 안 닫았다");
            Assert.IsNull(view.WorkbenchWorkId, "아니오인데 데려갔다");
            StringAssert.Contains(view.LastQuestion, "2026-09-14", "물음에 그 예약의 일정이 없다");
        }

        // 대상: ReservationPresenter — Workbench 로 넘길 때의 「저장에서 왔는가」 표시
        // 목적: 저장으로 생긴 건과 보러 가는 건은 착지 규칙이 다르다 — 저장 건은 그 날짜로 좁혀
        //       겨누고, 보러 가는 건은 그렇지 않다. 그 구분이 화면까지 가야 부모가 옳게 연다.
        // 확인: 저장으로 넘길 때 그 표시가 참으로 함께 올라간다.
        [TestMethod]
        public void 저장으로_생긴_건인지가_함께_올라간다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = Availability(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = Ok(),
                    Row = new WorkSaveResultDto { WorkId = 91, StatusCode = "RSV", RowVersion = new byte[8] },
                }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");
            presenter.Begin(PatientId, Patient());

            view.RaiseSaveRequested();

            Assert.AreEqual(true, view.WorkbenchFromSave);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 폐기 확인 여부의 기준
        // 목적: 03 §8.10 — 수검자가 확정된 순간부터 화면에는 조작자가 들인 것이 있고(일정·추가
        //       검사), 저장이 끝나면 초기화가 그것을 내린다. 확정 전에도 물으면 아무것도 안 한
        //       창을 닫을 때마다 확인창이 뜬다.
        // 확인: 확정 전에는 미저장 입력이 없다고 판정한다.
        [TestMethod]
        public void 수검자가_확정되기_전에는_폐기를_묻지_않는다()
        {
            var view = new FakeReservationView();
            var presenter = new ReservationPresenter(
                view, new FakeReservationService(), new FakePatientService(), Works(), "창구");

            Assert.IsFalse(presenter.HasUnsavedInput);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 확정 뒤의 미저장 입력 판정
        // 목적: 위와 한 쌍이다. 확정 뒤에는 화면에 입력값이 있으므로, 묻지 않고 닫으면 조작자가
        //       적은 것이 조용히 사라진다.
        // 확인: 확정 뒤에는 미저장 입력이 있다고 판정한다.
        [TestMethod]
        public void 수검자가_확정되면_폐기를_묻는다()
        {
            var view = new FakeReservationView();
            var presenter = new ReservationPresenter(
                view, new FakeReservationService { Availability = Availability() }, Patients(), Works(), "창구");

            presenter.Begin(PatientId, Patient());

            Assert.IsTrue(presenter.HasUnsavedInput);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 저장이 업무 판정으로 막힌 경우
        // 목적: 03 §8.11 실패 경로 — Commit 이 없으므로 Workbench 로 넘기면 안 되고, 일정·대상·
        //       검사구성을 최신값으로 다시 읽어야 조작자가 남이 채운 정원을 보고 다시 고른다.
        // 확인: DB 가 준 사유가 화면에 서고 Workbench 로 가지 않으며 가용성 조회가 한 번 더 난다.
        [TestMethod]
        public void 저장이_DB_에서_막히면_사유를_보이고_최신값을_다시_읽는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = Availability(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto
                {
                    Result = new DbResult
                    {
                        Success = false,
                        Code = (int)DbCode.SlotFull,
                        Message = "해당 시간대의 정원이 찼습니다.",
                    },
                }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");
            presenter.Begin(PatientId, Patient());
            int before = service.AvailabilityCalls;

            view.RaiseSaveRequested();

            Assert.AreEqual("해당 시간대의 정원이 찼습니다.", view.BlockMessage);
            Assert.IsNull(view.WorkbenchWorkId, "Commit 이 없는데 Workbench 로 갔다");
            Assert.AreEqual(before + 1, service.AvailabilityCalls, "최신값을 다시 읽지 않았다");
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 성공인데 RS1 이 없는 계약 위반
        // 목적: 05 §11 에서 성공이면 RS1 이 온다. 없으면 계약 위반이므로 업무ID 를 지어내지
        //       않는다 — 지어내면 없는 업무를 겨눈 Workbench 가 열린다.
        // 확인: Workbench 로 넘기지 않는다.
        [TestMethod]
        public void 성공인데_업무ID_가_없으면_Workbench_로_넘기지_않는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                Availability = Availability(),
                Save = OperationResult<WorkSaveReadDto>.Success(new WorkSaveReadDto { Result = Ok() }),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");
            presenter.Begin(PatientId, Patient());

            view.RaiseSaveRequested();

            Assert.IsNull(view.WorkbenchWorkId);
        }

        // 대상: ReservationPresenter (WF-RSV-01) — 조회에서 예외가 올라온 경우
        // 목적: 킷 §6 — provider 메시지는 DB 이름·서버명을 드러내므로 화면에 원문을 싣지 않는다.
        // 확인: 안내가 화면용 문장이고 예외에 든 서버명(DESKTOP…)이 문구에 없다.
        [TestMethod]
        public void 예외가_나도_예외_본문을_화면에_싣지_않는다()
        {
            var view = new FakeReservationView();
            var service = new FakeReservationService
            {
                AvailabilityFailure = new InvalidOperationException("서버 DESKTOP-XYZ 의 로그인에 실패했습니다"),
            };
            var presenter = new ReservationPresenter(view, service, Patients(), Works(), "창구");

            presenter.Begin(PatientId, Patient());

            Assert.AreEqual("예약 가능정보를 조회하지 못했습니다.", view.BlockMessage);
            StringAssert.DoesNotMatch(view.BlockMessage, new System.Text.RegularExpressions.Regex("DESKTOP"));
        }

        // ── helpers

        private static ReservationPresenter Loaded(FakeReservationView view, ReservationAvailabilityReadDto read)
        {
            var presenter = new ReservationPresenter(
                view, new FakeReservationService { Availability = read }, Patients(), Works(), "창구");
            presenter.Begin(PatientId, Patient());
            return presenter;
        }

        /// <summary>
        /// [R21] 목록 SP(`SP-PAT-01`) RS1 한 행. 부모가 이것을 그대로 넘긴다.
        /// `ValidWork` 가 `null` 이면 **예약 가능**이고, 그 판정은 SP 가 이미 냈다.
        /// </summary>
        private static PatientDto Patient()
        {
            return new PatientDto
            {
                PatientId = PatientId,
                ChartNo = "C000001",
                Name = "홍길동",
                Birthday = "19800101",
                Gender = "M",
            };
        }

        /// <summary>이미 유효예약이 있는 그 행 (00 RP-06). 신규예약은 여기서 접힌다.</summary>
        private static PatientDto Booked(PatientValidWorkDto work)
        {
            PatientDto row = Patient();
            row.ValidWork = work;
            return row;
        }

        private static FakePatientService Patients()
        {
            return new FakePatientService
            {
                DetailResult = OperationResult<PatientDto>.Success(new PatientDto
                {
                    PatientId = PatientId,
                    ChartNo = "C000001",
                    Name = "홍길동",
                    Birthday = "19800101",
                    Gender = "M",
                }),
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

        /// <summary>오늘이다 — 마감시각이 채워져 있고 아직 지나지 않았다.</summary>
        private static ReservationAvailabilityReadDto TodayAvailability()
        {
            return TodayAvailability(DbReserveType.Normal);
        }

        private static ReservationAvailabilityReadDto TodayAvailability(string reserveType)
        {
            ReservationAvailabilityReadDto read = Availability();
            read.Summary.ReserveType = reserveType;
            read.Slots[0].CutoffTime = new TimeSpan(10, 0, 0);
            read.Slots[1].CutoffTime = new TimeSpan(15, 0, 0);
            return read;
        }

        /// <summary>오늘인데 AM 마감이 지났다 — 일반으로는 못 넣는다 (05 §4.2 `304`).</summary>
        private static ReservationAvailabilityReadDto CutoffAvailability()
        {
            ReservationAvailabilityReadDto read = TodayAvailability();
            read.Slots[0].Selectable = false;
            read.Slots[0].CutoffPassed = true;
            read.Slots[0].BlockCode = (int)DbCode.CutoffPassed;
            read.Slots[0].BlockMessage = "해당 시간대의 마감시간이 지났습니다.";
            return read;
        }

        private static ReservationAvailabilityReadDto Availability()
        {
            return new ReservationAvailabilityReadDto
            {
                Result = Ok(),
                Summary = new ReservationSummaryDto
                {
                    ChangeScope = "ALL",
                    PatientId = PatientId,
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
                    new WorkExamItemDto { ExamItemCode = "EX001", ExamItemName = "문진/진찰", NexType = "BASIC" },
                },
                AexItems = new List<ReservationAexItemDto>
                {
                    new ReservationAexItemDto { AexCode = "OPT01", ExamItemName = "복부초음파", Selectable = true, ReasonMessage = string.Empty },
                },
            };
        }
    }

    internal sealed class FakeReservationView : IReservationView
    {
        public event EventHandler ScheduleChanged;
        public event EventHandler SaveRequested;

        public FakeReservationView()
        {
            AexSelection = new bool[ReservationAvailabilityRequest.AexParameterCount];
            ReserveDate = DateTime.Today;

            // 예전 동작(묻지 않고 데려간다)과 같은 기본값이라 기존 시험이 그대로 산다.
            ConfirmAnswer = true;
        }

        public string Title { get; set; }
        public PatientDto Patient { get; set; }
        public bool ScheduleEnabled { get; set; }
        public DateTime ReserveDate { get; set; }
        public string ReserveTypeText { get; set; }
        public IList<SlotInfoDto> Slots { get; set; }
        public string SlotCode { get; set; }
        public string TargetText { get; set; }
        public IList<WorkExamItemDto> NexItems { get; set; }
        public IList<ReservationAexItemDto> AexItems { get; set; }
        public bool AexEnabled { get; set; }
        public bool[] AexSelection { get; set; }
        public bool SaveEnabled { get; set; }
        public string BlockMessage { get; set; }
        public string LastMessage { get; private set; }

        public WorkContext? WorkbenchContext { get; private set; }
        public long? WorkbenchWorkId { get; private set; }
        public bool? WorkbenchFromSave { get; private set; }
        public bool Dismissed { get; private set; }

        /// <summary>03 §8.5 의 「예약 관리로 갈까요」 에 어떻게 답할 것인가.</summary>
        public bool ConfirmAnswer { get; set; }
        public string LastQuestion { get; private set; }

        public void GoToWorkbench(WorkContext context, long workId, bool fromSave)
        {
            WorkbenchContext = context;
            WorkbenchWorkId = workId;
            WorkbenchFromSave = fromSave;
        }

        public void Dismiss() { Dismissed = true; }

        public void ShowMessage(string message) { LastMessage = message; }

        public bool Confirm(string message)
        {
            LastQuestion = message;
            return ConfirmAnswer;
        }

        public void RaiseScheduleChanged()
        {
            EventHandler handler = ScheduleChanged;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }

        public void RaiseSaveRequested()
        {
            EventHandler handler = SaveRequested;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }
    }

    internal sealed class FakeReservationService : IReservationService
    {
        public ReservationAvailabilityReadDto Availability { get; set; }

        /// <summary>예약구분=WALKIN 으로 물었을 때의 답. 없으면 <see cref="Availability"/> 를 낸다.</summary>
        public ReservationAvailabilityReadDto WalkInAvailability { get; set; }

        public OperationResult<WorkSaveReadDto> Save { get; set; }
        public Exception AvailabilityFailure { get; set; }

        public int AvailabilityCalls { get; private set; }
        public ReservationAvailabilityRequest LastAvailability { get; private set; }
        public ReservationSaveRequest LastSave { get; private set; }

        public OperationResult<ReservationAvailabilityReadDto> GetAvailability(ReservationAvailabilityRequest request)
        {
            if (AvailabilityFailure != null)
            {
                throw AvailabilityFailure;
            }

            AvailabilityCalls++;
            LastAvailability = request;

            ReservationAvailabilityReadDto read =
                DbReserveType.WalkIn.Equals(request.ReserveType, StringComparison.Ordinal) && WalkInAvailability != null
                    ? WalkInAvailability
                    : Availability;

            return read == null
                ? OperationResult<ReservationAvailabilityReadDto>.Failure("없다")
                : OperationResult<ReservationAvailabilityReadDto>.Success(read);
        }

        public ReservationChangeRequest LastChange { get; private set; }
        public WorkActionRequest LastCancel { get; private set; }

        public OperationResult<WorkSaveReadDto> Change(ReservationChangeRequest request)
        {
            LastChange = request;
            return Save ?? OperationResult<WorkSaveReadDto>.Failure("없다");
        }

        public OperationResult<WorkSaveReadDto> Cancel(WorkActionRequest request)
        {
            LastCancel = request;
            return Save ?? OperationResult<WorkSaveReadDto>.Failure("없다");
        }

        public OperationResult<WorkSaveReadDto> Register(ReservationSaveRequest request)
        {
            LastSave = request;
            return Save ?? OperationResult<WorkSaveReadDto>.Failure("없다");
        }
    }
}
