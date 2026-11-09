namespace GateHelper.AiTrend
{
    partial class AiTrendControl
    {
        /// <summary> 
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 구성 요소 디자이너에서 생성한 코드

        /// <summary> 
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnBack = new System.Windows.Forms.Button();
            this.pnlToolbar = new System.Windows.Forms.Panel();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnManageKeywords = new System.Windows.Forms.Button();
            this.cmbCategory = new System.Windows.Forms.ComboBox();
            this.lblCategory = new System.Windows.Forms.Label();
            this.dgvAiTrends = new System.Windows.Forms.DataGridView();
            this.colRank = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStars = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLanguage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSummary = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlDetail = new System.Windows.Forms.Panel();
            this.btnCardPreview = new System.Windows.Forms.Button();
            this.btnToggleFavorite = new System.Windows.Forms.Button();
            this.btnCopyUrl = new System.Windows.Forms.Button();
            this.btnConfigApiKey = new System.Windows.Forms.Button();
            this.btnOpenGithub = new System.Windows.Forms.Button();
            this.txtDetailDescription = new System.Windows.Forms.TextBox();
            this.txtDetailSummary = new System.Windows.Forms.TextBox();
            this.lblDetailMeta = new System.Windows.Forms.Label();
            this.lblDetailRepo = new System.Windows.Forms.Label();
            this.pnlTop.SuspendLayout();
            this.pnlToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAiTrends)).BeginInit();
            this.pnlDetail.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlTop
            // 
            this.pnlTop.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(250)))));
            this.pnlTop.Controls.Add(this.lblSubtitle);
            this.pnlTop.Controls.Add(this.lblTitle);
            this.pnlTop.Controls.Add(this.btnBack);
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(1830, 65);
            this.pnlTop.TabIndex = 0;
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblSubtitle.ForeColor = System.Drawing.Color.Gray;
            this.lblSubtitle.Location = new System.Drawing.Point(100, 38);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(262, 15);
            this.lblSubtitle.TabIndex = 2;
            this.lblSubtitle.Text = "실시간 AI 오픈소스 트렌드 & 핵심 데일리 브리핑";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.lblTitle.Location = new System.Drawing.Point(98, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(260, 21);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "🔥 AI Trends & Daily Briefing";
            // 
            // btnBack
            // 
            this.btnBack.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.btnBack.FlatAppearance.BorderSize = 0;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnBack.Location = new System.Drawing.Point(12, 14);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(75, 34);
            this.btnBack.TabIndex = 0;
            this.btnBack.Text = "◀ 뒤로";
            this.btnBack.UseVisualStyleBackColor = false;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // pnlToolbar
            // 
            this.pnlToolbar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlToolbar.BackColor = System.Drawing.Color.White;
            this.pnlToolbar.Controls.Add(this.progressBar);
            this.pnlToolbar.Controls.Add(this.lblStatus);
            this.pnlToolbar.Controls.Add(this.btnManageKeywords);
            this.pnlToolbar.Controls.Add(this.btnRefresh);
            this.pnlToolbar.Controls.Add(this.cmbCategory);
            this.pnlToolbar.Controls.Add(this.lblCategory);
            this.pnlToolbar.Location = new System.Drawing.Point(0, 65);
            this.pnlToolbar.Name = "pnlToolbar";
            this.pnlToolbar.Size = new System.Drawing.Size(1830, 40);
            this.pnlToolbar.TabIndex = 1;
            // 
            // progressBar
            // 
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBar.Location = new System.Drawing.Point(0, 36);
            this.progressBar.MarqueeAnimationSpeed = 30;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(1830, 4);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 4;
            this.progressBar.Visible = false;
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.AutoEllipsis = true;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(590, 10);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1224, 18);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "마지막 갱신: -";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnManageKeywords
            // 
            this.btnManageKeywords.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.btnManageKeywords.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.btnManageKeywords.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnManageKeywords.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnManageKeywords.Location = new System.Drawing.Point(458, 6);
            this.btnManageKeywords.Name = "btnManageKeywords";
            this.btnManageKeywords.Size = new System.Drawing.Size(120, 26);
            this.btnManageKeywords.TabIndex = 5;
            this.btnManageKeywords.Text = "📌 관심 키워드";
            this.btnManageKeywords.UseVisualStyleBackColor = false;
            this.btnManageKeywords.Click += new System.EventHandler(this.btnManageKeywords_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.btnRefresh.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnRefresh.Location = new System.Drawing.Point(355, 6);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(95, 26);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "🔄 새로고침";
            this.btnRefresh.UseVisualStyleBackColor = false;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // cmbCategory
            // 
            this.cmbCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCategory.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.cmbCategory.FormattingEnabled = true;
            this.cmbCategory.Location = new System.Drawing.Point(68, 8);
            this.cmbCategory.Name = "cmbCategory";
            this.cmbCategory.Size = new System.Drawing.Size(275, 23);
            this.cmbCategory.TabIndex = 1;
            this.cmbCategory.SelectedIndexChanged += new System.EventHandler(this.cmbCategory_SelectedIndexChanged);
            // 
            // lblCategory
            // 
            this.lblCategory.AutoSize = true;
            this.lblCategory.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblCategory.Location = new System.Drawing.Point(12, 11);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(50, 15);
            this.lblCategory.TabIndex = 0;
            this.lblCategory.Text = "분   류:";
            // 
            // dgvAiTrends
            // 
            this.dgvAiTrends.AllowUserToAddRows = false;
            this.dgvAiTrends.AllowUserToDeleteRows = false;
            this.dgvAiTrends.AllowUserToResizeRows = false;
            this.dgvAiTrends.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvAiTrends.BackgroundColor = System.Drawing.Color.White;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(245)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvAiTrends.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvAiTrends.ColumnHeadersHeight = 30;
            this.dgvAiTrends.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvAiTrends.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colRank,
            this.colName,
            this.colStars,
            this.colLanguage,
            this.colSummary});
            this.dgvAiTrends.EnableHeadersVisualStyles = false;
            this.dgvAiTrends.Location = new System.Drawing.Point(12, 112);
            this.dgvAiTrends.MultiSelect = false;
            this.dgvAiTrends.Name = "dgvAiTrends";
            this.dgvAiTrends.ReadOnly = true;
            this.dgvAiTrends.RowHeadersVisible = false;
            this.dgvAiTrends.RowHeadersWidth = 92;
            this.dgvAiTrends.RowTemplate.Height = 50;
            this.dgvAiTrends.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvAiTrends.Size = new System.Drawing.Size(1806, 835);
            this.dgvAiTrends.TabIndex = 2;
            this.dgvAiTrends.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvAiTrends_CellDoubleClick);
            this.dgvAiTrends.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.dgvAiTrends_CellPainting);
            this.dgvAiTrends.CellMouseEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvAiTrends_CellMouseEnter);
            this.dgvAiTrends.CellMouseLeave += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvAiTrends_CellMouseLeave);
            this.dgvAiTrends.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dgvAiTrends_KeyDown);
            this.dgvAiTrends.SelectionChanged += new System.EventHandler(this.dgvAiTrends_SelectionChanged);
            // 
            // colRank
            // 
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.colRank.DefaultCellStyle = dataGridViewCellStyle2;
            this.colRank.HeaderText = "순위";
            this.colRank.MinimumWidth = 11;
            this.colRank.Name = "colRank";
            this.colRank.ReadOnly = true;
            this.colRank.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colRank.Width = 55;
            // 
            // colName
            // 
            this.colName.HeaderText = "프로젝트명";
            this.colName.MinimumWidth = 11;
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            this.colName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colName.Width = 260;
            // 
            // colStars
            // 
            this.colStars.HeaderText = "Stars ⭐";
            this.colStars.MinimumWidth = 11;
            this.colStars.Name = "colStars";
            this.colStars.ReadOnly = true;
            this.colStars.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colStars.Width = 85;
            // 
            // colLanguage
            // 
            this.colLanguage.HeaderText = "언어";
            this.colLanguage.MinimumWidth = 11;
            this.colLanguage.Name = "colLanguage";
            this.colLanguage.ReadOnly = true;
            this.colLanguage.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colLanguage.Width = 100;
            // 
            // colSummary
            // 
            this.colSummary.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colSummary.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.colSummary.HeaderText = "AI 핵심 요약 & 실무 활용 (Summary)";
            this.colSummary.MinimumWidth = 11;
            this.colSummary.Name = "colSummary";
            this.colSummary.ReadOnly = true;
            this.colSummary.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // pnlDetail
            // 
            this.pnlDetail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlDetail.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.pnlDetail.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlDetail.Controls.Add(this.btnCardPreview);
            this.pnlDetail.Controls.Add(this.btnToggleFavorite);
            this.pnlDetail.Controls.Add(this.btnCopyUrl);
            this.pnlDetail.Controls.Add(this.btnConfigApiKey);
            this.pnlDetail.Controls.Add(this.btnOpenGithub);
            this.pnlDetail.Controls.Add(this.txtDetailDescription);
            this.pnlDetail.Controls.Add(this.txtDetailSummary);
            this.pnlDetail.Controls.Add(this.lblDetailMeta);
            this.pnlDetail.Controls.Add(this.lblDetailRepo);
            this.pnlDetail.Location = new System.Drawing.Point(12, 953);
            this.pnlDetail.Name = "pnlDetail";
            this.pnlDetail.Padding = new System.Windows.Forms.Padding(10);
            this.pnlDetail.Size = new System.Drawing.Size(1806, 155);
            this.pnlDetail.TabIndex = 3;
            // 
            // btnCardPreview
            // 
            this.btnCardPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCardPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(245)))));
            this.btnCardPreview.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.btnCardPreview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCardPreview.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnCardPreview.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.btnCardPreview.Location = new System.Drawing.Point(1208, 6);
            this.btnCardPreview.Name = "btnCardPreview";
            this.btnCardPreview.Size = new System.Drawing.Size(135, 28);
            this.btnCardPreview.TabIndex = 8;
            this.btnCardPreview.Text = "📰 카드뉴스 보기";
            this.btnCardPreview.UseVisualStyleBackColor = false;
            this.btnCardPreview.Click += new System.EventHandler(this.btnCardPreview_Click);
            // 
            // btnToggleFavorite
            // 
            this.btnToggleFavorite.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnToggleFavorite.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(245)))));
            this.btnToggleFavorite.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.btnToggleFavorite.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnToggleFavorite.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnToggleFavorite.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.btnToggleFavorite.Location = new System.Drawing.Point(1350, 6);
            this.btnToggleFavorite.Name = "btnToggleFavorite";
            this.btnToggleFavorite.Size = new System.Drawing.Size(85, 28);
            this.btnToggleFavorite.TabIndex = 7;
            this.btnToggleFavorite.Text = "⭐ 찜하기";
            this.btnToggleFavorite.UseVisualStyleBackColor = false;
            this.btnToggleFavorite.Click += new System.EventHandler(this.btnToggleFavorite_Click);
            // 
            // btnCopyUrl
            // 
            this.btnCopyUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopyUrl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(245)))));
            this.btnCopyUrl.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.btnCopyUrl.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCopyUrl.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnCopyUrl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.btnCopyUrl.Location = new System.Drawing.Point(1441, 6);
            this.btnCopyUrl.Name = "btnCopyUrl";
            this.btnCopyUrl.Size = new System.Drawing.Size(73, 28);
            this.btnCopyUrl.TabIndex = 6;
            this.btnCopyUrl.Text = "📋 복사";
            this.btnCopyUrl.UseVisualStyleBackColor = false;
            this.btnCopyUrl.Click += new System.EventHandler(this.btnCopyUrl_Click);
            // 
            // btnConfigApiKey
            // 
            this.btnConfigApiKey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConfigApiKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(245)))));
            this.btnConfigApiKey.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.btnConfigApiKey.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfigApiKey.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnConfigApiKey.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.btnConfigApiKey.Location = new System.Drawing.Point(1520, 6);
            this.btnConfigApiKey.Name = "btnConfigApiKey";
            this.btnConfigApiKey.Size = new System.Drawing.Size(125, 28);
            this.btnConfigApiKey.TabIndex = 5;
            this.btnConfigApiKey.Text = "🔑 API 설정";
            this.btnConfigApiKey.UseVisualStyleBackColor = false;
            this.btnConfigApiKey.Click += new System.EventHandler(this.btnConfigApiKey_Click);
            // 
            // btnOpenGithub
            // 
            this.btnOpenGithub.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenGithub.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(41)))), ((int)(((byte)(46)))));
            this.btnOpenGithub.FlatAppearance.BorderSize = 0;
            this.btnOpenGithub.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenGithub.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnOpenGithub.ForeColor = System.Drawing.Color.White;
            this.btnOpenGithub.Location = new System.Drawing.Point(1651, 6);
            this.btnOpenGithub.Name = "btnOpenGithub";
            this.btnOpenGithub.Size = new System.Drawing.Size(143, 28);
            this.btnOpenGithub.TabIndex = 4;
            this.btnOpenGithub.Text = "🌐 원문 열기 ↗";
            this.btnOpenGithub.UseVisualStyleBackColor = false;
            this.btnOpenGithub.Click += new System.EventHandler(this.btnOpenGithub_Click);
            // 
            // txtDetailDescription
            // 
            this.txtDetailDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDetailDescription.BackColor = System.Drawing.Color.White;
            this.txtDetailDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDetailDescription.Font = new System.Drawing.Font("맑은 고딕", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.txtDetailDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.txtDetailDescription.Location = new System.Drawing.Point(10, 105);
            this.txtDetailDescription.Multiline = true;
            this.txtDetailDescription.Name = "txtDetailDescription";
            this.txtDetailDescription.ReadOnly = true;
            this.txtDetailDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDetailDescription.Size = new System.Drawing.Size(1784, 39);
            this.txtDetailDescription.TabIndex = 3;
            // 
            // txtDetailSummary
            // 
            this.txtDetailSummary.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDetailSummary.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(248)))), ((int)(((byte)(255)))));
            this.txtDetailSummary.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDetailSummary.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.txtDetailSummary.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(71)))), ((int)(((byte)(161)))));
            this.txtDetailSummary.Location = new System.Drawing.Point(10, 52);
            this.txtDetailSummary.Multiline = true;
            this.txtDetailSummary.Name = "txtDetailSummary";
            this.txtDetailSummary.ReadOnly = true;
            this.txtDetailSummary.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDetailSummary.Size = new System.Drawing.Size(1784, 48);
            this.txtDetailSummary.TabIndex = 2;
            // 
            // lblDetailMeta
            // 
            this.lblDetailMeta.AutoSize = true;
            this.lblDetailMeta.Font = new System.Drawing.Font("맑은 고딕", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblDetailMeta.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblDetailMeta.Location = new System.Drawing.Point(10, 31);
            this.lblDetailMeta.Name = "lblDetailMeta";
            this.lblDetailMeta.Size = new System.Drawing.Size(193, 13);
            this.lblDetailMeta.TabIndex = 1;
            this.lblDetailMeta.Text = "Stars: -  |  Forks: -  |  Language: -";
            // 
            // lblDetailRepo
            // 
            this.lblDetailRepo.AutoSize = true;
            this.lblDetailRepo.Font = new System.Drawing.Font("맑은 고딕", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblDetailRepo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.lblDetailRepo.Location = new System.Drawing.Point(10, 10);
            this.lblDetailRepo.Name = "lblDetailRepo";
            this.lblDetailRepo.Size = new System.Drawing.Size(168, 19);
            this.lblDetailRepo.TabIndex = 0;
            this.lblDetailRepo.Text = "저장소를 선택해 주세요.";
            // 
            // AiTrendControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.pnlDetail);
            this.Controls.Add(this.dgvAiTrends);
            this.Controls.Add(this.pnlToolbar);
            this.Controls.Add(this.pnlTop);
            this.Name = "AiTrendControl";
            this.Size = new System.Drawing.Size(1830, 1120);
            this.Load += new System.EventHandler(this.AiTrendControl_Load);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.pnlToolbar.ResumeLayout(false);
            this.pnlToolbar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAiTrends)).EndInit();
            this.pnlDetail.ResumeLayout(false);
            this.pnlDetail.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.Panel pnlTop;
        public System.Windows.Forms.Button btnBack;
        public System.Windows.Forms.Label lblTitle;
        public System.Windows.Forms.Label lblSubtitle;
        public System.Windows.Forms.Panel pnlToolbar;
        public System.Windows.Forms.Label lblCategory;
        public System.Windows.Forms.ComboBox cmbCategory;
        public System.Windows.Forms.Button btnRefresh;
        public System.Windows.Forms.Button btnManageKeywords;
        public System.Windows.Forms.Label lblStatus;
        public System.Windows.Forms.ProgressBar progressBar;
        public System.Windows.Forms.DataGridView dgvAiTrends;
        public System.Windows.Forms.Panel pnlDetail;
        public System.Windows.Forms.Label lblDetailRepo;
        public System.Windows.Forms.Label lblDetailMeta;
        public System.Windows.Forms.TextBox txtDetailSummary;
        public System.Windows.Forms.TextBox txtDetailDescription;
        public System.Windows.Forms.Button btnCardPreview;
        public System.Windows.Forms.Button btnToggleFavorite;
        public System.Windows.Forms.Button btnCopyUrl;
        public System.Windows.Forms.Button btnConfigApiKey;
        public System.Windows.Forms.Button btnOpenGithub;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRank;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStars;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLanguage;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSummary;
    }
}
