using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.StockAdvisor.Models;
using GateHelper.StockAdvisor.Services;

namespace GateHelper.StockAdvisor
{
    /// <summary>
    /// 증권사(KB H-able, 키움, 미래에셋 등) 엑셀/CSV/클립보드 잔고 데이터를
    /// 지능형 컬럼 자동 매핑 및 네이버 종목코드 자동 변환을 통해 일괄 등록하는 대화상자
    /// </summary>
    public class SmartPortfolioImportDialog : Form
    {
        private Button _btnOpenFile;
        private Button _btnPasteClipboard;
        private Button _btnClear;
        private Label _lblStatus;
        private ProgressBar _progResolve;

        // 지능형 컬럼 매핑 콤보박스
        private ComboBox _cboColName;
        private ComboBox _cboColCode;
        private ComboBox _cboColQty;
        private ComboBox _cboColPrice;

        // 미리보기 리스트뷰
        private ListView _lvPreview;
        private CheckBox _chkSelectAll;
        private Label _lblSelectionSummary;

        // 등록 방식 라디오
        private RadioButton _rbAppendMerge;
        private RadioButton _rbReplaceAll;

        private Button _btnApply;
        private Button _btnCancel;

        // 내부 파싱 상태
        private readonly List<StockHoldingItem> _currentPortfolio;
        private List<string[]> _rawRows = new List<string[]>();
        private string[] _detectedHeaders = new string[0];
        private CancellationTokenSource _ctsResolve;
        private readonly Dictionary<string, string> _nameToCodeCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 최종 등록용 정제 아이템 목록
        /// </summary>
        public List<StockHoldingItem> ResultItems { get; private set; } = new List<StockHoldingItem>();

        /// <summary>
        /// 기존 포트폴리오를 비우고 새로 교체할지 여부
        /// </summary>
        public bool IsReplaceMode => _rbReplaceAll.Checked;

        public SmartPortfolioImportDialog(List<StockHoldingItem> currentPortfolio = null)
        {
            _currentPortfolio = currentPortfolio ?? new List<StockHoldingItem>();

            this.Text = "📂 지능형 계좌 잔고 일괄 가져오기 (Universal Smart Portfolio Importer)";
            this.Size = new Size(950, 680);
            this.MinimumSize = new Size(880, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(18, 22, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Malgun Gothic", 9f);

            RegisterCodePagesEncoding();
            BuildUi();
        }

        private static void RegisterCodePagesEncoding()
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch { }
        }

        #region UI Layout Construction

        private void BuildUi()
        {
            this.SuspendLayout();

            // 상단 툴바 패널
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(24, 28, 40),
                Padding = new Padding(12, 10, 12, 10)
            };

            _btnOpenFile = CreateButton("📂 CSV / TXT 파일 열기", Color.FromArgb(20, 48, 70), Color.FromArgb(0, 216, 255));
            _btnOpenFile.Width = 170;
            _btnOpenFile.Dock = DockStyle.Left;
            _btnOpenFile.Click += (s, e) => OpenDataFile();

            _btnPasteClipboard = CreateButton("📋 클립보드 붙여넣기 (엑셀 복사)", Color.FromArgb(20, 52, 40), Color.FromArgb(0, 230, 118));
            _btnPasteClipboard.Width = 210;
            _btnPasteClipboard.Dock = DockStyle.Left;
            _btnPasteClipboard.Margin = new Padding(8, 0, 0, 0);
            _btnPasteClipboard.Click += (s, e) => PasteFromClipboard();

            _btnClear = CreateButton("초기화", Color.FromArgb(40, 44, 56), Color.FromArgb(180, 190, 210));
            _btnClear.Width = 75;
            _btnClear.Dock = DockStyle.Left;
            _btnClear.Click += (s, e) => ClearData();

            _lblStatus = new Label
            {
                Text = "💡 증권사 HTS의 잔고 내역을 복사(Ctrl+C)한 후 '클립보드 붙여넣기'를 누르거나 CSV 파일을 여세요.",
                ForeColor = Color.FromArgb(170, 185, 210),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 0, 0, 0)
            };

