using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// **누른 버튼이 도는 동안 잠기는가.**
    ///
    /// `[X]` 이 검사도 실제 결함에서 나왔다 (2026-09-14). `clsBusyScope` 는 **잠글 컨트롤을
    ///      받지 않으면 커서만 바꾼다** — 그런데 여섯 경로가 `new clsBusyScope(this)` 만
    ///      들고 있었다. 조회는 UI 스레드에서 동기로 SP 를 부르므로, 그동안 쌓인 클릭은
    ///      핸들러가 끝난 뒤 그대로 발화한다. 두 번 누르면 저장이 두 번 간다.
    ///
    /// `[X]` **아무 검사도 그것을 보지 않았다.** Presenter 시험은 이벤트를 직접 올리므로
    ///      버튼을 거치지 않고, 화면 시험 열여덟 건은 배치·컬럼만 본다. 그 틈이 여기다.
    ///
    /// `[!]` **Action 버튼을 가진 화면을 만들면 여기에 한 줄을 더한다.** 재는 것은
    ///      「핸들러가 도는 동안 그 버튼이 Enabled=false 인가」 하나뿐이다.
    /// </summary>
    [TestClass]
    public class ActionLockTests
    {
        // 대상: 접수·추가검사·예약·수검자·휴무일 다섯 화면의 Action 버튼 (DataRow 5건)
        // 목적: 조회·저장은 UI 스레드에서 동기로 SP 를 부른다. 그동안 쌓인 클릭은 핸들러가 끝난 뒤
        //       그대로 발화하므로, 버튼을 잠그지 않으면 두 번 누른 만큼 저장이 두 번 간다.
        //       Presenter 시험은 이벤트를 직접 올려 버튼을 거치지 않고 화면 시험은 배치만 보므로,
        //       그 틈을 보는 것은 이 시험뿐이다.
        // 확인: 각 화면에서 버튼을 눌러 핸들러가 도는 동안 그 버튼의 Enabled 가 false 이고,
        //       핸들러가 실제로 한 번 발화했다.
        [DataTestMethod]
        [DataRow(typeof(FrmReception), "btnReceive", "ReceiveRequested", "DLG-RCP-01 접수처리")]
        [DataRow(typeof(FrmExtraExam), "btnSave", "SaveRequested", "DLG-RCP-02 추가검사 저장")]
        [DataRow(typeof(FrmReservation), "btnSave", "SaveRequested", "WF-RSV-01 예약 저장")]
        [DataRow(typeof(FrmPatientEditor), "btnSave", "SaveRequested", "DLG-PAT-01 수검자 저장")]
        [DataRow(typeof(FrmHoliday), "btnSearch", "SearchRequested", "DLG-HOL-01 휴무일 조회")]
        public void 도는_동안_그_버튼은_잠긴다(Type screen, string buttonField, string eventName, string what)
        {
            bool enabledDuring = true;
            bool fired = false;

            RunSta(delegate
            {
                using (var form = (Form)Activator.CreateInstance(screen))
                {
                    SimpleButton button = Button(form, buttonField);

                    EventHandler probe = delegate
                    {
                        fired = true;
                        enabledDuring = button.Enabled;
                    };
                    screen.GetEvent(eventName).AddEventHandler(form, probe);

                    // [X] **폼을 띄우지 않으면 PerformClick 이 아무 일도 하지 않는다.**
                    //     ButtonBase.PerformClick 은 CanSelect 일 때만 OnClick 을 올리고,
                    //     안 띄운 폼의 버튼은 CanSelect 가 false 다 (2026-09-14 실측 —
                    //     이 시험의 첫 판은 다섯 건 전부 「이벤트가 안 올라왔다」로 red 였다).
                    //     Close() 는 부르지 않는다 — WF-RSV-01 은 닫을 때 폐기 확인을 묻는다.
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-32000, -32000);
                    form.Show();
                    Application.DoEvents();

                    // Presenter 가 없으니 기본 상태는 꺼져 있을 수 있다. 누를 수 있게 열고 누른다.
                    button.Enabled = true;
                    button.PerformClick();
                }
            });

            Assert.IsTrue(fired, what + " — 버튼을 눌렀는데 이벤트가 올라오지 않았다");
            Assert.IsFalse(enabledDuring,
                what + " — 도는 동안 버튼이 열려 있다. 쌓인 클릭이 끝난 뒤 그대로 발화한다");
        }

        private static SimpleButton Button(Form form, string field)
        {
            FieldInfo info = form.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, "Designer 에 " + field + " 가 없다 — 이름이 바뀌었으면 DataRow 를 고친다");

            var button = info.GetValue(form) as SimpleButton;
            Assert.IsNotNull(button, field + " 가 SimpleButton 이 아니다");
            return button;
        }

        private static void RunSta(Action action)
        {
            WindowsFormsSettings.DefaultFont = new Font("굴림", 9F);

            Exception failure = null;
            var thread = new Thread(delegate()
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                throw new AssertFailedException("화면을 세우다 터졌다: " + failure.Message, failure);
            }
        }
    }
}
