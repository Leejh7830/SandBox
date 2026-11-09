using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin;

namespace GateHelper.AiTrend
{
    public partial class AiTrendControl : UserControl
    {
        private readonly AiTrendService _service;
        private List<AiRepoItem> _currentItems = new List<AiRepoItem>();
        private AiTrendFetchResult _lastResult = null;
        private bool _isLoading = false;
        private bool _isDarkMode = false;
        private int _currentLoadRequestId = 0;
        private int _hoveredRowIndex = -1;

        // UI 활성 여부 인터락 (UI가 카테고리를 조회 중일 때 백그라운드 프리로딩 양보)
        public static bool IsUiActive { get; private set; } = false;
        private CancellationTokenSource _preloadCts = new CancellationTokenSource();
        private bool _hasPreloadStartedOnEnter = false;

        // 빈 상태 및 오류 안내 오버레이 컨트롤
        private Panel _pnlEmptyOverlay = null;
        private Label _lblEmptyIcon = null;
        private Label _lblEmptyTitle = null;
        private Label _lblEmptyReason = null;
        private Button _btnEmptyRetry = null;

        public AiTrendControl()
        {
            InitializeComponent();
            _service = new AiTrendService();
        }

        private void AiTrendControl_Load(object sender, EventArgs e)
        {
            // Visual Studio 폼 디자이너에서 열릴 때는 네트워크/초기화 로직 건너뜀 (안전 방어)
            if (DesignMode) return;

            // 테마 동기화 (MaterialSkinManager 기준)
            try
            {
                bool isDark = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.DARK;
                ApplyTheme(isDark);
            }
            catch { }

            InitCategories();

            // 그리드 툴팁 바인딩 (긴 요약문 마우스 호버 시 전체 표시)
            dgvAiTrends.CellToolTipTextNeeded += DgvAiTrends_CellToolTipTextNeeded;

            // 상세 텍스트박스 포커스 시 전체선택 방지
            txtDetailDescription.TabStop = false;
            txtDetailDescription.Enter += (s, ev) =>
            {
                txtDetailDescription.Select(0, 0);
                txtDetailDescription.SelectionLength = 0;
            };

            if (cmbCategory.Items.Count > 0)
            {
                cmbCategory.SelectedIndex = 0;
            }

            // AI Trend 화면 진입 시 유휴 상태에서 백그라운드 순차 프리로딩 시동
            TriggerBackgroundPreloadIfIdle();
        }

        public void ApplyTheme(bool isDark)
        {
            _isDarkMode = isDark;

            if (isDark)
            {
                // [다크 모드]
                this.BackColor = Color.FromArgb(32, 33, 36);

                pnlTop.BackColor = Color.FromArgb(24, 25, 28);
                lblTitle.ForeColor = Color.FromArgb(240, 240, 240);
                lblSubtitle.ForeColor = Color.FromArgb(160, 160, 160);
                btnBack.BackColor = Color.FromArgb(50, 52, 58);
                btnBack.ForeColor = Color.White;

                pnlToolbar.BackColor = Color.FromArgb(32, 33, 36);
                lblCategory.ForeColor = Color.FromArgb(230, 230, 230);
                cmbCategory.BackColor = Color.FromArgb(45, 48, 55);
                cmbCategory.ForeColor = Color.White;
                btnRefresh.BackColor = Color.FromArgb(50, 52, 58);
                btnRefresh.ForeColor = Color.White;

                dgvAiTrends.BackgroundColor = Color.FromArgb(32, 33, 36);
                dgvAiTrends.GridColor = Color.FromArgb(55, 58, 65);
                dgvAiTrends.DefaultCellStyle.BackColor = Color.FromArgb(40, 44, 52);
                dgvAiTrends.DefaultCellStyle.ForeColor = Color.FromArgb(235, 240, 245);
                dgvAiTrends.DefaultCellStyle.SelectionBackColor = Color.FromArgb(65, 85, 125);
                dgvAiTrends.DefaultCellStyle.SelectionForeColor = Color.White;

                dgvAiTrends.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(24, 26, 30);
                dgvAiTrends.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(220, 220, 220);

                pnlDetail.BackColor = Color.FromArgb(28, 29, 32);
                lblDetailRepo.ForeColor = Color.FromArgb(245, 245, 245);
                lblDetailMeta.ForeColor = Color.FromArgb(180, 185, 190);

                txtDetailSummary.BackColor = Color.FromArgb(20, 35, 55);
                txtDetailSummary.ForeColor = Color.FromArgb(130, 195, 255);

                txtDetailDescription.BackColor = Color.FromArgb(38, 40, 45);
                txtDetailDescription.ForeColor = Color.FromArgb(210, 215, 220);

                btnToggleFavorite.BackColor = Color.FromArgb(50, 52, 58);
                btnToggleFavorite.ForeColor = Color.White;

                btnCardPreview.BackColor = Color.FromArgb(50, 52, 58);
                btnCardPreview.ForeColor = Color.White;

                btnCopyUrl.BackColor = Color.FromArgb(50, 52, 58);
                btnCopyUrl.ForeColor = Color.White;

                btnConfigApiKey.BackColor = Color.FromArgb(50, 52, 58);
                btnConfigApiKey.ForeColor = Color.White;

                btnManageKeywords.BackColor = Color.FromArgb(50, 52, 58);
                btnManageKeywords.ForeColor = Color.White;
            }
            else
            {
                // [라이트 모드]
                this.BackColor = Color.White;

                pnlTop.BackColor = Color.FromArgb(245, 247, 250);
                lblTitle.ForeColor = Color.FromArgb(33, 33, 33);
                lblSubtitle.ForeColor = Color.Gray;
                btnBack.BackColor = Color.FromArgb(224, 224, 224);
                btnBack.ForeColor = Color.Black;

                pnlToolbar.BackColor = Color.White;
                lblCategory.ForeColor = Color.Black;
                cmbCategory.BackColor = Color.White;
                cmbCategory.ForeColor = Color.Black;
                btnRefresh.BackColor = Color.FromArgb(240, 240, 240);
                btnRefresh.ForeColor = Color.Black;

                btnManageKeywords.BackColor = Color.FromArgb(240, 240, 240);
                btnManageKeywords.ForeColor = Color.Black;

                dgvAiTrends.BackgroundColor = Color.White;
                dgvAiTrends.GridColor = Color.FromArgb(230, 230, 230);
                dgvAiTrends.DefaultCellStyle.BackColor = Color.White;
                dgvAiTrends.DefaultCellStyle.ForeColor = Color.Black;
                dgvAiTrends.DefaultCellStyle.SelectionBackColor = SystemColors.Highlight;
                dgvAiTrends.DefaultCellStyle.SelectionForeColor = SystemColors.HighlightText;

                dgvAiTrends.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 242, 245);
                dgvAiTrends.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;

                pnlDetail.BackColor = Color.FromArgb(250, 250, 250);
                lblDetailRepo.ForeColor = Color.FromArgb(33, 33, 33);
                lblDetailMeta.ForeColor = Color.FromArgb(100, 100, 100);

                txtDetailSummary.BackColor = Color.FromArgb(243, 248, 255);
                txtDetailSummary.ForeColor = Color.FromArgb(13, 71, 161);

                txtDetailDescription.BackColor = Color.White;
                txtDetailDescription.ForeColor = Color.FromArgb(80, 80, 80);

                btnToggleFavorite.BackColor = Color.FromArgb(240, 242, 245);
                btnToggleFavorite.ForeColor = Color.FromArgb(33, 33, 33);

                btnCardPreview.BackColor = Color.FromArgb(240, 242, 245);
                btnCardPreview.ForeColor = Color.FromArgb(33, 33, 33);

                btnCopyUrl.BackColor = Color.FromArgb(240, 242, 245);
                btnCopyUrl.ForeColor = Color.FromArgb(33, 33, 33);

                btnConfigApiKey.BackColor = Color.FromArgb(240, 242, 245);
                btnConfigApiKey.ForeColor = Color.FromArgb(33, 33, 33);
            }

