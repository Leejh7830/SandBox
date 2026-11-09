using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace GateHelper
{
    public static class Util
    {
        public static bool IsAnyMainOperationBusy() => false;

        public static string MetaFolder
        {
            get
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_meta");
                if (!Directory.Exists(folder))
                {
                    try { Directory.CreateDirectory(folder); } catch { }
                }
                return folder;
            }
        }
    }

    public static class LogManager
    {
        public enum Level
        {
            Info,
            Warning,
            Error,
            Critical,
            Debug
        }

        public static void LogMessage(string message, Level level = Level.Info, [CallerMemberName] string caller = "")
        {
            // Zero-Resource 원칙: 디스크 I/O 없이 메모리 내 무시 또는 디버그 출력만 수행
        }

        public static void LogException(Exception ex, Level level = Level.Error, string context = "", [CallerMemberName] string caller = "")
        {
        }
    }

    public static class Util_Option
    {
        public static void SaveUserOptions(AppSettings settings)
        {
            if (settings == null) return;
            try
            {
                string folder = Util.MetaFolder;
                string file = Path.Combine(folder, "user_options.json");
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(file, json);
            }
            catch { }
        }

        public static AppSettings LoadUserOptions()
        {
            try
            {
                string file = Path.Combine(Util.MetaFolder, "user_options.json");
                if (File.Exists(file))
                {
                    string json = File.ReadAllText(file);
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null) return settings;
                }
            }
            catch { }
            return new AppSettings();
        }
    }
}
