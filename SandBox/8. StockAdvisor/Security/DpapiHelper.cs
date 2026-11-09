using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GateHelper.StockAdvisor.Security
{
    /// <summary>
    /// Windows DPAPI (Data Protection API) 기반 로컬 보안 암호화/복호화 헬퍼
    /// 현재 Windows 로그인 사용자 세션(DataProtectionScope.CurrentUser)에 귀속되어 암호화되므로
    /// 다른 사용자나 외부 PC로 복사되어도 파일 내용이 유출되지 않습니다.
    /// </summary>
    public static class DpapiHelper
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SandBox_StockRisk_DPAPI_2026");

        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] cipherBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(cipherBytes);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "DpapiHelper.Protect");
                return string.Empty;
            }
        }

        public static string Unprotect(string cipherBase64)
        {
            if (string.IsNullOrEmpty(cipherBase64)) return string.Empty;
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherBase64);
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "DpapiHelper.Unprotect");
                return string.Empty;
            }
        }

        public static void SaveEncryptedFile(string filePath, string jsonContent)
        {
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string encrypted = Protect(jsonContent);
                File.WriteAllText(filePath, encrypted, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "DpapiHelper.SaveEncryptedFile");
            }
        }

        public static string LoadEncryptedFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return string.Empty;
                string encrypted = File.ReadAllText(filePath, Encoding.UTF8);
                return Unprotect(encrypted);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "DpapiHelper.LoadEncryptedFile");
                return string.Empty;
            }
        }
    }
}

