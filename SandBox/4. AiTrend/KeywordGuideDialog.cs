using System;
using System.Drawing;
using System.Windows.Forms;

namespace GateHelper.AiTrend
{
    /// <summary>
    /// 사용자 맞춤 관심 키워드 검색 쿼리 변환 규칙 및 삭제/관리 가이드 모달 다이얼로그
    /// </summary>
    public class KeywordGuideDialog : Form
    {
        private readonly bool _isDarkMode;

        public KeywordGuideDialog(bool isDarkMode = false)
        {
            _isDarkMode = isDarkMode;
            InitializeComponent();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            this.Text = "📌 관심 키워드 검색 & 관리 가이드";
            this.Size = new Size(540, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.Font = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point);

            // 헤더 패널
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(16, 12, 16, 8)
            };

            var lblTitle = new Label
            {
                Text = "💡 맞춤 관심 키워드 검색 가이드",
                Font = new Font("맑은 고딕", 11.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(14, 10)
            };

            var lblSubtitle = new Label
            {
                Text = "원하는 소식을 가장 정확하게 수집할 수 있는 검색어 작성 팁과 관리 방법입니다.",
                Font = new Font("맑은 고딕", 8.5F, FontStyle.Regular),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(16, 34)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // 본문 내용 패널
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 8, 16, 8),
                AutoScroll = true
            };

            // 그룹 1: 스마트 쿼리 작성법
            var grpQuery = new GroupBox
            {
                Text = "🔍 스마트 검색어(쿼리) 작성 규칙",
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold),
                Location = new Point(16, 6),
                Size = new Size(492, 220),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var txtQueryGuide = new Label
            {
                Text = "1. 자연스러운 핵심 검색어 (가장 추천 ⭐)\r\n" +
                       "   • 입력: [ 전기차 배터리 ] (또는 [ OTT 신작 ], [ 애플 아이폰 ])\r\n" +
                       "   • 효과: 구글 뉴스가 해당 주제와 가장 밀접한 알짜배기 최신 기사를 수집합니다.\r\n\r\n" +
                       "2. 다중 관심사 묶음 검색 (, 쉼표)\r\n" +
                       "   • 입력: [ 넷플릭스, 디즈니플러스, 티빙 ] (또는 [ 삼성전자, SK하이닉스 ])\r\n" +
                       "   • 효과: 관심 있는 브랜드나 기업들의 최신 소식을 한 번에 모아서 브리핑합니다.\r\n\r\n" +
                       "3. 필수 단어 조합 (+ 기호)\r\n" +
                       "   • 입력: [ 금리+대출 ] ➔ 두 단어가 기사 본문에 반드시 함께 들어간 기사만 수집\r\n\r\n" +
                       "💡 [지역 안내] 기본 검색이 '국내(한국)' 기준이므로, 해외 소식을 원할 때만 [해외 축구], [미국 금리]처럼 특정 지역을 명시하시면 가장 깔끔합니다.",
                Font = new Font("맑은 고딕", 8.75F, FontStyle.Regular),
                Location = new Point(14, 25),
                Size = new Size(464, 185)
            };
            grpQuery.Controls.Add(txtQueryGuide);

            // 그룹 2: 키워드 삭제 및 관리 방법
            var grpManage = new GroupBox
            {
                Text = "🗑️ 관심 키워드 삭제 및 관리 방법",
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold),
                Location = new Point(16, 235),
                Size = new Size(492, 130),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var txtManageGuide = new Label
            {
                Text = "1. 등록된 키워드 삭제:\r\n" +
                       "   • 키워드 관리 창의 하단 '등록된 관심 키워드 목록'에서 삭제할 항목을 선택합니다.\r\n" +
                       "   • 우측의 [🗑️ 선택 키워드 삭제] 버튼을 누르면 즉시 안전하게 제거됩니다.\r\n\r\n" +
                       "2. 드롭다운 메뉴 자동 연동:\r\n" +
                       "   • 등록 즉시 상단 카테고리에 '📌 [관심] {카테고리명}'으로 자동 추가되며,\r\n" +
                       "     삭제 시 드롭다운 메뉴에서도 자동으로 깔끔하게 정리됩니다.",
                Font = new Font("맑은 고딕", 8.75F, FontStyle.Regular),
                Location = new Point(14, 24),
                Size = new Size(464, 98)
            };
            grpManage.Controls.Add(txtManageGuide);

            pnlContent.Controls.Add(grpQuery);
            pnlContent.Controls.Add(grpManage);

            // 하단 버튼 패널
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                Padding = new Padding(16, 8, 16, 10)
            };

            var btnProceed = new Button
            {
                Text = "관심 키워드 등록/관리 바로가기 ▶",
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                Size = new Size(230, 32),
                Location = new Point(16, 10),
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat
            };

            var btnClose = new Button
            {
                Text = "닫기",
                Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                Size = new Size(80, 32),
                Location = new Point(428, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat
            };

            pnlBottom.Controls.Add(btnProceed);
            pnlBottom.Controls.Add(btnClose);

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlBottom);

            this.AcceptButton = btnProceed;
            this.CancelButton = btnClose;
        }

        private void ApplyTheme()
        {
            if (_isDarkMode)
            {
                this.BackColor = Color.FromArgb(32, 34, 38);
                this.ForeColor = Color.FromArgb(235, 240, 245);

                foreach (Control c in this.Controls)
                {
                    ApplyThemeRecursive(c, true);
                }
            }
            else
            {
                this.BackColor = Color.FromArgb(248, 249, 250);
                this.ForeColor = Color.FromArgb(33, 33, 33);

                foreach (Control c in this.Controls)
                {
                    ApplyThemeRecursive(c, false);
                }
            }
        }

        private void ApplyThemeRecursive(Control parent, bool isDark)
        {
            if (parent is GroupBox grp)
            {
                grp.ForeColor = isDark ? Color.FromArgb(145, 200, 255) : Color.FromArgb(13, 71, 161);
                foreach (Control child in grp.Controls)
                {
                    if (child is Label lbl)
                    {
                        lbl.ForeColor = isDark ? Color.FromArgb(220, 225, 230) : Color.FromArgb(50, 50, 50);
                    }
                }
            }
            else if (parent is Button btn)
            {
                if (btn.DialogResult == DialogResult.OK)
                {
                    btn.BackColor = isDark ? Color.FromArgb(25, 118, 210) : Color.FromArgb(13, 71, 161);
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.BorderSize = 0;
                }
                else
                {
                    btn.BackColor = isDark ? Color.FromArgb(50, 52, 58) : Color.FromArgb(230, 232, 235);
                    btn.ForeColor = isDark ? Color.White : Color.Black;
                    btn.FlatAppearance.BorderColor = isDark ? Color.FromArgb(70, 72, 80) : Color.Silver;
                }
            }

            foreach (Control sub in parent.Controls)
            {
                ApplyThemeRecursive(sub, isDark);
            }
        }
    }
}
