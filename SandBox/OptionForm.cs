using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GateHelper.MysteryTime;
using GateHelper.StockAdvisor.Security;

namespace GateHelper
{
    public partial class OptionForm : Form
    {
        private AppSettings _settings;

        // Gemini AI 요소
        private Label _lblApiStatus;
        private Button _btnRegisterApi;
        private CheckBox _chkAutoPreloadAi;

        // KB증권 Open API 요소 (Security: Windows DPAPI)
        private RadioButton _rbKbMock;
        private RadioButton _rbKbReal;
        private TextBox _txtKbAppKey;
        private TextBox _txtKbAppSecret;
        private TextBox _txtKbAccountNo;
        private Label _lblKbStatus;

        // 메타 데이터 및 버튼
        private Label _lblMetaFolder;
        private Button _btnOpenMetaFolder;
        private Button _btnClose;

        public OptionForm()
        {
            InitializeComponent();

            this.Text = "SandBox 통합 환경 설정 (Options)";
            this.ClientSize = new Size(550, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(24, 28, 36);
            this.ForeColor = Color.White;

            LoadSettings();
            BuildUi();
            UpdateApiStatusLabel();
            UpdateKbStatusLabel();
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

                    // 2. KB증권 Open API 설정 (Windows DPAPI 암호화)
                    _settings.KbIsMockTrading = _rbKbMock.Checked;
                    _settings.KbAppKey = _txtKbAppKey.Text.Trim();
                    string plainSecret = _txtKbAppSecret.Text.Trim();
                    _settings.KbAppSecretEncrypted = string.IsNullOrEmpty(plainSecret)
                        ? string.Empty
                        : DpapiHelper.Protect(plainSecret);
                    _settings.KbAccountNumber = _txtKbAccountNo.Text.Trim();

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

            // 2. KB증권 Open API 그룹박스 (Security: Windows DPAPI)
            var grpKb = new GroupBox
            {
                Text = "◆ KB증권 Open API 비상 탈출/주문 연동 설정 (Windows DPAPI 암호화)",
                Dock = DockStyle.Top,
                Height = 250,
                ForeColor = Color.FromArgb(255, 205, 110),
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                Padding = new Padding(12),
                Margin = new Padding(0, 10, 0, 0)
            };

            _lblKbStatus = new Label
            {
                Text = "KB증권 연동 상태 확인 중...",
                Font = new Font("Malgun Gothic", 8.8f),
                Location = new Point(16, 22),
                Size = new Size(480, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 환경 선택 (모의투자 / 실거래)
            var lblEnv = new Label
            {
                Text = "거래 환경:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 210, 225),
                Location = new Point(16, 48),
                Size = new Size(100, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _rbKbMock = new RadioButton
            {
                Text = "모의투자 (openapivts - 권장 안전 모드)",
                Location = new Point(125, 48),
                Size = new Size(250, 24),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(140, 220, 160),
                Checked = _settings.KbIsMockTrading,
                Cursor = Cursors.Hand
            };

            _rbKbReal = new RadioButton
            {
                Text = "실거래 (openapi)",
                Location = new Point(380, 48),
                Size = new Size(110, 24),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(255, 140, 140),
                Checked = !_settings.KbIsMockTrading,
                Cursor = Cursors.Hand
            };

            // App Key
            var lblKey = new Label
            {
                Text = "AppKey:",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(180, 195, 215),
                Location = new Point(16, 80),
                Size = new Size(105, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _txtKbAppKey = new TextBox
            {
                Location = new Point(125, 80),
                Size = new Size(365, 23),
                BackColor = Color.FromArgb(32, 36, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = _settings.KbAppKey ?? string.Empty
            };

            // App Secret
            var lblSecret = new Label
            {
                Text = "AppSecret (보안):",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(180, 195, 215),
                Location = new Point(16, 114),
                Size = new Size(105, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _txtKbAppSecret = new TextBox
            {
                Location = new Point(125, 114),
                Size = new Size(365, 23),
                BackColor = Color.FromArgb(32, 36, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };
            if (!string.IsNullOrEmpty(_settings.KbAppSecretEncrypted))
            {
                _txtKbAppSecret.Text = DpapiHelper.Unprotect(_settings.KbAppSecretEncrypted);
            }

            // 계좌번호
            var lblAccount = new Label
            {
                Text = "종합계좌번호:",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(180, 195, 215),
                Location = new Point(16, 148),
                Size = new Size(105, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _txtKbAccountNo = new TextBox
            {
                Location = new Point(125, 148),
                Size = new Size(200, 23),
                BackColor = Color.FromArgb(32, 36, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = _settings.KbAccountNumber ?? string.Empty
            };

            var lblSecurityTip = new Label
            {
                Text = "🔒 [보안 안내] AppSecret은 현재 윈도우 사용자 세션(DPAPI)으로 암호화되어 안전하게 보관됩니다.\r\n   (미설정 시 비상 탈출 실행 시 즉시 가상 시뮬레이션 모드로 안전하게 체결됩니다.)",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(160, 175, 195),
                Location = new Point(16, 185),
                Size = new Size(475, 48),
                TextAlign = ContentAlignment.MiddleLeft
            };

            grpKb.Controls.Add(lblSecurityTip);
            grpKb.Controls.Add(_txtKbAccountNo);
            grpKb.Controls.Add(lblAccount);
            grpKb.Controls.Add(_txtKbAppSecret);
            grpKb.Controls.Add(lblSecret);
            grpKb.Controls.Add(_txtKbAppKey);
            grpKb.Controls.Add(lblKey);
            grpKb.Controls.Add(_rbKbReal);
            grpKb.Controls.Add(_rbKbMock);
            grpKb.Controls.Add(lblEnv);
            grpKb.Controls.Add(_lblKbStatus);

            // 3. 데이터 폴더 그룹박스
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
            pnlContainer.Controls.Add(grpKb);
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
                _lblApiStatus.Text = $"● Google Gemini API Key 연동 완료 ({maskedKey})\r\n   [주식 참모], [AI 트렌드], [Mystery Time] 실시간 AI 분석 가동 중";
                _lblApiStatus.ForeColor = Color.FromArgb(70, 220, 140); // 녹색
            }
            else
            {
                _lblApiStatus.Text = "○ API Key 미등록 (오프라인 자체 룰베이스 모드로 동작 중)\r\n   [Gemini API Key 등록]을 클릭하여 키를 입력하시면 실시간 AI 참모가 활성화됩니다.";
                _lblApiStatus.ForeColor = Color.FromArgb(240, 200, 70); // 황색
            }
        }

        private void UpdateKbStatusLabel()
        {
            bool hasKey = !string.IsNullOrWhiteSpace(_settings?.KbAppKey);
            bool hasSecret = !string.IsNullOrWhiteSpace(_settings?.KbAppSecretEncrypted);

            if (hasKey && hasSecret)
            {
                string envStr = _settings.KbIsMockTrading ? "모의투자" : "실거래";
                _lblKbStatus.Text = $"● KB증권 Open API 등록 완료 ({envStr} 환경) - 비상 탈출 즉시 서버 접수 준비됨";
                _lblKbStatus.ForeColor = Color.FromArgb(70, 220, 140);
            }
            else
            {
                _lblKbStatus.Text = "○ API 미등록 (비상 탈출 시 안전 가상 시뮬레이션 모드로 체결됩니다)";
                _lblKbStatus.ForeColor = Color.FromArgb(240, 200, 70);
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
