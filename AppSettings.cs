namespace GateHelper
{
    public class AppSettings
    {
        public bool RemoveDuplicates { get; set; }
        public bool AutoLogin { get; set; }
        public bool AutoScreenUnlock { get; set; }
        public bool TestMode { get; set; }
        public bool ServerClickConnect { get; set; }
        public int PopupGraceMs { get; set; }
        public bool FavOneClickConnect { get; set; }
        public bool UseUDP { get; set; }
        public bool SaveOption { get; set; }

        // 옵션: Start Operation 시 설정 파일의 비밀번호를 자동 암호화할지 여부
        public bool EncryptCredentialsOnStart { get; set; }

        // AI 샌드박스 옵션: Gemini API Key
        public string GeminiApiKey { get; set; }

        // AI 샌드박스 옵션: 프로그램 시작 시 AI 데일리 브리핑 백그라운드 자동 예열 여부
        public bool AutoPreloadAiTrend { get; set; }

        public AppSettings() // 옵션 변수들
        {
            RemoveDuplicates = true;
            AutoLogin = false;
            AutoScreenUnlock = false;
            TestMode = false;
            ServerClickConnect = true;
            PopupGraceMs = 30000; // GraceTime Default Value
            FavOneClickConnect = false;
            UseUDP = false;
            SaveOption = false;
            EncryptCredentialsOnStart = false;
            GeminiApiKey = "";
            AutoPreloadAiTrend = false;
        }
    }
}
