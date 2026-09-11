using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Tests.Services;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Presenters
{
    /// <summary>DLG-LOG-01 변경이력 열람 (03 §23).</summary>
    [TestClass]
    public class ChangeLogPresenterTests
    {
        [TestMethod]
        public void 열면_대상을_그대로_물어_목록을_채운다()
        {
            var view = new FakeChangeLogView();
            var service = new FakeChangeLogService
            {
                Result = OperationResult<ChangeLogReadDto>.Success(new ChangeLogReadDto
                {
                    Rows = new List<ChangeLogItemDto> { Row("휴대전화") },
                }),
            };

            new ChangeLogPresenter(view, service).Begin(Target(DbLogTarget.Patient, 11));

            Assert.AreEqual("수검자", service.LastTargetTable);
            Assert.AreEqual(11L, service.LastTargetKey);
            Assert.AreEqual(1, view.Rows.Count);
            Assert.AreEqual("수검자 홍길동 (2026-000123)", view.Subject);
            Assert.IsNull(view.ValidationMessage);
        }

        /// <summary>
        /// 03 §23.4 — 0건은 오류가 아니다. 계약이 `결과코드=0` 을 주므로 (05 §8.3) 화면도
        /// 오류로 말하지 않는다. 빈 Grid 안내는 화면이 갖는다.
        /// </summary>
        [TestMethod]
        public void 기록이_0건이어도_오류로_말하지_않는다()
        {
            var view = new FakeChangeLogView();
            var service = new FakeChangeLogService
            {
                Result = OperationResult<ChangeLogReadDto>.Success(new ChangeLogReadDto
                {
                    Rows = new List<ChangeLogItemDto>(),
                }),
            };

            new ChangeLogPresenter(view, service).Begin(Target(DbLogTarget.Work, 77));

            Assert.AreEqual(0, view.Rows.Count);
            Assert.IsNull(view.ValidationMessage, "0건을 오류라고 적었다");
        }

        [TestMethod]
        public void 조회가_실패하면_사유를_적고_목록을_비운다()
        {
            var view = new FakeChangeLogView();
            var service = new FakeChangeLogService
            {
                Result = OperationResult<ChangeLogReadDto>.Failure("입력값이 올바르지 않습니다."),
            };

            new ChangeLogPresenter(view, service).Begin(Target(DbLogTarget.Patient, 11));

            Assert.AreEqual("입력값이 올바르지 않습니다.", view.ValidationMessage);
            Assert.AreEqual(0, view.Rows.Count);
        }

        // 킷 §6 — provider 메시지는 DB·머신 정보를 드러낸다. 본문을 화면에 싣지 않는다.
        [TestMethod]
        public void 예외가_나도_예외_본문을_화면에_싣지_않는다()
        {
            var view = new FakeChangeLogView();
            var service = new FakeChangeLogService { Failure = new InvalidOperationException("서버 SQLDEV01") };

            new ChangeLogPresenter(view, service).Begin(Target(DbLogTarget.Patient, 11));

            Assert.IsFalse(view.ValidationMessage.Contains("SQLDEV01"), view.ValidationMessage);
            Assert.AreEqual(0, view.Rows.Count);
        }

        [TestMethod]
        public void 대상이_없으면_묻지_않는다()
        {
            var view = new FakeChangeLogView();
            var service = new FakeChangeLogService();

            new ChangeLogPresenter(view, service).Begin(null);

            Assert.IsNull(service.LastTargetTable, "대상 없이 SP 를 불렀다");
            Assert.IsNotNull(view.ValidationMessage);
        }

        private static ChangeLogTarget Target(string table, long key)
        {
            return new ChangeLogTarget
            {
                TargetTable = table,
                TargetKey = key,
                Caption = "수검자 홍길동 (2026-000123)",
            };
        }

        private static ChangeLogItemDto Row(string column)
        {
            return new ChangeLogItemDto
            {
                LogId = 1,
                RecordedAt = new DateTime(2026, 9, 7, 14, 20, 0),
                OperatorName = "접수1번창구",
                ColumnName = column,
                BeforeValue = "010-0000-0001",
                AfterValue = "010-0000-0002",
            };
        }
    }

    internal sealed class FakeChangeLogView : IChangeLogView
    {
        public string Subject { get; set; }
        public IList<ChangeLogItemDto> Rows { get; set; }
        public string ValidationMessage { get; set; }
    }
}
