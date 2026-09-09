using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
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

        // [X] SP-PAT-02 의 RS1 열다섯 컬럼은 아직 실행으로 재지 못했다 — 수검자 한 행이
        //     있어야 하고 그 행을 만드는 것은 SP-PAT-03(DLG-PAT-01)이다. 07 §12 에 적어 둔다.

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
