using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using DevExpress.XtraEditors;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Tests.Presenters;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// WF-RSV-01 화면 자체의 규칙 — 03 §8.10 폐기 확인.
    ///
    /// [X] **이 시험이 사람의 손을 대신한다** (2026-09-11 사용자 지적). 캡처 시험이 폼을 닫을
    ///     때 `입력한 내용이 저장되지 않았습니다` 창이 떠 사용자가 직접 Yes 를 눌러야 회귀가
    ///     돌았다. 손이 필요한 것은 단위시험이 아니다. 폼이 묻는 길을 `Confirm` 하나로 모으고
    ///     그것을 덮어쓴 <see cref="SilentReservationForm"/> 으로 답을 미리 정해 둔다.
    /// </summary>
    [TestClass]
    public class FrmReservationTests
    {
        private const long PatientId = 1000;

        // 대상: FrmReservation (WF-RSV-01) — 입력값이 있는 상태에서 창을 닫을 때의 확인
        // 목적: 수검자가 확정된 뒤에는 화면에 조작자가 들인 값이 있다. 묻지 않고 닫으면 그것이
        //       조용히 사라진다. 「한 번」인 것도 규칙이다 — 두 번 물으면 확인창이 겹쳐 뜨고,
        //       캡처 회귀에서는 사람이 직접 Yes 를 눌러야 해 자동 회귀가 멈춘다 (2026-09-11).
        // 확인: 닫으면 확인창이 정확히 1회 뜨고 문구에 「저장되지 않았습니다」가 들어 있으며,
        //       Yes 를 고르면 폼이 실제로 닫힌다.
        [TestMethod]
        public void 수검자가_확정된_뒤_닫으면_한_번_묻는다()
        {
            RunSta(() =>
            {
                using (SilentReservationForm form = Form(true))
                {
                    form.Show();
                    form.Close();

                    Assert.AreEqual(1, form.Questions.Count, "묻지 않았거나 두 번 물었다");
                    StringAssert.Contains(form.Questions[0], "저장되지 않았습니다");
                    Assert.IsTrue(form.IsDisposed, "Yes 라고 했는데 안 닫혔다");
                }
            });
        }

        // 대상: FrmReservation (WF-RSV-01) — 폐기 확인에서 「아니오」를 고른 경우
        // 목적: 묻기만 하고 답을 무시하는 구현에서도 화면은 똑같아 보인다. 「아니오」가 실제로
        //       닫기를 막는지는 시험이 아니면 드러나지 않고, 그 결함은 입력값을 잃은 뒤에 알게 된다.
        // 확인: 확인창이 1회 뜨고, No 를 고르면 폼이 닫히지 않는다 (IsDisposed=false).
        [TestMethod]
        public void 아니오라고_하면_닫히지_않는다()
        {
            RunSta(() =>
            {
                using (SilentReservationForm form = Form(false))
                {
                    form.Show();
                    form.Close();

                    Assert.AreEqual(1, form.Questions.Count);
                    Assert.IsFalse(form.IsDisposed, "No 라고 했는데 닫혔다");
                }
            });
        }

        // 대상: FrmReservation (WF-RSV-01) — 입력값이 없는 상태에서 창을 닫을 때
        // 목적: 03 §8.10 — 수검자가 확정되기 전에는 화면에 조작자가 들인 것이 없다. 그때도
        //       물으면 아무것도 안 한 창을 닫을 때마다 확인창이 뜨고, 조작자는 확인창을 읽지 않고
        //       누르는 버릇이 든다.
        // 확인: 확정 전에 닫으면 확인창이 0회다.
        [TestMethod]
        public void 수검자가_확정되기_전에는_묻지_않는다()
        {
            RunSta(() =>
            {
                using (var form = new SilentReservationForm(true))
                {
                    form.Show();
                    form.Close();

                    Assert.AreEqual(0, form.Questions.Count);
                }
            });
        }

        /// <summary>
        /// [R21] **대상을 실어 연다.** 부모(`WF-PAT-01`)가 목록에서 받아 둔 행을 그대로 넘기고,
        /// 모달은 조회 없이 그것으로 선다 — 그래야 수검자가 확정되고 §8.10 이 걸린다.
        /// </summary>
        private static SilentReservationForm Form(bool answer)
        {
            WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);

            return new SilentReservationForm(
                new FakeReservationService { Availability = Availability() },
                new FakePatientService(), new FakeWorkService(), "접수1번창구", Patient(), answer);
        }

        /// <summary>목록 SP RS1 한 행. `ValidWork` 가 없으니 예약 가능이다 (00 RP-06).</summary>
        internal static PatientDto Patient()
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

        private static ReservationAvailabilityReadDto Availability()
        {
            return new ReservationAvailabilityReadDto
            {
                Result = new DbResult { Success = true, Code = 0, Message = "정상 처리되었습니다." },
                Summary = new ReservationSummaryDto
                {
                    ChangeScope = "ALL",
                    PatientId = PatientId,
                    ReserveType = "NORMAL",
                    WorkAllowed = true,
                    CanSave = false,
                    BlockMessage = string.Empty,
                },
                Slots = new List<SlotInfoDto>(),
                NexItems = new List<WorkExamItemDto>(),
                AexItems = new List<ReservationAexItemDto>(),
            };
        }

        private static void RunSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                throw new AssertFailedException(failure.Message, failure);
            }
        }
    }

    /// <summary>
    /// 묻는 자리를 미리 답해 두는 `FrmReservation`. **시험과 캡처는 이것만 쓴다** —
    /// 실물 폼을 쓰면 `XtraMessageBox` 가 떠 회귀가 사람을 기다린다 (2026-09-11).
    /// </summary>
    internal sealed class SilentReservationForm : FrmReservation
    {
        private readonly bool _answer;

        /// <summary>Presenter 없이 세운다 — 폐기 확인이 걸리지 않는 갈래를 재는 자리다.</summary>
        internal SilentReservationForm(bool answer)
        {
            _answer = answer;
        }

        /// <summary>
        /// [R21] 대상을 `PatientDto` 하나로 받는다. 예전에는 `patientId` 와 상세를 따로 넘겼고
        /// 상세 자리에 `null` 을 두면 모달이 스스로 조회했다 — 그 SP 가 사라졌다.
        /// </summary>
        internal SilentReservationForm(
            IReservationService service, IPatientService patientService,
            IWorkService workService, string operatorName, PatientDto patient, bool answer)
            : base(service, patientService, workService, operatorName,
                patient == null ? 0L : patient.PatientId, patient)
        {
            _answer = answer;
        }

        /// <summary>DLG-RSV-01(예약 변경) 갈래. 같은 폼이고 진입값만 다르다 (03 §10.2).</summary>
        internal SilentReservationForm(
            IReservationService service, IPatientService patientService,
            IWorkService workService, string operatorName, WorkDetailReadDto read, bool answer)
            : base(service, patientService, workService, operatorName, read)
        {
            _answer = answer;
        }

        internal IList<string> Questions { get { return _questions; } }

        private readonly IList<string> _questions = new List<string>();

        public override bool Confirm(string message)
        {
            _questions.Add(message);
            return _answer;
        }
    }
}
