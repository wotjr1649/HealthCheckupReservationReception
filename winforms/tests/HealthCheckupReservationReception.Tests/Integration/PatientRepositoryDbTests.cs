using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Integration
{
    /// <summary>
    /// **실물 DB 를 거치는 유일한 시험이다.** 나머지 전부는 fake 로 돈다.
    ///
    /// [X] 여기가 없으면 Repository 는 한 번도 실행되지 않은 채 남는다. `RSC-002` 가 컬럼
    ///     이름을 `05` 계약과 대조하지만 그것은 **문서 대조**다 — SP 가 실제로 그 이름으로
    ///     돌려주는지, 타입이 맞는지는 붙어 봐야 안다.
    ///
    /// [I] `ReadRows` 는 ordinal 을 루프 **밖에서** 잡는다. 그래서 0건이 나와도 RS1 의
    ///     컬럼 이름 열한 개가 전건 검증된다 — 데이터를 넣지 않고도 계약을 잰다.
    ///
    /// [I] DB 가 없으면 `Inconclusive` 다. 판정하지 못한 검사는 PASS 가 아니다
    ///     (`database/AGENTS.md` §10 의 같은 규칙).
    /// </summary>
    [TestClass]
    public class PatientRepositoryDbTests
    {
        // 05 §7.2 — 조건 없이 부르면 103 이다. 조건 하나를 실어 성공 경로로 간다.
        private const string ProbeChartNo = "존재하지-않는-차트번호";

        [TestMethod]
        [TestCategory("Db")]
        public void SP_PAT_01_은_RS0_와_RS1_열한_컬럼을_계약대로_돌려준다()
        {
            IPatientRepository repository = NewRepository();

            PatientListReadDto read = Run(() => repository.Search(new PatientSearchRequest { ChartNo = ProbeChartNo }));

            Assert.IsNotNull(read.Result, "RS0 을 읽지 못했다 (05 §3.1)");
            Assert.AreEqual((int)DbCode.Ok, read.Result.Code, "결과코드: " + read.Result.Message);

            // 여기까지 왔다는 것은 GetOrdinal 열한 번이 전부 이름을 찾았다는 뜻이다.
            Assert.IsNotNull(read.Rows, "RS1 을 읽지 못했다");
            Assert.AreEqual(0, read.Rows.Count, "없는 차트번호인데 행이 나왔다");
        }

        // 05 §7.3 — Patient 가 없으면 결과코드 200 이다. 값을 넣지 않고 실패 경로를 잰다.
        [TestMethod]
        [TestCategory("Db")]
        public void SP_PAT_02_는_없는_수검자에_200_을_돌려준다()
        {
            IPatientRepository repository = NewRepository();

            PatientDetailReadDto read = Run(() => repository.ReadDetail(-1));

            Assert.IsNotNull(read.Result, "RS0 을 읽지 못했다 (05 §3.1)");
            Assert.AreEqual((int)DbCode.PatientNotFound, read.Result.Code, "결과코드: " + read.Result.Message);
            Assert.IsNull(read.Detail, "실패인데 RS1 을 읽었다");
        }

        /// <summary>
        /// 쓰기 경로 한 벌을 실물로 잰다 — 등록(0) → 상세(RS1 열다섯) → 같은 주민번호 재등록(2)
        /// → 수정(0, 행버전 교체) → 옛 행버전 재수정(601).
        ///
        /// [I] **한 시험에 이어 붙인 것은 이 다섯이 같은 수검자 한 행에 매여 있기 때문이다.**
        ///     MSTest 는 시험 순서를 보장하지 않으므로 나누면 순서에 기대게 된다.
        ///
        /// [X] **이 시험은 DB 에 행을 남긴다.** `03` §5.1 이 삭제를 제공하지 않아 화면으로
        ///     지울 길이 없다 — 남기기로 사용자가 정했다(2026-09-09). 그래서 주민번호를
        ///     실행 시각으로 만들어 회차마다 다른 사람이 되게 한다.
        /// </summary>
        [TestMethod]
        [TestCategory("Db")]
        public void SP_PAT_03_과_04_로_등록부터_동시성까지_한_벌을_돈다()
        {
            IPatientRepository repository = NewRepository();
            string social = NewSocialNumber(repository);
            string name = "시험수검자" + social.Substring(7);

            // ── 등록 (05 §10.1). 차트번호는 저장 Transaction 이 확정한다 (03 §6.3).
            PatientSaveReadDto created = Run(() => repository.Register(NewRequest(social, name, true)));
            Assert.IsNotNull(created.Result, "RS0 을 읽지 못했다 (05 §3.1)");
            SkipOutsideBusinessHours(created.Result);
            Assert.AreEqual((int)DbCode.Ok, created.Result.Code, "결과코드: " + created.Result.Message);
            Assert.IsNotNull(created.Rows, "성공인데 RS1 이 없다 (05 §10.1)");
            Assert.AreEqual(1, created.Rows.Count);

            // 여기까지 왔다는 것은 RS1 여덟 컬럼이 전건 이름으로 잡혔다는 뜻이다.
            PatientSaveResultDto row = created.Rows[0];
            Assert.IsTrue(row.PatientId > 0);
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.ChartNo), "자동발급 차트번호가 비었다");
            Assert.AreEqual(social, row.SocialNumber);
            Assert.AreEqual("19990707", row.Birthday, "계산열이 유도한 생년월일이 다르다 (05 §10.1)");
            Assert.AreEqual("F", row.Gender);
            Assert.AreEqual(8, row.RowVersion.Length);

            // ── 상세 (05 §7.3). 수검자 한 행이 생겼으므로 RS1 열다섯 컬럼을 이제 잴 수 있다.
            PatientDetailReadDto detail = Run(() => repository.ReadDetail(row.PatientId));
            Assert.AreEqual((int)DbCode.Ok, detail.Result.Code, "결과코드: " + detail.Result.Message);
            Assert.IsNotNull(detail.Detail, "RS1 을 읽지 못했다");
            Assert.AreEqual(name, detail.Detail.Name);
            Assert.AreEqual("메모", detail.Detail.Memo);
            Assert.IsTrue(detail.Detail.HepatitisBExcluded, "B형간염 제외값이 저장되지 않았다 (03 §6.2a)");
            Assert.AreEqual(8, detail.Detail.RowVersion.Length);

            // ── 같은 주민번호 + 같은 이름 → 신규 INSERT 없이 기존 1행 (05 §10.1 결과표).
            PatientSaveReadDto again = Run(() => repository.Register(NewRequest(social, name, true)));
            Assert.AreEqual((int)DbCode.ExistingPatient, again.Result.Code, "결과코드: " + again.Result.Message);
            Assert.AreEqual(row.PatientId, again.Rows[0].PatientId);

            // ── 수정 (05 §10.2). RS1 은 세 컬럼이고 행버전이 바뀐다 (05 §16.3).
            PatientSaveRequest update = NewRequest(social, name, false);
            update.PatientId = row.PatientId;
            update.RowVersion = detail.Detail.RowVersion;
            update.ChartNo = row.ChartNo;
            update.Memo = "고친 메모";
            PatientSaveReadDto updated = Run(() => repository.Update(update));
            Assert.AreEqual((int)DbCode.Ok, updated.Result.Code, "결과코드: " + updated.Result.Message);
            Assert.AreEqual(1, updated.Rows.Count);
            CollectionAssert.AreNotEqual(detail.Detail.RowVersion, updated.Rows[0].RowVersion,
                "실제로 고쳤는데 행버전이 그대로다");

            // ── 옛 행버전으로 다시 고치면 601 이다 (05 §16.3 · 03 §16).
            update.Memo = "또 고친 메모";
            PatientSaveReadDto stale = Run(() => repository.Update(update));
            Assert.AreEqual((int)DbCode.RowChanged, stale.Result.Code, "결과코드: " + stale.Result.Message);
        }

        /// <summary>
        /// 이름 + 산출 생년월일이 같고 주민번호가 다르면 `203` 과 함께 후보 RS1 을 준다 —
        /// **실패인데 후속 Result Set 을 읽는 예외다** (05 §3.5). 확인값을 실으면 통과한다.
        /// </summary>
        [TestMethod]
        [TestCategory("Db")]
        public void SP_PAT_03_은_유사후보에_203_과_후보목록을_돌려준다()
        {
            IPatientRepository repository = NewRepository();
            string first = NewSocialNumber(repository);
            string second = NewSocialNumber(repository);
            string name = "유사수검자" + first.Substring(7);

            PatientSaveReadDto created = Run(() => repository.Register(NewRequest(first, name, true)));
            SkipOutsideBusinessHours(created.Result);
            Assert.AreEqual((int)DbCode.Ok, created.Result.Code, "결과코드: " + created.Result.Message);

            PatientSaveReadDto similar = Run(() => repository.Register(NewRequest(second, name, true)));
            Assert.AreEqual((int)DbCode.SimilarPatient, similar.Result.Code, "결과코드: " + similar.Result.Message);
            Assert.IsNotNull(similar.Rows, "203 인데 후보 RS1 이 없다 (05 §3.5)");
            Assert.IsTrue(similar.Rows.Count >= 1);

            // 03 §6.5 — `별도 수검자로 계속` 은 확인값을 실어 같은 요청을 다시 보낸다.
            PatientSaveRequest confirmed = NewRequest(second, name, true);
            confirmed.SimilarConfirmed = true;
            PatientSaveReadDto separate = Run(() => repository.Register(confirmed));
            Assert.AreEqual((int)DbCode.Ok, separate.Result.Code, "결과코드: " + separate.Result.Message);
            Assert.AreNotEqual(created.Rows[0].PatientId, separate.Rows[0].PatientId);
        }

        // [X] 초판은 꼬리를 HHmmss 로 만들었다. 같은 초 안에 도는 두 시험이 같은 값을 받아
        //     뒤 시험이 202(동일 주민번호·다른 이름)로 떨어졌다 — 실측으로 잡혔다.
        // [X] 마이크로초 + 단조 카운터로 고쳤더니 **회차 안**만 안전해졌다. 꼬리가 여섯 자리라
        //     공간이 10^6 이고 시험이 만든 행은 영영 지우지 않으므로(07 §12.9), 누적 행이
        //     늘수록 이전 회차의 값과 부딪힐 확률이 커진다. 부딪히면 정확히 같은 202 로 깨진다.
        private static int _seq;

        /// <summary>
        /// 앞 6자리는 고정 날짜(1999-07-07), 7번째는 `2`(1900년대 여), 나머지 여섯은 실행 시각이다.
        /// **아직 쓰이지 않은 값인지 SP-PAT-01 로 확인하고 돌려준다** — 회차 사이 충돌까지 막는
        /// 것은 이것뿐이다. 확률에 기대지 않는다.
        /// </summary>
        private static string NewSocialNumber(IPatientRepository repository)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                long tail = (DateTime.Now.Ticks / 10 + Interlocked.Increment(ref _seq)) % 1000000;
                string candidate = "990707" + "2" + tail.ToString("D6");

                PatientListReadDto read = Run(() =>
                    repository.Search(new PatientSearchRequest { SocialNumber = candidate }));
                if (read.Result != null && read.Result.Success
                    && (read.Rows == null || read.Rows.Count == 0))
                {
                    return candidate;
                }
            }

            Assert.Inconclusive("쓰이지 않은 주민번호를 스무 번 안에 찾지 못했다 — 누적 행이 너무 많다");
            return null;
        }

        /// <summary>
        /// 05 §10.1 검증순서에 `현재 공통 업무 가능` 이 있다 — 업무시간 밖이면 308·309 로 막힌다.
        /// 그때는 실패가 아니라 **재지 못한 것**이다 (`database/AGENTS.md` §10).
        /// </summary>
        private static void SkipOutsideBusinessHours(DbResult result)
        {
            if (result.Code == (int)DbCode.CenterClosed || result.Code == (int)DbCode.OutsideHours)
            {
                Assert.Inconclusive("업무시간 밖이라 쓰기 SP 를 재지 못했다 — " + result.Message);
            }
        }

        private static PatientSaveRequest NewRequest(string social, string name, bool autoChartNo)
        {
            return new PatientSaveRequest
            {
                AutoChartNo = autoChartNo,
                Name = name,
                SocialNumber = social,
                MobilePhone = "010-0000-0000",
                Memo = "메모",
                HepatitisBExcluded = true,
                OperatorName = "결선시험",
            };
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

        private static IPatientRepository NewRepository()
        {
            return new PatientRepository(ConnectionString());
        }

        /// <summary>
        /// 연결문자열을 여기 적지 않는다. `App.config` 하나가 단일 출처이고 `CFG-001`~`003`
        /// 이 그것을 `05` §1.1 과 대조한다 (ROOT `AGENTS.md` §6).
        ///
        /// `ConfigurationManager` 를 쓰지 않는 것은 그것이 machine.config 의 항목까지 섞어
        /// 주기 때문이다 — 파일 안에 실제로 적힌 것만 본다.
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
