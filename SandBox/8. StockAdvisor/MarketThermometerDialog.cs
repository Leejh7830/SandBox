using System;
using System.Drawing;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    public class MarketThermometerDialog : Form
    {
        private MarketTemperatureResult _result;
        private Label _lblHeader;
        private Label _lblSyncTime;
        private Label _lblKospiStatus;
        private Label _lblGuideContent;
        private Panel _pnlTop;
        private Button _btnRefresh;

        public MarketThermometerDialog(MarketTemperatureResult result)
        {
            _result = result ?? new MarketTemperatureResult();
            InitializeComponent();
            UpdateUi();
        }

        private void InitializeComponent()
        {
            this.Text = "실시간 코스피 시장 체온계 & 장세 대응 가이드";
            this.Size = new Size(600, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.BackColor = Color.FromArgb(18, 22, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Malgun Gothic", 9f);
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };

            // 1. 상단 타이틀 배너
            _pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 125,
                Padding = new Padding(16, 10, 16, 10)
            };

            var pnlTitleRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent
            };

            _lblHeader = new Label
            {
                Text = $"현재 시장 체온 : {_result.Temperature:F1}℃ [{_result.StatusName}]",
                Font = new Font("Malgun Gothic", 14f, FontStyle.Bold),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            _lblSyncTime = new Label
            {
                Text = $"동기화: {_result.UpdatedAt:yyyy-MM-dd HH:mm:ss}",
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 220, 255),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true,
                Padding = new Padding(0, 6, 0, 0)
            };

            pnlTitleRow.Controls.Add(_lblSyncTime);
            pnlTitleRow.Controls.Add(_lblHeader);

            _lblKospiStatus = new Label
            {
                Font = new Font("Malgun Gothic", 9.2f),
                ForeColor = Color.FromArgb(220, 230, 245),
                Dock = DockStyle.Fill
            };

            _pnlTop.Controls.Add(_lblKospiStatus);
            _pnlTop.Controls.Add(pnlTitleRow);

            // 2. 중앙 내용 영역
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 10, 16, 8)
            };

            var lblGuideTitle = new Label
            {
                Text = "【 현재 장세 진단 & 권고 행동 요령 】",
                Font = new Font("Malgun Gothic", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 255),
                Dock = DockStyle.Top,
                Height = 26
            };

            _lblGuideContent = new Label
            {
                Font = new Font("Malgun Gothic", 9.5f),
                ForeColor = Color.FromArgb(235, 245, 255),
                BackColor = Color.FromArgb(24, 28, 40),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Top,
                Height = 118,
                Padding = new Padding(10)
            };

            var lblStandardTitle = new Label
            {
                Text = "\n【 시장 체온계 5단계 판정 기준표 】",
                Font = new Font("Malgun Gothic", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 205, 235),
                Dock = DockStyle.Top,
                Height = 36
            };

            var lblStandardTable = new Label
            {
                Text = "• 38.5℃ 이상 [고열 과열] : 20일선 급이격 슈팅. 추격매수 금지 & 현금 40% 익절 확보\n" +
                       "• 37.0℃ ~ 38.4℃ [활황 상승] : 20일선 우상향 탄력. 1차 매수 타점 주도주 공략 (현금 20%)\n" +
                       "• 36.0℃ ~ 36.9℃ [정상 균형] : 20일선 안착 적정 궤도. 퀀트 8대 우량주 원칙 매매 (현금 30%)\n" +
                       "• 34.5℃ ~ 35.9℃ [조정 눌림] : 20일선 하회 눌림목. 2차 매수가(추매) 분할 접근 (현금 40%)\n" +
                       "• 34.5℃ 미만 [공포 투매] : 시장 빙하기 패닉. 뇌동매매 원천 금지 & 현금 60% 절대 방어",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(170, 190, 215),
                BackColor = Color.FromArgb(20, 24, 34),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            pnlBody.Controls.Add(lblStandardTable);
            pnlBody.Controls.Add(lblStandardTitle);
            pnlBody.Controls.Add(_lblGuideContent);
            pnlBody.Controls.Add(lblGuideTitle);

            // 3. 하단 액션 버튼
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(20, 24, 34),
                Padding = new Padding(16, 8, 16, 8)
            };

            _btnRefresh = new Button
            {
                Text = "시세 동기화",
                Dock = DockStyle.Left,
                Width = 110,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 38, 56),
                ForeColor = Color.FromArgb(100, 210, 255),
                Cursor = Cursors.Hand
            };
            _btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 230);
            _btnRefresh.Click += async (s, e) =>
            {
                _btnRefresh.Enabled = false;
                _btnRefresh.Text = "동기화 중...";
                var fresh = await MarketThermometerService.GetMarketTemperatureAsync(true).ConfigureAwait(true);
                _result = fresh;
                UpdateUi();
                _btnRefresh.Text = "시세 동기화";
                _btnRefresh.Enabled = true;
            };

            var btnClose = new Button
            {
                Text = "확인 (Esc)",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Right,
                Width = 100,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(32, 40, 56),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(70, 90, 130);
            
            pnlBottom.Controls.Add(_btnRefresh);
            pnlBottom.Controls.Add(btnClose);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(_pnlTop);
        }

        private void UpdateUi()
        {
            _pnlTop.BackColor = _result.BadgeBackColor;
            _lblHeader.Text = $"현재 시장 체온 : {_result.Temperature:F1}℃ [{_result.StatusName}]";
            _lblHeader.ForeColor = _result.BadgeForeColor;

            _lblSyncTime.Text = $"동기화: {_result.UpdatedAt:yyyy-MM-dd HH:mm:ss}";

            string chgPrefix = _result.KospiChangeRate > 0 ? "+" : "";
            _lblKospiStatus.Text =
                $"• 코스피 종합 지수 : {_result.KospiCurrent:N2} pt ({chgPrefix}{_result.KospiChangeRate:F2}% | {_result.KospiChangePoint:+0.00;-0.00;0.00} pt)\n" +
                $"• 20일 이동평균선(MA20) : {_result.KospiMa20:N2} pt  (20일선 이격도: {_result.DisparityPercent:+0.00;-0.00;0.00}%)\n" +
                $"• 실시간 동기화 : {_result.UpdatedAt:HH:mm:ss} 정상 수신 (네이버 코스피 기준)";

            _lblGuideContent.Text =
                $"• 진단 국면 : {_result.MarketPhase}\n" +
                $"• 권고 비중 : {_result.CashRatioGuide}\n\n" +
                $"• 실전 지침 : {_result.ActionGuide}";
        }
    }
}
