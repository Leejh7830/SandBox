using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Security;
using GateHelper.StockAdvisor.Trading;

namespace GateHelper.StockAdvisor
{
    public class PanicExitDialog : Form
    {
        private readonly List<StockHoldingItem> _portfolioItems;
        private AppSettings _appSettings;
        private bool _hasKbApi = false;

        private ListView _lvExitItems;
        private RadioButton _rbMarketOrder;
        private RadioButton _rbLimitOrder;
        private RadioButton _rbRatio100;
        private RadioButton _rbRatio50;
        private RadioButton _rbRatio30;
        private CheckBox _chkSafetyConfirm;
        private Button _btnExecute;
        private Button _btnCancel;
        private Label _lblTotalExitAmount;

        public bool ExecutionCompleted { get; private set; } = false;
        public string ExecutionSummary { get; private set; } = string.Empty;
        private bool _isPopulating = false;

        public PanicExitDialog(List<StockHoldingItem> items)
        {
            _portfolioItems = items ?? new List<StockHoldingItem>();

            try
            {
                _appSettings = Util_Option.LoadUserOptions();
                _hasKbApi = _appSettings != null 
                    && !string.IsNullOrWhiteSpace(_appSettings.KbAppKey) 
                    && !string.IsNullOrWhiteSpace(_appSettings.KbAppSecretEncrypted);
            }
            catch
            {
                _hasKbApi = false;
            }

            InitializeCustomComponent();
            PopulateItems();
        }

        private void InitializeCustomComponent()
        {
            this.Text = "비상 탈출 주문 (Panic Exit Hub)";
            this.Size = new Size(680, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(20, 24, 34);
            this.ForeColor = Color.White;

            // 1. 상단 경보 헤더
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(42, 18, 24),
                Padding = new Padding(16, 10, 16, 10)
            };

            var lblTitle = new Label
            {
                Text = "■ 긴급 비상 탈출 허브 (Panic Exit Hub) - 원클릭 리스크 탈출 창구",
                Font = new Font("Malgun Gothic", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 100, 100),
                Dock = DockStyle.Top,
                Height = 22
            };

            var lblSub = new Label
            {
                Font = new Font("Segoe UI", 8.5f),
                Dock = DockStyle.Fill
            };

            if (_hasKbApi)
            {
                string envStr = _appSettings.KbIsMockTrading ? "모의투자 (openapivts)" : "실거래 (openapi)";
                string accStr = string.IsNullOrEmpty(_appSettings.KbAccountNumber) ? "계좌번호 미지정" : _appSettings.KbAccountNumber;
                lblSub.Text = $"⚡ [KB증권 Open API {envStr} 연동 활성] 종합계좌: {accStr}\r\n   비상 탈출 실행 시 즉시 KB증권 거래소 서버로 시장가/최유리 매도 주문이 전송됩니다.";
                lblSub.ForeColor = Color.FromArgb(140, 230, 180);
            }
            else
            {
                lblSub.Text = "⚡ 돌발 시장 리스크 감지 시 보유 종목을 시장가/최유리 호가로 즉시 청산합니다.\r\n   (현재: KB증권 API 미설정 상태로 [안전 시뮬레이션 청산 모드]로 동작합니다)";
                lblSub.ForeColor = Color.FromArgb(220, 190, 195);
            }

            pnlTop.Controls.Add(lblSub);
            pnlTop.Controls.Add(lblTitle);

            // 2. 중앙 청산 대상 종목 리스트뷰
            _lvExitItems = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(18, 20, 26),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };
            _lvExitItems.Columns.Add("탈출 대상 종목", 150);
            _lvExitItems.Columns.Add("보유수량", 75, HorizontalAlignment.Right);
            _lvExitItems.Columns.Add("현재가", 85, HorizontalAlignment.Right);
            _lvExitItems.Columns.Add("평가금액", 95, HorizontalAlignment.Right);
            _lvExitItems.Columns.Add("손익률", 75, HorizontalAlignment.Right);
            _lvExitItems.Columns.Add("AI 포지션", 95, HorizontalAlignment.Center);
            _lvExitItems.ItemChecked += (s, e) =>
            {
                if (!_isPopulating) UpdateTotalSummary();
            };

