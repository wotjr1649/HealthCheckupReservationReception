// ── 화면을 PNG 로 뜨는 공통 기계 ─────────────────────────────────────────────
//
// `ShellCaptureTests`(fake · 결정적)와 `ScenarioCaptureTests`(실물 DB) 둘이 같은 방식으로
// 뜬다. 뜨는 방법을 두 곳에 두지 않는다 (ROOT AGENTS.md §6) — 무엇을 뜨는가만 다르다.

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthCheckupReservationReception.Tests.Visual
{
    /// <summary>
    /// 캡처의 공통부. 산출 위치는 `winforms/artifacts/logs/` 아래이고 `.gitignore` 대상이다 —
    /// 제출 산출물로 옮기는 것은 `tools/build-scenario-evidence.js` 가 한다.
    /// </summary>
    internal static class clsCapture
    {
        /// <summary>`winforms/artifacts/logs/<paramref name="name"/>`.</summary>
        public static string Save(Bitmap bmp, string name)
        {
            return Save(bmp, null, name);
        }

        /// <summary>
        /// `winforms/artifacts/logs/[subdir/]name`. 시나리오 증빙은 하위 폴더로 나눠 둔다 —
        /// 생성기가 그 폴더만 보면 되고, 화면 목록 캡처와 섞이지 않는다.
        /// </summary>
        public static string Save(Bitmap bmp, string subdir, string name)
        {
            // bin/Debug 에서 winforms/ 까지 올라간다.
            string dir = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "artifacts", "logs"));
            if (!string.IsNullOrEmpty(subdir))
            {
                dir = Path.Combine(dir, subdir);
            }

            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name);
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            return path;
        }

        /// <summary>
        /// WinForms 는 STA 에서만 돈다. MSTest 시험 스레드는 MTA 라 여기서 갈아탄다.
        /// </summary>
        public static void RunSta(Action action)
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

        /// <summary>
        /// Designer 가 만든 컨트롤을 시험에서 집는다 — **조작 경로를 그대로 밟기 위해서다.**
        /// 값을 화면 뒤로 밀어 넣으면 그림은 나오지만 「조회해서 나온 것」이 아니게 된다.
        ///
        /// [!] 이름이 바뀌면 여기서 멈춘다. 그것이 옳다 — 못 찾은 채 조용히 지나가면 조건 없이
        ///     뜬 그림이 증빙으로 나간다.
        /// </summary>
        public static T Control<T>(object owner, string name) where T : class
        {
            FieldInfo field = owner.GetType().GetField(
                name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field,
                owner.GetType().Name + " 에 " + name + " 가 없다 — Designer 필드 이름이 바뀌었다");

            var control = field.GetValue(owner) as T;
            Assert.IsNotNull(control, name + " 이 " + typeof(T).Name + " 가 아니다");
            return control;
        }

        /// <summary>
        /// 떠 놓은 PNG 가 실제 그림인지 본다. 폼이 그려지지 않으면 파일은 생기고 내용만 빈다 —
        /// 그때 조용히 통과하면 증빙이 빈 그림으로 제출된다.
        /// </summary>
        public static void AssertDrawn(string path)
        {
            Assert.IsNotNull(path, "PNG 를 쓰지 못했다");
            var info = new FileInfo(path);
            Assert.IsTrue(info.Exists && info.Length > 10 * 1024,
                "PNG 가 비었거나 너무 작다 — 폼이 그려지지 않았다: "
                + path + " (" + (info.Exists ? info.Length : 0) + " bytes)");
            Console.WriteLine("캡처: " + path);
        }
    }
}
