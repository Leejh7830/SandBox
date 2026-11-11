using System;
using System.Drawing;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    public class StockAdvisorOptionDialog : Form
    {
        private readonly PortfolioData _portfolio;

        private CheckBox _chkAutoPriceSync;
        private ComboBox _cboPriceInterval;
        private CheckBox _chkAutoAiDiagnose;
        private ComboBox _cboAiInterval;
        private CheckBox _chkEnablePriceAlert;
        private Button _btnScoreHelp;
        private Button _btnKbSetting;
        private Button _btnGeminiSetting;
        private Button _btnSave;
        private Button _btnCancel;

        public bool OptionsChanged { get; private set; } = false;

        public StockAdvisorOptionDialog(PortfolioData portfolio)
        {
            _portfolio = portfolio ?? new PortfolioData();
            InitializeComponent();
            LoadConfigToUi();
        }

        private void InitializeComponent()
        {
            Text = "주식 리스크 참모 환경 설정 (StockAdvisor Options)";
            ClientSize = new Size(640, 615);
            MinimumSize = new Size(640, 615);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(18, 22, 32);
            ForeColor = Color.FromArgb(230, 235, 245);
            Font = new Font("Malgun Gothic", 9.2f);

            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 16, 20, 16),
                BackColor = Color.FromArgb(18, 22, 32)
            };

            // 1. 상단 타이틀 배너
            var lblTitle = new Label
            {
                Text = "⚙ 주식 리스크 참모 환경 설정",
                Font = new Font("Malgun Gothic", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 216, 255),
                Location = new Point(5, 5),
                Size = new Size(590, 28)
            };

            var lblSubtitle = new Label
            {
                Text = "실시간 시세 및 AI 참모 진단 자동 동기화 주기 설정, 점수 산출 가이드 열람",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Color.FromArgb(150, 165, 185),
                Location = new Point(5, 34),
                Size = new Size(590, 20)
            };

            // 2. 그룹 1: 자동 동기화 주기 설정
            var grpSync = new GroupBox
            {
                Text = "⏱️ 실시간 자동 동기화 & 목표가/손절가 알림 설정",
                Location = new Point(5, 64),
                Size = new Size(590, 290),
                ForeColor = Color.FromArgb(120, 210, 255),
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                Padding = new Padding(14)
            };

            // (1) 실시간 시세 동기화
            _chkAutoPriceSync = new CheckBox
            {
                Text = "실시간 시세 자동 동기화 활성화 (네이버 금융 스크래핑)",
                Location = new Point(16, 28),
                Size = new Size(390, 26),
                Font = new Font("Malgun Gothic", 9.2f),
                ForeColor = Color.FromArgb(235, 240, 250),
                Cursor = Cursors.Hand
            };

            var lblPriceInterval = new Label
            {
                Text = "동기화 주기:",
                Location = new Point(36, 60),
                Size = new Size(80, 24),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 195, 215),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _cboPriceInterval = new ComboBox
            {
                Location = new Point(120, 58),
                Size = new Size(160, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(28, 34, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            _cboPriceInterval.Items.AddRange(new object[] { "3분", "5분 (권장)", "10분", "15분", "30분" });

            // (2) AI 참모 진단 동기화
            _chkAutoAiDiagnose = new CheckBox
            {
                Text = "AI 참모 진단 자동 동기화 활성화 (Gemini AI 정밀 리스크 분석)",
                Location = new Point(16, 96),
                Size = new Size(410, 26),
                Font = new Font("Malgun Gothic", 9.2f),
                ForeColor = Color.FromArgb(235, 240, 250),
                Cursor = Cursors.Hand
            };

            var lblAiInterval = new Label
            {
                Text = "진단 주기:",
                Location = new Point(36, 128),
                Size = new Size(80, 24),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 195, 215),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _cboAiInterval = new ComboBox
            {
                Location = new Point(120, 126),
                Size = new Size(200, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(28, 34, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            _cboAiInterval.Items.AddRange(new object[] { "5분 (권장 - 1회 일괄)", "10분", "15분", "30분", "60분" });

            // (3) 실시간 목표가/손절가/매수가 터치 알림
            _chkEnablePriceAlert = new CheckBox
            {
                Text = "보유 종목 목표가/손절가 및 관심 종목 AI 매수가 도달 시 실시간 알림",
                Location = new Point(16, 158),
                Size = new Size(540, 26),
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 180),
                Cursor = Cursors.Hand
            };

            // (4) 토큰 안심 가이드 박스
            var lblTokenTip = new Label
            {
                Text = "💡 [토큰 안심 가이드]\n" +
                       "• 시세 동기화는 네이버 금융 스크래핑으로 Gemini AI 토큰 소모가 전혀 없습니다.\n" +
                       "• AI 진단은 전체 종목을 1회의 API 호출로 묶어서 분석하므로, 5분 주기로도 장중(6.5시간) 동안\n" +
                       "  총 78회만 호출됩니다. (무료 한도 1,500회의 약 5.2%에 불과하여 한도 부담이 없습니다.)\n" +
                       "• 창이 최소화되거나 다른 탭으로 전환 시 타이머가 자동 중지되어 자원을 낭비하지 않습니다.",
                Location = new Point(16, 194),
                Size = new Size(556, 82),
                BackColor = Color.FromArgb(24, 30, 44),
                ForeColor = Color.FromArgb(160, 205, 240),
                Font = new Font("Segoe UI", 8.2f),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8, 6, 8, 6)
            };

            grpSync.Controls.Add(lblTokenTip);
            grpSync.Controls.Add(_chkEnablePriceAlert);
            grpSync.Controls.Add(_cboAiInterval);
            grpSync.Controls.Add(lblAiInterval);
            grpSync.Controls.Add(_chkAutoAiDiagnose);
            grpSync.Controls.Add(_cboPriceInterval);
            grpSync.Controls.Add(lblPriceInterval);
            grpSync.Controls.Add(_chkAutoPriceSync);

            // 3. 그룹 2: 점수 산출 가이드 및 외부 연동 바로가기
            var grpDocs = new GroupBox
            {
                Text = "📖 점수 산출 기준 및 연동 설정 바로가기",
                Location = new Point(5, 365),
                Size = new Size(590, 140),
                ForeColor = Color.FromArgb(255, 205, 110),
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                Padding = new Padding(14)
            };

            _btnScoreHelp = CreateButton("❓ 4대 진단 점수 및 등급 산출 기준 열기", Color.FromArgb(20, 45, 65), Color.FromArgb(0, 216, 255), Color.FromArgb(220, 245, 255));
            _btnScoreHelp.Location = new Point(16, 30);
            _btnScoreHelp.Size = new Size(310, 36);
            _btnScoreHelp.Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold);
            _btnScoreHelp.Click += (s, e) =>
            {
                using (var dlg = new StockScoreHelpDialog())
                {
                    dlg.ShowDialog(this);
                }
            };

            var lblHelpDesc = new Label
            {
                Text = "(차트·뉴스·손익비·종합점수 공식 및 타이밍 안전벨트 가이드)",
                Location = new Point(334, 38),
                Size = new Size(245, 22),
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = Color.FromArgb(160, 175, 195)
            };

            _btnKbSetting = CreateButton("KB증권 API 설정", Color.FromArgb(38, 30, 20), Color.FromArgb(255, 180, 50), Color.FromArgb(255, 235, 190));
            _btnKbSetting.Location = new Point(16, 80);
            _btnKbSetting.Size = new Size(180, 34);
            _btnKbSetting.Font = new Font("Malgun Gothic", 8.8f);
            _btnKbSetting.Click += (s, e) =>
            {
                using (var dlg = new KbSecSettingDialog())
                {
                    dlg.ShowDialog(this);
                }
            };

            _btnGeminiSetting = CreateButton("🔑 Gemini API Key 관리", Color.FromArgb(24, 36, 52), Color.FromArgb(100, 190, 255), Color.FromArgb(215, 235, 255));
            _btnGeminiSetting.Location = new Point(206, 80);
            _btnGeminiSetting.Size = new Size(180, 34);
            _btnGeminiSetting.Font = new Font("Malgun Gothic", 8.8f);
            _btnGeminiSetting.Click += (s, e) =>
            {
                using (var form = new GateHelper.MysteryTime.MysteryApiKeyForm(GeminiRiskAdvisorService.GetApiKey()))
                {
                    form.ShowDialog(this);
                }
            };

            grpDocs.Controls.Add(_btnGeminiSetting);
            grpDocs.Controls.Add(_btnKbSetting);
            grpDocs.Controls.Add(lblHelpDesc);
            grpDocs.Controls.Add(_btnScoreHelp);

            // 4. 하단 버튼 바
            var pnlBottom = new Panel
            {
                Location = new Point(5, 520),
                Size = new Size(590, 45)
            };

            _btnSave = CreateButton("설정 저장 (OK)", Color.FromArgb(16, 56, 40), Color.FromArgb(0, 230, 118), Color.FromArgb(220, 255, 235));
            _btnSave.Location = new Point(360, 4);
            _btnSave.Size = new Size(125, 36);
            _btnSave.Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold);
            _btnSave.Click += BtnSave_Click;

            _btnCancel = CreateButton("닫기", Color.FromArgb(35, 42, 54), Color.FromArgb(70, 85, 110), Color.FromArgb(200, 215, 230));
            _btnCancel.Location = new Point(495, 4);
            _btnCancel.Size = new Size(90, 36);
            _btnCancel.Font = new Font("Malgun Gothic", 9.2f);
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlBottom.Controls.Add(_btnCancel);
            pnlBottom.Controls.Add(_btnSave);

            // 메인 패널에 배치
            pnlMain.Controls.Add(pnlBottom);
            pnlMain.Controls.Add(grpDocs);
            pnlMain.Controls.Add(grpSync);
            pnlMain.Controls.Add(lblSubtitle);
            pnlMain.Controls.Add(lblTitle);

            Controls.Add(pnlMain);
            AcceptButton = _btnSave;
            CancelButton = _btnCancel;
        }

        private void LoadConfigToUi()
        {
            _chkAutoPriceSync.Checked = _portfolio.AutoPriceSyncEnabled;
            SelectComboInterval(_cboPriceInterval, _portfolio.PriceSyncIntervalMinutes, defaultIndex: 1); // 기본 5분

            _chkAutoAiDiagnose.Checked = _portfolio.AutoAiDiagnosisEnabled;
            SelectComboInterval(_cboAiInterval, _portfolio.AiDiagnosisIntervalMinutes, defaultIndex: 0); // 기본 5분

            _chkEnablePriceAlert.Checked = _portfolio.EnablePriceTouchAlert;
        }

        private void SelectComboInterval(ComboBox cbo, int minutes, int defaultIndex)
        {
            for (int i = 0; i < cbo.Items.Count; i++)
            {
                string text = cbo.Items[i].ToString();
                if (text.StartsWith($"{minutes}분"))
                {
                    cbo.SelectedIndex = i;
                    return;
                }
            }
            cbo.SelectedIndex = (defaultIndex >= 0 && defaultIndex < cbo.Items.Count) ? defaultIndex : 0;
        }

        private int ParseComboInterval(ComboBox cbo, int defaultMinutes)
        {
            if (cbo.SelectedItem == null) return defaultMinutes;
            string text = cbo.SelectedItem.ToString();
            string digits = string.Empty;
            foreach (char c in text)
            {
                if (char.IsDigit(c)) digits += c;
                else if (digits.Length > 0) break;
            }
            return int.TryParse(digits, out int val) && val > 0 ? val : defaultMinutes;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            _portfolio.AutoPriceSyncEnabled = _chkAutoPriceSync.Checked;
            _portfolio.PriceSyncIntervalMinutes = ParseComboInterval(_cboPriceInterval, 5);

            _portfolio.AutoAiDiagnosisEnabled = _chkAutoAiDiagnose.Checked;
            _portfolio.AiDiagnosisIntervalMinutes = ParseComboInterval(_cboAiInterval, 5);

            _portfolio.EnablePriceTouchAlert = _chkEnablePriceAlert.Checked;

            OptionsChanged = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private Button CreateButton(string text, Color bg, Color borderColor, Color foreColor)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = bg,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = borderColor;
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }
    }
}
