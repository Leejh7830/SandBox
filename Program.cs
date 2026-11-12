using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GateHelper
{
    internal static class Program
    {
        private const string SingleInstanceMutexName = "GateHelper_SandBox_SingleInstance_Mutex";
        internal const string SingleInstanceWakeEventName = "GateHelper_SandBox_Wake_Event";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool isNewInstance = false;
            using (var mutex = new Mutex(true, SingleInstanceMutexName, out isNewInstance))
            {
                if (!isNewInstance)
                {
                    // 1. 기존 실행 중인 SandBox 인스턴스에 깨우기(Wake) 신호 발송
                    try
                    {
                        using (var wakeEvent = EventWaitHandle.OpenExisting(SingleInstanceWakeEventName))
                        {
                            wakeEvent.Set();
                        }
                    }
                    catch { }

                    // 2. 메시지박스로 중복 실행 안내 및 토큰 낭비 방지 알림
                    MessageBox.Show(
                        "이미 SandBox가 실행 중입니다.\n(작업 표시줄 오른쪽 시스템 트레이 또는 백그라운드 가동 중)\n\n기존에 실행 중인 SandBox 창을 화면 앞으로 불러왔습니다.\n(중복 실행으로 인한 API 토큰 낭비를 방지하기 위해 새 창을 열지 않습니다)",
                        "SandBox 이미 실행 중",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => { };
                AppDomain.CurrentDomain.UnhandledException += (s, e) => { };

                try
                {
                    AiTrend.AiTrendService.PurgeStaleCaches(days: 3);
                }
                catch { }

                try
                {
                    var options = Util_Option.LoadUserOptions();
                    if (options != null && options.AutoPreloadAiTrend && !string.IsNullOrWhiteSpace(options.GeminiApiKey))
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(3000);
                                var svc = new AiTrend.AiTrendService();
                                await svc.PreloadAllCategoriesAsync(
                                    System.Threading.CancellationToken.None,
                                    canProceed: () => !AiTrend.AiTrendControl.IsUiActive
                                );
                            }
                            catch { }
                        });
                    }
                }
                catch { }

                Application.Run(new SandBox());
            }
        }
    }
}

