using System;
using System.Threading;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using HealthCheckupReservationReception.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Views
{
    /// <summary>
    /// **조회조건 드롭다운의 공통 규칙** — WF-PAT-01 · WF-WRK-01 · DLG-PAT-02 가 함께 쓴다.
    ///
    /// 여닫기 자체는 화면 시험이 이미 밟는다 (`조회조건을_끄면_칸이_사라지고_값도_비워진다`).
    /// **여기서는 그 시험이 닿지 못하는 자리만 잰다** — 생년월일 칸의 형식 변환이다.
    ///
    /// [X] **아무도 보지 않던 자리다** (2026-09-15 실측). 생년월일 조건은 2026-09-10 에
    ///     기본 꺼짐이 되었고 (03 §5.3), 화면 시험은 그것이 `null` 인 것만 확인한다 —
    ///     조건을 켜서 날짜를 골랐을 때 `yyyyMMdd` 로 바뀌는 경로는 한 번도 실행되지 않았다.
    /// </summary>
    [TestClass]
    public class clsSearchConditionsTests
    {
        // 대상: clsSearchConditions.BirthdayOf — 생년월일 칸 → 조회조건 값
        // 목적: 05 §7.2 의 `@생년월일` 은 VARCHAR(8) `yyyyMMdd` 다. 화면은 달력으로 고르고
        //       DB 는 문자열을 받으므로 그 사이 변환이 틀리면 조회가 조용히 0건이 된다 —
        //       조작자는 그 사람이 없는 줄로 읽는다. 문화권에 따라 표기가 흔들리지 않도록
        //       InvariantCulture 로 굳힌 것도 이 자리다.
        // 확인: 1999-07-07 을 고르면 "19990707" 이 나온다.
        [TestMethod]
        public void 생년월일은_조회조건이_쓰는_여덟자리로_바뀐다()
        {
            RunSta(delegate
            {
                using (var editor = new DateEdit())
                {
                    editor.EditValue = new DateTime(1999, 7, 7);

                    Assert.AreEqual("19990707", clsSearchConditions.BirthdayOf(editor));
                }
            });
        }

        // 대상: clsSearchConditions.BirthdayOf — 비어 있는 생년월일 칸
        // 목적: 비었으면 미입력이고 조회조건에서 빠져야 한다. 빈 문자열이나 최소날짜를
        //       보내면 DB 는 그것을 조건으로 받아 아무도 안 나오는 조회가 된다.
        // 확인: EditValue 가 비었을 때도, DBNull 일 때도 null 이다.
        [TestMethod]
        public void 생년월일이_비면_조건에서_빠진다()
        {
            RunSta(delegate
            {
                using (var editor = new DateEdit())
                {
                    // [!] 미입력은 **EditValue 가 null 인 상태**다. 갓 만든 DateEdit 은 오늘로
                    //     서 있어 미입력이 아니다 — 화면은 조건을 끌 때 null 로 비운다.
                    editor.EditValue = null;
                    Assert.IsNull(clsSearchConditions.BirthdayOf(editor), "빈 칸이 조건으로 갔다");

                    editor.EditValue = DBNull.Value;
                    Assert.IsNull(clsSearchConditions.BirthdayOf(editor), "DBNull 이 조건으로 갔다");
                }
            });
        }

        // 대상: clsSearchConditions.SetupBirthday — 생년월일 칸의 표기 규칙
        // 목적: 03 §7.2 → §5.3 에서 WF-PAT-01 과 DLG-PAT-02 가 같은 조회계약을 쓰므로 칸의
        //       규칙도 한 벌이어야 한다 — 두 곳에 적어 두면 한쪽만 고쳐진다
        //       (ROOT AGENTS.md §6). DevExpress 는 표시용과 입력용 형식을 따로 받으므로
        //       둘을 함께 세우지 않으면 고른 뒤와 치는 중의 표기가 달라 보인다.
        // 확인: DisplayFormat·EditFormat 둘 다 DateTime + yyyy-MM-dd 이고
        //       마스크를 표시형식으로 쓴다.
        [TestMethod]
        public void 생년월일_칸은_표시와_입력_형식을_함께_세운다()
        {
            RunSta(delegate
            {
                using (var editor = new DateEdit())
                {
                    clsSearchConditions.SetupBirthday(editor);

                    Assert.AreEqual(FormatType.DateTime, editor.Properties.DisplayFormat.FormatType);
                    Assert.AreEqual("yyyy-MM-dd", editor.Properties.DisplayFormat.FormatString);
                    Assert.AreEqual(FormatType.DateTime, editor.Properties.EditFormat.FormatType,
                        "치는 중의 표기가 고른 뒤와 달라 보인다");
                    Assert.AreEqual("yyyy-MM-dd", editor.Properties.EditFormat.FormatString);
                    Assert.IsTrue(editor.Properties.Mask.UseMaskAsDisplayFormat);
                }
            });
        }

        // 대상: clsSearchConditions.IsOn — 칸이 없는 조건의 켜짐 판정
        // 목적: 「예약 없는 수검자만」처럼 SP 로 가지 않고 화면이 거르는 조건은 여닫을 칸이
        //       없어 이 길로만 읽는다. 없는 이름에 true 를 돌려주면 켜지도 않은 조건이
        //       목록을 걸러 조작자가 찾는 사람이 사라진다.
        // 확인: 켠 항목은 true, 끈 항목은 false, 목록에 없는 이름은 false 다.
        [TestMethod]
        public void 칸이_없는_조건은_체크_상태로만_읽고_모르는_이름은_꺼짐이다()
        {
            RunSta(delegate
            {
                using (var list = new CheckedListBoxControl())
                {
                    // 생산코드(clsSearchConditions.Add)와 같은 방식으로 만든다 —
                    // Description 이 곧 조건 이름이고, IsOn 은 그것으로 찾는다.
                    list.Items.Add(new CheckedListBoxItem(new object(), "켠 조건", CheckState.Checked, true));
                    list.Items.Add(new CheckedListBoxItem(new object(), "끈 조건", CheckState.Unchecked, true));
                    var conditions = new clsSearchConditions(list);

                    Assert.IsTrue(conditions.IsOn("켠 조건"));
                    Assert.IsFalse(conditions.IsOn("끈 조건"));
                    Assert.IsFalse(conditions.IsOn("없는 조건"), "모르는 이름을 켜짐으로 읽었다");
                }
            });
        }

        private static void RunSta(Action action)
        {
            Exception failure = null;
            var thread = new Thread(delegate ()
            {
                try { action(); }
                catch (Exception ex) { failure = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null) { throw new AssertFailedException(failure.Message, failure); }
        }
    }
}