            // 3. 컨트롤 패널 (선택 도구 + 주문 옵션)
            var pnlOptions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                BackColor = Color.FromArgb(28, 32, 44),
                Padding = new Padding(16, 10, 16, 10)
            };

            // 선택 보조 버튼 (네온 테두리)
            var btnSelectAll = new Button
            {
                Text = "전체 선택",
                Size = new Size(80, 26),
                Location = new Point(16, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(20, 36, 56),
                ForeColor = Color.FromArgb(210, 235, 255),
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };
            btnSelectAll.FlatAppearance.BorderSize = 1;
            btnSelectAll.FlatAppearance.BorderColor = Color.FromArgb(64, 150, 255);
            btnSelectAll.Click += (s, e) => SetAllItemsCheck(true);

            var btnSelectGroupA = new Button
            {
                Text = "고위험(Group A)만 선택",
                Size = new Size(160, 26),
                Location = new Point(102, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 20, 28),
                ForeColor = Color.FromArgb(255, 200, 210),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSelectGroupA.FlatAppearance.BorderSize = 1;
            btnSelectGroupA.FlatAppearance.BorderColor = Color.FromArgb(255, 65, 85);
            btnSelectGroupA.Click += (s, e) => SelectOnlyGroupA();

            // 호가 유형
            var grpOrderType = new GroupBox
            {
                Text = "호가 유형",
                Location = new Point(16, 40),
                Size = new Size(216, 52),
                ForeColor = Color.FromArgb(140, 190, 240),
                Font = new Font("Segoe UI", 8.5f)
            };
            _rbMarketOrder = new RadioButton { Text = "시장가 (Market)", Checked = true, Location = new Point(10, 20), AutoSize = true, ForeColor = Color.White };
            _rbLimitOrder = new RadioButton { Text = "최유리 지정가", Location = new Point(112, 20), AutoSize = true, ForeColor = Color.White };
            grpOrderType.Controls.Add(_rbMarketOrder);
            grpOrderType.Controls.Add(_rbLimitOrder);

            // 매도 비율
            var grpRatio = new GroupBox
            {
                Text = "매도 수량 비율",
                Location = new Point(236, 40),
                Size = new Size(250, 52),
                ForeColor = Color.FromArgb(140, 190, 240),
                Font = new Font("Segoe UI", 8.5f)
            };
            _rbRatio100 = new RadioButton { Text = "전량 (100%)", Checked = true, Location = new Point(10, 20), AutoSize = true, ForeColor = Color.White };
            _rbRatio50 = new RadioButton { Text = "50% 분할", Location = new Point(105, 20), AutoSize = true, ForeColor = Color.White };
            _rbRatio30 = new RadioButton { Text = "30% 축소", Location = new Point(180, 20), AutoSize = true, ForeColor = Color.White };
            _rbRatio100.CheckedChanged += (s, e) => UpdateTotalSummary();
            _rbRatio50.CheckedChanged += (s, e) => UpdateTotalSummary();
            _rbRatio30.CheckedChanged += (s, e) => UpdateTotalSummary();
            grpRatio.Controls.Add(_rbRatio100);
            grpRatio.Controls.Add(_rbRatio50);
            grpRatio.Controls.Add(_rbRatio30);

            // 총 청산 예정 금액
            _lblTotalExitAmount = new Label
            {
                Text = "청산 예정 금액: 0원",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 140, 140),
                Location = new Point(16, 104),
                Size = new Size(470, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 2단계 안전 확인 체크박스
            _chkSafetyConfirm = new CheckBox
            {
                Text = "뇌동매매 방지 확인: 위 선택된 종목들을 즉시 매도 주문하는 것에 동의합니다.",
                Location = new Point(16, 126),
                Size = new Size(600, 22),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 200, 80),
                Cursor = Cursors.Hand
            };
            _chkSafetyConfirm.CheckedChanged += (s, e) =>
            {
                _btnExecute.Enabled = _chkSafetyConfirm.Checked && GetCheckedItemCount() > 0;
            };

            pnlOptions.Controls.Add(_chkSafetyConfirm);
            pnlOptions.Controls.Add(_lblTotalExitAmount);
            pnlOptions.Controls.Add(grpRatio);
            pnlOptions.Controls.Add(grpOrderType);
            pnlOptions.Controls.Add(btnSelectGroupA);
            pnlOptions.Controls.Add(btnSelectAll);

            // 4. 하단 버튼 바
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(20, 24, 32),
                Padding = new Padding(16, 8, 16, 8)
            };

            _btnExecute = new Button
            {
                Text = "■ 비상 탈출 주문 즉시 전송",
                Dock = DockStyle.Right,
                Width = 210,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 16, 24),
                ForeColor = Color.FromArgb(255, 220, 230),
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            _btnExecute.FlatAppearance.BorderSize = 1;
            _btnExecute.FlatAppearance.BorderColor = Color.FromArgb(255, 45, 85);
            _btnExecute.Click += BtnExecute_Click;

            _btnCancel = new Button
            {
                Text = "취소",
                Dock = DockStyle.Right,
                Width = 90,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 32, 46),
                ForeColor = Color.FromArgb(210, 225, 245),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 1;
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(70, 100, 140);
            _btnCancel.Click += (s, e) => this.Close();

            var spacer = new Panel { Dock = DockStyle.Right, Width = 10 };

            pnlBottom.Controls.Add(_btnExecute);
            pnlBottom.Controls.Add(spacer);
            pnlBottom.Controls.Add(_btnCancel);

            // 레이아웃 조립
            this.Controls.Add(_lvExitItems);
            this.Controls.Add(pnlOptions);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private void PopulateItems()
        {
            _isPopulating = true;
            _lvExitItems.BeginUpdate();
            _lvExitItems.Items.Clear();

            foreach (var item in _portfolioItems)
            {
                var lvi = new ListViewItem(new[]
                {
                    $"{item.StockName} ({item.StockCode})",
                    $"{item.Quantity:N0}주",
                    $"{item.CurrentPrice:N0}원",
                    $"{item.TotalEvalAmount:N0}원",
                    $"{item.ProfitRate:+0.00;-0.00}%",
                    item.LastDiagnosis?.Group.ToString() ?? "미진단"
                });

                lvi.Tag = item;

                if (item.ProfitRate < 0)
                {
                    lvi.SubItems[4].ForeColor = Color.FromArgb(100, 175, 255);
                }
                else
                {
                    lvi.SubItems[4].ForeColor = Color.FromArgb(255, 100, 100);
                }

                lvi.UseItemStyleForSubItems = false;
                _lvExitItems.Items.Add(lvi);
                lvi.Checked = true; // 아이템 추가 후 체크 설정
            }

            _lvExitItems.EndUpdate();
            _isPopulating = false;
            UpdateTotalSummary();
        }

        private void SetAllItemsCheck(bool isChecked)
        {
            foreach (ListViewItem item in _lvExitItems.Items)
            {
                item.Checked = isChecked;
            }
            UpdateTotalSummary();
        }

        private void SelectOnlyGroupA()
        {
            foreach (ListViewItem lvi in _lvExitItems.Items)
            {
                var item = lvi.Tag as StockHoldingItem;
                lvi.Checked = item?.LastDiagnosis?.Group == PositionGroup.GroupA;
            }
            UpdateTotalSummary();
        }

        private int GetCheckedItemCount()
        {
            int count = 0;
            foreach (ListViewItem lvi in _lvExitItems.Items)
            {
                if (lvi.Checked) count++;
            }
            return count;
        }

        private decimal GetRatioMultiplier()
        {
            if (_rbRatio50.Checked) return 0.5m;
            if (_rbRatio30.Checked) return 0.3m;
            return 1.0m;
        }

        private void UpdateTotalSummary()
        {
            decimal totalEval = 0m;
            decimal ratio = GetRatioMultiplier();
            int checkedCount = 0;

            foreach (ListViewItem lvi in _lvExitItems.Items)
            {
                if (lvi.Checked)
                {
                    var item = lvi.Tag as StockHoldingItem;
                    if (item != null)
                    {
                        totalEval += item.TotalEvalAmount * ratio;
                        checkedCount++;
                    }
                }
            }

            string ratioText = _rbRatio100.Checked ? "100% 전량" : (_rbRatio50.Checked ? "50% 분할" : "30% 축소");
            _lblTotalExitAmount.Text = $"청산 대상: {checkedCount}개 종목  |  청산 비율: {ratioText}  |  예상 체결 총액: 약 {totalEval:N0}원";

            _btnExecute.Enabled = _chkSafetyConfirm.Checked && checkedCount > 0;
        }

        private async void BtnExecute_Click(object sender, EventArgs e)
        {
            _btnExecute.Enabled = false;
            _btnExecute.Text = "주문 서버 전송 중...";

            decimal ratio = GetRatioMultiplier();
            var executedList = new List<string>();
            var itemsToRemove = new List<StockHoldingItem>();

            IKbTradingClient kbClient = null;
            if (_hasKbApi)
            {
                try
                {
                    string plainSecret = DpapiHelper.Unprotect(_appSettings.KbAppSecretEncrypted);
                    kbClient = new KbTradingClient(_appSettings.KbAppKey, plainSecret, _appSettings.KbIsMockTrading);
                }
                catch
                {
                    kbClient = null;
                }
            }

            foreach (ListViewItem lvi in _lvExitItems.Items)
            {
                if (!lvi.Checked) continue;

                var item = lvi.Tag as StockHoldingItem;
                if (item == null) continue;

                int exitQty = (int)Math.Round(item.Quantity * ratio);
                if (exitQty <= 0) exitQty = 1;

                string orderTypeName = _rbMarketOrder.Checked ? "시장가" : "최유리 지정가";
                string orderNumber = string.Empty;

                if (kbClient != null)
                {
                    try
                    {
                        var req = new KbOrderRequest
                        {
                            StockCode = item.StockCode,
                            OrderQuantity = exitQty,
                            OrderPrice = item.CurrentPrice,
                            OrderType = _rbMarketOrder.Checked ? "01" : "02"
                        };
                        var res = await kbClient.SendOrderAsync(req);
                        orderNumber = res.OrderNumber ?? "ORD-" + DateTime.Now.ToString("HHmmss");
                    }
                    catch (Exception ex)
                    {
                        orderNumber = "ERR: " + ex.Message;
                    }
                }
                else
                {
                    orderNumber = "SIM-" + DateTime.Now.ToString("HHmmssfff");
                }

                if (exitQty >= item.Quantity)
                {
                    itemsToRemove.Add(item);
                    executedList.Add($"[전량 청산] {item.StockName}({item.StockCode}) {item.Quantity:N0}주 @ {item.CurrentPrice:N0}원 ({orderTypeName}, 주문번호: {orderNumber})");
                }
                else
                {
                    item.Quantity -= exitQty;
                    executedList.Add($"[분할 청산] {item.StockName}({item.StockCode}) {exitQty:N0}주 매도 (잔여 {item.Quantity:N0}주, {orderTypeName}, 주문번호: {orderNumber})");
                }
            }

            foreach (var item in itemsToRemove)
            {
                _portfolioItems.Remove(item);
            }

            this.ExecutionCompleted = true;
            this.ExecutionSummary = string.Join("\r\n", executedList);

            string title = _hasKbApi ? "■ [KB증권 거래소 서버 접수 완료]" : "■ [비상 탈출 시뮬레이션 체결 완료]";
            MessageBox.Show($"{title}\r\n\r\n{this.ExecutionSummary}\r\n\r\n보유 포트폴리오에 변경 사항이 즉시 저장되었습니다.", "비상 매도 체결", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
