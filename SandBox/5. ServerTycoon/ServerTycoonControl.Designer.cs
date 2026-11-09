namespace GateHelper.ServerTycoon
{
    partial class ServerTycoonControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_renderTimer != null)
                {
                    _renderTimer.Stop();
                    _renderTimer.Tick -= RenderTimer_Tick;
                    _renderTimer.Dispose();
                    _renderTimer = null;
                }
                if (_logicTimer != null)
                {
                    _logicTimer.Stop();
                    _logicTimer.Tick -= LogicTimer_Tick;
                    _logicTimer.Dispose();
                    _logicTimer = null;
                }
                if (_state != null)
                {
                    _state.Save();
                }
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ServerTycoonControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(20)))), ((int)(((byte)(26)))));
            this.DoubleBuffered = true;
            this.Name = "ServerTycoonControl";
            this.Size = new System.Drawing.Size(636, 605);
            this.ResumeLayout(false);
        }
    }
}
