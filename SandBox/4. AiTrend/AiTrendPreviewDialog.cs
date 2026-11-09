using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace GateHelper.AiTrend
{
    /// <summary>
    /// 무거운 브라우저 임베딩 없이 0.01초 만에 기사/프로젝트 핵심을 시원하게 읽고 넘겨보는 초경량 카드뉴스 퀵 뷰어
    /// </summary>
    public class AiTrendPreviewDialog : Form
    {
        private readonly List<AiRepoItem> _items;
        private readonly AiTrendService _service;
        private readonly bool _isDarkMode;
        private int _currentIndex;

        private Label lblCategoryBadge;
        private Label lblIndexIndicator;
        private Label lblTitle;
        private Label lblMetaInfo;

        private Panel pnlAiCard;
        private Label lblAiBadge;
        private Label lblFactBriefing;
        private Label lblTipBriefing;

        private Panel pnlDescCard;
        private Label lblDescTitle;
        private TextBox txtDescription;

        private Button btnPrev;
        private Button btnNext;
        private Button btnFavorite;
        private Button btnCopy;
        private Button btnOpenOriginal;
        private Button btnClose;

        public int CurrentIndex => _currentIndex;
        public bool HasChanges { get; private set; }

        public AiTrendPreviewDialog(List<AiRepoItem> items, int startIndex, AiTrendService service, bool isDarkMode)
        {
            _items = items ?? new List<AiRepoItem>();
            _currentIndex = Math.Max(0, Math.Min(startIndex, _items.Count - 1));
            _service = service;
            _isDarkMode = isDarkMode;

            InitializeCustomComponents();
            ApplyTheme();
            DisplayCurrentItem();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "📰 데일리 브리핑 - 카드뉴스 퀵 뷰어";
            this.Size = new Size(680, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;
            this.Font = new Font("맑은 고딕", 9F, FontStyle.Regular);

            // [상단 헤더 패널]
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                Padding = new Padding(18, 12, 18, 6)
            };

            lblCategoryBadge = new Label
            {
                Text = "📰 실시간 브리핑",
                Font = new Font("맑은 고딕", 8F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(18, 10)
            };

            lblIndexIndicator = new Label
            {
                Text = "1 / 15",
                Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(600, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            lblTitle = new Label
            {
                Text = "기사 제목 또는 프로젝트 이름",
                Font = new Font("맑은 고딕", 12F, FontStyle.Bold),
                Location = new Point(18, 30),
                Size = new Size(630, 28),
                AutoEllipsis = true
            };

            lblMetaInfo = new Label
            {
                Text = "언론사: - | 발행: -",
                Font = new Font("맑은 고딕", 8.5F, FontStyle.Regular),
                Location = new Point(18, 60),
                Size = new Size(630, 18),
                ForeColor = Color.Gray
            };

            pnlTop.Controls.Add(lblCategoryBadge);
            pnlTop.Controls.Add(lblIndexIndicator);
            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(lblMetaInfo);

            // [중앙 메인 컨텐츠 영역]
            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 4, 18, 8)
            };

            // AI 2줄 요약 카드
            pnlAiCard = new Panel
            {
                Location = new Point(18, 8),
                Size = new Size(630, 160),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(14, 10, 14, 10)
            };

            lblAiBadge = new Label
            {
                Text = "💡 Gemini AI 심층 2줄 브리핑",
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold),
                Location = new Point(14, 10),
                Size = new Size(300, 20)
            };

            lblFactBriefing = new Label
            {
                Text = "📌 [핵심 팩트] 요약 생성 중...",
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Regular),
                Location = new Point(14, 38),
                Size = new Size(602, 52),
                AutoEllipsis = true
            };

            lblTipBriefing = new Label
            {
                Text = "💡 [핵심 시사점] 분석 생성 중...",
                Font = new Font("맑은 고딕", 9.25F, FontStyle.Regular),
                Location = new Point(14, 98),
                Size = new Size(602, 52),
                AutoEllipsis = true
            };

            pnlAiCard.Controls.Add(lblAiBadge);
            pnlAiCard.Controls.Add(lblFactBriefing);
            pnlAiCard.Controls.Add(lblTipBriefing);

            // 본문 요약 카드
            pnlDescCard = new Panel
            {
                Location = new Point(18, 178),
                Size = new Size(630, 225),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(14, 10, 14, 10)
            };

            lblDescTitle = new Label
            {
                Text = "📄 기사 본문 요약 / 원문 정보",
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                Location = new Point(14, 8),
                Size = new Size(300, 18)
            };

            txtDescription = new TextBox
            {
                Location = new Point(14, 32),
                Size = new Size(602, 180),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                TabStop = false
            };
            txtDescription.Enter += (s, e) =>
            {
                txtDescription.Select(0, 0);
                txtDescription.SelectionLength = 0;
            };
            txtDescription.MouseUp += (s, e) =>
            {
                if (txtDescription.SelectionLength == txtDescription.Text.Length && txtDescription.SelectionLength > 0)
                {
                    txtDescription.Select(0, 0);
                    txtDescription.SelectionLength = 0;
                }
            };

            pnlDescCard.Controls.Add(lblDescTitle);
            pnlDescCard.Controls.Add(txtDescription);

            pnlMain.Controls.Add(pnlAiCard);
            pnlMain.Controls.Add(pnlDescCard);

            // [하단 내비게이션 & 액션 툴바]
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(18, 10, 18, 12)
            };

            btnPrev = new Button
            {
                Text = "◀ 이전 (←)",
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                Size = new Size(95, 36),
                Location = new Point(18, 12),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPrev.Click += (s, e) => Navigate(-1);

            btnNext = new Button
            {
                Text = "다음 (→) ▶",
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                Size = new Size(95, 36),
                Location = new Point(120, 12),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnNext.Click += (s, e) => Navigate(1);

            btnFavorite = new Button
            {
                Text = "⭐ 스크랩",
                Font = new Font("맑은 고딕", 8.5F, FontStyle.Regular),
                Size = new Size(82, 36),
                Location = new Point(285, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnFavorite.Click += btnFavorite_Click;

            btnCopy = new Button
            {
                Text = "📋 복사",
                Font = new Font("맑은 고딕", 8.5F, FontStyle.Regular),
                Size = new Size(70, 36),
                Location = new Point(373, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCopy.Click += btnCopy_Click;

            btnOpenOriginal = new Button
            {
                Text = "🔗 원문 열기 ↗",
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                Size = new Size(115, 36),
                Location = new Point(449, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOpenOriginal.Click += btnOpenOriginal_Click;

            btnClose = new Button
            {
                Text = "닫기 (ESC)",
                Font = new Font("맑은 고딕", 8.5F, FontStyle.Regular),
                Size = new Size(78, 36),
                Location = new Point(570, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };

            pnlBottom.Controls.Add(btnPrev);
            pnlBottom.Controls.Add(btnNext);
            pnlBottom.Controls.Add(btnFavorite);
            pnlBottom.Controls.Add(btnCopy);
            pnlBottom.Controls.Add(btnOpenOriginal);
            pnlBottom.Controls.Add(btnClose);

            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);

            this.CancelButton = btnClose;

            // 열릴 때 TextBox 전체선택 방지 및 내비게이션 버튼에 기본 포커스 설정
            this.Shown += (s, e) =>
            {
                btnNext.Focus();
                txtDescription.Select(0, 0);
                txtDescription.SelectionLength = 0;
            };
            this.ActiveControl = btnNext;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.A)
            {
                Navigate(-1);
                return true;
            }
            if (keyData == Keys.Right || keyData == Keys.D)
            {
                Navigate(1);
                return true;
            }
            if (keyData == Keys.Enter || keyData == Keys.O)
            {
                btnOpenOriginal_Click(this, EventArgs.Empty);
                return true;
            }
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Navigate(int offset)
        {
            int newIdx = _currentIndex + offset;
            if (newIdx >= 0 && newIdx < _items.Count)
            {
                _currentIndex = newIdx;
                DisplayCurrentItem();
            }
        }

        private void DisplayCurrentItem()
        {
            if (_items == null || _items.Count == 0) return;

            var item = _items[_currentIndex];
            bool isNews = item.ItemType == TrendItemType.NewsArticle;

            lblIndexIndicator.Text = $"#{item.Rank} / {_items.Count}";
            lblTitle.Text = item.FullName;

            if (isNews)
            {
                lblCategoryBadge.Text = "📰 실시간 핵심 뉴스";
                string flame = item.IsRecentNews ? "🔥 " : "";
                lblMetaInfo.Text = $"언론사: {item.Owner}  |  {flame}발행: {item.TimeFormatted} ({item.PushedAt:yyyy-MM-dd HH:mm})";
                lblAiBadge.Text = "💡 Gemini AI 2줄 팩트 & 시사점 브리핑";
                lblFactBriefing.Text = string.IsNullOrWhiteSpace(item.AiSummary)
                    ? "📌 [핵심 팩트] 생성 중이거나 대기 중입니다."
                    : $"📌 [핵심 팩트] {item.AiSummary}";
                lblTipBriefing.Text = string.IsNullOrWhiteSpace(item.PracticalTip)
                    ? "💡 [핵심 시사점] 분석 대기 중입니다."
                    : $"💡 [핵심 시사점] {item.PracticalTip}";

                if (!string.IsNullOrWhiteSpace(item.DetailStory))
                {
                    lblDescTitle.Text = "📖 [AI 심층 스토리 브리핑]";
                    txtDescription.Text = item.DetailStory;
                }
                else
                {
                    lblDescTitle.Text = "📄 [기사 정보]";
                    bool isDuplicate = string.Equals(item.FullName?.Trim(), item.Description?.Trim(), StringComparison.OrdinalIgnoreCase);
                    txtDescription.Text = isDuplicate
                        ? "⏳ AI가 기사의 구체적 배경과 진행 맥락을 담은 [심층 스토리 브리핑]을 작성하고 있습니다... (완성 시 자동 반영)"
                        : item.Description;
                }

                btnFavorite.Text = item.IsFavorite ? "★ 찜 해제" : "⭐ 찜하기";
                btnOpenOriginal.Text = "🔗 기사 원문 ↗";
            }
            else
            {
                lblCategoryBadge.Text = "🌐 실시간 급상승 AI 오픈소스";
                lblMetaInfo.Text = $"⭐ Stars: {item.StarsFormatted}  |  🍴 Forks: {item.Forks:N0}  |  💻 언어: {item.Language}  |  ⏰ 최근 커밋: {item.PushedAtFormatted}";
                lblAiBadge.Text = "💡 Gemini AI 2줄 핵심 역할 & 실무 팁";
                lblFactBriefing.Text = string.IsNullOrWhiteSpace(item.AiSummary)
                    ? "📌 [핵심 요약] 생성 중이거나 대기 중입니다."
                    : $"📌 [핵심 요약] {item.AiSummary}";
                lblTipBriefing.Text = string.IsNullOrWhiteSpace(item.PracticalTip)
                    ? "🚀 [실무 활용] 분석 대기 중입니다."
                    : $"🚀 [실무 활용] {item.PracticalTip}";

                if (!string.IsNullOrWhiteSpace(item.DetailStory))
                {
                    lblDescTitle.Text = "📖 [AI 심층 기술 분석 및 배경]";
                    txtDescription.Text = item.DetailStory + (!string.IsNullOrWhiteSpace(item.Description) ? $"\r\n\r\n[원문 설명]\r\n{item.Description}" : "");
                }
                else
                {
                    lblDescTitle.Text = "📄 [프로젝트 원문 설명 전문]";
                    txtDescription.Text = item.Description;
                }

                btnFavorite.Text = item.IsFavorite ? "★ 찜 해제" : "⭐ 찜하기";
                btnOpenOriginal.Text = "🌐 프로젝트 저장소 열기 ↗";
            }

            // 즐겨찾기 버튼 색상
            if (item.IsFavorite)
            {
                btnFavorite.BackColor = _isDarkMode ? Color.FromArgb(70, 60, 20) : Color.FromArgb(255, 243, 205);
                btnFavorite.ForeColor = _isDarkMode ? Color.FromArgb(255, 220, 50) : Color.FromArgb(133, 100, 4);
            }
            else
            {
                btnFavorite.BackColor = _isDarkMode ? Color.FromArgb(50, 52, 58) : Color.FromArgb(240, 242, 245);
                btnFavorite.ForeColor = _isDarkMode ? Color.White : Color.FromArgb(33, 33, 33);
            }

            btnPrev.Enabled = _currentIndex > 0;
            btnNext.Enabled = _currentIndex < _items.Count - 1;

            // 텍스트 선택(파란색 하이라이트) 원천 방지
            txtDescription.Select(0, 0);
            txtDescription.SelectionLength = 0;
        }

        private void btnFavorite_Click(object sender, EventArgs e)
        {
            if (_items == null || _items.Count == 0) return;
            var item = _items[_currentIndex];
            if (_service != null)
            {
                _service.ToggleFavorite(item);
                HasChanges = true;
                DisplayCurrentItem();
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (_items == null || _items.Count == 0) return;
            var item = _items[_currentIndex];
            if (!string.IsNullOrWhiteSpace(item.HtmlUrl))
            {
                Clipboard.SetText(item.HtmlUrl);
                MessageBox.Show(this, "링크가 클립보드에 복사되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnOpenOriginal_Click(object sender, EventArgs e)
        {
            if (_items == null || _items.Count == 0) return;
            var item = _items[_currentIndex];
            if (!string.IsNullOrWhiteSpace(item.HtmlUrl))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(item.HtmlUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "브라우저를 열 수 없습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ApplyTheme()
        {
            if (_isDarkMode)
            {
                this.BackColor = Color.FromArgb(28, 30, 34);
                this.ForeColor = Color.FromArgb(240, 242, 245);

                lblCategoryBadge.ForeColor = Color.FromArgb(145, 200, 255);
                lblIndexIndicator.ForeColor = Color.FromArgb(180, 185, 195);
                lblTitle.ForeColor = Color.White;
                lblMetaInfo.ForeColor = Color.FromArgb(160, 165, 175);

                // AI 카드
                pnlAiCard.BackColor = Color.FromArgb(38, 42, 50);
                lblAiBadge.ForeColor = Color.FromArgb(145, 200, 255);
                lblFactBriefing.ForeColor = Color.FromArgb(235, 240, 245);
                lblTipBriefing.ForeColor = Color.FromArgb(170, 215, 255);

                // 본문 카드
                pnlDescCard.BackColor = Color.FromArgb(34, 36, 42);
                lblDescTitle.ForeColor = Color.FromArgb(200, 205, 215);
                txtDescription.BackColor = Color.FromArgb(34, 36, 42);
                txtDescription.ForeColor = Color.FromArgb(215, 220, 230);

                // 버튼
                btnPrev.BackColor = Color.FromArgb(48, 52, 60);
                btnPrev.ForeColor = Color.White;
                btnPrev.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 85);

                btnNext.BackColor = Color.FromArgb(48, 52, 60);
                btnNext.ForeColor = Color.White;
                btnNext.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 85);

                btnCopy.BackColor = Color.FromArgb(48, 52, 60);
                btnCopy.ForeColor = Color.White;
                btnCopy.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 85);

                btnOpenOriginal.BackColor = Color.FromArgb(25, 118, 210);
                btnOpenOriginal.ForeColor = Color.White;
                btnOpenOriginal.FlatAppearance.BorderSize = 0;

                btnClose.BackColor = Color.FromArgb(48, 52, 60);
                btnClose.ForeColor = Color.White;
                btnClose.FlatAppearance.BorderColor = Color.FromArgb(70, 75, 85);
            }
            else
            {
                this.BackColor = Color.FromArgb(248, 250, 252);
                this.ForeColor = Color.FromArgb(33, 33, 33);

                lblCategoryBadge.ForeColor = Color.FromArgb(21, 101, 192);
                lblIndexIndicator.ForeColor = Color.Gray;
                lblTitle.ForeColor = Color.FromArgb(20, 20, 20);
                lblMetaInfo.ForeColor = Color.FromArgb(90, 90, 90);

                // AI 카드
                pnlAiCard.BackColor = Color.FromArgb(238, 245, 255);
                lblAiBadge.ForeColor = Color.FromArgb(13, 71, 161);
                lblFactBriefing.ForeColor = Color.FromArgb(30, 30, 30);
                lblTipBriefing.ForeColor = Color.FromArgb(21, 101, 192);

                // 본문 카드
                pnlDescCard.BackColor = Color.White;
                lblDescTitle.ForeColor = Color.FromArgb(50, 50, 50);
                txtDescription.BackColor = Color.White;
                txtDescription.ForeColor = Color.FromArgb(60, 60, 60);

                // 버튼
                btnPrev.BackColor = Color.FromArgb(240, 242, 245);
                btnPrev.ForeColor = Color.FromArgb(33, 33, 33);
                btnPrev.FlatAppearance.BorderColor = Color.Silver;

                btnNext.BackColor = Color.FromArgb(240, 242, 245);
                btnNext.ForeColor = Color.FromArgb(33, 33, 33);
                btnNext.FlatAppearance.BorderColor = Color.Silver;

                btnCopy.BackColor = Color.FromArgb(240, 242, 245);
                btnCopy.ForeColor = Color.FromArgb(33, 33, 33);
                btnCopy.FlatAppearance.BorderColor = Color.Silver;

                btnOpenOriginal.BackColor = Color.FromArgb(21, 101, 192);
                btnOpenOriginal.ForeColor = Color.White;
                btnOpenOriginal.FlatAppearance.BorderSize = 0;

                btnClose.BackColor = Color.FromArgb(235, 237, 240);
                btnClose.ForeColor = Color.FromArgb(33, 33, 33);
                btnClose.FlatAppearance.BorderColor = Color.Silver;
            }
        }
    }
}
