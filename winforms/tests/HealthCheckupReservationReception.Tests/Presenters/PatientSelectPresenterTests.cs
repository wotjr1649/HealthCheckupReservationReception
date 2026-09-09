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
    /// DLG-PAT-02 (03 §7). 조회계약은 WF-PAT-01 과 같으므로 여기서는 **이 창만의 것** —
    /// 상세를 읽지 않는다는 것과 `[선택]` 의 열림 조건 — 을 본다.
    /// </summary>
    [TestClass]
    public class PatientSelectPresenterTests
    {
        // 03 §7.2 는 조회계약을 §5.3 에 위임한다 — 최소 1개 조건이 있어야 SP 를 부른다.
        [TestMethod]
        public void 조회조건이_하나도_없으면_SP_를_부르지_않는다()
        {
            var view = new FakePatientSelectView();
            var service = new FakePatientService();
            new PatientSelectPresenter(view, service);

            view.RaiseSearchRequested();

            Assert.IsNull(service.LastRequest);
            Assert.IsNotNull(view.LastMessage);
        }

        [TestMethod]
        public void 조회하면_행을_싣고_선택은_닫아_둔다()
        {
            var view = new FakePatientSelectView { ChartNo = "2026-000123" };
            var service = new FakePatientService { SearchResult = Rows() };
            new PatientSelectPresenter(view, service);

            view.RaiseSearchRequested();

            Assert.AreEqual(1, view.Rows.Count);
            Assert.IsFalse(view.SelectEnabled, "고르지도 않은 행으로 [선택] 이 열려 있다");
        }

        [TestMethod]
        public void 행을_고르면_선택이_열리고_상세는_읽지_않는다()
        {
            var view = new FakePatientSelectView();
            var service = new FakePatientService();
            new PatientSelectPresenter(view, service);

            view.RaiseSelectionChanged(11);

            Assert.IsTrue(view.SelectEnabled);
            Assert.IsNull(service.LastPatientId, "이 창은 PatientId 만 돌려준다 — 상세를 읽지 않는다");

            view.RaiseSelectionChanged(null);

            Assert.IsFalse(view.SelectEnabled);
        }

        private static OperationResult<IList<PatientListItemDto>> Rows()
        {
            return OperationResult<IList<PatientListItemDto>>.Success(new List<PatientListItemDto>
            {
                new PatientListItemDto
                {
                    PatientId = 11,
                    ChartNo = "2026-000123",
                    Name = "홍길동",
                    SocialNumber = "6603122000019",
                    Birthday = "19660312",
                    Gender = "F",
                },
            });
        }
    }

    internal sealed class FakePatientSelectView : IPatientSelectView
    {
        public event EventHandler SearchRequested;
        public event EventHandler<long?> SelectionChanged;

        public string ChartNo { get; set; }
        public string Name { get; set; }
        public string SocialNumber { get; set; }
        public string Birthday { get; set; }
        public string MobilePhone { get; set; }

        public IList<PatientListItemDto> Rows { get; set; }
        public bool SelectEnabled { get; set; }
        public string LastMessage { get; private set; }

        public void ShowMessage(string message)
        {
            LastMessage = message;
        }

        public void RaiseSearchRequested()
        {
            EventHandler handler = SearchRequested;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }

        public void RaiseSelectionChanged(long? patientId)
        {
            EventHandler<long?> handler = SelectionChanged;
            if (handler != null) { handler(this, patientId); }
        }
    }
}
