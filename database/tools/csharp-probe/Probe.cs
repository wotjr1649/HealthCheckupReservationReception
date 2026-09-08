// R4 C# 호출 사전 검수 — 한글 SP 이름 · 한글 SqlParameter · 한글 DataReader 컬럼을
// 실제 ADO.NET 으로 호출해 본다. WinForms 에 DB 호출 코드가 0건이라(plans/09 §1.2 실측)
// 지금까지 확인할 대상이 없었다. 이 파일이 그 첫 실행 증거다 — 05 §16.2 가 적은 계약 그대로다.
//
// [!] 이 파일은 UTF-8 with BOM 이어야 한다. BOM 이 없으면 csc 가 소스를 시스템 기본
//     코드페이지로 읽어 한글 문자열 리터럴이 깨진다. 05 §16.2 가 요구하는 것이 이것이다.
//
//   빌드·실행:  ./scripts/verify-csharp-call.sh
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

static class Probe
{
    const string CS = @"Server=.\SQLEXPRESS;Database=HealthCheckupReservationReceptionDb;Integrated Security=SSPI;";
    static int fail = 0;

    static void Pass(string id, string msg) { Console.WriteLine("PASS " + id + " " + msg); }
    static void Fail(string id, string msg) { Console.WriteLine("FAIL " + id + " " + msg); fail++; }

    static void Eq(string id, string what, object got, object want)
    {
        if (Equals(got, want)) Pass(id, what + " = " + got);
        else Fail(id, what + " 관측 [" + got + "] != 기대 [" + want + "]");
    }

