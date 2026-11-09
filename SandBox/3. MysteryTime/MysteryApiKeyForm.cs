using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using GateHelper;

namespace GateHelper.MysteryTime
{
    public partial class MysteryApiKeyForm : Form
    {
        public string CurrentApiKey { get; private set; }

        public MysteryApiKeyForm(string existingKey)
        {
            InitializeComponent();
            CurrentApiKey = existingKey ?? "";
            txtApiKey.Text = CurrentApiKey;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string key = txtApiKey.Text.Trim();
            CurrentApiKey = key;

            try
            {
                var options = Util_Option.LoadUserOptions() ?? new AppSettings();
                options.GeminiApiKey = key;
                options.SaveOption = true; // 옵션 영구 저장 활성화
                Util_Option.SaveUserOptions(options);

                MessageBox.Show(string.IsNullOrEmpty(key) 
                    ? "API Key가 삭제되었습니다.\n(오프라인 기본 모드로 동작합니다.)" 
                    : "Gemini API Key가 안전하게 저장되었습니다!\n이제 AI 실시간 추리가 동작합니다.", 
                    "설정 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtApiKey.Clear();
        }

        private void btnOpenAiStudio_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://aistudio.google.com/app/apikey");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"브라우저를 열 수 없습니다.\nURL: https://aistudio.google.com/app/apikey\n\n{ex.Message}", "안내");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
