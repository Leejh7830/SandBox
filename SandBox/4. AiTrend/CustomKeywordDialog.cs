using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GateHelper.AiTrend
{
    public class CustomKeywordDialog : Form
    {
        private readonly AiTrendService _service;
        private readonly bool _isDarkMode;

        private Label lblHeader;
        private Button btnShowGuide;
        private Label lblDisplayName;
        private TextBox txtDisplayName;
        private Label lblKeyword;
        private TextBox txtKeyword;
        private Button btnAdd;
        private Label lblTip;

        private Label lblListTitle;
        private ListView lvKeywords;
        private Button btnDelete;
        private Button btnClose;

        public bool HasChanges { get; private set; }
        public CustomKeywordItem LastAddedItem { get; private set; }

        public CustomKeywordDialog(AiTrendService service, bool isDarkMode)
        {
            _service = service ?? new AiTrendService();
            _isDarkMode = isDarkMode;

            InitializeCustomComponents();
            ApplyTheme();
            LoadKeywordList();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "📌 관심 뉴스 키워드 추가 / 관리";
            this.Size = new Size(560, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Font = new Font("맑은 고딕", 9F, FontStyle.Regular);

            // [상단 안내 헤더]
            lblHeader = new Label
            {
                Text = "관심 키워드를 등록하면 실시간 뉴스를 2줄로 요약해 드립니다.",
                Location = new Point(20, 16),
                Size = new Size(390, 20),
                Font = new Font("맑은 고딕", 9.25F, FontStyle.Bold)
            };

            // [검색 가이드 보기 버튼]
            btnShowGuide = new Button
            {
                Text = "📖 검색 가이드",
                Location = new Point(415, 12),
                Size = new Size(110, 26),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 8.5F, FontStyle.Regular)
            };
            btnShowGuide.FlatAppearance.BorderColor = Color.Silver;
            btnShowGuide.Click += (s, e) =>
            {
                using (var guide = new KeywordGuideDialog(_isDarkMode))
                {
                    guide.ShowDialog(this);
                }
            };

            // [표시명 입력]
            lblDisplayName = new Label
            {
                Text = "카테고리 이름 (화면 표시용):",
                Location = new Point(20, 50),
                Size = new Size(200, 18)
            };

            txtDisplayName = new TextBox
            {
                Location = new Point(20, 72),
                Size = new Size(220, 25),
                Font = new Font("맑은 고딕", 9.5F)
            };
            txtDisplayName.TextChanged += (s, e) =>
            {
                // 사용자가 키워드를 아직 직접 안 건드렸다면 자동으로 함께 채워줌
                if (string.IsNullOrWhiteSpace(txtKeyword.Text) || txtKeyword.Tag as string == "auto")
                {
                    txtKeyword.Tag = "auto";
                    txtKeyword.Text = txtDisplayName.Text;
                }
            };
            txtDisplayName.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    txtKeyword.Focus();
                    txtKeyword.SelectAll();
                }
            };

            // [검색 키워드 입력]
            lblKeyword = new Label
            {
                Text = "검색 키워드 (조합 가능):",
                Location = new Point(255, 50),
                Size = new Size(180, 18)
            };

            txtKeyword = new TextBox
            {
                Location = new Point(255, 72),
                Size = new Size(265, 25),
                Font = new Font("맑은 고딕", 9.5F)
            };
            txtKeyword.KeyDown += (s, e) =>
            {
                txtKeyword.Tag = "manual";
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    btnAdd_Click(this, EventArgs.Empty);
                }
            };

            // [추가 버튼]
            btnAdd = new Button
            {
                Text = "➕ 키워드 등록",
                Location = new Point(410, 105),
                Size = new Size(110, 30),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += btnAdd_Click;

            // [친절한 팁 안내]
            lblTip = new Label
            {
                Text = "💡 추천: '전기차 배터리'(핵심), '금리+대출'(조합), '삼성전자, SK하이닉스'(묶음)",
                Location = new Point(20, 108),
                Size = new Size(385, 32),
                Font = new Font("맑은 고딕", 8F, FontStyle.Regular)
            };

            // [구분선]
            var pnlDivider = new Panel
            {
                Location = new Point(20, 148),
                Size = new Size(505, 1),
                BackColor = Color.FromArgb(220, 220, 220)
            };

            // [등록된 키워드 목록 타이틀]
            lblListTitle = new Label
            {
                Text = "현재 등록된 관심 키워드 목록:",
                Location = new Point(20, 160),
                Size = new Size(300, 18),
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
            };

            // [키워드 ListView]
            lvKeywords = new ListView
            {
                Location = new Point(20, 182),
                Size = new Size(505, 240),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HideSelection = false,
                Font = new Font("맑은 고딕", 9F)
            };
            lvKeywords.Columns.Add("카테고리명", 160);
            lvKeywords.Columns.Add("검색 키워드", 200);
            lvKeywords.Columns.Add("등록 일시", 135);

            // [하단 액션 버튼]
            btnDelete = new Button
            {
                Text = "🗑️ 선택 삭제",
                Location = new Point(20, 432),
                Size = new Size(100, 32),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += btnDelete_Click;

            btnClose = new Button
            {
                Text = "닫기",
                Location = new Point(425, 432),
                Size = new Size(100, 32),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            // 컨트롤 폼에 배치
            this.Controls.Add(lblHeader);
            this.Controls.Add(btnShowGuide);
            this.Controls.Add(lblDisplayName);
            this.Controls.Add(txtDisplayName);
            this.Controls.Add(lblKeyword);
            this.Controls.Add(txtKeyword);
            this.Controls.Add(lblTip);
            this.Controls.Add(btnAdd);
            this.Controls.Add(pnlDivider);
            this.Controls.Add(lblListTitle);
            this.Controls.Add(lvKeywords);
            this.Controls.Add(btnDelete);
            this.Controls.Add(btnClose);
        }

        private void ApplyTheme()
        {
            if (_isDarkMode)
            {
                this.BackColor = Color.FromArgb(38, 40, 45);
                this.ForeColor = Color.White;

                lblHeader.ForeColor = Color.FromArgb(145, 200, 255);
                btnShowGuide.BackColor = Color.FromArgb(50, 52, 58);
                btnShowGuide.ForeColor = Color.White;
                btnShowGuide.FlatAppearance.BorderColor = Color.FromArgb(70, 72, 80);

                lblDisplayName.ForeColor = Color.Gainsboro;
                lblKeyword.ForeColor = Color.Gainsboro;
                lblTip.ForeColor = Color.FromArgb(160, 190, 220);
                lblListTitle.ForeColor = Color.White;

                txtDisplayName.BackColor = Color.FromArgb(48, 50, 56);
                txtDisplayName.ForeColor = Color.White;
                txtDisplayName.BorderStyle = BorderStyle.FixedSingle;

                txtKeyword.BackColor = Color.FromArgb(48, 50, 56);
                txtKeyword.ForeColor = Color.White;
                txtKeyword.BorderStyle = BorderStyle.FixedSingle;

                btnAdd.BackColor = Color.FromArgb(41, 128, 185);
                btnAdd.ForeColor = Color.White;

                lvKeywords.BackColor = Color.FromArgb(30, 32, 36);
                lvKeywords.ForeColor = Color.White;

                btnDelete.BackColor = Color.FromArgb(80, 40, 45);
                btnDelete.ForeColor = Color.FromArgb(255, 140, 140);

                btnClose.BackColor = Color.FromArgb(60, 62, 68);
                btnClose.ForeColor = Color.White;
            }
            else
            {
                this.BackColor = Color.FromArgb(248, 250, 252);
                this.ForeColor = Color.FromArgb(33, 33, 33);

                lblHeader.ForeColor = Color.FromArgb(21, 101, 192);
                btnShowGuide.BackColor = Color.FromArgb(240, 242, 245);
                btnShowGuide.ForeColor = Color.FromArgb(33, 33, 33);
                btnShowGuide.FlatAppearance.BorderColor = Color.Silver;

                lblDisplayName.ForeColor = Color.FromArgb(60, 60, 60);
                lblKeyword.ForeColor = Color.FromArgb(60, 60, 60);
                lblTip.ForeColor = Color.FromArgb(100, 110, 120);
                lblListTitle.ForeColor = Color.FromArgb(33, 33, 33);

                txtDisplayName.BackColor = Color.White;
                txtDisplayName.ForeColor = Color.Black;

                txtKeyword.BackColor = Color.White;
                txtKeyword.ForeColor = Color.Black;

                btnAdd.BackColor = Color.FromArgb(33, 150, 243);
                btnAdd.ForeColor = Color.White;

                lvKeywords.BackColor = Color.White;
                lvKeywords.ForeColor = Color.Black;

                btnDelete.BackColor = Color.FromArgb(255, 235, 238);
                btnDelete.ForeColor = Color.FromArgb(198, 40, 40);

                btnClose.BackColor = Color.FromArgb(230, 233, 238);
                btnClose.ForeColor = Color.FromArgb(33, 33, 33);
            }
        }

        private void LoadKeywordList()
        {
            lvKeywords.Items.Clear();
            var list = _service.LoadCustomKeywords();

            foreach (var item in list)
            {
                var lvi = new ListViewItem(item.DisplayName);
                lvi.SubItems.Add(item.RawKeyword);
                lvi.SubItems.Add(item.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
                lvi.Tag = item;
                lvKeywords.Items.Add(lvi);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string name = txtDisplayName.Text.Trim();
            string keyword = txtKeyword.Text.Trim();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                MessageBox.Show(this, "검색 키워드를 입력해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtKeyword.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = keyword;
            }

            var item = _service.AddCustomKeyword(name, keyword);
            if (item != null)
            {
                HasChanges = true;
                LastAddedItem = item;

                txtDisplayName.Clear();
                txtKeyword.Clear();
                txtKeyword.Tag = null;

                LoadKeywordList();
                txtDisplayName.Focus();
            }
            else
            {
                MessageBox.Show(this, "키워드 등록에 실패했습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (lvKeywords.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "삭제할 관심 키워드를 목록에서 선택해 주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedLvi = lvKeywords.SelectedItems[0];
            if (selectedLvi.Tag is CustomKeywordItem item)
            {
                var dr = MessageBox.Show(this,
                    $"'{item.DisplayName}' 키워드를 정말 삭제하시겠습니까?",
                    "관심 키워드 삭제 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    if (_service.DeleteCustomKeyword(item.Id))
                    {
                        HasChanges = true;
                        LoadKeywordList();
                    }
                }
            }
        }
    }
}