    // Result Set 의 컬럼 이름을 순서대로 돌려준다. 이름으로 읽는 계약의 근거가 이것이다.
    static string[] Cols(SqlDataReader r)
    {
        return Enumerable.Range(0, r.FieldCount).Select(i => r.GetName(i)).ToArray();
    }

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        using (var cn = new SqlConnection(CS))
        {
            cn.Open();

            // CS-001  파라미터 없는 SP. 한글 SP 이름을 CommandText 에 그대로 쓴다.
            //         RS0 5컬럼 이름이 계약과 정확히 같은가.
            using (var cmd = new SqlCommand("dbo.USP_HC_공통업무상태_조회", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using (var r = cmd.ExecuteReader())
                {
                    r.Read();
                    Eq("CS-001", "RS0 컬럼", string.Join(",", Cols(r)),
                       "성공여부,결과코드,결과메시지,오류항목,서버시각");
                    // 이름으로 읽는다. 순서에 기대지 않는다 (05 §16.2).
                    bool ok = (bool)r["성공여부"];
                    int code = (int)r["결과코드"];
                    Eq("CS-002", "RS0 성공여부/결과코드", ok + "/" + code, "True/0");

                    r.NextResult(); r.Read();
                    Eq("CS-003", "RS1 컬럼", string.Join(",", Cols(r)),
                       "오늘날짜,요일명,휴무일명,운영시작시각,운영종료시각,업무일여부,운영시간내여부,현재업무가능,차단코드,차단메시지");
                }
            }

            // CS-004  한글 SqlParameter 로 명시 전달. 이름 앞에 @ 를 붙인다.
            //         선택 Parameter 는 누락하지 않고 DBNull 로 넘긴다 (05 §2.1).
            //   특정 Fixture 에 매이지 않는다 — 있는 수검자 하나를 집는다.
            long pid = 0;
            string chart = null;
            using (var q = new SqlCommand("SELECT TOP (1) [차트번호] FROM [dbo].[수검자] ORDER BY [수검자ID]", cn))
                chart = q.ExecuteScalar() as string;
            if (chart == null)
            {
                // 판정하지 못한 검사는 PASS 가 아니다 (database/CLAUDE.md §10).
                Console.WriteLine("NOT RUN CS-005~008 수검자 0행 — rebuild + tests/00_Test_Harness.sql 이 먼저 필요하다");
            }
            else
            using (var cmd = new SqlCommand("dbo.USP_HC_수검자목록_조회", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@차트번호", SqlDbType.NVarChar, 100).Value = chart;
                cmd.Parameters.Add("@성명", SqlDbType.NVarChar, 100).Value = DBNull.Value;
                cmd.Parameters.Add("@주민번호", SqlDbType.VarChar, 13).Value = DBNull.Value;
                cmd.Parameters.Add("@생년월일", SqlDbType.VarChar, 8).Value = DBNull.Value;
                cmd.Parameters.Add("@휴대전화", SqlDbType.VarChar, 13).Value = DBNull.Value;
                using (var r = cmd.ExecuteReader())
                {
                    r.Read();
                    Eq("CS-004", "RS0 결과코드", (int)r["결과코드"], 0);
                    r.NextResult();
                    if (r.Read())
                    {
                        pid = (long)r["수검자ID"];
                        Eq("CS-005", "RS1 차트번호", (string)r["차트번호"], chart);
                        Pass("CS-006", "RS1 성명 = " + (string)r["성명"]);
                    }
                    else Fail("CS-005", "차트번호 " + chart + " 를 SP 가 찾지 못했다");
                }
            }

            // CS-007  동시성값 타입. 05 §16.3 — R7 부터 세 Entity 가 전부 행버전 -> byte[8] 이다.
            //         R7 이전에는 수검자만 최종수정일시(DateTime)였고, 그래서 C# 쪽 동시성 처리가
            //         Entity 마다 두 벌이었다. 여기서 그 통일을 확인한다.
            if (pid > 0)
            using (var cmd = new SqlCommand("dbo.USP_HC_수검자상세_조회", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@수검자ID", SqlDbType.BigInt).Value = pid;
                using (var r = cmd.ExecuteReader())
                {
                    r.Read(); r.NextResult(); r.Read();
                    var edit = (byte[])r["행버전"];
                    Eq("CS-007", "수검자 행버전 길이", edit.Length, 8);
                    Eq("CS-008", "B형간염제외여부 CLR 타입", r["B형간염제외여부"].GetType().Name, "Boolean");
                }
            }

            // CS-009  행버전은 byte[8] 로 오고, 그대로 되돌려 보낼 수 있어야 한다.
            using (var cmd = new SqlCommand(
                "SELECT TOP (1) [업무ID], [행버전] FROM [dbo].[예약접수] ORDER BY [업무ID]", cn))
            using (var r = cmd.ExecuteReader())
            {
                if (r.Read())
                {
                    var rv = (byte[])r["행버전"];
                    Eq("CS-009", "행버전 길이", rv.Length, 8);
                }
                else Pass("CS-009", "예약접수 0행 — 행버전 확인 생략");
            }

            // CS-010  실패 경로. RS0.오류항목 이 **한글 Parameter 이름**을 담는가.
            //         화면이 입력항목을 찾는 키이므로 Parameter 이름과 같아야 한다 (05 §16.2).
            using (var cmd = new SqlCommand("dbo.USP_HC_수검자상세_조회", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@수검자ID", SqlDbType.BigInt).Value = DBNull.Value;
                using (var r = cmd.ExecuteReader())
                {
                    r.Read();
                    Eq("CS-010", "실패 RS0 결과코드", (int)r["결과코드"], 100);
                    Eq("CS-011", "실패 RS0 오류항목", (string)r["오류항목"], "수검자ID");
                }
            }

            // CS-012  명명인수 순서를 바꿔도 이름으로 바인딩되는가.
            //         C# 은 Parameters 추가 순서가 SP 선언 순서와 다를 수 있다.
            using (var cmd = new SqlCommand("dbo.USP_HC_예약접수목록_조회", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@성명", SqlDbType.NVarChar, 100).Value = DBNull.Value;
                cmd.Parameters.Add("@상태코드", SqlDbType.Char, 3).Value = DBNull.Value;
                cmd.Parameters.Add("@종료일", SqlDbType.Date).Value = new DateTime(2026, 12, 31);
                cmd.Parameters.Add("@차트번호", SqlDbType.NVarChar, 100).Value = DBNull.Value;
                cmd.Parameters.Add("@시작일", SqlDbType.Date).Value = new DateTime(2026, 1, 1);
                using (var r = cmd.ExecuteReader())
                {
                    r.Read();
                    Eq("CS-012", "역순 등록 RS0 결과코드", (int)r["결과코드"], 0);
                    r.NextResult();
                    Eq("CS-013", "RS1 컬럼", string.Join(",", Cols(r)),
                       "업무ID,수검자ID,예약일,시간대코드,상태코드,상태명,성명,차트번호,성별,생년월일,휴대전화");
                }
            }

            // CS-014  sys.parameters 가 한글 Parameter 이름을 그대로 돌려주는가.
            //         C# 이 이름으로 바인딩하는 근거이며 tests/01 SCH-019 와 같은 축이다.
            //   [!] '@[가-힣]%' 로 세면 97 이 나온다 — @B형간염제외여부 가 ASCII 'B' 로 시작한다.
            //       "첫 글자가 한글" 이 아니라 "한글을 담는다" 가 계약이다. 전건과 예외 0건을 따로 센다.
            const string SPFILTER = "FROM sys.parameters pa JOIN sys.procedures p ON p.object_id = pa.object_id " +
                                    "WHERE p.name LIKE 'USP[_]HC[_]%'";
            using (var cmd = new SqlCommand("SELECT COUNT(*) " + SPFILTER, cn))
                Eq("CS-014", "계약 SP Parameter 전건", (int)cmd.ExecuteScalar(), 113);
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) " + SPFILTER + " AND pa.name NOT LIKE '%[가-힣]%'", cn))
                Eq("CS-015", "한글을 담지 않는 Parameter", (int)cmd.ExecuteScalar(), 0);
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.procedures WHERE name LIKE 'USP[_]HC[_]%' AND name NOT LIKE '%[가-힣]%'", cn))
                Eq("CS-016", "한글을 담지 않는 계약 SP", (int)cmd.ExecuteScalar(), 0);
        }
        Console.WriteLine("=== csharp-probe: FAIL " + fail + " ===");
        Environment.Exit(fail == 0 ? 0 : 1);
    }
}
