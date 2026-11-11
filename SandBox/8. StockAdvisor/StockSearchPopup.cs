using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 종목 검색 결과가 여러 개일 때 검색창 바로 아래에 세련되게 펼쳐지는 다크 테마 드롭다운 후보 리스트
    /// (마우스 1회 클릭 또는 방향키 + Enter로 스내피하게 선택)
    /// </summary>
    public class StockSearchPopup : ToolStripDropDown
    {
        private readonly ListView _listView;
        private readonly Label _lblGuide;
        private readonly Panel _container;
        private readonly ToolStripControlHost _host;
        private Control _anchorControl;

        public event Action<StockSearchResultItem> StockSelected;

        public StockSearchPopup()
        {
            AutoClose = true;
            DropShadowEnabled = true;
            Margin = Padding.Empty;
            Padding = Padding.Empty;
            BackColor = Color.FromArgb(20, 26, 38);

            _container = new Panel
            {
                BackColor = Color.FromArgb(16, 20, 30),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(1)
            };

            _listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.White,
                Font = new Font("Malgun Gothic", 9.2f),
                BorderStyle = BorderStyle.None,
                MultiSelect = false
            };

            _listView.Columns.Add("코드", 70, HorizontalAlignment.Center);
            _listView.Columns.Add("종목명", 180, HorizontalAlignment.Left);
            _listView.Columns.Add("시장구분", 85, HorizontalAlignment.Center);

            // 마우스 클릭 또는 더블클릭 시 즉시 선택
            _listView.MouseClick += (s, e) =>
            {
                var hit = _listView.HitTest(e.Location);
                if (hit.Item != null)
                {
                    ConfirmSelection();
                }
            };
            _listView.DoubleClick += (s, e) => ConfirmSelection();

            // 키보드 엔터 / ESC / 방향키 처리
            _listView.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ConfirmSelection();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    Close();
                    _anchorControl?.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Up && _listView.SelectedIndices.Count > 0 && _listView.SelectedIndices[0] == 0)
                {
                    _anchorControl?.Focus();
                    e.Handled = true;
                }
            };

            _lblGuide = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                BackColor = Color.FromArgb(20, 24, 34),
                ForeColor = Color.FromArgb(135, 155, 185),
                Font = new Font("Malgun Gothic", 8.0f),
                Text = " 💡 마우스 클릭 또는 방향키+Enter로 선택  (ESC: 닫기)",
                TextAlign = ContentAlignment.MiddleLeft
            };

            _container.Controls.Add(_listView);
            _container.Controls.Add(_lblGuide);

            _host = new ToolStripControlHost(_container)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false
            };

            Items.Add(_host);
        }

        public void FocusList()
        {
            if (_listView.IsHandleCreated && _listView.Visible)
            {
                _listView.Focus();
                if (_listView.Items.Count > 0 && _listView.SelectedItems.Count == 0)
                {
                    _listView.Items[0].Selected = true;
                    _listView.Items[0].Focused = true;
                }
            }
        }

        public void ShowResults(Control anchor, List<StockSearchResultItem> items)
        {
            if (anchor == null || items == null || items.Count == 0) return;
            _anchorControl = anchor;

            _listView.BeginUpdate();
            _listView.Items.Clear();

            var boldFont = new Font(_listView.Font, FontStyle.Bold);

            foreach (var itm in items)
            {
                string market = string.IsNullOrEmpty(itm.TypeName)
                    ? (itm.IsForeign ? "해외" : "국내")
                    : itm.TypeName;

                var lvi = new ListViewItem(new[]
                {
                    itm.Code,
                    itm.Name,
                    market
                })
                {
                    Tag = itm
                };

                lvi.SubItems[0].ForeColor = Color.FromArgb(140, 165, 195);
                lvi.SubItems[1].ForeColor = Color.FromArgb(240, 245, 255);
                lvi.SubItems[1].Font = boldFont;

                if (market.Contains("KOSPI")) lvi.SubItems[2].ForeColor = Color.FromArgb(0, 220, 245);
                else if (market.Contains("KOSDAQ")) lvi.SubItems[2].ForeColor = Color.FromArgb(255, 200, 80);
                else if (market.Contains("ETF")) lvi.SubItems[2].ForeColor = Color.FromArgb(140, 230, 120);
                else lvi.SubItems[2].ForeColor = Color.FromArgb(200, 170, 255);

                _listView.Items.Add(lvi);
            }

            if (_listView.Items.Count > 0)
            {
                _listView.Items[0].Selected = true;
                _listView.Items[0].Focused = true;
            }

            _listView.EndUpdate();

            // 목록 개수에 따라 높이 자동 조절 (최소 130px, 최대 260px)
            int itemHeight = 22;
            int totalListHeight = Math.Min(260, Math.Max(130, items.Count * itemHeight + 52));
            int totalWidth = Math.Max(anchor.Width, 350);

            _container.Size = new Size(totalWidth, totalListHeight);
            _host.Size = _container.Size;
            this.Size = _container.Size;

            // 앵커 컨트롤 바로 아래에 드롭다운 표시
            Show(anchor, new Point(0, anchor.Height + 2), ToolStripDropDownDirection.BelowRight);
            _listView.Focus();
        }

        private void ConfirmSelection()
        {
            if (_listView.SelectedItems.Count > 0)
            {
                var item = _listView.SelectedItems[0].Tag as StockSearchResultItem;
                if (item != null)
                {
                    Close();
                    StockSelected?.Invoke(item);
                }
            }
        }
    }
}