            pnlTop.Controls.Add(_lblStatus);
            pnlTop.Controls.Add(_btnClear);
            pnlTop.Controls.Add(_btnPasteClipboard);
            pnlTop.Controls.Add(_btnOpenFile);

            // 지능형 컬럼 매핑 바 (헤더 감지 및 사용자 매핑 지정)
            var pnlMapping = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(28, 34, 48),
                Padding = new Padding(12, 8, 12, 8)
            };

            var tblMapping = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tblMapping.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMapping.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMapping.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMapping.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblMapping.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
            tblMapping.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblColName = new Label { Text = "① 종목명 컬럼 (필수)", ForeColor = Color.FromArgb(0, 230, 255), Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold), Dock = DockStyle.Fill };
            var lblColCode = new Label { Text = "② 종목코드 컬럼 (선택)", ForeColor = Color.FromArgb(200, 210, 225), Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold), Dock = DockStyle.Fill };
            var lblColQty = new Label { Text = "③ 보유수량 컬럼 (필수)", ForeColor = Color.FromArgb(0, 230, 118), Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold), Dock = DockStyle.Fill };
            var lblColPrice = new Label { Text = "④ 매입단가 컬럼 (필수)", ForeColor = Color.FromArgb(255, 190, 60), Font = new Font("Malgun Gothic", 8.5f, FontStyle.Bold), Dock = DockStyle.Fill };

            _cboColName = CreateMappingComboBox();
            _cboColCode = CreateMappingComboBox();
            _cboColQty = CreateMappingComboBox();
            _cboColPrice = CreateMappingComboBox();

            _cboColName.SelectedIndexChanged += (s, e) => RebuildPreviewList();
            _cboColCode.SelectedIndexChanged += (s, e) => RebuildPreviewList();
            _cboColQty.SelectedIndexChanged += (s, e) => RebuildPreviewList();
            _cboColPrice.SelectedIndexChanged += (s, e) => RebuildPreviewList();

            tblMapping.Controls.Add(lblColName, 0, 0);
            tblMapping.Controls.Add(lblColCode, 1, 0);
            tblMapping.Controls.Add(lblColQty, 2, 0);
            tblMapping.Controls.Add(lblColPrice, 3, 0);

            tblMapping.Controls.Add(_cboColName, 0, 1);
            tblMapping.Controls.Add(_cboColCode, 1, 1);
            tblMapping.Controls.Add(_cboColQty, 2, 1);
            tblMapping.Controls.Add(_cboColPrice, 3, 1);

            pnlMapping.Controls.Add(tblMapping);

            // 중간 리스트뷰 영역 (체크박스 목록)
            var pnlCenter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 8, 12, 8)
            };

            var pnlListHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(2, 4, 2, 4)
            };

            _chkSelectAll = new CheckBox
            {
                Text = "전체 선택 / 해제",
                Checked = true,
                AutoSize = true,
                Dock = DockStyle.Left,
                ForeColor = Color.FromArgb(210, 220, 240),
                Cursor = Cursors.Hand
            };
            _chkSelectAll.CheckedChanged += (s, e) =>
            {
                bool chk = _chkSelectAll.Checked;
                foreach (ListViewItem item in _lvPreview.Items)
                {
                    item.Checked = chk;
                }
                UpdateSelectionSummary();
            };

            _lblSelectionSummary = new Label
            {
                Text = "선택: 0개 종목 | 매입총액: 0원",
                ForeColor = Color.FromArgb(0, 216, 255),
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Right,
                AutoSize = true
            };

            _progResolve = new ProgressBar
            {
                Visible = false,
                Height = 16,
                Width = 160,
                Dock = DockStyle.Right,
                Style = ProgressBarStyle.Continuous
            };

            pnlListHeader.Controls.Add(_progResolve);
            pnlListHeader.Controls.Add(_lblSelectionSummary);
            pnlListHeader.Controls.Add(_chkSelectAll);

            _lvPreview = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true,
                GridLines = true,
                BackColor = Color.FromArgb(14, 18, 26),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            _lvPreview.Columns.Add("선택/상태", 100);
            _lvPreview.Columns.Add("종목명", 180);
            _lvPreview.Columns.Add("종목코드", 100);
            _lvPreview.Columns.Add("보유수량", 90, HorizontalAlignment.Right);
            _lvPreview.Columns.Add("평균매입단가", 120, HorizontalAlignment.Right);
            _lvPreview.Columns.Add("매입총액", 140, HorizontalAlignment.Right);
            _lvPreview.Columns.Add("자동 매핑 안내", 170);

            _lvPreview.ItemChecked += (s, e) => UpdateSelectionSummary();

            pnlCenter.Controls.Add(_lvPreview);
            pnlCenter.Controls.Add(pnlListHeader);

            // 하단 액션 패널
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 74,
                BackColor = Color.FromArgb(24, 28, 40),
                Padding = new Padding(14, 8, 14, 8)
            };

            var tblRadio = new TableLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 480,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tblRadio.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            tblRadio.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));

            _rbAppendMerge = new RadioButton
            {
                Text = "기존 포트폴리오에 추가 (중복 시 수량/단가 갱신)",
                Checked = true,
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 230, 245),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 2),
                Font = new Font("Malgun Gothic", 9f)
            };

            _rbReplaceAll = new RadioButton
            {
                Text = "기존 포트폴리오 비우고 새로 덮어쓰기 (전체 교체)",
                Checked = false,
                AutoSize = true,
                ForeColor = Color.FromArgb(255, 170, 170),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 2),
                Font = new Font("Malgun Gothic", 9f)
            };

            tblRadio.Controls.Add(_rbAppendMerge, 0, 0);
            tblRadio.Controls.Add(_rbReplaceAll, 0, 1);

            var pnlButtons = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320
            };

            _btnApply = CreateButton("다음: 최종 표 미리보기 ▶ (0개)", Color.FromArgb(14, 52, 42), Color.FromArgb(0, 230, 118));
            _btnApply.Font = new Font("Malgun Gothic", 9.5f, FontStyle.Bold);
            _btnApply.Dock = DockStyle.Fill;
            _btnApply.Click += (s, e) => ApplySelectedStocks();

            _btnCancel = CreateButton("취소", Color.FromArgb(40, 44, 56), Color.FromArgb(190, 200, 215));
            _btnCancel.Width = 80;
            _btnCancel.Dock = DockStyle.Right;
            _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pnlButtons.Controls.Add(_btnApply);
            pnlButtons.Controls.Add(_btnCancel);

            pnlBottom.Controls.Add(pnlButtons);
            pnlBottom.Controls.Add(tblRadio);

            // 전체 폼에 추가
            this.Controls.Add(pnlCenter);
            this.Controls.Add(pnlMapping);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlBottom);

            this.ResumeLayout(false);
        }

        private static ComboBox CreateMappingComboBox()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(36, 44, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
        }

        private static Button CreateButton(string text, Color backColor, Color foreColor)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Malgun Gothic", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 4, 0)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(Math.Min(255, backColor.R + 40), Math.Min(255, backColor.G + 40), Math.Min(255, backColor.B + 40));
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        #endregion

        #region File & Clipboard Data Parsing

        private void OpenDataFile()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "증권사 잔고 내역 파일 열기 (CSV / TXT / TSV)";
                ofd.Filter = "잔고 데이터 파일 (*.csv;*.txt;*.tsv)|*.csv;*.txt;*.tsv|모든 파일 (*.*)|*.*";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        string content = ReadTextWithAutoEncoding(ofd.FileName);
                        ParseRawContent(content, Path.GetFileName(ofd.FileName));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"파일 열기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void PasteFromClipboard()
        {
            try
            {
                if (!Clipboard.ContainsText())
                {
                    MessageBox.Show(this, "클립보드에 복사된 텍스트 데이터가 없습니다.\n증권사 HTS나 엑셀에서 표 영역을 블록 지정 후 복사(Ctrl+C)해주세요.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string clipText = Clipboard.GetText();
                ParseRawContent(clipText, "클립보드 데이터");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"클립보드 읽기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearData()
        {
            _rawRows.Clear();
            _detectedHeaders = new string[0];
            _cboColName.Items.Clear();
            _cboColCode.Items.Clear();
            _cboColQty.Items.Clear();
            _cboColPrice.Items.Clear();
            _lvPreview.Items.Clear();
            _lblStatus.Text = "💡 데이터가 초기화되었습니다.";
            UpdateSelectionSummary();
        }

        /// <summary>
        /// 한국 증권사 환경(CP949/EUC-KR) 및 UTF-8 자동 판별 리더
        /// </summary>
        private static string ReadTextWithAutoEncoding(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);

            // 1. UTF-8 BOM 체크
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            }

            // 2. UTF-8 유효성 검사
            try
            {
                var utf8Strict = new UTF8Encoding(false, true);
                return utf8Strict.GetString(bytes);
            }
            catch
            {
                // UTF-8 파싱 실패 시 한국어 기본인 CP949(EUC-KR)로 디코딩
                try
                {
                    Encoding cp949 = Encoding.GetEncoding(949);
                    return cp949.GetString(bytes);
                }
                catch
                {
                    return Encoding.Default.GetString(bytes);
                }
            }
        }

        /// <summary>
        /// CSV, TSV 또는 엑셀 복사 문자열을 행/열 단위로 분해하고 헤더를 자동 판별
        /// </summary>
        private void ParseRawContent(string rawText, string sourceName)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                MessageBox.Show(this, "불러온 내용이 비어있습니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var lines = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return;

            // 구분자 감지 (탭 또는 쉼표)
            char delimiter = DetectDelimiter(lines);

            _rawRows.Clear();
            int headerIndex = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                var cols = SplitCsvRow(lines[i], delimiter);
                if (cols.Count == 0) continue;

                // 헤더 행 감지 (종목명, 보유잔고, 평균단가 등의 키워드가 포함된 행)
                if (headerIndex == -1 && cols.Any(c => MatchesKeyword(c, "종목", "종목명", "잔고", "수량", "단가", "평균단가")))
                {
                    headerIndex = i;
                    _detectedHeaders = cols.ToArray();
                    continue;
                }

                if (headerIndex != -1)
                {
                    _rawRows.Add(cols.ToArray());
                }
                else if (i < 5)
                {
                    // 아직 헤더를 못 찾았으면 후보로 보관
                    _rawRows.Add(cols.ToArray());
                }
            }

            // 만약 명시적 헤더를 못 찾았다면 첫 행을 임의 헤더로 채택
            if (headerIndex == -1 && _rawRows.Count > 0)
            {
                _detectedHeaders = _rawRows[0];
                _rawRows.RemoveAt(0);
            }

            if (_detectedHeaders == null || _detectedHeaders.Length == 0)
            {
                MessageBox.Show(this, "유효한 표 형식의 데이터를 찾지 못했습니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _lblStatus.Text = $"✅ [{sourceName}] 로드 완료: {_rawRows.Count}개 행 발견 (구분자: '{(delimiter == '\t' ? "탭" : "쉼표")}')";

            SetupMappingDropdowns();
            RebuildPreviewList();
        }

        private static char DetectDelimiter(string[] lines)
        {
            int tabCount = 0;
            int commaCount = 0;
            for (int i = 0; i < Math.Min(lines.Length, 10); i++)
            {
                tabCount += lines[i].Count(c => c == '\t');
                commaCount += lines[i].Count(c => c == ',');
            }
            return (tabCount >= commaCount && tabCount > 0) ? '\t' : ',';
        }

        private static List<string> SplitCsvRow(string line, char delimiter)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(line)) return result;

            if (delimiter == '\t')
            {
                // 탭 구분은 단순 split
                var parts = line.Split('\t');
                foreach (var p in parts) result.Add(p.Trim().Trim('"', '\''));
                return result;
            }

            // CSV 쉼표 구분: 따옴표 내 쉼표 처리
            bool inQuotes = false;
            var sb = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == delimiter && !inQuotes)
                {
                    result.Add(sb.ToString().Trim().Trim('"', '\''));
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString().Trim().Trim('"', '\''));
            return result;
        }

        #endregion

        #region Smart Column Mapping Logic

        private void SetupMappingDropdowns()
        {
            _cboColName.Items.Clear();
            _cboColCode.Items.Clear();
            _cboColQty.Items.Clear();
            _cboColPrice.Items.Clear();

            _cboColCode.Items.Add("(없음 - 종목명으로 자동 검색)");

            for (int i = 0; i < _detectedHeaders.Length; i++)
            {
                string h = _detectedHeaders[i].Trim();
                if (string.IsNullOrEmpty(h)) h = $"[열 {i + 1}]";
                string itemText = $"{i + 1}. {h}";

                _cboColName.Items.Add(itemText);
                _cboColCode.Items.Add(itemText);
                _cboColQty.Items.Add(itemText);
                _cboColPrice.Items.Add(itemText);
            }

            // 지능형 자동 추론
            _cboColName.SelectedIndex = FindBestHeaderIndex(new[] { "종목명", "종목", "종목(한글)", "name", "item", "stock" }, 0);
            
            int codeIndex = FindBestHeaderIndex(new[] { "종목코드", "단축코드", "코드", "code", "symbol", "ticker" }, -1);
            if (codeIndex >= 0)
            {
                _cboColCode.SelectedIndex = codeIndex + 1; // 0번이 '(없음)' 이므로 +1
            }
            else
            {
                _cboColCode.SelectedIndex = 0; // 자동 검색 모드
            }

            _cboColQty.SelectedIndex = FindBestHeaderIndex(new[] { "보유잔고", "보유수량", "수량", "잔고", "보유", "매도가능", "qty", "quantity", "balance" }, Math.Min(1, _detectedHeaders.Length - 1));
            _cboColPrice.SelectedIndex = FindBestHeaderIndex(new[] { "평균단가", "매입단가", "매입평균가", "매입가", "단가", "매수가", "avgprice", "buyprice", "cost" }, Math.Min(2, _detectedHeaders.Length - 1));
        }

        private int FindBestHeaderIndex(string[] keywords, int fallbackIndex)
        {
            // 1단계: 완전 일치 우선 탐색
            for (int i = 0; i < _detectedHeaders.Length; i++)
            {
                string cleanCol = Regex.Replace(_detectedHeaders[i], @"\s+", "").ToLowerInvariant();
                foreach (var kw in keywords)
                {
                    if (cleanCol == kw.ToLowerInvariant()) return i;
                }
            }

            // 2단계: 부분 일치 탐색 (단, 종목명과 종목코드가 서로 간섭하지 않도록 방어)
            for (int i = 0; i < _detectedHeaders.Length; i++)
            {
                string cleanCol = Regex.Replace(_detectedHeaders[i], @"\s+", "").ToLowerInvariant();
                foreach (var kw in keywords)
                {
                    string cleanKw = kw.ToLowerInvariant();
                    if (cleanCol.Contains(cleanKw))
                    {
                        // '종목' 검색 시 '종목코드'는 배제
                        if (cleanKw == "종목" && cleanCol.Contains("코드")) continue;
                        // '코드' 검색 시 '종목명'은 배제
                        if (cleanKw == "코드" && cleanCol.Contains("종목명")) continue;

                        return i;
                    }
                }
            }

            return (fallbackIndex >= 0 && fallbackIndex < _detectedHeaders.Length) ? fallbackIndex : -1;
        }

        private static bool MatchesKeyword(string text, params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string clean = Regex.Replace(text, @"\s+", "").ToLowerInvariant();
            foreach (var kw in keywords)
            {
                string cleanKw = kw.ToLowerInvariant();
                if (clean == cleanKw || clean.Contains(cleanKw)) return true;
            }
            return false;
        }

        #endregion

        #region Preview & Async Code Resolution

        private void RebuildPreviewList()
        {
            _ctsResolve?.Cancel();
            _ctsResolve = new CancellationTokenSource();

            _lvPreview.BeginUpdate();
            _lvPreview.Items.Clear();

            int idxName = _cboColName.SelectedIndex;
            int idxCodeSelected = _cboColCode.SelectedIndex; // 0이면 없음
            int idxQty = _cboColQty.SelectedIndex;
            int idxPrice = _cboColPrice.SelectedIndex;

            if (idxName < 0 || idxQty < 0 || idxPrice < 0)
            {
                _lvPreview.EndUpdate();
                return;
            }

            int idxCode = idxCodeSelected > 0 ? idxCodeSelected - 1 : -1;

            var itemsToResolve = new List<ListViewItem>();

            foreach (var row in _rawRows)
            {
                string rawName = GetColValue(row, idxName);
                if (string.IsNullOrWhiteSpace(rawName)) continue;

                // 머리글 행이 데이터에 섞여 들어간 경우 건너뜀
                if (MatchesKeyword(rawName, "종목명", "합계", "소계", "총계", "평가손익")) continue;

                string rawCode = idxCode >= 0 ? GetColValue(row, idxCode) : string.Empty;
                string rawQty = GetColValue(row, idxQty);
                string rawPrice = GetColValue(row, idxPrice);

                int qty = ParseInt(rawQty);
                decimal price = ParseDecimal(rawPrice);
                decimal total = qty * price;

                // 코드가 순수 6자리 숫자인지 검사
                string standardCode = CleanStockCode(rawCode);

                var lvi = new ListViewItem(string.IsNullOrEmpty(standardCode) ? "🟡 검색예정" : "🟢 코드확인");
                lvi.SubItems.Add(rawName);
                lvi.SubItems.Add(string.IsNullOrEmpty(standardCode) ? "..." : standardCode);
                lvi.SubItems.Add(qty.ToString("N0") + "주");
                lvi.SubItems.Add(price.ToString("N0") + "원");
                lvi.SubItems.Add(total.ToString("N0") + "원");
                lvi.SubItems.Add(string.IsNullOrEmpty(standardCode) ? "네이버 증권 검색 대기" : "정상 매핑");

                // 수량이 0인 행은 기본 체크 해제, 1 이상이면 체크
                lvi.Checked = (qty > 0);

                lvi.Tag = new StockHoldingItem
                {
                    StockName = rawName,
                    StockCode = standardCode,
                    Quantity = qty,
                    BuyPrice = price
                };

                _lvPreview.Items.Add(lvi);

                if (string.IsNullOrEmpty(standardCode))
                {
                    itemsToResolve.Add(lvi);
                }
            }

            _lvPreview.EndUpdate();
            UpdateSelectionSummary();

            // 코드가 비어있는 종목들에 대해 네이버 비동기 검색 자동 발굴 시작
            if (itemsToResolve.Count > 0)
            {
                _ = ResolveMissingCodesAsync(itemsToResolve, _ctsResolve.Token);
            }
        }

        private static string GetColValue(string[] row, int index)
        {
            if (row == null || index < 0 || index >= row.Length) return string.Empty;
            return row[index]?.Trim() ?? string.Empty;
        }

        private static string CleanStockCode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string digits = Regex.Replace(raw, @"[^\dA-Za-z]", "");
            if (digits.Length == 6 && Regex.IsMatch(digits, @"^\d+$")) return digits;
            if (Regex.IsMatch(digits, @"^[A-Za-z]{1,5}$")) return digits.ToUpperInvariant(); // 해외 티커
            return string.Empty;
        }

        private static int ParseInt(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0;
            string clean = Regex.Replace(raw, @"[^\d-]", "");
            int.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out int v);
            return Math.Max(0, v);
        }

        private static decimal ParseDecimal(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0m;
            string clean = Regex.Replace(raw, @"[^\d.-]", "");
            decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal v);
            return Math.Max(0m, v);
        }

        /// <summary>
        /// 네이버 증권 검색 API를 통해 종목명 -> 6자리 종목코드 자동 비동기 매핑
        /// </summary>
        private async Task ResolveMissingCodesAsync(List<ListViewItem> items, CancellationToken ct)
        {
            _progResolve.Visible = true;
            _progResolve.Maximum = items.Count;
            _progResolve.Value = 0;

            int resolvedCount = 0;

            for (int i = 0; i < items.Count; i++)
            {
                if (ct.IsCancellationRequested) break;

                var lvi = items[i];
                if (!(lvi.Tag is StockHoldingItem target)) continue;

                string query = target.StockName;

                // 캐시 확인
                string matchedCode = null;
                lock (_nameToCodeCache)
                {
                    _nameToCodeCache.TryGetValue(query, out matchedCode);
                }

                if (string.IsNullOrEmpty(matchedCode))
                {
                    try
                    {
                        var searchList = await NaverStockPriceService.SearchStockAsync(query).ConfigureAwait(true);
                        if (searchList != null && searchList.Count > 0)
                        {
                            // 가장 이름이 유사한 종목 매칭
                            var best = searchList.FirstOrDefault(x => string.Equals(x.Name, query, StringComparison.OrdinalIgnoreCase))
                                       ?? searchList[0];
                            matchedCode = best.Code;

                            lock (_nameToCodeCache)
                            {
                                _nameToCodeCache[query] = matchedCode;
                            }
                        }
                    }
                    catch { }
                }

                if (ct.IsCancellationRequested) break;

                if (!string.IsNullOrEmpty(matchedCode))
                {
                    target.StockCode = matchedCode;
                    lvi.SubItems[0].Text = "🟢 매핑성공";
                    lvi.SubItems[2].Text = matchedCode;
                    lvi.SubItems[6].Text = $"네이버 매칭 ({matchedCode})";
                    lvi.ForeColor = Color.FromArgb(220, 255, 230);
                    resolvedCount++;
                }
                else
                {
                    lvi.SubItems[0].Text = "🔴 미확인";
                    lvi.SubItems[6].Text = "종목코드 미발견 (수동 등록 권장)";
                    lvi.ForeColor = Color.FromArgb(255, 180, 180);
                }

                _progResolve.Value = Math.Min(_progResolve.Maximum, i + 1);
            }

            _progResolve.Visible = false;
            _lblStatus.Text = $"✨ 종목코드 자동 매핑 완료: 총 {items.Count}개 중 {resolvedCount}개 자동 확인";
            UpdateSelectionSummary();
        }

        private void UpdateSelectionSummary()
        {
            int selectedCount = 0;
            decimal totalAmount = 0m;

            foreach (ListViewItem item in _lvPreview.Items)
            {
                if (item.Checked && item.Tag is StockHoldingItem dto)
                {
                    selectedCount++;
                    totalAmount += (dto.Quantity * dto.BuyPrice);
                }
            }

            _lblSelectionSummary.Text = $"선택: {selectedCount:N0}개 종목 | 매입총액: {totalAmount:N0}원";
            _btnApply.Text = $"다음: 최종 표 미리보기 ▶ ({selectedCount}개)";
            _btnApply.Enabled = (selectedCount > 0);
        }

        #endregion

        #region Apply Selection & Close

        private void ApplySelectedStocks()
        {
            ResultItems.Clear();

            int invalidCount = 0;

            foreach (ListViewItem item in _lvPreview.Items)
            {
                if (item.Checked && item.Tag is StockHoldingItem dto)
                {
                    if (string.IsNullOrWhiteSpace(dto.StockCode))
                    {
                        invalidCount++;
                        continue;
                    }

                    ResultItems.Add(new StockHoldingItem
                    {
                        StockCode = dto.StockCode,
                        StockName = dto.StockName,
                        Quantity = dto.Quantity,
                        BuyPrice = dto.BuyPrice,
                        Memo = $"HTS 잔고 일괄 가져오기 ({DateTime.Now:yyyy-MM-dd})"
                    });
                }
            }

            if (ResultItems.Count == 0)
            {
                MessageBox.Show(this, "등록할 수 있는 유효한 종목코드가 없습니다.\n종목코드 자동 매핑 상태를 확인해주세요.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (invalidCount > 0)
            {
                var dr = MessageBox.Show(this, $"종목코드를 확인하지 못한 {invalidCount}개 종목은 제외하고,\n유효한 {ResultItems.Count}개 종목만 미리보기를 진행하시겠습니까?", "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr != DialogResult.Yes) return;
            }

            // 2단계: 최종 표 반영 시뮬레이션 미리보기 팝업 출력!
            using (var previewDlg = new PortfolioImportPreviewDialog(_currentPortfolio, ResultItems, IsReplaceMode))
            {
                var pr = previewDlg.ShowDialog(this);
                if (pr == DialogResult.OK && previewDlg.IsConfirmed)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                // 만약 취소/이전 버튼을 누르면 아무것도 하지 않고 현재 화면(SmartPortfolioImportDialog)에 그대로 머무름
            }
        }

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _ctsResolve?.Cancel();
            base.OnFormClosing(e);
        }
    }
}
