using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    public class StockItemDialog : Form
    {
        private TextBox txtSearchQuery;
        private TextBox txtCode;
        private TextBox txtName;
        private NumericUpDown numQuantity;
        private NumericUpDown numBuyPrice;
        private TextBox txtMemo;
        private Button btnFetchPrice;
        private Button btnSave;
        private Button btnCancel;
        private Label lblPreviewPrice;

        private bool _isPriceVerified = false;

        public StockHoldingItem Item { get; private set; }

        public StockItemDialog(StockHoldingItem existingItem = null)
        {
            this.Text = existingItem == null ? "📌 보유 / 관심 종목 등록" : "✏️ 종목 정보 수정";
            this.Size = new Size(520, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(20, 24, 34);
            this.ForeColor = Color.White;

            InitializeCustomUi();

            if (existingItem != null)
            {
                Item = existingItem;
                txtSearchQuery.Text = existingItem.StockName;
                txtCode.Text = existingItem.StockCode;
                txtName.Text = existingItem.StockName;
                numQuantity.Value = Math.Max(0, existingItem.Quantity);
                numBuyPrice.Value = Math.Max(0, existingItem.BuyPrice);
                txtMemo.Text = existingItem.Memo;
                lblPreviewPrice.Text = $"현재가: {existingItem.CurrentPrice:N0}원 ({existingItem.ChangeRate:+0.00;-0.00}%)";
                _isPriceVerified = true;
            }
            else
            {
                Item = new StockHoldingItem();
            }
        }

        private void InitializeCustomUi()
        {
            var pnlMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(22, 18, 22, 18),
                ColumnCount = 2,
                RowCount = 8,
                BackColor = Color.FromArgb(20, 24, 34)
            };
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // 0: 검색
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // 1: 코드
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // 2: 종목명
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // 3: 수량
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // 4: 매수가
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // 5: 안내 라벨
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 6: 메모
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // 7: 버튼 행

            Font lblFont = new Font("Malgun Gothic", 9.5f, FontStyle.Bold);
            Font inputFont = new Font("Segoe UI", 10f);

            // 1. 종목 검색창 (종목명 or 코드 자유 입력)
            var lblSearch = new Label { Text = "종목명 / 코드:", Font = lblFont, ForeColor = Color.FromArgb(0, 230, 255), Anchor = AnchorStyles.Left };
            var pnlCodeRow = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };

            btnFetchPrice = new Button
            {
                Text = "🔍 시세 조회",
                Dock = DockStyle.Right,
                Width = 100,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 65, 110),
                ForeColor = Color.FromArgb(120, 220, 255),
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnFetchPrice.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 255);
            btnFetchPrice.Click += async (s, e) => await AutoFetchStockInfoAsync();

            txtSearchQuery = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = inputFont,
                BackColor = Color.FromArgb(32, 38, 50),
                ForeColor = Color.White,
                MaxLength = 50
            };
            txtSearchQuery.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await AutoFetchStockInfoAsync();
                }
            };

            pnlCodeRow.Controls.Add(txtSearchQuery);
            pnlCodeRow.Controls.Add(btnFetchPrice);

            // 2. 자동 인식된 종목코드 (읽기 전용 표시)
            var lblCode = new Label { Text = "확정 종목코드:", Font = lblFont, ForeColor = Color.FromArgb(160, 180, 200), Anchor = AnchorStyles.Left };
            txtCode = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = inputFont,
                BackColor = Color.FromArgb(24, 28, 38),
                ForeColor = Color.FromArgb(200, 220, 240),
                ReadOnly = true
            };

            // 3. 확정 종목명
            var lblName = new Label { Text = "확정 종목명:", Font = lblFont, ForeColor = Color.FromArgb(160, 180, 200), Anchor = AnchorStyles.Left };
            txtName = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = inputFont,
                BackColor = Color.FromArgb(24, 28, 38),
                ForeColor = Color.FromArgb(200, 220, 240),
                ReadOnly = true
            };

            // 4. 보유 수량
            var lblQty = new Label { Text = "보유 수량:", Font = lblFont, ForeColor = Color.FromArgb(180, 200, 220), Anchor = AnchorStyles.Left };
            numQuantity = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Font = inputFont,
                BackColor = Color.FromArgb(32, 38, 50),
                ForeColor = Color.White,
                Maximum = 100000000,
                ThousandsSeparator = true
            };

            // 5. 매수 평단가
            var lblBuy = new Label { Text = "매수 평단가:", Font = lblFont, ForeColor = Color.FromArgb(180, 200, 220), Anchor = AnchorStyles.Left };
            numBuyPrice = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Font = inputFont,
                BackColor = Color.FromArgb(32, 38, 50),
                ForeColor = Color.White,
                Maximum = 1000000000,
                ThousandsSeparator = true
            };

            // 6. 실시간 시세 상태 안내
            lblPreviewPrice = new Label
            {
                Text = "※ 종목명(카카오, 테슬라 등) 또는 코드 입력 후 Enter를 누르세요.",
                Font = new Font("Malgun Gothic", 8.5f),
                ForeColor = Color.FromArgb(120, 175, 220),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            // 7. 투자 메모
            var lblMemo = new Label { Text = "투자 메모:", Font = lblFont, ForeColor = Color.FromArgb(180, 200, 220), Anchor = AnchorStyles.Left };
            txtMemo = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = inputFont,
                BackColor = Color.FromArgb(32, 38, 50),
                ForeColor = Color.White,
                Multiline = true
            };

            // 8. 하단 버튼
            var pnlBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 6, 0, 0)
            };
            btnCancel = new Button
            {
                Text = "취소",
                Width = 90,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 48, 62),
                ForeColor = Color.FromArgb(200, 210, 225),
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnSave = new Button
            {
                Text = "✔ 등록/저장",
                Width = 110,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 120, 175),
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 1;
            btnSave.FlatAppearance.BorderColor = Color.FromArgb(0, 200, 255);
            btnSave.Click += async (s, e) => await HandleSaveClickAsync();

            pnlBtns.Controls.Add(btnCancel);
            pnlBtns.Controls.Add(btnSave);

            pnlMain.Controls.Add(lblSearch, 0, 0);
            pnlMain.Controls.Add(pnlCodeRow, 1, 0);

            pnlMain.Controls.Add(lblCode, 0, 1);
            pnlMain.Controls.Add(txtCode, 1, 1);

            pnlMain.Controls.Add(lblName, 0, 2);
            pnlMain.Controls.Add(txtName, 1, 2);

            pnlMain.Controls.Add(lblQty, 0, 3);
            pnlMain.Controls.Add(numQuantity, 1, 3);

            pnlMain.Controls.Add(lblBuy, 0, 4);
            pnlMain.Controls.Add(numBuyPrice, 1, 4);

            pnlMain.Controls.Add(new Label(), 0, 5);
            pnlMain.Controls.Add(lblPreviewPrice, 1, 5);

            pnlMain.Controls.Add(lblMemo, 0, 6);
            pnlMain.Controls.Add(txtMemo, 1, 6);

            pnlMain.Controls.Add(new Label(), 0, 7);
            pnlMain.Controls.Add(pnlBtns, 1, 7);

            this.Controls.Add(pnlMain);
            this.CancelButton = btnCancel;
        }

        private async Task<bool> AutoFetchStockInfoAsync()
        {
            string query = txtSearchQuery.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                lblPreviewPrice.Text = "종목명 또는 종목코드를 입력해주세요.";
                lblPreviewPrice.ForeColor = Color.Orange;
                return false;
            }

            btnFetchPrice.Enabled = false;
            btnFetchPrice.Text = "조회 중...";

            try
            {
                var priceInfo = await NaverStockPriceService.FetchPriceAsync(query);
                if (priceInfo.IsSuccess)
                {
                    txtCode.Text = priceInfo.StockCode;
                    txtName.Text = priceInfo.StockName;

                    if (numBuyPrice.Value == 0)
                    {
                        numBuyPrice.Value = priceInfo.CurrentPrice;
                    }

                    string currency = priceInfo.IsForeign ? "$" : "원";
                    lblPreviewPrice.Text = $"✔ 조회 성공: {priceInfo.StockName} | 현재가 {priceInfo.CurrentPrice:N0}{currency} ({priceInfo.ChangeRate:+0.00;-0.00}%)";
                    lblPreviewPrice.ForeColor = Color.FromArgb(70, 230, 140);
                    _isPriceVerified = true;
                    return true;
                }
                else
                {
                    _isPriceVerified = false;
                    lblPreviewPrice.Text = $"❌ 조회 실패: {priceInfo.ErrorMessage}";
                    lblPreviewPrice.ForeColor = Color.FromArgb(255, 110, 110);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _isPriceVerified = false;
                lblPreviewPrice.Text = $"오류: {ex.Message}";
                lblPreviewPrice.ForeColor = Color.FromArgb(255, 110, 110);
                return false;
            }
            finally
            {
                btnFetchPrice.Enabled = true;
                btnFetchPrice.Text = "🔍 시세 조회";
            }
        }

        private async Task HandleSaveClickAsync()
        {
            string searchInput = txtSearchQuery.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchInput))
            {
                MessageBox.Show("종목명 또는 종목코드를 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSearchQuery.Focus();
                return;
            }

            // 시세 검증이 아직 안 된 상태라면 먼저 자동 조회를 시도
            if (!_isPriceVerified || string.IsNullOrWhiteSpace(txtCode.Text))
            {
                bool success = await AutoFetchStockInfoAsync();
                if (!success)
                {
                    MessageBox.Show(
                        "네이버 증권에서 조회되지 않는 종목입니다.\n\n" +
                        "오타나 비정상적인 종목명은 등록할 수 없습니다.\n" +
                        "정확한 종목명(예: 카카오, 삼성전자, 테슬라) 또는 종목코드를 입력해주세요.",
                        "종목 확인 실패",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    txtSearchQuery.Focus();
                    return;
                }
            }

            // 유효성 검증 완료 후 최종 저장
            Item.StockCode = txtCode.Text.Trim();
            Item.StockName = txtName.Text.Trim();
            Item.Quantity = (int)numQuantity.Value;
            Item.BuyPrice = numBuyPrice.Value;
            Item.Memo = txtMemo.Text.Trim();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
