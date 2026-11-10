using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Trading;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// KB증권 Open API 실시간 계좌 연동 설정 및 발급 가이드 다이얼로그
    /// - DPAPI 보안 암호화 저장
    /// - 1클릭 실시간 연동 테스트 및 시뮬레이션 지원
    /// - 공식 개발자 포털 원클릭 브라우저 연동
    /// </summary>
    public class KbSecSettingDialog : Form
    {
        private TextBox _txtAppKey;
        private TextBox _txtAppSecret;
        private TextBox _txtAccountNumber;
        private RadioButton _rbReal;
        private RadioButton _rbMock;
        private ComboBox _cboSyncInterval;
        private CheckBox _chkSimulation;
        private Button _btnToggleSecret;
        private Button _btnTestConnection;
        private Label _lblStatus;

        public KbSecApiConfig Config { get; private set; }

        public KbSecSettingDialog()
        {
            InitializeComponent();
            LoadCurrentConfig();
        }

        private void InitializeComponent()
        {
            Text = "KB증권 Open API 실시간 계좌 연동 설정";
            ClientSize = new Size(680, 545);
            MinimumSize = new Size(680, 545);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(18, 22, 32);
            ForeColor = Color.FromArgb(230, 235, 245);
            Font = new Font("Malgun Gothic", 9.2f);

            var pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            // 1. 상단 타이틀 배너
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70 };
            var lblTitle = new Label
            {
                Text = "⚡ KB증권 Open API 실시간 계좌 연동",
                Font = new Font("Malgun Gothic", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 216, 255),
                AutoSize = true,
                Location = new Point(0, 5)
            };
            var btnGuide = new Button
            {
                Text = "★ 발급 가이드",
                Size = new Size(115, 32),
                Location = new Point(515, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 45, 65),
                ForeColor = Color.FromArgb(100, 200, 255),
                Cursor = Cursors.Hand,
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold)
            };
            btnGuide.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 230);
            btnGuide.Click += (s, e) => ShowGuideDialog();

            var lblDesc = new Label
            {
                Text = "h-able과 실시간 동기화하여 내 실제 보유 잔고 및 평단가를 1초 만에 불러옵니다.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(150, 165, 185),
                Location = new Point(2, 38),
                Size = new Size(505, 26)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnGuide);
            pnlHeader.Controls.Add(lblDesc);

            // 2. 입력 폼 영역
            var pnlForm = new Panel { Dock = DockStyle.Fill };
            int curY = 10;

            // 접속 환경 (실전 vs 모의)
            var lblEnv = new Label { Text = "접속 환경:", Location = new Point(5, curY), AutoSize = true, ForeColor = Color.FromArgb(190, 205, 225) };
            _rbReal = new RadioButton { Text = "실전 투자 (공식 계좌)", Location = new Point(130, curY - 2), AutoSize = true, Checked = true, ForeColor = Color.White };
            _rbMock = new RadioButton { Text = "모의 투자 (VTS)", Location = new Point(310, curY - 2), AutoSize = true, ForeColor = Color.FromArgb(180, 190, 205) };
            pnlForm.Controls.Add(lblEnv);
            pnlForm.Controls.Add(_rbReal);
            pnlForm.Controls.Add(_rbMock);
            curY += 38;

            // App Key
            var lblKey = new Label { Text = "App Key:", Location = new Point(5, curY), AutoSize = true, ForeColor = Color.FromArgb(190, 205, 225) };
            _txtAppKey = new TextBox
            {
                Location = new Point(130, curY - 3),
                Size = new Size(495, 26),
                BackColor = Color.FromArgb(28, 34, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlForm.Controls.Add(lblKey);
            pnlForm.Controls.Add(_txtAppKey);
            curY += 36;

            // App Secret
            var lblSecret = new Label { Text = "App Secret:", Location = new Point(5, curY), AutoSize = true, ForeColor = Color.FromArgb(190, 205, 225) };
            _txtAppSecret = new TextBox
            {
                Location = new Point(130, curY - 3),
                Size = new Size(420, 26),
                BackColor = Color.FromArgb(28, 34, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●'
            };
            _btnToggleSecret = new Button
            {
                Text = "보기",
                Location = new Point(560, curY - 4),
                Size = new Size(65, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 42, 58),
                ForeColor = Color.FromArgb(180, 195, 215),
                Cursor = Cursors.Hand
            };
            _btnToggleSecret.FlatAppearance.BorderColor = Color.FromArgb(60, 75, 100);
            _btnToggleSecret.Click += (s, e) =>
            {
                bool isMasked = _txtAppSecret.PasswordChar != '\0';
                _txtAppSecret.PasswordChar = isMasked ? '\0' : '●';
                _btnToggleSecret.Text = isMasked ? "숨기기" : "보기";
            };
            pnlForm.Controls.Add(lblSecret);
            pnlForm.Controls.Add(_txtAppSecret);
            pnlForm.Controls.Add(_btnToggleSecret);
            curY += 36;

            // 계좌번호
            var lblAcc = new Label { Text = "계좌번호:", Location = new Point(5, curY), AutoSize = true, ForeColor = Color.FromArgb(190, 205, 225) };
            _txtAccountNumber = new TextBox
            {
                Location = new Point(130, curY - 3),
                Size = new Size(200, 26),
                BackColor = Color.FromArgb(28, 34, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblAccTip = new Label { Text = "(- 제외 10자리 또는 8자리, 종합위탁 01 계좌)", Location = new Point(338, curY), AutoSize = true, ForeColor = Color.FromArgb(130, 145, 165), Font = new Font("Segoe UI", 8.5f) };
            pnlForm.Controls.Add(lblAcc);
            pnlForm.Controls.Add(_txtAccountNumber);
            pnlForm.Controls.Add(lblAccTip);
            curY += 38;

            // 자동 동기화 주기
            var lblSync = new Label { Text = "동기화 주기:", Location = new Point(5, curY), AutoSize = true, ForeColor = Color.FromArgb(190, 205, 225) };
            _cboSyncInterval = new ComboBox
            {
                Location = new Point(130, curY - 3),
                Size = new Size(200, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(28, 34, 48),
                ForeColor = Color.White
            };
            _cboSyncInterval.Items.Add("수동 ([동기화] 버튼 클릭 시)");
            _cboSyncInterval.Items.Add("1분 주기 자동 동기화");
            _cboSyncInterval.Items.Add("5분 주기 자동 동기화");
            _cboSyncInterval.SelectedIndex = 0;
            pnlForm.Controls.Add(lblSync);
            pnlForm.Controls.Add(_cboSyncInterval);
            curY += 38;

            // 시뮬레이션 모드 옵션
            _chkSimulation = new CheckBox
            {
                Text = "API 키 미등록 시 가상 시뮬레이션 연동 테스트 허용 (체크 시에만 가상 데이터 제공)",
                Location = new Point(5, curY),
                Size = new Size(620, 24),
                Checked = false,
                ForeColor = Color.FromArgb(160, 210, 180)
            };
            pnlForm.Controls.Add(_chkSimulation);
            curY += 35;

            // 상태 및 피드백 레이블
            _lblStatus = new Label
            {
                Text = "• 입력된 모든 인증정보는 Windows DPAPI로 안전하게 암호화 보관됩니다.\n• ISA, 연금저축 계좌는 증권사 정책상 API가 미지원되므로 [클립보드 붙여넣기]를 권장합니다.",
                Location = new Point(5, curY),
                Size = new Size(620, 38),
                ForeColor = Color.FromArgb(130, 150, 175),
                Font = new Font("Segoe UI", 8.5f)
            };
            pnlForm.Controls.Add(_lblStatus);

            // 3. 하단 버튼 영역
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 55, Padding = new Padding(0, 10, 0, 0) };
            _btnTestConnection = new Button
            {
                Text = "⚡ 연동 테스트",
                Size = new Size(130, 36),
                Location = new Point(5, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(20, 48, 60),
                ForeColor = Color.FromArgb(0, 220, 255),
                Cursor = Cursors.Hand,
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold)
            };
            _btnTestConnection.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 220);
            _btnTestConnection.Click += async (s, e) => await TestConnectionAsync();

            var btnSave = new Button
            {
                Text = "저장 및 적용",
                Size = new Size(115, 36),
                Location = new Point(400, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 52, 36),
                ForeColor = Color.FromArgb(0, 240, 120),
                Cursor = Cursors.Hand,
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderColor = Color.FromArgb(0, 200, 100);
            btnSave.Click += (s, e) => SaveAndClose();

            var btnCancel = new Button
            {
                Text = "닫기",
                Size = new Size(95, 36),
                Location = new Point(530, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 40, 52),
                ForeColor = Color.FromArgb(200, 210, 225),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(70, 80, 100);
            btnCancel.Click += (s, e) => Close();

            pnlFooter.Controls.Add(_btnTestConnection);
            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnCancel);

            pnlMain.Controls.Add(pnlForm);
            pnlMain.Controls.Add(pnlHeader);
            pnlMain.Controls.Add(pnlFooter);
            Controls.Add(pnlMain);
        }

        private void LoadCurrentConfig()
        {
            Config = KbTradingClient.LoadConfig();
            _txtAppKey.Text = Config.AppKey;
            _txtAppSecret.Text = Config.AppSecret;
            _txtAccountNumber.Text = Config.AccountNumber;
            _rbReal.Checked = !Config.IsMockTrading;
            _rbMock.Checked = Config.IsMockTrading;
            _chkSimulation.Checked = Config.EnableSimulationWhenNoKey;

            if (Config.AutoSyncIntervalMinutes == 1) _cboSyncInterval.SelectedIndex = 1;
            else if (Config.AutoSyncIntervalMinutes == 5) _cboSyncInterval.SelectedIndex = 2;
            else _cboSyncInterval.SelectedIndex = 0;
        }

        private async Task TestConnectionAsync()
        {
            _btnTestConnection.Enabled = false;
            _lblStatus.Text = "⏳ KB증권 서버 통신 및 잔고 조회 테스트 중...";
            _lblStatus.ForeColor = Color.FromArgb(0, 210, 255);

            try
            {
                string key = _txtAppKey.Text.Trim();
                string secret = _txtAppSecret.Text.Trim();
                string acc = _txtAccountNumber.Text.Trim();
                bool isMock = _rbMock.Checked;
                bool enableSim = _chkSimulation.Checked;

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
                {
                    if (!enableSim)
                    {
                        _lblStatus.Text = "❌ App Key와 App Secret을 먼저 입력해 주세요.";
                        _lblStatus.ForeColor = Color.FromArgb(255, 100, 100);
                        MessageBox.Show(
                            "App Key와 App Secret이 입력되지 않았습니다.\n\n" +
                            "※ 아직 API 키가 없으시다면 상단 [★ 발급 가이드]를 참고하여 키를 발급받으시거나,\n" +
                            "하단의 '가상 시뮬레이션 연동 허용'을 체크하시면 가상 잔고 데이터로 테스트해 보실 수 있습니다.",
                            "인증키 미입력 안내",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }

                var client = new KbTradingClient(key, secret, isMock, enableSim);
                var balance = await client.FetchBalanceAsync(acc);

                if (balance.IsSuccess)
                {
                    _lblStatus.Text = $"✔ 연동 성공: {balance.Holdings.Count}개 보유 종목 조회 완료 (총 평가금: {balance.TotalEvalAmount:N0}원)";
                    _lblStatus.ForeColor = Color.FromArgb(0, 240, 120);
                    MessageBox.Show(
                        $"KB증권 계좌 정상 연결 확인!\n\n" +
                        $"• 보유 종목 수: {balance.Holdings.Count}개\n" +
                        $"• 총 평가금액: {balance.TotalEvalAmount:N0}원\n" +
                        $"• 모드: {(client.IsSimulationMode ? "가상 시뮬레이션 테스트 모드" : "KB증권 실거래 서버")}\n\n" +
                        (client.IsSimulationMode ? "※ 발급 전 가상 계좌 데이터로 UI 및 실시간 동기화가 안전하게 동작합니다." : "실제 h-able 잔고와 100% 동기화 준비 완료!"),
                        "연동 테스트 성공",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    _lblStatus.Text = $"❌ 연동 실패: {balance.ErrorMessage}";
                    _lblStatus.ForeColor = Color.FromArgb(255, 100, 100);
                    MessageBox.Show($"연동 실패 원인:\n\n{balance.ErrorMessage}", "연동 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"❌ 예외 발생: {ex.Message}";
                _lblStatus.ForeColor = Color.FromArgb(255, 100, 100);
            }
            finally
            {
                _btnTestConnection.Enabled = true;
            }
        }

        private void SaveAndClose()
        {
            Config.AppKey = _txtAppKey.Text.Trim();
            Config.AppSecret = _txtAppSecret.Text.Trim();
            Config.AccountNumber = _txtAccountNumber.Text.Trim();
            Config.IsMockTrading = _rbMock.Checked;
            Config.EnableSimulationWhenNoKey = _chkSimulation.Checked;

            if (_cboSyncInterval.SelectedIndex == 1) Config.AutoSyncIntervalMinutes = 1;
            else if (_cboSyncInterval.SelectedIndex == 2) Config.AutoSyncIntervalMinutes = 5;
            else Config.AutoSyncIntervalMinutes = 0;

            KbTradingClient.SaveConfig(Config);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ShowGuideDialog()
        {
            using (var guideForm = new Form())
            {
                guideForm.Text = "📖 KB증권 Open API 키 발급 4단계 가이드";
                guideForm.ClientSize = new Size(620, 480);
                guideForm.MinimumSize = new Size(620, 480);
                guideForm.StartPosition = FormStartPosition.CenterParent;
                guideForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                guideForm.MaximizeBox = false;
                guideForm.MinimizeBox = false;
                guideForm.BackColor = Color.FromArgb(16, 20, 28);
                guideForm.ForeColor = Color.White;
                guideForm.Font = new Font("Malgun Gothic", 9.2f);

                var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
                var lblMsg = new Label
                {
                    Text = "【 KB증권 Open API 발급 절차 】\n\n" +
                           "1단계: KB증권 종합위탁 계좌 준비 (끝자리 01 계좌)\n" +
                           "   • ※ ISA, 연금저축 계좌는 조세특례법 및 증권사 정책상 API 발급이 불가합니다.\n" +
                           "     (ISA 계좌는 SandBox의 [클립보드 붙여넣기]로 0.1초 만에 가져올 수 있습니다.)\n\n" +
                           "2단계: KB증권 Open API 개발자 포털(openapi.kbsec.com) 접속\n" +
                           "   • 우측 상단 [회원가입 / 로그인]\n\n" +
                           "3단계: [앱 관리] 메뉴 → [앱 등록] 클릭\n" +
                           "   • 앱 이름: SandBox (자유 입력)\n" +
                           "   • 권한: 국내주식 시세 및 주문/잔고 선택\n" +
                           "   • 등록 즉시 발급되는 'App Key'와 'App Secret'을 복사\n\n" +
                           "4단계: SandBox 설정창에 붙여넣고 [저장 및 적용] 클릭!\n" +
                           "   • 키 발급 전에도 '가상 시뮬레이션 모드'로 즉시 체험 가능합니다.",
                    Dock = DockStyle.Fill,
                    ForeColor = Color.FromArgb(220, 230, 245),
                    Font = new Font("Segoe UI", 9.5f)
                };

                var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 45 };
                var btnPortal = new Button
                {
                    Text = "🌐 KB증권 Open API 포털 열기 (브라우저)",
                    Size = new Size(310, 36),
                    Location = new Point(0, 5),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(20, 45, 65),
                    ForeColor = Color.FromArgb(0, 216, 255),
                    Cursor = Cursors.Hand
                };
                btnPortal.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 230);
                btnPortal.Click += (s, e) =>
                {
                    try { Process.Start(new ProcessStartInfo("https://openapi.kbsec.com") { UseShellExecute = true }); }
                    catch { }
                };

                var btnClose = new Button
                {
                    Text = "확인",
                    Size = new Size(95, 36),
                    Location = new Point(480, 5),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(35, 40, 52),
                    ForeColor = Color.White
                };
                btnClose.FlatAppearance.BorderColor = Color.FromArgb(70, 80, 100);
                btnClose.Click += (s, e) => guideForm.Close();

                pnlBtn.Controls.Add(btnPortal);
                pnlBtn.Controls.Add(btnClose);

                pnl.Controls.Add(lblMsg);
                pnl.Controls.Add(pnlBtn);
                guideForm.Controls.Add(pnl);
                guideForm.ShowDialog(this);
            }
        }
    }
}
