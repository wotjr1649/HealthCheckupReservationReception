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

        // 03 §6.2 — 형식·날짜·파생 실패 시 Birthday/Gender 를 Clear 하고 저장을 비활성화한다.
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

        // 05 §10.1 — 1800년대생(9·0)은 저장하지 않는다.
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

        // [!] 03 §6.2 는 Clear + 저장 차단까지만 요구한다. 그래서 화면이 **왜** 비었는지
        //     말해 주지 않았고 사용자가 "검증을 안 한다" 로 읽었다 (2026-09-10 실사용 지적).
        //     사유를 그 칸에 적는다. 자리가 덜 찬 동안에는 아무 말도 하지 않는다.
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

        [TestMethod]
        public void 앞_여섯자리가_없는_날짜면_그것을_안내한다()
        {
            var view = NewView("991307-2000018");          // 13월
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseInputChanged();

            StringAssert.Contains(view.Hint, "실제 날짜");
        }

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

        // 사용자가 실제로 넣은 값이다 (2026-09-10). 7번째 자리 2 는 1900년대 여자이므로
        // 화면이 1900-10-10 을 낸 것이 맞다 — 2000년대는 3·4 다.
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

        [TestMethod]
        public void 이름과_주민번호가_모두_있으면_저장이_열린다()
        {
            var view = NewView(Female1999);
            var service = new FakePatientService();
            new PatientEditorPresenter(view, service, "접수1번창구", null);

            view.RaiseInputChanged();

            Assert.IsTrue(view.SaveEnabled);
        }

        // 03 §6.3 EP-04 · 킷 §6 — 비어 있음은 Presenter 가 판정하고 SP 를 부르지 않는다.
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

        // 03 §6.3 — 수동입력이면 차트번호가 필수다.
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

        // 05 §10.1 조합 — 자동발급 여부가 그대로 요청에 실린다. 번호는 저장 SP 가 확정한다.
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

        // 03 §6.5 · 07 §3.3 — 203 은 DLG-PAT-03 을 띄우고, `별도 수검자로 계속` 이면 확인값을
        // 실어 SP-PAT-03 을 다시 부른다.
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

        // 03 §6.5 — 확인값은 성명 + 산출 생년월일 + 주민번호 조합 하나에만 유효하다.
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

        // 03 §6.3 · §15.2 — 동일 주민번호 + 이름 불일치(202)는 기존 수검자를 확인한 뒤
        // 신규 등록 없이 기존 PatientId 를 돌려준다.
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

        // 03 §15.1 · 05 §16.2 — 100~104 는 `오류항목` 이 가리키는 입력항목 옆 Inline 이다.
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

        // 07 §6.2 — 처리하지 않은 코드는 공통 경로로 떨어진다. 조용히 성공으로 읽지 않는다.
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

        // 07 §3.3 — Edit 진입값은 SP-PAT-02 가 주고, 저장은 SP-PAT-04 로 간다.
        [TestMethod]
        public void Edit_는_진입값을_읽고_수정_SP_로_간다()
        {
            var view = new FakePatientEditorView();
            var service = new FakePatientService
            {
                DetailResult = OperationResult<PatientDetailDto>.Success(Detail(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })),
                UpdateResult = Read(DbCode.Ok, Saved(7, "2026-000007")),
            };
            new PatientEditorPresenter(view, service, "접수1번창구", 7);

            view.RaiseViewLoaded();
            view.RaiseSaveRequested();

            Assert.IsTrue(view.EditModeShown, "03 §6.4 — Edit 는 자동발급 전환이 없다");
            Assert.AreEqual(1, view.LoadCalls);
            Assert.AreEqual(1, service.SaveRequests.Count);
            Assert.AreEqual(7L, service.SaveRequests[0].PatientId);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, (byte[])service.SaveRequests[0].RowVersion);
            Assert.AreEqual(7L, view.ClosedPatientId);
        }

        // 05 §16.3 — 601 은 최신 상세를 다시 조회하고 사용자 입력을 자동으로 덮어쓰지 않는다.
        [TestMethod]
        public void 행버전_충돌은_최신값을_다시_읽되_입력을_덮어쓰지_않는다()
        {
            var view = new FakePatientEditorView();
            var service = new FakePatientService
            {
                DetailResult = OperationResult<PatientDetailDto>.Success(Detail(new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 })),
                UpdateResult = Read(DbCode.RowChanged),
            };
            new PatientEditorPresenter(view, service, "접수1번창구", 7);

            view.RaiseViewLoaded();

            // 진입 뒤 사용자가 고친 값. 601 이 나도 이것이 남아 있어야 한다.
            view.Name = "고친 이름";
            view.RaiseInputChanged();
            view.RaiseSaveRequested();

            Assert.AreEqual(2, service.DetailCalls, "601 인데 최신 상세를 다시 읽지 않았다");
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

        private static PatientDetailDto Detail(byte[] rowVersion)
        {
            return new PatientDetailDto
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

        public void LoadDetail(PatientDetailDto detail)
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
