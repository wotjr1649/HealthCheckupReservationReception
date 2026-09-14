using System;
using System.Data.SqlClient;
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

        // 대상: 실물 DB — WorkRepository 가 부르는 예약접수상세 조회(SP-WRK-02), RS0~RS5 여섯
        // 목적: 05 §8.2 의 Result Set 여섯이 실제로 그 순서·그 이름으로 오는지는 붙어 봐야 안다.
        //       특히 RS5 추가검사구성은 R18 재봉인이 더한 자리이고, 그것이 없으면 DLG-RCP-02 가
        //       안 고른 추가검사의 이름과 가용성을 읽을 길이 없어 화면이 통째로 서지 못한다.
        // 확인: RS0 결과코드 0, RS1 업무상세·RS2 국가검사·RS3 추가검사·RS5 추가검사구성이 모두
        //       null 이 아니고, RS4 가능한업무가 정확히 5행이다. 예약접수가 0건인 DB 에서는
        //       판정할 것이 없으므로 Inconclusive 로 남기고 PASS 로 세지 않는다.
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

        // 대상: 실물 DB — 예약접수목록 조회(SP-WRK-01) 의 RS1 컬럼 매핑
        // 목적: 05 §8.1 의 컬럼 이름을 C# 이 그대로 읽는지를 잰다. ReadRows 가 ordinal 을 루프
        //       밖에서 잡으므로 0건이어도 컬럼 이름이 틀리면 그 자리에서 터진다 — 그래서 없는
        //       차트번호로 물어도 검증이 성립한다.
        // 확인: RS0 결과코드 0 이고 RS1 이 null 이 아니며, 없는 차트번호이므로 0건이다.
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

        // 대상: 실물 DB — 존재하지 않는 대상키로 변경이력 조회(SP-LOG-01)
        // 목적: 05 §8.3 이 「대상 행이 없어도 결과코드=0 · RS1 0행」으로 정했다. 200
        //       PatientNotFound 를 쓰지 않기로 한 것은 감사 기록이 대상 행보다 오래 살기
        //       때문이며, 계약의 그 문장이 실물에서도 참인지를 여기서 잰다.
        // 확인: 없는 대상키로 물어도 결과코드가 0 이고 RS1 이 null 이 아닌 0건이다.
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

        // 대상: 실물 DB — 허용값 밖의 대상테이블로 변경이력 조회(SP-LOG-01)
        // 목적: 05 §8.3 의 허용값은 둘뿐이고 그 밖은 101 이다. C# 쪽 DbLogTarget 상수가 실제로
        //       그 둘과 같은 문자열인지는 실행해야 드러난다 — 오타는 컴파일도 되고 Fake 시험도
        //       통과한다.
        // 확인: 허용 밖 문자열로 부르면 결과코드가 101(잘못된 값)이다.
        [TestMethod]
        [TestCategory("Db")]
        public void SP_LOG_01_은_허용밖_대상테이블에_101_이다()
        {
            IChangeLogRepository repository = new ChangeLogRepository(ConnectionString());

            ChangeLogReadDto read = Run(() => repository.Read("완료이력", 1));

            Assert.AreEqual((int)DbCode.BadValue, read.Result.Code, "RS0: " + read.Result.Message);
        }

        // 대상: 실물 DB — 휴무일목록 조회(SP-HOL-01) 의 RS1·RS2
        // 목적: 05 §12.5 에서 RS1 휴무일 목록은 0행일 수 있지만 RS2 공휴일등재현황은 항상 1행이다.
        //       화면은 RS2 의 잔여일수와 경고임계일수를 비교해 경고를 띄우므로, RS2 가 비면
        //       DLG-HOL-01 이 판단 근거를 잃는다.
        // 확인: 결과코드 0 이고 RS1 이 null 이 아니며, RS2 공휴일등재현황이 null 이 아니다.
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

        // 대상: 실물 DB — 허용값 밖의 @휴무동작코드로 자체휴무일 저장(SP-HOL-02)
        // 목적: R22 로 등록·수정이 SP 하나가 되며 @휴무동작코드 가 생겼다 (05 §12.6). C# 이 보내는
        //       문자열이 계약의 허용값과 같은지는 실행해야 드러난다 — 오타는 컴파일도 되고 Fake
        //       시험도 통과한다 (DbWorkAction 이 같은 이유로 게이트를 갖는다). 이 시험은 코드
        //       검증이 잠금·Transaction 앞이라 DB 에 아무것도 쓰지 않는다.
        // 확인: 결과코드가 101 이고 오류항목이 Parameter 이름 「휴무동작코드」이며, 실패이므로
        //       RS1 을 읽지 않았다 (HolidayDate 가 null).
        [TestMethod]
        [TestCategory("Db")]
        public void SP_HOL_02_는_허용밖_휴무동작코드에_101_이다()
        {
            IHolidayRepository repository = new HolidayRepository(ConnectionString());

            HolidaySaveReadDto read = Run(() => repository.Save(new HolidaySaveRequest
            {
                HolidayDate = new DateTime(2027, 5, 5),
                HolidayName = "코드시험",
                IsActive = true,
            }, "UPSERT"));

            Assert.AreEqual((int)DbCode.BadValue, read.Result.Code, "RS0: " + read.Result.Message);
            Assert.AreEqual("휴무동작코드", read.Result.Field, "오류항목이 Parameter 이름이 아니다");
            Assert.IsNull(read.HolidayDate, "실패인데 RS1 을 읽었다");
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
            return DbFixture.ConnectionString();
        }
    }
}
