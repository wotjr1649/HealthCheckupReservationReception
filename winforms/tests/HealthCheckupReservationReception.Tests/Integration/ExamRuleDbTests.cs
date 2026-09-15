// ── 대상판정·검사구성을 실물 DB 로 잰다 ─────────────────────────────────────
//
// 나이·성별·과거 완료이력이 TGT·NEX·AEX 를 어떻게 가르는지를 **화면이 받는 경로**(SP → DTO)
// 로 잰다. `docs/phase5/2026-09-14-Test-Scenarios.md` §3.1 이 같은 갈래를 한 번 실측해
// 적어 두었지만 그것을 지키는 것이 없었다 — 기대값의 단일 출처는 이제 이 파일이다.

using System;
using System.Collections.Generic;
using System.Linq;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Integration
{
    /// <summary>
    /// `USP_HC_예약가능정보_조회`(SP-RSV-01)의 RS3 검진대상 · RS4 국가검사 · RS5 추가검사를
    /// **갈래별로** 잰다.
    ///
    /// [X] **읽기만 한다.** 예약을 만들지 않으므로 정원을 먹지 않고 뒷정리도 없다.
    ///
    /// [I] **시드가 심은 여덟을 쓴다** (`winforms/scripts/seed-tgt-cases.sh`). 과거 완료이력은
    ///     쓰기 SP 가 없는 Seed Data 라(`02` 개발범위) 직접 `INSERT` 가 유일한 길이고, 그
    ///     일은 SQL 스크립트가 맡는다. 여기서는 **판정만** 한다. 시드가 없으면
    ///     `Inconclusive` 다 — 없는 것을 통과로 세지 않는다.
    ///
    /// [X] **DB 계층과 축이 다르다.** `03_Rule_Tests.sql` 은 `UFN_HC_검진대상확인` 같은
    ///     Rule 함수를 직접 부른다. 여기는 그 판정이 **SP 를 지나 C# DTO 까지 손실 없이
    ///     오는가**를 본다 — RS4 가 8행인지 11행인지는 Repository 가 ordinal 을 제대로
    ///     잡아야 비로소 셀 수 있다.
    /// </summary>
    [TestClass]
    public class ExamRuleDbTests
    {
        /// <summary>`00` §7.2.2 — 국가검사는 기본 8종이고 조건부는 최대 3종이다.</summary>
        private const int NexBase = 8;

        // 대상: 실물 DB — 검진대상자 여섯의 국가검사 구성 개수 (DataRow 6건)
        // 목적: 00 §7.2 NEX-01~07 이 나이와 성별로 조건부 검사를 가른다. 그 규칙이 틀리면
        //       수검자가 받아야 할 검사를 못 받거나 받지 않아도 될 검사를 받는다 — 화면은
        //       DB 가 준 목록을 그대로 그리므로 스스로 눈치채지 못한다. 2026-09-14 에 한 번
        //       실측해 문서에 적어 두었을 뿐 지키는 것이 없어서, 규칙이 바뀌어도 아무도 모른다.
        // 확인: 기본 8종 위에 조건부가 얹혀 만40 남 10 · 만56 남 11(상한) · 만54 여 9 ·
        //       만60 여 10 · 만66 여 10 · 만45 여 8 이고, 여섯 모두 검진대상여부가 1 이다.
        [DataTestMethod]
        [TestCategory("Db")]
        [DataRow("D0011", 10, "만40 남 — 조건부 둘")]
        [DataRow("D0012", 11, "만56 남 — 조건부 셋, 00 §7.2.2 의 상한")]
        [DataRow("D0013", 9, "만54 여 — 조건부 하나")]
        [DataRow("D0014", 10, "만60 여 — 조건부 둘")]
        [DataRow("D0015", 10, "만66 여 — 조건부 둘")]
        [DataRow("D0018", 8, "만45 여, 2023 완료 — 조건부 없음")]
        public void 검진대상의_국가검사는_나이와_성별대로_구성된다(string chartNo, int expected, string why)
        {
            ReservationAvailabilityReadDto read = Availability(chartNo);

            Assert.IsNotNull(read.Target, chartNo + ": RS3 검진대상이 서지 않았다 — " + why);
            Assert.IsTrue(read.Target.IsTarget, chartNo + ": 검진대상이 아니다 (" + read.Target.ReasonMessage + ")");

            Assert.AreEqual(expected, read.NexItems.Count, chartNo + ": " + why);
            Assert.IsTrue(read.NexItems.Count >= NexBase, chartNo + ": 기본 8종을 밑돈다 (00 §7.2.2)");
            Assert.IsTrue(read.NexItems.Count <= NexBase + 3, chartNo + ": 조건부가 셋을 넘었다 (00 §7.2.2)");
        }

        // 대상: 실물 DB — 검진 비대상 둘의 사유코드와 검사구성 (DataRow 2건)
        // 목적: 00 TGT-01(만20세 미만)과 TGT-04(주기 미도래)는 **막는 이유가 다르다.** 둘을
        //       같은 코드로 뭉치면 창구가 「내년에 오세요」와 「나이가 안 됐습니다」를 구분해
        //       말하지 못한다. 그리고 비대상에게 검사구성이 서면 저장까지 갈 수 있다.
        // 확인: 만19는 400, 2025 완료자는 401 이고, 둘 다 검진대상여부가 0 이며 RS4 국가검사가
        //       0행이다. RS5 추가검사는 7행이 오되 고를 수 있는 것이 하나도 없다 (05 §9.10).
        [DataTestMethod]
        [TestCategory("Db")]
        [DataRow("D0016", 400, "만19 — TGT-01 만20세 미만")]
        [DataRow("D0017", 401, "2025 완료 — TGT-04 주기 미도래")]
        public void 비대상은_사유코드로_갈리고_검사구성이_서지_않는다(string chartNo, int reason, string why)
        {
            ReservationAvailabilityReadDto read = Availability(chartNo);

            Assert.IsNotNull(read.Target, chartNo + ": RS3 검진대상이 서지 않았다 — " + why);
            Assert.IsFalse(read.Target.IsTarget, chartNo + ": 비대상이어야 한다 — " + why);
            Assert.AreEqual(reason, read.Target.ReasonCode, chartNo + ": 사유코드가 다르다 — " + why);
            Assert.AreEqual(0, read.NexItems.Count, chartNo + ": 비대상인데 국가검사가 섰다");

            // 05 §9.10 — 비대상인 ALL 에서도 7행은 오되 전부 선택가능=0 이다.
            Assert.AreEqual(7, read.AexItems.Count, chartNo + ": RS5 가 7행이 아니다");
            Assert.AreEqual(0, read.AexItems.Count(a => a.Selectable),
                chartNo + ": 비대상인데 고를 수 있는 추가검사가 있다");
        }

        // 대상: 실물 DB — 추가검사 7종의 성별 조건 (남 D0011 · 여 D0013)
        // 목적: 00 AEX-01~05 가 성별로 고를 수 있는 것을 가른다. 이 판정이 무너지면 남성에게
        //       여성 전용 검사가 열리고, 그것은 화면이 아니라 **저장 SP 가 411 로 막아야 하는**
        //       자리다. 개수만 재면 남녀가 우연히 같은 수일 때 규칙이 죽어도 통과하므로,
        //       **막힌 집합이 서로 다른지**까지 본다.
        // 확인: 남녀 모두 7행 중 5종을 고를 수 있고, 막힌 두 집합이 서로 같지 않다.
        //       막힌 항목에는 사유코드가 붙어 있다 (선택가능과 사유코드가 어긋나지 않는다).
        [TestMethod]
        [TestCategory("Db")]
        public void 추가검사는_성별로_고를_수_있는_것이_갈린다()
        {
            IList<ReservationAexItemDto> male = Availability("D0011").AexItems;
            IList<ReservationAexItemDto> female = Availability("D0013").AexItems;

            Assert.AreEqual(7, male.Count, "남 RS5 가 7행이 아니다");
            Assert.AreEqual(7, female.Count, "여 RS5 가 7행이 아니다");
            Assert.AreEqual(5, male.Count(a => a.Selectable), "남이 고를 수 있는 추가검사");
            Assert.AreEqual(5, female.Count(a => a.Selectable), "여가 고를 수 있는 추가검사");

            string blockedMale = Blocked(male);
            string blockedFemale = Blocked(female);
            Assert.AreNotEqual(blockedMale, blockedFemale,
                "남녀가 같은 항목에서 막힌다 — 성별 조건이 죽었다 (남 " + blockedMale + " · 여 " + blockedFemale + ")");

            foreach (ReservationAexItemDto item in male.Concat(female))
            {
                Assert.AreEqual(item.Selectable, item.ReasonCode == 0,
                    item.AexCode + ": 선택가능과 사유코드가 어긋난다");
            }
        }

        // ── 여기서부터는 조립용이다.

        private static string Blocked(IEnumerable<ReservationAexItemDto> items)
        {
            return string.Join("·", items.Where(a => !a.Selectable).Select(a => a.AexCode).OrderBy(c => c));
        }

        /// <summary>
        /// 그 수검자로 **열린 업무일 하나**를 찾아 예약가능정보를 받는다. 신규예약이므로
        /// 변경범위는 `ALL` 이고 TGT·NEX·AEX 가 전부 평가된다 (`05` §9.13).
        ///
        /// 날짜를 시험이 지어내지 않는다 — 요일·휴무일·마감·정원이 정하므로 DB 에 묻는다.
        /// </summary>
        private static ReservationAvailabilityReadDto Availability(string chartNo)
        {
            long patientId = PatientOf(chartNo);
            DateTime today = Today();

            for (int i = 0; i <= 14; i++)
            {
                DateTime day = today.AddDays(i);
                ReservationAvailabilityReadDto read = Ask(patientId, day, null);
                if (read.Result.Code != (int)DbCode.Ok || read.Slots == null)
                {
                    continue;
                }

                SlotInfoDto open = read.Slots.FirstOrDefault(s => s.Selectable);
                if (open == null)
                {
                    continue;
                }

                ReservationAvailabilityReadDto full = Ask(patientId, day, open.SlotCode);
                Assert.AreEqual((int)DbCode.Ok, full.Result.Code, chartNo + ": " + full.Result.Message);
                Assert.IsNotNull(full.NexItems, chartNo + ": RS4 를 읽지 못했다");
                Assert.IsNotNull(full.AexItems, chartNo + ": RS5 를 읽지 못했다");
                return full;
            }

            Assert.Inconclusive(chartNo + ": 앞으로 두 주 안에 열린 자리가 없다 — 판정하지 않는다.");
            return null;
        }

        private static ReservationAvailabilityReadDto Ask(long patientId, DateTime day, string slotCode)
        {
            return DbFixture.Run(() => new ReservationRepository(DbFixture.ConnectionString())
                .ReadAvailability(new ReservationAvailabilityRequest
                {
                    PatientId = patientId,
                    ReserveType = DbReserveType.Normal,
                    ReserveDate = day,
                    SlotCode = slotCode,
                }));
        }

        /// <summary>
        /// 시드가 심은 그 사람을 차트번호로 찾는다. 없으면 **판정하지 않는다** —
        /// `winforms/scripts/seed-tgt-cases.sh` 를 먼저 돌려야 한다.
        /// </summary>
        private static long PatientOf(string chartNo)
        {
            PatientListReadDto read = DbFixture.Run(() =>
                new PatientRepository(DbFixture.ConnectionString())
                    .Search(new PatientSearchRequest { ChartNo = chartNo }));

            Assert.AreEqual((int)DbCode.Ok, read.Result.Code, chartNo + ": " + read.Result.Message);
            if (read.Rows == null || read.Rows.Count == 0)
            {
                Assert.Inconclusive(
                    chartNo + " 가 없다 — winforms/scripts/seed-tgt-cases.sh 를 먼저 돌린다. 판정하지 않는다.");
            }

            return read.Rows[0].PatientId;
        }

        private static DateTime Today()
        {
            CommonWorkStatusReadDto read = DbFixture.Run(() =>
                new CommonStatusRepository(DbFixture.ConnectionString()).Read());
            if (read.Status == null)
            {
                Assert.Inconclusive("DB 오늘날짜를 받지 못했다: " + read.Result.Message);
            }

            // [X] DateTime.Today 를 쓰지 않는다 — PC 시계는 DB 시계가 아니다 (05 §2.3).
            return read.Status.Today;
        }
    }
}
