using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;
using HealthCheckupReservationReception.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Integration
{
    /// <summary>
    /// 과제 브리프 §3 업무범위와 §4 고려사항을 **실물 DB 로 끝까지 밟는다.**
    /// `docs/phase5/2026-09-14-Test-Scenarios.md` 의 결과 칸이 이 시험들이다.
    ///
    /// [X] **Repository 가 아니라 Service 를 부른다.** 재는 것이 SP 계약이 아니라
    ///     *업무가 되는가* 이므로, 화면이 실제로 쥐는 층에서 잰다. SP 계약 자체는
    ///     `SelectRepositoryDbTests`·`PatientRepositoryDbTests` 와 `database/` 계약시험이 맡는다.
    ///
    /// [I] **쓰기를 한다** — 그래서 읽기 전용인 두 파일과 자리를 나눴다. 매 실행이
    ///     **새 수검자**를 만들어 밟으므로 몇 번을 돌려도 서로를 방해하지 않는다.
    ///     차트번호는 자동발급이고 주민번호 뒷자리는 실행 시각에서 만든다.
    ///
    /// [I] DB 가 없으면 `Inconclusive` 다. 판정하지 못한 검사는 PASS 가 아니다.
    ///     마감이 지나 접수가 막히는 시각에 돌리면 그 시나리오도 `Inconclusive` 다 —
    ///     **초록으로 칠하지 않는다.** 마감 자체는 별도 실측이 맡는다
    ///     (`2026-09-14-Cutoff-Field-Measurement.md`).
    /// </summary>
    [TestClass]
    public class ScenarioDbTests
    {
        private const string Operator = "시나리오";

        /// <summary>
        /// 이 시험이 만든 오늘 업무. **정원은 공유 자원이다** — 수검자는 매 실행 새로 만들어
        /// 겹치지 않지만 시간대 정원 20 은 그렇지 않아서, 반복 실행이 그것을 먹어 치우면
        /// 다음 실행이 통째로 `Inconclusive` 가 된다. 실제로 PM 이 20/20 이 되어 그렇게 됐다
        /// (2026-09-14 실측). 그래서 만든 것은 시험이 되돌린다.
        /// </summary>
        private readonly List<long> _created = new List<long>();

        [TestCleanup]
        public void 만든_업무를_되돌린다()
        {
            foreach (long workId in _created)
            {
                // 이미 취소된 건은 502 를 낸다 — 정리이므로 결과를 따지지 않는다.
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

        // 대상: 실물 DB 전 구간 — 수검자 등록(SP-PAT-03) → 예약(SP-RSV-02) → 목록 조회(SP-WRK-01)
        //       → 접수완료(SP-RCP-01) 를 Service 계층으로 이어 부른다
        // 목적: 과제 브리프 §3 이 그린 기본 업무 흐름이다. 네 단계를 한 시험에 이은 것은
        //       단계 사이의 값이 실제로 흘러야 하기 때문이다 — 등록이 준 수검자ID 로 예약하고,
        //       예약이 준 업무ID·행버전으로 접수한다. 나누면 그 이음매가 시험되지 않는다.
        // 확인: 예약이 결과코드 0 · 상태 RSV 이고, 그 업무ID 가 목록 조회에 정확히 1건 나오며,
        //       접수가 결과코드 0 · 상태 RCP 로 전이된다.
        [TestMethod]
        [TestCategory("Db")]
        public void S01_등록에서_접수까지_한_흐름으로_통한다()
        {
            PatientSaveResultDto patient = NewPatient();
            DateTime today = Today();

            // 예약 — 오늘 자리가 있는 시간대로 넣는다.
            ReservationSaveRequest save = BookingFor(patient.PatientId, today);
            WorkSaveReadDto booked = SaveReservation(save);
            Assert.AreEqual((int)DbCode.Ok, booked.Result.Code, "예약: " + booked.Result.Message);
            Assert.AreEqual(DbWorkStatus.Reserved, booked.Row.StatusCode);

            // 조회 — 방금 넣은 건이 오늘 목록에 있다 (F-COM-001).
            IList<WorkListItemDto> rows = Ok(() => Works().Search(new WorkSearchRequest
            {
                FromDate = today,
                ToDate = today,
                ChartNo = patient.ChartNo,
            }));
            Assert.AreEqual(1, rows.Count(r => r.WorkId == booked.Row.WorkId), "예약이 목록에 없다");

            // 접수 — 상태가 RSV 이고 예약일이 오늘이어야 한다 (RCP-01~03).
            WorkSaveReadDto received = Ok(() => Works().CompleteReception(new WorkActionRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = booked.Row.RowVersion,
                OperatorName = Operator,
            }));

            SkipIfCutoff(received.Result, "접수");
            Assert.AreEqual((int)DbCode.Ok, received.Result.Code, "접수: " + received.Result.Message);
            Assert.AreEqual(DbWorkStatus.Received, received.Row.StatusCode);
        }

        // 대상: 실물 DB — 같은 주민번호·같은 이름으로 수검자를 다시 등록(SP-PAT-03)
        // 목적: 브리프 §4 「동일한 수검자가 이미 등록되어 있는 경우」다. 00 EP 정책은 같은 사람을
        //       두 행으로 만들지 않고 기존 행으로 확정한다 — 두 행이 되면 예약·접수 이력이 갈려
        //       한 사람의 검진 주기(TGT-04)를 계산할 수 없다.
        // 확인: 결과코드가 2(기존 수검자)이고, RS1 이 가리키는 수검자ID 가 처음 등록한 그 행이다
        //       — 새 행이 생기지 않는다.
        [TestMethod]
        [TestCategory("Db")]
        public void S02_같은_주민번호로_다시_등록하면_기존_수검자로_확정된다()
        {
            PatientSaveResultDto first = NewPatient();

            PatientSaveReadDto again = Ok(() => Patients().Register(new PatientSaveRequest
            {
                AutoChartNo = true,
                Name = first.Name,
                SocialNumber = first.SocialNumber,
                MobilePhone = "010-4002-0000",
                SimilarConfirmed = true,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.ExistingPatient, again.Result.Code,
                "새 수검자가 또 생겼다 — 중복 등록이 막히지 않았다: " + again.Result.Message);
            Assert.AreEqual(first.PatientId, again.Rows[0].PatientId, "기존 수검자로 확정하지 않았다");
        }

        // 대상: 실물 DB — 예약(SP-RSV-02) 뒤 예약취소(SP-WRK-03)
        // 목적: 브리프 §4 「예약 정보를 변경하거나 취소하는 경우」다. 취소는 행을 지우는 것이
        //       아니라 상태를 옮기는 것이며, 물리삭제하면 그 사람이 언제 무엇을 취소했는지가
        //       감사 기록에서 사라진다.
        // 확인: 예약이 결과코드 0 으로 선 뒤 취소도 결과코드 0 이고, 상태코드가 CNR 이다
        //       (행은 그대로 남는다).
        [TestMethod]
        [TestCategory("Db")]
        public void S03_예약을_취소하면_예약취소_상태가_된다()
        {
            PatientSaveResultDto patient = NewPatient();
            WorkSaveReadDto booked = SaveReservation(BookingFor(patient.PatientId, Today()));
            Assert.AreEqual((int)DbCode.Ok, booked.Result.Code, "예약: " + booked.Result.Message);

            WorkSaveReadDto cancelled = Ok(() => Works().CancelWork(DbWorkAction.CancelReservation, new WorkActionRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = booked.Row.RowVersion,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.Ok, cancelled.Result.Code, "취소: " + cancelled.Result.Message);
            Assert.AreEqual(DbWorkStatus.CancelledReservation, cancelled.Row.StatusCode,
                "취소인데 상태가 CNR 이 아니다 — 물리삭제하지 않는다 (RP-10)");
        }

        // 대상: 실물 DB — 접수완료(SP-RCP-01) 를 같은 업무에 두 번
        // 목적: 브리프 §4 「이미 접수된 예약을 다시 접수하려는 경우」다. 두 번 접수되면 같은
        //       수검자가 두 번 온 것으로 집계되고 정원도 두 번 깎인다. 막는 것은 화면이 아니라
        //       SP 의 상태 검증이어야 한다 — 창을 두 개 띄우면 화면 가드는 소용이 없다.
        // 확인: 첫 접수는 결과코드 0 이고, 같은 업무에 두 번째 접수는 502(현재 상태에서 처리 불가)로
        //       막힌다.
        [TestMethod]
        [TestCategory("Db")]
        public void S04_이미_접수된_예약은_다시_접수되지_않는다()
        {
            PatientSaveResultDto patient = NewPatient();
            WorkSaveReadDto booked = SaveReservation(BookingFor(patient.PatientId, Today()));
            Assert.AreEqual((int)DbCode.Ok, booked.Result.Code, "예약: " + booked.Result.Message);

            WorkSaveReadDto received = Ok(() => Works().CompleteReception(new WorkActionRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = booked.Row.RowVersion,
                OperatorName = Operator,
            }));
            SkipIfCutoff(received.Result, "접수");
            Assert.AreEqual((int)DbCode.Ok, received.Result.Code, "접수: " + received.Result.Message);

            // 같은 업무를 한 번 더 접수한다 — 상태가 이미 RCP 다.
            WorkSaveReadDto twice = Ok(() => Works().CompleteReception(new WorkActionRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = received.Row.RowVersion,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.WrongStatus, twice.Result.Code,
                "재접수가 막히지 않았다: " + twice.Result.Message);
        }

        // 대상: 실물 DB — 접수완료 뒤 접수취소(SP-RCP-03)
        // 목적: 브리프 §4 「접수 후 취소가 필요한 경우」다. 접수취소는 예약(RSV)으로 되돌리는
        //       것이 아니라 CNC 로 간다 — 되돌리면 그 사람이 내원했다는 사실이 지워지고 정원
        //       계산과 실적 집계가 어긋난다.
        // 확인: 예약·접수·접수취소가 모두 결과코드 0 이고, 최종 상태코드가 RSV 가 아니라 CNC 다.
        [TestMethod]
        [TestCategory("Db")]
        public void S05_접수_후_취소하면_접수취소_상태가_된다()
        {
            PatientSaveResultDto patient = NewPatient();
            WorkSaveReadDto booked = SaveReservation(BookingFor(patient.PatientId, Today()));
            Assert.AreEqual((int)DbCode.Ok, booked.Result.Code, "예약: " + booked.Result.Message);

            WorkSaveReadDto received = Ok(() => Works().CompleteReception(new WorkActionRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = booked.Row.RowVersion,
                OperatorName = Operator,
            }));
            SkipIfCutoff(received.Result, "접수");
            Assert.AreEqual((int)DbCode.Ok, received.Result.Code, "접수: " + received.Result.Message);

            WorkSaveReadDto cancelled = Ok(() => Works().CancelWork(DbWorkAction.CancelReception, new WorkActionRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = received.Row.RowVersion,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.Ok, cancelled.Result.Code, "접수취소: " + cancelled.Result.Message);
            Assert.AreEqual(DbWorkStatus.CancelledReception, cancelled.Row.StatusCode,
                "접수취소는 예약으로 되돌리지 않는다 (RCP-06)");
        }

        // 대상: 실물 DB — 이름을 비운 채 수검자 등록(SP-PAT-03)
        // 목적: 브리프 §4 「필수 정보가 입력되지 않은 경우」다. 05 §10.1 이 이름을 NOT NULL 로
        //       정했고, 화면 검증을 우회해 들어와도 DB 가 막아야 한다는 것이 이 시험의 자리다.
        // 확인: 결과코드가 100(필수값 누락)이고 수검자 행이 생기지 않는다.
        [TestMethod]
        [TestCategory("Db")]
        public void S06_이름_없이_등록하면_필수값으로_막힌다()
        {
            PatientSaveReadDto saved = Ok(() => Patients().Register(new PatientSaveRequest
            {
                AutoChartNo = true,
                Name = string.Empty,
                SocialNumber = NewSocialNumber(),
                MobilePhone = "010-4003-0000",
                SimilarConfirmed = true,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.MissingValue, saved.Result.Code,
                "이름이 비었는데 저장됐다: " + saved.Result.Message);
        }

        // 대상: 실물 DB — 유효예약이 있는 수검자에게 예약(SP-RSV-02) 을 한 번 더
        // 목적: 00 RP-06 동일 수검자 중복예약 금지다. 브리프 §4 가 열거하지 않았지만 「업무상
        //       발생할 수 있는 상황」으로 정의해 넣었다. 두 예약이 서면 둘 중 어느 것으로 내원할지
        //       알 수 없고 정원이 한 사람에게 두 자리 나간다.
        // 확인: 첫 예약은 결과코드 0 이고 두 번째는 0 이 아니며, 수검자 목록 RS1 의 유효업무가
        //       비어 있지 않고 그 업무ID 가 첫 예약을 가리킨다 (조작자가 기존 건을 찾아갈 수 있다).
        [TestMethod]
        [TestCategory("Db")]
        public void S07_유효예약이_있는_수검자는_또_예약하지_못한다()
        {
            PatientSaveResultDto patient = NewPatient();
            DateTime today = Today();

            WorkSaveReadDto booked = SaveReservation(BookingFor(patient.PatientId, today));
            Assert.AreEqual((int)DbCode.Ok, booked.Result.Code, "예약: " + booked.Result.Message);

            WorkSaveReadDto again = Ok(() => Reservations().Register(BookingFor(patient.PatientId, today)));
            Assert.AreNotEqual((int)DbCode.Ok, again.Result.Code,
                "같은 수검자가 두 번 예약됐다 (RP-06)");

            // 화면이 갈 곳을 아는가 — [R21] 목록 SP 한 행이 그 업무를 싣고 온다.
            PatientDto row = Ok(() => Patients().GetByChartNo(patient.ChartNo));
            Assert.IsNotNull(row.ValidWork, "유효업무가 목록에 실리지 않았다 (05 §7.2)");
            Assert.AreEqual(booked.Row.WorkId, row.ValidWork.WorkId, "기존 유효예약을 가리키지 못한다");
        }

        // ── 여기서부터는 조립용이다.

        /// <summary>
        /// 오늘 자리가 있는 시간대를 **DB 에 물어서** 고른다. AM·PM 어느 쪽이 열려 있는지는
        /// 요일·마감·정원이 정하므로 화면이 정하지 않는다 (05 §9.7).
        /// </summary>
        private static ReservationSaveRequest BookingFor(long patientId, DateTime day)
        {
            ReservationAvailabilityReadDto read = Ok(() => Reservations().GetAvailability(
                new ReservationAvailabilityRequest
                {
                    PatientId = patientId,
                    ReserveType = DbReserveType.Normal,
                    ReserveDate = day,
                }));

            if (read.Slots == null || read.Slots.Count == 0)
            {
                Assert.Inconclusive("시간대를 받지 못했다: " + read.Result.Message);
            }

            SlotInfoDto open = read.Slots.FirstOrDefault(s => s.Selectable);
            if (open == null)
            {
                Assert.Inconclusive("오늘 열린 시간대가 없다 — 마감·정원·휴무 중 하나다. 판정하지 않는다.");
            }

            return new ReservationSaveRequest
            {
                PatientId = patientId,
                ReserveType = DbReserveType.Normal,
                ReserveDate = day,
                SlotCode = open.SlotCode,
                OperatorName = Operator,
            };
        }

        private WorkSaveReadDto SaveReservation(ReservationSaveRequest request)
        {
            WorkSaveReadDto saved = Ok(() => Reservations().Register(request));
            if (saved.Row != null)
            {
                // 정리 대상으로 적어 둔다 (TestCleanup). 성공한 것만 실제 행이 된다.
                _created.Add(saved.Row.WorkId);
            }

            return saved;
        }

        /// <summary>
        /// 마감이 지난 시각에 돌리면 접수가 `304` 로 막힌다. 그것은 **옳은 동작**이므로 실패로
        /// 적지 않고, 그렇다고 통과로도 적지 않는다 — 이 시나리오를 못 잰 것이다.
        /// </summary>
        private static void SkipIfCutoff(DbResult result, string step)
        {
            if (result.Code == (int)DbCode.CutoffPassed || result.Code == (int)DbCode.OutsideHours)
            {
                Assert.Inconclusive(step + " 가 마감·운영시간으로 막혔다 (" + result.Code + ") — 업무시간 안에서 다시 돌려라.");
            }
        }

        /// <summary>
        /// 매 실행마다 새 수검자를 만든다. 생년월일은 `1986-01-15` 로 고정이라 나이가 흔들리지
        /// 않고, 주민번호 뒷자리는 실행 시각에서 만들어 이전 실행과 겹치지 않는다.
        /// **임의 생성한 테스트 값만 쓴다** (03 머리말).
        /// </summary>
        private static PatientSaveResultDto NewPatient()
        {
            PatientSaveReadDto saved = Ok(() => Patients().Register(new PatientSaveRequest
            {
                AutoChartNo = true,
                Name = "시나리오" + DateTime.Now.ToString("HHmmss"),
                SocialNumber = NewSocialNumber(),
                MobilePhone = "010-4000-0000",
                Memo = "시나리오 시험이 만든 수검자",
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
            CommonWorkStatusReadDto read = Run(() => new CommonStatusRepository(ConnectionString()).Read());
            if (read.Status == null)
            {
                Assert.Inconclusive("DB 오늘날짜를 받지 못했다: " + read.Result.Message);
            }

            // [X] DateTime.Today 를 쓰지 않는다 — PC 시계는 DB 시계가 아니다.
            return read.Status.Today;
        }

        private static IPatientService Patients()
        {
            return new PatientService(new PatientRepository(ConnectionString()));
        }

        private static IReservationService Reservations()
        {
            return new ReservationService(new ReservationRepository(ConnectionString()));
        }

        private static IWorkService Works()
        {
            return new WorkService(new WorkRepository(ConnectionString()));
        }

        private static T Ok<T>(Func<OperationResult<T>> call)
        {
            OperationResult<T> result;
            try
            {
                result = call();
            }
            catch (SqlException ex)
            {
                Assert.Inconclusive("DB 에 붙지 못했다 — " + ex.Message);
                throw;
            }

            Assert.IsTrue(result.IsSuccess, "서비스가 실패했다 — " + result.Message);
            return result.Value;
        }

        private static T Run<T>(Func<T> call)
        {
            try
            {
                return call();
            }
            catch (SqlException ex)
            {
                Assert.Inconclusive("DB 에 붙지 못했다 — " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 연결문자열을 여기 적지 않는다 — `App.config` 하나가 단일 출처다
        /// (ROOT `AGENTS.md` §6, 같은 폴더의 두 파일과 같은 규칙).
        /// </summary>
        private static string ConnectionString()
        {
            string path = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "src", "HealthCheckupReservationReception.WinForms", "App.config"));

            if (!File.Exists(path))
            {
                Assert.Inconclusive("App.config 을 찾지 못했다: " + path);
            }

            List<XElement> entries = XDocument.Load(path)
                .Root.Elements("connectionStrings").Elements("add").ToList();
            Assert.AreEqual(1, entries.Count, "App.config 의 연결문자열이 하나가 아니다");

            return entries[0].Attribute("connectionString").Value;
        }
    }
}