            // 현재 선택된 아이템 세부 뷰 색상 리프레시
            if (dgvAiTrends.SelectedRows.Count > 0 && dgvAiTrends.SelectedRows[0].Tag is AiRepoItem item)
            {
                UpdateDetailView(item);
            }
        }

        private void InitCategories(string selectCategoryId = null)
        {
            string prevId = selectCategoryId ?? (cmbCategory.SelectedItem as AiTrendCategory)?.Id;
            cmbCategory.Items.Clear();
            var categories = _service.GetCategories();
            int targetIndex = 0;

            for (int i = 0; i < categories.Count; i++)
            {
                cmbCategory.Items.Add(categories[i]);
                if (prevId != null && categories[i].Id == prevId)
                {
                    targetIndex = i;
                }
            }

            if (cmbCategory.Items.Count > 0)
            {
                cmbCategory.SelectedIndex = targetIndex;
            }
        }

        private async void cmbCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DesignMode) return;
            await LoadDataAsync(forceRefresh: false);
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            if (DesignMode) return;
            await LoadDataAsync(forceRefresh: true);
        }

        private void btnManageKeywords_Click(object sender, EventArgs e)
        {
            if (DesignMode) return;

            // 1단계: 검색 가이드(쿼리 변환 규칙 및 삭제 방법) 안내 팝업 폼 표시
            using (var guide = new KeywordGuideDialog(_isDarkMode))
            {
                if (guide.ShowDialog(this.FindForm()) != DialogResult.OK)
                {
                    return; // 사용자가 '닫기'를 누르면 가이드만 확인하고 종료
                }
            }

            // 2단계: 관심 키워드 추가/삭제 관리 다이얼로그 표시
            using (var dlg = new CustomKeywordDialog(_service, _isDarkMode))
            {
                dlg.ShowDialog(this.FindForm());
                if (dlg.HasChanges)
                {
                    string targetId = dlg.LastAddedItem != null ? $"custom_{dlg.LastAddedItem.Id}" : null;
                    InitCategories(targetId);
                }
            }
        }

        private async Task LoadDataAsync(bool forceRefresh)
        {
            if (!(cmbCategory.SelectedItem is AiTrendCategory selectedCat)) return;

            int requestId = ++_currentLoadRequestId;
            bool isNews = selectedCat.Kind != CategoryKind.GitHub;
            string sourceName = isNews ? "실시간 최신 뉴스" : "GitHub 최신 데이터";

            try
            {
                IsUiActive = true;
                SetLoading(true);
                lblStatus.Text = forceRefresh ? $"{sourceName} 수집 중..." : "데이터 로딩 중...";
                lblStatus.ForeColor = _isDarkMode ? Color.LightGray : Color.DimGray;
                lblStatus.Font = new Font(lblStatus.Font.FontFamily, 8.25F, FontStyle.Regular);

                UpdateGridHeaders(selectedCat);

                // [Stage 1: 초고속 1단계 목록 수집 및 즉시 렌더링 - 체감 0.8초 이내]
                var result = await _service.FetchRepositoriesAsync(selectedCat, forceRefresh);
                if (requestId != _currentLoadRequestId) return;

                _lastResult = result;
                _currentItems = result.Items ?? new List<AiRepoItem>();

                BindGrid(_currentItems);
                lblStatus.Text = result.DisplayStatus;

                // 스타터 팩 / 통신 실패 폴백 / 에러 케이스 처리
                if (result.IsStarterPack)
                {
                    lblStatus.ForeColor = Color.DarkOrange;
                    lblStatus.Font = new Font(lblStatus.Font.FontFamily, 8.5F, FontStyle.Bold);
                    HideEmptyOverlay();
                    SetLoading(false);
                    return;
                }
                else if (result.IsFallbackCache)
                {
                    lblStatus.ForeColor = Color.Crimson;
                    lblStatus.Font = new Font(lblStatus.Font.FontFamily, 8.5F, FontStyle.Bold);
                    HideEmptyOverlay();
                    SetLoading(false);
                    return;
                }
                else if (!result.IsLiveSuccess && _currentItems.Count == 0)
                {
                    lblStatus.ForeColor = Color.Red;
                    lblStatus.Font = new Font(lblStatus.Font.FontFamily, 8.5F, FontStyle.Bold);
                    ShowEmptyOverlay(result.FailureReason);
                    SetLoading(false);
                    return;
                }
                else
                {
                    HideEmptyOverlay();
                }

                // 이미 모든 AI 요약이 완비되어 있는 경우 (캐시 히트 or 기존 저장본 완전 재사용)
                if (result.IsFullyEnriched)
                {
                    lblStatus.ForeColor = result.IsLiveSuccess && !result.IsFallbackCache
                        ? Color.ForestGreen
                        : (_isDarkMode ? Color.LightGray : Color.DimGray);
                    lblStatus.Font = new Font(lblStatus.Font.FontFamily, 8.25F, FontStyle.Regular);
                    SetLoading(false);
                    return;
                }

                // [Stage 2 진입 준비]: 그리드는 이미 0.8초 시점에 표시되었으므로, 버튼과 콤보박스는 즉시 조작 가능하도록 활성화!
                _isLoading = false;
                btnRefresh.Enabled = true;
                btnManageKeywords.Enabled = true;
                cmbCategory.Enabled = true;
                progressBar.Visible = true; // 요약 진행 중임을 알리는 상단 인디케이터만 유지
                lblStatus.ForeColor = Color.DodgerBlue;
                lblStatus.Font = new Font(lblStatus.Font.FontFamily, 8.25F, FontStyle.Regular);

                // [Stage 2: 백그라운드 AI 핵심 요약 및 실무 팁/시사점 생성 - 1.5초~2초]
                var enrichStatus = await _service.EnrichRepositoriesAsync(selectedCat, _currentItems);
                if (requestId != _currentLoadRequestId) return;

                // 그리드 요약 컬럼 갱신 & 하단 상세 뷰 동기화
                RefreshGridSummaries();
                if (dgvAiTrends.SelectedRows.Count > 0 && dgvAiTrends.SelectedRows[0].Tag is AiRepoItem curItem)
                {
                    UpdateDetailView(curItem);
                }

                if (enrichStatus == AiEnrichStatus.QuotaExceeded)
                {
                    lblStatus.Text = $"✔ {sourceName} (⚠️ AI 요약 분당 한도 초과로 원문 표시 중 - 잠시 후 새로고침)";
                    lblStatus.ForeColor = Color.DarkOrange;
                }
                else
                {
                    lblStatus.Text = $"✔ {sourceName} ({DateTime.Now:yyyy-MM-dd HH:mm})";
                    lblStatus.ForeColor = Color.ForestGreen;
                }
                lblStatus.Font = new Font(lblStatus.Font.FontFamily, 8.25F, FontStyle.Regular);
            }
            catch (Exception ex)
            {
                if (requestId == _currentLoadRequestId)
                {
                    lblStatus.Text = "오류: " + ex.Message;
                    lblStatus.ForeColor = Color.Red;
                    LogManager.LogException(ex, LogManager.Level.Warning, "AiTrendControl LoadDataAsync Failed");
                    if (_currentItems.Count == 0)
                    {
                        ShowEmptyOverlay(ex.Message);
                    }
                }
            }
            finally
            {
                if (requestId == _currentLoadRequestId)
                {
                    SetLoading(false);
                }
                IsUiActive = false;
            }
        }

        private void UpdateGridHeaders(AiTrendCategory category)
        {
            if (category == null) return;

            bool isNews = category.Kind == CategoryKind.NewsRss || category.Kind == CategoryKind.CustomKeyword;
            if (isNews)
            {
                colName.HeaderText = "주요 뉴스 헤드라인";
                colName.Width = 360; // 기존 240에서 1.5배 확대
                colStars.HeaderText = "언론사";
                colStars.Width = 100;
                colLanguage.HeaderText = "발행 시간";
                colLanguage.Width = 100;
                colSummary.HeaderText = "💡 AI 핵심 1줄 브리핑 & 핵심 시사점";
            }
            else
            {
                colName.HeaderText = "프로젝트명";
                colName.Width = 260; // 기존 170에서 1.5배 확대
                colStars.HeaderText = "Stars ⭐";
                colStars.Width = 85;
                colLanguage.HeaderText = "언어";
                colLanguage.Width = 85;
                colSummary.HeaderText = "💡 AI 핵심 1줄 요약 & 실무 활용 팁";
            }
        }

        private void RefreshGridSummaries()
        {
            for (int i = 0; i < dgvAiTrends.Rows.Count; i++)
            {
                var row = dgvAiTrends.Rows[i];
                if (row.Tag is AiRepoItem item)
                {
                    row.Cells[colSummary.Index].Value = item.DisplayFullSummary;
                }
            }
            dgvAiTrends.InvalidateColumn(colSummary.Index);
        }

        private void SetLoading(bool loading)
        {
            _isLoading = loading;
            progressBar.Visible = loading;
            btnRefresh.Enabled = !loading;
            btnManageKeywords.Enabled = !loading;
            cmbCategory.Enabled = !loading;
        }

        private void BindGrid(List<AiRepoItem> items)
        {
            dgvAiTrends.Rows.Clear();

            foreach (var item in items)
            {
                string favIcon = item.IsFavorite ? "★ " : "";
                string displaySummary = item.DisplayFullSummary;

                string col3Value = item.ItemType == TrendItemType.NewsArticle ? item.Owner : item.StarsFormatted;
                string col4Value = item.ItemType == TrendItemType.NewsArticle ? item.TimeFormatted : item.Language;

                int rowIndex = dgvAiTrends.Rows.Add(
                    item.Rank,
                    favIcon + item.FullName,
                    col3Value,
                    col4Value,
                    displaySummary
                );

                dgvAiTrends.Rows[rowIndex].Tag = item;
            }

            if (dgvAiTrends.Rows.Count > 0)
            {
                dgvAiTrends.Rows[0].Selected = true;
                UpdateDetailView(items[0]);
            }
            else
            {
                ClearDetailView();
            }
        }

        private void dgvAiTrends_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvAiTrends.SelectedRows.Count > 0)
            {
                var selectedRow = dgvAiTrends.SelectedRows[0];
                if (selectedRow.Tag is AiRepoItem item)
                {
                    UpdateDetailView(item);
                }
            }
        }

        private void dgvAiTrends_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dgvAiTrends.Rows.Count)
            {
                OpenPreviewDialog(e.RowIndex);
            }
        }

        private void dgvAiTrends_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (dgvAiTrends.SelectedRows.Count > 0)
                {
                    OpenPreviewDialog(dgvAiTrends.SelectedRows[0].Index);
                }
            }
        }

        private void dgvAiTrends_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex != _hoveredRowIndex)
            {
                int oldIdx = _hoveredRowIndex;
                _hoveredRowIndex = e.RowIndex;
                if (oldIdx >= 0 && oldIdx < dgvAiTrends.Rows.Count) dgvAiTrends.InvalidateRow(oldIdx);
                dgvAiTrends.InvalidateRow(_hoveredRowIndex);
            }
        }

        private void dgvAiTrends_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (_hoveredRowIndex >= 0)
            {
                int oldIdx = _hoveredRowIndex;
                _hoveredRowIndex = -1;
                if (oldIdx >= 0 && oldIdx < dgvAiTrends.Rows.Count) dgvAiTrends.InvalidateRow(oldIdx);
            }
        }

        private void btnCardPreview_Click(object sender, EventArgs e)
        {
            OpenPreviewDialog();
        }

        private void OpenPreviewDialog(int startIndex = -1)
        {
            if (_currentItems == null || _currentItems.Count == 0) return;
            int idx = startIndex >= 0 ? startIndex : (dgvAiTrends.SelectedRows.Count > 0 ? dgvAiTrends.SelectedRows[0].Index : 0);

            using (var dlg = new AiTrendPreviewDialog(_currentItems, idx, _service, _isDarkMode))
            {
                dlg.ShowDialog(this.FindForm());

                // 팝업에서 마지막으로 보던 기사로 메인 그리드 선택 행 동기화
                if (dlg.CurrentIndex >= 0 && dlg.CurrentIndex < dgvAiTrends.Rows.Count)
                {
                    dgvAiTrends.ClearSelection();
                    dgvAiTrends.Rows[dlg.CurrentIndex].Selected = true;
                    UpdateDetailView(_currentItems[dlg.CurrentIndex]);
                }

                if (dlg.HasChanges)
                {
                    if (cmbCategory.SelectedItem is AiTrendCategory curCat && (curCat.Id == "favorites_github" || curCat.Id == "favorites_news" || curCat.Id == "favorites"))
                    {
                        _ = LoadDataAsync(forceRefresh: false);
                    }
                    else
                    {
                        for (int i = 0; i < dgvAiTrends.Rows.Count; i++)
                        {
                            var row = dgvAiTrends.Rows[i];
                            if (row.Tag is AiRepoItem it)
                            {
                                string favIcon = it.IsFavorite ? "★ " : "";
                                row.Cells[colName.Index].Value = favIcon + it.FullName;
                            }
                        }
                        if (dgvAiTrends.SelectedRows.Count > 0 && dgvAiTrends.SelectedRows[0].Tag is AiRepoItem cur)
                        {
                            UpdateDetailView(cur);
                        }
                    }
                }
            }
        }

        private void dgvAiTrends_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            bool isSelected = (e.State & DataGridViewElementStates.Selected) != 0;
            bool isHovered = !isSelected && e.RowIndex == _hoveredRowIndex;

            // 1) 순위 열 (colRank): 선택 시 세련된 4px 네온 블루 Accent Bar 렌더링
            if (dgvAiTrends.Columns[e.ColumnIndex] == colRank)
            {
                if (isHovered)
                {
                    Color hoverColor = _isDarkMode ? Color.FromArgb(45, 48, 55) : Color.FromArgb(242, 246, 252);
                    using (var b = new SolidBrush(hoverColor)) e.Graphics.FillRectangle(b, e.CellBounds);
                    e.Paint(e.ClipBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Background & ~DataGridViewPaintParts.Border);
                }
                else
                {
                    e.Paint(e.ClipBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Border);
                }

                if (isSelected)
                {
                    // 행 선택 시 맨 왼쪽에 선명한 4px 네온 블루 세로 포인트 바 (Accent Bar)
                    using (var accentBrush = new SolidBrush(Color.FromArgb(0, 150, 255)))
                    {
                        e.Graphics.FillRectangle(accentBrush, e.CellBounds.X, e.CellBounds.Y, 4, e.CellBounds.Height);
                    }
                }

                e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
                e.Handled = true;
                return;
            }

            // 2) 호버(Hover) 시 다른 일반 셀들의 배경 은은하게 하이라이트
            if (isHovered && dgvAiTrends.Columns[e.ColumnIndex] != colLanguage && dgvAiTrends.Columns[e.ColumnIndex] != colSummary)
            {
                Color hoverColor = _isDarkMode ? Color.FromArgb(45, 48, 55) : Color.FromArgb(242, 246, 252);
                using (var b = new SolidBrush(hoverColor)) e.Graphics.FillRectangle(b, e.CellBounds);
                e.Paint(e.ClipBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Background & ~DataGridViewPaintParts.Border);
                e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
                e.Handled = true;
                return;
            }

            // 1) 뉴스 모드 발행 시간 열: 24시간 이내 최신 뉴스에 주황색(DarkOrange) 불꽃 🔥 렌더링
            if (dgvAiTrends.Columns[e.ColumnIndex] == colLanguage)
            {
                if (dgvAiTrends.Rows[e.RowIndex].Tag is AiRepoItem item && item.ItemType == TrendItemType.NewsArticle)
                {
                    if (isHovered)
                    {
                        Color hoverColor = _isDarkMode ? Color.FromArgb(45, 48, 55) : Color.FromArgb(242, 246, 252);
                        using (var b = new SolidBrush(hoverColor)) e.Graphics.FillRectangle(b, e.CellBounds);
                    }
                    else
                    {
                        e.PaintBackground(e.ClipBounds, isSelected);
                    }

                    Rectangle contentBounds = e.CellBounds;
                    contentBounds.X += 6;
                    contentBounds.Width -= 12;

                    Color normalTextColor = isSelected
                        ? e.CellStyle.SelectionForeColor
                        : (_isDarkMode ? Color.FromArgb(235, 240, 245) : Color.FromArgb(40, 40, 40));

                    if (item.IsRecentNews)
                    {
                        // 주황색 불꽃 🔥 + 시간 문자열 (예: " 35분 전", " 2시간 전")
                        using (var flameFont = new Font(e.CellStyle.Font.FontFamily, 9F, FontStyle.Bold))
                        using (var timeFont = new Font(e.CellStyle.Font.FontFamily, 8.5F, FontStyle.Regular))
                        {
                            string flame = "🔥";
                            string timeText = " " + item.TimeFormatted;

                            Color flameColor = isSelected ? Color.Yellow : Color.FromArgb(255, 120, 0); // 선명하고 산뜻한 주황색
                            Size flameSize = TextRenderer.MeasureText(e.Graphics, flame, flameFont, Size.Empty, TextFormatFlags.NoPadding);

                            var flameRect = new Rectangle(contentBounds.X, contentBounds.Y, flameSize.Width, contentBounds.Height);
                            TextRenderer.DrawText(e.Graphics, flame, flameFont, flameRect, flameColor,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

                            var timeRect = new Rectangle(contentBounds.X + flameSize.Width, contentBounds.Y, contentBounds.Width - flameSize.Width, contentBounds.Height);
                            TextRenderer.DrawText(e.Graphics, timeText, timeFont, timeRect, normalTextColor,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                        }
                    }
                    else
                    {
                        using (var font = new Font(e.CellStyle.Font.FontFamily, 8.5F, FontStyle.Regular))
                        {
                            Color pastTextColor = isSelected ? e.CellStyle.SelectionForeColor : (_isDarkMode ? Color.LightGray : Color.Gray);
                            TextRenderer.DrawText(e.Graphics, item.TimeFormatted, font, contentBounds, pastTextColor,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                        }
                    }

                    e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
                    e.Handled = true;
                    return;
                }
            }

            // 2) 요약 열(colSummary) 데이터 셀의 세로 간격 쾌적화 렌더링 & 테두리선(Border) 보존
            if (dgvAiTrends.Columns[e.ColumnIndex] == colSummary)
            {
                if (dgvAiTrends.Rows[e.RowIndex].Tag is AiRepoItem item)
                {
                    if (isHovered)
                    {
                        Color hoverColor = _isDarkMode ? Color.FromArgb(45, 48, 55) : Color.FromArgb(242, 246, 252);
                        using (var b = new SolidBrush(hoverColor)) e.Graphics.FillRectangle(b, e.CellBounds);
                    }
                    else
                    {
                        e.PaintBackground(e.ClipBounds, isSelected);
                    }

                    // 셀 내부 여백 (좌우 6px, 상하 5px)
                    Rectangle contentBounds = e.CellBounds;
                    contentBounds.X += 6;
                    contentBounds.Width -= 12;
                    contentBounds.Y += 5;
                    contentBounds.Height -= 10;

                    Color textColor = isSelected
                        ? e.CellStyle.SelectionForeColor
                        : (_isDarkMode ? Color.FromArgb(235, 240, 245) : Color.FromArgb(30, 30, 30));

                    Color tipColor = isSelected
                        ? e.CellStyle.SelectionForeColor
                        : (_isDarkMode ? Color.FromArgb(145, 200, 255) : Color.FromArgb(21, 101, 192));

                    if (!string.IsNullOrWhiteSpace(item.PracticalTip))
                    {
                        using (var font1 = new Font(e.CellStyle.Font.FontFamily, 8.75F, FontStyle.Regular))
                        using (var font2 = new Font(e.CellStyle.Font.FontFamily, 8.25F, FontStyle.Regular))
                        {
                            string tipPrefix = item.ItemType == TrendItemType.NewsArticle ? "💡 [시사점]" : "🚀 [활용]";
                            string line1 = $"📌 {item.AiSummary}";
                            string line2 = $"{tipPrefix} {item.PracticalTip}";

                            // 1번째 줄 (핵심 요약 / 팩트)
                            int line1Height = 17;
                            var rect1 = new Rectangle(contentBounds.X, contentBounds.Y, contentBounds.Width, line1Height);
                            TextRenderer.DrawText(e.Graphics, line1, font1, rect1, textColor,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                            // 2번째 줄 (실무 활용 / 시사점) - 1번째 줄과의 세로 간격을 5px 띄워서 시원하게 렌더링!
                            int lineGap = 5;
                            int line2Y = contentBounds.Y + line1Height + lineGap;
                            int line2Height = 17;
                            var rect2 = new Rectangle(contentBounds.X, line2Y, contentBounds.Width, line2Height);
                            TextRenderer.DrawText(e.Graphics, line2, font2, rect2, tipColor,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                        }
                    }
                    else
                    {
                        // 팁이 없는 단일 줄 요약 또는 원문 설명: 셀 중앙(VerticalCenter)에 편안하게 정렬
                        using (var fontSingle = new Font(e.CellStyle.Font.FontFamily, 8.75F, FontStyle.Regular))
                        {
                            string textToDraw = !string.IsNullOrWhiteSpace(item.AiSummary)
                                ? $"📌 {item.AiSummary}"
                                : item.Description;

                            TextRenderer.DrawText(e.Graphics, textToDraw, fontSingle, contentBounds, textColor,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                        }
                    }

                    // [중요] 기본 그리드 테두리선(GridLine/Border) 보존 렌더링
                    e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
                    e.Handled = true;
                    return;
                }
            }
        }

        private void UpdateDetailView(AiRepoItem item)
        {
            if (item == null)
            {
                ClearDetailView();
                return;
            }

            bool isNews = item.ItemType == TrendItemType.NewsArticle;
            string favMark = item.IsFavorite ? " ⭐" : "";
            lblDetailRepo.Text = $"#{item.Rank} {item.FullName}{favMark}";

            // 오프라인 스타터 팩 / 과거 캐시 / 실시간 뱃지 구분
            string statusBadge = "";
            if (_lastResult != null && _lastResult.IsStarterPack)
            {
                statusBadge = "  |  📡 [오프라인 대표 AI 가이드]";
            }
            else if (_lastResult != null && _lastResult.IsFallbackCache)
            {
                statusBadge = $"  |  ⚠️ [오프라인 저장본: {_lastResult.DataTimestamp:yyyy-MM-dd HH:mm}]";
            }

            if (isNews)
            {
                lblDetailMeta.Text = $"📰 언론사: {item.Owner}  |  🔥 발행: {item.TimeFormatted} ({item.PushedAt:yyyy-MM-dd HH:mm}){statusBadge}";
                btnToggleFavorite.Text = item.IsFavorite ? "★ 찜 해제" : "⭐ 찜하기";
                btnOpenGithub.Text = "🔗 기사 원문";

                if (!string.IsNullOrWhiteSpace(item.DetailStory))
                {
                    txtDetailDescription.Text = "📖 [AI 심층 스토리] " + item.DetailStory;
                }
                else
                {
                    bool isDuplicate = string.Equals(item.FullName?.Trim(), item.Description?.Trim(), StringComparison.OrdinalIgnoreCase);
                    txtDetailDescription.Text = isDuplicate
                        ? "⏳ [AI 심층 브리핑 작성 중...] 기사의 배경 맥락과 전개 상황을 분석하고 있습니다."
                        : "📄 [기사 본문 요약] " + item.Description;
                }
            }
            else
            {
                lblDetailMeta.Text = $"⭐ Stars: {item.StarsFormatted} ({item.Stars:N0})  |  🍴 Forks: {item.Forks:N0}  |  💻 언어: {item.Language}  |  ⏰ 최근 커밋: {item.PushedAtFormatted}{statusBadge}";
                btnToggleFavorite.Text = item.IsFavorite ? "★ 찜 해제" : "⭐ 찜하기";
                btnOpenGithub.Text = "🔗 프로젝트 원문";
                txtDetailDescription.Text = !string.IsNullOrWhiteSpace(item.DetailStory)
                    ? "📖 [AI 심층 분석] " + item.DetailStory
                    : "📄 [원문 설명] " + item.Description;
            }

            // 찜하기 버튼 상태 동기화 및 고대비 색상 적용
            if (item.IsFavorite)
            {
                btnToggleFavorite.BackColor = _isDarkMode ? Color.FromArgb(70, 60, 20) : Color.FromArgb(255, 243, 205);
                btnToggleFavorite.ForeColor = _isDarkMode ? Color.FromArgb(255, 220, 50) : Color.FromArgb(133, 100, 4);
            }
            else
            {
                btnToggleFavorite.BackColor = _isDarkMode ? Color.FromArgb(50, 52, 58) : Color.FromArgb(240, 242, 245);
                btnToggleFavorite.ForeColor = _isDarkMode ? Color.White : Color.FromArgb(33, 33, 33);
            }

            // 요약 표시 (2줄 구조: 1줄 핵심 정의/팩트, 2줄 실무/시사점 포인트 - 세로 여백 최적화)
            if (_lastResult != null && _lastResult.IsStarterPack)
            {
                btnConfigApiKey.Visible = false; // 스타터 팩은 이미 최고 품질 한국어 해설이 완비됨
                txtDetailSummary.Text = $"📌 [핵심 요약] {item.AiSummary}\r\n🚀 [실무 활용] {item.PracticalTip}";
            }
            else
            {
                btnConfigApiKey.Visible = true;
                bool hasKey = _service.HasApiKey;
                if (!hasKey)
                {
                    btnConfigApiKey.Text = "🔑 API 등록";
                    btnConfigApiKey.BackColor = Color.FromArgb(255, 236, 179); // 골드 옐로우 강조
                    txtDetailSummary.Text = isNews
                        ? "💡 [Gemini API Key 미등록]\r\n우측 상단의 '🔑 API 등록' 버튼을 눌러 무료 키를 등록하시면 기사의 핵심 사실과 주요 시사점을 2줄로 브리핑해 드립니다."
                        : "💡 [Gemini API Key 미등록]\r\n우측 상단의 '🔑 API 등록' 버튼을 눌러 무료 키를 등록하시면 각 도구의 핵심 역할과 실무 활용 팁을 2줄로 요약해 드립니다.";
                }
                else
                {
                    btnConfigApiKey.Text = "🔑 API 설정";
                    btnConfigApiKey.BackColor = _isDarkMode ? Color.FromArgb(50, 52, 58) : Color.FromArgb(240, 242, 245);

                    if (!string.IsNullOrWhiteSpace(item.PracticalTip))
                    {
                        string tipHeader = isNews ? "💡 [핵심 시사점]" : "🚀 [실무 활용]";
                        string summaryHeader = isNews ? "📌 [핵심 팩트]" : "📌 [핵심 요약]";
                        txtDetailSummary.Text = $"{summaryHeader} {item.AiSummary}\r\n{tipHeader} {item.PracticalTip}";
                    }
                    else
                    {
                        string summaryHeader = isNews ? "📌 [핵심 팩트] " : "📌 [핵심 요약] ";
                        string rawHeader = isNews ? "📄 [기사 내용] " : "📄 [원문 요약] ";
                        txtDetailSummary.Text = string.IsNullOrWhiteSpace(item.AiSummary)
                            ? (isNews ? "⏳ [AI 브리핑 생성 중...] 잠시 후 핵심 팩트와 주요 시사점이 자동으로 채워집니다." : (rawHeader + item.Description))
                            : (summaryHeader + item.AiSummary);
                    }
                }
            }

            btnOpenGithub.Tag = item.HtmlUrl;
            btnOpenGithub.Enabled = !string.IsNullOrWhiteSpace(item.HtmlUrl);
            btnCopyUrl.Tag = item.HtmlUrl;
            btnCopyUrl.Enabled = !string.IsNullOrWhiteSpace(item.HtmlUrl);
        }

        private void ClearDetailView()
        {
            lblDetailRepo.Text = "표시할 프로젝트가 없습니다.";
            lblDetailMeta.Text = "-";
            txtDetailSummary.Clear();
            txtDetailDescription.Clear();
            btnOpenGithub.Tag = null;
            btnOpenGithub.Enabled = false;
            btnCopyUrl.Tag = null;
            btnCopyUrl.Enabled = false;
            btnToggleFavorite.Text = "⭐ 찜하기";
        }

        private void btnToggleFavorite_Click(object sender, EventArgs e)
        {
            if (dgvAiTrends.SelectedRows.Count == 0) return;
            if (!(dgvAiTrends.SelectedRows[0].Tag is AiRepoItem item)) return;

            bool isFavNow = _service.ToggleFavorite(item);
            UpdateDetailView(item);

            // 현재 '내가 찜한 오픈소스/뉴스' 보관함 카테고리를 보고 있는 상태에서 찜 해제 시 목록에서 즉시 제거
            if (cmbCategory.SelectedItem is AiTrendCategory curCat && (curCat.Id == "favorites_github" || curCat.Id == "favorites_news" || curCat.Id == "favorites"))
            {
                if (!isFavNow)
                {
                    _currentItems.Remove(item);
                    BindGrid(_currentItems);
                    string typeName = curCat.Id == "favorites_news" ? "뉴스 기사" : "오픈소스 프로젝트";
                    lblStatus.Text = $"⭐ 내가 찜한 {typeName} ({_currentItems.Count}개 보관 중)";
                    return;
                }
            }

            // 일반 카테고리에서는 그리드 행 텍스트 갱신 (★ 붙이기/떼기)
            var selRow = dgvAiTrends.SelectedRows[0];
            string favIcon = isFavNow ? "★ " : "";
            selRow.Cells[1].Value = favIcon + item.FullName;
        }

        private async void btnCopyUrl_Click(object sender, EventArgs e)
        {
            string url = btnCopyUrl.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                Clipboard.SetText(url);
                string origText = btnCopyUrl.Text;
                btnCopyUrl.Text = "✔ 복사됨!";
                btnCopyUrl.BackColor = Color.FromArgb(200, 230, 201);
                await Task.Delay(1200);
                btnCopyUrl.Text = origText;
                btnCopyUrl.BackColor = _isDarkMode ? Color.FromArgb(50, 52, 58) : Color.FromArgb(240, 242, 245);
            }
            catch (Exception ex)
            {
                MessageBox.Show("클립보드 복사 실패: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DgvAiTrends_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dgvAiTrends.Rows.Count)
            {
                if (dgvAiTrends.Rows[e.RowIndex].Tag is AiRepoItem item)
                {
                    string summary = string.IsNullOrWhiteSpace(item.PracticalTip)
                        ? item.AiSummary
                        : $"📌 [핵심 요약] {item.AiSummary}\n🚀 [실무 활용] {item.PracticalTip}";

                    if (string.IsNullOrWhiteSpace(summary)) summary = item.Description;

                    string banner = "";
                    if (_lastResult != null && _lastResult.IsStarterPack)
                    {
                        banner = "[📡 인터넷 연결 대기 중 - 오프라인 AI 대표 명작 가이드]\n\n";
                    }
                    else if (_lastResult != null && _lastResult.IsFallbackCache)
                    {
                        banner = $"[⚠️ 실시간 갱신 실패로 {_lastResult.DataTimestamp:yyyy-MM-dd HH:mm} 저장된 캐시 데이터 표시 중]\n(실패 사유: {_lastResult.FailureReason})\n\n";
                    }

                    e.ToolTipText = $"{banner}[{item.FullName}]\n⭐ Stars: {item.StarsFormatted} ({item.Stars:N0}) | 언어: {item.Language} | 최근 커밋: {item.PushedAtFormatted}\n\n{summary}\n\n[📄 원문 설명]\n{item.Description}";
                }
            }
        }

        #region [Empty State Overlay]

        private void EnsureEmptyOverlayCreated()
        {
            if (_pnlEmptyOverlay != null) return;

            _pnlEmptyOverlay = new Panel
            {
                BackColor = _isDarkMode ? Color.FromArgb(32, 33, 36) : Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = dgvAiTrends.Location,
                Size = dgvAiTrends.Size
            };

            _lblEmptyIcon = new Label
            {
                Text = "📡 ❌",
                Font = new Font("맑은 고딕", 26F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            _lblEmptyTitle = new Label
            {
                Text = "데이터를 불러올 수 없습니다",
                Font = new Font("맑은 고딕", 12F, FontStyle.Bold),
                ForeColor = _isDarkMode ? Color.White : Color.FromArgb(50, 50, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 35
            };

            _lblEmptyReason = new Label
            {
                Text = "인터넷 연결 상태를 확인하시거나 아래 버튼을 눌러 다시 시도하세요.",
                Font = new Font("맑은 고딕", 9F, FontStyle.Regular),
                ForeColor = _isDarkMode ? Color.LightGray : Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 50
            };

            _btnEmptyRetry = new Button
            {
                Text = "🔄 다시 시도 (Retry)",
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                Size = new Size(150, 36),
                BackColor = Color.FromArgb(36, 41, 46),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnEmptyRetry.FlatAppearance.BorderSize = 0;
            _btnEmptyRetry.Click += (s, ev) => btnRefresh_Click(s, ev);

            var btnContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            btnContainer.Controls.Add(_btnEmptyRetry);
            btnContainer.Padding = new Padding((_pnlEmptyOverlay.Width - 150) / 2, 5, 0, 0);

            _pnlEmptyOverlay.Controls.Add(btnContainer);
            _pnlEmptyOverlay.Controls.Add(_lblEmptyReason);
            _pnlEmptyOverlay.Controls.Add(_lblEmptyTitle);
            _pnlEmptyOverlay.Controls.Add(_lblEmptyIcon);
            _pnlEmptyOverlay.Padding = new Padding(20, 40, 20, 20);

            this.Controls.Add(_pnlEmptyOverlay);
            _pnlEmptyOverlay.BringToFront();
        }

        private void ShowEmptyOverlay(string reason)
        {
            EnsureEmptyOverlayCreated();
            _lblEmptyReason.Text = $"원인: {reason}\n네트워크 연결 상태를 확인하시거나 아래 버튼을 눌러 다시 시도하세요.";
            _pnlEmptyOverlay.Location = dgvAiTrends.Location;
            _pnlEmptyOverlay.Size = dgvAiTrends.Size;
            _pnlEmptyOverlay.Visible = true;
            _pnlEmptyOverlay.BringToFront();
        }

        private void HideEmptyOverlay()
        {
            if (_pnlEmptyOverlay != null)
            {
                _pnlEmptyOverlay.Visible = false;
            }
        }

        #endregion

        private async void btnConfigApiKey_Click(object sender, EventArgs e)
        {
            string currentKey = _service.GetGeminiApiKey();

            using (var form = new GateHelper.MysteryTime.MysteryApiKeyForm(currentKey))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    bool hasKeyNow = _service.HasApiKey;
                    if (hasKeyNow && _currentItems.Count > 0 && !(_lastResult?.IsStarterPack ?? false))
                    {
                        try
                        {
                            btnConfigApiKey.Enabled = false;
                            btnConfigApiKey.Text = "요약 생성 중...";
                            lblStatus.Text = "새로 등록된 키로 AI 핵심 요약 및 실무 활용 팁을 실시간 생성하고 있습니다...";
                            progressBar.Visible = true;

                            string catId = (cmbCategory.SelectedItem as AiTrendCategory)?.Id ?? "trending";
                            bool success = await _service.ReSummarizeItemsAsync(_currentItems, catId);

                            BindGrid(_currentItems);
                            lblStatus.Text = success 
                                ? "한국어 AI 요약 및 실무 활용 포인트가 성공적으로 적용되었습니다!" 
                                : "AI 요약 갱신 완료";
                        }
                        finally
                        {
                            btnConfigApiKey.Enabled = true;
                            btnConfigApiKey.Text = "🔑 API 설정";
                            btnConfigApiKey.BackColor = _isDarkMode ? Color.FromArgb(50, 52, 58) : Color.FromArgb(240, 242, 245);
                            progressBar.Visible = false;
                        }
                    }
                    else
                    {
                        if (dgvAiTrends.SelectedRows.Count > 0 && dgvAiTrends.SelectedRows[0].Tag is AiRepoItem item)
                        {
                            UpdateDetailView(item);
                        }
                    }
                }
            }
        }

        private void btnOpenGithub_Click(object sender, EventArgs e)
        {
            try
            {
                string url = btnOpenGithub.Tag?.ToString();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("브라우저를 열지 못했습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (this.ParentForm is SandBox sb)
            {
                sb.BackToList();
            }
        }

        private void TriggerBackgroundPreloadIfIdle()
        {
            if (_hasPreloadStartedOnEnter) return;
            _hasPreloadStartedOnEnter = true;

            // 옵션이 꺼져 있더라도 AI Trend 탭에 사용자가 들어온 순간 백그라운드 유휴 큐 시동!
            // (옵션이 켜져 있었다면 이미 프로그램 시작 시 가동되었으므로 안전한 내부 락으로 중복 실행 방지됨)
            _ = Task.Run(async () =>
            {
                try
                {
                    // 첫 화면 즉시 렌더링(0.8초) 후 곧바로 백그라운드 전체 순차 예열 시작 (2초 대기)
                    await Task.Delay(2000, _preloadCts.Token);
                    await _service.PreloadAllCategoriesAsync(
                        _preloadCts.Token,
                        canProceed: () => !IsUiActive && !Util.IsAnyMainOperationBusy()
                    );
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    LogManager.LogException(ex, LogManager.Level.Warning, "AiTrendControl Preload Task Error");
                }
            }, _preloadCts.Token);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                _preloadCts?.Cancel();
            }
            catch { }
            base.OnHandleDestroyed(e);
        }
    }
}
