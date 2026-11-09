namespace GateHelper.MysteryTime
{
    partial class MysteryTimeControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.pnlLobby = new System.Windows.Forms.Panel();
            this.lblLobbyTitle = new System.Windows.Forms.Label();
            this.lblLobbySub = new System.Windows.Forms.Label();
            this.lblDifficulty = new System.Windows.Forms.Label();
            this.cmbDifficulty = new System.Windows.Forms.ComboBox();
            this.btnAiGenerateCase = new System.Windows.Forms.Button();
            this.lblLobbyCase = new System.Windows.Forms.Label();
            this.cmbLobbyCase = new System.Windows.Forms.ComboBox();
            this.lblCaseDesc = new System.Windows.Forms.Label();
            this.lblLobbyAiStatus = new System.Windows.Forms.Label();
            this.btnLobbySettings = new System.Windows.Forms.Button();
            this.btnStartGame = new System.Windows.Forms.Button();
            this.btnLobbyBack = new System.Windows.Forms.Button();
            this.pnlGame = new System.Windows.Forms.Panel();
            this.cmbCaseList = new System.Windows.Forms.ComboBox();
            this.lblQuestionCount = new System.Windows.Forms.Label();
            this.lblAiStatus = new System.Windows.Forms.Label();
            this.btnGiveUp = new System.Windows.Forms.Button();
            this.btnBackToLobby = new System.Windows.Forms.Button();
            this.txtSituation = new System.Windows.Forms.TextBox();
            this.txtHint = new System.Windows.Forms.TextBox();
            this.rtbChatLog = new System.Windows.Forms.RichTextBox();
            this.txtQuestionInput = new System.Windows.Forms.TextBox();
            this.btnSendQuestion = new System.Windows.Forms.Button();
            this.btnShowHint = new System.Windows.Forms.Button();
            this.btnSubmitAnswer = new System.Windows.Forms.Button();
            this.picMainScene = new System.Windows.Forms.PictureBox();
            this.picHintScene = new System.Windows.Forms.PictureBox();
            this.pnlLobby.SuspendLayout();
            this.pnlGame.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMainScene)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picHintScene)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlLobby
            // 
            this.pnlLobby.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pnlLobby.Controls.Add(this.btnLobbyBack);
            this.pnlLobby.Controls.Add(this.btnStartGame);
            this.pnlLobby.Controls.Add(this.btnLobbySettings);
            this.pnlLobby.Controls.Add(this.lblLobbyAiStatus);
            this.pnlLobby.Controls.Add(this.lblCaseDesc);
            this.pnlLobby.Controls.Add(this.cmbLobbyCase);
            this.pnlLobby.Controls.Add(this.lblLobbyCase);
            this.pnlLobby.Controls.Add(this.btnAiGenerateCase);
            this.pnlLobby.Controls.Add(this.cmbDifficulty);
            this.pnlLobby.Controls.Add(this.lblDifficulty);
            this.pnlLobby.Controls.Add(this.lblLobbySub);
            this.pnlLobby.Controls.Add(this.lblLobbyTitle);
            this.pnlLobby.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlLobby.Location = new System.Drawing.Point(0, 0);
            this.pnlLobby.Name = "pnlLobby";
            this.pnlLobby.Size = new System.Drawing.Size(615, 425);
            this.pnlLobby.TabIndex = 0;
            // 
            // lblLobbyTitle
            // 
            this.lblLobbyTitle.Font = new System.Drawing.Font("맑은 고딕", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblLobbyTitle.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lblLobbyTitle.Location = new System.Drawing.Point(0, 25);
            this.lblLobbyTitle.Name = "lblLobbyTitle";
            this.lblLobbyTitle.Size = new System.Drawing.Size(615, 35);
            this.lblLobbyTitle.TabIndex = 0;
            this.lblLobbyTitle.Text = "🔍 미스터리 타임 (Mystery Time)";
            this.lblLobbyTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLobbySub
            // 
            this.lblLobbySub.Font = new System.Drawing.Font("맑은 고딕", 9.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblLobbySub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblLobbySub.Location = new System.Drawing.Point(15, 63);
            this.lblLobbySub.Name = "lblLobbySub";
            this.lblLobbySub.Size = new System.Drawing.Size(585, 36);
            this.lblLobbySub.TabIndex = 1;
            this.lblLobbySub.Text = "바다거북 스프 수수께끼 추리 게임\r\n진행자에게 \'예/아니오\'로 답변 가능한 질문을 던져 숨겨진 진실을 밝혀내세요!";
            this.lblLobbySub.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblDifficulty
            // 
            this.lblDifficulty.AutoSize = true;
            this.lblDifficulty.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblDifficulty.Location = new System.Drawing.Point(90, 122);
            this.lblDifficulty.Name = "lblDifficulty";
            this.lblDifficulty.Size = new System.Drawing.Size(78, 17);
            this.lblDifficulty.TabIndex = 2;
            this.lblDifficulty.Text = "난이도 필터:";
            // 
            // cmbDifficulty
            // 
            this.cmbDifficulty.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDifficulty.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.cmbDifficulty.FormattingEnabled = true;
            this.cmbDifficulty.Location = new System.Drawing.Point(175, 119);
            this.cmbDifficulty.Name = "cmbDifficulty";
            this.cmbDifficulty.Size = new System.Drawing.Size(115, 23);
            this.cmbDifficulty.TabIndex = 3;
            this.cmbDifficulty.SelectedIndexChanged += new System.EventHandler(this.cmbDifficulty_SelectedIndexChanged);
            // 
            // btnAiGenerateCase
            // 
            this.btnAiGenerateCase.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnAiGenerateCase.Location = new System.Drawing.Point(300, 117);
            this.btnAiGenerateCase.Name = "btnAiGenerateCase";
            this.btnAiGenerateCase.Size = new System.Drawing.Size(155, 26);
            this.btnAiGenerateCase.TabIndex = 11;
            this.btnAiGenerateCase.Text = "🎲 AI 새 사건 창작 ▾";
            this.btnAiGenerateCase.UseVisualStyleBackColor = true;
            this.btnAiGenerateCase.Click += new System.EventHandler(this.btnAiGenerateCase_Click);
            // 
            // lblLobbyCase
            // 
            this.lblLobbyCase.AutoSize = true;
            this.lblLobbyCase.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblLobbyCase.Location = new System.Drawing.Point(30, 160);
            this.lblLobbyCase.Name = "lblLobbyCase";
            this.lblLobbyCase.Size = new System.Drawing.Size(138, 17);
            this.lblLobbyCase.TabIndex = 4;
            this.lblLobbyCase.Text = "사건 선택 (📁오프라인):";
            // 
            // cmbLobbyCase
            // 
            this.cmbLobbyCase.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLobbyCase.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.cmbLobbyCase.FormattingEnabled = true;
            this.cmbLobbyCase.Location = new System.Drawing.Point(175, 157);
            this.cmbLobbyCase.Name = "cmbLobbyCase";
            this.cmbLobbyCase.Size = new System.Drawing.Size(340, 23);
            this.cmbLobbyCase.TabIndex = 5;
            this.cmbLobbyCase.SelectedIndexChanged += new System.EventHandler(this.cmbLobbyCase_SelectedIndexChanged);
            // 
            // lblCaseDesc
            // 
            this.lblCaseDesc.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblCaseDesc.ForeColor = System.Drawing.Color.DimGray;
            this.lblCaseDesc.Location = new System.Drawing.Point(175, 187);
            this.lblCaseDesc.Name = "lblCaseDesc";
            this.lblCaseDesc.Size = new System.Drawing.Size(340, 32);
            this.lblCaseDesc.TabIndex = 6;
            this.lblCaseDesc.Text = "사건 정보가 표시됩니다.";
            // 
            // lblLobbyAiStatus
            // 
            this.lblLobbyAiStatus.AutoSize = true;
            this.lblLobbyAiStatus.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblLobbyAiStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblLobbyAiStatus.Location = new System.Drawing.Point(175, 230);
            this.lblLobbyAiStatus.Name = "lblLobbyAiStatus";
            this.lblLobbyAiStatus.Size = new System.Drawing.Size(103, 17);
            this.lblLobbyAiStatus.TabIndex = 7;
            this.lblLobbyAiStatus.Text = "○ 오프라인 모드";
            // 
            // btnLobbySettings
            // 
            this.btnLobbySettings.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnLobbySettings.Location = new System.Drawing.Point(310, 224);
            this.btnLobbySettings.Name = "btnLobbySettings";
            this.btnLobbySettings.Size = new System.Drawing.Size(140, 28);
            this.btnLobbySettings.TabIndex = 8;
            this.btnLobbySettings.Text = "⚙ Gemini API 설정";
            this.btnLobbySettings.UseVisualStyleBackColor = true;
            this.btnLobbySettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // btnStartGame
            // 
            this.btnStartGame.BackColor = System.Drawing.Color.MidnightBlue;
            this.btnStartGame.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartGame.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnStartGame.ForeColor = System.Drawing.Color.White;
            this.btnStartGame.Location = new System.Drawing.Point(175, 275);
            this.btnStartGame.Name = "btnStartGame";
            this.btnStartGame.Size = new System.Drawing.Size(260, 44);
            this.btnStartGame.TabIndex = 9;
            this.btnStartGame.Text = "🚀 사건 수사 시작";
            this.btnStartGame.UseVisualStyleBackColor = false;
            this.btnStartGame.Click += new System.EventHandler(this.btnStartGame_Click);
            // 
            // btnLobbyBack
            // 
            this.btnLobbyBack.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnLobbyBack.Location = new System.Drawing.Point(235, 335);
            this.btnLobbyBack.Name = "btnLobbyBack";
            this.btnLobbyBack.Size = new System.Drawing.Size(140, 30);
            this.btnLobbyBack.TabIndex = 10;
            this.btnLobbyBack.Text = "🔙 샌드박스 목록";
            this.btnLobbyBack.UseVisualStyleBackColor = true;
            this.btnLobbyBack.Click += new System.EventHandler(this.btnLobbyBack_Click);
            // 
            // pnlGame
            // 
            this.pnlGame.Controls.Add(this.picHintScene);
            this.pnlGame.Controls.Add(this.picMainScene);
            this.pnlGame.Controls.Add(this.btnBackToLobby);
            this.pnlGame.Controls.Add(this.btnGiveUp);
            this.pnlGame.Controls.Add(this.btnSubmitAnswer);
            this.pnlGame.Controls.Add(this.btnShowHint);
            this.pnlGame.Controls.Add(this.btnSendQuestion);
            this.pnlGame.Controls.Add(this.txtQuestionInput);
            this.pnlGame.Controls.Add(this.rtbChatLog);
            this.pnlGame.Controls.Add(this.txtHint);
            this.pnlGame.Controls.Add(this.txtSituation);
            this.pnlGame.Controls.Add(this.lblAiStatus);
            this.pnlGame.Controls.Add(this.lblQuestionCount);
            this.pnlGame.Controls.Add(this.cmbCaseList);
            this.pnlGame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGame.Location = new System.Drawing.Point(0, 0);
            this.pnlGame.Name = "pnlGame";
            this.pnlGame.Size = new System.Drawing.Size(615, 425);
            this.pnlGame.TabIndex = 1;
            this.pnlGame.Visible = false;
            // 
            // cmbCaseList
            // 
            this.cmbCaseList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCaseList.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.cmbCaseList.FormattingEnabled = true;
            this.cmbCaseList.Location = new System.Drawing.Point(12, 10);
            this.cmbCaseList.Name = "cmbCaseList";
            this.cmbCaseList.Size = new System.Drawing.Size(195, 23);
            this.cmbCaseList.TabIndex = 0;
            this.cmbCaseList.SelectedIndexChanged += new System.EventHandler(this.cmbCaseList_SelectedIndexChanged);
            // 
            // lblQuestionCount
            // 
            this.lblQuestionCount.AutoSize = true;
            this.lblQuestionCount.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblQuestionCount.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lblQuestionCount.Location = new System.Drawing.Point(213, 13);
            this.lblQuestionCount.Name = "lblQuestionCount";
            this.lblQuestionCount.Size = new System.Drawing.Size(140, 17);
            this.lblQuestionCount.TabIndex = 1;
            this.lblQuestionCount.Text = "[ 남은 기회: 10 / 10 ]";
            // 
            // lblAiStatus
            // 
            this.lblAiStatus.AutoSize = true;
            this.lblAiStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblAiStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblAiStatus.Location = new System.Drawing.Point(342, 14);
            this.lblAiStatus.Name = "lblAiStatus";
            this.lblAiStatus.Size = new System.Drawing.Size(89, 15);
            this.lblAiStatus.TabIndex = 12;
            this.lblAiStatus.Text = "○ 오프라인 모드";
            // 
            // btnGiveUp
            // 
            this.btnGiveUp.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnGiveUp.Location = new System.Drawing.Point(433, 7);
            this.btnGiveUp.Name = "btnGiveUp";
            this.btnGiveUp.Size = new System.Drawing.Size(87, 27);
            this.btnGiveUp.TabIndex = 14;
            this.btnGiveUp.Text = "🏳️ 정답 확인";
            this.btnGiveUp.UseVisualStyleBackColor = true;
            this.btnGiveUp.Click += new System.EventHandler(this.btnGiveUp_Click);
            // 
            // btnBackToLobby
            // 
            this.btnBackToLobby.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnBackToLobby.Location = new System.Drawing.Point(525, 7);
            this.btnBackToLobby.Name = "btnBackToLobby";
            this.btnBackToLobby.Size = new System.Drawing.Size(78, 27);
            this.btnBackToLobby.TabIndex = 9;
            this.btnBackToLobby.Text = "메인으로";
            this.btnBackToLobby.UseVisualStyleBackColor = true;
            this.btnBackToLobby.Click += new System.EventHandler(this.btnBackToLobby_Click);
            // 
            // txtSituation
            // 
            this.txtSituation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(251)))), ((int)(((byte)(253)))));
            this.txtSituation.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSituation.Font = new System.Drawing.Font("맑은 고딕", 9.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.txtSituation.Location = new System.Drawing.Point(12, 42);
            this.txtSituation.Multiline = true;
            this.txtSituation.Name = "txtSituation";
            this.txtSituation.ReadOnly = true;
            this.txtSituation.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtSituation.Size = new System.Drawing.Size(280, 125);
            this.txtSituation.TabIndex = 2;
            // 
            // txtHint
            // 
            this.txtHint.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(254)))), ((int)(((byte)(248)))));
            this.txtHint.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtHint.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.txtHint.Location = new System.Drawing.Point(12, 173);
            this.txtHint.Multiline = true;
            this.txtHint.Name = "txtHint";
            this.txtHint.ReadOnly = true;
            this.txtHint.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtHint.Size = new System.Drawing.Size(280, 70);
            this.txtHint.TabIndex = 3;
            // 
            // btnShowHint
            // 
            this.btnShowHint.Font = new System.Drawing.Font("맑은 고딕", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnShowHint.Location = new System.Drawing.Point(12, 249);
            this.btnShowHint.Name = "btnShowHint";
            this.btnShowHint.Size = new System.Drawing.Size(135, 29);
            this.btnShowHint.TabIndex = 7;
            this.btnShowHint.Text = "💡 힌트보기 (-2회)";
            this.btnShowHint.UseVisualStyleBackColor = true;
            this.btnShowHint.Click += new System.EventHandler(this.btnShowHint_Click);
            // 
            // btnSubmitAnswer
            // 
            this.btnSubmitAnswer.BackColor = System.Drawing.Color.MidnightBlue;
            this.btnSubmitAnswer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSubmitAnswer.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnSubmitAnswer.ForeColor = System.Drawing.Color.White;
            this.btnSubmitAnswer.Location = new System.Drawing.Point(153, 249);
            this.btnSubmitAnswer.Name = "btnSubmitAnswer";
            this.btnSubmitAnswer.Size = new System.Drawing.Size(139, 29);
            this.btnSubmitAnswer.TabIndex = 8;
            this.btnSubmitAnswer.Text = "🏆 정답 도전";
            this.btnSubmitAnswer.UseVisualStyleBackColor = false;
            this.btnSubmitAnswer.Click += new System.EventHandler(this.btnSubmitAnswer_Click);
            // 
            // picMainScene
            // 
            this.picMainScene.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(250)))));
            this.picMainScene.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picMainScene.Location = new System.Drawing.Point(12, 285);
            this.picMainScene.Name = "picMainScene";
            this.picMainScene.Size = new System.Drawing.Size(135, 128);
            this.picMainScene.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picMainScene.TabIndex = 10;
            this.picMainScene.TabStop = false;
            // 
            // picHintScene
            // 
            this.picHintScene.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(250)))));
            this.picHintScene.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picHintScene.Location = new System.Drawing.Point(153, 285);
            this.picHintScene.Name = "picHintScene";
            this.picHintScene.Size = new System.Drawing.Size(139, 128);
            this.picHintScene.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picHintScene.TabIndex = 11;
            this.picHintScene.TabStop = false;
            // 
            // rtbChatLog
            // 
            this.rtbChatLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(253)))), ((int)(((byte)(254)))), ((int)(((byte)(255)))));
            this.rtbChatLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rtbChatLog.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.rtbChatLog.Location = new System.Drawing.Point(300, 42);
            this.rtbChatLog.Name = "rtbChatLog";
            this.rtbChatLog.ReadOnly = true;
            this.rtbChatLog.Size = new System.Drawing.Size(303, 338);
            this.rtbChatLog.TabIndex = 4;
            this.rtbChatLog.Text = "";
            // 
            // txtQuestionInput
            // 
            this.txtQuestionInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtQuestionInput.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.txtQuestionInput.Location = new System.Drawing.Point(300, 388);
            this.txtQuestionInput.Name = "txtQuestionInput";
            this.txtQuestionInput.Size = new System.Drawing.Size(225, 25);
            this.txtQuestionInput.TabIndex = 5;
            this.txtQuestionInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtQuestionInput_KeyDown);
            // 
            // btnSendQuestion
            // 
            this.btnSendQuestion.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.btnSendQuestion.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSendQuestion.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnSendQuestion.ForeColor = System.Drawing.Color.White;
            this.btnSendQuestion.Location = new System.Drawing.Point(530, 386);
            this.btnSendQuestion.Name = "btnSendQuestion";
            this.btnSendQuestion.Size = new System.Drawing.Size(73, 28);
            this.btnSendQuestion.TabIndex = 6;
            this.btnSendQuestion.Text = "전송";
            this.btnSendQuestion.UseVisualStyleBackColor = false;
            this.btnSendQuestion.Click += new System.EventHandler(this.btnSendQuestion_Click);
            // 
            // MysteryTimeControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlLobby);
            this.Controls.Add(this.pnlGame);
            this.Name = "MysteryTimeControl";
            this.Size = new System.Drawing.Size(615, 425);
            this.Load += new System.EventHandler(this.MysteryTimeControl_Load);
            this.pnlLobby.ResumeLayout(false);
            this.pnlLobby.PerformLayout();
            this.pnlGame.ResumeLayout(false);
            this.pnlGame.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMainScene)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picHintScene)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlLobby;
        private System.Windows.Forms.Label lblLobbyTitle;
        private System.Windows.Forms.Label lblLobbySub;
        private System.Windows.Forms.Label lblDifficulty;
        private System.Windows.Forms.ComboBox cmbDifficulty;
        private System.Windows.Forms.Button btnAiGenerateCase;
        private System.Windows.Forms.Label lblLobbyCase;
        private System.Windows.Forms.ComboBox cmbLobbyCase;
        private System.Windows.Forms.Label lblCaseDesc;
        private System.Windows.Forms.Label lblLobbyAiStatus;
        private System.Windows.Forms.Button btnLobbySettings;
        private System.Windows.Forms.Button btnStartGame;
        private System.Windows.Forms.Button btnLobbyBack;

        private System.Windows.Forms.Panel pnlGame;
        private System.Windows.Forms.ComboBox cmbCaseList;
        private System.Windows.Forms.Label lblQuestionCount;
        private System.Windows.Forms.Label lblAiStatus;
        private System.Windows.Forms.Button btnGiveUp;
        private System.Windows.Forms.Button btnBackToLobby;
        private System.Windows.Forms.TextBox txtSituation;
        private System.Windows.Forms.TextBox txtHint;
        private System.Windows.Forms.RichTextBox rtbChatLog;
        private System.Windows.Forms.TextBox txtQuestionInput;
        private System.Windows.Forms.Button btnSendQuestion;
        private System.Windows.Forms.Button btnShowHint;
        private System.Windows.Forms.Button btnSubmitAnswer;
        private System.Windows.Forms.PictureBox picMainScene;
        private System.Windows.Forms.PictureBox picHintScene;
    }
}
