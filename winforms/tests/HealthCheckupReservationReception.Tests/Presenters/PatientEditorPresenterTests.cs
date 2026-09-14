using System;
using System.Collections.Generic;
using HealthCheckupReservationReception.Common;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Presenters;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Presenters
{
    /// <summary>
    /// DLG-PAT-01 (03 §6). 여기서 보는 것은 **화면이 판정하는 둘**과 **결과코드 분기**다 —
    /// 고유성·후보판정은 DB 가 하므로 fake 가 그 답을 대신 준다.
    /// </summary>
    [TestClass]
    public class PatientEditorPresenterTests
    {
        // 03 §6.2 · 05 §10.1 — 7번째 자리 2 는 1900년대 여자다. 부록 F-1 의 예시값 그대로다.
        private const string Female1999 = "990707-2000018";

        // 7번째 자리 3 은 2000년대 남자다.
        private const string Male2007 = "070707-3000015";

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 주민번호에서 생년월일·성별 파생
        // 목적: 03 §6.2 에서 생년월일·성별은 조작자가 따로 적는 값이 아니라 주민번호에서 나온다.
        //       두 번 입력받으면 주민번호와 어긋난 생년월일이 저장될 길이 생기고, 그 어긋남은
        //       대상판정(TGT)과 국가검사 구성(NEX)까지 틀리게 만든다.
        // 확인: 7번째 자리 2 면 1999-07-07 · 여, 3 이면 2007-07-07 · 남이 화면에 선다.
        [TestMethod]
        public void 주민번호_일곱째_자리로_생년월일과_성별을_산출한다()
        {
            var view = NewView(Female1999);
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseInputChanged();

            Assert.AreEqual("1999-07-07", view.Birthday);
            Assert.AreEqual("여", view.Gender);

            view.SocialNumber = Male2007;
            view.RaiseInputChanged();

            Assert.AreEqual("2007-07-07", view.Birthday);
            Assert.AreEqual("남", view.Gender);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 파생 실패 시의 화면 상태
        // 목적: 03 §6.2 는 형식·날짜·파생 실패 시 두 칸을 비우고 저장을 막도록 정했다. 앞 입력의
        //       파생값이 남아 있으면 지금 주민번호와 다른 생년월일이 화면에 선 채로 저장이 열린다.
        // 확인: 생년월일·성별이 빈 문자열이 되고 저장 버튼이 닫힌다.
        [TestMethod]
        public void 산출이_실패하면_생년월일과_성별을_비우고_저장을_막는다()
        {
            var view = NewView("991307-2000018");
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseInputChanged();

            Assert.AreEqual(string.Empty, view.Birthday);
            Assert.AreEqual(string.Empty, view.Gender);
            Assert.IsFalse(view.SaveEnabled, "파생이 실패했는데 저장이 열려 있다");
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 1800년대생(9·0) 입력
        // 목적: 05 §10.1 은 1800년대생을 저장하지 않는다. 파생표에 없는 값을 통과시키면 생년월일
        //       없이 저장이 열린다.
        // 확인: 생년월일이 빈 문자열이고 저장이 닫힌다.
        [TestMethod]
        public void 일곱째_자리가_구나_영이면_파생하지_않는다()
        {
            var view = NewView("990707-9000011");
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseInputChanged();

            Assert.AreEqual(string.Empty, view.Birthday);
            Assert.IsFalse(view.SaveEnabled);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 주민번호를 아직 다 입력하지 않은 동안
        // 목적: 03 §6.2 는 Clear + 저장 차단까지만 요구해, 화면이 왜 비었는지 말해 주지 않았고
        //       사용자가 「검증을 안 한다」로 읽었다 (2026-09-10 실사용 지적). 그래서 사유를 적되,
        //       타이핑 중에 안내가 깜빡이면 그것대로 방해가 되므로 자리가 덜 찬 동안에는 말하지
        //       않는다.
        // 확인: 입력이 덜 찬 동안 안내 문구가 빈 문자열이고 저장은 닫혀 있다.
        [TestMethod]
        public void 자리가_덜_차면_안내하지_않는다()
        {
            var view = NewView("001010-23");
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseInputChanged();

            Assert.AreEqual(string.Empty, view.Hint);
            Assert.IsFalse(view.SaveEnabled);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 7번째 자리가 9·0 인 경우의 안내
        // 목적: 03 §6.2 의 파생표에 없는 값을 조용히 막기만 하면 조작자는 무엇이 틀렸는지 모른다.
        //       허용 범위를 말해 주는 것까지가 이 검사다.
        // 확인: 안내가 붙는 칸이 주민번호이고 문구에 「1~8」이 들어 있다.
        [TestMethod]
        public void 일곱째_자리가_표에_없으면_1_8_범위를_안내한다()
        {
            var view = NewView("990707-9000011");          // 9 = 1800년대. 03 §6.2 표에 없다
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseInputChanged();

            Assert.AreEqual(PatientErrorField.SocialNumber, view.HintField);
            StringAssert.Contains(view.Hint, "1~8");
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 13월·32일처럼 없는 날짜 입력
        // 목적: 자리수만 세는 검사로는 통과하는 값이다. 05 §10.1 이 101 로 막기 전에 화면이 먼저
        //       말해 주면 조작자가 왕복 없이 고친다.
        // 확인: 안내 문구에 「실제 날짜」가 들어 있다.
        [TestMethod]
        public void 앞_여섯자리가_없는_날짜면_그것을_안내한다()
        {
            var view = NewView("991307-2000018");          // 13월
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseInputChanged();

            StringAssert.Contains(view.Hint, "실제 날짜");
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 잘못된 입력을 고쳤을 때의 안내 해제
        // 목적: 안내가 남아 있으면 조작자는 아직 틀린 줄 안다. 고친 것을 화면이 인정하지 않으면
        //       무엇을 더 고쳐야 하는지 알 길이 없다.
        // 확인: 잘못된 값에서는 「1~8」 안내가 뜨고, 올바른 값으로 고치면 안내가 빈 문자열이 되며
        //       저장이 열린다.
        [TestMethod]
        public void 값이_바로잡히면_안내가_사라진다()
        {
            var view = NewView("990707-9000011");
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseInputChanged();
            StringAssert.Contains(view.Hint, "1~8");

            view.SocialNumber = Female1999;
            view.RaiseInputChanged();

            Assert.AreEqual(string.Empty, view.Hint);
            Assert.IsTrue(view.SaveEnabled);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 세기 판정 경계 (7번째 자리 2 와 4)
        // 목적: 사용자가 실제로 넣은 값이다 (2026-09-10). 00 년생은 1900년대인지 2000년대인지가
        //       7번째 자리로만 갈리는데 그것을 뒤집으면 만나이가 100년 어긋나 TGT 판정이 통째로
        //       틀린다. 2 는 1900년대 여자이고 2000년대는 3·4 다.
        // 확인: 7번째 자리 2 면 1900-10-10 · 여이고 안내가 없다. 4 로 바꾸면 2000-10-10 · 남이다.
        [TestMethod]
        public void 사용자_입력값_001010_2304052_는_1900년대_여자다()
        {
            var view = NewView("001010-2304052");
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseInputChanged();

            Assert.AreEqual("1900-10-10", view.Birthday);
            Assert.AreEqual("여", view.Gender);
            Assert.AreEqual(string.Empty, view.Hint);

            view.SocialNumber = "001010-3304052";          // 같은 날짜에 세기 자리만 3
            view.RaiseInputChanged();

            Assert.AreEqual("2000-10-10", view.Birthday);
            Assert.AreEqual("남", view.Gender);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 저장 버튼 활성화 조건
        // 목적: 05 §10.1 에서 이름과 주민번호가 NOT NULL 이다. 비었는지 보는 것은 화면 몫이고
        //       (킷 §6) 그 둘이 차면 더 막을 이유가 없다 — 고유성·후보 판정은 SP 가 한다.
        //       여기서 더 막으면 계약이 허락한 입력을 화면이 좁히는 것이 된다.
        // 확인: 이름과 주민번호가 모두 유효하면 저장 버튼이 열린다.
        [TestMethod]
        public void 이름과_주민번호가_모두_있으면_저장이_열린다()
        {
            var view = NewView(Female1999);
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseInputChanged();

            Assert.IsTrue(view.SaveEnabled);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 이름 미입력 시의 저장 차단
        // 목적: 03 §6.3 EP-04 · 킷 §6 — 비어 있음은 Presenter 가 판정하고 SP 를 부르지 않는다.
        //       보내 두고 100 을 받아 오면 왕복이 헛돌고, 어느 칸이 문제인지도 한 번 더 풀어야 한다.
        // 확인: SP 가 호출되지 않고 오류가 붙는 항목이 「성명」이다.
        [TestMethod]
        public void 이름이_비면_저장하지_않고_해당_항목에_Inline_오류다()
        {
            var view = NewView(Female1999);
            view.Name = "   ";
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseSaveRequested();

            Assert.AreEqual(0, service.SaveRequests.Count, "이름이 비었는데 SP 를 불렀다");
            Assert.AreEqual("성명", view.LastErrorField);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 수동 차트번호 모드의 필수값
        // 목적: 03 §6.3 에서 수동입력이면 차트번호가 필수다. 자동발급을 끈 채 비워 보내면 05
        //       §10.1 조합 계약 위반이 된다.
        // 확인: SP 가 호출되지 않고 오류가 붙는 항목이 「차트번호」다.
        [TestMethod]
        public void 수동입력인데_차트번호가_비면_저장하지_않는다()
        {
            var view = NewView(Female1999);
            view.AutoChartNo = false;
            view.ChartNo = string.Empty;
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseSaveRequested();

            Assert.AreEqual(0, service.SaveRequests.Count);
            Assert.AreEqual("차트번호", view.LastErrorField);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01 New) — 등록 요청 조립과 성공 뒤 반환값
        // 목적: 05 §10.1 조합에서 자동발급 여부가 그대로 요청에 실리고 번호는 저장 SP 가 확정한다.
        //       화면이 번호를 미리 만들면 두 창구가 같은 번호를 만들 수 있다. 성공 뒤에는 부모가
        //       바로 그 수검자를 가리켜야 하므로 수검자ID·차트번호를 돌려준다.
        // 확인: 등록 SP 가 1회 불리고 자동발급=true · 조작자명이 실리며, 창이 닫히면서 수검자ID 11
        //       과 차트번호 2026-000123 을 부모에게 넘긴다.
        [TestMethod]
        public void New_는_등록_SP_로_가고_자동발급_여부를_싣는다()
        {
            var view = NewView(Female1999);
            var service = new FakePatientService();
            service.RegisterResults.Enqueue(Read(DbCode.Ok, Saved(11, "2026-000123")));
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseSaveRequested();

            Assert.AreEqual(1, service.SaveRequests.Count);
            Assert.IsTrue(service.SaveRequests[0].AutoChartNo);
            Assert.AreEqual("접수1번창구", service.SaveRequests[0].OperatorName);
            Assert.AreEqual(11L, view.ClosedPatientId);
            Assert.AreEqual("2026-000123", view.ClosedChartNo);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 203 유사후보 → DLG-PAT-03 → 재호출 경로
        // 목적: 03 §6.5 · 07 §3.3 에서 203 은 후보 창을 띄우고, 「별도 수검자로 계속」을 고르면
        //       확인값을 실어 SP-PAT-03 을 다시 부른다. 확인값을 안 실으면 같은 203 이 무한히
        //       돌아오고, 후보를 다 안 넘기면 조작자가 동명이인 여부를 판단할 수 없다.
        // 확인: 후보 창이 1회 뜨고 후보 2건이 모두 넘어가며 산출 생년월일도 함께 간다.
        //       SP 는 2회 불리는데 첫 호출은 확인값 없이, 두 번째는 확인값을 실어 부르고,
        //       성공하면 새 수검자ID 31 로 닫힌다.
        [TestMethod]
        public void 후보가_나오면_창을_띄우고_별도등록이면_확인값을_실어_다시_부른다()
        {
            var view = NewView(Female1999);
            view.DuplicateAnswer = DuplicateChoice.Continue;
            var service = new FakePatientService();
            service.RegisterResults.Enqueue(Read(DbCode.SimilarPatient, Saved(21, "2026-000098"), Saved(22, "2026-000114")));
            service.RegisterResults.Enqueue(Read(DbCode.Ok, Saved(31, "2026-000200")));
            new PatientEditorPresenter(view, service, "접수1번창구", null);
            view.RaiseInputChanged();

            view.RaiseSaveRequested();

            Assert.AreEqual(1, view.DuplicateCalls);
            Assert.AreEqual(2, view.LastCandidates.Count, "203 의 후보 전건을 넘기지 않았다");
            Assert.AreEqual("1999-07-07", clsPatientText.FormatBirthday(view.LastDuplicateInput.Birthday));
            Assert.AreEqual(2, service.SaveRequests.Count);
            Assert.IsFalse(service.SaveRequests[0].SimilarConfirmed);
            Assert.IsTrue(service.SaveRequests[1].SimilarConfirmed, "확인값을 싣지 않고 다시 불렀다");
            Assert.AreEqual(31L, view.ClosedPatientId);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 후보 창에서 「입력값 수정」을 고른 경우
        // 목적: 05 §10.1 203 에서 조작자가 「입력값 수정」을 골랐는데 그대로 등록을 다시 부르면
        //       고칠 기회 없이 저장된다 — 고르게 해 놓고 답을 무시하는 셈이다.
        // 확인: SP 가 1회만 불리고(재호출 없음) 창이 닫히지 않아 Editor 로 돌아온다.
        [TestMethod]
        public void 후보_창에서_입력값_수정을_고르면_다시_부르지_않는다()
        {
            var view = NewView(Female1999);
            view.DuplicateAnswer = DuplicateChoice.Modify;
            var service = new FakePatientService();
            service.RegisterResults.Enqueue(Read(DbCode.SimilarPatient, Saved(21, "2026-000098")));
            new PatientEditorPresenter(view, service, "접수1번창구", null);
            view.RaiseInputChanged();

            view.RaiseSaveRequested();

            Assert.AreEqual(1, service.SaveRequests.Count);
            Assert.IsFalse(view.Closed, "Editor 로 돌아가야 하는데 창을 닫았다");
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 유사후보 확인값의 유효 범위
        // 목적: 03 §6.5 에서 확인값은 성명 + 산출 생년월일 + 주민번호 조합 하나에만 유효하다.
        //       값이 바뀌었는데 확인값이 남아 있으면, 조작자가 확인한 적 없는 다른 사람을
        //       「별도 등록으로 확인했다」며 저장하게 된다.
        // 확인: 확인 뒤 주민번호를 바꿔 다시 저장하면 세 번째 호출의 확인값이 꺼져 있다.
        [TestMethod]
        public void 확인값은_주민번호가_바뀌면_풀린다()
        {
            var view = NewView(Female1999);
            view.DuplicateAnswer = DuplicateChoice.Continue;
            var service = new FakePatientService();
            service.RegisterResults.Enqueue(Read(DbCode.SimilarPatient, Saved(21, "2026-000098")));
            service.RegisterResults.Enqueue(Read(DbCode.Ok, Saved(31, "2026-000200")));
            service.RegisterResults.Enqueue(Read(DbCode.Ok, Saved(32, "2026-000201")));
            new PatientEditorPresenter(view, service, "접수1번창구", null);
            view.RaiseInputChanged();
            view.RaiseSaveRequested();

            // 조합의 한 값을 바꾼다 — 확인값이 유효한 조합이 아니게 된다.
            view.SocialNumber = Male2007;
            view.RaiseInputChanged();
            view.RaiseSaveRequested();

            Assert.AreEqual(3, service.SaveRequests.Count);
            Assert.IsTrue(service.SaveRequests[1].SimilarConfirmed);
            Assert.IsFalse(service.SaveRequests[2].SimilarConfirmed, "값이 바뀌었는데 확인값이 남아 있다");
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 202 동일번호 이름불일치의 확인 경로
        // 목적: 03 §6.3·§15.2 에서 202 는 기존 수검자를 확인한 뒤 신규 등록 없이 기존 수검자ID 를
        //       돌려준다. 새로 등록해 버리면 같은 사람이 두 행이 되어 검진 주기 계산이 갈린다.
        // 확인: 확인창이 1회 뜨고, 확인하면 기존 수검자ID 41 로 창이 닫힌다 (새 등록 없음).
        [TestMethod]
        public void 동일_주민번호_이름불일치는_확인하면_기존_수검자로_닫는다()
        {
            var view = NewView(Female1999);
            view.ConfirmAnswer = true;
            var service = new FakePatientService();
            service.RegisterResults.Enqueue(Read(DbCode.SameNumberDifferentName, Saved(41, "2026-000041")));
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseSaveRequested();

            Assert.AreEqual(1, view.ConfirmCalls);
            Assert.AreEqual(41L, view.ClosedPatientId);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 202 확인을 거절한 경우
        // 목적: 같은 주민번호에 다른 이름이면 동일인이 아닐 수 있다. 확인하지 않았는데 창이
        //       닫히면 조작자는 무엇으로 저장됐는지 모른 채 다음으로 넘어간다.
        // 확인: 확인을 거절하면 창이 닫히지 않는다.
        [TestMethod]
        public void 동일_주민번호_이름불일치를_확인하지_않으면_닫지_않는다()
        {
            var view = NewView(Female1999);
            view.ConfirmAnswer = false;
            var service = new FakePatientService();
            service.RegisterResults.Enqueue(Read(DbCode.SameNumberDifferentName, Saved(41, "2026-000041")));
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseSaveRequested();

            Assert.IsFalse(view.Closed);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — DB 가 낸 100~104 의 화면 표시 방식
        // 목적: 03 §15.1 · 05 §16.2 에서 100~104 는 오류항목 이 가리키는 입력항목 옆 Inline 이다.
        //       모달로 띄우면 조작자가 창을 닫은 뒤 어느 칸이 문제였는지 다시 찾아야 한다.
        // 확인: 오류가 붙는 항목이 「차트번호」이고 창이 닫히지 않는다.
        [TestMethod]
        public void 필수값_실패는_오류항목이_가리키는_곳에_Inline_이다()
        {
            var view = NewView(Female1999);
            var service = new FakePatientService();
            var read = Read(DbCode.MissingValue);
            read.Value.Result.Field = "차트번호";
            service.RegisterResults.Enqueue(read);
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseSaveRequested();

            Assert.AreEqual("차트번호", view.LastErrorField);
            Assert.IsFalse(view.Closed);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 화면이 처리하지 않는 결과코드
        // 목적: 07 §6.2 — 처리하지 않은 코드는 공통 경로로 떨어진다. 조용히 성공으로 읽으면
        //       저장되지 않은 것이 저장된 것처럼 보이고, 새 결과코드가 계약에 더해질 때마다
        //       같은 결함이 되풀이된다.
        // 확인: 모르는 코드가 오면 창이 닫히지 않고 안내 메시지가 선다.
        [TestMethod]
        public void 계약에_없는_코드를_성공으로_읽지_않는다()
        {
            var view = NewView(Female1999);
            var service = new FakePatientService();
            service.RegisterResults.Enqueue(Read(DbCode.ChartNoUsed));
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseSaveRequested();

            Assert.IsFalse(view.Closed);
            Assert.IsNotNull(view.LastMessage);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01 Edit) — 수정 모드의 진입과 저장 경로
        // 목적: 07 §3.3 에서 Edit 진입값은 상세가 주고 저장은 수정 SP 로 간다. 등록 SP 로 가면
        //       같은 사람이 한 행 더 생기고, 행버전을 안 실으면 남의 변경을 덮어쓴다.
        // 확인: 자동발급 전환이 없는 Edit 모드로 서고 진입 로드가 1회이며, 저장이 수정 SP 로
        //       1회 가면서 수검자ID 7 과 byte[8] 행버전이 그대로 실린다.
        [TestMethod]
        public void Edit_는_진입값을_읽고_수정_SP_로_간다()
        {
            var view = new FakePatientEditorView();
            var service = new FakePatientService
            {
                DetailResult = OperationResult<PatientDto>.Success(Detail(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })),
                UpdateResult = Read(DbCode.Ok, Saved(7, "2026-000007")),
            };
            new PatientEditorPresenter(view, service, "접수1번창구", service.DetailResult.Value);

            view.RaiseViewLoaded();
            view.RaiseSaveRequested();

            Assert.IsTrue(view.EditModeShown, "03 §6.4 — Edit 는 자동발급 전환이 없다");
            Assert.AreEqual(1, view.LoadCalls);
            Assert.AreEqual(1, service.SaveRequests.Count);
            Assert.AreEqual(7L, service.SaveRequests[0].PatientId);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, (byte[])service.SaveRequests[0].RowVersion);
            Assert.AreEqual(7L, view.ClosedPatientId);
        }

        // 대상: PatientEditorPresenter (DLG-PAT-01) — 601 행버전 충돌 처리
        // 목적: 05 §16.3 에서 601 은 최신 상세를 다시 조회한다 — 남이 무엇을 바꿨는지 보아야
        //       조작자가 판단할 수 있다. 그러나 화면을 자동으로 덮어쓰면 조작자가 방금 친 값이
        //       사라지고, 무엇을 잃었는지도 모른다. 읽되 덮지 않는 것이 이 처리의 핵심이다.
        // 확인: 상세 조회가 1회 더 불리지만 화면 로드는 1회 그대로이고, 조작자가 고친 이름이
        //       화면에 남아 있으며, 창은 닫히지 않고 안내가 선다.
        [TestMethod]
        public void 행버전_충돌은_최신값을_다시_읽되_입력을_덮어쓰지_않는다()
        {
            var view = new FakePatientEditorView();
            var service = new FakePatientService
            {
                DetailResult = OperationResult<PatientDto>.Success(Detail(new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 })),
                UpdateResult = Read(DbCode.RowChanged),
            };
            new PatientEditorPresenter(view, service, "접수1번창구", service.DetailResult.Value);

            view.RaiseViewLoaded();

            // 진입 뒤 사용자가 고친 값. 601 이 나도 이것이 남아 있어야 한다.
            view.Name = "고친 이름";
            view.RaiseInputChanged();
            view.RaiseSaveRequested();

            // 2026-09-14 — 진입은 부모가 준 상세로 열므로 조회는 **충돌 복구 한 번**뿐이다.
            // 예전에는 진입에서도 읽어 둘이었다.
            Assert.AreEqual(1, service.DetailCalls, "601 인데 최신 상세를 다시 읽지 않았다");
            Assert.AreEqual(1, view.LoadCalls, "사용자 입력을 자동으로 덮어썼다");
            Assert.AreEqual("고친 이름", view.Name, "601 이 사용자 입력을 되돌렸다");
            Assert.IsFalse(view.Closed);
            Assert.IsNotNull(view.LastMessage);
        }

        private static FakePatientEditorView NewView(string socialNumber)
        {
            return new FakePatientEditorView
            {
                AutoChartNo = true,
                Name = "홍길동",
                SocialNumber = socialNumber,
            };
        }

        private static PatientDto Detail(byte[] rowVersion)
        {
            return new PatientDto
            {
                PatientId = 7,
                ChartNo = "2026-000007",
                Name = "홍길동",
                SocialNumber = "9907072000018",
                Birthday = "19990707",
                Gender = "F",
                RowVersion = rowVersion,
            };
        }

        private static PatientSaveResultDto Saved(long patientId, string chartNo)
        {
            return new PatientSaveResultDto
            {
                PatientId = patientId,
                ChartNo = chartNo,
                Name = "홍길동",
                SocialNumber = "9907072000018",
                Birthday = "19990707",
                Gender = "F",
                RowVersion = new byte[8],
            };
        }

        private static OperationResult<PatientSaveReadDto> Read(DbCode code, params PatientSaveResultDto[] rows)
        {
            bool success = code == DbCode.Ok || code == DbCode.NoChange || code == DbCode.ExistingPatient;
            return OperationResult<PatientSaveReadDto>.Success(new PatientSaveReadDto
            {
                Result = new DbResult
                {
                    Success = success,
                    Code = (int)code,
                    Message = "결과 메시지",
                    ServerTime = new DateTime(2026, 9, 9),
                },
                Rows = rows.Length == 0 ? null : new List<PatientSaveResultDto>(rows),
            });
        }
    }

    internal sealed class FakePatientEditorView : IPatientEditorView
    {
        public event EventHandler ViewLoaded;
        public event EventHandler InputChanged;
        public event EventHandler SaveRequested;

        public bool AutoChartNo { get; set; }
        public string ChartNo { get; set; }
        public string Name { get; set; }
        public string SocialNumber { get; set; }
        public string MobilePhone { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Zipcode { get; set; }
        public string Address { get; set; }
        public string AddressDetail { get; set; }
        public string Memo { get; set; }
        public bool HepatitisBExcluded { get; set; }

        public string Birthday { get; set; }
        public string Gender { get; set; }
        public bool SaveEnabled { get; set; }

        // 입력 중 안내. ShowFieldError 와 달리 Focus 를 옮기지 않는다.
        public string HintField { get; private set; }

        public string Hint { get; private set; }

        public void ShowFieldHint(string parameterName, string message)
        {
            HintField = parameterName;
            Hint = message;
        }

        public bool EditModeShown { get; private set; }

        /// <summary>진입 로드 뒤에 화면을 다시 덮어썼는지 세는 값이다 (05 §16.3).</summary>
        public int LoadCalls { get; private set; }

        public string LastMessage { get; private set; }
        public string LastErrorField { get; private set; }
        public string LastErrorMessage { get; private set; }
        public bool Closed { get; private set; }
        public long? ClosedPatientId { get; private set; }
        public string ClosedChartNo { get; private set; }

        public bool ConfirmAnswer { get; set; }
        public int ConfirmCalls { get; private set; }
        public DuplicateChoice DuplicateAnswer { get; set; }
        public int DuplicateCalls { get; private set; }
        public PatientSaveResultDto LastDuplicateInput { get; private set; }
        public IList<PatientSaveResultDto> LastCandidates { get; private set; }

        public void ShowEditMode()
        {
            EditModeShown = true;
        }

        public void LoadDetail(PatientDto detail)
        {
            LoadCalls++;

            // 실물 화면이 하는 그대로다 — 진입값을 입력칸에 담는다 (03 §6.4).
            ChartNo = detail.ChartNo;
            Name = detail.Name;
            SocialNumber = clsPatientText.FormatSocialNumber(detail.SocialNumber);
            MobilePhone = detail.MobilePhone;
            HepatitisBExcluded = detail.HepatitisBExcluded;
        }

        public void ClearFieldErrors()
        {
            LastErrorField = null;
            LastErrorMessage = null;
        }

        public void ShowFieldError(string parameterName, string message)
        {
            LastErrorField = parameterName;
            LastErrorMessage = message;
        }

        public void ShowMessage(string message)
        {
            LastMessage = message;
        }

        public bool ConfirmExistingPatient(PatientSaveResultDto existing)
        {
            ConfirmCalls++;
            return ConfirmAnswer;
        }

        public DuplicateChoice ShowDuplicateCandidates(
            PatientSaveResultDto input, IList<PatientSaveResultDto> candidates)
        {
            DuplicateCalls++;
            LastDuplicateInput = input;
            LastCandidates = candidates;
            return DuplicateAnswer;
        }

        public void CloseWith(long? patientId, string chartNo)
        {
            Closed = true;
            ClosedPatientId = patientId;
            ClosedChartNo = chartNo;
        }

        public void RaiseViewLoaded()
        {
            EventHandler handler = ViewLoaded;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }

        public void RaiseInputChanged()
        {
            EventHandler handler = InputChanged;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }

        public void RaiseSaveRequested()
        {
            EventHandler handler = SaveRequested;
            if (handler != null) { handler(this, EventArgs.Empty); }
        }
    }
}
