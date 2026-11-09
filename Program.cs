using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GateHelper
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => { };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { };

            try
            {
                Dashboard.DashboardManager.Initialize();
            }
            catch { }

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
