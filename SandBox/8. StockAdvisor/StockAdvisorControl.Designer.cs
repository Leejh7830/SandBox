namespace GateHelper.StockAdvisor
{
    partial class StockAdvisorControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StockToastNotification.AlertLogged -= OnAlertLogged;
                _autoSyncTimer?.Stop();
                _autoSyncTimer?.Dispose();
                _kbSyncTimer?.Stop();
                _kbSyncTimer?.Dispose();
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();
            // 
            // StockAdvisorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(20)))), ((int)(((byte)(26)))));
            this.DoubleBuffered = true;
            this.Name = "StockAdvisorControl";
            this.Size = new System.Drawing.Size(950, 650);
            this.ResumeLayout(false);
        }
    }
}

