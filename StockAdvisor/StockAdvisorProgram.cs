using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace GateHelper.StockAdvisor.Standalone
{
    internal static class StockAdvisorProgram
    {
        internal const string SingleInstanceMutexName = "GateHelper_StockAdvisor_SingleInstance_Mutex";
        internal const string SingleInstanceWakeEventName = "GateHelper_StockAdvisor_Wake_Event";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 기존 SandBox의 _meta 데이터(포트폴리오·텔레그램·API 키·매매일지) 자동 공유
            ResolveSharedMetaFolder();

            bool isNewInstance = false;
            using (var mutex = new Mutex(true, SingleInstanceMutexName, out isNewInstance))
            {
                if (!isNewInstance)
                {
                    try
                    {
                        using (var wakeEvent = EventWaitHandle.OpenExisting(SingleInstanceWakeEventName))
                        {
                            wakeEvent.Set();
                        }
                    }
                    catch { }

                    Thread.Sleep(50);

                    MessageBox.Show(
                        "이미 StockAdvisor(독립 주식 리스크 참모)가 실행 중입니다.\n(시스템 트레이 또는 백그라운드 가동 중)\n\n기존에 실행 중인 StockAdvisor 창을 앞으로 불러왔습니다.\n(API 토큰 낭비 및 텔레그램 봇 충돌 방지를 위해 중복 실행하지 않습니다)",
                        "StockAdvisor 이미 실행 중",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => { };
                AppDomain.CurrentDomain.UnhandledException += (s, e) => { };

                Application.Run(new StockAdvisorMainForm());
            }
        }

        private static void ResolveSharedMetaFolder()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string localMeta = Path.Combine(baseDir, "_meta");
                if (Directory.Exists(localMeta) && File.Exists(Path.Combine(localMeta, "stock_portfolio.dat")))
                {
                    return;
                }

                // bin\StockAdvisor\Release\ -> bin\Release\_meta 우선 연결 (기존 데이터 100% 공유)
                string[] candidates = new[]
                {
                    Path.GetFullPath(Path.Combine(baseDir, @"..\..\Release\_meta")),
                    Path.GetFullPath(Path.Combine(baseDir, @"..\..\Debug\_meta"))
                };

                foreach (string candidate in candidates)
                {
                    if (Directory.Exists(candidate))
                    {
                        Util.MetaFolderOverride = candidate;
                        return;
                    }
                }
            }
            catch { }
        }
    }
}
