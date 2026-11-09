namespace GateHelper
{
    partial class GameListControl
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
            this.GL_FlowPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.GL_btnSelectBitFlip = new MaterialSkin.Controls.MaterialCard();
            this.materialLabel1 = new MaterialSkin.Controls.MaterialLabel();
            this.GL_btnSelectSignalLink = new MaterialSkin.Controls.MaterialCard();
            this.materialLabel2 = new MaterialSkin.Controls.MaterialLabel();
            this.GL_btnSelectMysteryTime = new MaterialSkin.Controls.MaterialCard();
            this.materialLabel3 = new MaterialSkin.Controls.MaterialLabel();
            this.GL_btnSelectAiTrend = new MaterialSkin.Controls.MaterialCard();
            this.materialLabelAiTrend = new MaterialSkin.Controls.MaterialLabel();
            this.GL_btnSelectServerTycoon = new MaterialSkin.Controls.MaterialCard();
            this.materialLabelServerTycoon = new MaterialSkin.Controls.MaterialLabel();
            this.GL_btnSelectDataFlip = new MaterialSkin.Controls.MaterialCard();
            this.materialLabelDataFlip = new MaterialSkin.Controls.MaterialLabel();
            this.GL_btnSelectPocketFarm = new MaterialSkin.Controls.MaterialCard();
            this.materialLabelPocketFarm = new MaterialSkin.Controls.MaterialLabel();
            this.GL_FlowPanel1.SuspendLayout();
            this.GL_btnSelectBitFlip.SuspendLayout();
            this.GL_btnSelectSignalLink.SuspendLayout();
            this.GL_btnSelectMysteryTime.SuspendLayout();
            this.GL_btnSelectAiTrend.SuspendLayout();
            this.GL_btnSelectServerTycoon.SuspendLayout();
            this.GL_btnSelectDataFlip.SuspendLayout();
            this.GL_btnSelectPocketFarm.SuspendLayout();
            this.SuspendLayout();
            // 
            // GL_FlowPanel1
            // 
            this.GL_FlowPanel1.Controls.Add(this.GL_btnSelectBitFlip);
            this.GL_FlowPanel1.Controls.Add(this.GL_btnSelectSignalLink);
            this.GL_FlowPanel1.Controls.Add(this.GL_btnSelectMysteryTime);
            this.GL_FlowPanel1.Controls.Add(this.GL_btnSelectAiTrend);
            this.GL_FlowPanel1.Controls.Add(this.GL_btnSelectServerTycoon);
            this.GL_FlowPanel1.Controls.Add(this.GL_btnSelectDataFlip);
            this.GL_FlowPanel1.Controls.Add(this.GL_btnSelectPocketFarm);
            this.GL_FlowPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GL_FlowPanel1.Location = new System.Drawing.Point(0, 0);
            this.GL_FlowPanel1.Name = "GL_FlowPanel1";
            this.GL_FlowPanel1.Size = new System.Drawing.Size(500, 500);
            this.GL_FlowPanel1.TabIndex = 0;
            // 
            // GL_btnSelectBitFlip
            // 
            this.GL_btnSelectBitFlip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.GL_btnSelectBitFlip.Controls.Add(this.materialLabel1);
            this.GL_btnSelectBitFlip.Depth = 0;
            this.GL_btnSelectBitFlip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.GL_btnSelectBitFlip.Location = new System.Drawing.Point(14, 14);
            this.GL_btnSelectBitFlip.Margin = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectBitFlip.MouseState = MaterialSkin.MouseState.HOVER;
            this.GL_btnSelectBitFlip.Name = "GL_btnSelectBitFlip";
            this.GL_btnSelectBitFlip.Padding = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectBitFlip.Size = new System.Drawing.Size(150, 70);
            this.GL_btnSelectBitFlip.TabIndex = 0;
            this.GL_btnSelectBitFlip.Click += new System.EventHandler(this.btnSelectBitFlip_Click);
            // 
            // materialLabel1
            // 
            this.materialLabel1.AutoSize = true;
            this.materialLabel1.Depth = 0;
            this.materialLabel1.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.materialLabel1.Location = new System.Drawing.Point(54, 26);
            this.materialLabel1.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialLabel1.Name = "materialLabel1";
            this.materialLabel1.Size = new System.Drawing.Size(50, 19);
            this.materialLabel1.TabIndex = 2;
            this.materialLabel1.Text = "Bit Flip";
            // 
            // GL_btnSelectSignalLink
            // 
            this.GL_btnSelectSignalLink.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.GL_btnSelectSignalLink.Controls.Add(this.materialLabel2);
            this.GL_btnSelectSignalLink.Depth = 0;
            this.GL_btnSelectSignalLink.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.GL_btnSelectSignalLink.Location = new System.Drawing.Point(192, 14);
            this.GL_btnSelectSignalLink.Margin = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectSignalLink.MouseState = MaterialSkin.MouseState.HOVER;
            this.GL_btnSelectSignalLink.Name = "GL_btnSelectSignalLink";
            this.GL_btnSelectSignalLink.Padding = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectSignalLink.Size = new System.Drawing.Size(150, 70);
            this.GL_btnSelectSignalLink.TabIndex = 1;
            this.GL_btnSelectSignalLink.Click += new System.EventHandler(this.GL_btnSelectSignalLink_Click);
            // 
            // materialLabel2
            // 
            this.materialLabel2.AutoSize = true;
            this.materialLabel2.Depth = 0;
            this.materialLabel2.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.materialLabel2.Location = new System.Drawing.Point(36, 26);
            this.materialLabel2.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialLabel2.Name = "materialLabel2";
            this.materialLabel2.Size = new System.Drawing.Size(80, 19);
            this.materialLabel2.TabIndex = 3;
            this.materialLabel2.Text = "Signal Link";
            // 
            // GL_btnSelectMysteryTime
            // 
            this.GL_btnSelectMysteryTime.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.GL_btnSelectMysteryTime.Controls.Add(this.materialLabel3);
            this.GL_btnSelectMysteryTime.Depth = 0;
            this.GL_btnSelectMysteryTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.GL_btnSelectMysteryTime.Location = new System.Drawing.Point(14, 112);
            this.GL_btnSelectMysteryTime.Margin = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectMysteryTime.MouseState = MaterialSkin.MouseState.HOVER;
            this.GL_btnSelectMysteryTime.Name = "GL_btnSelectMysteryTime";
            this.GL_btnSelectMysteryTime.Padding = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectMysteryTime.Size = new System.Drawing.Size(150, 70);
            this.GL_btnSelectMysteryTime.TabIndex = 2;
            this.GL_btnSelectMysteryTime.Click += new System.EventHandler(this.btnSelectMysteryTime_Click);
            // 
            // materialLabel3
            // 
            this.materialLabel3.AutoSize = true;
            this.materialLabel3.Depth = 0;
            this.materialLabel3.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.materialLabel3.Location = new System.Drawing.Point(26, 26);
            this.materialLabel3.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialLabel3.Name = "materialLabel3";
            this.materialLabel3.Size = new System.Drawing.Size(98, 19);
            this.materialLabel3.TabIndex = 4;
            this.materialLabel3.Text = "Mystery Time";
            // 
            // GL_btnSelectAiTrend
            // 
            this.GL_btnSelectAiTrend.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.GL_btnSelectAiTrend.Controls.Add(this.materialLabelAiTrend);
            this.GL_btnSelectAiTrend.Depth = 0;
            this.GL_btnSelectAiTrend.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.GL_btnSelectAiTrend.Location = new System.Drawing.Point(192, 112);
            this.GL_btnSelectAiTrend.Margin = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectAiTrend.MouseState = MaterialSkin.MouseState.HOVER;
            this.GL_btnSelectAiTrend.Name = "GL_btnSelectAiTrend";
            this.GL_btnSelectAiTrend.Padding = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectAiTrend.Size = new System.Drawing.Size(150, 70);
            this.GL_btnSelectAiTrend.TabIndex = 3;
            this.GL_btnSelectAiTrend.Click += new System.EventHandler(this.GL_btnSelectAiTrend_Click);
            // 
            // materialLabelAiTrend
            // 
            this.materialLabelAiTrend.AutoSize = true;
            this.materialLabelAiTrend.Depth = 0;
            this.materialLabelAiTrend.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.materialLabelAiTrend.Location = new System.Drawing.Point(40, 26);
            this.materialLabelAiTrend.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialLabelAiTrend.Name = "materialLabelAiTrend";
            this.materialLabelAiTrend.Size = new System.Drawing.Size(65, 19);
            this.materialLabelAiTrend.TabIndex = 5;
            this.materialLabelAiTrend.Text = "AI Trend";
            // 
            // GL_btnSelectServerTycoon
            // 
            this.GL_btnSelectServerTycoon.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.GL_btnSelectServerTycoon.Controls.Add(this.materialLabelServerTycoon);
            this.GL_btnSelectServerTycoon.Depth = 0;
            this.GL_btnSelectServerTycoon.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.GL_btnSelectServerTycoon.Location = new System.Drawing.Point(14, 210);
            this.GL_btnSelectServerTycoon.Margin = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectServerTycoon.MouseState = MaterialSkin.MouseState.HOVER;
            this.GL_btnSelectServerTycoon.Name = "GL_btnSelectServerTycoon";
            this.GL_btnSelectServerTycoon.Padding = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectServerTycoon.Size = new System.Drawing.Size(150, 70);
            this.GL_btnSelectServerTycoon.TabIndex = 4;
            this.GL_btnSelectServerTycoon.Click += new System.EventHandler(this.GL_btnSelectServerTycoon_Click);
            // 
            // materialLabelServerTycoon
            // 
            this.materialLabelServerTycoon.AutoSize = true;
            this.materialLabelServerTycoon.Depth = 0;
            this.materialLabelServerTycoon.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.materialLabelServerTycoon.Location = new System.Drawing.Point(22, 26);
            this.materialLabelServerTycoon.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialLabelServerTycoon.Name = "materialLabelServerTycoon";
            this.materialLabelServerTycoon.Size = new System.Drawing.Size(106, 19);
            this.materialLabelServerTycoon.TabIndex = 6;
            this.materialLabelServerTycoon.Text = "Server Tycoon";
            this.materialLabelServerTycoon.Click += new System.EventHandler(this.GL_btnSelectServerTycoon_Click);
            // 
            // GL_btnSelectDataFlip
            // 
            this.GL_btnSelectDataFlip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.GL_btnSelectDataFlip.Controls.Add(this.materialLabelDataFlip);
            this.GL_btnSelectDataFlip.Depth = 0;
            this.GL_btnSelectDataFlip.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.GL_btnSelectDataFlip.Location = new System.Drawing.Point(192, 210);
            this.GL_btnSelectDataFlip.Margin = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectDataFlip.MouseState = MaterialSkin.MouseState.HOVER;
            this.GL_btnSelectDataFlip.Name = "GL_btnSelectDataFlip";
            this.GL_btnSelectDataFlip.Padding = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectDataFlip.Size = new System.Drawing.Size(150, 70);
            this.GL_btnSelectDataFlip.TabIndex = 5;
            this.GL_btnSelectDataFlip.Click += new System.EventHandler(this.GL_btnSelectDataFlip_Click);
            // 
            // materialLabelDataFlip
            // 
            this.materialLabelDataFlip.AutoSize = true;
            this.materialLabelDataFlip.Depth = 0;
            this.materialLabelDataFlip.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.materialLabelDataFlip.Location = new System.Drawing.Point(36, 26);
            this.materialLabelDataFlip.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialLabelDataFlip.Name = "materialLabelDataFlip";
            this.materialLabelDataFlip.Size = new System.Drawing.Size(76, 19);
            this.materialLabelDataFlip.TabIndex = 7;
            this.materialLabelDataFlip.Text = "Core Front";
            this.materialLabelDataFlip.Click += new System.EventHandler(this.GL_btnSelectDataFlip_Click);
            // 
            // GL_btnSelectPocketFarm
            // 
            this.GL_btnSelectPocketFarm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.GL_btnSelectPocketFarm.Controls.Add(this.materialLabelPocketFarm);
            this.GL_btnSelectPocketFarm.Depth = 0;
            this.GL_btnSelectPocketFarm.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.GL_btnSelectPocketFarm.Location = new System.Drawing.Point(14, 308);
            this.GL_btnSelectPocketFarm.Margin = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectPocketFarm.MouseState = MaterialSkin.MouseState.HOVER;
            this.GL_btnSelectPocketFarm.Name = "GL_btnSelectPocketFarm";
            this.GL_btnSelectPocketFarm.Padding = new System.Windows.Forms.Padding(14);
            this.GL_btnSelectPocketFarm.Size = new System.Drawing.Size(150, 70);
            this.GL_btnSelectPocketFarm.TabIndex = 6;
            this.GL_btnSelectPocketFarm.Click += new System.EventHandler(this.GL_btnSelectPocketFarm_Click);
            // 
            // materialLabelPocketFarm
            // 
            this.materialLabelPocketFarm.AutoSize = true;
            this.materialLabelPocketFarm.Depth = 0;
            this.materialLabelPocketFarm.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.materialLabelPocketFarm.Location = new System.Drawing.Point(30, 26);
            this.materialLabelPocketFarm.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialLabelPocketFarm.Name = "materialLabelPocketFarm";
            this.materialLabelPocketFarm.Size = new System.Drawing.Size(90, 19);
            this.materialLabelPocketFarm.TabIndex = 8;
            this.materialLabelPocketFarm.Text = "Pocket Farm";
            this.materialLabelPocketFarm.Click += new System.EventHandler(this.GL_btnSelectPocketFarm_Click);
            // 
            // GameListControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.GL_FlowPanel1);
            this.Name = "GameListControl";
            this.Size = new System.Drawing.Size(500, 500);
            this.GL_FlowPanel1.ResumeLayout(false);
            this.GL_btnSelectBitFlip.ResumeLayout(false);
            this.GL_btnSelectBitFlip.PerformLayout();
            this.GL_btnSelectSignalLink.ResumeLayout(false);
            this.GL_btnSelectSignalLink.PerformLayout();
            this.GL_btnSelectMysteryTime.ResumeLayout(false);
            this.GL_btnSelectMysteryTime.PerformLayout();
            this.GL_btnSelectAiTrend.ResumeLayout(false);
            this.GL_btnSelectAiTrend.PerformLayout();
            this.GL_btnSelectServerTycoon.ResumeLayout(false);
            this.GL_btnSelectServerTycoon.PerformLayout();
            this.GL_btnSelectDataFlip.ResumeLayout(false);
            this.GL_btnSelectDataFlip.PerformLayout();
            this.GL_btnSelectPocketFarm.ResumeLayout(false);
            this.GL_btnSelectPocketFarm.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel GL_FlowPanel1;
        private MaterialSkin.Controls.MaterialCard GL_btnSelectBitFlip;
        private MaterialSkin.Controls.MaterialCard GL_btnSelectSignalLink;
        private MaterialSkin.Controls.MaterialCard GL_btnSelectMysteryTime;
        private MaterialSkin.Controls.MaterialCard GL_btnSelectAiTrend;
        private MaterialSkin.Controls.MaterialCard GL_btnSelectServerTycoon;
        private MaterialSkin.Controls.MaterialCard GL_btnSelectDataFlip;
        private MaterialSkin.Controls.MaterialCard GL_btnSelectPocketFarm;
        private MaterialSkin.Controls.MaterialLabel materialLabel1;
        private MaterialSkin.Controls.MaterialLabel materialLabel2;
        private MaterialSkin.Controls.MaterialLabel materialLabel3;
        private MaterialSkin.Controls.MaterialLabel materialLabelAiTrend;
        private MaterialSkin.Controls.MaterialLabel materialLabelServerTycoon;
        private MaterialSkin.Controls.MaterialLabel materialLabelDataFlip;
        private MaterialSkin.Controls.MaterialLabel materialLabelPocketFarm;
    }
}
