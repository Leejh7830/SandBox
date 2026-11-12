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
                    IntPtr targetHwnd = IntPtr.Zero;
                    try
                    {
                        using (var wakeEvent = EventWaitHandle.OpenExisting(SingleInstanceWakeEventName))
                        {
                            wakeEvent.Set();
                        }
                    }
                    catch { }

                    // 기존 프로세스의 메인 윈도우 핸들 탐색
                    try
                    {
                        var cur = System.Diagnostics.Process.GetCurrentProcess();
                        var procs = System.Diagnostics.Process.GetProcessesByName(cur.ProcessName);
                        foreach (var p in procs)
                        {
                            if (p.Id != cur.Id && p.MainWindowHandle != IntPtr.Zero)
                            {
                                targetHwnd = p.MainWindowHandle;
                                break;
                            }
                        }
                    }
                    catch { }

                    // 기존 폼이 복원되어 올라올 수 있도록 짧은 양보(50ms)
                    Thread.Sleep(50);

                    // 2. 부모 윈도우(또는 데스크톱 모달)를 소유자로 지정하여 메시지박스가 창 뒤로 숨지 않도록 보장
                    string msg = "이미 SandBox가 실행 중입니다.\n(작업 표시줄 오른쪽 시스템 트레이 또는 백그라운드 가동 중)\n\n기존에 실행 중인 SandBox 창을 화면 앞으로 불러왔습니다.\n(중복 실행으로 인한 API 토큰 낭비를 방지하기 위해 새 창을 열지 않습니다)";
                    string caption = "SandBox 이미 실행 중";

                    if (targetHwnd != IntPtr.Zero)
                    {
                        var owner = new NativeWindowOwner(targetHwnd);
                        MessageBox.Show(owner, msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(null, msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    }
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

        private class NativeWindowOwner : IWin32Window
        {
            public IntPtr Handle { get; }
            public NativeWindowOwner(IntPtr handle) { Handle = handle; }
        }
    }
}

