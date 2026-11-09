using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GateHelper.MysteryTime;

namespace GateHelper.MysteryTime
{
    public partial class MysteryTimeControl : UserControl
    {
        private MysteryCaseRepository _repository;
        private MysteryAiEngine _aiEngine;
        private MysteryCase _currentCase;
        private List<MysteryCase> _filteredCases = new List<MysteryCase>();
        private int _remainingQuestions = 10;
        private int _currentQuestionNumber = 0;
        private bool _isHintRevealed = false;

        public MysteryTimeControl()
        {
            InitializeComponent();
            _repository = new MysteryCaseRepository();
            _aiEngine = new MysteryAiEngine(GetApiKeyFromConfig());
        }

        private string GetApiKeyFromConfig()
        {
            try
            {
                return Util_Option.LoadUserOptions()?.GeminiApiKey ?? "";
            }
            catch
            {
                return "";
            }
        }

        private async void MysteryTimeControl_Load(object sender, EventArgs e)
        {
            InitDifficultyCombo();
            LoadGameCasesCombo();
            ShowLobby();
            SetupVisualPlaceholders();
            await UpdateAiStatusAsync(notifyFail: false);
        }

        private void SetupVisualPlaceholders()
        {
            if (picMainScene != null)
            {
                picMainScene.Paint += (s, pe) =>
                {
                    if (picMainScene.Image == null)
                    {
                        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        using (var brush = new SolidBrush(Color.Gray))
                        using (var font = new Font("맑은 고딕", 9F, FontStyle.Regular))
                        {
                            pe.Graphics.DrawString("[현장 시각 단서]\n(사건 현장)", font, brush, picMainScene.ClientRectangle, sf);
                        }
                    }
                };
            }

            if (picHintScene != null)
            {
                picHintScene.Paint += (s, pe) =>
                {
                    if (picHintScene.Image == null)
                    {
                        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        using (var brush = new SolidBrush(Color.Gray))
                        using (var font = new Font("맑은 고딕", 9F, FontStyle.Regular))
                        {
                            pe.Graphics.DrawString("[핵심 추리 단서]\n(힌트 보기 시 공개)", font, brush, picHintScene.ClientRectangle, sf);
                        }
                    }
                };
            }
        }

        #region [Lobby & Navigation]

        private void ShowLobby()
        {
            pnlLobby.Visible = true;
            pnlGame.Visible = false;
            pnlLobby.BringToFront();
        }

        private void ShowGame(MysteryCase targetCase)
        {
            if (targetCase == null) return;
            pnlLobby.Visible = false;
            pnlGame.Visible = true;
            pnlGame.BringToFront();
            StartNewCase(targetCase);
        }

        private void InitDifficultyCombo()
        {
            cmbDifficulty.Items.Clear();
            cmbDifficulty.Items.Add("전체 난이도");
            cmbDifficulty.Items.Add("쉬움");
            cmbDifficulty.Items.Add("보통");
            cmbDifficulty.Items.Add("어려움");
            cmbDifficulty.SelectedIndex = 0;
        }

        private void cmbDifficulty_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedDiff = cmbDifficulty.SelectedItem?.ToString() ?? "전체 난이도";
            var allCases = _repository.GetAllCases();

            if (selectedDiff == "전체 난이도")
            {
                _filteredCases = allCases.ToList();
            }
            else
            {
                _filteredCases = allCases.Where(c => c.Difficulty == selectedDiff).ToList();
            }

            cmbLobbyCase.Items.Clear();
            cmbLobbyCase.Items.Add("🎲 [랜덤 사건 선택]");
            foreach (var c in _filteredCases)
            {
                string tag = c.IsGeneratedByAi ? "✨AI창작" : "📁오프라인";
                cmbLobbyCase.Items.Add($"[{tag}/{c.Difficulty}] {c.Title} ({c.Category})");
            }

            if (cmbLobbyCase.Items.Count > 0)
            {
                cmbLobbyCase.SelectedIndex = 0;
            }
        }

