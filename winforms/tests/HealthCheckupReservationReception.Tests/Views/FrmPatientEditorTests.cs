using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraEditors;
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

        // 03 §6.3 · §6.4 — New 의 기본값은 자동발급이고, Edit 에는 그 선택 자체가 없다.
        [TestMethod]
        public void Edit_는_자동발급_전환이_없고_차트번호를_연다()
        {
            RunSta(() =>
            {
                WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);

                using (var form = new FrmPatientEditor(new FakePatientService(), "접수1번창구", 7))
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
