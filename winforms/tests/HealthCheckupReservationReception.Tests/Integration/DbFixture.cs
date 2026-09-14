// ── 실물 DB 시험이 공유하는 것 ───────────────────────────────────────────────
//
// 이 폴더의 시험은 전부 SQLEXPRESS 에 붙는다. 붙는 자리마다 같은 것이 둘 필요했다 —
// `App.config` 을 읽는 길과 **운영기준의 창**이다. 둘 다 여기 한 벌만 둔다
// (ROOT AGENTS.md §6). 설계는 docs/phase5/2026-09-15-Integration-Test-Design.md §2.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Integration
{
    /// <summary>
    /// 실물 DB 시험의 픽스처. 연결문자열 하나와 운영기준의 창을 갖는다.
    ///
    /// [X] **창을 여는 이유.** 쓰기 SP 는 운영시간 밖에서 `309` 로, 당일 마감 뒤에는 `304`
    ///     로 막힌다. 둘 다 옳은 동작이라 실패로 적을 수 없고, 그렇다고 통과로도 적을 수
    ///     없다 — 밤에 돌리면 시험이 통째로 `Inconclusive` 가 된다. 사람이 업무시간에 맞춰
    ///     앉아 있는 것은 방법이 아니다 (2026-09-14 사용자 지시). `06` §33.2a 와
    ///     `scripts/measure-cutoff.sh` 가 쓰는 같은 패턴이고, 다른 것은 SQL 이 아니라
    ///     C# 이 한다는 것뿐이다.
    ///
    /// [I] **어셈블리에 한 번씩이다.** 시험마다 열고 닫으면 여는 쪽이 이미 열린 값을
    ///     「원본」으로 붙들 수 있다. 한 번만 열고 한 번만 되돌리면 그 경로가 없다.
    ///
    /// [!] **되돌림을 선언으로 두지 않는다.** 프로세스가 죽으면 `AssemblyCleanup` 도 안
    ///     돈다. 최후 방어선은 `database/scripts/verify-operating-baseline.sh` 의 `OPR-G4`
    ///     이고, 그것이 실제로 red 를 내는 것은 2026-09-15 에 확인했다. 돌린 뒤 그것을 본다.
    ///
    /// [!] `운영기준` 에는 관리 SP 가 없고 **없는 것이 의도다** (`05` §1.3) — 정책 수치를
    ///     설정으로 바꾸지 않는다. 그래서 창을 옮기는 길은 직접 SQL 뿐이고, 그 문장을
    ///     **시험 픽스처 안에만** 둔다. 생산 코드에는 없다 (`verify-layering.sh` LAY-003).
    /// </summary>
    [TestClass]
    public class DbFixture
    {
        /// <summary>`04` §8.7 운영기준 한 행의 여섯 값. 순서를 `OPR-G4` 의 출력과 맞춘다.</summary>
        private static readonly string[] Columns =
        {
            "운영시작시각", "운영종료시각",
            "일반예약AM마감", "일반예약PM마감", "접수AM마감", "접수PM마감",
        };

        /// <summary>하루를 통째로 연다. `measure-cutoff.sh` 의 「열림」과 같은 값이다.</summary>
        private static readonly string[] OpenWindow =
        {
            "00:00:00", "23:59:59", "23:59:59", "23:59:59", "23:59:59", "23:59:59",
        };

        /// <summary>붙들어 둔 원본. DB 에 못 붙었으면 null 이고, 그러면 되돌릴 것도 없다.</summary>
        private static string[] _saved;

        [AssemblyInitialize]
        public static void 운영기준의_창을_연다(TestContext context)
        {
            try
            {
                _saved = ReadWindow();
                if (_saved != null)
                {
                    WriteWindow(OpenWindow);
                }
            }
            catch (SqlException ex)
            {
                // DB 가 없는 기계에서도 단위시험은 돌아야 한다. 창을 못 열면 Db 시험이
                // 스스로 Inconclusive 가 되므로, 여기서 실행 전체를 세우지 않는다.
                _saved = null;
                Console.WriteLine("운영기준의 창을 열지 못했다 — " + ex.Message);
            }
        }

        [AssemblyCleanup]
        public static void 운영기준의_창을_되돌린다()
        {
            if (_saved == null)
            {
                return;
            }

            try
            {
                WriteWindow(_saved);
            }
            catch (SqlException ex)
            {
                // 되돌림을 여기서 판정하지 않는다 — OPR-G4 가 그 자리다. 삼키는 것은
                // 예외이고, 판정은 게이트가 한다.
                Console.WriteLine("운영기준을 되돌리지 못했다 — OPR-G4 로 확인해라: " + ex.Message);
            }

            _saved = null;
        }

        // 대상: 시험 어셈블리의 [assembly: Parallelize] 선언 유무
        // 목적: 이 폴더의 시험은 운영기준 한 행과 시간대 정원을 공유한다. 병렬로 돌면 한
        //       시험이 연 창을 다른 시험이 되돌리고, 그 순간 남은 시험은 다른 제품을 재게
        //       된다. 오늘 MSTest 는 순차이지만 선언 한 줄이면 바뀌고 그 한 줄은 조용하다 —
        //       시험은 여전히 green 이고 무엇이 어긋났는지 아무도 못 본다.
        // 확인: 어셈블리에 ParallelizeAttribute 가 0건이다.
        [TestMethod]
        public void 시험_어셈블리는_병렬로_돌지_않는다()
        {
            object[] declared = typeof(DbFixture).Assembly
                .GetCustomAttributes(typeof(ParallelizeAttribute), false);

            Assert.AreEqual(0, declared.Length,
                "[assembly: Parallelize] 가 들어왔다 — 픽스처가 연 운영기준의 창을 두 시험이 서로 덮는다.");
        }

        /// <summary>
        /// 연결문자열을 C# 에 적지 않는다 — `App.config` 하나가 단일 출처이고 `CFG-001`~`003`
        /// 이 그것을 `05` §1.1 과 대조한다 (ROOT `AGENTS.md` §6).
        ///
        /// `ConfigurationManager` 를 쓰지 않는 것은 그것이 `machine.config` 의 항목까지 섞어
        /// 주기 때문이다 — 파일 안에 실제로 적힌 것만 본다.
        /// </summary>
        internal static string ConnectionString()
        {
            string value = ConnectionStringOrNull();
            if (value == null)
            {
                Assert.Inconclusive(
                    "App.config 에서 연결문자열 한 줄을 읽지 못했다 (파일이 없거나 항목이 하나가 아니다): "
                    + ConfigPath());
            }

            return value;
        }

        /// <summary>
        /// 실물 DB 가 없으면 `Inconclusive` 다. 판정하지 못한 검사는 PASS 가 아니다
        /// (`database/AGENTS.md` §10 의 같은 규칙).
        /// </summary>
        internal static T Run<T>(Func<T> call)
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

        /// <summary>`Assert` 를 쓰지 않는다 — 픽스처에서 부르면 실행 전체가 선다.</summary>
        private static string ConnectionStringOrNull()
        {
            string path = ConfigPath();
            if (!File.Exists(path))
            {
                return null;
            }

            List<XElement> entries = XDocument.Load(path)
                .Root.Elements("connectionStrings").Elements("add").ToList();

            return entries.Count == 1 ? entries[0].Attribute("connectionString").Value : null;
        }

        private static string ConfigPath()
        {
            return Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "src", "HealthCheckupReservationReception.WinForms", "App.config"));
        }

        /// <summary>행이 없으면 Seed 가 빠진 DB 다 — 열 것도 되돌릴 것도 없다.</summary>
        private static string[] ReadWindow()
        {
            string connectionString = ConnectionStringOrNull();
            if (connectionString == null)
            {
                return null;
            }

            string sql = "SELECT "
                + string.Join(", ", Columns.Select(c => "CONVERT(VARCHAR(8), [" + c + "])"))
                + " FROM [dbo].[운영기준] WHERE [기준ID] = 1;";

            var values = new string[Columns.Length];
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandType = CommandType.Text;
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = reader.GetString(i);
                    }
                }
            }

            return values;
        }

        private static void WriteWindow(string[] values)
        {
            string connectionString = ConnectionStringOrNull();
            if (connectionString == null)
            {
                return;
            }

            string sql = "UPDATE [dbo].[운영기준] SET "
                + string.Join(", ", Columns.Select((c, i) => "[" + c + "] = @v" + i))
                + " WHERE [기준ID] = 1;";

            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandType = CommandType.Text;
                for (int i = 0; i < values.Length; i++)
                {
                    command.Parameters.Add("@v" + i, SqlDbType.VarChar, 8).Value = values[i];
                }

                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}