        private void cmbLobbyCase_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = cmbLobbyCase.SelectedIndex;
            if (idx == 0)
            {
                lblCaseDesc.Text = "난이도 필터 범위 내에서 무작위로 사건을 수사합니다. (📁오프라인 및 ✨AI창작 포함)";
            }
            else if (idx > 0 && idx - 1 < _filteredCases.Count)
            {
                var c = _filteredCases[idx - 1];
                string origin = c.IsGeneratedByAi ? "[✨ Gemini AI 실시간 창작 사건]" : "[📁 오프라인 기본 내장 사건]";
                lblCaseDesc.Text = $"{origin} 카테고리: {c.Category} | {c.Situation}";
            }
        }

        private void btnStartGame_Click(object sender, EventArgs e)
        {
            int idx = cmbLobbyCase.SelectedIndex;
            MysteryCase targetCase = null;

            if (idx == 0) // 랜덤
            {
                var rand = new Random();
                if (_filteredCases.Count > 0)
                {
                    targetCase = _filteredCases[rand.Next(_filteredCases.Count)];
                }
                else
                {
                    targetCase = _repository.GetRandomCase();
                }
            }
            else if (idx > 0 && idx - 1 < _filteredCases.Count)
            {
                targetCase = _filteredCases[idx - 1];
            }

            if (targetCase != null)
            {
                // 게임 화면의 콤보박스에도 동기화
                SyncGameComboSelection(targetCase);
                ShowGame(targetCase);
            }
            else
            {
                MessageBox.Show("수사할 사건을 찾을 수 없습니다.", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void btnAiGenerateCase_Click(object sender, EventArgs e)
        {
            if (_aiEngine == null || !_aiEngine.IsApiKeyConfigured)
            {
                var ask = MessageBox.Show(
                    "AI 사건 창작 기능은 Google Gemini API 온라인 연결이 필요합니다.\n지금 바로 API Key를 등록하시겠습니까?",
                    "API Key 필요",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (ask == DialogResult.Yes)
                {
                    btnSettings_Click(null, null);
                }
                return;
            }

            if (!_aiEngine.IsOnlineVerified)
            {
                await UpdateAiStatusAsync(notifyFail: true);
                if (!_aiEngine.IsOnlineVerified)
                {
                    return;
                }
            }

            // 난이도 선택 팝업 메뉴 표시
            var cms = new ContextMenuStrip();
            cms.Font = new Font("맑은 고딕", 9F, FontStyle.Regular);

            var itemEasy = new ToolStripMenuItem("🌱 [쉬움] 온화한 지능형 가이드 사건 창작");
            itemEasy.Click += async (s, ev) => await ExecuteGenerateCaseAsync("쉬움");

            var itemNormal = new ToolStripMenuItem("⚖️ [보통] 표준 바다거북 스프 사건 창작");
            itemNormal.Click += async (s, ev) => await ExecuteGenerateCaseAsync("보통");

            var itemHard = new ToolStripMenuItem("🔥 [어려움] 단답형 정통 하드보일드 사건 창작");
            itemHard.Click += async (s, ev) => await ExecuteGenerateCaseAsync("어려움");

            cms.Items.Add(itemEasy);
            cms.Items.Add(itemNormal);
            cms.Items.Add(itemHard);

            cms.Show(btnAiGenerateCase, new Point(0, btnAiGenerateCase.Height));
        }

        private async Task ExecuteGenerateCaseAsync(string targetDifficulty)
        {
            try
            {
                btnAiGenerateCase.Enabled = false;
                btnAiGenerateCase.Text = $"⏳ [{targetDifficulty}] 집필 중...";
                Cursor.Current = Cursors.WaitCursor;

                var newCase = await _aiEngine.GenerateNewCaseAsync(targetDifficulty);

                if (newCase != null)
                {
                    _repository.AddCase(newCase);

                    // 전체 난이도 필터이거나 생성된 난이도와 일치하는 경우
                    string currentFilter = cmbDifficulty.SelectedItem?.ToString() ?? "전체 난이도";
                    if (currentFilter == "전체 난이도" || currentFilter == newCase.Difficulty)
                    {
                        _filteredCases.Insert(0, newCase);
                        cmbLobbyCase.Items.Insert(1, $"[✨AI창작/{newCase.Difficulty}] {newCase.Title} ({newCase.Category})");
                        cmbLobbyCase.SelectedIndex = 1;
                    }
                    else
                    {
                        // 해당 난이도로 자동 전환
                        cmbDifficulty.SelectedItem = newCase.Difficulty;
                        if (cmbLobbyCase.Items.Count > 1)
                        {
                            cmbLobbyCase.SelectedIndex = 1;
                        }
                    }

                    LoadGameCasesCombo();

                    MessageBox.Show(
                        $"✨ Gemini AI가 새로운 바다거북 스프 사건을 성공적으로 창작했습니다!\n\n" +
                        $"📌 제목: [{newCase.Difficulty}] {newCase.Title}\n" +
                        $"🏷️ 카테고리: {newCase.Category}\n" +
                        $"📝 현장 상황:\n{newCase.Situation}\n\n" +
                        $"하단의 [🚀 사건 수사 시작] 버튼을 누르면 즉시 수사가 시작됩니다!",
                        "AI 사건 창작 완료",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI 사건 창작 중 오류가 발생했습니다.\n\n오류: {ex.Message}", "창작 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                btnAiGenerateCase.Enabled = true;
                btnAiGenerateCase.Text = "🎲 AI 새 사건 창작 ▾";
                Cursor.Current = Cursors.Default;
            }
        }

        private void btnBackToLobby_Click(object sender, EventArgs e)
        {
            ShowLobby();
        }

        private void btnLobbyBack_Click(object sender, EventArgs e)
        {
            if (this.ParentForm is SandBox sb)
            {
                sb.ShowGameList();
            }
        }

        #endregion

        #region [AI Status & Settings]

        public async Task UpdateAiStatusAsync(bool notifyFail = false)
        {
            if (_aiEngine == null || !_aiEngine.IsApiKeyConfigured)
            {
                SetStatusLabelText("○ 오프라인 모드", Color.Gray);
                return;
            }

            SetStatusLabelText("⏳ 연결 확인 중...", Color.DarkOrange);

            var (success, errorMsg) = await _aiEngine.ValidateConnectionAsync();

            if (success)
            {
                SetStatusLabelText("● AI 온라인", Color.DarkGreen);
            }
            else
            {
                SetStatusLabelText("○ 오프라인 모드", Color.Gray);

                if (notifyFail)
                {
                    MessageBox.Show(
                        $"Gemini API 서버 연결에 실패하여 오프라인 모드로 전환됩니다.\n(사내망/방화벽 차단 또는 유효하지 않은 키)\n\n오류 내용: {errorMsg}",
                        "AI 연결 실패 안내",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        private void SetStatusLabelText(string text, Color color)
        {
            if (lblAiStatus != null)
            {
                lblAiStatus.Text = text;
                lblAiStatus.ForeColor = color;
            }
            if (lblLobbyAiStatus != null)
            {
                lblLobbyAiStatus.Text = text;
                lblLobbyAiStatus.ForeColor = color;
            }
        }

        private async void btnSettings_Click(object sender, EventArgs e)
        {
            string currentKey = GetApiKeyFromConfig();
            using (var form = new MysteryApiKeyForm(currentKey))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _aiEngine = new MysteryAiEngine(form.CurrentApiKey);
                    await UpdateAiStatusAsync(notifyFail: true);
                }
            }
        }

        #endregion

        #region [Game Play Logic]

        public void LoadGameCasesCombo()
        {
            try
            {
                cmbCaseList.Items.Clear();
                var cases = _repository.GetAllCases();
                foreach (var c in cases)
                {
                    string tag = c.IsGeneratedByAi ? "✨AI창작" : "📁오프라인";
                    cmbCaseList.Items.Add($"[{tag}/{c.Difficulty}] {c.Title}");
                }

                if (cmbCaseList.Items.Count > 0)
                {
                    cmbCaseList.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void SyncGameComboSelection(MysteryCase targetCase)
        {
            var cases = _repository.GetAllCases();
            int idx = cases.FindIndex(c => c.Id == targetCase.Id);
            if (idx >= 0 && idx < cmbCaseList.Items.Count)
            {
                cmbCaseList.SelectedIndex = idx;
            }
        }

        private void cmbCaseList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = cmbCaseList.SelectedIndex;
            var cases = _repository.GetAllCases();
            if (idx >= 0 && idx < cases.Count)
            {
                if (_currentCase == null || _currentCase.Id != cases[idx].Id)
                {
                    StartNewCase(cases[idx]);
                }
            }
        }

        public void StartNewCase(MysteryCase targetCase)
        {
            _currentCase = targetCase;
            _remainingQuestions = 10;
            _currentQuestionNumber = 0;
            _isHintRevealed = false;

            if (lblQuestionCount != null)
                lblQuestionCount.Text = $"[ 남은 기회: {_remainingQuestions} / 10 ]";

            if (txtSituation != null)
                txtSituation.Text = _currentCase.Situation;

            if (txtHint != null)
                txtHint.Text = "💡 [힌트 보기]는 질문 5회 이상 진행 후 사용할 수 있습니다.";

            if (rtbChatLog != null)
            {
                rtbChatLog.Clear();
                string modeText = (_aiEngine != null && _aiEngine.IsOnlineVerified) 
                    ? "✨ Gemini AI 실시간 분석 모드 가동 중" 
                    : "📁 오프라인 기본 문제 팩 모드";

                string diffGuide = "";
                if (_currentCase.Difficulty == "어려움")
                {
                    diffGuide = "⚖️ [어려움 모드] 정통 바다거북 스프 룰 적용: AI는 사견이나 힌트 없이 오직 '예.', '아니오.', '사건과 관련 없습니다.', '질문이 모호합니다.' 단답형으로만 엄격하게 응답합니다.";
                }
                else if (_currentCase.Difficulty == "보통")
                {
                    diffGuide = "🔍 [보통 모드] 기본 단답형을 유지하되, 결정적인 핵심 단서에 근접했을 때 가벼운 힌트 피드백이 제공됩니다.";
                }
                else // 쉬움
                {
                    diffGuide = "💡 [쉬움 모드] 지능형 피드백 활성화: 질문의 발상 방향성과 핵심 정답 근접도(🎯/💡/반은 맞고 반은 틀림)에 대한 AI 가이드가 함께 제공됩니다.";
                }

                AppendChatLog("🤖 [AI 진행자]", $"사건 '{_currentCase.Title}' 수사가 시작되었습니다.\n난이도: [{_currentCase.Difficulty}] | ({modeText})\n{diffGuide}\n\n사건 현장을 살펴보고 '예/아니오'로 답변 가능한 질문을 던져보세요!", Color.DarkBlue);
            }

            if (picHintScene != null)
                picHintScene.Image = null;

            if (txtQuestionInput != null)
            {
                txtQuestionInput.Enabled = true;
                txtQuestionInput.ReadOnly = false;
                txtQuestionInput.Clear();
            }

            if (btnSendQuestion != null)
            {
                btnSendQuestion.Enabled = true;
            }
        }

        private void btnGiveUp_Click(object sender, EventArgs e)
        {
            if (_currentCase == null) return;

            var ask = MessageBox.Show(
                "정말 수사를 포기하고 숨겨진 사건의 전말(정답)을 확인하시겠습니까?",
                "수사 포기 및 정답 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (ask == DialogResult.Yes)
            {
                RevealTruth("수사 포기");
            }
        }

        public void RevealTruth(string triggerReason)
        {
            if (_currentCase == null) return;

            _remainingQuestions = 0;
            if (lblQuestionCount != null)
            {
                lblQuestionCount.Text = "[ 수사 종료 ]";
            }

            string separator = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
            string truthLog = 
                $"\n{separator}\n" +
                $"📢 [사건 수사 종료 - 정답 전말 공개 ({triggerReason})]\n" +
                $"📌 사건 제목: [{_currentCase.Difficulty}] {_currentCase.Title} ({_currentCase.Category})\n\n" +
                $"🔍 [숨겨진 사건의 진실과 전말]:\n{_currentCase.Truth}\n\n" +
                $"💡 [핵심 트릭/단서 해설]:\n{_currentCase.HintText}\n" +
                $"{separator}";

            AppendChatLog("🤖 [AI 진행자]", truthLog, Color.Firebrick);

            if (txtQuestionInput != null)
            {
                txtQuestionInput.Text = "수사가 종료되었습니다. [메인으로]를 눌러 다른 사건을 플레이해 보세요.";
                txtQuestionInput.Enabled = false;
            }

            if (btnSendQuestion != null)
            {
                btnSendQuestion.Enabled = false;
            }

            MessageBox.Show(
                $"[사건의 비밀 전말 공개]\n\n" +
                $"📌 사건: {_currentCase.Title}\n\n" +
                $"📜 숨겨진 진실:\n{_currentCase.Truth}\n\n" +
                $"💡 핵심 단서:\n{_currentCase.HintText}",
                "사건의 비밀 전말",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private async void txtQuestionInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // 엔터 경고음 방지
                await ProcessUserQuestionAsync();
            }
        }

        private async void btnSendQuestion_Click(object sender, EventArgs e)
        {
            await ProcessUserQuestionAsync();
        }

        private async Task ProcessUserQuestionAsync()
        {
            if (_currentCase == null) return;

            string questionText = txtQuestionInput.Text.Trim();
            if (string.IsNullOrEmpty(questionText)) return;

            // 포기 및 정답 요청 키워드 감지
            string cleanText = questionText.Replace(" ", "").ToLower();
            if (cleanText.Contains("포기") || cleanText.Contains("기권") || cleanText.Contains("항복") || cleanText.Contains("정답알려줘") || cleanText == "정답" || cleanText == "gg")
            {
                txtQuestionInput.Clear();
                btnGiveUp_Click(null, null);
                return;
            }

            if (_remainingQuestions <= 0)
            {
                var ask = MessageBox.Show(
                    "남은 질문 기회를 모두 소진하셨습니다!\n\n" +
                    "[예]를 누르면 사건의 비밀 전말(정답)을 바로 확인합니다.\n" +
                    "[아니오]를 누르면 마지막으로 [🏆 정답 도전]을 작성해봅니다.",
                    "질문 기회 소진 - 정답 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (ask == DialogResult.Yes)
                {
                    RevealTruth("질문 기회 소진");
                }
                else
                {
                    btnSubmitAnswer_Click(null, null);
                }
                return;
            }

            txtQuestionInput.Clear();
            _currentQuestionNumber++;
            _remainingQuestions--;

            lblQuestionCount.Text = $"[ 남은 기회: {_remainingQuestions} / 10 ]";
            AppendChatLog($"Q{_currentQuestionNumber}. 유저", questionText, Color.Black);

            btnSendQuestion.Enabled = false;
            txtQuestionInput.ReadOnly = true;
            try
            {
                var response = await _aiEngine.EvaluateQuestionAsync(_currentCase, questionText, _currentQuestionNumber);
                
                Color ansColor = Color.DarkGreen;
                if (response.AnswerType == MysteryAnswerType.No) ansColor = Color.DarkRed;
                else if (response.AnswerType == MysteryAnswerType.Ambiguous) ansColor = Color.OrangeRed;
                else if (response.AnswerType == MysteryAnswerType.Irrelevant) ansColor = Color.Gray;

                string senderTitle = (_aiEngine != null && _aiEngine.IsOnlineVerified)
                    ? "🤖 [AI 진행자 (Gemini)]"
                    : "🤖 [AI 진행자 (로컬)]";

                AppendChatLog(senderTitle, response.AnswerMessage, ansColor);

                if (_aiEngine != null && !_aiEngine.IsOnlineVerified)
                {
                    SetStatusLabelText("○ 오프라인 모드", Color.Gray);
                }
            }
            catch (Exception ex)
            {
                AppendChatLog("🤖 [AI 진행자]", $"오류 발생: {ex.Message}", Color.Red);
            }
            finally
            {
                btnSendQuestion.Enabled = true;
                txtQuestionInput.ReadOnly = false;
                txtQuestionInput.Focus();
            }
        }

        private async void btnSubmitAnswer_Click(object sender, EventArgs e)
        {
            if (_currentCase == null) return;

            string solutionText = txtQuestionInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(solutionText))
            {
                MessageBox.Show("하단 질문 입력창에 추리한 사건의 전말(정답)을 적은 후 [🏆 정답 도전] 버튼을 눌러주세요!", "안내", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnSubmitAnswer.Enabled = false;
            try
            {
                var result = await _aiEngine.EvaluateAnswerAsync(_currentCase, solutionText);

                try
                {
                    GateHelper.Dashboard.DashboardManager.RecordMysteryAttempt();
                    if (result.isCorrect)
                    {
                        GateHelper.Dashboard.DashboardManager.RecordMysterySolved();
                    }
                }
                catch { }
                
                string titleStr = result.isCorrect ? "🎉 정답입니다!" : "❌ 아쉬운 오답입니다.";
                MessageBoxIcon iconStr = result.isCorrect ? MessageBoxIcon.Information : MessageBoxIcon.Warning;

                MessageBox.Show(result.feedback, titleStr, MessageBoxButtons.OK, iconStr);

                if (result.isCorrect)
                {
                    AppendChatLog("🏆 [축하합니다!]", $"정답을 맞히셨습니다!\n{result.feedback}", Color.Blue);
                }

                txtQuestionInput.Clear();
            }
            finally
            {
                btnSubmitAnswer.Enabled = true;
            }
        }

        private void btnShowHint_Click(object sender, EventArgs e)
        {
            if (_currentCase == null) return;

            if (_isHintRevealed)
            {
                txtHint.Text = $"💡 [힌트 공개]: {_currentCase.HintText}";
                AppendChatLog("💡 [힌트 공개]", _currentCase.HintText, Color.DarkOrange);
                return;
            }

            if (_currentQuestionNumber < 5)
            {
                MessageBox.Show($"힌트는 질문을 최소 5회 이상 진행한 후 사용할 수 있습니다.\n(현재 진행 질문: {_currentQuestionNumber} / 5회)", "힌트 사용 제한", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_remainingQuestions < 2)
            {
                MessageBox.Show("힌트를 보려면 남은 질문 기회가 최소 2개 이상 필요합니다.", "기회 부족", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _isHintRevealed = true;
            _remainingQuestions -= 2;
            lblQuestionCount.Text = $"[ 남은 기회: {_remainingQuestions} / 10 ]";

            txtHint.Text = $"💡 [힌트 공개]: {_currentCase.HintText}";
            AppendChatLog("💡 [힌트 공개]", _currentCase.HintText, Color.DarkOrange);
        }

        private void AppendChatLog(string senderName, string message, Color color)
        {
            if (rtbChatLog == null) return;

            rtbChatLog.SelectionStart = rtbChatLog.TextLength;
            rtbChatLog.SelectionLength = 0;

            // 메시지 발화자 헤더 (볼드 10pt)
            rtbChatLog.SelectionColor = color;
            rtbChatLog.SelectionFont = new Font("맑은 고딕", 10F, FontStyle.Bold);
            rtbChatLog.AppendText($"{senderName}\n");

            // 메시지 본문 (9.5pt Regular, 앞쪽 들여쓰기 공백)
            rtbChatLog.SelectionColor = Color.FromArgb(45, 52, 58);
            rtbChatLog.SelectionFont = new Font("맑은 고딕", 9.5F, FontStyle.Regular);

            // 각 줄마다 가벼운 들여쓰기 적용
            string[] lines = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                rtbChatLog.AppendText($"   {lines[i]}\n");
            }

            // 질문-답변 턴 구분선 (연한 회색 점선)
            rtbChatLog.SelectionColor = Color.FromArgb(220, 224, 230);
            rtbChatLog.SelectionFont = new Font("맑은 고딕", 8F, FontStyle.Regular);
            rtbChatLog.AppendText("──────────────────────────────────────────\n\n");

            rtbChatLog.ScrollToCaret();
        }

        #endregion
    }
}
