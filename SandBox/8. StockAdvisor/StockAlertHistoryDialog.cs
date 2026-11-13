using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 목표가 도달, 손절선 터치, AI 적정 매수가 진입 알림 기록을 한눈에 조회하고
    /// 더블클릭 시 종목 평가로 이동하거나 관심 종목으로 즉시 담을 수 있는 다이얼로그 (Zero-Resource, 0B 디스크 I/O)
    /// </summary>
    public class StockAlertHistoryDialog : Form
    {
        private readonly List<StockAlertLogItem> _history;
        private readonly HashSet<string> _existingCodes;
        private readonly Action<string, string, decimal> _onAddWatchlist;
        private readonly Func<string, Form, bool> _onEvaluateStock;

        private ListView _lvHistory;
        private Label _lblSummary;
        private Label _lblFooterGuide;
        private Button _btnClear;
        private Button _btnClose;

        // [신규] 4대 탭 필터 버튼
        private Button _btnTabAll;
        private Button _btnTabHit;
        private Button _btnTabNear;
        private Button _btnTabChanged;
        private int _currentTabFilter = 0; // 0: 전체, 1: 도달완료, 2: 1.5%도달임박, 3: 앱주문 타점변경

        private static readonly Font BoldFont = new Font("Segoe UI", 9.2f, FontStyle.Bold);

        public StockAlertHistoryDialog(
            List<StockAlertLogItem> history,
            Action<string> onSelectStock = null,
            HashSet<string> existingCodes = null,
            Action<string, string, decimal> onAddWatchlist = null,
            Func<string, Form, bool> onEvaluateStock = null)
        {
            _history = history ?? new List<StockAlertLogItem>();
            _existingCodes = existingCodes != null
                ? new HashSet<string>(existingCodes.Select(x => x.TrimStart('0')), StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _onAddWatchlist = onAddWatchlist;
            _onEvaluateStock = onEvaluateStock;

            this.Text = "가격 도달 알림 기록 내역 (Price Alert History)";
            this.Size = new Size(950, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(18, 22, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Malgun Gothic", 9f);
            this.MinimumSize = new Size(820, 420);

            InitializeCustomUi();
            LoadHistoryItems();

            this.Shown += (s, e) =>
            {
                _lvHistory.SelectedItems.Clear();
                if (_lvHistory.FocusedItem != null) _lvHistory.FocusedItem.Focused = false;
                this.ActiveControl = _btnClose;
            };
        }

        private void InitializeCustomUi()
        {
            // 1. 상단 헤더 (타이틀 + 서브안내 + 탭 바)
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 98,
                BackColor = Color.FromArgb(24, 28, 40),
                Padding = new Padding(16, 8, 16, 6)
            };

            var lblTitle = new Label
            {
                Text = "가격 도달 및 타점 변동 알림 기록 내역 (Price & Diagnosis Alert History)",
                Font = new Font("Malgun Gothic", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 190, 40),
                Dock = DockStyle.Top,
                Height = 24
            };

            _lblSummary = new Label
            {
                Text = "보유종목의 목표가/손절가 도달, 1.5% 도달임박, 및 앱주문 설정 종목의 AI 타점 변동 알림 히스토리입니다.",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(170, 190, 220),
                Dock = DockStyle.Top,
                Height = 22
            };

            // 4대 탭 필터 바 (전체 / 도달완료 / 1.5%도달임박 / 앱주문 타점변동)
            var flpTabs = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            _btnTabAll = CreateFilterTab("전체 알림", 0);
            _btnTabHit = CreateFilterTab("도달 완료", 1);
            _btnTabNear = CreateFilterTab("1.5% 도달임박", 2);
            _btnTabChanged = CreateFilterTab("앱주문 타점변경", 3);

            flpTabs.Controls.Add(_btnTabAll);
            flpTabs.Controls.Add(_btnTabHit);
            flpTabs.Controls.Add(_btnTabNear);
            flpTabs.Controls.Add(_btnTabChanged);

            pnlHeader.Controls.Add(flpTabs);
            pnlHeader.Controls.Add(_lblSummary);
            pnlHeader.Controls.Add(lblTitle);

            // 2. 리스트뷰
            _lvHistory = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = true,
                GridLines = true,
                BackColor = Color.FromArgb(18, 20, 26),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.2f),
                BorderStyle = BorderStyle.None,
                ShowItemToolTips = true
            };

            _lvHistory.Columns.Add("시간", 75, HorizontalAlignment.Center);
            _lvHistory.Columns.Add("알림 구분", 125, HorizontalAlignment.Center);
            _lvHistory.Columns.Add("코드", 65, HorizontalAlignment.Center);
            _lvHistory.Columns.Add("종목명", 120);
            _lvHistory.Columns.Add("현재가", 85, HorizontalAlignment.Right);
            _lvHistory.Columns.Add("감지가격", 85, HorizontalAlignment.Right);
            _lvHistory.Columns.Add("등락률", 75, HorizontalAlignment.Right);
            _lvHistory.Columns.Add("관심 등록", 80, HorizontalAlignment.Center);
            _lvHistory.Columns.Add("AI 대응 권고 및 메시지", 210);

            // 더블 버퍼링
            typeof(ListView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, _lvHistory, new object[] { true });

            // [+ 담기] 셀 클릭 시 관심종목 즉시 등록 (타점변동 탭에서는 관심등록 컬럼 없음)
            _lvHistory.MouseClick += (s, e) =>
            {
                if (_currentTabFilter == 3) return; // 타점변동 탭은 관심등록 컬럼 제외
                var hit = _lvHistory.HitTest(e.Location);
                if (hit.Item != null && hit.SubItem != null)
                {
                    int colIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
                    if (colIndex == 7 || hit.SubItem.Text == "+ 담기")
                    {
                        AddAlertItemToWatchlist(hit.Item);
                    }
                }
            };

            // 마우스 호버 시 [+ 담기] 셀 위에서 손가락(Hand) 커서 표시
            _lvHistory.MouseMove += (s, e) =>
            {
                if (_currentTabFilter == 3)
                {
                    _lvHistory.Cursor = Cursors.Default;
                    return;
                }
                var hit = _lvHistory.HitTest(e.Location);
                if (hit.Item != null && hit.SubItem != null && hit.SubItem.Text == "+ 담기")
                {
                    _lvHistory.Cursor = Cursors.Hand;
                }
                else
                {
                    _lvHistory.Cursor = Cursors.Default;
                }
            };

            // 행 더블클릭 시 해당 종목의 [종목 평가] 다이얼로그로 이동
            _lvHistory.MouseDoubleClick += (s, e) =>
            {
                var hit = _lvHistory.HitTest(e.Location);
                if (hit.Item == null) return;
                if (_currentTabFilter != 3 && hit.SubItem != null && hit.Item.SubItems.IndexOf(hit.SubItem) == 7) return;

                var alertItem = hit.Item.Tag as StockAlertLogItem;
                string code = alertItem != null ? alertItem.StockCode : (hit.Item.Tag as string);
                if (string.IsNullOrWhiteSpace(code)) return;

                if (_onEvaluateStock != null)
                {
                    bool added = _onEvaluateStock(code, this);
                    if (added)
                    {
                        MarkStockAsRegistered(code);
                    }
                }
                else
                {
                    using (var dlg = new StockEvaluatorDialog(code))
                    {
                        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.AddedStockItem != null)
                        {
                            MarkStockAsRegistered(code);
                        }
                    }
                }
            };

            // 3. 하단 푸터 바
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = Color.FromArgb(24, 28, 40),
                Padding = new Padding(16, 7, 16, 7)
            };

            _lblFooterGuide = new Label
            {
                Text = "[안내] 항목을 더블클릭하면 [종목 평가] 창으로 이동하며, [+ 담기] 클릭 시 관심종목에 등록됩니다.",
                Font = new Font("Malgun Gothic", 8.8f),
                ForeColor = Color.FromArgb(190, 215, 245),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            _btnClose = new Button
            {
                Text = "닫기",
                Font = new Font("Malgun Gothic", 9f),
                Size = new Size(76, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(32, 38, 52),
                ForeColor = Color.FromArgb(200, 215, 230),
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderColor = Color.FromArgb(60, 75, 100);
            _btnClose.Click += (s, e) => this.Close();

            _btnClear = new Button
            {
                Text = "기록 비우기",
                Font = new Font("Malgun Gothic", 8.8f),
                Size = new Size(88, 32),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(42, 24, 28),
                ForeColor = Color.FromArgb(255, 140, 150),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnClear.FlatAppearance.BorderColor = Color.FromArgb(140, 50, 60);
            _btnClear.Click += (s, e) =>
            {
                lock (_history)
                {
                    _history.Clear();
                }
                LoadHistoryItems();
            };

            pnlFooter.Controls.Add(_lblFooterGuide);
            pnlFooter.Controls.Add(_btnClear);
            pnlFooter.Controls.Add(_btnClose);

            this.Controls.Add(_lvHistory);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
        }

        private void AddAlertItemToWatchlist(ListViewItem lvi)
        {
            var alertItem = lvi?.Tag as StockAlertLogItem;
            if (alertItem == null || string.IsNullOrWhiteSpace(alertItem.StockCode)) return;

            string normCode = alertItem.StockCode.TrimStart('0');
            string cleanName = (alertItem.StockName ?? string.Empty).Replace("★레이더 ", "").Replace("★", "").Trim();

            if (_existingCodes.Contains(normCode))
            {
                ToastNotification.Show($"[{cleanName}] 종목은 이미 포트폴리오/관심종목에 등록되어 있습니다.", "알림", ToastIcon.Info);
                return;
            }

            _onAddWatchlist?.Invoke(alertItem.StockCode, cleanName, alertItem.CurrentPrice);
            MarkStockAsRegistered(alertItem.StockCode);
            ToastNotification.Show($"[{cleanName}]\n관심 종목에 등록되었습니다.", "관심 종목 등록 완료", ToastIcon.Success);
        }

        private void MarkStockAsRegistered(string stockCode)
        {
            if (string.IsNullOrWhiteSpace(stockCode)) return;
            string normCode = stockCode.TrimStart('0');
            _existingCodes.Add(normCode);

            foreach (ListViewItem row in _lvHistory.Items)
            {
                if (row.Tag is StockAlertLogItem itm &&
                    itm.StockCode != null &&
                    itm.StockCode.TrimStart('0').Equals(normCode, StringComparison.OrdinalIgnoreCase) &&
                    row.SubItems.Count > 7)
                {
                    row.SubItems[7].Text = "등록됨";
                    row.SubItems[7].ForeColor = Color.FromArgb(0, 216, 255);
                }
            }
        }

        private Button CreateFilterTab(string text, int filterMode)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Malgun Gothic", 8.8f, FontStyle.Bold),
                Size = new Size(130, 28),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 2, 6, 2),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += (s, e) =>
            {
                _currentTabFilter = filterMode;
                UpdateTabButtonStyles();
                LoadHistoryItems();
            };
            return btn;
        }

        private void ConfigureColumnsForTab(int tabIndex)
        {
            _lvHistory.Columns.Clear();
            _lvHistory.Columns.Add("시간", 75, HorizontalAlignment.Center);
            _lvHistory.Columns.Add("알림 구분", 125, HorizontalAlignment.Center);
            _lvHistory.Columns.Add("코드", 65, HorizontalAlignment.Center);
            _lvHistory.Columns.Add("종목명", 120);
            _lvHistory.Columns.Add("현재가", 85, HorizontalAlignment.Right);
            _lvHistory.Columns.Add("감지가격", 85, HorizontalAlignment.Right);
            _lvHistory.Columns.Add("등락률", 75, HorizontalAlignment.Right);

            if (tabIndex != 3) // 타점변동 탭(3)이 아닐 때만 [관심 등록] 열 노출
            {
                _lvHistory.Columns.Add("관심 등록", 80, HorizontalAlignment.Center);
                _lvHistory.Columns.Add("AI 대응 권고 및 메시지", 260);
            }
            else
            {
                // 타점변동 탭: [관심 등록] 열 제거 및 메시지 열 너비 확장
                _lvHistory.Columns.Add("AI 대응 권고 및 메시지", 340);
            }
        }

        private void UpdateTabButtonStyles()
        {
            SetSingleTabStyle(_btnTabAll, _currentTabFilter == 0);
            SetSingleTabStyle(_btnTabHit, _currentTabFilter == 1);
            SetSingleTabStyle(_btnTabNear, _currentTabFilter == 2);
            SetSingleTabStyle(_btnTabChanged, _currentTabFilter == 3);
        }

        private void SetSingleTabStyle(Button btn, bool isSelected)
        {
            if (btn == null) return;
            if (isSelected)
            {
                btn.BackColor = Color.FromArgb(0, 160, 210);
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderColor = Color.FromArgb(0, 220, 255);
            }
            else
            {
                btn.BackColor = Color.FromArgb(28, 34, 48);
                btn.ForeColor = Color.FromArgb(160, 180, 210);
                btn.FlatAppearance.BorderColor = Color.FromArgb(50, 62, 85);
            }
        }

        private void LoadHistoryItems()
        {
            UpdateTabButtonStyles();
            ConfigureColumnsForTab(_currentTabFilter);

            _lvHistory.BeginUpdate();
            _lvHistory.Items.Clear();

            int targetCount = 0;
            int stopCount = 0;
            int buyCount = 0;
            int nearCount = 0;
            int changedCount = 0;

            List<StockAlertLogItem> snapshot;
            lock (_history)
            {
                snapshot = new List<StockAlertLogItem>(_history);
                foreach (var item in snapshot) item.IsRead = true;
            }

            // 최신 알림이 맨 위에 오도록 역순 나열
            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                var item = snapshot[i];

                // 통계 집계
                if (item.AlertType == ToastAlertType.TargetHit) targetCount++;
                else if (item.AlertType == ToastAlertType.StopHit) stopCount++;
                else if (item.AlertType == ToastAlertType.BuyHit) buyCount++;
                else if (item.AlertType == ToastAlertType.TargetNear || item.AlertType == ToastAlertType.StopNear || item.AlertType == ToastAlertType.BuyNear) nearCount++;
                else if (item.AlertType == ToastAlertType.TargetChanged || item.AlertType == ToastAlertType.StopChanged) changedCount++;

                // 탭 필터링 (0: 전체, 1: 도달완료, 2: 1.5%도달임박, 3: 앱주문 타점변경)
                if (_currentTabFilter == 1) // 도달완료
                {
                    if (item.AlertType != ToastAlertType.TargetHit && item.AlertType != ToastAlertType.StopHit && item.AlertType != ToastAlertType.BuyHit)
                        continue;
                }
                else if (_currentTabFilter == 2) // 1.5% 도달임박
                {
                    if (item.AlertType != ToastAlertType.TargetNear && item.AlertType != ToastAlertType.StopNear && item.AlertType != ToastAlertType.BuyNear)
                        continue;
                }
                else if (_currentTabFilter == 3) // 앱주문 타점변경
                {
                    if (item.AlertType != ToastAlertType.TargetChanged && item.AlertType != ToastAlertType.StopChanged)
                        continue;
                }

                string typeStr = "목표가 달성";
                Color typeColor = Color.FromArgb(0, 230, 118);

                switch (item.AlertType)
                {
                    case ToastAlertType.TargetHit:
                        typeStr = "목표가 달성 (익절)";
                        typeColor = Color.FromArgb(0, 230, 118);
                        break;
                    case ToastAlertType.StopHit:
                        typeStr = "손절선 터치 (방어)";
                        typeColor = Color.FromArgb(255, 75, 85);
                        break;
                    case ToastAlertType.BuyHit:
                        typeStr = "AI 매수가 도달 (진입)";
                        typeColor = Color.FromArgb(0, 216, 255);
                        break;
                    case ToastAlertType.TargetNear:
                        typeStr = "익절 1.5% 임박";
                        typeColor = Color.FromArgb(255, 193, 7);
                        break;
                    case ToastAlertType.StopNear:
                        typeStr = "손절 1.5% 경고";
                        typeColor = Color.FromArgb(255, 140, 0);
                        break;
                    case ToastAlertType.BuyNear:
                        typeStr = "매수 1.5% 임박";
                        typeColor = Color.FromArgb(0, 216, 255);
                        break;
                    case ToastAlertType.TargetChanged:
                        typeStr = "익절가 변동";
                        typeColor = Color.FromArgb(180, 140, 255);
                        break;
                    case ToastAlertType.StopChanged:
                        typeStr = "손절가 변동";
                        typeColor = Color.FromArgb(255, 110, 160);
                        break;
                }

                string cleanName = (item.StockName ?? string.Empty).Replace("★레이더 ", "").Replace("★", "").Trim();
                string cleanMsg = (item.Message ?? string.Empty).Replace("★레이더 ", "").Replace("★", "").Trim();

                // [사용자 요청 3] AI대응 권고 및 메시지열 내용에서 맨 앞 [종목명] 텍스트 제거
                // 예: "[현대엘리베이터] AI 익절가 변경: ..." -> "AI 익절가 변경: ..."
                //     "[삼성전자 75,000원 매수가 도달]" -> "75,000원 매수가 도달"
                if (!string.IsNullOrEmpty(cleanName))
                {
                    string bracketPrefix = $"[{cleanName}]";
                    if (cleanMsg.StartsWith(bracketPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        cleanMsg = cleanMsg.Substring(bracketPrefix.Length).Trim();
                    }
                    else if (cleanMsg.StartsWith("[" + cleanName, StringComparison.OrdinalIgnoreCase))
                    {
                        int closeIdx = cleanMsg.IndexOf(']');
                        if (closeIdx > 0)
                        {
                            string inner = cleanMsg.Substring(1, closeIdx - 1).Trim();
                            if (inner.StartsWith(cleanName, StringComparison.OrdinalIgnoreCase))
                            {
                                string rest = inner.Substring(cleanName.Length).Trim();
                                string tail = cleanMsg.Substring(closeIdx + 1).Trim();
                                cleanMsg = (rest + " " + tail).Trim();
                            }
                        }
                    }
                }

                bool isRegistered = !string.IsNullOrEmpty(item.StockCode) && _existingCodes.Contains(item.StockCode.TrimStart('0'));
                string regStr = isRegistered ? "등록됨" : "+ 담기";

                ListViewItem lvi;
                if (_currentTabFilter == 3) // 타점변동 탭: [관심 등록] 열 제외
                {
                    lvi = new ListViewItem(new[]
                    {
                        item.AlertTime.ToString("HH:mm:ss"),
                        typeStr,
                        item.StockCode,
                        cleanName,
                        $"{item.CurrentPrice:N0}원",
                        $"{item.TriggerPrice:N0}원",
                        $"{item.ChangeRate:+0.00;-0.00;0.00}%",
                        cleanMsg
                    });
                }
                else
                {
                    lvi = new ListViewItem(new[]
                    {
                        item.AlertTime.ToString("HH:mm:ss"),
                        typeStr,
                        item.StockCode,
                        cleanName,
                        $"{item.CurrentPrice:N0}원",
                        $"{item.TriggerPrice:N0}원",
                        $"{item.ChangeRate:+0.00;-0.00;0.00}%",
                        regStr,
                        cleanMsg
                    });
                }

                lvi.Tag = item;
                lvi.SubItems[0].ForeColor = Color.FromArgb(140, 160, 190);
                lvi.SubItems[1].ForeColor = typeColor;
                lvi.SubItems[1].Font = BoldFont;
                lvi.SubItems[2].ForeColor = Color.FromArgb(150, 170, 200);
                lvi.SubItems[3].ForeColor = Color.FromArgb(235, 245, 255);
                lvi.SubItems[3].Font = BoldFont;

                if (item.ChangeRate > 0) lvi.SubItems[6].ForeColor = Color.FromArgb(255, 95, 95);
                else if (item.ChangeRate < 0) lvi.SubItems[6].ForeColor = Color.FromArgb(85, 160, 255);
                else lvi.SubItems[6].ForeColor = Color.FromArgb(180, 190, 205);

                if (_currentTabFilter != 3)
                {
                    lvi.SubItems[7].ForeColor = isRegistered ? Color.FromArgb(0, 216, 255) : Color.FromArgb(255, 140, 210);
                    lvi.SubItems[7].Font = BoldFont;
                    lvi.SubItems[8].ForeColor = Color.FromArgb(200, 220, 240);
                }
                else
                {
                    lvi.SubItems[7].ForeColor = Color.FromArgb(200, 220, 240);
                }

                lvi.UseItemStyleForSubItems = false;
                _lvHistory.Items.Add(lvi);
            }

            _lvHistory.EndUpdate();

            // 탭 텍스트 동적 개수 업데이트 (이모지 100% 제거)
            if (_btnTabAll != null) _btnTabAll.Text = $"전체 ({snapshot.Count})";
            if (_btnTabHit != null) _btnTabHit.Text = $"도달완료 ({targetCount + stopCount + buyCount})";
            if (_btnTabNear != null) _btnTabNear.Text = $"1.5%임박 ({nearCount})";
            if (_btnTabChanged != null) _btnTabChanged.Text = $"타점변동 ({changedCount})";

            _lblSummary.Text = $"총 {snapshot.Count}건 알림 (도달완료 {targetCount + stopCount + buyCount}건 | 1.5% 도달임박 {nearCount}건 | 앱주문 타점변동 {changedCount}건)";
            _btnClear.Enabled = _lvHistory.Items.Count > 0;

            _lvHistory.SelectedItems.Clear();
            if (_lvHistory.FocusedItem != null) _lvHistory.FocusedItem.Focused = false;
        }
    }
}

