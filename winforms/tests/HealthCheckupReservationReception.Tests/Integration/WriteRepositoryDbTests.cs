// ── 쓰기 SP 를 실물 DB 에 붙여 돌린다 ────────────────────────────────────────
//
// `Integration/` 이 한 번도 실물로 밟지 않던 다섯이 여기 있다. 설계와 기대 형상은
// docs/phase5/2026-09-15-Integration-Test-Design.md §3 이 갖는다.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Integration
{
    /// <summary>
    /// 쓰기 SP 를 **실물 DB 에 붙여** 돌린다. `SelectRepositoryDbTests` 가 「이 파일이 도는
    /// 동안 DB 상태가 바뀌지 않는다」고 적었으므로 자리를 나눴다.
    ///
    /// [X] **재는 것은 형상이다** — Result Set 개수·컬럼 이름·타입, 그리고 변경범위별
    ///     Cardinality (`05` §9.11). 결과코드 분기는 DB 계층 계약시험이 이미 잰다.
    ///     같은 판정을 두 계층에 두지 않는다 (ROOT `AGENTS.md` §6).
    ///
    /// [X] **틀릴 수 있는 것은 사람이 두 곳에 적은 값이다.** `verify-rs-columns.sh` ·
    ///     `verify-param-size.sh` 는 `05` 문서를 보지 DB 를 보지 않으므로, 문서와 코드가
    ///     사이좋게 함께 틀리면 둘 다 green 이다. 그 틈이 이 파일의 대상이다.
    ///
    /// [I] **만든 것은 시험이 되돌린다** (`TestCleanup`). 시간대 정원은 공유 자원이라
    ///     남기면 다음 실행이 `305` 로 막힌다 (2026-09-14 실측).
    ///
    /// [I] 운영시간·마감의 창은 <see cref="DbFixture"/> 가 연다. 그래서 밤에 돌려도
    ///     `309`·`304` 로 `Inconclusive` 가 되지 않는다.
    /// </summary>
    [TestClass]
    public class WriteRepositoryDbTests
    {
        private const string Operator = "통합시험";

        /// <summary>
        /// 아무 시험도 쓰지 않는 먼 미래다. `UFN_HC_일정확인` 이 휴무일을 업무일 판정에
        /// 쓰므로 가까운 날짜를 고르면 **다른 시험이 전부 `308` 로 막힌다.**
        /// </summary>
        private static readonly DateTime HolidayProbe = new DateTime(2099, 12, 30);

        /// <summary>이 시험이 만든 업무. 상태에 맞는 취소로 되돌린다.</summary>
        private readonly List<long> _created = new List<long>();

        /// <summary>심어 둔 자체휴무일. 시험이 끝까지 못 가면 여기서 지운다.</summary>
        private DateTime? _holiday;

        [TestCleanup]
        public void 만든_것을_되돌린다()
        {
            foreach (long workId in _created)
            {
                try
                {
                    WorkDetailReadDto read = Works().ReadDetail(workId);
                    if (read.Detail == null)
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

            if (_holiday != null)
            {
                try
                {
                    RemoveHoliday(_holiday.Value);
                }
                catch (SqlException)
                {
                }

                _holiday = null;
            }
        }

        // 대상: 실물 DB — 예약가능정보 조회(SP-RSV-01)의 변경범위 다섯과 그 Result Set 형상
        // 목적: 05 §9.4 가 「무엇이 바뀌었는지 보내지 않는다 — 원하는 상태를 통째로 보내면
        //       DB 가 현재 행과 견주어 잰다」로 정했고, 그 판정에 따라 RS2~RS5 가 서기도 하고
        //       0행이 되기도 한다 (05 §9.11). 화면은 그 형상에 기대어 시간대 칸과 추가검사
        //       칸을 그리므로, 변경범위 하나가 어긋나면 화면이 빈 칸을 「선택지가 없다」로
        //       읽는다. 문서 대조 게이트는 05 를 볼 뿐 DB 를 보지 않아 이 틈을 못 막는다.
        // 확인: 다섯 호출이 각각 변경범위 ALL·NONE·SLOT·EXTRA·SLOT_EXTRA 를 돌려주고,
        //       RS2 시간대정보·RS3 검진대상·RS4 국가검사항목·RS5 추가검사항목의 행수가
        //       05 §9.11 표 그대로다. ALL 은 「0 또는 …」의 0 쪽을 허용하지 않는다 — 새로
        //       만든 수검자이고 창이 열려 있으므로 일정 평가가 가능한 쪽이 참이어야 한다.
        [TestMethod]
        [TestCategory("Db")]
        public void SP_RSV_01_은_변경범위_다섯을_계약된_Cardinality_로_낸다()
        {
            DateTime day = Today();

            // ALL 은 **예약이 없는 쪽**에게 묻는다. 다른 유효업무가 있으면 저장가능=0 이 되고
            // 그때 RS3~RS5 가 서는지는 계약이 가르지 않는다 — 재려는 것이 형상이므로
            // 갈리지 않는 쪽으로 묻는다.
            long visitor = NewPatient();
            long owner = NewPatient();

            ReservationAvailabilityReadDto opening = Opening(visitor, day);
            SlotInfoDto chosen = OpenSlot(opening);
            string other = OtherSlot(opening, chosen.SlotCode);

            Shape(Availability(visitor, null, null, day, chosen.SlotCode, NoAex()),
                  "ALL", 2, 1, 8, 11, 7);

            WorkSaveReadDto booked = Book(owner, day, chosen.SlotCode);
            long workId = booked.Row.WorkId;
            byte[] rowVersion = booked.Row.RowVersion;

            Shape(Availability(owner, workId, rowVersion, day, chosen.SlotCode, NoAex()),
                  "NONE", 0, 0, 0, 0, 0);
            Shape(Availability(owner, workId, rowVersion, day, other, NoAex()),
                  "SLOT", 2, 0, 0, 0, 0);
            Shape(Availability(owner, workId, rowVersion, day, chosen.SlotCode, OneAex(0)),
                  "EXTRA", 0, 0, 0, 0, 7);
            Shape(Availability(owner, workId, rowVersion, day, other, OneAex(0)),
                  "SLOT_EXTRA", 2, 0, 0, 0, 7);
        }

        // 대상: 실물 DB — 예약 변경(SP-RSV-03)의 AEX 실제 변경 경로와 그 No-op
        // 목적: 이 SP 는 C# 에서 한 번도 실행된 적이 없다. 05 §11.2 가 「AEX 실제 변경이면
        //       행버전이 자동으로 바뀌고 새 행버전을 반환한다」, 「동일집합이면 UPDATE 없이
        //       결과코드 1 과 기존 행버전」으로 갈랐는데, 그 둘이 뒤섞이면 화면이 낡은 토큰을
        //       쥔 채 다음 저장에서 601 로 막히거나 — 더 나쁘게 — 남의 변경을 덮어쓴다.
        //       행버전은 BINARY(8) 왕복이라 문자열로 바꾸는 순간 조용히 깨진다 (05 §16.3).
        // 확인: 선택 가능한 추가검사 하나를 켜서 보내면 결과코드 0 · 상태코드 RSV 유지 ·
        //       행버전이 달라지고, 그 새 행버전으로 같은 값을 한 번 더 보내면 결과코드 1 에
        //       행버전이 그대로다.
        [TestMethod]
        [TestCategory("Db")]
        public void SP_RSV_03_은_AEX_를_바꾸면_새_행버전을_내고_같은_값에는_1_이다()
        {
            long patientId = NewPatient();
            DateTime day = Today();
            SlotInfoDto open = OpenSlot(Opening(patientId, day));
            WorkSaveReadDto booked = Book(patientId, day, open.SlotCode);

            // 어느 추가검사를 켤 수 있는지는 성별·중복 Rule 이 정한다 (05 §6.4) — DB 에 묻는다.
            bool[] want = SelectableAex(
                Availability(patientId, booked.Row.WorkId, booked.Row.RowVersion, day, open.SlotCode, OneAex(0)));

            WorkSaveReadDto changed = Run(() => Reservations().Change(new ReservationChangeRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = booked.Row.RowVersion,
                ReserveDate = day,
                SlotCode = open.SlotCode,
                AexSelected = want,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.Ok, changed.Result.Code, "예약변경: " + changed.Result.Message);
            Assert.IsNotNull(changed.Row, "성공인데 RS1 이 없다 (05 §11)");
            Assert.AreEqual(DbWorkStatus.Reserved, changed.Row.StatusCode, "AEX 변경이 상태를 옮겼다");
            Assert.AreNotEqual(Hex(booked.Row.RowVersion), Hex(changed.Row.RowVersion),
                "행이 바뀌었는데 행버전이 그대로다 (05 §11.2)");

            WorkSaveReadDto again = Run(() => Reservations().Change(new ReservationChangeRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = changed.Row.RowVersion,
                ReserveDate = day,
                SlotCode = open.SlotCode,
                AexSelected = want,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.NoChange, again.Result.Code,
                "같은 값인데 변경으로 처리됐다: " + again.Result.Message);
            Assert.AreEqual(Hex(changed.Row.RowVersion), Hex(again.Row.RowVersion),
                "No-op 인데 행버전이 바뀌었다 (05 §11.2)");
        }

        // 대상: 실물 DB — 접수 뒤 추가검사 변경(SP-RCP-02)의 실제 변경 경로와 그 No-op
        // 목적: 이 SP 도 C# 에서 한 번도 실행된 적이 없고, 그 앞에 RCP 상태 업무가 필요해
        //       예약→접수까지 세우지 않으면 닿지 못한다. 05 §12.2 가 「예약일·시간대·TGT·NEX
        //       를 변경하거나 재평가하지 않는다」로 못박았으므로 DLG-RCP-02 는 AEX 일곱과
        //       동시성 토큰만 보낸다 — 그 좁은 계약이 실물에서도 참인지, 상태가 RCP 로 남는지는
        //       붙어 봐야 안다. 여기서 상태가 옮겨지면 접수한 사람이 목록에서 사라진다.
        // 확인: 접수완료가 결과코드 0 · 상태 RCP 이고, 상세 RS5 가 선택가능하다고 적은 추가검사
        //       하나를 켜면 결과코드 0 · 상태 RCP 유지 · 행버전이 달라지며, 같은 값을 한 번 더
        //       보내면 결과코드 1 에 행버전이 그대로다.
        [TestMethod]
        [TestCategory("Db")]
        public void SP_RCP_02_는_접수_뒤_AEX_만_바꾸고_상태를_옮기지_않는다()
        {
            long patientId = NewPatient();
            DateTime day = Today();
            SlotInfoDto open = OpenSlot(Opening(patientId, day));
            WorkSaveReadDto booked = Book(patientId, day, open.SlotCode);

            WorkSaveReadDto received = Run(() => Works().CompleteReception(new WorkActionRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = booked.Row.RowVersion,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.Ok, received.Result.Code, "접수완료: " + received.Result.Message);
            Assert.AreEqual(DbWorkStatus.Received, received.Row.StatusCode);

            // 접수 화면이 보는 자리에서 고른다 — 상세 RS5 추가검사구성 (05 §8.2).
            WorkDetailReadDto detail = Run(() => Works().ReadDetail(booked.Row.WorkId));
            Assert.AreEqual(7, detail.AexOptions.Count, "RS5 가 7행이 아니다 (05 §8.2)");
            bool[] want = TurnOn(detail.AexOptions.Select(o => o.Selectable).ToList());

            WorkSaveReadDto changed = Run(() => Works().ChangeExtraExam(new ExtraExamChangeRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = received.Row.RowVersion,
                AexSelected = want,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.Ok, changed.Result.Code, "추가검사 변경: " + changed.Result.Message);
            Assert.IsNotNull(changed.Row, "성공인데 RS1 이 없다 (05 §11)");
            Assert.AreEqual(DbWorkStatus.Received, changed.Row.StatusCode,
                "추가검사 변경이 상태를 옮겼다 (05 §12.2)");
            Assert.AreNotEqual(Hex(received.Row.RowVersion), Hex(changed.Row.RowVersion),
                "행이 바뀌었는데 행버전이 그대로다");

            WorkSaveReadDto again = Run(() => Works().ChangeExtraExam(new ExtraExamChangeRequest
            {
                WorkId = booked.Row.WorkId,
                RowVersion = changed.Row.RowVersion,
                AexSelected = want,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.NoChange, again.Result.Code,
                "같은 값인데 변경으로 처리됐다: " + again.Result.Message);
            Assert.AreEqual(Hex(changed.Row.RowVersion), Hex(again.Row.RowVersion),
                "No-op 인데 행버전이 바뀌었다 (05 §12.2)");
        }

        // 대상: 실물 DB — 자체휴무일 저장(SP-HOL-02)의 성공 경로 둘과 삭제(SP-HOL-04)
        // 목적: 지금 도는 것은 저장의 101 실패 경로뿐이고 그 시험 자신이 「아무것도 쓰지
        //       않는다」고 적는다 — 코드 검증이 잠금·Transaction 앞에서 끝난다. 그래서 등록도
        //       수정도 삭제도 실물에서 한 번도 쓰인 적이 없다. 셋을 한 시험에 이은 것은
        //       심는 것이 곧 지우는 준비이기 때문이고, 삭제가 요구하는 행버전은 저장이 준
        //       BINARY(8) 을 그대로 되돌려 보내야만 맞는다 (05 §16.3).
        // 확인: CREATE_HOLIDAY 가 결과코드 0 에 RS1 휴무일자·행버전 8바이트를 내고, 목록
        //       조회가 그 날짜를 휴무구분 자체휴무일 1행으로 싣는다. UPDATE_HOLIDAY 가
        //       결과코드 0 에 새 행버전을 내고, 그 행버전으로 삭제하면 결과코드 0 이며
        //       RS1 이 없고(05 §12.8) 목록에서 사라진다.
        [TestMethod]
        [TestCategory("Db")]
        public void SP_HOL_02_와_SP_HOL_04_는_한_행을_심고_고치고_지운다()
        {
            IHolidayRepository holidays = Holidays();

            // 앞선 실행이 도중에 죽어 남겨 둔 행이 있으면 801 이 난다. 먼저 치운다.
            Run(() => RemoveHoliday(HolidayProbe));

            HolidaySaveReadDto created = Run(() => holidays.Save(new HolidaySaveRequest
            {
                HolidayDate = HolidayProbe,
                HolidayName = "통합시험 자체휴무일",
                IsActive = true,
                Memo = "통합시험이 심고 지운다",
            }, DbHolidayAction.Create));

            Assert.AreEqual((int)DbCode.Ok, created.Result.Code, "등록: " + created.Result.Message);
            _holiday = HolidayProbe;
            Assert.AreEqual(HolidayProbe, created.HolidayDate, "RS1 휴무일자가 요청한 날짜가 아니다");
            Assert.IsNotNull(created.RowVersion, "RS1 행버전이 없다 (05 §12.6)");
            Assert.AreEqual(8, created.RowVersion.Length, "행버전이 BINARY(8) 이 아니다");

            HolidayListItemDto row = OneHoliday(HolidayProbe);
            Assert.IsNotNull(row, "등록했는데 목록에 없다");
            Assert.AreEqual(DbHolidayType.Own, row.HolidayType, "자체휴무일로 서지 않았다 (00 HOL-05)");
            Assert.AreEqual(Hex(created.RowVersion), Hex(row.RowVersion), "목록의 행버전이 등록이 준 것과 다르다");

            HolidaySaveReadDto updated = Run(() => holidays.Save(new HolidaySaveRequest
            {
                HolidayDate = HolidayProbe,
                RowVersion = created.RowVersion,
                HolidayName = "통합시험 자체휴무일 (수정)",
                IsActive = false,
                Memo = "통합시험이 고쳤다",
            }, DbHolidayAction.Update));

            Assert.AreEqual((int)DbCode.Ok, updated.Result.Code, "수정: " + updated.Result.Message);
            Assert.AreNotEqual(Hex(created.RowVersion), Hex(updated.RowVersion),
                "값이 바뀌었는데 행버전이 그대로다 (05 §12.6)");

            HolidaySaveReadDto deleted = Run(() => holidays.Delete(HolidayProbe, updated.RowVersion));

            Assert.AreEqual((int)DbCode.Ok, deleted.Result.Code, "삭제: " + deleted.Result.Message);
            Assert.IsNull(deleted.HolidayDate, "삭제에는 RS1 이 없다 (05 §12.8)");
            _holiday = null;

            Assert.IsNull(OneHoliday(HolidayProbe), "물리 삭제인데 행이 남았다 (05 §12.8)");
        }

        // ── 여기서부터는 조립용이다.

        /// <summary>`05` §9.11 표 한 줄을 그대로 건다.</summary>
        private static void Shape(ReservationAvailabilityReadDto read, string scope,
                                  int slots, int target, int nexMin, int nexMax, int aex)
        {
            Assert.AreEqual((int)DbCode.Ok, read.Result.Code, scope + " RS0: " + read.Result.Message);
            Assert.IsNotNull(read.Summary, scope + ": RS1 예약요약이 없다");
            Assert.AreEqual(scope, read.Summary.ChangeScope, "변경범위가 다르다 (05 §9.4)");

            // 0행과 「그 Result Set 이 없다」는 다르다 — SP 는 늘 여섯을 낸다 (05 §9.5).
            Assert.IsNotNull(read.Slots, scope + ": RS2 를 읽지 못했다");
            Assert.IsNotNull(read.NexItems, scope + ": RS4 를 읽지 못했다");
            Assert.IsNotNull(read.AexItems, scope + ": RS5 를 읽지 못했다");

            Assert.AreEqual(slots, read.Slots.Count, scope + ": RS2 시간대정보 (05 §9.11)");
            Assert.AreEqual(target, read.Target == null ? 0 : 1, scope + ": RS3 검진대상 (05 §9.11)");
            Assert.IsTrue(read.NexItems.Count >= nexMin && read.NexItems.Count <= nexMax,
                scope + ": RS4 국가검사항목이 " + read.NexItems.Count + "행이다 — 기대 "
                + nexMin + "~" + nexMax + " (05 §9.11)");
            Assert.AreEqual(aex, read.AexItems.Count, scope + ": RS5 추가검사항목 (05 §9.11)");
        }

        private static ReservationAvailabilityReadDto Availability(
            long patientId, long? workId, byte[] rowVersion, DateTime day, string slotCode, bool[] aex)
        {
            return Run(() => Reservations().ReadAvailability(new ReservationAvailabilityRequest
            {
                PatientId = patientId,
                WorkId = workId,
                RowVersion = rowVersion,
                ReserveType = DbReserveType.Normal,
                ReserveDate = day,
                SlotCode = slotCode,
                AexSelected = aex,
            }));
        }

        /// <summary>날짜만 고른 상태로 묻는다 — 그때 SP 가 AM·PM 둘 다 낸다 (`05` §9.3).</summary>
        private static ReservationAvailabilityReadDto Opening(long patientId, DateTime day)
        {
            ReservationAvailabilityReadDto read = Availability(patientId, null, null, day, null, NoAex());
            Assert.AreEqual((int)DbCode.Ok, read.Result.Code, "예약가능정보: " + read.Result.Message);
            Assert.IsNotNull(read.Slots, "RS2 를 읽지 못했다");
            return read;
        }

        /// <summary>
        /// 열린 시간대를 **DB 에 물어서** 고른다. AM·PM 어느 쪽이 열려 있는지는 요일·마감·정원이
        /// 정하므로 시험이 정하지 않는다 (`05` §9.7).
        /// </summary>
        private static SlotInfoDto OpenSlot(ReservationAvailabilityReadDto read)
        {
            SlotInfoDto open = read.Slots.FirstOrDefault(s => s.Selectable);
            if (open == null)
            {
                Assert.Inconclusive("오늘 열린 시간대가 없다 — 요일·정원·휴무 중 하나다. 판정하지 않는다.");
            }

            return open;
        }

        private static string OtherSlot(ReservationAvailabilityReadDto read, string slotCode)
        {
            SlotInfoDto other = read.Slots.FirstOrDefault(s => !s.SlotCode.Equals(slotCode, StringComparison.Ordinal));
            if (other == null)
            {
                Assert.Inconclusive("시간대가 하나뿐이라 SLOT 변경을 물을 수 없다.");
            }

            return other.SlotCode;
        }

        /// <summary>
        /// RS5 는 `추가검사코드` 오름차순이고 `@추가검사01`~`07` 도 같은 순서다 (`05` §9.6) —
        /// 자리 번호가 곧 Parameter 번호이므로 코드 문자열을 C# 에 적지 않는다.
        /// </summary>
        private static bool[] SelectableAex(ReservationAvailabilityReadDto read)
        {
            Assert.AreEqual((int)DbCode.Ok, read.Result.Code, "예약가능정보: " + read.Result.Message);
            Assert.AreEqual(7, read.AexItems.Count, "RS5 가 7행이 아니다 (05 §9.10)");
            return TurnOn(read.AexItems.Select(a => a.Selectable).ToList());
        }

        private static bool[] TurnOn(IList<bool> selectable)
        {
            int index = -1;
            for (int i = 0; i < selectable.Count; i++)
            {
                if (selectable[i])
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                Assert.Inconclusive("이 수검자가 고를 수 있는 추가검사가 없다 — 성별·중복 Rule 이다. 판정하지 않는다.");
            }

            bool[] want = NoAex();
            want[index] = true;
            return want;
        }

        private static bool[] NoAex()
        {
            return new bool[ReservationAvailabilityRequest.AexParameterCount];
        }

        private static bool[] OneAex(int index)
        {
            bool[] want = NoAex();
            want[index] = true;
            return want;
        }

        private WorkSaveReadDto Book(long patientId, DateTime day, string slotCode)
        {
            WorkSaveReadDto saved = Run(() => Reservations().Register(new ReservationSaveRequest
            {
                PatientId = patientId,
                ReserveType = DbReserveType.Normal,
                ReserveDate = day,
                SlotCode = slotCode,
                AexSelected = NoAex(),
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.Ok, saved.Result.Code, "예약: " + saved.Result.Message);
            Assert.IsNotNull(saved.Row, "성공인데 RS1 이 없다 (05 §11)");

            // 정리 대상으로 적어 둔다 — 성공한 것만 실제 행이 된다.
            _created.Add(saved.Row.WorkId);
            return saved;
        }

        /// <summary>
        /// 매 실행마다 새 수검자를 만든다. 생년월일은 `1986-01-15` 로 고정이라 나이가 흔들리지
        /// 않고, 주민번호 뒷자리는 실행 시각에서 만들어 이전 실행과 겹치지 않는다.
        /// **임의 생성한 테스트 값만 쓴다** (`00` §2.1).
        /// </summary>
        private static long NewPatient()
        {
            PatientSaveReadDto saved = Run(() => Patients().Register(new PatientSaveRequest
            {
                AutoChartNo = true,
                Name = "통합" + DateTime.Now.ToString("HHmmssfff"),
                SocialNumber = NewSocialNumber(),
                MobilePhone = "010-4100-0000",
                Memo = "통합시험이 만든 수검자",
                SimilarConfirmed = true,
                OperatorName = Operator,
            }));

            Assert.AreEqual((int)DbCode.Ok, saved.Result.Code, "수검자 등록: " + saved.Result.Message);
            Assert.IsTrue(saved.Rows != null && saved.Rows.Count == 1, "등록 결과가 1행이 아니다");
            return saved.Rows[0].PatientId;
        }

        private static string NewSocialNumber()
        {
            // 860115 + 1(1900년대 남) + 6자리. 뒤 여섯은 100000 주기로 도는 시각값이다.
            long tail = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % 1000000;
            return "8601151" + tail.ToString("D6");
        }

        private static HolidayListItemDto OneHoliday(DateTime day)
        {
            HolidayListReadDto read = Run(() => Holidays().Search(new HolidaySearchRequest
            {
                FromDate = day,
                ToDate = day,
            }));

            Assert.AreEqual((int)DbCode.Ok, read.Result.Code, "휴무일 목록: " + read.Result.Message);
            Assert.IsNotNull(read.Rows, "RS1 을 읽지 못했다");
            return read.Rows.FirstOrDefault(r => r.HolidayDate == day);
        }

        /// <summary>남아 있으면 지운다. 없으면 아무 일도 하지 않는다.</summary>
        private static bool RemoveHoliday(DateTime day)
        {
            HolidayListReadDto read = Holidays().Search(new HolidaySearchRequest { FromDate = day, ToDate = day });
            HolidayListItemDto row = read.Rows == null
                ? null
                : read.Rows.FirstOrDefault(r => r.HolidayDate == day);
            if (row == null)
            {
                return false;
            }

            Holidays().Delete(day, row.RowVersion);
            return true;
        }

        private static DateTime Today()
        {
            CommonWorkStatusReadDto read = Run(() => new CommonStatusRepository(Connection()).Read());
            if (read.Status == null)
            {
                Assert.Inconclusive("DB 오늘날짜를 받지 못했다: " + read.Result.Message);
            }

            // [X] DateTime.Today 를 쓰지 않는다 — PC 시계는 DB 시계가 아니다 (05 §2.3).
            return read.Status.Today;
        }

        private static string Hex(byte[] value)
        {
            return value == null ? "(없다)" : BitConverter.ToString(value);
        }

        private static IPatientRepository Patients()
        {
            return new PatientRepository(Connection());
        }

        private static IReservationRepository Reservations()
        {
            return new ReservationRepository(Connection());
        }

        private static IWorkRepository Works()
        {
            return new WorkRepository(Connection());
        }

        private static IHolidayRepository Holidays()
        {
            return new HolidayRepository(Connection());
        }

        private static string Connection()
        {
            return DbFixture.ConnectionString();
        }

        private static T Run<T>(Func<T> call)
        {
            return DbFixture.Run(call);
        }
    }
}
