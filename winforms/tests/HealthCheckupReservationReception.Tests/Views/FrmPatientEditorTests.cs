using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using HealthCheckupReservationReception.Models;
using HealthCheckupReservationReception.Tests.Presenters;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// DLG-PAT-01 화면 자체의 규칙. Presenter 시험이 못 보는 것만 본다 —
    /// **`오류항목` 이 어느 입력칸을 가리키는지**가 그것이다 (05 §16.2).
    ///
    /// [X] 알 수 없는 `오류항목` 은 Blocking 으로 떨어지는데 그것은 `XtraMessageBox` 라
    ///     시험에서 열면 창이 멈춘 채로 남는다. 그 갈래는 여기서 재지 않는다.
    /// </summary>
    [TestClass]
    public class FrmPatientEditorTests
    {
        // 대상: FrmPatientEditor (DLG-PAT-01) — RS0 의 오류항목 을 입력칸에 매는 배선
        // 목적: 05 §16.2 에서 오류항목 은 어느 칸이 틀렸는지를 DB 가 알려 주는 값이다. 그것을
        //       입력칸에 매지 않으면 조작자는 메시지만 보고 무엇을 고쳐야 하는지 모른다.
        //       Presenter 시험은 메시지 문자열까지만 보므로 이 배선은 화면에서만 드러난다.
        // 확인: 오류항목이 이름이면 txtName 에, 주민번호면 txtSocialNumber 에, 차트번호면
        //       txtChartNo 에 해당 문구가 붙는다. 오류가 풀리면 그 칸의 문구가 빈 문자열로 지워진다.
        [TestMethod]
        public void 오류항목이_가리키는_입력칸에_Inline_오류가_붙는다()
        {
            RunSta(() =>
            {
                WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);

                using (var form = new FrmPatientEditor(new FakePatientService(), "접수1번창구", null))
                {
                    IPatientEditorView view = form;

                    view.ShowFieldError(PatientErrorField.Name, "이름을 입력하십시오.");
                    Assert.AreEqual("이름을 입력하십시오.", ErrorOf(form, "txtName"));

                    view.ClearFieldErrors();
                    Assert.AreEqual(string.Empty, ErrorOf(form, "txtName"));

                    view.ShowFieldError(PatientErrorField.SocialNumber, "주민등록번호를 입력하십시오.");
                    Assert.AreEqual("주민등록번호를 입력하십시오.", ErrorOf(form, "txtSocialNumber"));

                    view.ClearFieldErrors();
                    view.ShowFieldError(PatientErrorField.ChartNo, "차트번호를 입력하십시오.");
                    Assert.AreEqual("차트번호를 입력하십시오.", ErrorOf(form, "txtChartNo"));
                }
            });
        }

        // 대상: FrmPatientEditor (DLG-PAT-01) — New 와 Edit 두 모드의 차트번호 입력 상태
        // 목적: 03 §6.3·§6.4 가 두 모드를 다르게 정했다. New 의 기본값은 자동발급이고 Edit 에는
        //       그 선택 자체가 없다 — 이미 발급된 차트번호를 자동발급으로 다시 만들 수는 없기
        //       때문이다. 모드를 섞으면 수정 창에서 차트번호가 새로 발급되거나 잠겨 못 고친다.
        // 확인: New 로 열면 자동발급이 켜져 있고, Edit 로 열면 자동발급이 꺼져 있으며
        //       txtChartNo 가 ReadOnly 가 아니다 (고칠 수 있다).
        [TestMethod]
        public void Edit_는_자동발급_전환이_없고_차트번호를_연다()
        {
            RunSta(() =>
            {
                WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);

                using (var form = new FrmPatientEditor(new FakePatientService(), "접수1번창구", new PatientDto { PatientId = 7, ChartNo = "2026-000007" }))
                {
                    IPatientEditorView view = form;
                    Assert.IsTrue(view.AutoChartNo, "New 의 기본값은 자동발급이다 (03 §6.3)");

                    view.ShowEditMode();

                    Assert.IsFalse(view.AutoChartNo, "Edit 인데 자동발급이 켜져 있다 (03 §6.4)");
                    Assert.IsFalse(Editor(form, "txtChartNo").Properties.ReadOnly,
                        "Edit 는 기존 차트번호를 고칠 수 있다");
                }
            });
        }

        private static string ErrorOf(Form form, string name)
        {
            return Editor(form, name).ErrorText;
        }

        private static BaseEdit Editor(Form form, string name)
        {
            Control[] found = form.Controls.Find(name, true);
            Assert.AreEqual(1, found.Length, "Control 을 찾지 못했다: " + name);
            return (BaseEdit)found[0];
        }

        private static void RunSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                throw new AssertFailedException(failure.Message, failure);
            }
        }
    }
}
