using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor
{
    public enum PreviewEntryStatus
    {
        NewInsert,     // 🟢 신규 추가
        UpdateModify,  // 🔵 기존 갱신
        KeepExisting,  // ⚪ 기존 유지
        Removed        // 🔴 삭제 (전체 교체 모드 시 기존 종목)
    }

    public class PreviewStockEntry
    {
        public PreviewEntryStatus Status { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;
        public int OldQuantity { get; set; }
        public int NewQuantity { get; set; }
        public decimal OldBuyPrice { get; set; }
        public decimal NewBuyPrice { get; set; }
        public decimal FinalTotalAmount => NewQuantity * NewBuyPrice;
        public decimal WeightPercent { get; set; }
        public string DetailText { get; set; } = string.Empty;
        public StockHoldingItem TargetItem { get; set; }
    }

    /// <summary>
    /// 계좌 가져오기 데이터를 최종 포트폴리오에 적용하기 전에 
    /// 신규 추가, 단가/수량 갱신, 예상 비중을 한눈에 검증하는 최종 확인 미리보기 팝업
    /// </summary>
    public class PortfolioImportPreviewDialog : Form
    {
        private Label _lblSummaryCurrent;
        private Label _lblSummaryAfter;
        private Label _lblSummaryDiff;
        private ListView _lvFinalPreview;
        private Button _btnConfirmApply;
        private Button _btnBack;

        public bool IsConfirmed { get; private set; } = false;

        public PortfolioImportPreviewDialog(List<StockHoldingItem> currentPortfolio, List<StockHoldingItem> importedItems, bool isReplaceMode)
        {
            this.Text = "🔍 [최종 검증] 포트폴리오 반영 시뮬레이션 미리보기";
            this.Size = new Size(1020, 680);
            this.MinimumSize = new Size(920, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(18, 22, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Malgun Gothic", 9f);

            BuildUi(isReplaceMode);
            CalculateAndPopulateSimulation(currentPortfolio, importedItems, isReplaceMode);
        }

        #region UI Layout Construction

        private void BuildUi(bool isReplaceMode)
        {
            this.SuspendLayout();

            // 1. 상단 브리핑 카드 패널 (기존 vs 반영 후 비교 요약)
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(24, 28, 42),
                Padding = new Padding(16, 12, 16, 12)
            };

            var tblSummary = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1
            };
            tblSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            tblSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
            tblSummary.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var cardCurrent = CreateSummaryCard("현재 등록 계좌", out _lblSummaryCurrent, Color.FromArgb(140, 155, 180));
            var cardAfter = CreateSummaryCard(isReplaceMode ? "반영 후 예상 (전체 교체)" : "반영 후 예상 (추가/갱신 병합)", out _lblSummaryAfter, Color.FromArgb(0, 230, 255));
            var cardDiff = CreateSummaryCard("변동 내역 요약", out _lblSummaryDiff, Color.FromArgb(0, 230, 118));

            tblSummary.Controls.Add(cardCurrent, 0, 0);
            tblSummary.Controls.Add(cardAfter, 1, 0);
            tblSummary.Controls.Add(cardDiff, 2, 0);

            pnlTop.Controls.Add(tblSummary);

            // 2. 중앙 최종 시뮬레이션 리스트뷰
            var pnlCenter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 10, 16, 10)
            };

            var lblGridHeader = new Label
            {
                Text = "◆ 최종 표 적용 시뮬레이션 (적용 시 아래 데이터와 포맷 그대로 메인 화면에 반영됩니다):",
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(215, 230, 255),
                Dock = DockStyle.Top,
                Height = 24
            };

            _lvFinalPreview = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };

            _lvFinalPreview.Columns.Add("반영 구분", 110);
            _lvFinalPreview.Columns.Add("종목명 (코드)", 200);
            _lvFinalPreview.Columns.Add("보유수량", 100, HorizontalAlignment.Right);
            _lvFinalPreview.Columns.Add("평균매입단가", 130, HorizontalAlignment.Right);
            _lvFinalPreview.Columns.Add("최종매입총액", 140, HorizontalAlignment.Right);
            _lvFinalPreview.Columns.Add("예상비중(%)", 100, HorizontalAlignment.Right);
            _lvFinalPreview.Columns.Add("변동 내역 및 상세 안내", 200);

            pnlCenter.Controls.Add(_lvFinalPreview);
            pnlCenter.Controls.Add(lblGridHeader);

            // 3. 하단 액션 패널
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(24, 28, 42),
                Padding = new Padding(16, 10, 16, 10)
            };

            var lblNotice = new Label
            {
                Text = "💡 위 표의 수치와 종목이 맞는지 확인하신 후 [최종 적용]을 누르면 즉시 포트폴리오가 갱신됩니다.",
                ForeColor = Color.FromArgb(160, 180, 210),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Left,
                AutoSize = true
            };

            var pnlBtns = new Panel
            {
                Dock = DockStyle.Right,
                Width = 360
            };

            _btnConfirmApply = new Button
            {
                Text = "★ 확인: 포트폴리오에 최종 적용",
                BackColor = Color.FromArgb(16, 56, 42),
                ForeColor = Color.FromArgb(0, 230, 118),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };
            _btnConfirmApply.FlatAppearance.BorderColor = Color.FromArgb(0, 230, 118);
            _btnConfirmApply.FlatAppearance.BorderSize = 1;
            _btnConfirmApply.Click += (s, e) =>
            {
                IsConfirmed = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            _btnBack = new Button
            {
                Text = "← 이전 (가져오기 수정)",
                BackColor = Color.FromArgb(40, 46, 60),
                ForeColor = Color.FromArgb(200, 210, 230),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Malgun Gothic", 9f),
                Width = 140,
                Dock = DockStyle.Left,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            _btnBack.FlatAppearance.BorderColor = Color.FromArgb(70, 80, 100);
            _btnBack.FlatAppearance.BorderSize = 1;
            _btnBack.Click += (s, e) =>
            {
                IsConfirmed = false;
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            pnlBtns.Controls.Add(_btnConfirmApply);
            pnlBtns.Controls.Add(_btnBack);

            pnlBottom.Controls.Add(pnlBtns);
            pnlBottom.Controls.Add(lblNotice);

            this.Controls.Add(pnlCenter);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlBottom);

            this.ResumeLayout(false);
        }

        private static Panel CreateSummaryCard(string title, out Label valueLabel, Color accentColor)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(32, 38, 54),
                Margin = new Padding(4, 0, 4, 0),
                Padding = new Padding(10, 6, 10, 6)
            };

            var lblTitle = new Label
            {
                Text = title,
                ForeColor = Color.FromArgb(170, 185, 205),
                Font = new Font("Malgun Gothic", 8.5f),
                Dock = DockStyle.Top,
                Height = 18
            };

            valueLabel = new Label
            {
                Text = "계산 중...",
                ForeColor = accentColor,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnl.Controls.Add(valueLabel);
            pnl.Controls.Add(lblTitle);
            return pnl;
        }

        #endregion

        #region Simulation Calculation

        private void CalculateAndPopulateSimulation(List<StockHoldingItem> currentPortfolio, List<StockHoldingItem> importedItems, bool isReplaceMode)
        {
            _lvFinalPreview.BeginUpdate();
            _lvFinalPreview.Items.Clear();

            var currentList = currentPortfolio ?? new List<StockHoldingItem>();
            var importList = importedItems ?? new List<StockHoldingItem>();

            decimal currentTotalAmount = currentList.Sum(x => x.Quantity * x.BuyPrice);
            int currentCount = currentList.Count;

            var entries = new List<PreviewStockEntry>();

            if (isReplaceMode)
            {
                // 전체 교체 모드
                foreach (var imp in importList)
                {
                    entries.Add(new PreviewStockEntry
                    {
                        Status = PreviewEntryStatus.NewInsert,
                        StockCode = imp.StockCode,
                        StockName = imp.StockName,
                        OldQuantity = 0,
                        NewQuantity = imp.Quantity,
                        OldBuyPrice = 0,
                        NewBuyPrice = imp.BuyPrice,
                        DetailText = "전체 교체 신규 등록",
                        TargetItem = imp
                    });
                }
            }
            else
            {
                // 기존 포트폴리오 유지 및 병합 모드
                var importedCodeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var imp in importList)
                {
                    importedCodeSet.Add(imp.StockCode.TrimStart('0'));

                    var existing = currentList.FirstOrDefault(x => 
                        x.StockCode.TrimStart('0').Equals(imp.StockCode.TrimStart('0'), StringComparison.OrdinalIgnoreCase));

                    if (existing != null)
                    {
                        // 기존 종목 갱신
                        string diffNote = string.Empty;
                        if (existing.BuyPrice != imp.BuyPrice || existing.Quantity != imp.Quantity)
                        {
                            diffNote = $"단가: {existing.BuyPrice:N0}→{imp.BuyPrice:N0}원 | 수량: {existing.Quantity}→{imp.Quantity}주";
                        }
                        else
                        {
                            diffNote = "수량/단가 변동 없음";
                        }

                        entries.Add(new PreviewStockEntry
                        {
                            Status = PreviewEntryStatus.UpdateModify,
                            StockCode = imp.StockCode,
                            StockName = imp.StockName,
                            OldQuantity = existing.Quantity,
                            NewQuantity = imp.Quantity,
                            OldBuyPrice = existing.BuyPrice,
                            NewBuyPrice = imp.BuyPrice,
                            DetailText = diffNote,
                            TargetItem = imp
                        });
                    }
                    else
                    {
                        // 신규 종목 추가
                        entries.Add(new PreviewStockEntry
                        {
                            Status = PreviewEntryStatus.NewInsert,
                            StockCode = imp.StockCode,
                            StockName = imp.StockName,
                            OldQuantity = 0,
                            NewQuantity = imp.Quantity,
                            OldBuyPrice = 0,
                            NewBuyPrice = imp.BuyPrice,
                            DetailText = "신규 편입 종목",
                            TargetItem = imp
                        });
                    }
                }

                // 기존 종목 중 이번 가져오기에 포함되지 않은 종목들도 '기존 유지'로 함께 표시
                foreach (var cur in currentList)
                {
                    if (!importedCodeSet.Contains(cur.StockCode.TrimStart('0')))
                    {
                        entries.Add(new PreviewStockEntry
                        {
                            Status = PreviewEntryStatus.KeepExisting,
                            StockCode = cur.StockCode,
                            StockName = cur.StockName,
                            OldQuantity = cur.Quantity,
                            NewQuantity = cur.Quantity,
                            OldBuyPrice = cur.BuyPrice,
                            NewBuyPrice = cur.BuyPrice,
                            DetailText = "기존 보유 유지",
                            TargetItem = cur
                        });
                    }
                }
            }

            // 예상 총 매입금액 및 비중 계산
            decimal simulatedTotalAmount = entries.Sum(x => x.FinalTotalAmount);
            int newCount = entries.Count(x => x.Status == PreviewEntryStatus.NewInsert);
            int updateCount = entries.Count(x => x.Status == PreviewEntryStatus.UpdateModify);
            int keepCount = entries.Count(x => x.Status == PreviewEntryStatus.KeepExisting);

            foreach (var e in entries)
            {
                e.WeightPercent = simulatedTotalAmount > 0 ? (e.FinalTotalAmount / simulatedTotalAmount * 100m) : 0m;
            }

            // 상단 요약 카드 업데이트
            _lblSummaryCurrent.Text = $"{currentCount:N0}개 종목 | {currentTotalAmount:N0}원";
            _lblSummaryAfter.Text = $"{entries.Count:N0}개 종목 | {simulatedTotalAmount:N0}원";

            decimal diffAmount = simulatedTotalAmount - currentTotalAmount;
            string diffSign = diffAmount > 0 ? "+" : "";
            _lblSummaryDiff.Text = $"신규: {newCount}건, 갱신: {updateCount}건 | {diffSign}{diffAmount:N0}원";

            // 그리드 행 생성
            foreach (var e in entries)
            {
                string statusText = "";
                Color rowColor = Color.White;

                switch (e.Status)
                {
                    case PreviewEntryStatus.NewInsert:
                        statusText = "🟢 신규추가";
                        rowColor = Color.FromArgb(215, 255, 230);
                        break;
                    case PreviewEntryStatus.UpdateModify:
                        statusText = "🔵 기존갱신";
                        rowColor = Color.FromArgb(210, 240, 255);
                        break;
                    case PreviewEntryStatus.KeepExisting:
                        statusText = "⚪ 기존유지";
                        rowColor = Color.FromArgb(180, 190, 205);
                        break;
                }

                var lvi = new ListViewItem(statusText);
                lvi.ForeColor = rowColor;

                lvi.SubItems.Add($"{e.StockName} ({e.StockCode})");
                lvi.SubItems.Add(e.NewQuantity.ToString("N0") + "주");
                lvi.SubItems.Add(e.NewBuyPrice.ToString("N0") + "원");
                lvi.SubItems.Add(e.FinalTotalAmount.ToString("N0") + "원");
                lvi.SubItems.Add(e.WeightPercent.ToString("F1") + "%");
                lvi.SubItems.Add(e.DetailText);

                _lvFinalPreview.Items.Add(lvi);
            }

            _lvFinalPreview.EndUpdate();
        }

        #endregion
    }
}

