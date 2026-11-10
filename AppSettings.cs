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

        // AI 샌드박스 옵션: 선호 진단 모델 (gemini-1.5-pro, gemini-1.5-flash, gemini-2.0-flash)
        public string GeminiModel { get; set; }

        // AI 샌드박스 옵션: 프로그램 시작 시 AI 데일리 브리핑 백그라운드 자동 예열 여부
        public bool AutoPreloadAiTrend { get; set; }

        // KB증권 Open API 비상 주문 연동 설정 (Security: Windows DPAPI 암호화)
        public string KbAppKey { get; set; }
        public string KbAppSecretEncrypted { get; set; }
        public string KbAccountNumber { get; set; }
        public bool KbIsMockTrading { get; set; }

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
            GeminiModel = "gemini-1.5-pro";
            AutoPreloadAiTrend = false;
            KbAppKey = "";
            KbAppSecretEncrypted = "";
            KbAccountNumber = "";
            KbIsMockTrading = true;
        }
    }
}
