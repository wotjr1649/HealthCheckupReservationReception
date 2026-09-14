using System;
using System.Data.SqlClient;
using System.Threading;
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

        // 대상: 실물 DB — PatientRepository.Search 가 부르는 수검자목록 조회(SP-PAT-01)
        // 목적: 05 §7.2 가 정한 컬럼 이름을 C# 이 그대로 읽는지는 붙어 봐야 안다. 컬럼 이름
        //       오타는 컴파일도 되고 fake 를 쓰는 단위시험도 통과하며 실행할 때만 터진다 —
        //       verify-rs-columns.sh 는 05 문서를 보지 DB 를 보지 않는다.
        // 확인: RS0 를 읽을 수 있고 결과코드가 0 이며, 없는 차트번호로 물어도 RS1 이 null 이
        //       아니라 0건짜리 목록이다 (0건은 성공이다).
        [TestMethod]
        [TestCategory("Db")]
        public void SP_PAT_01_은_RS0_와_RS1_열아홉_컬럼을_계약대로_돌려준다()
        {
            IPatientRepository repository = NewRepository();

            PatientListReadDto read = Run(() => repository.Search(new PatientSearchRequest { ChartNo = ProbeChartNo }));

            Assert.IsNotNull(read.Result, "RS0 을 읽지 못했다 (05 §3.1)");
            Assert.AreEqual((int)DbCode.Ok, read.Result.Code, "결과코드: " + read.Result.Message);

            // 여기까지 왔다는 것은 GetOrdinal 열아홉 번이 전부 이름을 찾았다는 뜻이다.
            Assert.IsNotNull(read.Rows, "RS1 을 읽지 못했다");
            Assert.AreEqual(0, read.Rows.Count, "없는 차트번호인데 행이 나왔다");
        }

        // 대상: 실물 DB — 등록(SP-PAT-03) → 상세 → 같은 주민번호 재등록 → 수정(SP-PAT-04)
        //       → 옛 행버전으로 재수정, 다섯 단계를 한 수검자에 이어 부른다
        // 목적: 쓰기 경로 한 벌이 실제 계약대로 도는지를 잰다. 다섯을 한 시험에 이은 것은 모두
        //       같은 수검자 한 행에 매여 있어서다 — MSTest 는 시험 순서를 보장하지 않으므로
        //       나누면 순서에 기대게 된다. 특히 낙관적 동시성(06 §26)은 앞 단계가 바꾼 행버전이
        //       있어야만 시험할 수 있다.
        // 확인: 등록이 결과코드 0 이고 RS1 1행이 서며, 자동발급 차트번호가 비어 있지 않고,
        //       주민번호에서 유도된 생년월일 19990707 · 성별 F 가 계산열 계약대로 나온다.
        //       같은 주민번호 재등록은 2, 수정은 0 이며 행버전이 바뀌고, 옛 행버전으로 다시
        //       수정하면 601(다른 사용자가 변경)로 막힌다.
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

            // ── [R21] 상세를 목록 SP 로 읽는다 (05 §7.2). `SELECT_수검자상세` 가 사라졌고,
            //    그 RS1 이 목록 RS1 안으로 들어왔다. 차트번호는 `UQ_수검자_CHART_NO` 라 1행이다.
            PatientListReadDto back = Run(() => repository.Search(
                new PatientSearchRequest { ChartNo = row.ChartNo }));
            Assert.AreEqual((int)DbCode.Ok, back.Result.Code, "결과코드: " + back.Result.Message);
            Assert.AreEqual(1, back.Rows.Count, "차트번호 한 건이 아니다");

            PatientDto detail = back.Rows[0];
            Assert.AreEqual(name, detail.Name);
            Assert.AreEqual("메모", detail.Memo);
            Assert.IsTrue(detail.HepatitisBExcluded, "B형간염 제외값이 저장되지 않았다 (03 §6.2a)");
            Assert.AreEqual(8, detail.RowVersion.Length);
            Assert.IsNull(detail.ValidWork, "방금 만든 수검자에게 유효업무가 붙었다 (00 RP-06)");

            // ── 같은 주민번호 + 같은 이름 → 신규 INSERT 없이 기존 1행 (05 §10.1 결과표).
            PatientSaveReadDto again = Run(() => repository.Register(NewRequest(social, name, true)));
            Assert.AreEqual((int)DbCode.ExistingPatient, again.Result.Code, "결과코드: " + again.Result.Message);
            Assert.AreEqual(row.PatientId, again.Rows[0].PatientId);

            // ── 수정 (05 §10.2). RS1 은 세 컬럼이고 행버전이 바뀐다 (05 §16.3).
            PatientSaveRequest update = NewRequest(social, name, false);
            update.PatientId = row.PatientId;
            update.RowVersion = detail.RowVersion;
            update.ChartNo = row.ChartNo;
            update.Memo = "고친 메모";
            PatientSaveReadDto updated = Run(() => repository.Update(update));
            Assert.AreEqual((int)DbCode.Ok, updated.Result.Code, "결과코드: " + updated.Result.Message);
            Assert.AreEqual(1, updated.Rows.Count);
            CollectionAssert.AreNotEqual(detail.RowVersion, updated.Rows[0].RowVersion,
                "실제로 고쳤는데 행버전이 그대로다");

            // ── 옛 행버전으로 다시 고치면 601 이다 (05 §16.3 · 03 §16).
            update.Memo = "또 고친 메모";
            PatientSaveReadDto stale = Run(() => repository.Update(update));
            Assert.AreEqual((int)DbCode.RowChanged, stale.Result.Code, "결과코드: " + stale.Result.Message);
        }

        // 대상: 실물 DB — 이름과 산출 생년월일이 같고 주민번호가 다른 수검자 등록(SP-PAT-03)
        // 목적: 05 §3.5 의 예외다 — 203 은 실패 결과코드인데도 후속 Result Set(후보 목록)을
        //       읽어야 한다. 이 규약이 깨지면 화면이 후보를 못 받아, 동명이인인지 같은 사람인지
        //       조작자가 판단할 근거가 사라진다.
        // 확인: 첫 등록이 0 이고, 유사한 사람으로 등록하면 203 과 함께 후보 RS1 이 1건 이상 온다.
        //       확인값을 실어 다시 부르면 0 으로 통과하고 앞사람과 다른 수검자ID 가 발급된다.
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
            return DbFixture.ConnectionString();
        }
    }
}
