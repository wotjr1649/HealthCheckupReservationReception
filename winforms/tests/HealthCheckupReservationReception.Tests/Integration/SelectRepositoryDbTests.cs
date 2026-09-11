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
    /// SELECT 계열 Repository 를 **실물 DB 에 붙여** 돌린다. `PatientRepositoryDbTests` 와
    /// 같은 자리이고 같은 이유다 — `verify-rs-columns.sh` 는 `05` 를 보지 DB 를 보지 않는다.
    /// 컬럼 이름 오타는 컴파일도 되고 fake 를 쓰는 단위시험도 통과하며 **실행할 때만** 터진다.
    ///
    /// [I] `ReadRows` 계열이 ordinal 을 루프 **밖에서** 잡으므로 0건이 나와도 컬럼 이름이
    ///     전건 검증된다 — 데이터를 넣지 않고 계약을 잰다.
    ///
    /// [X] **읽기만 한다.** Write SP 는 데이터를 바꾸므로 여기 두지 않는다 (`database/`
    ///     계열의 계약시험이 그쪽을 맡는다). 이 파일이 도는 동안 DB 상태가 바뀌지 않는다.
    ///
    /// [I] DB 가 없으면 `Inconclusive` 다. 판정하지 못한 검사는 PASS 가 아니다
    ///     (`database/AGENTS.md` §10 의 같은 규칙).
    /// </summary>
    [TestClass]
    public class SelectRepositoryDbTests
    {
        private const string ProbeChartNo = "존재하지-않는-차트번호";

        /// <summary>
        /// 05 §8.2 — RS0~RS5 여섯이고 **RS5 는 R18 이 더한 자리**다. 그 일곱 컬럼이 실제로
        /// 그 이름으로 오는지는 여기서만 드러난다.
        /// </summary>
        [TestMethod]
        [TestCategory("Db")]
        public void SP_WRK_02_는_RS0부터_RS5까지_계약대로_돌려준다()
        {
            IWorkRepository repository = new WorkRepository(ConnectionString());

            // 있는 업무 하나를 목록에서 얻는다. 0건이면 잴 것이 없으므로 판정하지 않는다.
            WorkListReadDto list = Run(() => repository.Search(new WorkSearchRequest
            {
                FromDate = new DateTime(2000, 1, 1),
            }));
            Assert.AreEqual((int)DbCode.Ok, list.Result.Code, "RS0: " + list.Result.Message);
            if (list.Rows == null || list.Rows.Count == 0)
            {
                Assert.Inconclusive("예약접수가 0건이라 상세를 잴 수 없다 — 데이터를 넣고 다시 돌려라.");
            }

            WorkDetailReadDto read = Run(() => repository.ReadDetail(list.Rows[0].WorkId));

            Assert.IsNotNull(read.Result, "RS0 을 읽지 못했다 (05 §3.1)");
            Assert.AreEqual((int)DbCode.Ok, read.Result.Code, "RS0: " + read.Result.Message);
            Assert.IsNotNull(read.Detail, "RS1 업무상세가 없다");
            Assert.IsNotNull(read.NexItems, "RS2 를 읽지 못했다");
            Assert.IsNotNull(read.AexItems, "RS3 를 읽지 못했다");

            // 05 §8.2 — RS4 는 정확히 5행이고 업무동작코드가 고정이다.
            Assert.AreEqual(5, read.Actions.Count, "RS4 가 5행이 아니다");

            // [R18] RS5 추가검사구성 — 정확히 7행. 여기까지 왔다는 것은 GetOrdinal 일곱이
            //       전부 이름을 찾았다는 뜻이다.
            Assert.IsNotNull(read.AexOptions, "RS5 를 읽지 못했다 — R18 이 더한 자리다");
            Assert.AreEqual(7, read.AexOptions.Count, "RS5 가 7행이 아니다 (05 §8.2)");
            foreach (ReservationAexItemDto option in read.AexOptions)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(option.AexCode), "추가검사코드가 비었다");
                Assert.IsFalse(string.IsNullOrWhiteSpace(option.ExamItemName), "검사항목명이 비었다");

                // 선택 가능한 항목에 사유코드가 남아 있으면 둘 중 하나가 거짓이다.
                Assert.AreEqual(option.Selectable, option.ReasonCode == 0,
                    option.AexCode + ": 선택가능과 사유코드가 어긋난다");
            }
        }

        // 05 §8.1 — 조건 하나를 실어 성공 경로로 간다. 0건이어도 RS1 컬럼 열한 개가 검증된다.
        [TestMethod]
        [TestCategory("Db")]
        public void SP_WRK_01_은_RS1_열한_컬럼을_계약대로_돌려준다()
        {
            IWorkRepository repository = new WorkRepository(ConnectionString());

            WorkListReadDto read = Run(() => repository.Search(new WorkSearchRequest { ChartNo = ProbeChartNo }));

            Assert.AreEqual((int)DbCode.Ok, read.Result.Code, "RS0: " + read.Result.Message);
            Assert.IsNotNull(read.Rows, "RS1 을 읽지 못했다");
            Assert.AreEqual(0, read.Rows.Count, "없는 차트번호인데 행이 나왔다");
        }

        /// <summary>
        /// 05 §8.3 — 대상 행이 없어도 `결과코드=0` + RS1 0행이다. `200` 을 쓰지 않는다:
        /// 감사 기록은 대상 행보다 오래 살기 때문이다. **계약의 그 문장을 실물로 잰다.**
        /// </summary>
        [TestMethod]
        [TestCategory("Db")]
        public void SP_LOG_01_은_없는_대상에도_0_과_0행을_돌려준다()
        {
            IChangeLogRepository repository = new ChangeLogRepository(ConnectionString());

            ChangeLogReadDto read = Run(() => repository.Read(DbLogTarget.Patient, -1));

            Assert.AreEqual((int)DbCode.Ok, read.Result.Code, "RS0: " + read.Result.Message);
            Assert.IsNotNull(read.Rows, "RS1 을 읽지 못했다");
            Assert.AreEqual(0, read.Rows.Count);
        }

        // 05 §8.3 — 허용값 둘 밖은 101 이다. DbLogTarget 이 그 둘이라는 것을 실물이 확인한다.
        [TestMethod]
        [TestCategory("Db")]
        public void SP_LOG_01_은_허용밖_대상테이블에_101_이다()
        {
            IChangeLogRepository repository = new ChangeLogRepository(ConnectionString());

            ChangeLogReadDto read = Run(() => repository.Read("완료이력", 1));

            Assert.AreEqual((int)DbCode.BadValue, read.Result.Code, "RS0: " + read.Result.Message);
        }

        /// <summary>05 §12.5 — RS1 은 0행일 수 있지만 RS2 공휴일등재현황은 **항상 1행**이다.</summary>
        [TestMethod]
        [TestCategory("Db")]
        public void SP_HOL_01_은_RS1_과_RS2_를_계약대로_돌려준다()
        {
            IHolidayRepository repository = new HolidayRepository(ConnectionString());

            HolidayListReadDto read = Run(() => repository.Search(new HolidaySearchRequest
            {
                FromDate = new DateTime(2026, 1, 1),
                ToDate = new DateTime(2026, 12, 31),
            }));

            Assert.AreEqual((int)DbCode.Ok, read.Result.Code, "RS0: " + read.Result.Message);
            Assert.IsNotNull(read.Rows, "RS1 을 읽지 못했다");
            Assert.IsNotNull(read.Registry, "RS2 공휴일등재현황은 항상 1행이다 (05 §12.5)");
        }

        /// <summary>
        /// 05 §7.4 — 조회범위는 `예약일 >= 오늘날짜` 이고 정상 Cardinality 는 0행 또는 1행이다.
        ///
        /// [X] **없는 수검자는 `200` 이다.** §7.4 본문은 그 말을 하지 않고 §13 의 허용
        ///     결과코드 표(`SELECT_수검자유효업무 | 0, 100, 200, 701`)만 갖고 있다 — 실측으로
        ///     확인했다. 같은 SELECT 계열이라도 `SP-LOG-01` 은 반대다(대상이 없어도 `0`):
        ///     감사 기록은 대상 행보다 오래 살기 때문이고, 이쪽은 그 수검자로 예약을
        ///     이어 갈 수 있는지를 묻는 자리라 존재를 따진다.
        /// </summary>
        [TestMethod]
        [TestCategory("Db")]
        public void SP_PAT_05_는_없는_수검자에_200_이다()
        {
            IPatientRepository repository = new PatientRepository(ConnectionString());

            PatientValidWorkReadDto read = Run(() => repository.ReadValidWork(-1));

            Assert.AreEqual((int)DbCode.PatientNotFound, read.Result.Code, "RS0: " + read.Result.Message);
            Assert.IsNull(read.Work, "실패인데 유효업무가 나왔다");
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
        /// (ROOT `AGENTS.md` §6, `PatientRepositoryDbTests` 와 같은 규칙).
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
