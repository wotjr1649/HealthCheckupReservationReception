// ── 시나리오 증빙 — 실물 DB 데이터가 뜬 화면을 단계마다 PNG 로 남긴다 ───────
//
// 과제 브리프 §9 가 화면 측 증빙을 요구한다. 받는 사람이 확인해야 하는 것은 「화면이 어떻게
// 생겼는가」가 아니라 **「업무가 실제로 처리됐는가」**이므로, 한 장이 아니라 **전·후 한 쌍**으로
// 뜬다. 대응표는 docs/phase5/2026-09-14-Test-Scenarios.md 의 「화면」 칸이 갖는다.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;
using HealthCheckupReservationReception.Services;
using HealthCheckupReservationReception.Tests.Integration;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Visual
{
    /// <summary>
    /// `ScenarioDbTests` 의 S01·S03·S04·S05 를 **화면으로** 한 번 더 밟아 증빙을 남긴다.
    ///
    /// [X] **`ShellCaptureTests` 와 자리를 나눈 이유.** 그 파일은 머리말에 「DB 에 붙지 않고
    ///     fake 로 상태를 고정한다 — 결정적이다」라고 적는다. 실물 DB 캡처를 거기 섞으면 그
    ///     문장이 거짓이 된다. 저쪽은 **배치**를, 이쪽은 **업무**를 보인다.
    ///
    /// [X] **예외는 메시지가 아니라 결과 화면으로 보인다.** 화면의 메시지는 전부 모달이라
    ///     시험 스레드가 그 순간 그림을 뜰 수 없다. 그리고 재접수가 막혔다는 증거는 대화상자
    ///     문구가 아니라 **상태가 그대로 RCP 라는 것**이다 — 문구는 단위시험이 따로 잰다.
    ///
    /// [I] 판정을 함께 건다. 그림만 뜨고 단언이 없으면 **틀린 그림이 조용히 증빙이 된다.**
    ///     그려 넣기 전에 행수와 상태코드를 먼저 확인한다.
    ///
    /// [I] 운영시간·마감의 창은 <see cref="DbFixture"/> 가 연다. 밤에 돌려도 뜬다.
    /// </summary>
    [TestClass]
    public class ScenarioCaptureTests
    {
        private const string Operator = "증빙";
        private const string Subdir = "scenario";

        /// <summary>실행 화면과 같은 크기다 — 캡처끼리 폭이 흔들리면 눈으로 견줄 수 없다.</summary>
        private static readonly Size ScreenSize = new Size(1916, 887);

        private readonly List<long> _created = new List<long>();

        [TestCleanup]
        public void 만든_업무를_되돌린다()
        {
            foreach (long workId in _created)
            {
                try
                {
                    WorkDetailReadDto read = Works().GetDetail(workId).Value;
                    if (read == null || read.Detail == null)
                    {
                        continue;
                    }

                    var request = new WorkActionRequest
                    {
                        WorkId = workId,
                        RowVersion = read.Detail.RowVersion,
                        OperatorName = Operator,
                    };

                    // 이미 취소된 건은 502 를 낸다 — 정리이므로 결과를 따지지 않는다.
                    if (DbWorkStatus.Received.Equals(read.Detail.StatusCode, StringComparison.Ordinal))
                    {
                        Works().CancelWork(DbWorkAction.CancelReception, request);
                    }
                    else if (DbWorkStatus.Reserved.Equals(read.Detail.StatusCode, StringComparison.Ordinal))
                    {
                        Works().CancelWork(DbWorkAction.CancelReservation, request);
                    }
                }
                catch (SqlException)
                {
                    // DB 가 없으면 시험 자체가 Inconclusive 다 — 정리에서 다시 말하지 않는다.
                }
            }

            _created.Clear();
        }

        // 대상: S01 정상 업무 흐름 — 등록 → 예약 → 조회 → 접수를 화면으로 네 장
        // 목적: 브리프 §9 가 화면 측 증빙을 요구한다. 받는 사람은 저장소를 열지 않으므로
        //       「시험이 PASS 했다」는 문장만으로는 업무가 처리되는 것을 볼 수 없다. 한 장짜리
        //       화면 사진도 배치만 보일 뿐 처리를 보이지 못한다 — 같은 조회 조건으로 전·후를
        //       나란히 떠야 「없던 줄이 생겼다 · 상태가 옮겨졌다」가 그림에서 읽힌다.
        // 확인: 그 수검자 차트번호로 조회하면 예약 전 0행, 예약 후 1행 RSV, 접수 후 1행 RCP 이고
        //       네 장이 전부 10KB 를 넘는 실제 그림이다 (폼이 안 그려지면 파일만 생기고 빈다).
        [TestMethod]
        [TestCategory("Visual")]
        [TestCategory("Db")]
        public void S01_등록에서_접수까지를_화면_네_장으로_남긴다()
        {
            PatientSaveResultDto patient = NewPatient();
            DateTime day = Today();

            // ① 예약 전 — 같은 조회 조건으로 먼저 떠 둔다. 이 한 장이 없으면 뒤 장이 무엇을
            //    바꿨는지 보이지 않는다.
            CaptureWorkbench("s01_1_예약전.png", patient.ChartNo, null, WorkContext.Reservation);

            // ② 등록 — 수검자 화면에 그 사람이 서 있다.
            CapturePatients("s01_2_수검자등록.png", patient.ChartNo);

            // ③ 예약 후 — 없던 줄이 RSV 로 생긴다.
            WorkSaveReadDto booked = Book(patient.PatientId, day);
            CaptureWorkbench("s01_3_예약후.png", patient.ChartNo, DbWorkStatus.Reserved, WorkContext.Reservation);

            // ④ 접수 후 — 같은 줄의 상태가 RCP 로 옮겨진다.
            Receive(booked);
            CaptureWorkbench("s01_4_접수후.png", patient.ChartNo, DbWorkStatus.Received, WorkContext.Reception);
        }

        // 대상: S03 예약취소 — 취소 뒤 화면 한 장
        // 목적: 브리프 §4 「예약 정보를 변경하거나 취소하는 경우」다. 취소가 행을 지우는 것이
        //       아니라 상태를 옮기는 것이라는 점이 이 그림의 요점이다 — 줄이 사라졌다면 감사
        //       기록도 함께 사라진 것이고, 그것은 RP-10 위반이다. 그림은 줄이 남아 있고 상태만
        //       바뀐 것을 보인다.
        // 확인: 취소가 결과코드 0 이고, 그 차트번호 조회가 여전히 1행이며 상태코드가 CNR 이다.
        [TestMethod]
        [TestCategory("Visual")]
        [TestCategory("Db")]
        public void S03_예약취소_뒤_화면을_남긴다()
        {
            PatientSaveResultDto patient = NewPatient();
            WorkSaveReadDto booked = Book(patient.PatientId, Today());

            WorkSaveReadDto cancelled = Ok(() => Works().CancelWork(DbWorkAction.CancelReservation, new WorkActionRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = booked.Row.RowVersion,
                OperatorName = Operator,
            }));
            Assert.AreEqual((int)DbCode.Ok, cancelled.Result.Code, "예약취소: " + cancelled.Result.Message);

            CaptureWorkbench("s03_예약취소후.png", patient.ChartNo, DbWorkStatus.CancelledReservation, WorkContext.Reservation);
        }

        // 대상: S04 재접수 차단 — 이미 접수된 건을 다시 접수한 뒤 화면 한 장
        // 목적: 브리프 §4 「이미 접수된 예약을 다시 접수하려는 경우」다. 막혔다는 증거는
        //       대화상자 문구가 아니라 **아무것도 바뀌지 않았다**는 것이다 — 두 번 접수되면
        //       같은 사람이 두 번 온 것으로 집계되고 정원도 두 번 깎인다. 그림은 시도 뒤에도
        //       줄이 하나이고 상태가 그대로임을 보인다.
        // 확인: 두 번째 접수가 결과코드 502 로 막히고, 그 뒤 조회가 1행 RCP 그대로다.
        [TestMethod]
        [TestCategory("Visual")]
        [TestCategory("Db")]
        public void S04_재접수가_막힌_뒤_화면을_남긴다()
        {
            PatientSaveResultDto patient = NewPatient();
            WorkSaveReadDto booked = Book(patient.PatientId, Today());
            WorkSaveReadDto received = Receive(booked);

            WorkSaveReadDto twice = Ok(() => Works().CompleteReception(new WorkActionRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = received.Row.RowVersion,
                OperatorName = Operator,
            }));
            Assert.AreEqual((int)DbCode.WrongStatus, twice.Result.Code,
                "재접수가 막히지 않았다: " + twice.Result.Message);

            CaptureWorkbench("s04_재접수시도후.png", patient.ChartNo, DbWorkStatus.Received, WorkContext.Reception);
        }

        // 대상: S05 접수취소 — 접수 뒤 취소한 화면 한 장
        // 목적: 브리프 §4 「접수 후 취소가 필요한 경우」다. 접수취소가 예약(RSV)으로 되돌아가지
        //       않고 CNC 로 간다는 것이 이 그림의 요점이다 — 되돌리면 그 사람이 내원했다는
        //       사실이 지워지고 정원 계산과 실적 집계가 어긋난다.
        // 확인: 접수취소가 결과코드 0 이고, 조회가 1행이며 상태코드가 RSV 가 아니라 CNC 다.
        [TestMethod]
        [TestCategory("Visual")]
        [TestCategory("Db")]
        public void S05_접수취소_뒤_화면을_남긴다()
        {
            PatientSaveResultDto patient = NewPatient();
            WorkSaveReadDto booked = Book(patient.PatientId, Today());
            WorkSaveReadDto received = Receive(booked);

            WorkSaveReadDto cancelled = Ok(() => Works().CancelWork(DbWorkAction.CancelReception, new WorkActionRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = received.Row.RowVersion,
                OperatorName = Operator,
            }));
            Assert.AreEqual((int)DbCode.Ok, cancelled.Result.Code, "접수취소: " + cancelled.Result.Message);

            CaptureWorkbench("s05_접수취소후.png", patient.ChartNo, DbWorkStatus.CancelledReception, WorkContext.Reception);
        }

        // ── 여기서부터는 조립용이다.

        /// <summary>
        /// WF-WRK-01 을 실물 서비스로 세우고 **화면에서 조회한다.** 차트번호를 조건 칸에 넣고
        /// `[조회]` 를 실제로 누르므로, 그림에 든 줄은 Presenter → Service → SP → 실물 DB 를
        /// 지나온 것이다. 값을 화면 뒤로 밀어 넣으면 그림은 같아 보여도 아무것도 증명하지
        /// 못한다 — 조건 칸이 빈 채 결과만 있는 그림은 받는 사람에게 그렇게 읽힌다.
        ///
        /// 조건을 그 수검자의 차트번호 하나로 고정하는 이유는 전·후 두 장이 **같은 조건**
        /// 이어야 비교가 성립하기 때문이다. 오늘 전체를 조회하면 다른 시험이 만든 줄이 섞여
        /// 무엇이 달라졌는지 읽히지 않는다.
        /// </summary>
        private static void CaptureWorkbench(string file, string chartNo, string expectedStatus, WorkContext context)
        {
            int expected = expectedStatus == null ? 0 : 1;

            string path = null;
            int shown = -1;
            string shownStatus = null;
            bool detailFilled = false;

            clsCapture.RunSta(() =>
            {
                UiDefaults();

                var screen = new UcWorkbench();
                screen.Attach(Works(), Reservations(), Status(), "접수1번창구");

                using (var host = NewHost())
                {
                    screen.Dock = DockStyle.Fill;
                    host.Controls.Add(screen);
                    host.Show();
                    Application.DoEvents();

                    // 창구를 먼저 연다 — Context 가 상태 목록을 정한다 (03 §9.1).
                    // 접수 상태 업무는 예약 관리 탭에 나타나지 않는다.
                    screen.OpenContext(context, null);
                    Application.DoEvents();

                    clsCapture.Control<TextEdit>(screen, "txtChartNo").Text = chartNo;
                    clsCapture.Control<SimpleButton>(screen, "btnSearch").PerformClick();
                    Application.DoEvents();

                    GridView grid = clsCapture.Control<GridView>(screen, "gvWorkList");
                    shown = grid.RowCount;
                    if (shown > 0)
                    {
                        // [X] DB 를 따로 묻지 않는다. 화면에 든 줄은 이미 Presenter → Service
                        //     → SP → DB 를 지나온 것이고, 따로 물으면 **조회 조건이 갈린다** —
                        //     화면은 창구별 기간까지 쓰는데 시험은 차트번호만 물었다.
                        //     보이는 것을 재는 것이 곧 화면과 DB 를 한 번에 재는 것이다.
                        var row = grid.GetRow(0) as WorkListItemDto;
                        shownStatus = row == null ? null : row.StatusCode;

                        // 조회 직후에는 아무것도 선택되지 않은 상태가 계약이다 (03 §9.4) —
                        // Grid 가 잡아 둔 행은 사용자가 고른 행이 아니다. 사용자가 고르는
                        // 그 지점을 그대로 부른다 (03 §9.1 Targeted Navigation 과 같은 길).
                        clsCapture.Control<clsGridRowPicker>(screen, "_picker").Select(0);
                        Application.DoEvents();
                        detailFilled = screen.CurrentDetail != null;
                    }

                    path = Draw(host, file);
                    host.Close();
                }
            });

            Assert.AreEqual(expected, shown, file + ": 화면 목록의 행수가 다르다");
            if (expected > 0)
            {
                Assert.AreEqual(expectedStatus, shownStatus, file + ": 화면 줄의 상태코드가 다르다");
                Assert.IsTrue(detailFilled, file + ": 줄을 골랐는데 상세가 비었다 — 배선이 끊겼다");
            }

            clsCapture.AssertDrawn(path);
        }

        /// <summary>WF-PAT-01 도 같은 방식이다 — 조건을 넣고 화면에서 조회한다.</summary>
        private static void CapturePatients(string file, string chartNo)
        {
            IList<PatientDto> rows = Ok(() => Patients().Search(new PatientSearchRequest { ChartNo = chartNo }));
            Assert.AreEqual(1, rows.Count, file + ": 방금 등록한 수검자가 DB 에 1건이 아니다");

            string path = null;
            int shown = -1;

            clsCapture.RunSta(() =>
            {
                UiDefaults();

                var screen = new UcPatientManagement();
                screen.Attach(Patients(), Works());

                using (var host = NewHost())
                {
                    screen.Dock = DockStyle.Fill;
                    host.Controls.Add(screen);
                    host.Show();
                    Application.DoEvents();

                    clsCapture.Control<TextEdit>(screen, "txtChartNo").Text = chartNo;
                    clsCapture.Control<SimpleButton>(screen, "btnSearch").PerformClick();
                    Application.DoEvents();

                    GridView grid = clsCapture.Control<GridView>(screen, "gvPatientList");
                    shown = grid.RowCount;
                    if (shown > 0)
                    {
                        clsCapture.Control<clsGridRowPicker>(screen, "_picker").Select(0);
                        Application.DoEvents();
                    }

                    path = Draw(host, file);
                    host.Close();
                }
            });

            Assert.AreEqual(1, shown, file + ": 화면 목록의 행수가 DB 와 다르다");
            clsCapture.AssertDrawn(path);
        }

        /// <summary>
        /// `Program.cs` 는 시험에서 돌지 않는다. 글꼴 기준을 여기서 같게 맞춘다 (킷 §1) —
        /// 맞추지 않으면 증빙 그림이 실행본과 다른 글꼴로 나간다.
        /// </summary>
        private static void UiDefaults()
        {
            WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);
            WindowsFormsSettings.DefaultMenuFont = new Font("굴림", 9F);
        }

        /// <summary>화면 밖에 띄운다. 보이지 않으면 DevExpress 가 스킨을 그리지 않는다.</summary>
        private static Form NewHost()
        {
            return new Form
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-32000, -32000),
                ClientSize = ScreenSize,
            };
        }

        private static string Draw(Form host, string file)
        {
            using (var bmp = new Bitmap(host.ClientSize.Width, host.ClientSize.Height))
            {
                host.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size));
                return clsCapture.Save(bmp, Subdir, file);
            }
        }

        private WorkSaveReadDto Book(long patientId, DateTime day)
        {
            ReservationAvailabilityReadDto open = Ok(() => Reservations().GetAvailability(
                new ReservationAvailabilityRequest
                {
                    PatientId = patientId,
                    ReserveType = DbReserveType.Normal,
                    ReserveDate = day,
                }));

            SlotInfoDto slot = open.Slots == null ? null : FirstOpen(open.Slots);
            if (slot == null)
            {
                Assert.Inconclusive("오늘 열린 시간대가 없다 — 요일·정원·휴무 중 하나다. 판정하지 않는다.");
            }

            WorkSaveReadDto saved = Ok(() => Reservations().Register(new ReservationSaveRequest
            {
                PatientId = patientId,
                ReserveType = DbReserveType.Normal,
                ReserveDate = day,
                SlotCode = slot.SlotCode,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.Ok, saved.Result.Code, "예약: " + saved.Result.Message);
            _created.Add(saved.Row.WorkId);
            return saved;
        }

        private static WorkSaveReadDto Receive(WorkSaveReadDto booked)
        {
            WorkSaveReadDto received = Ok(() => Works().CompleteReception(new WorkActionRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = booked.Row.RowVersion,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.Ok, received.Result.Code, "접수: " + received.Result.Message);
            return received;
        }

        private static SlotInfoDto FirstOpen(IList<SlotInfoDto> slots)
        {
            foreach (SlotInfoDto slot in slots)
            {
                if (slot.Selectable)
                {
                    return slot;
                }
            }

            return null;
        }

        /// <summary>
        /// 매 실행마다 새 수검자를 만든다. 차트번호가 겹치지 않아야 **전·후 두 장이 같은 조건
        /// 으로 한 사람만** 비춘다. 임의 생성한 시험값만 쓴다 (`00` §2.1).
        /// </summary>
        private static PatientSaveResultDto NewPatient()
        {
            PatientSaveReadDto saved = Ok(() => Patients().Register(new PatientSaveRequest
            {
                AutoChartNo = true,
                Name = "증빙" + DateTime.Now.ToString("HHmmss"),
                SocialNumber = NewSocialNumber(),
                MobilePhone = "010-4200-0000",
                Memo = "시나리오 증빙이 만든 수검자",
                SimilarConfirmed = true,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.Ok, saved.Result.Code, "수검자 등록: " + saved.Result.Message);
            Assert.IsTrue(saved.Rows != null && saved.Rows.Count == 1, "등록 결과가 1행이 아니다");
            return saved.Rows[0];
        }

        private static string NewSocialNumber()
        {
            // 860115 + 1(1900년대 남) + 6자리. 뒤 여섯은 100000 주기로 도는 시각값이다.
            long tail = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % 1000000;
            return "8601151" + tail.ToString("D6");
        }

        private static DateTime Today()
        {
            CommonWorkStatusDto status = Ok(() => Status().GetCurrent());

            // [X] DateTime.Today 를 쓰지 않는다 — PC 시계는 DB 시계가 아니다 (05 §2.3).
            return status.Today;
        }

        private static IPatientService Patients()
        {
            return new PatientService(new PatientRepository(Connection()));
        }

        private static IWorkService Works()
        {
            return new WorkService(new WorkRepository(Connection()));
        }

        private static IReservationService Reservations()
        {
            return new ReservationService(new ReservationRepository(Connection()));
        }

        private static ICommonStatusService Status()
        {
            return new CommonStatusService(new CommonStatusRepository(Connection()));
        }

        private static string Connection()
        {
            return DbFixture.ConnectionString();
        }

        private static T Ok<T>(Func<OperationResult<T>> call)
        {
            OperationResult<T> result = DbFixture.Run(call);
            Assert.IsTrue(result.IsSuccess, "서비스가 실패했다 — " + result.Message);
            return result.Value;
        }
    }
}
