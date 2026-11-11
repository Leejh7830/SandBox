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
        private RadioButton rdoHolding;
        private RadioButton rdoWatchlist;
        private NumericUpDown numQuantity;
        private NumericUpDown numBuyPrice;
        private TextBox txtMemo;
        private Button btnFetchPrice;
        private Button btnSave;
        private Button btnCancel;
        private Label lblPreviewPrice;
        private StockSearchPopup _searchPopup;

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

                if (existingItem.IsWatchlist)
                {
                    rdoWatchlist.Checked = true;
                }
                else
                {
                    rdoHolding.Checked = true;
                }
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
                RowCount = 9,
                BackColor = Color.FromArgb(20, 24, 34)
            };
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // 0: 검색
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // 1: 코드
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // 2: 종목명
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // 3: 종목 구분(보유 vs 관심)
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // 4: 수량
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // 5: 매수가
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // 6: 안내 라벨
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 7: 메모
            pnlMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // 8: 버튼 행

            Font lblFont = new Font("Malgun Gothic", 9.5f, FontStyle.Bold);
            Font inputFont = new Font("Segoe UI", 9.5f);

            // 1. 종목 검색창 (종목명 or 코드 자유 입력)
            var lblSearch = new Label { Text = "종목명 / 코드:", Font = lblFont, ForeColor = Color.FromArgb(0, 230, 255), Anchor = AnchorStyles.Left };
            var pnlCodeRow = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };

            _searchPopup = new StockSearchPopup();
            _searchPopup.StockSelected += async (item) =>
            {
                txtSearchQuery.Text = item.Name;
                await FetchPriceDirectAsync(item.Code, item.Name);
            };
            this.FormClosing += (s, e) =>
            {
                _searchPopup?.Close();
                _searchPopup?.Dispose();
            };

            btnFetchPrice = new Button
            {
                Text = "시세 조회",
                Size = new Size(88, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 52, 85),
                ForeColor = Color.FromArgb(0, 216, 255),
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnFetchPrice.FlatAppearance.BorderColor = Color.FromArgb(0, 180, 230);
            btnFetchPrice.Click += async (s, e) => await AutoFetchStockInfoAsync();

            txtSearchQuery = new TextBox
            {
                Font = inputFont,
                BackColor = Color.FromArgb(32, 38, 50),
                ForeColor = Color.White,
                MaxLength = 50,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            txtSearchQuery.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await AutoFetchStockInfoAsync();
                }
                else if (e.KeyCode == Keys.Down && _searchPopup.Visible)
                {
                    _searchPopup.FocusList();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape && _searchPopup.Visible)
                {
                    _searchPopup.Close();
                    e.Handled = true;
                }
            };

            void LayoutSearchRow()
            {
                int topY = 3;
                btnFetchPrice.Location = new Point(pnlCodeRow.Width - btnFetchPrice.Width - 3, topY);
                txtSearchQuery.Location = new Point(0, topY);
                txtSearchQuery.Width = Math.Max(50, btnFetchPrice.Left - 8);
            }
            pnlCodeRow.Resize += (s, e) => LayoutSearchRow();
            LayoutSearchRow();

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

            // 4. 종목 구분 (보유 vs 관심)
            var lblType = new Label { Text = "종목 구분:", Font = lblFont, ForeColor = Color.FromArgb(0, 230, 255), Anchor = AnchorStyles.Left };
            var pnlType = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };
            rdoHolding = new RadioButton
            {
                Text = "보유 종목",
                Font = inputFont,
                ForeColor = Color.FromArgb(235, 245, 255),
                AutoSize = true,
                Checked = true,
                Margin = new Padding(0, 4, 16, 0),
                Cursor = Cursors.Hand
            };
            rdoWatchlist = new RadioButton
            {
                Text = "★ 관심 종목 (0주 / 매수 타점 대기)",
                Font = inputFont,
                ForeColor = Color.FromArgb(0, 220, 255),
                AutoSize = true,
                Checked = false,
                Margin = new Padding(0, 4, 0, 0),
                Cursor = Cursors.Hand
            };

            rdoHolding.CheckedChanged += (s, e) =>
            {
                if (rdoHolding.Checked)
                {
                    numQuantity.Enabled = true;
                    numBuyPrice.Enabled = true;
                    if (numQuantity.Value <= 0) numQuantity.Value = 1;
                    lblPreviewPrice.Text = "※ 보유 종목으로 등록됩니다. 평단가와 목표가/손절가가 실시간 추적됩니다.";
                }
            };

            rdoWatchlist.CheckedChanged += (s, e) =>
            {
                if (rdoWatchlist.Checked)
                {
                    numQuantity.Value = 0;
                    numQuantity.Enabled = false;
                    numBuyPrice.Value = 0;
                    numBuyPrice.Enabled = false;
                    lblPreviewPrice.Text = "※ 관심 종목으로 등록됩니다. AI 1차 권고 매수가 도달 시 실시간 알림이 제공됩니다.";
                }
            };

            pnlType.Controls.Add(rdoHolding);
            pnlType.Controls.Add(rdoWatchlist);

            // 5. 보유 수량
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

            // 6. 매수 평단가
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

            // 7. 실시간 시세 상태 안내
            lblPreviewPrice = new Label
            {
                Text = "※ 종목명(카카오, 테슬라 등) 또는 코드 입력 후 Enter를 누르세요.",
                Font = new Font("Malgun Gothic", 8.5f),
                ForeColor = Color.FromArgb(120, 175, 220),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            // 8. 투자 메모
            var lblMemo = new Label { Text = "투자 메모:", Font = lblFont, ForeColor = Color.FromArgb(180, 200, 220), Anchor = AnchorStyles.Left };
            txtMemo = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = inputFont,
                BackColor = Color.FromArgb(32, 38, 50),
                ForeColor = Color.White,
                Multiline = true
            };

            // 9. 하단 버튼
            var pnlBtns = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            btnCancel = new Button
            {
                Text = "취소",
                Size = new Size(85, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 42, 56),
                ForeColor = Color.FromArgb(200, 215, 230),
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand,
                Font = new Font("Malgun Gothic", 9f)
            };
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(70, 85, 110);

            btnSave = new Button
            {
                Text = "✔ 등록/저장",
                Size = new Size(115, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 120, 175),
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 9.2f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 1;
            btnSave.FlatAppearance.BorderColor = Color.FromArgb(0, 200, 255);
            btnSave.Click += async (s, e) => await HandleSaveClickAsync();

            void LayoutActionButtons()
            {
                int btnY = 5;
                btnCancel.Location = new Point(pnlBtns.Width - btnCancel.Width - 3, btnY);
                btnSave.Location = new Point(btnCancel.Left - btnSave.Width - 8, btnY);
            }
            pnlBtns.Resize += (s, e) => LayoutActionButtons();
            LayoutActionButtons();

            pnlBtns.Controls.Add(btnCancel);
            pnlBtns.Controls.Add(btnSave);

            pnlMain.Controls.Add(lblSearch, 0, 0);
            pnlMain.Controls.Add(pnlCodeRow, 1, 0);

            pnlMain.Controls.Add(lblCode, 0, 1);
            pnlMain.Controls.Add(txtCode, 1, 1);

            pnlMain.Controls.Add(lblName, 0, 2);
            pnlMain.Controls.Add(txtName, 1, 2);

            pnlMain.Controls.Add(lblType, 0, 3);
            pnlMain.Controls.Add(pnlType, 1, 3);

            pnlMain.Controls.Add(lblQty, 0, 4);
            pnlMain.Controls.Add(numQuantity, 1, 4);

            pnlMain.Controls.Add(lblBuy, 0, 5);
            pnlMain.Controls.Add(numBuyPrice, 1, 5);

            pnlMain.Controls.Add(new Label(), 0, 6);
            pnlMain.Controls.Add(lblPreviewPrice, 1, 6);

            pnlMain.Controls.Add(lblMemo, 0, 7);
            pnlMain.Controls.Add(txtMemo, 1, 7);

            pnlMain.Controls.Add(new Label(), 0, 8);
            pnlMain.Controls.Add(pnlBtns, 1, 8);

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

            // 6자리 순수 숫자 코드인 경우 직통 조회
            if (query.Length <= 6 && System.Text.RegularExpressions.Regex.IsMatch(query, @"^\d+$"))
            {
                _searchPopup?.Close();
                return await FetchPriceDirectAsync(query);
            }

            btnFetchPrice.Enabled = false;
            btnFetchPrice.Text = "검색 중...";
            lblPreviewPrice.Text = $"🔍 [{query}] 종목 후보 검색 중...";
            lblPreviewPrice.ForeColor = Color.FromArgb(0, 230, 255);

            try
            {
                var searchResults = await NaverStockPriceService.SearchStockAsync(query);
                if (searchResults == null || searchResults.Count == 0)
                {
                    _searchPopup?.Close();
                    _isPriceVerified = false;
                    lblPreviewPrice.Text = $"❌ '{query}' 종목을 네이버 증권에서 찾을 수 없습니다.";
                    lblPreviewPrice.ForeColor = Color.FromArgb(255, 110, 110);
                    return false;
                }

                if (searchResults.Count == 1)
                {
                    _searchPopup?.Close();
                    txtSearchQuery.Text = searchResults[0].Name;
                    return await FetchPriceDirectAsync(searchResults[0].Code, searchResults[0].Name);
                }
                else
                {
                    // 2건 이상인 경우 드롭다운 후보 리스트 팝업 표시
                    lblPreviewPrice.Text = $"🔍 '{query}' 검색 결과 {searchResults.Count}건. 아래 목록에서 원하는 종목을 선택하세요.";
                    lblPreviewPrice.ForeColor = Color.FromArgb(120, 220, 255);
                    _searchPopup.ShowResults(txtSearchQuery, searchResults);
                    return false;
                }
            }
            catch (Exception ex)
            {
                lblPreviewPrice.Text = $"오류: {ex.Message}";
                lblPreviewPrice.ForeColor = Color.FromArgb(255, 110, 110);
                return false;
            }
            finally
            {
                btnFetchPrice.Enabled = true;
                btnFetchPrice.Text = "시세 조회";
            }
        }

        private async Task<bool> FetchPriceDirectAsync(string code, string fallbackName = null)
        {
            _searchPopup?.Close();
            btnFetchPrice.Enabled = false;
            btnFetchPrice.Text = "조회 중...";

            try
            {
                var priceInfo = await NaverStockPriceService.FetchPriceAsync(code);
                if (priceInfo.IsSuccess)
                {
                    txtCode.Text = priceInfo.StockCode;
                    txtName.Text = !string.IsNullOrEmpty(priceInfo.StockName) ? priceInfo.StockName : (fallbackName ?? string.Empty);

                    if (numBuyPrice.Value == 0)
                    {
                        numBuyPrice.Value = priceInfo.CurrentPrice;
                    }

                    string currency = priceInfo.IsForeign ? "$" : "원";
                    lblPreviewPrice.Text = $"✔ 조회 성공: {txtName.Text} | 현재가 {priceInfo.CurrentPrice:N0}{currency} ({priceInfo.ChangeRate:+0.00;-0.00}%)";
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
                btnFetchPrice.Text = "시세 조회";
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
            Item.Quantity = rdoWatchlist.Checked ? 0 : (int)numQuantity.Value;
            Item.BuyPrice = rdoWatchlist.Checked ? 0m : numBuyPrice.Value;
            Item.Memo = txtMemo.Text.Trim();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
