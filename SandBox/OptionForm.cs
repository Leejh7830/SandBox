using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GateHelper.MysteryTime;

namespace GateHelper
{
    public partial class OptionForm : Form
    {
        private AppSettings _settings;

        // Gemini AI 요소
        private Label _lblApiStatus;
        private Button _btnRegisterApi;
        private CheckBox _chkAutoPreloadAi;

        // 메타 데이터 및 버튼
        private Label _lblMetaFolder;
        private Button _btnOpenMetaFolder;
        private Button _btnClose;

        public OptionForm()
        {
            InitializeComponent();

            this.Text = "SandBox 통합 환경 설정 (Options)";
            this.ClientSize = new Size(550, 390);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(24, 28, 36);
            this.ForeColor = Color.White;

            LoadSettings();
            BuildUi();
            UpdateApiStatusLabel();
        }

        private void LoadSettings()
        {
            try
            {
                _settings = Util_Option.LoadUserOptions() ?? new AppSettings();
            }
            catch
            {
                _settings = new AppSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                if (_settings != null)
                {
                    // 1. 일반 & AI 설정
                    _settings.AutoPreloadAiTrend = _chkAutoPreloadAi.Checked;

                    _settings.SaveOption = true;
                    Util_Option.SaveUserOptions(_settings);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "OptionForm.SaveSettings");
            }
        }

        private void BuildUi()
        {
            this.SuspendLayout();

            var pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 14),
                BackColor = Color.FromArgb(24, 28, 36)
            };

            // 1. Gemini AI 연동 그룹박스
            var grpAi = new GroupBox
            {
                Text = "◆ Google Gemini AI 공용 연동 설정",
                Dock = DockStyle.Top,
                Height = 168,
                ForeColor = Color.FromArgb(120, 210, 255),
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                Padding = new Padding(12)
            };

            _lblApiStatus = new Label
            {
                Text = "API Key 상태 확인 중...",
                Font = new Font("Malgun Gothic", 8.8f),
                Location = new Point(16, 24),
                Size = new Size(480, 38),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnRegisterApi = new Button
            {
                Text = "★ Gemini API Key 등록 / 변경",
                Location = new Point(16, 68),
                Size = new Size(220, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 149, 193),
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnRegisterApi.FlatAppearance.BorderSize = 0;
            _btnRegisterApi.Click += BtnRegisterApi_Click;

            _chkAutoPreloadAi = new CheckBox
            {
                Text = "프로그램 시작 시 AI 데일리 브리핑 백그라운드 자동 예열",
                Location = new Point(16, 110),
                Size = new Size(480, 22),
                Checked = _settings.AutoPreloadAiTrend,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(220, 225, 235),
                Cursor = Cursors.Hand
            };

            var lblPreloadWarning = new Label
            {
                Text = "※ (주식참모진단 한도에 영향이 갈 수 있습니다.)",
                Location = new Point(34, 134),
                Size = new Size(460, 20),
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = Color.FromArgb(255, 180, 100),
                Cursor = Cursors.Hand
            };
            lblPreloadWarning.Click += (s, e) => _chkAutoPreloadAi.Checked = !_chkAutoPreloadAi.Checked;

            grpAi.Controls.Add(lblPreloadWarning);
            grpAi.Controls.Add(_chkAutoPreloadAi);
            grpAi.Controls.Add(_btnRegisterApi);
            grpAi.Controls.Add(_lblApiStatus);

            // 2. 데이터 폴더 그룹박스
            var grpData = new GroupBox
            {
                Text = "◆ 로컬 메타 데이터 저장소",
                Dock = DockStyle.Top,
                Height = 90,
                ForeColor = Color.FromArgb(160, 200, 240),
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                Padding = new Padding(12),
                Margin = new Padding(0, 10, 0, 0)
            };

            _lblMetaFolder = new Label
            {
                Text = $"저장 경로: {Util.MetaFolder}\r\n(포트폴리오 및 환경설정 파일 관리)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(180, 190, 205),
                Location = new Point(16, 24),
                Size = new Size(340, 48),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnOpenMetaFolder = new Button
            {
                Text = "메타 폴더 열기",
                Location = new Point(365, 30),
                Size = new Size(125, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 60, 85),
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnOpenMetaFolder.FlatAppearance.BorderSize = 0;
            _btnOpenMetaFolder.Click += (s, e) =>
            {
                try { Process.Start("explorer.exe", Util.MetaFolder); } catch { }
            };

            grpData.Controls.Add(_btnOpenMetaFolder);
            grpData.Controls.Add(_lblMetaFolder);

            // 4. 하단 닫기/저장 버튼 바
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                Padding = new Padding(0, 8, 0, 0)
            };

            _btnClose = new Button
            {
                Text = "설정 저장 및 닫기",
                Dock = DockStyle.Right,
                Width = 140,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 120, 95),
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) =>
            {
                SaveSettings();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            pnlBottom.Controls.Add(_btnClose);

            // 컨트롤 배치 (도킹 역순)
            pnlContainer.Controls.Add(pnlBottom);
            pnlContainer.Controls.Add(grpData);
            pnlContainer.Controls.Add(grpAi);

            this.Controls.Add(pnlContainer);
            this.AcceptButton = _btnClose;
            this.ResumeLayout(false);
        }

        private void UpdateApiStatusLabel()
        {
            string key = _settings?.GeminiApiKey?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(key))
            {
                string maskedKey = key.Length > 8
                    ? key.Substring(0, 4) + "...." + key.Substring(key.Length - 4)
                    : "••••••••";
                _lblApiStatus.Text = $"● Google Gemini API Key 연동 완료 ({maskedKey})\r\n   [AI 트렌드], [Mystery Time] 실시간 AI 분석 가동 중";
                _lblApiStatus.ForeColor = Color.FromArgb(70, 220, 140); // 녹색
            }
            else
            {
                _lblApiStatus.Text = "○ API Key 미등록 (오프라인 자체 룰베이스 모드로 동작 중)\r\n   [Gemini API Key 등록]을 클릭하여 키를 입력하시면 실시간 AI 기능이 활성화됩니다.";
                _lblApiStatus.ForeColor = Color.FromArgb(240, 200, 70); // 황색
            }
        }

        private void BtnRegisterApi_Click(object sender, EventArgs e)
        {
            string currentKey = _settings?.GeminiApiKey ?? string.Empty;
            using (var dlg = new MysteryApiKeyForm(currentKey))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadSettings();
                    UpdateApiStatusLabel();
                }
            }
        }
    }
}
